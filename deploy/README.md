# InkassoAkten public demo - deployment runbook

All commands run on the VM unless noted. Secrets live in `.env` (never committed).

## 1. Prepare the VM (after approval)

- Install Docker + Compose plugin.
- Create `/opt/inkassoakten`, clone repo, copy `deploy/` there.
- `cp deploy/.env.example deploy/.env` and fill values.
- `chmod 600 deploy/.env`.

## 2. First deploy

```bash
cd /opt/inkassoakten/deploy
docker compose build web
docker compose up -d db
# wait for db healthy
docker compose exec db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -C -Q "SELECT 1"
# migrations + seed (admin scope, one-shot)
docker compose run --rm -e ConnectionStrings__DefaultConnection="Server=db,1433;Database=InkassoDb;User Id=sa;Password=$SA_PASSWORD;TrustServerCertificate=True" web dotnet InkassoAkten.Web.dll --seed-demo
# create read-only app login
docker compose exec -T db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -C -i /dev/stdin < sql/create-readonly-user.sql
# (substitute <APP_PASSWORD> first, or run the file after sed)
docker compose up -d
```

## 3. Verify

- `curl -fsS https://$PUBLIC_HOSTNAME/health`
- Open `/akten`, `/berichte`, download a PDF.
- Try `/login` -> must redirect to `/login` (operator login disabled in public mode).
- Try a persisted write with an operator identity -> must be rejected.
- `docker compose ps` -> all healthy.
- Confirm no SQL port published: `ss -lntp | grep 1433` (empty).

## 4. Backup

```bash
cd /opt/inkassoakten/deploy
SA_PASSWORD=... ./backup.sh
```

Copy the resulting `.bak` to the approved off-host directory (oldmac `/backups/inkassoakten` or equivalent). Keep last 7 on the VM.

## 5. Restore test (isolated DB, never the live DB)

```bash
SA_PASSWORD=... BAK_PATH=/path/to.bak RESTORE_DB=InkassoDb_restore_test ./restore.sh
# verify row counts, then drop:
docker compose exec db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -C -Q "DROP DATABASE [InkassoDb_restore_test];"
```

## 6. Rollback

```bash
cd /opt/inkassoakten
git checkout <previous-commit>
cd deploy
docker compose build web
docker compose up -d web
```

Database rollback requires restoring from a pre-change backup (see section 5).

## 7. Secret handling

- `.env` mode 600, owned by deploy user.
- No secret in Git, image layers, logs, or public dir.
- Rotate `SA_PASSWORD` and `APP_PASSWORD` after any exposure.
EOF
echo done