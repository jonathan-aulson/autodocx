import { Alert, AlertDescription } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip";
import { useToast } from "@/components/ui/use-toast";
import { useSpreadsheetNavigation } from "@/hooks/useSpreadsheetNavigation";
import { Contract } from "@/lib/models/Contract";
import { OtherExpenseDto } from "@/lib/models/OtherExpenses";
import { Customer } from "@/lib/models/Statistics";
import { ChevronDown, ChevronUp, Info } from "lucide-react";
import { forwardRef, useEffect, useImperativeHandle, useRef, useState } from "react";
import { NumericFormat } from "react-number-format";
import useDragAndCopy from "@/hooks/useDragAndCopy";
import { VarianceIndicator } from "@/components/Forecast/Statistics/components/VarianceIndicator";
import { ViewModeRadioGroup } from "../ViewModeRadioGroup";


interface OtherExpensesProps {
    customers: Customer[];
    selectedSite: string;
    startingMonth: string;
    isGuideOpen: boolean;
    setIsGuideOpen: (value: boolean) => void;
    hasUnsavedChanges: boolean;
    setHasUnsavedChanges: (dirty: boolean) => void;
    onLoadingChange?: (loading: boolean) => void;
    contractDetails?: Contract | null;
}

const EXPENSE_TYPES = [
    { id: "employeeRelations", label: "Employee Relations" },
    { id: "fuelVehicles", label: "Fuel Vehicles" },
    { id: "lossAndDamageClaims", label: "Loss & Damage Claims" },
    { id: "officeSupplies", label: "Office Supplies" },
    { id: "outsideServices", label: "Outside Services" },
    { id: "rentsParking", label: "Rents Parking" },
    { id: "repairsAndMaintenance", label: "Repairs & Maintenance" },
    { id: "repairsAndMaintenanceVehicle", label: "Repairs & Maintenance Vehicle" },
    { id: "signage", label: "Signage" },
    { id: "suppliesAndEquipment", label: "Supplies & Equipment" },
    { id: "ticketsAndPrintedMaterial", label: "Tickets & Printed Material" },
    { id: "uniforms", label: "Uniforms" },
    { id: "miscOtherExpenses", label: "Misc Other Expenses" },
    { id: "totalOtherExpenses", label: "Total Other Expenses" }
];

// Map expense type id to account number for headers
const EXPENSE_ACCOUNT_CODES: Record<string, string> = {
    employeeRelations: "7045",
    fuelVehicles: "7075",
    lossAndDamageClaims: "7100",
    officeSupplies: "7113",
    outsideServices: "7115",
    rentsParking: "7170",
    repairsAndMaintenance: "7175",
    repairsAndMaintenanceVehicle: "7178",
    signage: "7180",
    suppliesAndEquipment: "7185",
    ticketsAndPrintedMaterial: "7205",
    uniforms: "7220"
};

type ExpenseTypeId = typeof EXPENSE_TYPES[number]["id"];

type EditedCells = Record<string, Record<ExpenseTypeId, number>>;

const EXPENSE_TYPE_KEYS = {
    employeeRelations: "employeeRelations",
    fuelVehicles: "fuelVehicles",
    lossAndDamageClaims: "lossAndDamageClaims",
    officeSupplies: "officeSupplies",
    outsideServices: "outsideServices",
    rentsParking: "rentsParking",
    repairsAndMaintenance: "repairsAndMaintenance",
    repairsAndMaintenanceVehicle: "repairsAndMaintenanceVehicle",
    signage: "signage",
    suppliesAndEquipment: "suppliesAndEquipment",
    ticketsAndPrintedMaterial: "ticketsAndPrintedMaterial",
    uniforms: "uniforms",
    miscOtherExpenses: "miscOtherExpenses",
    totalOtherExpenses: "totalOtherExpenses"
} as const;

type ExpenseTypeKey = keyof typeof EXPENSE_TYPE_KEYS;

// Helper function to check if an expense account is billable
const isExpenseAccountBillable = (expenseCode: string, contractDetails?: Contract | null): boolean => {
    if (!contractDetails?.billableAccounts?.enabled) return false;
    
    if (!contractDetails.billableAccounts.billableAccountsData?.[0]?.payrollExpenseAccountsData) {
        return false;
    }
    
    try {
        const payrollExpenseAccounts = JSON.parse(contractDetails.billableAccounts.billableAccountsData[0].payrollExpenseAccountsData);
        const account = payrollExpenseAccounts.find((acc: any) => acc.code === expenseCode);
        return account?.isEnabled === true;
    } catch (error) {
        console.error('Error parsing payrollExpenseAccountsData:', error);
        return false;
    }
};

const OtherExpenses = forwardRef(function OtherExpenses(
    {
        customers,
        selectedSite,
        startingMonth,
        isGuideOpen,
        setIsGuideOpen,
        hasUnsavedChanges,
        setHasUnsavedChanges,
        onLoadingChange,
        contractDetails
    }: OtherExpensesProps,
    ref
) {
    const [data, setData] = useState<OtherExpenseDto | null>(null);
    const [isLoading, setIsLoading] = useState(false);
    const [isSaving, setIsSaving] = useState(false);
    const [viewMode, setViewMode] = useState<'flash' | 'budget' | 'priorYear'>('flash');
    const [editedCells, setEditedCells] = useState<EditedCells>({});
    const [lastFetchKey, setLastFetchKey] = useState<string>("");
    // For spreadsheet-like replace-on-type (per-cell)
    const replaceOnNextInputRef = useRef<{ [key: string]: boolean }>({});


    const tableRef = useRef<HTMLTableElement>(null);



    const { toast } = useToast();

    useEffect(() => {
        if (onLoadingChange) {
            onLoadingChange(isLoading);
        }
    }, [isLoading, onLoadingChange]);

    useEffect(() => {
        if (!selectedSite || !startingMonth) return;

        const currentFetchKey = `${selectedSite}-${startingMonth}`;

        // Only fetch and reset edited cells if we're actually switching to different data
        if (currentFetchKey !== lastFetchKey) {
            setIsLoading(true);
            setLastFetchKey(currentFetchKey);

            fetch(`/api/otherExpense/${selectedSite}/${startingMonth}`)
                .then(res => res.ok ? res.json() : Promise.reject(res.status))
                .then(data => {
                    setData(data);
                    // Only clear edited cells after successfully loading new data
                    // This ensures we don't lose edits if the fetch fails
                    setEditedCells({});
                })
                .catch((error) => {
                    console.error('Failed to fetch other expense data:', error);
                    setData(null);
                    // Don't clear edited cells if fetch fails - preserve user input
                })
                .finally(() => setIsLoading(false));
        }
    }, [selectedSite, startingMonth, lastFetchKey]);

    const getTimePeriods = () => {
        const [yearStr, monthStr] = startingMonth.split("-");
        const year = Number(yearStr);
        const month = Number(monthStr) - 1;
        const monthNames = [
            "January", "February", "March", "April", "May", "June",
            "July", "August", "September", "October", "November", "December"
        ];
        const periods = [];
        for (let i = 0; i < 12; i++) {
            const currentMonth = (month + i) % 12;
            const currentYear = year + Math.floor((month + i) / 12);
            periods.push({
                id: `${currentYear}-${(currentMonth + 1).toString().padStart(2, "0")}`,
                label: `${monthNames[currentMonth].substring(0, 3)} ${currentYear}`,
                date: new Date(currentYear, currentMonth, 1),
            });
        }
        return periods;
    };

    const isPastMonth = (periodDate: Date) => {
        const today = new Date();
        return periodDate < new Date(today.getFullYear(), today.getMonth(), 1);
    };

    const isCurrentMonth = (periodDate: Date) => {
        const today = new Date();
        const currentMonth = new Date(today.getFullYear(), today.getMonth(), 1);
        const periodMonth = new Date(periodDate.getFullYear(), periodDate.getMonth(), 1);
        return currentMonth.getTime() === periodMonth.getTime();
    };

    const hasActualizedData = (periodId: string, expenseType: ExpenseTypeId): boolean => {
        const actual = data?.actualData?.find(d => d.monthYear === periodId);
        return !!(actual && EXPENSE_TYPE_KEYS[expenseType as ExpenseTypeKey] in actual);
    };

    const getDataForPeriod = (periodId: string, expenseType: ExpenseTypeId) => {
        const key = EXPENSE_TYPE_KEYS[expenseType as ExpenseTypeKey];

        const budget = data?.budgetData?.find(d => d.monthYear === periodId)?.[key];
        const forecast = data?.forecastData?.find(d => d.monthYear === periodId)?.[key];
        const actual = data?.actualData?.find(d => d.monthYear === periodId)?.[key];

        return { budget, forecast, actual };
    };

    const handleCellChange = (periodId: string, expenseType: ExpenseTypeId, value: string) => {
        const timePeriod = getTimePeriods().find(p => p.id === periodId);
        if (!timePeriod) return;

        // Don't allow changes to read-only columns, budget view, or past months
        const isReadOnlyColumn = expenseType === 'miscOtherExpenses' || expenseType === 'totalOtherExpenses';
        if (isReadOnlyColumn || viewMode === 'budget' || isPastMonth(timePeriod.date)) {
            return;
        }

        // Clear the replace flag after the first input
        const rowIndex = timePeriods.findIndex(p => p.id === periodId);
        const colIndex = EXPENSE_TYPES.findIndex(e => e.id === expenseType);
        const cellKey = `${rowIndex}-${colIndex}`;
        if (replaceOnNextInputRef.current[cellKey]) {
            replaceOnNextInputRef.current[cellKey] = false;
        }

        const parsedValue = Number.parseFloat(value);
        const finalValueForCell = value === "" ? 0 : parsedValue;

        if (value !== "" && isNaN(finalValueForCell)) {
            return;
        }

        if (finalValueForCell < 0) {
            return;
        }

        const { forecast, budget } = getDataForPeriod(periodId, expenseType);
        const effectiveBudget = budget ?? 0;
        const originalDisplayValueIfNoEdit =
            forecast !== undefined && forecast !== null && forecast !== 0
                ? forecast
                : effectiveBudget;

        const isRevert = Math.abs(finalValueForCell - originalDisplayValueIfNoEdit) < 0.01;

        let newEditedCells = { ...editedCells };
        if (newEditedCells[periodId]) {
            newEditedCells[periodId] = { ...newEditedCells[periodId] };
        }

        if (isRevert) {
            if (newEditedCells[periodId]) {
                delete newEditedCells[periodId][expenseType];
                if (Object.keys(newEditedCells[periodId]).length === 0) {
                    delete newEditedCells[periodId];
                }
            }
        } else {
            if (!newEditedCells[periodId]) {
                newEditedCells[periodId] = {};
            }
            newEditedCells[periodId][expenseType] = finalValueForCell;
        }

        setEditedCells(newEditedCells);
        setHasUnsavedChanges(Object.keys(newEditedCells).length > 0);
    };

    const getCellValue = (periodId: string, expenseType: ExpenseTypeId): number => {
        const { forecast, budget, actual } = getDataForPeriod(periodId, expenseType);
        const timePeriod = getTimePeriods().find(p => p.id === periodId);

        if (!timePeriod) return 0;

        // If in budget view, always show budget values
        if (viewMode === 'budget') {
            return budget !== undefined && budget !== null ? budget : 0;
        }

        // Forecast view logic:
        // Always return user input if it exists (preserves user changes)
        if (editedCells[periodId]?.[expenseType] !== undefined) {
            return editedCells[periodId][expenseType];
        }

        // Past periods: show actual values
        if (isPastMonth(timePeriod.date) && !isCurrentMonth(timePeriod.date)) {
            return actual !== undefined && actual !== null ? actual : 0;
        }

        // Current period: show forecast values for input field, fallback to budget
        // (Actual values are displayed separately in the dual-row layout)
        if (isCurrentMonth(timePeriod.date)) {
           if (forecast !== undefined && forecast !== null) return forecast;
            return budget !== undefined && budget !== null ? budget : 0;
        }

        // Future periods: show forecast values, fallback to budget
     if (forecast !== undefined && forecast !== null) return forecast;
        if (budget !== undefined && budget !== null) return budget;
        return 0;
    };

    const isForecastDifferentFromBudget = (periodId: string, expenseType: ExpenseTypeId) => {
        const { forecast, budget } = getDataForPeriod(periodId, expenseType);
        const forecastValue = forecast ?? 0;
        const budgetValue = budget ?? 0;
        return Math.abs(forecastValue - budgetValue) > 0.01;
    };

    const isCellModified = (periodId: string, expenseType: ExpenseTypeId) => {
        // In budget view, we're showing budget values, so no cells should appear modified
        if (viewMode === 'budget') {
            return false;
        }

        // Check if there are unsaved edits for this cell
        if (editedCells[periodId]?.[expenseType] !== undefined) {
            return true;
        }

        // Otherwise, compare saved forecast against budget to show existing modifications
        const { budget, forecast } = getDataForPeriod(periodId, expenseType);
        const budgetValue = budget ?? 0;
        const forecastValue = forecast ?? 0;
        return Math.abs(forecastValue - budgetValue) > 0.01;
    };


    const calculateTotalOtherExpenses = (periodId: string) => {
        let total = 0;

        EXPENSE_TYPES.forEach((expense) => {
            if (expense.id !== 'miscOtherExpenses' && expense.id !== 'totalOtherExpenses') {
                total += getCellValue(periodId, expense.id as ExpenseTypeId);
            }
        });

        const miscOtherExpenses = getCellValue(periodId, 'miscOtherExpenses' as ExpenseTypeId);
        total += miscOtherExpenses;

        return total;
    };

    const calculateTotalBudgetExpenses = (periodId: string) => {
        let total = 0;

        EXPENSE_TYPES.forEach((expense) => {
            if (expense.id !== 'miscOtherExpenses' && expense.id !== 'totalOtherExpenses') {
                const { budget } = getDataForPeriod(periodId, expense.id as ExpenseTypeId);
                total += budget ?? 0;
            }
        });

        // Add miscOtherExpenses budget
        const { budget: miscBudget } = getDataForPeriod(periodId, 'miscOtherExpenses');
        total += miscBudget ?? 0;

        return total;
    };


    const handleSave = async () => {
        if (!selectedSite || !startingMonth) {
            toast({
                title: "Error",
                description: "Please select a customer site and starting month.",
                variant: "destructive"
            });
            return;
        }

        setIsSaving(true);

        const timePeriods = getTimePeriods();

        const forecastData: any[] = [];

        timePeriods.forEach(period => {
            const existingForecast = data?.forecastData?.find(d => d.monthYear === period.id);
            const budgetForPeriod = data?.budgetData?.find(d => d.monthYear === period.id);
            const editsForPeriod = editedCells[period.id];

            const forecastEntry = {
                id: existingForecast?.id || null,
                monthYear: period.id,
                // For each expense type, use: edited value > existing forecast > budget > 0
                employeeRelations: editsForPeriod?.employeeRelations ??
                    existingForecast?.employeeRelations ??
                    budgetForPeriod?.employeeRelations ?? 0,
                fuelVehicles: editsForPeriod?.fuelVehicles ??
                    existingForecast?.fuelVehicles ??
                    budgetForPeriod?.fuelVehicles ?? 0,
                lossAndDamageClaims: editsForPeriod?.lossAndDamageClaims ??
                    existingForecast?.lossAndDamageClaims ??
                    budgetForPeriod?.lossAndDamageClaims ?? 0,
                officeSupplies: editsForPeriod?.officeSupplies ??
                    existingForecast?.officeSupplies ??
                    budgetForPeriod?.officeSupplies ?? 0,
                outsideServices: editsForPeriod?.outsideServices ??
                    existingForecast?.outsideServices ??
                    budgetForPeriod?.outsideServices ?? 0,
                rentsParking: editsForPeriod?.rentsParking ??
                    existingForecast?.rentsParking ??
                    budgetForPeriod?.rentsParking ?? 0,
                repairsAndMaintenance: editsForPeriod?.repairsAndMaintenance ??
                    existingForecast?.repairsAndMaintenance ??
                    budgetForPeriod?.repairsAndMaintenance ?? 0,
                repairsAndMaintenanceVehicle: editsForPeriod?.repairsAndMaintenanceVehicle ??
                    existingForecast?.repairsAndMaintenanceVehicle ??
                    budgetForPeriod?.repairsAndMaintenanceVehicle ?? 0,
                signage: editsForPeriod?.signage ??
                    existingForecast?.signage ??
                    budgetForPeriod?.signage ?? 0,
                suppliesAndEquipment: editsForPeriod?.suppliesAndEquipment ??
                    existingForecast?.suppliesAndEquipment ??
                    budgetForPeriod?.suppliesAndEquipment ?? 0,
                ticketsAndPrintedMaterial: editsForPeriod?.ticketsAndPrintedMaterial ??
                    existingForecast?.ticketsAndPrintedMaterial ??
                    budgetForPeriod?.ticketsAndPrintedMaterial ?? 0,
                uniforms: editsForPeriod?.uniforms ??
                    existingForecast?.uniforms ??
                    budgetForPeriod?.uniforms ?? 0,
                miscOtherExpenses: existingForecast?.miscOtherExpenses ??
                    budgetForPeriod?.miscOtherExpenses ?? 0,
                totalOtherExpenses: calculateTotalOtherExpenses(period.id),
            };

            forecastData.push(forecastEntry);
        });

        const customer = customers.find(c => c.customerSiteId === selectedSite);

        const payload: OtherExpenseDto = {
            id: null,
            customerSiteId: selectedSite,
            siteNumber: customer?.siteNumber || "",
            name: customer?.siteName || "",
            billingPeriod: startingMonth,
            forecastData,
            budgetData: [],
            actualData: []
        };

        try {
            const response = await fetch("/api/otherExpense", {
                method: "PATCH",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload),
            });

            if (!response.ok) {
                throw new Error("Failed to save changes.");
            }

            setData(prevData => {
                if (!prevData) return payload;
                return { ...prevData, forecastData };
            });
            setEditedCells({});
            setHasUnsavedChanges(false);

            toast({
                title: "Changes saved",
                description: "Other expenses forecast data has been successfully updated.",
                variant: "default"
            });
        } catch (err) {
            toast({
                title: "Error saving changes",
                description: err instanceof Error ? err.message : "An unexpected error occurred",
                variant: "destructive"
            });
        } finally {
            setIsSaving(false);
        }
    };

    const handleViewModeChange = (mode: 'flash' | 'budget' | 'priorYear') => {
        setViewMode(mode);
    };

    const timePeriods = getTimePeriods();

    useImperativeHandle(ref, () => ({
        save: handleSave
    })); 

    const spreadsheetNavigation = useSpreadsheetNavigation({
        tableRef,
        rowCountCallback: () => timePeriods.length,
        columnCountCallback: () => EXPENSE_TYPES.length,
        isCellEditableCallback: (rowIndex, colIndex) => {
            const period = timePeriods[rowIndex];
            const expenseType = EXPENSE_TYPES[colIndex];
            const isReadOnlyColumn = expenseType.id === 'miscOtherExpenses' || expenseType.id === 'totalOtherExpenses';

            return !isReadOnlyColumn &&
                !hasActualizedData(period.id, expenseType.id as ExpenseTypeId) &&
                !isPastMonth(period.date);
        },
   onCellActivate: (rowIndex, colIndex, cellElement) => {
            // Only focus the input, don't select - let the user's typing behavior be natural
    if (cellElement) {
        const input = cellElement.querySelector('input');
        if (input && !input.disabled && !input.readOnly) {
            input.focus({ preventScroll: true });
            input.select(); 
        }
    }
},
       onCellEditRequest: (rowIndex, colIndex) => {
            const period = timePeriods[rowIndex];
            const expenseType = EXPENSE_TYPES[colIndex];
            const isReadOnlyColumn = expenseType.id === 'miscOtherExpenses' || expenseType.id === 'totalOtherExpenses';

            if (!isReadOnlyColumn &&
                !hasActualizedData(period.id, expenseType.id as ExpenseTypeId) &&
                !isPastMonth(period.date)) {
                const cell = tableRef.current?.querySelector(`#cell-${rowIndex}-${colIndex}`) as HTMLElement;
                const input = cell?.querySelector('input') as HTMLInputElement;
        if (input) {
            input.focus({ preventScroll: true });
            input.select();
                    // Set up for replace on next input
                    const cellKey = `${rowIndex}-${colIndex}`;
                    replaceOnNextInputRef.current[cellKey] = true;
        }
    }
}
    });


    const {
        isDragging,
        dragPreviewCells,
        handleDragStart,
        handleDragMove,
        handleDragEnd,
        isDragPreviewCell
    } = useDragAndCopy({
        activeCell: spreadsheetNavigation.activeCell,
        rowCount: timePeriods.length,
        colCount: EXPENSE_TYPES.length,
        context: { timePeriods, expenseTypes: EXPENSE_TYPES },
        onCopy: (cells, { timePeriods, expenseTypes }) => {
            return cells.map(({ rowIndex, colIndex }) => {
                const periodId = timePeriods[rowIndex].id;
                const expenseType = expenseTypes[colIndex].id as ExpenseTypeId;
                const value = getCellValue(periodId, expenseType);
                return value == null ? "" : String(value);
            });
        },
        onPaste: (cells, clipboard, { timePeriods, expenseTypes }) => {
            const newEditedCells = { ...editedCells };

            cells.forEach((cell, i) => {
                const value = clipboard[i];
                const parsed = value === "" ? 0 : parseFloat(value);
                const periodId = timePeriods[cell.rowIndex].id;
                const expenseType = expenseTypes[cell.colIndex].id as ExpenseTypeId;

                const { forecast, budget } = getDataForPeriod(periodId, expenseType);
                const fallback = forecast ?? budget ?? 0;
                const isRevert = Math.abs(parsed - fallback) < 0.01;

                if (isPastMonth(timePeriods[cell.rowIndex].date)) {
                    return;
                }

                if (isRevert) {
                    delete newEditedCells[periodId]?.[expenseType];
                    if (newEditedCells[periodId] && Object.keys(newEditedCells[periodId]).length === 0) {
                        delete newEditedCells[periodId];
                    }
                } else {
                    if (!newEditedCells[periodId]) {
                        newEditedCells[periodId] = {};
                    }
                    newEditedCells[periodId][expenseType] = parsed;
                }
            });

            setEditedCells(newEditedCells);
            setHasUnsavedChanges(Object.keys(newEditedCells).length > 0);
        }
    });
    return (
        <TooltipProvider>
            <div className="w-full p-1 space-y-6">
                <div className="flex justify-between items-start mb-4">
                    <div>
                        <h1 className="text-2xl font-bold tracking-tight">Other Expenses</h1>
                        <p className="text-muted-foreground">Manage additional expense categories for your parking operations.</p>
                    </div>
                </div>

                <Button
                    variant="outline"
                    onClick={() => setIsGuideOpen(!isGuideOpen)}
                    className="flex items-center gap-2 mb-2"
                    data-qa-id="button-toggle-other-expenses-guide"
                >
                    <Info className="h-4 w-4" />
                    {isGuideOpen ? "Hide Guide" : "Show Guide"}
                    {isGuideOpen ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
                </Button>

                {isGuideOpen && (
                    <div className="space-y-6 p-6 border-2 border-border rounded-lg bg-muted dark:bg-gray-900 text-card-foreground mb-6 shadow-sm">
                        <div className="border-b-2 border-border pb-3">
                            <h3 className="text-xl font-semibold text-foreground">Other Expense — Guide</h3>
                        </div>

                                                 <div className="space-y-6">
                             {/* First row: Purpose (left) + What to enter & How inputs are used (right) */}
                             <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                                 <div className="p-4 border border-border rounded-lg bg-card shadow-sm">
                                     <h4 className="font-semibold mb-3 text-foreground border-b border-border pb-2">Purpose</h4>
                                     <ul className="list-disc pl-5 space-y-1 text-muted-foreground">
                                         <li>Adjust non-payroll expense assumptions where you expect variances from budget.</li>
                                         <li>Identify the accounts which are Billable to the client based on your Contract Details by viewing the badge.</li>
                                         <li>Compare your Actualized expenses against your Budget by viewing the Variance Indicators</li>
                                         <ul className="list-disc pl-5 space-y-1 mt-1">
                                             <li><span className="text-green-600 dark:text-green-400">Green ▼</span> if Actual &lt; Budget</li>
                                             <li>Black ● if Actual = Budget</li>
                                             <li><span className="text-red-600 dark:text-red-400">Red ▲</span> if Actual &gt; Budget</li>
                                         </ul>
                                     </ul>
                                 </div>

                                 <div className="space-y-4">
                                     <div className="p-4 border border-border rounded-lg bg-card shadow-sm">
                                         <h4 className="font-semibold mb-3 text-foreground border-b border-border pb-2">What to enter</h4>
                                         <ul className="list-disc pl-5 space-y-1 text-muted-foreground">
                                             <li>Material changes for controllable expenses (uniforms, supplies, small tools/equipment, etc.).</li>
                                         </ul>
                                     </div>

                                     <div className="p-4 border border-border rounded-lg bg-card shadow-sm">
                                         <h4 className="font-semibold mb-3 text-foreground border-b border-border pb-2">How your inputs are used</h4>
                                         <ol className="list-decimal pl-5 space-y-1 text-muted-foreground">
                                             <li>Replaces budget for those lines in the forecasted P&L.</li>
                                             <li>At Management Agreement sites, many of these expenses are billed back — focus on accuracy of the forecasted amount.</li>
                                         </ol>
                                     </div>
                                 </div>
                             </div>

                             {/* Second row: Tips and guardrails (full width) */}
                             <div className="p-4 border border-border rounded-lg bg-card shadow-sm">
                                 <h4 className="font-semibold mb-3 text-foreground border-b border-border pb-2">Tips and guardrails</h4>
                                 <ul className="list-disc pl-5 space-y-1 text-muted-foreground">
                                     <li>The Parking Rents column of the Other Expense forecast tab is shown in its own row on the P&L View- to reconcile Other Expenses tab with the P&L row for Other Expenses, subtract the Parking Rents amount from the Total Other Expenses column.</li>
                                     <li>Management Agreement Sites</li>
                                     <ul className="list-disc pl-5 space-y-1 mt-1">
                                         <li>Do NOT enter client-paid amounts here (see "Client Paid Expense" on Other Revenue).</li>
                                     </ul>
                                     <li>Non-MA Sites</li>
                                     <ul className="list-disc pl-5 space-y-1 mt-1">
                                         <li>You may enter a client-paid amount here ONLY if it's a one-off (ie: not budgeted) expense.</li>
                                     </ul>
                                     <li>Do NOT enter Insurance or PTEB here — those are auto-calculated from the Payroll forecast.</li>
                                     <li>Focus on items with meaningful dollar impact.</li>
                                 </ul>
                             </div>
                         </div>
                    </div>
                )}

                <Card>
                    <CardHeader className="pb-3">
                        <div className="flex flex-col space-y-4">
                            <div className="flex justify-between items-center">
                                <CardTitle>Other Expenses</CardTitle>
                            </div>
                            <div className="flex justify-between items-center">
                                <div className="p-3 text-xs">
                                    <p>
                                        <strong>Note:</strong> Other Expenses may only be input by month. All values should be entered as positive dollar amounts.
                                    </p>
                                </div>
                                <ViewModeRadioGroup
                                    viewMode={viewMode}
                                    onViewModeChange={handleViewModeChange}
                                    disabled={!selectedSite || !startingMonth || isLoading}
                                />
                            </div>
                        </div>
                    </CardHeader>
                    <CardContent>
                        <div className="overflow-x-auto">
                            {isLoading ? (
                                <Skeleton className="h-[400px] w-full" />
                            ) : (
                                <Table
                                    ref={tableRef}
                                    className="w-full table-fixed"
                                    {...spreadsheetNavigation.tableProps}
                                >
                                    <TableHeader>
                                        <TableRow>
                                            <TableHead className="w-[80px] text-left px-1 py-0.5 font-medium text-xs">Month</TableHead>
                                            {EXPENSE_TYPES.map((expense) => {
                                                const accountCode = EXPENSE_ACCOUNT_CODES[expense.id];
                                                const isBillable = accountCode && isExpenseAccountBillable(accountCode, contractDetails);
                                                
                                                return (
                                                    <TableHead
                                                        key={expense.id}
                                                        className={`${expense.id === 'miscOtherExpenses' || expense.id === 'totalOtherExpenses' ? 'w-[120px]' : 'w-[100px]'} text-center px-1 py-0.5 font-medium text-xs whitespace-normal break-words`}
                                                    >
                                                        {expense.id === 'miscOtherExpenses' || expense.id === 'totalOtherExpenses'
                                                            ? expense.label
                                                            : (
                                                                <div className="flex flex-col items-center gap-1">
                                                                    <div className="flex items-center gap-1">
                                                                        <span>{accountCode || ""}</span>
                                                                        {isBillable && (
                                                                            <Badge 
                                                                                variant="secondary" 
                                                                                className="bg-blue-100 text-blue-800 text-[9px] px-1 py-0 h-3 font-medium"
                                                                            >
                                                                                Billable
                                                                            </Badge>
                                                                        )}
                                                                    </div>
                                                                    <span>{expense.label}</span>
                                                                </div>
                                                            )
                                                        }
                                                    </TableHead>
                                                );
                                            })}
                                        </TableRow>
                                    </TableHeader>
                                    <TableBody>
                                        {timePeriods.map((period, rowIndex) => (
                                            <TableRow key={period.id} className={`${isPastMonth(period.date)
                                                    ? "bg-gray-100 dark:bg-gray-800"
                                                    : isCurrentMonth(period.date) && viewMode === 'flash'
                                                        ? "bg-orange-200 dark:bg-[#ec500267]"
                                                        : ""
                                                }`}>
                                                <TableCell className="px1 py-0.5 text-xs font-medium min-w-[80px]">
                                                    <div className="flex flex-col items-start">
                                                        <span>
                                                            {period.label}
                                                            {isCurrentMonth(period.date) && viewMode === 'flash' && (
                                                                <span className="ml-1 text-[10px] text-blue-600 dark:text-blue-400 font-medium">Actual</span>
                                                            )}
                                                        </span>
                                                    </div>
                                                </TableCell>

                                                {EXPENSE_TYPES.map((expense, colIndex) => {
                                                    const isReadOnlyColumn = expense.id === 'miscOtherExpenses' || expense.id === 'totalOtherExpenses';
                                                    const hasActual = hasActualizedData(period.id, expense.id as ExpenseTypeId);
                                                    const actualValue = hasActual ? getDataForPeriod(period.id, expense.id as ExpenseTypeId).actual : null;

                                                    // Get the appropriate value based on column type and view mode
                                                    let displayValue;
                                                    if (viewMode === 'budget') {
                                                        // Budget view: ALWAYS show budget values for ALL periods (including current month)
                                                        if (expense.id === 'totalOtherExpenses') {
                                                            // Calculate total from budget values only
                                                            displayValue = calculateTotalBudgetExpenses(period.id);
                                                        } else {
                                                            // Get budget value directly for this expense type
                                                            const { budget } = getDataForPeriod(period.id, expense.id as ExpenseTypeId);
                                                            displayValue = budget ?? 0;
                                                        }
                                                    } else {
                                                        // Flash view: use the standard logic
                                                        if (expense.id === 'totalOtherExpenses') {
                                                            displayValue = calculateTotalOtherExpenses(period.id);
                                                        } else {
                                                            displayValue = getCellValue(period.id, expense.id as ExpenseTypeId);
                                                        }
                                                    }

                                                    const isDragCell = isDragPreviewCell(rowIndex, colIndex);
                                                    const isActive = spreadsheetNavigation.activeCell?.rowIndex === rowIndex && spreadsheetNavigation.activeCell?.colIndex === colIndex;

                                                    return (
                                                        <TableCell
                                                            data-row={rowIndex}
                                                            data-col={colIndex}
                                                            key={expense.id}
                                                            className={`px-1 py-0.5 text-center text-xs ${expense.id === 'miscOtherExpenses' || expense.id === 'totalOtherExpenses' ? 'min-w-[120px]' : 'min-w-[100px]'} ${!isReadOnlyColumn ? 'cursor-ns-resize' : ''} ${isDragCell || isActive ? "border-2 border-blue-500 z-10 relative" : ""}`}
                                                            {...spreadsheetNavigation.getCellProps(rowIndex, colIndex)}
                                                            onClick={() => {
                                                                // Set up replace behavior when user clicks on the cell
                                                                if (!isReadOnlyColumn && !hasActualizedData(period.id, expense.id as ExpenseTypeId) && !isPastMonth(period.date)) {
                                                                    const cellKey = `${rowIndex}-${colIndex}`;
                                                                    replaceOnNextInputRef.current[cellKey] = true;
                                                                }
                                                                // Call the original onClick from getCellProps
                                                                const originalProps = spreadsheetNavigation.getCellProps(rowIndex, colIndex);
                                                                if (originalProps.onClick) {
                                                                    originalProps.onClick();
                                                                }
                                                            }}
                                                            onMouseDown={e => {
                                                                if (e.button !== 0 || isReadOnlyColumn) return;
                                                                handleDragStart(rowIndex, colIndex);
                                                            }}
                                                            onMouseMove={(e) => {
                                                                if (isDragging && !isReadOnlyColumn) {
                                                                    handleDragMove(rowIndex, colIndex);
                                                                }
                                                            }}
                                                            onMouseUp={e => {
                                                                if (!isReadOnlyColumn) {
                                                                    handleDragEnd();
                                                                }
                                                            }}
                                                        >
                                                            <div className="relative highlight-cell">
                                                                {/* Current month dual-row display: forecast (main) + actual (secondary) */}
                                                                {hasActual && isCurrentMonth(period.date) && viewMode === 'flash' ? (
                                                                    <div className="relative w-full">
                                                                        <NumericFormat
                                                                            value={getCellValue(period.id, expense.id as ExpenseTypeId)}
                                                                            onValueChange={(values) => {
                                                                                handleCellChange(period.id, expense.id as ExpenseTypeId, values.value);
                                                                            }}
                                                                            onFocus={() => {
                                                                                // Don't set replace behavior on focus - let it be controlled by edit request
}}
                                                                            onKeyDown={(e) => {
                                                                                const cellKey = `${rowIndex}-${colIndex}`;
                                                                                if (
                                                                                    replaceOnNextInputRef.current[cellKey] &&
                                                                                    e.key.length === 1 &&
                                                                                    !e.ctrlKey && !e.metaKey && !e.altKey
                                                                                ) {
                                                                                    e.preventDefault();
                                                                                    handleCellChange(period.id, expense.id as ExpenseTypeId, e.key);
                                                                                    replaceOnNextInputRef.current[cellKey] = false;
                                                                                }
                                                                            }}
                                                                            thousandSeparator={true}
                                                                            prefix="$"
                                                                            decimalScale={0}
                                                                            allowNegative={false}
                                                                            placeholder="0"
                                                                            readOnly={isReadOnlyColumn}
                                                                            disabled={isReadOnlyColumn}
                                                                            className={`text-right w-full pb-6 ${isCellModified(period.id, expense.id as ExpenseTypeId) ? "border-blue-600 bg-blue-50 dark:bg-slate-800" : ""} 
                                                                            rounded-sm border border-input bg-background px-1 py-1 
                                                                            focus-visible:outline-none focus-visible:ring-1 pr-6`}
                                                                            data-qa-id={`input-${expense.id}-${period.id}`}
                                                                        />
                                                                        {/* Actual value row (read-only, smaller text) with variance indicator */}
                                                                        <div className="absolute bottom-0 left-1 right-1 text-xs text-gray-600 dark:text-gray-400 bg-gray-50 dark:bg-gray-800 px-1 py-0.5 rounded-sm text-right pr-6">
                                                                            ${Math.round(actualValue || 0).toLocaleString()}
                                                                            {/* Variance indicator for current month dual-row layout */}
                                                                            {(() => {
                                                                                const actual = actualValue || 0;
                                                                                const { budget } = getDataForPeriod(period.id, expense.id as ExpenseTypeId);
                                                                                
                                                                                // Only show variance indicator if we have both actual and budget data
                                                                                if (budget === undefined || budget === null) {
                                                                                    return null;
                                                                                }
                                                                                
                                                                                return (
                                                                                    <div className="absolute right-1 top-1/2 transform -translate-y-1/2 z-10">
                                                                                        <VarianceIndicator
                                                                                            actualValue={actual}
                                                                                            forecastValue={budget}
                                                                                            className="ml-0"
                                                                                            isExpense={true}
                                                                                        />
                                                                                    </div>
                                                                                );
                                                                            })()}
                                                                        </div>
                                                                    </div>
                                                                ) : (
                                                                    /* Standard single input for past/future months */
                                                                    <div className="relative inline-flex items-center w-full">
                                                                        <NumericFormat
                                                                            value={displayValue}
                                                                            onValueChange={(values) => {
                                                                                handleCellChange(period.id, expense.id as ExpenseTypeId, values.value);
                                                                            }}
                                                                            onFocus={() => {
                                                                                // Don't set replace behavior on focus - let it be controlled by edit request
                                                                            }}
                                                                            onKeyDown={(e) => {
                                                                                const cellKey = `${rowIndex}-${colIndex}`;
                                                                                if (
                                                                                    replaceOnNextInputRef.current[cellKey] &&
                                                                                    e.key.length === 1 &&
                                                                                    !e.ctrlKey && !e.metaKey && !e.altKey
                                                                                ) {
                                                                                    e.preventDefault();
                                                                                    handleCellChange(period.id, expense.id as ExpenseTypeId, e.key);
                                                                                    replaceOnNextInputRef.current[cellKey] = false;
                                                                                }
                                                                            }}
                                                                            thousandSeparator={true}
                                                                            prefix="$"
                                                                            decimalScale={0}
                                                                            allowNegative={false}
                                                                            placeholder="0"
                                                                            readOnly={isReadOnlyColumn || viewMode === 'budget' || isPastMonth(period.date)}
                                                                            disabled={isReadOnlyColumn || viewMode === 'budget' || (isPastMonth(period.date) && hasActual)}
                                                                            className={`text-right w-full ${isCellModified(period.id, expense.id as ExpenseTypeId) ? "border-blue-600 bg-blue-50 dark:bg-slate-800" : ""} 
                                                                            rounded-sm border border-input bg-background px-1 py-0.5 
                                                                            focus-visible:outline-none focus-visible:ring-1 
                                                                            disabled:cursor-not-allowed disabled:opacity-50 ${hasActual && (isPastMonth(period.date) || isCurrentMonth(period.date)) && viewMode === 'flash' ? "pr-6" : ""}`}
                                                                            data-qa-id={`input-${expense.id}-${period.id}`}
                                                                        />
                                                                        {/* Variance indicator for past months only (current month handled above) */}
                                                                        {hasActual && isPastMonth(period.date) && viewMode === 'flash' && (() => {
                                                                            const actual = actualValue || 0;
                                                                            const { budget } = getDataForPeriod(period.id, expense.id as ExpenseTypeId);
                                                                            
                                                                            // Only show variance indicator if we have both actual and budget data
                                                                            if (budget === undefined || budget === null) {
                                                                                return null;
                                                                            }
                                                                            
                                                                            return (
                                                                                <div className="absolute right-1 top-1/2 transform -translate-y-1/2 z-10">
                                                                                    <VarianceIndicator
                                                                                        actualValue={actual}
                                                                                        forecastValue={budget}
                                                                                        className="ml-0"
                                                                                        isExpense={true}
                                                                                    />
                                                                                </div>
                                                                            );
                                                                        })()}
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

                        {/* Visual Legend */}
                        <div className="mt-4 p-3 border rounded-md bg-muted/10">
                            <h4 className="text-sm font-medium mb-2">Legend</h4>
                            <div className="flex flex-wrap gap-4 text-xs">
                                <div className="flex items-center gap-1">
                                    <div className="w-4 h-4 bg-gray-100 dark:bg-gray-800 border rounded"></div>
                                    <span>Complete Actual Data</span>
                                </div>
                                <div className="flex items-center gap-1">
                                    <div className="w-4 h-4 bg-orange-200 dark:bg-[#ec500267] border rounded"></div>
                                    <span>Partial Actual Data (Current Month)</span>
                                </div>
                                <div className="flex items-center gap-1">
                                    <span className="text-green-600">▼</span>
                                    <span>Actual &lt; Budget (Favorable)</span>
                                </div>
                                <div className="flex items-center gap-1">
                                    <span className="text-red-600">▲</span>
                                    <span>Actual &gt; Budget (Unfavorable)</span>
                                </div>
                            </div>
                        </div>
                    </CardContent>
                </Card>
            </div>
        </TooltipProvider>
    );
});

export { OtherExpenses };
export default OtherExpenses;
