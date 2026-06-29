#!/usr/bin/env bash
set -euo pipefail

REPO_DIR="$(cd "$(dirname "$0")" && pwd)"
ENV_FILE="${ENV_FILE:-$HOME/.config/bimlab/dynlock-auth.env}"
SERVICE_FILE="$HOME/.config/systemd/user/bimlab-auth.service"

if [[ ! -f "$ENV_FILE" ]]; then
  echo "Missing env file: $ENV_FILE"
  echo "Run ./setup_fedora_auth_server.sh first."
  exit 1
fi

mkdir -p "$(dirname "$SERVICE_FILE")"

cat > "$SERVICE_FILE" <<EOF
[Unit]
Description=BIMLab Auth Server
After=network.target

[Service]
Type=simple
WorkingDirectory=$REPO_DIR
EnvironmentFile=$ENV_FILE
ExecStart=$REPO_DIR/run_auth_server_fedora.sh
Restart=always
RestartSec=5

[Install]
WantedBy=default.target
EOF

systemctl --user daemon-reload
systemctl --user enable --now bimlab-auth.service

echo "[OK] Installed and started user service: bimlab-auth.service"
echo "Check status:"
echo "  systemctl --user status bimlab-auth.service"
echo "Follow logs:"
echo "  journalctl --user -u bimlab-auth.service -f"
