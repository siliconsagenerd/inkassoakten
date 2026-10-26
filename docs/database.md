# Database - InkassoAkten

## Money Policy

All monetary values use `decimal(18,2)`. Never use `float` or `double` for money.

## Concurrency Policy

Optimistic concurrency via SQL Server `rowversion` on mutable aggregate roots (`Mandant`, `Schuldner`, `Forderung`, `InkassoAkte`, `Zahlung`, `Ratenzahlungsplan`).

## Schema

Core tables: `Mandanten`, `Schuldner`, `Forderungen`, `InkassoAkten`, `Zahlungen`, `Buchungen`, `Ratenzahlungsplaene`, `Raten`, `BasiszinsPerioden`, `AuditEintraege`.

Phase 3 adds:

- `IX_InkassoAkten_MandantId_AkteStatus`
- `IX_Forderungen_Faelligkeitsdatum`
- `vw_ForderungsAltersstruktur`

## Reporting View

`vw_ForderungsAltersstruktur` joins cases, claims, debtors, and clients and computes:

- `TageUeberfaellig`
- buckets: current, `1-30`, `31-60`, `61-90`, `90+`

## Index Strategy

- `InkassoAkte`: unique `Aktenzeichen`, `AkteStatus`, `MandantId`, `SchuldnerId`, composite `(MandantId, AkteStatus)`.
- `Schuldner`: composite `(NachnameOderFirma, PLZ)`.
- `Forderung`: `Faelligkeitsdatum`, FKs to `Mandant` and `Schuldner`.
- `Zahlung`, `Buchung`, `Ratenzahlungsplan`, `Rate`: foreign-key and lookup indexes.

## Interest Assumptions

- Basis rates are seeded in `BasiszinsPerioden`.
- § 288 BGB surcharge assumptions are encoded in domain logic and tests.
- Exact legal applicability depends on debtor type and contract terms; this is a technical demo, not legal advice.

## Rounding

Monetary allocation uses decimal arithmetic. Bucket and aging calculations use integer day differences. No floating-point rounding is used for money.

## Evidence Status

SQL query plans and measured database performance are not included. The index and view definitions are present in the Phase 3 migration; performance claims remain unverified.
