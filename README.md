# InkassoAkten

Deterministic receivables and case management for German debt collection (technical demo).

## What it demonstrates

- .NET 8 / ASP.NET Core Blazor Server
- EF Core + SQL Server 2022
- Clean Architecture
- § 367 BGB-style allocation (costs, then interest, then principal)
- § 288 BGB interest assumptions with seeded basis rates
- State machine, immutable ledger, audit trail
- Optimistic concurrency and concurrent payment handling
- Aging report and PDF statement generation

## Tech Stack

- .NET 8 / ASP.NET Core Blazor Server
- EF Core + Microsoft.EntityFrameworkCore.SqlServer
- MS SQL Server 2022 (Docker Compose)
- xUnit + FluentAssertions + Testcontainers.MsSql
- QuestPDF (Community license) for PDF rendering in Infrastructure

## Architecture

See [docs/architecture.md](docs/architecture.md).

## Local Development

```bash
dotnet restore InkassoAkten.sln
dotnet build InkassoAkten.sln
```

## Docker

```bash
cp .env.example .env
docker compose up -d
```

## Database Migrations

```bash
dotnet ef database update --project src/InkassoAkten.Infrastructure --startup-project src/InkassoAkten.Web
```

The migration command uses the default connection string `Server=localhost,1433;Database=InkassoDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;`. If `.env` changes `MSSQL_SA_PASSWORD`, `MSSQL_DB_NAME`, or `MSSQL_PORT`, set `ConnectionStrings__DefaultConnection` to the matching SQL Server connection string before running migrations or the app.

## Seed Demo Data

The web app does not seed automatically. After migrations, seed the synthetic demo data with:

```bash
TEST_SQLSERVER_CONNECTIONSTRING="Server=localhost,1433;Database=InkassoDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;Encrypt=False;" \
dotnet test tests/InkassoAkten.IntegrationTests/InkassoAkten.IntegrationTests.csproj \
  --filter "FullyQualifiedName~Seed_Creates_Exactly_Five_InkassoAkten"
```

This command is only for an empty or disposable demo database. It applies migrations and inserts the five seeded cases when the case table is empty.

## Run the Web App

```bash
dotnet run --project src/InkassoAkten.Web
```

Open `http://localhost:5018`. The default profile uses that URL. To use another SQL Server, set `ConnectionStrings__DefaultConnection` before starting the app.

## Screenshots

![Case list](docs/screenshots/akten-list.png)

![Case detail](docs/screenshots/akten-detail.png)

![Aging report](docs/screenshots/berichte.png)

## Tests

```bash
dotnet test tests/InkassoAkten.Domain.Tests/InkassoAkten.Domain.Tests.csproj
dotnet test tests/InkassoAkten.IntegrationTests/InkassoAkten.IntegrationTests.csproj
```

Integration tests require a SQL Server instance. The fixture uses Testcontainers when `TEST_SQLSERVER_CONNECTIONSTRING` is not set. A pre-existing SQL Server can be used through that environment variable; use an isolated disposable test database only.

## Documentation

- [docs/architecture.md](docs/architecture.md)
- [docs/database.md](docs/database.md)
- [docs/decisions.md](docs/decisions.md)

## Legal / Demo Limitations

This repository is a technical demonstration. It is not legal advice, not a production debt-collection system, and must not be used to make enforceable claims without qualified legal review. Synthetic seed data only.

- DevExpress was evaluated for the UI layer but is not implemented; the UI uses standard ASP.NET Core Blazor components.
- Full UI repeated-click protection has not been verified beyond disabled submit buttons.
- QuestPDF is used under its Community license; commercial-use eligibility must be verified before commercial deployment.

## License

MIT
