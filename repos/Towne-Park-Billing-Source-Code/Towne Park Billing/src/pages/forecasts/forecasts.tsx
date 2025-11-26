import { routes } from "@/authConfig";
import OtherExpenses from "@/components/Forecast/OtherExpenses/OtherExpenses";
import OtherRevenue from "@/components/Forecast/OtherRevenue/OtherRevenue";
import ParkingRateForm from "@/components/Forecast/ParkingRates/ParkingRates";
import PayrollForecast from "@/components/Forecast/Payroll/PayrollForecast";
import SidebarContainer from "@/components/Forecast/SidebarContainer";
import React, { useRef, useCallback, useMemo } from "react";
import SiteStatisticsForm from "@/components/Forecast/Statistics/Statistics";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { useAuth } from "@/contexts/AuthContext";
import { useCustomer } from "@/contexts/CustomerContext";
import { Contract } from "@/lib/models/Contract";
import { Customer, TimeRangeType } from "@/lib/models/Statistics";
import { useEffect, useState } from "react";
import { useToast } from "@/components/ui/use-toast";
import { useNavigate } from "react-router-dom";
import { debounce } from "@/lib/utils/debounce";

const PARKING_RATE_GUIDE_STORAGE_KEY = 'isParkingRateGuideExpanded';
const STATISTICS_GUIDE_STORAGE_KEY = 'isStatisticsGuideOpen';
const PAYROLL_GUIDE_STORAGE_KEY = 'isPayrollGuideOpen';
const OTHER_REVENUE_GUIDE_STORAGE_KEY = 'isOtherRevenueGuideOpen';
const STARTING_MONTH_STORAGE_KEY = 'selectedStartingMonth';
const TIME_PERIOD_STORAGE_KEY = 'selectedTimePeriod';
const ACTIVE_TAB_STORAGE_KEY = 'selectedActiveTab';

export const Forecasts = () => {
    const { userRoles } = useAuth();
    const { selectedCustomer, setSelectedCustomerById } = useCustomer();
    
    const [customers, setCustomers] = useState<Customer[]>([]);
    const [isLoadingCustomers, setIsLoadingCustomers] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [selectedSite, setSelectedSite] = useState<string>("");
    const [totalRooms, setTotalRooms] = useState<number | undefined>(undefined);



    // Track statistics loading state for sidebar disabling
    const [isStatisticsLoading, setIsStatisticsLoading] = useState(false);
    const [isParkingRatesLoading, setIsParkingRatesLoading] = useState(false);
    const [isPayrollLoading, setIsPayrollLoading] = useState(false);
    const [isOtherRevenueLoading, setIsOtherRevenueLoading] = useState(false);
    const [isOtherExpensesLoading, setIsOtherExpensesLoading] = useState(false);

    const [isParkingRateGuideExpanded, setIsParkingRateGuideExpandedState] = useState<boolean>(() => {
        const savedState = localStorage.getItem(PARKING_RATE_GUIDE_STORAGE_KEY);
        return savedState === null ? true : savedState === 'true';
    });

    const [isStatisticsGuideOpen, setIsStatisticsGuideOpenState] = useState<boolean>(() => {
        const savedState = localStorage.getItem(STATISTICS_GUIDE_STORAGE_KEY);
        return savedState === null ? true : savedState === 'true';
    });

    const [isOtherRevenueGuideOpen, setIsOtherRevenueGuideOpenState] = useState<boolean>(() => {
        const savedState = localStorage.getItem(OTHER_REVENUE_GUIDE_STORAGE_KEY);
        return savedState === null ? true : savedState === 'true';
    });

    const [isOtherExpensesGuideOpen, setIsOtherExpensesGuideOpenState] = useState<boolean>(() => {
        const savedState = localStorage.getItem("isOtherExpensesGuideOpen");
        return savedState === null ? true : savedState === 'true';
    });

    const [isPayrollGuideOpen, setIsPayrollGuideOpenState] = useState<boolean>(() => {
        const savedState = localStorage.getItem(PAYROLL_GUIDE_STORAGE_KEY);
        return savedState === null ? true : savedState === 'true';
    });

    const [isSidebarExpanded, setIsSidebarExpanded] = useState(true);

    // Contract details for sharing between PayrollForecast and OtherExpenses
    const [contractDetails, setContractDetails] = useState<Contract | null>(null);

    const navigate = useNavigate();

    const setIsParkingRateGuideExpanded = (value: boolean) => {
        setIsParkingRateGuideExpandedState(value);
        localStorage.setItem(PARKING_RATE_GUIDE_STORAGE_KEY, value.toString());
    };

    const setIsStatisticsGuideOpen = (value: boolean) => {
        setIsStatisticsGuideOpenState(value);
        localStorage.setItem(STATISTICS_GUIDE_STORAGE_KEY, value.toString());
    };

    const setIsOtherRevenueGuideOpen = (value: boolean) => {
        setIsOtherRevenueGuideOpenState(value);
        localStorage.setItem(OTHER_REVENUE_GUIDE_STORAGE_KEY, value.toString());
    };

    const setIsPayrollGuideOpen = (value: boolean) => {
        setIsPayrollGuideOpenState(value);
        localStorage.setItem(PAYROLL_GUIDE_STORAGE_KEY, value.toString());
    };

    const [startingMonth, setStartingMonth] = useState<string>(() => {
        const savedMonth = sessionStorage.getItem(STARTING_MONTH_STORAGE_KEY);
        return savedMonth || new Date().toISOString().slice(0, 7);
    });

    const [timePeriod, setTimePeriod] = useState<TimeRangeType>(() => {
        const savedTimePeriod = sessionStorage.getItem(TIME_PERIOD_STORAGE_KEY);
        return savedTimePeriod ? (savedTimePeriod as TimeRangeType) : TimeRangeType.DAILY;
    });

    const [activeTab, setActiveTab] = useState<string>(() => {
        // First try to get from sessionStorage
        const savedTab = sessionStorage.getItem(ACTIVE_TAB_STORAGE_KEY);
        if (savedTab) {
            return savedTab;
        }
        
        // Fall back to URL parameters
        const urlParams = new URLSearchParams(window.location.search);
        const tabFromUrl = urlParams.get("tab");
        if (tabFromUrl === "parking-rates" || tabFromUrl === "payroll" || tabFromUrl === "otherRevenue" || tabFromUrl === "otherExpenses") {
            return tabFromUrl;
        }
        
        // Default to statistics
        return "statistics";
    });

    const setIsOtherExpensesGuideOpen = (value: boolean) => {
        setIsOtherExpensesGuideOpenState(value);
        localStorage.setItem("isOtherExpensesGuideOpen", value.toString());
    };

    const [statisticsDirty, setStatisticsDirty] = useState(false);
    const [parkingRatesDirty, setParkingRatesDirty] = useState(false);
    const [otherRevenueDirty, setOtherRevenueDirty] = useState(false);
    const [otherExpensesDirty, setOtherExpensesDirty] = useState(false);
    const [payrollDirty, setPayrollDirty] = useState(false);

    // Ref for SiteStatisticsForm
    const statisticsRef = useRef<{ save: () => Promise<void>; refresh: () => Promise<void> }>(null);
    // Ref for ParkingRateForm
    const parkingRatesRef = useRef<{ save: () => Promise<void> }>(null);
    // Ref for PayrollForecast
    const payrollRef = useRef<{ save: () => Promise<void> }>(null);
    // Ref for OtherRevenue
    const otherRevenueRef = useRef<{ save: () => Promise<void> }>(null);
    // Ref for OtherExpenses
    const otherExpensesRef = useRef<{ save: () => Promise<void> }>(null);


    // Enhanced Save All state variables for race condition protection
    const [saveOperationId, setSaveOperationId] = useState<string | null>(null);
    const [savingSiteId, setSavingSiteId] = useState<string | null>(null);
    const [lastSaveAttempt, setLastSaveAttempt] = useState<number>(0);

    // Toast for notifications
    const { toast, dismiss } = useToast();

    // Callback to refresh statistics when parking rates are saved
    const handleParkingRatesSaved = async () => {
        if (statisticsRef.current?.refresh) {
            try {
                await statisticsRef.current.refresh();
                toast({
                    title: "Statistics Updated",
                    description: "Site statistics have been refreshed with the new parking rates.",
                    variant: "default"
                });
            } catch (error) {
                console.error('Failed to refresh statistics after parking rates save:', error);
                // Don't show error toast as statistics component will handle it
            }
        }
    };

    // Helper function for smart change detection
    const getTabsWithChanges = useCallback(() => {
        const changedTabs: Array<{
            name: string;
            ref: any;
            clearDirty: () => void;
            isDirty: boolean;
        }> = [];
        
        if (statisticsDirty && statisticsRef.current) {
            changedTabs.push({
                name: "Statistics",
                ref: statisticsRef.current,
                clearDirty: () => setStatisticsDirty(false),
                isDirty: true
            });
        }
        if (parkingRatesDirty && parkingRatesRef.current) {
            changedTabs.push({
                name: "Parking Rates", 
                ref: parkingRatesRef.current,
                clearDirty: () => setParkingRatesDirty(false),
                isDirty: true
            });
        }
        if (payrollDirty && payrollRef.current) {
            changedTabs.push({
                name: "Payroll",
                ref: payrollRef.current, 
                clearDirty: () => setPayrollDirty(false),
                isDirty: true
            });
        }
        if (otherRevenueDirty && otherRevenueRef.current) {
            changedTabs.push({
                name: "Other Revenue",
                ref: otherRevenueRef.current,
                clearDirty: () => setOtherRevenueDirty(false),
                isDirty: true
            });
        }
        if (otherExpensesDirty && otherExpensesRef.current) {
            changedTabs.push({
                name: "Other Expenses", 
                ref: otherExpensesRef.current,
                clearDirty: () => setOtherExpensesDirty(false),
                isDirty: true
            });
        }
        
        return changedTabs;
    }, [statisticsDirty, parkingRatesDirty, payrollDirty, otherRevenueDirty, otherExpensesDirty]);

    // Enhanced comprehensive result summary
    const showSaveResultSummary = useCallback((results: {
        successful: string[], 
        failed: Array<{name: string, error: any}>, 
        siteChanged: boolean,
        noChanges: boolean
    }) => {
        if (results.siteChanged) {
            toast({
                title: "Save Cancelled",
                description: "Site was changed during save operation. Please try again.",
                variant: "destructive"
            });
            return;
        }
        
        if (results.noChanges) {
            toast({
                title: "No Changes to Save",
                description: "All forecast data is already up to date.",
                variant: "default"
            });
            return;
        }
        
        if (results.failed.length === 0) {
            // All successful
            toast({
                title: "Save Complete",
                description: `Successfully saved ${results.successful.length} tab${results.successful.length > 1 ? 's' : ''}: ${results.successful.join(', ')}`,
                variant: "default"
            });
        } else if (results.successful.length === 0) {
            // All failed
            toast({
                title: "Save Failed",
                description: `Failed to save ${results.failed.length} tab${results.failed.length > 1 ? 's' : ''}: ${results.failed.map(f => f.name).join(', ')}`,
                variant: "destructive"
            });
        } else {
            // Mixed results
            toast({
                title: "Partial Save Success",
                description: `Saved: ${results.successful.join(', ')}. Failed: ${results.failed.map(f => f.name).join(', ')}`,
                variant: "default"
            });
        }
        
        // Log detailed errors for debugging
        if (results.failed.length > 0) {
            console.error('Save operation errors:', results.failed);
        }
    }, [toast]);


    // Enhanced Global Save handler with protection
    const handleEnhancedGlobalSave = useCallback(
        debounce(async () => {
            if (!selectedSite) return;
            
            const now = Date.now();
            
            // Prevent rapid successive saves (debouncing backup)
            if (now - lastSaveAttempt < 1000) {
                console.warn('Save attempt blocked: too soon after last save');
                return;
            }
            
            const operationId = `save-${now}`;
            const currentSiteId = selectedSite;
            
            setSaveOperationId(operationId);
            setSavingSiteId(currentSiteId);
            setLastSaveAttempt(now);
            
            try {
                
                // Smart change detection - only save tabs with changes
                const changedTabs = getTabsWithChanges();
                
                if (changedTabs.length === 0) {
                    // Single consolidated message instead of multiple toasts
                    showSaveResultSummary({
                        successful: [],
                        failed: [],
                        siteChanged: false,
                        noChanges: true
                    });
                    // Clear operation state for no changes case
                    setSaveOperationId(null);
                    setSavingSiteId(null);
                    return;
                }
                
                // Show single saving toast for all changes
                const savingToast = toast({
                    title: `Saving ${changedTabs.length} tab${changedTabs.length > 1 ? 's' : ''}...`,
                    description: "Please wait while we save your changes.",
                    variant: "default",
                    duration: 10000
                });
                
                // Execute saves with site validation
                const results: {
                    successful: string[], 
                    failed: Array<{name: string, error: any}>, 
                    siteChanged: boolean,
                    noChanges: boolean
                } = { successful: [], failed: [], siteChanged: false, noChanges: false };
                
                await Promise.allSettled(changedTabs.map(async (tab, idx) => {
                    try {
                        // Verify site hasn't changed during save
                        if (selectedSite !== currentSiteId) {
                            results.siteChanged = true;
                            throw new Error("Site changed during save operation");
                        }
                        
                        await tab.ref.save();
                        tab.clearDirty();
                        results.successful.push(tab.name);
                        
                    } catch (error) {
                        results.failed.push({ name: tab.name, error });
                    }
                }));
                
                
                // Clear the saving toast
                if (savingToast?.id) {
                    dismiss(savingToast.id);
                }
                
                // Show comprehensive result summary
                showSaveResultSummary(results);
                
            } catch (error) {
                console.error('Global save operation failed:', error);
                toast({
                    title: "Save Operation Failed",
                    description: "An unexpected error occurred. Please try again.",
                    variant: "destructive"
                });
            } finally {
                // Always clear operation state when done
                setSaveOperationId(null);
                setSavingSiteId(null);
            }
        }, 500), // 500ms debounce
        [selectedSite, getTabsWithChanges, lastSaveAttempt, saveOperationId, toast, dismiss]
    );

    // Global Save handler - always use enhanced logic
    const handleGlobalSave = useCallback(async () => {
        await handleEnhancedGlobalSave();
    }, [handleEnhancedGlobalSave]);

    // Save All button disabled condition - enhanced logic
    const isSaveAllButtonDisabled = useMemo((): boolean => {
        // Enhanced logic: enabled whenever site is selected and not currently saving
        return !!saveOperationId || 
               !selectedSite || 
               (!!savingSiteId && savingSiteId !== selectedSite);
    }, [
        saveOperationId, 
        selectedSite, 
        savingSiteId
    ]);

    const [showUnsavedModal, setShowUnsavedModal] = useState(false);
    const [pendingSidebarChange, setPendingSidebarChange] = useState<{ type: 'site' | 'period', value: string } | null>(null);

    // New: Modal for navigating to Other Revenue with unsaved changes
    const [showOtherRevenueUnsavedModal, setShowOtherRevenueUnsavedModal] = useState(false);
    const [pendingTabChange, setPendingTabChange] = useState<string | null>(null);
    const previousTimePeriod = useRef<TimeRangeType | null>(null);

    const handleSiteChange = (site: string) => {
        // If save is in progress, allow site switch without showing unsaved changes dialog
        if (saveOperationId) {
            setSelectedSite(site);
            setSelectedCustomerById(site);
            sessionStorage.setItem("selectedSite", site);
            return;
        }
        
        if (statisticsDirty || parkingRatesDirty || otherRevenueDirty || payrollDirty) {
            setPendingSidebarChange({ type: 'site', value: site });
            setShowUnsavedModal(true);
        } else {
            setSelectedSite(site);
            setSelectedCustomerById(site);
            sessionStorage.setItem("selectedSite", site);
        }
    };

    const handlePeriodChange = (period: string) => {
        // If save is in progress, allow period switch without showing unsaved changes dialog
        if (saveOperationId) {
            setStartingMonth(period);
            sessionStorage.setItem(STARTING_MONTH_STORAGE_KEY, period);
            return;
        }
        
        if (statisticsDirty || parkingRatesDirty || otherRevenueDirty || payrollDirty) {
            setPendingSidebarChange({ type: 'period', value: period });
            setShowUnsavedModal(true);
        } else {
            setStartingMonth(period);
            sessionStorage.setItem(STARTING_MONTH_STORAGE_KEY, period);
        }
    };

    const handleTimePeriodChange = (newTimePeriod: TimeRangeType) => {
        setTimePeriod(newTimePeriod);
        sessionStorage.setItem(TIME_PERIOD_STORAGE_KEY, newTimePeriod);
    };

    const handleSetActiveTab = (tab: string) => {
        setActiveTab(tab);
        sessionStorage.setItem(ACTIVE_TAB_STORAGE_KEY, tab);
        
        // Also update URL
        const url = new URL(window.location.href);
        url.searchParams.set("tab", tab);
        window.history.pushState({}, "", url);
    };

    const handleConfirmSidebarChange = () => {
        if (pendingSidebarChange) {
            if (pendingSidebarChange.type === 'site') {
                setSelectedSite(pendingSidebarChange.value);
                setSelectedCustomerById(pendingSidebarChange.value);
                sessionStorage.setItem("selectedSite", pendingSidebarChange.value);
            } else if (pendingSidebarChange.type === 'period') {
                setStartingMonth(pendingSidebarChange.value);
                sessionStorage.setItem(STARTING_MONTH_STORAGE_KEY, pendingSidebarChange.value);
            }
        }
        // Reset all dirty flags when proceeding without saving
        setStatisticsDirty(false);
        setParkingRatesDirty(false);
        setOtherRevenueDirty(false);
        setOtherExpensesDirty(false);
        setPayrollDirty(false);
        setShowUnsavedModal(false);
        setPendingSidebarChange(null);
    };

    const handleCancelSidebarChange = () => {
        setShowUnsavedModal(false);
        setPendingSidebarChange(null);
    };

    useEffect(() => {
        const siteFromSession = sessionStorage.getItem("selectedSite");
        if (siteFromSession) {
            setSelectedSite(siteFromSession);
            setSelectedCustomerById(siteFromSession);
        }
    }, []);

    useEffect(() => {
        if (selectedCustomer && selectedCustomer.customerSiteId !== selectedSite) {
            setSelectedSite(selectedCustomer.customerSiteId);
        }
    }, [selectedCustomer, selectedSite]);

    useEffect(() => {
    if (userRoles.length > 0 && 
        !(userRoles.includes('accountManager') || userRoles.includes('districtManager'))) {
        navigate(routes.customersList);
    }
}, [userRoles, navigate]);


    useEffect(() => {
        const url = new URL(window.location.href);
        url.searchParams.set("tab", activeTab);
        window.history.replaceState({}, "", url);
    }, []);

    useEffect(() => {
        return () => {
            sessionStorage.setItem(ACTIVE_TAB_STORAGE_KEY, activeTab);
        };
    }, [activeTab]);

    useEffect(() => {
        async function fetchCustomers() {
            setIsLoadingCustomers(true);
            setError(null);
            try {
                const claimsHeader = JSON.stringify(userRoles);
                const response = await fetch("/api/customers?isForecast=true", {
                    headers: {
                        "x-client-roles": claimsHeader,
                    },
                });

                if (!response.ok) {
                    throw new Error(`Error fetching customers: ${response.status}`);
                }

                const data = await response.json();
                const sortedCustomers: Customer[] = data.sort((a: Customer, b: Customer) => a.siteNumber.localeCompare(b.siteNumber));
                setCustomers(sortedCustomers);
            } catch (err) {
                console.error('Failed to fetch customers:', err);
                setError('Failed to load customers. Please try again later.');
            } finally {
                setIsLoadingCustomers(false);
            }
        }

        fetchCustomers();
    }, [userRoles]);

    // Custom tab change handler for Other Revenue warning
    const handleTabChange = (value: string) => {
        const forecastTabs = ["statistics", "payroll", "parking-rates", "otherExpenses"];
        // Only show warning when navigating TO otherRevenue and there are unsaved changes in any tab except otherRevenue itself
        if (
            value === "otherRevenue" &&
            (statisticsDirty || parkingRatesDirty || payrollDirty || otherExpensesDirty)
        ) {
            setShowOtherRevenueUnsavedModal(true);
            setPendingTabChange(value);
            return;
        }
        handleSetActiveTab(value);

        // If switching to Other Revenue from any other tab, remember the existing timePeriod and switch to MONTHLY
        if (value === "otherRevenue") {
            previousTimePeriod.current = timePeriod;
            handleTimePeriodChange(TimeRangeType.MONTHLY);
        }

        // If leaving Other Revenue, restore the previously remembered timePeriod (if any)
        if (forecastTabs.includes(value)) {
            if (previousTimePeriod.current !== null) {
                handleTimePeriodChange(previousTimePeriod.current);
                previousTimePeriod.current = null;
            }
        }
    };

    // Handler for confirming navigation to Other Revenue (lose changes)
    const handleConfirmOtherRevenueTabChange = () => {
        if (pendingTabChange) {
            handleSetActiveTab(pendingTabChange);
            if (pendingTabChange === "otherRevenue") {
                handleTimePeriodChange(TimeRangeType.MONTHLY);
            }
        }
        setShowOtherRevenueUnsavedModal(false);
        setPendingTabChange(null);
        // Clear dirty flags for all except otherRevenue
        setStatisticsDirty(false);
        setParkingRatesDirty(false);
        setPayrollDirty(false);
        setOtherExpensesDirty(false);
    };

    // Handler for cancelling navigation to Other Revenue
    const handleCancelOtherRevenueTabChange = () => {
        setShowOtherRevenueUnsavedModal(false);
        setPendingTabChange(null);
    };

    // Loader state for Save and Continue in Other Revenue modal
    const [isOtherRevenueSaveLoading, setIsOtherRevenueSaveLoading] = useState(false);

    // Handler for saving before navigating to Other Revenue
    const handleSaveAndGoToOtherRevenue = async () => {
        setIsOtherRevenueSaveLoading(true);
        let savingToastId: string | undefined;
        let savingTimeout: NodeJS.Timeout | undefined;

        // Show "Saving..." toast immediately
        const savingToast = toast({
            title: "Saving...",
            description: "Your changes are being saved. Please wait.",
            variant: "default",
            duration: 10000,
        });
        savingToastId = savingToast?.id;

        try {
            // Save all dirty tabs except otherRevenue
            const savePromises: Promise<void>[] = [];
            if (statisticsDirty && statisticsRef.current) savePromises.push(statisticsRef.current.save());
            if (parkingRatesDirty && parkingRatesRef.current) savePromises.push(parkingRatesRef.current.save());
            if (payrollDirty && payrollRef.current) savePromises.push(payrollRef.current.save());
            if (otherExpensesDirty && otherExpensesRef.current) savePromises.push(otherExpensesRef.current.save());
            await Promise.all(savePromises);
            setStatisticsDirty(false);
            setParkingRatesDirty(false);
            setPayrollDirty(false);
            setOtherExpensesDirty(false);
            // Success toast
            toast({
                title: "Saved successfully",
                description: "All changes have been saved.",
                variant: "default",
            });
            // Now navigate
            handleConfirmOtherRevenueTabChange();
        } catch (err) {
            toast({
                title: "Save failed",
                description: "Some changes could not be saved. Please try again.",
                variant: "destructive",
            });
        } finally {
            setIsOtherRevenueSaveLoading(false);
            if (savingTimeout) clearTimeout(savingTimeout);
            if (savingToastId) dismiss(savingToastId);
        }
    };

    const handleTotalRoomsChange = (rooms: number) => {
        setTotalRooms(rooms);
    };

    const canAccessForecast = userRoles.includes('accountManager') || userRoles.includes('districtManager');

    return (
        <div className="p-4 flex flex-col md:flex-row gap-6 w-full">
            <SidebarContainer
                customers={customers}
                isLoadingCustomers={isLoadingCustomers}
                error={error}
                selectedSite={selectedSite}
                setSelectedSite={handleSiteChange}
                totalRooms={totalRooms}
                startingMonth={startingMonth}
                setStartingMonth={handlePeriodChange}
                timePeriod={timePeriod}
                setTimePeriod={handleTimePeriodChange}

                activeTab={activeTab}
                isExpanded={isSidebarExpanded}
                onExpandedChange={setIsSidebarExpanded}
                isSidebarDisabled={
                    (activeTab === "statistics" && isStatisticsLoading) ||
                    (activeTab === "parking-rates" && isParkingRatesLoading) ||
                    (activeTab === "payroll" && isPayrollLoading) ||
                    (activeTab === "otherRevenue" && isOtherRevenueLoading)
                }
            />
            <div className="transition-all duration-300 ease-in-out grow overflow-x-hidden">
                {canAccessForecast && (
                    <>
                        <Tabs value={activeTab} onValueChange={handleTabChange} data-qa-id="tabs-forecasts">
                            <TabsList data-qa-id="tabsList-forecasts">
                                <TabsTrigger value="statistics" data-qa-id="tab-statistics">Parking Statistics</TabsTrigger>
                                <TabsTrigger value="payroll" data-qa-id="tab-payroll">Payroll Expense</TabsTrigger>
                                <TabsTrigger value="parking-rates" data-qa-id="tab-parkingRates">Parking Rates</TabsTrigger>
                                <TabsTrigger value="otherExpenses" data-qa-id="tab-otherExpenses">Other Expense</TabsTrigger>
                                <TabsTrigger value="otherRevenue" data-qa-id="tab-otherRevenue">Other Revenue</TabsTrigger>
                            </TabsList>
                        </Tabs>
                        {/* Global Save Button */}
                        <div className="flex justify-end mb-4">
                            <Button
                                onClick={handleGlobalSave}
                                disabled={isSaveAllButtonDisabled}
                                variant="default"
                                data-qa-id="button-global-save"
                            >
                                {saveOperationId ? "Saving..." : "Save All"}
                            </Button>
                        </div>
                        {/* (Removed: Still saving notification under dialog) */}
                        <div style={{ display: activeTab === 'statistics' ? 'block' : 'none' }}>
                            <SiteStatisticsForm
                                ref={statisticsRef}
                                customers={customers}
                                isLoadingCustomers={isLoadingCustomers}
                                error={error}
                                selectedSite={selectedSite}
                                setSelectedSite={handleSiteChange}
                                onTotalRoomsChange={handleTotalRoomsChange}
                                startingMonth={startingMonth}
                                timePeriod={timePeriod}
                                isGuideOpen={isStatisticsGuideOpen}
                                setIsGuideOpen={setIsStatisticsGuideOpen}
                                hasUnsavedChanges={statisticsDirty}
                                setHasUnsavedChanges={setStatisticsDirty}
                                onLoadingChange={setIsStatisticsLoading}
                            />
                        </div>
                        <div style={{ display: activeTab === 'parking-rates' ? 'block' : 'none' }}>
                            <ParkingRateForm
                                ref={parkingRatesRef}
                                customers={customers}
                                error={error}
                                isParkingRateGuideExpanded={isParkingRateGuideExpanded}
                                setIsParkingRateGuideExpanded={setIsParkingRateGuideExpanded}
                                selectedSite={selectedSite}
                                startingMonth={startingMonth}
                                hasUnsavedChanges={parkingRatesDirty}
                                setHasUnsavedChanges={setParkingRatesDirty}
                                onLoadingChange={setIsParkingRatesLoading}
                                onParkingRatesSaved={handleParkingRatesSaved}
                            />
                        </div>
                        <div style={{ display: activeTab === 'payroll' ? 'block' : 'none' }}>
                            <PayrollForecast
                                ref={payrollRef}
                                customers={customers}
                                error={error}
                                selectedSite={selectedSite}
                                startingMonth={startingMonth}
                                isGuideOpen={isPayrollGuideOpen}
                                setIsGuideOpen={setIsPayrollGuideOpen}
                                hasUnsavedChanges={payrollDirty}
                                setHasUnsavedChanges={setPayrollDirty}
                                onLoadingChange={setIsPayrollLoading}
                                onContractDetailsChange={setContractDetails}
                            />
                        </div>
                        <div style={{ display: activeTab === 'otherRevenue' ? 'block' : 'none' }}>
                            <OtherRevenue
                                ref={otherRevenueRef}
                                customers={customers}
                                selectedSite={selectedSite}
                                startingMonth={startingMonth}
                                isGuideOpen={isOtherRevenueGuideOpen}
                                setIsGuideOpen={setIsOtherRevenueGuideOpen}
                                hasUnsavedChanges={otherRevenueDirty}
                                setHasUnsavedChanges={setOtherRevenueDirty}
                                onLoadingChange={setIsOtherRevenueLoading}
                            />
                        </div>
                        <div style={{ display: activeTab === 'otherExpenses' ? 'block' : 'none' }}>
                            <OtherExpenses
                                ref={otherExpensesRef}
                                customers={customers}
                                selectedSite={selectedSite}
                                startingMonth={startingMonth}
                                isGuideOpen={isOtherExpensesGuideOpen}
                                setIsGuideOpen={setIsOtherExpensesGuideOpen}
                                hasUnsavedChanges={otherExpensesDirty}
                                setHasUnsavedChanges={setOtherExpensesDirty}
                                onLoadingChange={setIsOtherExpensesLoading}
                                contractDetails={contractDetails}
                            />
                        </div>
                        <Dialog open={showUnsavedModal} onOpenChange={setShowUnsavedModal}>
                            <DialogContent>
                                <DialogHeader>
                                    <DialogTitle>You have unsaved changes</DialogTitle>
                                    <DialogDescription>
                                        Your changes will be lost if you continue. Do you want to proceed?
                                    </DialogDescription>
                                </DialogHeader>
                                <DialogFooter>
                                    <Button variant="outline" onClick={handleCancelSidebarChange}>Cancel</Button>
                                    <Button onClick={handleConfirmSidebarChange}>Continue</Button>
                                </DialogFooter>
                            </DialogContent>
                        </Dialog>
                        {/* Modal for navigating to Other Revenue with unsaved changes */}
                        <Dialog open={showOtherRevenueUnsavedModal} onOpenChange={setShowOtherRevenueUnsavedModal}>
                            <DialogContent>
                                <DialogHeader>
                                    <DialogTitle>Unsaved Changes Detected</DialogTitle>
                                    <DialogDescription>
                                        You have unsaved changes in another tab. If you proceed to Other Revenue, your updates will be lost. Would you like to save your changes before continuing?
                                    </DialogDescription>
                                </DialogHeader>
                                <DialogFooter>
                                    <Button variant="outline" onClick={handleCancelOtherRevenueTabChange}>Cancel</Button>
                                    <Button variant="secondary" onClick={handleConfirmOtherRevenueTabChange}>Proceed Without Saving</Button>
                                    <Button onClick={handleSaveAndGoToOtherRevenue} disabled={isOtherRevenueSaveLoading}>
                                        {isOtherRevenueSaveLoading ? "Saving..." : "Save and Continue"}
                                    </Button>
                                </DialogFooter>
                            </DialogContent>
                        </Dialog>
                    </>
                )}
            </div>
        </div>
    );
};
