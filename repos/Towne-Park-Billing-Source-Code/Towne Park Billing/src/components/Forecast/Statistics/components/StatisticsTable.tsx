import { Skeleton } from "@/components/ui/skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import useDragAndCopy, { DragCell } from "@/hooks/useDragAndCopy";
import {
    ALL_STATISTICS,
    FormValuesByDate,
    SiteStatisticData,
    SiteStatisticDetailData,
    TimeRangeType
} from "@/lib/models/Statistics";
import { cn, formatCurrency } from "@/lib/utils";
import React from "react";
import { NumericFormat } from "react-number-format";
import { PeriodEntry } from "../hooks/usePeriodEntries";
import { HEADER_DISPLAY_NAMES } from "../lib/constants";
import { formatPercentage, formatPeriodLabelForDisplay, getShortMonthName, hasCompleteActualData, isCurrentPeriod } from "../lib/helpers";
import { VarianceIndicator } from "./VarianceIndicator";

interface StatisticsTableComponentProps {
    isLoadingStatistics: boolean;
    selectedSite: string;
    selectedPeriod: string;
    inputType: string;
    timePeriod: TimeRangeType;
    viewMode: 'flash' | 'budget' | 'priorYear';
    currentMonthIndex: number;
    allMonthsData: SiteStatisticData[];
    periodEntries: PeriodEntry[];
    formValues: FormValuesByDate;
    budgetValues: FormValuesByDate;
    forecastValues: FormValuesByDate;
    actualValues: FormValuesByDate;
    budgetRatesByPeriod: Record<string, any>;
    handleInputChange: (periodStart: string, statId: string, value: string) => void;
    isFieldModified: (periodStart: string, statId: string) => boolean;
    calculateOvernightSelf: (periodStart: string) => number;
    calculateOvernightValet: (periodStart: string) => number;
    calculateActualOvernight: (periodStart: string, type: "self" | "valet") => number;
    calculateOccupancy: (periodStart: string) => number;
    getOccupiedRooms: (periodStart: string) => number;
    calculateExternalRevenue: (periodStart: string) => number;
    isPastPeriod: boolean;
    isDateBeforeToday: (dateString: string) => boolean;
    tableRef: React.RefObject<HTMLTableElement>;
    spreadsheetNavigation: any;
    setFormValues: React.Dispatch<React.SetStateAction<FormValuesByDate>>;
    setForecastValues: React.Dispatch<React.SetStateAction<FormValuesByDate>>;
    setMonthlyForecastValues: React.Dispatch<React.SetStateAction<Record<number, FormValuesByDate>>>;
    monthlyForecastValues: Record<number, FormValuesByDate>;
    setHasUnsavedChanges: (dirty: boolean) => void;
    availableRooms: number;
}

export function StatisticsTableComponent({
    isLoadingStatistics,
    selectedSite,
    selectedPeriod,
    inputType,
    timePeriod,
    viewMode,
    currentMonthIndex,
    allMonthsData,
    periodEntries,
    formValues,
    budgetValues,
    forecastValues,
    actualValues,
    budgetRatesByPeriod,
    handleInputChange,
    isFieldModified,
    calculateOvernightSelf,
    calculateOvernightValet,
    calculateActualOvernight,
    calculateOccupancy,
    getOccupiedRooms,
    calculateExternalRevenue,
    isPastPeriod,
    isDateBeforeToday,
    tableRef,
    spreadsheetNavigation,
    setForecastValues,
    setFormValues,
    setMonthlyForecastValues,
    monthlyForecastValues,
    setHasUnsavedChanges,
    availableRooms
}: StatisticsTableComponentProps) {

    const displayStatistics = React.useMemo(() => {
        const s = (id: string) => ALL_STATISTICS.find(x => x.id === id);

        const statsList = [
            // Rooms/Occupancy first
            s("occupied-rooms"),
            s("occupancy"),

            // Ratios
            s("drive-in-ratio-input"),
            s("capture-ratio-input"),

            // Valet sequence: Daily -> Overnight -> Monthly
            s("valet-daily"),
            s("valet-overnight"),
            s("valet-monthly"),
            s("valet-comps"),
            s("valet-aggregator"),

            // Self sequence: Daily -> Overnight -> Monthly
            s("self-daily"),
            s("self-overnight"),
            s("self-monthly"),
            s("self-comps"),
            s("self-aggregator"),
        ];

        return statsList.filter((stat): stat is typeof ALL_STATISTICS[number] => !!stat);
    }, []);

    const {
        isDragging,
        dragStartCell,
        dragEndCell,
        dragPreviewCells,
        handleDragStart,
        handleDragMove,
        handleDragEnd,
        isDragPreviewCell,
        resetDragSelection,
    } = useDragAndCopy({
        activeCell: spreadsheetNavigation.activeCell,
        rowCount: periodEntries.length,
        onCopy: (cells: DragCell[], context) => {
            const { periodEntries, displayStatistics, formValues } = context;
            return cells
                .sort((a, b) => a.rowIndex - b.rowIndex)
                .map(cell => {
                    const entry = periodEntries[cell.rowIndex];
                    const stat = displayStatistics[cell.colIndex];
                    const value = entry && stat ? (formValues[entry.rowKey]?.[stat.id] ?? "") : "";
                    return value === "" ? "" : String(value);
                });
        },
        onPaste: (cells: DragCell[], clipboard: string[], context) => {
            const {
                periodEntries,
                displayStatistics,
                formValues,
                forecastValues,
                monthlyForecastValues,
                setFormValues,
                setForecastValues,
                setMonthlyForecastValues,
                currentMonthIndex,
                viewMode,
                setHasUnsavedChanges,
                timePeriod,
                isDateBeforeToday,
                isPastPeriod,
            } = context;

            const newFormValues = { ...formValues };
            const newForecastValues = { ...forecastValues };
            const newMonthlyForecastValues = { ...monthlyForecastValues };

            const startCell = cells[0];
            if (!startCell) return;

            const isSingleCell = cells.length === 1;
            const colIndex = startCell.colIndex;

            for (let i = 0; i < clipboard.length; i++) {
                const rowIndex = isSingleCell ? startCell.rowIndex + i : cells[i]?.rowIndex;
                const col = isSingleCell ? colIndex : cells[i]?.colIndex;

                if (rowIndex === undefined || col === undefined) continue;

                const entry = periodEntries[rowIndex];
                const stat = displayStatistics[col];
                if (!entry || !stat) continue;

                // Skip pasting to cells that are disabled (past dates, past periods, budget mode, non-daily)
                const isDisabled = isPastPeriod || viewMode === 'budget' || timePeriod !== "DAILY" || (timePeriod === "DAILY" && isDateBeforeToday(entry.rowKey));
                if (isDisabled) continue;

                const value = clipboard[i] ?? "";
                // Handled pasting percentage value
                const numValue = (() => {
                    if (value === "") return 0;
                    const v = String(value).trim();
                    if (v.includes("%")) {
                        const parsed = Number(v.replace(/%/g, "").replace(/,/g, ""));
                        return Number.isNaN(parsed) ? 0 : Number(parsed / 100);
                    }
                    const parsed = Number(v.replace(/,/g, ""));
                    return Number.isNaN(parsed) ? 0 : Number(parsed);
                })();

                if (!newFormValues[entry.rowKey]) newFormValues[entry.rowKey] = {};
                newFormValues[entry.rowKey][stat.id] = numValue;

                if (viewMode === 'flash') {
                    if (!newForecastValues[entry.rowKey]) newForecastValues[entry.rowKey] = {};
                    newForecastValues[entry.rowKey][stat.id] = numValue;

                    if (!newMonthlyForecastValues[currentMonthIndex]) {
                        newMonthlyForecastValues[currentMonthIndex] = {};
                    }
                    if (!newMonthlyForecastValues[currentMonthIndex][entry.rowKey]) {
                        newMonthlyForecastValues[currentMonthIndex][entry.rowKey] = {};
                    }
                    newMonthlyForecastValues[currentMonthIndex][entry.rowKey][stat.id] = numValue;
                }
            }

            setFormValues(newFormValues);
            setForecastValues(newForecastValues);
            setMonthlyForecastValues(newMonthlyForecastValues);
            setHasUnsavedChanges(true);
        },

        context: {
            periodEntries,
            displayStatistics,
            formValues,
            forecastValues,
            monthlyForecastValues,
            setFormValues,
            setForecastValues,
            setMonthlyForecastValues,
            currentMonthIndex,
            viewMode,
            setHasUnsavedChanges,
            timePeriod,
            isDateBeforeToday,
            isPastPeriod,
        }
    });

    // Clear drag selection when active cell changes (Tab/Enter navigation)
    React.useEffect(() => {
        resetDragSelection();
    }, [spreadsheetNavigation.activeCell]);

    if (isLoadingStatistics) {
        return (
            <div className="p-4">
                <Skeleton className="h-[400px] w-full" />
            </div>
        );
    }

    return (
        <div>
            <Table ref={tableRef} className="w-full table-fixed" {...spreadsheetNavigation.tableProps}>
            <TableHeader>
                <TableRow>
                    <TableHead className="w-[60px] text-center whitespace-normal break-words text-xs p-1">
                        Date
                    </TableHead>
                    {displayStatistics.map((stat) => (
                        <TableHead
                            key={stat.id}
                            className="w-[80px] text-center whitespace-normal break-words text-xs p-1"
                        >
                            {HEADER_DISPLAY_NAMES[stat.id] || stat.name}
                        </TableHead>
                    ))}
                    <TableHead className="w-[90px] text-center whitespace-normal break-words text-xs p-1">
                        External<br />Revenue
                    </TableHead>
                </TableRow>
            </TableHeader>
            <TableBody>
                {periodEntries.length === 0 ? (
                    <TableRow>
                        <TableCell colSpan={displayStatistics.length + 2} className="text-center text-muted-foreground">
                            {selectedSite && selectedPeriod
                                ? "No data is available for the selected site and period."
                                : "Please select a customer site and period"}
                        </TableCell>
                    </TableRow>
                ) : (
                    periodEntries.map((entry, rowIndex) => {
                        const needsWeekendSeparatorAfter = () => {
                            if (timePeriod !== "DAILY") return false;
                            const [year, month, day] = entry.rowKey.split('-').map(Number);
                            const currentDate = new Date(year, month - 1, day);
                            const dayOfWeek = currentDate.getDay();
                            return dayOfWeek === 0;
                        };

                        const needsWeekendSeparatorBefore = () => {
                            if (timePeriod !== "DAILY") return false;
                            const [year, month, day] = entry.rowKey.split('-').map(Number);
                            const currentDate = new Date(year, month - 1, day);
                            const dayOfWeek = currentDate.getDay();
                            return dayOfWeek === 6;
                        };

                        // Check if this row has complete actual data for gray background
                        const hasActualData = hasCompleteActualData(actualValues, entry.rowKey);
                        const isCurrentPd = isCurrentPeriod(entry.rowKey, timePeriod);
                        
                        // Row styling logic
                        const rowClasses = isCurrentPd && (timePeriod === "WEEKLY" || timePeriod === "MONTHLY") && viewMode === 'flash'
                            ? "bg-orange-200 dark:bg-[#ec500267]"      // Orange for current week/month (partial actuals) - only in flash view
                            : hasActualData 
                                ? "bg-gray-100 dark:bg-gray-800"      // Gray for past with complete actuals
                                : "";                                 // Default for future/no actuals

                        return (
                            <React.Fragment key={entry.rowKey}>
                                {needsWeekendSeparatorBefore() && (
                                    <TableRow key={`weekend-before-${entry.rowKey}`} className="border-0">
                                        <TableCell colSpan={displayStatistics.length + 2} className="h-2 px-0 py-1 border-0 bg-gray-100 dark:bg-gray-700">
                                        </TableCell>
                                    </TableRow>
                                )}
                                <TableRow key={entry.rowKey} className={rowClasses}>
                                    <TableCell className="px-1 py-0.5 text-center">
                                        <div className="flex flex-col items-center">
                                            <span>
                                                {timePeriod === "WEEKLY"
                                                    ? (() => {
                                                        const match = entry.displayLabel.match(/^([A-Za-z]+)\s+(.+)$/);
                                                        if (match) {
                                                            const month = match[1];
                                                            const days = match[2];
                                                            const shortMonth = getShortMonthName(month);
                                                            return (
                                                                <>
                                                                    {shortMonth}
                                                                    <br />
                                                                    {days}
                                                                </>
                                                            );
                                                        }
                                                        return entry.displayLabel;
                                                    })()
                                                    : formatPeriodLabelForDisplay(entry.displayLabel)}
                                            </span>
                                        </div>
                                    </TableCell>
                                    {displayStatistics.map((stat, colIndex) => {
                                        // Helper constants for this cell
                                        const PERCENT_STAT_IDS = new Set([
                                            "occupancy",
                                            "drive-in-ratio-input",
                                            "capture-ratio-input",
                                        ]);
                                        const OVERNIGHT_STAT_IDS = new Set(["self-overnight", "valet-overnight"]);

                                        const API_FIELD_MAP: Record<string, keyof SiteStatisticDetailData> = {
                                            "occupancy": "occupancy",
                                            "occupied-rooms": "occupiedRooms",
                                            "valet-daily": "valetDaily",
                                            "valet-overnight": "valetOvernight",
                                            "valet-monthly": "valetMonthly",
                                            "valet-comps": "valetComps",
                                            "valet-aggregator": "valetAggregator",
                                            "self-daily": "selfDaily",
                                            "self-overnight": "selfOvernight",
                                            "self-monthly": "selfMonthly",
                                            "self-comps": "selfComps",
                                            "self-aggregator": "selfAggregator",
                                            "drive-in-ratio-input": "driveInRatio",
                                            "capture-ratio-input": "captureRatio",
                                            "external-revenue": "externalRevenue",
                                        };

                                        const isPercentField = PERCENT_STAT_IDS.has(stat.id);
                                        const isOvernight = OVERNIGHT_STAT_IDS.has(stat.id);
                                        const isRooms = stat.id === "occupied-rooms";
                                        const isOccupancy = stat.id === "occupancy";

                                        const isCellReadOnly = (entryKey: string) =>
                                            isPastPeriod ||
                                            viewMode === "budget" ||
                                            timePeriod !== "DAILY" ||
                                            (timePeriod === "DAILY" && isDateBeforeToday(entryKey));

                                        // Derived/editability rules
                                        const isEditable = (() => {
                                            if (isOvernight) return false; // derived
                                            if (isOccupancy && inputType === "occupied-rooms") return false; // derived from rooms
                                            if (isRooms && inputType !== "occupied-rooms") return false; // derived from occupancy
                                            return !isCellReadOnly(entry.rowKey);
                                        })();

                                        // Find forecast/budget rollups for WEEKLY/MONTHLY (scan once per cell)
                                        const findForecastAndBudget = () => {
                                            let forecastPeriod: SiteStatisticDetailData | undefined;
                                            let budgetPeriod: SiteStatisticDetailData | undefined;
                                            for (const monthData of allMonthsData) {
                                            if (!forecastPeriod) {
                                                forecastPeriod = monthData.forecastData?.find(d =>
                                                (d.periodStart && d.periodStart === entry.rowKey) ||
                                                (d.periodLabel && d.periodLabel === entry.displayLabel) ||
                                                (timePeriod === "MONTHLY" && monthData.periodLabel === entry.rowKey)
                                                );
                                            }
                                            if (!budgetPeriod) {
                                                budgetPeriod = monthData.budgetData?.find(d =>
                                                (d.periodStart && d.periodStart === entry.rowKey) ||
                                                (d.periodLabel && d.periodLabel === entry.displayLabel) ||
                                                (timePeriod === "MONTHLY" && monthData.periodLabel === entry.rowKey)
                                                );
                                            }
                                            if (forecastPeriod && budgetPeriod) break;
                                            }
                                            return { forecastPeriod, budgetPeriod };
                                        };

                                        // Compute display value
                                        const displayValue = (() => {
                                            // WEEKLY/MONTHLY
                                            if (timePeriod === "WEEKLY" || timePeriod === "MONTHLY") {
                                            // MONTHLY + current month: compute from daily arrays (actual < today, forecast >= today)
                                            if (timePeriod === "MONTHLY") {
                                                const now = new Date();
                                                const currentMonthKey = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
                                                if (entry.rowKey === currentMonthKey) {
                                                    const monthData = allMonthsData.find(m => m.periodLabel === entry.rowKey);
                                                    const apiField = API_FIELD_MAP[stat.id];
                                                    if (monthData && apiField) {
                                                        // Budget view: compute from budget daily entries only (no blending)
                                                        if (viewMode === 'budget') {
                                                            const budgetItems = monthData.budgetData || [];
                                                            // Occupancy: sum occupied rooms and divide by (availableRooms * daysInMonth)
                                                            if (isOccupancy) {
                                                                if (budgetItems.length === 0) return "";
                                                                let totalOccupied = 0;
                                                                const daysInMonth = new Date(now.getFullYear(), now.getMonth() + 1, 0).getDate();
                                                                budgetItems.forEach(d => {
                                                                    const occRooms = Number((d as any).occupiedRooms ?? 0);
                                                                    let occDec = Number((d as any).occupancy ?? 0);
                                                                    if (occRooms > 0 && availableRooms > 0) {
                                                                        occDec = occRooms / availableRooms;
                                                                    }
                                                                    if (availableRooms > 0) {
                                                                        const dayOccupied = occRooms > 0 ? occRooms : (availableRooms * occDec);
                                                                        totalOccupied += dayOccupied;
                                                                    }
                                                                });
                                                                const denom = availableRooms > 0 ? (availableRooms * daysInMonth) : 0;
                                                                const occDec = denom > 0 ? (totalOccupied / denom) : 0;
                                                                return (occDec * 100).toFixed(2);
                                                            }
                                                            // Ratios: arithmetic mean
                                                            if (stat.id === "drive-in-ratio-input" || stat.id === "capture-ratio-input") {
                                                                const key = stat.id === "drive-in-ratio-input" ? "driveInRatio" : "captureRatio";
                                                                if (budgetItems.length === 0) return "";
                                                                const vals = budgetItems.map(d => Number((d as any)[key] ?? 0));
                                                                const avg = vals.length > 0 ? (vals.reduce((a, b) => a + b, 0) / vals.length) : 0;
                                                                return avg.toFixed(2);
                                                            }
                                                            // Overnight / counts / monetary -> sum
                                                            const sumBudget = budgetItems.reduce((acc, d) => acc + Number((d as any)[apiField] ?? 0), 0);
                                                            if (
                                                                stat.id === "occupied-rooms" ||
                                                                stat.id.endsWith("-daily") ||
                                                                stat.id.endsWith("-monthly") ||
                                                                stat.id.endsWith("-comps") ||
                                                                stat.id.endsWith("-aggregator")
                                                            ) {
                                                                return Math.round(Number(sumBudget));
                                                            }
                                                            return String(sumBudget);
                                                        }
                                                        const parseDetailDate = (d: SiteStatisticDetailData): Date | null => {
                                                            let key = d.periodStart || d.periodLabel || "";
                                                            if (/^\d{4}-\d{2}-\d{2}$/.test(key)) {
                                                                const [y, m, day] = key.split('-').map(Number);
                                                                return new Date(y, m - 1, day);
                                                            }
                                                            if (/^\d{2}\/\d{2}\/\d{4}$/.test(key)) {
                                                                const [m, day, y] = key.split('/').map(Number);
                                                                return new Date(y, m - 1, day);
                                                            }
                                                            return null;
                                                        };

                                                        // Determine cutoff date: prefer ExternalRevenueLastDate from payload; fallback to max actual date; otherwise today
                                                        const parseCutoff = (val: string | null | undefined): Date | null => {
                                                            if (!val) return null;
                                                            if (/^\d{4}-\d{2}-\d{2}$/.test(val)) {
                                                                const [y, m, d] = val.split('-').map(Number);
                                                                return new Date(y, m - 1, d);
                                                            }
                                                            if (/^\d{2}\/\d{2}\/\d{4}$/.test(val)) {
                                                                const [m, d, y] = val.split('/').map(Number);
                                                                return new Date(y, m - 1, d);
                                                            }
                                                            return null;
                                                        };
                                                        const extDates: Date[] = [];
                                                        // Only read last-actual date from actuals; forecast entries may be null
                                                        (monthData.actualData || []).forEach(d => {
                                                            const cd = parseCutoff((d as any).externalRevenueLastDate as string | undefined);
                                                            if (cd) extDates.push(cd);
                                                        });
                                                        let cutoff = extDates.length ? new Date(Math.max.apply(null, extDates.map(t => t.getTime()))) : null;
                                                        if (!cutoff) {
                                                            const actualDates = (monthData.actualData || [])
                                                                .map(parseDetailDate)
                                                                .filter((d): d is Date => !!d);
                                                            if (actualDates.length) cutoff = new Date(Math.max.apply(null, actualDates.map(t => t.getTime())));
                                                        }
                                                        const cutoffMidnight = cutoff ? new Date(cutoff.getFullYear(), cutoff.getMonth(), cutoff.getDate()) : new Date(now.getFullYear(), now.getMonth(), now.getDate());
                                                        let actualDays = (monthData.actualData || []).filter(d => {
                                                            const dt = parseDetailDate(d);
                                                            return !!dt && dt <= cutoffMidnight;
                                                        });
                                                        let forecastDays = (monthData.forecastData || []).filter(d => {
                                                            const dt = parseDetailDate(d);
                                                            return !!dt && dt > cutoffMidnight;
                                                        });

                                                        // If externalRevenueLastDate is not in the current month, use forecast only (no actuals)
                                                        const isExtDateInCurrentMonth = extDates.some(d => d.getFullYear() === now.getFullYear() && d.getMonth() === now.getMonth());
                                                        if (!isExtDateInCurrentMonth) {
                                                            actualDays = [];
                                                            forecastDays = (monthData.forecastData || []).slice();
                                                        }

                                                        // Occupancy (percent): sum daily occupied rooms and divide by (availableRooms * daysInMonth)
                                                        if (isOccupancy) {
                                                            const combined = [...actualDays, ...forecastDays];
                                                            if (combined.length === 0) return "";
                                                            let totalOccupied = 0;
                                                            const daysInMonth = new Date(now.getFullYear(), now.getMonth() + 1, 0).getDate();
                                                            combined.forEach(d => {
                                                                const occRooms = Number((d as any).occupiedRooms ?? 0);
                                                                let occDec = Number((d as any).occupancy ?? 0);
                                                                if (occRooms > 0 && availableRooms > 0) {
                                                                    occDec = occRooms / availableRooms;
                                                                }
                                                                if (availableRooms > 0) {
                                                                    const dayOccupied = occRooms > 0 ? occRooms : (availableRooms * occDec);
                                                                    totalOccupied += dayOccupied;
                                                                }
                                                            });
                                                            const denom = availableRooms > 0 ? (availableRooms * daysInMonth) : 0;
                                                            const occDecFinal = denom > 0 ? (totalOccupied / denom) : 0;
                                                            return (occDecFinal * 100).toFixed(2);
                                                        }

                                                        // Ratios: arithmetic mean
                                                        if (stat.id === "drive-in-ratio-input" || stat.id === "capture-ratio-input") {
                                                            const key = stat.id === "drive-in-ratio-input" ? "driveInRatio" : "captureRatio";
                                                            const combined = [...actualDays, ...forecastDays];
                                                            if (combined.length === 0) return "";
                                                            const vals = combined.map(d => Number((d as any)[key] ?? 0));
                                                            const avg = vals.length > 0 ? (vals.reduce((a, b) => a + b, 0) / vals.length) : 0;
                                                            return avg.toFixed(2);
                                                        }

                                                        // Overnight, counts, monetary -> sum
                                                        const sumField = (items: SiteStatisticDetailData[], field: keyof SiteStatisticDetailData) => {
                                                            return items.reduce((acc, d) => acc + Number((d as any)[field] ?? 0), 0);
                                                        };

                                                        const total = sumField(actualDays, apiField) + sumField(forecastDays, apiField);

                                                        if (isPercentField) {
                                                            // safety: percent fields handled above; fallback
                                                            return (Number(total) * 100).toFixed(2);
                                                        }

                                                        if (
                                                            stat.id === "occupied-rooms" ||
                                                            stat.id.endsWith("-daily") ||
                                                            stat.id.endsWith("-monthly") ||
                                                            stat.id.endsWith("-comps") ||
                                                            stat.id.endsWith("-aggregator")
                                                        ) {
                                                            return Math.round(Number(total));
                                                        }

                                                        return String(total);
                                                    }
                                                }
                                            }

                                            // WEEKLY current/past/future aggregation rules mirror MONTHLY blending
                                            if (timePeriod === "WEEKLY") {
                                                // Parse week range from label like "September 1 - 7"
                                                const parseWeekRange = (label: string): { start: Date | null; end: Date | null } => {
                                                    const match = label.match(/^([A-Za-z]+)\s+(\d+)\s*-\s*(\d+)$/);
                                                    if (!match) return { start: null, end: null };
                                                    const monthName = match[1];
                                                    const dayStart = Number(match[2]);
                                                    const dayEnd = Number(match[3]);
                                                    const now = new Date();
                                                    const month = new Date(Date.parse(monthName + ' 1, ' + now.getFullYear())).getMonth();
                                                    const start = new Date(now.getFullYear(), month, dayStart);
                                                    const end = new Date(now.getFullYear(), month, dayEnd);
                                                    return { start, end };
                                                };
                                                const parseDetailDate = (d: SiteStatisticDetailData): Date | null => {
                                                    let key = d.periodStart || d.periodLabel || "";
                                                    if (/^\d{4}-\d{2}-\d{2}$/.test(key)) {
                                                        const [y, m, day] = key.split('-').map(Number);
                                                        return new Date(y, m - 1, day);
                                                    }
                                                    if (/^\d{2}\/\d{2}\/\d{4}$/.test(key)) {
                                                        const [m, day, y] = key.split('/').map(Number);
                                                        return new Date(y, m - 1, day);
                                                    }
                                                    return null;
                                                };
                                                const { start: weekStart, end: weekEnd } = parseWeekRange(entry.displayLabel);
                                                if (weekStart && weekEnd) {
                                                    const monthData = allMonthsData.find(m => {
                                                        // same calendar month as weekStart
                                                        return m.periodLabel === `${weekStart.getFullYear()}-${String(weekStart.getMonth() + 1).padStart(2,'0')}`;
                                                    });
                                                    const inRange = (d: SiteStatisticDetailData) => {
                                                        const dt = parseDetailDate(d);
                                                        return !!dt && dt >= weekStart && dt <= weekEnd;
                                                    };
                                                    const actualDays = (monthData?.actualData || []).filter(inRange);
                                                    const forecastDays = (monthData?.forecastData || []).filter(inRange);
                                                    const now = new Date();
                                                    const todayMid = new Date(now.getFullYear(), now.getMonth(), now.getDate());
                                                    const isPastWeek = weekEnd < todayMid;
                                                    const isFutureWeek = weekStart > todayMid;
                                                    let useActual: SiteStatisticDetailData[] = [];
                                                    let useForecast: SiteStatisticDetailData[] = [];
                                                    if (isPastWeek) {
                                                        useActual = actualDays;
                                                        useForecast = [];
                                                    } else if (isFutureWeek) {
                                                        useActual = [];
                                                        useForecast = forecastDays;
                                                    } else {
                                                        // current week: if no actuals, use full forecast; otherwise split by cutoff similar to monthly
                                                        if (actualDays.length === 0) {
                                                            useActual = [];
                                                            useForecast = forecastDays;
                                                        } else {
                                                        const parseCutoff = (val: string | null | undefined): Date | null => {
                                                            if (!val) return null;
                                                            if (/^\d{4}-\d{2}-\d{2}$/.test(val)) {
                                                                const [y, m, d] = val.split('-').map(Number);
                                                                return new Date(y, m - 1, d);
                                                            }
                                                            if (/^\d{2}\/\d{2}\/\d{4}$/.test(val)) {
                                                                const [m, d, y] = val.split('/').map(Number);
                                                                return new Date(y, m - 1, d);
                                                            }
                                                            return null;
                                                        };
                                                        const extDates: Date[] = [];
                                                        actualDays.forEach(d => {
                                                            const cd = parseCutoff((d as any).externalRevenueLastDate as string | undefined);
                                                            if (cd) extDates.push(cd);
                                                        });
                                                        let cutoff = extDates.length ? new Date(Math.max.apply(null, extDates.map(t => t.getTime()))) : null;
                                                        if (!cutoff) {
                                                            const actualDates = actualDays.map(parseDetailDate).filter((d): d is Date => !!d);
                                                            if (actualDates.length) cutoff = new Date(Math.max.apply(null, actualDates.map(t => t.getTime())));
                                                        }
                                                        const cutoffMidnight = cutoff ? new Date(cutoff.getFullYear(), cutoff.getMonth(), cutoff.getDate()) : todayMid;
                                                        useActual = actualDays.filter(d => {
                                                            const dt = parseDetailDate(d);
                                                            return !!dt && dt <= cutoffMidnight;
                                                        });
                                                        useForecast = forecastDays.filter(d => {
                                                            const dt = parseDetailDate(d);
                                                            return !!dt && dt > cutoffMidnight;
                                                        });
                                                        }
                                                    }
                                                    const apiField = API_FIELD_MAP[stat.id];
                                                    // Occupancy: sum daily occupied rooms, divide by (availableRooms * daysInWeek)
                                                    if (isOccupancy) {
                                                        const combined = [...useActual, ...useForecast];
                                                        if (combined.length === 0) return "";
                                                        let totalOccupied = 0;
                                                        const msPerDay = 24 * 60 * 60 * 1000;
                                                        const daysInWeek = Math.floor((weekEnd.getTime() - weekStart.getTime()) / msPerDay) + 1;
                                                        combined.forEach(d => {
                                                            const occRooms = Number((d as any).occupiedRooms ?? 0);
                                                            let occDec = Number((d as any).occupancy ?? 0);
                                                            if (occRooms > 0 && availableRooms > 0) occDec = occRooms / availableRooms;
                                                            if (availableRooms > 0) {
                                                                const dayOccupied = occRooms > 0 ? occRooms : (availableRooms * occDec);
                                                                totalOccupied += dayOccupied;
                                                            }
                                                        });
                                                        const denom = availableRooms > 0 ? (availableRooms * daysInWeek) : 0;
                                                        const occDec = denom > 0 ? (totalOccupied / denom) : 0;
                                                        return (occDec * 100).toFixed(2);
                                                    }
                                                    // Ratios
                                                    if (stat.id === "drive-in-ratio-input" || stat.id === "capture-ratio-input") {
                                                        const key = stat.id === "drive-in-ratio-input" ? "driveInRatio" : "captureRatio";
                                                        const combined = [...useActual, ...useForecast];
                                                        if (combined.length === 0) return "";
                                                        const vals = combined.map(d => Number((d as any)[key] ?? 0));
                                                        const avg = vals.length > 0 ? (vals.reduce((a,b)=>a+b,0) / vals.length) : 0;
                                                        return avg.toFixed(2);
                                                    }
                                                    // Overnight
                                                    if (isOvernight) {
                                                        const key = stat.id === "self-overnight" ? "selfOvernight" : "valetOvernight";
                                                        const total = [...useActual, ...useForecast].reduce((acc,d)=> acc + Number((d as any)[key] ?? 0), 0);
                                                        return Math.round(total);
                                                    }
                                                    // Other numeric
                                                    if (apiField) {
                                                        const total = [...useActual, ...useForecast].reduce((acc,d)=> acc + Number((d as any)[apiField] ?? 0), 0);
                                                        if (
                                                            stat.id === "occupied-rooms" ||
                                                            stat.id.endsWith("-daily") ||
                                                            stat.id.endsWith("-monthly") ||
                                                            stat.id.endsWith("-comps") ||
                                                            stat.id.endsWith("-aggregator")
                                                        ) {
                                                            return Math.round(total);
                                                        }
                                                        return String(total);
                                                    }
                                                }
                                            }

                                            // Default WEEKLY/MONTHLY -> read from API aggregates
                                            const { forecastPeriod, budgetPeriod } = findForecastAndBudget();
                                            const apiField = API_FIELD_MAP[stat.id];

                                            // Overnight (self/valet) handled by their dedicated fields (rounded)
                                            if (isOvernight) {
                                                const val =
                                                viewMode === "budget"
                                                    ? (stat.id === "self-overnight"
                                                        ? budgetPeriod?.selfOvernight
                                                        : budgetPeriod?.valetOvernight)
                                                    : (stat.id === "self-overnight"
                                                        ? (forecastPeriod?.selfOvernight ?? budgetPeriod?.selfOvernight)
                                                        : (forecastPeriod?.valetOvernight ?? budgetPeriod?.valetOvernight));
                                                return Math.round(val ?? 0);
                                            }

                                            // MONTHLY (non-current): compute occupancy from daily data
                                            // - Past months: actuals only
                                            // - Future months: forecast only
                                            if (isOccupancy && timePeriod === "MONTHLY") {
                                                const monthData = allMonthsData.find(m => m.periodLabel === entry.rowKey);
                                                if (monthData) {
                                                    // Budget view: budget daily only
                                                    if (viewMode === 'budget') {
                                                        const budgetItems = monthData.budgetData || [];
                                                        if (budgetItems.length > 0) {
                                                            let totalOccupied = 0;
                                                            const [yy, mm] = entry.rowKey.split('-').map(Number);
                                                            const daysInMonth = new Date(yy, mm, 0).getDate();
                                                            budgetItems.forEach(d => {
                                                                const occRooms = Number((d as any).occupiedRooms ?? 0);
                                                                let occDec = Number((d as any).occupancy ?? 0);
                                                                if (occRooms > 0 && availableRooms > 0) {
                                                                    occDec = occRooms / availableRooms;
                                                                }
                                                                if (availableRooms > 0) {
                                                                    const dayOccupied = occRooms > 0 ? occRooms : (availableRooms * occDec);
                                                                    totalOccupied += dayOccupied;
                                                                }
                                                            });
                                                            const denom = availableRooms > 0 ? (availableRooms * daysInMonth) : 0;
                                                            const occDec = denom > 0 ? (totalOccupied / denom) : 0;
                                                            return (occDec * 100).toFixed(2);
                                                        }
                                                    }
                                                    const now = new Date();
                                                    const currentMonthKey = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
                                                    const isPastMonth = entry.rowKey < currentMonthKey;
                                                    const combined = isPastMonth ? (monthData.actualData || []) : (monthData.forecastData || []);
                                                    if (combined.length > 0) {
                                                        let totalOccupied = 0;
                                                        combined.forEach(d => {
                                                            const occRooms = Number((d as any).occupiedRooms ?? 0);
                                                            let occDec = Number((d as any).occupancy ?? 0);
                                                            if (occRooms > 0 && availableRooms > 0) {
                                                                occDec = occRooms / availableRooms;
                                                            }
                                                            if (availableRooms > 0) {
                                                                const dayOccupied = occRooms > 0 ? occRooms : (availableRooms * occDec);
                                                                totalOccupied += dayOccupied;
                                                            }
                                                        });
                                                        const [yy2, mm2] = entry.rowKey.split('-').map(Number);
                                                        const daysInMonth2 = new Date(yy2, mm2, 0).getDate();
                                                        const denom = availableRooms > 0 ? (availableRooms * daysInMonth2) : 0;
                                                        const occDec = denom > 0 ? (totalOccupied / denom) : 0;
                                                        return (occDec * 100).toFixed(2);
                                                    }
                                                }
                                            }

                                            // MONTHLY occupied rooms for all non-current months
                                            // - Past months: actuals only (prefer monthly actual aggregate if available; else sum daily)
                                            // - Future months: forecast only (sum daily)
                                            if (isRooms && timePeriod === "MONTHLY") {
                                                const now = new Date();
                                                const currentMonthKey = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
                                                const monthData = allMonthsData.find(m => m.periodLabel === entry.rowKey);
                                                if (monthData) {
                                                    if (entry.rowKey === currentMonthKey) {
                                                        // Current month handled earlier by daily split; sum already calculated in that branch
                                                    } else {
                                                        // Budget view: sum budget daily occupiedRooms only
                                                        if (viewMode === 'budget') {
                                                            const budgetItems = monthData.budgetData || [];
                                                            const totalRooms = budgetItems.reduce((acc, d) => acc + Number((d as any).occupiedRooms ?? 0), 0);
                                                            return Math.round(totalRooms);
                                                        }
                                                        const isPastMonth = entry.rowKey < currentMonthKey;
                                                        const combined = isPastMonth ? (monthData.actualData || []) : (monthData.forecastData || []);
                                                        if (combined.length > 0) {
                                                            let totalRooms = 0;
                                                            if (isPastMonth) {
                                                                // Past months: strictly sum actual occupiedRooms
                                                                totalRooms = combined.reduce((acc, d) => acc + Number((d as any).occupiedRooms ?? 0), 0);
                                                            } else {
                                                                // Future months: sum forecast occupiedRooms; if missing, derive from occupancy
                                                                totalRooms = combined.reduce((acc, d) => {
                                                                    const occRooms = Number((d as any).occupiedRooms ?? 0);
                                                                    if (occRooms > 0) return acc + occRooms;
                                                                    const occDec = Number((d as any).occupancy ?? 0);
                                                                    if (occDec > 0 && availableRooms > 0) return acc + Math.round(availableRooms * occDec);
                                                                    return acc;
                                                                }, 0);
                                                            }
                                                            return Math.round(totalRooms);
                                                        }
                                                    }
                                                }
                                            }

                                            // MONTHLY (non-current): compute all other stats from daily data (no mixing)
                                            // - Past months: actuals only
                                            // - Future months: forecast only
                                            if (timePeriod === "MONTHLY") {
                                                const now = new Date();
                                                const currentMonthKey = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
                                                if (entry.rowKey !== currentMonthKey) {
                                                    const monthData = allMonthsData.find(m => m.periodLabel === entry.rowKey);
                                                    if (monthData) {
                                                        // Budget view: compute from budget daily only
                                                        if (viewMode === 'budget') {
                                                            const budgetItems = monthData.budgetData || [];
                                                            if (budgetItems.length > 0) {
                                                                const apiField = API_FIELD_MAP[stat.id];
                                                                if (isPercentField) {
                                                                    const key = stat.id === "drive-in-ratio-input" ? "driveInRatio" : (stat.id === "capture-ratio-input" ? "captureRatio" : undefined);
                                                                    if (key) {
                                                                        const vals = budgetItems.map(d => Number((d as any)[key] ?? 0));
                                                                        const avg = vals.length > 0 ? (vals.reduce((a, b) => a + b, 0) / vals.length) : 0;
                                                                        return avg.toFixed(2);
                                                                    }
                                                                }
                                                                if (isOvernight) {
                                                                    const key = stat.id === "self-overnight" ? "selfOvernight" : "valetOvernight";
                                                                    const total = budgetItems.reduce((acc, d) => acc + Number((d as any)[key] ?? 0), 0);
                                                                    return Math.round(total);
                                                                }
                                                                if (apiField) {
                                                                    const total = budgetItems.reduce((acc, d) => acc + Number((d as any)[apiField] ?? 0), 0);
                                                                    if (
                                                                        stat.id.endsWith("-daily") ||
                                                                        stat.id.endsWith("-monthly") ||
                                                                        stat.id.endsWith("-comps") ||
                                                                        stat.id.endsWith("-aggregator")
                                                                    ) {
                                                                        return Math.round(total);
                                                                    }
                                                                    return String(total);
                                                                }
                                                            }
                                                        }
                                                        const isPastMonth = entry.rowKey < currentMonthKey;
                                                        const combined = isPastMonth ? (monthData.actualData || []) : (monthData.forecastData || []);
                                                        if (combined.length > 0) {
                                                            const apiField = API_FIELD_MAP[stat.id];

                                                            // Percent fields: arithmetic mean
                                                            if (isPercentField) {
                                                                const key = stat.id === "drive-in-ratio-input" ? "driveInRatio" : (stat.id === "capture-ratio-input" ? "captureRatio" : undefined);
                                                                if (key) {
                                                                    const vals = combined.map(d => Number((d as any)[key] ?? 0));
                                                                    const avg = vals.length > 0 ? (vals.reduce((a, b) => a + b, 0) / vals.length) : 0;
                                                                    return avg.toFixed(2);
                                                                }
                                                            }

                                                            // Overnight fields: sum
                                                            if (isOvernight) {
                                                                const key = stat.id === "self-overnight" ? "selfOvernight" : "valetOvernight";
                                                                const total = combined.reduce((acc, d) => acc + Number((d as any)[key] ?? 0), 0);
                                                                return Math.round(total);
                                                            }

                                                            // Other numeric fields: sum
                                                            if (apiField) {
                                                                const total = combined.reduce((acc, d) => acc + Number((d as any)[apiField] ?? 0), 0);
                                                                if (
                                                                    stat.id.endsWith("-daily") ||
                                                                    stat.id.endsWith("-monthly") ||
                                                                    stat.id.endsWith("-comps") ||
                                                                    stat.id.endsWith("-aggregator")
                                                                ) {
                                                                    return Math.round(total);
                                                                }
                                                                return String(total);
                                                            }
                                                        }
                                                    }
                                                }
                                            }

                                            // Generic field via apiField map
                                            if (apiField) {
                                                let raw;
                                                
                                                if (viewMode === "budget") {
                                                    // Budget mode: always show budget data
                                                    raw = budgetPeriod?.[apiField];
                                                } else {
                                                    // Flash mode: show actual data for past periods when available, otherwise forecast/budget
                                                    // Check if this specific entry is in the past (not current period)
                                                    const isPastEntry = !isCurrentPeriod(entry.rowKey, timePeriod);
                                                    
                                                    if (isPastEntry) {
                                                        // For past periods, check if actual data exists
                                                        const actualRaw = actualValues[entry.rowKey]?.[stat.id];
                                                        if (actualRaw != null) {
                                                            raw = actualRaw;
                                                        } else {
                                                            // Fall back to forecast, then budget
                                                            raw = forecastPeriod?.[apiField] ?? budgetPeriod?.[apiField];
                                                        }
                                                    } else {
                                                        // For current/future periods, use forecast with budget fallback
                                                        raw = forecastPeriod?.[apiField] ?? budgetPeriod?.[apiField];
                                                    }
                                                }

                                                if (raw == null) return "";

                                                if (isPercentField) {
                                                const pct = Number(raw) * 100;
                                                return pct.toFixed(2);
                                                }

                                                // Integer formatting for counts
                                                if (
                                                stat.id === "occupied-rooms" ||
                                                stat.id.endsWith("-daily") ||
                                                stat.id.endsWith("-monthly") ||
                                                stat.id.endsWith("-comps") ||
                                                stat.id.endsWith("-aggregator")
                                                ) {
                                                return Math.round(Number(raw));
                                                }

                                                return String(raw);
                                            }

                                            return "";
                                            }

                                            // DAILY
                                            const isPastDate = isDateBeforeToday(entry.rowKey);

                                            // Overnight (derived)
                                            if (isOvernight) {
                                            const type = stat.id === "self-overnight" ? "self" : "valet";

                                            if (viewMode === "flash" && isPastDate) {
                                                // Try to get actual overnight data first
                                                const actualOvernight = calculateActualOvernight(entry.rowKey, type);
                                                if (actualOvernight > 0) {
                                                    // Actual data exists, use it
                                                    return Math.round(actualOvernight);
                                                } else {
                                                    // No actual data, fall back to forecast values
                                                    return Math.round(
                                                        type === "self"
                                                        ? calculateOvernightSelf(entry.rowKey)
                                                        : calculateOvernightValet(entry.rowKey)
                                                    );
                                                }
                                            }
                                            if (viewMode === "budget") {
                                                const key = type === "self" ? "selfOvernight" : "valetOvernight";
                                                return Math.round(budgetRatesByPeriod[entry.rowKey]?.[key] || 0);
                                            }
                                            // forecast calc for daily
                                            return Math.round(
                                                type === "self"
                                                ? calculateOvernightSelf(entry.rowKey)
                                                : calculateOvernightValet(entry.rowKey)
                                            );
                                            }

                                            // For past dates in flash view, show actuals when available
                                            if (viewMode === "flash" && isPastDate) {
                                            // Occupancy: prefer computing from occupied rooms to avoid rounding drift
                                            if (isOccupancy) {
                                                const actualRooms = actualValues[entry.rowKey]?.["occupied-rooms"];
                                                if (actualRooms != null) {
                                                    // Actual data exists, use it
                                                    const rooms = Number(actualRooms);
                                                    const occ = availableRooms > 0 ? rooms / availableRooms : 0;
                                                    return (occ * 100).toFixed(2);
                                                } else {
                                                    // No actual data, fall back to forecast values
                                                    const forecastRooms = formValues[entry.rowKey]?.["occupied-rooms"];
                                                    const forecastOcc = formValues[entry.rowKey]?.["occupancy"];
                                                    
                                                    if (forecastRooms != null) {
                                                        const rooms = Number(forecastRooms);
                                                        const occ = availableRooms > 0 ? rooms / availableRooms : 0;
                                                        return (occ * 100).toFixed(2);
                                                    } else if (forecastOcc != null) {
                                                        return (Number(forecastOcc) * 100).toFixed(2);
                                                    }
                                                    
                                                    return ""; // No actual or forecast data
                                                }
                                            }

                                            const actualRaw = actualValues[entry.rowKey]?.[stat.id];

                                            if (actualRaw != null) {
                                                // Actual data exists, use it
                                                if (isPercentField) {
                                                    return (Number(actualRaw) * 100).toFixed(2);
                                                }

                                                if (isRooms) {
                                                    return Math.round(Number(actualRaw));
                                                }

                                                return String(actualRaw);
                                            } else {
                                                // No actual data, fall back to forecast values
                                                const forecastRaw = formValues[entry.rowKey]?.[stat.id];
                                                
                                                if (forecastRaw != null) {
                                                    if (isPercentField) {
                                                        return (Number(forecastRaw) * 100).toFixed(2);
                                                    }

                                                    if (isRooms) {
                                                        return Math.round(Number(forecastRaw));
                                                    }

                                                    return String(forecastRaw);
                                                }
                                                
                                                return ""; // No actual or forecast data
                                            }
                                            }

                                            // Forecast entry for today/future in flash, or daily in general (non-budget)
                                            // Derived relationship between rooms and occupancy
                                            if (isOccupancy && inputType === "occupied-rooms") {
                                            const rooms = Number(formValues[entry.rowKey]?.["occupied-rooms"] ?? 0);
                                            const occ = availableRooms > 0 ? rooms / availableRooms : 0;
                                            return (occ * 100).toFixed(2);
                                            }
                                            if (isRooms && inputType !== "occupied-rooms") {
                                            const occDec = Number(formValues[entry.rowKey]?.["occupancy"] ?? 0); // stored as decimal
                                            const rooms = availableRooms > 0 ? Math.round(availableRooms * occDec) : 0;
                                            return rooms;
                                            }

                                            // Standard form value
                                            const fv = formValues[entry.rowKey]?.[stat.id];

                                            if (fv == null) return "";

                                            if (isPercentField) {
                                            return (Number(fv) * 100).toFixed(2);
                                            }

                                            // integer fields
                                            if (
                                            stat.id === "occupied-rooms" ||
                                            stat.id.endsWith("-daily") ||
                                            stat.id.endsWith("-monthly") ||
                                            stat.id.endsWith("-comps") ||
                                            stat.id.endsWith("-aggregator")
                                            ) {
                                            return Math.round(Number(fv));
                                            }

                                            return String(fv);
                                        })();

                                        // Variance: show in flash, for past periods, when both actual and forecast exist
                                        const shouldShowVariance = (() => {
                                            if (viewMode !== "flash") return false;
                                            if (isCurrentPd) return false;

                                            // DAILY: only show for past dates
                                            if (timePeriod === "DAILY" && !isDateBeforeToday(entry.rowKey)) return false;

                                            // Determine if actual exists
                                            let hasActual = false;
                                            if (isOvernight) {
                                            // daily overnight actual via calculateActualOvernight; for non-daily depends on actualValues presence
                                            if (timePeriod === "DAILY") {
                                                // Check if actual overnight data exists by looking for the required fields
                                                const hasDriveInRatio = actualValues[entry.rowKey]?.["drive-in-ratio-input"] != null;
                                                const hasCaptureRatio = actualValues[entry.rowKey]?.["capture-ratio-input"] != null;
                                                const hasOccupiedRooms = actualValues[entry.rowKey]?.["occupied-rooms"] != null;
                                                hasActual = hasDriveInRatio && hasCaptureRatio && hasOccupiedRooms;
                                            } else {
                                                const key = stat.id;
                                                hasActual = actualValues[entry.rowKey]?.[key] != null;
                                            }
                                            } else if (isOccupancy) {
                                            // we can compute from occupied rooms actual
                                            hasActual =
                                                actualValues[entry.rowKey]?.["occupied-rooms"] != null ||
                                                actualValues[entry.rowKey]?.["occupancy"] != null;
                                            } else {
                                            hasActual = actualValues[entry.rowKey]?.[stat.id] != null;
                                            }

                                            if (!hasActual) return false;

                                            // Determine if forecast exists (independent of inputType)
                                            if (timePeriod === "DAILY") {
                                            if (isOvernight) return true; // we can compute forecast via calc funcs
                                            if (isOccupancy) {
                                                return (
                                                formValues[entry.rowKey]?.["occupancy"] != null ||
                                                formValues[entry.rowKey]?.["occupied-rooms"] != null
                                                );
                                            }
                                            if (isRooms) {
                                                return (
                                                formValues[entry.rowKey]?.["occupied-rooms"] != null ||
                                                formValues[entry.rowKey]?.["occupancy"] != null
                                                );
                                            }
                                            return formValues[entry.rowKey]?.[stat.id] != null;
                                            } else {
                                            const { forecastPeriod, budgetPeriod } = findForecastAndBudget();
                                            if (isOvernight) return !!(forecastPeriod ?? budgetPeriod); // allow budget fallback
                                            const apiField = API_FIELD_MAP[stat.id];
                                            return apiField ? ((forecastPeriod?.[apiField] ?? budgetPeriod?.[apiField]) != null) : false;
                                            }
                                        })();

                                        // Values for VarianceIndicator (numbers; percent fields in percent units)
                                        const varianceValues = (() => {
                                            if (!shouldShowVariance) return null;

                                            let actualValue = 0;
                                            let forecastValue = 0;

                                            if (timePeriod === "DAILY") {
                                            // Actual
                                            if (isOvernight) {
                                                const type = stat.id === "self-overnight" ? "self" : "valet";
                                                actualValue = calculateActualOvernight(entry.rowKey, type);
                                            } else if (isOccupancy) {
                                                const rooms = Number(actualValues[entry.rowKey]?.["occupied-rooms"] ?? 0);
                                                const occ = availableRooms > 0 ? rooms / availableRooms : 0;
                                                actualValue = occ * 100; // percent units
                                            } else if (isPercentField) {
                                                actualValue = Number(actualValues[entry.rowKey]?.[stat.id] ?? 0) * 100;
                                            } else if (isRooms) {
                                                actualValue = Number(actualValues[entry.rowKey]?.["occupied-rooms"] ?? 0);
                                            } else {
                                                actualValue = Number(actualValues[entry.rowKey]?.[stat.id] ?? 0);
                                            }

                                            // Forecast
                                            if (isOvernight) {
                                                const type = stat.id === "self-overnight" ? "self" : "valet";
                                                forecastValue =
                                                type === "self"
                                                    ? calculateOvernightSelf(entry.rowKey)
                                                    : calculateOvernightValet(entry.rowKey);
                                            } else if (isOccupancy) {
                                                const occDec = formValues[entry.rowKey]?.["occupancy"];
                                                if (occDec != null) {
                                                    forecastValue = Number(occDec) * 100;
                                                } else {
                                                    const rooms = Number(formValues[entry.rowKey]?.["occupied-rooms"] ?? 0);
                                                    const occ = availableRooms > 0 ? rooms / availableRooms : 0;
                                                    forecastValue = occ * 100;
                                                }
                                            } else if (isRooms) {
                                                const roomsFv = formValues[entry.rowKey]?.["occupied-rooms"];
                                                if (roomsFv != null) {
                                                    forecastValue = Number(roomsFv);
                                                } else {
                                                    const occDec = Number(formValues[entry.rowKey]?.["occupancy"] ?? 0); // decimal
                                                    forecastValue = Math.round(availableRooms * occDec);
                                                }
                                            } else if (isPercentField) {
                                                forecastValue = Number(formValues[entry.rowKey]?.[stat.id] ?? 0) * 100;
                                            } else {
                                                forecastValue = Number(formValues[entry.rowKey]?.[stat.id] ?? 0);
                                            }
                                            } else {
                                            // WEEKLY/MONTHLY
                                            const { forecastPeriod, budgetPeriod } = findForecastAndBudget();
                                            const comparePeriod = forecastPeriod ?? budgetPeriod;
                                            if (!comparePeriod) return null;

                                            if (isOvernight) {
                                                const f = stat.id === "self-overnight" ? comparePeriod.selfOvernight : comparePeriod.valetOvernight;
                                                forecastValue = Number(f ?? 0);
                                                // Actual: rely on actualValues aggregate if present; if not present, we won't show variance
                                                const key = stat.id;
                                                actualValue = Number(actualValues[entry.rowKey]?.[key] ?? 0);
                                            } else {
                                                const apiField = API_FIELD_MAP[stat.id];
                                                if (!apiField) return null;
                                                const f = comparePeriod[apiField];

                                                if (isPercentField) {
                                                forecastValue = Number(f ?? 0) * 100;
                                                const a =
                                                    stat.id === "occupancy"
                                                    ? (Number(actualValues[entry.rowKey]?.["occupancy"] ?? 0) * 100)
                                                    : (Number(actualValues[entry.rowKey]?.[stat.id] ?? 0) * 100);
                                                actualValue = a;
                                                } else if (isRooms) {
                                                forecastValue = Number((comparePeriod as any).occupiedRooms ?? 0);
                                                actualValue = Number(actualValues[entry.rowKey]?.["occupied-rooms"] ?? 0);
                                                } else {
                                                forecastValue = Number(f ?? 0);
                                                actualValue = Number(actualValues[entry.rowKey]?.[stat.id] ?? 0);
                                                }
                                            }
                                            }

                                            return { actualValue, forecastValue };
                                        })();

                                        const decimalScale =
                                            isPercentField
                                            ? 2
                                            : (isOvernight || isRooms || stat.id.endsWith("-daily") || stat.id.endsWith("-monthly") || stat.id.endsWith("-comps") || stat.id.endsWith("-aggregator"))
                                                ? 0
                                                : 2;

                                        return (
                                            <TableCell
                                            data-row={rowIndex}
                                            data-col={colIndex}
                                            key={`${entry.rowKey}-${stat.id}`}
                                            data-cell-id={`cell-${rowIndex}-${colIndex}`}
                                            className={`px-1 py-0.5 min-w-[80px] cursor-ns-resize ${
                                                spreadsheetNavigation.activeCell?.rowIndex === rowIndex &&
                                                spreadsheetNavigation.activeCell?.colIndex === colIndex
                                                ? "border-2 border-blue-500 z-10 relative"
                                                : ""
                                            } ${isDragPreviewCell(rowIndex, colIndex) ? "border-2 border-blue-500 z-10 relative" : ""}`}
                                            {...spreadsheetNavigation.getCellProps(rowIndex, colIndex)}
                                            onMouseDown={(e) => {
                                                e.preventDefault();
                                                handleDragStart(rowIndex, colIndex);
                                            }}
                                            onMouseUp={handleDragEnd}
                                            onMouseMove={() => {
                                                if (isDragging) handleDragMove(rowIndex, colIndex);
                                            }}
                                            >
                                            <div className="flex flex-col items-end">
                                                <div
                                                className={cn(
                                                    "relative inline-flex items-center w-full",
                                                    isDragPreviewCell(rowIndex, colIndex) && "bg-blue-100 dark:bg-blue-900/30"
                                                )}
                                                >
                                                <NumericFormat
                                                    value={displayValue}
                                                    onValueChange={(values) => {
                                                    if (!isEditable) return;
                                                    const raw = values.value;
                                                    const num = raw === "" ? 0 : Number(raw);
                                                    const stored = isPercentField ? num / 100 : num;
                                                    handleInputChange(entry.rowKey, stat.id, stored.toString());
                                                    }}
                                                    thousandSeparator={false}
                                                    decimalScale={decimalScale}
                                                    allowNegative={false}
                                                    placeholder={stat.placeholder}
                                                    readOnly={!isEditable}
                                                    disabled={!isEditable}
                                                    data-qa-id={`input-${stat.id}-${entry.rowKey}`}
                                                    className={`text-right w-full ${
                                                    isEditable && isFieldModified(entry.rowKey, stat.id)
                                                        ? "border-blue-600 bg-blue-50 dark:bg-slate-800"
                                                        : ""
                                                    } rounded-sm border border-input bg-background px-1 py-0.5
                                                    focus-visible:outline-none focus-visible:ring-1
                                                    disabled:cursor-not-allowed disabled:opacity-50 ${
                                                        varianceValues ? "pr-6" : ""
                                                    }`}
                                                    suffix={isPercentField ? "%" : undefined}
                                                />

                                                {varianceValues && (
                                                    <div className="absolute right-1 top-1/2 transform -translate-y-1/2">
                                                    <VarianceIndicator
                                                        actualValue={varianceValues.actualValue}
                                                        forecastValue={varianceValues.forecastValue}
                                                        className="ml-0"
                                                    />
                                                    </div>
                                                )}
                                                </div>
                                            </div>
                                            </TableCell>
                                        );
                                    })}
                                    <TableCell className="text-right px-1 py-0.5">
                                        <div className="flex flex-col items-end">
                                            <div className="inline-flex items-center w-full justify-between">
                                                <span>
                                                    {(timePeriod !== "MONTHLY" && actualValues[entry.rowKey] && actualValues[entry.rowKey]?.["external-revenue"] != null && viewMode !== 'budget')
                                                        ? formatCurrency(actualValues[entry.rowKey]?.["external-revenue"])
                                                        : (timePeriod === "MONTHLY")
                                                            ? (() => {
                                                                // Sum external revenue over all daily entries for the month
                                                                const monthData = allMonthsData.find(m => m.periodLabel === entry.rowKey);
                                                                if (!monthData) return formatCurrency(0);
                                                                if (viewMode === 'budget') {
                                                                    const sumBudget = (monthData.budgetData || []).reduce((acc, d) => acc + Number(d.externalRevenue || 0), 0);
                                                                    return formatCurrency(sumBudget);
                                                                } else {
                                                                    const now = new Date();
                                                                    const currentMonthKey = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;

                                                                    const parseDetailDate = (d: SiteStatisticDetailData): Date | null => {
                                                                        let key = d.periodStart || d.periodLabel || "";
                                                                        if (/^\d{4}-\d{2}-\d{2}$/.test(key)) {
                                                                            const [y, m, day] = key.split('-').map(Number);
                                                                            return new Date(y, m - 1, day);
                                                                        }
                                                                        if (/^\d{2}\/\d{2}\/\d{4}$/.test(key)) {
                                                                            const [m, day, y] = key.split('/').map(Number);
                                                                            return new Date(y, m - 1, day);
                                                                        }
                                                                        return null;
                                                                    };

                                                                    if (entry.rowKey === currentMonthKey) {
                                                                        // Determine cutoff date: prefer ExternalRevenueLastDate from actuals; fallback to max actual date; otherwise today
                                                                        const parseCutoff = (val: string | null | undefined): Date | null => {
                                                                            if (!val) return null;
                                                                            if (/^\d{4}-\d{2}-\d{2}$/.test(val)) {
                                                                                const [y, m, d] = val.split('-').map(Number);
                                                                                return new Date(y, m - 1, d);
                                                                            }
                                                                            if (/^\d{2}\/\d{2}\/\d{4}$/.test(val)) {
                                                                                const [m, d, y] = val.split('/').map(Number);
                                                                                return new Date(y, m - 1, d);
                                                                            }
                                                                            return null;
                                                                        };
                                                                        const extDates: Date[] = [];
                                                                        (monthData.actualData || []).forEach(d => {
                                                                            const cd = parseCutoff((d as any).externalRevenueLastDate as string | undefined);
                                                                            if (cd) extDates.push(cd);
                                                                        });
                                                                        let cutoff = extDates.length ? new Date(Math.max.apply(null, extDates.map(t => t.getTime()))) : null;
                                                                        if (!cutoff) {
                                                                            const actualDates = (monthData.actualData || [])
                                                                                .map(parseDetailDate)
                                                                                .filter((d): d is Date => !!d);
                                                                            if (actualDates.length) cutoff = new Date(Math.max.apply(null, actualDates.map(t => t.getTime())));
                                                                        }
                                                                        const cutoffMidnight = cutoff ? new Date(cutoff.getFullYear(), cutoff.getMonth(), cutoff.getDate()) : new Date(now.getFullYear(), now.getMonth(), now.getDate());

                                                                        // If externalRevenueLastDate is not within current month, use forecast only
                                                                        const isExtDateInCurrentMonth = extDates.some(d => d.getFullYear() === now.getFullYear() && d.getMonth() === now.getMonth());
                                                                        if (!isExtDateInCurrentMonth) {
                                                                            const forecastOnlySum = (monthData.forecastData || [])
                                                                                .reduce((acc, d) => acc + Number(d.externalRevenue || 0), 0);
                                                                            return formatCurrency(forecastOnlySum);
                                                                        }

                                                                        const actualSum = (monthData.actualData || [])
                                                                            .filter(d => {
                                                                                const dt = parseDetailDate(d);
                                                                                return !!dt && dt <= cutoffMidnight;
                                                                            })
                                                                            .reduce((acc, d) => acc + Number(d.externalRevenue || 0), 0);

                                                                        // Forecast for days strictly AFTER cutoff
                                                                        const forecastSum = (monthData.forecastData || [])
                                                                            .filter(d => {
                                                                                const dt = parseDetailDate(d);
                                                                                return !!dt && dt > cutoffMidnight;
                                                                            })
                                                                            .reduce((acc, d) => acc + Number(d.externalRevenue || 0), 0);

                                                                        return formatCurrency(actualSum + forecastSum);
                                                                    } else {
                                                                        // Other months: past -> actuals only; future -> forecast only
                                                                        const now2 = new Date();
                                                                        const currentMonthKey2 = `${now2.getFullYear()}-${String(now2.getMonth() + 1).padStart(2, '0')}`;
                                                                        const isPastMonth2 = entry.rowKey < currentMonthKey2;
                                                                        if (isPastMonth2) {
                                                                            const actualSum = (monthData.actualData || []).reduce((acc, d) => acc + Number(d.externalRevenue || 0), 0);
                                                                            return formatCurrency(actualSum);
                                                                        } else {
                                                                            const forecastSum = (monthData.forecastData || []).reduce((acc, d) => acc + Number(d.externalRevenue || 0), 0);
                                                                            return formatCurrency(forecastSum);
                                                                        }
                                                                    }
                                                                }
                                                            })()
                                                            : (timePeriod === "WEEKLY")
                                                                ? (() => {
                                                                    // Weekly: keep existing aggregate read from API (single item)
                                                                    let forecastPeriod: SiteStatisticDetailData | undefined;
                                                                    let budgetPeriod: SiteStatisticDetailData | undefined;
                                                                    for (const monthData of allMonthsData) {
                                                                        if (!forecastPeriod) {
                                                                            forecastPeriod = monthData.forecastData?.find(d =>
                                                                                (d.periodStart && d.periodStart === entry.rowKey) ||
                                                                                (d.periodLabel && d.periodLabel === entry.displayLabel)
                                                                            );
                                                                        }
                                                                        if (!budgetPeriod) {
                                                                            budgetPeriod = monthData.budgetData?.find(d =>
                                                                                (d.periodStart && d.periodStart === entry.rowKey) ||
                                                                                (d.periodLabel && d.periodLabel === entry.displayLabel)
                                                                            );
                                                                        }
                                                                    }
                                                                    if (viewMode === 'budget') {
                                                                        return formatCurrency(budgetPeriod?.externalRevenue || 0);
                                                                    } else {
                                                                        return formatCurrency(forecastPeriod?.externalRevenue || budgetPeriod?.externalRevenue || 0);
                                                                    }
                                                                })()
                                                                : (viewMode === 'budget' && timePeriod === "DAILY"
                                                                    ? formatCurrency(budgetValues[entry.rowKey]?.["external-revenue"] || 0)
                                                                    : formatCurrency(calculateExternalRevenue(entry.rowKey)))
                                                    }
                                                </span>
                                                {/* Variance indicator for External Revenue - only for past periods with both actual and forecast data, not in budget view */}
                                                {actualValues[entry.rowKey] && actualValues[entry.rowKey]?.["external-revenue"] != null && 
                                                    (() => {
                                                        // Check if forecast/budget data exists for this period
                                                        if (timePeriod === "MONTHLY" || timePeriod === "WEEKLY") {
                                                            let forecastPeriod: SiteStatisticDetailData | undefined;
                                                            let budgetPeriod: SiteStatisticDetailData | undefined;
                                                            for (const monthData of allMonthsData) {
                                                                if (!forecastPeriod) {
                                                                    forecastPeriod = monthData.forecastData?.find(d =>
                                                                        (d.periodStart && d.periodStart === entry.rowKey) ||
                                                                        (d.periodLabel && d.periodLabel === entry.displayLabel) ||
                                                                        (timePeriod === "MONTHLY" && monthData.periodLabel === entry.rowKey)
                                                                    );
                                                                }
                                                                if (!budgetPeriod) {
                                                                    budgetPeriod = monthData.budgetData?.find(d =>
                                                                        (d.periodStart && d.periodStart === entry.rowKey) ||
                                                                        (d.periodLabel && d.periodLabel === entry.displayLabel) ||
                                                                        (timePeriod === "MONTHLY" && monthData.periodLabel === entry.rowKey)
                                                                    );
                                                                }
                                                            }
                                                            return (forecastPeriod ?? budgetPeriod) !== undefined;
                                                        }
                                                        return true; // For DAILY, assume forecast data exists if we have form values
                                                    })() && !isCurrentPd && viewMode === 'flash' && (
                                                    <VarianceIndicator
                                                        actualValue={actualValues[entry.rowKey]?.["external-revenue"]}
                                                        forecastValue={
                                                            (timePeriod === "MONTHLY" || timePeriod === "WEEKLY")
                                                                ? (() => {
                                                                    let forecastPeriod: SiteStatisticDetailData | undefined;
                                                                    let budgetPeriod: SiteStatisticDetailData | undefined;
                                                                    for (const monthData of allMonthsData) {
                                                                        if (!forecastPeriod) {
                                                                            forecastPeriod = monthData.forecastData?.find(d =>
                                                                                (d.periodStart && d.periodStart === entry.rowKey) ||
                                                                                (d.periodLabel && d.periodLabel === entry.displayLabel) ||
                                                                                (timePeriod === "MONTHLY" && monthData.periodLabel === entry.rowKey)
                                                                            );
                                                                        }
                                                                        if (!budgetPeriod) {
                                                                            budgetPeriod = monthData.budgetData?.find(d =>
                                                                                (d.periodStart && d.periodStart === entry.rowKey) ||
                                                                                (d.periodLabel && d.periodLabel === entry.displayLabel) ||
                                                                                (timePeriod === "MONTHLY" && monthData.periodLabel === entry.rowKey)
                                                                            );
                                                                        }
                                                                    }
                                                                    return (forecastPeriod?.externalRevenue ?? budgetPeriod?.externalRevenue ?? 0);
                                                                })()
                                                                : calculateExternalRevenue(entry.rowKey)
                                                        }
                                                        className="ml-2"
                                                    />
                                                )}
                                            </div>
                                        </div>
                                    </TableCell>
                                </TableRow>
                                {needsWeekendSeparatorAfter() && (
                                    <TableRow key={`weekend-after-${entry.rowKey}`} className="border-0">
                                        <TableCell colSpan={displayStatistics.length + 2} className="h-2 px-0 py-1 border-0 bg-gray-100 dark:bg-gray-700">
                                        </TableCell>
                                    </TableRow>
                                )}
                            </React.Fragment>
                        );
                    })
                )}
            </TableBody>
        </Table>
        
        {/* Statistics Legend */}
        <div className="mt-4 p-3 bg-gray-50 dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-700">
            <h4 className="text-sm font-medium mb-2 text-gray-700 dark:text-gray-300">Statistics Legend</h4>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-2 text-xs">
                <div className="flex items-center gap-2">
                    <div className="w-4 h-4 bg-gray-100 dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded"></div>
                    <span className="text-gray-600 dark:text-gray-400">Past periods with complete actual data</span>
                </div>
                <div className="flex items-center gap-2">
                    <div className="w-4 h-4 bg-orange-200 dark:bg-[#ec500267] border border-orange-300 dark:border-orange-500 rounded"></div>
                    <span className="text-gray-600 dark:text-gray-400">Current week/month (partial actual data)</span>
                </div>
                <div className="flex items-center gap-2">
                    <span className="text-green-600 dark:text-green-400 font-bold">▲</span>
                    <span className="text-red-600 dark:text-red-400 font-bold">▼</span>
                    <span className="text-gray-600 dark:text-gray-400">Variance indicators (actual vs forecast)</span>
                </div>
            </div>
            <div className="mt-2 text-xs text-gray-500 dark:text-gray-400">
                Note: Variance indicators only appear for past periods with complete actual data.
            </div>
        </div>
        </div>
    );
}
