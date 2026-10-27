#!/usr/bin/env bash
# Restore from a .bak into a FRESH isolated database for verification.
# Never run against the live DB without explicit approval.
set -euo pipefail

: "${SA_PASSWORD:?SA_PASSWORD must be set}"
: "${BAK_PATH:?BAK_PATH must be set (absolute path to .bak)}"
RESTORE_DB="${RESTORE_DB:-InkassoDb_restore_test}"

CONTAINER_BAK="/var/opt/mssql/backup/restore-$(basename "${BAK_PATH}").$$"
docker cp "${BAK_PATH}" "inkassoakten-db-1:${CONTAINER_BAK}"
docker exec -u 0 inkassoakten-db-1 chmod 644 "${CONTAINER_BAK}"
docker exec inkassoakten-db-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "${SA_PASSWORD}" -C -Q \
  "IF DB_ID(N'${RESTORE_DB}') IS NOT NULL ALTER DATABASE [${RESTORE_DB}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE [${RESTORE_DB}] FROM DISK = N'${CONTAINER_BAK}' WITH REPLACE, RECOVERY, MOVE N'InkassoDb' TO N'/var/opt/mssql/data/${RESTORE_DB}.mdf', MOVE N'InkassoDb_log' TO N'/var/opt/mssql/data/${RESTORE_DB}_log.ldf';"
docker exec inkassoakten-db-1 rm -f "${CONTAINER_BAK}"
echo "Restored into ${RESTORE_DB}. Verify, then drop it."
