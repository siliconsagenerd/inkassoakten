# Decisions - InkassoAkten

## ADR-001: EF Core Migrations as Source of Truth (Phase 1)

EF Core migrations define the schema. No hand-written SQL scripts.

## ADR-002: Monetary Values Use decimal(18,2)

Financial precision requires decimal. Float/double are prohibited for money.

## ADR-003: Optimistic Concurrency via RowVersion

SQL Server `rowversion` columns on mutable aggregates. Concurrent updates raise `DbUpdateConcurrencyException`.

## ADR-004: DevExpress Deferred

UI component library deferred until domain engine (§ 367 BGB, § 288 BGB) is implemented and tested.
