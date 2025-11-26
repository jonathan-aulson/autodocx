import { CustomerFilter, SelectedFilters } from "@/components/CustomerFilter/CustomerFilter";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { useCustomer } from "@/contexts/CustomerContext";
import { CustomerSummary } from "@/lib/models/GeneralInfo";
import { PnlResponse } from "@/lib/models/Pnl";
import { formatCurrencyWhole, getCurrentMonthIdx } from "@/lib/utils";
import { ChevronDown, ChevronUp, Filter, Info } from "lucide-react";
import { useEffect, useMemo, useState, useRef } from "react";
import { useParams } from "react-router-dom";
import { 
    PnlTooltip, 
    TooltipAmountSection, 
    TooltipDateSection, 
    TooltipDivider,
    TooltipComparisonSection,
    TooltipDateComparisonSection
} from "@/components/pnl/PnlTooltip";

const MONTHS = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

const VARIANCE_LEVEL_HIGH = 7.5;
const VARIANCE_LEVEL_MEDIUM = 5;
const APPLIED_FILTERS_STORAGE_KEY = 'pnl-applied-filters';
const STARTING_MONTH_STORAGE_KEY = 'selectedStartingMonth';

// Row type mapping for business logic
const ROW_TYPE_MAPPING: Record<string, 'revenue' | 'expense' | 'flc'> = {
    externalRevenue: 'revenue',
    internalRevenue: 'revenue',
    payroll: 'expense',
    claims: 'expense',
    parkingRents: 'expense',
    otherExpense: 'expense',
    pteb: 'expense',
    insurance: 'expense',
    flc: 'flc',
    flcCumulative: 'flc'
};

function getVarianceClass(variance: number, percent: number, itemType: 'revenue' | 'expense' | 'flc' = 'revenue') {
    const absPercent = Math.abs(percent);
    
    // Zero variance - circle-dot indicator with black/gray styling
    if (variance === 0) {
        return "text-gray-700 dark:text-gray-300";
    }
    
    // Determine if variance is favorable based on item type
    const isFavorable = (() => {
        switch (itemType) {
            case 'revenue': // Revenue: Above budget = favorable
            case 'flc':     // FLC: Above budget = favorable
                return variance > 0;
            case 'expense': // Expense: Below budget = favorable
                return variance < 0;
            default:
                return variance > 0;
        }
    })();
    
    // Apply color and boldness based on favorability and threshold
    if (isFavorable) {
        return absPercent >= VARIANCE_LEVEL_HIGH 
            ? "text-green-600 dark:text-green-400 font-bold"  // Favorable ≥7.5% = Bold Green
            : "text-green-600 dark:text-green-400";           // Favorable <7.5% = Green
    } else {
        return absPercent >= VARIANCE_LEVEL_HIGH 
            ? "text-red-600 dark:text-red-400 font-bold"      // Unfavorable ≥7.5% = Bold Red
            : "text-red-600 dark:text-red-400";               // Unfavorable <7.5% = Red
    }
}

const CODE_TO_API_COLUMN: Record<string, string> = {
    externalRevenue: "ExternalRevenue",
    internalRevenue: "InternalRevenue",
    payroll: "Payroll",
    claims: "Claims",
    parkingRents: "ParkingRents",
    otherExpense: "OtherExpense",
    pteb: "Pteb",
    insurance: "Insurance",
    flc: "FLC",
    flcCumulative: "FLC_CUMULATIVE",
};

function SkeletonTable() {
    return (
        <div className="w-full overflow-auto animate-pulse">
            <Table className="w-full table-fixed">
                <TableHeader>
                    <TableRow className="h-[50px]">
                        <TableHead className="sticky left-0 bg-muted w-[160px] z-30">
                            <div className="h-4 bg-gray-200 rounded w-3/4 mx-auto" />
                        </TableHead>
                        {MONTHS.map((_, idx) => (
                            <TableHead key={idx} className="text-center p-2 w-[85px] z-20 bg-muted">
                                <div className="h-3 bg-gray-200 rounded w-2/3 mx-auto" />
                            </TableHead>
                        ))}
                        <TableHead className="text-center p-2 w-[110px] z-20 bg-muted">
                            <div className="h-3 bg-gray-200 rounded w-2/3 mx-auto" />
                        </TableHead>
                        <TableHead className="text-center p-2 w-[85px] z-20 bg-muted">
                            <div className="h-3 bg-gray-200 rounded w-2/3 mx-auto" />
                        </TableHead>
                    </TableRow>
                </TableHeader>
                <TableBody>
                    {[...Array(10)].map((_, rowIdx) => (
                        <TableRow key={rowIdx} className="h-[50px]">
                            <TableCell className="sticky left-0 bg-background w-[140px]">
                                <div className="h-3 bg-gray-200 rounded w-2/3 mx-auto" />
                            </TableCell>
                            {MONTHS.map((_, idx) => (
                                <TableCell key={idx} className="text-right p-1 w-[70px] h-[50px]">
                                    <div className="h-3 bg-gray-200 rounded w-3/4 mx-auto" />
                                </TableCell>
                            ))}
                            <TableCell className="text-right p-1 w-[90px] h-[50px]">
                                <div className="h-3 bg-gray-200 rounded w-3/4 mx-auto" />
                            </TableCell>
                            <TableCell className="text-right p-1 w-[70px] h-[50px]">
                                <div className="h-3 bg-gray-200 rounded w-2/3 mx-auto" />
                            </TableCell>
                        </TableRow>
                    ))}
                </TableBody>
            </Table>
        </div>
    );
}

function AnimatedLoadingText() {
    const messages = [
        "Crunching numbers...",
        "Fetching P&L data...",
        "Aggregating sites...",
        "Almost there...",
        "Still working, please wait...",
    ];
    const [idx, setIdx] = useState(0);
    useEffect(() => {
        const timer = setInterval(() => setIdx(i => (i + 1) % messages.length), 1800);
        return () => clearInterval(timer);
    }, []);
    return (
        <div className="w-full flex justify-center mt-4">
            <span className="text-sm text-blue-700 animate-pulse">{messages[idx]}</span>
        </div>
    );
}

function ProgressBar() {
    return (
        <div className="w-full h-2 bg-gray-200 dark:bg-gray-700 rounded overflow-hidden mb-2">
            <div className="h-full bg-gradient-to-r from-transparent via-blue-500 to-transparent animate-loop-progress" />
            <style>
                {`
                @keyframes loop-progress {
                    0% { transform: translateX(-100%); }
                    100% { transform: translateX(100%); }
                }
                .animate-loop-progress {
                    width: 50%;
                    animation: loop-progress 1.5s ease-in-out infinite;
                }
                `}
            </style>
        </div>
    );
}

export default function PnlView() {
    const { customerId } = useParams<{ customerId?: string }>();
    const { selectedCustomer, setSelectedCustomerById, customerSummaries, fetchCustomerSummaries } = useCustomer();
    
    const [viewMode, setViewMode] = useState<"Forecast" | "Budget">("Forecast");
    const [showVariance, setShowVariance] = useState(true);
    const [filterOpen, setFilterOpen] = useState(false);
    const [showGuide, setShowGuide] = useState(false);
    const [loading, setLoading] = useState(true);
    const [pnlData, setPnlData] = useState<PnlResponse | null>(null);
    const [customers, setCustomers] = useState<CustomerSummary[]>([]);
    const [filteredSiteIds, setFilteredSiteIds] = useState<string[]>([]);
    const [loadError, setLoadError] = useState<string | null>(null);
    const [routeFilterApplied, setRouteFilterApplied] = useState(false);
    const [activeTooltip, setActiveTooltip] = useState<{
        visible: boolean;
        rowCode: string;
        month: number;
        x: number;
        y: number;
        rightSide?: boolean;
        repositioned?: boolean; // Track if tooltip was repositioned due to viewport constraints
    } | null>(null);
    
    // Refs for tooltip timing control
    const hideTimeoutRef = useRef<NodeJS.Timeout | null>(null);
    const showTimeoutRef = useRef<NodeJS.Timeout | null>(null);
    const isHoveringTooltip = useRef<boolean>(false);
    const isHoveringTrigger = useRef<boolean>(false);

    // Clear all timeouts helper
    const clearAllTimeouts = () => {
        if (hideTimeoutRef.current) {
            clearTimeout(hideTimeoutRef.current);
            hideTimeoutRef.current = null;
        }
        if (showTimeoutRef.current) {
            clearTimeout(showTimeoutRef.current);
            showTimeoutRef.current = null;
        }
    };

    const [appliedFilters, setAppliedFilters] = useState<SelectedFilters>(() => {
        const savedFilters = sessionStorage.getItem(APPLIED_FILTERS_STORAGE_KEY);
        if (savedFilters) {
            try {
                return JSON.parse(savedFilters);
            } catch (error) {
                console.error('Failed to parse saved filters:', error);
            }
        }
        return {};
    });

    const [selectedYear, setSelectedYear] = useState(() => {
        const savedStartingMonth = sessionStorage.getItem(STARTING_MONTH_STORAGE_KEY);
        if (savedStartingMonth) {
            try {
                const year = new Date(savedStartingMonth + '-01').getFullYear();
                return String(year);
            } catch (error) {
                console.error('Failed to parse saved starting month:', error);
            }
        }
        return String(new Date().getFullYear());
    });

    const currentMonthIdx = getCurrentMonthIdx(Number(selectedYear));

    useEffect(() => {
        if (customerId && (!selectedCustomer || selectedCustomer.customerSiteId !== customerId)) {
            setSelectedCustomerById(customerId);
            
            if (customerSummaries.length === 0) {
                fetchCustomerSummaries(true);
            }
        }
    }, [customerId, selectedCustomer, setSelectedCustomerById, customerSummaries.length, fetchCustomerSummaries]);

    useEffect(() => {
        if (selectedCustomer) {
            const initialFilters: SelectedFilters = {
                site: [selectedCustomer.siteNumber]
            };
            setAppliedFiltersWithStorage(initialFilters);
        }
    }, [selectedCustomer]);

    useEffect(() => {
        if (customerId && customers.length > 0 && !routeFilterApplied) {
            setAppliedFiltersWithStorage({ site: [customerId] });
            setFilteredSiteIds([customerId]);
            setRouteFilterApplied(true);
        }
    }, [customerId, customers.length, routeFilterApplied]);

    function filterCustomersByFilters(customersList: CustomerSummary[], filters: SelectedFilters): string[] {
        let filtered = [...customersList];

        const legalEntityFilters = filters.legalEntity || [];
        if (legalEntityFilters.length > 0) {
            filtered = filtered.filter(c =>
                c.legalEntity && legalEntityFilters.includes(c.legalEntity)
            );
        }

        const regionFilters = filters.region || [];
        if (regionFilters.length > 0) {
            filtered = filtered.filter(c =>
                c.svpRegion && regionFilters.includes(c.svpRegion)
            );
        }

        const districtFilters = filters.district || [];
        if (districtFilters.length > 0) {
            filtered = filtered.filter(c =>
                c.district && districtFilters.includes(c.district)
            );
        }

        const siteFilters = filters.site || [];
        if (siteFilters.length > 0) {
            filtered = filtered.filter(c =>
                siteFilters.includes(c.siteNumber)
            );
        }

        const accountManagerFilters = filters.accountManager || [];
        if (accountManagerFilters.length > 0) {
            filtered = filtered.filter(c =>
                (c.accountManager && accountManagerFilters.includes(c.accountManager)) ||
                (c.districtManager && accountManagerFilters.includes(c.districtManager))
            );
        }

        const plCategoryFilters = filters.plCategory || [];
        if (plCategoryFilters.length > 0) {
            filtered = filtered.filter(c =>
                c.plCategory && plCategoryFilters.includes(c.plCategory)
            );
        }

        const cogSegmentFilters = filters.cogSegment || [];
        if (cogSegmentFilters.length > 0) {
            filtered = filtered.filter(c =>
                c.cogSegment && cogSegmentFilters.includes(c.cogSegment)
            );
        }

        const businessSegmentFilters = filters.businessSegment || [];
        if (businessSegmentFilters.length > 0) {
            filtered = filtered.filter(c =>
                c.businessSegment && businessSegmentFilters.includes(c.businessSegment)
            );
        }

        const contractTypeFilters = filters.contractType || [];
        if (contractTypeFilters.length > 0) {
            filtered = filtered.filter(c =>
                c.contractType && contractTypeFilters.includes(c.contractType)
            );
        }

        return filtered.map(c => c.siteNumber);
    }

    const setAppliedFiltersWithStorage = (filters: SelectedFilters) => {
        setAppliedFilters(filters);
        sessionStorage.setItem(APPLIED_FILTERS_STORAGE_KEY, JSON.stringify(filters));
    };

    useEffect(() => {
        let cancelled = false;
        async function fetchCustomers() {
            setLoading(true);
            setLoadError(null);
            try {
                const customersRes = await fetch(`/api/customers?isForecast=true`);
                if (!customersRes.ok) throw new Error("Failed to fetch customers");
                const customersData: CustomerSummary[] = await customersRes.json();

                if (!cancelled) {
                    setCustomers(customersData);
                    if (selectedCustomer) {
                        const initialFilters: SelectedFilters = {
                            site: [selectedCustomer.siteNumber]
                        };
                        setAppliedFiltersWithStorage(initialFilters);
                        setFilteredSiteIds([selectedCustomer.siteNumber]);
                    } else {
                        let siteIds: string[];
                        const hasFilters = Object.values(appliedFilters).some(v => Array.isArray(v) && v.length > 0);
                        if (hasFilters) {
                            siteIds = filterCustomersByFilters(customersData, appliedFilters);
                        } else {
                            siteIds = customersData.map(c => c.siteNumber);
                        }
                        setFilteredSiteIds(siteIds);
                    }
                }
            } catch (err: any) {
                if (!cancelled) {
                    setLoadError(err.message || "Unknown error");
                    setLoading(false);
                }
            }
        }
        fetchCustomers();
        return () => { cancelled = true; };
    }, [selectedCustomer]);

    useEffect(() => {
        if (filteredSiteIds.length === 0) return;

        if (customerId && !routeFilterApplied) {
            if (filteredSiteIds.length !== 1 || filteredSiteIds[0] !== customerId) {
                return;
            }
        }

        let cancelled = false;
        async function fetchFilteredPnlData() {
            setLoading(true);
            try {
                const pnlRes = await fetch("/api/pnl", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ 
                        siteIds: filteredSiteIds, 
                        year: Number(selectedYear) 
                    }),
                });
                if (!pnlRes.ok) throw new Error("Failed to fetch P&L data");
                const pnlData = await pnlRes.json();
                
                if (!cancelled) {
                    if (pnlData.years) {
                        const yearData = pnlData.years.find((y: PnlResponse) => y.year === Number(selectedYear));
                        if (yearData) {
                            setPnlData(yearData);
                        } else {
                            setLoadError("No data available for selected year");
                        }
                    } else if (pnlData.year) {
                        setPnlData(pnlData);
                    } else {
                        setLoadError("Invalid data format received from API");
                    }
                    setLoading(false);
                }
            } catch (err: any) {
                if (!cancelled) {
                    setLoadError(err.message || "Unknown error");
                    setLoading(false);
                }
            }
        }

        if (filteredSiteIds.length > 0) {
            fetchFilteredPnlData();
        }

        return () => { cancelled = true; };
    }, [filteredSiteIds, selectedYear]);

    // Clear any active tooltip if more than one site is selected
    useEffect(() => {
        if (activeTooltip && filteredSiteIds.length !== 1) {
            setActiveTooltip(null);
        }
    }, [filteredSiteIds, activeTooltip]);

    // Cleanup timeouts on component unmount
    useEffect(() => {
        return () => {
            clearAllTimeouts();
        };
    }, []);

    const handleApplyFilters = (filters: SelectedFilters) => {
        setAppliedFiltersWithStorage(filters);
        const newFilteredSiteIds = filterCustomersByFilters(customers, filters);
        setFilteredSiteIds(newFilteredSiteIds);
    };

    const yearOptions = useMemo(() => {
        const currentYear = new Date().getFullYear();
        return [currentYear - 1, currentYear, currentYear + 1];
    }, []);

    function findRow(rows: any[] | undefined, code: string) {
        if (!rows || !Array.isArray(rows)) return null;
        const apiCol = CODE_TO_API_COLUMN[code];
        return rows.find(r => 
            r?.code === apiCol || 
            r?.code === code || 
            r?.columnName === apiCol || 
            r?.columnName === code
        );
    }

    function getTrendRow(code: string) {
        if (!pnlData) return Array(MONTHS.length).fill(null);
        if (code === "flc") {
            return MONTHS.map((_, idx) => {
                // In budget mode, use budget data for all months
                // In forecast mode, use actual for past months and forecast for future months
                const internalRevenue = viewMode === "Budget"
                    ? findRow(pnlData.budgetRows, "internalRevenue")?.monthlyValues[idx]?.value ?? 0
                    : idx <= currentMonthIdx
                        ? findRow(pnlData.actualRows, "internalRevenue")?.monthlyValues[idx]?.value ?? 0
                        : findRow(pnlData.forecastRows, "internalRevenue")?.monthlyValues[idx]?.value ?? 0;

                const expenses = ["payroll", "claims", "parkingRents", "otherExpense", "pteb", "insurance"].reduce((sum, expenseCode) => {
                    const expenseValue = viewMode === "Budget"
                        ? findRow(pnlData.budgetRows, expenseCode)?.monthlyValues[idx]?.value ?? 0
                        : idx <= currentMonthIdx
                            ? findRow(pnlData.actualRows, expenseCode)?.monthlyValues[idx]?.value ?? 0
                            : findRow(pnlData.forecastRows, expenseCode)?.monthlyValues[idx]?.value ?? 0;
                    return sum + (expenseValue ?? 0);
                }, 0);

                return internalRevenue - expenses;
            });
        } else if (code === "flcCumulative") {
            let cumulativeSum = 0;
            return MONTHS.map((_, idx) => {
                const actualForecastFLC = viewMode === "Budget"
                    ? calculateFLCForMonth(idx, "budget")
                    : idx <= currentMonthIdx
                        ? calculateFLCForMonth(idx, "actual")
                        : calculateFLCForMonth(idx, "forecast");

                const budgetFLC = calculateFLCForMonth(idx, "budget");

                const monthlyVariance = actualForecastFLC - budgetFLC;
                cumulativeSum += monthlyVariance;

                return cumulativeSum;
            });
        } else {
            const actual = findRow(pnlData.actualRows, code);
            const forecast = findRow(pnlData.forecastRows, code);
            const budget = findRow(pnlData.budgetRows, code);
            return MONTHS.map((_, idx) => {
                // In budget mode, use budget data for all months
                if (viewMode === "Budget") {
                    return budget?.monthlyValues[idx]?.value ?? null;
                }
                // Special handling for "otherExpense" row:
                // For current/future months, display the greater of actual and forecast; for historical, display actual only
                if (code === "otherExpense") {
                    if (idx < currentMonthIdx) {
                        return actual?.monthlyValues[idx]?.value ?? null;
                    }
                    const actualVal = actual?.monthlyValues[idx]?.value ?? null;
                    const forecastVal = forecast?.monthlyValues[idx]?.value ?? null;
                    if (actualVal == null && forecastVal == null) return null;
                    if (actualVal == null) return forecastVal;
                    if (forecastVal == null) return actualVal;
                    return Math.max(actualVal, forecastVal);
                }
                // Special handling
                // - PTEB: existing behavior (actual for past, forecast for current/future)
                // - Insurance: only show actuals when actual value > 0; otherwise use forecast (then budget)
                if (code === "pteb" || code === "insurance") {
                    if (code === "insurance") {
                        if (idx < currentMonthIdx) {
                            const av = actual?.monthlyValues[idx]?.value as number | undefined;
                            if (typeof av === "number" && !Number.isNaN(av) && av > 0) {
                                return av;
                            }
                            const fc = forecast?.monthlyValues[idx]?.value as number | undefined;
                            const fcValid = typeof fc === "number" && !Number.isNaN(fc) && fc > 0;
                            const bdg = budget?.monthlyValues[idx]?.value ?? null;
                            return fcValid ? fc : bdg;
                        }
                        const fc = forecast?.monthlyValues[idx]?.value as number | undefined;
                        const fcValid = typeof fc === "number" && !Number.isNaN(fc) && fc > 0;
                        const bdg = budget?.monthlyValues[idx]?.value ?? null;
                        return fcValid ? fc : bdg;
                    } else {
                        if (idx < currentMonthIdx) {
                            return actual?.monthlyValues[idx]?.value ?? null;
                        }
                        const fcVal = forecast?.monthlyValues[idx]?.value;
                        return (fcVal ?? budget?.monthlyValues[idx]?.value) ?? null;
                    }
                }
                // Default behavior: actual for past/current months, forecast for future months
                if (idx <= currentMonthIdx) return actual?.monthlyValues[idx]?.value ?? null;
                // For Parking Rents, display the greater of actual and forecast for the current month
                if (code === "parkingRents" && idx === currentMonthIdx) {
                    const actualVal = actual?.monthlyValues[idx]?.value ?? null;
                    const forecastVal = forecast?.monthlyValues[idx]?.value ?? null;
                    if (actualVal == null && forecastVal == null) return null;
                    if (actualVal == null) return forecastVal;
                    if (forecastVal == null) return actualVal;
                    return Math.max(actualVal, forecastVal);
                }
                return forecast?.monthlyValues[idx]?.value ?? null;
            });
    }
    }

    function calculateFLCForMonth(monthIdx: number, dataType: "actual" | "forecast" | "budget") {
        if (!pnlData) return 0;
        const rowsToUse = dataType === "actual"
            ? pnlData.actualRows
            : dataType === "forecast"
                ? pnlData.forecastRows
                : pnlData.budgetRows;

        const internalRevenue = findRow(rowsToUse, "internalRevenue")?.monthlyValues[monthIdx]?.value ?? 0;

        const expenses = ["payroll", "claims", "parkingRents", "otherExpense", "pteb", "insurance"].reduce((sum, expenseCode) => {
            const expenseValue = findRow(rowsToUse, expenseCode)?.monthlyValues[monthIdx]?.value ?? 0;
            return sum + (expenseValue ?? 0);
        }, 0);

        return internalRevenue - expenses;
    }

    function getVarianceRow(code: string) {
        if (!pnlData) return Array(MONTHS.length).fill({});
        if (code === "flc") {
            return MONTHS.map((_, idx) => {
                const forecastFLC = calculateFLCForMonth(idx, "forecast");
                const budgetFLC = calculateFLCForMonth(idx, "budget");
                const amount = forecastFLC - budgetFLC;
                const percentage = budgetFLC !== 0 ? (amount / budgetFLC) * 100 : 0;

                return {
                    month: idx,
                    amount,
                    percentage
                };
            });
        } else if (code === "flcCumulative") {
            let forecastCumulative = 0;
            let budgetCumulative = 0;

            return MONTHS.map((_, idx) => {
                const forecastFLC = calculateFLCForMonth(idx, "forecast");
                const budgetFLC = calculateFLCForMonth(idx, "budget");

                forecastCumulative += forecastFLC;
                budgetCumulative += budgetFLC;

                const amount = forecastCumulative - budgetCumulative;
                const percentage = budgetCumulative !== 0 ? (amount / budgetCumulative) * 100 : 0;

                return {
                    month: idx,
                    amount,
                    percentage
                };
            });
        } else {
            const variance = findRow(pnlData.varianceRows, code);
            return variance?.monthlyVariances ?? [];
        }
    }

function getTrendTotal(code: string) {
        if (!pnlData) return null;
        if (code === "flc") {
            const months = MONTHS.length;
            let total = 0;

            for (let i = 0; i < months; i++) {
                if (viewMode === "Budget") {
                    total += calculateFLCForMonth(i, "budget");
                } else if (i <= currentMonthIdx) {
                    total += calculateFLCForMonth(i, "actual");
                } else {
                    total += calculateFLCForMonth(i, "forecast");
                }
            }

            return total;
        } else if (code === "flcCumulative") {
            const trendRow = getTrendRow(code);
            return trendRow[trendRow.length - 1];
        } else if (code === "internalRevenue"){
            return getActualPlusForecastTotal(code);
        } else {
            let total = 0;
            for (let i = 0; i < MONTHS.length; i++) {
                if (viewMode === "Budget") {
                    const budget = findRow(pnlData.budgetRows, code);
                    total += budget?.monthlyValues[i]?.value ?? 0;
                    continue;
                }
                if (code === "pteb" || code === "insurance") {
                    if (i < currentMonthIdx) {
                        const actual = findRow(pnlData.actualRows, code);
                        total += actual?.monthlyValues[i]?.value ?? 0;
                    } else {
                        const forecast = findRow(pnlData.forecastRows, code);
                        const budget = findRow(pnlData.budgetRows, code);
                        const fcVal = forecast?.monthlyValues[i]?.value ?? null;
                        total += (fcVal ?? (budget?.monthlyValues[i]?.value ?? 0));
                    }
                    continue;
                }
                if (i <= currentMonthIdx) {
                    const actual = findRow(pnlData.actualRows, code);
                    total += actual?.monthlyValues[i]?.value ?? 0;
                } else {
                    const forecast = findRow(pnlData.forecastRows, code);
                    total += forecast?.monthlyValues[i]?.value ?? 0;
                }
            }
            return total;
        }
    } 

    function getActualPlusForecastTotal(code: string) {
        if (!pnlData) return null;
        const actual = findRow(pnlData.actualRows, code);
        const forecast = findRow(pnlData.forecastRows, code);
    
        let total = 0;
        // Sum actuals for months up to and including currentMonthIdx
        for (let i = 0; i <= currentMonthIdx; i++) {
            total += actual?.monthlyValues[i]?.value ?? 0;
        }
        // Sum forecast for months after currentMonthIdx
        for (let i = currentMonthIdx + 1; i < MONTHS.length; i++) {
            total += forecast?.monthlyValues[i]?.value ?? 0;
        }
        return total;
    }

    function getActualTotal(code: string) {
        if (!pnlData) return null;
        if (code === "flcCumulative") {
            return null;
        }

        if (code === "flc") {
            let total = 0;

            for (let i = 0; i <= currentMonthIdx; i++) {
                total += calculateFLCForMonth(i, "actual");
            }

            return total;
        } else {
            const actual = findRow(pnlData.actualRows, code);
            if (!actual) return null;
            return actual.monthlyValues
                .filter((v: any, idx: number) => idx <= currentMonthIdx)
                .reduce((sum: number, v: any) => sum + (typeof v.value === "number" ? v.value : 0), 0);
        }
    }

    function getVarianceTotal(code: string) {
        if (!pnlData) return { amount: 0, percent: 0 };
        if (code === "flc") {
            const forecastTotal = MONTHS.reduce((sum, _, idx) => sum + calculateFLCForMonth(idx, "forecast"), 0);
            const budgetTotal = MONTHS.reduce((sum, _, idx) => sum + calculateFLCForMonth(idx, "budget"), 0);
            const amount = forecastTotal - budgetTotal;
            const percent = budgetTotal !== 0 ? (amount / budgetTotal) * 100 : 0;

            return { amount, percent };
        } else if (code === "flcCumulative") {
            const lastMonth = MONTHS.length - 1;
            const forecastCumulative = MONTHS.reduce((sum, _, idx) => {
                return sum + (calculateFLCForMonth(idx, "forecast") - calculateFLCForMonth(idx, "budget"));
            }, 0);

            const budgetSum = MONTHS.reduce((sum, _, idx) => sum + calculateFLCForMonth(idx, "budget"), 0);
            const percent = budgetSum !== 0 ? (forecastCumulative / budgetSum) * 100 : 0;

            return { amount: forecastCumulative, percent };
        } else {
            const variance = findRow(pnlData.varianceRows, code);
            return { amount: variance?.totalVarianceAmount ?? 0, percent: variance?.totalVariancePercent ?? 0 };
        }
    }

    function calculatePercentOfIR(rowCode: string): number | null {
        if (!pnlData) return null;
        if (rowCode === "internalRevenue") return 100.0;
        if (rowCode === "externalRevenue" || rowCode === "flcCumulative") return null;
    
        const irTotal = getTrendTotal("internalRevenue") ?? 0;
        const rowTotal = getTrendTotal(rowCode);
    
        if (irTotal <= 0) return null;
        if (rowTotal === null) return null;
        const percent = (rowTotal / irTotal) * 100;
        
        return percent;
    }

    const rowDefs = [
        { code: "externalRevenue", label: "External Revenue" },
        { code: "internalRevenue", label: "Internal Revenue" },
        { code: "payroll", label: "Payroll" },
        { code: "claims", label: "Claims" },
        { code: "parkingRents", label: "Parking Rents" },
        { code: "otherExpense", label: "Other Expense" },
        { code: "pteb", label: "PTEB" },
        { code: "insurance", label: "Insurance" },
        { code: "flc", label: "Front Line Contribution (FLC)" },
        { code: "flcCumulative", label: "FLC $ to Budget - Cumulative" },
    ];

    // Add row codes to this array to enable tooltips
const tooltipEnabledRows = ['claims', 'flc', 'flcCumulative', 'internalRevenue', 'externalRevenue', 'payroll', 'pteb', 'insurance', 'parkingRents']

    // Type definition for tooltip data
    type TooltipData = {
        actualAmount: number;
        forecastAmount?: number;
        actualUpToDate?: Date | string;
        forecastFromDate?: Date | string;
        rowCode: string;
        insurance?: {
            ratePercent?: number;
            basePayroll?: number;
            vehicleInsurance7082?: number;
            additionalInsurance?: number;
            isManagementAgreement?: boolean;
            source?: string;
            basePayrollSource?: string;
        };
        flcBreakdown?: {
            internalRevenue: number;
            expenses: {
                payroll: number;
                claims: number;
                parkingRents: number;
                otherExpense: number;
                pteb: number;
                insurance: number;
                total: number;
            };
        };
        flcCumulativeBreakdown?: {
            monthlyVariances: Array<{
                month: string;
                actualForecast: number;
                budget: number;
                variance: number;
            }>;
            totalVariance: number;
        };
        highlight?: string;
        label?: string;
    };

    function getTooltipData(rowCode: string, monthIdx: number): TooltipData | null {
        if (!pnlData) return null;
        
        const currentDate = new Date();
        const currentYear = currentDate.getFullYear();
        const currentMonth = currentDate.getMonth();
        const isCurrentMonth = Number(selectedYear) === currentYear && monthIdx === currentMonth;
        const isSingleSiteSelected = filteredSiteIds.length === 1;
        
        // Only show tooltips in Trend view (not Variance view), for current month, enabled rows, and when exactly one site is selected
        if (showVariance || !isCurrentMonth || !tooltipEnabledRows.includes(rowCode) || !isSingleSiteSelected) return null;
        
        // Get actual data from the API response
        const actualRow = findRow(pnlData.actualRows, rowCode);
        const actualAmount = actualRow?.monthlyValues[monthIdx]?.value ?? 0;
        
        // Get row-specific tooltip data
        return getTooltipDataForRow(rowCode, actualAmount, monthIdx);
    }

    function getTooltipDataForRow(rowCode: string, actualAmount: number, monthIdx: number): TooltipData {
        // Claims row - only actual amount, no forecast or dates
        if (rowCode === 'claims') {
            return {
                actualAmount,
                forecastAmount: undefined,
                actualUpToDate: undefined,
                forecastFromDate: undefined,
                rowCode,
            };
        }

        // PTEB tooltip - show Forecast (then Budget) regardless of actuals, with Month End date
        if (rowCode === 'pteb') {
            const actualRow = findRow(pnlData!.actualRows, 'pteb');
            const forecastRow = findRow(pnlData!.forecastRows, 'pteb');
            const budgetRow = findRow(pnlData!.budgetRows, 'pteb');
            const forecastVal = forecastRow?.monthlyValues[monthIdx]?.value ?? null;
            const budgetVal = budgetRow?.monthlyValues[monthIdx]?.value ?? 0;
            const usingForecast = typeof forecastVal === 'number' && !Number.isNaN(forecastVal);
            const amountToShow = usingForecast ? (forecastVal as number) : budgetVal;

            // Display date should be the last day of the prior month
            const monthEnd = new Date(Number(selectedYear), monthIdx, 0);

            // Pull PTEB breakdown from matching site detail on the selected row
            let ptebBreakdown: any = undefined;
            const siteNumber = filteredSiteIds?.[0];
            if (siteNumber) {
                const siteDetails = forecastRow?.monthlyValues?.[monthIdx]?.siteDetails as any[] | undefined;
                const sd = siteDetails?.find((s: any) => s?.siteId === siteNumber);
                ptebBreakdown = sd?.ptebBreakdown;
            }

            return {
                actualAmount: amountToShow,
                forecastAmount: undefined,
                actualUpToDate: usingForecast ? 'Forecast' : 'Budget',
                forecastFromDate: monthEnd,
                rowCode,
                // piggyback: include insurance for typing compatibility; rendering handled later
                insurance: undefined,
            } as TooltipData & { ptebBreakdown?: { ratePercent?: number; basePayroll?: number; source?: string } } & any;
        }

        // Insurance tooltip - show Actual when present; else Forecast (then Budget), with Month End date
        if (rowCode === 'insurance') {
            const actualRow = findRow(pnlData!.actualRows, 'insurance');
            const forecastRow = findRow(pnlData!.forecastRows, 'insurance');
            const budgetRow = findRow(pnlData!.budgetRows, 'insurance');
            const actualVal = actualRow?.monthlyValues[monthIdx]?.value ?? null;
            const forecastVal = forecastRow?.monthlyValues[monthIdx]?.value ?? null;
            const budgetVal = budgetRow?.monthlyValues[monthIdx]?.value ?? 0;
            const usingActual = typeof actualVal === 'number' && !Number.isNaN(actualVal) && (actualVal as number) > 0;
            const usingForecast = !usingActual && typeof forecastVal === 'number' && !Number.isNaN(forecastVal);
            const amountToShow = usingActual ? (actualVal as number) : usingForecast ? (forecastVal as number) : budgetVal;

            // Display date should be the last day of the prior month
            const monthEnd = new Date(Number(selectedYear), monthIdx, 0);

            // Pull breakdown from the matching site detail when available (single-site tooltip)
            let insuranceBreakdown: any = undefined;
            const siteNumber = filteredSiteIds?.[0];
            if (siteNumber) {
                const srcRow = usingActual ? actualRow : forecastRow;
                const siteDetails = srcRow?.monthlyValues?.[monthIdx]?.siteDetails as any[] | undefined;
                const sd = siteDetails?.find((s: any) => s?.siteId === siteNumber);
                insuranceBreakdown = sd?.insuranceBreakdown;
            }

            return {
                actualAmount: amountToShow,
                forecastAmount: undefined,
                actualUpToDate: usingActual ? 'Actual' : (usingForecast ? 'Forecast' : 'Budget'),
                forecastFromDate: monthEnd,
                rowCode,
                insurance: insuranceBreakdown ? {
                    ratePercent: insuranceBreakdown.ratePercent,
                    basePayroll: insuranceBreakdown.basePayroll,
                    vehicleInsurance7082: insuranceBreakdown.vehicleInsurance7082,
                    isManagementAgreement: insuranceBreakdown.isManagementAgreement,
                    source: insuranceBreakdown.source,
                } : undefined,
            };
        }

        // FLC row - show calculation breakdown
        if (rowCode === 'flc') {
            // There is no explicit FLC row from the API; compute it from constituent rows
            const rowsToUse = pnlData!.actualRows;
            const internalRevenue = findRow(rowsToUse, "internalRevenue")?.monthlyValues[monthIdx]?.value ?? 0;
            
            const expenses = {
                payroll: findRow(rowsToUse, "payroll")?.monthlyValues[monthIdx]?.value ?? 0,
                claims: findRow(rowsToUse, "claims")?.monthlyValues[monthIdx]?.value ?? 0,
                parkingRents: findRow(rowsToUse, "parkingRents")?.monthlyValues[monthIdx]?.value ?? 0,
                otherExpense: findRow(rowsToUse, "otherExpense")?.monthlyValues[monthIdx]?.value ?? 0,
                pteb: findRow(rowsToUse, "pteb")?.monthlyValues[monthIdx]?.value ?? 0,
                insurance: findRow(rowsToUse, "insurance")?.monthlyValues[monthIdx]?.value ?? 0,
                total: 0
            };
            expenses.total = expenses.payroll + expenses.claims + expenses.parkingRents + 
                            expenses.otherExpense + expenses.pteb + expenses.insurance;

            // Compute actual FLC for the month instead of relying on a non-existent API row
            const flcActualAmount = calculateFLCForMonth(monthIdx, "actual");

            return {
                actualAmount: flcActualAmount,
                forecastAmount: undefined,
                actualUpToDate: undefined,
                forecastFromDate: undefined,
                rowCode,
                flcBreakdown: {
                    internalRevenue,
                    expenses
                }
            };
        }

        // FLC Cumulative row - show monthly variance breakdown
        if (rowCode === 'flcCumulative') {
            const monthlyVariances = [];
            let cumulativeSum = 0;
            
            for (let i = 0; i <= monthIdx; i++) {
                const actualForecast = calculateFLCForMonth(i, "actual");
                const budget = calculateFLCForMonth(i, "budget");
                const variance = actualForecast - budget;
                cumulativeSum += variance;
                
                monthlyVariances.push({
                    month: MONTHS[i],
                    actualForecast,
                    budget,
                    variance
                });
            }

            return {
                actualAmount,
                forecastAmount: undefined,
                actualUpToDate: undefined,
                forecastFromDate: undefined,
                rowCode,
                flcCumulativeBreakdown: {
                    monthlyVariances,
                    totalVariance: cumulativeSum
                }
            };
        }

        if (rowCode === "externalRevenue") {
            if (!pnlData) {
                return {
                    actualAmount: 0,
                    forecastAmount: 0,
                    actualUpToDate: "N/A",
                    forecastFromDate: "N/A",
                    rowCode,
                };
            }
            const actualRow = findRow(pnlData.actualRows, rowCode);

            // Get breakdown from siteDetails[0] if available
            const mv = actualRow?.monthlyValues[monthIdx];
            const siteDetail = mv?.siteDetails?.[0];
            const breakdown = siteDetail?.externalRevenueBreakdown;
            const actualAmount = breakdown?.actualExternalRevenue ?? 0;
            const forecastAmount = breakdown?.forecastedExternalRevenue ?? 0;

            // Determine display dates: if no current-month actuals, show last day of prior month
            const monthStart = new Date(Number(selectedYear), monthIdx, 1);
            const lastDayPrevMonth = new Date(Number(selectedYear), monthIdx, 0);
            let actualUpToDate: Date | string = "N/A";
            let forecastFromDate: Date | string = "N/A";
            const lastActual = breakdown?.lastActualRevenueDate ? new Date(breakdown.lastActualRevenueDate) : null;
            const isSameMonth = !!lastActual && lastActual.getFullYear() === monthStart.getFullYear() && lastActual.getMonth() === monthStart.getMonth();
            if (isSameMonth && lastActual) {
                actualUpToDate = lastActual;
                forecastFromDate = new Date(lastActual.getFullYear(), lastActual.getMonth(), lastActual.getDate() + 1);
            } else {
                actualUpToDate = lastDayPrevMonth;
                forecastFromDate = monthStart;
            }

            return {
                actualAmount,
                forecastAmount,
                actualUpToDate,
                forecastFromDate,
                rowCode,
            };
        }

        // Internal Revenue row - show actual vs forecast split, actual split is in forecast row
        if (rowCode === 'internalRevenue') {
            const actualsRow = findRow(pnlData!.actualRows, "internalRevenue");
            const currentMonthData = actualsRow?.monthlyValues[monthIdx];
            
            // Get the current month split data from the API
            const currentMonthSplit = currentMonthData?.internalRevenueCurrentMonthSplit;
            
            if (currentMonthSplit) {
                // Ensure we have valid dates or fallback to previous month logic
                let actualUpToDate = currentMonthSplit.lastActualDate;
                let forecastFromDate = currentMonthSplit.forecastStartDate;
                
                // If dates are missing or invalid, try to find dates from previous months
                if (!actualUpToDate || actualUpToDate === "N/A") {
                    let earliestLastActualDate: Date | null = null;
                    
                    // Look back through previous months to find the earliest last actual date
                    for (let i = monthIdx - 1; i >= 0; i--) {
                        const prevMonthData = actualsRow?.monthlyValues[i];
                        const prevMonthSplit = prevMonthData?.internalRevenueCurrentMonthSplit;
                        if (prevMonthSplit?.lastActualDate) {
                            const date = new Date(prevMonthSplit.lastActualDate);
                            if (!earliestLastActualDate || date < earliestLastActualDate) {
                                earliestLastActualDate = date;
                            }
                        }
                    }
                    
                    // If we found a date from a previous month, use it
                    if (earliestLastActualDate) {
                        actualUpToDate = earliestLastActualDate;
                        const forecastDate = new Date(earliestLastActualDate);
                        forecastDate.setDate(earliestLastActualDate.getDate() + 1);
                        forecastFromDate = forecastDate;
                    }
                }
                
                return {
                    actualAmount: currentMonthSplit.actualTotal,
                    forecastAmount: currentMonthSplit.forecastTotal,
                    actualUpToDate,
                    forecastFromDate,
                    rowCode,
                };
            }
            
            // Fallback if no current month split data is available
            return {
                actualAmount,
                forecastAmount: 0,
                actualUpToDate: undefined,
                forecastFromDate: undefined,
                rowCode,
            };
        }

        // Payroll tooltip
        if (rowCode === "payroll") {
            if (!pnlData) {
                return {
                    actualAmount: 0,
                    forecastAmount: 0,
                    actualUpToDate: "N/A",
                    forecastFromDate: "N/A",
                    rowCode,
                };
            }
            const actualRow = findRow(pnlData.actualRows, rowCode);
            const mv = actualRow?.monthlyValues[monthIdx];
            const siteDetail = mv?.siteDetails?.[0];
            const breakdown = siteDetail?.payrollBreakdown;
            const actualAmount = breakdown?.actualPayroll ?? 0;
            const forecastAmount = breakdown?.forecastedPayroll ?? 0;

            // Determine display dates: if no current-month actuals, show last day of prior month
            let actualUpToDate: Date | string = "N/A";
            let forecastFromDate: Date | string = "N/A";
            const payrollMonthStart = new Date(Number(selectedYear), monthIdx, 1);
            const payrollLastDayPrevMonth = new Date(Number(selectedYear), monthIdx, 0);
            const payrollLastActual = breakdown?.actualPayrollLastDate ? new Date(breakdown.actualPayrollLastDate) : null;
            const payrollIsSameMonth = !!payrollLastActual && payrollLastActual.getFullYear() === payrollMonthStart.getFullYear() && payrollLastActual.getMonth() === payrollMonthStart.getMonth();
            if (payrollIsSameMonth && payrollLastActual) {
                actualUpToDate = payrollLastActual;
                forecastFromDate = new Date(payrollLastActual.getFullYear(), payrollLastActual.getMonth(), payrollLastActual.getDate() + 1);
            } else {
                actualUpToDate = payrollLastDayPrevMonth;
                forecastFromDate = payrollMonthStart;
            }

            return {
                actualAmount,
                forecastAmount,
                actualUpToDate,
                forecastFromDate,
                rowCode,
            };
        }

        // Parking Rents tooltip (custom logic)
        if (rowCode === "parkingRents") {
            if (!pnlData) {
                return {
                    actualAmount: 0,
                    forecastAmount: 0,
                    highlight: "none",
                    label: "",
                    rowCode,
                };
            }
            const actualRow = findRow(pnlData.actualRows, rowCode);
            const forecastRow = findRow(pnlData.forecastRows, rowCode);
            const budgetRow = findRow(pnlData.budgetRows, rowCode);
            const actualAmount = actualRow?.monthlyValues[monthIdx]?.value ?? 0;
            const forecastAmount = forecastRow?.monthlyValues[monthIdx]?.value ?? 0;
            const budgetAmount = budgetRow?.monthlyValues[monthIdx]?.value ?? 0;

            // Past months: fully actualized
            if (monthIdx < currentMonthIdx) {
                return {
                    actualAmount,
                    forecastAmount: undefined,
                    highlight: "actual",
                    label: "Actual",
                    rowCode,
                };
            }
            // Current month: emphasize greater of Actual/Forecast
            if (monthIdx === currentMonthIdx) {
                let highlight = "actual";
                let label = "Actual / Forecast";
                if (forecastAmount > actualAmount) highlight = "forecast";
                return {
                    actualAmount,
                    forecastAmount,
                    highlight,
                    label,
                    rowCode,
                };
            }
            // Future months
            if (forecastAmount === null || forecastAmount === 0) {
                // No forecast: show Actual/Budget
                return {
                    actualAmount,
                    forecastAmount: budgetAmount,
                    highlight: "budget",
                    label: "Actual / Budget",
                    rowCode,
                };
            }
            // Future months with forecast: show Actual/Forecast
            let highlight = "forecast";
            let label = "Actual / Forecast";
            if (actualAmount > forecastAmount) highlight = "actual";
            return {
                actualAmount,
                forecastAmount,
                highlight,
                label,
                rowCode,
            };
        }

        return {
            actualAmount,
            forecastAmount: 0,
            actualUpToDate: "N/A",
            forecastFromDate: "N/A",
            rowCode,
        };
    }

    // Check if should hide tooltip
    const shouldHideTooltip = () => {
        return !isHoveringTrigger.current && !isHoveringTooltip.current;
    };

    function handleRowMouseEnter(event: React.MouseEvent, rowCode: string, monthIdx: number) {
        const tooltipData = getTooltipData(rowCode, monthIdx);
        if (!tooltipData) return; // Don't show tooltip if not current month or no data
        
        isHoveringTrigger.current = true;
        clearAllTimeouts();
        
        // Show tooltip immediately for better responsiveness
        const rect = event.currentTarget.getBoundingClientRect();
        
        // Viewport dimensions for boundary detection
        const viewportWidth = window.innerWidth;
        const viewportHeight = window.innerHeight;
        const tooltipEstimatedWidth = 300; // Estimated tooltip width
        
        // More accurate height estimates based on actual content
        const tooltipEstimatedHeight = (() => {
            switch (rowCode) {
                case 'flcCumulative': 
                    return 450; // Tall: monthly breakdown with scrollable area
                case 'flc': 
                    return 500; // Very tall: title + amount + revenue + 6 expenses + total + dividers + padding
                case 'claims':
                    return 120; // Short: just title + amount
                default:
                    return 200; // Default for other tooltips
            }
        })();
        
        // Horizontal positioning: right side preferred, left side if not enough space
        const spaceOnRight = viewportWidth - rect.right;
        const spaceOnLeft = rect.left;
        const useRightSide = spaceOnRight >= tooltipEstimatedWidth || spaceOnRight >= spaceOnLeft;
        
        // Vertical positioning: center preferred, but adjust if would go off-screen
        let tooltipY = rect.top + rect.height / 2; // Default: center with cell
        const originalY = tooltipY;
        const tooltipBottom = tooltipY + tooltipEstimatedHeight / 2; // Bottom edge if centered
        const tooltipTop = tooltipY - tooltipEstimatedHeight / 2; // Top edge if centered
        
        let repositioned = false;
        
        // Adjust Y position if tooltip would go below viewport
        if (tooltipBottom > viewportHeight) {
            tooltipY = viewportHeight - tooltipEstimatedHeight / 2 - 20; // 20px margin from bottom
            repositioned = true;
        }
        
        // Adjust Y position if tooltip would go above viewport
        if (tooltipTop < 0) {
            tooltipY = tooltipEstimatedHeight / 2 + 20; // 20px margin from top
            repositioned = true;
        }
        
        const tooltipToShow = {
            visible: true,
            rowCode,
            month: monthIdx,
            x: useRightSide ? rect.right + 10 : rect.left - 10,
            y: tooltipY,
            rightSide: useRightSide,
            repositioned
        };

        // Always show the tooltip immediately on hover
        setActiveTooltip(tooltipToShow);
    }

    function handleRowMouseLeave() {
        isHoveringTrigger.current = false;
        clearAllTimeouts();
        
        // Wait before hiding to allow user to move to tooltip
        hideTimeoutRef.current = setTimeout(() => {
            if (shouldHideTooltip()) {
                setActiveTooltip(null);
            }
            hideTimeoutRef.current = null;
        }, 600); // Increased to 600ms for more generous timing
    }

    function handleTooltipMouseEnter() {
        isHoveringTooltip.current = true;
        clearAllTimeouts();
    }

    function handleTooltipMouseLeave() {
        isHoveringTooltip.current = false;
        clearAllTimeouts();
        
        // Hide tooltip immediately when leaving it
        setActiveTooltip(null);
    }

    const filterCount = Object.values(appliedFilters).reduce<number>((acc, v) => acc + (Array.isArray(v) ? v.length : 0), 0);

    return (
        <div className="w-full max-w-[95vw] mx-auto p-4">
            <div className="flex flex-col md:flex-row justify-between items-start md:items-center gap-4 mb-6">
                <h1 className="text-2xl font-bold">P&amp;L View</h1>
                <div className="flex flex-wrap items-center gap-4">
                    <Button
                        variant="outline"
                        className="flex items-center gap-2"
                        onClick={() => setFilterOpen(true)}
                        disabled={loading}
                        data-qa-id="button-filter"
                    >
                        <Filter className="h-4 w-4" />
                        Filters
                        {filterCount > 0 && (
                            <Badge variant="secondary" className="ml-1">{filterCount}</Badge>
                        )}
                    </Button>
                    <Button
                        variant="outline"
                        onClick={() => setViewMode(viewMode === "Forecast" ? "Budget" : "Forecast")}
                        disabled={loading}
                        data-qa-id="button-toggle-view-mode"
                    >
                        {viewMode === "Forecast" ? "Show Budget" : "Show Forecast"}
                    </Button>
                </div>
            </div>

            <Button
                variant="outline"
                size="sm"
                onClick={() => setShowGuide(g => !g)}
                className="flex items-center gap-1 mb-4"
                data-qa-id="pnl-guide-toggle"
            >
                <Info className="h-4 w-4" />
                {showGuide ? "Hide Guide" : "Show Guide"}
                {showGuide ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
            </Button>
            {showGuide && (
                <div className="space-y-4 p-4 border rounded-md bg-muted/20 mb-6" data-qa-id="guide-content">
                    <h3 className="text-lg font-semibold text-brand-navy">P&L View Guide</h3>
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                        <div className="space-y-3">
                            <div>
                                <h4 className="text-sm font-semibold mb-1">View Modes</h4>
                                <ul className="list-disc pl-5 text-sm space-y-1">
                                    <li>Toggle between <span className="text-brand-blue font-medium">Forecast</span> and <span className="text-brand-navy font-medium">Budget</span> using the view button.</li>
                                </ul>
                            </div>
                            <div>
                                <h4 className="text-sm font-semibold mb-1">Data Display</h4>
                                <ul className="list-disc pl-5 text-sm space-y-1">
                                    <li>
                                        <span className="bg-blue-50 dark:bg-blue-900 text-xs px-2 py-1 rounded font-medium">ACTUAL</span> months are highlighted with a blue background.
                                    </li>
                                    <li>
                                        <span className="font-bold">FLC</span> row values are bold for emphasis.
                                    </li>
                                </ul>
                            </div>
                            <div>
                                <h4 className="text-sm font-semibold mb-1">Trend vs Variance</h4>
                                <ul className="list-disc pl-5 text-sm space-y-1">
                                    <li><span className="font-medium">Trend:</span> Shows actual dollar amounts for each month.</li>
                                    <li><span className="font-medium">Variance:</span> Shows the difference between Forecast and Budget values.</li>
                                </ul>
                            </div>
                        </div>
                        <div className="space-y-3">
                            <div>
                                <h4 className="text-sm font-semibold mb-1">Variance Indicators</h4>
                                <ul className="list-disc pl-5 text-sm space-y-1">
                                    <li>
                                        <span className="text-green-600 dark:text-green-400 font-bold">▲ +1,000</span> Positive variance (Forecast exceeds Budget)
                                    </li>
                                    <li>
                                        <span className="text-red-600 dark:text-red-400 font-bold">▼ -1,000</span> Negative variance (Forecast below Budget)
                                    </li>
                                </ul>
                            </div>
                            <div>
                                <h4 className="text-sm font-semibold mb-1">Variance Significance</h4>
                                <ul className="list-disc pl-5 text-sm space-y-1">
                                    <li>
                                        <span className="text-green-600 dark:text-green-400 font-bold">7.5%+</span> High variance (bold) - requires attention
                                    </li>
                                    <li>
                                        <span className="text-green-600 dark:text-green-400">0-7.5%</span> Medium variance - within range
                                    </li>
                                    <li>
                                        <span className="text-[9px]">(+5.2%)</span> Percentage variance is shown below the dollar amount
                                    </li>
                                </ul>
                            </div>
                            <div>
                                <h4 className="text-sm font-semibold mb-1">Dark Mode Accessibility</h4>
                                <ul className="list-disc pl-5 text-sm space-y-1">
                                    <li>
                                        Colors are optimized for visibility in both light and dark themes.
                                    </li>
                                    <li>
                                        Positive variances use <span className="text-green-400 font-bold">green</span> in dark mode for clarity.
                                    </li>
                                    <li>
                                        Negative variances use <span className="text-red-400 font-bold">red</span> in dark mode.
                                    </li>
                                </ul>
                            </div>
                            <div>
                                <h4 className="text-sm font-semibold mb-1">Filtering</h4>
                                <ul className="list-disc pl-5 text-sm space-y-1">
                                    <li>Use the <span className="font-medium">Filters</span> button to open the filter modal and narrow your data by:</li>
                                    <li><span className="bg-blue-50 dark:bg-blue-900 text-xs px-2 py-0.5 rounded">Organizational</span>: Legal Entity, Region, District, Site, Account/District Manager</li>
                                    <li><span className="bg-green-50 dark:bg-green-900 text-xs px-2 py-0.5 rounded">Customer</span>: P&L Category, COG, Business Segment, Contract Type</li>
                                </ul>
                                <p className="text-xs text-gray-500 dark:text-gray-400 italic mt-1">Note: Filter data is sourced from the TP_EDW database for accurate reporting.</p>
                                <p className="text-xs text-gray-700 dark:text-gray-300 mt-2">The filter dialog shows which Site IDs are included in your current view. More organizational filters expand the list, more customer filters narrow it down.</p>
                            </div>
                        </div>
                    </div>
                    <Alert>
                        <AlertDescription>
                            Tip: For the most accurate variance analysis, compare Forecast to Budget using the Variance view.
                        </AlertDescription>
                    </Alert>
                </div>
            )}
            <CustomerFilter 
                open={filterOpen}
                onOpenChange={setFilterOpen}
                onApplyFilters={handleApplyFilters}
                currentFilters={appliedFilters}
                customers={customers}
            />
            <div className="flex items-center gap-4 mb-4">
                <div className="flex items-center">
                    <label htmlFor="year-select" className="text-sm font-medium mr-2">Year:</label>
                    <Select 
                        value={selectedYear} 
                        onValueChange={setSelectedYear} 
                        disabled={loading}
                        data-qa-id="select-year"
                    >
                        <SelectTrigger id="year-select" className="w-[120px] focus:ring-brand-blue" data-qa-id="trigger-year">
                            <SelectValue placeholder="Select Year" />
                        </SelectTrigger>
                        <SelectContent>
                            {yearOptions.map(y => (
                                <SelectItem key={y} value={String(y)} data-qa-id={`select-item-year-${y}`}>{y}</SelectItem>
                            ))}
                        </SelectContent>
                    </Select>
                </div>
                <Button
                    variant="outline"
                    size="sm"
                    onClick={() => setShowVariance(v => !v)}
                    disabled={loading}
                    className={`ml-auto ${showVariance ? "bg-brand-blue/10 border-brand-blue text-brand-blue" : ""}`}
                    data-qa-id="button-toggle-variance"
                >
                    {showVariance ? "Show Trend" : "Show Variance"}
                </Button>
            </div>
            <Card>
                <CardContent className="p-0">
                    {loading ? (
                        <>
                            <ProgressBar />
                            <SkeletonTable />
                            <AnimatedLoadingText />
                        </>
                    ) : loadError ? (
                        <div className="p-6 text-center text-red-600 font-bold">{loadError}</div>
                    ) : (
                        <div className="w-full overflow-auto">
                            <Table className="w-full table-fixed">
                                <TableHeader>
                                    <TableRow className="h-[50px]">
                                        <TableHead
                                            className="sticky left-0 bg-muted w-[160px] z-30"
                                            style={{ boxShadow: "2px 0 0 0 rgba(0,0,0,0.04)" }}
                                        >
                                            {selectedYear} {showVariance ? "VARIANCE" : "TREND"} ({viewMode})
                                        </TableHead>
                                        {MONTHS.map((m, idx) => (
                                            <TableHead
                                                key={m}
                                                className="text-center p-2 w-[85px] z-20 bg-muted"
                                                style={{ left: undefined }}
                                            >
                                                <div className="flex flex-col items-center text-xs">
                                                    <span>{m.toUpperCase()}</span>
                                                    {!showVariance && idx <= currentMonthIdx && (
                                                        <span className="text-[10px] opacity-80">ACTUAL</span>
                                                    )}
                                                </div>
                                            </TableHead>
                                        ))}
                                        <TableHead className="text-center p-2 w-[110px] z-20 bg-muted">TOTAL</TableHead>
                                        <TableHead className="text-center p-2 w-[85px] z-20 bg-muted">% of IR</TableHead>
                                    </TableRow>
                                </TableHeader>
                                <TableBody>
                                    {rowDefs.map(row => (
                                        <TableRow key={row.code} className="hover:bg-muted/50 h-[50px]">
                                            <TableCell className="sticky left-0 font-medium p-2 bg-background w-[140px]">
                                                <div className="text-sm">{row.label}</div>
                                            </TableCell>
                                            {MONTHS.map((_, idx) => (
                                                <TableCell
                                                    key={idx}
                                                className={`text-right p-1 w-[70px] h-[50px] relative ${row.code === "flcCumulative"
                                                        ? ""
                                                        : !showVariance && idx <= currentMonthIdx
                                                            ? "bg-blue-100 dark:bg-blue-950/30"
                                                            : ""
                                                         } ${Number(selectedYear) === new Date().getFullYear() && idx === new Date().getMonth() && !showVariance && tooltipEnabledRows.includes(row.code) && filteredSiteIds.length === 1 ? "cursor-help" : ""}`}
                                                    onMouseEnter={(e) => handleRowMouseEnter(e, row.code, idx)}
                                                    onMouseLeave={handleRowMouseLeave}
                                                >
                                                    <div className="h-[40px] flex flex-col justify-center">
                                                        {row.code === "flcCumulative" ? (
                                                            (() => {
                                                                const value = getTrendRow(row.code)[idx];
                                                                if (value == null) {
                                                                    return <span className="text-xs text-gray-400">-</span>;
                                                                }
                                                                return (
                                                                    <span className={`text-xs ${value < 0 ? "text-red-600" : "text-gray-700"}`}>
                                                                        {formatCurrencyWhole(value)}
                                                                    </span>
                                                                );
                                                            })()
                                                        ) : showVariance ? (
                                                            (() => {
                                                                const v = getVarianceRow(row.code)[idx];
                                                                if (!v || v.amount == null) return (
                                                                    <>
                                                                        <span className="text-[10px] text-gray-500">N/A</span>
                                                                        <span className="text-[8px] opacity-0">placeholder</span>
                                                                    </>
                                                                );
                                                                const itemType = ROW_TYPE_MAPPING[row.code] || 'revenue';
                                                                const cls = getVarianceClass(v.amount, v.percentage ?? 0, itemType);
                                                                return (
                                                                    <>
                                                                        <div className={`text-[10px] ${cls}`}>
                                                                            {v.amount === 0 ? "●" : v.amount > 0 ? "▲" : "▼"}
                                                                            {v.amount > 0 ? "+" : ""}
                                                                            {formatCurrencyWhole(v.amount).replace('$', '')}
                                                                        </div>
                                                                        <div className={`text-[8px] ${cls}`}>
                                                                            ({v.amount > 0 ? "+" : ""}{(v.percentage ?? 0).toFixed(1)}%)
                                                                        </div>
                                                                    </>
                                                                );
                                                            })()
                                                        ) : (
                                                            <>
                                                            <div className="text-xs">
                                                            {row.code === "parkingRents" && idx === currentMonthIdx
                                                                ? (() => {
                                                                    const actualRow = findRow(pnlData?.actualRows, "parkingRents");
                                                                    const forecastRow = findRow(pnlData?.forecastRows, "parkingRents");
                                                                    const actualVal = actualRow?.monthlyValues[idx]?.value;
                                                                    const forecastVal = forecastRow?.monthlyValues[idx]?.value;
                                                                    if (actualVal == null && forecastVal == null) return "-";
                                                                    if (actualVal == null) return formatCurrencyWhole(forecastVal as number);
                                                                    if (forecastVal == null) return formatCurrencyWhole(actualVal as number);
                                                                    return formatCurrencyWhole(Math.max(actualVal, forecastVal));
                                                                })()
                                                                : getTrendRow(row.code)[idx] != null
                                                                    ? formatCurrencyWhole(getTrendRow(row.code)[idx] as number)
                                                                    : "-"}
                                                            </div>
                                                                <div className="h-[12px]"></div>
                                                            </>
                                                        )}
                                                    </div>
                                                </TableCell>
                                            ))}
                                            <TableCell className="text-right p-1 w-[90px] h-[50px]">
                                                <div className="h-[40px] flex flex-col justify-center">
                                                    {showVariance ? (
                                                        (() => {
                                                            const t = getVarianceTotal(row.code);
                                                            const itemType = ROW_TYPE_MAPPING[row.code] || 'revenue';
                                                            const cls = getVarianceClass(t.amount, t.percent, itemType);
                                                            return (
                                                                <>
                                                                    <div className={`text-[10px] ${cls} font-medium`}>
                                                                        {t.amount === 0 ? "●" : t.amount > 0 ? "▲" : "▼"}
                                                                        {t.amount > 0 ? "+" : ""}
                                                                        {formatCurrencyWhole(t.amount).replace('$', '')}
                                                                    </div>
                                                                    <div className={`text-[8px] ${cls}`}>
                                                                        ({t.amount > 0 ? "+" : ""}{(t.percent ?? 0).toFixed(1)}%)
                                                                    </div>
                                                                </>
                                                            );
                                                        })()
                                                    ) : (
                                                        <>
                                                            <div className="text-xs font-bold">
                                                                {getTrendTotal(row.code) != null
                                                                    ? formatCurrencyWhole(getTrendTotal(row.code) as number)
                                                                    : "-"}
                                                            </div>
                                                            {row.code !== "flcCumulative" ? (
                                                                <div className="text-[10px] text-blue-600">
                                                                    {getActualTotal(row.code) != null
                                                                        ? formatCurrencyWhole(getActualTotal(row.code) as number)
                                                                        : "-"}
                                                                    <span className="text-[8px] text-gray-500 ml-1">ACT</span>
                                                                </div>
                                                            ) : <div className="h-[12px]"></div>}
                                                        </>
                                                    )}
                                                </div>
                                            </TableCell>
                                            <TableCell className="text-right p-1 w-[70px] h-[50px]">
                                                <div className="h-[40px] flex flex-col justify-center">
                                                    {!showVariance ? (
                                                        <>
                                                            <div className="text-xs">
                                                                {calculatePercentOfIR(row.code) !== null
                                                                    ? `${calculatePercentOfIR(row.code)?.toFixed(1)}%`
                                                                    : ""}
                                                            </div>
                                                            <div className="h-[12px]"></div>
                                                        </>
                                                    ) : (
                                                        <>
                                                            <div className="opacity-0 text-xs">-</div>
                                                            <div className="h-[12px]"></div>
                                                        </>
                                                    )}
                                                </div>
                                            </TableCell>
                                        </TableRow>
                                    ))}
                                </TableBody>
                            </Table>
                        </div>
                    )}
                </CardContent>
            </Card>
            
            {/* P&L Row Tooltip */}
            {activeTooltip && (() => {
                const isRightSide = activeTooltip.rightSide !== false; // Default to right side
                return (
                  <div
                        className="fixed z-50 pointer-events-auto"
           style={{
                left: activeTooltip.x,
                top: activeTooltip.y,
                            transform: isRightSide ? 'translateY(-50%)' : 'translateX(-100%) translateY(-50%)', // Position based on side
                            paddingLeft: isRightSide ? '10px' : '15px',   // Bridge gap on appropriate side
                paddingRight: isRightSide ? '15px' : '10px',
                            paddingTop: '15px',    // Vertical padding for easier targeting
                paddingBottom: '15px',
                            marginLeft: isRightSide ? '-10px' : '-15px',   // Compensate for padding
                            marginTop: '-15px',    // Compensate for vertical padding
                marginBottom: '-15px'
            }}
            onMouseEnter={handleTooltipMouseEnter}
            onMouseLeave={handleTooltipMouseLeave}
>
                    {(() => {
                        const tooltipData = getTooltipData(activeTooltip.rowCode, activeTooltip.month);
                        if (!tooltipData) return null;
                        
                        const formatDate = (date: Date) => {
                            return `${(date.getMonth() + 1).toString().padStart(2, '0')}-${date.getDate().toString().padStart(2, '0')}-${date.getFullYear()}`;
                        };

                       
                        const formatDateAny = (value?: Date | string) => {
                            if (!value) return 'N/A';
                            if (typeof value === 'string') {
                                const isoMatch = value.match(/^(\d{4})-(\d{2})-(\d{2})/);
                                if (isoMatch) {
                                    const [, year, month, day] = isoMatch;
                                    return `${month}-${day}-${year}`;
                                }
                                const parsed = new Date(value);
                                if (isNaN(parsed.getTime())) return 'N/A';
                                return formatDate(parsed);
                            }
                            return formatDate(value);
                        };

                        return (
                            <PnlTooltip
                                title={`${rowDefs.find(r => r.code === tooltipData.rowCode)?.label || tooltipData.rowCode} Details`}
                            >
                                {tooltipData.rowCode === 'claims' ? (
                                    // Claims tooltip - only show actual amount with blue background styling
                                    <TooltipAmountSection
                                        label="Actual Amount"
                                        amount={tooltipData.actualAmount}
                                        className="text-blue-800 dark:text-blue-300 bg-blue-50 dark:bg-blue-900/30 px-2 py-1 rounded font-semibold"
                                    />
                                ) : tooltipData.rowCode === 'flc' ? (
                                    // FLC tooltip - show calculation breakdown
                                    <>
                                        <TooltipAmountSection
                                            label="Front Line Contribution"
                                            amount={tooltipData.actualAmount}
                                            className="text-blue-800 dark:text-blue-300 bg-blue-50 dark:bg-blue-900/30 px-2 py-1 rounded font-semibold"
                                        />
                                        <TooltipDivider />
                                        <div className="text-sm">
                                            <div className="font-semibold mb-2 text-gray-900 dark:text-gray-100">Calculation:</div>
                                            <TooltipAmountSection
                                                label="Internal Revenue"
                                                amount={tooltipData.flcBreakdown?.internalRevenue || 0}
                                                className="text-green-600 dark:text-green-400"
                                            />
                                            <div className="text-xs text-gray-500 dark:text-gray-400 mb-1">Less Expenses:</div>
                                            <TooltipAmountSection
                                                label="  Payroll"
                                                amount={tooltipData.flcBreakdown?.expenses.payroll || 0}
                                                className="text-red-600 dark:text-red-400"
                                            />
                                            <TooltipAmountSection
                                                label="  Claims"
                                                amount={tooltipData.flcBreakdown?.expenses.claims || 0}
                                                className="text-red-600 dark:text-red-400"
                                            />
                                            <TooltipAmountSection
                                                label="  Parking Rents"
                                                amount={tooltipData.flcBreakdown?.expenses.parkingRents || 0}
                                                className="text-red-600 dark:text-red-400"
                                            />
                                            <TooltipAmountSection
                                                label="  Other Expense"
                                                amount={tooltipData.flcBreakdown?.expenses.otherExpense || 0}
                                                className="text-red-600 dark:text-red-400"
                                            />
                                            <TooltipAmountSection
                                                label="  PTEB"
                                                amount={tooltipData.flcBreakdown?.expenses.pteb || 0}
                                                className="text-red-600 dark:text-red-400"
                                            />
                                            <TooltipAmountSection
                                                label="  Insurance"
                                                amount={tooltipData.flcBreakdown?.expenses.insurance || 0}
                                                className="text-red-600 dark:text-red-400"
                                            />
                                            <TooltipDivider />
                                            <TooltipAmountSection
                                                label="Total Expenses"
                                                amount={tooltipData.flcBreakdown?.expenses.total || 0}
                                                className="text-red-600 dark:text-red-400 font-semibold"
                                            />
                                        </div>
                                    </>
                                ) : tooltipData.rowCode === 'flcCumulative' ? (
                                    // FLC Cumulative tooltip - show monthly variance breakdown
                                    <>
                                        <div className="text-sm">
                                            <div className="font-semibold mb-2 text-gray-900 dark:text-gray-100">Monthly Breakdown:</div>
                                            <div className="max-h-40 overflow-y-auto">
                                            {tooltipData.flcCumulativeBreakdown?.monthlyVariances.map((variance, idx) => (
                                                <div key={idx} className="mb-1 p-1 bg-gray-50 dark:bg-gray-800 rounded">
                                                    <div className="font-medium text-gray-900 dark:text-gray-100">{variance.month}</div>
                                                    <div className="text-xs">
                                                        <div className="text-gray-700 dark:text-gray-300">Actual: {variance.actualForecast != null ? formatCurrencyWhole(variance.actualForecast) : "-"}</div>
                                                        <div className="text-gray-700 dark:text-gray-300">Budget: {variance.budget != null ? formatCurrencyWhole(variance.budget) : "-"}</div>
                                                        <div className={`font-semibold ${variance.variance >= 0 ? "text-green-600 dark:text-green-400" : "text-red-600 dark:text-red-400"}`}>
                                                            Variance: {variance.variance != null ? formatCurrencyWhole(variance.variance) : "-"}
                                                        </div>
                                                    </div>
                                                </div>
                                            ))}
                                            </div>
                                            <TooltipDivider />
                                            <TooltipAmountSection
                                                label="Total Cumulative Variance"
                                                amount={tooltipData.flcCumulativeBreakdown?.totalVariance || 0}
                                                className={`text-blue-800 dark:text-blue-300 bg-blue-50 dark:bg-blue-900/30 px-2 py-1 rounded font-semibold ${(tooltipData.flcCumulativeBreakdown?.totalVariance || 0) >= 0 ? "text-green-600 dark:text-green-400" : "text-red-600 dark:text-red-400"}`}
                                            />
                                        </div>
                                    </>
                                ) : tooltipData.rowCode === 'internalRevenue' ? (
                                    // Internal Revenue tooltip - show actual vs forecast with special styling
                                    <>
                                        <TooltipComparisonSection
                                            leftLabel="Actual"
                                            leftAmount={tooltipData.actualAmount}
                                            leftClassName="text-blue-800 dark:text-blue-300 bg-blue-50 dark:bg-blue-900/30 px-2 py-1 rounded font-semibold"
                                            rightLabel="Forecast"
                                            rightAmount={tooltipData.forecastAmount || 0}
                                              rightClassName="text-gray-800 dark:text-gray-300 px-2 py-1 rounded font-semibold"
                                        />
                                        <TooltipDivider />
                                        <div className="text-center space-y-1">
                                            <div className="text-xs text-gray-600 dark:text-gray-400 font-medium">
                                         Actual up to / Forecast from
                                            </div>
                                            <div className="text-xs">
                                                <span className="text-blue-800 dark:text-blue-300 bg-blue-50 dark:bg-blue-900/30 px-1 py-0.5 rounded font-semibold">
                                                    {formatDateAny(tooltipData.actualUpToDate)}
                                                </span>
                                                <span className="text-gray-600 dark:text-gray-400 mx-1">/</span>
                                                <span className="text-gray-700 dark:text-gray-300">
                                                    {formatDateAny(tooltipData.forecastFromDate)}
                                                </span>
                                            </div>
                                        </div>
                                    </>
                                ) : tooltipData.rowCode === 'externalRevenue' ? (
                                    // External Revenue tooltip
                                    <>
                                        <TooltipComparisonSection
                                            leftLabel="Actual"
                                            leftAmount={tooltipData.actualAmount}
                                            leftClassName="text-blue-800 dark:text-blue-300 bg-blue-50 dark:bg-blue-900/30 px-2 py-1 rounded font-semibold"
                                            rightLabel="Forecast"
                                            rightAmount={tooltipData.forecastAmount || 0}
                                           rightClassName="text-gray-800 dark:text-gray-300 px-2 py-1 rounded font-semibold"
                                        />
                                        <TooltipDivider />
                                        <div className="text-center space-y-1">
                                            <div className="text-xs text-gray-600 dark:text-gray-400 font-medium">
                                              Actual up to/ Forecast from
                                            </div>
                                            <div className="text-xs">
                                                <span className="text-blue-800 dark:text-blue-300 bg-blue-50 dark:bg-blue-900/30 px-1 py-0.5 rounded font-semibold">
                                                    {formatDateAny(tooltipData.actualUpToDate)}
                                                </span>
                                                <span className="text-gray-600 dark:text-gray-400 mx-1">/</span>
                                                <span className="text-gray-700 dark:text-gray-300">
                                                    {formatDateAny(tooltipData.forecastFromDate)}
                                                </span>
                                            </div>
                                        </div>
                                    </>
                                ) : tooltipData.rowCode === 'payroll' ? (
                                    // Payroll tooltip - match External/Internal Revenue style
                                    <>
                                        <TooltipComparisonSection
                                            leftLabel="Actual"
                                            leftAmount={tooltipData.actualAmount}
                                            leftClassName="text-blue-800 dark:text-blue-300 bg-blue-50 dark:bg-blue-900/30 px-2 py-1 rounded font-semibold"
                                            rightLabel="Forecast"
                                            rightAmount={tooltipData.forecastAmount || 0} 
                                             rightClassName="text-gray-800 dark:text-gray-300 px-2 py-1 rounded font-semibold"
                                          
                                        />
                                        <TooltipDivider />
                                        <div className="text-center space-y-1">
                                            <div className="text-xs text-gray-600 dark:text-gray-400 font-medium">
                                         Actual up to/ Forecast from
                                            </div>
                                            <div className="text-xs">
                                                <span className="text-blue-800 dark:text-blue-300 bg-blue-50 dark:bg-blue-900/30 px-1 py-0.5 rounded font-semibold">
                                                    {formatDateAny(tooltipData.actualUpToDate)}
                                                </span>
                                                <span className="text-gray-600 dark:text-gray-400 mx-1">/</span>
                                                <span className="text-gray-700 dark:text-gray-300">
                                                    {formatDateAny(tooltipData.forecastFromDate)}
                                                </span>
                                            </div>
                                        </div>
                                    </>
                                ) : tooltipData.rowCode === 'pteb' ? (
                                    // PTEB tooltip - show amount and, when forecast, the rate × base calculation
                                    <>
                                        {(() => {
                                            const siteNumber = filteredSiteIds?.[0];
                                            const forecastRow = findRow(pnlData!.forecastRows, 'pteb');
                                            const mv = forecastRow?.monthlyValues[activeTooltip!.month];
                                            const sd: any = mv?.siteDetails?.find((s: any) => s?.siteId === siteNumber);
                                            const pteb = sd?.ptebBreakdown;
                                            const labelText = (pteb && typeof pteb.source === 'string' && (pteb.source === 'Actual' || pteb.source === 'Actuals'))
                                                ? 'Actuals'
                                                : ((tooltipData.actualUpToDate as string) || "Forecast");
                                            return (
                                                <TooltipAmountSection
                                                    label={labelText}
                                                    amount={tooltipData.actualAmount}
                                                    className="text-blue-800 dark:text-blue-300 bg-blue-50 dark:bg-blue-900/30 px-2 py-1 rounded font-semibold"
                                                />
                                            );
                                        })()}
                                        {(() => {
                                            // Safely read pteb breakdown if present
                                            const siteNumber = filteredSiteIds?.[0];
                                            const forecastRow = findRow(pnlData!.forecastRows, 'pteb');
                                            const mv = forecastRow?.monthlyValues[activeTooltip!.month];
                                            const sd: any = mv?.siteDetails?.find((s: any) => s?.siteId === siteNumber);
                                            const pteb = sd?.ptebBreakdown;
                                            if (pteb && pteb.source !== 'Actual' && typeof pteb.ratePercent === 'number' && typeof pteb.basePayroll === 'number') {
                                                return (
                                                    <div className="mt-2 text-xs sm:text-sm space-y-2">
                                                        <div className="font-semibold text-gray-900 dark:text-gray-100">Calculation</div>
                                                        <TooltipAmountSection
                                                            label={`Rate × Forecasted Payroll (${pteb.ratePercent}% of Forecast)`}
                                                            amount={tooltipData.actualAmount}
                                                            className="text-gray-700 dark:text-gray-300"
                                                        />
                                                    </div>
                                                );
                                            }
                                            return null;
                                        })()}
                                       
                                        
                                    </>
                                ) : tooltipData.rowCode === 'insurance' ? (
                                    // Insurance tooltip with source label, breakdown, and Month End date
                                    <>
                                        <TooltipAmountSection
                                            label={(tooltipData.actualUpToDate as string) || "Forecast"}
                                            amount={tooltipData.actualAmount}
                                            className="text-blue-800 dark:text-blue-300 bg-blue-50 dark:bg-blue-900/30 px-2 py-1 rounded font-semibold"
                                        />
                                        {tooltipData.insurance && (
                                            <div className="mt-2 text-xs sm:text-sm space-y-2">
                                                {tooltipData.insurance.source !== 'Actual' ? (
                                                    <>
                                                        <div className="font-semibold text-gray-900 dark:text-gray-100">Calculation</div>
                                                        {(() => {
                                                            const base = tooltipData.insurance!.basePayroll ?? 0;
                                                            const rate = tooltipData.insurance!.ratePercent ?? 0;
                                                            const baseSource = tooltipData.insurance!.basePayrollSource ?? 'Forecast';
                                                            const isMA = !!tooltipData.insurance!.isManagementAgreement;
                                                            const vi7082 = tooltipData.insurance!.vehicleInsurance7082 ?? 0;
                                                            // Use backend-calculated total to avoid rounding drift; derive base×rate portion from it
                                                            const backendTotal = tooltipData.actualAmount;
                                                            const baseRatePortion = isMA ? Math.max(0, backendTotal - vi7082) : backendTotal;
                                                            return (
                                                                <div className="space-y-1">
                                                                    <TooltipAmountSection
                                                                        label={`Base × Rate (${rate}% of ${baseSource})`}
                                                                        amount={baseRatePortion}
                                                                        className="text-gray-700 dark:text-gray-300"
                                                                    />
                                                                    {isMA && (
                                                                        <>
                                                                            <TooltipAmountSection
                                                                                label="Vehicle Insurance (7082)"
                                                                                amount={vi7082}
                                                                                className="text-gray-700 dark:text-gray-300"
                                                                            />
                                                                            {/* Additional Insurance intentionally omitted from P&L tooltip */}
                                                                        </>
                                                                    )}
                                                                </div>
                                                            );
                                                        })()}
                                                    </>
                                                ) : (
                                                    <div className="text-gray-700 dark:text-gray-300">Actual</div>
                                                )}
                                            </div>
                                        )}
                                      
                                    </>
                                ) : tooltipData.rowCode === 'parkingRents' ? (
                                    // Parking Rents tooltip - conditional highlight
                                    <>
                                        <TooltipComparisonSection
                                            leftLabel="Actual"
                                            leftAmount={tooltipData.actualAmount}
                                            leftClassName={tooltipData.highlight === "actual"
                                                ? "text-blue-800 dark:text-blue-300 bg-blue-50 dark:bg-blue-900/30 px-2 py-1 rounded font-semibold"
                                                : "text-gray-700 dark:text-gray-300"}
                                            rightLabel={tooltipData.label === "Actual / Budget" ? "Budget" : "Forecast"}
                                            rightAmount={tooltipData.forecastAmount || 0}
                                            rightClassName={tooltipData.highlight === "forecast" || tooltipData.highlight === "budget"
                                                ? "text-blue-800 dark:text-blue-300 bg-blue-50 dark:bg-blue-900/30 px-2 py-1 rounded font-semibold"
                                                : "text-gray-800 dark:text-gray-300 px-2 py-1 rounded font-semibold"}
                                        />
                                    </>
                                ) : (
                                    // Other rows - static highlight
                                    <>
                                        <TooltipComparisonSection
                                            leftLabel="Actual"
                                            leftAmount={tooltipData.actualAmount}
                                            leftClassName="text-blue-800 dark:text-blue-300 bg-blue-50 dark:bg-blue-900/30 px-2 py-1 rounded font-semibold"
                                            rightLabel="Forecast"
                                            rightAmount={tooltipData.forecastAmount || 0}
                                         rightClassName="text-gray-800 dark:text-gray-300 px-2 py-1 rounded font-semibold"
                                        />
                                    </>
                                )}
                            </PnlTooltip>
                        );
                    })()}
                    </div>
                );
            })()}
        </div>
    );
}
