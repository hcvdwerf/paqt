workspace "PAQT Back-End 2026" "Vier zelfstandige opdrachten en een lokale Wmo-opstelling." {
    model {
        candidate = person "Kandidaat / reviewer" "Voert de opdrachten en tests lokaal uit."
        callcenter = person "Callcenter" "Haalt Utrechtse bewoners op en boekt ritten."
        carrier = person "Taxibedrijf" "Bekijkt uitsluitend eigen ritten."
        maintenance = person "Technische beheerclient" "Kan verschuldigde verlengingen activeren."

        paqt = softwareSystem "PAQT-codeopdrachten" "Zelfstandige algoritmeopdrachten en Wmo-taxivervoer." {
            fizzbuzz = container "01 FizzBuzz" "Oplopende getallen met Fizz/Buzz-vervangingen; eigen tests." "C# / .NET 10 console"
            collections = container "02 Verzamelingen" "Verdeelt een lijst zonder waarden of volgorde te verliezen; eigen tests." "C# / .NET 10 console"
            weeks = container "03 Volledige weken" "Berekent maandagen van volledig ingesloten weken; eigen tests." "C# / .NET 10 console"
            kong = container "Kong Gateway" "Enige toegang tot de Wmo-API; TLS, routing, limieten. DB-less." "Kong OSS"
            keycloak = container "Identityprovider" "Lokale tokenuitgifte voor clients met afgebakende rollen." "Keycloak / OAuth 2.0"
            database = container "Persistente opslag" "Gescheiden databases wmo, wmo_test en keycloak; aparte accounts." "PostgreSQL 17" "Database"
            api = container "Wmo-API" "Modulaire monoliet; autorisatie, boeken, ritinzage en jaarlijkse budgetreset." "ASP.NET Core 10" {
                http = component "HTTP-contract" "Versieerde routes, DTO's, validatie en Problem Details." "Minimal APIs"
                authorization = component "Authenticatie en autorisatie" "JWT-validatie met JWKS, rollen en vertrouwde vervoerderclaim." "ASP.NET Core JwtBearer"
                booking = component "Boeken" "Selecteert vanuit woonadres de vervoerder en boekt kilometers transactioneel af." "Wmo.Application"
                renewal = component "Verlengen" "Verwerkt verschuldigde verjaardagen en haalt gemiste uitvoeringen in." "Wmo.Application"
                scheduler = component "Periodieke taak" "Start direct en vervolgens elk uur; eigen dependency scope per uitvoering." "BackgroundService"
                queries = component "Gegevensinzage" "Gepagineerde bewonersinzage en filtering op eigen taxibedrijf." "Wmo.Application-contract"
                domain = component "Domeinregels" "Geldigheid, budgetperioden, kilometerbudget en schrikkeldagen." "Wmo.Domain"
                storage = component "Opslagadapter" "Implementeert kleine applicatie-interfaces; transacties, row locks en audittrail." "Wmo.Infrastructure / EF Core"
            }
        }

        candidate -> fizzbuzz "Voert uit" "Console"
        candidate -> collections "Voert uit" "Console"
        candidate -> weeks "Voert uit" "Console"
        callcenter -> keycloak "Vraagt callcentertoken aan" "HTTPS / client credentials"
        carrier -> keycloak "Vraagt bedrijfstoken aan" "HTTPS / client credentials"
        maintenance -> keycloak "Vraagt technisch token aan" "HTTPS / client credentials"
        callcenter -> kong "Bewoners ophalen en rit boeken" "HTTPS / JSON / Bearer"
        carrier -> kong "Eigen ritten ophalen" "HTTPS / JSON / Bearer"
        maintenance -> kong "Verschuldigde budgetverlengingen activeren" "HTTPS / Bearer"
        kong -> http "Stuurt requests door; valideert upstream-certificaat" "HTTPS"
        http -> authorization "Controleert identiteit en rechten"
        authorization -> keycloak "Haalt discovery en ondertekeningssleutels op" "HTTPS / OIDC / JWKS"
        http -> booking "Boekt een rit"
        http -> renewal "Activeert verlenging"
        http -> queries "Vraagt toegestane gegevens op"
        scheduler -> renewal "Activeert periodieke verlenging"
        booking -> domain "Past budget- en geldigheidsregels toe"
        renewal -> domain "Bepaalt nieuwe jaarperiode"
        booking -> storage "Gebruikt boekings-, transactie- en auditcontracten"
        renewal -> storage "Vergrendelt en vernieuwt beschikkingen"
        queries -> storage "Gebruikt querycontract"
        storage -> database "Leest/schrijft wmo; append-only audit voor runtime-account" "PostgreSQL / TLS"
        keycloak -> database "Beheert uitsluitend keycloak-database" "JDBC / TLS"
    }
    views {
        systemContext paqt "context" {
            include *
            autolayout lr
            title "C4 niveau 1 - PAQT-codeopdrachten"
        }
        container paqt "containers" {
            include *
            autolayout lr
            title "C4 niveau 2 - Vier opdrachten en lokale infrastructuur"
        }
        component api "wmo-components" {
            include *
            autolayout lr
            title "C4 niveau 3 - Wmo-verantwoordelijkheden en afhankelijkheden"
        }
        styles {
            element "Person" {
                shape person
                background #173d63
                color #ffffff
            }
            element "Software System" {
                background #173d63
                color #ffffff
            }
            element "Container" {
                background #247a9e
                color #ffffff
            }
            element "Component" {
                background #d8edf5
                color #173d63
            }
            element "Database" {
                shape cylinder
            }
        }
    }
}
