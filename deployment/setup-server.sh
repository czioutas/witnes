#!/bin/bash

# Witnes Server Initial Setup Script
# This script sets up a clean server for running Witnes

set -e

echo "========================================"
echo "Witnes Server Initial Setup"
echo "========================================"
echo ""

# Configuration
REPO_URL="https://github.com/Witnes/witnes.git"
REPO_DIR="/home/hs/repos/witnes/witnes"
ENV_FILE="/home/hs/repos/witnes/.env"
SERVICE_NAME="witnes-api"

# Check if running as root
if [ "$(id -u)" -ne 0 ]; then
    echo "❌ This script must be run as root (use sudo)"
    exit 1
fi

echo "📦 Step 1: Installing dependencies..."
echo "----------------------------------------"

# Update package list
apt-get update

# Install .NET 10 SDK
if ! command -v dotnet &> /dev/null; then
    echo "Installing .NET 10 SDK..."
    wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
    dpkg -i packages-microsoft-prod.deb
    rm packages-microsoft-prod.deb
    apt-get update
    apt-get install -y dotnet-sdk-10.0
    echo "✓ .NET 10 SDK installed"
else
    echo "✓ .NET SDK already installed"
    dotnet --version
fi

echo ""
echo "⚙️  Step 2: Setting up environment file..."
echo "----------------------------------------"

# Create .env file from template if not exists
if [ ! -f "$ENV_FILE" ]; then
    cp "${REPO_DIR}/deployment/.env.template" "$ENV_FILE"
    echo "✓ Environment file created at $ENV_FILE"
    echo ""
    echo "⚠️  IMPORTANT: You must edit $ENV_FILE and fill in all secrets!"
    echo "   Replace all YOUR_* placeholders with actual values."
    echo ""
    read -p "Press Enter to open the editor, or Ctrl+C to exit and edit manually..."
    nano "$ENV_FILE"
else
    echo "✓ Environment file already exists at $ENV_FILE"
fi

echo ""
echo "🔧 Step 3: Installing systemd service..."
echo "----------------------------------------"

# Copy systemd service file
cp "${REPO_DIR}/deployment/witnes-api.service" /etc/systemd/system/
systemctl daemon-reload
systemctl enable "$SERVICE_NAME"
echo "✓ Systemd service installed and enabled (not started yet)"

echo ""
echo "📜 Step 4: Creating deployment script..."
echo "----------------------------------------"

# Create deployment script in /root
cat > /root/deploy-api.sh <<'DEPLOY_EOF'
#!/bin/bash
set -e
cd /home/hs/repos/witnes/witnes
git pull origin main
cd code/api/Api
dotnet build -c Release
sudo systemctl restart witnes-api
echo "Deployment complete!"
sudo systemctl status witnes-api --no-pager
DEPLOY_EOF

chmod +x /root/deploy-api.sh
echo "✓ Deployment script created at /root/deploy-api.sh"

echo ""
echo "========================================"
echo "✅ Setup Complete!"
echo "========================================"
echo ""
echo "⚠️  NEXT STEPS:"
echo ""
echo "📝 1. Edit .env and replace ALL placeholders"
echo "   nano $ENV_FILE"
echo ""
echo "   Replace:"
echo "   - YOUR_POSTGRES_PASSWORD     → Strong password (32+ chars)"
echo "   - YOUR_RABBITMQ_PASSWORD     → Strong password (32+ chars)"
echo "   - YOUR_SECRET_KEY_HERE       → JWT key (64+ chars)"
echo "   - YOUR_MAILTRAP_API_KEY      → Mailtrap key"
echo ""
echo "🐳 2. Start infrastructure (PostgreSQL, Redis, RabbitMQ, MinIO)"
echo "   cd ${REPO_DIR}/code && docker-compose -f production-full.yml up -d"
echo ""
echo "🚀 3. Deploy and start API"
echo "   /root/deploy-api.sh"
echo ""
echo "✅ 4. Verify"
echo "   docker ps"
echo "   systemctl status $SERVICE_NAME"
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "🔗 Useful Commands:"
echo "  Edit .env:          nano $ENV_FILE"
echo "  Start infrastructure: cd ${REPO_DIR}/code && docker-compose -f production-full.yml up -d"
echo "  Stop infrastructure:  cd ${REPO_DIR}/code && docker-compose -f production-full.yml down"
echo "  Start API:          systemctl start $SERVICE_NAME"
echo "  Restart API:        systemctl restart $SERVICE_NAME"
echo "  Stop API:           systemctl stop $SERVICE_NAME"
echo "  Check API status:   systemctl status $SERVICE_NAME"
echo "  View API logs:      journalctl -u $SERVICE_NAME -f"
echo "  Check all services: docker ps && systemctl status $SERVICE_NAME"
echo "  Deploy updates:     /root/deploy-api.sh"
echo ""
echo "🌐 Services (once started):"
echo "  API:                http://localhost:7070"
echo "  MinIO Console:      http://localhost:9001"
echo "  RabbitMQ Console:   http://localhost:15672"
echo ""
