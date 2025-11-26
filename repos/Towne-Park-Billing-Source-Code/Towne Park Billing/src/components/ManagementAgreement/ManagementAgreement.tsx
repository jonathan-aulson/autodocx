import { Button } from "@/components/ui/button";
import {
    Card,
    CardContent,
    CardHeader,
    CardTitle,
} from "@/components/ui/card";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Switch } from "@/components/ui/switch";
import {
    BillablePayrollTerms,
    ClaimsType,
    EscalatorFormatType,
    ExpensetPayrollType,
    InsuranceType,
    InvoiceGroup,
    JobCode,
    ManagementAgreementTerms,
    ManagementAgreementType,
    ManagementFee,
    Month,
    NonGlBillableExpenseDto,
    NonGlExpensetype,
    PayrollDataItem,
    ProfitShareAccumulationType,
    Tier,
    ValidationThresholdType,
} from "@/lib/models/Contract";
import { Info, MinusCircle, PlusCircle, X } from "lucide-react";
import React, { useEffect, useRef, useState } from "react";
import { NumericFormat } from "react-number-format";
import { RadioGroup, RadioGroupItem } from "../ui/radio-group";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "../ui/tooltip";
import { useToast } from "../ui/use-toast";
import { useFormContext } from "react-hook-form";

const createDefaultManagementFee = (invoiceGroups: InvoiceGroup[]): ManagementFee => ({
    id: "",
    invoiceGroup: invoiceGroups.length ? invoiceGroups[0].groupNumber : 1,
    managementAgreementType: ManagementAgreementType.FIXED_FEE,
    managementFeeEscalatorEnabled: false,
    managementFeeEscalatorMonth: Month.JANUARY,
    managementFeeEscalatorType: EscalatorFormatType.PERCENTAGE,
    managementFeeEscalatorValue: null,
    fixedFeeAmount: null,
    perLaborHourJobCodeData: [],
    laborHourJobCode: null,
    laborHourRate: null,
    laborHourOvertimeRate: null,
    revenuePercentageAmount: null,
    insuranceEnabled: false,
    insuranceLineTitle: null,
    insuranceType: InsuranceType.BASED_ON_BILLABLE_ACCOUNTS,
    insuranceAdditionalPercentage: 0,
    insuranceFixedFeeAmount: 0,
    claimsEnabled: false,
    claimsType: ClaimsType.ANNUALLY_ANIVERSARY,
    claimsLineTitle: null,
    claimsCapAmount: null,
    profitShareEnabled: false,
    profitShareAccumulationType: ProfitShareAccumulationType.MONTHLY,
    profitShareTierData: [],
    profitShareEscalatorEnabled: false,
    profitShareEscalatorMonth: Month.JANUARY,
    profitShareEscalatorType: EscalatorFormatType.PERCENTAGE,
    validationThresholdEnabled: false,
    validationThresholdAmount: null,
    validationThresholdType: ValidationThresholdType.VALIDATION_AMOUNT,
    nonGlBillableExpensesEnabled: false,
    nonGlBillableExpenses: []
});

interface ManagementAgreementProps {
    managementAgreement: ManagementAgreementTerms;
    onUpdateManagementAgreement: (updatedAgreement: ManagementAgreementTerms) => void;
    invoiceGroups: InvoiceGroup[];
    watchInvoiceGroupingEnabled: boolean;
    billableAccounts: BillablePayrollTerms;
    errors: any;
    isEditable: boolean;
}

interface ManagementFeeSectionProps {
    fee: ManagementFee;
    index: number;
    invoiceGroups: InvoiceGroup[];
    watchInvoiceGroupingEnabled: boolean;
    onUpdate: (changes: Partial<ManagementFee>) => void;
    errors: any;
    isEditable: boolean;
    handleClearClick: () => void;
    isClearVisible: boolean;
    setIsClearVisible: React.Dispatch<React.SetStateAction<boolean>>;
}

interface InsuranceSectionProps {
    fee: ManagementFee;
    index: number;
    billableInsuranceAccounts: PayrollDataItem[];
    onUpdate: (changes: Partial<ManagementFee>) => void;
    errors: any;
    isEditable: boolean;
}

interface ProfitShareSectionProps {
    fee: ManagementFee;
    index: number;
    onUpdate: (changes: Partial<ManagementFee>) => void;
    errors: any;
    isEditable: boolean;
}

interface ClaimsSectionProps {
    fee: ManagementFee;
    index: number;
    claimsExpenseAccounts: PayrollDataItem[];
    onUpdate: (changes: Partial<ManagementFee>) => void;
    errors: any;
    isEditable: boolean;
}
interface NonGLBillableExpensesSectionProps {
    fee: ManagementFee;
    index: number;
    onUpdate: (changes: Partial<ManagementFee>) => void;
    errors: any;
    isEditable: boolean;
}
const ManagementAgreement: React.FC<ManagementAgreementProps> = ({
    managementAgreement,
    onUpdateManagementAgreement,
    invoiceGroups,
    watchInvoiceGroupingEnabled,
    billableAccounts,
    errors,
    isEditable
}) => {
  //  const {formState:{isSubmitting}} = useFormContext()
 //   const updateState = useRef(false)
    const [state, setState] = useState<ManagementAgreementTerms>(() => {
        const initialManagementFees = managementAgreement.ManagementFees.length > 0
            ? managementAgreement.ManagementFees.map(fee => ({
                ...fee,
                profitShareTierData: fee.profitShareTierData || [],
                profitShareAccumulationType: fee.profitShareAccumulationType || ProfitShareAccumulationType.MONTHLY,
                fixedFeeAmount: fee.fixedFeeAmount,
                perLaborHourJobCodeData: fee.perLaborHourJobCodeData || [],
                revenuePercentageAmount: fee.revenuePercentageAmount,
                laborHourJobCode: fee.laborHourJobCode,
                laborHourRate: fee.laborHourRate,
                laborHourOvertimeRate: fee.laborHourOvertimeRate,
                insuranceLineTitle: fee.insuranceLineTitle,
                claimsLineTitle: fee.claimsLineTitle,
            }))
            : [createDefaultManagementFee(invoiceGroups)];

        return {
            ...managementAgreement,
            ManagementFees: initialManagementFees,
            enabled: managementAgreement.enabled ?? false,
            profitShareAccumulationType: managementAgreement.ManagementFees[0]?.profitShareAccumulationType || ProfitShareAccumulationType.MONTHLY,
        };
    });

    const [isDirty, setIsDirty] = useState(false);
    const originalStateRef = useRef(state);
    
    useEffect(() => {
        if (!isEditable && isDirty) {
            setState(originalStateRef.current);
            setIsDirty(false);
        }
    }, [isEditable]);

    // useEffect(() => {
    //     if (isEditable && !updateState.current) {
    //         updateState.current = true
    //         setState(() => {
    //             const initialManagementFees = managementAgreement.ManagementFees.length
    //                 ? managementAgreement.ManagementFees.map(fee => ({
    //                     ...fee,
    //                     profitShareTierData: fee.profitShareTierData || [],
    //                     profitShareAccumulationType: fee.profitShareAccumulationType || ProfitShareAccumulationType.MONTHLY,
    //                     fixedFeeAmount: fee.fixedFeeAmount,
    //                     revenuePercentageAmount: fee.revenuePercentageAmount,
    //                     laborHourJobCode: fee.laborHourJobCode,
    //                     laborHourRate: fee.laborHourRate,
    //                     laborHourOvertimeRate: fee.laborHourOvertimeRate,
    //                     insuranceLineTitle: fee.insuranceLineTitle,
    //                     claimsLineTitle: fee.claimsLineTitle,
    //                 }))
    //                 : [createDefaultManagementFee(invoiceGroups)];

    //             return {
    //                 ...managementAgreement,
    //                 ManagementFees: initialManagementFees,
    //                 enabled: managementAgreement.enabled ?? false,
    //                 profitShareAccumulationType: managementAgreement.ManagementFees[0]?.profitShareAccumulationType || ProfitShareAccumulationType.MONTHLY,
    //             };
    //         });
    //     }
    // }, [managementAgreement,isEditable]);
    useEffect(() => {
        if (!isEditable) {
            setState(() => {
                const initialManagementFees = managementAgreement.ManagementFees.length
                    ? managementAgreement.ManagementFees.map(fee => ({
                        ...fee,
                        profitShareTierData: fee.profitShareTierData || [],
                        profitShareAccumulationType: fee.profitShareAccumulationType || ProfitShareAccumulationType.MONTHLY,
                        fixedFeeAmount: fee.fixedFeeAmount,
                        perLaborHourJobCodeData: fee.perLaborHourJobCodeData || [],
                        revenuePercentageAmount: fee.revenuePercentageAmount,
                        laborHourJobCode: fee.laborHourJobCode,
                        laborHourRate: fee.laborHourRate,
                        laborHourOvertimeRate: fee.laborHourOvertimeRate,
                        insuranceLineTitle: fee.insuranceLineTitle,
                        claimsLineTitle: fee.claimsLineTitle,
                    }))
                    : [createDefaultManagementFee(invoiceGroups)];

                return {
                    ...managementAgreement,
                    ManagementFees: initialManagementFees,
                    enabled: managementAgreement.enabled ?? false,
                    profitShareAccumulationType: managementAgreement.ManagementFees[0]?.profitShareAccumulationType || ProfitShareAccumulationType.MONTHLY,
                };
            });
        }
    }, [managementAgreement,isEditable]);

    const [selectedInvoiceGroup, setSelectedInvoiceGroup] = useState<string>(
        invoiceGroups.length > 0 ? invoiceGroups[0].groupNumber.toString() : "1"
    );
    // useEffect(()=>{
    //     return()=>{
    //         updateState.current =false
    //     }
    // },[isSubmitting])
    useEffect(() => {
        if (!billableAccounts.enabled) {
            setState(prev => {
                const updatedState = { ...prev, enabled: false };
                onUpdateManagementAgreement(updatedState);
                return updatedState;
            });
        }
    }, [billableAccounts.enabled, onUpdateManagementAgreement]);

    useEffect(() => {
        if (billableAccounts.enabled && state.enabled) {
            onUpdateManagementAgreement(state);
        }
    }, [billableAccounts.enabled]);

    const updateManagementFee = (index: number, changes: Partial<ManagementFee>) => {
        setIsDirty(true);
        setState(prev => ({
            ...prev,
            ManagementFees: prev.ManagementFees.map((fee, i) =>
                i === index ? {
                    ...fee,
                    ...changes,
                    profitShareTierData: changes.profitShareTierData
                        ? (changes.profitShareTierData)
                        : fee.profitShareTierData
                } : fee
            ),
        }));
        onUpdateManagementAgreement({
            ...state,
            ManagementFees: state.ManagementFees.map((fee, i) =>
                i === index ? {
                    ...fee,
                    ...changes,
                    profitShareTierData: changes.profitShareTierData
                        ? (changes.profitShareTierData)
                        : fee.profitShareTierData
                } : fee
            ),
        });
    };

    const { toast } = useToast();

    const billableInsuranceAccounts: PayrollDataItem[] = billableAccounts.billableAccountsData
        .flatMap((data) => {
            try {
                return JSON.parse(data.payrollExpenseAccountsData) as PayrollDataItem[];
            } catch (error) {
                console.error("Failed to parse payrollExpenseAccountsData:", error);
                return [];
            }
        })
        .filter((account) => [7080, 7082, 7085].includes(parseInt(account.code, 10)) && account.isEnabled);

    const claimsExpenseAccounts: PayrollDataItem[] = billableAccounts.billableAccountsData
        .flatMap((data) => {
            try {
                return JSON.parse(data.payrollExpenseAccountsData) as PayrollDataItem[];
            } catch (error) {
                console.error("Failed to parse payrollExpenseAccountsData:", error);
                return [];
            }
        })
        .filter((account) => [7099, 7100, 7101, 7102].includes(parseInt(account.code, 10)) && account.isEnabled);

    const toggleManagementAgreementEnabled = (enabled: boolean) => {
        setIsDirty(true);
        if (!billableAccounts.enabled && enabled) {
            toast({
                title: "Warning",
                description: "Management Agreement cannot be enabled if Billable Accounts is disabled.",
            });
            return;
        }
        const updatedState = { ...state, enabled };
        setState(updatedState);
        onUpdateManagementAgreement(updatedState);
    };
    const [isClearVisible, setIsClearVisible] = useState(false);
    const handleClear = () => {
        const clearedState = {
            ...state,
            ManagementFees: [createDefaultManagementFee(invoiceGroups)],
        };
        setState(clearedState);
        onUpdateManagementAgreement(clearedState);
    };


    const handleClearClick = () => {
        setIsClearVisible(false);
        handleClear();
    };
    useEffect(() => {
        if (state.ManagementFees[0]?.managementAgreementType) {
            setIsClearVisible(true);
        }
    }, [state]);


    return (
        <TooltipProvider>
            <Card data-testid="management-agreement-card">
                <CardHeader data-testid="management-agreement-header">
                    <div className="flex items-center justify-between">
                        <Label>Enable Management Agreement</Label>
                        <Switch
                            data-qa-id="switch-enableManagementAgreement-agreement"
                            checked={state.enabled}
                            onCheckedChange={toggleManagementAgreementEnabled}
                            data-testid="management-agreement-switch"
                            disabled={!isEditable}
                        />
                    </div>
                </CardHeader>
                <CardContent>
                    <p className="text-sm text-muted-foreground mb-4">
                        Enable to create the Profit Share, Parking Validation Threshold, Insurance, and Claims configurations of a Management Agreement contract.
                    </p>
                    {state.enabled && (
                        <div className="space-y-6">
                            {/* Invoice Selection */}
                            {watchInvoiceGroupingEnabled && (
                                <div className="space-y-2">
                                    <div className="flex items-center space-x-2">
                                        <Label htmlFor="management-agreement-invoice-group">Invoice</Label>
                                        <Tooltip>
                                            <TooltipTrigger asChild>
                                                <Info className="h-4 w-4 text-muted-foreground" />
                                            </TooltipTrigger>
                                            <TooltipContent>
                                                <p>
                                                    The Management Fee, Profit Share,Insurance and Claims will all be grouped together on the same invoice.
                                                </p>
                                            </TooltipContent>
                                        </Tooltip>
                                    </div>
                                    <Select
                                        data-qa-id="select-invoiceGroup-agreement"
                                        value={selectedInvoiceGroup}
                                        onValueChange={(value) => setSelectedInvoiceGroup(value)}
                                        disabled={!isEditable}
                                    >
                                        <SelectTrigger id="management-agreement-invoice-group">
                                            <SelectValue placeholder="Select Invoice Group" />
                                        </SelectTrigger>
                                        <SelectContent>
                                            {invoiceGroups.map((group) => (
                                                <SelectItem key={group.groupNumber} value={group.groupNumber.toString()}>
                                                    {group.groupNumber}
                                                </SelectItem>
                                            ))}
                                        </SelectContent>
                                    </Select>
                                </div>
                            )}

                            {state.ManagementFees.map((fee, index) => (
                                <div key={index} className="space-y-6 mb-4">
                                    {/* Management Fee Section */}
                                    <ManagementFeeSection
                                        fee={fee}
                                        index={index}
                                        invoiceGroups={invoiceGroups}
                                        watchInvoiceGroupingEnabled={watchInvoiceGroupingEnabled}
                                        onUpdate={(changes) => updateManagementFee(index, changes)}
                                        errors={errors}
                                        isEditable={isEditable}
                                        handleClearClick={handleClearClick}
                                        isClearVisible={isClearVisible}
                                        setIsClearVisible={setIsClearVisible}
                                    />

                                    {/* Profit Share Section */}
                                    <ProfitShareSection
                                        fee={fee}
                                        index={index}
                                        onUpdate={(changes) => updateManagementFee(index, changes)}
                                        errors={errors}
                                        isEditable={isEditable}
                                    />
                                    {/* Insurance Section */}
                                    <InsuranceSection
                                        fee={fee}
                                        index={index}
                                        billableInsuranceAccounts={billableInsuranceAccounts}
                                        onUpdate={(changes) => updateManagementFee(index, changes)}
                                        errors={errors}
                                        isEditable={isEditable}
                                    />


                                    {/* Claims Section */}
                                    <ClaimsSection
                                        fee={fee}
                                        index={index}
                                        claimsExpenseAccounts={claimsExpenseAccounts}
                                        onUpdate={(changes) => updateManagementFee(index, changes)}
                                        errors={errors}
                                        isEditable={isEditable}
                                    />
                                    <NonGLBillableExpensesSection
                                        fee={fee}
                                        index={index}
                                        onUpdate={(changes: any) => updateManagementFee(index, changes)}
                                        errors={errors}
                                        isEditable={isEditable}
                                    />
                                </div>
                            ))}
                        </div>
                    )}
                </CardContent>
            </Card>
        </TooltipProvider>
    );
};

const ManagementFeeSection: React.FC<ManagementFeeSectionProps> = ({
    fee,
    index,
    invoiceGroups,
    watchInvoiceGroupingEnabled,
    onUpdate,
    isEditable,
    errors,
    handleClearClick,
    isClearVisible,
    setIsClearVisible,

}) => {
    const [jobCodes, setJobCodes] = useState<JobCode[]>(() => {
        try{
            const jobCodeData = fee.perLaborHourJobCodeData ? (fee.perLaborHourJobCodeData) : [];
            return Array.isArray(jobCodeData) && jobCodeData.length > 0 
                ? jobCodeData.map(jobCode => ({
                    code: jobCode.code,
                    description: jobCode.description,
                    standardRate: jobCode.standardRate,
                    overtimeRate: jobCode.overtimeRate,
                    standardRateEscalatorValue: jobCode.standardRateEscalatorValue,
                    overtimeRateEscalatorValue: jobCode.overtimeRateEscalatorValue,
                }))
                : [{id: "1", code: "", description: "", standardRate: 0, overtimeRate: 0, standardRateEscalatorValue: 0, overtimeRateEscalatorValue: 0}];
        } catch {
            return [{id: "1", code: "", description: "", standardRate: 0, overtimeRate: 0, standardRateEscalatorValue: 0, overtimeRateEscalatorValue: 0}];
        }
    })

    const [isManagementFeeEscalatorEnabled, setIsManagementFeeEscalatorEnabled] = useState(false);

    const addJobCode = () => {
        const newId = (jobCodes.length + 1).toString()
        setJobCodes([...jobCodes, { id: newId, code: "", description: "", standardRate: 0, overtimeRate: 0, standardRateEscalatorValue: 0, overtimeRateEscalatorValue: 0 }])
    }

    const removeJobCode = (index: number) => {
        if (jobCodes.length > 1) {
            const updatedJobCodes = jobCodes.filter((_, jobIndex) => jobIndex !== index);

            const resetJobCodes = updatedJobCodes.map((jobCode, index) => ({
                ...jobCode,
                id: (index + 1).toString(),
            }));
            setJobCodes(resetJobCodes);

            onUpdate({
                perLaborHourJobCodeData: resetJobCodes,
            })
        }
    }

    const updateJobCode = (index: number, field: keyof JobCode, value: string | number) => {
        const updatedlaborHourJobCodes = jobCodes.map((jobCode, jobIndex) =>
            jobIndex === index ? { ...jobCode, [field]: value } : jobCode
        );
    
        setJobCodes(updatedlaborHourJobCodes);
    
        onUpdate({
            perLaborHourJobCodeData: updatedlaborHourJobCodes,
        });
    };
       
    return (
        <Card className="space-y-6">
            <CardHeader>
                <div className="flex items-center justify-between">
                    <CardTitle className="text-2xl font-bold">Management Fee</CardTitle>
                    {isClearVisible && (
                        <Button
                            data-qa-id="button-clearManagementFee-managementFee"
                            onClick={handleClearClick}
                            disabled={!isEditable}
                            variant="ghost"
                        >
                            Clear
                        </Button>

                    )}
                </div>
            </CardHeader>
            <CardContent className="space-y-4">
                <p className="text-sm text-muted-foreground mb-4">
                    A Management Fee may be billed as a fixed amount, a percentage of revenue, or at an hourly rate. It will always appear on the invoice listed as a 'Management Fee' expense to the customer.
                </p>
                <RadioGroup
                    value={fee.managementAgreementType}
                    data-testid={`management-type-radio-${index}`}
                    onValueChange={(type: ManagementAgreementType) => {
                        setIsClearVisible(true);
                        let resetFields: Partial<ManagementFee> = {};

                        switch (type) {
                            case ManagementAgreementType.FIXED_FEE:
                                resetFields = {
                                    laborHourJobCode: null,
                                    laborHourRate: null,
                                    laborHourOvertimeRate: null,
                                    revenuePercentageAmount: null,
                                    perLaborHourJobCodeData: [{id: "1", code: "", description: "", standardRate: 0, overtimeRate: 0, standardRateEscalatorValue: 0, overtimeRateEscalatorValue: 0}],
                                };
                                break;
                            case ManagementAgreementType.PER_LABOR_HOUR:
                                resetFields = {
                                    fixedFeeAmount: null,
                                    revenuePercentageAmount: null,
                                };
                                setJobCodes([{ id: "1", code: "", description: "", standardRate: 0, overtimeRate: 0, standardRateEscalatorValue: 0, overtimeRateEscalatorValue: 0 }]);
                                break;
                            case ManagementAgreementType.REVENUE_PERCENTAGE:
                                resetFields = {
                                    fixedFeeAmount: null,
                                    laborHourJobCode: null,
                                    laborHourRate: null,
                                    laborHourOvertimeRate: null,
                                    perLaborHourJobCodeData: [{id: "1", code: "", description: "", standardRate: 0, overtimeRate: 0, standardRateEscalatorValue: 0, overtimeRateEscalatorValue: 0}],
                                };
                                break;
                        }

                        onUpdate({
                            managementAgreementType: type,
                            ...resetFields,
                        });
                    }}
                >
                    <div className="flex items-center space-x-2">
                        <RadioGroupItem data-qa-id={`radio-fixedFee-${index}-managementFee`} value={ManagementAgreementType.FIXED_FEE} id={`fixed-fee-${index}`} data-testid={`fixed-fee-${index}`} disabled={!isEditable} />
                        <Label htmlFor={`fixed-fee-${index}`}>Fixed Fee</Label>
                    </div>
                    <div className="flex items-center space-x-2">
                        <RadioGroupItem data-qa-id={`radio-perLaborHour-${index}-managementFee`} value={ManagementAgreementType.PER_LABOR_HOUR} id={`per-labor-hour-${index}`} data-testid={`per-labor-hour-${index}`} disabled={!isEditable} />
                        <Label htmlFor={`per-labor-hour-${index}`}>Per Labor Hour</Label>
                    </div>
                    <div className="flex items-center space-x-2">
                        <RadioGroupItem data-qa-id={`radio-revenuePercentage-${index}-managementFee`} value={ManagementAgreementType.REVENUE_PERCENTAGE} id={`revenue-percentage-${index}`} data-testid={`revenue-percentage-${index}`} disabled={!isEditable} />
                        <Label htmlFor={`revenue-percentage-${index}`}>Revenue Percentage</Label>
                    </div>
                </RadioGroup>

                <div className="space-y-4">
                    <div className="flex items-center space-x-2">
                        <Checkbox 
                            data-qa-id="checkbox-enableManagementFeeEscalator"
                            id="management-fee-escalator-enabled"
                            checked={fee.managementFeeEscalatorEnabled}
                            disabled={!isEditable}
                            onCheckedChange={(checked: boolean) => {
                                onUpdate({ managementFeeEscalatorEnabled: checked as boolean })
                                setIsManagementFeeEscalatorEnabled(checked);
                            }}
                            onFocus={() => setIsManagementFeeEscalatorEnabled(true)}
                            onBlur={() => setIsManagementFeeEscalatorEnabled(false)}
                        />
                        <Label htmlFor="management-fee-escalator-enabled">Enable Escalator</Label>
                        <Tooltip>
                            <TooltipTrigger asChild>
                                <Button data-qa-id="button-management-fee-escalator-enabled" variant="ghost" size="icon" onClick={e => e.preventDefault()}>
                                    <Info className="h-4 w-4" />
                                </Button>
                            </TooltipTrigger>
                            <TooltipContent className="max-w-xs">
                                <p>The Escalator feature allows you to automatically increase the Management Fee on a yearly basis.
                                    For Arrears Billing Type, the escalator will increase the Management Fee by the specified amount
                                    or percentage on the last Friday of the month preceding the Escalation Month. This ensures the
                                    correct billing cycles are affected. The increase will continue to apply every anniversary of that
                                    month.
                                </p>
                            </TooltipContent>
                        </Tooltip>
                    </div>

                    {fee.managementFeeEscalatorEnabled && (
                        <div className="space-y-4 ml-6">
                            <div className="space-y-2">
                                <div className="flex items-center space-x-2">
                                    <Label htmlFor="management-fee-escalation-month">Escalation Month</Label>
                                    <Tooltip>
                                        <TooltipTrigger asChild>
                                            <Button data-qa-id="button-management-fee-escalation-month" variant="ghost" size="icon" onClick={e => e.preventDefault()}>
                                                <Info className="h-4 w-4" />
                                            </Button>
                                        </TooltipTrigger>
                                        <TooltipContent className="max-w-xs">
                                            <p>The month in which the escalation will take effect. For Arrears Billing Type, the escalation
                                                will be applied on the last Friday of the month preceding this selected month.
                                            </p>
                                        </TooltipContent>
                                    </Tooltip>
                                </div>
                                <Select 
                                    data-qa-id="select-management-fee-escalation-Month"
                                    value={fee.managementFeeEscalatorMonth || Month.JANUARY}
                                    onValueChange={(value) => onUpdate({ managementFeeEscalatorMonth: value as Month })}
                                    required
                                    disabled={!isEditable}
                                >
                                    <SelectTrigger>
                                        <SelectValue placeholder="Select escalation Month" />
                                    </SelectTrigger>
                                    <SelectContent>
                                        {Object.values(Month).map((month) => (
                                            <SelectItem key={month} value={month}>
                                                {month}
                                            </SelectItem>
                                        ))}
                                    </SelectContent>
                                </Select>
                            </div>
                            <div className="space-y-2">
                                <div className="flex items-center space-x-2">
                                    <Label htmlFor="management-fee-escalator-format">Escalator Format</Label>
                                    <Tooltip>
                                        <TooltipTrigger asChild>
                                            <Button data-qa-id="button-management-fee-escalator-format" variant="ghost" size="icon" onClick={e => e.preventDefault()}>
                                                <Info className="h-4 w-4" />
                                            </Button>
                                        </TooltipTrigger>
                                        <TooltipContent className="max-w-xs">
                                            <p>Choose whether the escalation should be a percentage increase (e.g., 3% more each year) or a
                                                fixed amount increase (e.g., $500 more each year).
                                            </p>
                                        </TooltipContent>
                                    </Tooltip>
                                </div>
                                <RadioGroup 
                                    value={fee.managementFeeEscalatorType || EscalatorFormatType.PERCENTAGE}
                                    onValueChange={(value) => onUpdate({ managementFeeEscalatorType: value as EscalatorFormatType })}
                                    data-qa-id="radio-managementFeeEscalatorFormat"
                                >
                                    <div className="flex items-center space-x-2">
                                        <RadioGroupItem data-qa-id="radio-managementFeeEscalatorPercentage" value={EscalatorFormatType.PERCENTAGE} id="management-fee-escalator-percentage" data-testid="radio-managementFeeEscalatorPercentage" disabled={!isEditable} />
                                        <Label htmlFor="management-fee-escalator-percentage">Percentage</Label>
                                    </div>
                                    <div className="flex items-center space-x-2">
                                        <RadioGroupItem data-qa-id="radio-managementFeeEscalatorFixedAmount" value={EscalatorFormatType.FIXEDAMOUNT} id="management-fee-escalator-fixed-amount" data-testid="radio-managementFeeEscalatorFixedAmount" disabled={!isEditable} />
                                        <Label htmlFor="management-fee-escalator-fixed-amount">Fixed Amount</Label>
                                    </div>
                                </RadioGroup>
                            </div>
                        </div>
                    )}
                </div>

                {fee.managementAgreementType === ManagementAgreementType.FIXED_FEE && (
                    <div className="space-y-2">
                        <Label htmlFor={`fixed-fee-amount-${index}`}>Fixed Fee Amount</Label>
                        <div className="flex items-center space-x-2">
                            <NumericFormat
                                data-qa-id={`input-fixedFeeAmount-${index}-managementFee`}
                                id={`fixed-fee-amount-${index}`}
                                required={true}
                                displayType="input"
                                decimalScale={2}
                                fixedDecimalScale={true}
                                prefix="$"
                                inputMode="decimal"
                                allowNegative={false}
                                placeholder="Enter Fixed Fee Amount"
                                value={fee.fixedFeeAmount ?? ''}
                                onValueChange={(values) =>
                                    onUpdate({
                                        fixedFeeAmount: parseFloat(values.floatValue?.toString() || ''),
                                    })
                                }
                                data-testid={`fixed-fee-input-${index}`}
                                customInput={Input}
                                disabled={!isEditable}
                            />
                        </div>
                        {errors?.managementAgreement?.ManagementFees?.[index]?.fixedFeeAmount && (
                            <p className="text-red-500 text-sm">
                                {errors.managementAgreement.ManagementFees[index].fixedFeeAmount.message}
                            </p>
                        )}
                    
                        {fee.managementFeeEscalatorEnabled && (
                            <div>
                                <Label htmlFor={`fixed-fee-escalator-${index}`}>{fee.managementFeeEscalatorType === EscalatorFormatType.PERCENTAGE ? "Escalator Percentage" : "Escalator Amount"}</Label>
                                    <Tooltip>
                                        <TooltipTrigger asChild>
                                            <Button data-qa-id="button-fixed-fee-escalator" variant="ghost" size="icon" onClick={e => e.preventDefault()}>
                                                <Info className="h-4 w-4" />
                                            </Button>
                                        </TooltipTrigger>
                                        <TooltipContent className="max-w-xs">
                                            <p>{fee.managementFeeEscalatorType === EscalatorFormatType.PERCENTAGE ? `The percentage by which the Fixed Fee will increase by ${fee.managementFeeEscalatorValue}% each year on the anniversary of the Escalation Month - ${fee.managementFeeEscalatorMonth}.`
                                                : `The fixed amount by which the Fixed Fee will increase by $${fee.managementFeeEscalatorValue} each year on the anniversary of the Escalation Month - ${fee.managementFeeEscalatorMonth}.`}
                                            </p>
                                        </TooltipContent>
                                    </Tooltip>
                                    <div className="flex items-center space-x-2">
                                        <NumericFormat
                                            data-qa-id={`fixed-fee-escalator-${index}`}
                                            type="text"
                                            value={fee.managementFeeEscalatorValue ?? ''}
                                            onValueChange={(values) =>
                                                onUpdate({
                                                    managementFeeEscalatorValue: parseFloat(values.floatValue?.toString() || ''),
                                                })
                                            }
                                            decimalScale={2}
                                            fixedDecimalScale={true}
                                            allowNegative={false}
                                            placeholder= {fee.managementFeeEscalatorType === EscalatorFormatType.PERCENTAGE ? "Enter Percentage" : "Enter Amount"}
                                            suffix= {fee.managementFeeEscalatorType === EscalatorFormatType.PERCENTAGE ? "%" : ""}
                                            prefix= {fee.managementFeeEscalatorType === EscalatorFormatType.FIXEDAMOUNT ? "$" : ""}
                                            min={0}
                                            max={100}
                                            customInput={Input}
                                            disabled={!isEditable}
                                        />
                                    </div>
                            </div>
                        )}
                    </div>
                )}               

                {fee.managementAgreementType === ManagementAgreementType.PER_LABOR_HOUR && (
                    <div className="space-y-4">
                        <div className="space-y-2">
                            <Label>Job Codes</Label>
                            <p className="text-sm text-muted-foreground mb-4">
                                Select which job codes should be included in the management fee calculation. For each job code, specify the Standard Rate and Overtime Rate that will be used to calculate the fee based on hours worked.
                            </p>
                        </div>

                        {jobCodes.map((jobCode, jobIndex) => (
                            <Card key={jobCode.id} className="rounded-lg border bg-card p-4">
                                <div className="flex justify-between items-start mb-4">
                                    <h4 className="font-medium">Job Code {jobIndex + 1}</h4>
                                    {jobCodes.length > 1 && (
                                        <Button 
                                        variant="ghost" 
                                        size="icon" 
                                        className="h-8 w-8" 
                                        disabled={!isEditable} 
                                        onClick={(e) => {e.preventDefault(); 
                                        removeJobCode(jobIndex)}}
                                        >
                                            <X className="h-4 w-4" />
                                        </Button>
                                    )}
                                </div>

                                <div className="space-y-3">
                                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                        <div className="space-y-2">
                                            <Label htmlFor={`job-code-${jobIndex}`}>Code</Label>
                                            <Input 
                                            id={`job-code-${jobIndex}`} 
                                            value={jobCode.code || ""}
                                            placeholder="Enter Job Code" 
                                            onChange={(e) => updateJobCode(jobIndex, "code", e.target.value)}
                                            data-qa-id = {`input-laborHourJobCode-${jobIndex}`} 
                                            disabled={!isEditable} />

                                            {errors?.managementAgreement?.ManagementFees?.[index]?.perLaborHourJobCodeData?.[jobIndex]?.code && (
                                                <p className="text-red-500 text-sm">
                                                    {errors.managementAgreement.ManagementFees[index].perLaborHourJobCodeData[jobIndex].code.message}
                                                </p>
                                            )}
                                        </div>
                                        
                                        <div className="space-y-2">
                                            <Label htmlFor={`job-description-${jobIndex}`}>Description</Label>
                                            <Input 
                                            id = {`job-description-${jobIndex}`}
                                            value={jobCode.description || ""}
                                            placeholder="Enter Job Description"
                                            onChange={(e) => updateJobCode(jobIndex, "description", e.target.value)}
                                            data-qa-id = {`input-laborHourJobDescription-${jobIndex}`}
                                            disabled={!isEditable}
                                            />
                                        </div>
                                    </div>

                                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                        <div className="space-y-2">
                                            <Label htmlFor={`rate-${jobIndex}`}>Standard Rate</Label>
                                            <NumericFormat 
                                            id={`rate-${jobIndex}`}
                                            required={true}
                                            displayType="input"
                                            thousandSeparator={true}
                                            decimalScale={2}
                                            fixedDecimalScale={true}
                                            prefix="$"
                                            inputMode="numeric"
                                            allowNegative={false}
                                            placeholder="Enter standard hourly rate"
                                            value={jobCode.standardRate || 0}
                                            customInput={Input}
                                            onValueChange={(values) => {
                                                updateJobCode(jobIndex, "standardRate", parseFloat(values.value) || 0)
                                            }}
                                            data-qa-id={`input-laborHourRate-${jobIndex}-managementFee`} 
                                            disabled={!isEditable}
                                            />
                                            <p className="text-xs text-muted-foreground">Rate applied to standard hours worked</p>
                                            {errors?.managementAgreement?.ManagementFees?.[index]?.perLaborHourJobCodeData?.[jobIndex]?.standardRate && (
                                                <p className="text-red-500 text-sm">
                                                    {errors.managementAgreement.ManagementFees[index].perLaborHourJobCodeData[jobIndex].standardRate.message}
                                                </p>
                                            )}
                                        </div>
                                        <div className="space-y-2">
                                            <Label htmlFor={`overtime-rate-${jobIndex}`}>Overtime Rate</Label>
                                            <NumericFormat 
                                            id={`overtime-rate-${jobIndex}`}
                                            required={true}
                                            displayType="input"
                                            thousandSeparator={true}
                                            decimalScale={2}
                                            fixedDecimalScale={true}
                                            prefix="$"
                                            inputMode="numeric"
                                            allowNegative={false}
                                            customInput={Input}
                                            placeholder="Enter overtime hourly rate"
                                            value={jobCode.overtimeRate || 0}
                                            onValueChange={(values) => {
                                                updateJobCode(jobIndex, "overtimeRate", parseFloat(values.value) || 0)
                                            }}
                                            data-qa-id={`input-laborHourOvertimeRate-${jobIndex}-managementFee`} 
                                            disabled={!isEditable}
                                            />
                                            <p className="text-xs text-muted-foreground">Rate applied to overtime hours worked</p>
                                            {errors?.managementAgreement?.ManagementFees?.[index]?.perLaborHourJobCodeData?.[jobIndex]?.overtimeRate && (
                                                <p className="text-red-500 text-sm">
                                                    {errors.managementAgreement.ManagementFees[index].perLaborHourJobCodeData[jobIndex].overtimeRate.message}
                                                </p>
                                            )}
                                        </div>
                                    </div>

                                    {fee.managementFeeEscalatorEnabled && (
                                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                            <div>
                                                <Label htmlFor={`standard-rate-escalator-${jobIndex}`}>{fee.managementFeeEscalatorType === EscalatorFormatType.PERCENTAGE ? "Escalator Percentage" : "Escalator Amount"}</Label>
                                                <Tooltip>
                                                    <TooltipTrigger asChild>
                                                        <Button data-qa-id="button-standard-rate-escalator" variant="ghost" size="icon" onClick={e => e.preventDefault()}>
                                                            <Info className="h-4 w-4" />
                                                        </Button>
                                                    </TooltipTrigger>
                                                    <TooltipContent className="max-w-xs">
                                                        <p>{fee.managementFeeEscalatorType === EscalatorFormatType.PERCENTAGE ? `The percentage by which the Standard Rate will increase by ${jobCode.standardRateEscalatorValue}% each year on the anniversary of the Escalation Month - ${fee.managementFeeEscalatorMonth}.`
                                                            : `The fixed amount by which the Standard Rate will increase by $${jobCode.standardRateEscalatorValue} each year on the anniversary of the Escalation Month - ${fee.managementFeeEscalatorMonth}.`}
                                                        </p>
                                                    </TooltipContent>
                                                </Tooltip>
                                                <NumericFormat
                                                    data-qa-id={`standard-rate-escalator-${index}`}
                                                    type="text"
                                                    value={jobCode.standardRateEscalatorValue || 0}
                                                    onValueChange={(values) => {
                                                        updateJobCode(jobIndex, "standardRateEscalatorValue", parseFloat(values.value) || 0)
                                                    }}
                                                    decimalScale={2}
                                                    fixedDecimalScale={true}
                                                    allowNegative={false}
                                                    placeholder= {fee.managementFeeEscalatorType === EscalatorFormatType.PERCENTAGE ? "Enter Percentage" : "Enter Amount"}
                                                    suffix= {fee.managementFeeEscalatorType === EscalatorFormatType.PERCENTAGE ? "%" : ""}
                                                    prefix= {fee.managementFeeEscalatorType === EscalatorFormatType.FIXEDAMOUNT ? "$" : ""}
                                                    min={0}
                                                    max={100}
                                                    customInput={Input}
                                                    disabled={!isEditable}
                                                />
                                            </div>
                                            <div>
                                                <Label htmlFor={`overtime-rate-escalator-${jobIndex}`}>{fee.managementFeeEscalatorType === EscalatorFormatType.PERCENTAGE ? "Escalator Percentage" : "Escalator Amount"}</Label>
                                                <Tooltip>
                                                    <TooltipTrigger asChild>
                                                        <Button data-qa-id="button-overtime-rate-escalator" variant="ghost" size="icon" onClick={e => e.preventDefault()}>
                                                            <Info className="h-4 w-4" />
                                                        </Button>
                                                    </TooltipTrigger>
                                                    <TooltipContent className="max-w-xs">
                                                        <p>{fee.managementFeeEscalatorType === EscalatorFormatType.PERCENTAGE ? `The percentage by which the Overtime Rate will increase by ${jobCode.overtimeRateEscalatorValue}% each year on the anniversary of the Escalation Month - ${fee.managementFeeEscalatorMonth}.`
                                                            : `The fixed amount by which the Overtime Rate will increase by $${jobCode.overtimeRateEscalatorValue} each year on the anniversary of the Escalation Month - ${fee.managementFeeEscalatorMonth}.`}
                                                        </p>
                                                    </TooltipContent>
                                                </Tooltip>
                                                <NumericFormat
                                                    data-qa-id={`overtime-rate-escalator-${index}`}
                                                    type="text"
                                                    value={jobCode.overtimeRateEscalatorValue || 0}
                                                    onValueChange={(values) => {
                                                        updateJobCode(jobIndex, "overtimeRateEscalatorValue", parseFloat(values.value) || 0)
                                                    }}
                                                    decimalScale={2}
                                                    fixedDecimalScale={true}
                                                    allowNegative={false}
                                                    placeholder= {fee.managementFeeEscalatorType === EscalatorFormatType.PERCENTAGE ? "Enter Percentage" : "Enter Amount"}
                                                    suffix= {fee.managementFeeEscalatorType === EscalatorFormatType.PERCENTAGE ? "%" : ""}
                                                    prefix= {fee.managementFeeEscalatorType === EscalatorFormatType.FIXEDAMOUNT ? "$" : ""}
                                                    min={0}
                                                    max={100}
                                                    customInput={Input}
                                                    disabled={!isEditable}
                                                />
                                            </div>
                                        </div>
                                    )}
                                </div>
                            </Card>
                        ))}

                        <div className="flex justify-start space-x-2">
                            <Button 
                            data-qa-id="button-add-JobCode"
                            variant="outline"
                            type="button"
                            disabled={!isEditable}
                            onClick={addJobCode}>
                                <PlusCircle className="w-4 h-4 mr-2" />
                                Add Job Code
                            </Button>
                        </div>
                    </div>
                )}

                {fee.managementAgreementType === ManagementAgreementType.REVENUE_PERCENTAGE && (
                    <div className="space-y-2">
                        <Label htmlFor={`revenue-percentage-${index}`}>Revenue Percentage</Label>
                        <div className="flex items-center space-x-2">
                            <NumericFormat
                                data-qa-id={`input-revenuePercentage-${index}-managementFee`}
                                id={`revenue-percentage-${index}`}
                                required={true}
                                displayType="input"
                                decimalScale={2}
                                fixedDecimalScale={true}
                                suffix="%"
                                inputMode="decimal"
                                allowNegative={false}
                                placeholder="Enter Revenue Percentage"
                                value={fee.revenuePercentageAmount ?? ''}
                                onValueChange={(values) =>
                                    onUpdate({
                                        revenuePercentageAmount: parseFloat(values.floatValue?.toString() || ''),
                                    })
                                }
                                customInput={Input}
                                disabled={!isEditable}
                            />
                        </div>
                        {errors?.managementAgreement?.ManagementFees?.[index]?.revenuePercentageAmount && (
                            <p className="text-red-500 text-sm">
                                {errors.managementAgreement.ManagementFees[index].revenuePercentageAmount.message}
                            </p>
                        )}

                        {fee.managementFeeEscalatorEnabled && (
                            <div>
                                <Label htmlFor={`revenue-percentage-escalator-${index}`}>{fee.managementFeeEscalatorType === EscalatorFormatType.PERCENTAGE ? "Escalator Percentage" : "Escalator Amount"}</Label>
                                    <Tooltip>
                                        <TooltipTrigger asChild>
                                            <Button data-qa-id="button-revenue-percentage-escalator" variant="ghost" size="icon" onClick={e => e.preventDefault()}>
                                                <Info className="h-4 w-4" />
                                            </Button>
                                        </TooltipTrigger>
                                        <TooltipContent className="max-w-xs">
                                            <p>{fee.managementFeeEscalatorType === EscalatorFormatType.PERCENTAGE ? `The percentage by which the Revenue Percentage will increase by ${fee.managementFeeEscalatorValue}% each year on the anniversary of the Escalation Month - ${fee.managementFeeEscalatorMonth}.`
                                                : `The fixed amount by which the Revenue Percentage will increase by $${fee.managementFeeEscalatorValue} each year on the anniversary of the Escalation Month - ${fee.managementFeeEscalatorMonth}.`}
                                            </p>
                                        </TooltipContent>
                                    </Tooltip>
                                    <div className="flex items-center space-x-2">
                                        <NumericFormat
                                            data-qa-id={`revenue-percentage-escalator-${index}`}
                                            type="text"
                                            value={fee.managementFeeEscalatorValue ?? ''}
                                            onValueChange={(values) =>
                                                onUpdate({
                                                    managementFeeEscalatorValue: parseFloat(values.floatValue?.toString() || ''),
                                                })
                                            }
                                            decimalScale={2}
                                            fixedDecimalScale={true}
                                            allowNegative={false}
                                            placeholder= {fee.managementFeeEscalatorType === EscalatorFormatType.PERCENTAGE ? "Enter Percentage" : "Enter Amount"}
                                            suffix= {fee.managementFeeEscalatorType === EscalatorFormatType.PERCENTAGE ? "%" : ""}
                                            prefix= {fee.managementFeeEscalatorType === EscalatorFormatType.FIXEDAMOUNT ? "$" : ""}
                                            min={0}
                                            max={100}
                                            customInput={Input}
                                            disabled={!isEditable}
                                        />
                                    </div>
                            </div>
                        )}
                    </div>
                )}
            </CardContent>
        </Card>
    );
};

const InsuranceSection: React.FC<InsuranceSectionProps> = ({ fee, index, billableInsuranceAccounts, onUpdate, isEditable, errors }) => {
    const [isEditingInsuranceSection, setIsEditingInsuranceSection] = useState(false);
    return (
        <Card>
            <CardHeader>
                <CardTitle className="text-2xl font-bold">Insurance</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
                <p className="text-sm text-muted-foreground">
                    Insurance is typically the sum of three general ledger accounts: 7080 - Insurance - General Liability,  7082 - Insurance - Vehicle, and 7085 - Insurance - Other or could also be charged as a fixed fee.
                </p>
                <div className="flex items-center space-x-2">
                    <Checkbox
                        data-qa-id="checkbox-enableInsurance-insurance"
                        id="create-insurance-line-item"
                        data-testid="create-insurance-line-item"
                        checked={fee.insuranceEnabled}
                        onCheckedChange={(checked: boolean) => {
                            if (isEditingInsuranceSection) {
                                onUpdate({ insuranceEnabled: checked as boolean })
                            }
                            setIsEditingInsuranceSection(checked);
                        }}
                        disabled={!isEditable}
                        onFocus={() => setIsEditingInsuranceSection(true)}
                        onBlur={() => setIsEditingInsuranceSection(false)}
                    />
                    <Label htmlFor="create-insurance-line-item">Create Insurance Line-item</Label>
                </div>
                {fee.insuranceEnabled && (
                    <>
                        <RadioGroup
                            value={fee.insuranceType}
                            onValueChange={(type: InsuranceType) =>
                                onUpdate({ insuranceType: type })
                            }
                        >
                            <div className="flex items-center space-x-2">
                                <RadioGroupItem data-qa-id="radio-insuranceBillableAccounts-insurance" value={InsuranceType.BASED_ON_BILLABLE_ACCOUNTS} id="insurance-billable-accounts" data-testid="insurance-billable-accounts" disabled={!isEditable} />
                                <Label htmlFor="insurance-billable-accounts">Based on Billable Accounts</Label>
                            </div>
                            <div className="flex items-center space-x-2">
                                <RadioGroupItem data-qa-id="radio-insuranceFixedFee-insurance" value={InsuranceType.FIXED_FEE} id="insurance-fixed-fee" data-testid="insurance-fixed-fee" disabled={!isEditable} />
                                <Label htmlFor="insurance-fixed-fee">Fixed Fee</Label>
                            </div>
                        </RadioGroup>

                        {fee.insuranceType === InsuranceType.BASED_ON_BILLABLE_ACCOUNTS && (
                            <div className="space-y-4">
                                <p className="text-sm text-muted-foreground">
                                    If Insurance is charged to the customer using the Based on Billable Accounts feature, then the sum of Insurance related expense accounts (shown in Billable Insurance Accounts below) will be charged to the customer in this line-item, and not in the Billable Expense Accounts line-item.
                                </p>
                                <div className="space-y-2">
                                    <Label>Billable Insurance Accounts</Label>
                                    <p className="text-sm text-muted-foreground">
                                        These are the Insurance accounts which are not excluded in the Billable Expense Accounts section. You may add an additional percentage to the sum of these accounts, which will then be added together to calculate this line-item.
                                    </p>
                                    {billableInsuranceAccounts.length > 0 ? (
                                        billableInsuranceAccounts.map(account => (
                                            <div key={account.code} className="flex items-center space-x-2">
                                                <Checkbox data-qa-id={`checkbox-insuranceAccount-${account.code}-insurance`} id={`insurance-account-${account.code}`} checked disabled />
                                                <Label htmlFor={`insurance-account-${account.code}`}>{account.title}</Label>
                                            </div>
                                        ))
                                    ) : (
                                        <p className="text-sm text-muted-foreground">No billable insurance accounts found.</p>
                                    )}
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="additional-insurance-percentage">Additional Percentage</Label>
                                    <div className="flex items-center space-x-2">
                                        <NumericFormat
                                            data-qa-id={`input-insuranceAdditionalPercentage-${index}-insurance`}
                                            id="additional-insurance-percentage"
                                            displayType="input"
                                            decimalScale={2}
                                            fixedDecimalScale={true}
                                            suffix="%"
                                            inputMode="decimal"
                                            allowNegative={false}
                                            value={fee.insuranceAdditionalPercentage || null}
                                            onValueChange={(values) =>
                                                onUpdate({
                                                    insuranceAdditionalPercentage: parseFloat(values.floatValue?.toString() || '')
                                                })
                                            }
                                            placeholder="Enter percentage"
                                            data-testid={`additional-insurance-percentage-${index}`}
                                            customInput={Input}
                                            required
                                            disabled={!isEditable}
                                        />
                                    </div>
                                    {errors?.managementAgreement?.ManagementFees?.[index]?.insuranceAdditionalPercentage && (
                                        <p className="text-red-500 text-sm">
                                            {errors.managementAgreement.ManagementFees[index].insuranceAdditionalPercentage.message}
                                        </p>
                                    )}
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="insurance-line-item-display-name">Line-item Title</Label>
                                    <Input
                                        data-qa-id={`input-insuranceLineTitle-${index}-insurance`}
                                        id="insurance-line-item-display-name"
                                        value={fee.insuranceLineTitle || ''}
                                        onChange={(e) =>
                                            onUpdate({
                                                insuranceLineTitle: e.target.value
                                            })
                                        }
                                        placeholder="Enter display name"
                                        data-testid={`insurance-line-item-display-name-${index}`}
                                        required
                                        disabled={!isEditable}
                                    />
                                </div>
                            </div>
                        )}

                        {fee.insuranceType === InsuranceType.FIXED_FEE && (
                            <div className="space-y-4">
                                <div className="p-4 bg-amber-50 border border-amber-200 rounded-md">
                                    <p className="text-sm text-amber-800">
                                    <strong>Important:</strong> When Insurance is billed as a Fixed Fee, the Insurance related
                                    accounts (7080 - Insurance - General Liability, 7082 - Insurance - Vehicle, and 7085 - Insurance -
                                    Other) need to be added to the 'Excluded Expense Accounts' list in the Billable Accounts section
                                    to avoid double-billing.
                                    </p>
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="insurance-line-item-display-name">Line-item Title</Label>
                                    <Input
                                        data-qa-id={`input-insuranceLineTitle-${index}-insurance`}
                                        id="insurance-line-item-display-name"
                                        value={fee.insuranceLineTitle || ''}
                                        onChange={(e) =>
                                            onUpdate({
                                                insuranceLineTitle: e.target.value
                                            })
                                        }
                                        placeholder="Enter display name"
                                        data-testid={`insurance-line-item-display-name-${index}`}
                                        disabled={!isEditable}
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="insurance-fixed-fee-amount">Fixed Fee Amount</Label>
                                    <div className="flex items-center space-x-2">
                                        <NumericFormat
                                            data-qa-id={`input-insuranceFixedFeeAmount-${index}-insurance`}
                                            id="insurance-fixed-fee-amount"
                                            displayType="input"
                                            decimalScale={2}
                                            fixedDecimalScale={true}
                                            prefix="$"
                                            inputMode="decimal"
                                            allowNegative={false}
                                            value={fee.insuranceFixedFeeAmount || null}
                                            onValueChange={(values) =>
                                                onUpdate({
                                                    insuranceFixedFeeAmount: parseFloat(values.floatValue?.toString() || '')
                                                })
                                            }
                                            placeholder="Enter fixed fee amount"
                                            data-testid={`insurance-fixed-fee-amount-${index}`}
                                            customInput={Input}
                                            disabled={!isEditable}
                                        />
                                    </div>
                                    {errors?.managementAgreement?.ManagementFees?.[index]?.insuranceFixedFeeAmount && (
                                        <p className="text-red-500 text-sm">
                                            {errors.managementAgreement.ManagementFees[index].insuranceFixedFeeAmount.message}
                                        </p>
                                    )}
                                </div>
                            </div>
                        )}
                    </>
                )}
            </CardContent>
        </Card>
    );
};

const ProfitShareSection: React.FC<ProfitShareSectionProps> = ({
    fee,
    index,
    onUpdate,
    isEditable,
    errors
}) => {
    const [tiers, setTiers] = useState<Tier[]>(() => {
        try {
            const tierData = fee.profitShareTierData ? (fee.profitShareTierData) : [];
            return Array.isArray(tierData) && tierData.length > 0
                ? tierData.map(tier => ({
                    sharePercentage: tier.sharePercentage,
                    amount: tier.amount,
                    escalatorValue: tier.escalatorValue,
                }))
                : [{ sharePercentage: 0, amount: 0, escalatorValue: 0 }];
        } catch {
            return [{ sharePercentage: 0, amount: 0, escalatorValue: 0 }];
        }
    });

    const [isThresholdTierEnabled, setIsThresholdTierEnabled] = useState(() => {
        try {
            const tierData = fee.profitShareTierData ? (fee.profitShareTierData) : [];
            return Array.isArray(tierData) && tierData.length > 1;
        } catch {
            return false;
        }
    });

    const handleTierUpdate = (newTiers: Tier[]) => {
        setTiers(newTiers);
        onUpdate({
            profitShareTierData: (newTiers),
            ...((!isThresholdTierEnabled && {
                profitShareAccumulationType: ProfitShareAccumulationType.MONTHLY
            }))
        });
    };

    const toggleThresholdStructure = (enabled: boolean) => {
        setIsThresholdTierEnabled(enabled);
        if (enabled) {
            handleTierUpdate([
                { sharePercentage: 0, amount: 0, escalatorValue: 0 },
                { sharePercentage: 0, amount: 0, escalatorValue: 0 }
            ]);
        } else {
            handleTierUpdate([{
                sharePercentage: tiers[0]?.sharePercentage || 0,
                amount: 0,
                escalatorValue: tiers[0]?.escalatorValue || 0
            }]);
            onUpdate({
                profitShareAccumulationType: ProfitShareAccumulationType.MONTHLY
            });
        }
    };

    const isAmountValid = (tierIndex: number) => {
        if (tierIndex > 0 && tierIndex < tiers.length - 1) {
            const prevAmount = tiers[tierIndex - 1]?.amount;
            const currentAmount = tiers[tierIndex]?.amount;
            return prevAmount !== undefined && currentAmount !== undefined ? currentAmount > prevAmount : false;
        }
        return true;
    };

    const [isEditingProfitShare, setIsEditingProfitShare] = useState(false);
    const [isEditingProfitShareThresold, setIsEditingProfitShareThresold] = useState(false);
    const [isProfitShareEscalatorEnabled, setIsProfitShareEscalatorEnabled] = useState(false);

    return (
        <Card>
            <CardHeader>
                <CardTitle className="text-2xl font-bold">Profit Share</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
                <p className="text-sm text-muted-foreground">
                    A profit share agreement will first calculate all revenue for the site, including excess parking validations when applicable, before then subtracting all expenses such as Management Fee, Billable Accounts, Insurance, Claims, PTEB, and/or Support Services as well as any customer paid expenses that occur to determine monthly profit. The profit is then shared between Towne Park and the owner, with Towne Park receiving the Profit Share Percentage that applies. To change the percentage based on the amount of profit, create a tier Threshold Structure.
                </p>
                <div>
                    <div className="flex items-center space-x-2">
                        <Checkbox
                            data-qa-id={`checkbox-enableProfitShare-${index}-profitShare`}
                            id={`profit-share-enabled-${index}`}
                            checked={fee.profitShareEnabled}
                            onCheckedChange={(checked: boolean) => {
                                if (isEditingProfitShare) {
                                    if(!checked && fee.validationThresholdEnabled){
                                        onUpdate({ profitShareEnabled: checked as boolean, validationThresholdEnabled: false });
                                    } else {
                                        onUpdate({ profitShareEnabled: checked as boolean })
                                    }
                                }
                                setIsEditingProfitShare(checked);
                            }}
                            disabled={!isEditable}
                            onFocus={() => setIsEditingProfitShare(true)}
                            onBlur={() => setIsEditingProfitShare(false)}
                        />
                        <Label htmlFor={`profit-share-enabled-${index}`}>Enable Profit Share</Label>
                    </div>
                    <p className="text-sm text-muted-foreground mt-2">
                        Options to configure profit share contract billing items.
                    </p>
                </div>

                {fee.profitShareEnabled && (
                    <div className="space-y-4">
                        <div className="flex items-center space-x-2">
                            <Checkbox 
                                data-qa-id="checkbox-enableProfitShareEscalator"
                                id="profit-share-escalator-enabled"
                                checked={fee.profitShareEscalatorEnabled}
                                disabled={!isEditable}
                                onCheckedChange={(checked: boolean) => {
                                    onUpdate({ profitShareEscalatorEnabled: checked as boolean })
                                    setIsProfitShareEscalatorEnabled(checked);
                                }}
                                onFocus={() => setIsProfitShareEscalatorEnabled(true)}
                                onBlur={() => setIsProfitShareEscalatorEnabled(false)}
                            />
                            <Label htmlFor="profit-share-escalator-enabled">Enable Escalator</Label>
                            <Tooltip>
                                <TooltipTrigger asChild>
                                    <Button data-qa-id="button-profit-share-escalator-enabled" variant="ghost" size="icon" onClick={e => e.preventDefault()}>
                                        <Info className="h-4 w-4" />
                                    </Button>
                                </TooltipTrigger>
                                <TooltipContent className="max-w-xs">
                                    <p>The Escalator feature allows you to automatically increase the Profit Share Percentages on a yearly basis. For Arrears Billing Type, the escalator will increase each tier's Share Percentage
                                        by the specified amount or percentage on the last Friday of the month preceding the Escalation
                                        Month. This ensures the correct billing cycles are affected. The increase will continue to apply
                                        every anniversary of that month.
                                    </p>
                                </TooltipContent>
                            </Tooltip>
                        </div>
                        {fee.profitShareEscalatorEnabled && (
                            <div className="space-y-4 ml-6">
                                <div className="space-y-2">
                                    <div className="flex items-center space-x-2">
                                        <Label htmlFor="profit-share-escalation-month">Escalation Month</Label>
                                        <Tooltip>
                                            <TooltipTrigger asChild>
                                                <Button data-qa-id="button-profit-share-escalation-month" variant="ghost" size="icon" onClick={e => e.preventDefault()}>
                                                    <Info className="h-4 w-4" />
                                                </Button>
                                            </TooltipTrigger>
                                            <TooltipContent className="max-w-xs">
                                                <p>The month in which the escalation will take effect. For Arrears Billing Type, the
                                                    escalation will be applied on the last Friday of the month preceding this selected month.
                                                </p>
                                            </TooltipContent>
                                        </Tooltip>
                                    </div>
                                    <Select 
                                        data-qa-id="select-profit-share-escalation-Month"
                                        value={fee.profitShareEscalatorMonth || Month.JANUARY}
                                        onValueChange={(value) => onUpdate({ profitShareEscalatorMonth: value as Month })}
                                        required
                                        disabled={!isEditable}
                                    >
                                        <SelectTrigger>
                                            <SelectValue placeholder="Select escalation Month" />
                                        </SelectTrigger>
                                        <SelectContent>
                                            {Object.values(Month).map((month) => (
                                                <SelectItem key={month} value={month}>
                                                    {month}
                                                </SelectItem>
                                            ))}
                                        </SelectContent>
                                    </Select>
                                </div>
                                <div className="space-y-2">
                                    <div className="flex items-center space-x-2">
                                        <Label htmlFor="profit-share-escalator-format">Escalator Format</Label>
                                        <Tooltip>
                                            <TooltipTrigger asChild>
                                                <Button data-qa-id="button-profit-share-escalator-format" variant="ghost" size="icon" onClick={e => e.preventDefault()}>
                                                    <Info className="h-4 w-4" />
                                                </Button>
                                            </TooltipTrigger>
                                            <TooltipContent className="max-w-xs">
                                                <p>Choose whether the escalation should be a percentage increase (e.g., 3% more each year) or
                                                    a fixed amount increase (e.g., 2 percentage points more each year).
                                                </p>
                                            </TooltipContent>
                                        </Tooltip>
                                    </div>
                                    <RadioGroup 
                                        value={fee.profitShareEscalatorType || EscalatorFormatType.PERCENTAGE}
                                        onValueChange={(value) => onUpdate({ profitShareEscalatorType: value as EscalatorFormatType })}
                                        data-qa-id="radio-profitShareEscalatorFormat"
                                    >
                                        <div className="flex items-center space-x-2">
                                            <RadioGroupItem data-qa-id="radio-profitShareEscalatorPercentage" value={EscalatorFormatType.PERCENTAGE} id="profit-share-escalator-percentage" data-testid="radio-profitShareEscalatorPercentage" disabled={!isEditable} />
                                            <Label htmlFor="profit-share-escalator-percentage">Percentage</Label>
                                        </div>
                                        <div className="flex items-center space-x-2">
                                            <RadioGroupItem data-qa-id="radio-profitShareEscalatorFixedAmount" value={EscalatorFormatType.FIXEDAMOUNT} id="profit-share-escalator-fixed-amount" data-testid="radio-profitShareEscalatorFixedAmount" disabled={!isEditable} />
                                            <Label htmlFor="profit-share-escalator-fixed-amount">Fixed Amount</Label>
                                        </div>
                                    </RadioGroup>
                                </div>
                            </div>
                        )}
                        {/* Profit Share Tiers */}
                        <div className="space-y-4">
                            {isThresholdTierEnabled ? (
                                <>
                                    {/* Threshold Structure Checkbox */}
                                    <div className="space-y-2">
                                        <div className="flex items-center space-x-2">
                                            <Checkbox
                                                data-qa-id={`checkbox-enableThreshold-${index}-profitShare`}
                                                id={`threshold-structure-${index}`}
                                                checked={isThresholdTierEnabled}
                                                onCheckedChange={toggleThresholdStructure}
                                                disabled={!isEditable}
                                            />
                                            <Label htmlFor={`threshold-structure-${index}`}>Create Threshold Structure</Label>
                                        </div>
                                    </div>

                                    {/* Accumulation Type Selection */}
                                    <div className="space-y-2">
                                        <Label>Profit Accumulation Type</Label>
                                        <span className="inline-flex items-center">
                                            <Tooltip>
                                                <TooltipTrigger asChild>
                                                    <div className="inline-flex h-8 w-8 items-center justify-center rounded-md hover:bg-accent">
                                                        <Info className="h-4 w-4" />
                                                    </div>
                                                </TooltipTrigger>
                                                <TooltipContent>
                                                    <p>This setting determines the duration for the profit to accumulate before triggering the threshold.</p>
                                                </TooltipContent>
                                            </Tooltip>
                                        </span>
                                        <Select
                                            data-qa-id={`select-profitAccumulationType-${index}-profitShare`}
                                            value={fee.profitShareAccumulationType || ProfitShareAccumulationType.MONTHLY}
                                            onValueChange={(value) => onUpdate({
                                                profitShareAccumulationType: value as ProfitShareAccumulationType
                                            })}
                                            disabled={!isEditable}
                                        >
                                            <SelectTrigger>
                                                <SelectValue placeholder="Select accumulation type" />
                                            </SelectTrigger>
                                            <SelectContent>
                                                <SelectItem value={ProfitShareAccumulationType.MONTHLY}>Monthly</SelectItem>
                                                <SelectItem value={ProfitShareAccumulationType.ANNUALLY_CALENDAR}>Annual (Calendar)</SelectItem>
                                                <SelectItem value={ProfitShareAccumulationType.ANNUALLY_ANIVERSARY}>Annual (Anniversary)</SelectItem>
                                            </SelectContent>
                                        </Select>
                                        <p className="text-sm text-muted-foreground mt-2">
                                            Profit accumulation type will apply to all threshold tiers.
                                        </p>
                                    </div>

                                    <div className="mt-8">
                                        <Label>Threshold Tiers</Label>
                                        <p className="text-sm text-muted-foreground mt-2">
                                            Structure for distinguishing separate share percentages at different profit totals.
                                        </p>
                                    </div>

                                    {/* Tiers mapping */}
                                    {tiers.map((tier, idx) => (
                                        <Card key={idx} className="p-4">
                                            <div className="space-y-2">
                                                <h4 className="font-medium">Tier {idx + 1}</h4>
                                                <div className="grid">
                                                    <div className="space-y-2">
                                                        <Label>Share Percentage</Label>
                                                        <div className="flex items-center space-x-2">
                                                            <NumericFormat
                                                                data-qa-id={`input-tierSharePercentage-${index}-${idx}-profitShare`}
                                                                type="text"
                                                                value={tier.sharePercentage}
                                                                onValueChange={(values) => {
                                                                    const newTiers = [...tiers];
                                                                    newTiers[idx].sharePercentage = parseFloat(values.value) || 0;
                                                                    handleTierUpdate(newTiers);
                                                                }}
                                                                decimalScale={2}
                                                                fixedDecimalScale={true}
                                                                allowNegative={false}
                                                                placeholder="Enter Share Percentage"
                                                                suffix="%"
                                                                min={0}
                                                                max={100}
                                                                customInput={Input}
                                                                disabled={!isEditable}
                                                            />
                                                        </div>
                                                        {idx === tiers.length - 1 && (
                                                            <p className="text-sm text-gray-500">
                                                                The highest tier amount is always unlimited.
                                                            </p>
                                                        )}
                                                    </div>
                                                    {fee.profitShareEscalatorEnabled && (
                                                        <div className="space-y-2">
                                                            <Label htmlFor={`tier-escalator-${index}-${idx}`}>{fee.profitShareEscalatorType === EscalatorFormatType.PERCENTAGE ? "Escalator Percentage" : "Escalator Amount"}</Label>
                                                            <Tooltip>
                                                                <TooltipTrigger asChild>
                                                                    <Button data-qa-id="button-profit-share-tier-escalator" variant="ghost" size="icon" onClick={e => e.preventDefault()}>
                                                                        <Info className="h-4 w-4" />
                                                                    </Button>
                                                                </TooltipTrigger>
                                                                <TooltipContent className="max-w-xs">
                                                                    <p>{fee.profitShareEscalatorType === EscalatorFormatType.PERCENTAGE ? `The percentage by which this tier's Share Percentage will increase by ${tier.escalatorValue}.00% each year on the anniversary of the Escalation Month - ${fee.profitShareEscalatorMonth}.`
                                                                        : `The fixed amount (in percentage points) by which this tier's Share Percentage will increase by $${tier.escalatorValue}.00 points each year on the anniversary of the Escalation Month - ${fee.profitShareEscalatorMonth}.`}
                                                                    </p>
                                                                </TooltipContent>
                                                            </Tooltip>
                                                            <div className="flex items-center space-x-2">
                                                                <NumericFormat
                                                                    data-qa-id={`tier-escalator-${index}-${idx}`}
                                                                    type="text"
                                                                    value={tier.escalatorValue}
                                                                    onValueChange={(values) => {
                                                                        const newTiers = [...tiers];
                                                                        newTiers[idx].escalatorValue = parseFloat(values.value) || 0;
                                                                        handleTierUpdate(newTiers);
                                                                    }}
                                                                    decimalScale={2}
                                                                    fixedDecimalScale={true}
                                                                    allowNegative={false}
                                                                    placeholder= {fee.profitShareEscalatorType === EscalatorFormatType.PERCENTAGE ? "Enter Percentage" : "Enter Amount"}
                                                                    suffix= {fee.profitShareEscalatorType === EscalatorFormatType.PERCENTAGE ? "%" : ""}
                                                                    prefix= {fee.profitShareEscalatorType === EscalatorFormatType.FIXEDAMOUNT ? "$" : ""}
                                                                    min={0}
                                                                    max={100}
                                                                    customInput={Input}
                                                                    disabled={!isEditable}
                                                                />
                                                            </div>
                                                        </div>
                                                    )}

                                                    {idx < tiers.length - 1 && (
                                                        <div className="space-y-2">
                                                            <Label>Amount</Label>

                                                            <Tooltip>
                                                                <TooltipTrigger asChild>
                                                                    <div className="inline-flex h-8 w-8 items-center justify-center rounded-md hover:bg-accent">
                                                                        <Info className="h-4 w-4" />
                                                                    </div>
                                                                </TooltipTrigger>
                                                                <TooltipContent>
                                                                    <p>Profit amount up to which this share percentage applies.</p>
                                                                </TooltipContent>
                                                            </Tooltip>
                                                            <div className="flex items-center space-x-2">
                                                                <NumericFormat
                                                                    data-qa-id={`input-tierAmount-${index}-${idx}-profitShare`}
                                                                    value={tier.amount}
                                                                    onValueChange={(values) => {
                                                                        const newTiers = [...tiers];
                                                                        newTiers[idx].amount = parseFloat(values.value) || 0;
                                                                        handleTierUpdate(newTiers);
                                                                    }}
                                                                    decimalScale={2}
                                                                    fixedDecimalScale={true}
                                                                    allowNegative={false}
                                                                    prefix="$"
                                                                    customInput={Input}
                                                                    className={!isAmountValid(idx) ? "border-red-500" : ""}
                                                                    disabled={!isEditable}
                                                                />
                                                            </div>
                                                            {!isAmountValid(idx) && (
                                                                <p className="text-sm text-red-500">
                                                                    Amount must be greater than the previous tier's amount.
                                                                </p>
                                                            )}
                                                        </div>
                                                    )}
                                                </div>
                                            </div>
                                        </Card>
                                    ))}





                                    {/* Tier Management Buttons */}
                                    <div className="flex justify-start space-x-2">
                                        <Button
                                            data-qa-id={`button-addTier-${index}-profitShare`}
                                            variant="outline"
                                            type="button"
                                            size="sm"
                                            onClick={() => handleTierUpdate([...tiers, { sharePercentage: 0, amount: 0 , escalatorValue: 0}])}
                                            disabled={!isEditable}
                                        >
                                            <PlusCircle className="w-4 h-4 mr-2" />
                                            Add Tier
                                        </Button>
                                        {tiers.length > 2 && (
                                            <Button
                                                data-qa-id={`button-removeTier-${index}-profitShare`}
                                                variant="outline"
                                                type="button"
                                                size="sm"
                                                onClick={() => {
                                                    const newTiers = [...tiers];
                                                    newTiers.pop();
                                                    if (newTiers.length > 0) {
                                                        newTiers[newTiers.length - 1].amount = 0;
                                                    }
                                                    handleTierUpdate(newTiers);
                                                }}
                                                disabled={!isEditable}
                                            >
                                                <MinusCircle className="w-4 h-4 mr-2" />
                                                Remove Tier
                                            </Button>
                                        )}
                                    </div>
                                </>
                            ) : (
                                <>
                                    <div className="space-y-2">
                                        <Label htmlFor={`share-percentage-${index}`}>Profit Share Percentage</Label>
                                        <Tooltip>
                                            <TooltipTrigger asChild>
                                                <div className="inline-flex h-8 w-8 items-center justify-center rounded-md hover:bg-accent">
                                                    <Info className="h-4 w-4" />
                                                </div>
                                            </TooltipTrigger>
                                            <TooltipContent>
                                                <p>This is the percentage of profit that Towne Park receives. By default, this will apply to all profit. You may also create Tiers to charge different profit percentages at different Amounts.</p>
                                            </TooltipContent>
                                        </Tooltip>
                                        <div className="flex items-center space-x-2">
                                            <NumericFormat
                                                data-qa-id={`input-sharePercentage-${index}-profitShare`}
                                                id={`share-percentage-${index}`}
                                                value={tiers[0]?.sharePercentage || null}
                                                onValueChange={(values) => {
                                                    handleTierUpdate([{
                                                        sharePercentage: parseFloat(values.value) || 0,
                                                        amount: 0,
                                                        escalatorValue: 0,
                                                    }]);
                                                }}
                                                decimalScale={2}
                                                fixedDecimalScale={true}
                                                allowNegative={false}
                                                placeholder="Enter Profit Share Percentage"
                                                suffix="%"
                                                min={0}
                                                max={100}
                                                customInput={Input}
                                                required
                                                disabled={!isEditable}
                                            />
                                        </div>
                                    </div>

                                    {/* Threshold Structure Checkbox */}
                                    <div className="space-y-2">
                                        <div className="flex items-center space-x-2">
                                            <Checkbox
                                                data-qa-id={`checkbox-enableThreshold-${index}-profitShare`}
                                                id={`threshold-structure-${index}`}
                                                checked={isThresholdTierEnabled}
                                                onCheckedChange={toggleThresholdStructure}
                                                disabled={!isEditable}
                                            />
                                            <Label htmlFor={`threshold-structure-${index}`}>Create Threshold Structure</Label>
                                        </div>
                                    </div>
                                </>
                            )}
                        </div>
                        {errors?.managementAgreement?.ManagementFees?.[index]?.profitShareTierData && (
                            <p className="text-red-500 text-sm">
                                {errors.managementAgreement.ManagementFees[index].profitShareTierData.message}
                            </p>
                        )}
                        {/* Validation Threshold */}
                        <Card>
                            <CardHeader>
                                <CardTitle>Validation</CardTitle>

                                <p className="text-sm text-muted-foreground">
                                    Parking validation revenue above the threshold set here will be added to the total validation amount before subtracting expenses and sharing profit with the owner.
                                </p>
                            </CardHeader>
                            <CardContent className="space-y-4">
                                <div className="flex items-center space-x-2">
                                    <Checkbox
                                        data-qa-id={`checkbox-enableValidationThreshold-${index}-profitShare`}
                                        id={`validation-threshold-${index}`}
                                        checked={fee.validationThresholdEnabled}
                                        onCheckedChange={(checked: boolean) => {
                                            if (isEditingProfitShareThresold) {
                                                onUpdate({ validationThresholdEnabled: checked as boolean })
                                            }
                                            setIsEditingProfitShareThresold(checked);
                                        }}
                                        disabled={!isEditable}
                                        onFocus={() => setIsEditingProfitShareThresold(true)}
                                        onBlur={() => setIsEditingProfitShareThresold(false)}
                                    />
                                    <Label htmlFor={`validation-threshold-${index}`}>Set Validation Threshold</Label>
                                </div>

                                {fee.validationThresholdEnabled && (
                                    <div className="space-y-2">
                                        <Label>Threshold Type</Label>
                                        <Select
                                            data-qa-id={`select-validationThresholdType-${index}-profitShare`}
                                            value={fee.validationThresholdType || ValidationThresholdType.REVENUE_PERCENTAGE}
                                            onValueChange={(value) =>
                                                onUpdate({ validationThresholdType: value as ValidationThresholdType })
                                            }
                                            disabled={!isEditable}
                                        >
                                            <SelectTrigger>
                                                <SelectValue placeholder="Select threshold type" />
                                            </SelectTrigger>
                                            <SelectContent>
                                                <SelectItem value={ValidationThresholdType.REVENUE_PERCENTAGE}>
                                                    Revenue Percentage
                                                </SelectItem>
                                                <SelectItem value={ValidationThresholdType.VALIDATION_AMOUNT}>
                                                    Validation Amount
                                                </SelectItem>
                                                <SelectItem value={ValidationThresholdType.VEHICLE_COUNT}>
                                                    Vehicle Count
                                                </SelectItem>
                                            </SelectContent>
                                        </Select>

                                        <div className="space-y-2">
                                            <Label>
                                                {fee.validationThresholdType === ValidationThresholdType.VALIDATION_AMOUNT
                                                    ? "Amount"
                                                    : fee.validationThresholdType === ValidationThresholdType.VEHICLE_COUNT
                                                        ? "Monthly Number of Vehicles"
                                                        : "Percentage"
                                                }
                                            </Label>
                                            <NumericFormat
                                                data-qa-id={`input-validationThresholdAmount-${index}-profitShare`}
                                                type="text"
                                                value={fee.validationThresholdAmount ?? ''}
                                                onValueChange={(values) => onUpdate({
                                                    validationThresholdAmount: parseFloat(values.value) || null
                                                })}
                                                decimalScale={fee.validationThresholdType === ValidationThresholdType.VEHICLE_COUNT ? 0 : 2}
                                                fixedDecimalScale={fee.validationThresholdType !== ValidationThresholdType.VEHICLE_COUNT}
                                                allowNegative={false}
                                                prefix={fee.validationThresholdType === ValidationThresholdType.VALIDATION_AMOUNT ? "$" : undefined}
                                                suffix={fee.validationThresholdType === ValidationThresholdType.REVENUE_PERCENTAGE ? "%" : undefined}
                                                customInput={Input}
                                                placeholder={`Enter ${fee.validationThresholdType === ValidationThresholdType.VEHICLE_COUNT ? 'number of vehicles' : 'threshold value'}`}
                                                required
                                                disabled={!isEditable}
                                            />
                                        </div>
                                        {errors?.managementAgreement?.ManagementFees?.[index]?.validationThresholdAmount && (
                                            <p className="text-red-500 text-sm">
                                                {errors.managementAgreement.ManagementFees[index].validationThresholdAmount.message}
                                            </p>
                                        )}
                                    </div>
                                )}
                            </CardContent>
                        </Card>
                    </div>
                )}
            </CardContent>
        </Card>
    );
};

const ClaimsSection: React.FC<ClaimsSectionProps> = ({
    fee,
    index,
    claimsExpenseAccounts,
    onUpdate,
    errors,
    isEditable
}) => {
    const [isEditingClaimsSection, setIsEditingClaimsSection] = useState(false);

    return (
        <Card>
            <CardHeader>
                <CardTitle className="text-2xl font-bold">Claims</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
                <p className="text-sm text-muted-foreground">
                    Claims are typically the sum of four general ledger accounts: 7099 - Loss & Damage - Prior Year, 7100 - Loss & Damage, 7101 - Service Recovery, and 7102 - Claims Handling Fees. If Claims are charged to the customer here, then the sum of those accounts will not be included in the sum of the Billable Expense Accounts line-item which appears on the invoice.
                </p>
                <div className="flex items-center space-x-2">
                    <Checkbox
                        data-qa-id="checkbox-enableClaims-claims"
                        id="create-claims-line-item"
                        data-testid="create-claims-line-item"
                        checked={fee.claimsEnabled}
                        onCheckedChange={(checked: boolean) => {
                            if (isEditingClaimsSection) {
                                onUpdate({ claimsEnabled: checked })
                            }
                            setIsEditingClaimsSection(checked);
                        }}
                        disabled={!isEditable}
                        onFocus={() => setIsEditingClaimsSection(true)}
                        onBlur={() => setIsEditingClaimsSection(false)}
                    />
                    <Label htmlFor="create-claims-line-item">Create Claims Line-item</Label>
                </div>

                {fee.claimsEnabled && (
                    <>
                        <div className="space-y-4">
                            <div className="space-y-2">
                                <Label>Billable Claims Accounts</Label>
                                <p className="text-sm text-muted-foreground">
                                    These are the Claims accounts which are not excluded in the Billable Expense Accounts section below. You may set a limit on the amount of claims expense that will be charged to the customer on an annual (either calendar-year or contract anniversary-year) or per-claim basis.
                                </p>
                                {claimsExpenseAccounts.length > 0 ? (
                                    claimsExpenseAccounts.map(account => (
                                        <div key={account.code} className="flex items-center space-x-2">
                                            <Checkbox
                                                data-qa-id={`checkbox-claimsAccount-${account.code}-claims`}
                                                id={`claims-account-${account.code}`}
                                                checked
                                                disabled
                                            />
                                            <Label htmlFor={`claims-account-${account.code}`}>{account.title}</Label>
                                        </div>
                                    ))
                                ) : (
                                    <p className="text-sm text-muted-foreground">No billable claims accounts found.</p>
                                )}
                            </div>

                            <div className="space-y-2">
                                <div className="flex items-center space-x-2">
                                    <Label>Claims Cap Type</Label>
                                    <Tooltip>
                                        <TooltipTrigger asChild>
                                            <div className="inline-flex h-8 w-8 items-center justify-center rounded-md hover:bg-accent">
                                                <Info className="h-4 w-4" />
                                            </div>
                                        </TooltipTrigger>
                                        <TooltipContent>
                                            <p>This setting determines the duration for the claims expenses to accumulate before being limited by the claims cap.</p>
                                        </TooltipContent>
                                    </Tooltip>
                                </div>
                                <Select
                                    data-qa-id="select-claimsCapType-claims"
                                    value={fee.claimsType || ClaimsType.ANNUALLY_CALENDAR}
                                    onValueChange={(value) =>
                                        onUpdate({ claimsType: value as ClaimsType })
                                    }
                                    disabled={!isEditable}
                                >
                                    <SelectTrigger>
                                        <SelectValue placeholder="Select Cap Type" />
                                    </SelectTrigger>
                                    <SelectContent>
                                        <SelectItem value={ClaimsType.ANNUALLY_CALENDAR}>Annual (Calendar)</SelectItem>
                                        <SelectItem value={ClaimsType.ANNUALLY_ANIVERSARY}>Annual (Anniversary)</SelectItem>
                                        <SelectItem value={ClaimsType.PER_CLAIM}>Per Claim</SelectItem>
                                    </SelectContent>
                                </Select>
                            </div>

                            <div className="space-y-2">
                                <Label htmlFor="claims-cap-amount">Claims Cap Amount</Label>
                                <div className="flex items-center space-x-2">
                                    <NumericFormat
                                        data-qa-id={`input-claimsCapAmount-${index}-claims`}
                                        id="claims-cap-amount"

                                        displayType="input"
                                        decimalScale={2}
                                        fixedDecimalScale={true}
                                        prefix="$"
                                        inputMode="decimal"
                                        allowNegative={false}
                                        value={fee.claimsCapAmount || ''}
                                        onValueChange={(values) =>
                                            onUpdate({
                                                claimsCapAmount: parseFloat(values.floatValue?.toString() || '0')
                                            })
                                        }
                                        placeholder="Enter cap amount"
                                        data-testid={`claims-cap-amount-${index}`}
                                        customInput={Input}
                                        required
                                        disabled={!isEditable}
                                    />
                                </div>
                                {errors?.managementAgreement?.ManagementFees?.[index]?.claimsCapAmount && (
                                    <p className="text-red-500 text-sm">
                                        {errors.managementAgreement.ManagementFees[index].claimsCapAmount.message}
                                    </p>
                                )}
                                {fee.claimsType === ClaimsType.PER_CLAIM && (
                                    <p className="text-sm text-muted-foreground">
                                        NOTE: A cap cannot be automatically calculated on a per-claim basis at this time. The system will add all claim amounts to the billing statement. Towne Park accounting team must ensure any amount over the Per Claim cap is credited back to the Customer by adding an Ad-hoc Line-item on the billing statement for any amount over the limit.
                                    </p>
                                )}
                            </div>

                            <div className="space-y-2">
                                <Label htmlFor="nonglexpense-line-item-display-name">Line-item Title</Label>
                                <Input
                                    data-qa-id={`input-claimsLineTitle-${index}-claims`}
                                    id="nonglexpense-line-item-display-name"
                                    value={fee.claimsLineTitle || ''}
                                    onChange={(e) =>
                                        onUpdate({
                                            claimsLineTitle: e.target.value
                                        })
                                    }
                                    placeholder="Enter display name"

                                    data-testid={`nonglexpense-line-item-display-name-${index}`}
                                    required
                                    disabled={!isEditable}
                                />
                            </div>
                        </div>
                    </>

                )}
            </CardContent>
        </Card>
    );
};
const NonGLBillableExpensesSection: React.FC<NonGLBillableExpensesSectionProps> = ({
    fee,
    index,
    onUpdate,
    errors,
    isEditable,
}) => {
    const [enabled, setEnabled] = useState(fee.nonGlBillableExpensesEnabled ?? false);
    const parseMonthToUTC = (monthString: string | null): Date | null => {
        if (!monthString) return null;
        // Add 1 day to prevent date rollback
        return new Date(Date.UTC(parseInt(monthString.split('-')[0]), parseInt(monthString.split('-')[1]) - 1, 2));
      };
      
      const formatUTCDateToMonth = (date: Date | null): string => {
        if (!date) return '';
        // Use ISO string and split instead of getUTCMonth()
        return date.toISOString().split('T')[0].substring(0, 7);
      };
      const [expenses, setExpenses] = useState<NonGlBillableExpenseDto[]>(() => {
        if (!fee.nonGlBillableExpensesEnabled) return [];
        
        return fee.nonGlBillableExpenses.length > 0 
        
          ? fee.nonGlBillableExpenses.map(expense => ({
              ...expense,
              finalperiodbilled: expense.finalperiodbilled
                ? parseMonthToUTC(
                    typeof expense.finalperiodbilled === 'string' 
                      ? expense.finalperiodbilled
                      : formatUTCDateToMonth(expense.finalperiodbilled)
                  )
                : null
            }))
          : [{
              nonglexpensetype: NonGlExpensetype.FIXEDAMOUNT,
              expensepayrolltype: ExpensetPayrollType.BILLABLE,
              expenseamount: null,
              expensetitle: '',
              id: "",
              finalperiodbilled: null,
              sequenceNumber:1
            }];
      });
      

      const updateExpense = (id: number, field: keyof NonGlBillableExpenseDto, value: any) => {
        setExpenses(prev => {
          const updated = prev.map((expense, idx) => 
            idx === id ? { ...expense, [field]: value } : expense
          );
          onUpdate({
            nonGlBillableExpensesEnabled: enabled,
            nonGlBillableExpenses: updated,
          });
          return updated;
        });
      };

    const addExpense = () => {
         const maxSequence = expenses.length > 0
        ? Math.max(...expenses.map(exp => exp.sequenceNumber || 0))
        : 0;
        const newExpense: NonGlBillableExpenseDto = {
            nonglexpensetype: NonGlExpensetype.FIXEDAMOUNT,
            expensepayrolltype: ExpensetPayrollType.BILLABLE,
            expenseamount: null,
            expensetitle: "",
            id: "",
            finalperiodbilled: null,
            sequenceNumber:maxSequence + 1
        };
        const updatedExpenses = [...expenses, newExpense];
        setExpenses(updatedExpenses);
        onUpdate({
            nonGlBillableExpensesEnabled: enabled,
            nonGlBillableExpenses: updatedExpenses,
        });
    };

    const removeExpense = (id: number) => {
        const updatedExpenses = expenses.filter((_, idx) => idx !== id);
        setExpenses(updatedExpenses);
        onUpdate({
            nonGlBillableExpensesEnabled: enabled,
            nonGlBillableExpenses: updatedExpenses,
        });
    };

    const clearExpenses = () => {
        setExpenses([]);
        setEnabled(false);
        onUpdate({
            nonGlBillableExpensesEnabled: false,
            nonGlBillableExpenses: undefined,
        });
    };
    const [isEditingNonGLSection, setIsEditingNonGLSection] = useState(false);

    return (
        <Card>
            <CardHeader>
                <div className="flex items-center justify-between">
                    <CardTitle className="text-2xl font-bold">Non-GL Billable Expenses</CardTitle>
                    {enabled && (
            <Button
                onClick={(e) => {
                    e.preventDefault();
                    clearExpenses();
                }}
                disabled={!isEditable}
                variant="ghost"
            >
                Clear
            </Button>
        )}
                </div>
            </CardHeader>
            <CardContent className="space-y-4">
                <p className="text-sm text-muted-foreground">
                    Non-GL Billable Expenses are additional charges that can be added to the invoice.
                </p>

                <div className="flex items-center space-x-2">
                    <Checkbox
                    id="nonglexpensenable"
                        checked={fee.nonGlBillableExpensesEnabled}
                        onCheckedChange={(checked: boolean) => {
                            if (checked && expenses.length === 0) {
                                setExpenses([
                                    {
                                        nonglexpensetype: NonGlExpensetype.FIXEDAMOUNT,
                                        expensepayrolltype: ExpensetPayrollType.BILLABLE,
                                        expenseamount: null,
                                        expensetitle: "",
                                        id: "",
                                        finalperiodbilled: null,
                                        sequenceNumber:1
                                    },
                                ]);
                            }
                            if (isEditingNonGLSection) {
                                setEnabled(checked);
                                onUpdate({
                                    nonGlBillableExpensesEnabled: checked,
                                    nonGlBillableExpenses: checked ? expenses : [],
                                });
                            }
                            setIsEditingNonGLSection(checked);
                        }}
                        disabled={!isEditable}
                        onFocus={() => setIsEditingNonGLSection(true)}
                        onBlur={() => setIsEditingNonGLSection(false)}
                    />
                    <Label>Enable Non-GL Billable Expenses</Label>
                </div>

                {enabled &&
                    expenses.map((expense, idx) => (
                        <Card key={idx} className="p-4 relative">
                            <div className="flex justify-between items-center mb-4">
                                <h3 className="font-semibold">Expense Item {idx + 1}</h3>
                                {idx > 0 && (
                                    <Button
                                    type="button"
                                        variant="ghost"
                                        size="sm"
                                        onClick={() => removeExpense(idx)}
                                        disabled={!isEditable}
                                    >
                                        ×
                                    </Button>
                                )}
                            </div>
                            <div className="space-y-4">
                         
                                <div className="space-y-2">
                                    <Label>Type</Label>
                                    <RadioGroup
                                        value={expense.nonglexpensetype}
                                        onValueChange={(value) =>
                                            updateExpense(idx, "nonglexpensetype", value)
                                        }
                                        disabled={!isEditable}
                                    >
                                        <div className="space-y-2">
                                            <div className="flex items-center space-x-2">
                                                <RadioGroupItem
                                                    value={NonGlExpensetype.FIXEDAMOUNT}
                                                    id={`fixed-${idx}`}
                                                />
                                                <Label htmlFor={`fixed-${idx}`}>Fixed Amount</Label>
                                            </div>
                                            <div className="flex items-center space-x-2">
                                                <RadioGroupItem
                                                    value={NonGlExpensetype.PAYROLL}
                                                    id={`payroll-${idx}`}
                                                />
                                                <Label htmlFor={`payroll-${idx}`}>% Payroll</Label>
                                            </div>
                                            <div className="flex items-center space-x-2">
                                                <RadioGroupItem
                                                    value={NonGlExpensetype.REVENUE}
                                                    id={`revenue-${idx}`}
                                                />
                                                <Label htmlFor={`revenue-${idx}`}>% Revenue</Label>
                                            </div>
                                        </div>
                                    </RadioGroup>
                                    {expense.nonglexpensetype === NonGlExpensetype.REVENUE && (
                                        <p className="text-sm text-muted-foreground">
                                            This is calculated as a percentage of Sum(Net_External_Revenue) for the month.
                                        </p>
                                    )}
                                </div>

                              
                                {expense.nonglexpensetype === NonGlExpensetype.PAYROLL && (
                                    <div className="space-y-2">
                                        <Label>Payroll Type</Label>
                                        <RadioGroup
                                            value={expense.expensepayrolltype}
                                            onValueChange={(value) =>
                                                updateExpense(idx, "expensepayrolltype", value)
                                            }
                                            disabled={!isEditable}
                                        >
                                            <div className="space-y-2">
                                                <div className="flex items-center space-x-2">
                                                    <RadioGroupItem
                                                        value={ExpensetPayrollType.BILLABLE}
                                                        id={`billable-${idx}`}
                                                    />
                                                    <Label htmlFor={`billable-${idx}`}>
                                                        Billable Payroll
                                                    </Label>
                                                </div>
                                                <div className="flex items-center space-x-2">
                                                    <RadioGroupItem
                                                        value={ExpensetPayrollType.TOTAL}
                                                        id={`total-${idx}`}
                                                    />
                                                    <Label htmlFor={`total-${idx}`}>
                                                        Total Payroll
                                                    </Label>
                                                </div>
                                            </div>
                                        </RadioGroup>
                                        <p className="text-sm text-muted-foreground">
                                            Select whether this percentage applies to billable payroll or total payroll.
                                        </p>
                                    </div>
                                )}


                                <div className="space-y-2">
                                    <Label>
                                        {expense.nonglexpensetype === NonGlExpensetype.FIXEDAMOUNT
                                            ? "Amount"
                                            : "Percentage"}
                                    </Label>
                                    <NumericFormat
                                        customInput={Input}
                                        required
                                        value={expense.expenseamount ?? ""}
                                        onValueChange={(values) => {
                                            updateExpense(
                                                idx,
                                                "expenseamount",
                                                values.floatValue === undefined ? null : values.floatValue
                                            );
                                        }}
                                        prefix={expense.nonglexpensetype === NonGlExpensetype.FIXEDAMOUNT ? "$" : ""}
                                          suffix={
        expense.nonglexpensetype !== NonGlExpensetype.FIXEDAMOUNT &&
        (expense.nonglexpensetype === NonGlExpensetype.REVENUE ||
            expense.expensepayrolltype === ExpensetPayrollType.BILLABLE ||
            expense.expensepayrolltype === ExpensetPayrollType.TOTAL)
            ? "%"
            : ""
    }
                                        decimalScale={3}
                                        fixedDecimalScale={
                                            expense.nonglexpensetype === NonGlExpensetype.REVENUE ||
                                            expense.expensepayrolltype === ExpensetPayrollType.TOTAL ||
                                            expense.expensepayrolltype === ExpensetPayrollType.BILLABLE

                                        }
                                        allowNegative={false}
                                        placeholder={`Enter ${expense.nonglexpensetype === NonGlExpensetype.FIXEDAMOUNT
                                                ? "amount"
                                                : "percentage"
                                            }`}
                                        disabled={!isEditable}
                                        data-qa-id={`input-amount-${idx}`}
                                    />
                                </div>
                                {errors?.managementAgreement?.ManagementFees?.[index]?.nonGlBillableExpenses?.[idx]?.expenseamount && (
                                    <p className="text-red-500 text-sm mt-1">
                                        {errors.managementAgreement.ManagementFees[index].nonGlBillableExpenses[idx].expenseamount.message}
                                    </p>
                                )}
                                <div className="space-y-2">
                                    <Label>Title</Label>
                                    <Input
                                        required
                                        type="text"
                                        placeholder="Billable Equipment"
                                        value={expense.expensetitle || ""}
                                        onChange={(e) =>
                                            updateExpense(idx, "expensetitle", e.target.value)
                                        }
                                        disabled={!isEditable}
                                    />
                                </div>
                                {errors?.managementAgreement?.ManagementFees?.[index]?.nonGlBillableExpenses?.[idx]?.expensetitle && (
                                    <p className="text-red-500 text-sm mt-1">
                                        {errors.managementAgreement.ManagementFees[index].nonGlBillableExpenses[idx].expensetitle.message}
                                    </p>
                                )}
                                <div className="space-y-2">
                                    <Label htmlFor={`final-period-${idx}`}>Final Period Billed</Label>
                                    <Tooltip>
                                        <TooltipTrigger asChild>
                                            <div className="inline-flex h-8 w-8 items-center justify-center rounded-md hover:bg-accent">
                                                <Info className="h-4 w-4" />
                                            </div>
                                        </TooltipTrigger>
                                        <TooltipContent>
                                            <p>This expense will be charged monthly until the Final Period Billed Service Period (Arrears only), after which it will no longer be billed. If left blank, it will continue to be billed indefinitely.</p>
                                        </TooltipContent>
                                    </Tooltip>
                                    <Input
  id={`final-period-${idx}`}
  type="month"
  value={formatUTCDateToMonth(expense.finalperiodbilled)}
  onChange={(e) => {
    // Ensure valid date before updating
    if (e.target.validity.valid) {
      const dateValue = parseMonthToUTC(e.target.value);
      updateExpense(idx, 'finalperiodbilled', dateValue);
    }
  }}
  disabled={!isEditable}
  data-qa-id={`input-finalPeriod-${idx}`}
/>

                                    <p className="text-sm text-muted-foreground">
                                        If left blank, this expense will continue to be billed indefinitely.
                                    </p>
                                </div>
                            </div>
                        </Card>
                    ))}

                {enabled && (
                    <Button
                        variant="outline"
                        type="button"
                        disabled={!isEditable}
                        size="sm"
                        onClick={addExpense}
                    >
                        <PlusCircle className="w-4 h-4 mr-2" />
                        Add Expense
                    </Button>
                )}
            </CardContent>
        </Card>
    );
};






export default ManagementAgreement;