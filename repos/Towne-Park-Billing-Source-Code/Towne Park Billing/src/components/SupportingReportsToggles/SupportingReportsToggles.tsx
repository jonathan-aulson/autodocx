import { Switch } from "@/components/ui/switch";
import { SupportingReportsType } from "@/lib/models/Contract";
import { useEffect, useState } from "react";

interface SupportingReportsProps {
    reportsState: Record<SupportingReportsType, boolean>;
    onReportTypeChange: (reportType: SupportingReportsType) => void;
    isFixedFeeEnabled: boolean;
    isPerLaborHourEnabled: boolean;
    isPerOccupiedRoomEnabled: boolean;
    isRevenueShareEnabled: boolean;
    isManagementAgreementEnabled: boolean;
    isBillablePayrollEnabled: boolean;
    isEditable: boolean;
    isBillingAdmin: boolean;
    isRevenueShareValidationEnabled?: boolean;
    isManagementAgreementValidationEnabled?: boolean;
}

export default function SupportingReports({
    reportsState,
    onReportTypeChange,
    isFixedFeeEnabled,
    isPerLaborHourEnabled,
    isPerOccupiedRoomEnabled,
    isRevenueShareEnabled,
    isManagementAgreementEnabled,
    isBillablePayrollEnabled,
    isEditable,
    isBillingAdmin,
    isRevenueShareValidationEnabled = false,
    isManagementAgreementValidationEnabled = false,
}: SupportingReportsProps) {
    const [internalState, setInternalState] = useState(reportsState);

    useEffect(() => {
        setInternalState(reportsState);
    }, [reportsState]);

    const handleReportTypeChange = (reportType: SupportingReportsType) => {
        if (!isEditable) return;

        const newValue = !internalState[reportType];
        setInternalState(prev => ({
            ...prev,
            [reportType]: newValue
        }));
        onReportTypeChange(reportType);
    };


    useEffect(() => {
        if (isBillingAdmin) {
            return;
        }

        const updates: SupportingReportsType[] = [];

        const validationEnabled = (isRevenueShareEnabled && isRevenueShareValidationEnabled) || 
                                (isManagementAgreementEnabled && isManagementAgreementValidationEnabled);

        if (!validationEnabled && internalState[SupportingReportsType.VALIDATION_REPORT]) {
            updates.push(SupportingReportsType.VALIDATION_REPORT);
        }

        if ((!isRevenueShareEnabled && !isManagementAgreementEnabled) && 
            internalState[SupportingReportsType.VALIDATION_REPORT]) {
            updates.push(SupportingReportsType.VALIDATION_REPORT);
        }

        if (!isPerLaborHourEnabled && internalState[SupportingReportsType.HOURS_BACKUP_REPORT]) {
            updates.push(SupportingReportsType.HOURS_BACKUP_REPORT);
        }

        if (!isManagementAgreementEnabled || !isBillablePayrollEnabled) {
            if (internalState[SupportingReportsType.LABOR_DISTRIBUTION_REPORT]) {
                updates.push(SupportingReportsType.LABOR_DISTRIBUTION_REPORT);
            }
            if (internalState[SupportingReportsType.OTHER_EXPENSES]) {
                updates.push(SupportingReportsType.OTHER_EXPENSES);
            }
        }

        if (!isManagementAgreementEnabled && !isRevenueShareEnabled) {
            if (internalState[SupportingReportsType.PARKING_DEPARTMENT_REPORT]) {
                updates.push(SupportingReportsType.PARKING_DEPARTMENT_REPORT);
            }
            if (internalState[SupportingReportsType.VALIDATION_REPORT]) {
                updates.push(SupportingReportsType.VALIDATION_REPORT);
            }
        }

        const anyFeatureEnabled = isFixedFeeEnabled || isPerLaborHourEnabled ||
            isPerOccupiedRoomEnabled || isRevenueShareEnabled || isManagementAgreementEnabled;

        if (!anyFeatureEnabled) {
            if (internalState[SupportingReportsType.MIX_OF_SALES]) {
                updates.push(SupportingReportsType.MIX_OF_SALES);
            }
            if (internalState[SupportingReportsType.TAX_REPORT]) {
                updates.push(SupportingReportsType.TAX_REPORT);
            }
        }

        if (updates.length > 0) {
            setInternalState(prev => {
                const newState = { ...prev };
                updates.forEach(reportType => {
                    newState[reportType] = false;
                });
                return newState;
            });

            updates.forEach(reportType => {
                onReportTypeChange(reportType);
            });
        }
    }, [
        isFixedFeeEnabled,
        isPerLaborHourEnabled,
        isPerOccupiedRoomEnabled,
        isRevenueShareEnabled,
        isManagementAgreementEnabled,
        isBillablePayrollEnabled,
        isRevenueShareValidationEnabled,
        isManagementAgreementValidationEnabled,
        internalState[SupportingReportsType.VALIDATION_REPORT],
        isBillingAdmin
    ]);

    return (
        <div className="space-y-4">
        <div className="flex flex-row items-center justify-between">
            <div className="space-y-0.5">
                <div className="font-medium">Hours Backup Report</div>
                <div className="text-sm text-muted-foreground flex flex-col">
                    <span>Available For: Per Labor Hour</span>
                </div>
            </div>
            <Switch
                data-qa-id="switch-hoursBackupReport-reportSettings"
                checked={internalState[SupportingReportsType.HOURS_BACKUP_REPORT]}
                onCheckedChange={() => handleReportTypeChange(SupportingReportsType.HOURS_BACKUP_REPORT)}
                disabled={!isEditable || (!isPerLaborHourEnabled && !isBillingAdmin)}
            />
        </div>

        <div className="flex flex-row items-center justify-between">
            <div className="space-y-0.5">
                <div className="font-medium">Labor Distribution Report</div>
                <div className="text-sm text-muted-foreground flex flex-col">
                    <span>Available For: Management Agreement</span>
                </div>
            </div>
            <Switch
                data-qa-id="switch-laborDistributionReport-reportSettings"
                checked={internalState[SupportingReportsType.LABOR_DISTRIBUTION_REPORT]}
                onCheckedChange={() => handleReportTypeChange(SupportingReportsType.LABOR_DISTRIBUTION_REPORT)}
                disabled={!isEditable || (!isManagementAgreementEnabled && !isBillingAdmin)}
            />
        </div>

        <div className="flex flex-row items-center justify-between">
            <div className="space-y-0.5">
        
                 <div className="font-medium">Mix of Sales Report</div> 
                <div className="text-sm text-muted-foreground flex flex-col">
                    <span>Available For: Fixed Fee, Per Labor Hour, Occupied Room, Revenue Share, Management Agreement</span>
                </div>
            </div>
            <Switch
                data-qa-id="switch-mixOfSalesReport-reportSettings"
                checked={internalState[SupportingReportsType.MIX_OF_SALES]}
                onCheckedChange={() => handleReportTypeChange(SupportingReportsType.MIX_OF_SALES)}
                disabled={!isEditable || (!isBillingAdmin && !isFixedFeeEnabled && !isPerLaborHourEnabled && !isPerOccupiedRoomEnabled && !isRevenueShareEnabled && !isManagementAgreementEnabled)}
            />
        </div>

        <div className="flex flex-row items-center justify-between">
            <div className="space-y-0.5">
                 <div className="font-medium">Other Expenses Report</div>
                <div className="text-sm text-muted-foreground flex flex-col">
                    <span>Available For: Management Agreement</span>
                </div>
            </div>
            <Switch
                data-qa-id="switch-otherExpensesReport-reportSettings"
                checked={internalState[SupportingReportsType.OTHER_EXPENSES]}
                onCheckedChange={() => handleReportTypeChange(SupportingReportsType.OTHER_EXPENSES)}
                disabled={!isEditable || (!isBillingAdmin && !isManagementAgreementEnabled)}
            />
        </div>

        <div className="flex flex-row items-center justify-between">
            <div className="space-y-0.5">
                <div className="font-medium">Tax Report</div>
                <div className="text-sm text-muted-foreground flex flex-col">
                    <span>Available For: Fixed Fee, Per Labor Hour, Occupied Room, Revenue Share, Management Agreement</span>
                </div>
            </div>
            <Switch
                data-qa-id="switch-taxReport-reportSettings"
                checked={internalState[SupportingReportsType.TAX_REPORT]}
                onCheckedChange={() => handleReportTypeChange(SupportingReportsType.TAX_REPORT)}
                disabled={!isEditable || (!isBillingAdmin && !isFixedFeeEnabled && !isPerLaborHourEnabled && !isPerOccupiedRoomEnabled && !isRevenueShareEnabled && !isManagementAgreementEnabled)}
            />
        </div>

        <div className="flex flex-row items-center justify-between">
            <div className="space-y-0.5">
                <div className="font-medium">Parking Department Report</div>
                <div className="text-sm text-muted-foreground flex flex-col">
                    <span>Available For: Management Agreement, Revenue Share</span>
                </div>
            </div>
            <Switch
                data-qa-id="switch-parkingDepartmentReport-reportSettings"
                checked={internalState[SupportingReportsType.PARKING_DEPARTMENT_REPORT]}
                onCheckedChange={() => handleReportTypeChange(SupportingReportsType.PARKING_DEPARTMENT_REPORT)}
                disabled={!isEditable || (!isBillingAdmin && !isManagementAgreementEnabled && !isRevenueShareEnabled)}
            />
        </div>

        <div className="flex flex-row items-center justify-between">
            <div className="space-y-0.5">
                <div className="font-medium">Validation Report</div>
                <div className="text-sm text-muted-foreground flex flex-col">
                    <span>Available For: Revenue Share, Management Agreement (Profit Share)</span>
                </div>
            </div>
            <Switch
                data-qa-id="switch-validationReport-reportSettings"
                checked={internalState[SupportingReportsType.VALIDATION_REPORT]}
                onCheckedChange={() => handleReportTypeChange(SupportingReportsType.VALIDATION_REPORT)}
                disabled={!isEditable || (!isBillingAdmin && !(
                    (isRevenueShareEnabled && isRevenueShareValidationEnabled) ||
                    (isManagementAgreementEnabled && isManagementAgreementValidationEnabled)
                ))}
            />
        </div>

    </div >
    );
}