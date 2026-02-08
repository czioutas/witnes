---
name: verify-build
description: Run full build and test suite to verify changes
---

You need to verify that all changes build correctly and pass tests.

**Full verification includes:**

1. **Backend Build**
   ```bash
   cd code/api/Api
   dotnet build
   ```

2. **Unit Tests (ALWAYS run these)**
   ```bash
   cd code/api/Api.Tests
   dotnet test --filter "FullyQualifiedName~UnitTests"
   ```

3. **Frontend TypeCheck & Lint**
   ```bash
   cd code/fe
   npm run typecheck
   npm run lint
   ```

4. **Integration Tests (ONLY on demand or leave for CI/CD)**
   ```bash
   # Don't run these by default - they require full infrastructure
   cd code/api/Api.Tests
   dotnet test --filter "FullyQualifiedName~IntegrationTests"
   # OR run full script with infra setup
   ./scripts/build-and-run.sh
   ```

**What to check:**
- ✅ API builds without errors
- ✅ All **unit tests** pass (NOT integration tests)
- ✅ No TypeScript errors
- ✅ No linting errors

**If failures occur:**
1. Read the error messages carefully
2. Fix the issues
3. Re-run the verification
4. Don't proceed to commit until everything passes

**Expected output:**
- Backend Build: "Build succeeded"
- Unit Tests: "Test Run Successful" with all tests passed
- Frontend TypeCheck: "No errors found"
- Frontend Lint: Success or warnings (warnings are OK)

**Note:** Integration tests are NOT run during normal verification. They require full Docker infrastructure and are left for CI/CD or explicit on-demand testing.

Execute the verification steps now and report results.
