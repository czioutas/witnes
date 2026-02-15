#!/usr/bin/env bash
set -euo pipefail

echo "Installing git hooks..."
echo ""

GIT_ROOT="$(git rev-parse --show-toplevel 2>/dev/null || true)"
if [ -z "$GIT_ROOT" ]; then
  echo "Error: Not in a git repository"
  exit 1
fi

HOOKS_SRC="$GIT_ROOT/scripts/git-hooks"
HOOKS_DEST="$GIT_ROOT/.git/hooks"

if [ ! -d "$HOOKS_SRC" ]; then
  echo "Error: Hooks directory not found at $HOOKS_SRC"
  exit 1
fi

installed_count=0
for hook in "$HOOKS_SRC"/*; do
  if [ -f "$hook" ]; then
    hook_name="$(basename "$hook")"
    cp "$hook" "$HOOKS_DEST/$hook_name"
    chmod +x "$HOOKS_DEST/$hook_name"
    echo "Installed: $hook_name"
    installed_count=$((installed_count + 1))
  fi
done

echo ""
if [ "$installed_count" -eq 0 ]; then
  echo "No hook files found in $HOOKS_SRC"
  exit 1
fi

echo "Git hooks installed successfully"
echo ""
echo "Your commits will now be validated against the GitVersion format:"
echo "  type: <breaking|major|feature|minor|fix|patch|none|skip> - <description>"
echo ""
