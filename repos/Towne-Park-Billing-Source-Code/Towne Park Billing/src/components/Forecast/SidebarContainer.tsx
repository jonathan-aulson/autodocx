import { Customer, TimeRangeType } from "@/lib/models/Statistics";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { useEffect, useState } from "react";
import { Button } from "../ui/button";
import CustomerSiteWidget from "./CustomerSiteWidget";
import ViewOptionsWidget from "./ViewOptionsWidget";

interface SidebarContainerProps {
    customers: Customer[];
    isLoadingCustomers: boolean;
    error: string | null;
    selectedSite: string;
    setSelectedSite: (site: string) => void;
    totalRooms?: number;
    startingMonth: string;
    setStartingMonth: (month: string) => void;
    timePeriod: TimeRangeType;
    setTimePeriod: (period: TimeRangeType) => void;
    activeTab: string;
    isExpanded?: boolean;
    onExpandedChange?: (expanded: boolean) => void;
    isSidebarDisabled?: boolean;
}

const SidebarContainer: React.FC<SidebarContainerProps> = ({
    customers,
    isLoadingCustomers,
    error,
    selectedSite,
    setSelectedSite,
    totalRooms,
    startingMonth,
    setStartingMonth,
    timePeriod,
    setTimePeriod,
    activeTab,
    isExpanded: propIsExpanded,
    onExpandedChange,
    isSidebarDisabled
}) => {
    const [isExpandedState, setIsExpandedState] = useState(true);
    const [isMobile, setIsMobile] = useState(false);

    const isExpanded = propIsExpanded !== undefined ? propIsExpanded : isExpandedState;

    useEffect(() => {
        const checkScreenSize = () => {
            setIsMobile(window.innerWidth < 768);
            if (window.innerWidth < 768) {
                setIsExpandedState(true);
            }
        };

        checkScreenSize();

        window.addEventListener('resize', checkScreenSize);

        return () => window.removeEventListener('resize', checkScreenSize);
    }, []);

    const selectedCustomer = selectedSite
        ? customers.find(c => c.customerSiteId === selectedSite)
        : undefined;

    const getMonthName = (monthStr: string) => {
        const [year, month] = monthStr.split('-');
        const date = new Date(parseInt(year), parseInt(month) - 1);
        return date.toLocaleString('en-US', { month: 'long' });
    };

    const toggleSidebar = () => {
        const newExpandedState = !isExpanded;
        
        if (propIsExpanded === undefined) {
            setIsExpandedState(newExpandedState);
        }
        
        if (onExpandedChange) {
            onExpandedChange(newExpandedState);
        }
    };

    if (isMobile) {
        return (
            <div className="w-full space-y-6">
                <CustomerSiteWidget
                    customers={customers}
                    isLoadingCustomers={isLoadingCustomers}
                    error={error}
                    selectedSite={selectedSite}
                    setSelectedSite={setSelectedSite}
                    totalRooms={totalRooms}
                    isSidebarDisabled={isSidebarDisabled}
                />
                <ViewOptionsWidget
                    startingMonth={startingMonth}
                    setStartingMonth={setStartingMonth}
                    timePeriod={timePeriod}
                    setTimePeriod={setTimePeriod}
                    activeTab={activeTab}
                    isSidebarDisabled={isSidebarDisabled}
                />
            </div>
        );
    }

    return (
        <div 
            className={`sticky top-20 h-[calc(100vh-4rem)] overflow-x-hidden max-h-screen transition-all duration-300 ease-in-out flex flex-col
            ${isExpanded ? 'w-80 min-w-[320px] max-w-[320px]' : 'w-24 min-w-[96px] max-w-[96px]'}`}
            style={{flex: '0 0 auto'}}
        >
            <Button
                variant="outline"
                size="icon"
                className={`absolute -right-0 ${isExpanded ? 'top-1/3' : 'top-24'} z-30 h-8 w-8 rounded-full border bg-background shadow-md`}
                onClick={toggleSidebar}
                data-qa-id="toggle-sidebar-button"
            >
                {isExpanded ? <ChevronLeft className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}
            </Button>

            {isExpanded ? (
                <div className="space-y-2 border rounded-md p-2 bg-white dark:bg-slate-900 w-full">
                    <CustomerSiteWidget
                        customers={customers}
                        isLoadingCustomers={isLoadingCustomers}
                        error={error}
                        selectedSite={selectedSite}
                        setSelectedSite={setSelectedSite}
                        totalRooms={totalRooms}
                        isSidebarDisabled={isSidebarDisabled}
                    />
                    <ViewOptionsWidget
                        startingMonth={startingMonth}
                        setStartingMonth={setStartingMonth}
                        timePeriod={timePeriod}
                        setTimePeriod={setTimePeriod}
                        activeTab={activeTab}
                        isSidebarDisabled={isSidebarDisabled}
                    />
                </div>
            ) : (
                <div className="p-2 border rounded-md shadow-sm bg-white dark:bg-slate-900 w-24">
                    <div className="space-y-4 text-xs">
                        <div className="pb-2 border-b">
                            <h4 className="font-bold mb-1">Site</h4>
                            {selectedCustomer ? (
                                <div className="truncate" title={`${selectedCustomer.siteName} (${selectedCustomer.siteNumber})`}>
                                    {selectedCustomer.siteNumber}
                                </div>
                            ) : (
                                <div className="text-muted-foreground">None</div>
                            )}
                            
                            {totalRooms !== undefined && (
                                <div className="mt-1 truncate" title={`${totalRooms} rooms`}>
                                    {totalRooms} rooms
                                </div>
                            )}
                        </div>
                        
                        <div className="pb-2 border-b">
                            <h4 className="font-bold mb-1">Date</h4>
                            <div className="truncate" title={`${getMonthName(startingMonth)}, ${startingMonth.split('-')[0]}`}>
                                {getMonthName(startingMonth).substring(0, 3)} {startingMonth.split('-')[0]}
                            </div>
                        </div>
                        
                        <div className="pb-2 border-b">
                            <h4 className="font-bold mb-1">View</h4>
                            <div className="truncate" title={timePeriod}>
                                {timePeriod}
                            </div>
                        </div>
                        

                    </div>
                </div>
            )}
        </div>
    );
};

export default SidebarContainer;
