#!/usr/bin/env bash
# Stops Supplier Hub's API, database and file storage on macOS or Linux.
# Usage: ./stop.sh          keeps your data for next time
#        ./stop.sh --reset  also deletes all data (suppliers, photos, videos)
set -euo pipefail
cd "$(dirname "$0")/backend"

if [ "${1:-}" = "--reset" ]; then
  docker compose down -v
  echo "Stopped, and all data deleted. The next ./start.sh begins with the sample suppliers again."
else
  docker compose down
  echo "Stopped. Your data is kept; run ./start.sh to start again."
fi
