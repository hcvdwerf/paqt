# Toepassingsprofiel en CIZ-toetsing

## Bronnen en status

De ontwerpbasis is op verzoek van de opdrachtgever de lokale CIZ-architectuurkennisbank, commit `730453aca2200b9be6100f7d1481cf231cc9b34b` (9 september 2026), remote `CIZArchitectuur/architectuurkaders-en-besluiten`. STD-0001 t/m STD-0010: versie 0.1, Concept (25 augustus 2026). NFR-catalogus: Concept, redactioneel bijgewerkt 7 september 2026. Principes: lokale gesynchroniseerde kopie `futurestate/docs/principes/principes-ciz/principes_ciz.md`; upstreamactualiteit en formele beleidsstatus zijn niet afzonderlijk aangetoond.

Toepassingsscope: lokale PAQT-demonstratie met uitsluitend fictieve gegevens. Dit document legt projectkeuzes en technisch bewijs vast; het is geen formeel toepasselijkheidsbesluit of risicoacceptatie namens CIZ. Originele organisatiebeleidsdocumenten worden niet meegeleverd. Ontbrekende afspraken leveren geen impliciete goedkeuring op.

## Geselecteerde toetsing

| Bron / toets-ID | Projectafspraak en acceptatiecriterium | Bewijs en begrenzing |
|---|---|---|
| STD-0001 | `DateOnly` voor kalenderdatums; UTC voor tijdstippen; Amsterdam expliciet voor kalenderregels; ISO 8601 en UTF-8 | Unit-tests rond schrikkeldagen en lokale jaargrens; API-test weigert tijdstippen zonder offset. |
| STD-0002; NFR-UX-02/03 | API-major in pad; OpenAPI-contract; stabiele applicatiefoutcodes en correlation-ID zonder gevoelige details | Playwright-test van 400/401/403/404/409; gatewayfouten apart gedocumenteerd in standaard Kong-formaat. |
| STD-0003 | Certificaatvalidatie op API-, identity- en databaseverkeer; lokale CA; geen private sleutels in bron/images | Werkende keten via Kong/Keycloak/PostgreSQL; runtime-app gebruikt `VerifyFull`; Playwright schakelt TLS-validatie niet uit. Geen centraal/HSM-sleutelbeheer of bewezen host-/back-upencryptie. Interne health-/statusinterfaces zonder persoonsgegevens gebruiken HTTP. |
| STD-0004; NFR-SEC-01/07 | Geen adressen, gezondheidsgegevens, tokens of secrets in applicatielogs; kritieke mutaties hebben transactionele auditregels | Integratietests tonen audit bij boeking/verlenging en rollback bij fout; runtime-account kan auditregels niet wijzigen/verwijderen. Geen centrale doorzoekbaarheid, WORM-opslag, beheerreview of formele retentie bewezen. |
| STD-0005; NFR-DOC-05/08 | Versiegebonden packages/lockfiles en images; scheiding configuratie/secrets; declaratieve migratie/start | `global.json`, NuGet/npm-lockfiles, image-digests, Compose en migraties; start-/test-/herstartcontrole. Lokale bestandsinjectie vervangt geen centrale secretvoorziening of releasegoedkeuring. |
| STD-0006 | Containergebruik volgt de expliciete opdracht; processen/gegevens gescheiden; API non-root; graceful stop | Eén Compose-stack zonder cloud/Kubernetes; volumes, healthchecks, beperkte runtime-account; diagram toont verantwoordelijkheden. Resourceprofielen, capaciteit en productiegeschiktheid niet vastgesteld. |
| STD-0007; NFR-DOC-10 | Aparte liveness/readiness en privacyveilige, gestructureerde logs | Health- en API-tests; README beschrijft diagnose. Geen volledige OpenTelemetry-/metrics-/dashboard-/alertingvoorziening; geen SLO afgesproken. |
| STD-0008; NFR-DOC-06 | Vier onafhankelijke opdrachten; domein en applicatie zonder HTTP/EF; vervangbare adapter; expliciet contract | Architectuurtest, Structurizr C4 en OpenAPI. Databasebackup/-restore geeft technische portabiliteit; een afzonderlijk duurzaam semantisch exportcontract is niet geleverd. |
| STD-0009; NFR-TEST-01/02/03 | Geen automatische retries van boekingen; transacties en locks; idempotente reset; periodieke inhaalverwerking | PostgreSQL-integratietests voor overspending, reset/boek-races en rollback; Playwright-resettest. Geen HA, failover, loadprofiel of overeengekomen RTO/RPO. |
| STD-0010 | Wmo-API en database intern; Kong als ingang; alleen loopbackpoorten; geen gatewaybeheerpoort | Compose-netwerken en hostbereikbaarheid gecontroleerd. Kong en Keycloak hebben een apart ingressnetwerk. Geen centrale ingress/egress, netwerkpolicymanager of formele risicoacceptatie geleverd. |
| NFR-CODE-01/04/05; NFR-TEST-07 | C#-conventies, nullable, warnings-as-errors, beperkte dependencies, herleidbare testrapporten | Buildresultaat en TRX/JUnit/HTML onder `artifacts/`; geen willekeurig coveragepercentage als norm. |
| NFR-CODE-08; NFR-SEC-05/06 | Dependencies en bron op bekende kwetsbaarheden/secrets onderzoeken | Zie `verificatie.md` voor daadwerkelijke scanresultaten; een scan is geen bewijs van afwezigheid van kwetsbaarheden. |
| NFR-SEC-02/09 | Uitsluitend fictieve gegevens; minimaal antwoord per rol; bedrijfsafscherming vanuit ondertekende identiteit | Playwright test rolmatrix, ontbrekend bedrijfs-ID, verkeerd audience en inzage van een andere vervoerder. |

## Niet aantoonbaar binnen deze opdracht

Formele CIZ-beleidsactualiteit, classificatie, DPIA-beoordeling, bewaartermijnen, licentie-/leveranciersacceptatie, individuele menselijke herleidbaarheid, scheiding van beoordelaar/ontwikkelaar en productiebeheerafspraken zijn niet vastgesteld. De technische clients identificeren systemen, geen medewerkers.

De ontwikkelaar kan technische resultaten opleveren, maar is niet bevoegd om namens CIZ risico's te accepteren of conceptstandaarden vast te stellen. Een eventuele productie-inzet vraagt een afzonderlijk toepassingsprofiel en beoordeling door bevoegde eigenaren. De historische Apoyo-baseline is niet van toepassing.
