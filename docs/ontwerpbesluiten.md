# Ontwerpbesluiten

Status: projectkeuzes voor de lokale PAQT-opdracht, 16 oktober 2026.

## Eén zelfstandig project per opdracht

De drie algoritmeopdrachten hebben ieder een eigen console- en testproject. Hun logica is zonder infrastructuur aanroepbaar. De Wmo-casus is een modulaire monoliet met Domain, Application, Infrastructure en Api. Dit houdt de opdracht uitlegbaar en voorkomt onnodige services. Afhankelijkheidsrichting wordt getest.

## PostgreSQL en expliciete transacties

Een boeking omvat een saldoaanpassing, rit en auditgebeurtenis. Alle drie moeten slagen of terugrollen. Boekingen en resets nemen dezelfde row lock op de beschikking. De jaarlijkse reset wordt door de opgeslagen budgetperiode idempotent; een tweede reset kan verbruik niet terugdraaien. PostgreSQL-integratietests bewijzen deze garantie met afzonderlijke verbindingen; een in-memory database zou dit onvoldoende toetsen.

## Budgetjaar op beschikkingsverjaardag

De opdrachtgever koos verjaardag in plaats van kalenderjaar en directe afschrijving in plaats van reserveringen. Boeken over een toekomstige budgetgrens is buiten scope. Verlenging wordt altijd vanaf de oorspronkelijke ingangsdatum berekend om verschuiving van 29 februari te voorkomen. Gemiste perioden worden in één stap ingehaald; oude ongebruikte budgetten stapelen niet.

## Kong OSS met autorisatie in de API

Kong verzorgt TLS, upstreamcertificaatcontrole, routing, begrensde requests en rate limiting. DB-less configuratie voorkomt een extra gatewaydatabase. De Enterprise OIDC-plugin is niet nodig: Keycloak geeft standaard tokens uit; ASP.NET valideert via OIDC-discovery/JWKS en controleert iedere operatie en de vervoerdersscope. De backend vertrouwt geen door callers ingevulde identityheaders.

## Lokale TLS en gescheiden accounts

Een bootstrapcontainer maakt een lokale CA en servercertificaten. Private CA-sleutels worden niet aan runtime-services gemount. Elke runtime-service krijgt uitsluitend zijn eigen sleutel en benodigde secrets. Wmo en Keycloak hebben een eigen databaseaccount; migraties gebruiken een afzonderlijke eigenaar. De CA wordt niet in het hostsysteem geïnstalleerd. Dit is geen vervanging voor CIZ-centraal sleutelbeheer of bewezen opslagversleuteling.

## Geïsoleerde acceptatietests

Playwright test HTTP-API's door de echte gateway en identityprovider heen. Er is geen gebruikersinterface nodig voor deze back-endopdracht. Een apart Compose-project met eigen opslag maakt de tests reproduceerbaar zonder demogegevens of andere projecten te wissen. Integratietests gebruiken bovendien een apart benoemde testdatabase.

## Exacte hoeveelheden en geld

Het huidige budget is uitgedrukt in kilometers: `decimal` in C# en `numeric(12,3)` in PostgreSQL. Geld is een andere eenheid en maakt nog geen deel uit van de opdracht. Een toekomstige EUR-uitbreiding gebruikt als uitgangspunt `decimal`, `numeric(19,2)` en een expliciete valutacode, met afzonderlijk vastgelegde afronding en validatie. Tarieven en tussenberekeningen mogen meer precisie vereisen. De applicatie mag voor geld geen binaire floating-pointgetallen gebruiken of onbedoelde databaseafronding accepteren. Zie de README voor de ontwerpafspraak; dit introduceert geen betaalproces.
