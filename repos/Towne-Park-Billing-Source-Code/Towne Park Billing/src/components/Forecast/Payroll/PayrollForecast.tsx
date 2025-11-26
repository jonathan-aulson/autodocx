import { Badge } from "@/components/ui/badge";
import { Collapsible, CollapsibleContent } from "@/components/ui/collapsible";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip";
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from "@/components/ui/dialog";
import type { Contract } from "@/lib/models/Contract";
import type { JobCode } from "@/lib/models/jobCode";
import { JobCodeForecastDto, JobGroupForecastDto, JobGroupScheduledDto, PayrollDetailDto, PayrollDto } from "@/lib/models/Payroll";
import { Customer } from "@/lib/models/Statistics";
import { formatCurrency } from "@/lib/utils";
import { Calendar, ChevronDown, ChevronLeft, ChevronRight, ChevronUp, Edit3, Info } from "lucide-react";
import { forwardRef, useCallback, useEffect, useImperativeHandle, useState } from "react";
import { Alert, AlertDescription } from "../../ui/alert";
import { Button } from "../../ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "../../ui/card";
import { Skeleton } from "../../ui/skeleton";
import { useToast } from "../../ui/use-toast";
import ForecastEditDialog, { ForecastDayData } from "./ForecastEditDialog";
import { ReconciliationDashboard, usePayrollReconciliation } from "./reconciliationDashboard";

const MAX_EXPECTED_HOURS = 1500;

export interface PayrollForecastProps {
    customers: Customer[];
    error: string | null;
    selectedSite: string;
    startingMonth: string;
    isGuideOpen: boolean;
    setIsGuideOpen: (value: boolean) => void;
    hasUnsavedChanges: boolean;
    setHasUnsavedChanges: (dirty: boolean) => void;
    onLoadingChange?: (loading: boolean) => void;
    onContractDetailsChange?: (contractDetails: Contract | null) => void;
}

const PayrollForecast = forwardRef(function PayrollForecast(
    {
        customers,
        error,
        selectedSite,
        startingMonth,
        isGuideOpen,
        setIsGuideOpen,
        hasUnsavedChanges,
        setHasUnsavedChanges,
        onLoadingChange,
        onContractDetailsChange
    }: PayrollForecastProps,
    ref
) {
    const [isLoadingPayroll, setIsLoadingPayroll] = useState(false);

    useEffect(() => {
        if (onLoadingChange) {
            onLoadingChange(isLoadingPayroll);
        }
    }, [isLoadingPayroll, onLoadingChange]);
    const [isSaving, setIsSaving] = useState(false);
    const [payrollData, setPayrollData] = useState<PayrollDto | null>(null);
    const [forecastPayrollDetails, setForecastPayrollDetails] = useState<PayrollDetailDto[]>([]);
    const [forecastJobGroups, setForecastJobGroups] = useState<JobGroupForecastDto[]>([]);

    // Enhanced state for business logic
    const [contractDetails, setContractDetails] = useState<Contract | null>(null);
    const [jobCodes, setJobCodes] = useState<JobCode[]>([]);
    const [isLoadingContract, setIsLoadingContract] = useState(false);
    const [isLoadingJobCodes, setIsLoadingJobCodes] = useState(false);
    const [budgetPayrollDetails, setBudgetPayrollDetails] = useState<PayrollDetailDto[]>([]);
    const [actualPayrollDetails, setActualPayrollDetails] = useState<PayrollDetailDto[]>([]);
    const [scheduledPayrollDetails, setScheduledPayrollDetails] = useState<PayrollDetailDto[]>([]);
    const [selectedDates, setSelectedDates] = useState<Date[]>([]);
    const [isPastPeriod, setIsPastPeriod] = useState<boolean>(false);
    const [visibleWeekIndex, setVisibleWeekIndex] = useState(0);
    const [expandedDates, setExpandedDates] = useState<Record<string, boolean>>({});
    const [selectedJob, setSelectedJob] = useState("all");
    const [viewMode, setViewMode] = useState<"hours" | "cost">("hours");
    const [weeks, setWeeks] = useState<{ start: Date, end: Date, dates: Date[] }[]>([]);
    const [scheduledData, setScheduledData] = useState<Record<string, Record<string, number>>>({});

    // Job group level data - stores group totals directly from API
    const [scheduledGroupData, setScheduledGroupData] = useState<Record<string, Record<string, number>>>({});
    const [actualGroupData, setActualGroupData] = useState<Record<string, Record<string, number>>>({});

    // Hierarchical expansion state for Days -> Job Groups -> Job Codes structure
    const [expandedDays, setExpandedDays] = useState<Set<string>>(new Set());
    const [expandedJobGroups, setExpandedJobGroups] = useState<Set<string>>(new Set());
    const [actualData, setActualData] = useState<Record<string, Record<string, number>>>({});
    const [forecastData, setForecastData] = useState<Record<string, Record<string, number>>>({});
    const [budgetData, setBudgetData] = useState<Record<string, Record<string, number>>>({});
    const [isEditDialogOpen, setIsEditDialogOpen] = useState(false);
    const [currentEditData, setCurrentEditData] = useState<ForecastDayData | null>(null);
    const [availableDates, setAvailableDates] = useState<Date[]>([]);
    const [availableJobs, setAvailableJobs] = useState<Array<{ id: string; name: string; type: 'jobGroup' | 'jobCode' }>>([]);
    const [currentDateIndex, setCurrentDateIndex] = useState<number>(0);
    const [currentJobIndex, setCurrentJobIndex] = useState<number>(0);
    const [isHandlingWarning, setIsHandlingWarning] = useState(false);

    // Copy Schedule to Forecast functionality state
    const [canUndo, setCanUndo] = useState(false);
    const [undoSnapshot, setUndoSnapshot] = useState<{
        data: JobGroupForecastDto[] | null;
        wasFallback: boolean;
    } | null>(null);
    const [showCopyConfirmation, setShowCopyConfirmation] = useState(false);
    const [copyStats, setCopyStats] = useState<{
        jobGroupsCopied: number;
        jobCodesCopied: number;
        excludedSalaries: number;
    } | null>(null);

    const { toast } = useToast();

    // Helper function for data transformation to eliminate duplication
    const transformJobGroupToPayrollDetails = (
        jobGroups: any[] | undefined,
        hoursProperty: string
    ): PayrollDetailDto[] => {
        const details: PayrollDetailDto[] = [];
        if (jobGroups && Array.isArray(jobGroups)) {
            jobGroups.forEach((jobGroup: any) => {
                if (jobGroup.jobCodes && Array.isArray(jobGroup.jobCodes)) {
                    jobGroup.jobCodes.forEach((jobCode: any) => {
                        // Ensure we have valid date and jobCode values
                        const date = jobCode.date || '';
                        const jobCodeValue = jobCode.jobCode || '';
                        const hours = jobCode[hoursProperty] || 0;



                        // Only add if we have valid data
                        if (date && jobCodeValue) {
                            details.push({
                                id: jobCode.id,
                                date: date,
                                displayName: jobCode.displayName,
                                jobCode: jobCodeValue,
                                regularHours: hours
                            });
                        }
                    });
                }
            });
        }
        return details;
    };

    // Helper functions for hierarchical expansion
    const toggleDayExpansion = (dateKey: string) => {
        setExpandedDays(prev => {
            const newSet = new Set(prev);
            if (newSet.has(dateKey)) {
                newSet.delete(dateKey);
            } else {
                newSet.add(dateKey);
            }
            return newSet;
        });
    };

    const toggleJobGroupExpansion = (jobGroupKey: string) => {
        setExpandedJobGroups(prev => {
            const newSet = new Set(prev);
            if (newSet.has(jobGroupKey)) {
                newSet.delete(jobGroupKey);
            } else {
                newSet.add(jobGroupKey);
            }
            return newSet;
        });
    };

    interface JobRole {
        displayName: string;
        jobCode: string;
        hourlyRate: number;
    }
    const jobRoles: JobRole[] = [];

    // Business logic functions
    const determineContractType = (payrollData: PayrollDto, contractDetails?: Contract | null): "Standard" | "PerLaborHour" => {
        // Primary: Check payrollForecastMode from API
        if (payrollData.payrollForecastMode === "Code") return "PerLaborHour";
        if (payrollData.payrollForecastMode === "Group") return "Standard";

        // Fallback: Check contract details
        return contractDetails?.perLaborHour?.enabled ? "PerLaborHour" : "Standard";
    };

    const fetchContractDetails = async (customerSiteId: string) => {
        setIsLoadingContract(true);
        try {
            const response = await fetch(`/api/customers/${customerSiteId}/contract`);
            if (response.ok) {
                const contract = await response.json();
                setContractDetails(contract);
                if (onContractDetailsChange) {
                    onContractDetailsChange(contract);
                }
            }
        } catch (error) {
            console.error('Failed to fetch contract details:', error);
        } finally {
            setIsLoadingContract(false);
        }
    };

    const checkForUnknownGroupJobCodes = (jobCodesData: JobCode[]) => {
        const unknownGroupJobCodes = jobCodesData.filter(jc => !jc.jobGroupId || jc.jobGroupId === 'unknown-group');
        
        if (unknownGroupJobCodes.length > 0) {
            const jobCodesList = unknownGroupJobCodes.map(jc => jc.jobCode).join(', ');
            toast({
                title: "Job Codes Missing Job Groups",
                description: `The following job codes are not assigned to a group and must be fixed before saving: ${jobCodesList}.`,
                variant: "destructive",
                duration: 8000
            });
        }
        
        return unknownGroupJobCodes;
    };

    const fetchJobCodes = async (customerSiteId: string) => {
        setIsLoadingJobCodes(true);
        try {
            const response = await fetch(`/api/job-codes/by-site/${customerSiteId}`);
            if (response.ok) {
                const jobCodesData = await response.json();
                setJobCodes(jobCodesData);
                
                checkForUnknownGroupJobCodes(jobCodesData);
            }
        } catch (error) {
            console.error('Failed to fetch job codes:', error);
        } finally {
            setIsLoadingJobCodes(false);
        }
    };

    const getBillableRates = (jobCode: string, contractDetails?: Contract | null) => {
        if (!contractDetails?.perLaborHour?.enabled) return undefined;

        const jobRate = contractDetails.perLaborHour.jobRates?.find(
            rate => rate.jobCode === jobCode
        );

        return jobRate ? {
            rate: jobRate.rate,
            overtimeRate: jobRate.overtimeRate
        } : undefined;
    };

    const getJobCodeDetails = (jobCodeId: string): JobCode | undefined => {
        return jobCodes.find(jc => jc.jobCodeId === jobCodeId);
    };

    // Copy Schedule to Forecast functionality
    const hasScheduledData = useCallback((): boolean => {
        return Boolean(
            payrollData?.scheduledPayroll?.length &&
            payrollData.scheduledPayroll.some(group =>
                group.scheduledHours > 0 ||
                group.jobCodes?.some(jc => jc.scheduledHours > 0)
            )
        );
    }, [payrollData]);

    const getCopyPreview = useCallback(() => {
        if (!payrollData?.scheduledPayroll) {
            return { jobGroupCount: 0, jobCodeCount: 0, excludedCount: 0 };
        }

        let jobGroupCount = 0;
        let jobCodeCount = 0;
        let excludedCount = 0;

        payrollData.scheduledPayroll.forEach(group => {
            if (group.scheduledHours > 0) {
                // Check if group has any hourly jobs
                const hasHourlyJobs = group.jobCodes?.some(jc =>
                    jc.scheduledHours > 0 && !isSalariedJobCode(jc.jobCodeId)
                ) ?? false;

                if (hasHourlyJobs) {
                    jobGroupCount++;
                }
            }

            if (group.jobCodes) {
                group.jobCodes.forEach(jc => {
                    if (jc.scheduledHours > 0) {
                        if (isSalariedJobCode(jc.jobCodeId)) {
                            excludedCount++;
                        } else {
                            jobCodeCount++;
                        }
                    }
                });
            }
        });

        return { jobGroupCount, jobCodeCount, excludedCount };
    }, [payrollData, getJobCodeDetails]);

    // Helper function to get current displayed value for a job code
    const getCurrentDisplayedValue = (dateKey: string, jobCode: string): number => {
        // Priority: forecast data > budget data > 0
        return forecastData[dateKey]?.[jobCode] ?? budgetData[dateKey]?.[jobCode] ?? 0;
    };

    // Helper function to check if a job code is salaried
    const isSalariedJobCode = (jobCodeId: string): boolean => {
        const jobCodeDetails = getJobCodeDetails(jobCodeId);
        return Boolean(jobCodeDetails?.allocatedSalaryCost);
    };

    // Helper function to preserve salaried position values
    const preserveSalariedValues = (scheduledGroup: JobGroupScheduledDto, dateKey: string): Map<string, JobCodeForecastDto> => {
        const salariedPreservation = new Map<string, JobCodeForecastDto>();

        scheduledGroup.jobCodes?.forEach(jc => {
            if (jc.jobCodeId && isSalariedJobCode(jc.jobCodeId)) {
                const currentHours = getCurrentDisplayedValue(dateKey, jc.jobCode!);
                const currentCost = calculateJobCodeCost(jc.jobCode!, currentHours);

                salariedPreservation.set(jc.jobCodeId, {
                    id: undefined,
                    jobCodeId: jc.jobCodeId,
                    jobCode: jc.jobCode,
                    displayName: jc.displayName,
                    forecastHours: currentHours,
                    forecastPayrollCost: currentCost,
                    forecastPayrollRevenue: currentCost, // Assuming same for salary
                    date: jc.date
                });
            }
        });

        return salariedPreservation;
    };

    const performCopyOperation = useCallback((): {
        jobGroupsCopied: number;
        jobCodesCopied: number;
        excludedSalaries: number;
        newForecastData: JobGroupForecastDto[];
    } => {
        if (!payrollData?.scheduledPayroll) {
            throw new Error('Scheduled data not available');
        }

        // Handle missing forecast data by creating empty forecast structure
        if (!payrollData?.forecastPayroll || payrollData.forecastPayroll.length === 0) {
            // Create forecast data structure from scheduled data and copy values
            const newForecastData: JobGroupForecastDto[] = payrollData.scheduledPayroll.map(scheduledGroup => {
                // Helper function to check if a date is in the past (excludes current day)
                const isPastDate = (dateString?: string): boolean => {
                    if (!dateString) return false;
                    const today = new Date();
                    today.setHours(0, 0, 0, 0); // Reset time to start of day
                    const date = new Date(dateString);
                    date.setHours(0, 0, 0, 0); // Reset time to start of day
                    return date < today; // Current day (date === today) is allowed
                };

                // Skip past dates - preserve existing fallback data instead of zeroing
                if (isPastDate(scheduledGroup.date)) {
                    // For past dates, preserve existing fallback values from budget data
                    const dateKey = formatDateKey(new Date(scheduledGroup.date!));
                    const preservedJobCodes = scheduledGroup.jobCodes?.map(jc => {
                        // Get current displayed value (forecast > budget > 0)
                        const currentHours = getCurrentDisplayedValue(dateKey, jc.jobCode!);
                        const currentCost = calculateJobCodeCost(jc.jobCode!, currentHours);

                        return {
                            id: undefined,
                            jobCodeId: jc.jobCodeId,
                            jobCode: jc.jobCode,
                            displayName: jc.displayName,
                            forecastHours: currentHours,
                            date: jc.date,
                            forecastPayrollCost: currentCost,
                            forecastPayrollRevenue: currentCost
                        };
                    }) || [];

                    const totalHours = preservedJobCodes.reduce((sum, jc) => sum + (jc.forecastHours || 0), 0);
                    const totalCost = preservedJobCodes.reduce((sum, jc) => sum + (jc.forecastPayrollCost || 0), 0);
                    const totalRevenue = preservedJobCodes.reduce((sum, jc) => sum + (jc.forecastPayrollRevenue || 0), 0);

                    return {
                        id: undefined,
                        jobGroupId: scheduledGroup.jobGroupId,
                        jobGroupName: scheduledGroup.jobGroupName,
                        forecastHours: totalHours,
                        date: scheduledGroup.date,
                        jobCodes: preservedJobCodes,
                        forecastPayrollCost: totalCost,
                        forecastPayrollRevenue: totalRevenue
                    };
                }

                // Check if this job group contains only salaried positions
                const hasOnlySalariedJobs = scheduledGroup.jobCodes?.every(jc =>
                    isSalariedJobCode(jc.jobCodeId)
                ) ?? false;

                if (hasOnlySalariedJobs) {
                    // Preserve current values for salaried job groups
                    const dateKey = formatDateKey(new Date(scheduledGroup.date!));
                    const salariedValues = preserveSalariedValues(scheduledGroup, dateKey);

                    const preservedJobCodes = scheduledGroup.jobCodes?.map(jc =>
                        jc.jobCodeId ? salariedValues.get(jc.jobCodeId)! : undefined
                    ).filter((jc): jc is JobCodeForecastDto => jc !== undefined) || [];

                    const totalHours = preservedJobCodes.reduce((sum, jc) => sum + (jc.forecastHours || 0), 0);
                    const totalCost = preservedJobCodes.reduce((sum, jc) => sum + (jc.forecastPayrollCost || 0), 0);
                    const totalRevenue = preservedJobCodes.reduce((sum, jc) => sum + (jc.forecastPayrollRevenue || 0), 0);

                    return {
                        id: undefined,
                        jobGroupId: scheduledGroup.jobGroupId,
                        jobGroupName: scheduledGroup.jobGroupName,
                        forecastHours: totalHours,
                        date: scheduledGroup.date,
                        jobCodes: preservedJobCodes,
                        forecastPayrollCost: totalCost,
                        forecastPayrollRevenue: totalRevenue
                    };
                }

                // Process mixed or hourly job groups - copy scheduled values for hourly, preserve values for salaried
                const dateKey = formatDateKey(new Date(scheduledGroup.date!));
                const salariedValues = preserveSalariedValues(scheduledGroup, dateKey);

                const updatedJobCodes = scheduledGroup.jobCodes?.map(jc => {
                    if (jc.jobCodeId && isSalariedJobCode(jc.jobCodeId)) {
                        // Use preserved salaried value
                        return salariedValues.get(jc.jobCodeId)!;
                    } else {
                        // Copy scheduled values for hourly positions
                        return {
                            id: undefined,
                            jobCodeId: jc.jobCodeId,
                            jobCode: jc.jobCode,
                            displayName: jc.displayName,
                            forecastHours: jc.scheduledHours,
                            date: jc.date,
                            forecastPayrollCost: jc.scheduledPayrollCost,
                            forecastPayrollRevenue: jc.scheduledPayrollRevenue
                        };
                    }
                }).filter((jc): jc is JobCodeForecastDto => jc !== undefined) || []; // Remove any undefined entries

                // Calculate group totals
                const newGroupHours = updatedJobCodes.reduce((sum, jc) => sum + jc.forecastHours, 0);
                const newGroupCost = updatedJobCodes.reduce((sum, jc) => sum + (jc.forecastPayrollCost || 0), 0);
                const newGroupRevenue = updatedJobCodes.reduce((sum, jc) => sum + (jc.forecastPayrollRevenue || 0), 0);

                return {
                    id: undefined,
                    jobGroupId: scheduledGroup.jobGroupId,
                    jobGroupName: scheduledGroup.jobGroupName,
                    forecastHours: newGroupHours,
                    date: scheduledGroup.date,
                    jobCodes: updatedJobCodes,
                    forecastPayrollCost: newGroupCost,
                    forecastPayrollRevenue: newGroupRevenue
                };
            });

            // Count copied items
            const jobGroupsCopied = newForecastData.filter(group => group.forecastHours > 0).length;
            const jobCodesCopied = newForecastData.reduce((sum, group) =>
                sum + (group.jobCodes?.filter(jc => jc.forecastHours > 0).length || 0), 0);

            // Count preserved salaried positions (they now have forecast data)
            const excludedSalaries = newForecastData.reduce((sum, group) => {
                return sum + (group.jobCodes?.filter(jc => {
                    // Check if this job code is salaried
                    const jobCodeDetails = getJobCodeDetails(jc.jobCodeId!);
                    return Boolean(jobCodeDetails?.allocatedSalaryCost);
                }).length || 0);
            }, 0);

            return {
                jobGroupsCopied,
                jobCodesCopied,
                excludedSalaries,
                newForecastData
            };
        }

        const stats = {
            jobGroupsCopied: 0,
            jobCodesCopied: 0,
            excludedSalaries: 0
        };

        // Helper function to check if a date is in the past (excludes current day)
        const isPastDate = (dateString?: string): boolean => {
            if (!dateString) return false;
            const today = new Date();
            today.setHours(0, 0, 0, 0); // Reset time to start of day
            const date = new Date(dateString);
            date.setHours(0, 0, 0, 0); // Reset time to start of day
            return date < today; // Current day (date === today) is allowed
        };

        // Create new forecast array based on scheduled data
        const newForecastData: JobGroupForecastDto[] = payrollData.forecastPayroll.map(forecastGroup => {
            // Skip past dates - don't modify past day data
            if (isPastDate(forecastGroup.date)) {
                return forecastGroup; // Keep original forecast data for past dates
            }

            // Find matching scheduled group by both jobGroupId and date
            const scheduledGroup = payrollData.scheduledPayroll?.find(sg =>
                sg.jobGroupId === forecastGroup.jobGroupId && sg.date === forecastGroup.date
            );

            if (!scheduledGroup) {
                // No scheduled data for this forecast group - preserve existing forecast values
                return forecastGroup; // Keep original forecast data completely unchanged
            }

            // Check if this job group contains only salaried positions
            const hasOnlySalariedJobs = scheduledGroup.jobCodes?.every(jc =>
                isSalariedJobCode(jc.jobCodeId)
            ) ?? false;

            if (hasOnlySalariedJobs) {
                // Count preserved salaried job codes
                const salariedJobCodeCount = scheduledGroup.jobCodes?.length ?? 0;
                stats.excludedSalaries += salariedJobCodeCount;
                // Process the group to preserve salaried values
                const dateKey = formatDateKey(new Date(scheduledGroup.date!));
                const salariedValues = preserveSalariedValues(scheduledGroup, dateKey);

                const preservedJobCodes = scheduledGroup.jobCodes?.map(jc =>
                    jc.jobCodeId ? salariedValues.get(jc.jobCodeId)! : undefined
                ).filter((jc): jc is JobCodeForecastDto => jc !== undefined) || [];

                const totalHours = preservedJobCodes.reduce((sum, jc) => sum + (jc.forecastHours || 0), 0);
                const totalCost = preservedJobCodes.reduce((sum, jc) => sum + (jc.forecastPayrollCost || 0), 0);
                const totalRevenue = preservedJobCodes.reduce((sum, jc) => sum + (jc.forecastPayrollRevenue || 0), 0);

                return {
                    ...forecastGroup,
                    jobCodes: preservedJobCodes,
                    forecastHours: totalHours,
                    forecastPayrollCost: totalCost,
                    forecastPayrollRevenue: totalRevenue
                };
            }

            // Process mixed or hourly job groups
            let groupHasHourlyJobs = false;
            let updatedJobCodes: JobCodeForecastDto[] = [];

            if (scheduledGroup.jobCodes && forecastGroup.jobCodes) {
                updatedJobCodes = forecastGroup.jobCodes.map(forecastJobCode => {
                    const scheduledJobCode = scheduledGroup.jobCodes?.find(sjc =>
                        sjc.jobCodeId === forecastJobCode.jobCodeId && sjc.date === forecastJobCode.date
                    );

                    if (!scheduledJobCode) {
                        return forecastJobCode;
                    }

                    // Check if this individual job code is salaried
                    if (isSalariedJobCode(scheduledJobCode.jobCodeId)) {
                        stats.excludedSalaries++;
                        // Preserve current displayed value for salaried job codes
                        const dateKey = formatDateKey(new Date(scheduledJobCode.date!));
                        const currentHours = getCurrentDisplayedValue(dateKey, scheduledJobCode.jobCode!);
                        const currentCost = calculateJobCodeCost(scheduledJobCode.jobCode!, currentHours);

                        return {
                            ...forecastJobCode,
                            forecastHours: currentHours,
                            forecastPayrollCost: currentCost,
                            forecastPayrollRevenue: currentCost
                        };
                    }

                    // This is an hourly job code - copy the scheduled values
                    groupHasHourlyJobs = true;
                    stats.jobCodesCopied++;
                    return {
                        ...forecastJobCode,
                        forecastHours: scheduledJobCode.scheduledHours,
                        forecastPayrollCost: scheduledJobCode.scheduledPayrollCost,
                        forecastPayrollRevenue: scheduledJobCode.scheduledPayrollRevenue
                    };
                });
            }

            // If group has hourly jobs, update group-level totals and count it
            if (groupHasHourlyJobs) {
                stats.jobGroupsCopied++;

                // Calculate new group totals from job codes (for mixed groups)
                const newGroupHours = updatedJobCodes.reduce((sum, jc) => sum + jc.forecastHours, 0);
                const newGroupCost = updatedJobCodes.reduce((sum, jc) => sum + (jc.forecastPayrollCost || 0), 0);
                const newGroupRevenue = updatedJobCodes.reduce((sum, jc) => sum + (jc.forecastPayrollRevenue || 0), 0);

                return {
                    ...forecastGroup,
                    forecastHours: newGroupHours,
                    forecastPayrollCost: newGroupCost,
                    forecastPayrollRevenue: newGroupRevenue,
                    jobCodes: updatedJobCodes
                };
            }

            return forecastGroup; // No changes for this group
        });

        return { ...stats, newForecastData };
    }, [payrollData, getJobCodeDetails]);

    const handleCopyScheduleToForecast = useCallback(() => {
        if (isPastPeriod) {
            toast({
                title: "Cannot Copy",
                description: "Copy operation is not available for past periods",
                variant: "destructive"
            });
            return;
        }

        if (!hasScheduledData()) {
            toast({
                title: "No Data Available",
                description: "No scheduled data available to copy",
                variant: "destructive"
            });
            return;
        }

        setShowCopyConfirmation(true);
    }, [hasScheduledData, isPastPeriod, toast]);

    const handleConfirmCopy = useCallback(() => {
        try {
            // Always create fresh snapshot before each copy operation
            // This captures the current state including budget fallbacks and manual edits
            // Use forecastJobGroups instead of payrollData.forecastPayroll to capture fallback data
            // Also capture whether this was a fallback case at the time of snapshot
            const wasFallback = !payrollData?.forecastPayroll || payrollData.forecastPayroll.length === 0;
            setUndoSnapshot(forecastJobGroups.length > 0 ? {
                data: [...forecastJobGroups],
                wasFallback: wasFallback
            } : null);

            // Perform copy operation
            const result = performCopyOperation();

            // Update main payroll data state
            setPayrollData(prev => prev ? {
                ...prev,
                forecastPayroll: result.newForecastData
            } : null);

            // Update forecast payroll details to trigger UI refresh
            const updatedForecastDetails = transformJobGroupToPayrollDetails(result.newForecastData, 'forecastHours');



            setForecastPayrollDetails(updatedForecastDetails);

            setCopyStats(result);
            setCanUndo(true);
            setShowCopyConfirmation(false);

            // The copy operation works correctly - it only modifies current and future dates
            // Past dates are intentionally excluded from copy operations per business rules

            // Success notification
            toast({
                title: "Copy Successful",
                description: `Successfully copied ${result.jobGroupsCopied} job groups and ${result.jobCodesCopied} job codes to forecast`,
                duration: 5000
            });

            if (result.excludedSalaries > 0) {
                toast({
                    title: "Salaried Positions Preserved",
                    description: `${result.excludedSalaries} salaried positions preserved their current values`,
                    duration: 4000
                });
            }

            // Mark as having unsaved changes
            setHasUnsavedChanges(true);

        } catch (error) {
            console.error('Copy operation failed:', error);
            const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';

            // Provide specific error messages based on the error type
            let userMessage = "Failed to copy schedule data. Please try again.";
            if (errorMessage.includes('Scheduled data not available')) {
                userMessage = "No scheduled data available to copy. Please ensure scheduled data exists for this period.";
            } else if (errorMessage.includes('Required data not available')) {
                userMessage = "Required data is missing. Please refresh the page and try again.";
            }

            toast({
                title: "Copy Failed",
                description: userMessage,
                variant: "destructive"
            });
        }
    }, [performCopyOperation, payrollData, forecastJobGroups, toast, setHasUnsavedChanges, transformJobGroupToPayrollDetails]);

    useEffect(() => {
        if (!selectedSite || !startingMonth) return;

        // Clear expansion states when site changes to prevent job groups from previous site persisting
        setExpandedJobGroups(new Set());
        setExpandedDays(new Set());

        // Reset copy/undo state when data context changes (different site or time period)
        setCanUndo(false);
        setUndoSnapshot(null);
        setCopyStats(null);

        // Reset visible week index to first week when site or starting month changes
        setVisibleWeekIndex(0);

        fetchPayrollData();
        fetchContractDetails(selectedSite);
        fetchJobCodes(selectedSite);
    }, [selectedSite, startingMonth]);

    useEffect(() => {
        updateDatesFromPeriod(startingMonth);
    }, [startingMonth]);

    useEffect(() => {
        if (selectedDates.length === 0) return;

        const sortedDates = [...selectedDates].sort((a, b) => a.getTime() - b.getTime());
        const groupedWeeks: { start: Date, end: Date, dates: Date[] }[] = [];

        let currentWeekStart = new Date(sortedDates[0]);
        currentWeekStart.setDate(currentWeekStart.getDate() - currentWeekStart.getDay());

        let currentWeekDates: Date[] = [];

        sortedDates.forEach(date => {
            const weekStart = new Date(date);
            weekStart.setDate(weekStart.getDate() - weekStart.getDay());

            if (weekStart.getTime() !== currentWeekStart.getTime()) {
                const weekEnd = new Date(currentWeekStart);
                weekEnd.setDate(weekEnd.getDate() + 6);

                groupedWeeks.push({
                    start: new Date(currentWeekStart),
                    end: weekEnd,
                    dates: currentWeekDates
                });

                currentWeekStart = weekStart;
                currentWeekDates = [date];
            } else {
                currentWeekDates.push(date);
            }
        });

        if (currentWeekDates.length > 0) {
            const weekEnd = new Date(currentWeekStart);
            weekEnd.setDate(weekEnd.getDate() + 6);

            groupedWeeks.push({
                start: currentWeekStart,
                end: weekEnd,
                dates: currentWeekDates
            });
        }

        setWeeks(groupedWeeks);
    }, [selectedDates]);

    useEffect(() => {
        const newScheduledData: Record<string, Record<string, number>> = {};
        const newActualData: Record<string, Record<string, number>> = {};
        const newForecastData: Record<string, Record<string, number>> = {};
        const newBudgetData: Record<string, Record<string, number>> = {};

        const uniqueJobCodes = new Set<string>();
        const uniqueDateKeys = new Set<string>();

        jobRoles.forEach((job: JobRole) => uniqueJobCodes.add(job.jobCode));

        selectedDates.forEach(date => {
            uniqueDateKeys.add(formatDateKey(date));
        });

        const processPayrollDetails = (details: PayrollDetailDto[], target: Record<string, Record<string, number>>) => {
            details.forEach(detail => {
                if (detail.jobCode) {
                    uniqueJobCodes.add(detail.jobCode);
                }
                if (detail.date) {
                    uniqueDateKeys.add(detail.date);
                }
            });
        };

        processPayrollDetails(forecastPayrollDetails, newForecastData);
        processPayrollDetails(scheduledPayrollDetails, newScheduledData);
        processPayrollDetails(actualPayrollDetails, newActualData);
        processPayrollDetails(budgetPayrollDetails, newBudgetData);

        uniqueDateKeys.forEach(dateKey => {
            newScheduledData[dateKey] = {};
            newActualData[dateKey] = {};
            newForecastData[dateKey] = {};
            newBudgetData[dateKey] = {};
        });

        uniqueDateKeys.forEach(dateKey => {
            uniqueJobCodes.forEach(jobCode => {
                if (!newScheduledData[dateKey]) newScheduledData[dateKey] = {};
                if (!newActualData[dateKey]) newActualData[dateKey] = {};
                if (!newForecastData[dateKey]) newForecastData[dateKey] = {};
                if (!newBudgetData[dateKey]) newBudgetData[dateKey] = {};

                newScheduledData[dateKey][jobCode] = 0;
                newActualData[dateKey][jobCode] = 0;
                newForecastData[dateKey][jobCode] = 0;
                newBudgetData[dateKey][jobCode] = 0;
            });
        });

        scheduledPayrollDetails.forEach(detail => {
            if (!detail.date || !detail.jobCode) return;

            const dateKey = detail.date;
            const jobCode = detail.jobCode;

            if (!newScheduledData[dateKey]) newScheduledData[dateKey] = {};
            newScheduledData[dateKey][jobCode] = detail.regularHours;
        });

        actualPayrollDetails.forEach(detail => {
            if (!detail.date || !detail.jobCode) return;

            const dateKey = detail.date;
            const jobCode = detail.jobCode;

            if (!newActualData[dateKey]) newActualData[dateKey] = {};
            newActualData[dateKey][jobCode] = detail.regularHours;
        });

        forecastPayrollDetails.forEach(detail => {
            if (!detail.date || !detail.jobCode) {
                return;
            }

            const dateKey = detail.date;
            const jobCode = detail.jobCode;

            if (!newForecastData[dateKey]) newForecastData[dateKey] = {};
            newForecastData[dateKey][jobCode] = detail.regularHours;


        });



        budgetPayrollDetails.forEach(detail => {
            if (!detail.date || !detail.jobCode) return;

            const dateKey = detail.date;
            const jobCode = detail.jobCode;

            if (!newBudgetData[dateKey]) newBudgetData[dateKey] = {};
            newBudgetData[dateKey][jobCode] = detail.regularHours;
        });

        setScheduledData(newScheduledData);
        setActualData(newActualData);
        setForecastData(newForecastData);
        setBudgetData(newBudgetData);
    }, [forecastPayrollDetails, selectedDates, scheduledPayrollDetails, actualPayrollDetails, budgetPayrollDetails, jobCodes]);

    // Process job group level data directly from API response
    useEffect(() => {
        const newScheduledGroupData: Record<string, Record<string, number>> = {};
        const newActualGroupData: Record<string, Record<string, number>> = {};

        if (payrollData) {
            // Process scheduled payroll job groups
            if (payrollData.scheduledPayroll) {
                payrollData.scheduledPayroll.forEach((jobGroup: any) => {
                    if (jobGroup.date && jobGroup.jobGroupId && typeof jobGroup.scheduledHours === 'number') {
                        const dateKey = jobGroup.date;
                        const groupId = jobGroup.jobGroupId;

                        if (!newScheduledGroupData[dateKey]) {
                            newScheduledGroupData[dateKey] = {};
                        }
                        newScheduledGroupData[dateKey][groupId] = jobGroup.scheduledHours;
                    }
                });
            }

            // Process actual payroll job groups
            if (payrollData.actualPayroll) {
                payrollData.actualPayroll.forEach((jobGroup: any) => {
                    if (jobGroup.date && jobGroup.jobGroupId && typeof jobGroup.actualHours === 'number') {
                        const dateKey = jobGroup.date;
                        const groupId = jobGroup.jobGroupId;

                        if (!newActualGroupData[dateKey]) {
                            newActualGroupData[dateKey] = {};
                        }
                        newActualGroupData[dateKey][groupId] = jobGroup.actualHours;
                    }
                });
            }
        }

        setScheduledGroupData(newScheduledGroupData);
        setActualGroupData(newActualGroupData);
    }, [payrollData]);

    const fetchPayrollData = async () => {
        setIsLoadingPayroll(true);

        try {
            const response = await fetch(`/api/payroll/${selectedSite}/${startingMonth}`);

            if (!response.ok) {
                const emptyPayrollData: PayrollDto = {
                    customerSiteId: selectedSite,
                    siteNumber: customers.find(c => c.customerSiteId === selectedSite)?.siteNumber,
                    name: customers.find(c => c.customerSiteId === selectedSite)?.siteName,
                    billingPeriod: startingMonth,
                    forecastPayroll: [],
                    budgetPayroll: [],
                    actualPayroll: [],
                    scheduledPayroll: []
                };
                setPayrollData(emptyPayrollData);
                setForecastPayrollDetails([]);
                setBudgetPayrollDetails([]);
                setActualPayrollDetails([]);
                setScheduledPayrollDetails([]);

                if (response.status !== 404) {
                    console.error(`Error fetching payroll data: ${response.status}`);
                }

                setIsLoadingPayroll(false);
                return;
            }

            const data: PayrollDto = await response.json();
            setPayrollData(data);

            // Handle new JobGroupForecastDto structure
            setForecastJobGroups(data.forecastPayroll || []);

            // Convert JobGroupForecastDto to PayrollDetailDto for backward compatibility using helper function
            const flattenedForecastDetails = transformJobGroupToPayrollDetails(data.forecastPayroll, 'forecastHours');
            const flattenedBudgetDetails = transformJobGroupToPayrollDetails(data.budgetPayroll, 'budgetHours');
            setBudgetPayrollDetails(flattenedBudgetDetails);

            // If forecastPayroll is missing or empty, initialize from budgetPayroll
            if (flattenedForecastDetails.length === 0) {
                if (flattenedBudgetDetails.length > 0) {
                    // Copy budget details as forecast details
                    setForecastPayrollDetails(
                        flattenedBudgetDetails.map(detail => ({
                            ...detail,
                            regularHours: detail.regularHours
                        }))
                    );
                } else if (jobCodes.length > 0) {
                    // Fallback: initialize all job codes with zero, grouped by job group
                    const zeroedForecastDetails: PayrollDetailDto[] = jobCodes.map(jc => ({
                        id: jc.jobCodeId,
                        date: undefined,
                        jobCode: jc.jobCode,
                        displayName: jc.jobTitle,
                        regularHours: 0
                    }));
                    setForecastPayrollDetails(zeroedForecastDetails);
                } else {
                    setForecastPayrollDetails([]);
                }
            } else {
                setForecastPayrollDetails(flattenedForecastDetails);
            }

            // Flatten all remaining data types using helper function
            const flattenedActualDetails = transformJobGroupToPayrollDetails(data.actualPayroll, 'actualHours');
            const flattenedScheduledDetails = transformJobGroupToPayrollDetails(data.scheduledPayroll, 'scheduledHours');
            setActualPayrollDetails(flattenedActualDetails);
            setScheduledPayrollDetails(flattenedScheduledDetails);
        } catch (err) {
            console.error('Failed to fetch payroll data:', err);
            toast({
                title: "Error",
                description: "Failed to load payroll data. Please try again later.",
                variant: "destructive"
            });
            const emptyPayrollData: PayrollDto = {
                customerSiteId: selectedSite,
                siteNumber: customers.find(c => c.customerSiteId === selectedSite)?.siteNumber,
                name: customers.find(c => c.customerSiteId === selectedSite)?.siteName,
                billingPeriod: startingMonth,
                forecastPayroll: [],
                budgetPayroll: [],
                actualPayroll: [],
                scheduledPayroll: []
            };
            setPayrollData(emptyPayrollData);
            setForecastPayrollDetails([]);
            setBudgetPayrollDetails([]);
            setActualPayrollDetails([]);
            setScheduledPayrollDetails([]);
        } finally {
            setIsLoadingPayroll(false);
        }
    };

    // Always return the full set of job groups/jobs, merging in forecast values if present
    // Fallback: forecastPayroll > budgetPayroll > jobCodes (zero)
    const getJobGroupsWithFallback = (): JobGroupForecastDto[] => {
        if (!selectedDates || selectedDates.length === 0) return [];

        if (forecastPayrollDetails.length > 0) {
            // Build a lookup for forecast values: key = `${date}|${jobCode}`
            const forecastDetailMap = new Map<string, PayrollDetailDto>();
            forecastPayrollDetails.forEach(detail => {
                if (detail.date && detail.jobCode) {
                    forecastDetailMap.set(`${detail.date}|${detail.jobCode}`, detail);
                }
            });

            // Use jobCodes as the authoritative source for all jobs/groups
            const groupedByJobGroup: Record<string, JobCode[]> = {};
            jobCodes.forEach(jobCode => {
                const jobGroupId = jobCode.jobGroupId || 'unknown-group';
                if (!groupedByJobGroup[jobGroupId]) {
                    groupedByJobGroup[jobGroupId] = [];
                }
                groupedByJobGroup[jobGroupId].push(jobCode);
            });

            // For each group, create a JobGroupForecastDto for each date
            const result: JobGroupForecastDto[] = [];
            for (const dateObj of selectedDates) {
                const dateKey = formatDateKey(dateObj);
                for (const [jobGroupId, jobCodesInGroup] of Object.entries(groupedByJobGroup)) {
                    const firstJobCode = jobCodesInGroup[0];
                    const jobGroupName = firstJobCode?.jobGroupName || 'Unknown Group';
                    const jobCodes = jobCodesInGroup.map(jc => {
                        const forecast = forecastDetailMap.get(`${dateKey}|${jc.jobCode}`);
                        const hours = forecast?.regularHours ?? 0;
                        const cost = calculateJobCodeCost(jc.jobCode, hours);
                        return {
                            id: forecast?.id ?? undefined,
                            jobCodeId: jc.jobCodeId,
                            jobCode: jc.jobCode,
                            displayName: getJobTitleFallback(jc),
                            forecastHours: hours,
                            date: dateKey,
                            forecastPayrollCost: cost,
                            forecastPayrollRevenue: 0
                        };
                    });

                    // Sort job codes alphabetically by display name
                    const sortedJobCodes = jobCodes.sort((a, b) =>
                        (a.displayName || a.jobCode || "").localeCompare(b.displayName || b.jobCode || "")
                    );

                    const totalHours = sortedJobCodes.reduce((sum, jc) => sum + (jc.forecastHours || 0), 0);
                    const totalCost = sortedJobCodes.reduce((sum, jc) => sum + (jc.forecastPayrollCost || 0), 0);
                    const totalRevenue = sortedJobCodes.reduce((sum, jc) => sum + (jc.forecastPayrollRevenue || 0), 0);
                    result.push({
                        id: undefined,
                        jobGroupId,
                        jobGroupName,
                        forecastHours: totalHours,
                        date: dateKey,
                        jobCodes: sortedJobCodes,
                        forecastPayrollCost: totalCost,
                        forecastPayrollRevenue: totalRevenue
                    });
                }
            }
            return result;
        }

        // 2. If budgetPayrollDetails exist, use them as forecast
        if (budgetPayrollDetails.length > 0) {
            // Build a lookup for budget values: key = `${date}|${jobCode}`
            const budgetDetailMap = new Map<string, PayrollDetailDto>();
            budgetPayrollDetails.forEach(detail => {
                if (detail.date && detail.jobCode) {
                    budgetDetailMap.set(`${detail.date}|${detail.jobCode}`, detail);
                }
            });

            const groupedByJobGroup: Record<string, JobCode[]> = {};
            jobCodes.forEach(jobCode => {
                const jobGroupId = jobCode.jobGroupId || 'unknown-group';
                if (!groupedByJobGroup[jobGroupId]) {
                    groupedByJobGroup[jobGroupId] = [];
                }
                groupedByJobGroup[jobGroupId].push(jobCode);
            });

            const result: JobGroupForecastDto[] = [];
            for (const dateObj of selectedDates) {
                const dateKey = formatDateKey(dateObj);
                for (const [jobGroupId, jobCodesInGroup] of Object.entries(groupedByJobGroup)) {
                    const firstJobCode = jobCodesInGroup[0];
                    const jobGroupName = firstJobCode?.jobGroupName || 'Unknown Group';
                    const jobCodes = jobCodesInGroup.map(jc => {
                        const budget = budgetDetailMap.get(`${dateKey}|${jc.jobCode}`);
                        const hours = budget?.regularHours ?? 0;
                        const cost = calculateJobCodeCost(jc.jobCode, hours);
                        return {
                            id: budget?.id ?? undefined,
                            jobCodeId: jc.jobCodeId,
                            jobCode: jc.jobCode,
                            displayName: getJobTitleFallback(jc),
                            forecastHours: hours,
                            date: dateKey,
                            forecastPayrollCost: cost,
                            forecastPayrollRevenue: 0
                        };
                    });

                    // Sort job codes alphabetically by display name
                    const sortedJobCodes = jobCodes.sort((a, b) =>
                        (a.displayName || a.jobCode || "").localeCompare(b.displayName || b.jobCode || "")
                    );

                    const totalHours = sortedJobCodes.reduce((sum, jc) => sum + (jc.forecastHours || 0), 0);
                    const totalCost = sortedJobCodes.reduce((sum, jc) => sum + (jc.forecastPayrollCost || 0), 0);
                    const totalRevenue = sortedJobCodes.reduce((sum, jc) => sum + (jc.forecastPayrollRevenue || 0), 0);
                    result.push({
                        id: undefined,
                        jobGroupId,
                        jobGroupName,
                        forecastHours: totalHours,
                        date: dateKey,
                        jobCodes: sortedJobCodes,
                        forecastPayrollCost: totalCost,
                        forecastPayrollRevenue: totalRevenue
                    });
                }
            }
            return result;
        }

        // 3. Fallback: use job codes by site, all zeros
        const groupedByJobGroup: Record<string, JobCode[]> = {};
        jobCodes.forEach(jobCode => {
            const jobGroupId = jobCode.jobGroupId || 'unknown-group';
            if (!groupedByJobGroup[jobGroupId]) {
                groupedByJobGroup[jobGroupId] = [];
            }
            groupedByJobGroup[jobGroupId].push(jobCode);
        });

        const result: JobGroupForecastDto[] = [];
        for (const dateObj of selectedDates) {
            const dateKey = formatDateKey(dateObj);
            for (const [jobGroupId, jobCodesInGroup] of Object.entries(groupedByJobGroup)) {
                const firstJobCode = jobCodesInGroup[0];
                const jobGroupName = firstJobCode?.jobGroupName || 'Unknown Group';
                const jobCodes = jobCodesInGroup.map(jc => {
                    return {
                        id: undefined,
                        jobCodeId: jc.jobCodeId,
                        jobCode: jc.jobCode,
                        displayName: getJobTitleFallback(jc),
                        forecastHours: 0,
                        date: dateKey,
                        forecastPayrollCost: 0,
                        forecastPayrollRevenue: 0
                    };
                });

                // Sort job codes alphabetically by display name
                const sortedJobCodes = jobCodes.sort((a, b) =>
                    (a.displayName || a.jobCode || "").localeCompare(b.displayName || b.jobCode || "")
                );

                result.push({
                    id: undefined,
                    jobGroupId,
                    jobGroupName,
                    forecastHours: 0,
                    date: dateKey,
                    jobCodes: sortedJobCodes,
                    forecastPayrollCost: 0,
                    forecastPayrollRevenue: 0
                });
            }
        }
        return result;
    };

    const shouldShowHRISWarning = (): boolean => {
        if (isLoadingPayroll || isLoadingContract || isLoadingJobCodes) {
            return false;
        }

        const fallbackJobGroups = getJobGroupsWithFallback();
        const fallbackFailed = fallbackJobGroups.length === 0;

        return fallbackFailed && Boolean(selectedSite);
    };

    // Create job groups from budget data
    // Use jobGroupId and jobGroupName directly from budgetPayroll (API response)
    const createJobGroupsFromBudgetData = (): JobGroupForecastDto[] => {
        // Flatten all job group objects from budgetPayrollDetails
        const groupMap: Record<string, JobGroupForecastDto> = {};

        // budgetPayrollDetails here is actually a flat array of job codes, but the API gives us jobGroupId and jobGroupName per group
        // Instead, we should use the original budgetPayroll array from payrollData if available
        if (payrollData?.budgetPayroll && payrollData.budgetPayroll.length > 0) {
            return payrollData.budgetPayroll.map(group => {
                const jobCodes = (group.jobCodes || []).map(jobCode => ({
                    id: jobCode.id,
                    jobCodeId: jobCode.jobCodeId,
                    jobCode: jobCode.jobCode,
                    displayName: jobCode.displayName,
                    forecastHours: jobCode.budgetHours,
                    date: jobCode.date,
                    forecastPayrollCost: jobCode.budgetPayrollCost,
                    forecastPayrollRevenue: jobCode.budgetPayrollRevenue
                }));

                // Sort job codes alphabetically by display name
                const sortedJobCodes = jobCodes.sort((a, b) =>
                    (a.displayName || a.jobCode || "").localeCompare(b.displayName || b.jobCode || "")
                );

                return {
                    id: group.id,
                    jobGroupId: group.jobGroupId,
                    jobGroupName: group.jobGroupName,
                    forecastHours: group.budgetHours,
                    date: group.date,
                    jobCodes: sortedJobCodes,
                    forecastPayrollCost: group.budgetPayrollCost,
                    forecastPayrollRevenue: group.budgetPayrollRevenue
                };
            });
        }

        // fallback: group by jobGroupId from budgetPayrollDetails (legacy)
        budgetPayrollDetails.forEach(budgetItem => {
            if (!budgetItem.jobCode || !budgetItem.date) return;
            const jobCodeInfo = jobCodes.find(jc => jc.jobCode === budgetItem.jobCode);
            const jobGroupId = jobCodeInfo?.jobGroupId || 'unknown-group';
            const jobGroupName = jobCodeInfo?.jobGroupName || 'Unknown Group';
            if (!groupMap[jobGroupId]) {
                groupMap[jobGroupId] = {
                    id: undefined,
                    jobGroupId,
                    jobGroupName,
                    forecastHours: 0,
                    date: budgetItem.date,
                    jobCodes: [],
                    forecastPayrollCost: 0,
                    forecastPayrollRevenue: 0
                };
            }
            const group = groupMap[jobGroupId];
            if (group && Array.isArray(group.jobCodes) && typeof group.forecastHours === "number" && typeof group.forecastPayrollCost === "number") {
                group.jobCodes.push({
                    id: budgetItem.id,
                    jobCodeId: jobCodeInfo?.jobCodeId || '',
                    jobCode: budgetItem.jobCode,
                    displayName: budgetItem.displayName || jobCodeInfo?.jobTitle || budgetItem.jobCode || "Unknown Job Title/Job Code",
                    forecastHours: budgetItem.regularHours,
                    date: budgetItem.date,
                    forecastPayrollCost: budgetItem.regularHours * (jobCodeInfo?.averageHourlyRate || 15),
                    forecastPayrollRevenue: 0
                });
                group.forecastHours += budgetItem.regularHours;
                const cost = calculateJobCodeCost(budgetItem.jobCode, budgetItem.regularHours);
                group.jobCodes.push({
                    id: budgetItem.id,
                    jobCodeId: jobCodeInfo?.jobCodeId || '',
                    jobCode: budgetItem.jobCode,
                    displayName: budgetItem.displayName || jobCodeInfo?.jobTitle || budgetItem.jobCode || "Unknown Job Title/Job Code",
                    forecastHours: budgetItem.regularHours,
                    date: budgetItem.date,
                    forecastPayrollCost: cost,
                    forecastPayrollRevenue: 0
                });
                group.forecastHours += budgetItem.regularHours;
                group.forecastPayrollCost += cost;
            }
        });

        // Sort job codes within each group before returning
        const sortedGroups = Object.values(groupMap).map(group => ({
            ...group,
            jobCodes: (group.jobCodes || []).sort((a, b) =>
                (a.displayName || a.jobCode || "").localeCompare(b.displayName || b.jobCode || "")
            )
        }));

        return sortedGroups;
    };

    // Create job groups from job codes, initialized to zero
    const createJobGroupsFromJobCodes = (): JobGroupForecastDto[] => {
        const groupedByJobGroup: Record<string, JobCode[]> = {};

        jobCodes.forEach(jobCode => {
            const jobGroupId = jobCode.jobGroupId || 'unknown-group';
            if (!groupedByJobGroup[jobGroupId]) {
                groupedByJobGroup[jobGroupId] = [];
            }
            groupedByJobGroup[jobGroupId].push(jobCode);
        });

        return Object.entries(groupedByJobGroup).map(([jobGroupId, jobCodesInGroup]) => {
            const firstJobCode = jobCodesInGroup[0];

            const jobCodes = jobCodesInGroup.map(jc => ({
                id: undefined,
                jobCodeId: jc.jobCodeId,
                jobCode: jc.jobCode,
                displayName: jc.jobTitle,
                forecastHours: 0,
                date: undefined,
                forecastPayrollCost: 0,
                forecastPayrollRevenue: 0
            }));

            // Sort job codes alphabetically by display name
            const sortedJobCodes = jobCodes.sort((a, b) =>
                (a.displayName || a.jobCode || "").localeCompare(b.displayName || b.jobCode || "")
            );

            return {
                id: undefined,
                jobGroupId: jobGroupId,
                jobGroupName: firstJobCode?.jobGroupName || 'Unknown Group',
                forecastHours: 0,
                date: undefined,
                jobCodes: sortedJobCodes,
                forecastPayrollCost: 0,
                forecastPayrollRevenue: 0
            };
        });
    };

    const updateDatesFromPeriod = (period: string) => {
        const [year, month] = period.split("-").map((num) => Number.parseInt(num));
        const monthDates = [];
        const firstDay = new Date(year, month - 1, 1);
        const lastDay = new Date(year, month, 0);

        for (let day = new Date(firstDay); day <= lastDay; day.setDate(day.getDate() + 1)) {
            monthDates.push(new Date(day));
        }

        setSelectedDates(monthDates);

        const today = new Date();
        const periodDate = new Date(year, month - 1);
        // Fix: Compare with first day of current month to allow current month editing
        setIsPastPeriod(periodDate < new Date(today.getFullYear(), today.getMonth(), 1));
    };

    const formatDateKey = (date: Date) => {
        return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
    };

    const toggleGuide = () => {
        setIsGuideOpen(!isGuideOpen);
    };

    const handleSavePayroll = async () => {
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

            // Build full matrix: every date, every job group, every job code for that group
            const allDates = selectedDates.map(d => formatDateKey(d));
            
            // Filter out unknown-group job codes and track them for notification
            const unknownGroupJobCodes = jobCodes.filter(jc => !jc.jobGroupId || jc.jobGroupId === 'unknown-group');
            const validJobCodes = jobCodes.filter(jc => jc.jobGroupId && jc.jobGroupId !== 'unknown-group');
            
            // Group valid job codes by jobGroupId
            const groupedJobCodes: Record<string, JobCode[]> = {};
            validJobCodes.forEach(jc => {
                const groupId = jc.jobGroupId!;
                if (!groupedJobCodes[groupId]) groupedJobCodes[groupId] = [];
                groupedJobCodes[groupId].push(jc);
            });

            const jobGroups = Object.entries(groupedJobCodes).map(([jobGroupId, codes]) => {
                const jobGroupName = codes[0]?.jobGroupName || 'Unknown Group';
                return { jobGroupId, jobGroupName, jobCodes: codes };
            });

            // Build a lookup for existing forecast details
            const forecastDetailMap = new Map<string, PayrollDetailDto>();
            forecastPayrollDetails.forEach(detail => {
                // Support both jobCode and jobCodeString for robustness
                forecastDetailMap.set(`${detail.date}|${detail.jobCode}`, detail);
                if ((detail as any).jobCodeString) {
                    forecastDetailMap.set(`${detail.date}|${(detail as any).jobCodeString}`, detail);
                }
            });

            const aggregatedForecastPayroll: JobGroupForecastDto[] = [];

            for (const date of allDates) {
                for (const group of jobGroups) {
                    const jobCodesForGroup = group.jobCodes.map(jc => {
                        const codeKey = jc.jobCode || jc.jobCodeString;
                        const key = `${date}|${codeKey}`;
                        const detail = forecastDetailMap.get(key);
                        const hours = detail?.regularHours ?? 0;
                        const cost = calculateJobCodeCost(codeKey, hours);
                        return {
                            id: detail?.id,
                            jobCodeId: jc.jobCodeId,
                            jobCode: codeKey,
                            displayName: jc.jobTitle || jc.jobCode || detail?.displayName || codeKey || "Unknown Job Title/Job Code",
                            forecastHours: hours,
                            date: date,
                            forecastPayrollCost: cost,
                            forecastPayrollRevenue: 0 // You can enhance this if revenue logic is needed
                        };
                    });

                    const totalHours = jobCodesForGroup.reduce((sum, jc) => sum + (jc.forecastHours || 0), 0);
                    const totalCost = jobCodesForGroup.reduce((sum, jc) => sum + (jc.forecastPayrollCost || 0), 0);
                    const totalRevenue = jobCodesForGroup.reduce((sum, jc) => sum + (jc.forecastPayrollRevenue || 0), 0);

                    aggregatedForecastPayroll.push({
                        id: undefined,
                        jobGroupId: group.jobGroupId,
                        jobGroupName: group.jobGroupName,
                        forecastHours: totalHours,
                        date: date,
                        jobCodes: jobCodesForGroup,
                        forecastPayrollCost: totalCost,
                        forecastPayrollRevenue: totalRevenue
                    });
                }
            }

            const payload: PayrollDto = {
                id: payrollData?.id,
                customerSiteId: selectedSite,
                siteNumber: customer?.siteNumber,
                name: customer?.siteName,
                billingPeriod: startingMonth,
                payrollForecastMode: payrollData?.payrollForecastMode || "Code",
                forecastPayroll: aggregatedForecastPayroll,
                budgetPayroll: payrollData?.budgetPayroll || [],
                actualPayroll: payrollData?.actualPayroll || [],
                scheduledPayroll: payrollData?.scheduledPayroll || []
            };

            const response = await fetch(`/api/payroll`, {
                method: 'PATCH',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(payload),
            });

            if (!response.ok) {
                throw new Error(`Error saving payroll data: ${response.status}`);
            }

            const savedData = await response.json();

            setPayrollData(savedData);

            // Transform all data types using the helper function to eliminate duplication
            const flattenedForecastDetails = transformJobGroupToPayrollDetails(savedData.forecastPayroll, 'forecastHours');
            const flattenedBudgetDetails = transformJobGroupToPayrollDetails(savedData.budgetPayroll, 'budgetHours');
            const flattenedActualDetails = transformJobGroupToPayrollDetails(savedData.actualPayroll, 'actualHours');
            const flattenedScheduledDetails = transformJobGroupToPayrollDetails(savedData.scheduledPayroll, 'scheduledHours');

            setForecastPayrollDetails(flattenedForecastDetails);
            setBudgetPayrollDetails(flattenedBudgetDetails);
            setActualPayrollDetails(flattenedActualDetails);
            setScheduledPayrollDetails(flattenedScheduledDetails);

            toast({
                title: "Success",
                description: "Payroll data saved successfully."
            });

            // Show notification for filtered out job codes after a delay to avoid overlapping with success toast
            if (unknownGroupJobCodes.length > 0) {
                const filteredJobCodesList = unknownGroupJobCodes.map(jc => jc.jobCode).join(', ');
                setTimeout(() => {
                    toast({
                        title: "Payroll Expense - Job Codes Filtered Out",
                        description: `${unknownGroupJobCodes.length} job code(s) were not saved because they don't have a job group assigned: ${filteredJobCodesList}`,
                        variant: "destructive",
                        duration: 8000
                    });
                }, 2000);
            }

            setHasUnsavedChanges(false);

        } catch (err) {
            console.error('Failed to save payroll data:', err);
            toast({
                title: "Error",
                description: "Failed to save payroll data. Please try again.",
                variant: "destructive"
            });
        } finally {
            setIsSaving(false);
        }
    };

    const handleHoursChange = (date: Date, jobCode: string, value: string) => {
        const numValue = value === "" ? 0 : parseFloat(value);
        const dateKey = formatDateKey(date);

        updateForecastValue(dateKey, jobCode, numValue);
        setHasUnsavedChanges(true);
    };

    const getHoursForDateAndJob = (date: Date, jobCode: string): number => {
        const dateKey = formatDateKey(date);
        const detail = forecastPayrollDetails.find(
            detail => detail.date === dateKey && detail.jobCode === jobCode
        );

        return detail?.regularHours || 0;
    };

    const getTotalHoursForDate = (date: Date): number => {
        const dateKey = formatDateKey(date);
        return forecastPayrollDetails
            .filter(detail => detail.date === dateKey)
            .reduce((sum, detail) => sum + detail.regularHours, 0);
    };

    const getTotalHoursForJob = (jobCode: string): number => {
        return forecastPayrollDetails
            .filter(detail => detail.jobCode === jobCode)
            .reduce((sum, detail) => sum + detail.regularHours, 0);
    };

    const getTotalHours = (): number => {
        return forecastPayrollDetails.reduce((sum, detail) => sum + detail.regularHours, 0);
    };

    const isReadOnly = (date: Date): boolean => {
        if (isPastPeriod) return true;

        const today = new Date();
        const currentYear = today.getFullYear();
        const currentMonth = today.getMonth();
        const currentDay = today.getDate();

        if (date.getFullYear() < currentYear) return true;
        if (date.getFullYear() === currentYear && date.getMonth() < currentMonth) return true;
        if (date.getFullYear() === currentYear && date.getMonth() === currentMonth && date.getDate() < currentDay) return true;

        return false;
    };

    const handlePreviousWeek = () => {
        if (visibleWeekIndex > 0) {
            setVisibleWeekIndex(visibleWeekIndex - 1);
        }
    };

    const handleNextWeek = () => {
        if (visibleWeekIndex < weeks.length - 1) {
            setVisibleWeekIndex(visibleWeekIndex + 1);
        }
    };

    const toggleDateExpand = (dateKey: string) => {
        setExpandedDates(prev => ({
            ...prev,
            [dateKey]: !prev[dateKey]
        }));
    };

    const getTotalForDate = (dateKey: string, dataSource: Record<string, Record<string, number>>): number => {
        if (!dataSource[dateKey]) return 0;

        return Object.values(dataSource[dateKey]).reduce((sum, hours) => sum + hours, 0);
    };

    // Get totals from job group level data (for accurate display in Scheduled & Actual Data section)
    const getGroupTotalForDate = (dateKey: string, groupDataSource: Record<string, Record<string, number>>): number => {
        if (!groupDataSource[dateKey]) return 0;

        return Object.values(groupDataSource[dateKey]).reduce((sum, hours) => sum + hours, 0);
    };

    const getVariancePercentage = (dateKey: string, jobId?: string): number => {
        if (!actualData[dateKey] || !scheduledData[dateKey]) return 0;

        if (jobId) {
            const scheduled = scheduledData[dateKey][jobId] || 0;
            const actual = actualData[dateKey][jobId] || 0;

            if (scheduled === 0) return 0;
            return ((actual - scheduled) / scheduled) * 100;
        } else {
            // Use job group level data for variance calculation in Scheduled & Actual Data section
            const scheduledTotal = getGroupTotalForDate(dateKey, scheduledGroupData);
            const actualTotal = getGroupTotalForDate(dateKey, actualGroupData);

            if (scheduledTotal === 0) return 0;
            return ((actualTotal - scheduledTotal) / scheduledTotal) * 100;
        }
    };

    // Compute monthly global maximum for scaling bars
    const getMonthlyGlobalMax = (): number => {
        let maxVal = 0;
        selectedDates.forEach(date => {
            const dateKey = formatDateKey(date);
            const forecastTotal = getTotalForDate(dateKey, forecastData);
            const scheduledTotal = getGroupTotalForDate(dateKey, scheduledGroupData);
            const actualTotal = getGroupTotalForDate(dateKey, actualGroupData);
            const budgetTotal = getTotalForDate(dateKey, budgetData);
            maxVal = Math.max(maxVal, forecastTotal, scheduledTotal, actualTotal, budgetTotal);
        });
        return maxVal;
    };

    // Use monthly global max for bar width scaling
    const getBarWidth = (value: number): number => {
        const maxVal = getMonthlyGlobalMax();
        return maxVal > 0 ? Math.min(100, (value / maxVal) * 100) : 0;
    };

    const getMaxValueForDate = (dateKey: string): number => {
        const forecastTotal = getTotalForDate(dateKey, forecastData);
        // Use job group level data for max value calculation in Scheduled & Actual Data section
        const scheduledTotal = getGroupTotalForDate(dateKey, scheduledGroupData);
        const actualTotal = getGroupTotalForDate(dateKey, actualGroupData);
        const budgetTotal = getTotalForDate(dateKey, budgetData);

        return Math.max(forecastTotal, scheduledTotal, actualTotal, budgetTotal);
    };

    const formatDateDisplay = (date: Date): string => {
        return date.toLocaleDateString("en-US", { weekday: 'long', month: 'short', day: 'numeric' });
    };

    const formatDateRange = (start: Date, end: Date): string => {
        return `${start.toLocaleDateString("en-US", { month: 'short', day: 'numeric' })} - ${end.toLocaleDateString("en-US", { month: 'short', day: 'numeric' })}`;
    };

    const hasActualData = (dateKey: string): boolean => {
        // Check job group level data for actual hours
        return Object.values(actualGroupData[dateKey] || {}).some(hours => hours > 0);
    };

    const isFutureDate = (date: Date): boolean => {
        const today = new Date();
        today.setHours(0, 0, 0, 0); // Reset time to start of day
        const checkDate = new Date(date);
        checkDate.setHours(0, 0, 0, 0); // Reset time to start of day
        return checkDate >= today; // Include current day as "future" (editable)
    };

    /**
     * Returns the list of job codes actually present in scheduledPayrollDetails and actualPayrollDetails,
     * using the endpoint data, not the static jobRoles.
     */
    const getFilteredJobs = () => {
        const jobCodeSet = new Set<string>();
        scheduledPayrollDetails.forEach(detail => {
            if (detail.jobCode) jobCodeSet.add(detail.jobCode);
        });
        actualPayrollDetails.forEach(detail => {
            if (detail.jobCode) jobCodeSet.add(detail.jobCode);
        });

        const jobsFromEndpoint = Array.from(jobCodeSet).map(jobCode => {
            const detail = scheduledPayrollDetails.find(d => d.jobCode === jobCode)
                || actualPayrollDetails.find(d => d.jobCode === jobCode);

            return {
                displayName: detail?.displayName || jobCode || "Unknown Job Title/Job Code",
                jobCode,
                hourlyRate: getJobCodeHourlyRate(jobCode)
            };
        });

        if (selectedJob === "all") {
            return jobsFromEndpoint;
        }
        return jobsFromEndpoint.filter((job) => job.jobCode === selectedJob);
    };

    const findDisplayNameForJobCode = (jobCode: string): string | undefined => {
        const predefinedRole = jobRoles.find((role: JobRole) => role.jobCode === jobCode);
        if (predefinedRole) return predefinedRole.displayName;

        const detailWithJobCode = [
            ...forecastPayrollDetails,
            ...budgetPayrollDetails,
            ...actualPayrollDetails
        ].find(detail => detail.jobCode === jobCode);

        return detailWithJobCode?.displayName;
    };

    const getJobTitleFallback = (jobCode: JobCode): string => {
        return jobCode.jobTitle || jobCode.jobCode || "Unknown Job Title/Job Code";
    };

    const currentWeek = weeks[visibleWeekIndex] || { start: new Date(), end: new Date(), dates: [] };

    const getDisplayValue = (hours: number, jobId?: string): string => {
        if (viewMode === "hours") {
            return `${hours.toFixed(1)} hrs`;
        } else {
            const cost = jobId ? calculateJobCodeCost(jobId, hours) : hours * 15; // fallback for unknown job codes
            return formatCurrency(cost);
        }
    };

    // Always show both hours and cost for the Scheduled & Actual Data panel job rows
    const getHoursAndCostString = (hours: number, jobId?: string): string => {
        const cost = jobId ? calculateJobCodeCost(jobId, hours) : 0; // fallback for unknown job codes
        return `${hours.toFixed(1)} hrs (${formatCurrency(cost)})`;
    };

    const [userModifiedData, setUserModifiedData] = useState(false);

    useEffect(() => {
        if (userModifiedData) {
            setHasUnsavedChanges(true);
            setUserModifiedData(false);
        }
    }, [userModifiedData, setHasUnsavedChanges]);

    // Recalculate aggregated job group totals for a given date and affected job code
    // This maintains a synthetic group-level record (keyed by jobGroupName) alongside individual job-code records
    function recalculateJobGroupAggregationsForUpdate(
        currentDetails: PayrollDetailDto[],
        dateKey: string,
        updatedJobId: string
    ): PayrollDetailDto[] {
        const matchingJob = jobCodes.find(jc =>
            jc.jobCode === updatedJobId ||
            jc.jobCodeString === updatedJobId ||
            jc.name === updatedJobId ||
            jc.id === updatedJobId
        );

        const jobGroupName = matchingJob?.jobGroupName;
        if (!jobGroupName) {
            return currentDetails;
        }

        // Sum hours across all job codes in this group for the specified date
        const groupJobCodes = jobCodes.filter(jc => jc.jobGroupName === jobGroupName);
        const groupTotalHours = groupJobCodes.reduce((total, jc) => {
            const codeCandidates = [jc.jobCode, jc.jobCodeString, jc.name, jc.id].filter(Boolean) as string[];
            const existing = currentDetails.find(d => d.date === dateKey && codeCandidates.includes(d.jobCode || ""));
            return total + (existing?.regularHours || 0);
        }, 0);

        const existingGroupIndex = currentDetails.findIndex(d => d.date === dateKey && d.jobCode === jobGroupName);

        // If there are no hours, remove any existing synthetic group record to avoid stale zeros
        if (groupTotalHours <= 0) {
            if (existingGroupIndex >= 0) {
                const trimmed = [...currentDetails];
                trimmed.splice(existingGroupIndex, 1);
                return trimmed;
            }
            return currentDetails;
        }

        if (existingGroupIndex >= 0) {
            const updated = [...currentDetails];
            updated[existingGroupIndex] = {
                ...updated[existingGroupIndex],
                regularHours: groupTotalHours
            };
            return updated;
        }

        return [
            ...currentDetails,
            {
                id: undefined,
                date: dateKey,
                jobCode: jobGroupName,
                displayName: jobGroupName,
                regularHours: groupTotalHours
            }
        ];
    }

    const updateForecastValue = (dateKey: string, jobId: string, hours: number) => {
        setForecastData(prev => {
            // Avoid unnecessary state updates if value is unchanged
            if (prev[dateKey]?.[jobId] === hours) return prev;
            return {
                ...prev,
                [dateKey]: {
                    ...(prev[dateKey] || {}),
                    [jobId]: hours
                }
            };
        });
        setUserModifiedData(true);

        setForecastPayrollDetails(prevDetails => {
            const idx = prevDetails.findIndex(
                d => d.date === dateKey && d.jobCode === jobId
            );

            let intermediate: PayrollDetailDto[] = prevDetails;

            if (hours > 0) {
                if (idx >= 0) {
                    // Avoid updating if the hours haven't changed
                    if (prevDetails[idx].regularHours !== hours) {
                        const updated = [...prevDetails];
                        updated[idx] = { ...updated[idx], regularHours: hours };
                        intermediate = updated;
                    }
                } else {
                    const jobRole = jobRoles.find((role: JobRole) => role.jobCode === jobId);
                    intermediate = [
                        ...prevDetails,
                        {
                            id: undefined,
                            date: dateKey,
                            jobCode: jobId,
                            displayName: jobRole?.displayName || jobId || "Unknown Job Title/Job Code",
                            regularHours: hours
                        }
                    ];
                }
            } else {
                if (idx >= 0) {
                    const updated = [...prevDetails];
                    updated.splice(idx, 1);
                    intermediate = updated;
                }
            }

            // Recalculate and upsert the aggregated group record for the affected group
            const withAggregations = recalculateJobGroupAggregationsForUpdate(intermediate, dateKey, jobId);
            return withAggregations;
        });
    };

    const openEditDialog = (dateKey: string, jobCode?: string) => {
        // Parse date in local timezone to avoid off-by-one day issue
        const [year, month, day] = dateKey.split('-').map(Number);
        const date = new Date(year, month - 1, day);
        const contractType = determineContractType(payrollData!, contractDetails);

        // For Standard sites, we need to calculate aggregated job group totals
        // For Per Labor Hour sites, we use individual job code data

        let scheduledHours = 0;
        let forecastHours = 0;
        let budgetHours = 0;
        let budgetCost = 0;
        let hourlyRate = 0;
        let jobName = undefined;

        if (contractType === "Standard") {
            // Standard sites: Use aggregated job group totals
            // Find the job group for this job code
            const jobGroupData = getJobGroupsWithFallback().find(jg =>
                jg.jobCodes?.some((jc: JobCodeForecastDto) => jc.jobCode === jobCode)
            );

            if (jobGroupData) {
                // Calculate aggregated totals for the job group
                const jobCodesInGroup = jobCodes.filter(jc => jc.jobGroupId === jobGroupData.jobGroupId);

                // Sum up hours for all job codes in the group
                scheduledHours = jobCodesInGroup.reduce((total, jc) => {
                    return total + (scheduledData[dateKey]?.[jc.jobCode] || 0);
                }, 0);

                forecastHours = jobCodesInGroup.reduce((total, jc) => {
                    return total + (forecastData[dateKey]?.[jc.jobCode] || 0);
                }, 0);

                budgetHours = jobCodesInGroup.reduce((total, jc) => {
                    return total + (budgetData[dateKey]?.[jc.jobCode] || 0);
                }, 0);

                // Calculate weighted average hourly rate for the group
                const totalWeightedRate = jobCodesInGroup.reduce((total, jc) => {
                    const hours = forecastData[dateKey]?.[jc.jobCode] || 0;
                    return total + ((jc.averageHourlyRate || 0) * hours);
                }, 0);

                hourlyRate = forecastHours > 0 ? totalWeightedRate / forecastHours : 0;

                // Calculate budget cost using the group's average rate
                budgetCost = budgetHours * hourlyRate;

                // Use job group name
                jobName = jobGroupData.jobGroupName || "Unknown Group";
            }
        } else {
            // Per Labor Hour sites: Use individual job code data
            scheduledHours = jobCode ? scheduledData[dateKey]?.[jobCode] || 0 : 0;
            forecastHours = jobCode ? forecastData[dateKey]?.[jobCode] || 0 : 0;

            const job = jobCode ? jobRoles.find((j: JobRole) => j.jobCode === jobCode) : undefined;
            hourlyRate = job?.hourlyRate || getJobCodeHourlyRate(jobCode || '');

            budgetHours = jobCode ? budgetData[dateKey]?.[jobCode] || 0 : 0;
            budgetCost = calculateJobCodeCost(jobCode || '', budgetHours);

            jobName = jobCode ? findDisplayNameForJobCode(jobCode) || jobCode || "Unknown Job Title/Job Code" : undefined;
        }

        // Determine editing level and context based on contract type
        let editLevel: "JobGroup" | "JobTitle" = "JobTitle";
        let jobGroupContext = undefined;
        let jobTitleContext = undefined;

        if (contractType === "Standard") {
            // Standard sites: Job Group level only
            editLevel = "JobGroup";

            // Find the job group for this job code
            const jobGroupData = getJobGroupsWithFallback().find(jg =>
                jg.jobCodes?.some((jc: JobCodeForecastDto) => jc.jobCode === jobCode)
            );

            if (jobGroupData) {
                // Get all job codes in this group with their details
                const jobCodesInGroup = jobCodes.filter(jc => jc.jobGroupId === jobGroupData.jobGroupId);

                jobGroupContext = {
                    id: jobGroupData.jobGroupId,
                    name: jobGroupData.jobGroupName || "Unknown Group",
                    averageHourlyRate: calculateGroupAverageRate(jobCodesInGroup),
                    jobCodes: jobCodesInGroup.map(jc => ({
                        jobCodeId: jc.jobCodeId,
                        jobCode: jc.jobCode,
                        displayName: getJobTitleFallback(jc),
                        activeEmployeeCount: jc.activeEmployeeCount || 0,
                        averageHourlyRate: jc.averageHourlyRate
                    }))
                };
            }
        } else if (contractType === "PerLaborHour" && jobCode) {
            // Per Labor Hour sites: Job Title level
            editLevel = "JobTitle";
            const jobCodeDetails = getJobCodeDetails(jobCode);
            const billableRates = getBillableRates(jobCode, contractDetails);

            jobTitleContext = {
                jobCodeId: jobCode,
                jobCode: jobCode,
                displayName: findDisplayNameForJobCode(jobCode) || jobCode || "Unknown Job Title/Job Code",
                hourlyRate: jobCodeDetails?.averageHourlyRate || hourlyRate,
                billableRates
            };
        }

        setCurrentEditData({
            date: dateKey,
            dateObj: date,
            displayDate: formatDateDisplay(date),
            scheduledHours,
            scheduledCost: contractType === "Standard" ? scheduledHours * hourlyRate : (jobCode ? calculateJobCodeCost(jobCode, scheduledHours) : 0),
            forecastHours,
            jobCode,
            jobName,
            hourlyRate,
            budgetHours,
            budgetCost,
            contractType,
            editLevel,
            jobGroup: jobGroupContext,
            jobTitle: jobTitleContext
        });

        // Build available dates list (all visible dates in the current view)
        const allDates: Date[] = [];
        weeks.forEach(week => {
            week.dates.forEach(date => {
                // Only include future dates
                if (date >= new Date(new Date().setHours(0, 0, 0, 0))) {
                    allDates.push(date);
                }
            });
        });
        setAvailableDates(allDates);

        // Find current date index
        const dateIndex = allDates.findIndex(d => formatDateKey(d) === dateKey);
        setCurrentDateIndex(dateIndex);

        // Build available jobs list based on contract type
        const jobsList: Array<{ id: string; name: string; type: 'jobGroup' | 'jobCode' }> = [];

        if (contractType === "Standard") {
            // For Standard sites, navigate between job groups
            const jobGroups = getJobGroupsWithFallback();
            jobGroups.forEach(jg => {
                jobsList.push({
                    id: jg.jobGroupId,
                    name: jg.jobGroupName || "Unknown Group",
                    type: 'jobGroup'
                });
            });
        } else if (contractType === "PerLaborHour") {
            // For PLH sites, navigate between job codes
            // Sort job codes alphabetically by display name to match timeline order
            const sortedJobCodes = [...jobCodes].sort((a, b) => {
                const aDisplayName = getJobTitleFallback(a);
                const bDisplayName = getJobTitleFallback(b);
                return aDisplayName.localeCompare(bDisplayName);
            });

            sortedJobCodes.forEach(jc => {
                jobsList.push({
                    id: jc.jobCodeId,
                    name: `${getJobTitleFallback(jc)} (${jc.jobCode})`,
                    type: 'jobCode'
                });
            });
        }

        setAvailableJobs(jobsList);

        // Find current job index
        let jobIndex = 0;
        if (contractType === "Standard" && jobGroupContext) {
            jobIndex = jobsList.findIndex(j => j.id === jobGroupContext.id);
        } else if (contractType === "PerLaborHour" && jobCode) {
            const jobCodeDetails = jobCodes.find(jc => jc.jobCode === jobCode);
            if (jobCodeDetails) {
                jobIndex = jobsList.findIndex(j => j.id === jobCodeDetails.jobCodeId);
            }
        }
        setCurrentJobIndex(Math.max(0, jobIndex));

        setIsEditDialogOpen(true);
    };

    /**
     * Calculates the weighted average hourly rate for a job group,
     * weighted by active employee count. Accepts any object with averageHourlyRate and activeEmployeeCount.
     * Falls back to simple average if no counts.
     * @param jobCodesInGroup Array of objects with averageHourlyRate and activeEmployeeCount
     */
    const calculateGroupAverageRate = (
        jobCodesInGroup: Array<{ averageHourlyRate?: number; activeEmployeeCount?: number; allocatedSalaryCost?: number }>
    ): number => {
        let totalWeighted = 0;
        let totalEmployees = 0;
        let fallbackRates: number[] = [];

        jobCodesInGroup.forEach(jc => {
            let rate = jc.averageHourlyRate;

            // If no averageHourlyRate, try to calculate from allocatedSalaryCost
            if (!rate && jc.allocatedSalaryCost) {
                rate = jc.allocatedSalaryCost / 365; // This would be daily rate, but for average calculation we'll use it
            }

            const count = jc.activeEmployeeCount || 0;
            if (rate && rate > 0) {
                fallbackRates.push(rate);
                totalWeighted += rate * count;
                totalEmployees += count;
            }
        });

        if (totalEmployees > 0) {
            return totalWeighted / totalEmployees;
        }
        if (fallbackRates.length > 0) {
            return fallbackRates.reduce((sum, rate) => sum + rate, 0) / fallbackRates.length;
        }
        return 0; // No default rate
    };


    /**
     * Updates the edit dialog data without closing and reopening the dialog.
     * This preserves the dialog state while updating the content for navigation.
     */
    const updateEditDialogData = (dateKey: string, jobCode?: string) => {
        if (!isEditDialogOpen || !currentEditData) return;

        // Parse date in local timezone to avoid off-by-one day issue
        const [year, month, day] = dateKey.split('-').map(Number);
        const date = new Date(year, month - 1, day);
        const contractType = determineContractType(payrollData!, contractDetails);

        const scheduledHours = jobCode
            ? scheduledData[dateKey]?.[jobCode] || 0
            : getTotalForDate(dateKey, scheduledData);
        const forecastHours = jobCode
            ? forecastData[dateKey]?.[jobCode] || 0
            : getTotalForDate(dateKey, forecastData);

        const job = jobCode ? jobRoles.find((j: JobRole) => j.jobCode === jobCode) : undefined;
        const hourlyRate = job?.hourlyRate || getJobCodeHourlyRate(jobCode || '');

        let budgetHours = 0;
        let budgetCost = 0;
        if (jobCode) {
            budgetHours = budgetData[dateKey]?.[jobCode] || 0;
            budgetCost = calculateJobCodeCost(jobCode, budgetHours);
        } else {
            budgetHours = getTotalForDate(dateKey, budgetData);
            // For total budget cost calculation, we need to calculate each job code individually
            const jobCodesInBudget = Object.keys(budgetData[dateKey] || {});
            budgetCost = jobCodesInBudget.reduce((total, jc) => {
                const hours = budgetData[dateKey]?.[jc] || 0;
                return total + calculateJobCodeCost(jc, hours);
            }, 0);
        }

        // Determine editing level and context based on contract type
        let editLevel: "JobGroup" | "JobTitle" = "JobTitle";
        let jobGroupContext = undefined;
        let jobTitleContext = undefined;

        if (contractType === "Standard") {
            // Standard sites: Job Group level only
            editLevel = "JobGroup";

            // Find the job group for this job code
            const jobGroupData = getJobGroupsWithFallback().find(jg =>
                jg.jobCodes?.some((jc: JobCodeForecastDto) => jc.jobCode === jobCode)
            );

            if (jobGroupData) {
                // Get all job codes in this group with their details
                const jobCodesInGroup = jobCodes.filter(jc => jc.jobGroupId === jobGroupData.jobGroupId);

                jobGroupContext = {
                    id: jobGroupData.jobGroupId,
                    name: jobGroupData.jobGroupName || "Unknown Group",
                    averageHourlyRate: calculateGroupAverageRate(jobCodesInGroup),
                    jobCodes: jobCodesInGroup.map(jc => ({
                        jobCodeId: jc.jobCodeId,
                        jobCode: jc.jobCode,
                        displayName: getJobTitleFallback(jc),
                        activeEmployeeCount: jc.activeEmployeeCount || 0,
                        averageHourlyRate: jc.averageHourlyRate
                    }))
                };
            }
        } else if (contractType === "PerLaborHour" && jobCode) {
            // Per Labor Hour sites: Job Title level
            editLevel = "JobTitle";
            const jobCodeDetails = getJobCodeDetails(jobCode);
            const billableRates = getBillableRates(jobCode, contractDetails);

            jobTitleContext = {
                jobCodeId: jobCode,
                jobCode: jobCode,
                displayName: findDisplayNameForJobCode(jobCode) || jobCode || "Unknown Job Title/Job Code",
                hourlyRate: jobCodeDetails?.averageHourlyRate || hourlyRate,
                billableRates
            };
        }

        // Update the current edit data without resetting the entire object
        setCurrentEditData({
            ...currentEditData,
            date: dateKey,
            dateObj: date,
            displayDate: formatDateDisplay(date),
            scheduledHours,
            scheduledCost: jobCode ? calculateJobCodeCost(jobCode, scheduledHours) : 0,
            forecastHours,
            jobCode,
            jobName: jobCode ? findDisplayNameForJobCode(jobCode) || jobCode || "Unknown Job Title/Job Code" : undefined,
            hourlyRate,
            budgetHours,
            budgetCost,
            contractType,
            editLevel,
            jobGroup: jobGroupContext,
            jobTitle: jobTitleContext
        });

        // Update indices
        const newDateIndex = availableDates.findIndex(d => formatDateKey(d) === dateKey);
        if (newDateIndex !== -1) setCurrentDateIndex(newDateIndex);

        // Find the job index based on the contract type and current job
        let newJobIndex = currentJobIndex; // Default to current if not found
        if (contractType === "Standard" && jobGroupContext) {
            newJobIndex = availableJobs.findIndex(j => j.id === jobGroupContext.id);
        } else if (contractType === "PerLaborHour" && jobCode) {
            const jobCodeDetails = jobCodes.find(jc => jc.jobCode === jobCode);
            if (jobCodeDetails) {
                newJobIndex = availableJobs.findIndex(j => j.id === jobCodeDetails.jobCodeId);
            }
        }
        if (newJobIndex !== -1) setCurrentJobIndex(newJobIndex);
    };

    /**
     * Centralized payroll forecast processing function matching the flowchart logic.
     * Handles both Standard and Per Labor Hour contract types with explicit decision points.
     */
    const handleNavigateDate = (direction: 'prev' | 'next') => {
        const newIndex = direction === 'prev' ? currentDateIndex - 1 : currentDateIndex + 1;

        if (newIndex < 0 || newIndex >= availableDates.length) return;

        const newDate = availableDates[newIndex];
        const newDateKey = formatDateKey(newDate);
        const currentJob = availableJobs[currentJobIndex];

        if (isEditDialogOpen) {
            // Update existing dialog data
            if (currentJob.type === 'jobGroup') {
                const jobGroup = getJobGroupsWithFallback().find(jg => jg.jobGroupId === currentJob.id);
                const firstJobCode = jobGroup?.jobCodes?.[0]?.jobCode;
                updateEditDialogData(newDateKey, firstJobCode);
            } else {
                const jobCode = jobCodes.find(jc => jc.jobCodeId === currentJob.id)?.jobCode;
                updateEditDialogData(newDateKey, jobCode);
            }
        } else {
            // Open new dialog
            if (currentJob.type === 'jobGroup') {
                const jobGroup = getJobGroupsWithFallback().find(jg => jg.jobGroupId === currentJob.id);
                const firstJobCode = jobGroup?.jobCodes?.[0]?.jobCode;
                openEditDialog(newDateKey, firstJobCode);
            } else {
                const jobCode = jobCodes.find(jc => jc.jobCodeId === currentJob.id)?.jobCode;
                openEditDialog(newDateKey, jobCode);
            }
        }
    };

    const handleNavigateJob = (direction: 'prev' | 'next') => {
        const newIndex = direction === 'prev' ? currentJobIndex - 1 : currentJobIndex + 1;

        if (newIndex < 0 || newIndex >= availableJobs.length) return;

        const currentDateKey = currentEditData?.date || availableDates[currentDateIndex]?.toISOString().split('T')[0];
        const newJob = availableJobs[newIndex];

        if (isEditDialogOpen) {
            // Update existing dialog data
            if (newJob.type === 'jobGroup') {
                const jobGroup = getJobGroupsWithFallback().find(jg => jg.jobGroupId === newJob.id);
                const firstJobCode = jobGroup?.jobCodes?.[0]?.jobCode;
                updateEditDialogData(currentDateKey, firstJobCode);
            } else {
                const jobCode = jobCodes.find(jc => jc.jobCodeId === newJob.id)?.jobCode;
                updateEditDialogData(currentDateKey, jobCode);
            }
        } else {
            // Open new dialog
            if (newJob.type === 'jobGroup') {
                const jobGroup = getJobGroupsWithFallback().find(jg => jg.jobGroupId === newJob.id);
                const firstJobCode = jobGroup?.jobCodes?.[0]?.jobCode;
                openEditDialog(currentDateKey, firstJobCode);
            } else {
                const jobCode = jobCodes.find(jc => jc.jobCodeId === newJob.id)?.jobCode;
                openEditDialog(currentDateKey, jobCode);
            }
        }
    };

    const handleUnsavedChangesWarning = async (): Promise<boolean> => {
        if (isHandlingWarning) return Promise.resolve(false);
        setIsHandlingWarning(true);

        return new Promise<boolean>((resolve) => {
            const { dismiss } = toast({
                title: "Unsaved Changes",
                description: "You have unsaved changes. Do you want to save them before navigating?",
                duration: 60000, // Keep toast open longer
                action: (
                    <div className="flex gap-2">
                        <Button size="sm" onClick={() => {
                            dismiss();
                            resolve(true);
                        }}>
                            Apply & Continue
                        </Button>
                        <Button size="sm" variant="outline" onClick={() => {
                            dismiss();
                            resolve(false);
                        }}>
                            Discard
                        </Button>
                    </div>
                ),
            });
        }).finally(() => {
            // Add a small delay before resetting the flag to prevent race conditions
            setTimeout(() => {
                setIsHandlingWarning(false);
            }, 100);
        });
    };

    const handleSaveEdit = (updatedData: ForecastDayData) => {
        const contractType = determineContractType(payrollData!, contractDetails);

        // Decision: Is Per Labor Hour enabled?
        if (contractType === "Standard") {
            // Standard Site Path: Job Group Level Only
            if (updatedData.editLevel === "JobGroup" && updatedData.jobGroup) {
                handleStandardSiteUpdate(updatedData);
                setHasUnsavedChanges(true);
                return;
            }
            // Restrict Job Title edits for Standard sites
            // (No-op or show error if needed)
            return;
        }

        if (contractType === "PerLaborHour") {
            // Per Labor Hour Site Path: Job Title Level
            if (updatedData.editLevel === "JobTitle" && updatedData.jobCode && updatedData.jobTitle) {
                handlePerLaborHourSiteUpdate(updatedData);
                setHasUnsavedChanges(true);
                return;
            }
            // Restrict Job Group edits for PLH sites
            // (No-op or show error if needed)
            return;
        }

        // Fallback: legacy logic (should not be reached in normal flow)
        if (updatedData.jobCode) {
            updateForecastValue(updatedData.date, updatedData.jobCode, updatedData.forecastHours);
            setHasUnsavedChanges(true);
        }
    };

    /**
     * Standard Site Path: Distribute hours proportionally by ActiveEmployeeCount,
     * and calculate job group cost using weighted average rate.
     */
    const handleStandardSiteUpdate = (updatedData: ForecastDayData) => {
        if (!updatedData.jobGroup) return;

        const totalHours = updatedData.forecastHours;
        const jobCodes = updatedData.jobGroup.jobCodes;

        // Calculate total employee count
        const totalEmployeeCount = jobCodes.reduce((sum, jc) => sum + (jc.activeEmployeeCount || 0), 0);

        // Calculate weighted average hourly rate for the group
        const avgRate = calculateGroupAverageRate(jobCodes);
        const groupCost = totalHours * avgRate;

        // Distribute hours and update forecast for each job code
        if (totalEmployeeCount === 0) {
            // Equal distribution if no employee count data
            const hoursPerJobCode = totalHours / jobCodes.length;
            jobCodes.forEach(jc => {
                updateForecastValue(updatedData.date, jc.jobCode, hoursPerJobCode);
                updateJobCodeForecast(updatedData.date, jc.jobCode, hoursPerJobCode, hoursPerJobCode * avgRate, 0);
            });
        } else {
            // Proportional distribution based on ActiveEmployeeCount
            jobCodes.forEach(jc => {
                const proportion = (jc.activeEmployeeCount || 0) / totalEmployeeCount;
                const distributedHours = totalHours * proportion;
                updateForecastValue(updatedData.date, jc.jobCode, distributedHours);
                updateJobCodeForecast(updatedData.date, jc.jobCode, distributedHours, distributedHours * avgRate, 0);
            });
        }

        // Update the job group forecast data (hours and cost)
        updateJobGroupForecast(updatedData.date, updatedData.jobGroup.id, totalHours);
    };

    /**
     * Helper to get all dates in the same Sat-Sun week as the given date.
     * @param dateStr ISO date string
     * @returns Array of ISO date strings for the week
     */
    const getWeekDatesSatToSun = (dateStr: string): string[] => {
        const date = new Date(dateStr);
        // 6 = Saturday, 0 = Sunday
        const day = date.getDay();
        // Find previous Saturday
        const prevSat = new Date(date);
        prevSat.setDate(date.getDate() - ((day + 1) % 7));
        // Find next Sunday
        const nextSun = new Date(prevSat);
        nextSun.setDate(prevSat.getDate() + 6);
        const weekDates: string[] = [];
        for (let d = new Date(prevSat); d <= nextSun; d.setDate(d.getDate() + 1)) {
            weekDates.push(d.toISOString().slice(0, 10));
        }
        return weekDates;
    };

    /**
     * Calculates total forecasted hours for a job code in the same Sat-Sun week as the given date.
     * @param dateStr ISO date string
     * @param jobCode string
     * @returns number
     */
    const getWeeklyHoursForJobCode = (dateStr: string, jobCode: string): number => {
        const weekDates = getWeekDatesSatToSun(dateStr);
        let total = 0;
        weekDates.forEach(d => {
            if (forecastData[d] && forecastData[d][jobCode]) {
                total += forecastData[d][jobCode];
            }
        });
        return total;
    };

    // Per Labor Hour Site Path: Job Title Level with cost and revenue calculations
    const handlePerLaborHourSiteUpdate = (updatedData: ForecastDayData) => {
        if (!updatedData.jobCode || !updatedData.jobTitle) return;

        const hours = updatedData.forecastHours;
        const jobCode = updatedData.jobCode;
        const hourlyRate = updatedData.jobTitle.hourlyRate;

        // Calculate cost (always calculated)
        const cost = hours * hourlyRate;

        // Calculate revenue if billable rates exist
        let revenue = 0;
        if (updatedData.jobTitle.billableRates) {
            const { rate, overtimeRate } = updatedData.jobTitle.billableRates;

            // Calculate total hours for this job code in the Sat-Sun week
            const weekTotal = getWeeklyHoursForJobCode(updatedData.date, jobCode) - (forecastData[updatedData.date]?.[jobCode] || 0) + hours;

            // Overtime logic: up to 40 hours regular, rest overtime, for the week
            const regularHours = Math.min(weekTotal, 40, hours);
            const overtimeHours = Math.max(0, weekTotal - 40, hours - regularHours);

            // If this edit pushes the week over 40, split accordingly
            let reg = 0, ot = 0;
            if (weekTotal <= 40) {
                reg = hours;
                ot = 0;
            } else if (weekTotal - hours >= 40) {
                reg = 0;
                ot = hours;
            } else {
                reg = 40 - (weekTotal - hours);
                ot = hours - reg;
            }

            const regularRevenue = reg * rate;
            const overtimeRevenue = ot * overtimeRate;
            revenue = regularRevenue + overtimeRevenue;
        }

        // Update forecast value
        updateForecastValue(updatedData.date, jobCode, hours);

        // Update the job code forecast with cost and revenue
        updateJobCodeForecast(updatedData.date, jobCode, hours, cost, revenue);
    };

    // Helper function to update job group forecast data
    const updateJobGroupForecast = (date: string, jobGroupId: string, totalHours: number) => {
        setForecastJobGroups(prevGroups => {
            const idx = prevGroups.findIndex(group => group.jobGroupId === jobGroupId);
            if (idx === -1) return prevGroups;

            const group = prevGroups[idx];

            // Remove duplicate jobCodes for the same jobCode and date
            const uniqueJobCodesMap = new Map<string, JobCodeForecastDto>();
            (group.jobCodes || []).forEach((jc: JobCodeForecastDto) => {
                // Use jobCode + date as unique key
                const key = `${jc.jobCode}|${jc.date}`;
                // Only keep the first occurrence (or you could keep the last, depending on business logic)
                if (!uniqueJobCodesMap.has(key)) {
                    uniqueJobCodesMap.set(key, jc);
                }
            });
            let uniqueJobCodes = Array.from(uniqueJobCodesMap.values());

            // Now update the forecastHours for the matching date
            uniqueJobCodes = uniqueJobCodes.map((jc: JobCodeForecastDto) =>
                jc.date === date ? { ...jc, forecastHours: totalHours } : jc
            );

            const updatedGroup = {
                ...group,
                forecastHours: totalHours,
                date: date,
                jobCodes: uniqueJobCodes
            };

            const newGroups = [...prevGroups];
            newGroups[idx] = updatedGroup;
            return newGroups;
        });
    };

    /**
     * Updates job code forecast with cost and revenue, and aggregates revenue/cost to group level.
     * Ensures that for each jobCode and date, there is only one unique entry.
     * @param date string
     * @param jobCode string
     * @param hours number
     * @param cost number
     * @param revenue number
     */
    const updateJobCodeForecast = (date: string, jobCode: string, hours: number, cost: number, revenue: number) => {
        setForecastJobGroups(prevGroups => {
            return prevGroups.map(group => {
                let updatedJobCodes = group.jobCodes || [];

                // Remove any existing entry for this jobCode and date
                updatedJobCodes = updatedJobCodes.filter(jc => !(jc.jobCode === jobCode && jc.date === date));

                // Find a template for this jobCode to preserve required fields
                const template = updatedJobCodes.find(jc => jc.jobCode === jobCode);

                // Add the new/updated entry, ensuring jobCodeId is always a string
                updatedJobCodes.push({
                    ...(template || {}),
                    jobCode,
                    jobCodeId: template?.jobCodeId || "", // Ensure string, not undefined
                    date,
                    forecastHours: hours,
                    forecastPayrollCost: cost,
                    forecastPayrollRevenue: revenue
                });

                // Calculate group totals for this date
                const groupJobCodesForDate = updatedJobCodes.filter((jc: JobCodeForecastDto) => jc.date === date);

                const groupTotalHours = groupJobCodesForDate.reduce((sum: number, jc: JobCodeForecastDto) => sum + (jc.forecastHours || 0), 0);
                const groupTotalCost = groupJobCodesForDate.reduce((sum: number, jc: JobCodeForecastDto) => sum + (jc.forecastPayrollCost || 0), 0);
                const groupTotalRevenue = groupJobCodesForDate.reduce((sum: number, jc: JobCodeForecastDto) => sum + (jc.forecastPayrollRevenue || 0), 0);

                return {
                    ...group,
                    jobCodes: updatedJobCodes,
                    forecastHours: groupTotalHours,
                    forecastPayrollCost: groupTotalCost,
                    forecastPayrollRevenue: groupTotalRevenue,
                    date: date
                };
            });
        });
    };

const calculateJobCodeCost = (jobCodeString: string, hours: number): number => {
    if (hours === 0) {
        return 0;
    }

    const jobCodeInfo = jobCodes.find(jc => jc.jobCode === jobCodeString || jc.jobCodeString === jobCodeString);

    if (!jobCodeInfo) {
        return 0;
    }

    // If no averageHourlyRate but allocatedSalaryCost exists, use daily rate (not multiplied by hours)
    if (jobCodeInfo.allocatedSalaryCost && jobCodeInfo.allocatedSalaryCost > 0) {
        return jobCodeInfo.allocatedSalaryCost / 365;
    }

    // If averageHourlyRate exists, use it * hours
    if (jobCodeInfo.averageHourlyRate && jobCodeInfo.averageHourlyRate > 0) {
        return jobCodeInfo.averageHourlyRate * hours;
    }

    // Otherwise return 0
    return 0;
    };
    // Helper function to check if a date is in the past
    const isPastDateHelper = useCallback((dateString?: string): boolean => {
        if (!dateString) return false;
        const today = new Date();
        today.setHours(0, 0, 0, 0); // Reset time to start of day
        const date = new Date(dateString);
        date.setHours(0, 0, 0, 0); // Reset time to start of day
        return date < today; // Current day (date === today) is allowed
    }, []);

    // Intelligent undo that only reverts copied values while preserving manual edits
    const performIntelligentUndo = useCallback((): JobGroupForecastDto[] => {
        if (!undoSnapshot) {
            throw new Error('No undo data available');
        }

        // Use the stored fallback flag from the snapshot
        const wasFallback = undoSnapshot.wasFallback;

        // Take a "before undo" snapshot of current state (includes manual edits)
        // CRITICAL FIX: Use forecastJobGroups (local state with manual edits) instead of payrollData.forecastPayroll (parent props)
        const currentSnapshot = forecastJobGroups;

        // Create a map for quick lookup of original values (before copy)
        const originalValueMap = new Map<string, number>();
        undoSnapshot.data?.forEach(group => {
            if (!isPastDateHelper(group.date)) { // Only process current/future dates
                group.jobCodes?.forEach(jobCode => {
                    const key = `${group.date}-${jobCode.jobCodeId}`;
                    // Store original value (could be forecast, budget fallback, or 0)
                    originalValueMap.set(key, jobCode.forecastHours || 0);
                });
            }
        });

        // For fallback cases, we need to handle the restoration differently
        if (wasFallback) {
            // In fallback case, the undo snapshot contains the original fallback data
            // We need to restore the entire structure to the original state
            return undoSnapshot.data?.map(group => {
                // Skip past dates - preserve existing fallback values
                if (isPastDateHelper(group.date)) {
                    return group;
                }

                // For current/future dates, restore the original values
                const updatedJobCodes = group.jobCodes?.map(jobCode => ({
                    ...jobCode,
                    forecastPayrollCost: calculateJobCodeCost(jobCode.jobCode!, jobCode.forecastHours || 0),
                    forecastPayrollRevenue: calculateJobCodeCost(jobCode.jobCode!, jobCode.forecastHours || 0)
                }));

                const newGroupHours = updatedJobCodes?.reduce((sum, jc) => sum + (jc.forecastHours || 0), 0) || 0;
                const newGroupCost = updatedJobCodes?.reduce((sum, jc) => sum + (jc.forecastPayrollCost || 0), 0) || 0;
                const newGroupRevenue = updatedJobCodes?.reduce((sum, jc) => sum + (jc.forecastPayrollRevenue || 0), 0) || 0;

                return {
                    ...group,
                    jobCodes: updatedJobCodes,
                    forecastHours: newGroupHours,
                    forecastPayrollCost: newGroupCost,
                    forecastPayrollRevenue: newGroupRevenue
                };
            }) || [];
        }

        // Standard case: forecast data exists, use intelligent comparison
        if (!payrollData?.forecastPayroll) {
            throw new Error('No forecast data available for undo');
        }

        // Process current forecast data (before undo snapshot)
        const restoredForecastData = currentSnapshot.map(forecastGroup => {
            // Skip past dates entirely
            if (isPastDateHelper(forecastGroup.date)) {
                return forecastGroup;
            }

            // Find matching scheduled group
            const scheduledGroup = payrollData.scheduledPayroll?.find(sg =>
                sg.jobGroupId === forecastGroup.jobGroupId && sg.date === forecastGroup.date
            );

            const updatedJobCodes = forecastGroup.jobCodes?.map(forecastJobCode => {
                const scheduledJobCode = scheduledGroup?.jobCodes?.find(sjc =>
                    sjc.jobCodeId === forecastJobCode.jobCodeId
                );

                if (!scheduledJobCode) {
                    return forecastJobCode; // No scheduled data, keep as is
                }

                const currentForecast = forecastJobCode.forecastHours || 0;
                const scheduledValue = scheduledJobCode.scheduledHours || 0;
                const originalKey = `${forecastGroup.date}-${forecastJobCode.jobCodeId}`;
                const originalValue = originalValueMap.get(originalKey) || 0;

                // INTELLIGENT COMPARISON:
                // If current forecast exactly matches scheduled value → it was copied → revert to original
                if (currentForecast === scheduledValue) {
                    return {
                        ...forecastJobCode,
                        forecastHours: originalValue,
                        forecastPayrollCost: calculateJobCodeCost(forecastJobCode.jobCode!, originalValue),
                        forecastPayrollRevenue: calculateJobCodeCost(forecastJobCode.jobCode!, originalValue)
                    };
                }

                // If different from scheduled → it was manually edited → preserve current value
                return forecastJobCode;
            });

            // Recalculate group totals
            const newGroupHours = updatedJobCodes?.reduce((sum, jc) => sum + (jc.forecastHours || 0), 0) || 0;
            const newGroupCost = updatedJobCodes?.reduce((sum, jc) => sum + (jc.forecastPayrollCost || 0), 0) || 0;
            const newGroupRevenue = updatedJobCodes?.reduce((sum, jc) => sum + (jc.forecastPayrollRevenue || 0), 0) || 0;

            return {
                ...forecastGroup,
                jobCodes: updatedJobCodes,
                forecastHours: newGroupHours,
                forecastPayrollCost: newGroupCost,
                forecastPayrollRevenue: newGroupRevenue
            };
        });

        return restoredForecastData;
    }, [undoSnapshot, forecastJobGroups, payrollData, calculateJobCodeCost, isPastDateHelper]);
    const handleUndo = useCallback(() => {
        if (!undoSnapshot) {
            toast({
                title: "No Undo Available",
                description: "No undo data available",
                variant: "destructive"
            });
            return;
        }

        try {
            // FIXED: Use intelligent undo that preserves manual edits
            const restoredForecastData = performIntelligentUndo();

            // Restore main payroll data state with intelligent restoration
            setPayrollData(prev => prev ? {
                ...prev,
                forecastPayroll: restoredForecastData
            } : null);

            // Update forecast payroll details to trigger UI refresh
            const restoredForecastDetails = transformJobGroupToPayrollDetails(restoredForecastData, 'forecastHours');
            setForecastPayrollDetails(restoredForecastDetails);

            setCanUndo(false);
            setUndoSnapshot(null);
            setCopyStats(null);

            toast({
                title: "Undo Successful",
                description: "Copy operation undone successfully - manual edits preserved"
            });

            // Mark as having unsaved changes
            setHasUnsavedChanges(true);

        } catch (error) {
            toast({
                title: "Undo Failed",
                description: "Failed to undo copy operation. Please try again.",
                variant: "destructive"
            });
        }
    }, [undoSnapshot, performIntelligentUndo, toast, setHasUnsavedChanges, transformJobGroupToPayrollDetails]);

    // Helper function to calculate cost for multiple job codes (for group totals)
    const calculateGroupCost = (jobCodeStrings: string[], hoursMap: Record<string, number>): number => {
        return jobCodeStrings.reduce((total, jobCodeString) => {
            const hours = hoursMap[jobCodeString] || 0;
            return total + calculateJobCodeCost(jobCodeString, hours);
        }, 0);
    };

    // Helper function to get hourly rate for a job code (for display purposes)
    const getJobCodeHourlyRate = (jobCodeString: string): number => {
        const jobCodeInfo = jobCodes.find(jc => jc.jobCode === jobCodeString || jc.jobCodeString === jobCodeString);
        return jobCodeInfo?.averageHourlyRate || 0;
    };

    // Effect to initialize job groups with fallback logic when data becomes available
    // New logic: initialize forecastJobGroups ONLY when selectedSite or jobCodes change and forecastJobGroups is empty
    useEffect(() => {
        if (
            selectedSite &&
            jobCodes.length > 0 &&
            (!forecastJobGroups || forecastJobGroups.length === 0)
        ) {
            const fallbackJobGroups = getJobGroupsWithFallback();
            if (fallbackJobGroups.length > 0) {
                setForecastJobGroups(fallbackJobGroups);
            }
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [selectedSite, jobCodes, payrollData]); // Remove forecastJobGroups and budgetPayrollDetails from dependencies to avoid unnecessary re-initializations

    // Cleanup on component unmount for memory optimization
    useEffect(() => {
        return () => {
            setUndoSnapshot(null);
            setCopyStats(null);
            setCanUndo(false);
        };
    }, []);

    useImperativeHandle(ref, () => ({
        save: handleSavePayroll
    }));

    const updateIsPastPeriod = useCallback((year: number, month: number) => {
        const today = new Date();
        const periodDate = new Date(year, month - 1);
        // Fix: Compare with current date, not first day of current month
        // Allow current month to be editable
        setIsPastPeriod(periodDate < new Date(today.getFullYear(), today.getMonth(), 1));
    }, []);

    return (
        <div className="w-full p-1 space-y-6">
            <div className="flex justify-between items-start mb-4">
                <div>
                    <h1 className="text-2xl font-bold tracking-tight">Payroll Expense</h1>
                    <p className="text-muted-foreground">Manage payroll hours for your staff and teams.</p>
                    {error && <p className="text-red-500 mt-2">{error}</p>}
                </div>
            </div>

            {!shouldShowHRISWarning() && (
                <Button
                    variant="outline"
                    onClick={toggleGuide}
                    className="flex items-center gap-2"
                    data-qa-id="button-toggle-payroll-guide"
                >
                    <Info className="h-4 w-4" />
                    {isGuideOpen ? "Hide Guide" : "Show Guide"}
                    {isGuideOpen ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
                </Button>
            )}

            {!shouldShowHRISWarning() && isGuideOpen && (
                <div className="space-y-6 p-6 border-2 border-border rounded-lg bg-muted dark:bg-gray-900 text-card-foreground mb-6 shadow-sm">
                    <div className="border-b-2 border-border pb-3">
                        <h3 className="text-xl font-semibold text-foreground">Payroll — Guide</h3>
                    </div>

                    <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                        <div className="p-4 border border-border rounded-lg bg-card shadow-sm">
                            <h3 className="font-semibold mb-3 text-foreground border-b border-border pb-2">Purpose</h3>
                            <ul className="list-disc pl-5 space-y-1 text-muted-foreground">
                                <li>Forecast labor (hours/costs and salaries) to produce Forecasted Payroll. This drives the Payroll line and auto-calculates Insurance and PTEB in the forecast.</li>
                            </ul>
                        </div>
                        <div className="p-4 border border-border rounded-lg bg-card shadow-sm">
                            <h3 className="font-semibold mb-3 text-foreground border-b border-border pb-2">What to enter</h3>
                            <ol className="list-decimal pl-5 space-y-1 text-muted-foreground">
                                <li>Standard sites: forecast hours by Job Group (e.g., Bell, Parking Mgmt).</li>
                                <li>Per Labor Hour sites: forecast hours by Job Title (e.g., Bell Attendant) — these hours also drive PLH-based Internal Revenue.</li>
                                <li>Salaried costs are auto-allocated; ensure employee transfers/ends are reflected in your plan.</li>
                            </ol>
                        </div>
                        <div className="p-4 border border-border rounded-lg bg-card shadow-sm">
                            <h3 className="font-semibold mb-3 text-foreground border-b border-border pb-2">How your inputs are used</h3>
                            <ol className="list-decimal pl-5 space-y-1 text-muted-foreground">
                                <li>Forecasted Payroll = Hourly labor + Allocated salaries.</li>
                                <li>Insurance (forecast months):</li>
                                <ol className="list-decimal pl-8 space-y-1 mt-1">
                                    <li>Management Agreement sites: payroll portion = 5.77% × Forecasted Payroll; system also adds vehicle insurance (account 7082) and any configured add&apos;l amount.</li>
                                    <li>Non-Management Agreement sites: applies a site-specific insurance rate derived from Budget (defaults to ~4.45% where needed) × Forecasted Payroll.</li>
                                </ol>
                                <li>PTEB (forecast months): the system derives a site PTEB rate from Budget and applies it to Forecasted Payroll.</li>
                            </ol>
                        </div>
                        <div className="p-4 border border-border rounded-lg bg-card shadow-sm">
                            <h3 className="font-semibold mb-3 text-foreground border-b border-border pb-2">Tips and guardrails</h3>
                            <ol className="list-decimal pl-5 space-y-1 text-muted-foreground">
                                <li>Keep payroll current — Insurance/PTEB forecasts update automatically from it.</li>
                                <li>Don&apos;t enter Insurance or PTEB manually; they are calculated from your payroll forecast.</li>
                            </ol>
                        </div>
                    </div>

                    <div className="mt-8 p-5 border-2 border-border rounded-lg bg-muted dark:bg-gray-900">
                        <h3 className="font-semibold mb-4 text-foreground border-b-2 border-border pb-2">How to use the Copy Schedule to Forecast feature</h3>
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <div className="p-3 border border-border rounded-md bg-card">
                                <h4 className="font-medium mb-2 text-muted-foreground">Step 1: Access the Legion Data card</h4>
                                <ul className="list-disc pl-5 space-y-1 text-muted-foreground">
                                    <li>On the Payroll tab, in the Labor Hours & Cost Timeline section, locate the Scheduled & Actual Data card on the left hand side of the screen</li>
                                    <li>In the upper right corner of the card, beneath the badge for &apos;Legion Data&apos; find the &apos;Copy Schedule to Forecast&apos; button</li>
                                </ul>
                            </div>
                            <div className="p-3 border border-border rounded-md bg-card">
                                <h4 className="font-medium mb-2 text-muted-foreground">Step 2: Click the Copy Schedule to Forecast button</h4>
                                <ul className="list-disc pl-5 space-y-1 text-muted-foreground">
                                    <li>Upon clicking the button, the system will present a confirmation dialog informing you of the feature</li>
                                    <li>Click Continue to perform the Copy action</li>
                                </ul>
                            </div>
                            <div className="p-3 border border-border rounded-md bg-card">
                                <h4 className="font-medium mb-2 text-muted-foreground">Step 3: Review the Forecast data</h4>
                                <ul className="list-disc pl-5 space-y-1 text-muted-foreground">
                                    <li>The Forecast Data card will now display the amount of hours that were Scheduled in Legion for each Job, for as many dates as the Legion schedule has provided</li>
                                    <li>Review each Job to ensure accuracy</li>
                                </ul>
                            </div>
                            <div className="p-3 border border-border rounded-md bg-card">
                                <h4 className="font-medium mb-2 text-muted-foreground">Step 4: Save the Forecast data</h4>
                                <ul className="list-disc pl-5 space-y-1 text-muted-foreground">
                                    <li>The copied schedule data will only be saved to your Forecast once the Save All button in upper right corner of the system is clicked</li>
                                    <li>If you don&apos;t wish to save the copied schedule data, you may click the Undo Copy button which temporarily replaces the Copy button until the data has been Saved or Undone</li>
                                </ul>
                            </div>
                        </div>
                    </div>

                    <div className="mt-8 p-5 border-2 border-border rounded-lg bg-muted dark:bg-gray-900">
                        <h3 className="font-semibold mb-4 text-foreground border-b-2 border-border pb-2">How to use the Payroll Variance Dashboard</h3>
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <div className="p-3 border border-border rounded-md bg-card">
                                <h4 className="font-medium mb-2 text-muted-foreground">Step 1: Access Variance Dashboard</h4>
                                <ul className="list-disc pl-5 space-y-1 text-muted-foreground">
                                    <li>On the Payroll tab, scroll down to the Variance Reconciliation Dashboard section</li>
                                </ul>
                            </div>
                            <div className="p-3 border border-border rounded-md bg-card">
                                <h4 className="font-medium mb-2 text-muted-foreground">Step 2: Customize Date Range</h4>
                                <ul className="list-disc pl-5 space-y-1 text-muted-foreground">
                                    <li>Click on the start date and end date picker to modify the analysis period</li>
                                    <li>System automatically updates the table data based on the new date range</li>
                                </ul>
                            </div>
                            <div className="p-3 border border-border rounded-md bg-card">
                                <h4 className="font-medium mb-2 text-muted-foreground">Step 3: Filter by Job Category</h4>
                                <ul className="list-disc pl-5 space-y-1 text-muted-foreground">
                                    <li>Click on the job filter dropdown and select a specific job group or &apos;All Jobs&apos;</li>
                                    <li>System updates the table to show only the selected job categories</li>
                                </ul>
                            </div>
                            <div className="p-3 border border-border rounded-md bg-card">
                                <h4 className="font-medium mb-2 text-muted-foreground">Step 4: Analyze Variance Data</h4>
                                <ul className="list-disc pl-5 space-y-1 text-muted-foreground">
                                    <li>Review the table showing Scheduled, Actual, Forecast, Budget, and Variance columns</li>
                                    <li>Examine variance indicators (arrows) to understand actual vs budget performance</li>
                                </ul>
                            </div>
                            <div className="p-3 border border-border rounded-md bg-card md:col-span-2">
                                <h4 className="font-medium mb-2 text-muted-foreground">Step 5: Sort and Analyze Trends</h4>
                                <ul className="list-disc pl-5 space-y-1 text-muted-foreground">
                                    <li>Click on column headers to sort data by job code or variance</li>
                                    <li>Review the summary totals below the table</li>
                                    <li>Identify jobs with significant variances requiring attention</li>
                                </ul>
                            </div>
                        </div>
                    </div>
                </div>
            )}

            {shouldShowHRISWarning() ? (
                <Card className="w-full">
                    <CardContent className="p-8">
                        <div className="flex flex-col items-center justify-center text-center space-y-4">
                            <div className="w-16 h-16 bg-blue-300 rounded-full flex items-center justify-center">
                                <Info className="h-8 w-8 text-blue-600" />
                            </div>
                            <div className="space-y-2">
                                <h3 className="text-xl font-semibold text-gray-600 dark:text-blue-500">
                                    Please contact HRIS team for Job Code assignment
                                </h3>
                            </div>
                        </div>
                    </CardContent>
                </Card>
            ) : (
                <Card className="w-full relative">
                    <CardHeader className="flex flex-row items-center justify-between">
                        <CardTitle>Labor Hours & Cost Timeline</CardTitle>
                        <div className="flex items-center space-x-2">
                            <Button
                                variant="outline"
                                size="sm"
                                onClick={handlePreviousWeek}
                                data-qa-id="button-previous-week"
                            >
                                <ChevronLeft className="h-4 w-4" />
                            </Button>
                            <span className="text-sm font-medium">
                                {weeks.length > 0 ?
                                    formatDateRange(currentWeek.start, currentWeek.end) :
                                    "No dates available"}
                            </span>
                            <Button
                                variant="outline"
                                size="sm"
                                onClick={handleNextWeek}
                                data-qa-id="button-next-week"
                            >
                                <ChevronRight className="h-4 w-4" />
                            </Button>
                        </div>
                    </CardHeader>
                    <CardContent>
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-6 items-start">
                            <Card className="h-fit">
                                <CardContent className="p-4">
                                    <div className="flex justify-between items-center h-14 mb-6">
                                        <h3 className="font-semibold">Scheduled & Actual Data</h3>
                                        <div className="flex flex-col items-end h-14 justify-center">
                                            <Badge
                                                variant="outline"
                                                className="flex items-center mb-1"
                                                data-qa-id="badge-legion-data"
                                            >
                                                <Calendar className="h-3 w-3 mr-1" />
                                                Legion Data
                                            </Badge>
                                            {!canUndo ? (
                                                <Button
                                                    variant="outline"
                                                    size="sm"
                                                    className="text-xs h-6"
                                                    onClick={handleCopyScheduleToForecast}
                                                    disabled={!hasScheduledData() || isPastPeriod}
                                                    data-qa-id="button-copy-schedule-to-forecast"
                                                >
                                                    Copy Schedule to Forecast
                                                </Button>
                                            ) : (
                                                <Button
                                                    variant="outline"
                                                    size="sm"
                                                    className="text-xs h-6 text-orange-600 dark:text-orange-400 border-orange-600 dark:border-orange-400 hover:bg-orange-50 dark:hover:bg-orange-950"
                                                    onClick={handleUndo}
                                                    data-qa-id="button-undo-copy"
                                                >
                                                    Undo Copy
                                                </Button>
                                            )}
                                        </div>
                                    </div>


                                    <div className="space-y-6">
                                        {isLoadingPayroll ? (
                                            <Skeleton className="h-[400px] w-full" />
                                        ) : (
                                            currentWeek.dates.map((date) => {
                                                const dateKey = formatDateKey(date);
                                                const isExpanded = expandedDays.has(dateKey);
                                                // Use job group level data for accurate totals in Scheduled & Actual Data section
                                                const scheduledTotal = getGroupTotalForDate(dateKey, scheduledGroupData);
                                                const actualTotal = getGroupTotalForDate(dateKey, actualGroupData);
                                                const hasActualValue = hasActualData(dateKey);
                                                const variance = getVariancePercentage(dateKey);

                                                return (
                                                    <Collapsible
                                                        key={dateKey}
                                                        className="relative"
                                                        open={isExpanded}
                                                        data-qa-id={`collapsible-scheduled-${dateKey}`}
                                                    >
                                                        <div className="flex justify-between items-center mb-1">
                                                            <span className="text-sm font-medium">
                                                                {formatDateDisplay(date)}
                                                            </span>
                                                            <span className="flex flex-col items-end text-sm">
                                                                <span>
                                                                    {hasActualValue ? (() => {
                                                                        const actualCost = Object.entries(actualGroupData[dateKey] || {}).reduce((total, [groupId, hours]) => {
                                                                            // Get job codes for this group and calculate cost using actual individual job code data
                                                                            const groupJobCodes = jobCodes.filter(jc => jc.jobGroupId === groupId);
                                                                            return total + groupJobCodes.reduce((sum, jc) => {
                                                                                // Use actual individual job code hours instead of dividing group total
                                                                                const individualHours = actualData[dateKey]?.[jc.jobCode] || 0;
                                                                                return sum + calculateJobCodeCost(jc.jobCode, individualHours);
                                                                            }, 0);
                                                                        }, 0);
                                                                        return `${actualTotal.toFixed(1)} hrs (${formatCurrency(actualCost)})`;
                                                                    })() : "No actual data yet"}
                                                                </span>
                                                            </span>
                                                        </div>

                                                        <div
                                                            className="h-8 bg-muted rounded-md overflow-hidden relative cursor-pointer hover:bg-muted/80"
                                                            onClick={() => toggleDayExpansion(dateKey)}
                                                            data-qa-id={`button-toggle-date-${dateKey}`}
                                                        >
                                                            <TooltipProvider>
                                                                <Tooltip>
                                                                    <TooltipTrigger asChild>
                                                                        <div className="absolute inset-0">
                                                                            <div
                                                                                className="absolute top-0 h-full opacity-0"
                                                                                style={{
                                                                                    left: `${getBarWidth(getMonthlyGlobalMax())}%`
                                                                                }}
                                                                            >&nbsp;</div>

                                                                            {hasActualValue && actualTotal >= scheduledTotal ? (
                                                                                <>
                                                                                    <div
                                                                                        className="absolute top-0 left-0 h-full bg-orange-500/70 rounded-md border-r-2 border-orange-700"
                                                                                        style={{ width: `${getBarWidth(actualTotal)}%` }}
                                                                                    ></div>
                                                                                    <div
                                                                                        className="absolute top-0 left-0 h-full bg-blue-500/70 rounded-md z-10"
                                                                                        style={{ width: `${getBarWidth(scheduledTotal)}%` }}
                                                                                    ></div>
                                                                                </>
                                                                            ) : (
                                                                                <>
                                                                                    <div
                                                                                        className="absolute top-0 left-0 h-full bg-blue-500/70 rounded-md"
                                                                                        style={{ width: `${getBarWidth(scheduledTotal)}%` }}
                                                                                    ></div>
                                                                                    {hasActualValue && (
                                                                                        <div
                                                                                            className="absolute top-0 left-0 h-full bg-orange-500/70 rounded-md border-r-2 border-orange-700 z-10"
                                                                                            style={{ width: `${getBarWidth(actualTotal)}%` }}
                                                                                        ></div>
                                                                                    )}
                                                                                </>
                                                                            )}
                                                                        </div>
                                                                    </TooltipTrigger>
                                                                    <TooltipContent side="top" className="w-48">
                                                                        <div className="space-y-1">
                                                                            <div className="flex items-center justify-between">
                                                                                <div className="flex items-center">
                                                                                    <div className="w-3 h-3 bg-blue-500 rounded-sm mr-2"></div>
                                                                                    <span className="text-xs">Scheduled:</span>
                                                                                </div>
                                                                                <span className="text-xs font-medium">
                                                                                    {getDisplayValue(scheduledTotal)}
                                                                                </span>
                                                                            </div>
                                                                            {hasActualValue && (
                                                                                <div className="flex items-center justify-between">
                                                                                    <div className="flex items-center">
                                                                                        <div className="w-3 h-3 bg-orange-500 rounded-sm mr-2"></div>
                                                                                        <span className="text-xs">Actual:</span>
                                                                                    </div>
                                                                                    <span className="text-xs font-medium">
                                                                                        {getDisplayValue(actualTotal)}
                                                                                    </span>
                                                                                </div>
                                                                            )}
                                                                            {hasActualValue && (
                                                                                <div className="flex items-center justify-between pt-1 border-t border-border/50">
                                                                                    <span className="text-xs">Variance:</span>
                                                                                    <span className={`text-xs font-medium ${variance > 0 ? "text-red-500" : "text-green-500"}`}>
                                                                                        {variance > 0 ? "+" : ""}
                                                                                        {variance.toFixed(1)}%
                                                                                    </span>
                                                                                </div>
                                                                            )}
                                                                        </div>
                                                                    </TooltipContent>
                                                                </Tooltip>
                                                            </TooltipProvider>

                                                            <Button
                                                                variant="ghost"
                                                                size="sm"
                                                                className="absolute right-1 bottom-1 h-6 w-6 p-0 bg-background/80 hover:bg-background"
                                                                onClick={(e) => {
                                                                    e.stopPropagation();
                                                                    toggleDayExpansion(dateKey);
                                                                }}
                                                                data-qa-id={`button-expand-date-${dateKey}`}
                                                            >
                                                                <ChevronDown
                                                                    className={`h-4 w-4 transition-transform ${isExpanded ? "rotate-180" : ""}`}
                                                                />
                                                            </Button>
                                                        </div>

                                                        <div className="flex justify-between text-xs text-muted-foreground mt-1">
                                                            <span className="text-muted-foreground text-xs">
                                                                Scheduled: {(() => {
                                                                    const scheduledCost = Object.entries(scheduledGroupData[dateKey] || {}).reduce((total, [groupId, hours]) => {
                                                                        const groupJobCodes = jobCodes.filter(jc => jc.jobGroupId === groupId);
                                                                        return total + groupJobCodes.reduce((sum, jc) => {
                                                                            // Use actual individual job code hours instead of dividing group total
                                                                            const individualHours = scheduledData[dateKey]?.[jc.jobCode] || 0;
                                                                            return sum + calculateJobCodeCost(jc.jobCode, individualHours);
                                                                        }, 0);
                                                                    }, 0);
                                                                    return `${scheduledTotal.toFixed(1)} hrs (${formatCurrency(scheduledCost)})`;
                                                                })()}
                                                            </span>
                                                            {hasActualValue && (
                                                                <span className={variance > 0 ? "text-red-500" : "text-green-500"}>
                                                                    {variance > 0 ? "+" : ""}
                                                                    {variance.toFixed(1)}%
                                                                </span>
                                                            )}
                                                        </div>

                                                        <CollapsibleContent className="mt-2 pl-4 border-l-2 border-muted">
                                                            {(() => {
                                                                const contractType = payrollData ? determineContractType(payrollData, contractDetails) : "Standard";

                                                                // Group scheduled and actual details by group and code, similar to forecast/budget
                                                                // Use scheduledPayrollDetails and actualPayrollDetails for the real breakdown
                                                                // Group by jobGroupId and jobGroupName using jobCodes from the endpoint

                                                                // Build a group map
                                                                const groupMap: Record<string, { jobGroupId: string, jobGroupName: string, jobCodes: any[] }> = {};

                                                                // Use jobCodes from the endpoint to get the group-code relationship
                                                                jobCodes.forEach(jc => {
                                                                    const groupId = jc.jobGroupId || "unknown-group";
                                                                    const groupName = jc.jobGroupName || "Unknown Group";
                                                                    if (!groupMap[groupId]) {
                                                                        groupMap[groupId] = { jobGroupId: groupId, jobGroupName: groupName, jobCodes: [] };
                                                                    }
                                                                    groupMap[groupId].jobCodes.push({
                                                                        jobCode: jc.jobCode,
                                                                        displayName: getJobTitleFallback(jc)
                                                                    });
                                                                });

                                                                // For each group, show all job codes (including those with 0 values for salaried positions)
                                                                // Then sort jobGroups and jobCodes alphabetically
                                                                const jobGroups = Object.values(groupMap)
                                                                    .map(group => {
                                                                        const sortedJobCodes = group.jobCodes
                                                                            .sort((a, b) =>
                                                                                (a.displayName || a.jobCode || "").localeCompare(b.displayName || b.jobCode || "")
                                                                            );
                                                                        return {
                                                                            ...group,
                                                                            jobCodes: sortedJobCodes
                                                                        };
                                                                    })
                                                                    .filter(group => group.jobCodes.length > 0)
                                                                    .sort((a, b) =>
                                                                        (a.jobGroupName || "").localeCompare(b.jobGroupName || "")
                                                                    );


                                                                if (contractType === "PerLaborHour") {
                                                                    // For PLH sites: Show only job codes/titles directly (no job group hierarchy)
                                                                    const allJobCodes = jobGroups.flatMap(group => group.jobCodes || [])
                                                                        .sort((a, b) => (a.displayName || a.jobCode || "").localeCompare(b.displayName || b.jobCode || ""));

                                                                    return allJobCodes.map((jobCode) => {
                                                                        const scheduledHours = scheduledData[dateKey]?.[jobCode.jobCode] || 0;
                                                                        const actualHours = actualData[dateKey]?.[jobCode.jobCode] || 0;
                                                                        const jobVariance = scheduledHours === 0 ? 0 : ((actualHours - scheduledHours) / scheduledHours) * 100;

                                                                        return (
                                                                            <div key={`${dateKey}-${jobCode.jobCode}`} className="mb-2 min-h-10">
                                                                                <div className="flex justify-between items-center mb-1">
                                                                                    <span className="text-xs font-medium whitespace-nowrap">{jobCode.displayName || jobCode.jobCode}</span>
                                                                                    <span className="text-xs whitespace-nowrap text-right">
                                                                                        {hasActualValue ?
                                                                                            getHoursAndCostString(actualHours, jobCode.jobCode) :
                                                                                            getHoursAndCostString(scheduledHours, jobCode.jobCode)}
                                                                                    </span>
                                                                                </div>
                                                                                <div className="h-4 bg-muted/50 rounded overflow-hidden relative">
                                                                                    <TooltipProvider>
                                                                                        <Tooltip>
                                                                                            <TooltipTrigger asChild>
                                                                                                <div className="absolute inset-0">
                                                                                                    {/* Blue: Scheduled (scaled to total) */}
                                                                                                    <div
                                                                                                        className="absolute top-0 left-0 h-full bg-blue-500/70 rounded-md"
                                                                                                        style={{
                                                                                                            width: `${getMonthlyGlobalMax() > 0
                                                                                                                ? Math.max(1, (scheduledHours / getMonthlyGlobalMax()) * 100)
                                                                                                                : 0
                                                                                                                }%`
                                                                                                        }}
                                                                                                    ></div>
                                                                                                    {/* Orange: Actual (scaled to total) */}
                                                                                                    <div
                                                                                                        className="absolute top-0 left-0 h-full bg-orange-500/70 rounded-md border-r-2 border-orange-700 z-10"
                                                                                                        style={{
                                                                                                            width: `${getMonthlyGlobalMax() > 0
                                                                                                                ? Math.max(1, (actualHours / getMonthlyGlobalMax()) * 100)
                                                                                                                : 0
                                                                                                                }%`
                                                                                                        }}
                                                                                                    ></div>
                                                                                                </div>
                                                                                            </TooltipTrigger>
                                                                                            <TooltipContent side="top" className="w-48">
                                                                                                <div className="space-y-1">
                                                                                                    <div className="flex items-center justify-between">
                                                                                                        <div className="flex items-center">
                                                                                                            <div className="w-3 h-3 bg-blue-500 rounded-sm mr-2"></div>
                                                                                                            <span className="text-xs">Scheduled:</span>
                                                                                                        </div>
                                                                                                        <span className="text-xs font-medium">
                                                                                                            {getHoursAndCostString(scheduledHours, jobCode.jobCode)}
                                                                                                        </span>
                                                                                                    </div>
                                                                                                    {hasActualValue && (
                                                                                                        <div className="flex items-center justify-between">
                                                                                                            <div className="flex items-center">
                                                                                                                <div className="w-3 h-3 bg-orange-500 rounded-sm mr-2"></div>
                                                                                                                <span className="text-xs">Actual:</span>
                                                                                                            </div>
                                                                                                            <span className="text-xs font-medium">
                                                                                                                {getHoursAndCostString(actualHours, jobCode.jobCode)}
                                                                                                            </span>
                                                                                                        </div>
                                                                                                    )}
                                                                                                    {hasActualValue && (
                                                                                                        <div className="flex items-center justify-between pt-1 border-t border-border/50">
                                                                                                            <span className="text-xs">Variance:</span>
                                                                                                            <span className={`text-xs font-medium ${jobVariance > 0 ? "text-red-500" : "text-green-500"}`}>
                                                                                                                {jobVariance > 0 ? "+" : ""}
                                                                                                                {jobVariance.toFixed(1)}%
                                                                                                            </span>
                                                                                                        </div>
                                                                                                    )}
                                                                                                </div>
                                                                                            </TooltipContent>
                                                                                        </Tooltip>
                                                                                    </TooltipProvider>
                                                                                </div>
                                                                                <div className="flex justify-between text-xs text-muted-foreground mt-1">
                                                                                    <span>
                                                                                        {hasActualValue ?
                                                                                            `Actual: ${getHoursAndCostString(actualHours, jobCode.jobCode)}` :
                                                                                            ""}
                                                                                    </span>
                                                                                    {hasActualValue && (
                                                                                        <span className={jobVariance > 0 ? "text-red-500" : "text-green-500"}>
                                                                                            {jobVariance > 0 ? "+" : ""}
                                                                                            {jobVariance.toFixed(1)}%
                                                                                        </span>
                                                                                    )}
                                                                                </div>
                                                                            </div>
                                                                        );
                                                                    });
                                                                } else {
                                                                    // Standard: Show only jobGroup progress bars (no job code breakdown)
                                                                    return jobGroups.map((jobGroup) => {
                                                                        const groupKey = `${dateKey}-${jobGroup.jobGroupId}`;
                                                                        // Get totals for the group from job group level data (not aggregated from job codes)
                                                                        const scheduledHours = scheduledGroupData[dateKey]?.[jobGroup.jobGroupId] || 0;
                                                                        const actualHours = actualGroupData[dateKey]?.[jobGroup.jobGroupId] || 0;
                                                                        const jobVariance = scheduledHours === 0 ? 0 : ((actualHours - scheduledHours) / scheduledHours) * 100;

                                                                        return (
                                                                            <div key={groupKey} className="relative mb-2 min-h-10">
                                                                                <div className="flex justify-between items-center text-xs mb-1">
                                                                                    <span className="font-medium whitespace-nowrap">{jobGroup.jobGroupName}</span>
                                                                                    <span className="flex items-center">
                                                                                        <span>
                                                                                            {(() => {
                                                                                                const groupJobCodes = jobCodes.filter(jc => jc.jobGroupId === jobGroup.jobGroupId);
                                                                                                const actualCost = groupJobCodes.reduce((sum, jc) => {
                                                                                                    // Use actual individual job code hours instead of dividing group total
                                                                                                    const individualHours = actualData[dateKey]?.[jc.jobCode] || 0;
                                                                                                    return sum + calculateJobCodeCost(jc.jobCode, individualHours);
                                                                                                }, 0);
                                                                                                return `${actualHours.toFixed(1)} hrs (${formatCurrency(actualCost)})`;
                                                                                            })()}
                                                                                        </span>
                                                                                        <span className="text-muted-foreground text-xs ml-2">
                                                                                            (Scheduled: {(() => {
                                                                                                const groupJobCodes = jobCodes.filter(jc => jc.jobGroupId === jobGroup.jobGroupId);
                                                                                                const scheduledCost = groupJobCodes.reduce((sum, jc) => {
                                                                                                    // Use actual individual job code hours instead of dividing group total
                                                                                                    const individualHours = scheduledData[dateKey]?.[jc.jobCode] || 0;
                                                                                                    return sum + calculateJobCodeCost(jc.jobCode, individualHours);
                                                                                                }, 0);
                                                                                                return `${scheduledHours.toFixed(1)} hrs (${formatCurrency(scheduledCost)})`;
                                                                                            })()})
                                                                                        </span>
                                                                                    </span>
                                                                                </div>
                                                                                {/* Group progress bar */}
                                                                                <div className="h-4 bg-muted/50 rounded overflow-hidden relative">
                                                                                    <TooltipProvider>
                                                                                        <Tooltip>
                                                                                            <TooltipTrigger asChild>
                                                                                                <div className="absolute inset-0">
                                                                                                    {/* Blue: Scheduled (scaled to total) */}
                                                                                                    <div
                                                                                                        className="absolute top-0 left-0 h-full bg-blue-500/70 rounded-md"
                                                                                                        style={{
                                                                                                            width: `${getMonthlyGlobalMax() > 0
                                                                                                                ? Math.max(1, (scheduledHours / getMonthlyGlobalMax()) * 100)
                                                                                                                : 0
                                                                                                                }%`
                                                                                                        }}
                                                                                                    ></div>
                                                                                                    {/* Orange: Actual (scaled to total) */}
                                                                                                    <div
                                                                                                        className="absolute top-0 left-0 h-full bg-orange-500/70 rounded-md border-r-2 border-orange-700 z-10"
                                                                                                        style={{
                                                                                                            width: `${getMonthlyGlobalMax() > 0
                                                                                                                ? Math.max(1, (actualHours / getMonthlyGlobalMax()) * 100)
                                                                                                                : 0
                                                                                                                }%`
                                                                                                        }}
                                                                                                    ></div>
                                                                                                </div>
                                                                                            </TooltipTrigger>
                                                                                            <TooltipContent side="top" className="w-48">
                                                                                                <div className="space-y-1">
                                                                                                    <div className="flex items-center justify-between">
                                                                                                        <div className="flex items-center">
                                                                                                            <div className="w-3 h-3 bg-blue-500 rounded-sm mr-2"></div>
                                                                                                            <span className="text-xs">Scheduled:</span>
                                                                                                        </div>
                                                                                                        <span className="text-xs font-medium">
                                                                                                            {scheduledHours.toFixed(1)} hrs
                                                                                                        </span>
                                                                                                    </div>
                                                                                                    {hasActualValue && (
                                                                                                        <div className="flex items-center justify-between">
                                                                                                            <div className="flex items-center">
                                                                                                                <div className="w-3 h-3 bg-orange-500 rounded-sm mr-2"></div>
                                                                                                                <span className="text-xs">Actual:</span>
                                                                                                            </div>
                                                                                                            <span className="text-xs font-medium">
                                                                                                                {actualHours.toFixed(1)} hrs
                                                                                                            </span>
                                                                                                        </div>
                                                                                                    )}
                                                                                                    {hasActualValue && (
                                                                                                        <div className="flex items-center justify-between pt-1 border-t border-border/50">
                                                                                                            <span className="text-xs">Variance:</span>
                                                                                                            <span className={`text-xs font-medium ${jobVariance > 0 ? "text-red-500" : "text-green-500"}`}>
                                                                                                                {jobVariance > 0 ? "+" : ""}
                                                                                                                {jobVariance.toFixed(1)}%
                                                                                                            </span>
                                                                                                        </div>
                                                                                                    )}
                                                                                                </div>
                                                                                            </TooltipContent>
                                                                                        </Tooltip>
                                                                                    </TooltipProvider>
                                                                                </div>
                                                                            </div>
                                                                        );
                                                                    });
                                                                }
                                                            })()}
                                                        </CollapsibleContent>
                                                    </Collapsible>
                                                );
                                            })
                                        )}
                                    </div>
                                </CardContent>
                            </Card>

                            <Card className="h-fit">
                                <CardContent className="p-4">
                                    <div className="flex justify-between items-center h-14 mb-6">
                                        <h3 className="font-semibold">Forecast Data</h3>
                                        <div className="flex items-center gap-2">
                                            <div className="flex items-center text-xs space-x-1">
                                                <div className="w-3 h-3 bg-green-500 rounded-sm"></div>
                                                <span>Forecast</span>
                                            </div>
                                            <div className="flex items-center text-xs space-x-1">
                                                <div className="w-3 h-3 border-r-2 border-red-500"></div>
                                                <span>Budget</span>
                                            </div>
                                            <div className="flex items-center text-xs space-x-1">
                                                <Edit3 className="w-3 h-3" />
                                                <span>Click to edit</span>
                                            </div>
                                        </div>
                                    </div>


                                    <div className="space-y-6">
                                        {isLoadingPayroll ? (
                                            <Skeleton className="h-[400px] w-full" />
                                        ) : (
                                            currentWeek.dates.map((date) => {
                                                const dateKey = formatDateKey(date);
                                                const isDayExpanded = expandedDays.has(dateKey);
                                                const forecastTotal = getTotalForDate(dateKey, forecastData);
                                                const budgetTotal = getTotalForDate(dateKey, budgetData);
                                                const variancePercentage = budgetTotal > 0 ? ((forecastTotal - budgetTotal) / budgetTotal) * 100 : 0;
                                                const hasSignificantVariance = Math.abs(variancePercentage) > 10;
                                                const isFuture = isFutureDate(date);
                                                const contractType = payrollData ? determineContractType(payrollData, contractDetails) : "Standard";
                                                let jobGroups = getJobGroupsWithFallback()
                                                    .map(group => ({
                                                        ...group,
                                                        jobCodes: (group.jobCodes || [])
                                                            .map((code) => ({
                                                                ...code,
                                                                date: code.date || dateKey,
                                                            }))
                                                            .filter(jc => jc.date === dateKey)
                                                            .sort((a, b) =>
                                                                (a.displayName || a.jobCode || "").localeCompare(
                                                                    b.displayName || b.jobCode || ""
                                                                )
                                                            )
                                                    }))
                                                    .filter(group => group.jobCodes.length > 0)
                                                    .sort((a, b) =>
                                                        (a.jobGroupName || "").localeCompare(b.jobGroupName || "")
                                                    );

                                                return (
                                                    <div key={dateKey} className="relative">
                                                        <div className="flex justify-between items-center mb-1">
                                                            <span className="text-sm font-medium">{formatDateDisplay(date)}</span>
                                                            <span className="text-sm flex items-center">
                                                                {(() => {
                                                                    const groups = getJobGroupsWithFallback().filter(g => g.date === dateKey);
                                                                    const groupCost = groups.reduce((sum, group) => {
                                                                        return sum + (group.jobCodes?.reduce((s, jc) => s + (jc.forecastPayrollCost || 0), 0) || 0);
                                                                    }, 0);
                                                                    return `${forecastTotal.toFixed(1)} hrs (${formatCurrency(groupCost)})`;
                                                                })()}
                                                                {hasSignificantVariance && (
                                                                    <span
                                                                        className="ml-1 text-yellow-500"
                                                                        title={`${Math.abs(variancePercentage).toFixed(1)}% ${variancePercentage > 0 ? "above" : "below"
                                                                            } budget`}
                                                                    >
                                                                        ⚠
                                                                    </span>
                                                                )}
                                                            </span>
                                                        </div>
                                                        <div
                                                            className="h-8 bg-muted rounded-md overflow-hidden relative cursor-pointer hover:bg-muted/80"
                                                            onClick={() => toggleDayExpansion(dateKey)}
                                                            data-qa-id={`button-toggle-forecast-date-${dateKey}`}
                                                        >
                                                            <div className="absolute inset-0">
                                                                <div
                                                                    className="absolute top-0 left-0 h-full bg-green-500/70 rounded-md"
                                                                    style={{
                                                                        width: `${getMonthlyGlobalMax() > 0
                                                                            ? Math.min(100, (forecastTotal / getMonthlyGlobalMax()) * 100)
                                                                            : 0
                                                                            }%`
                                                                    }}
                                                                ></div>
                                                                <div
                                                                    className="absolute top-0 h-full border-r-4 border-red-600 shadow-[0_0_8px_rgba(220,38,38,0.5)] z-20"
                                                                    style={{
                                                                        left: `${getMonthlyGlobalMax() > 0
                                                                            ? Math.min(100, (budgetTotal / getMonthlyGlobalMax()) * 100)
                                                                            : 0
                                                                            }%`,
                                                                        height: "100%",
                                                                    }}
                                                                ></div>
                                                            </div>
                                                            <Button
                                                                variant="ghost"
                                                                size="sm"
                                                                className="absolute right-1 bottom-1 h-6 w-6 p-0 bg-background/80 hover:bg-background"
                                                                data-qa-id={`button-expand-forecast-date-${dateKey}`}
                                                            >
                                                                <ChevronDown className={`h-4 w-4 ${isDayExpanded ? "rotate-180" : ""}`} />
                                                            </Button>
                                                        </div>
                                                        <div className="flex justify-between text-xs text-muted-foreground mt-1">
                                                            <span>
                                                                Budget: {(() => {
                                                                    const budgetCost = Object.entries(budgetData[dateKey] || {}).reduce((total, [jobCode, hours]) => {
                                                                        return total + calculateJobCodeCost(jobCode, hours);
                                                                    }, 0);
                                                                    return `${budgetTotal.toFixed(1)} hrs (${formatCurrency(budgetCost)})`;
                                                                })()}
                                                                {hasSignificantVariance && (
                                                                    <span className={`ml-1 ${variancePercentage > 0 ? "text-green-500" : "text-red-500"}`}>
                                                                        ({variancePercentage > 0 ? "+" : ""}
                                                                        {variancePercentage.toFixed(1)}%)
                                                                    </span>
                                                                )}
                                                            </span>
                                                            <span>{isFuture ? "Future" : "Past"}</span>
                                                        </div>

                                                        {isDayExpanded && (
                                                            <div className="mt-2 space-y-2 pl-4 border-l-2 border-muted">
                                                                {contractType === "PerLaborHour" ? (
                                                                    // For PLH sites: Show only job codes/titles directly (no job group hierarchy)
                                                                    jobGroups.flatMap(group => group.jobCodes || [])
                                                                        .sort((a, b) => (a.displayName || a.jobCode || "").localeCompare(b.displayName || b.jobCode || ""))
                                                                        .map((jobMapping) => {
                                                                            const jobCode = jobMapping.jobCode;
                                                                            if (!jobCode) return null;
                                                                            const jobForecast = forecastData[dateKey]?.[jobCode] || 0;
                                                                            const jobBudget = budgetData[dateKey]?.[jobCode] || 0;
                                                                            const jobVariance = jobBudget > 0 ? ((jobForecast - jobBudget) / jobBudget) * 100 : 0;
                                                                            const hasJobSignificantVariance = Math.abs(jobVariance) > 10;

                                                                            return (
                                                                                <div
                                                                                    key={`${dateKey}-${jobCode}`}
                                                                                    className="relative mb-2 min-h-10"
                                                                                    title={`Job Code: ${jobCode} - Click to edit`}
                                                                                >
                                                                                    <div className="flex justify-between items-center text-xs mb-1">
                                                                                        <span className="font-medium whitespace-nowrap">{jobMapping.displayName || jobMapping.jobCode}</span>
                                                                                        <div className="flex items-center gap-2 whitespace-nowrap text-right">
                                                                                            <span className="flex items-center whitespace-nowrap">
                                                                                                {(() => {
                                                                                                    const forecastCost = calculateJobCodeCost(jobCode, jobForecast);
                                                                                                    return `${jobForecast.toFixed(1)} hrs (${formatCurrency(forecastCost)})`;
                                                                                                })()}
                                                                                                {hasJobSignificantVariance && (
                                                                                                    <span
                                                                                                        className="ml-1 text-yellow-500"
                                                                                                        title={`${Math.abs(jobVariance).toFixed(1)}% ${jobVariance > 0 ? "above" : "below"} budget`}
                                                                                                    >
                                                                                                        ⚠
                                                                                                    </span>
                                                                                                )}
                                                                                            </span>
                                                                                            <span className="text-muted-foreground text-xs whitespace-nowrap">
                                                                                                (B: {(() => {
                                                                                                    const budgetCost = calculateJobCodeCost(jobCode, jobBudget);
                                                                                                    return `${jobBudget.toFixed(1)} hrs (${formatCurrency(budgetCost)})`;
                                                                                                })()})
                                                                                            </span>
                                                                                        </div>
                                                                                    </div>
                                                                                    <div
                                                                                        className={`h-4 bg-muted/50 rounded overflow-hidden relative ${isFuture ? "cursor-pointer hover:bg-green-200/50 group" : ""}`}
                                                                                        onClick={(e) => {
                                                                                            e.stopPropagation();
                                                                                            if (isFuture) {
                                                                                                openEditDialog(dateKey, jobCode);
                                                                                            }
                                                                                        }}
                                                                                        data-qa-id={`button-edit-job-forecast-${dateKey}-${jobCode}`}
                                                                                    >
                                                                                        <div
                                                                                            className="absolute top-0 left-0 h-full bg-green-400/60 rounded"
                                                                                            style={{
                                                                                                width: `${getMonthlyGlobalMax() > 0
                                                                                                    ? Math.min(100, (jobForecast / getMonthlyGlobalMax()) * 100)
                                                                                                    : 0
                                                                                                    }%`
                                                                                            }}
                                                                                        ></div>
                                                                                        <div
                                                                                            className="absolute top-0 h-full border-r-2 border-red-500 z-10"
                                                                                            style={{
                                                                                                left: `${getMonthlyGlobalMax() > 0
                                                                                                    ? Math.min(100, (jobBudget / getMonthlyGlobalMax()) * 100)
                                                                                                    : 0
                                                                                                    }%`,
                                                                                                height: "100%",
                                                                                            }}
                                                                                        ></div>
                                                                                        {isFuture && (
                                                                                            <Edit3 className="absolute right-1 top-0.5 h-3 w-3 text-green-600 group-hover:opacity-100 opacity-100 transition-opacity" />
                                                                                        )}
                                                                                    </div>
                                                                                    {/* Spacer row to match height with Scheduled & Actual footer - only show when Scheduled section has actual values */}
                                                                                    {hasActualData(dateKey) && (
                                                                                        <div className="flex justify-between text-xs text-muted-foreground mt-1 invisible">
                                                                                            <span>Actual:&nbsp;</span>
                                                                                            <span>0.0%</span>
                                                                                        </div>
                                                                                    )}
                                                                                </div>
                                                                            );
                                                                        })
                                                                ) : (
                                                                    // Standard sites: Show only job groups (no job code breakdown)
                                                                    jobGroups.map((jobGroup) => {
                                                                        const jobsInGroup = jobGroup.jobCodes || [];
                                                                        const totalGroupForecast = jobsInGroup.reduce((sum, jc) => {
                                                                            const code = jc.jobCode;
                                                                            return sum + (code ? (forecastData[dateKey]?.[code] || 0) : 0);
                                                                        }, 0);
                                                                        const totalGroupBudget = jobsInGroup.reduce((sum, jc) => {
                                                                            const code = jc.jobCode;
                                                                            return sum + (code ? (budgetData[dateKey]?.[code] || 0) : 0);
                                                                        }, 0);
                                                                        const jobVariance = totalGroupBudget > 0 ? ((totalGroupForecast - totalGroupBudget) / totalGroupBudget) * 100 : 0;
                                                                        const hasJobSignificantVariance = Math.abs(jobVariance) > 10;

                                                                        return (
                                                                            <div key={`${dateKey}-${jobGroup.jobGroupId}`} className="relative mb-2 min-h-10">
                                                                                <div className="flex justify-between items-center text-xs mb-1">
                                                                                    <span className="font-medium whitespace-nowrap">{jobGroup.jobGroupName}</span>
                                                                                    <div className="flex items-center gap-2">
                                                                                        <span className="flex items-center">
                                                                                            {(() => {
                                                                                                const groupForecastHours = jobsInGroup.reduce((sum, jc) => {
                                                                                                    if (!jc.jobCode) return sum;
                                                                                                    return sum + (forecastData[dateKey]?.[jc.jobCode] || 0);
                                                                                                }, 0);
                                                                                                const groupForecastCost = jobsInGroup.reduce((sum, jc) => {
                                                                                                    if (!jc.jobCode) return sum;
                                                                                                    const hours = forecastData[dateKey]?.[jc.jobCode] || 0;
                                                                                                    return sum + calculateJobCodeCost(jc.jobCode, hours);
                                                                                                }, 0);
                                                                                                return `${groupForecastHours.toFixed(1)} hrs (${formatCurrency(groupForecastCost)})`;
                                                                                            })()}
                                                                                            {hasJobSignificantVariance && (
                                                                                                <span
                                                                                                    className="ml-1 text-yellow-500"
                                                                                                    title={`${Math.abs(jobVariance).toFixed(1)}% ${jobVariance > 0 ? "above" : "below"} budget`}
                                                                                                >
                                                                                                    ⚠
                                                                                                </span>
                                                                                            )}
                                                                                        </span>
                                                                                        <span className="text-muted-foreground text-xs">
                                                                                            (B: {(() => {
                                                                                                const budgetCost = jobsInGroup.reduce((sum, jc) => {
                                                                                                    if (!jc.jobCode) return sum;
                                                                                                    const hours = budgetData[dateKey]?.[jc.jobCode] || 0;
                                                                                                    return sum + calculateJobCodeCost(jc.jobCode, hours);
                                                                                                }, 0);
                                                                                                return `${totalGroupBudget.toFixed(1)} hrs (${formatCurrency(budgetCost)})`;
                                                                                            })()})
                                                                                        </span>
                                                                                    </div>
                                                                                </div>
                                                                                <div
                                                                                    className={`h-4 bg-muted/50 rounded overflow-hidden relative ${isFuture ? "cursor-pointer hover:bg-green-200/50 group" : ""}`}
                                                                                    onClick={(e) => {
                                                                                        e.stopPropagation();
                                                                                        if (isFuture) {
                                                                                            openEditDialog(dateKey, jobsInGroup[0]?.jobCode);
                                                                                        }
                                                                                    }}
                                                                                    title={isFuture ? "Click to edit job group forecast" : undefined}
                                                                                    data-qa-id={`button-edit-job-group-forecast-${dateKey}-${jobGroup.jobGroupId}`}
                                                                                >
                                                                                    <div
                                                                                        className="absolute top-0 left-0 h-full bg-green-400/60 rounded"
                                                                                        style={{
                                                                                            width: `${getMonthlyGlobalMax() > 0
                                                                                                ? Math.min(100, (totalGroupForecast / getMonthlyGlobalMax()) * 100)
                                                                                                : 0
                                                                                                }%`
                                                                                        }}
                                                                                    ></div>
                                                                                    <div
                                                                                        className="absolute top-0 h-full border-r-2 border-red-500 z-10"
                                                                                        style={{
                                                                                            left: `${getMonthlyGlobalMax() > 0
                                                                                                ? Math.min(100, (totalGroupBudget / getMonthlyGlobalMax()) * 100)
                                                                                                : 0
                                                                                                }%`,
                                                                                            height: "100%",
                                                                                        }}
                                                                                    ></div>
                                                                                    {isFuture && (
                                                                                        <Edit3 className="absolute right-1 top-0.5 h-3 w-3 text-green-600 group-hover:opacity-100 opacity-100 transition-opacity" />
                                                                                    )}
                                                                                </div>
                                                                            </div>
                                                                        );
                                                                    })
                                                                )}
                                                            </div>
                                                        )}
                                                    </div>
                                                );
                                            })
                                        )}
                                    </div>
                                </CardContent>
                            </Card>
                        </div>


                    </CardContent>
                </Card>
            )}

            {currentEditData && (
                <ForecastEditDialog
                    isOpen={isEditDialogOpen}
                    onClose={() => setIsEditDialogOpen(false)}
                    dayData={currentEditData}
                    onSave={handleSaveEdit}
                    jobRoles={jobRoles}
                    availableDates={availableDates}
                    currentDateIndex={currentDateIndex}
                    onNavigateDate={handleNavigateDate}
                    availableJobs={availableJobs}
                    currentJobIndex={currentJobIndex}
                    onNavigateJob={handleNavigateJob}
                    onUnsavedChangesWarning={handleUnsavedChangesWarning}
                />
            )}

            {/* Copy Schedule to Forecast Confirmation Dialog */}
            <Dialog open={showCopyConfirmation} onOpenChange={setShowCopyConfirmation}>
                <DialogContent className="sm:max-w-[425px]" data-qa-id="dialog-copy-confirmation">
                    <DialogHeader>
                        <DialogTitle>Copy Schedule to Forecast</DialogTitle>
                        <DialogDescription>
                            This will copy all scheduled hours from Legion data to replace the current forecast values for all job
                            titles and groups across all dates. This action can be undone until you save the changes.
                        </DialogDescription>
                    </DialogHeader>
                    <DialogFooter>
                        <Button
                            variant="outline"
                            onClick={() => setShowCopyConfirmation(false)}
                            data-qa-id="button-cancel-copy"
                        >
                            Cancel
                        </Button>
                        <Button
                            onClick={handleConfirmCopy}
                            data-qa-id="button-confirm-copy"
                        >
                            Continue
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>

            {/* Payroll Reconciliation Dashboard */}
            {!shouldShowHRISWarning() && (
                <PayrollReconciliationSection
                    payrollData={payrollData}
                    startingMonth={startingMonth}
                    isLoadingPayroll={isLoadingPayroll}
                    // Pass live data for real-time updates
                    forecastPayrollDetails={forecastPayrollDetails}
                    scheduledPayrollDetails={scheduledPayrollDetails}
                    actualPayrollDetails={actualPayrollDetails}
                    budgetPayrollDetails={budgetPayrollDetails}
                    jobCodes={jobCodes}
                />
            )}
        </div>
    );
});

/**
 * Separate component for Payroll Reconciliation Dashboard section
 * Uses the hook to manage data transformation and error handling
 * Matches the styling of Labor Hours & Cost Timeline section
 */
function PayrollReconciliationSection({
    payrollData,
    startingMonth,
    isLoadingPayroll,
    // Live data for real-time updates
    forecastPayrollDetails,
    scheduledPayrollDetails,
    actualPayrollDetails,
    budgetPayrollDetails,
    jobCodes
}: {
    payrollData: PayrollDto | null;
    startingMonth: string;
    isLoadingPayroll: boolean;
    // Live data parameters
    forecastPayrollDetails: PayrollDetailDto[];
    scheduledPayrollDetails: PayrollDetailDto[];
    actualPayrollDetails: PayrollDetailDto[];
    budgetPayrollDetails: PayrollDetailDto[];
    jobCodes: JobCode[];
}) {
    // Use live data if available, otherwise fall back to static payrollData
    const useLiveData = forecastPayrollDetails.length > 0 || scheduledPayrollDetails.length > 0;

    const livePayrollData = useLiveData ? {
        forecastDetails: forecastPayrollDetails,
        scheduledDetails: scheduledPayrollDetails,
        actualDetails: actualPayrollDetails,
        budgetDetails: budgetPayrollDetails,
        jobCodes: jobCodes,
        customerSiteId: payrollData?.customerSiteId || ''
    } : null;

    const { reconciliationData, originalDto, error } = usePayrollReconciliation(
        useLiveData ? livePayrollData : payrollData,
        startingMonth,
        useLiveData
    );

    return (
        <Card className="w-full relative mt-8">
            <CardHeader>
                <CardTitle>Payroll Reconciliation Dashboard</CardTitle>
            </CardHeader>
            <CardContent className="p-4">
                {isLoadingPayroll ? (
                    <Skeleton className="h-[400px] w-full" />
                ) : error ? (
                    <Alert className="border-red-200 bg-red-50 dark:border-red-800 dark:bg-red-950">
                        <AlertDescription className="text-red-700 dark:text-red-300">
                            Error loading reconciliation data: {error}
                        </AlertDescription>
                    </Alert>
                ) : reconciliationData.length > 0 ? (
                    <ReconciliationDashboard
                        data={reconciliationData}
                        payrollDto={originalDto}
                        availableJobGroups={useLiveData ? [...new Set(jobCodes.map(jc => jc.jobGroupName).filter(Boolean))] : undefined}
                        billingPeriod={startingMonth}
                        timeHorizon="monthly"
                        showComparison={true}
                        comparisonType="actual-vs-budget"
                    />
                ) : (
                    <div className="text-center py-8 text-muted-foreground">
                        No payroll data available for reconciliation analysis.
                    </div>
                )}
            </CardContent>
        </Card>
    );
}

export default PayrollForecast;
