# Witnes Dashboard Redesign - TODO

## Overview
Complete redesign of the Witnes dashboard for performance monitoring and analytics.

**Frontend Location:** `code/fe`

**Important:**
- Don't use `any`, use types from `api.ts`
- Regenerate API client: `sh scripts/generate-openapi.sh`

---

## Phase 1: Project Keys / Usage

### Backend
- [ ] Create `ProjectKeysController` in backend
  - [ ] GET endpoint to retrieve project key (single key allowed)
  - [ ] GET endpoint to retrieve total page loads consumed
  - [ ] GET endpoint to retrieve package info/limits
- [ ] Test API endpoints

### Frontend
- [ ] Create `pages/dashboard/usage.astro` (or usage page)
- [ ] Create `components/usage/ProjectKeyDisplay.tsx`
  - [ ] Display project key with copy-to-clipboard functionality
  - [ ] Show total page loads consumed
  - [ ] Show package info (plan name, limits, renewal date)
- [ ] Style usage page with Tailwind CSS
- [ ] Integrate with API using `useApiToast` hook
- [ ] Test usage page

---

## Phase 2: Users View

### Backend
- [ ] Create `UsersController` in backend
  - [ ] GET endpoint to query gold table for users list
  - [ ] Implement pagination using `code/libs/Libs/Pagination/PaginationRequest.cs`
  - [ ] Add user ID search filter
  - [ ] Add time range filter
- [ ] Test API endpoints

### Frontend
- [ ] Create `pages/dashboard/users.astro` (or users page)
- [ ] Create `components/users/UsersTable.tsx`
  - [ ] Table view with pagination
  - [ ] User ID column, browsers, OS, last seen date
- [ ] Create `components/users/UserSearch.tsx`
  - [ ] Top left search input for user ID
- [ ] Create `components/filters/TimeRangeFilter.tsx` (reusable)
  - [ ] Top right time filter
  - [ ] Preset ranges (Last 24h, Last 7d, Last 30d, Custom)
  - [ ] "Around" option: user picks a time, automatically creates ±15 min range (e.g., "around 11:00" = 10:45-11:15)
- [ ] Implement pagination controls
- [ ] Integrate with API using `useApiToast` hook
- [ ] Test users page with various filters

---

## Phase 3: User Detail View

### Backend
- [ ] Create `UserDetailController` in backend (or add to UsersController)
  - [ ] GET endpoint to query gold table for specific user's page loads
  - [ ] Implement pagination
  - [ ] Add time filter support
- [ ] Test API endpoints

### Frontend
- [ ] Create `pages/dashboard/users/[userId].astro` (or user detail page)
- [ ] Create `components/users/UserInfoHeader.tsx`
  - [ ] Display user ID
  - [ ] Display browser(s) used
  - [ ] Display OS(s) used
- [ ] Create `components/users/UserPageLoadsTable.tsx`
  - [ ] Table showing page loads from gold table
  - [ ] Implement pagination
  - [ ] Clickable rows to navigate to page load detail
- [ ] Reuse `TimeRangeFilter.tsx` component
- [ ] Integrate with API using `useApiToast` hook
- [ ] Test user detail page

---

## Phase 4: Page Load Detail View

### Backend
- [ ] Create `PageLoadController` in backend
  - [ ] GET endpoint to query silver table for page load data
  - [ ] Include network timing data for waterfall view
  - [ ] Include jank/performance metrics
- [ ] Test API endpoints

### Frontend
- [ ] Create `pages/dashboard/pageloads/[pageLoadId].astro` (or page load detail page)
- [ ] Create `components/pageloads/NetworkWaterfall.tsx`
  - [ ] Waterfall chart showing network requests
  - [ ] Timeline visualization
  - [ ] Request details (URL, duration, size, type)
- [ ] Create `components/pageloads/JankReports.tsx`
  - [ ] Display jank metrics (long tasks, layout shifts, etc.)
  - [ ] Performance score visualization
- [ ] Style page load detail page
- [ ] Integrate with API using `useApiToast` hook
- [ ] Test page load detail page

---

## Phase 5: Home Page

### Backend
- [ ] Create `HomeController` in backend (or add to existing)
  - [ ] GET endpoint to check if project key exists
  - [ ] GET endpoint to check if data exists (past 1 day)
  - [ ] GET endpoint to get recent users (top 4 by date)
- [ ] Test API endpoints

### Frontend
- [ ] Create `pages/dashboard/index.astro` (or home page)
- [ ] Create `components/home/NoProjectKeyState.tsx`
  - [ ] First load: Prompt to add project key
  - [ ] Link to usage/project keys page
- [ ] Create `components/home/NoDataState.tsx`
  - [ ] Second load: Prompt to add integration
  - [ ] Link to integration documentation
  - [ ] Instructions/code snippet
- [ ] Create `components/home/ActiveState.tsx`
  - [ ] Large search component for user ID
  - [ ] Navigate to user detail on search
- [ ] Create `components/home/RecentUsers.tsx`
  - [ ] Display top 4 recent users by date
  - [ ] Clickable to navigate to user detail
- [ ] Create `components/home/DataIngestionStatus.tsx`
  - [ ] Show "data ingestion working" if data exists in past 1 day
  - [ ] Visual indicator (green checkmark, etc.)
- [ ] Implement conditional rendering based on state
- [ ] Integrate with API using `useApiToast` hook
- [ ] Test all three home page states

---

## Phase 6: Navigation Structure

### Frontend
- [ ] Update sidenav in `components/navigation/Sidenav.tsx` (or create if doesn't exist)
  - [ ] Home
  - [ ] Users
  - [ ] Organizations
  - [ ] Analytics
- [ ] Create/update bottom nav
  - [ ] Team (rename from "Users" if it exists)
  - [ ] Usage
  - [ ] Organization Settings
- [ ] Test navigation between all pages

---

## Phase 7: Cleanup - Remove Old Pages

### Frontend
- [ ] Remove everything under Reporting section
- [ ] Remove everything under Ledger section
- [ ] Remove everything under Catalog section
- [ ] Remove Accounting section
- [ ] Update navigation to remove deleted pages
- [ ] Remove unused components related to deleted pages
- [ ] Clean up unused routes

### Backend (if applicable)
- [ ] Remove unused controllers for deleted features
- [ ] Keep endpoints if still needed for other purposes

---

## Phase 8: Polish & Testing

### Frontend
- [ ] Ensure all components use types from `api.ts` (no `any` types)
- [ ] Verify responsive design on mobile/tablet
- [ ] Add loading states for all API calls
- [ ] Add error states for failed API calls
- [ ] Ensure consistent styling across all pages
- [ ] Add proper TypeScript types for all components
- [ ] Format code: `sh scripts/format-code.sh`

### Testing
- [ ] Test all user flows end-to-end
- [ ] Test pagination on all tables
- [ ] Test time filters
- [ ] Test search functionality
- [ ] Test navigation between pages
- [ ] Test error handling

### Documentation
- [ ] Update README if needed
- [ ] Document new API endpoints
- [ ] Update integration documentation

---

## Notes

- Follow vertical slice architecture (keep features self-contained)
- Use `useApiToast` hook for all API calls
- Import types from `generated/api.ts` (regenerate after backend changes)
- Follow commit convention: `type: feature - <description>`
- Make `TimeRangeFilter.tsx` reusable (not just for tables)
- Time filter "around" feature creates ±15 minute window (10:45-11:15 for "around 11:00")

---

## Progress Tracker

- [ ] Phase 1: Project Keys / Usage
- [ ] Phase 2: Users View
- [ ] Phase 3: User Detail View
- [ ] Phase 4: Page Load Detail View
- [ ] Phase 5: Home Page
- [ ] Phase 6: Navigation Structure
- [ ] Phase 7: Cleanup
- [ ] Phase 8: Polish & Testing
