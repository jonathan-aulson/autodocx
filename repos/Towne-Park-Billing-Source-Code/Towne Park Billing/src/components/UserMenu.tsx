import { routes } from "@/authConfig";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Button } from "@/components/ui/button";
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuLabel,
    DropdownMenuSeparator,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { useAuth } from "@/contexts/AuthContext";
import { getInitials } from "@/lib/utils/get-initials";
import { useNavigate } from "react-router-dom";

const formatRoleName = (role: string): string => {
    return role
        .replace(/([A-Z])/g, ' $1')
        .replace(/^./, str => str.toUpperCase())
        .trim();
};

export const UserMenu = () => {
    const { userName, userRoles, logout } = useAuth();
    const navigate = useNavigate();

    const isBillingAdmin = userRoles.includes('billingAdmin');
    const formattedRoles = userRoles
        .filter(role => role)
        .map(formatRoleName);

    return (
        <DropdownMenu>
            <DropdownMenuTrigger asChild>
                <Button
                    variant="outline"
                    className="relative flex h-12 flex-row rounded-full px-0 py-2"
                    data-qa-id="button-userMenu"
                >
                    <HamburgerMenuIcon className="ml-4" />
                    <div className="p-2">
                        <Avatar className="h-8 w-8">
                            <AvatarImage src={""} alt="user avatar" />
                            <AvatarFallback>{getInitials(`${userName}`)}</AvatarFallback>
                        </Avatar>
                    </div>
                </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent className="w-56" align="end" forceMount data-qa-id="dropdown-userOptions">
                <DropdownMenuLabel className="font-normal">
                    <div className="flex flex-col space-y-1">
                        <p className="text-sm font-medium leading-none">{userName}</p>
                        <div className="text-xs leading-relaxed text-muted-foreground">
                            {formattedRoles.map((role, index) => (
                                <p key={index}>{role}</p>
                            ))}
                        </div>
                    </div>
                </DropdownMenuLabel>
                <DropdownMenuSeparator />
                {isBillingAdmin && (
                    <>
                        <DropdownMenuItem
                            onClick={() => navigate(routes.adminPanel)}
                            key="adminPanelRedirect"
                        >
                            Admin Panel
                        </DropdownMenuItem>
                        <DropdownMenuSeparator />
                    </>
                )}
                <DropdownMenuItem
                    onClick={logout}
                    key="logoutRedirect"
                    data-qa-id="dropdown-item-logout"
                >
                    Logout
                </DropdownMenuItem>
            </DropdownMenuContent>
        </DropdownMenu>
    );
};

function HamburgerMenuIcon(props: React.SVGProps<SVGSVGElement>) {
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
            <line x1="3" y1="12" x2="21" y2="12" />
            <line x1="3" y1="6" x2="21" y2="6" />
            <line x1="3" y1="18" x2="21" y2="18" />
        </svg>
    )
}