# InkassoAkten Deployment Plan (Phase 4A readiness)

Status: readiness and assessment only. No deployment, firewall, DNS, tunnel, or cloud-resource changes were made in this phase. No provisioning, no deployment, no money spent.

## Hosting decision (approved direction)

- Primary host: Google Cloud, using the user's existing trial credit.
- Do NOT use Cloudflare Workers to host the .NET Blazor application. Cloudflare Workers is an edge/serverless runtime without a .NET runtime, without support for long-lived Blazor Server SignalR/WebSocket circuits, and without private SQL Server connectivity. It cannot run ASP.NET Core Interactive Server.
- Cloudflare may be considered only for DNS/CDN in front of the Google Cloud VM, not for application hosting.
- AWS is a fallback only after verifying the account's current Free Plan/credit eligibility and SQL Server Express availability on that account.
- Do NOT use `oldmac` for the public demo. `oldmac` is a shared, saturated development host (see observed baseline) with no host firewall and SQL Server published on `0.0.0.0:1433`.

Rationale for Google Cloud primary:

- User already has an active Google Cloud account and trial credit.
- Compute Engine supports a standard Linux VM running Docker (Caddy + app + SQL Server Express container) with no special licensing.
- Region can be chosen close to the operator; private VPC networking keeps SQL Server off the public internet.
- Trial credit absorbs initial cost; cost is predictable and small (see estimate).

## Rejected option: Cloudflare Workers

Rejected because:

- No .NET runtime; cannot host ASP.NET Core / Blazor Server.
- No long-lived WebSocket/SignalR circuit support required by Blazor Interactive Server.
- No private connectivity to SQL Server.
- Application state (Blazor circuits) is incompatible with edge request/response model.
- Cloudflare remains viable only as DNS/CDN/proxy in front of the Google Cloud VM.

## Fallback: AWS

AWS is permitted only after verifying:

1. The account's current Free Plan / credit eligibility (Free Tier or credits remaining).
2. SQL Server Express availability and licensing on the chosen AWS service (EC2 Linux container or RDS).
3. Region and cost against the available credit.

Do not provision on AWS until those checks pass and are documented here.

## Google Cloud account verification (required before provisioning)

Before recommending a specific Google Cloud configuration, verify (interactively, by the user; do not paste credentials or account details into docs or chat):

1. Account state and billing: run `gcloud auth login` interactively, then `gcloud billing accounts list` and `gcloud billing projects describe <PROJECT_ID>`.
2. Available credit: Google Cloud Console -> Billing -> Credits (or `gcloud billing accounts describe`).
3. Region: choose a region with Compute Engine availability and acceptable latency; set `gcloud config set compute/region <REGION>` and `.../zone`.
4. Confirm the trial credit has not expired and covers the planned spend window (trial credits typically expire 90 days after activation).

Status: gcloud authentication is currently expired in this environment, so live account/credit/region verification could not be performed non-interactively. This is an approval gate (see below). No credentials or account details are recorded here.

## SQL Server licensing conclusion

- SQL Server 2022 Developer Edition: free for development and testing only. NOT licensed for a public demo or production use. Must not be used for the public deployment.
- SQL Server 2022 Express: free for production use, with limits (database size <= 10 GB, buffer pool <= 1 GB, max 4 cores, max 1 socket / 4 cores). Suitable for a synthetic non-production public demonstration. Source: Microsoft Learn, "SQL Server editions" and "SQL Server 2022 Express" (https://learn.microsoft.com/en-us/sql/sql-server/editions-and-components-of-sql-server-2022).
- SQL Server 2022 can run in a Linux Docker container. No Windows VM is required. Use image `mcr.microsoft.com/mssql/server:2022-latest` with `MSSQL_PID=Express`. Source: Microsoft Learn, "Deploy and connect to SQL Server Linux containers" (https://learn.microsoft.com/en-us/sql/linux/sql-server-linux-docker-container-deployment).
- Conclusion: use SQL Server 2022 Express in a Linux Docker container on a private Docker network. No Windows VM, no paid SQL license. If the demo later needs > 10 GB data or > 1 GB buffer pool, upgrade to a licensed Standard/Enterprise edition or a managed service.

## QuestPDF eligibility conclusion

- QuestPDF Community License is free for individuals and businesses with annual gross revenue under USD 1,000,000, plus qualifying charitable organisations, academic institutions, and open-source projects. Public-sector entities and publicly traded companies are not eligible regardless of revenue. Source: https://www.questpdf.com/license/community.html and https://www.questpdf.com/license/guide.html.
- This project is an individual's synthetic non-production public demo. Likely eligible under "individual use" or "learning and evaluation" provided the individual's annual gross revenue from all sources is under USD 1,000,000.
- If eligibility is unclear or the revenue threshold is exceeded, disable PDF download in the public deployment or purchase a paid license before enabling PDFs.
- Approval gate: confirm QuestPDF eligibility (revenue threshold) before enabling PDF download in production.

## Exact VM / container / database topology

Single Google Cloud Compute Engine VM (Linux, e2-medium recommended):

- VM: `inkassoakten-demo` (e2-medium: 2 vCPU, 4 GB RAM, 30 GB balanced persistent disk) in the chosen region/zone.
- Docker on the VM runs three containers on a private Docker network `inkassoakten_net`:
  1. `caddy` (reverse proxy, ports 80/443 published to the VM; auto HTTPS via Let's Encrypt).
  2. `inkassoakten-web` (ASP.NET Core, HTTP on 8080, internal only).
  3. `inkassoakten-mssql` (SQL Server 2022 Express, private network only, no published port).
- Persistent disk/volume: `inkassoakten_mssql_data` Docker volume on the VM's persistent disk for SQL Server data files.
- Public HTTPS only for the web application (Caddy -> app). SQL Server port 1433 is NOT published and is reachable only inside `inkassoakten_net`.
- Caddy proxies Blazor Server WebSocket connections (automatic upgrade; no buffering; timeouts >= 120s).
- Dedicated least-privilege read-only database login `inkassoakten_app` for the web app (db_datareader + EXECUTE on stored procs; no DDL, no db_datawriter).
- Separate administrative identity `inkassoakten_admin` used only for migrations and initial seeding (db_owner on the application database; not used by the app at runtime).
- Fail-closed `Security:PublicReadOnly=true` in the public deployment. No operator login, no payment posting, no state transitions, no persisted mutations.
- Read-only features remain available: case list, case details, ledger, aging report, PDF download, and non-persisting payment preview.

## Environment variables (names only, no values)

Application container:

- `ConnectionStrings__DefaultConnection` (read-only `inkassoakten_app` login)
- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_URLS=http://+:8080`
- `Security__RequireOperatorAuth=true`
- `Security__PublicReadOnly=true`
- `Auth__Operator__Username` / `Auth__Operator__Password` (admin identity; only meaningful when PublicReadOnly is false)
- `Auth__CookieName`
- `QuestPdf__LicenseKey` (if a paid license is purchased; otherwise Community License applies)

SQL Server container (bootstrap only):

- `MSSQL_SA_PASSWORD` (rotated, stored in secret manager, never in source control)
- `MSSQL_PID=Express`
- `MSSQL_DB_NAME=InkassoAkten`

Tests only:

- `TEST_SQLSERVER_CONNECTIONSTRING`

Never commit values. Reference by placeholder only.

## PublicReadOnly enforcement and tests

Implemented and tested locally (Phase4AuthTests):

- `Security:PublicReadOnly` switch is fail-closed: when unset in Production, writes remain disabled (no anonymous writes).
- Anonymous reads work with PublicReadOnly on (`/akten`, `/berichte`, PDF endpoint return 200).
- Operator login endpoint is disabled when PublicReadOnly is on (POST `/auth/login` redirects back to `/login`).
- All persisted web mutations are rejected server-side (`CanWrite == false` => `UnauthorizedAccessException` on `SaveChanges`).
- Even a valid operator credential cannot write while PublicReadOnly is on (TestOperatorContext with isOperator=true still CanWrite=false).
- Payment preview does not persist (preview path does not call SaveChanges).
- Existing tests remain green (74 tests: 35 domain + 39 integration).

Relevant source: `IOperatorContext.PublicReadOnly` / `CanWrite`, `SecurityInterfaces`, `Program.cs` DI wiring, `Phase4AuthTests.cs`.

## Migration / seed / start sequence

1. Provision the VM (approval gate).
2. Install Docker and Caddy on the VM.
3. Create the private Docker network and the `inkassoakten_mssql_data` volume.
4. Start the SQL Server Express container on the private network (no published port).
5. Connect as `inkassoakten_admin` and create the database.
6. Create the least-privilege `inkassoakten_app` login and grant read/execute.
7. Set environment variables (names above; values in secret manager).
8. Apply migrations as admin:
   `dotnet ef database update --project src/InkassoAkten.Infrastructure --startup-project src/InkassoAkten.Web`
9. Seed synthetic demo data:
   `dotnet run --project src/InkassoAkten.Web -- --seed-demo`
   (explicit opt-in; idempotent for the expected dataset; refuses incompatible existing data)
10. Start the web app container with the read-only login and `Security__PublicReadOnly=true`.
11. Configure Caddy for HTTPS + WebSocket upgrade.
12. Verify `/health`, one case detail page, one report, and one PDF.

The seed command is idempotent and refuses incompatible existing data without changing it (covered by DemoSeedTests).

## HTTPS and Blazor connection handling

- Terminate HTTPS at Caddy (Let's Encrypt). App uses HTTP internally on 8080.
- Caddy reverse_proxy must support WebSocket upgrade for Blazor Server circuits (automatic in Caddy).
- Proxy timeouts >= 120 seconds for long-lived connections.
- Do not buffer SignalR/WebSocket traffic.
- HSTS and secure cookies when HTTPS is enabled.
- Single app instance for the demo; no sticky sessions/backplane needed.

Caddyfile sketch (for approval, not applied):

```
inkassoakten.example.com {
    reverse_proxy inkassoakten-web:8080 {
        transport http {
            read_timeout 120s
            write_timeout 120s
        }
    }
}
```

## Database backup and recovery

- Scheduled SQL Server backups to a separate path on the persistent disk (not inside the data volume).
- Copy backups off-host (e.g., to Google Cloud Storage bucket) on a defined schedule.
- Test restore into an isolated database before relying on it.
- Keep migration scripts and the previous app image available for rollback.
- Document the last successful backup and restore test.

## Application rollback vs. database rollback

- Application rollback: redeploy the previous app image/container. Do not change the database.
- Database rollback: restore from a verified backup or apply a tested down migration.
- Treat application rollback and database rollback as separate operations.
- If a migration is incompatible with the previous app version, restore the database backup and then redeploy the previous app image.
- Never combine an untested migration rollback with a production/demo cutover.

## Health checks

- `/health` is public and returns only `{ status, app }`.
- No dependency details, connection strings, or diagnostic data.
- Startup probe verifies SQL Server connectivity before marking the container healthy.
- Readiness probe confirms migration state; do not serve traffic before migrations complete.

## Cost estimate against available credit

Assumptions: region us-central1 (or nearest), on-demand pricing, single VM, no committed use discounts.

| Item | Estimate (USD/month) |
|---|---|
| Compute Engine e2-medium (2 vCPU, 4 GB) | ~25 |
| Balanced persistent disk 30 GB | ~3 |
| Egress (demo traffic, minimal) | ~1-2 |
| SQL Server 2022 Express license | 0 (free) |
| Caddy / Let's Encrypt | 0 |
| QuestPDF Community License (if eligible) | 0 |
| **Total** | **~29-30** |

- Google Cloud trial credit: USD 300 (typically expires 90 days after activation). At ~USD 30/month this covers ~10 months of runtime but the credit must be used within the trial window; plan usage accordingly and monitor burn rate.
- If the trial expires or is insufficient, fallback to AWS only after eligibility checks (see AWS fallback).
- Costs depend on region, disk type, and traffic; re-estimate before provisioning. Do not assume credits cover SQL Server licensing or all services (Express is free; Standard/Enterprise is not).

## Exact actions requiring approval

No action below may be taken without explicit approval:

1. `gcloud auth login` (interactive) to verify account state, credit, region, and billing.
2. Provision the Compute Engine VM and persistent disk (cost).
3. DNS / firewall / reverse-proxy changes to expose a public URL.
4. AWS fallback provisioning (only after eligibility checks).
5. Create/rotate the `sa` password and the least-privilege `inkassoakten_app` login.
6. Run migrations and the seed command against the demo database.
7. Enable the public HTTPS endpoint and secure cookies.
8. Confirm QuestPDF Community License eligibility (revenue threshold) before enabling PDF download; otherwise disable PDF or buy a license.
9. Any change to operator credentials or rate-limit settings.
10. Any change to `Security:PublicReadOnly` in a deployed environment.

Stop before provisioning or deployment. All items above are approval-gated.
