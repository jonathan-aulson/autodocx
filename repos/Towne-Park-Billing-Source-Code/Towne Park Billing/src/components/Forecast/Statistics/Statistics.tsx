import React, { useEffect, useMemo, useRef, useState, forwardRef, useImperativeHandle } from "react";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";

import { useToast } from "@/components/ui/use-toast";
import { useSpreadsheetNavigation } from "@/hooks/useSpreadsheetNavigation";
import {
    ALL_STATISTICS,
    Customer,
    DetailIds,
    FormValuesByDate,
    SiteStatisticData,
    SiteStatisticDetailData,
    TimeRangeType
} from "@/lib/models/Statistics";
import { isPeriodInPast, isDateBeforeToday } from "@/lib/utils/dateUtils";
import { Eye, EyeOff } from 'lucide-react';

import { OVERNIGHT_ADJUSTMENT_RATE } from "./lib/constants";
import { MonthPagination } from "./components/MonthPagination";
import { StatisticsGuide } from "./components/StatisticsGuide";
import { StatisticsControls } from "./components/StatisticsControls";
import { ViewModeRadioGroup } from "../ViewModeRadioGroup";
import { StatisticsDataProcessor } from "./services/StatisticsDataProcessor";
import { StatisticsDataManager } from "./services/StatisticsDataManager";
import { StatisticsCalculations } from "./lib/calculations";
import { usePeriodEntries } from "./hooks/usePeriodEntries";
import { StatisticsTableComponent } from "./components/StatisticsTable";

interface SiteStatisticsFormProps {
    customers: Customer[];
    isLoadingCustomers: boolean;
    error: string | null;
    selectedSite: string;
    setSelectedSite: (site: string) => void;
    onTotalRoomsChange?: (rooms: number) => void;
    startingMonth: string;
    timePeriod: TimeRangeType;
    isGuideOpen: boolean;
    setIsGuideOpen: (value: boolean) => void;
    hasUnsavedChanges: boolean;
    setHasUnsavedChanges: (dirty: boolean) => void;
    onLoadingChange?: (loading: boolean) => void;
}


function SiteStatisticsForm(
    {
        customers,
        isLoadingCustomers,
        error,
        selectedSite,
        setSelectedSite,
        onTotalRoomsChange,
        startingMonth,
        timePeriod,
        isGuideOpen,
        setIsGuideOpen,
        hasUnsavedChanges,
        setHasUnsavedChanges,
        onLoadingChange
    }: SiteStatisticsFormProps,
    ref: React.Ref<any>
) {
    const [isLoadingStatistics, setIsLoadingStatistics] = useState(false);
    const [isSaving, setIsSaving] = useState(false);

    useEffect(() => {
        if (onLoadingChange) {
            onLoadingChange(isLoadingStatistics || isSaving);
        }
    }, [isLoadingStatistics, isSaving, onLoadingChange]);

    const [availableRooms, setAvailableRooms] = useState<number>(0);
    const [siteStatisticId, setSiteStatisticId] = useState<string>("");

    // Store all 3 months of data
    const [allMonthsData, setAllMonthsData] = useState<SiteStatisticData[]>([]);

    // Separate state for each month's edits (keyed by month index: 0, 1, 2)
    const [monthlyForecastValues, setMonthlyForecastValues] = useState<Record<number, FormValuesByDate>>({});
    const [monthlyBudgetValues, setMonthlyBudgetValues] = useState<Record<number, FormValuesByDate>>({});
    const [monthlyActualValues, setMonthlyActualValues] = useState<Record<number, FormValuesByDate>>({});
    const [monthlyForecastDetailIds, setMonthlyForecastDetailIds] = useState<Record<number, DetailIds>>({});
    const [monthlyBudgetDetailIds, setMonthlyBudgetDetailIds] = useState<Record<number, DetailIds>>({});
    const [monthlyInitialForecastValues, setMonthlyInitialForecastValues] = useState<Record<number, Record<string, Record<string, number>>>>({});

    // Current display state (derived from current month)
    const [initialForecastValues, setInitialForecastValues] = useState<Record<string, Record<string, number>>>({});
    const [budgetRatesByPeriod, setBudgetRatesByPeriod] = useState<Record<string, Pick<SiteStatisticDetailData,
        'valetRateDaily' |
        'valetRateMonthly' |
        'valetRateOvernight' |
        'selfRateDaily' |
        'selfRateMonthly' |
        'selfRateOvernight' |
        'baseRevenue' |
        'selfOvernight' |
        'valetOvernight' |
        'selfAggregator' |
        'valetAggregator' |
        'adjustmentPercentage'
    >>>({});
    const [forecastRatesByPeriod, setForecastRatesByPeriod] = useState<Record<string, Pick<SiteStatisticDetailData,
        'valetRateDaily' |
        'valetRateMonthly' |
        'valetRateOvernight' |
        'selfRateDaily' |
        'selfRateMonthly' |
        'selfRateOvernight' |
        'baseRevenue' |
        'selfOvernight' |
        'valetOvernight' |
        'selfAggregator' |
        'valetAggregator' |
        'adjustmentPercentage'
    >>>({});
    const [actualRatesByPeriod, setActualRatesByPeriod] = useState<Record<string, Pick<SiteStatisticDetailData,
        'valetRateDaily' |
        'valetRateMonthly' |
        'valetRateOvernight' |
        'selfRateDaily' |
        'selfRateMonthly' |
        'selfRateOvernight' |
        'baseRevenue' |
        'selfOvernight' |
        'valetOvernight' |
        'selfAggregator' |
        'valetAggregator' |
        'adjustmentPercentage'
    >>>({});

    const [selectedPeriod, setSelectedPeriod] = useState<string>("");
    const [inputType, setInputType] = useState<string>("occupancy");
    const [formValues, setFormValues] = useState<FormValuesByDate>({});
    const [initialized, setInitialized] = useState<boolean>(false);
    const [isPastPeriod, setIsPastPeriod] = useState<boolean>(false);

    const [viewMode, setViewMode] = useState<'flash' | 'budget' | 'priorYear'>('flash');

    // Derived state for backward compatibility
    const showingBudget = viewMode === 'budget';
    const showingFlash = viewMode === 'flash';
    const showingPriorYear = viewMode === 'priorYear';

    const [budgetValues, setBudgetValues] = useState<FormValuesByDate>({});
    const [forecastValues, setForecastValues] = useState<FormValuesByDate>({});
    const [actualValues, setActualValues] = useState<FormValuesByDate>({});
    const [budgetDetailIds, setBudgetDetailIds] = useState<DetailIds>({});
    const [forecastDetailIds, setForecastDetailIds] = useState<DetailIds>({});
    const saveOperationRef = useRef<string | null>(null);

    const suppressDirtyCheckRef = useRef(false);

    // Current month index for pagination (0, 1, 2)
    const [currentMonthIndex, setCurrentMonthIndex] = useState<number>(0);

    const { toast } = useToast();

    const tableRef = useRef<HTMLTableElement>(null);

    const deepCompare = (obj1: any, obj2: any): boolean => {
        return JSON.stringify(obj1) === JSON.stringify(obj2);
    };

    // Check if there are unsaved changes across all months
    const checkForUnsavedChanges = (): boolean => {
        for (let monthIndex = 0; monthIndex < 3; monthIndex++) {
            const monthForecastValues = monthlyForecastValues[monthIndex] || {};
            const monthInitialForecastValues = monthlyInitialForecastValues[monthIndex] || {};

            // Check if this month has any data
            const hasData = Object.keys(monthForecastValues).length > 0 || Object.keys(monthInitialForecastValues).length > 0;

            if (hasData && !deepCompare(monthForecastValues, monthInitialForecastValues)) {
                return true; // Found unsaved changes in this month
            }
        }
        return false; // No unsaved changes found in any month
    };

    useEffect(() => {
        if (suppressDirtyCheckRef.current) {
            suppressDirtyCheckRef.current = false;
            return;
        }
        const hasChanges = checkForUnsavedChanges();
        setHasUnsavedChanges(hasChanges);
    }, [monthlyForecastValues, monthlyInitialForecastValues, setHasUnsavedChanges]);

    useEffect(() => {
        if (!initialized) {
            const today = new Date();
            const currentPeriod = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, "0")}`;
            setSelectedPeriod(currentPeriod);
            setIsPastPeriod(false);
            setInitialized(true);
        }
    }, [initialized]);

    useEffect(() => {
        setIsPastPeriod(isPeriodInPast(selectedPeriod));
    }, [selectedPeriod, isPastPeriod, isPeriodInPast]);

    useEffect(() => {
        setSelectedPeriod(startingMonth);
        setCurrentMonthIndex(0); // Reset month index when starting month changes
    }, [startingMonth]);

    // Reset pagination when site changes and clear monthly data
    useEffect(() => {
        setCurrentMonthIndex(0);
        // Clear monthly data when site changes to prevent stale data
        setMonthlyForecastValues({});
        setMonthlyBudgetValues({});
        setMonthlyActualValues({});
        setMonthlyForecastDetailIds({});
        setMonthlyBudgetDetailIds({});
        setMonthlyInitialForecastValues({});
        // Explicitly reset unsaved changes flag when switching sites
        setHasUnsavedChanges(false);
        saveOperationRef.current = null;
    }, [selectedSite, setHasUnsavedChanges]);

    // Update selectedPeriod when currentMonthIndex changes
    useEffect(() => {
        if (!startingMonth) return;

        const [year, month] = startingMonth.split('-').map(Number);
        const newDate = new Date(year, month - 1 + currentMonthIndex, 1);
        const newYear = newDate.getFullYear();
        const newMonth = newDate.getMonth() + 1;
        const newPeriod = `${newYear}-${String(newMonth).padStart(2, '0')}`;

        setSelectedPeriod(newPeriod);
    }, [startingMonth, currentMonthIndex]);

    // Main data fetching effect - fetch all 3 months upfront
    useEffect(() => {
        if (!selectedSite || !startingMonth) return;

        async function fetchAllMonthsStatistics() {
            setIsLoadingStatistics(true);

            try {
                const dataArr = await StatisticsDataManager.fetchAllMonthsStatistics(selectedSite, startingMonth, timePeriod);
                setAllMonthsData(dataArr);
                processAllMonthsData(dataArr);
            } catch (err) {
                console.error('Failed to fetch statistics:', err);
                toast({
                    title: "Error",
                    description: "Failed to load statistics data. Please try again later.",
                    variant: "destructive"
                });
                // Create empty data on error
                const emptyMonthsData = StatisticsDataProcessor.createEmptyMonthsData(selectedSite, startingMonth, timePeriod);
                setAllMonthsData(emptyMonthsData);
                processAllMonthsData(emptyMonthsData);
            } finally {
                setIsLoadingStatistics(false);
            }
        }

        fetchAllMonthsStatistics();
    }, [selectedSite, startingMonth, timePeriod]);

    // Update current display when currentMonthIndex changes (local pagination)
    useEffect(() => {
        if (allMonthsData.length > 0 && currentMonthIndex >= 0 && currentMonthIndex < 3) {
            updateCurrentMonthDisplay(currentMonthIndex);
        }
    }, [currentMonthIndex, allMonthsData, showingBudget]);



    // Process all 3 months of data and store in separate state
    const processAllMonthsData = (dataArr: SiteStatisticData[]): void => {
        const newMonthlyForecastValues: Record<number, FormValuesByDate> = {};
        const newMonthlyBudgetValues: Record<number, FormValuesByDate> = {};
        const newMonthlyActualValues: Record<number, FormValuesByDate> = {};
        const newMonthlyForecastDetailIds: Record<number, DetailIds> = {};
        const newMonthlyBudgetDetailIds: Record<number, DetailIds> = {};
        const newMonthlyInitialForecastValues: Record<number, Record<string, Record<string, number>>> = {};

        // Process each month's data
        dataArr.forEach((monthData, monthIndex) => {
            if (monthIndex >= 3) return; // Only process first 3 months

            // Set site-level info from first month
            if (monthIndex === 0) {
                if (monthData.totalRooms !== undefined && monthData.totalRooms !== null) {
                    setAvailableRooms(monthData.totalRooms);
                    if (onTotalRoomsChange) {
                        onTotalRoomsChange(monthData.totalRooms);
                    }
                }
                setSiteStatisticId(monthData.siteStatisticId || "");
            }

            // Process this month's data using existing logic
            const processedData = StatisticsDataProcessor.processMonthStatisticsData(monthData, timePeriod, availableRooms);

            newMonthlyForecastValues[monthIndex] = processedData.forecastValues;
            newMonthlyBudgetValues[monthIndex] = processedData.budgetValues;
            newMonthlyActualValues[monthIndex] = processedData.actualValues;
            newMonthlyForecastDetailIds[monthIndex] = processedData.forecastDetailIds;
            newMonthlyBudgetDetailIds[monthIndex] = processedData.budgetDetailIds;
            newMonthlyInitialForecastValues[monthIndex] = processedData.initialForecastValues;
        });

        // Update state
        setMonthlyForecastValues(newMonthlyForecastValues);
        setMonthlyBudgetValues(newMonthlyBudgetValues);
        setMonthlyActualValues(newMonthlyActualValues);
        setMonthlyForecastDetailIds(newMonthlyForecastDetailIds);
        setMonthlyBudgetDetailIds(newMonthlyBudgetDetailIds);
        setMonthlyInitialForecastValues(newMonthlyInitialForecastValues);

        // Set initial display to first month
        updateCurrentMonthDisplay(0);
        setInitialized(true);
    };

    // Update current month display from stored month data
    const updateCurrentMonthDisplay = (monthIndex: number): void => {
        if (monthIndex < 0 || monthIndex >= 3) return;

        // For MONTHLY view, combine data from all months instead of showing just one month
        if (timePeriod === "MONTHLY") {
            const combinedForecastValues: FormValuesByDate = {};
            const combinedBudgetValues: FormValuesByDate = {};
            const combinedActualValues: FormValuesByDate = {};
            const combinedForecastDetailIds: DetailIds = {};
            const combinedBudgetDetailIds: DetailIds = {};
            const combinedInitialForecastValues: Record<string, Record<string, number>> = {};

            // Combine data from all three months
            for (let i = 0; i < 3; i++) {
                const monthForecastValues = monthlyForecastValues[i] || {};
                const monthBudgetValues = monthlyBudgetValues[i] || {};
                const monthActualValues = monthlyActualValues[i] || {};
                const monthForecastDetailIds = monthlyForecastDetailIds[i] || {};
                const monthBudgetDetailIds = monthlyBudgetDetailIds[i] || {};
                const monthInitialForecastValues = monthlyInitialForecastValues[i] || {};

                Object.assign(combinedForecastValues, monthForecastValues);
                Object.assign(combinedBudgetValues, monthBudgetValues);
                Object.assign(combinedActualValues, monthActualValues);
                Object.assign(combinedForecastDetailIds, monthForecastDetailIds);
                Object.assign(combinedBudgetDetailIds, monthBudgetDetailIds);
                Object.assign(combinedInitialForecastValues, monthInitialForecastValues);
            }

            // Update current display state with combined data
            setForecastValues(combinedForecastValues);
            setBudgetValues(combinedBudgetValues);
            setActualValues(combinedActualValues);
            setForecastDetailIds(combinedForecastDetailIds);
            setBudgetDetailIds(combinedBudgetDetailIds);
            setInitialForecastValues(combinedInitialForecastValues);
            setFormValues(showingBudget ? combinedBudgetValues : combinedForecastValues);
        } else {
            // For DAILY/WEEKLY views, show only one month at a time (original logic)
            const monthForecastValues = monthlyForecastValues[monthIndex] || {};
            const monthBudgetValues = monthlyBudgetValues[monthIndex] || {};
            const monthActualValues = monthlyActualValues[monthIndex] || {};
            const monthForecastDetailIds = monthlyForecastDetailIds[monthIndex] || {};
            const monthBudgetDetailIds = monthlyBudgetDetailIds[monthIndex] || {};
            const monthInitialForecastValues = monthlyInitialForecastValues[monthIndex] || {};

            // Update current display state
            setForecastValues(monthForecastValues);
            setBudgetValues(monthBudgetValues);
            setActualValues(monthActualValues);
            setForecastDetailIds(monthForecastDetailIds);
            setBudgetDetailIds(monthBudgetDetailIds);
            setInitialForecastValues(monthInitialForecastValues);
            setFormValues(showingBudget ? monthBudgetValues : monthForecastValues);
        }

        // Update rates for current month - budget rates from budgetData, forecast rates from forecastData
        if (allMonthsData[monthIndex]) {
            // Extract budget rates
            const budgetRatesByPeriod: Record<string, any> = {};
            allMonthsData[monthIndex].budgetData?.forEach(dayData => {
                let key = dayData.periodStart || dayData.periodLabel || "";
                // Normalize key format for monthly data to match processed data
                if (timePeriod === "MONTHLY" && key.match(/^\d{4}-\d{2}-\d{2}$/)) {
                    key = key.substring(0, 7); // Extract "YYYY-MM" from "YYYY-MM-DD"
                }
                if (key) {
                    budgetRatesByPeriod[key] = {
                        valetRateDaily: dayData.valetRateDaily || 0,
                        valetRateMonthly: dayData.valetRateMonthly || 0,
                        valetRateOvernight: dayData.valetRateOvernight || 0,
                        selfRateDaily: dayData.selfRateDaily || 0,
                        selfRateMonthly: dayData.selfRateMonthly || 0,
                        selfRateOvernight: dayData.selfRateOvernight || 0,
                        baseRevenue: dayData.baseRevenue || 0,
                        selfOvernight: dayData.selfOvernight || 0,
                        valetOvernight: dayData.valetOvernight || 0,
                        selfAggregator: dayData.selfAggregator || 0,
                        valetAggregator: dayData.valetAggregator || 0,
                        adjustmentPercentage: dayData.adjustmentPercentage || 0,
                    };
                }
            });
            setBudgetRatesByPeriod(budgetRatesByPeriod);

            // Extract forecast rates from forecastData (updated by parking rates service)
            const forecastRatesByPeriod: Record<string, any> = {};

            allMonthsData[monthIndex].forecastData?.forEach((dayData, index) => {
                let key = dayData.periodStart || dayData.periodLabel || "";

                // Convert MM/dd/yyyy format to yyyy-MM-dd format for consistency
                if (key.match(/^\d{2}\/\d{2}\/\d{4}$/)) {
                    const [month, day, year] = key.split('/');
                    key = `${year}-${month.padStart(2, '0')}-${day.padStart(2, '0')}`;
                }

                // Normalize key format for monthly data to match processed data
                if (timePeriod === "MONTHLY" && key.match(/^\d{4}-\d{2}-\d{2}$/)) {
                    key = key.substring(0, 7); // Extract "YYYY-MM" from "YYYY-MM-DD"
                }
                if (key) {
                    const rates = {
                        valetRateDaily: dayData.valetRateDaily || 0,
                        valetRateMonthly: dayData.valetRateMonthly || 0,
                        valetRateOvernight: dayData.valetRateOvernight || 0,
                        selfRateDaily: dayData.selfRateDaily || 0,
                        selfRateMonthly: dayData.selfRateMonthly || 0,
                        selfRateOvernight: dayData.selfRateOvernight || 0,
                        baseRevenue: dayData.baseRevenue || 0,
                        selfOvernight: dayData.selfOvernight || 0,
                        valetOvernight: dayData.valetOvernight || 0,
                        selfAggregator: dayData.selfAggregator || 0,
                        valetAggregator: dayData.valetAggregator || 0,
                        adjustmentPercentage: dayData.adjustmentPercentage || 0,
                    };
                    forecastRatesByPeriod[key] = rates;

                }
            });

            setForecastRatesByPeriod(forecastRatesByPeriod);

            // Extract actual rates from actualData
            const actualRatesByPeriod: Record<string, any> = {};

            allMonthsData[monthIndex].actualData?.forEach((dayData, index) => {
                let key = dayData.periodStart || dayData.periodLabel || "";

                // Convert MM/dd/yyyy format to yyyy-MM-dd format for consistency
                if (key.match(/^\d{2}\/\d{2}\/\d{4}$/)) {
                    const [month, day, year] = key.split('/');
                    key = `${year}-${month.padStart(2, '0')}-${day.padStart(2, '0')}`;
                }

                // Normalize key format for monthly data to match processed data
                if (timePeriod === "MONTHLY" && key.match(/^\d{4}-\d{2}-\d{2}$/)) {
                    key = key.substring(0, 7); // Extract "YYYY-MM" from "YYYY-MM-DD"
                }

                if (key) {
                    const rates = {
                        valetRateDaily: dayData.valetRateDaily || 0,
                        valetRateMonthly: dayData.valetRateMonthly || 0,
                        valetRateOvernight: dayData.valetRateOvernight || 0,
                        selfRateDaily: dayData.selfRateDaily || 0,
                        selfRateMonthly: dayData.selfRateMonthly || 0,
                        selfRateOvernight: dayData.selfRateOvernight || 0,
                        baseRevenue: dayData.baseRevenue || 0,
                        selfOvernight: dayData.selfOvernight || 0,
                        valetOvernight: dayData.valetOvernight || 0,
                        selfAggregator: dayData.selfAggregator || 0,
                        valetAggregator: dayData.valetAggregator || 0,
                        adjustmentPercentage: dayData.adjustmentPercentage || 0,
                    };
                    actualRatesByPeriod[key] = rates;
                }
            });

            setActualRatesByPeriod(actualRatesByPeriod);
        }
    };



    useEffect(() => {
        const handleBeforeUnload = (e: BeforeUnloadEvent) => {
            if (hasUnsavedChanges) {
                e.preventDefault();
                e.returnValue = '';
                return '';
            }
        };

        window.addEventListener('beforeunload', handleBeforeUnload);
        return () => {
            window.removeEventListener('beforeunload', handleBeforeUnload);
        };
    }, [hasUnsavedChanges]);

    const toggleGuide = () => {
        setIsGuideOpen(!isGuideOpen);
    };

    const lastStatObjRef = useRef<SiteStatisticData | null>(null);

    // Update lastStatObjRef when current month changes
    useEffect(() => {
        if (allMonthsData.length > 0 && currentMonthIndex >= 0 && currentMonthIndex < 3) {
            lastStatObjRef.current = allMonthsData[currentMonthIndex];
        }
    }, [allMonthsData, currentMonthIndex]);

    const handleSaveStatistics = async () => {
        if (!selectedSite || !initialized || allMonthsData.length === 0) {
            toast({
                title: "Error",
                description: "Please select a customer site first.",
                variant: "destructive"
            });
            return;
        }


        const savingSite = selectedSite;
        saveOperationRef.current = savingSite;

        setIsSaving(true);

        try {
            const savedData = await StatisticsDataManager.saveStatistics(
                savingSite,
                allMonthsData,
                monthlyForecastValues,
                monthlyForecastDetailIds,
                monthlyBudgetDetailIds,
                availableRooms,
                timePeriod,
                calculateOvernightSelfForMonth,
                calculateOvernightValetForMonth,
                calculateExternalRevenueForMonth,
                customers,
                inputType
            );


            if (selectedSite === savingSite && saveOperationRef.current === savingSite) {
                // Update the initial values to the saved values for tracking changes
                setMonthlyInitialForecastValues(monthlyForecastValues);
                setInitialForecastValues(monthlyForecastValues[currentMonthIndex] || {});
                setHasUnsavedChanges(false);

                // Update local data with the saved response but preserve current form values
                setAllMonthsData(savedData);
                // Don't call processAllMonthsData here as it would overwrite current form values
                // The form values should remain as the user entered them

                toast({
                    title: "Success",
                    description: "All statistics saved successfully!"
                });
            }

        } catch (err) {
            console.error('Failed to save statistics:', err);


            if (selectedSite === savingSite && saveOperationRef.current === savingSite) {
                toast({
                    title: "Error",
                    description: "Failed to save statistics. Please try again.",
                    variant: "destructive"
                });
            }
        } finally {
            setIsSaving(false);

            if (saveOperationRef.current === savingSite) {
                saveOperationRef.current = null;
            }
        }
    };


    const refreshStatistics = async () => {
        if (!selectedSite || !startingMonth) return;

        setIsLoadingStatistics(true);
        try {
            const dataArr = await StatisticsDataManager.fetchAllMonthsStatistics(selectedSite, startingMonth, timePeriod);
            setAllMonthsData(dataArr);
            processAllMonthsData(dataArr);
        } catch (err) {
            console.error('Failed to refresh statistics:', err);
            toast({
                title: "Error",
                description: "Failed to refresh statistics data. Please try again later.",
                variant: "destructive"
            });
            // Create empty data on error
            const emptyMonthsData = StatisticsDataProcessor.createEmptyMonthsData(selectedSite, startingMonth, timePeriod);
            setAllMonthsData(emptyMonthsData);
            processAllMonthsData(emptyMonthsData);
        } finally {
            setIsLoadingStatistics(false);
        }
    };

    useImperativeHandle(ref, () => ({
        save: handleSaveStatistics,
        refresh: refreshStatistics
    }));

    const handleInputTypeChange = (value: string) => {
        applyInputTypeChange(value);
    };

    const applyInputTypeChange = (value: string) => {
        setInputType(value);
        setBudgetValues({ ...budgetValues });
        setForecastValues({ ...forecastValues });
        setFormValues(showingBudget ? { ...budgetValues } : { ...forecastValues });
    };

    const toggleBudgetForecast = () => {
        const newViewMode = viewMode === 'budget' ? 'flash' : 'budget';
        setViewMode(newViewMode);
        
        const newShowingBudget = newViewMode === 'budget';
        
        if (newShowingBudget) {
            setFormValues({ ...budgetValues });
        } else {
            setFormValues({ ...forecastValues });
        }
    };

    const handleViewModeChange = (mode: 'flash' | 'budget' | 'priorYear') => {
        setViewMode(mode);
        
        const newShowingBudget = mode === 'budget';
        
        if (newShowingBudget) {
            setFormValues({ ...budgetValues });
        } else {
            setFormValues({ ...forecastValues });
        }
    };

    const handleInputChange = (periodStart: string, statId: string, value: string) => {
        const numValue = value === "" ? 0 : Number(value);
        
        // Create base update object
        const baseUpdate = {
            [periodStart]: {
                ...(formValues[periodStart] || {}),
                [statId]: numValue,
            },
        };
        
        // Handle bidirectional occupancy/occupied-rooms relationship
        if (statId === "occupancy") {
            // When occupancy is changed, also update occupied rooms
            const occupiedRooms = Math.round(availableRooms * numValue);
            baseUpdate[periodStart]["occupied-rooms"] = occupiedRooms;
        } else if (statId === "occupied-rooms") {
            // When occupied rooms is changed, also update occupancy
            const occupancy = availableRooms > 0 ? numValue / availableRooms : 0;
            baseUpdate[periodStart]["occupancy"] = occupancy;
        }
        
        // Update all state objects with synchronized values
        setFormValues(prev => ({
            ...prev,
            ...baseUpdate
        }));
        
        if (timePeriod === "DAILY" && !showingBudget) {
            setForecastValues(prevForecastValues => ({
                ...prevForecastValues,
                ...baseUpdate
            }));
            
            setMonthlyForecastValues(prevMonthlyValues => ({
                ...prevMonthlyValues,
                [currentMonthIndex]: {
                    ...(prevMonthlyValues[currentMonthIndex] || {}),
                    ...baseUpdate
                }
            }));
        }
        
        // Add budget mode synchronization
        if (timePeriod === "DAILY" && showingBudget) {
            setBudgetValues(prevBudgetValues => ({
                ...prevBudgetValues,
                ...baseUpdate
            }));
            
            setMonthlyBudgetValues(prevMonthlyValues => ({
                ...prevMonthlyValues,
                [currentMonthIndex]: {
                    ...(prevMonthlyValues[currentMonthIndex] || {}),
                    ...baseUpdate
                }
            }));
        }
    };


    // Helper function for month-specific calculations  
    const calculateOvernightForMonth = (monthIndex: number, periodStart: string, type: "self" | "valet") => {
        return StatisticsCalculations.calculateOvernightForMonth(monthIndex, periodStart, type, monthlyForecastValues, inputType, availableRooms);
    };

    // Backward compatibility helpers
    const calculateOvernightSelfForMonth = (monthIndex: number, periodStart: string) =>
        StatisticsCalculations.calculateOvernightSelfForMonth(monthIndex, periodStart, monthlyForecastValues, inputType, availableRooms);

    const calculateOvernightValetForMonth = (monthIndex: number, periodStart: string) =>
        StatisticsCalculations.calculateOvernightValetForMonth(monthIndex, periodStart, monthlyForecastValues, inputType, availableRooms);

    const calculateExternalRevenueForMonth = (monthIndex: number, periodStart: string) => {
        return StatisticsCalculations.calculateExternalRevenueForMonth(
            monthIndex,
            periodStart,
            selectedSite,
            monthlyForecastValues,
            allMonthsData,
            availableRooms,
            inputType,
            showingBudget,
            timePeriod
        );
    };

    const getOccupiedRoomsForMonth = (monthIndex: number, periodStart: string) => {
        return StatisticsCalculations.getOccupiedRoomsForMonth(monthIndex, periodStart, monthlyForecastValues, availableRooms);
    };



    const calculateOvernightSelf = (periodStart: string) => {
        return StatisticsCalculations.calculateOvernightSelf(periodStart, formValues, inputType, availableRooms);
    };

    const calculateOvernightValet = (periodStart: string) => {
        return StatisticsCalculations.calculateOvernightValet(periodStart, formValues, inputType, availableRooms);
    };

    const calculateActualOvernight = (periodStart: string, type: "self" | "valet") => {
        return StatisticsCalculations.calculateActualOvernight(periodStart, type, actualValues);
    };

    const calculateOccupancy = (periodStart: string) => {
        return StatisticsCalculations.calculateOccupancy(periodStart, formValues, availableRooms);
    };

    const getOccupiedRooms = (periodStart: string) => {
        return StatisticsCalculations.getOccupiedRooms(periodStart, formValues, availableRooms);
    };

    const calculateExternalRevenue = (periodStart: string) => {
        // Determine if the period is in the past (should use actual rates)
        const periodDate = new Date(periodStart + 'T00:00:00');
        const today = new Date();
        today.setHours(0, 0, 0, 0); // Reset time to start of day for accurate comparison
        const isPastDate = periodDate < today;

        let ratesByPeriod: Record<string, any>;
        let ratesSource: string;

        if (isPastDate && Object.keys(actualRatesByPeriod).length > 0) {
            // For past dates, try to use actual rates first
            const actualRates = actualRatesByPeriod[periodStart];
            if (actualRates) {
                ratesByPeriod = actualRatesByPeriod;
                ratesSource = "actual";
            } else {
                // Fall back to forecast/budget if no actual rates available
                ratesByPeriod = showingBudget ? budgetRatesByPeriod : forecastRatesByPeriod;
                ratesSource = showingBudget ? "budget" : "forecast";
            }
        } else {
            // For future dates, use forecast or budget rates
            ratesByPeriod = showingBudget ? budgetRatesByPeriod : forecastRatesByPeriod;
            ratesSource = showingBudget ? "budget" : "forecast";
        }

        const rates = ratesByPeriod[periodStart];
        
        return StatisticsCalculations.calculateExternalRevenue(
            periodStart,
            selectedSite,
            formValues,
            budgetRatesByPeriod,
            inputType,
            availableRooms,
            forecastRatesByPeriod,
            actualRatesByPeriod,
            showingBudget
        );
    };


    const periodEntries = usePeriodEntries(
        timePeriod,
        selectedPeriod,
        currentMonthIndex,
        allMonthsData,
        showingBudget,
        budgetValues,
        forecastValues,
        actualValues
    );

    const isFieldModified = (periodStart: string, statId: string) => {
        if (showingBudget) {
            return false;
        }

        const budgetValue = budgetValues[periodStart]?.[statId] || 0;
        const forecastValue = forecastValues[periodStart]?.[statId] || 0;
        return forecastValue !== budgetValue;
    };

    // Spreadsheet navigation configuration
    const spreadsheetNavigation = useSpreadsheetNavigation({
        tableRef,
        rowCountCallback: () => periodEntries.length,
        columnCountCallback: () => 11, // Fixed number of input columns
        isCellNavigableCallback: (rowIndex: number, colIndex: number) => {
            return true;
        },
        isCellEditableCallback: (rowIndex: number, colIndex: number) => {
            if (!isPastPeriod && showingFlash && timePeriod === "DAILY" && periodEntries[rowIndex]) {
                const entry = periodEntries[rowIndex];
                return !isDateBeforeToday(entry.rowKey);
            }
            return false;
        },
        onCellActivate: (rowIndex: number, colIndex: number, cellElement: HTMLElement | null) => {
            if (cellElement) {
                const input = cellElement.querySelector('input');
                if (input) {
                    input.focus({ preventScroll: true });
                    input.select();
                }
            }
        },
        onCellEditRequest: (rowIndex: number, colIndex: number) => {
            const cellElement = tableRef.current?.querySelector(`#cell-${rowIndex}-${colIndex}`);
            if (cellElement) {
                const input = cellElement.querySelector('input') as HTMLInputElement;
                if (input) {
                    input.focus({ preventScroll: true });
                    input.select();
                }
            }
        },
        onCellSubmit: (rowIndex: number, colIndex: number) => {
            const cellElement = tableRef.current?.querySelector(`#cell-${rowIndex}-${colIndex}`);
            if (cellElement) {
                const input = cellElement.querySelector('input') as HTMLInputElement;
                if (input) {
                    input.blur();
                }
            }
        },
        onCellCancel: (rowIndex: number, colIndex: number) => {
            const cellElement = tableRef.current?.querySelector(`#cell-${rowIndex}-${colIndex}`);
            if (cellElement) {
                const input = cellElement.querySelector('input') as HTMLInputElement;
                if (input) {
                    input.blur();
                }
            }
        }
    });

    return (
        <div className="mx-auto p-1 space-y-6">
            <div className="flex justify-between items-start mb-4">
                <div>
                    <h1 className="text-2xl font-bold tracking-tight">Parking Statistics</h1>
                    <p className="text-muted-foreground">Input customer site statistics for the properties you manage.</p>
                </div>
            </div>
            <StatisticsGuide
                isGuideOpen={isGuideOpen}
                toggleGuide={toggleGuide}
                error={error}
            />

            {timePeriod !== "DAILY" && (
                <div className="mb-4">
                    <Alert>
                        <AlertDescription>
                            <strong>Note:</strong> Editing and saving statistics is only available in the <b>Daily View</b> time period. Other views are read-only.
                        </AlertDescription>
                    </Alert>
                </div>
            )}
            <Card>
                <CardHeader className="flex flex-row items-center justify-between">
                    <CardTitle>Statistics for Selected Dates</CardTitle>
                    <div className="flex flex-col gap-4 items-end">
                        <RadioGroup
                            value={inputType}
                            onValueChange={handleInputTypeChange}
                            className="flex gap-4"
                            disabled={!selectedSite || !selectedPeriod || isLoadingStatistics}
                            data-qa-id="radio-group-input-type"
                        >
                            <div className="flex items-center space-x-2">
                                <RadioGroupItem value="occupancy" id="occupancy" data-qa-id="radio-occupancy" />
                                <Label htmlFor="occupancy">Percentage</Label>
                            </div>
                            <div className="flex items-center space-x-2">
                                <RadioGroupItem value="occupied-rooms" id="occupied-rooms" data-qa-id="radio-occupied-rooms" />
                                <Label htmlFor="occupied-rooms">Occupied Rooms</Label>
                            </div>
                        </RadioGroup>
                        <ViewModeRadioGroup
                            viewMode={viewMode}
                            onViewModeChange={handleViewModeChange}
                            disabled={!selectedSite || !selectedPeriod || isLoadingStatistics}
                        />
                    </div>
                </CardHeader>
                <CardContent>
                    {(timePeriod === "DAILY" || timePeriod === "WEEKLY") && (
                        <div className="mb-2 flex justify-between items-center">
                            <MonthPagination
                                currentMonthIndex={currentMonthIndex}
                                setCurrentMonthIndex={setCurrentMonthIndex}
                                startingMonth={startingMonth}
                                hasUnsavedChanges={hasUnsavedChanges}
                                saveChanges={handleSaveStatistics}
                                timePeriod={timePeriod}
                            />
                        </div>
                    )}
                    {timePeriod === "DAILY" && allMonthsData.length > 0 && (
                        <div className="mb-2 text-center">
                            <h3 className="text-base font-medium text-foreground">
                                {(() => {
                                    const currentMonthData = allMonthsData[currentMonthIndex];
                                    if (currentMonthData && currentMonthData.periodLabel) {
                                        const [year, month] = currentMonthData.periodLabel.split('-');
                                        const date = new Date(Number(year), Number(month) - 1, 1);
                                        return date.toLocaleDateString(undefined, { month: 'long', year: 'numeric' });
                                    }
                                    return selectedPeriod ? (() => {
                                        const [year, month] = selectedPeriod.split('-');
                                        const date = new Date(Number(year), Number(month) - 1, 1);
                                        return date.toLocaleDateString(undefined, { month: 'long', year: 'numeric' });
                                    })() : '';
                                })()}
                            </h3>
                        </div>
                    )}
                    <div className="rounded-md border">
                        <div>
                            <StatisticsTableComponent
                                isLoadingStatistics={isLoadingStatistics}
                                selectedSite={selectedSite}
                                selectedPeriod={selectedPeriod}
                                inputType={inputType}
                                timePeriod={timePeriod}
                                viewMode={viewMode}
                                currentMonthIndex={currentMonthIndex}
                                allMonthsData={allMonthsData}
                                periodEntries={periodEntries}
                                formValues={formValues}
                                budgetValues={budgetValues}
                                forecastValues={forecastValues}
                                monthlyForecastValues={monthlyForecastValues}
                                actualValues={actualValues}
                                budgetRatesByPeriod={showingBudget ? budgetRatesByPeriod : forecastRatesByPeriod}
                                handleInputChange={handleInputChange}
                                isFieldModified={isFieldModified}
                                calculateOvernightSelf={calculateOvernightSelf}
                                calculateOvernightValet={calculateOvernightValet}
                                calculateActualOvernight={calculateActualOvernight}
                                calculateOccupancy={calculateOccupancy}
                                getOccupiedRooms={getOccupiedRooms}
                                calculateExternalRevenue={calculateExternalRevenue}
                                isPastPeriod={isPastPeriod}
                                isDateBeforeToday={isDateBeforeToday}
                                tableRef={tableRef}
                                spreadsheetNavigation={spreadsheetNavigation}
                                setFormValues={setFormValues}
                                setForecastValues={setForecastValues}
                                setMonthlyForecastValues={setMonthlyForecastValues}
                                setHasUnsavedChanges={setHasUnsavedChanges}
                                availableRooms={availableRooms}
                            />
                        </div>
                    </div>
                </CardContent>
            </Card >
        </div>

    );
}



export default forwardRef(SiteStatisticsForm);
