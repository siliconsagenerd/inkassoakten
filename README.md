# InkassoAkten

> Deterministic receivables and case management for German debt collection.

## What it demonstrates

C#/.NET 8, MS SQL Server 2022, EF Core, Clean Architecture, optimistic concurrency, and deterministic seed data.

## Tech Stack

- .NET 8 / ASP.NET Core Blazor Server
- EF Core + Microsoft.EntityFrameworkCore.SqlServer
- MS SQL Server 2022 (Docker)
- xUnit + Testcontainers.MsSql

## Architecture

See [docs/architecture.md](docs/architecture.md).

## Local Development

```bash
dotnet restore InkassoAkten.sln
dotnet build InkassoAkten.sln
dotnet test InkassoAkten.sln
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

## Tests

```bash
dotnet test InkassoAkten.sln
```

## Roadmap

- Phase 2: ✅ § 367 BGB allocation, § 288 BGB interest, state machine, immutable ledger, concurrency
- Phase 3: Blazor UI, aging report, DevExpress evaluation, PDF statement
- Future: bank-payment import and matching engine

## License

MIT
