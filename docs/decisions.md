# Decisions - InkassoAkten

## ADR-001: EF Core Migrations as Source of Truth

EF Core migrations define the schema. No hand-written SQL scripts are the source of truth.

## ADR-002: Monetary Values Use decimal(18,2)

Financial precision requires decimal. Float/double are prohibited for money.

## ADR-003: Optimistic Concurrency via RowVersion

SQL Server `rowversion` columns on mutable aggregates. Concurrent updates raise `DbUpdateConcurrencyException`.

## ADR-004: DevExpress Deferred

UI component library deferred until domain engine (§ 367 BGB, § 288 BGB) is implemented and tested. Phase 3 uses standard Blazor components and Bootstrap.

## ADR-005: QuestPDF in Infrastructure

Application exposes only interfaces and DTOs. QuestPDF rendering and its package dependency are isolated in Infrastructure. QuestPDF Community license is used for open-source/evaluation; commercial eligibility must be verified before production use.

## ADR-006: Testcontainers for SQL Server Integration Tests

Integration tests use Testcontainers.MsSql by default and support `TEST_SQLSERVER_CONNECTIONSTRING` for an existing isolated SQL Server. They are not replaced with in-memory providers.

## ADR-007: Demo and Legal Limitations

This project is a technical demonstration. It does not provide legal advice, production-ready debt collection workflows, or enforceable legal claims.
