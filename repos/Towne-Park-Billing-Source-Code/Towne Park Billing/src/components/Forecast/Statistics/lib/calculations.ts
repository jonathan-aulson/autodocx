import { FormValuesByDate, SiteStatisticDetailData } from "@/lib/models/Statistics";
import { OVERNIGHT_ADJUSTMENT_RATE } from "./constants";

export class StatisticsCalculations {
    // Helper function for month-specific calculations  
    static calculateOvernightForMonth(
        monthIndex: number, 
        periodStart: string, 
        type: "self" | "valet",
        monthlyForecastValues: Record<number, FormValuesByDate>,
        inputType: string,
        availableRooms: number
    ) {
        const monthValues = monthlyForecastValues[monthIndex] || {};
        const values = monthValues[periodStart] || {};

        const driveInRatio = values["drive-in-ratio-input"] || 0;
        const captureRatio = values["capture-ratio-input"] || 0;
        const occupiedRooms = inputType === "occupied-rooms" ? 
            values["occupied-rooms"] || 0 : 
            this.getOccupiedRoomsForMonth(monthIndex, periodStart, monthlyForecastValues, availableRooms);

        if (type === "self" && captureRatio >= 1) return 0;
        const totalOvernight = driveInRatio * occupiedRooms;
        return type === "self" 
            ? totalOvernight * (1 - captureRatio) 
            : totalOvernight * captureRatio;
    }

    // Backward compatibility helpers
    static calculateOvernightSelfForMonth(
        monthIndex: number, 
        periodStart: string,
        monthlyForecastValues: Record<number, FormValuesByDate>,
        inputType: string,
        availableRooms: number
    ) {
        return this.calculateOvernightForMonth(monthIndex, periodStart, "self", monthlyForecastValues, inputType, availableRooms);
    }

    static calculateOvernightValetForMonth(
        monthIndex: number, 
        periodStart: string,
        monthlyForecastValues: Record<number, FormValuesByDate>,
        inputType: string,
        availableRooms: number
    ) {
        return this.calculateOvernightForMonth(monthIndex, periodStart, "valet", monthlyForecastValues, inputType, availableRooms);
    }

    static calculateExternalRevenueForMonth(
        monthIndex: number, 
        periodStart: string,
        selectedSite: string,
        monthlyForecastValues: Record<number, FormValuesByDate>,
        allMonthsData: any[],
        availableRooms: number,
        inputType: string,
        showingBudget?: boolean,
        timePeriod?: string
    ): { externalRevenue: number; adjustmentValue: number; adjustmentPercentage: number } {
        if (!selectedSite) return { externalRevenue: 0, adjustmentValue: 0, adjustmentPercentage: 0 };

        const monthValues = monthlyForecastValues[monthIndex] || {};
        const values = monthValues[periodStart] || {};
        const monthData = allMonthsData[monthIndex];

        if (!monthData) return { externalRevenue: 0, adjustmentValue: 0, adjustmentPercentage: 0 };

        // Normalize periodStart for matching - handle MONTHLY vs DAILY format differences
        let normalizedPeriodStart = periodStart;
        if (timePeriod === "MONTHLY" && periodStart.match(/^\d{4}-\d{2}-\d{2}$/)) {
            normalizedPeriodStart = periodStart.substring(0, 7); // Extract "YYYY-MM" from "YYYY-MM-DD"
        }

        // Determine if the period is in the past (should use actual rates)
        const periodDate = new Date(periodStart + 'T00:00:00');
        const today = new Date();
        today.setHours(0, 0, 0, 0);
        const isPastDate = periodDate < today;
        
        let rateItem: any;
        let ratesSource: string;
        
        // Helper function to normalize date format from MM/dd/yyyy to yyyy-MM-dd
        const normalizeMMddyyyyToYyyyMMdd = (dateStr: string): string => {
            if (!dateStr) return "";
            
            // Handle MM/dd/yyyy format (like "08/14/2025") -> "2025-08-14"
            const mmddyyyyMatch = dateStr.match(/^(\d{1,2})\/(\d{1,2})\/(\d{4})$/);
            if (mmddyyyyMatch) {
                const [, month, day, year] = mmddyyyyMatch;
                return `${year}-${month.padStart(2, '0')}-${day.padStart(2, '0')}`;
            }
            
            return dateStr; // Return as is if not in MM/dd/yyyy format
        };

        // Helper function to find rate item with both original and normalized period formats
        const findRateItem = (dataArray: any[]) => {
            return dataArray?.find((item: any) => {
                const itemPeriodStart = item.periodStart || "";
                const itemPeriodLabel = item.periodLabel || "";
                
                // Convert MM/dd/yyyy periodLabel to yyyy-MM-dd for comparison
                const normalizedItemPeriodLabel = normalizeMMddyyyyToYyyyMMdd(itemPeriodLabel);
                
                // Try to match with various combinations
                return (itemPeriodStart === periodStart) || 
                       (itemPeriodLabel === periodStart) ||
                       (itemPeriodStart === normalizedPeriodStart) || 
                       (itemPeriodLabel === normalizedPeriodStart) ||
                       (normalizedItemPeriodLabel === periodStart) ||
                       (normalizedItemPeriodLabel === normalizedPeriodStart);
            });
        };
        
        if (isPastDate && monthData.actualData?.length > 0) {
            // For past dates, try to use actual rates first
            rateItem = findRateItem(monthData.actualData);
            if (rateItem) {
                ratesSource = "actual";
            } else {
                // Fall back to forecast/budget if no actual rates available
                rateItem = showingBudget 
                    ? findRateItem(monthData.budgetData)
                    : findRateItem(monthData.forecastData);
                ratesSource = showingBudget ? "budget" : "forecast";
            }
        } else {
            // For future dates, use forecast or budget rates
            rateItem = showingBudget 
                ? findRateItem(monthData.budgetData)
                : findRateItem(monthData.forecastData);
            ratesSource = showingBudget ? "budget" : "forecast";
        }

        if (!rateItem) return { externalRevenue: 0, adjustmentValue: 0, adjustmentPercentage: 0 };

        const rates = {
            valetRateDaily: rateItem.valetRateDaily || 0,
            valetRateMonthly: rateItem.valetRateMonthly || 0,
            valetRateOvernight: rateItem.valetRateOvernight || 0,
            selfRateDaily: rateItem.selfRateDaily || 0,
            selfRateMonthly: rateItem.selfRateMonthly || 0,
            selfRateOvernight: rateItem.selfRateOvernight || 0,
            baseRevenue: rateItem.baseRevenue || 0,
        };

        const valetDaily = (values["valet-daily"] || 0);
        const valetMonthly = (values["valet-monthly"] || 0);
        const selfDaily = (values["self-daily"] || 0);
        const selfMonthly = (values["self-monthly"] || 0);

        const valetOvernight = this.calculateOvernightValetForMonth(monthIndex, periodStart, monthlyForecastValues, inputType, availableRooms);
        const selfOvernight = this.calculateOvernightSelfForMonth(monthIndex, periodStart, monthlyForecastValues, inputType, availableRooms);

        const valetDailyRevenue = valetDaily * rates.valetRateDaily;
        const valetMonthlyRevenue = valetMonthly * rates.valetRateMonthly;
        const valetOvernightRevenue = valetOvernight * rates.valetRateOvernight;
        const selfDailyRevenue = selfDaily * rates.selfRateDaily;
        const selfMonthlyRevenue = selfMonthly * rates.selfRateMonthly;
        const selfOvernightRevenue = selfOvernight * rates.selfRateOvernight;

        const grossRevenue =
            valetDailyRevenue +
            valetMonthlyRevenue +
            valetOvernightRevenue +
            selfDailyRevenue +
            selfMonthlyRevenue +
            selfOvernightRevenue;

        // Use adjustment percentage from the same data source as rates for consistency
        const adjustmentPercentage = rateItem?.adjustmentPercentage || 0;
        const finalExternalRevenue = grossRevenue * (1 + adjustmentPercentage);
        const adjustmentValue = grossRevenue * adjustmentPercentage; // adjustmentValue should still be grossRevenue * adjustmentPercentage

        return {
            externalRevenue: finalExternalRevenue,
            adjustmentValue: adjustmentValue,
            adjustmentPercentage: adjustmentPercentage
        };
    }

      // Calculate externalRevenue for non-daily (weekly, monthly) periods using aggregate period data
    static calculateExternalRevenueForPeriod(periodData: SiteStatisticDetailData) {
        if (!periodData) return 0;

        const {
            valetRateDaily = 0,
            valetRateMonthly = 0,
            valetRateOvernight = 0,
            selfRateDaily = 0,
            selfRateMonthly = 0,
            selfRateOvernight = 0,
            valetDaily = 0,
            valetMonthly = 0,
            valetOvernight = 0,
            selfDaily = 0,
            selfMonthly = 0,
            selfOvernight = 0,
            adjustmentPercentage = 0 
        } = periodData;

        const valetDailyRevenue = valetDaily * valetRateDaily;
        const valetMonthlyRevenue = valetMonthly * valetRateMonthly;
        const valetOvernightRevenue = valetOvernight * valetRateOvernight;
        const selfDailyRevenue = selfDaily * selfRateDaily;
        const selfMonthlyRevenue = selfMonthly * selfRateMonthly;
        const selfOvernightRevenue = selfOvernight * selfRateOvernight;

        const grossRevenue = 
            valetDailyRevenue +
            valetMonthlyRevenue +
            valetOvernightRevenue +
            selfDailyRevenue +
            selfMonthlyRevenue +
            selfOvernightRevenue;
     

        // Apply adjustment percentage
        const finalExternalRevenue = grossRevenue * (1 + adjustmentPercentage);
            
        return finalExternalRevenue;
    }

    static getOccupiedRoomsForMonth(
        monthIndex: number, 
        periodStart: string,
        monthlyForecastValues: Record<number, FormValuesByDate>,
        availableRooms: number
    ) {
        const monthValues = monthlyForecastValues[monthIndex] || {};
        const values = monthValues[periodStart] || {};

        const occupancy = values["occupancy"] || 0;
        return occupancy * availableRooms;
    }

    // Current period calculations (for daily view)
    static calculateOvernightSelf(
        periodStart: string,
        formValues: FormValuesByDate,
        inputType: string,
        availableRooms: number
    ) {
        const values = formValues[periodStart] || {};

        const driveInRatio = values["drive-in-ratio-input"] || 0;
        const captureRatio = values["capture-ratio-input"] || 0;
        const occupiedRooms = inputType === "occupied-rooms" ? 
            values["occupied-rooms"] || 0 : 
            this.getOccupiedRooms(periodStart, formValues, availableRooms);

        if (captureRatio >= 1) return 0;
        const totalOvernight = driveInRatio * occupiedRooms;
        return totalOvernight * (1 - captureRatio);
    }

    static calculateOvernightValet(
        periodStart: string,
        formValues: FormValuesByDate,
        inputType: string,
        availableRooms: number
    ) {
        const values = formValues[periodStart] || {};

        const driveInRatio = values["drive-in-ratio-input"] || 0;
        const captureRatio = values["capture-ratio-input"] || 0;
        const occupiedRooms = inputType === "occupied-rooms" ? 
            values["occupied-rooms"] || 0 : 
            this.getOccupiedRooms(periodStart, formValues, availableRooms);

        const totalOvernight = driveInRatio * occupiedRooms;
        return totalOvernight * captureRatio;
    }

    static calculateActualOvernight(
        periodStart: string, 
        type: "self" | "valet",
        actualValues: FormValuesByDate
    ) {
        if (!actualValues[periodStart]) return 0;

        const values = actualValues[periodStart];

        const driveInRatio = values["drive-in-ratio-input"] || 0;
        const captureRatio = values["capture-ratio-input"] || 0;
        const occupiedRooms = values["occupied-rooms"] || 0;

        if (type === "self" && captureRatio >= 1) return 0;

        const totalOvernight = driveInRatio * occupiedRooms;
        return type === "self"
            ? totalOvernight * (1 - captureRatio)
            : totalOvernight * captureRatio;
    }

    static calculateOccupancy(
        periodStart: string,
        formValues: FormValuesByDate,
        availableRooms: number
    ) {
        const values = formValues[periodStart] || {};
        
        const occupiedRooms = values["occupied-rooms"] || 0;
        if (availableRooms <= 0) return 0; // Handle edge case
        
        return occupiedRooms / availableRooms;
    }

    static getOccupiedRooms(
        periodStart: string,
        formValues: FormValuesByDate,
        availableRooms: number
    ) {
        const values = formValues[periodStart] || {};
        
        const occupancy = values["occupancy"] || 0;
        if (availableRooms <= 0) return 0; // Handle edge case
        
        // Bug 2633: Removed Math.Round causing wrong external revenue calculation for DAILY View
        return (occupancy * availableRooms);
    }

    static calculateExternalRevenue(
        periodStart: string,
        selectedSite: string,
        formValues: FormValuesByDate,
        budgetRatesByPeriod: Record<string, any>,
        inputType: string,
        availableRooms: number,
        forecastRatesByPeriod?: Record<string, any>,
        actualRatesByPeriod?: Record<string, any>,
        showingBudget?: boolean
    ) {
        if (!selectedSite) return 0;
        const values = formValues[periodStart] || {};

        // Determine if the period is in the past (should use actual rates)
        const periodDate = new Date(periodStart + 'T00:00:00');
        const today = new Date();
        today.setHours(0, 0, 0, 0); // Reset time to start of day for accurate comparison
        const isPastDate = periodDate < today;
        
        let ratesByPeriod: Record<string, any>;
        let ratesSource: string;
        
        if (isPastDate && actualRatesByPeriod && Object.keys(actualRatesByPeriod).length > 0) {
            // For past dates, try to use actual rates first
            const actualRates = actualRatesByPeriod[periodStart];
            if (actualRates) {
                ratesByPeriod = actualRatesByPeriod;
                ratesSource = "actual";
            } else {
                // Fall back to forecast/budget if no actual rates available
                ratesByPeriod = showingBudget ? budgetRatesByPeriod : (forecastRatesByPeriod || budgetRatesByPeriod);
                ratesSource = showingBudget ? "budget" : "forecast";
            }
        } else {
            // For future dates, use forecast or budget rates
            ratesByPeriod = showingBudget ? budgetRatesByPeriod : (forecastRatesByPeriod || budgetRatesByPeriod);
            ratesSource = showingBudget ? "budget" : "forecast";
        }

        const rates = ratesByPeriod[periodStart] || {
            valetRateDaily: 0,
            valetRateMonthly: 0,
            valetRateOvernight: 0,
            selfRateDaily: 0,
            selfRateMonthly: 0,
            selfRateOvernight: 0,
            baseRevenue: 0,
            selfOvernight: 0,
            valetOvernight: 0,
            selfAggregator: 0,
            valetAggregator: 0,
            adjustmentPercentage: 0,
        };

        const {
            valetRateDaily,
            valetRateMonthly,
            valetRateOvernight,
            selfRateDaily,
            selfRateMonthly,
            selfRateOvernight,
            baseRevenue,
        } = rates;

        const valetDaily = values["valet-daily"] || 0;
        const valetMonthly = values["valet-monthly"] || 0;
        const valetOvernight = this.calculateOvernightValet(periodStart, formValues, inputType, availableRooms);
        const selfDaily = values["self-daily"] || 0;
        const selfMonthly = values["self-monthly"] || 0;
        const selfOvernight = this.calculateOvernightSelf(periodStart, formValues, inputType, availableRooms);

        const valetDailyRevenue = valetDaily * valetRateDaily;
        const valetMonthlyRevenue = valetMonthly * valetRateMonthly;
        const valetOvernightRevenue = valetOvernight * valetRateOvernight;
        const selfDailyRevenue = selfDaily * selfRateDaily;
        const selfMonthlyRevenue = selfMonthly * selfRateMonthly;
        const selfOvernightRevenue = selfOvernight * selfRateOvernight;

        const grossRevenue =
            valetDailyRevenue +
            valetMonthlyRevenue +
            valetOvernightRevenue +
            selfDailyRevenue +
            selfMonthlyRevenue +
            selfOvernightRevenue;

        const adjustmentPercentage = rates.adjustmentPercentage || 0;
        const finalExternalRevenue = grossRevenue * (1 + adjustmentPercentage);
        return finalExternalRevenue;
    }


}
