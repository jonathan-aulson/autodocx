import { useSpreadsheetNavigation } from "@/hooks/useSpreadsheetNavigation";
import React, { useRef, useState, useEffect, forwardRef, useImperativeHandle, useMemo } from "react";
import {
    PARKING_RATE_TYPE_MAPPING,
    PARKING_RATE_TYPE_NAMES,
    ParkingRateData,
    ParkingRateDetailData
} from "@/lib/models/ParkingRates";
import { Customer } from "@/lib/models/Statistics";
import { ChevronDown, ChevronUp, Info } from "lucide-react";
import { NumericFormat } from "react-number-format";
import { Alert, AlertDescription } from "../../ui/alert";
import { Button } from "../../ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "../../ui/card";
import { Skeleton } from "../../ui/skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "../../ui/table";
import { useToast } from "../../ui/use-toast";
import useDragAndCopy, { DragCell } from "@/hooks/useDragAndCopy";
import { VarianceIndicator } from "@/components/Forecast/Statistics/components/VarianceIndicator";
import { ViewModeRadioGroup } from "../ViewModeRadioGroup";


interface ParkingRateFormProps {
    customers: Customer[];
    error: string | null;
    isParkingRateGuideExpanded: boolean;
    setIsParkingRateGuideExpanded: (value: boolean) => void;
    selectedSite: string;
    startingMonth: string;
    hasUnsavedChanges: boolean;
    setHasUnsavedChanges: (dirty: boolean) => void;
    onLoadingChange?: (loading: boolean) => void;
    onParkingRatesSaved?: () => void;
}

const ParkingRateForm = forwardRef(function ParkingRateForm(
    {
        customers,
        error,
        isParkingRateGuideExpanded: isGuideExpanded,
        setIsParkingRateGuideExpanded: setIsGuideExpanded,
        selectedSite,
        startingMonth,
        hasUnsavedChanges,
        setHasUnsavedChanges,
        onLoadingChange,
        onParkingRatesSaved
    }: ParkingRateFormProps,
    ref
) {
    const [isLoadingRates, setIsLoadingRates] = useState(false);
    const [manualActiveCell, setManualActiveCell] = useState<{ rowIndex: number; colIndex: number } | null>(null);


    useEffect(() => {
        if (onLoadingChange) {
            onLoadingChange(isLoadingRates);
        }
    }, [isLoadingRates, onLoadingChange]);
    const [isSaving, setIsSaving] = useState(false);
    const [parkingRateId, setParkingRateId] = useState<string>("");

    const [selectedYear, setSelectedYear] = useState<number>(new Date().getFullYear());

    // Replace showBudgetedRates with viewMode
    const [viewMode, setViewMode] = useState<'flash' | 'budget' | 'priorYear'>('flash');
    const [isParkingRatesExpanded, setIsParkingRatesExpanded] = useState<boolean>(true);

    const [actualRates, setActualRates] = useState<ParkingRateDetailData[]>([]);
    const [budgetRates, setBudgetRates] = useState<ParkingRateDetailData[]>([]);
    const [forecastRates, setForecastRates] = useState<ParkingRateDetailData[]>([]);
    const [pendingAction, setPendingAction] = useState<{ type: string, payload?: any } | null>(null);
    const [editedCells, setEditedCells] = useState<Record<string, Record<number, number>>>({});
    const [savedEditedCells, setSavedEditedCells] = useState<Record<string, Record<number, number>>>({});
    const tableRef = useRef<HTMLTableElement>(null);
    // For spreadsheet-like replace-on-type
    const shouldReplaceOnNextInput = useRef(false);

    const suppressDirtyCheckRef = useRef(false);

    const { toast } = useToast();

    const months = useMemo(() => [
        "Jan", "Feb", "Mar", "Apr", "May", "Jun",
        "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
    ], []);

    const parkingRateTypes = useMemo(() => PARKING_RATE_TYPE_NAMES, []);

    useEffect(() => {
        if (!selectedSite || !selectedYear || !startingMonth) return;

        fetchParkingRates();
    }, [selectedSite, selectedYear, startingMonth]);

    useEffect(() => {
        const [year] = startingMonth.split("-");
        setSelectedYear(parseInt(year, 10));
    }, [startingMonth]);

    const fetchParkingRates = async () => {
        setIsLoadingRates(true);

        try {
            const response = await fetch(`/api/parkingRates/${selectedSite}/${selectedYear}`);

            if (!response.ok) {
                throw new Error(`Error fetching parking rates: ${response.status}`);
            }

            const data: ParkingRateData = await response.json();
            processParkingRatesData(data);
            setEditedCells({});
        } catch (err) {
            console.error('Failed to fetch parking rates:', err);
            toast({
                title: "Error",
                description: "Failed to load parking rates data. Please try again later.",
                variant: "destructive"
            });
        } finally {
            setIsLoadingRates(false);
        }
    };

    const processParkingRatesData = (data: ParkingRateData): void => {
        setParkingRateId(data.parkingRateId || "");

        const newActualRates: ParkingRateDetailData[] = [];
        const newBudgetRates: ParkingRateDetailData[] = [];
        let newForecastRates: ParkingRateDetailData[] = [];

        if (data.actualRates && data.actualRates.length > 0) {
            data.actualRates.forEach(rate => {
                newActualRates.push(rate);
            });
        }

        if (data.budgetRates && data.budgetRates.length > 0) {
            data.budgetRates.forEach(rate => {
                newBudgetRates.push(rate);
            });
        }

        if (data.forecastRates && data.forecastRates.length > 0) {
            data.forecastRates.forEach(rate => {
                newForecastRates.push(rate);
            });
        } else if (newBudgetRates.length > 0) {
            newForecastRates = newBudgetRates.map(rate => ({
                ...rate,
                parkingRateDetailId: "",
            }));
        }

        setActualRates(newActualRates);
        setBudgetRates(newBudgetRates);
        setForecastRates(newForecastRates);
    };

    const ensureCompleteRateData = (rates: ParkingRateDetailData[]): ParkingRateDetailData[] => {
        const uniqueRates = new Map<string, ParkingRateDetailData>();

        rates.forEach(rate => {
            const key = `${rate.month}-${rate.rateCategory}`;
            if (!uniqueRates.has(key) ||
                (rate.parkingRateDetailId && !uniqueRates.get(key)?.parkingRateDetailId)) {
                uniqueRates.set(key, rate);
            }
        });

        for (const rateType of parkingRateTypes) {
            const rateCategory = PARKING_RATE_TYPE_MAPPING[rateType];
            if (!rateCategory) continue;

            for (let month = 1; month <= 12; month++) {
                const key = `${month}-${rateCategory}`;

                if (!uniqueRates.has(key)) {
                    uniqueRates.set(key, {
                        parkingRateDetailId: "",
                        month: month,
                        rateCategory: rateCategory,
                        rate: 0,
                        isIncrease: false,
                        increaseAmount: 0
                    });
                }
            }
        }

        return Array.from(uniqueRates.values());
    };

    const handleSaveParkingRates = async () => {
        if (!selectedSite) {
            toast({
                title: "Error",
                description: "Please select a customer site first.",
                variant: "destructive"
            });
            return;
        }

        setIsSaving(true);

        try {
            const customer = customers.find(c => c.customerSiteId === selectedSite);
            const completeForecastRates = ensureCompleteRateData(forecastRates);

            const payload: ParkingRateData = {
                parkingRateId: parkingRateId,
                name: customer?.siteName || "",
                customerSiteId: selectedSite,
                siteNumber: customer?.siteNumber || "",
                year: selectedYear,
                actualRates: [],
                budgetRates: [],
                forecastRates: completeForecastRates
            };

            const response = await fetch(`/api/parkingRates`, {
                method: 'PATCH',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(payload),
            });

            if (!response.ok) {
                throw new Error(`Error saving parking rates: ${response.status}`);
            }

            toast({
                title: "Success",
                description: "Parking rates saved successfully."
            });

            setEditedCells({});
            setSavedEditedCells({});
            setHasUnsavedChanges(false);

            // Trigger refresh of other components (like Statistics)
            if (onParkingRatesSaved) {
                onParkingRatesSaved();
            }

        } catch (err) {
            console.error('Failed to save parking rates:', err);
            toast({
                title: "Error",
                description: "Failed to save parking rates. Please try again.",
                variant: "destructive"
            });
        } finally {
            setIsSaving(false);
        }
    };

    const applyYearChange = (value: string) => {
        setSelectedYear(parseInt(value, 10));
    };

    const handleViewModeChange = (value: string) => {
        const newViewMode = value as 'flash' | 'budget' | 'priorYear';
        if (newViewMode === 'priorYear') {
            // Prior Year is disabled for now
            return;
        }
        
        suppressDirtyCheckRef.current = true;
        if (newViewMode === 'budget') {
            // Switching from flash view to budget view - save current edits and clear them
            setSavedEditedCells(editedCells);
            setEditedCells({});
            setHasUnsavedChanges(false);
        } else {
            // Switching from budget view back to flash view - restore edited cells
            setEditedCells(savedEditedCells);
            setHasUnsavedChanges(Object.keys(savedEditedCells).length > 0);
        }
        setViewMode(newViewMode);
    };

    // Helper to check if any forecast value differs from budget value
    const hasForecastDiffFromBudget = () => {
        for (const rateType of parkingRateTypes) {
            const rateCategory = PARKING_RATE_TYPE_MAPPING[rateType];
            if (!rateCategory) continue;
            for (let month = 1; month <= 12; month++) {
                const forecastRate = forecastRates.find(r => r.month === month && r.rateCategory === rateCategory)?.rate ?? 0;
                const budgetRate = budgetRates.find(r => r.month === month && r.rateCategory === rateCategory)?.rate ?? 0;
                if (forecastRate !== budgetRate) {
                    return true;
                }
            }
        }
        return false;
    };



    useEffect(() => {
        if (suppressDirtyCheckRef.current) {
            suppressDirtyCheckRef.current = false;
            return;
        }
        setHasUnsavedChanges(Object.keys(editedCells).length > 0);
    }, [editedCells, setHasUnsavedChanges]);

    const handleRateChange = (rateType: string, monthIndex: number, value: string) => {

        const numValue = value?.trim() === "" || isNaN(parseFloat(value)) ? 0 : parseFloat(value);

        const rateCategory = PARKING_RATE_TYPE_MAPPING[rateType];
        if (!rateCategory) return;

        if (viewMode === 'flash') {
            const updatedRates = [...forecastRates];

            const existingRateIndex = updatedRates.findIndex(
                rate => rate.month === monthIndex + 1 && rate.rateCategory === rateCategory
            );

            const budgetRate = budgetRates.find(r => r.month === monthIndex + 1 && r.rateCategory === rateCategory);
            const forecastRate = forecastRates.find(r => r.month === monthIndex + 1 && r.rateCategory === rateCategory);
            const originalValue = forecastRate?.rate ?? budgetRate?.rate ?? 0;

            let newEditedCells = { ...editedCells };
            if (!newEditedCells[rateType]) {
                newEditedCells[rateType] = {};
            }
            if (numValue !== originalValue) {
                newEditedCells[rateType][monthIndex] = numValue;
            } else {
                delete newEditedCells[rateType][monthIndex];
                if (Object.keys(newEditedCells[rateType]).length === 0) {
                    delete newEditedCells[rateType];
                }
            }
            setEditedCells(newEditedCells);

            if (existingRateIndex >= 0) {
                updatedRates[existingRateIndex] = {
                    ...updatedRates[existingRateIndex],
                    rate: numValue
                };
            } else {
                updatedRates.push({
                    parkingRateDetailId: "",
                    month: monthIndex + 1,
                    rateCategory: rateCategory,
                    rate: numValue,
                    isIncrease: false,
                    increaseAmount: 0
                });
            }

            setForecastRates(updatedRates);
        }
    };

    const getRateDisplayValue = (rateType: string, monthIndex: number): string => {
        const rateCategory = PARKING_RATE_TYPE_MAPPING[rateType];
        if (!rateCategory) return "0.00";

        const month = monthIndex + 1;

        // Budget View: ALL periods show budget values (read-only)
        if (viewMode === 'budget') {
            const budgetRate = budgetRates.find(r => r.month === month && r.rateCategory === rateCategory);
            return budgetRate ? budgetRate.rate.toFixed(2) : "0.00";
        }

        // Forecast View logic (matching Statistics/Other Expenses pattern):
        // Past months: Show actual values IN PLACE (not below) when available
        if (isPastMonth(monthIndex)) {
            const actualRate = actualRates.find(r => r.month === month && r.rateCategory === rateCategory);
            if (actualRate) {
                return actualRate.rate.toFixed(2);
            }
        }

        // Current/Future months OR past months without actual data: Show forecast, fallback to budget
const forecastRate = forecastRates.find(r => r.month === month && r.rateCategory === rateCategory);
if (forecastRate) { 
    return forecastRate.rate.toFixed(2);
}
        
        // Final fallback to budget
        const budgetRate = budgetRates.find(r => r.month === month && r.rateCategory === rateCategory);
        return budgetRate ? budgetRate.rate.toFixed(2) : "0.00";
    };

    const getBudgetedValue = (rateType: string, monthIndex: number): string => {
        const rateCategory = PARKING_RATE_TYPE_MAPPING[rateType];
        if (!rateCategory) return "0.00";

        const month = monthIndex + 1;
        const rate = budgetRates.find(r => r.month === month && r.rateCategory === rateCategory);
        return rate ? rate.rate.toFixed(2) : "0.00";
    };

    const getActualizedValue = (rateType: string, monthIndex: number): string | null => {
        const rateCategory = PARKING_RATE_TYPE_MAPPING[rateType];
        if (!rateCategory) return null;

        const month = monthIndex + 1;
        const rate = actualRates.find(r => r.month === month && r.rateCategory === rateCategory);
        return rate ? rate.rate.toFixed(2) : null;
    };

    const isFieldEdited = (rateType: string, monthIndex: number): boolean => {
        if (viewMode === 'budget') return false;

        const rateCategory = PARKING_RATE_TYPE_MAPPING[rateType];
        if (!rateCategory) return false;

        const month = monthIndex + 1;
        const forecastRate = forecastRates.find(r => r.month === month && r.rateCategory === rateCategory)?.rate ?? 0;
        const budgetRate = budgetRates.find(r => r.month === month && r.rateCategory === rateCategory)?.rate ?? 0;

        return forecastRate !== budgetRate;
    };

    const isReadOnly = (monthIndex: number): boolean => {
        // Budget view: All periods are read-only
        if (viewMode === 'budget') return true;

        // Forecast view: Past months with actual data are read-only
        if (isPastMonth(monthIndex)) {
            return true; // Past months are always read-only in forecast view
        }

        // Current/Future months are editable in forecast view
        return false;
    };

    const hasActualizedData = (monthIndex: number): boolean => {
        const month = monthIndex + 1;
        const rateExists = actualRates.some(r => r.month === month);
        return rateExists;
    };

    const isPastMonth = (monthIndex: number): boolean => {
        const month = monthIndex + 1;
        const currentYear = new Date().getFullYear();
        const currentMonth = new Date().getMonth() + 1;
        
        if (selectedYear < currentYear) {
            return true;
        }
        
        if (selectedYear === currentYear) {
            return month < currentMonth;
        }
        
        return false;
    };

    const isCurrentMonth = (monthIndex: number): boolean => {
        const month = monthIndex + 1;
        const currentYear = new Date().getFullYear();
        const currentMonth = new Date().getMonth() + 1;
        
        return selectedYear === currentYear && month === currentMonth;
    };

    const hasActualAndForecastData = (rateType: string, monthIndex: number): boolean => {
        const actualValue = getActualizedValue(rateType, monthIndex);
        const forecastValue = getForecastValue(rateType, monthIndex);
        return actualValue !== null && forecastValue !== null;
    };

   const getForecastValue = (rateType: string, monthIndex: number): string | null => {
    const rateCategory = PARKING_RATE_TYPE_MAPPING[rateType];
    if (!rateCategory) return null;

    const month = monthIndex + 1;
    const forecastRate = forecastRates.find(r => r.month === month && r.rateCategory === rateCategory);
    if (forecastRate) {
        return forecastRate.rate.toFixed(2);
    }
        
        // Fallback to budget for comparison
        const budgetRate = budgetRates.find(r => r.month === month && r.rateCategory === rateCategory);
        return budgetRate ? budgetRate.rate.toFixed(2) : null;
    };

    const getBudgetValue = (rateType: string, monthIndex: number): string | null => {
        const rateCategory = PARKING_RATE_TYPE_MAPPING[rateType];
        if (!rateCategory) return null;

        const month = monthIndex + 1;
        const budgetRate = budgetRates.find(r => r.month === month && r.rateCategory === rateCategory);
        return budgetRate ? budgetRate.rate.toFixed(2) : null;
    };

    const calculateVarianceIndicator = (rateType: string, monthIndex: number) => {
        const actualValue = getActualizedValue(rateType, monthIndex);
        const budgetValue = getBudgetValue(rateType, monthIndex);
        
        // Show variance indicator if we have both actual and budget values
        // (even if budget is 0, we want to show the comparison)
        if (actualValue === null || budgetValue === null) {
            return null;
        }
        
        return (
            <VarianceIndicator
                actualValue={parseFloat(actualValue)}
                forecastValue={parseFloat(budgetValue)}
                isExpense={false}  // Revenue context - higher actual = favorable
                className="ml-0"
            />
        );
    };

    useImperativeHandle(ref, () => ({
        save: handleSaveParkingRates
    }));

    // Spreadsheet navigation configuration
    const spreadsheetNavigation = useSpreadsheetNavigation({
        tableRef,
        rowCountCallback: () => parkingRateTypes.length,
        columnCountCallback: () => months.length,
        isCellNavigableCallback: (rowIndex: number, colIndex: number) => {
            return true; // All cells are navigable
        },
        isCellEditableCallback: (rowIndex: number, colIndex: number) => {
            return !isReadOnly(colIndex) && viewMode !== 'budget';
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
                    const rateType = parkingRateTypes[rowIndex];
                    const originalValue = getRateDisplayValue(rateType, colIndex);
                    input.value = originalValue;
                    input.blur();
                }
            }
        }
    });

    function determineStartCell(

        activeCell: { rowIndex: number; colIndex: number } | null,
        selectedCells: { rowIndex: number; colIndex: number }[]
    ): { rowIndex: number; colIndex: number } | null {
        if (activeCell) return activeCell;
        if (selectedCells.length > 0) {
            return [...selectedCells].sort((a, b) => a.rowIndex - b.rowIndex || a.colIndex - b.colIndex)[0];
        }
        return null;
    }

    function generateAutoSelection(
        startRow: number,
        startCol: number,
        rowCount: number,
        maxRows: number
    ): { rowIndex: number; colIndex: number }[] {
        const rowsToSelect = Math.min(rowCount, maxRows - startRow);
        const selection = [];
        for (let i = 0; i < rowsToSelect; i++) {
            selection.push({ rowIndex: startRow + i, colIndex: startCol });
        }
        return selection;
    }

    function generatePastedSelection(
        startRow: number,
        startCol: number,
        pastedRows: number,
        pastedCols: number,
        maxRows: number,
        maxCols: number
    ): { rowIndex: number; colIndex: number }[] {
        const selection = [];
        for (let i = 0; i < pastedRows; i++) {
            const targetRow = startRow + i;
            if (targetRow >= maxRows) break;
            selection.push({ rowIndex: targetRow, colIndex: startCol });

        }
        return selection;
    }

    // Drag/copy cell highlighting effect

    const {
        dragPreviewCells,
        handleDragStart,
        handleDragMove,
        handleDragEnd,
        isDragPreviewCell,
        isDragging
    } = useDragAndCopy({
        onCopy: (cells) => {
            return cells.map(cell => {
                const rateType = parkingRateTypes[cell.rowIndex];
                return getRateDisplayValue(rateType, cell.colIndex);
            });
        },
        onPaste: (cells, clipboard) => {
            // Parse clipboard into 2D array (rows/columns)
            let parsedRows: string[][] = [];
            if (clipboard.length === 1 && clipboard[0].includes("\n")) {
                parsedRows = clipboard[0].split("\n").map(row => row.split("\t"));
            } else if (clipboard.length === 1 && clipboard[0].includes("\t")) {
                parsedRows = [clipboard[0].split("\t")];
            } else {
                parsedRows = clipboard.map(row => [row]);
            }

            // Remove trailing empty row if present (from Excel copy)
            if (parsedRows.length > 1 && parsedRows[parsedRows.length - 1].every(cell => cell === "")) {
                parsedRows.pop();
            }

            if (parsedRows.length === 0) return;

            const startCell = determineStartCell(manualActiveCell ?? spreadsheetNavigation.activeCell, dragPreviewCells);
            if (!startCell) return;

            const { rowIndex: startRow, colIndex: startCol } = startCell;

            let updatedRates = [...forecastRates];
            let newEditedCells = { ...editedCells };

            for (let i = 0; i < parsedRows.length; i++) {
                const rowVals = parsedRows[i];
                const targetRow = startRow + i;
                if (targetRow >= parkingRateTypes.length) break;

                const rateType = parkingRateTypes[targetRow];
                const rateCategory = PARKING_RATE_TYPE_MAPPING[rateType];
                if (!rateCategory) continue;

                for (let j = 0; j < rowVals.length; j++) {
                    const targetCol = startCol + j;
                    if (targetCol >= months.length) break;

                    const value = rowVals[j] ?? "";
                    const numValue = value === "" ? 0 : parseFloat(value);
                    const month = targetCol + 1;

                    const existingRateIndex = updatedRates.findIndex(
                        rate => rate.month === month && rate.rateCategory === rateCategory
                    );
                    const budgetRate = budgetRates.find(
                        r => r.month === month && r.rateCategory === rateCategory
                    );
                    const forecastRate = forecastRates.find(
                        r => r.month === month && r.rateCategory === rateCategory
                    );
                    const originalValue = forecastRate?.rate ?? budgetRate?.rate ?? 0;

                    if (!newEditedCells[rateType]) {
                        newEditedCells[rateType] = {};
                    }

                    if (numValue !== originalValue) {
                        newEditedCells[rateType][targetCol] = numValue;
                    } else {
                        delete newEditedCells[rateType][targetCol];
                        if (Object.keys(newEditedCells[rateType]).length === 0) {
                            delete newEditedCells[rateType];
                        }
                    }

                    if (existingRateIndex >= 0) {
                        updatedRates = [
                            ...updatedRates.slice(0, existingRateIndex),
                            { ...updatedRates[existingRateIndex], rate: numValue },
                            ...updatedRates.slice(existingRateIndex + 1)
                        ];
                    } else {
                        updatedRates = [
                            ...updatedRates,
                            {
                                parkingRateDetailId: "",
                                month,
                                rateCategory,
                                rate: numValue,
                                isIncrease: false,
                                increaseAmount: 0
                            }
                        ];
                    }
                }
            }

            setForecastRates(updatedRates);
            setEditedCells(newEditedCells);
            setHasUnsavedChanges(Object.keys(newEditedCells).length > 0);
        },
        activeCell: manualActiveCell,
        rowCount: parkingRateTypes.length
    });

    return (
        <div className="w-full p-1 space-y-6">
            <div className="flex justify-between items-start mb-4">
                <div>
                    <h1 className="text-2xl font-bold tracking-tight">Parking Rates</h1>
                    <p className="text-muted-foreground">Manage parking rates for the properties you manage.</p>
                    {error && <p className="text-red-500 mt-2">{error}</p>}
                </div>
            </div>
            <Button
                variant="outline"
                onClick={() => setIsGuideExpanded(!isGuideExpanded)}
                className="flex items-center gap-2"
                data-qa-id="parking-rates-button-toggle-guide"
            >
                <Info className="h-4 w-4" />
                {isGuideExpanded ? "Hide Guide" : "Show Guide"}
                {isGuideExpanded ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
            </Button>

            {isGuideExpanded && (
                <div className="space-y-6 p-6 border-2 border-border rounded-lg bg-muted dark:bg-gray-900 text-card-foreground mb-6 shadow-sm">
                    <div className="border-b-2 border-border pb-3">
                        <h3 className="text-xl font-semibold text-foreground">Parking Rates — Guide</h3>
                    </div>

                    <div className="space-y-6">
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                            <div className="p-4 border border-border rounded-lg bg-card shadow-sm">
                                <h4 className="font-semibold mb-3 text-foreground border-b border-border pb-2">Purpose</h4>
                                <ul className="list-disc pl-5 space-y-1 text-muted-foreground">
                                    <li>Maintain the forecasted prices used with your Stats to calculate External Revenue.</li>
                                    <li>Compare your Actualized volume against your Forecast by viewing the Variance Indicators</li>
                                    <ul className="list-disc pl-5 space-y-1 mt-1">
                                        <li><span className="text-green-600 dark:text-green-400">Green ▲</span> if Actual &gt; Forecast</li>
                                        <li>Black ● if Actual = Forecast</li>
                                        <li><span className="text-red-600 dark:text-red-400">Red ▼</span> if Actual &lt; Forecast</li>
                                    </ul>
                                </ul>
                            </div>

                            <div className="space-y-4">
                                <div className="p-4 border border-border rounded-lg bg-card shadow-sm">
                                    <h4 className="font-semibold mb-3 text-foreground border-b border-border pb-2">What to enter</h4>
                                    <ol className="list-decimal pl-5 space-y-1 text-muted-foreground">
                                        <li>Rates by revenue code family (e.g., Self Daily, Valet Daily/Overnight, Monthly).</li>
                                        <li>Planned rate changes and escalators with effective dates.</li>
                                    </ol>
                                </div>

                                <div className="p-4 border border-border rounded-lg bg-card shadow-sm">
                                    <h4 className="font-semibold mb-3 text-foreground border-b border-border pb-2">How your inputs are used</h4>
                                    <ol className="list-decimal pl-5 space-y-1 text-muted-foreground">
                                        <li>External Revenue forecast updates automatically as Stats × Rates.</li>
                                        <li>Escalators apply beginning on the effective date.</li>
                                    </ol>
                                </div>
                            </div>
                        </div>

                        {/* Second row: Tips and guardrails (full width) */}
                        <div className="p-4 border border-border rounded-lg bg-card shadow-sm">
                            <h4 className="font-semibold mb-3 text-foreground border-b border-border pb-2">Tips and guardrails</h4>
                            <ol className="list-decimal pl-5 space-y-1 text-muted-foreground">
                                <li>Keep rate changes documented (event pricing, season shifts).</li>
                                <li>Ensure the right rate is applied to the right service (Self vs. Valet vs. Overnight vs. Monthly).</li>
                            </ol>
                        </div>
                    </div>
                </div>
            )}

            <Card className="w-full">
                <CardHeader className="flex flex-row items-center justify-between">
                    <CardTitle>Parking Rates</CardTitle>
                    <ViewModeRadioGroup
                        viewMode={viewMode}
                        onViewModeChange={handleViewModeChange}
                        disabled={!selectedSite || !selectedYear || isLoadingRates}
                    />
                </CardHeader>
                {isParkingRatesExpanded && (
                    <CardContent>

                        <div className="overflow-x-auto">
                            {isLoadingRates ? (
                                <div className="p-4">
                                    <Skeleton className="h-[400px] w-full" />
                                </div>
                            ) : (
                                <Table ref={tableRef} className="w-full table-fixed" {...spreadsheetNavigation.tableProps}>
                                    <TableHeader>
                                        <TableRow>
                                            <TableHead className="w-[120px] text-left px-1 py-0.5 font-medium text-xs sticky left-0 z-20 bg-muted">Rate Type</TableHead>
                                            {months.map((month) => (
                                                <TableHead
                                                    key={month}
                                                    className="w-[100px] text-center px-1 py-0.5 font-medium text-xs whitespace-normal break-words"
                                                >
                                                    {month}
                                                </TableHead>
                                            ))}
                                        </TableRow>
                                    </TableHeader>
                                    <TableBody>
                                        {parkingRateTypes.map((rateType, rowIndex) => (
                                            <TableRow key={rateType}>
                                                <TableCell className="px-1 py-0.5 text-xs font-medium sticky left-0 z-20 bg-background">
                                                    {rateType}
                                                </TableCell>
                                                {months.map((_, monthIndex) => {
                                                    // Add background colors for different time periods
                                                    const cellBackgroundClass = isPastMonth(monthIndex)
                                                        ? "bg-gray-100 dark:bg-gray-800"
                                                        : isCurrentMonth(monthIndex) && viewMode === 'flash'
                                                            ? "bg-orange-200 dark:bg-[#ec500267]"
                                                            : "";
                                                    
                                                    return (
                                                    <TableCell
                                                        key={`${rateType}-${monthIndex}`}
                                                        data-row={rowIndex}
                                                        data-col={monthIndex}
                                                        data-cell-id={`cell-${rowIndex}-${monthIndex}`}
                                                        className={`px-1 py-0.5 text-center text-xs min-w-[100px] ${!isReadOnly(monthIndex) ? 'cursor-ns-resize' : ''} ${isDragPreviewCell(rowIndex, monthIndex) || (spreadsheetNavigation.activeCell?.rowIndex === rowIndex && spreadsheetNavigation.activeCell?.colIndex === monthIndex) ? "border-2 border-blue-500 z-10 relative" : ""} ${cellBackgroundClass}`}
                                                        {...spreadsheetNavigation.getCellProps(rowIndex, monthIndex)}
                                                        onMouseDown={(e) => {
                                                            if (e.button !== 0) return;
                                                            setManualActiveCell({ rowIndex, colIndex: monthIndex });
                                                            // Clear drag selection if not dragging
                                                            if (!isDragging) {
                                                                // @ts-ignore
                                                                if (typeof dragPreviewCells !== "undefined" && dragPreviewCells.length > 0) {
                                                                    // Clear dragPreviewCells by calling handleDragEnd
                                                                    handleDragEnd();
                                                                }
                                                            }
                                                            handleDragStart(rowIndex, monthIndex);
                                                        }}
                                                        onMouseMove={(e) => {
                                                            if (isDragging) {
                                                                handleDragMove(rowIndex, monthIndex);
                                                            }
                                                        }}
                                                        onMouseUp={handleDragEnd}
                                                    >
                                                        <div className="relative inline-flex items-center w-full">
                                                           <NumericFormat
                                                    value={getRateDisplayValue(rateType, monthIndex)}
                                                    onValueChange={(values) => {
                                                     handleRateChange(rateType, monthIndex, values.value);
                                                        }}
                                                             onFocus={(e) => {                                                
                                                   e.target.select();
                                                   }}
                                                        thousandSeparator={true}
                                                               prefix="$"
                                                      decimalScale={2}
                                                     fixedDecimalScale={true}
                                                     allowNegative={false}
                                                     placeholder="0.00"
                                                     readOnly={isReadOnly(monthIndex)}
                                                      disabled={isReadOnly(monthIndex)}
                                                     data-qa-id={`parking-rates-input-${rateType.toLowerCase().replace(' ', '-')}-${monthIndex}`}
                                                     className={`text-right w-full ${hasActualAndForecastData(rateType, monthIndex) && viewMode !== 'budget' && isPastMonth(monthIndex) ? "pr-6" : ""} ${viewMode !== 'budget' && isFieldEdited(rateType, monthIndex) && !isReadOnly(monthIndex)
                                                       ? "border-blue-600 bg-blue-50 dark:bg-slate-800"
                                                         : ""
                                                         } rounded-sm border border-input bg-background px-1 py-0.5 
                                                              focus-visible:outline-none focus-visible:ring-1 
                                                        disabled:cursor-not-allowed disabled:opacity-50`}
                                                           />
                                                            {/* Show variance indicators for past periods with actual data (comparing actual vs forecast) */}
                                                            {hasActualAndForecastData(rateType, monthIndex) && viewMode !== 'budget' && isPastMonth(monthIndex) && (
                                                                <div className="absolute right-1 top-1/2 transform -translate-y-1/2">
                                                                    {calculateVarianceIndicator(rateType, monthIndex)}
                                                                </div>
                                                            )}
                                                        </div>
                                                    </TableCell>
                                                    );
                                                })}
                                            </TableRow>
                                        ))}
                                    </TableBody>
                                </Table>
                            )}
                        </div>
                    </CardContent>
                )}
            </Card>
        </div>
    );
});

export default ParkingRateForm;
