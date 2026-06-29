#!/usr/bin/env bash
set -euo pipefail

AUTH_SERVER_URL="${AUTH_SERVER_URL:-http://192.168.110.213:5051}"
BIND_URL="${BIND_URL:-http://0.0.0.0:5051}"
SUPER_ADMIN_EMAIL="${SUPER_ADMIN_EMAIL:-projectbim.bimlab@gmail.com}"
DATABASE_PROVIDER="${DATABASE_PROVIDER:-postgres}"
POSTGRES_CONNECTION_STRING="${POSTGRES_CONNECTION_STRING:-Host=127.0.0.1;Port=5432;Database=bimlab_auth;Username=bimlab;Password=CHANGE_ME}"
ENV_FILE="${ENV_FILE:-$HOME/.config/bimlab/dynlock-auth.env}"

mkdir -p "$(dirname "$ENV_FILE")"

cat > "$ENV_FILE" <<EOF
DYNLOCK_AUTH_SERVER_URL="$AUTH_SERVER_URL"
DYNLOCK_AUTH_SERVER_ADMIN_EMAIL="$SUPER_ADMIN_EMAIL"
DYNLOCK_AUTH_DATABASE_PROVIDER="$DATABASE_PROVIDER"
DYNLOCK_AUTH_DATABASE_CONNECTION_STRING="$POSTGRES_CONNECTION_STRING"
DYNLOCK_AUTH_SERVER_BIND_URL="$BIND_URL"
EOF

chmod 600 "$ENV_FILE"

echo "[OK] Wrote Fedora auth server env: $ENV_FILE"
echo "[OK] Auth server URL : $AUTH_SERVER_URL"
echo "[OK] Bind URL        : $BIND_URL"
echo "[OK] Super admin     : $SUPER_ADMIN_EMAIL"
echo "[OK] DB provider     : $DATABASE_PROVIDER"
echo
echo "Before starting, make sure Fedora has these packages:"
echo "  sudo dnf install -y dotnet-sdk-8.0 postgresql-server postgresql-contrib"
echo
echo "If firewalld is enabled, open port 5051:"
echo "  sudo firewall-cmd --add-port=5051/tcp --permanent"
echo "  sudo firewall-cmd --reload"
echo
echo "Start Auth Server:"
echo "  ./run_auth_server_fedora.sh"
