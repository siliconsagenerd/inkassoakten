# InkassoAkten Deployment Plan (Phase 4A readiness)

Status: readiness and assessment only. No deployment, firewall, DNS, tunnel, or cloud-resource changes were made in this phase.

## Recommended host

Recommended for a public demo: a separate small Linux VM/VPS or a licensed SQL Server host, not the current `oldmac` machine.

`oldmac` is suitable for private development and non-public testing only. It already runs SQL Server 2022 Developer Edition and several unrelated workloads, has no host firewall, and exposes SQL Server on `0.0.0.0:1433`.

For a public demo, use a host with:

- 2 vCPU minimum, 4 vCPU recommended
- 4 GB RAM minimum, 8 GB recommended
- 40 GB disk minimum, with separate backup storage
- A licensed or Express SQL Server instance
- A reverse proxy or Tailscale Funnel configured for HTTPS and WebSocket upgrade

## Application and SQL Server topology

- Blazor Interactive Server runs as one ASP.NET Core process/container.
- SQL Server runs as a separate container or managed instance.
- The web app connects through `ConnectionStrings__DefaultConnection`.
- The app does not auto-migrate on normal startup. Run migrations before starting the app.
- For the synthetic demo, run the standalone seed command after migrations.
- Keep one app instance for the demo. Blazor Server circuits do not require sticky sessions for a single instance.

## Persistent storage

- SQL Server data: Docker volume `inkassoakten_mssql_data` (source `/var/lib/docker/volumes/inkassoakten_mssql_data/_data`).
- Application container: stateless; no persistent application volume is required.
- Backups must be written outside the SQL data volume and copied off-host.
- Container logs are not a backup mechanism.

## Environment variables

Set these without storing values in source control:

- `ConnectionStrings__DefaultConnection`
- `ASPNETCORE_ENVIRONMENT`
- `ASPNETCORE_URLS`
- `MSSQL_SA_PASSWORD` (container bootstrap only, not application config)
- `MSSQL_DB_NAME`
- `TEST_SQLSERVER_CONNECTIONSTRING` (tests only)

Optional future access-control settings:

- `DemoAuth__OperatorUserName`
- `DemoAuth__OperatorPassword`
- `DemoAuth__CookieName`

## Demo access boundaries

Proposed public-demo boundary:

- Anonymous users may read synthetic case lists, case details, reports, and PDFs.
- Payment posting, status transitions, plan changes, and all other writes require server-side authorization.
- Hiding buttons is not sufficient. Blazor components invoke services directly, so every write path must be checked on the server.
- No public database, backup, secret, configuration, or diagnostic endpoint access.
- `/health` may remain public but must return only minimal status.
- PDF generation should be rate-limited.
- All demo data must remain synthetic.

Required changes before implementing authentication or access control:

1. Add an authentication scheme and a demo operator account sourced from environment variables or a secret store.
2. Add authorization policies for read and write operations.
3. Move or wrap all mutating operations behind application services that enforce authorization.
4. Update `AkteDetail.razor` write handlers to check authorization and fail closed.
5. Protect `ZahlungPostingService` and state-transition calls with the same policy.
6. Keep the PDF endpoint anonymous but validate the case id and rate-limit requests.
7. Add integration tests for anonymous read success and unauthorized write rejection.
8. Add a visible demo banner stating that data is synthetic.

## Migration and standalone seed sequence

1. Start SQL Server and confirm the volume is available.
2. Set `ConnectionStrings__DefaultConnection`.
3. Apply migrations:

   `dotnet ef database update --project src/InkassoAkten.Infrastructure --startup-project src/InkassoAkten.Web`

4. Seed synthetic demo data:

   `dotnet run --project src/InkassoAkten.Web -- --seed-demo`

5. Start the web app.
6. Verify `/health`, one case detail page, one report, and one PDF.

The seed command is explicit opt-in. It applies migrations, inserts the five synthetic cases only when the case table is empty, is idempotent for the expected demo dataset, and refuses incompatible existing data without changing it.

## HTTPS and Blazor connection handling

- Terminate HTTPS at Tailscale Funnel or a reverse proxy. The app may use HTTP internally.
- The proxy must support WebSocket upgrade for Blazor Server circuits.
- Set proxy timeouts to at least 120 seconds for long-lived connections.
- Do not buffer SignalR/WebSocket traffic.
- Use HSTS and secure cookies when HTTPS is enabled.
- If multiple app instances are ever added, add sticky sessions or a shared backplane; this is not part of the current demo.

## Database backup and recovery

- Take SQL Server backups to a separate location on a defined schedule.
- Copy backups off-host.
- Test restore into an isolated database before relying on it.
- Keep migration scripts and the previous app image available for rollback.
- Document the last successful backup and restore test.

## Application rollback and database rollback

- Application rollback: redeploy the previous app image/container. Do not change the database.
- Database rollback: restore from a verified backup or apply a tested down migration.
- Treat application rollback and database rollback as separate operations.
- If a migration is incompatible with the previous app version, restore the database backup and then redeploy the previous app image.
- Never combine an untested migration rollback with a production/demo cutover.

## Health checks

- `/health` is public and returns only `{ status, app }`.
- Do not expose dependency details, database connection strings, or diagnostic data.
- Add a startup probe that verifies SQL Server connectivity before marking the container healthy.
- Add a readiness probe that confirms migration state; do not serve traffic before migrations complete.

## Exact later actions requiring approval

1. Implement authentication and authorization (cookie auth is already wired; add a demo operator account from environment variables, authorization policies, and server-side write guards).
2. Expose a public URL (DNS, firewall, reverse proxy, or Tailscale Funnel changes).
3. Provision or rent a host or managed SQL instance.
4. Rotate the `sa` password and create a least-privilege application login.
5. Replace SQL Server Developer Edition with Express or a licensed edition.
6. Run the seed command against the production/demo database.
7. Enable HTTPS and secure cookies for the public endpoint.
8. Any change to operator credentials or rate-limit settings.

No action above may be taken without explicit approval.

## Observed oldmac baseline (read-only measurements)

Captured 2026-10-26 via SSH. No changes were made to oldmac.

- Architecture: x86_64, 4 vCPU.
- Memory: 7.7 GiB total, ~4.1 GiB used, ~3.6 GiB available; 8.8 GiB swap with ~3.3 GiB used.
- Disk: root 225 GiB, 128 GiB used (60%), 86 GiB free; /tmp 3.9 GiB.
- Load average: 3.45 / 3.56 / 3.58 (saturated for 4 cores).
- SQL Server container `inkassoakten_mssql`: 294 MiB RAM, Developer Edition (`MSSQL_PID=developer`), published on `0.0.0.0:1433`.
- Co-resident workloads: omniroute (~905 MiB, ~47% CPU), onboardpilot frontend/backend, laya-adapter, portainer, gitea, uptime-kuma, postgres, redis, np-py.
- Conclusion: oldmac is a shared, saturated development host. It is not a suitable public-demo host. Use it only for private development and integration testing.

## Security proposal for any future public demo

- Host firewall: deny inbound by default; allow only 22 (restricted), 80/443 (proxy), and Tailscale. Do not publish SQL Server port 1433.
- SQL Server: bind to the Docker network or 127.0.0.1 only; never 0.0.0.0. Use a non-sa application login with least privilege. Rotate the sa password and store it in a secret manager, not in source control or committed `.env`.
- TLS: terminate HTTPS at Tailscale Funnel or a reverse proxy (Caddy/nginx). HTTP internally only. HSTS and secure cookies when HTTPS is enabled.
- App: anonymous read of synthetic data only; all writes require server-side authorization (not UI hiding). Rate-limit PDF generation. Minimal `/health` output. Visible synthetic-data banner. No public DB/backup/secret/diagnostic endpoints.
- Secrets: environment variables or secret store; never committed. Placeholder names only in docs/examples.
- Backups: scheduled SQL backups to a separate path, copied off-host, restored and verified into an isolated database before relying on them.
- Licensing: replace Developer Edition with Express (free, size-limited) or a licensed edition before any public use.

## Costs and unresolved questions

- SQL Server Developer Edition is free for development and testing only. It is not licensed for a public demo or production use.
- SQL Server Express is free but has database-size and resource limits.
- SQL Server Standard/Enterprise requires a paid license.
- A suitable small VPS is expected to cost roughly USD 20-40 per month for 4 GB RAM; actual prices depend on provider and region.
- Managed SQL may cost roughly USD 15-50 per month depending on tier.
- Tailscale Funnel is already configured on `oldmac` for another service. Confirm plan limits and WebSocket support before using it for InkassoAkten.
- Confirm whether the public demo should be internet-accessible or Tailscale-only.
- Confirm who owns patching and uptime for the host.
- Confirm whether a separate SQL Server license or Express migration is approved.
