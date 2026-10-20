# PAQT - Codeopdrachten Back-End 2026

Vier afzonderlijke opdrachten in C# en **.NET 10 LTS**. De Wmo-casus is een modulaire monoliet met PostgreSQL, Keycloak en Kong. Alle onderdelen zijn lokaal met Docker te bouwen en te testen; een lokale .NET- of Node-installatie is niet nodig.

## Snel starten

Vereist: een draaiende Docker-engine met Docker Compose v2.24.4 of nieuwer, vrije poorten 8443 en 8444 en ruimte voor de images. ARM64 en AMD64 worden door de gebruikte basisimages ondersteund. Downloads zijn nodig tijdens bouwen; de API en database staan op een intern netwerk. Alleen Kong en Keycloak hebben daarnaast een ingressnetwerk voor de loopbackpoorten. Er worden tijdens de bedrijfsprocessen geen externe diensten aangeroepen.

```sh
./scripts/start.sh
```

Dit bouwt de software, genereert lokale demo-secrets en certificaten, initialiseert PostgreSQL, voert EF Core-migraties uit en voegt fictieve voorbeeldgegevens toe. Vervolgens starten Keycloak, de API en Kong. De eerste start kan enkele minuten duren. Gereedheid controleren:

```sh
curl --cacert .local/certs/ca.crt https://localhost:8443/health/ready
docker compose ps -a
```

| Voorziening | Adres |
|---|---|
| Wmo-API via Kong | https://localhost:8443 |
| Swagger UI | https://localhost:8443/swagger/index.html |
| Keycloak | https://localhost:8444 |
| Structurizr (optioneel starten, hieronder) | http://localhost:8080 |

De API en PostgreSQL hebben geen gepubliceerde hostpoort. Keycloak heeft alleen voor deze lokale demonstratie een loopbackpoort. Kong draait DB-less, zonder toegankelijke Admin API. Swagger UI dient als lokale API-verkenner.

**De certificaten worden niet automatisch door je browser vertrouwd.** Gebruik voor scripts de meegegeven CA, zoals `curl --cacert`; certificaatvalidatie uitschakelen is niet nodig. Er wordt niets in de truststore van je computer geïnstalleerd.

## Opdrachten 1, 2 en 3

Elke opdracht heeft een eigen consoleproject, namespace en testproject, zonder projectverwijzingen naar andere opdrachten.

```sh
./scripts/exercise.sh 1 1 15
./scripts/exercise.sh 2 '1,2,3,4,5,6,7,8,9,10' 3
./scripts/exercise.sh 3 2026-10-01 2026-10-31
```

Met een lokale .NET 10 SDK kan ook `dotnet run --project opdrachten/01-fizzbuzz/src -- 1 15` worden gebruikt, analoog voor opdracht 2 en 3.

- FizzBuzz gebruikt inclusieve grenzen en standaard 1 t/m 100. Nul is een veelvoud van zowel 3 als 5. Negatieve bereiken zijn toegestaan.
- Verdelen behoudt waarden, volgorde en invoer; de laatste groep mag kleiner zijn. Lege invoer geeft `[]`. De groepsgrootte is een positief geheel getal; een decimale grootte is ongeldige console-invoer.
- Volledige weken gebruikt inclusieve datumgrenzen en een week van maandag t/m zondag. Alleen de maandag van volledig ingesloten weken wordt teruggegeven. Datums hebben formaat `yyyy-MM-dd`.
- Ongeldige invoer geeft een foutmelding op stderr en exitcode 1.

### Voorbeeldinvoer en uitkomsten

De consoleopdrachten lezen hun invoer uit argumenten na het commando. Deze voorbeelden tonen precies wat je meegeeft en wat het programma teruggeeft:

| Opdracht | Invoer | Uitkomst |
|---|---|---|
| 1 — FizzBuzz | `./scripts/exercise.sh 1 1 15` (start 1, einde 15) | Eén regel per getal: `1`, `2`, `Fizz`, `4`, `Buzz`, `Fizz`, `7`, `8`, `Fizz`, `Buzz`, `11`, `Fizz`, `13`, `14`, `FizzBuzz`. |
| 2 — Verzamelingen | `./scripts/exercise.sh 2 '1,2,3,4,5,6,7,8,9,10' 3` (getallen, groepsgrootte 3) | `[[1,2,3],[4,5,6],[7,8,9],[10]]` als JSON; de laatste groep mag kleiner zijn. |
| 3 — Volledige weken | `./scripts/exercise.sh 3 2026-10-01 2026-10-31` (begindatum, einddatum) | `2026-10-05`, `2026-10-12` en `2026-10-19`, elk op een eigen regel. Alleen weken waarvan maandag én zondag binnen het bereik vallen tellen mee. |

Voor de Wmo-API voer je HTTP-verzoeken uit met een Keycloak-access-token. Swagger toont dezelfde invoervelden en responses op https://localhost:8443/swagger/index.html. Dit zijn de hoofdverzoeken:

| Verzoek | Invoer | Uitkomst |
|---|---|---|
| `GET /api/v1/residents?page=1&pageSize=50` (rol `callcenter`) | Geen requestbody; optionele paginanummering. | Pagina met fictieve bewoners (`items`, `pageNumber`, `pageSize`, `totalCount`), met huidig kilometerbudget als dat bestaat. |
| `POST /api/v1/residents/{residentId}/trips` (rol `callcenter`) | `departureAt` met tijdzone-offset, `destinationAddress` en positieve `distanceKilometers` met maximaal 3 decimalen. | `201 Created` met rit-ID, bewoner, toegewezen vervoerder, adressen, vertrektijd en afstand. Het kilometerbudget wordt in dezelfde transactie verlaagd. |
| `GET /api/v1/trips?page=1&pageSize=50` (rol `carrier`) | Geen requestbody; vervoerders-ID komt uit het ondertekende token. | Alleen de ritten van die vervoerder, als gepagineerde lijst. |
| `POST /api/v1/budget-renewals` (rol `renewal`) | Geen requestbody; de datum komt van de systeemklok. | `{"renewed": aantal}` met het aantal verlengde budgetperioden. |

Voorbeeldinvoer voor een ritboeking (bewoner-ID 2 is fictieve demo-invoer):

```json
{
  "departureAt": "2026-10-17T10:00:00Z",
  "destinationAddress": "Fictief reisplein 10, Utrecht",
  "distanceKilometers": 12.125
}
```

Ongeldige invoer of een regelconflict geeft een HTTP-fout met Problem Details, een stabiele `code` en `correlationId`. Een niet-bestaande bewoner geeft bijvoorbeeld 404, onvoldoende budget 409 en een ontbrekend token 401. De details en een volledig curl-voorbeeld staan verderop bij [API-contract en autorisatie](#api-contract-en-autorisatie) en [Voorbeeldverzoek](#voorbeeldverzoek).

## Architectuur en SOLID

```text
opdrachten/
  01-fizzbuzz/          src + tests
  02-verzamelingen/     src + tests
  03-volledige-weken/   src + tests
  04-wmo/
    src/
      Wmo.Domain/          entiteiten en zuivere bedrijfsregels
      Wmo.Application/     use-cases en gerichte opslagcontracten
      Wmo.Infrastructure/  EF Core, PostgreSQL, transacties en migraties
      Wmo.Api/             HTTP, JWT-autorisatie en periodieke taak
    tests/                 Unit, Integration, Playwright
infra/                     Compose-onderdelen, CA, Kong en Keycloak-bootstrap
architecture/              Structurizr DSL en toelichting
```

De afhankelijkheden lopen naar binnen: Domain kent geen andere Wmo-projecten; Application kent uitsluitend Domain; Infrastructure implementeert de contracten uit Application. De API koppelt deze onderdelen via dependency injection. Een architectuurtest bewaakt dat Domain en Application geen HTTP-/EF-/infrastructuurafhankelijkheden krijgen.

SOLID is toegepast door kleine verantwoordelijkheden en interfaces: boeken, verlengen, query's, audit en transacties hebben ieder een eigen contract. De klok is injecteerbaar via `TimeProvider`. Er is geen generieke repositorylaag, inheritance-framework of microserviceverdeling toegevoegd. Vervangbaarheid betekent hier dat een opslagadapter de transactie- en vergrendelingsgaranties van het contract moet behouden.

### Structurizr / C4

```sh
docker compose --profile architecture up -d architecture
```

Open http://localhost:8080. Het model bevat een **systeemcontext**, **containerdiagram** en **componentdiagram van de Wmo-API**. Bron en validatiecommando staan in [architecture/README.md](architecture/README.md). De viewer gebruikt lokaal HTTP en verwerkt alleen architectuurmetadata, geen bewonersgegevens of tokens.

## API-contract en autorisatie

Keycloak geeft tokens uit via OAuth 2.0 client credentials. Dit demonstreert systeemclients, geen persoonlijk callcentermedewerker-login. De API valideert issuer, audience `wmo-api`, RS256-handtekening, geldigheid en rollen. Bedrijfsrechten worden ontleend aan de ondertekende `taxi_company_id`-claim. Aangeleverde identiteitheaders gelden niet als autorisatiebewijs.

| Route | Rol | Betekenis |
|---|---|---|
| `GET /api/v1/residents?page=1&pageSize=50` | `callcenter` | Utrechtse bewoners, ook zonder beschikking |
| `POST /api/v1/residents/{residentId}/trips` | `callcenter` | Rit boeken en kilometers afboeken |
| `GET /api/v1/trips?page=1&pageSize=50` | `carrier` + geldig bedrijfs-ID | Alleen eigen ritten |
| `POST /api/v1/budget-renewals` | `renewal` | Verwerk verschuldigde verlengingen |
| `GET /openapi/v1.json` | Geen | Publiek machineleesbaar API-contract; bevat geen persoonsgegevens |
| `GET /health/live`, `GET /health/ready` | Geen | Niet-gevoelige proces-/databasegereedheid |

Paginering is stabiel gesorteerd, standaard 50 en maximaal 100 items. Responses bevatten `items`, `pageNumber`, `pageSize` en `totalCount`. Bewonersinzage bevat naam, woonadres en indien aanwezig de huidige budgetperiode, resterende kilometers en stopdatum. Vervoerders krijgen alleen de ritgegevens die zij nodig hebben, geen volledig bewonersregister of medische gegevens.

Clients: `callcenter`, `carrier-a`, `carrier-b`, `renewal`. De bootstrap maakt daarnaast drie negatieve testclients. Secrets worden lokaal gegenereerd en staan onder de genegeerde map `.local/`. Beheerlogin voor Keycloak is `local-admin`; het lokale wachtwoord staat in `.local/secrets/keycloak-admin-password`. Publiceer deze bestanden niet.

### Swagger UI

Open https://localhost:8443/swagger/index.html. De lokale CA moet door je browser worden vertrouwd; de applicatie installeert geen certificaten op je computer. Swagger gebruikt de standaard-UI en laadt het publieke machineleesbare contract van `/openapi/v1.json`. Dat contract beschrijft alleen routes, schemas en autorisatie; het bevat geen bewonersgegevens.

Gebruik **Authorize** om een access-token in te voeren. Met **Try it out** kun je daarna echte handelingen op fictieve demogegevens uitvoeren. API-aanroepen blijven beveiligd en vereisen de bijbehorende rol; er staan geen client-secrets in de UI. Er is geen externe validator of CDN nodig. Requests gebruiken een relatieve server-URL en blijven op dezelfde Kong-origin.

Swagger UI staat standaard uit (`Swagger__Enabled=false`) en wordt alleen door de lokale Compose-configuratie ingeschakeld. Het OpenAPI-contract is publiek; daadwerkelijke API-routes blijven geautoriseerd. Het startscript herstart Kong om ook bij een bestaande stack de declaratieve routeconfiguratie opnieuw te laden.

### Voorbeeldverzoek

Vanuit de projectmap, met `curl` en Python 3 voor het uitlezen van JSON:

```sh
TOKEN=$(curl --silent --show-error --fail --cacert .local/certs/ca.crt \
  https://localhost:8444/realms/paqt/protocol/openid-connect/token \
  --data-urlencode grant_type=client_credentials \
  --data-urlencode client_id=callcenter \
  --data-urlencode client_secret@.local/client-secrets/callcenter-secret \
  | python3 -c 'import sys,json; print(json.load(sys.stdin)["access_token"])')

curl --cacert .local/certs/ca.crt -H "Authorization: Bearer $TOKEN" \
  https://localhost:8443/api/v1/residents
```

Boeken gebruikt onderstaande JSON; vervang het tijdstip door een toekomstig tijdstip binnen de actuele budgetperiode. Tijdstippen vereisen `Z` of een expliciete offset.

```json
{
  "departureAt": "2026-10-17T10:00:00Z",
  "destinationAddress": "Fictief reisplein 10, Utrecht",
  "distanceKilometers": 12.125
}
```

Stuur dit met `POST`, `Content-Type: application/json` en het token naar `/api/v1/residents/30000000-0000-0000-0000-000000000002/trips`. Een geslaagde boeking geeft `201` met de rit. Geef geen vervoerder of ophaaladres mee: de API bepaalt die. Onbekende JSON-velden worden geweigerd.

Bewoners-ID's eindigen op `000000000001` t/m `000000000007`: 1 Noord, 2 Zuid, 3 klein budget, 4 stopgezet, 5 achterstallige verlenging, 6 zonder beschikking, 7 buiten Utrecht. Alle gegevens zijn fictief.

### Fouten en gateway

API-fouten gebruiken Problem Details met stabiele `code` (snake_case, afgeleid van de domeinfout-enum `DomainError`) en `correlationId`, ook in header `X-Correlation-ID`. Invoerfouten geven 400, ontbrekende/ongeldige tokens 401, onvoldoende rechten 403, onbekende bewoners 404 en conflicterende bedrijfsregels 409. Tijdelijke opslaguitval geeft 503. Stacktraces en opslagdetails worden niet aan clients teruggegeven.

Kong beperkt API-verkeer tot 600 verzoeken per minuut per bron-IP en maximaal 64 KiB requestbody. Het limiet is lokaal per gateway-instance en bedoeld voor deze demonstratie. Timeouts zijn begrensd; automatisch opnieuw uitvoeren van boekingen staat uit. Kong valideert het upstream-certificaat. Gatewayfouten (zoals 413 en 429) volgen het standaard Kong-foutformaat; ze zijn onderscheiden van het Problem Details-contract van de applicatie.

## Wmo-regels en expliciete aannames

- Een beschikking is één jaar geldig en wordt automatisch verlengd, tenzij stopgezet. Een bewoner heeft in deze casus maximaal één beschikking.
- De verjaardag van de oorspronkelijke ingangsdatum bepaalt budgetperioden: begindatum inclusief, einddatum exclusief. Verlenging zet het saldo terug naar het oorspronkelijke jaarbudget; ongebruikt saldo vervalt.
- Bij 29 februari geldt 28 februari in niet-schrikkeljaren; in een volgend schrikkeljaar keert 29 februari terug.
- Een stopdatum is inclusief: vanaf die datum is de beschikking inactief. Een toekomstige stopdatum begrenst ook boekbare ritten.
- Ritdatum én boekingsdatum moeten onder een actieve beschikking vallen. Alleen toekomstige ritten binnen de actuele budgetperiode zijn toegestaan.
- De afstand wordt door het callcenter aangeleverd, is positief en heeft maximaal drie decimalen. Er is geen externe routeberekening of subsidie-/indicatiebeoordeling.
- Bij boeken wordt het saldo direct en transactioneel verlaagd. Onvoldoende saldo blokkeert de hele transactie. Er is geen annulerings- of restitutieproces toegevoegd.
- Het woonadres verwijst in de fictieve stamgegevens naar één perceel, dat precies één vervoerder heeft. Ritgegevens bewaren het gekozen ophaaladres en bedrijf als momentopname.
- De periodieke taak start bij applicatieopstart en vervolgens elk uur. Gemiste budgetperioden worden ingehaald tot de huidige periode; ook boeken controleert eerst of verlenging nodig is. Herhaling mag verbruik niet opnieuw aanvullen.
- Boekingen, verlengingen en auditregels worden atomair opgeslagen. `SELECT ... FOR UPDATE` serialiseert wijzigingen van dezelfde beschikking. Er zijn geen automatische retries van niet-idempotente boekingen; clients moeten een onduidelijke uitkomst onderzoeken voordat zij opnieuw boeken.
- Kalenderregels gebruiken `Europe/Amsterdam`; tijdstippen worden in UTC opgeslagen. De servertijdzone bepaalt de bedrijfsdatum niet.

## Precisie van budgetten en eventuele geldbedragen

De huidige opdracht bevat **kilometerbudgetten**, geen geldbedragen of tarieven. Hiervoor gebruikt C# `decimal` en PostgreSQL expliciet `numeric(12,3)`. Deze mapping staat ook in de EF Core-migratie; de applicatie weigert afstanden met niet-representeerbare extra decimalen voordat zij worden opgeslagen.

Bij een toekomstige uitbreiding met geld geldt als ontwerpafspraak:

- Gebruik C# `decimal` en een expliciete PostgreSQL `numeric(precision, scale)`-kolom; geen `float`, `double`, tekstkolom of PostgreSQL `money` als generiek bedragstype.
- Voor definitieve EUR-bedragen is het uitgangspunt `numeric(19,2)`, gecombineerd met een expliciete valutacode `EUR`. Dit is een projectkeuze voor EUR, geen universele schaal voor alle valuta of tarieven.
- Houd geld, kilometers en tarieven als verschillende domeintypen uit elkaar. Vermenigvuldig of tel bedragen van verschillende valuta niet zonder een expliciete conversieregel.
- Leg afrondingsmoment en afrondingsmethode vast vanuit de betreffende bedrijfsregel. Tussentijdse tarieven/berekeningen kunnen meer precisie nodig hebben dan het definitieve bedrag. Laat afronding niet ongemerkt aan de database over.
- Valideer bereik en toegestane decimalen vóór opslag en test de exacte database-round-trip, grenswaarden en afrondingsgevallen. Negatieve bedragen zijn alleen toegestaan wanneer de betreffende bedrijfsregel dat vereist, bijvoorbeeld bij een correctie.

Er zijn geen ongebruikte geldkolommen of betalingsfunctionaliteit aan deze kilometercasus toegevoegd.

## Testen

Voor een snelle, losse controle van één opdracht heb je alleen Docker nodig. Het script bouwt zo nodig de vastgepinde .NET-testimage en draait alleen de gekozen tests:

```sh
./scripts/test-opdracht.sh 1          # FizzBuzz
./scripts/test-opdracht.sh 2          # Verzamelingen verdelen
./scripts/test-opdracht.sh 3          # Volledige weken
./scripts/test-opdracht.sh wmo-unit   # Wmo-unit- en architectuurtests
```

Met de gepinde .NET 10 SDK kun je opdracht 1 t/m 3 ook rechtstreeks testen, bijvoorbeeld `dotnet test opdrachten/01-fizzbuzz/tests`. Vervang het pad door `opdrachten/02-verzamelingen/tests` of `opdrachten/03-volledige-weken/tests` voor de andere opdrachten.

Voor alle controles, inclusief PostgreSQL-integratie en API-tests via Keycloak en Kong, gebruik je:

```sh
./scripts/test.sh
```

Dit draait unittests en bouwt vervolgens een **afzonderlijke wegwerpstack `paqt-tests`** zonder hostpoorten. Integratietests gebruiken uitsluitend `wmo_test`; Playwright gebruikt de fictieve Wmo-database in die teststack. Bij afloop wordt alleen het testproject inclusief zijn datavolume verwijderd. De demo onder projectnaam `paqt` blijft behouden.

- Unittests: algoritmen, budgetten, schrikkeldagen, stopzetting, grensdatums en architectuurafhankelijkheden.
- PostgreSQL-integratietests: migraties, persistentie, vervoerdersselectie, transactie-rollback, row-lock-concurrentie, dubbele reset en beperkte audit-/DDL-rechten. De rechten van het runtime-account (`wmo`) worden door de EF Core-migratie `RuntimePermissions` ingesteld; de migratie draait onder `wmo_owner` via `--migrate --seed-demo` in de `migrate`-service.
- Playwright API-tests: echte Keycloak-tokens, alle API-handelingen via Kong, paginering, afschrijving, bedrijfsafscherming, negatieve token-/roltests, veilige foutresponses en gatewaylimieten. Dit zijn HTTP-tests zonder browser of screenshots.

Rapporten staan in `artifacts/unit`, `artifacts/integration` en `artifacts/playwright`. Traces staan uit zodat bearer tokens niet in tracebestanden terechtkomen. Testresultaten en beperkingen staan in [docs/verificatie.md](docs/verificatie.md).

### Continuous integration

De GitHub Actions-workflow [`.github/workflows/ci.yml`](.github/workflows/ci.yml) draait bij elke push naar `main` en bij pull requests:

- **Container bouwen en scannen:** `docker compose build api`, daarna Trivy op de API-image en op de repository (afhankelijkheden en secrets). De job faalt bij HIGH- of CRITICAL-bevindingen waarvoor een fix beschikbaar is.
- **Tests:** `./scripts/test.sh` (unit-, integratie- en Playwright-tests tegen de wegwerpstack met Keycloak en Kong). De rapporten worden zeven dagen als artefact bewaard.

De workflow is nog niet op GitHub uitgevoerd; lokaal zijn de image-build en de Trivy-scans gecontroleerd. De misconfig-scan van Trivy is niet opgenomen: die meldt dat de Playwright-testimage als root draait.

## Beheer en configuratie

```sh
docker compose logs --tail 100 api keycloak kong
docker compose down                   # Stoppen; demo-opslag blijft behouden
./scripts/start.sh                    # Herstarten met bestaande data
```

Pas voor een update de gepinde versies aan, voer tests/scans uit en bouw opnieuw. Databasewijzigingen gebruiken versiebeheerbare EF Core-migraties. Rol applicaties alleen terug als het databaseschema compatibel is; anders eerst gecontroleerd vooruit herstellen. Exporteer een back-up vóór schemawijzigingen en test herstel in een afzonderlijke database. Voor deze lokale demonstratie is er geen automatische back-updienst of HA-opstelling.

| Configuratie | Default / betekenis | Wijziging |
|---|---|---|
| `Database__Host/Name/User` | `postgres/wmo/wmo`; niet-geheime verbindingsinstellingen | Herstart |
| `Database__PasswordFile` | Pad naar lokaal geïnjecteerd secret; inhoud gevoelig | Account en injectie gezamenlijk wijzigen |
| `Database__RootCertificate` | `/certs/ca.crt`; vertrouwde CA | Vertrouwensketen gezamenlijk wijzigen |
| `Authentication__Issuer` | `https://localhost:8444/realms/paqt` | Keycloak en API consistent aanpassen |
| `Authentication__MetadataAddress` | Interne HTTPS-discovery van Keycloak | Alleen vertrouwde interne locatie |
| `Swagger__Enabled` | `false` in de applicatie, `true` in lokale Compose; alleen UI/startpagina en assets | Herstart |
| `Renewal__IntervalSeconds` | `3600`; geldig 1 t/m 86400 | Herstart |
| Kong declaratieve configuratie | Bootstrapbron `infra/bootstrap.py`; geen secrets | Init opnieuw bouwen/draaien en Kong herstarten |

Netwerk- en authenticatiefouten zijn fail-closed. Liveness zegt alleen dat het proces draait; readiness controleert opslagbereikbaarheid en is geen bewijs dat alle gebruikershandelingen werken. Gebruik bij functionele problemen ook de smoke-/Playwright-tests. De vernieuwingstaak meldt aantallen en fouten zonder bewonersgegevens en probeert bij de volgende uitvoering opnieuw.

Lokale servercertificaten zijn 90 dagen geldig; de CA 365 dagen. Verlopen servercertificaten kunnen na stoppen van de stack worden vervangen door uitsluitend de betreffende bestanden onder `.local/certs/<service>/` te verwijderen en bootstrap opnieuw uit te voeren. Bewaar de CA en secrets; een volledige CA-rotatie vraagt heruitgifte van alle servercertificaten en herstart van alle betrokken services. Secrets worden niet automatisch geroteerd. Keycloak importeert een bestaande realm niet opnieuw: wijzig client-secrets bij rotatie óók in Keycloak.

## CIZ-ontwerpreferentie en begrenzing

Als afgesproken referentie is de lokale architectuurkennisbank gebruikt: commit `730453aca2200b9be6100f7d1481cf231cc9b34b`, 9 september 2026. STD-0001 t/m STD-0010 hebben daar versie 0.1 en status **Concept**; ook de NFR-catalogus is Concept. De geraadpleegde principes zijn een lokale gesynchroniseerde kopie uit de naastliggende architectuurrepository. Dit zijn ontwerpreferenties, geen door PAQT opgelegde criteria.

[docs/ciz-toetsing.md](docs/ciz-toetsing.md) beschrijft toepassing, acceptatiecriteria en bewijs. De imagescan bevat open upstreambevindingen in Keycloak (volgens Docker Scout; Trivy meldt er geen). De PostgreSQL-image is een eigen build zonder de kwetsbare `gosu`-binary; zie [docs/verificatie.md](docs/verificatie.md). Er wordt **geen formele CIZ-, BIO-, ISO- of NEN-conformiteit** geclaimd. Centrale HSM-/secretvoorzieningen, organisatiebrede logging, formele retentie, productie-ingress, HA, SLO/RTO/RPO en versleuteling van de Docker-hostopslag zijn niet geleverd of bewezen. De lokale secrets en CA zijn uitsluitend voor fictieve gegevens en vervangen geen centrale CIZ-voorziening.
