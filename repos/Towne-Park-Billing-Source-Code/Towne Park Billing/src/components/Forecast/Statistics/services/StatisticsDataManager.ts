import {
    FormValuesByDate,
    SiteStatisticData,
    SiteStatisticDetailData,
    DetailIds,
    TimeRangeType,
    Customer
} from "@/lib/models/Statistics";
import { StatisticsDataProcessor } from "./StatisticsDataProcessor";

export class StatisticsDataManager {
    static async fetchAllMonthsStatistics(
        selectedSite: string,
        startingMonth: string,
        timePeriod: TimeRangeType
    ): Promise<SiteStatisticData[]> {
        const response = await fetch(`/api/siteStatistics/${selectedSite}/${startingMonth}?timeRange=${timePeriod}`);

        if (!response.ok) {
            throw new Error(`Error fetching statistics: ${response.status}`);
        }

        const dataArr = await response.json();

        if (Array.isArray(dataArr) && dataArr.length > 0) {
            return dataArr;
        } else {
            // Handle empty response - create empty data for 3 months
            return StatisticsDataProcessor.createEmptyMonthsData(selectedSite, startingMonth, timePeriod);
        }
    }

    static async saveStatistics(
        selectedSite: string,
        allMonthsData: SiteStatisticData[],
        monthlyForecastValues: Record<number, FormValuesByDate>,
        monthlyForecastDetailIds: Record<number, DetailIds>,
        monthlyBudgetDetailIds: Record<number, DetailIds>,
        availableRooms: number,
        timePeriod: TimeRangeType,
        calculateOvernightSelfForMonth: (monthIndex: number, periodKey: string, monthlyForecastValues: Record<number, FormValuesByDate>, inputType: string, availableRooms: number) => number,
        calculateOvernightValetForMonth: (monthIndex: number, periodKey: string, monthlyForecastValues: Record<number, FormValuesByDate>, inputType: string, availableRooms: number) => number,
        calculateExternalRevenueForMonth: (monthIndex: number, periodKey: string, selectedSite: string, monthlyForecastValues: Record<number, FormValuesByDate>, allMonthsData: any[], availableRooms: number, inputType: string, showingBudget?: boolean, timePeriod?: string) => { externalRevenue: number; adjustmentValue: number; adjustmentPercentage: number },
        customers: Customer[],
        inputType: string
    ): Promise<SiteStatisticData[]> {
        const customer = customers.find(c => c.customerSiteId === selectedSite);

        // Create payload as array of SiteStatisticData objects (one for each month)
        const payload: SiteStatisticData[] = [];

        for (let monthIndex = 0; monthIndex < 3; monthIndex++) {
            const monthData = allMonthsData[monthIndex];
            const monthForecastValues = monthlyForecastValues[monthIndex] || {};
            const monthForecastDetailIds = monthlyForecastDetailIds[monthIndex] || {};
            const monthBudgetDetailIds = monthlyBudgetDetailIds[monthIndex] || {};

            if (!monthData) continue;

            // Build forecast data for this month
            const monthForecastArray: SiteStatisticDetailData[] = Object.entries(monthForecastValues).map(([periodKey, values]) => {
                // Get budget rates for this day from the original month data
                const budgetItem = monthData.budgetData?.find(item => 
                    (item.periodStart === periodKey) || (item.periodLabel === periodKey)
                );

                const rates = budgetItem ? {
                    valetRateDaily: budgetItem.valetRateDaily || 0,
                    valetRateMonthly: budgetItem.valetRateMonthly || 0,
                    valetRateOvernight: budgetItem.valetRateOvernight || 0,
                    selfRateDaily: budgetItem.selfRateDaily || 0,
                    selfRateMonthly: budgetItem.selfRateMonthly || 0,
                    selfRateOvernight: budgetItem.selfRateOvernight || 0,
                    baseRevenue: budgetItem.baseRevenue || 0,
                } : {
                    valetRateDaily: 0,
                    valetRateMonthly: 0,
                    valetRateOvernight: 0,
                    selfRateDaily: 0,
                    selfRateMonthly: 0,
                    selfRateOvernight: 0,
                    baseRevenue: 0,
                };

                const externalRevenueCalculated = calculateExternalRevenueForMonth(monthIndex, periodKey, selectedSite, monthlyForecastValues, allMonthsData, availableRooms, inputType, false, timePeriod);

                return {
                    siteStatisticDetailId: monthForecastDetailIds[periodKey] || "00000000-0000-0000-0000-000000000000",
                    type: "Forecast",
                    periodStart: periodKey,
                    periodEnd: periodKey,
                    periodLabel: periodKey,
                    valetRateDaily: rates.valetRateDaily,
                    valetRateMonthly: rates.valetRateMonthly,
                    valetRateOvernight: rates.valetRateOvernight,
                    selfRateDaily: rates.selfRateDaily,
                    selfRateMonthly: rates.selfRateMonthly,
                    selfRateOvernight: rates.selfRateOvernight,
                    baseRevenue: rates.baseRevenue,
                    occupiedRooms: values["occupied-rooms"] || 0,
                    occupancy: values["occupancy"] || 0,
                    selfOvernight: calculateOvernightSelfForMonth(monthIndex, periodKey, monthlyForecastValues, inputType, availableRooms),
                    valetOvernight: calculateOvernightValetForMonth(monthIndex, periodKey, monthlyForecastValues, inputType, availableRooms),
                    valetDaily: values["valet-daily"] || 0,
                    valetMonthly: values["valet-monthly"] || 0,
                    selfDaily: values["self-daily"] || 0,
                    selfMonthly: values["self-monthly"] || 0,
                    valetComps: values["valet-comps"] || 0,
                    selfComps: values["self-comps"] || 0,
                    driveInRatio: (values["drive-in-ratio-input"] || 0) * 100,
                    captureRatio: (values["capture-ratio-input"] || 0) * 100,
                    selfAggregator: values["self-aggregator"] || 0,
                    valetAggregator: values["valet-aggregator"] || 0,
                    externalRevenue: externalRevenueCalculated.externalRevenue,
                    adjustmentValue: externalRevenueCalculated.adjustmentValue,
                    adjustmentPercentage: externalRevenueCalculated.adjustmentPercentage,
                };
            });

            // Build budget data for this month (include all existing budget data)
            const monthBudgetArray: SiteStatisticDetailData[] = monthData.budgetData?.map(budgetItem => ({
                siteStatisticDetailId: monthBudgetDetailIds[budgetItem.periodStart || budgetItem.periodLabel || ""] || "00000000-0000-0000-0000-000000000000",
                type: "Budget",
                periodStart: budgetItem.periodStart || budgetItem.periodLabel || "",
                periodEnd: budgetItem.periodEnd || budgetItem.periodStart || budgetItem.periodLabel || "",
                periodLabel: budgetItem.periodLabel || budgetItem.periodStart || "",
                externalRevenue: budgetItem.externalRevenue || 0,
                valetRateDaily: budgetItem.valetRateDaily || 0,
                valetRateMonthly: budgetItem.valetRateMonthly || 0,
                valetRateOvernight: budgetItem.valetRateOvernight || 0,
                selfRateDaily: budgetItem.selfRateDaily || 0,
                selfRateMonthly: budgetItem.selfRateMonthly || 0,
                selfRateOvernight: budgetItem.selfRateOvernight || 0,
                baseRevenue: budgetItem.baseRevenue || 0,
                occupiedRooms: budgetItem.occupiedRooms || 0,
                occupancy: budgetItem.occupancy || 0,
                selfOvernight: budgetItem.selfOvernight || 0,
                valetOvernight: budgetItem.valetOvernight || 0,
                valetDaily: budgetItem.valetDaily || 0,
                valetMonthly: budgetItem.valetMonthly || 0,
                selfDaily: budgetItem.selfDaily || 0,
                selfMonthly: budgetItem.selfMonthly || 0,
                valetComps: budgetItem.valetComps || 0,
                selfComps: budgetItem.selfComps || 0,
                driveInRatio: budgetItem.driveInRatio || 0,
                captureRatio: budgetItem.captureRatio || 0,
                selfAggregator: budgetItem.selfAggregator || 0,
                valetAggregator: budgetItem.valetAggregator || 0,
                adjustmentValue: budgetItem.adjustmentValue || 0,
                adjustmentPercentage: budgetItem.adjustmentPercentage || 0,
            })) || [];

            // Create month object
            const monthPayload: SiteStatisticData = {
                siteStatisticId: monthData.siteStatisticId || null,
                customerSiteId: selectedSite,
                name: customer?.siteName || null,
                siteNumber: customer?.siteNumber || "",
                totalRooms: availableRooms,
                timeRangeType: timePeriod,
                periodLabel: monthData.periodLabel,
                budgetData: monthBudgetArray,
                forecastData: monthForecastArray,
                actualData: [] // Empty as per sample
            };

            payload.push(monthPayload);
        }

        const response = await fetch(`/api/siteStatistics`, {
            method: 'PATCH',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(payload),
        });

        if (!response.ok) {
            throw new Error(`Error saving statistics: ${response.status}`);
        }

        // Return the saved data from the response
        const savedData = await response.json();
        return savedData;
    }
}
