#!/bin/bash

# Witnes OpenAPI Generator Script
# This script builds the API, generates the OpenAPI spec,
# moves it to the frontend directory, and generates the frontend client.

set -e          # Exit on any error
set -u          # Exit on undefined variable
set -o pipefail # Exit on pipe failure

echo "📄 Witnes OpenAPI Generator & Frontend Client Setup"
echo "========================================================"

# Check if we're in the right directory
if [ ! -d "code/api/Api" ]; then
    echo "❌ Error: Must be run from the repository root directory"
    exit 1
fi

# Build the API first to ensure the DLL is up-to-date
echo "📦 Building API..."
# Using pushd/popd to manage directories without cd
pushd code/api/Api > /dev/null
if ! dotnet build; then
    echo "❌ API build failed"
    popd > /dev/null
    exit 1
fi
popd > /dev/null
echo "✅ API build completed"

# Generate OpenAPI spec (run from API dir so appsettings.json is found)
echo "📄 Generating OpenAPI specification..."
pushd code/api/Api > /dev/null
if ! dotnet swagger tofile --output ../../../code/openapi.json bin/Debug/net10.0/api.dll v1; then
    echo "❌ OpenAPI specification generation failed"
    popd > /dev/null
    exit 1
fi
popd > /dev/null
echo "✅ OpenAPI spec generated: code/openapi.json"

# Move spec to frontend directory
echo "🚚 Moving OpenAPI spec to frontend directory..."
if ! mv code/openapi.json code/fe/openapi.json; then
    echo "❌ Failed to move openapi.json"
    exit 1
fi
echo "✅ Spec moved to code/fe/openapi.json"

# Navigate to frontend directory to run client generation
pushd code/fe > /dev/null

# Remove old generated client
echo "🗑️ Removing old generated client..."
if [ -d "src/generated" ]; then
    rm -rf src/generated
    echo "✅ Old client removed"
else
    echo "ℹ️ No old client directory to remove"
fi


# Generate frontend client
echo "🚀 Generating frontend client..."
if ! npm run generate-client; then
    echo "❌ Frontend client generation failed"
    popd > /dev/null
    exit 1
fi
echo "✅ Frontend client generated"

popd > /dev/null

echo "🎉 OpenAPI spec and frontend client generated successfully!"