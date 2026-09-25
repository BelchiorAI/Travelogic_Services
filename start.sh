#!/usr/bin/env bash
# Starts Supplier Hub on macOS or Linux: the API, database and file storage in Docker, then the web app.
# Usage: ./start.sh        (stop the backend later with ./stop.sh)
set -euo pipefail
cd "$(dirname "$0")"

API_URL="http://localhost:5000"
GEMINI_ENDPOINT="https://generativelanguage.googleapis.com/v1beta/openai/"

step() { printf '\n\033[1;34m==> %s\033[0m\n' "$1"; }
fail() { printf '\n\033[1;31mError:\033[0m %s\n' "$1" >&2; exit 1; }

# 1. Prerequisites, with plain instructions when something is missing.
step "Checking prerequisites"
command -v docker >/dev/null 2>&1 || fail "Docker is not installed. Install Docker Desktop: https://www.docker.com/products/docker-desktop/"
docker info >/dev/null 2>&1 || fail "Docker is installed but not running. Start Docker Desktop, wait until it says it is running, then try again."
command -v node >/dev/null 2>&1 || fail "Node.js is not installed. Install version 20 or newer from https://nodejs.org/"
node_major=$(node -p 'process.versions.node.split(".")[0]')
[ "$node_major" -ge 20 ] || fail "Node.js 20 or newer is needed (you have $(node -v)). Update it from https://nodejs.org/"
echo "Docker and Node.js $(node -v) found."

# 2. Optional AI import: asked once, remembered in backend/.env (git-ignored).
if [ ! -f backend/.env ]; then
  step "AI import (optional)"
  echo "AI import reads a pasted rate sheet or email and fills in the supplier form."
  echo "Paste a Google Gemini API key to switch it on, or just press Enter to skip."
  key=""
  if [ -t 0 ]; then read -r -s -p "Gemini API key: " key || true; echo; fi
  if [ -n "$key" ]; then
    printf 'AI_ENABLED=true\nAI_MODEL=gemini-3.5-flash\nAI_API_KEY=%s\nAI_ENDPOINT=%s\n' "$key" "$GEMINI_ENDPOINT" > backend/.env
    echo "AI import switched on (saved in backend/.env)."
  else
    printf '# AI import is off. To switch it on, set these and run ./start.sh again:\nAI_ENABLED=false\nAI_MODEL=gemini-3.5-flash\nAI_API_KEY=\nAI_ENDPOINT=%s\n' "$GEMINI_ENDPOINT" > backend/.env
    echo "Skipped. The app works without it; add a key to backend/.env later if you like."
  fi
fi

# 3. Backend: API + SQL Server + S3 store.
step "Starting the API, database and file storage"
echo "The first run downloads about 1 GB and builds the API, which can take several minutes."
(cd backend && docker compose up -d --build)

step "Waiting for the API to be ready"
ready=false
for _ in $(seq 1 90); do
  if curl -fs "$API_URL/health/ready" >/dev/null 2>&1; then ready=true; break; fi
  printf '.'; sleep 2
done
echo
$ready || fail "The API didn't become ready in 3 minutes. See what happened with: cd backend && docker compose logs api"
echo "API ready at $API_URL (docs: $API_URL/scalar/v1)."

# 4. Web app.
cd frontend
[ -f .env.local ] || cp .env.example .env.local
if [ ! -d node_modules ]; then
  step "Installing the web app's packages (first run only)"
  if command -v bun >/dev/null 2>&1; then bun install --frozen-lockfile; else npm install --no-audit --no-fund; fi
fi

step "Starting the web app"
echo "Your browser opens automatically. Press Ctrl+C here to stop the web app;"
echo "the API keeps running until you run ./stop.sh."
exec npm run dev -- --open
