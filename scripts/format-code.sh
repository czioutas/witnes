#!/usr/bin/env bash

# Format C# code using dotnet format
# This script formats the API codebase

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
API_DIR="$PROJECT_ROOT/code/api/Api"

echo "Formatting C# code in $API_DIR..."

cd "$API_DIR"
dotnet format

echo "✓ Code formatting complete"
