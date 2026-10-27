#!/usr/bin/env bash
# Restore from a .bak into a FRESH isolated database for verification.
# Never run against the live DB without explicit approval.
set -euo pipefail

: "${SA_PASSWORD:?SA_PASSWORD must be set}"
: "${BAK_PATH:?BAK_PATH must be set (absolute path to .bak)}"
RESTORE_DB="${RESTORE_DB:-InkassoDb_restore_test}"

docker cp "${BAK_PATH}" inkassoakten-db-1:/var/opt/mssql/backup/restore.bak
docker exec inkassoakten-db-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "${SA_PASSWORD}" -C -Q \
  "IF DB_ID(N'${RESTORE_DB}') IS NOT NULL ALTER DATABASE [${RESTORE_DB}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE [${RESTORE_DB}] FROM DISK = N'/var/opt/mssql/backup/restore.bak' WITH REPLACE, RECOVERY;"
echo "Restored into ${RESTORE_DB}. Verify, then drop it."
EOF