# Database - InkassoAkten

## Money Policy

All monetary values use `decimal(18,2)`. Never use `float` or `double` for money.

## Concurrency Policy

Optimistic concurrency via SQL Server `rowversion` on mutable aggregate roots (`Mandant`, `Schuldner`, `Forderung`, `InkassoAkte`).

## Schema (Phase 1)

Tables: `Mandanten`, `Schuldner`, `Forderungen`, `InkassoAkten`, `AuditEintraege`.

## Index Strategy

- `InkassoAkte`: unique `Aktenzeichen`, `AkteStatus`, `MandantId`, `SchuldnerId`.
- `Schuldner`: composite `(NachnameOderFirma, PLZ)`.
- `Forderung`: FKs to `Mandant` and `Schuldner`.
