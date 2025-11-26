import { useMemo, useRef, useEffect } from "react";
import {
    FormValuesByDate,
    SiteStatisticData,
    SiteStatisticDetailData,
    TimeRangeType
} from "@/lib/models/Statistics";
import { getDaysInMonth } from "../lib/helpers";

export interface PeriodEntry {
    rowKey: string;
    displayLabel: string;
}

export class PeriodEntriesGenerator {
    static generatePeriodEntries(
        timePeriod: TimeRangeType,
        selectedPeriod: string,
        currentMonthIndex: number,
        allMonthsData: SiteStatisticData[],
        showingBudget: boolean,
        budgetValues: FormValuesByDate,
        forecastValues: FormValuesByDate,
        actualValues: FormValuesByDate
    ): PeriodEntry[] {
        let keys: string[] = [];
        let periodLabelMap: Record<string, string> = {};

        // Helper to extract periodLabel for each periodStart from data arrays
        const extractLabels = (arr?: SiteStatisticDetailData[]) => {
            if (!arr) return;
            arr.forEach(item => {
                if (item.periodStart && item.periodLabel) {
                    periodLabelMap[item.periodStart] = item.periodLabel;
                }
            });
        };

        // For MONTHLY timePeriod, collect keys from all months
        if (timePeriod === "MONTHLY" && allMonthsData.length > 0) {
            const allKeys = new Set<string>();
            
            // Collect keys from all months' data
            allMonthsData.forEach((monthData) => {
                extractLabels(monthData.budgetData);
                extractLabels(monthData.forecastData);
                extractLabels(monthData.actualData);
                
                // For MONTHLY, also add the main periodLabel as fallback
                if (monthData.periodLabel) {
                    allKeys.add(monthData.periodLabel);
                    const [year, month] = monthData.periodLabel.split("-");
                    if (year && month) {
                        const date = new Date(Number(year), Number(month) - 1, 1);
                        periodLabelMap[monthData.periodLabel] = date.toLocaleDateString(undefined, { month: "long", year: "numeric" });
                    }
                }
            });
            
            // Add any keys from periodLabelMap
            Object.keys(periodLabelMap).forEach(key => allKeys.add(key));
            
            keys = Array.from(allKeys);
        } else if (timePeriod === "WEEKLY" && allMonthsData.length > 0) {
            // For WEEKLY, show only current month's weeks (paginated by month)
            const currentMonthData = allMonthsData[currentMonthIndex];
            if (currentMonthData) {
                extractLabels(currentMonthData.budgetData);
                extractLabels(currentMonthData.forecastData);
                extractLabels(currentMonthData.actualData);
                keys = Object.keys(periodLabelMap);
            }
        } else {
            // For DAILY or when no allMonthsData, use existing logic
            if (showingBudget && Object.keys(budgetValues).length > 0) {
                keys = Object.keys(budgetValues);
            } else if (!showingBudget && Object.keys(forecastValues).length > 0) {
                keys = Object.keys(forecastValues);
            } else if (Object.keys(actualValues).length > 0) {
                keys = Object.keys(actualValues);
            }
        }

        // Handle sorting and formatting based on time period
        if (timePeriod === "WEEKLY") {
            // Sort by periodStart date for proper chronological order
            keys.sort((a, b) => a.localeCompare(b));
            return keys.map((key) => ({
                rowKey: key,
                displayLabel: periodLabelMap[key] || key,
            }));
        }

        if (timePeriod === "MONTHLY") {
            // Handle both quarterly (e.g., "Q1 2025") and monthly (e.g., "2025-07") formats
            const quarterRegex = /^Q\d \d{4}$/;
            const monthlyRegex = /^\d{4}-\d{2}$/;
            
            const quarterKeys = keys.filter(k => quarterRegex.test(k));
            const monthlyKeys = keys.filter(k => monthlyRegex.test(k));
            
            if (quarterKeys.length > 0) {
                // Handle quarterly data (existing logic)
                const [selYearStr, selMonthStr] = selectedPeriod.split("-");
                const selYear = parseInt(selYearStr, 10);
                const selMonth = parseInt(selMonthStr, 10) - 1;
                const selQuarter = Math.floor(selMonth / 3) + 1;

                const filtered = quarterKeys
                    .map(k => {
                        const [q, y] = k.replace("Q", "").split(" ");
                        return { q: parseInt(q, 10), y: parseInt(y, 10), orig: k };
                    })
                    .filter(p => !isNaN(p.q) && !isNaN(p.y));

                const startIdx = filtered.findIndex(
                    p => p.q === selQuarter && p.y === selYear
                );

                if (startIdx === -1) {
                    keys = filtered.map(p => p.orig);
                } else {
                    const ordered = [];
                    for (let i = 0; i < filtered.length; i++) {
                        ordered.push(filtered[(startIdx + i) % filtered.length]);
                    }
                    keys = ordered.map(p => p.orig);
                }
            } else if (monthlyKeys.length > 0) {
                // Handle monthly data (e.g., "2025-07", "2025-08", "2025-09")
                keys = monthlyKeys.sort((a, b) => a.localeCompare(b));
            } else {
                // Fallback to existing sort
                keys.sort((a, b) => a.localeCompare(b));
            }
        } else {
            keys.sort((a, b) => a.localeCompare(b));
        }

        // For MONTHLY, format the periodKey appropriately
        if (timePeriod === "MONTHLY") {
            return keys.map((key) => {
                // Handle both formats: "2025-07-01" (from periodStart) and "2025-07" (from periodLabel)
                if (key.match(/^\d{4}-\d{2}-\d{2}$/)) {
                    // Format like "2025-07-01" - extract year and month and normalize rowKey
                    const [year, month] = key.split("-");
                    const date = new Date(Number(year), Number(month) - 1, 1);
                    const displayLabel = date.toLocaleDateString(undefined, { month: "long", year: "numeric" });
                    return {
                        rowKey: `${year}-${month}`, // Normalize to YYYY-MM format
                        displayLabel,
                    };
                } else if (key.match(/^\d{4}-\d{2}$/)) {
                    // Format like "2025-07" - direct parsing
                    const [year, month] = key.split("-");
                    const date = new Date(Number(year), Number(month) - 1, 1);
                    const displayLabel = date.toLocaleDateString(undefined, { month: "long", year: "numeric" });
                    return {
                        rowKey: key, // Already in YYYY-MM format
                        displayLabel,
                    };
                } else {
                    // Use periodLabelMap if available, otherwise fallback to key
                    return {
                        rowKey: key,
                        displayLabel: periodLabelMap[key] || key,
                    };
                }
            });
        }

        // For DAILY, build labels directly from current month data for better consistency
        if (timePeriod === "DAILY" && selectedPeriod) {
            const [yearStr, monthStr] = selectedPeriod.split("-");
            const year = Number(yearStr);
            const month = Number(monthStr) - 1;
            const days = getDaysInMonth(year, month);
            
            return keys.map((key) => {
                // Check if this key exists in the generated days
                const matchingDay = days.find(d => 
                    `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}` === key
                );
                
                if (matchingDay) {
                    const dayName = matchingDay.toLocaleDateString(undefined, { weekday: "short" });
                    const monthName = matchingDay.toLocaleDateString(undefined, { month: "short" });
                    return {
                        rowKey: key,
                        displayLabel: `${dayName}\n${monthName} ${matchingDay.getDate()}`,
                    };
                }
                
                // Fallback to periodLabelMap or raw key
                return {
                    rowKey: key,
                    displayLabel: periodLabelMap[key] || key,
                };
            });
        }

        return keys.map((key) => ({
            rowKey: key,
            displayLabel: periodLabelMap[key] || key,
        }));
    }
}

export function usePeriodEntries(
    timePeriod: TimeRangeType,
    selectedPeriod: string,
    currentMonthIndex: number,
    allMonthsData: SiteStatisticData[],
    showingBudget: boolean,
    budgetValues: FormValuesByDate,
    forecastValues: FormValuesByDate,
    actualValues: FormValuesByDate
): PeriodEntry[] {
    return useMemo(() => {
        return PeriodEntriesGenerator.generatePeriodEntries(
            timePeriod,
            selectedPeriod,
            currentMonthIndex,
            allMonthsData,
            showingBudget,
            budgetValues,
            forecastValues,
            actualValues
        );
    }, [timePeriod, selectedPeriod, currentMonthIndex, allMonthsData, showingBudget, budgetValues, forecastValues, actualValues]);
} 