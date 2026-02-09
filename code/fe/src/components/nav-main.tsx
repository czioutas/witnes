"use client"

import { type LucideIcon } from "lucide-react"

import {
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
} from '@/components/ui/sidebar'
import { Badge } from '@/components/ui/badge'

export function NavMain({
  items,
}: {
  items: {
    title: string
    items: {
      title: string
      url: string
      icon?: LucideIcon
      isActive?: boolean
      badge?: string
      disabled?: boolean
    }[]
  }[]
}) {
  return (
    <>
      {items.map((group) => (
        <SidebarGroup key={group.title || 'default'} className={!group.title ? 'pb-0' : undefined}>
          {group.title && <SidebarGroupLabel>{group.title}</SidebarGroupLabel>}
          <SidebarGroupContent>
            <SidebarMenu>
              {group.items.map((item) => (
                <SidebarMenuItem key={item.title}>
                  <SidebarMenuButton tooltip={item.title} isActive={item.isActive} asChild>
                    <a
                      href={item.disabled ? undefined : item.url}
                      className={item.disabled ? 'pointer-events-none opacity-50' : undefined}
                      aria-disabled={item.disabled}
                    >
                      {item.icon && <item.icon />}
                      <span>{item.title}</span>
                      {item.badge && (
                        <Badge variant="secondary" className="ml-auto text-[10px] px-1.5 py-0 font-normal bg-emerald-100 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-400">
                          {item.badge}
                        </Badge>
                      )}
                    </a>
                  </SidebarMenuButton>
                </SidebarMenuItem>
              ))}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
      ))}
    </>
  )
}
