# Claude AI Coding Agent Instructions for Witnes

## Quick Start

**For specialized tasks, use these agents:**
- `witnes-backend` - Backend .NET development (vertical slices, multi-tenancy, migrations)
- `witnes-frontend` - Frontend Astro/React development (useApiToast, API client, forms)
- `test-runner` - Run and fix tests (unit tests only, not integration)
- `code-simplifier` - Remove over-engineering after implementation

**Common workflow commands:**
- `/commit-push-pr` - Create commit, push, and open PR
- `/regenerate-api-client` - Update TypeScript client after backend changes
- `/verify-build` - Build and run unit tests

**See `.claude/README.md` for full setup documentation and best practices.**

---

## Project Overview
- **Backend**: .NET 10 API with PostgreSQL, Redis, RabbitMQ, MinIO
- **Frontend**: Astro 5 + React 18 + Tailwind CSS + shadcn/ui
- **Domain**: Carbon footprint tracking (CCF/PCF), emission calculations, supplier management

## Critical: Commit Convention (MANDATORY)
**ALL commits MUST follow this format** (validated by git hooks):
```
type: <type> - <description>

Valid types:
- type: feature  → Minor version bump (new features)
- type: fix      → Patch version bump (bug fixes)
- type: breaking → Major version bump (breaking changes)
- type: none     → No version bump (docs, formatting)
```

**Examples:**
```bash
type: feature - Add dark mode toggle to dashboard
type: fix - Resolve token refresh race condition
type: breaking - Remove deprecated /v1/activities endpoint
```

**Setup git hooks first:** `./scripts/setup-hooks.sh`

**CRITICAL: Before committing, ALWAYS run code formatter:**
```bash
sh scripts/format-code.sh
```
This formats all C# code to pass CI/CD build validation. Skipping this step will cause build failures.

## Architecture Patterns

### Multi-Tenancy (Row-Level Security)
Every database query is automatically filtered by `TenantId` and/or `BusinessId` via EF Core query filters. Middleware extracts these from JWT claims.

**Entity Base Classes:**
```csharp
// Tenant-scoped only
public class MyEntity : TenantAwareEntity { }

// Tenant + Business scoped
public class MyEntity : BusinessTenantAwareEntity { }
```

### Vertical Slice Architecture
Features are **self-contained** in `/code/api/Api/Product/<Feature>/`:
```
Product/Activities/
├── Entities/          # Domain entities
├── Services/          # Business logic interfaces + implementations
├── ActivitiesController.cs
├── Models/            # DTOs, requests, responses
├── Jobs/              # Background jobs
└── README.md          # Feature documentation
```

**When adding features:** Keep everything for that feature in ONE folder.

### Code-First Database Pattern
1. Modify entities in `/Entities/`
2. Run `dotnet ef migrations add MigrationName` from `/code/api/Api/`
3. Migrations apply automatically on startup via `Program.cs`
4. Seed data in `/Data/Seeders/`

## Development Workflows

### Setup Local Environment
```bash
# 1. Start infrastructure (PostgreSQL, Redis, RabbitMQ, MinIO, Seq)
docker-compose -f code/development-infra.yml up -d

# 2. Build and run API
cd code/api/Api
dotnet run  # API runs on http://localhost:7070

# 3. Build and run frontend
cd code/fe
npm install
npm run dev  # Frontend runs on http://localhost:4321
```

**Check logs:** http://localhost:5341 (Seq UI)

### API Client Generation (After Backend Changes)
**CRITICAL:** After modifying API controllers or models, regenerate the frontend client:
```bash
./scripts/generate-openapi.sh
```

This:
1. Builds the API
2. Generates OpenAPI spec
3. Uses Orval to generate TypeScript client at `/code/fe/src/generated/api.ts`

**Frontend MUST use auto-generated client** - never write manual API calls.

### Build & Test Everything
```bash
./scripts/build-and-run.sh
```
Runs: API build → unit tests → OpenAPI generation → integration tests

### Format Code
```bash
./scripts/format-code.sh  # Formats all C# code
```

## Backend Conventions

### API Controllers
```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]  // Requires authentication
[CustomExceptionFilter]  // Global error handling
public class MyController : ControllerBase
{
    // Use constructor injection
    private readonly IMyService _service;

    public MyController(IMyService service) => _service = service;

    [HttpGet]
    [OutputCache(PolicyName = OutputCachePolicyFiveMinutes)]
    public async Task<ActionResult<List<MyDto>>> GetAll()
    {
        var items = await _service.GetAllAsync();
        return Ok(items);
    }
}
```

**Key Points:**
- All controllers in `Product/` namespaces
- Use `[Authorize]` for protected endpoints
- Apply `[OutputCache]` for read-heavy endpoints
- Return `ActionResult<T>` for typed responses

### Service Registration Pattern
Add to `/Application/Extensions/StartupServiceExtentions.cs` OR `Startup.AutoDiscover()`:
```csharp
services.AddTransient<IMyService, MyService>();
```

**Lifetimes:**
- `AddTransient` - New instance per request (default for services)
- `AddScoped` - One instance per HTTP request (for context-dependent services)
- `AddSingleton` - Single instance app-wide (for stateless services)

### Database Read/Write Separation
```csharp
// For WRITES (inserts, updates, deletes) - with change tracking
private readonly ApplicationDbContext _context;

// For READS (queries, reports) - no tracking, faster
private readonly ApplicationDbContextRead _contextRead;

// Complex read queries - use Dapper
```

### Caching Strategy
```csharp
// Controller-level output caching (Redis-backed)
[OutputCache(PolicyName = OutputCachePolicyFiveMinutes)]

// Service-level distributed caching (FusionCache)
await _cache.GetOrSetAsync<T>(
    key: $"emissions:{activityId}",
    factory: async ct => await _repo.GetAsync(activityId, ct),
    options: new FusionCacheEntryOptions { Duration = TimeSpan.FromHours(1) }
);
```

**Available policies:** `OutputCachePolicyFiveMinutes`, `OutputCachePolicyOneHour`, `OutputCachePolicyTwoDays`

### Background Jobs (Coravel)
Jobs registered in `/Application/Extensions/SchedulerExtensions.cs`:
```csharp
scheduler.Schedule<MyJob>()
    .EveryMinute();  // or .Hourly(), .Daily(), .Cron("0 0 * * *")
```

**Existing jobs:**
- `DropZoneLlmJob` - Processes PDF uploads with LLM (every minute)
- `DropZoneOutboundJob` - Creates activities from processed files (every minute)
- `ResetMonthlyLimitsJob` - Resets usage limits (monthly at 00:01 UTC)
- `RequeuePendingEmissionsJob` - Retries failed emission calculations (every 15 min)

## Frontend Conventions

### File Structure
```
src/
├── pages/                # Astro routes (SSR)
│   ├── dashboard.astro   # Protected app (checks auth)
│   └── index.astro       # Public landing page
├── components/
│   ├── ui/               # shadcn/ui base components
│   ├── activities/       # Feature-specific components
│   └── forms/            # Reusable form components
├── contexts/             # React Context providers
│   ├── AuthContext.tsx   # Authentication state
│   └── FeatureContext.tsx # Feature flags
├── hooks/
│   └── useApiToast.ts    # API error handling (ALWAYS USE)
├── generated/
│   └── api.ts            # Auto-generated API client (DO NOT EDIT)
└── lib/
    └── axios-instance.ts # Axios with auto token refresh
```

### API Calls (MANDATORY PATTERN)
**ALWAYS use `useApiToast` hook** for consistent error handling:

```typescript
import { useApiToast } from "../hooks/useApiToast";
import { postV1Activities } from "../generated/api";

function MyComponent() {
  const { handleApiCall } = useApiToast();

  const handleSubmit = async (data: CreateActivityRequest) => {
    await handleApiCall({
      apiCall: () => postV1Activities(data),
      successMessage: "Activity created successfully",
      onSuccess: (response) => {
        navigate(`/dashboard/activities/${response.id}`);
      },
      onError: () => setSubmitting(false),
    });
  };
}
```

**Why:** Automatically extracts error messages from `ApplicationProblemDetailsModel`, shows toast notifications, handles success/error callbacks.

### Authentication Flow
```typescript
// 1. Login returns access + refresh tokens
const tokens = await postV1AccountLogin(credentials);
localStorage.setItem('auth_tokens', JSON.stringify(tokens));

// 2. Axios instance auto-adds Bearer token to requests
// 3. On 401, automatically refreshes tokens and retries
// 4. See: /code/fe/src/lib/axios-instance.ts
```

### Form Patterns (react-hook-form + Zod)
```typescript
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";

const schema = z.object({
  name: z.string().min(1, "Required"),
  email: z.string().email(),
});

type FormData = z.infer<typeof schema>;

function MyForm() {
  const { register, handleSubmit, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
  });

  const onSubmit = handleSubmit(async (data) => {
    // Use useApiToast here
  });
}
```

### Styling Convention
- **Tailwind CSS** for all styling (no CSS-in-JS, no CSS modules)
- Use **shadcn/ui components** from `/components/ui/` (Button, Input, Dialog, etc.)
- Dark mode via `dark:` prefix (system preference detection)

### Type Safety and API Integration
**CRITICAL: Always use types from the generated API client**

- **NEVER define enums or types in the frontend that already exist in the backend**
- Import enums from `/src/generated/api.ts` (e.g., `BusinessType`, `FocusArea`, `EmissionScope`)
- Use `Object.values(EnumName)` to iterate over enum values for dropdowns/lists
- This ensures frontend stays in sync with backend changes after regenerating the API client

**Example:**
```typescript
// ❌ WRONG - Defining enum values manually in frontend
const BUSINESS_TYPES = [
  { value: "bakery", label: "Bakery" },
  { value: "manufacturing", label: "Manufacturing" },
];

// ✅ CORRECT - Using enum from generated API
import { BusinessType } from "../generated/api";

const BUSINESS_TYPES = Object.values(BusinessType).map((type) => ({
  value: type,
  label: type.split('_').map(w => w.charAt(0).toUpperCase() + w.slice(1)).join(' ')
}));
```

## Domain Knowledge

### Carbon Accounting Concepts
**ActionCode** (Activity Types): Transport, Energy, Water, Waste, Materials, etc.
**ThingGroup**: Categories like Fuels, Electricity, Materials, Products
**EmissionScope**: Scope 1 (direct), Scope 2 (indirect energy), Scope 3 (supply chain)

**Key Files:**
- `/code/libs/Libs/Domain/ActionCode.cs` - All activity types
- `/code/libs/Libs/Domain/ThingGroup.cs` - Thing categories
- `/code/api/Api/Product/EmissionFactors/` - Emission calculation logic

### DropZone PDF Processing Pipeline
1. User uploads PDF → MinIO storage (`files` bucket)
2. `DropZoneFileEntity` created (status: `Pending`)
3. `DropZoneLlmJob` extracts data via LLM (Anthropic Claude or local Ollama)
4. LLM generates structured JSON (action, thing, quantity, date, location)
5. `DropZoneOutboundJob` creates `Activity` from JSON
6. `ActivityEmissionsCalculationService` calculates emissions via RabbitMQ

**Configuration:**
- DocLink service: `appsettings.Development.json` → `DocLinkSettings`
- LLM settings: `appsettings.Development.json` → `LlmSettings`

## Testing

### Unit Tests (xUnit)
Located in `/code/api/Api.Tests/`. Follow existing patterns:
```csharp
public class MyServiceTests
{
    [Fact]
    public async Task MyMethod_ShouldReturnExpectedResult()
    {
        // Arrange
        var service = new MyService();

        // Act
        var result = await service.MyMethod();

        // Assert
        Assert.NotNull(result);
    }
}
```

### Integration Tests
Uses `code/integration-infra.yml` (isolated Docker environment).
Run via: `./scripts/build-and-run.sh`

## Configuration & Secrets

### Development Settings
- API: `/code/api/Api/appsettings.Development.json`
- Frontend: `/code/fe/.env` (PUBLIC_ prefix for client-side vars)

**Connection Details (Development):**
```
PostgreSQL: localhost:5433 / witnes / greenactions_dev_password
Redis: localhost:6378
RabbitMQ: localhost:15672 (UI), 5672 (AMQP)
MinIO: localhost:9001 (console), 9000 (API)
Seq: localhost:5341
```

### Production (Never commit secrets!)
- Use environment variables
- Secrets managed via Docker secrets or CI/CD secrets
- See `code/production-full.yml` for structure

## Deployment

### API Deployment
```bash
# Deploy to VPS (runs /root/deploy-api.sh on remote server)
# Triggered by GitHub Actions on push to main if /code/api/ changed
```

### Frontend Deployment
```bash
# Cloudflare Pages webhook triggered on push to main if /code/fe/ changed
# Builds Astro site with @astrojs/cloudflare adapter
```

**Workflow:** See `.github/workflows/deploy.yml`

## Common Pitfalls & Solutions

### Problem: Frontend API calls fail with 404
**Solution:** Regenerate API client: `./scripts/generate-openapi.sh`

### Problem: Database entity changes not applied
**Solution:** Delete database volume and restart:
```bash
docker-compose -f code/development-infra.yml down -v
docker-compose -f code/development-infra.yml up -d
```

### Problem: Git commit rejected
**Solution:** Follow commit format: `type: <type> - <description>`

### Problem: Token refresh loop on frontend
**Solution:** Check JWT expiration settings in `appsettings.Development.json` → `TokenProviderSettings`

### Problem: Background jobs not running
**Solution:** Check `SchedulerExtensions.cs` registration and ensure `serviceProvider.ScheduleJobs()` is called in `Startup.Configure()`

## Key Files for Reference

### Entry Points
- API: `/code/api/Api/Program.cs` (startup, migrations, seeding)
- API: `/code/api/Api/Startup.cs` (service registration, middleware)
- Frontend: `/code/fe/astro.config.mjs` (build config)

### Core Services
- `/code/api/Api/Data/ApplicationDbContext.cs` - All EF entities
- `/code/api/Api/Application/Extensions/StartupServiceExtentions.cs` - Service setup
- `/code/fe/src/lib/axios-instance.ts` - HTTP client with auth

### Workflows
- `/scripts/build-and-run.sh` - Full build & test cycle
- `/scripts/generate-openapi.sh` - API client generation
- `/.github/workflows/` - CI/CD pipelines

## Quick Start Checklist for New Features

- [ ] Run `./scripts/setup-hooks.sh` (if not done)
- [ ] Start infrastructure: `docker-compose -f code/development-infra.yml up -d`
- [ ] **Backend:** Create entities in `Product/<Feature>/Entities/`
- [ ] **Backend:** Add migration: `dotnet ef migrations add <Name>` from `/code/api/Api/`
- [ ] **Backend:** Create service interface + implementation in `Product/<Feature>/Services/`
- [ ] **Backend:** Register service in `Startup.AutoDiscover()`
- [ ] **Backend:** Create controller in `Product/<Feature>/<Feature>Controller.cs`
- [ ] **Regenerate client:** `./scripts/generate-openapi.sh`
- [ ] **Frontend:** Use generated API functions from `/src/generated/api.ts`
- [ ] **Frontend:** Create components in `/src/components/<feature>/`
- [ ] **Frontend:** Use `useApiToast` for all API calls
- [ ] **Test:** Add xUnit tests to `/code/api/Api.Tests/`
- [ ] **Commit:** `type: feature - Add <description>`

## Additional Resources

- **Carbon Accounting Concepts:** `/documentation/`
- **AI Processing Pipeline:** `/AI.md` (if exists)
- **Sustainability Reports:** `/documentation/SustainabilityReport/`
- **Contributing Guide:** `/CONTRIBUTING.md`

---

**Remember:** This is a production codebase with real users. Always test locally, follow commit conventions, and regenerate the API client after backend changes.
