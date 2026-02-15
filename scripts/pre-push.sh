#!/usr/bin/env bash

set -e
set -u
set -o pipefail

if [ ! -d "code/tracker" ] || [ ! -d "code/api/Api" ] || [ ! -d "scripts" ]; then
  echo "Error: Must be run from the repository root directory"
  exit 1
fi

echo "Running pre-push checks..."

./scripts/build-tracker.sh
./scripts/format-code.sh
./scripts/generate-openapi.sh

echo "Pre-push checks complete"
