---
name: commit-push-pr
description: Create a commit with proper format, push, and open a PR
---

```bash
# Pre-compute git info to avoid back-and-forth
GIT_STATUS=$(git status --porcelain)
GIT_DIFF=$(git diff --stat)
GIT_BRANCH=$(git branch --show-current)
GIT_LOG=$(git log --oneline -5)
STAGED_FILES=$(git diff --cached --name-only)
```

You are about to create a commit, push, and open a pull request for the Witnes project.

**CRITICAL: Commit Message Format**
ALL commits MUST follow this exact format:
```
type: <type> - <description>
```

Valid types:
- `type: feature` - New features (minor version bump)
- `type: fix` - Bug fixes (patch version bump)
- `type: breaking` - Breaking changes (major version bump)
- `type: none` - No version bump (docs, formatting)

**Current State:**
- Branch: ${GIT_BRANCH}
- Staged files: ${STAGED_FILES}
- Diff: ${GIT_DIFF}
- Recent commits: ${GIT_LOG}

**Your task:**
1. Review all changes (staged and unstaged)
2. Stage relevant files (if not already staged)
3. Create a descriptive commit message following the format above
4. Include co-author line: `Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>`
5. Push to remote
6. Create PR using `gh pr create` with:
   - Title matching commit message
   - Body with Summary section (bullet points of changes)
   - Body with Test plan section (how to test)
   - Footer: `🤖 Generated with [Claude Code](https://claude.com/claude-code)`

**Example commit:**
```bash
git commit -m "$(cat <<'EOF'
type: feature - Add water consumption tracking to activities

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
EOF
)"
```

**Example PR body:**
```markdown
## Summary
- Added water consumption activity tracking
- Created WaterConsumptionEntity with emission calculations
- Added API endpoints for CRUD operations

## Test plan
- [ ] Run `dotnet test` - all tests pass
- [ ] Test POST /api/v1/activities with water consumption data
- [ ] Verify emissions calculated correctly
- [ ] Check Swagger docs at http://localhost:7070/swagger

🤖 Generated with [Claude Code](https://claude.com/claude-code)
```

Proceed with the commit, push, and PR creation.
