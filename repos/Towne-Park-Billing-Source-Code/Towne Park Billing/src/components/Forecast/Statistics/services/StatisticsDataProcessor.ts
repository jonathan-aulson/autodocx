import {
    FormValuesByDate,
    SiteStatisticData,
    SiteStatisticDetailData,
    DetailIds,
    TimeRangeType
} from "@/lib/models/Statistics";
import { syncOccupancyAndOccupiedRooms, getDaysInMonth } from "../lib/helpers";

export interface ProcessedMonthData {
    forecastValues: FormValuesByDate;
    budgetValues: FormValuesByDate;
    actualValues: FormValuesByDate;
    forecastDetailIds: DetailIds;
    budgetDetailIds: DetailIds;
    initialForecastValues: Record<string, Record<string, number>>;
}

export class StatisticsDataProcessor {
    static createEmptyMonthsData(
        customerSiteId: string, 
        startingMonth: string, 
        timePeriod: TimeRangeType
    ): SiteStatisticData[] {
        const [year, month] = startingMonth.split('-').map(Number);
        const emptyMonthsData: SiteStatisticData[] = [];

        for (let i = 0; i < 3; i++) {
            const currentDate = new Date(year, month - 1 + i, 1);
            const currentYear = currentDate.getFullYear();
            const currentMonth = currentDate.getMonth() + 1;
            const periodLabel = `${currentYear}-${String(currentMonth).padStart(2, '0')}`;

            emptyMonthsData.push({
                siteStatisticId: "",
                customerSiteId: customerSiteId,
                name: "",
                siteNumber: "",
                totalRooms: 0,
                timeRangeType: timePeriod,
                periodLabel: periodLabel,
                budgetData: [],
                forecastData: [],
                actualData: []
            });
        }

        return emptyMonthsData;
    }

    static processMonthStatisticsData(
        data: SiteStatisticData, 
        timePeriod: TimeRangeType, 
        availableRooms: number
    ): ProcessedMonthData {
        // Generate all periods for the selected time range
        let periodKeys: string[] = [];
        let periodLabels: Record<string, string> = {};

        if (!data.periodLabel) {
            return {
                forecastValues: {},
                budgetValues: {},
                actualValues: {},
                forecastDetailIds: {},
                budgetDetailIds: {},
                initialForecastValues: {}
            };
        }

        const [yearStr, monthStr] = data.periodLabel.split("-");
        const year = Number(yearStr);
        const month = Number(monthStr) - 1;

        if (timePeriod === "DAILY") {
            const days = getDaysInMonth(year, month);
            periodKeys = days.map(d => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`);
            days.forEach(d => {
                const dayName = d.toLocaleDateString(undefined, { weekday: "short" });
                const monthName = d.toLocaleDateString(undefined, { month: "short" });
                periodLabels[`${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`] =
                    `${dayName}\n${monthName} ${d.getDate()}`;
            });
        } else if (timePeriod === "WEEKLY") {
            // Use API-defined weeks only
            const weekPeriods: Record<string, string> = {};
            const addWeeksFrom = (arr?: SiteStatisticDetailData[]) => {
                if (!arr) return;
                arr.forEach(item => {
                    if (item.periodStart && item.periodLabel) {
                        weekPeriods[item.periodStart] = item.periodLabel;
                    }
                });
            };
            addWeeksFrom(data.budgetData);
            addWeeksFrom(data.forecastData);
            addWeeksFrom(data.actualData);
            periodKeys = Object.keys(weekPeriods);
            periodLabels = { ...weekPeriods };
        } else if (timePeriod === "MONTHLY") {
            // For monthly data, collect all period keys from budget and forecast data
            const monthlyPeriods: Record<string, string> = {};
            const addMonthlyPeriods = (arr?: SiteStatisticDetailData[]) => {
                if (!arr) return;
                arr.forEach(item => {
                    if (item.periodStart && item.periodLabel) {
                        monthlyPeriods[item.periodStart] = item.periodLabel;
                    }
                });
            };
            addMonthlyPeriods(data.budgetData);
            addMonthlyPeriods(data.forecastData);
            addMonthlyPeriods(data.actualData);
            
            if (Object.keys(monthlyPeriods).length > 0) {
                periodKeys = Object.keys(monthlyPeriods);
                periodLabels = { ...monthlyPeriods };
            } else {
                // Fallback to the original logic if no periods found
                periodKeys = [data.periodLabel];
                const [year, month] = data.periodLabel.split("-");
                const date = new Date(Number(year), Number(month) - 1, 1);
                periodLabels[data.periodLabel] = date.toLocaleDateString(undefined, { month: "long", year: "numeric" });
            }
        }

        const zeroStats = {
            "occupancy": 0,
            "occupied-rooms": 0,
            "valet-daily": 0,
            "valet-monthly": 0,
            "self-daily": 0,
            "self-monthly": 0,
            "valet-comps": 0,
            "self-comps": 0,
            "self-aggregator": 0,
            "valet-aggregator": 0,
            "drive-in-ratio-input": 0,
            "capture-ratio-input": 0,
            "external-revenue": 0
        };

        const newBudgetValues: FormValuesByDate = {};
        const newForecastValues: FormValuesByDate = {};
        const newActualValues: FormValuesByDate = {};
        const newBudgetDetailIds: DetailIds = {};
        const newForecastDetailIds: DetailIds = {};


        // Use existing fillValues logic
        const normalizeDailyPeriodLabel = (label: string | null | undefined): string | null => {
            if (!label || timePeriod !== "DAILY") return label || null;
            return label.replace(/(\d+)\/(\d+)\/(\d+)/, (match, month, day, year) => {
                const m = month.padStart(2, '0');
                const d = day.padStart(2, '0');
                return `${year}-${m}-${d}`;
            });
        };

        const getKey = (item: SiteStatisticDetailData) => {
            if (timePeriod === "DAILY") {
                return normalizeDailyPeriodLabel(item.periodStart) || normalizeDailyPeriodLabel(item.periodLabel) || "";
            } else if (timePeriod === "MONTHLY") {
                // For monthly data, normalize to YYYY-MM format to ensure consistency
                const periodStart = item.periodStart || "";
                const periodLabel = item.periodLabel || "";
                
                let result = "";
                // If periodStart is in YYYY-MM-DD format, extract YYYY-MM
                if (periodStart.match(/^\d{4}-\d{2}-\d{2}$/)) {
                    result = periodStart.substring(0, 7); // Extract "YYYY-MM" from "YYYY-MM-DD"
                }
                // If periodLabel is in YYYY-MM format, use it
                else if (periodLabel.match(/^\d{4}-\d{2}$/)) {
                    result = periodLabel;
                }
                // Fallback to original logic
                else {
                    result = periodStart || periodLabel;
                }
                
                return result;
            } else {
                return item.periodStart || item.periodLabel || "";
            }
        };

        const fillValues = (arr: SiteStatisticDetailData[] | undefined, target: FormValuesByDate, detailIds: DetailIds) => {
            if (!arr) return;
            arr.forEach(item => {
                const key = getKey(item);
                if (key) {
                    const values = {
                        "occupancy": item.occupancy || 0,
                        "occupied-rooms": item.occupiedRooms || 0,
                        "valet-daily": item.valetDaily || 0,
                        "valet-monthly": item.valetMonthly || 0,
                        "self-daily": item.selfDaily || 0,
                        "self-monthly": item.selfMonthly || 0,
                        "valet-comps": item.valetComps || 0,
                        "self-comps": item.selfComps || 0,
                        "self-aggregator": item.selfAggregator || 0,
                        "valet-aggregator": item.valetAggregator || 0,
                        "drive-in-ratio-input": item.driveInRatio ? item.driveInRatio / 100 : 0,
                        "capture-ratio-input": item.captureRatio ? item.captureRatio / 100 : 0,
                        "external-revenue": item.externalRevenue || 0
                    };

                    // Check if this is actual data (target === newActualValues)
                    // If all values are zero, it means no actual data exists yet, so skip it
                    const isActualData = target === newActualValues;
                    if (isActualData) {
                        const allValuesAreZero = Object.values(values).every(val => Number(val) === 0);
                        if (allValuesAreZero) {
                            // Skip adding this entry - treat as missing actual data
                            return;
                        }
                    }

                    target[key] = values;
                    if (item.siteStatisticDetailId) {
                        detailIds[key] = item.siteStatisticDetailId;
                    }
                }
            });
        };

        fillValues(data.budgetData, newBudgetValues, newBudgetDetailIds);
        fillValues(data.forecastData, newForecastValues, newForecastDetailIds);
        fillValues(data.actualData, newActualValues, {});

        // Ensure all periods are present and initialized with correct logic
        periodKeys.forEach(key => {
            if (!newBudgetValues[key]) newBudgetValues[key] = { ...zeroStats };
            if (!newForecastValues[key]) {
                if (newBudgetValues[key]) {
                    newForecastValues[key] = { ...newBudgetValues[key] };
                } else {
                    newForecastValues[key] = { ...zeroStats };
                }
            }
        });

        const syncedForecastValues = syncOccupancyAndOccupiedRooms(newForecastValues, data.totalRooms || availableRooms);
        const syncedBudgetValues = syncOccupancyAndOccupiedRooms(newBudgetValues, data.totalRooms || availableRooms);

        return {
            forecastValues: syncedForecastValues,
            budgetValues: syncedBudgetValues,
            actualValues: newActualValues,
            forecastDetailIds: newForecastDetailIds,
            budgetDetailIds: newBudgetDetailIds,
            initialForecastValues: syncedForecastValues
        };
    }

    static processAllMonthsData(
        dataArr: SiteStatisticData[], 
        timePeriod: TimeRangeType, 
        availableRooms: number
    ): Record<number, ProcessedMonthData> {
        const processedMonths: Record<number, ProcessedMonthData> = {};

        // Process each month's data
        dataArr.forEach((monthData, monthIndex) => {
            if (monthIndex >= 3) return; // Only process first 3 months

            // Process this month's data using existing logic
            processedMonths[monthIndex] = this.processMonthStatisticsData(monthData, timePeriod, availableRooms);
        });

        return processedMonths;
    }
} 