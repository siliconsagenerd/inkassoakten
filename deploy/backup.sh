#!/usr/bin/env bash
# VM-side backup: SQL Server BACKUP to a timestamped .bak on the VM disk,
# then copy out of the container. Keep last 7 backups.
set -euo pipefail

: "${SA_PASSWORD:?SA_PASSWORD must be set}"
TS=$(date +%Y%m%d-%H%M%S)
CONTAINER="inkassoakten-db-1"
IN_CONTAINER_PATH="/var/opt/mssql/backup/InkassoDb-${TS}.bak"
HOST_DIR="/var/backups/inkassoakten"
HOST_PATH="${HOST_DIR}/InkassoDb-${TS}.bak"

mkdir -p "${HOST_DIR}"
docker exec "${CONTAINER}" mkdir -p /var/opt/mssql/backup
docker exec "${CONTAINER}" /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "${SA_PASSWORD}" -C \
  -Q "BACKUP DATABASE [InkassoDb] TO DISK = N'${IN_CONTAINER_PATH}' WITH INIT, CHECKSUM;"

docker cp "${CONTAINER}:${IN_CONTAINER_PATH}" "${HOST_PATH}"
docker exec "${CONTAINER}" rm -f "${IN_CONTAINER_PATH}"

# Optional off-VM upload to GCS (set GCS_BUCKET, e.g. gs://inkassoakten-demo-backups)
if [ -n "${GCS_BUCKET:-}" ]; then
  gcloud storage cp "${HOST_PATH}" "${GCS_BUCKET}/InkassoDb-${TS}.bak"
  echo "Uploaded to ${GCS_BUCKET}/InkassoDb-${TS}.bak"
fi

# Retention: keep last 7
ls -t "${HOST_DIR}"/*.bak 2>/dev/null | tail -n +8 | xargs -r rm -f
echo "Backup created: ${HOST_PATH}"
