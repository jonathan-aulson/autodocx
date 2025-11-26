import { routes } from "@/authConfig";
import { Icons } from '@/components/Icons/Icons';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { useAuth } from "@/contexts/AuthContext";
import { Building2, ChartColumnDecreasing, TrendingUp } from "lucide-react";
import { Link, useLocation } from "react-router-dom";
import { useTheme } from "./ThemeProvider";
import { Button } from "./ui/button";
import { UserMenu } from "./UserMenu";

export function ModeToggle() {
  const { setTheme } = useTheme();

  return (
<DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="outline" size="icon" data-qa-id="button-themeToggle">
          <Icons.sun className="h-[1.2rem] w-[1.2rem] rotate-0 scale-100 transition-all dark:-rotate-90 dark:scale-0" />
          <Icons.moon className="absolute h-[1.2rem] w-[1.2rem] rotate-90 scale-0 transition-all dark:rotate-0 dark:scale-100" />
          <span className="sr-only">Toggle theme</span>
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" data-qa-id="dropdown-themeOptions">
        <DropdownMenuItem onClick={() => setTheme("light")} data-qa-id="dropdown-item-lightTheme">
          Light
        </DropdownMenuItem>
        <DropdownMenuItem onClick={() => setTheme("dark")} data-qa-id="dropdown-item-darkTheme">
          Dark
        </DropdownMenuItem>
        <DropdownMenuItem onClick={() => setTheme("system")} data-qa-id="dropdown-item-systemTheme">
          System
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

export const SiteHeader = () => {
  const { theme } = useTheme(); // Access the current theme
  const location = useLocation();
  const { userRoles } = useAuth();

  const canAccessForecast = userRoles.includes('accountManager') || userRoles.includes('districtManager');

  const getLinkClass = (path: string) => {
    const isSelected = location.pathname.startsWith(path);
    if (theme === "dark") {
      return isSelected ? "text-gray-300" : "text-gray-600 hover:text-gray-400";
    } else {
      return isSelected ? "text-gray-600" : "font-bold text-gray-300 hover:text-gray-400";
    }
  };

  const getIconClass = (path: string) => {
    const isSelected = location.pathname.startsWith(path);
    if (theme === "dark") {
      return isSelected ? "text-gray-300" : "text-gray-600";
    } else {
      return isSelected ? "text-gray-600" : "text-gray-300";
    }
  };

  // Define dynamic classes based on the theme
  const headerBgClass = theme === "dark" ? "bg-black/80" : "bg-white/80";
  const textColorClass = theme === "dark" ? "text-white" : "text-gray-800";

  return (
    <div>
      <header className={`fixed top-0 left-0 right-0 z-40 ${headerBgClass} backdrop-blur-sm shadow-md ${textColorClass}`}>
        <div className="flex items-center justify-between p-3">
          <div className="flex items-center space-x-4 ml-4 md:ml-16">
            <Link className={`flex items-center gap-2 font-semibold ${textColorClass}`} to={routes.customersList}>
              <img src="/tp-logo.png" alt="Towne Park logo" className="h-22 w-48 md:h-8 md:w-36" />
            </Link>
            <div className="space-x-6 hidden md:block">
              <Link className={getLinkClass(routes.customersList)} to={routes.customersList}>
                Customers
              </Link>
              <Link className={getLinkClass(routes.statements)} to={routes.statements}>
                Statements
              </Link>
              {canAccessForecast && (
                <Link className={getLinkClass(routes.forecasts)} to={routes.forecasts}>
                  Forecasts
                </Link>
              )}
              {canAccessForecast && (
                <Link className={getLinkClass(routes.pnlView)} to={routes.pnlView}>
                  P&L View
                </Link>
              )}
            </div>
          </div>
          <div className="flex items-center space-x-4">
            <ModeToggle />
            <UserMenu />
          </div>
        </div>
      </header>
      <div className={`fixed bottom-0 left-0 right-0 z-[9999] ${headerBgClass} backdrop-blur-sm shadow-md md:hidden`}>
        <div className="flex justify-around p-4 border-t-2">
          <Link to={routes.customersList} className={`flex flex-col items-center ${getLinkClass(routes.customersList)}`}>
            <Building2 className={`h-6 w-6 mb-1 ${getIconClass(routes.customersList)}`} />
            <span className="text-xs">Customers</span>
          </Link>
          <Link to={routes.statements} className={`flex flex-col items-center ${getLinkClass(routes.statements)}`}>
            <FileTextIcon className={`h-6 w-6 mb-1 ${getIconClass(routes.statements)}`} />
            <span className="text-xs">Statements</span>
          </Link>
          {canAccessForecast && (
            <Link to={routes.forecasts} className={`flex flex-col items-center ${getLinkClass(routes.forecasts)}`}>
              <TrendingUp className={`h-6 w-6 mb-1 ${getIconClass(routes.forecasts)}`} />
              <span className="text-xs">Forecasts</span>
            </Link>
          )}
          {canAccessForecast && (
            <Link to={routes.pnlView} className={`flex flex-col items-center ${getLinkClass(routes.pnlView)}`}>
              <ChartColumnDecreasing className={`h-6 w-6 mb-1 ${getIconClass(routes.pnlView)}`} />
              <span className="text-xs">P&L View</span>
            </Link>
          )}
        </div>
      </div>
    </div>
  );
};

function BarChartIcon(props: React.SVGProps<SVGSVGElement>) {
  return (
    <svg
      {...props}
      xmlns="http://www.w3.org/2000/svg"
      width="24"
      height="24"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <line x1="12" x2="12" y1="20" y2="10" />
      <line x1="18" x2="18" y1="20" y2="4" />
      <line x1="6" x2="6" y1="20" y2="16" />
    </svg>
  );
}

function FileTextIcon(props: React.SVGProps<SVGSVGElement>) {
  return (
    <svg
      {...props}
      xmlns="http://www.w3.org/2000/svg"
      width="24"
      height="24"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M15 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7Z" />
      <path d="M14 2v4a2 2 0 0 0 2 2h4" />
      <path d="M10 9H8" />
      <path d="M16 13H8" />
      <path d="M16 17H8" />
    </svg>
  );
}

function LayoutDashboardIcon(props: React.SVGProps<SVGSVGElement>) {
  return (
    <svg
      {...props}
      xmlns="http://www.w3.org/2000/svg"
      width="24"
      height="24"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M3 9.5L12 3l9 6.5V21a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V9.5z" />
      <polyline points="9 22 9 12 15 12 15 22" />
    </svg>
  );
}

function BellIcon(props: React.SVGProps<SVGSVGElement>) {
  return (
    <svg
      {...props}
      xmlns="http://www.w3.org/2000/svg"
      width="24"
      height="24"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M6 8a6 6 0 0 1 12 0c0 7 3 9 3 9H3s3-2 3-9" />
      <path d="M10.3 21a1.94 1.94 0 0 0 3.4 0" />
    </svg>
  );
}
