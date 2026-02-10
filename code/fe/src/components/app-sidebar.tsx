"use client";

import * as React from "react";
import {
  BarChart3,
  Settings,
  ShoppingBag,
  Users,
  Home,
  Activity,
  Receipt,
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
        {
          title: "Visitors",
          url: "/dashboard/visitors",
          icon: Users,
          isActive:
            typeof window !== "undefined" &&
            window.location.pathname.startsWith("/dashboard/visitors"),
        },
        {
          title: "Organizations",
          url: "/dashboard/organizations",
          icon: ShoppingBag,
          badge: "Coming soon",
          disabled: true,
          isActive:
            typeof window !== "undefined" &&
            window.location.pathname.startsWith("/dashboard/organizations"),
        },
        {
          title: "Analytics",
          url: "/dashboard/analytics",
          icon: BarChart3,
          badge: "Coming soon",
          disabled: true,
          isActive:
            typeof window !== "undefined" &&
            window.location.pathname.startsWith("/dashboard/analytics"),
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
      title: "Usage",
      url: "/dashboard/usage",
      icon: Activity,
    },
    {
      title: "Billing",
      url: "/dashboard/billing",
      icon: Receipt,
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
    const adminOnlyPages = ["Users", "Usage", "Billing", "Organization Settings"];
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
