# Frontend Development Guide

> **📝 Note**: When you introduce new patterns, update both this file AND `.agent.md`

## Quick Start

```bash
npm install
npm run dev                        # Dev server at localhost:4321
npm run typecheck                  # Type check
npm run build                      # Production build

# When backend API changes:
../../scripts/generate-openapi.sh  # Regenerates src/generated/api.ts
```

> **⚠️ Important**: `src/generated/api.ts` is auto-generated. Never edit it manually.

---

## Tech Stack

- **Framework**: Astro + React
- **UI**: shadcn/ui (Radix + Tailwind)
- **Forms**: react-hook-form + zod
- **API**: Orval (auto-generated from OpenAPI spec)
- **Toasts**: Sonner

---

## File Structure

```
code/fe/
├── .agent.md              # Auto-read by AI agents
├── INSTRUCTIONS.md        # This file
├── src/
│   ├── components/
│   │   ├── pages/        # Page components
│   │   ├── ui/           # shadcn/ui components
│   │   └── forms/        # Form components
│   ├── hooks/
│   │   └── useApiToast.ts # API error handling
│   ├── contexts/         # Auth, Feature contexts
│   ├── generated/        # ⚠️ AUTO-GENERATED - DO NOT EDIT
│   └── lib/
│       └── axios-instance.ts # Axios config
```

---

## API Calls - Use `useApiToast`

```typescript
import { useApiToast } from "./hooks/useApiToast";

const { handleApiCall } = useApiToast();

await handleApiCall({
  apiCall: () => api.postResource(data),
  successMessage: "Created successfully",
  onSuccess: () => navigate("/success"),
  onError: () => setSubmitting(false),
});
```

**Options**:
- `showSuccessToast: false` - Hide success toast
- `showErrorToast: false` - Hide error toast
- `onSuccess: (data) => {}` - Handle response
- `onError: () => {}` - Handle error

**⚠️ Important**: Never add `handleApiCall` to `useEffect` dependencies - it causes infinite loops!

```typescript
// ❌ WRONG - Infinite loop!
useEffect(() => {
  fetchData();
}, [productId, handleApiCall]);

// ✅ CORRECT
useEffect(() => {
  fetchData();
}, [productId]);
```

---

## Form Pages Pattern

See [src/components/pages/LocationFormPage.tsx](src/components/pages/LocationFormPage.tsx)

```typescript
export function ResourceFormPage({ resourceId }: Props) {
  const [submitting, setSubmitting] = useState(false);
  const { handleApiCall } = useApiToast();

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: defaultValues,
  });

  const onSubmit = async (values: FormValues) => {
    setSubmitting(true);
    await handleApiCall({
      apiCall: () => api.createResource(values),
      successMessage: "Created",
      onSuccess: () => navigate("/resources"),
      onError: () => setSubmitting(false),
    });
  };

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)}>
        <FormField ... />
        <Button disabled={submitting}>Submit</Button>
      </form>
    </Form>
  );
}
```

---

## Authentication

```typescript
useRequireAuth();                                    // Must be logged in
useRequireRole(AccountRoles.AdminUserRole);          // Must be admin

const auth = useAuth();
console.log(auth.user.firstName);
```

---

## Feature Flags

```typescript
import { FeatureKey } from "./generated/api";

const { isFeatureEnabled } = useFeatures();
if (isFeatureEnabled(FeatureKey.dropzone)) {
  return <NewFeature />;
}
```

---

## Project Architecture

### Component Structure
- **Astro Pages**: Use `.astro` files for pages
- **React Components**: Use `.tsx` files for interactive components
- **Client Hydration**: Use `client:load` directive for React in Astro

### Styling
- **Tailwind CSS**: Primary styling framework
- **shadcn/ui**: Component library
- **Responsive**: Mobile-first (dashboard is desktop-only)

---

## Common Tasks

### Adding New API Endpoints
1. Update backend OpenAPI spec
2. Run `../../scripts/generate-openapi.sh`
3. Use generated types from `src/generated/api.ts`
4. Use `useApiToast` hook for API calls

### Creating Data Tables with Pagination
**Follow the 3-file pattern** (see `src/components/activities/` for reference):

1. **Create `columns.tsx`**: Define column definitions with types
2. **Copy `data-table.tsx`**: Reuse from `activities/data-table.tsx`
3. **Update page component**: Add pagination state and API calls

**Example**:
```typescript
// 1. columns.tsx
export const columns: ColumnDef<Item>[] = [
  { accessorKey: "name", header: "Name" },
  // ... more columns
];

// 2. Copy data-table.tsx from activities folder

// 3. Page component
const [data, setData] = useState<Item[]>([]);
const [pageNumber, setPageNumber] = useState(1);
const [totalCount, setTotalCount] = useState(0);

useEffect(() => {
  const api = getWitnesServerAPI();
  const response = await api.getV1Items({
    PageNumber: pageNumber,  // Note: Capitalized
    PageSize: 20,
  });
  setData(response.data.data ?? []);
  setTotalCount(response.data.total_count ?? 0);
}, [pageNumber]);

return (
  <DataTable
    columns={columns}
    data={data}
    pageNumber={pageNumber}
    pageSize={20}
    totalCount={totalCount}
    onPageChange={setPageNumber}
  />
);
```

### Creating New Pages
1. Create `.astro` file in `src/pages/`
2. Use React components with `client:load` as needed
3. Follow authentication patterns for protected routes

### Adding New Components
1. Create `.tsx` file in appropriate `src/components/` subdirectory
2. Use TypeScript interfaces for props
3. Follow shadcn/ui patterns for styling
4. Use `useApiToast` for all API calls

---

See [README.md](README.md) for architecture overview and project structure.
