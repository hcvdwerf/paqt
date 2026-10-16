# C4-architectuur

De bron is `workspace.dsl`. De code en Compose-configuratie zijn leidend voor het daadwerkelijke gedrag; pas dit model bij structurele wijzigingen mee aan.

```sh
docker compose --profile architecture up -d architecture
```

Open http://localhost:8080. De poort is uitsluitend op loopback gepubliceerd.

De views zijn `context` (C4 niveau 1), `containers` (niveau 2) en `wmo-components` (niveau 3). De eerste drie opdrachten zijn onafhankelijke consoleprojecten. Het componentdiagram beschrijft alleen opdracht 4. Interface-aanroepen lopen tijdens runtime naar de opslagadapter; compile-time verwijst de adapter naar de applicatiecontracten, niet andersom.

Valideer de DSL zonder de viewer te starten:

```sh
docker run --rm -v "$PWD/architecture:/usr/local/structurizr" structurizr/structurizr:2026.05.16 validate -workspace workspace.dsl
```

De PostgreSQL-container heeft afzonderlijke databases en accounts voor Wmo en Keycloak. De testdatabase wordt uitsluitend door integratietests gebruikt. Unit- en Playwright-runners zijn tijdelijke testgereedschappen en staan niet in het runtime-diagram. De lokale CA, migratiestap en demo-seeding zijn opstartvoorzieningen, geen permanente services.
