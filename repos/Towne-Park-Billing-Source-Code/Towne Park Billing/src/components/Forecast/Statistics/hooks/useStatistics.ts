import { useState, useEffect, useRef } from "react";
import { useToast } from "@/components/ui/use-toast";
import {
    Customer,
    DetailIds,
    FormValuesByDate,
    SiteStatisticData,
    TimeRangeType
} from "@/lib/models/Statistics";
import { StatisticsDataProcessor } from "../services/StatisticsDataProcessor";
import { StatisticsDataManager } from "../services/StatisticsDataManager";

interface UseStatisticsDataProps {
    selectedSite: string;
    startingMonth: string;
    timePeriod: TimeRangeType;
    onTotalRoomsChange?: (rooms: number) => void;
    onLoadingChange?: (loading: boolean) => void;
}

export function useStatisticsData({
    selectedSite,
    startingMonth,
    timePeriod,
    onTotalRoomsChange,
    onLoadingChange
}: UseStatisticsDataProps) {
    const [isLoadingStatistics, setIsLoadingStatistics] = useState(false);
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

    const { toast } = useToast();

    useEffect(() => {
        if (onLoadingChange) {
            onLoadingChange(isLoadingStatistics);
        }
    }, [isLoadingStatistics, onLoadingChange]);

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
    };

    return {
        isLoadingStatistics,
        availableRooms,
        siteStatisticId,
        allMonthsData,
        monthlyForecastValues,
        monthlyBudgetValues,
        monthlyActualValues,
        monthlyForecastDetailIds,
        monthlyBudgetDetailIds,
        monthlyInitialForecastValues,
        setMonthlyForecastValues,
        setMonthlyInitialForecastValues
    };
}

interface UseStatisticsFormProps {
    hasUnsavedChanges: boolean;
    setHasUnsavedChanges: (dirty: boolean) => void;
    monthlyForecastValues: Record<number, FormValuesByDate>;
    monthlyBudgetValues: Record<number, FormValuesByDate>;
    monthlyActualValues: Record<number, FormValuesByDate>;
    monthlyForecastDetailIds: Record<number, DetailIds>;
    monthlyBudgetDetailIds: Record<number, DetailIds>;
    monthlyInitialForecastValues: Record<number, Record<string, Record<string, number>>>;
    currentMonthIndex: number;
    allMonthsData: SiteStatisticData[];
    showingBudget: boolean;
}

export function useStatisticsForm({
    hasUnsavedChanges,
    setHasUnsavedChanges,
    monthlyForecastValues,
    monthlyBudgetValues,
    monthlyActualValues,
    monthlyForecastDetailIds,
    monthlyBudgetDetailIds,
    monthlyInitialForecastValues,
    currentMonthIndex,
    allMonthsData,
    showingBudget
}: UseStatisticsFormProps) {
    // Current display state (derived from current month)
    const [initialForecastValues, setInitialForecastValues] = useState<Record<string, Record<string, number>>>({});
    const [budgetRatesByPeriod, setBudgetRatesByPeriod] = useState<Record<string, any>>({});
    const [selectedPeriod, setSelectedPeriod] = useState<string>("");
    const [inputType, setInputType] = useState<string>("occupancy");
    const [formValues, setFormValues] = useState<FormValuesByDate>({});
    const [initialized, setInitialized] = useState<boolean>(false);
    const [budgetValues, setBudgetValues] = useState<FormValuesByDate>({});
    const [forecastValues, setForecastValues] = useState<FormValuesByDate>({});
    const [actualValues, setActualValues] = useState<FormValuesByDate>({});
    const [budgetDetailIds, setBudgetDetailIds] = useState<DetailIds>({});
    const [forecastDetailIds, setForecastDetailIds] = useState<DetailIds>({});

    // Current month index for pagination (0, 1, 2)
    const [currentMonthIndexState, setCurrentMonthIndex] = useState<number>(0);

    const deepCompare = (obj1: any, obj2: any): boolean => {
        return JSON.stringify(obj1) === JSON.stringify(obj2);
    };

    useEffect(() => {
        if (Object.keys(initialForecastValues).length > 0 || Object.keys(forecastValues).length > 0) {
            setHasUnsavedChanges(!deepCompare(forecastValues, initialForecastValues));
        } else {
            setHasUnsavedChanges(false);
        }
    }, [forecastValues, initialForecastValues, setHasUnsavedChanges]);

    useEffect(() => {
        if (!initialized) {
            const today = new Date();
            const currentPeriod = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, "0")}`;
            setSelectedPeriod(currentPeriod);
            setInitialized(true);
        }
    }, [initialized]);

    // Update current month display from stored month data
    const updateCurrentMonthDisplay = (monthIndex: number): void => {
        if (monthIndex < 0 || monthIndex >= 3) return;

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

        // Update budget rates for current month
        if (allMonthsData[monthIndex]) {
            const ratesByPeriod: Record<string, any> = {};
            allMonthsData[monthIndex].budgetData?.forEach(dayData => {
                const key = dayData.periodStart || dayData.periodLabel || "";
                if (key) {
                    ratesByPeriod[key] = {
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
                    };
                }
            });
            setBudgetRatesByPeriod(ratesByPeriod);
        }
    };

    // Update current display when currentMonthIndex changes (local pagination)
    useEffect(() => {
        if (allMonthsData.length > 0 && currentMonthIndex >= 0 && currentMonthIndex < 3) {
            updateCurrentMonthDisplay(currentMonthIndex);
        }
    }, [currentMonthIndex, allMonthsData, showingBudget]);

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

    return {
        initialForecastValues,
        budgetRatesByPeriod,
        selectedPeriod,
        inputType,
        formValues,
        initialized,
        budgetValues,
        forecastValues,
        actualValues,
        budgetDetailIds,
        forecastDetailIds,
        currentMonthIndexState,
        setInitialForecastValues,
        setSelectedPeriod,
        setInputType,
        setFormValues,
        setInitialized,
        setBudgetValues,
        setForecastValues,
        setActualValues,
        setBudgetDetailIds,
        setForecastDetailIds,
        setCurrentMonthIndex,
        updateCurrentMonthDisplay
    };
} 