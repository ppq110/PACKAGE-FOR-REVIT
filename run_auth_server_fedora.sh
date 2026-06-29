#!/usr/bin/env bash
set -euo pipefail

ENV_FILE="${ENV_FILE:-$HOME/.config/bimlab/dynlock-auth.env}"

if [[ ! -f "$ENV_FILE" ]]; then
  echo "Missing env file: $ENV_FILE"
  echo "Run ./setup_fedora_auth_server.sh first."
  exit 1
fi

set -a
# shellcheck disable=SC1090
source "$ENV_FILE"
set +a

REPO_DIR="$(cd "$(dirname "$0")" && pwd)"

if command -v dotnet >/dev/null 2>&1; then
  exec dotnet run --project "$REPO_DIR/src/DynLock.AuthServer/DynLock.AuthServer.csproj" -c Release
fi

if ! command -v docker >/dev/null 2>&1; then
  echo "Neither dotnet nor docker is available. Install dotnet-sdk-8.0 or Docker."
  exit 1
fi

exec docker run --rm \
  --name bimlab-auth-server \
  --network bimlab_infra \
  -p 5051:5051 \
  -v "$REPO_DIR:/src:Z" \
  -w /src \
  -e DYNLOCK_AUTH_SERVER_URL="$DYNLOCK_AUTH_SERVER_URL" \
  -e DYNLOCK_AUTH_SERVER_ADMIN_EMAIL="$DYNLOCK_AUTH_SERVER_ADMIN_EMAIL" \
  -e DYNLOCK_AUTH_DATABASE_PROVIDER="$DYNLOCK_AUTH_DATABASE_PROVIDER" \
  -e DYNLOCK_AUTH_DATABASE_CONNECTION_STRING="$DYNLOCK_AUTH_DATABASE_CONNECTION_STRING" \
  -e DYNLOCK_AUTH_SERVER_BIND_URL="$DYNLOCK_AUTH_SERVER_BIND_URL" \
  mcr.microsoft.com/dotnet/sdk:8.0 \
  dotnet run --project /src/src/DynLock.AuthServer/DynLock.AuthServer.csproj -c Release
