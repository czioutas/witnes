#!/usr/bin/env bash

set -e
set -u
set -o pipefail

if [ ! -d "code/tracker" ]; then
  echo "Error: Must be run from the repository root directory"
  exit 1
fi

pushd code/tracker > /dev/null
npm run build
popd > /dev/null

echo "Tracker build complete"
