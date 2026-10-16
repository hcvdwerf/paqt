using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Npgsql;
using Wmo.Api;
using Wmo.Application;
using Wmo.Domain;
using Wmo.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(o => o.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ");
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
var dbConfig = builder.Configuration.GetSection("Database");
var connection = new NpgsqlConnectionStringBuilder
{
    Host = dbConfig["Host"] ?? "postgres",
    Database = dbConfig["Name"] ?? "wmo",
    Username = dbConfig["User"] ?? "wmo",
    Password = File.ReadAllText(dbConfig["PasswordFile"] ?? "/secrets/wmo-password").Trim(),
    SslMode = SslMode.VerifyFull,
    RootCertificate = dbConfig["RootCertificate"] ?? "/certs/ca.crt",
    Timeout = 10,
    CommandTimeout = 15,
    IncludeErrorDetail = false
};
connection["GSS Encryption Mode"] = "Disable";
builder.Services.AddDbContext<WmoDbContext>(o => o.UseNpgsql(connection.ConnectionString));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<BusinessClock>();
builder.Services.AddScoped<PostgresWmoStore>();
builder.Services.AddScoped<IUnitOfWork>(s => s.GetRequiredService<PostgresWmoStore>());
builder.Services.AddScoped<IBookingData>(s => s.GetRequiredService<PostgresWmoStore>());
builder.Services.AddScoped<IRenewalData>(s => s.GetRequiredService<PostgresWmoStore>());
builder.Services.AddScoped<IAuditWriter>(s => s.GetRequiredService<PostgresWmoStore>());
builder.Services.AddScoped<IWmoQueries>(s => s.GetRequiredService<PostgresWmoStore>());
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<RenewalService>();
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
    o.SerializerOptions.Converters.Add(new ExplicitOffsetConverter());
});
builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
{
    document.Info.Title = "PAQT Wmo-API";
    document.Info.Description = "Lokale demonstratie met fictieve gegevens. Gebruik een Keycloak-token met de rol die bij de handeling hoort.";
    document.Servers = [new OpenApiServer { Url = "/" }];
    document.Components ??= new();
    document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
    document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Keycloak-access-token, zonder het voorvoegsel Bearer. Rollen en vervoerdersscope worden door de API gecontroleerd."
    };
    foreach (var path in document.Paths.Values)
    {
        if (path.Operations is null) continue;
        foreach (var operation in path.Operations.Values)
        {
            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });
        }
    }
    return Task.CompletedTask;
}));
builder.Services.Configure<RouteHandlerOptions>(o => o.ThrowOnBadRequest = true);
var issuer = builder.Configuration["Authentication:Issuer"] ?? "https://localhost:8444/realms/paqt";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.Authority = issuer;
    o.MetadataAddress = builder.Configuration["Authentication:MetadataAddress"] ?? "https://keycloak:8443/realms/paqt/.well-known/openid-configuration";
    o.Audience = "wmo-api";
    o.MapInboundClaims = false;
    o.BackchannelTimeout = TimeSpan.FromSeconds(10);
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = issuer,
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        RequireSignedTokens = true,
        ClockSkew = TimeSpan.FromSeconds(10),
        RoleClaimType = "roles",
        NameClaimType = "sub",
        ValidAlgorithms = [SecurityAlgorithms.RsaSha256]
    };
});
builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("callcenter", p => p.RequireAuthenticatedUser().RequireRole("callcenter"));
    o.AddPolicy("carrier", p => p.RequireAuthenticatedUser().RequireRole("carrier").RequireAssertion(c =>
        Guid.TryParse(c.User.FindFirstValue("taxi_company_id"), out _)));
    o.AddPolicy("renewal", p => p.RequireAuthenticatedUser().RequireRole("renewal"));
});
builder.Services.AddHostedService<RenewalWorker>();
var app = builder.Build();

if (args.Contains("--migrate"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<WmoDbContext>();
    await db.Database.MigrateAsync();
    if (args.Contains("--seed-demo"))
        await SeedData.SeedAsync(db, scope.ServiceProvider.GetRequiredService<BusinessClock>().Today);
    return;
}

async Task WriteProblemAsync(HttpContext context, int status, string code, string message)
{
    context.Response.StatusCode = status;
    var problem = new ProblemDetails { Status = status, Title = message, Type = $"urn:paqt:problem:{code}" };
    problem.Extensions["code"] = code;
    problem.Extensions["correlationId"] = context.TraceIdentifier;
    await context.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
    {
        HttpContext = context,
        ProblemDetails = problem
    });
}

Actor RequestActor(HttpContext context) => new(context.User.FindFirstValue("sub")!, context.TraceIdentifier);

app.Use(async (context, next) =>
{
    // Geen door de client gekozen identificatie als vertrouwde auditreferentie gebruiken.
    context.TraceIdentifier = Guid.NewGuid().ToString("N");
    context.Response.Headers["X-Correlation-ID"] = context.TraceIdentifier;
    context.Response.Headers["Cache-Control"] = "no-store";
    var started = Stopwatch.GetTimestamp();
    try { await next(context); }
    catch (Exception ex) when (!context.RequestAborted.IsCancellationRequested)
    {
        var (status, code, message) = ex switch
        {
            InputException e => (400, "invalid_request", e.Message),
            BadHttpRequestException => (400, "invalid_request", "Het verzoek bevat ongeldige invoer."),
            NotFoundException => (404, "resident_not_found", "Bewoner niet gevonden."),
            DomainException e => (409, JsonNamingPolicy.SnakeCaseLower.ConvertName(e.Code.ToString()), e.Message),
            NpgsqlException => (503, "storage_unavailable", "De dienst is tijdelijk niet beschikbaar."),
            _ => (500, "internal_error", "Er is een interne fout opgetreden.")
        };
        if (status >= 500) app.Logger.LogError("RequestFailure {Code} {CorrelationId} {ExceptionType}", code, context.TraceIdentifier, ex.GetType().Name);
        await WriteProblemAsync(context, status, code, message);
    }
    app.Logger.LogInformation("RequestCompleted {Method} {Route} {Status} {ElapsedMs} {ActorId} {CorrelationId}",
        context.Request.Method, context.GetEndpoint()?.DisplayName ?? "unmatched", context.Response.StatusCode,
        Stopwatch.GetElapsedTime(started).TotalMilliseconds, context.User.FindFirstValue("sub") ?? "anonymous", context.TraceIdentifier);
});
app.UseStatusCodePages(async statusContext =>
{
    var ctx = statusContext.HttpContext;
    var (code, text) = ctx.Response.StatusCode switch
    {
        401 => ("unauthenticated", "Een geldig toegangstoken is vereist."),
        403 => ("forbidden", "Geen rechten voor deze handeling."),
        404 => ("not_found", "De gevraagde route bestaat niet."),
        _ => ("request_failed", "Het verzoek kon niet worden verwerkt.")
    };
    await WriteProblemAsync(ctx, ctx.Response.StatusCode, code, text);
});
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health/live", () => Results.Ok(new { status = "alive" })).ExcludeFromDescription();
app.MapGet("/health/ready", async (WmoDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct) ? Results.Ok(new { status = "ready" }) : Results.StatusCode(503)).ExcludeFromDescription();
app.MapOpenApi().AllowAnonymous();

if (builder.Configuration.GetValue<bool>("Swagger:Enabled"))
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "PAQT Wmo-API v1"));


app.MapGet("/api/v1/residents", async (int? page, int? pageSize, IWmoQueries queries, CancellationToken ct) =>
{
    var (number, size) = Pagination.Validate(page, pageSize);
    return Results.Ok(await queries.ResidentsAsync(number, size, ct));
}).RequireAuthorization("callcenter").WithName("ListResidents").WithSummary("Alle Utrechtse bewoners ophalen").WithDescription("Vereist rol callcenter. Paginagrootte 1-100.")
    .Produces<Page<ResidentView>>().ProducesProblem(400).ProducesProblem(401).ProducesProblem(403);

app.MapPost("/api/v1/residents/{residentId:guid}/trips", async (Guid residentId, BookingRequest request,
    BookingService service, HttpContext ctx, CancellationToken ct) =>
{
    var trip = await service.BookAsync(residentId, request, RequestActor(ctx), ct);
    return Results.Json(trip, statusCode: 201);
}).RequireAuthorization("callcenter").WithName("BookTrip").WithSummary("Rit boeken en kilometers afboeken").WithDescription("Vereist rol callcenter. De vervoerder volgt uit het woonadres.")
    .Produces<TripView>(201).ProducesProblem(400).ProducesProblem(401).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409);

app.MapGet("/api/v1/trips", async (int? page, int? pageSize, IWmoQueries queries, HttpContext ctx, CancellationToken ct) =>
{
    var (number, size) = Pagination.Validate(page, pageSize);
    var companyId = Guid.Parse(ctx.User.FindFirstValue("taxi_company_id")!);
    return Results.Ok(await queries.TripsAsync(companyId, number, size, ct));
}).RequireAuthorization("carrier").WithName("ListOwnTrips").WithSummary("Eigen vervoerdersritten ophalen").WithDescription("Vereist rol carrier en taxi_company_id in het gevalideerde token.")
    .Produces<Page<TripView>>().ProducesProblem(400).ProducesProblem(401).ProducesProblem(403);

app.MapPost("/api/v1/budget-renewals", async (RenewalService service, HttpContext ctx, CancellationToken ct) =>
    Results.Ok(new RenewalResult(await service.RenewAsync(RequestActor(ctx), ct))))
    .RequireAuthorization("renewal").WithName("RenewBudgets").WithSummary("Verschuldigde budgetperioden vernieuwen").WithDescription("Vereist technische rol renewal; datum volgt uit de systeemklok.")
    .Produces<RenewalResult>().ProducesProblem(401).ProducesProblem(403);

await app.RunAsync();

public partial class Program;
