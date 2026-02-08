"use client";

import * as React from "react";
import {
  BarChart3,
  List,
  Settings,
  ShoppingBag,
  Users,
  Upload,
  FileText,
  Package2,
  Home,
} from "lucide-react";

import { NavMain } from "@/components/nav-main";
import { NavSecondary } from "@/components/nav-secondary";
import { NavUser } from "@/components/nav-user";
import { useFeatures } from "@/contexts/FeatureContext";
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
} from "@/components/ui/sidebar";
import { AccountRoles, FeatureKey } from "@/generated/api";

const data = {
  navMain: [
    {
      title: "",
      items: [
        {
          title: "Home",
          url: "/dashboard/",
          icon: Home,
          isActive:
            typeof window !== "undefined" &&
            window.location.pathname === "/dashboard/",
        },
      ],
    },
    {
      title: "Catalog",
      items: [
        {
          title: "Products",
          url: "/dashboard/products",
          icon: ShoppingBag,
          isActive:
            typeof window !== "undefined" &&
            window.location.pathname.startsWith("/dashboard/products"),
        },
      ],
    },
    {
      title: "Ledger",
      items: [
        {
          title: "Activities",
          url: "/dashboard/activities",
          icon: List,
          isActive:
            typeof window !== "undefined" &&
            window.location.pathname.startsWith("/dashboard/activities"),
        },
      ],
    },
    {
      title: "Reporting",
      items: [
        {
          title: "Corporate Carbon Footprint Overview",
          url: "/dashboard/corporate-carbon-footprint",
          icon: BarChart3,
          isActive:
            typeof window !== "undefined" &&
            window.location.pathname ===
              "/dashboard/corporate-carbon-footprint",
        },
        {
          title: "Product Carbon Footprint",
          url: "/dashboard/product-carbon-footprint",
          icon: Package2,
          isActive:
            typeof window !== "undefined" &&
            window.location.pathname === "/dashboard/product-carbon-footprint",
        },
        {
          title: "Sustainability Reports",
          url: "/dashboard/sustainability-reports",
          icon: FileText,
          isActive:
            typeof window !== "undefined" &&
            window.location.pathname.startsWith(
              "/dashboard/sustainability-reports",
            ),
        },
      ],
    },
    {
      title: "Experimental",
      items: [
        {
          title: "DropZone",
          url: "/dashboard/dropzone",
          icon: Upload,
          isActive:
            typeof window !== "undefined" &&
            window.location.pathname.startsWith("/dashboard/dropzone"),
          badge: "Experimental",
          featureKey: FeatureKey.dropzone, // Feature-gated navigation item
        },
      ],
    },
  ],
  navSecondary: [
    {
      title: "Users",
      url: "/dashboard/users",
      icon: Users,
    },
    {
      title: "Accounting",
      url: "/dashboard/accounting",
      icon: Settings,
    },
    {
      title: "Organization Settings",
      url: "/dashboard/organization-settings",
      icon: Settings,
    },
  ],
};

export function AppSidebar({
  user,
  onLogout,
  ...props
}: React.ComponentProps<typeof Sidebar> & {
  user?: {
    name: string;
    email: string;
    initials?: string;
    roles?: AccountRoles[];
  };
  onLogout?: () => void;
}) {
  const { isFeatureEnabled } = useFeatures();

  // Helper function to check if user has admin role
  const isAdmin =
    user?.roles &&
    (Array.isArray(user.roles)
      ? user.roles.includes(AccountRoles.AdminUserRole)
      : user.roles === AccountRoles.AdminUserRole);

  // Filter main nav groups and items based on feature flags
  const filteredNavMain = data.navMain
    .map((group) => ({
      ...group,
      items: group.items.filter((item) => {
        // Check if item has a feature requirement
        if ("featureKey" in item && item.featureKey) {
          return isFeatureEnabled(item.featureKey as FeatureKey);
        }
        return true;
      }),
    }))
    .filter((group) => group.items.length > 0); // Remove empty groups

  // Filter secondary nav items based on role
  const filteredNavSecondary = data.navSecondary.filter((item) => {
    // Only show admin-only pages to admins
    const adminOnlyPages = ["Users", "Accounting", "Organization Settings"];
    if (adminOnlyPages.includes(item.title)) {
      return isAdmin;
    }
    return true;
  });
  return (
    <Sidebar collapsible="icon" {...props}>
      <SidebarHeader>
        <SidebarMenu>
          <SidebarMenuItem>
            <SidebarMenuButton asChild size="lg">
              <a
                href="/"
                className="flex items-center justify-center w-full py-4"
              >
                <img
                  src="/logo/witnes-light.svg"
                  alt="Witnes"
                  className="h-8 w-auto dark:hidden"
                />
                <img
                  src="/logo/witnes-dark.svg"
                  alt="Witnes"
                  className="h-8 w-auto hidden dark:block"
                />
              </a>
            </SidebarMenuButton>
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarHeader>
      <SidebarContent>
        <NavMain items={filteredNavMain} />
        <NavSecondary items={filteredNavSecondary} className="mt-auto" />
      </SidebarContent>
      <SidebarFooter>
        {user && <NavUser user={user} onLogout={onLogout} />}
      </SidebarFooter>
    </Sidebar>
  );
}
