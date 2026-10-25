# Architecture - InkassoAkten

## Clean Architecture Boundaries

- **Domain**: Pure C# entities and business rules. No infrastructure dependencies.
- **Application**: Use cases, DTOs, persistence interfaces. No EF Core dependency.
- **Infrastructure**: EF Core DbContext, migrations, repositories. SQL Server provider only.
- **Web**: Blazor Server UI and HTTP health endpoint.

## Planned UML Diagrams

_(Placeholder for future class and sequence diagrams.)_
