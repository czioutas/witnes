# Witnes Energy Dashboard Frontend

A modern dashboard built with Astro + React + shadcn/ui for monitoring and analyzing data in real-time.

## 🚀 Quick Start

```bash
npm install
npm run dev                        # Dev server at localhost:4321
npm run typecheck                  # Type check
npm run build                      # Production build
```

## 🏗️ Architecture

- **Framework**: Astro with React integration
- **Styling**: Tailwind CSS + shadcn/ui components
- **Authentication**: JWT-based with refresh tokens
- **State Management**: React Context for auth state
- **API Client**: Axios with interceptors (auto-generated from OpenAPI)
- **Forms**: react-hook-form + zod validation
- **Toasts**: Sonner

## 📁 Project Structure

```
code/fe/
├── .agent.md              # Auto-read by AI agents
├── INSTRUCTIONS.md        # Developer guide
├── src/
│   ├── components/
│   │   ├── pages/        # Page components
│   │   ├── ui/           # shadcn/ui base components
│   │   └── forms/        # Form components
│   ├── hooks/
│   │   └── useApiToast.ts # API error handling hook
│   ├── contexts/
│   │   ├── AuthContext.tsx
│   │   └── FeatureContext.tsx
│   ├── lib/
│   │   └── axios-instance.ts # Axios config with auth
│   ├── generated/
│   │   └── api.ts       # ⚠️ AUTO-GENERATED (DO NOT EDIT)
│   └── pages/
│       ├── index.astro          # Landing page
│       ├── docs.astro           # Documentation
│       ├── dashboard.astro      # Main dashboard (protected)
│       └── authenticate/        # Auth pages
```

## 🔐 Authentication System

- **JWT Tokens**: Access and refresh token handling
- **Route Protection**: Dashboard requires authentication
- **Persistent Sessions**: Tokens stored in localStorage
- **Auto-refresh**: Automatic token renewal on expiry
- **Protected Routes**: Use `useRequireAuth()` and `useRequireRole()`

## 🧞 Commands

| Command | Action |
|:--------|:-------|
| `npm install` | Install dependencies |
| `npm run dev` | Start dev server at `localhost:4321` |
| `npm run build` | Build production site to `./dist/` |
| `npm run preview` | Preview production build locally |
| `npm run typecheck` | Run TypeScript type checking |
| `../../scripts/generate-openapi.sh` | Regenerate API client from backend OpenAPI spec |

## 📚 Documentation

See [INSTRUCTIONS.md](INSTRUCTIONS.md) for:
- API patterns (`useApiToast` hook)
- Form page patterns
- Authentication patterns
- Feature flags
- Common development tasks

## 🔧 Environment Variables

```env
PUBLIC_API_BASE_URL=http://localhost:5000/api
```

Set `PUBLIC_API_BASE_URL` to your backend API endpoint. If not set, defaults to `http://localhost:5000/api`.

## 🚨 Important Notes

### Auto-Generated API Client

`src/generated/api.ts` is **auto-generated** from the backend OpenAPI spec. Never edit it manually.

To regenerate after backend changes:
```bash
../../scripts/generate-openapi.sh
```

### API Error Handling

Always use the `useApiToast` hook for API calls - never manually extract errors:

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

See [INSTRUCTIONS.md](INSTRUCTIONS.md) for detailed patterns.

## 📄 Pages

### Landing Page (`/`)
- Hero section with main CTA
- Problem/solution sections
- TSO coverage table
- Pricing tiers
- Support and footer

### Documentation Page (`/docs`)
- API documentation
- Getting started guide
- Authentication flows
- Data retrieval patterns
- Methodology

### Dashboard (`/dashboard`)
- Protected route (requires authentication)
- Main application interface
- Desktop-only design

### Legal Pages
- Terms of Service (`/terms`)
- Privacy Policy (`/privacy`)

## 👀 Learn More

- [Astro Documentation](https://docs.astro.build)
- [shadcn/ui Documentation](https://ui.shadcn.com)
- [INSTRUCTIONS.md](INSTRUCTIONS.md) - Development patterns and guides
