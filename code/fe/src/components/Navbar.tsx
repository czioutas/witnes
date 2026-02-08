import { useState } from "react";
import { Menu, X, ChevronDown } from "lucide-react";
import { Button } from "./ui/button";
import { AuthActions } from "./AuthActions";
import {
  NavigationMenu,
  NavigationMenuContent,
  NavigationMenuItem,
  NavigationMenuLink,
  NavigationMenuList,
  NavigationMenuTrigger,
} from "./ui/navigation-menu";

interface NavbarProps {
  currentPath?: string;
}

export function Navbar({ currentPath }: NavbarProps) {
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [isSolutionsOpen, setIsSolutionsOpen] = useState(false);

  const navigation = [
    { name: "Why we exist", href: "/#why" },
    { name: "Impact", href: "/#impact" },
    { name: "Reports", href: "/#reports" },
    { name: "Principles", href: "/#principles" },
  ];

  const solutions = [
    { name: "Emissions per Product", href: "/solutions/emissions-per-product" },
    { name: "Compliance & Audit", href: "/solutions/compliance-audit" },
    { name: "CBAM Reporting", href: "/solutions/cbam-reporting" },
    {
      name: "Guided CO₂ Measurement",
      href: "/solutions/guided-co2-measurement",
    },
  ];

  const isCurrentPath = (href: string) => {
    if (href === "/" && currentPath === "/") return true;
    if (href !== "/" && currentPath?.startsWith(href)) return true;
    return false;
  };

  const isSolutionsPage = currentPath?.startsWith("/solutions");

  return (
    <nav className="fixed top-0 left-0 right-0 z-50 border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="flex h-16 items-center">
          {/* Logo */}
          <div className="flex items-center flex-1">
            <a href="/" className="flex items-center">
              <img src="/logo/witnes-dark.svg" alt="Witnes" className="h-8" />
            </a>
          </div>

          {/* Desktop Navigation - Centered */}
          <div className="hidden sm:flex sm:items-center sm:gap-6">
            <NavigationMenu viewport={false}>
              <NavigationMenuList>
                {navigation.map((item) => (
                  <NavigationMenuItem key={item.name}>
                    <a
                      href={item.href}
                      className={`inline-flex items-center px-3 py-2 text-sm font-medium transition-colors rounded-md ${
                        isCurrentPath(item.href)
                          ? "text-foreground bg-accent"
                          : "text-muted-foreground hover:text-foreground hover:bg-accent/50"
                      }`}
                    >
                      {item.name}
                    </a>
                  </NavigationMenuItem>
                ))}

                {/* Solutions Dropdown */}
                <NavigationMenuItem>
                  <NavigationMenuTrigger
                    className={`${
                      isSolutionsPage
                        ? "text-foreground bg-accent"
                        : "text-muted-foreground"
                    }`}
                  >
                    Solutions
                  </NavigationMenuTrigger>
                  <NavigationMenuContent>
                    <ul className="grid w-[240px] gap-1 p-2">
                      {solutions.map((solution) => (
                        <li key={solution.name}>
                          <NavigationMenuLink asChild>
                            <a
                              href={solution.href}
                              className={`block select-none rounded-md p-3 leading-none no-underline outline-none transition-colors hover:bg-accent hover:text-accent-foreground focus:bg-accent focus:text-accent-foreground ${
                                isCurrentPath(solution.href)
                                  ? "bg-accent text-accent-foreground"
                                  : ""
                              }`}
                            >
                              <div className="text-sm font-medium leading-none mb-1">
                                {solution.name}
                              </div>
                            </a>
                          </NavigationMenuLink>
                        </li>
                      ))}
                    </ul>
                  </NavigationMenuContent>
                </NavigationMenuItem>
              </NavigationMenuList>
            </NavigationMenu>
          </div>

          {/* Desktop Auth Actions - Only shown on desktop */}
          <div className="hidden sm:flex sm:items-center sm:justify-end flex-1">
            <div className="hidden sm:block">
              <AuthActions />
            </div>
          </div>

          {/* Mobile menu button - Only shown on mobile */}
          <Button
            variant="ghost"
            size="sm"
            onClick={() => setIsMenuOpen(!isMenuOpen)}
            aria-label="Toggle menu"
            className="sm:hidden"
          >
            {isMenuOpen ? (
              <X className="h-6 w-6" />
            ) : (
              <Menu className="h-6 w-6" />
            )}
          </Button>
        </div>
      </div>

      {/* Mobile menu */}
      {isMenuOpen && (
        <div className="sm:hidden">
          <div className="space-y-1 pb-3 pt-2">
            {navigation.map((item) => (
              <a
                key={item.name}
                href={item.href}
                className={`block px-4 py-2 text-base font-medium transition-colors ${
                  isCurrentPath(item.href)
                    ? "bg-primary/10 text-primary"
                    : "text-muted-foreground hover:bg-accent hover:text-foreground"
                }`}
                onClick={() => setIsMenuOpen(false)}
              >
                {item.name}
              </a>
            ))}

            {/* Mobile Solutions Section */}
            <div className="pt-2">
              <button
                onClick={() => setIsSolutionsOpen(!isSolutionsOpen)}
                className="flex w-full items-center justify-between px-4 py-2 text-base font-medium text-muted-foreground hover:bg-accent hover:text-foreground transition-colors"
              >
                <span>Solutions</span>
                <ChevronDown
                  className={`h-4 w-4 transition-transform ${
                    isSolutionsOpen ? "rotate-180" : ""
                  }`}
                />
              </button>
              {isSolutionsOpen && (
                <div className="bg-muted/50">
                  {solutions.map((solution) => (
                    <a
                      key={solution.name}
                      href={solution.href}
                      className={`block px-8 py-2 text-sm transition-colors ${
                        isCurrentPath(solution.href)
                          ? "bg-primary/10 text-primary"
                          : "text-muted-foreground hover:bg-accent hover:text-foreground"
                      }`}
                      onClick={() => setIsMenuOpen(false)}
                    >
                      {solution.name}
                    </a>
                  ))}
                </div>
              )}
            </div>
          </div>
          <div className="border-t border-border pb-3 pt-4">
            <div className="px-4">
              <AuthActions />
            </div>
          </div>
        </div>
      )}
    </nav>
  );
}
