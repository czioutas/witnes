import {
  AuthProvider,
  useRequireAuth,
  useRequireRole,
  hasRole,
} from "../contexts/AuthContext";
import { FeatureProvider } from "../contexts/FeatureContext";
import { AccountRoles } from "../generated/api";
import { AppSidebar } from "./app-sidebar";
import { SiteHeader, type BreadcrumbItemType } from "./site-header";
import { Home } from "./dashboard/Home";
import ComingSoonPage from "./ComingSoonPage";
import UserProfilePage from "./pages/UserProfilePage";
import { OrganizationSettingsPage } from "./pages/OrganizationSettingsPage";
import NotFoundPage from "./NotFoundPage";
import { UsersPage } from "./pages/UsersPage";
import { UsagePage } from "./pages/UsagePage";
import { CustomersPage } from "./pages/CustomersPage";
import UserDetailPage from "./pages/UserDetailPage";
import PageLoadDetailPage from "./pages/PageLoadDetailPage";
import { Toaster } from "sonner";
import { SidebarInset, SidebarProvider } from "@/components/ui/sidebar";

// Map page identifiers to their components
const pageComponents = {
  home: Home,
  "user-profile": UserProfilePage,
  "organization-settings": OrganizationSettingsPage,
  analytics: ComingSoonPage,
  organizations: ComingSoonPage,
  reports: ComingSoonPage,
  insights: ComingSoonPage,
  users: UsersPage,
  team: UsersPage,
  customers: CustomersPage,
  "customer-detail": UserDetailPage,
  "page-load-detail": PageLoadDetailPage,
  usage: UsagePage,
  "api-keys": ComingSoonPage,
  "cross-border-flows": ComingSoonPage,
  settings: ComingSoonPage,
};

interface DashboardPageProps {
  page: keyof typeof pageComponents;
  title: string;
  description: string;
  breadcrumbs?: BreadcrumbItemType[];
  actionCode?: string;
  thingGroup?: string;
  activityId?: number;
  productId?: string;
  materialId?: string;
  dropzoneFileId?: string;
  locationId?: string;
  supplierId?: string;
  reportId?: string;
  userId?: string;
  pageLoadId?: string;
}

function AuthenticatedDashboard({
  page,
  title,
  description,
  breadcrumbs,
  ...rest
}: DashboardPageProps) {
  const auth = useRequireAuth();

  // Pages that require admin role
  const adminOnlyPages = ["users", "team", "accounting", "organization-settings"];
  const isAdminOnlyPage = adminOnlyPages.includes(page);

  // Use role-based auth for admin-only pages
  if (isAdminOnlyPage) {
    useRequireRole(AccountRoles.AdminUserRole);
  }

  // While loading or not authenticated, render nothing (useRequireAuth handles redirect)
  if (auth.isLoading || !auth.isAuthenticated) {
    return null;
  }

  // Check if user has admin role for admin-only pages
  if (isAdminOnlyPage && !hasRole(auth.user, AccountRoles.AdminUserRole)) {
    return null; // useRequireRole will handle redirect
  }

  const handleLogout = async () => {
    await auth.logout();
    window.location.href = "/authenticate/login";
  };

  const sidebarUser = auth.user
    ? {
        name:
          `${auth.user.firstName || ""} ${auth.user.lastName || ""}`.trim() ||
          "User",
        email: auth.user.email || "",
        initials:
          `${auth.user.firstName?.charAt(0) || ""}${auth.user.lastName?.charAt(0) || ""}`.toUpperCase(),
        roles: auth.user.roles || [],
      }
    : undefined;

  // Render page content based on page type
  const renderPageContent = () => {
    // Handle pages that need specific props
    switch (page) {
      case "home":
        return <Home />;
      case "user-profile":
        return <UserProfilePage />;
      case "organization-settings":
        return <OrganizationSettingsPage />;
      case "analytics":
      case "organizations":
      case "reports":
      case "insights":
      case "api-keys":
      case "settings":
        return <ComingSoonPage />;
      case "users":
      case "team":
        return <UsersPage />;
      case "customers":
        return <CustomersPage />;
      case "customer-detail":
        return <UserDetailPage userId={rest.userId!} />;
      case "page-load-detail":
        return <PageLoadDetailPage pageLoadId={rest.pageLoadId!} />;
      case "usage":
        return <UsagePage />;
      default:
        return <NotFoundPage />;
    }
  };

  return (
    <SidebarProvider>
      <Toaster position="top-right" richColors />
      <AppSidebar user={sidebarUser} onLogout={handleLogout} />
      <SidebarInset className="w-full">
        <SiteHeader title={title} breadcrumbs={breadcrumbs} />
        <div className="flex flex-1 flex-col gap-8 p-8 pt-10 text-[17px]">
          {renderPageContent()}
        </div>
      </SidebarInset>
    </SidebarProvider>
  );
}

export default function DashboardPage(props: DashboardPageProps) {
  return (
    <AuthProvider>
      <FeatureProvider>
        <AuthenticatedDashboard {...props} />
      </FeatureProvider>
    </AuthProvider>
  );
}
