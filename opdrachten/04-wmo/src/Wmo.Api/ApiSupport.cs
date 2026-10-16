using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Wmo.Application;

namespace Wmo.Api;

public static class Pagination
{
    public static (int Page, int Size) Validate(int? page, int? size)
    {
        var number = page ?? 1;
        var count = size ?? 50;
        if (number < 1 || number > 1_000_000 || count < 1 || count > 100)
            throw new InputException("Pagina moet tussen 1 en 1000000 liggen; paginagrootte tussen 1 en 100.");
        return (number, count);
    }
}

public sealed partial class ExplicitOffsetConverter : JsonConverter<DateTimeOffset>
{
    [GeneratedRegex(@"T.*(?:Z|[+-]\d{2}:\d{2})$", RegexOptions.CultureInvariant)]
    private static partial Regex OffsetPattern();
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (value is null || !OffsetPattern().IsMatch(value) ||
            !DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            throw new JsonException("Gebruik een ISO 8601-tijdstip met Z of expliciete offset.");
        return result;
    }
    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToUniversalTime());
}

public sealed class RenewalWorker(IServiceScopeFactory scopes, IConfiguration configuration, ILogger<RenewalWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var seconds = configuration.GetValue("Renewal:IntervalSeconds", 3600);
        if (seconds is < 1 or > 86400) throw new InvalidOperationException("Ongeldig vernieuwingsinterval.");
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(seconds));
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var count = await scope.ServiceProvider.GetRequiredService<RenewalService>()
                    .RenewAsync(new("system:renewal-worker", Guid.NewGuid().ToString("N")), stoppingToken);
                logger.LogInformation("RenewalCompleted {Count}", count);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogError("RenewalFailed {ExceptionType}; volgende uitvoering probeert opnieuw", ex.GetType().Name);
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
