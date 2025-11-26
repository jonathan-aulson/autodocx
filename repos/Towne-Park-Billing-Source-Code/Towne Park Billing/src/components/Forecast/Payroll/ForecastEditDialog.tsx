import { Button } from "@/components/ui/button"
import {
    Dialog,
    DialogContent,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Slider } from "@/components/ui/slider"
import { Switch } from "@/components/ui/switch"
import { formatCurrency } from "@/lib/utils"
import { ChevronLeft, ChevronRight } from "lucide-react"
import { useEffect, useState, useRef } from "react"

export interface ForecastDayData {
    date: string
    dateObj: Date
    displayDate: string
    forecastHours: number
    scheduledHours: number
    scheduledCost: number
    jobCode?: string
    jobName?: string
    hourlyRate?: number
    budgetHours?: number
    budgetCost?: number
    actualHours?: number
    actualCost?: number
    
    // Enhanced properties for business logic
    contractType?: "Standard" | "PerLaborHour"
    editLevel?: "JobGroup" | "JobTitle"
    
    // Job Group context (Standard sites)
    jobGroup?: {
        id: string
        name: string
        averageHourlyRate: number
        jobCodes: Array<{
            jobCodeId: string
            jobCode: string
            displayName: string
            activeEmployeeCount: number
            averageHourlyRate?: number
        }>
    }
    
    // Job Title context (PLH sites)
    jobTitle?: {
        jobCodeId: string
        jobCode: string
        displayName: string
        hourlyRate: number
        billableRates?: {
            rate: number          // Regular billable rate
            overtimeRate: number  // Overtime billable rate
        }
    }
}

export interface ForecastEditDialogProps {
    isOpen: boolean
    onClose: () => void
    dayData: ForecastDayData
    onSave: (updatedData: ForecastDayData) => void
    jobRoles: Array<{ jobCode: string, displayName: string, hourlyRate: number }>
    
    // Navigation props
    availableDates?: Date[]
    currentDateIndex?: number
    onNavigateDate?: (direction: 'prev' | 'next') => void
    
    availableJobs?: Array<{
        id: string
        name: string
        type: 'jobGroup' | 'jobCode'
    }>
    currentJobIndex?: number
    onNavigateJob?: (direction: 'prev' | 'next') => void
    
    hasUnsavedChanges?: boolean
    onUnsavedChangesWarning?: () => Promise<boolean> // Returns true to proceed
}

export default function ForecastEditDialog({
    isOpen,
    onClose,
    dayData,
    onSave,
    jobRoles,
    availableDates,
    currentDateIndex,
    onNavigateDate,
    availableJobs,
    currentJobIndex,
    onNavigateJob,
    hasUnsavedChanges,
    onUnsavedChangesWarning
}: ForecastEditDialogProps) {
    const [hours, setHours] = useState(0)
    /*
     * We need to ensure the dialog stays open when the user has unsaved changes
     * and chooses to navigate (next/previous date/job). While the unsaved-changes
     * toast is displayed the user clicks a button that is technically outside of
     * the dialog. Radix’s Dialog fires onOpenChange(false) for that interaction,
     * which would normally invoke handleCloseAttempt and close the dialog.
     *
     * To prevent this, we keep track of when a navigation operation is active.
     * While that flag is set, handleCloseAttempt will ignore the open change
     * event so the dialog remains visible after the user elects to save or
     * discard their edits.
     */
    const navigationInProgress = useRef(false)
    const [enableRateOverride, setEnableRateOverride] = useState(false)
    const [overrideHourlyRate, setOverrideHourlyRate] = useState<number | undefined>(undefined)
    const [isDirty, setIsDirty] = useState(false)
    const [isClosing, setIsClosing] = useState(false)

    // Track previous date and job to detect navigation
    const [prevDate, setPrevDate] = useState<string | undefined>();
    const [prevJobIdentifier, setPrevJobIdentifier] = useState<string | undefined>();
    
    // Track edited values per job to preserve edits when navigating between jobs
    const [editedValuesPerJob, setEditedValuesPerJob] = useState<Map<string, { 
        hours: number; 
        overrideRate?: number;
        jobContext: ForecastDayData; // Store full context for proper saving
    }>>(new Map());
    
    // Track original backend state for each job to enable proper cancellation
    const [originalBackendState, setOriginalBackendState] = useState<Map<string, ForecastDayData>>(new Map());
    
    // Snapshot of original local values for the current job/date context
    const originalRef = useRef<{ hours: number; enableRateOverride: boolean; overrideHourlyRate?: number } | null>(null)
    
    // Determine dialog type based on contractType and editLevel (moved up to avoid dependency issues)
    const isJobGroupDialog = dayData?.contractType === "Standard" && dayData?.editLevel === "JobGroup"
    const isJobCodeDialog = dayData?.contractType === "PerLaborHour" && dayData?.editLevel === "JobTitle"
    
   useEffect(() => {
    if (isOpen && dayData) {
        // Create a unique identifier for the current job
        const currentJobIdentifier = isJobGroupDialog 
            ? `${dayData.date}_group_${dayData.jobGroup?.id}` 
            : isJobCodeDialog 
                ? `${dayData.date}_code_${dayData.jobTitle?.jobCodeId}` 
                : `${dayData.date}_job_${dayData.jobCode}`;
                
        // Check if we navigated to a new date or job
        const navigatedToNewDate = prevDate && prevDate !== dayData.date;
        const navigatedToNewJob = prevJobIdentifier && prevJobIdentifier !== currentJobIdentifier;
        const hasNavigated = navigatedToNewDate || navigatedToNewJob;
        
        // Auto-save current edits when navigating away from a job with unsaved changes
        if (hasNavigated && isDirty && prevJobIdentifier) {
            // Get the previous job context from our stored edits, or use current dayData as fallback
            const existingEdit = editedValuesPerJob.get(prevJobIdentifier);
            const jobContext = existingEdit?.jobContext || dayData;
            
            // Save the current edits to the backend immediately using the correct context
            const updatedData = {
                ...jobContext,
                forecastHours: hours,
            };
            onSave(updatedData);
            
            // Update the edited values map with the new values and context
            const currentEdits = {
                hours: hours,
                overrideRate: enableRateOverride ? overrideHourlyRate : undefined,
                jobContext: jobContext
            };
            setEditedValuesPerJob(prev => new Map(prev.set(prevJobIdentifier, currentEdits)));
        }
        
        // Reset dirty state when navigating to new date/job
        if (hasNavigated && isDirty) {
            setIsDirty(false);
        }
        
        // Update data when:
        // 1. Dialog just opened (no prevJobIdentifier)
        // 2. Navigated to new date/job
        // 3. Just became not dirty (after save/discard)
        if (!prevJobIdentifier || hasNavigated || (!isDirty && prevJobIdentifier === currentJobIdentifier)) {
            // Store original backend state for this job if we haven't seen it before
            if (!originalBackendState.has(currentJobIdentifier)) {
                setOriginalBackendState(prev => new Map(prev.set(currentJobIdentifier, { ...dayData })));
            }

            // Baseline hours snapshot from dayData (original state when entering this context)
            const baselineHours = dayData.forecastHours !== undefined ? dayData.forecastHours : (dayData.scheduledHours || 0)
            
            // Capture original snapshot for this context (hours + rate override fields)
            originalRef.current = {
                hours: baselineHours,
                enableRateOverride: false,
                overrideHourlyRate: undefined,
            }

            // Check if we have edited values for this job
            const editedValues = editedValuesPerJob.get(currentJobIdentifier);
            
            if (editedValues) {
                // Use edited values
                setHours(editedValues.hours);
                if (editedValues.overrideRate !== undefined) {
                    setEnableRateOverride(true);
                    setOverrideHourlyRate(editedValues.overrideRate);
                } else {
                    setEnableRateOverride(false);
                    setOverrideHourlyRate(undefined);
                }
            } else {
                // Use original values
                const initialHours = dayData.forecastHours !== undefined ? dayData.forecastHours : dayData.scheduledHours || 0;
                setHours(initialHours);
                setEnableRateOverride(false);
                setOverrideHourlyRate(undefined);
            }
        }
        
        // Update previous date and job identifier
        setPrevDate(dayData.date);
        setPrevJobIdentifier(currentJobIdentifier);
    }
}, [isOpen, dayData, isDirty, prevDate, prevJobIdentifier, isJobGroupDialog, isJobCodeDialog, hours, enableRateOverride, overrideHourlyRate, editedValuesPerJob])

    // Separate effect for dialog open/close to reset state
    useEffect(() => {
    if (isOpen) {
        // Reset dirty flag when dialog opens
        setIsDirty(false)
     
        setIsClosing(false)
        // Reset rate override only when dialog initially opens
        if (!dayData) {
            setEnableRateOverride(false)
            setOverrideHourlyRate(undefined)
        }
    } else {
        // Reset tracking when dialog closes
        setPrevDate(undefined);
        setPrevJobIdentifier(undefined);
        setEditedValuesPerJob(new Map()); // Clear all edited values when dialog closes
        
        setIsClosing(false)
    }
}, [isOpen])


    if (!dayData) return null

    const getEditType = () => {
        return isJobGroupDialog ? "jobGroup" : "jobCode"
    }

    const getDisplayName = () => {
        if (isJobGroupDialog) {
            return dayData.jobGroup?.name || "Unknown Job Group"
        } else if (isJobCodeDialog) {
            return `${dayData.jobTitle?.displayName || dayData.jobCode || "Unknown Job Title/Job Code"}`
        }
        return dayData.jobName || dayData.jobCode || "Unknown Job Title/Job Code"
    }

    const getDefaultHourlyRate = () => {
        if (isJobGroupDialog) {
            return dayData.jobGroup?.averageHourlyRate || 15
        } else if (isJobCodeDialog) {
            return dayData.jobTitle?.hourlyRate || 15
        }
        return dayData.hourlyRate || 15
    }

    const defaultHourlyRate = getDefaultHourlyRate()
    const hourlyRate = enableRateOverride && overrideHourlyRate !== undefined ? overrideHourlyRate : defaultHourlyRate

    // Calculate values
    const currentValue = dayData.scheduledHours || 0
    const currentCost = currentValue * defaultHourlyRate
    const budgetValue = dayData.budgetHours
    const budgetCost = budgetValue ? budgetValue * defaultHourlyRate : undefined
    const maxValue = dayData.actualHours // This represents actual hours for job code dialogs
    const newCost = hours * hourlyRate

    const formatNumber = (value: number) => value.toFixed(1)

    const storeJobContextIfNeeded = () => {
        // Store the current job context when user first makes an edit
        const currentJobIdentifier = isJobGroupDialog 
            ? `${dayData.date}_group_${dayData.jobGroup?.id}` 
            : isJobCodeDialog 
                ? `${dayData.date}_code_${dayData.jobTitle?.jobCodeId}` 
                : `${dayData.date}_job_${dayData.jobCode}`;
        
        if (!editedValuesPerJob.has(currentJobIdentifier)) {
            const currentEdits = {
                hours: dayData.forecastHours !== undefined ? dayData.forecastHours : dayData.scheduledHours || 0,
                overrideRate: undefined,
                jobContext: dayData
            };
            setEditedValuesPerJob(prev => new Map(prev.set(currentJobIdentifier, currentEdits)));
        }
    };

    const handleValueChange = (value: number[]) => {
        storeJobContextIfNeeded();
        setHours(value[0])
        setIsDirty(true)
    }

    const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        storeJobContextIfNeeded();
        const value = parseFloat(e.target.value) || 0
        setHours(value)
        setIsDirty(true)
    }

    const handleOverrideRateChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        storeJobContextIfNeeded();
        const value = parseFloat(e.target.value) || 0
        setOverrideHourlyRate(value)
        setIsDirty(true)
    }

    const handleResetToBudget = () => {
        if (budgetValue !== undefined) {
            setHours(budgetValue)
            setIsDirty(true)
        }
    }
    
    // Helper to restore local state back to the original snapshot
    const resetToOriginal = () => {
        if (originalRef.current) {
            setHours(originalRef.current.hours)
            setEnableRateOverride(originalRef.current.enableRateOverride)
            setOverrideHourlyRate(originalRef.current.overrideHourlyRate)
        }
        setIsDirty(false)
    }
    
    // Helper to revert all backend changes made during this dialog session
    const revertAllBackendChanges = () => {
        originalBackendState.forEach((originalData: ForecastDayData, jobIdentifier: string) => {
            onSave(originalData);
        });
    }
    
    // Helper to check if there are any unsaved changes
    const checkForUnsavedChanges = () => {
        return isDirty || editedValuesPerJob.size > 0 || 
            (originalRef.current && (
                hours !== originalRef.current.hours ||
                enableRateOverride !== originalRef.current.enableRateOverride ||
                overrideHourlyRate !== originalRef.current.overrideHourlyRate
            ));
    }

    const handleUpdate = (closeAfterSave: boolean = true) => {
        // Save the current job with current values
        const updatedData = {
            ...dayData,
            forecastHours: hours,
        }
        onSave(updatedData)
        
        // Update the edited values map for the current job
        const currentJobIdentifier = isJobGroupDialog 
            ? `${dayData.date}_group_${dayData.jobGroup?.id}` 
            : isJobCodeDialog 
                ? `${dayData.date}_code_${dayData.jobTitle?.jobCodeId}` 
                : `${dayData.date}_job_${dayData.jobCode}`;
        
        const currentEdits = {
            hours: hours,
            overrideRate: enableRateOverride ? overrideHourlyRate : undefined,
            jobContext: dayData
        };
        setEditedValuesPerJob(prev => new Map(prev.set(currentJobIdentifier, currentEdits)));
        
        // Apply any other unsaved edits across all jobs/dates
        editedValuesPerJob.forEach((edits, jobId) => {
            if (jobId !== currentJobIdentifier) {
                // Apply edits for other jobs that haven't been saved yet
                const otherJobData = {
                    ...edits.jobContext,
                    forecastHours: edits.hours,
                };
                if (edits.overrideRate !== undefined) {
                    // Apply rate override if specified
                    if (otherJobData.jobTitle) {
                        otherJobData.jobTitle.hourlyRate = edits.overrideRate;
                    } else if (otherJobData.jobGroup) {
                        otherJobData.jobGroup.averageHourlyRate = edits.overrideRate;
                    }
                }
                onSave(otherJobData);
            }
        });
        
        setIsDirty(false)
        setEditedValuesPerJob(new Map()) // Clear all edits after applying everything
        if (closeAfterSave) {
            onClose()
        }
    }

const handleCloseAttempt = async () => {

    if (isClosing) {
        return;
    }
    
    // If a navigation is in progress, ignore this close attempt so the
    // dialog stays open. Radix fires onOpenChange(false) when the user
    // clicks the toast buttons which are outside the dialog, but in this
    // scenario we explicitly want to keep the dialog open.
    if (navigationInProgress.current) {
        return;
    }
    
    if (checkForUnsavedChanges() && onUnsavedChangesWarning) {
   
        setIsClosing(true);
        try {
            const shouldProceed = await onUnsavedChangesWarning();
            if (shouldProceed) {
                handleUpdate(true); // Save and close
            } else {
                // Discard changes: revert all backend changes and reset local state
                revertAllBackendChanges();
                resetToOriginal();
                setEditedValuesPerJob(new Map()); // Clear all edited values when discarding
                setOriginalBackendState(new Map()); // Clear original backend state
                onClose();
            }
        } finally {
            setIsClosing(false);
        }
    } else {
        // No changes: still ensure local state reflects original baseline and clear tracking
        resetToOriginal();
        setEditedValuesPerJob(new Map());
        setOriginalBackendState(new Map());
        onClose(); // No changes, just close
    }
};


    const handleNavigation = async (navigationFn: () => void) => {
        navigationInProgress.current = true
        
        if (checkForUnsavedChanges() && onUnsavedChangesWarning) {
            const shouldSave = await onUnsavedChangesWarning!();
            if (shouldSave) {
                // Save changes without closing
                handleUpdate(false);
                // Navigate immediately after save
                navigationFn();
            } else {
                // Discard changes - revert all backend changes and reset local state
                revertAllBackendChanges();
                resetToOriginal();
                setEditedValuesPerJob(new Map()); // Clear all edited values when discarding
                setOriginalBackendState(new Map()); // Clear original backend state
                // Navigate after discarding changes
                navigationFn();
            }
        } else {
            navigationFn();
        }
        navigationInProgress.current = false
    }

  const handleNavigateDateClick = (direction: 'prev' | 'next') => {
    onNavigateDate?.(direction)
}

const handleNavigateJobClick = (direction: 'prev' | 'next') => {
    onNavigateJob?.(direction)
}
    

    // Keyboard navigation
    useEffect(() => {
        if (!isOpen) return

        const handleKeyDown = (e: KeyboardEvent) => {
            // Don't handle navigation if user is typing in an input
            if (e.target instanceof HTMLInputElement || e.target instanceof HTMLTextAreaElement) {
                return
            }

            switch (e.key) {
                case 'ArrowLeft':
                    e.preventDefault()
                    if (onNavigateDate && currentDateIndex !== undefined && currentDateIndex > 0) {
                        handleNavigateDateClick('prev')
                    }
                    break
                case 'ArrowRight':
                    e.preventDefault()
                    if (onNavigateDate && currentDateIndex !== undefined && availableDates && currentDateIndex < availableDates.length - 1) {
                        handleNavigateDateClick('next')
                    }
                    break
                case 'ArrowUp':
                    e.preventDefault()
                    if (onNavigateJob && currentJobIndex !== undefined && currentJobIndex > 0) {
                        handleNavigateJobClick('prev')
                    }
                    break
                case 'ArrowDown':
                    e.preventDefault()
                    if (onNavigateJob && currentJobIndex !== undefined && availableJobs && currentJobIndex < availableJobs.length - 1) {
                        handleNavigateJobClick('next')
                    }
                    break
            }
        }

        window.addEventListener('keydown', handleKeyDown)
        return () => window.removeEventListener('keydown', handleKeyDown)
    }, [isOpen, onNavigateDate, onNavigateJob, currentDateIndex, currentJobIndex, availableDates, availableJobs, handleNavigateDateClick, handleNavigateJobClick])

    const maxSliderValue = 500
    const minValue = 0

    return (
        <Dialog open={isOpen} onOpenChange={(open) => {
            if (!open) {
                handleCloseAttempt();
            }
        }} data-qa-id="dialog-forecast-edit">
            <DialogContent className="sm:max-w-[700px]">
                <DialogHeader>
                    <DialogTitle className="break-words">Edit Forecast - {getEditType() === "jobGroup" ? "Job Group" : "Job Code"}</DialogTitle>
                    
                    {/* Navigation Controls */}
                    <div className="flex flex-col gap-3 mt-4">
                        {/* Date Navigation */}
                        <div className="flex items-center justify-center gap-1">
                            <Button
                                variant="ghost"
                                size="icon"
                                onClick={() => handleNavigateDateClick('prev')}
                                disabled={!onNavigateDate || currentDateIndex === 0}
                                className="h-8 w-8 flex-shrink-0"
                                data-qa-id="button-navigate-date-prev"
                            >
                                <ChevronLeft className="h-4 w-4" />
                            </Button>
                            <span className="text-sm font-medium px-2 text-center min-w-[120px]">
                                {dayData.displayDate}
                            </span>
                            <Button
                                variant="ghost"
                                size="icon"
                                onClick={() => handleNavigateDateClick('next')}
                                disabled={!onNavigateDate || (currentDateIndex !== undefined && availableDates && currentDateIndex >= availableDates.length - 1)}
                                className="h-8 w-8 flex-shrink-0"
                                data-qa-id="button-navigate-date-next"
                            >
                                <ChevronRight className="h-4 w-4" />
                            </Button>
                        </div>
                        
                        {/* Job Navigation */}
                        <div className="flex items-center justify-center gap-1">
                            <Button
                                variant="ghost"
                                size="icon"
                                onClick={() => handleNavigateJobClick('prev')}
                                disabled={!onNavigateJob || currentJobIndex === 0}
                                className="h-8 w-8 flex-shrink-0"
                                data-qa-id="button-navigate-job-prev"
                            >
                                <ChevronLeft className="h-4 w-4" />
                            </Button>
                            <div className="flex-1 min-w-0 px-2">
                                <span className="text-sm font-medium text-center block break-words leading-tight">
                                    {getDisplayName()}
                                </span>
                            </div>
                            <Button
                                variant="ghost"
                                size="icon"
                                onClick={() => handleNavigateJobClick('next')}
                                disabled={!onNavigateJob || (currentJobIndex !== undefined && availableJobs && currentJobIndex >= availableJobs.length - 1)}
                                className="h-8 w-8 flex-shrink-0"
                                data-qa-id="button-navigate-job-next"
                            >
                                <ChevronRight className="h-4 w-4" />
                            </Button>
                        </div>
                    </div>
                    
                    {/* Keyboard shortcuts hint */}
                    <div className="text-xs text-muted-foreground text-center mt-2">
                        Use arrow keys: ← → for dates, ↑ ↓ for jobs
                    </div>
                </DialogHeader>
                <div className="grid gap-4 py-4">
                    <div className="space-y-2">
                        <div className="flex items-center justify-between">
                            <p className="text-xs text-muted-foreground">
                                Default Hourly Rate: {formatCurrency(defaultHourlyRate)}
                            </p>
                            {enableRateOverride && overrideHourlyRate !== undefined && (
                                <p className="text-xs font-medium text-green-600">
                                    Using Override Rate: {formatCurrency(overrideHourlyRate)}
                                </p>
                            )}
                        </div>
                    </div>

                    <div className="space-y-4">
                        <div className="space-y-2">
                            <label className="text-sm font-medium">Hours Forecast:</label>
                            <div className="flex items-center space-x-2">
                                <Slider
                                    value={[hours]}
                                    max={maxSliderValue}
                                    min={minValue}
                                    step={0.5}
                                    onValueChange={handleValueChange}
                                    className="flex-1"
                                    data-qa-id="slider-hours-forecast"
                                />
                                <Input 
                                    type="number" 
                                    value={hours} 
                                    onChange={handleInputChange} 
                                    className="w-24" 
                                    step={0.5}
                                    data-qa-id="input-hours-forecast"
                                />
                            </div>
                        </div>

                        <div className="flex items-center space-x-2 pt-2">
                            <Switch 
                                id="override-rate" 
                                checked={enableRateOverride} 
                                onCheckedChange={(checked) => {
                                    setEnableRateOverride(checked)
                                    setIsDirty(true)
                                }}
                                data-qa-id="switch-override-rate"
                            />
                            <Label htmlFor="override-rate" className="text-sm">
                                Override Hourly Cost
                            </Label>
                        </div>

                        {enableRateOverride && (
                            <div className="space-y-2">
                                <label className="text-sm font-medium break-words">Override Hourly Cost from {dayData.displayDate} forward:</label>
                                <div className="flex items-center space-x-2">
                                    <span className="text-sm">$</span>
                                    <Input
                                        type="number"
                                        value={overrideHourlyRate !== undefined ? overrideHourlyRate : ""}
                                        onChange={handleOverrideRateChange}
                                        className="w-24"
                                        step={0.01}
                                        placeholder={defaultHourlyRate.toFixed(2)}
                                        data-qa-id="input-override-rate"
                                    />
                                    <span className="text-sm text-muted-foreground">per hour</span>
                                </div>
                                <p className="text-xs text-muted-foreground">
                                    This will apply to forecasting from {dayData.displayDate} until another override is set.
                                </p>
                            </div>
                        )}

                        <div className="grid grid-cols-2 gap-4 text-sm pt-2">
                            <div className="space-y-1">
                                <p className="text-muted-foreground">Current Hours:</p>
                                <p className="font-medium">{formatNumber(currentValue) + " hrs"}</p>
                            </div>
                            <div className="space-y-1">
                                <p className="text-muted-foreground">Current Cost:</p>
                                <p className="font-medium">{formatCurrency(currentCost)}</p>
                            </div>

                            {budgetValue !== undefined && (
                                <>
                                    <div className="space-y-1">
                                        <p className="text-muted-foreground">Budget Hours:</p>
                                        <p className="font-medium">{formatNumber(budgetValue) + " hrs"}</p>
                                    </div>
                                    <div className="space-y-1">
                                        <p className="text-muted-foreground">Budget Cost:</p>
                                        <p className="font-medium">{formatCurrency(budgetCost || 0)}</p>
                                    </div>
                                </>
                            )}

                            {maxValue && isJobCodeDialog && (
                                <>
                                    <div className="space-y-1">
                                        <p className="text-muted-foreground">Actual Hours:</p>
                                        <p className="font-medium">{formatNumber(maxValue) + " hrs"}</p>
                                    </div>
                                    <div className="space-y-1">
                                        <p className="text-muted-foreground">Actual Cost:</p>
                                        <p className="font-medium">{formatCurrency(maxValue * hourlyRate)}</p>
                                    </div>
                                </>
                            )}

                            <div className="space-y-1">
                                <p className="text-muted-foreground">New Hours:</p>
                                <p className="font-medium text-green-600">{formatNumber(hours) + " hrs"}</p>
                            </div>
                            <div className="space-y-1">
                                <p className="text-muted-foreground">New Cost:</p>
                                <p className="font-medium text-green-600">{formatCurrency(newCost)}</p>
                            </div>
                        </div>

                        {budgetValue !== undefined && (
                            <div className="space-y-2">
                                <p className="text-sm text-muted-foreground">
                                    Variance from Budget:
                                    <span
                                        className={`ml-1 font-medium ${
                                            newCost > (budgetCost || 0)
                                                ? "text-red-600"
                                                : newCost < (budgetCost || 0)
                                                ? "text-green-600"
                                                : "text-gray-600"
                                        }`}
                                    >
                                        {budgetCost && budgetCost > 0 ? (((newCost - budgetCost) / budgetCost) * 100).toFixed(1) : 0}%
                                        {newCost > (budgetCost || 0) ? " over" : newCost < (budgetCost || 0) ? " under" : " on target"}
                                    </span>
                                </p>

                                <Button 
                                    variant="outline" 
                                    size="sm" 
                                    onClick={handleResetToBudget} 
                                    className="w-full"
                                    data-qa-id="button-reset-to-budget"
                                >
                                    Reset to Budget Value
                                </Button>
                            </div>
                        )}
                    </div>
                </div>
                <DialogFooter>
                    <Button variant="outline" onClick={handleCloseAttempt} data-qa-id="button-cancel">
                        Cancel
                    </Button>
                    <Button onClick={() => handleUpdate()} data-qa-id="button-update-forecast">
                        Apply to Forecast
                    </Button>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    )
}
