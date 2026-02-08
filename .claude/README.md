# Witnes Claude Code Setup

This document describes how the Witnes project is configured for optimal Claude Code usage, following best practices from the Claude Code team.

## File Structure

```
.claude/
├── CLAUDE.md                    # Main project instructions (read by all sessions)
├── settings.json                # Shared team settings (checked into git)
├── settings.local.json          # Personal overrides (gitignored)
├── agents/                      # Specialized agents for specific tasks
│   ├── backend.md              # Backend .NET development
│   ├── frontend.md             # Frontend Astro/React development
│   ├── test-runner.md          # Test execution and debugging
│   └── code-simplifier.md      # Post-implementation simplification
└── commands/                    # Slash commands for inner-loop workflows
    ├── commit-push-pr.md       # /commit-push-pr - Full PR workflow
    ├── regenerate-api-client.md # /regenerate-api-client - Update TypeScript client
    └── verify-build.md         # /verify-build - Run build and tests
```

## Best Practices Implemented

### 1. Shared CLAUDE.md (Checked into Git)

**Location:** `.claude/CLAUDE.md`

**Purpose:** Central source of truth for project conventions, patterns, and domain knowledge.

**Maintenance:** The whole team contributes when they see Claude make mistakes or need to document new patterns.

**Key sections:**
- Commit message format (GitVersion requirements)
- Multi-tenancy patterns (row-level security)
- Vertical slice architecture
- API client generation workflow
- Carbon accounting domain concepts

### 2. Plan Mode First

**Workflow:** For any non-trivial PR:
1. Press Shift+Tab twice to enter plan mode
2. Discuss the approach with Claude until the plan is solid
3. Switch to auto-accept edits mode
4. Claude usually one-shots it with a good plan

**Benefits:**
- Ensures alignment before writing code
- Catches architectural issues early
- Reduces rework and wasted effort

### 3. Specialized Agents

**Backend Agent** (`witnes-backend`):
- Vertical slice architecture enforcement
- Multi-tenancy security patterns
- Database migration workflows
- Emission calculation logic
- Background job patterns

**Frontend Agent** (`witnes-frontend`):
- Mandatory `useApiToast` hook usage
- Auto-generated API client patterns
- React Hook Form + Zod validation
- shadcn/ui component conventions
- Astro islands architecture

**Test Runner Agent** (`test-runner`):
- Runs unit tests (NOT integration tests by default)
- Diagnoses test failures
- Fixes broken tests
- Verifies builds before commits

**Code Simplifier Agent** (`code-simplifier`):
- Removes over-engineering after implementation
- Applies YAGNI principle
- Inlines single-use abstractions
- Deletes premature optimizations

### 4. Slash Commands for Inner Loops

**`/commit-push-pr`:**
- Creates properly formatted commit (GitVersion)
- Pushes to remote
- Opens PR with summary and test plan
- Uses inline bash to pre-compute git state (fast!)

**`/regenerate-api-client`:**
- Builds backend API
- Generates OpenAPI spec
- Uses Orval to create TypeScript client
- Critical after backend changes

**`/verify-build`:**
- Builds API
- Runs unit tests (not integration!)
- Checks TypeScript types
- Fast verification before commits

### 5. PostToolUse Hooks for Formatting

**Auto-formatting after Write/Edit:**
- C# files: `dotnet format`
- TypeScript/JavaScript: `prettier`

**Benefits:**
- No formatting errors in CI
- Consistent code style
- Claude doesn't need to worry about formatting

### 6. Pre-Allowed Permissions

**Location:** `.claude/settings.json` (team-shared)

**Common commands pre-allowed:**
- Build: `dotnet build`, `npm run build`
- Test: `dotnet test`, `npm run typecheck`
- Git: `git status`, `git diff`, `git add`, `git commit`, `git push`
- Tools: `tree`, `find`, `grep`, `ls`, `cat`
- Scripts: `./scripts/generate-openapi.sh`, `./scripts/format-code.sh`

**Benefits:**
- No permission prompts for safe commands
- Faster Claude execution
- Better for long-running tasks

### 7. Verification Loops

**Give Claude ways to verify its work:**

**Backend verification:**
```bash
cd code/api/Api && dotnet build
cd code/api/Api.Tests && dotnet test --filter "FullyQualifiedName~UnitTests"
```

**Frontend verification:**
```bash
cd code/fe
npm run typecheck
npm run lint
```

**Integration verification (on-demand only):**
```bash
./scripts/build-and-run.sh
```

**Benefit:** Claude can iterate until tests pass, resulting in 2-3x better quality.

## Development Workflows

### Starting a New Feature

1. **Enter plan mode:** Shift+Tab twice
2. **Discuss approach:** Refine until plan is solid
3. **Implement:** Switch to auto-accept edits
4. **Run tests:** Use `/verify-build` or test-runner agent
5. **Simplify:** Use code-simplifier agent if needed
6. **Commit & PR:** Use `/commit-push-pr`

### Fixing a Bug

1. **Reproduce:** Write a failing test first
2. **Fix:** Update code to make test pass
3. **Verify:** Run `/verify-build`
4. **Commit:** Use `/commit-push-pr` with `type: fix`

### Adding Backend API Endpoint

1. **Plan:** Design in vertical slice pattern
2. **Implement:** Entity → Service → Controller → DTOs
3. **Regenerate client:** Run `/regenerate-api-client`
4. **Test:** Use test-runner agent
5. **Commit:** Use `/commit-push-pr` with `type: feature`

### Adding Frontend Component

1. **Use witnes-frontend agent:** Ensures patterns followed
2. **Verify API client:** Check if `/regenerate-api-client` needed
3. **Implement:** Component with `useApiToast` hook
4. **Type check:** `npm run typecheck`
5. **Commit:** Use `/commit-push-pr`

## Team Conventions

### Updating CLAUDE.md

**When to update:**
- Claude makes same mistake repeatedly
- New architectural pattern introduced
- Domain knowledge needs clarification
- Common pitfall discovered

**How to update:**
- Edit `.claude/CLAUDE.md` directly
- Add specific examples from codebase
- Keep concise (not a novel!)
- Commit with `type: none`

### Adding New Agents

**When to create:**
- Workflow repeated for most PRs
- Complex multi-step process
- Specialized domain knowledge needed

**Pattern:**
```markdown
---
name: my-agent
description: When to use this agent
model: sonnet
color: blue
---

Clear instructions with examples...
```

### Adding New Commands

**When to create:**
- Inner-loop workflow done many times daily
- Steps can be automated
- Faster than manual prompting

**Pattern:**
```markdown
---
name: my-command
description: What this command does
---

\`\`\`bash
# Pre-compute info to avoid back-and-forth
STATE=$(some-command)
\`\`\`

Instructions...
```

## Tips from Claude Code Creator

### Run Multiple Claudes in Parallel

**Terminal:** Run 5 Claude sessions in numbered tabs
**Web:** Run 5-10 on claude.ai/code
**Mobile:** Start sessions from iPhone app

**Benefits:** Massive productivity boost, especially for independent tasks.

### Use Opus 4.5 with Thinking

Even though it's slower, Opus 4.5 requires less steering and has better tool use, making it **faster overall** for complex tasks.

### Long-Running Tasks

For tasks that take hours:
- Use `--permission-mode=dontAsk` in sandboxed environments
- Let Claude run agents in background
- Use Stop hooks to verify work when done
- Come back to completed work

### Verification is Critical

**Most important tip:** Give Claude a feedback loop to verify its work.

For Witnes:
- Unit tests verify backend logic
- TypeScript typecheck verifies frontend
- Integration tests verify full system (on-demand)

## Quick Reference

### Most Common Commands

```bash
# Verify changes before commit
/verify-build

# Commit, push, open PR
/commit-push-pr

# After backend changes
/regenerate-api-client
```

### Most Common Agents

```
"Use witnes-backend agent to add emission calculation feature"
"Use witnes-frontend agent to create dashboard chart"
"Use test-runner agent to fix failing tests"
"Use code-simplifier agent to remove over-engineering"
```

### Keyboard Shortcuts

- **Shift+Tab (twice):** Enter plan mode
- **Ctrl+C:** Interrupt Claude
- **Shift+Enter:** Add newline in prompt

---

**Remember:** There's no one correct way to use Claude Code. Experiment, customize, and find what works best for your workflow!
