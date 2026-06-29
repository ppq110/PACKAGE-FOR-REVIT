#!/usr/bin/env bash
set -euo pipefail

ENV_FILE="${ENV_FILE:-$HOME/.config/bimlab/dynlock-auth.env}"

if [[ ! -f "$ENV_FILE" ]]; then
  echo "Missing env file: $ENV_FILE"
  echo "Run ./setup_fedora_auth_server.sh first."
  exit 1
fi

# shellcheck disable=SC1090
source "$ENV_FILE"

dotnet run --project "$(dirname "$0")/src/DynLock.AuthServer/DynLock.AuthServer.csproj" -c Release
