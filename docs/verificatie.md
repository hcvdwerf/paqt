# Verificatie

Uitgevoerd op 16 oktober 2026 op macOS ARM64 met Docker Engine 29.6.2 en Compose 5.3.1. Alle applicatiebuilds en tests zijn in Linux-containers uitgevoerd.

## Functionele controles

| Controle | Resultaat |
|---|---|
| Solutionbuild (.NET SDK 10.0.402) | Geslaagd, 0 waarschuwingen en 0 fouten |
| Unittests | 30 geslaagd: FizzBuzz 3, verzamelingen 6, volledige weken 9, Wmo 12 |
| PostgreSQL-integratietests | 7 geslaagd |
| Playwright API-tests | 9 geslaagd, inclusief Swagger UI en HTTP 429 bij overschrijding van de Kong-verzoeklimiet |
| C# formatter/analyzers | `dotnet format --verify-no-changes --no-restore` geslaagd |
| Structurizr | DSL-validatie geslaagd; context-, container- en componentview beschikbaar in lokale viewer |
| Consolevoorbeelden | FizzBuzz 1-15, PDF-verdeelvoorbeeld en maandagen oktober 2026 uitgevoerd en gecontroleerd |
| TLS-/herstartsmoke | Readiness 200, 6 fictieve Utrechtse bewoners behouden, OpenAPI 200 met 4 routes en responsecontracten; echte Keycloak-tokenuitgifte werkt met gevalideerde lokale CA |
| Isolatie | Teststack bouwt eigen databases zonder hostpoorten; verwijdert uitsluitend eigen disposable testvolume |

In totaal zijn **46 tests geslaagd**, zonder overgeslagen of falende tests.

TRX/JUnit/HTML staan onder `artifacts/`. De source-secretcontrole staat in `scripts/verify-secrets.py`. Deze controle zoekt lokale gegenereerde secretwaarden, private sleutels en JWT-patronen; het is geen volledige SAST- of penetratietest.

## Dependency- en imagescans

NuGet controle met `--vulnerable --include-transitive`: geen bekende kwetsbare dependencies in de 12 projecten. npm-lockfilecontrole: geen bekende kwetsbaarheden. Dit zijn momentopnamen van de gebruikte adviesbronnen.

Docker Scout v1.24.0 onderzocht de volgende images op door de scanner als HIGH of CRITICAL aangemerkte bevindingen. De projectafspraak stelt geen algemene CVE-vrijgavegrens vast; onderstaande resultaten zijn dus geen formele risicoacceptatie.

| Image | HIGH | CRITICAL | Bewijs |
|---|---:|---:|---|
| Wmo-applicatie | 0 | 0 | `artifacts/scans/wmo-image.sarif` |
| Kong 3.9.4 | 0 | 0 | `artifacts/scans/kong-image.sarif` |
| Keycloak 26.8.1 | 11 | 1 | `artifacts/scans/keycloak-image.sarif` |
| PostgreSQL 17-alpine (officieel, onaangepast) | 32 | 4 | `artifacts/scans/postgres-image.sarif` |
| PostgreSQL 17-alpine (eigen build `infra/Dockerfile.postgres`) | 0 | 0 | Trivy 0.67.2, lokaal uitgevoerd (geen SARIF bewaard) |

### Open upstreambevindingen

- Keycloak: 12 bevindingen in de onderliggende RPM-pakketten acl, attr, glibc, pcre2, sqlite en zlib. De scanner meldt hiervoor `not fixed`. Trivy 0.67.2 vond op dezelfde image 0 HIGH/CRITICAL; dit is een ander resultaat dan Docker Scout. Zonder upstream-fix is dit niet door het project op te lossen. De officiële downloadpagina is gecontroleerd en versie 26.8.1 is gebruikt.
- PostgreSQL: de bevindingen zaten uitsluitend in de meegeleverde `gosu`-binary (verouderde Go-stdlib). `infra/Dockerfile.postgres` vervangt die door `su-exec` uit Alpine en levert een platte image, zodat de oude binary niet in een verborgen laag achterblijft. De Compose-stack gebruikt deze build; alle 46 tests slaagden daarna en de demo startte op het bestaande datavolume. De tabelregel van de onaangepaste officiële image blijft als historisch Docker Scout-bewijs. Trivy en Docker Scout kunnen verschillen in bevindingen: Scout is niet opnieuw uitgevoerd op de eigen build. Oorspronkelijke analyse: 36 bevindingen in meegeleverde Go-stdlib (gosu-helper), libxml2 en zlib. Voor een deel zijn fixversies bekend. De officiële tag `postgres:17-alpine` is opnieuw gepulld en verwees naar dezelfde gepinde digest; er is binnen deze uitvoering geen nieuwere officiële taginhoud aangetroffen. De packagebevindingen zijn niet als onbereikbaar of vals-positief afgedaan.
- Dit blijft een lokale demonstratie met fictieve gegevens en uitsluitend loopbacktoegang. Er is geen productie-securityvrijgave. Voor productie zijn herbeoordeling, geactualiseerde images of een onderbouwde alternatieve build en risicoacceptatie door de bevoegde eigenaar nodig.
- De overige toolingimages (SDK, Node-testrunner, bootstrap en Structurizr) vallen buiten deze runtime-imagescan. Licentieacceptatie, complete SAST, pentest, load-/failover-/hersteltests en formele CIZ-toetsing zijn niet uitgevoerd.

De oudere NuGet-baseline gaf aanvankelijk kwetsbaarheidsmeldingen. De implementatie is vóór de geslaagde eindbuild bijgewerkt naar ASP.NET/EF Core 10.0.13 en Npgsql EF-provider 10.0.3. Er zijn geen kwetsbaarheidswaarschuwingen onderdrukt.

## Uitbreiding Swagger UI

De standaard Swagger UI is beschikbaar achter Kong en laadt een publiek OpenAPI-contract met Bearer-authenticatie en een relatieve gateway-URL. Alleen het contract is publiek; API-aanroepen blijven geautoriseerd. De Playwright-controle dekt de UI-/asset-routes, het contract en de afwijzing van een anonieme API-aanroep. Ook de actieve demo is opnieuw gebouwd en gestart.

## CI-teststabiliteit (20 oktober 2026)

De GitHub Actions-run op commit `f5440fd` faalde in vier Playwright-tests: drie Keycloak-tokenaanvragen kregen HTTP 500 tijdens de startfase en één controle las de budgetperiode voordat de achtergrondvernieuwing klaar was. De tokenhelper probeert tijdelijke serverfouten nu begrensd opnieuw. De controle van de achtergrondtaak wacht maximaal 10 seconden op de vernieuwde periode. Na deze wijziging slaagde `./scripts/test.sh` lokaal: 30 unit-, 7 PostgreSQL-integratie- en 9 Playwright-tests, in totaal 46. De GitHub-rerun volgt op de commit van deze correctie.
