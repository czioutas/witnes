#!/bin/bash

# Witnes API Deployment Script
# This script is designed to be called by GitHub Actions or manually
# Location: /root/deploy-api.sh

set -e

echo "========================================"
echo "Witnes API Deployment"
echo "========================================"
echo ""

# Configuration
REPO_DIR="/opt/witnes/witnes"
API_DIR="${REPO_DIR}/code/api/Api"
SERVICE_NAME="witnes-api"

# Check if running as root
if [ "$(id -u)" -ne 0 ]; then
    echo "❌ This script must be run as root"
    exit 1
fi

echo "📥 Pulling latest code..."
cd "$REPO_DIR"
git fetch origin
git reset --hard origin/main
echo "✓ Code updated"

echo ""
echo "🏗️  Building API..."
cd "$API_DIR"
dotnet restore
dotnet build -c Release
echo "✓ Build complete"

echo ""
echo "🔧 Updating systemd service..."
cp "${REPO_DIR}/deployment/witnes-api.service" /etc/systemd/system/
systemctl daemon-reload
echo "✓ Service file updated"

echo ""
echo "🔄 Restarting service..."
systemctl restart "$SERVICE_NAME"
echo "✓ Service restarted"

echo ""
echo "⏳ Waiting for service to start..."
sleep 3

echo ""
echo "📊 Service Status:"
echo "----------------------------------------"
systemctl status "$SERVICE_NAME" --no-pager || true

echo ""
if systemctl is-active --quiet "$SERVICE_NAME"; then
    echo "✅ Deployment successful!"
    echo ""
    echo "📜 View logs: journalctl -u $SERVICE_NAME -f"
    echo "🔗 API: http://localhost:7070"
else
    echo "❌ Service failed to start!"
    echo ""
    echo "🔍 Recent logs:"
    journalctl -u "$SERVICE_NAME" -n 30 --no-pager
    exit 1
fi
