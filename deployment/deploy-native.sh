#!/bin/bash

# Witnes API Deployment Script (Native/Systemd)
# This script deploys the .NET API without Docker

set -e

echo "================================"
echo "Witnes API Deployment"
echo "================================"

# Configuration
REPO_DIR="/home/hs/repos/witnes/witnes"
API_DIR="${REPO_DIR}/code/api/Api"
SERVICE_NAME="witnes-api"

# Pull latest code
echo "Pulling latest code from main branch..."
cd "$REPO_DIR"
git fetch origin
git reset --hard origin/main

# Build the API
echo "Building API..."
cd "$API_DIR"
dotnet publish -c Release -o bin/Release/net9.0/publish

# Stop the service if running
echo "Stopping service..."
sudo systemctl stop "$SERVICE_NAME" || true

# Copy the systemd service file
echo "Installing systemd service..."
sudo cp "${REPO_DIR}/deployment/witnes-api.service" /etc/systemd/system/
sudo systemctl daemon-reload

# Start the service
echo "Starting service..."
sudo systemctl enable "$SERVICE_NAME"
sudo systemctl start "$SERVICE_NAME"

# Check status
echo ""
echo "Deployment complete! Service status:"
sudo systemctl status "$SERVICE_NAME" --no-pager

echo ""
echo "Useful commands:"
echo "  View logs:    sudo journalctl -u $SERVICE_NAME -f"
echo "  Restart:      sudo systemctl restart $SERVICE_NAME"
echo "  Stop:         sudo systemctl stop $SERVICE_NAME"
echo "  Status:       sudo systemctl status $SERVICE_NAME"
