# Agentinstructies - PAQT Back-End 2026

## Scope

Implementeer en onderhoud de vier opdrachten uit de door de gebruiker aangeleverde PDF. De gebruiker koos .NET 10 LTS, gescheiden opdrachten, Docker Compose, PostgreSQL, Keycloak, Kong, unit-/integratietests, Playwright API-tests, een Nederlandse README en Structurizr C4-diagrammen. Gebruik uitsluitend fictieve gegevens. Geen externe bedrijfsintegraties, publicatie of versturen van bestanden.

## Werkwijze

- Lees README.md en relevante code voordat je wijzigt. Gebruik de gepinde SDK, packages en lockfiles.
- Houd opdracht 1, 2 en 3 onafhankelijk uitvoerbaar. Voeg geen gedeelde bibliotheek of infrastructuurafhankelijkheid tussen opdrachten toe.
- Wmo.Domain kent geen andere projectlagen; Wmo.Application kent uitsluitend Domain. Concrete adapters implementeren gerichte applicatiecontracten.
- Toelichting en README zijn Nederlands; C#-identifiers zijn Engels.
- Pas uitsluitend concrete, uitlegbare abstracties toe. Geen microservices, broker of Kubernetes toevoegen.
- Wijzig datum-, budget-, autorisatie- of transactiegedrag alleen met passende tests en documentatie.
- Houd kilometers en geld gescheiden. Gebruik voor exacte hoeveelheden `decimal` en expliciete PostgreSQL `numeric`-precisie/schaal. Geld vereist ook een expliciete valuta en overeengekomen afrondingsregel; voeg pas geldfunctionaliteit toe wanneer de functionele scope dit vraagt. Zie README.md.

## Controleren

- `./scripts/start.sh` start de lokale demo met TLS.
- `./scripts/test.sh` draait alle tests in een geïsoleerd project `paqt-tests`; de disposable testvolumes worden verwijderd, demovolumes niet.
- C4-valideren: zie architecture/README.md.
- Documenteer werkelijke resultaten en beperkingen in docs/verificatie.md. Claim geen geslaagde test zonder uitvoering.

## Kaders

Gebruik docs/ciz-toetsing.md voor de afgesproken bronversie en toepassingsscope. De CIZ-standaarden en NFR-catalogus in die versie zijn Concept. Claim geen formele CIZ-, BIO-, ISO- of NEN-conformiteit. Verzin geen verplichte coverage, SLO of bewaartermijn.

Bewonersgegevens en tokens horen niet in technische logs of testrapporten. Houd secrets, private sleutels, lokale data en buildmappen buiten broncode/images. Autoriseer ieder API-gebruik en beperk ritinzage tot het bedrijf uit de gevalideerde identiteit. Een private netwerkverbinding vervangt geen autorisatie of TLS.
