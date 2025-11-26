import { FormValuesByDate, SiteStatisticDetailData } from "@/lib/models/Statistics";
import React from "react";
import { OVERNIGHT_ADJUSTMENT_RATE } from "./constants";

export function getDaysInMonth(year: number, month: number) {
    const date = new Date(year, month, 1);
    const days = [];
    while (date.getMonth() === month) {
        days.push(new Date(date));
        date.setDate(date.getDate() + 1);
    }
    return days;
}

export function syncOccupancyAndOccupiedRooms(
    values: FormValuesByDate,
    availableRooms: number
): FormValuesByDate {
    const newValues: FormValuesByDate = {};
    Object.entries(values).forEach(([periodKey, periodVals]) => {
        const occupiedRooms = periodVals["occupied-rooms"];
        const occupancy = periodVals["occupancy"];
        
        // Handle edge case: availableRooms = 0
        if (availableRooms <= 0) {
            newValues[periodKey] = {
                ...periodVals,
                "occupied-rooms": 0,
                "occupancy": 0,
            };
            return;
        }
        
        // Check if values are missing (undefined) vs explicitly set to 0
        const hasOccupiedRooms = occupiedRooms !== undefined;
        const hasOccupancy = occupancy !== undefined;
        const occupiedRoomsValue = occupiedRooms ?? 0;
        const occupancyValue = occupancy ?? 0;
        
        // Enhanced bidirectional synchronization
        if (hasOccupancy && occupancyValue === 0) {
            // When occupancy is explicitly set to 0, set both to 0 for consistency
            newValues[periodKey] = {
                ...periodVals,
                "occupied-rooms": 0,
                "occupancy": 0,
            };
        } else if (hasOccupiedRooms && occupiedRoomsValue === 0) {
            // When occupied rooms is explicitly set to 0, set both to 0 for consistency
            newValues[periodKey] = {
                ...periodVals,
                "occupied-rooms": 0,
                "occupancy": 0,
            };
        } else if (hasOccupiedRooms && hasOccupancy && occupiedRoomsValue > 0 && occupancyValue > 0) {
            // Both values exist and are valid - keep as is
            newValues[periodKey] = { ...periodVals };
        } else {
            // Handle missing values - calculate from the other
            if (hasOccupiedRooms && occupiedRoomsValue > 0 && !hasOccupancy) {
                // Calculate occupancy from occupied rooms when occupancy is missing
                newValues[periodKey] = {
                    ...periodVals,
                    occupancy: occupiedRoomsValue / availableRooms,
                };
            } else if (hasOccupancy && occupancyValue > 0 && !hasOccupiedRooms) {
                // Calculate occupied rooms from occupancy when occupied rooms is missing
                newValues[periodKey] = {
                    ...periodVals,
                    "occupied-rooms": Math.round(occupancyValue * availableRooms),
                };
            } else {
                // Both are zero, missing, or invalid - set both to 0
                newValues[periodKey] = {
                    ...periodVals,
                    "occupied-rooms": 0,
                    "occupancy": 0,
                };
            }
        }
    });
    
    return newValues;
}

export function formatPeriodLabelForDisplay(label: string): React.ReactNode {
    if (!label) return "";
    const match = label.match(/^([A-Za-z]+)\s+(.+)$/);
    if (match) {
        const month = match[1];
        const rest = match[2];
        const shortMonth = getShortMonthName(month);
        return React.createElement('div', {}, [
            shortMonth,
            React.createElement('br', { key: 'period-br' }),
            rest
        ]);
    }
    return label;
}

export function getShortMonthName(month: string): string {
    const months = [
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December"
    ];
    const shortMonths = [
        "Jan", "Feb", "Mar", "Apr", "May", "Jun",
        "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
    ];
    const idx = months.findIndex(m => m.toLowerCase() === month.toLowerCase());
    if (idx !== -1) return shortMonths[idx];
    const idxShort = shortMonths.findIndex(m => m.toLowerCase() === month.toLowerCase());
    if (idxShort !== -1) return shortMonths[idxShort];
    return month;
}

export function formatPercentage(value: number) {
    return `${(value * 100).toFixed(2)}%`;
}

export function formatDateKey(date: Date) {
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
}

export function calculateOvernight(
    periodStart: string,
    type: "self" | "valet",
    formValues: FormValuesByDate,
    inputType: string,
    availableRooms: number,
    getOccupiedRooms: (periodStart: string) => number
) {
    const values = formValues[periodStart] || {};

    const driveInRatio = values["drive-in-ratio-input"] || 0;
    const captureRatio = values["capture-ratio-input"] || 0;
    const occupiedRooms = inputType === "occupied-rooms" ? values["occupied-rooms"] || 0 : getOccupiedRooms(periodStart);

    if (type === "self" && captureRatio >= 1) return 0;
    const totalOvernight = driveInRatio * occupiedRooms;
    return type === "self" 
        ? totalOvernight * (1 - captureRatio) 
        : totalOvernight * captureRatio;
}

// Backward compatibility exports
export function calculateOvernightSelf(
    periodStart: string,
    formValues: FormValuesByDate,
    inputType: string,
    availableRooms: number,
    getOccupiedRooms: (periodStart: string) => number
) {
    return calculateOvernight(periodStart, "self", formValues, inputType, availableRooms, getOccupiedRooms);
}

export function calculateOvernightValet(
    periodStart: string,
    formValues: FormValuesByDate,
    inputType: string,
    availableRooms: number,
    getOccupiedRooms: (periodStart: string) => number
) {
    return calculateOvernight(periodStart, "valet", formValues, inputType, availableRooms, getOccupiedRooms);
}

export function calculateActualOvernight(
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

export function calculateOccupancy(
    periodStart: string,
    formValues: FormValuesByDate,
    availableRooms: number
) {
    const values = formValues[periodStart] || {};

    const occupiedRooms = values["occupied-rooms"] || 0;
    if (availableRooms === 0) return 0;

    return occupiedRooms / availableRooms;
}

export function getOccupiedRooms(
    periodStart: string,
    formValues: FormValuesByDate,
    availableRooms: number
) {
    const values = formValues[periodStart] || {};

    const occupancy = values["occupancy"] || 0;
    return occupancy * availableRooms;
}

export function calculateExternalRevenue(
    periodStart: string,
    formValues: FormValuesByDate,
    budgetRatesByPeriod: Record<string, any>,
    calculateOvernightSelfFn: (periodStart: string) => number,
    calculateOvernightValetFn: (periodStart: string) => number,
    selectedSite: string
) {
    if (!selectedSite) return 0;

    const values = formValues[periodStart] || {};

    const rates = budgetRatesByPeriod[periodStart] || {
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
    };

    const {
        valetRateDaily,
        valetRateMonthly,
        valetRateOvernight,
        selfRateDaily,
        selfRateMonthly,
        selfRateOvernight,
    } = rates;

    const valetDaily = values["valet-daily"] || 0;
    const valetMonthly = values["valet-monthly"] || 0;
    const valetOvernight = calculateOvernightValetFn(periodStart);
    const selfDaily = values["self-daily"] || 0;
    const selfMonthly = values["self-monthly"] || 0;
    const selfOvernight = calculateOvernightSelfFn(periodStart);

    const valetDailyRevenue = valetDaily * valetRateDaily;
    const valetMonthlyRevenue = valetMonthly * valetRateMonthly;
    const valetOvernightRevenue = valetOvernight * valetRateOvernight;
    const selfDailyRevenue = selfDaily * selfRateDaily;
    const selfMonthlyRevenue = selfMonthly * selfRateMonthly;
    const selfOvernightRevenue = selfOvernight * selfRateOvernight;

    const totalRevenue =
        valetDailyRevenue +
        valetMonthlyRevenue +
        valetOvernightRevenue +
        selfDailyRevenue +
        selfMonthlyRevenue +
        selfOvernightRevenue;

    return Math.round(totalRevenue);
}

export function calculateExternalRevenueForPeriod(periodData: SiteStatisticDetailData) {
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
        selfOvernight = 0
    } = periodData;

    const valetDailyRevenue = valetDaily * valetRateDaily;
    const valetMonthlyRevenue = valetMonthly * valetRateMonthly;
    const valetOvernightRevenue = valetOvernight * valetRateOvernight;
    const selfDailyRevenue = selfDaily * selfRateDaily;
    const selfMonthlyRevenue = selfMonthly * selfRateMonthly;
    const selfOvernightRevenue = selfOvernight * selfRateOvernight;

    const totalRevenue =
        valetDailyRevenue +
        valetMonthlyRevenue +
        valetOvernightRevenue +
        selfDailyRevenue +
        selfMonthlyRevenue +
        selfOvernightRevenue;

    return Math.round(totalRevenue);
}

// Variance calculation utilities
export function calculateVariancePercentage(actual: number, forecast: number): number {
    if (forecast === undefined || forecast === null) {
        return 0;
    }
    if (forecast === 0) {
        // When forecast is 0, return 100% if actual > 0, -100% if actual < 0
        return actual > 0 ? 100 : (actual < 0 ? -100 : 0);
    }
    return ((actual - forecast) / forecast) * 100;
}

export function hasCompleteActualData(actualValues: FormValuesByDate, periodKey: string): boolean {
    const periodActuals = actualValues[periodKey];
    if (!periodActuals) return false;
    
    // Check if we have actual data for key statistics
    const hasOccupancy = periodActuals["occupancy"] !== undefined && periodActuals["occupancy"] !== null;
    const hasOccupiedRooms = periodActuals["occupied-rooms"] !== undefined && periodActuals["occupied-rooms"] !== null;
    
    // Consider complete if we have either occupancy or occupied rooms data
    return hasOccupancy || hasOccupiedRooms;
}

export function shouldShowVarianceIndicator(actual: number, forecast: number): boolean {
    // Don't show if either value is missing or invalid
    if (actual === undefined || actual === null || 
        forecast === undefined || forecast === null) {
        return false;
    }
    
    // Don't show if there's no meaningful variance
    if (actual === forecast) {
        return false;
    }
    
    return true;
}

export function getVarianceColor(actual: number, forecast: number): 'positive' | 'negative' | 'neutral' {
    if (!shouldShowVarianceIndicator(actual, forecast)) {
        return 'neutral';
    }
    
    return actual > forecast ? 'positive' : 'negative';
}

// Simplified current period detection
export function isCurrentPeriod(
    periodKey: string,
    timePeriod: "DAILY" | "WEEKLY" | "MONTHLY" | "QUARTERLY",
    currentDate: Date = new Date()
): boolean {
    const today = new Date();
    const todayStr = today.toISOString().split('T')[0]; // YYYY-MM-DD format
    
    switch (timePeriod) {
        case "DAILY":
            return periodKey === todayStr;
        case "WEEKLY":
            // Simple week check - if today falls within this week period
            return isDateInWeekPeriod(today, periodKey);
        case "MONTHLY":
            // Simple month check - if today falls within this month period
            return isDateInMonthPeriod(today, periodKey);
        case "QUARTERLY":
            // Quarterly support can be added later if needed
            return false;
        default:
            return false;
    }
}

function isDateInWeekPeriod(date: Date, weekPeriodKey: string): boolean {
    // Week period detection for format like "2025-07-07" to "2025-07-13"
    // weekPeriodKey is the start date of the week (Monday)
    const weekStartDate = new Date(weekPeriodKey);
    const weekEndDate = new Date(weekStartDate);
    weekEndDate.setDate(weekEndDate.getDate() + 6); // Add 6 days to get Sunday
    
    // Check if current date falls within this week
    const currentDate = new Date(date.getFullYear(), date.getMonth(), date.getDate());
    const startDate = new Date(weekStartDate.getFullYear(), weekStartDate.getMonth(), weekStartDate.getDate());
    const endDate = new Date(weekEndDate.getFullYear(), weekEndDate.getMonth(), weekEndDate.getDate());
    
    return currentDate >= startDate && currentDate <= endDate;
}

function isDateInMonthPeriod(date: Date, monthPeriodKey: string): boolean {
    // Simplified month period detection
    // monthPeriodKey format is typically like "2025-07" or "July 2025"
    const year = date.getFullYear();
    const month = date.getMonth() + 1; // getMonth() returns 0-11
    const monthKey = `${year}-${month.toString().padStart(2, '0')}`;
    return monthPeriodKey === monthKey;
}

function getWeekNumber(date: Date): number {
    const firstDayOfYear = new Date(date.getFullYear(), 0, 1);
    const pastDaysOfYear = (date.getTime() - firstDayOfYear.getTime()) / 86400000;
    return Math.ceil((pastDaysOfYear + firstDayOfYear.getDay() + 1) / 7);
} 