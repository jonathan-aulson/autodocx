import RevenueShare from "@/components/RevenueShare/RevenueShare";
import {
    Accordion,
    AccordionContent,
    AccordionItem,
    AccordionTrigger,
} from "@/components/ui/accordion";
import { Dialog, DialogContent, DialogFooter } from "@/components/ui/dialog";
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuLabel,
    DropdownMenuSeparator,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
    Form,
    FormControl,
    FormDescription,
    FormField,
    FormItem,
    FormLabel,
    FormMessage,
} from "@/components/ui/form";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Separator } from "@/components/ui/separator";
import { Switch } from "@/components/ui/switch";
import { useToast } from "@/components/ui/use-toast";
import { useAuth } from "@/contexts/AuthContext";
import {
    AdvancedArrearsType,
    BellServiceFeeTerms,
    BillablePayrollTerms,
    ClaimsType,
    Contract,
    ContractConfig,
    ContractWithDataEntitySchema,
    DepositedRevenueTerms,
    EscalatorFormatType,
    ExpensetPayrollType,
    GLCode,
    InsuranceType,
    ManagementAgreementTerms,
    ManagementAgreementType,
    MidMonthAdvancedTerms,
    Month,
    NonGlBillableExpenseDto,
    NonGlExpensetype,
    PaymentTermsType,
    PayrollAccount,
    ProfitShareAccumulationType,
    PTEBBillingType,
    SupportingReportsType,
    SupportPayrollType,
    SupportServicesType,
    ThresholdStructure,
    ValidationThresholdType
} from "@/lib/models/Contract";
import { CustomerDetail } from "@/lib/models/GeneralInfo";
import { validateAlphanumeric } from "@/lib/utils";
import { zodResolver } from "@hookform/resolvers/zod";
import { Info, Trash } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { SubmitHandler, useFieldArray, useForm } from "react-hook-form";
import { NumericFormat } from "react-number-format";
import PulseLoader from "react-spinners/PulseLoader";
import { z } from "zod";
import AdditionalFees from "../AdditionalFees/AdditionalFees";
import BillablePayrollComponent from "../BillablePayroll/BillablePayroll";
import ManagementAgreement from "../ManagementAgreement/ManagementAgreement";
import SupportingReports from "../SupportingReportsToggles/SupportingReportsToggles";
import { Button } from "../ui/button";
import { Input } from "../ui/input";
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "../ui/select";
import { Textarea } from "../ui/textarea";
import {
    Tooltip,
    TooltipContent,
    TooltipProvider,
    TooltipTrigger,
} from "../ui/tooltip";
import { setQuarter } from "date-fns";

export default function ContractDetailsTab({
    contractDetails,
    customerDetail,
    onUpdateContractDetails,
}: {
    contractDetails: Contract | null;
    customerDetail: CustomerDetail;
    onUpdateContractDetails: (updatedContract: Contract) => void;
}) {
    type FormValues = z.infer<typeof ContractWithDataEntitySchema>;

    const [isLoading, setIsLoading] = useState(false);
    const { userRoles } = useAuth();
    
    const [invoiceGroups, setInvoiceGroups] = useState(
        contractDetails?.invoiceGrouping?.invoiceGroups || []
    );
    const [isDialogOpen, setIsDialogOpen] = useState(false);
    const [formError, setFormError] = useState<string | null>(null);
    const daysDueRef = useRef<HTMLDivElement | null>(null);
    const [revenueShareEnabled, setRevenueShareEnabled] = useState(contractDetails?.revenueShare?.enabled || false);
    const [thresholdStructures, setThresholdStructures] = useState<ThresholdStructure[]>(contractDetails?.revenueShare?.thresholdStructures || []);

    const currentBellServiceFee: BellServiceFeeTerms = {
        enabled: contractDetails?.bellServiceFee?.enabled || false,
        bellServices: contractDetails?.bellServiceFee?.bellServices?.length
            ? contractDetails.bellServiceFee.bellServices.map(service => ({
                id: service.id || "",
                invoiceGroup: Number(service.invoiceGroup || 1)
            }))
            : []
    };

    const currentMidMonthAdvanced: MidMonthAdvancedTerms = {
        enabled: contractDetails?.midMonthAdvance?.enabled || false,
        midMonthAdvances: contractDetails?.midMonthAdvance?.midMonthAdvances?.length
            ? contractDetails?.midMonthAdvance?.midMonthAdvances?.map(midPayment => ({
                id: midPayment.id || "",
                amount: midPayment.amount || 0,
                lineTitle: midPayment.lineTitle || null,
                invoiceGroup: Number(midPayment.invoiceGroup || 1)
            }))
            : []
    };

    const currentDepositedRevenue: DepositedRevenueTerms = {
        enabled: contractDetails?.depositedRevenue?.enabled || false,
        depositData: contractDetails?.depositedRevenue?.depositData?.length
            ? contractDetails?.depositedRevenue?.depositData?.map(deposit => ({
                id: deposit.id || "",
                towneParkResponsibleForParkingTax: deposit.towneParkResponsibleForParkingTax || false,
                depositedRevenueEnabled: deposit.depositedRevenueEnabled || false,
                invoiceGroup: Number(deposit.invoiceGroup || 1)
            }))
            : []
    };

    const currentBillablePayroll: BillablePayrollTerms = {
        enabled: contractDetails?.billableAccounts?.enabled || false,
        billableAccountsData: contractDetails?.billableAccounts?.billableAccountsData.length
            ? contractDetails?.billableAccounts?.billableAccountsData?.map(account => ({
                id: account.id || "",
                payrollAccountsData: account.payrollAccountsData || "",
                payrollAccountsLineTitle: account.payrollAccountsLineTitle || "",
                payrollAccountsInvoiceGroup: Number(account.payrollAccountsInvoiceGroup || 1),
                payrollTaxesEnabled: account.payrollTaxesEnabled || false,
                payrollTaxesBillingType: account.payrollTaxesBillingType || PTEBBillingType.ACTUAL,
                payrollTaxesPercentage: account.payrollTaxesPercentage || null,
                payrollTaxesLineTitle: account.payrollTaxesLineTitle || '',
                payrollSupportEnabled: account.payrollSupportEnabled || false,
                payrollSupportBillingType: account.payrollSupportBillingType || SupportServicesType.FIXED,
                payrollSupportPayrollType: account.payrollSupportPayrollType || SupportPayrollType.BILLABLE,
                payrollSupportAmount: account.payrollSupportAmount || null,
                payrollSupportLineTitle: account.payrollSupportLineTitle || '',
                payrollExpenseAccountsData: account.payrollExpenseAccountsData || "",
                payrollExpenseAccountsLineTitle: account.payrollExpenseAccountsLineTitle || "",
                additionalPayrollAmount: account.additionalPayrollAmount || 0,
                payrollExpenseAccountsInvoiceGroup: Number(account.payrollExpenseAccountsInvoiceGroup || 1),
                payrollTaxesEscalatorEnable:account.payrollTaxesEscalatorEnable || false,
                payrollTaxesEscalatorMonth:account.payrollTaxesEscalatorMonth || Month.JANUARY,
                payrollTaxesEscalatorvalue:account.payrollTaxesEscalatorvalue || 0,
                payrollTaxesEscalatorType:account.payrollTaxesEscalatorType || EscalatorFormatType.PERCENTAGE,


            }))
            : []
    };

    const currentManagementAgreement: ManagementAgreementTerms = {
        enabled: contractDetails?.managementAgreement?.enabled || false,
        ManagementFees: contractDetails?.managementAgreement?.ManagementFees.length
            ? contractDetails?.managementAgreement?.ManagementFees?.map(agreement => ({
                id: agreement.id || "",
                invoiceGroup: Number(agreement.invoiceGroup || 1),
                managementAgreementType: agreement.managementAgreementType || ManagementAgreementType.FIXED_FEE,
                managementFeeEscalatorEnabled: agreement.managementFeeEscalatorEnabled || false,
                managementFeeEscalatorMonth: agreement.managementFeeEscalatorMonth || Month.JANUARY,
                managementFeeEscalatorType: agreement.managementFeeEscalatorType || EscalatorFormatType.PERCENTAGE,
                managementFeeEscalatorValue: agreement.managementFeeEscalatorValue || 0,
                fixedFeeAmount: agreement.fixedFeeAmount || 0,
                perLaborHourJobCodeData: agreement.perLaborHourJobCodeData?.map(jobCode => ({
                    ...agreement,
                    code: jobCode.code ?? "",
                    description: jobCode.description ?? "",
                    standardRate: jobCode.standardRate ?? 0,
                    overtimeRate: jobCode.overtimeRate ?? 0,
                    standardRateEscalatorValue: jobCode.standardRateEscalatorValue ?? 0,
                    overtimeRateEscalatorValue: jobCode.overtimeRateEscalatorValue ?? 0,
                })),
                laborHourJobCode: agreement.laborHourJobCode || "",
                laborHourRate: agreement.laborHourRate || 0,
                laborHourOvertimeRate: agreement.laborHourOvertimeRate || 0,
                revenuePercentageAmount: agreement.revenuePercentageAmount || 0,
                insuranceEnabled: agreement.insuranceEnabled || false,
                insuranceLineTitle: agreement.insuranceLineTitle || "",
                insuranceType: agreement.insuranceType || InsuranceType.BASED_ON_BILLABLE_ACCOUNTS,
                insuranceAdditionalPercentage: agreement.insuranceAdditionalPercentage || 0,
                insuranceFixedFeeAmount: agreement.insuranceFixedFeeAmount || 0,
                claimsEnabled: agreement.claimsEnabled || false,
                claimsType: agreement.claimsType || ClaimsType.ANNUALLY_ANIVERSARY,
                claimsCapAmount: agreement.claimsCapAmount || 0,
                claimsLineTitle: agreement.claimsLineTitle || null,
                profitShareEnabled: agreement.profitShareEnabled || false,
                profitShareAccumulationType: agreement.profitShareAccumulationType || ProfitShareAccumulationType.MONTHLY,
                profitShareTierData: agreement.profitShareTierData?.map(tier => ({
                    ...agreement,
                    sharePercentage: tier.sharePercentage ?? 0,
                    amount: tier.amount ?? 0,
                    escalatorValue: tier.escalatorValue ?? 0,
                })),
                profitShareEscalatorEnabled: agreement.profitShareEscalatorEnabled || false,
                profitShareEscalatorMonth: agreement.profitShareEscalatorMonth || Month.JANUARY,
                profitShareEscalatorType: agreement.profitShareEscalatorType || EscalatorFormatType.PERCENTAGE,
                validationThresholdEnabled: agreement.validationThresholdEnabled || false,
                validationThresholdAmount: agreement.validationThresholdAmount || 0,
                validationThresholdType: agreement.validationThresholdType || ValidationThresholdType.VALIDATION_AMOUNT,
                nonGlBillableExpensesEnabled:agreement.nonGlBillableExpensesEnabled || false,
                nonGlBillableExpenses: agreement.nonGlBillableExpenses
                ?.filter((expense): expense is NonGlBillableExpenseDto => !!expense) // Filter out undefined items
                .map(expense => ({
                    id: expense.id || '',
                    nonglexpensetype: expense.nonglexpensetype || NonGlExpensetype.FIXEDAMOUNT,
                    expensepayrolltype: expense.expensepayrolltype || ExpensetPayrollType.BILLABLE,
                    expenseamount: expense.expenseamount ?? 0,
                    expensetitle: expense.expensetitle || "Default Expense Title",
                    finalperiodbilled: expense.finalperiodbilled || null,
                    sequenceNumber:expense.sequenceNumber || 1
                })) || []
            }))
            : []
    };

    const validateRevenueCodesAllocation = () => {
        const allSelectedCodes = new Set<string>();
        let duplicateFound = false;

        for (const structure of thresholdStructures) {
            for (const code of structure.revenueCodes) {
                if (allSelectedCodes.has(code)) {
                    duplicateFound = true;
                    break;
                }
                allSelectedCodes.add(code);
            }
        }

        if (duplicateFound) {
            toast({
                description:
                    "Revenue codes must be allocated uniquely to one structure.",
            });
            return false;
        }

        if (allSelectedCodes.size !== revenueCodes.length) {
            toast({
                title: "Check Revenue Codes",
                description: "All revenue codes must be allocated to a structure.",
            });
            return false;
        }

        return true;
    };

    const defaultValues: FormValues = {
        purchaseOrder: contractDetails?.purchaseOrder || "",
        paymentTerms: contractDetails?.paymentTerms || "",
        billingType:
            contractDetails?.billingType || AdvancedArrearsType.ADVANCED,
        incrementMonth: contractDetails?.incrementMonth || "January",
        incrementAmount: contractDetails?.incrementAmount || 0,
        consumerPriceIndex: contractDetails?.consumerPriceIndex ?? false,
        contractType: contractDetails?.contractType || "",
        deposits: contractDetails?.deposits || false,
        notes: contractDetails?.notes || "",
        fixedFee: {
            enabled: contractDetails?.fixedFee?.enabled ?? false,
            serviceRates: (
                contractDetails?.fixedFee.serviceRates &&
                    contractDetails.fixedFee.serviceRates.length > 0
                    ? contractDetails.fixedFee.serviceRates
                    : []
            ).map((service) => ({
                ...service,
                invoiceGroup: String(service.invoiceGroup ?? "1"),
            })),
        },
        perLaborHour: {
            enabled: contractDetails?.perLaborHour?.enabled ?? false,

            hoursBackupReport:
                contractDetails?.perLaborHour?.hoursBackupReport ?? true,
            jobRates: (
                contractDetails?.perLaborHour?.jobRates &&
                    contractDetails.perLaborHour.jobRates.length > 0
                    ? contractDetails.perLaborHour.jobRates
                    : []
            ).map((job) => ({
                ...job,
                invoiceGroup: String(job.invoiceGroup ?? "1")
            }))
        },
        perOccupiedRoom: {
            enabled: contractDetails?.perOccupiedRoom?.enabled ?? false,
            roomRate: contractDetails?.perOccupiedRoom?.roomRate || 0,
            invoiceGroup: String(contractDetails?.perOccupiedRoom?.invoiceGroup || "1")
        },
        deviationPercentage: contractDetails?.deviationPercentage || 0,
        deviationAmount: contractDetails?.deviationAmount || 0,
        invoiceGrouping: {
            enabled: contractDetails?.invoiceGrouping?.enabled || false,
            invoiceGroups: contractDetails?.invoiceGrouping?.invoiceGroups || []
        },
        revenueShare: {
            enabled: revenueShareEnabled,
            thresholdStructures: thresholdStructures.map(structure => ({
                ...structure,
                tiers: structure.tiers.map(tier => ({
                    ...tier,
                    amount: tier.amount ?? 0
                })),
                invoiceGroup: Number(structure.invoiceGroup) || 1,
                validationThresholdType: structure.validationThresholdType || null,
                validationThresholdAmount: structure.validationThresholdAmount || 0,
            })),
        },
        bellServiceFee: currentBellServiceFee,
        midMonthAdvance: currentMidMonthAdvanced,
        depositedRevenue: currentDepositedRevenue,
        billableAccounts: currentBillablePayroll,
        managementAgreement: {
            ...currentManagementAgreement,
            ManagementFees: currentManagementAgreement?.ManagementFees?.map(fee => ({
                ...fee,
                profitShareTierData: fee.profitShareTierData?.map(tier => ({
                    ...tier,
                    amount: tier.amount ?? 0,
                    escalatorValue: tier.escalatorValue ?? 0,
                })),
                nonGlBillableExpenses: fee.nonGlBillableExpenses?.map(expense => ({
                    ...expense,
                    id:expense.id || '',
                    nonglexpensetype: expense.nonglexpensetype || NonGlExpensetype.FIXEDAMOUNT,
                    expensepayrolltype: expense.expensepayrolltype || ExpensetPayrollType.BILLABLE, 
                    expenseamount: expense.expenseamount ?? 0,
                    expensetitle: expense.expensetitle || "Default Expense Title", 
                    finalperiodbilled: expense.finalperiodbilled ?? null,
                    sequenceNumber: expense.sequenceNumber || 1
                })),
            })),
        },
        supportingReports: contractDetails?.supportingReports || [],
    };

    const reportTypes = contractDetails?.supportingReports || [];

    const [reportsState, setReportsState] = useState<Record<SupportingReportsType, boolean>>({
        [SupportingReportsType.MIX_OF_SALES]: reportTypes.includes(SupportingReportsType.MIX_OF_SALES),
        [SupportingReportsType.HOURS_BACKUP_REPORT]: reportTypes.includes(SupportingReportsType.HOURS_BACKUP_REPORT),
        [SupportingReportsType.TAX_REPORT]: reportTypes.includes(SupportingReportsType.TAX_REPORT),
        [SupportingReportsType.LABOR_DISTRIBUTION_REPORT]: reportTypes.includes(SupportingReportsType.LABOR_DISTRIBUTION_REPORT),
        [SupportingReportsType.OTHER_EXPENSES]: reportTypes.includes(SupportingReportsType.OTHER_EXPENSES),
        [SupportingReportsType.PARKING_DEPARTMENT_REPORT]: reportTypes.includes(SupportingReportsType.PARKING_DEPARTMENT_REPORT),
        [SupportingReportsType.VALIDATION_REPORT]: reportTypes.includes(SupportingReportsType.VALIDATION_REPORT),
    });

    const handleReportTypeChange = (reportType: SupportingReportsType) => {
        setReportsState((prevState) => {
            const updatedState = {
                ...prevState,
                [reportType]: !prevState[reportType],
            };

            const updatedReportTypes = Object.keys(updatedState)
                .filter((key) => updatedState[key as SupportingReportsType])
                .map(key => key as SupportingReportsType);

            setValue('supportingReports', updatedReportTypes);

            return updatedState;
        });
    };

    const form = useForm({
        resolver: zodResolver(ContractWithDataEntitySchema),
        defaultValues
    });

    const isBillingAdmin = userRoles.includes('billingAdmin');
    const isBillingManager = userRoles.includes('billingManager');

    const { fields: jobRatesFields, append: appendJobRate, remove: removeJobRate } = useFieldArray({
        control: form.control,
        name: "perLaborHour.jobRates"
    });

    const {
        fields: fixedFeeServicesFields,
        append: appendFixedFeeService,
        remove: removeFixedFeeService
    } = useFieldArray({
        control: form.control,
        name: "fixedFee.serviceRates"
    });

    const { control, handleSubmit, watch, setValue } = form;
    const watchFixedFeeEnabled = watch("fixedFee.enabled");
    const watchPerLaborHourEnabled = watch("perLaborHour.enabled");
    const watchInvoiceGroupingEnabled = watch("invoiceGrouping.enabled");
    const watchPaymentTerms = watch("paymentTerms");
    const watchManagementAgreementEnabled = watch("managementAgreement.enabled");
    const watchBillablePayrollEnabled = watch("billableAccounts.enabled");

    const [services, setServices] = useState<GLCode[]>([]);
    const [jobs, setJobs] = useState<GLCode[]>([]);
    const [defaultRate, setDefaultRate] = useState<number>();
    const [defaultOvertimeRate, setDefaultOvertimeRate] = useState<number>();
    const [defaultFee, setDefaultFee] = useState<number>();
    const [nonSalariedJobsList, setNonSalariedJobs] = useState<GLCode[]>([]);
    const [revenueCodes, setRevenueCodes] = useState<string[]>([]);
    const [payrollAccounts, setPayrollAccounts] = useState<PayrollAccount[]>([]);
    const [expenseAccounts, setExpenseAccounts] = useState<PayrollAccount[]>([]);

    useEffect(() => {
        const fetchGlCodes = async () => {
            const codeTypes = [
                "Service",
                "SalariedJob",
                "NonSalariedJob",
                "RateAndFeeData",
                "RevenueCode",
                "PayrollAccount",
                "ExpenseAccount"
            ];
            const params = new URLSearchParams();

            codeTypes.forEach((type) => params.append("codeTypes", type));

            try {
                const response = await fetch(`/api/gl-codes?${params.toString()}`);
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                const data: ContractConfig = await response.json();
                // Separate the data into services and jobs
                const servicesList = data.glCodes.filter(
                    (item: GLCode) => item.type === "Service"
                );
                const jobsList = data.glCodes.filter(
                    (item: GLCode) =>
                        item.type === "SalariedJob" || item.type === "NonSalariedJob"
                );
                const nonSalariedJobsList = data.glCodes.filter(
                    (item: GLCode) => item.type === "NonSalariedJob"
                );
                const revenueCodesList = data.glCodes
                    .filter((item: GLCode) => item.type === "RevenueCode")
                    .map((item) => item.code);
                const payrollAccountsList = data.glCodes
                    .filter((item: GLCode) => item.type === "PayrollAccount")
                    .map((item) => ({ id: item.code, name: item.name }));
                const expenseAccountsList = data.glCodes
                    .filter((item: GLCode) => item.type === "ExpenseAccount")
                    .map((item) => ({ id: item.code, name: item.name }));
                setDefaultRate(data.defaultRate);
                setDefaultOvertimeRate(data.defaultOvertimeRate);
                setDefaultFee(data.defaultFee);
                setServices(servicesList);
                setJobs(jobsList);
                setNonSalariedJobs(nonSalariedJobsList);
                setRevenueCodes(revenueCodesList);
                setPayrollAccounts(payrollAccountsList);
                setExpenseAccounts(expenseAccountsList);

            } catch (error) {
                console.error("Failed to fetch GL codes:", error);
            }
        };

        fetchGlCodes();
    }, []);

    useEffect(() => {
        if (!currentBillablePayroll.enabled) {
            setValue("managementAgreement.enabled", false);
        }
    }, [currentBillablePayroll.enabled, setValue]);


    useEffect(() => {
        if (!currentManagementAgreement.enabled) {
            setReportsState((prevState) => ({
                ...prevState,
                [SupportingReportsType.LABOR_DISTRIBUTION_REPORT]: false,
                [SupportingReportsType.OTHER_EXPENSES]: false,
            }));
        }
    }, [currentManagementAgreement.enabled || currentBillablePayroll.enabled]);

    useEffect(() => {
        if (!currentBillablePayroll.enabled) {
            setValue("managementAgreement.enabled", false);
            setReportsState((prevState) => ({
                ...prevState,
                [SupportingReportsType.LABOR_DISTRIBUTION_REPORT]: false,
                [SupportingReportsType.OTHER_EXPENSES]: false,
            }));
        }
    }, [currentBillablePayroll.enabled, setValue]);

    const { toast } = useToast();
    const [paymentTermsDetails, setPaymentTermsDetails] = useState("");
    const [jobCode, setJobCodes] = useState("");

    useEffect(() => {
        if (contractDetails && contractDetails.paymentTerms) {
            if (
                contractDetails.paymentTerms.startsWith("Due by ") &&
                contractDetails.paymentTerms !== PaymentTermsType.PAYMENT_TERM_FIRST_OF_MONTH
            ) {
                // Handling "Due by"
                setValue(
                    "paymentTerms",
                    PaymentTermsType.PAYMENT_TERM_CUSTOM_DUE_BY
                );
                setPaymentTermsDetails(
                    contractDetails.paymentTerms.replace("Due by ", "")
                );
            } else if (contractDetails.paymentTerms.startsWith("Due in ")) {
                // Handling "Due in X Days"
                setValue("paymentTerms", PaymentTermsType.PAYMENT_TERM_DUE_IN);
                const daysPart = contractDetails.paymentTerms.match(/(\d+) Days/);
                if (daysPart && daysPart[1]) {
                    setPaymentTermsDetails(daysPart[1]);
                }
            } else if (
                contractDetails.paymentTerms === PaymentTermsType.PAYMENT_TERM_FIRST_OF_MONTH ||
                contractDetails.paymentTerms === PaymentTermsType.PAYMENT_TERM_DUE_ON_RECEIPT
            ) {
                // Directly set the known payment terms without needing additional details
                setValue("paymentTerms", contractDetails.paymentTerms);
            }
        }
    }, [contractDetails, setValue]);

    const sortInvoiceGroups = (groups: any[]) => {
        return [...groups].sort((a, b) => a.groupNumber - b.groupNumber);
    };

    const updateInvoiceGroupSelections = (updatedGroups: any[]) => {
        const sortedGroups = sortInvoiceGroups(updatedGroups);
        setInvoiceGroups(sortedGroups);

        const validGroupNumbers = new Set(
            sortedGroups.map((g) => String(g.groupNumber))
        );

        let newFixedFeeServices = fixedFeeServicesFields.map((service) => {
            if (!validGroupNumbers.has(service.invoiceGroup)) {
                return { ...service, invoiceGroup: '' };
            }
            return service;
        });

        let newJobRates = jobRatesFields.map((job) => {
            if (!validGroupNumbers.has(job.invoiceGroup)) {
                return { ...job, invoiceGroup: '' };
            }
            return job;
        });

        newFixedFeeServices.forEach((service, index) => {
            setValue(
                `fixedFee.serviceRates.${index}.invoiceGroup`,
                service.invoiceGroup
            );
        });

        newJobRates.forEach((job, index) => {
            setValue(
                `perLaborHour.jobRates.${index}.invoiceGroup`,
                job.invoiceGroup
            );
        });

        // Update invoice group for perOccupiedRoom
        if (!validGroupNumbers.has(defaultValues.perOccupiedRoom.invoiceGroup)) {
            setValue("perOccupiedRoom.invoiceGroup", "1");
        }
    };

    const addInvoiceGroup = () => {
        const newGroup = {
            id: "",
            groupNumber: invoiceGroups.length + 1,
            title: "",
            description: "",
            vendorId: customerDetail.vendorId || "",
            siteNumber: customerDetail.siteNumber || "",
            customerName: customerDetail.siteName || "",
            billingContactEmails: customerDetail.billingContactEmail || "",
        };
        const updatedGroups = sortInvoiceGroups([...invoiceGroups, newGroup]);
        setInvoiceGroups(updatedGroups);
    };

    const removeLastInvoiceGroup = () => {
        if (invoiceGroups.length > 2) {
            const updatedGroups = invoiceGroups.slice(0, -1);
            updateInvoiceGroupSelections(updatedGroups);
        }
    };

    const onSubmit: SubmitHandler<FormValues> = async (data) => {
       
        if (revenueShareEnabled && !validateRevenueCodesAllocation()) {
            return;
        }

        setIsLoading(true);

        data.invoiceGrouping.invoiceGroups = invoiceGroups;

        data.revenueShare = {
            enabled: revenueShareEnabled,
            thresholdStructures: thresholdStructures.map(structure => ({
                ...structure,
                tiers: structure.tiers.map(tier => ({
                    ...tier,
                    amount: tier.amount ?? 0
                })),
                accumulationType: thresholdStructures[0].accumulationType || "Monthly",
                invoiceGroup: thresholdStructures[0].invoiceGroup,
                validationThresholdType: thresholdStructures[0].validationThresholdType,
                validationThresholdAmount: thresholdStructures[0].validationThresholdAmount,
            })),
        };

        const isValid = invoiceGroups.every(group => {
            return group.title !== "";
        });

        const isValidTitle = invoiceGroups.every(group => {
            return group.title.length > 0 && group.title.length <= 100;
        });

        if (
            data.paymentTerms ===
            PaymentTermsType.PAYMENT_TERM_DUE_IN && !paymentTermsDetails
        ) {
            setFormError(
                "Number of Days is required when 'Due in' is selected."
            );
            daysDueRef.current?.scrollIntoView({ behavior: "smooth" });
            setIsLoading(false);
            return;
        }

        if (
            data.paymentTerms ===
            PaymentTermsType.PAYMENT_TERM_DUE_IN
        ) {
            data.paymentTerms = `Due in ${paymentTermsDetails} Days`;
        } else if (
            data.paymentTerms ===
            PaymentTermsType.PAYMENT_TERM_CUSTOM_DUE_BY && paymentTermsDetails
        ) {
            data.paymentTerms = `Due by ${paymentTermsDetails}`;
        }

        if (!isValid) {
            toast({
                title: "Validation Error",
                description: "All invoice groups must have a title.",
            });
            setIsLoading(false);
            return;
        }

        if (!isValidTitle) {
            toast({
                title: "Form Error",
                description: "Please fix the validation errors before saving.",
            });
            setIsLoading(false);
            return;
        }

        if (
            data.fixedFee.enabled && data.fixedFee.serviceRates.length === 0
        ) {
            toast({
                title: "Validation Error",
                description: 'Please add at least one item when "Fixed Fee" is enabled.',
            });
            setIsLoading(false);
            return;
        }

        if (
            data.perLaborHour.enabled && data.perLaborHour.jobRates.length === 0
        ) {
            toast({
                title: "Validation Error",
                description: 'Please add at least one item when "Per Labor Hour" is enabled.',
            });
            setIsLoading(false);
            return;
        }

        const hasNonSalariedJob = data.perLaborHour.jobRates.some((job) =>
            nonSalariedJobsList.some(
                (nonSalariedJob) => nonSalariedJob.name === job.name
            )
        );

        if (
            data.perLaborHour.enabled && !hasNonSalariedJob
        ) {
            toast({
                title: "Validation Error",
                description: 'Please add at least one non-salaried job when "Per Labor Hour" is enabled.',
            });
            setIsLoading(false);
            return;
        }
        try {
            const response = await fetch(
                `/api/contracts/${contractDetails?.id}`,
                {
                    method: "PATCH",
                    headers: {
                        "Content-Type": "application/json",
                    },
                    body: JSON.stringify(data),
                }
            );

            if (!response.ok) {
                throw new Error("Network response was not ok");
            } else {
                toast({
                    title: "Success!",
                    description: "Contract updated successfully!",
                });
  
                onUpdateContractDetails(data as Contract);
                setIsEditable(false);
                            }
        } catch (error) {
            toast({
                title: "Unexpected Error",
                description: "An unexpected error occurred. Please try again later.",
            });
        } finally {
            setIsLoading(false);
        }
    };

    const renderPerLaborJob = (index: number) => {
        const jobName = watch(`perLaborHour.jobRates.${index}.name`);

        return (
            <div className="w-32 text-xs">
                <p className="font-semibold">{jobName}</p>
            </div>
        );
    };

    const renderFixedFeeService = (index: number) => {
        const name = watch(`fixedFee.serviceRates.${index}.name`);
        const code = watch(`fixedFee.serviceRates.${index}.code`);

        return (
            <div className="w-32 text-xs">
                <p className="font-semibold">{name}</p>
                <p className="text-muted-foreground">{code}</p>
            </div>
        );
    };

    const handleInvoiceGroupingChange = (enabled: boolean) => {
        if (!enabled) {
            setIsDialogOpen(true);
        } else {
            form.setValue("invoiceGrouping.enabled", true);
            if (invoiceGroups.length === 0) {
                const defaultGroups = [
                    {
                        id: "",
                        groupNumber: 1,
                        title: "",
                        description: "",
                        vendorId: customerDetail.vendorId || "",
                        siteNumber: customerDetail.siteNumber || "",
                        customerName: customerDetail.siteName || "",
                        billingContactEmails: customerDetail.billingContactEmail || "",
                    },
                    {
                        id: "",
                        groupNumber: 2,
                        title: "",
                        description: "",
                        vendorId: customerDetail.vendorId || "",
                        siteNumber: customerDetail.siteNumber || "",
                        customerName: customerDetail.siteName || "",
                        billingContactEmails: customerDetail.billingContactEmail || "",
                    },
                ];
                setInvoiceGroups(sortInvoiceGroups(defaultGroups));
            }
        }
    };

    const handleDialogConfirm = () => {
        setIsDialogOpen(false);

        setInvoiceGroups([]);

        fixedFeeServicesFields.forEach((_, index) => {
            form.setValue(`fixedFee.serviceRates.${index}.invoiceGroup`, "1");
        });
        jobRatesFields.forEach((_, index) => {
            form.setValue(`perLaborHour.jobRates.${index}.invoiceGroup`, "1");
        });
        form.setValue("perOccupiedRoom.invoiceGroup", "1");
        form.setValue("revenueShare.thresholdStructures", thresholdStructures.map(structure => ({
            ...structure,
            tiers: structure.tiers.map(tier => ({
                sharePercentage: tier.sharePercentage ?? 0,
                amount: tier.amount ?? 0
            })),
            invoiceGroup: 1
        })));
        form.setValue("bellServiceFee.bellServices", [{ invoiceGroup: 1 }]);
        form.setValue("midMonthAdvance.midMonthAdvances", currentMidMonthAdvanced.midMonthAdvances.map(advance => ({
            ...advance,
            lineTitle: advance.lineTitle || null,
            invoiceGroup: 1
        })));
        form.setValue("depositedRevenue.depositData", currentDepositedRevenue.depositData.map(revenue => ({
            ...revenue,
            invoiceGroup: 1
        })));
        form.setValue("billableAccounts.billableAccountsData", currentBillablePayroll.billableAccountsData.map(payroll => ({
            ...payroll,
            payrollAccountsInvoiceGroup: 1,
        })));
        form.setValue("managementAgreement.ManagementFees", currentManagementAgreement.ManagementFees.map(agreement => ({
            ...agreement,
            perLaborHourJobCodeData: agreement.perLaborHourJobCodeData.map(jobCode => ({
                code: jobCode.code ?? "",
                description: jobCode.description ?? "",
                standardRate: jobCode.standardRate ?? 0,
                overtimeRate: jobCode.overtimeRate ?? 0,
                standardRateEscalatorValue: jobCode.standardRateEscalatorValue ?? 0,
                overtimeRateEscalatorValue: jobCode.overtimeRateEscalatorValue ?? 0,
            })),
            profitShareTierData: agreement.profitShareTierData.map(tier => ({
                sharePercentage: tier.sharePercentage ?? 0,
                amount: tier.amount ?? 0,
                escalatorValue: tier.escalatorValue ?? 0
            })),
            invoiceGroup: 1,
            nonGlBillableExpenses: agreement.nonGlBillableExpenses
        ?.filter((expense): expense is NonGlBillableExpenseDto => !!expense) // Remove undefined values
        .map(expense => ({
            id: expense.id || '',
            nonglexpensetype: expense.nonglexpensetype || NonGlExpensetype.FIXEDAMOUNT,
            expensepayrolltype: expense.expensepayrolltype || ExpensetPayrollType.BILLABLE,
            expenseamount: expense.expenseamount ?? 0,
            expensetitle: expense.expensetitle || "Default Expense Title",
            finalperiodbilled: expense.finalperiodbilled || null,
            sequenceNumber:expense.sequenceNumber || 1,
        })),
        })));
        form.setValue("invoiceGrouping.enabled", false);
    };

    const handleDialogCancel = () => {
        setIsDialogOpen(false);
        form.setValue("invoiceGrouping.enabled", true);
    };

    const handleBellServiceFeeUpdate = (updatedFee: BellServiceFeeTerms) => {
        const updatedBellServices = updatedFee.bellServices.map(service => ({
            ...service,
            invoiceGroup: Number(service.invoiceGroup)
        }));

        form.setValue("bellServiceFee", {
            ...updatedFee,
            bellServices: updatedBellServices
        });
    };

    const handleMidMonthAdvanceUpdate = (updateMidMonthAdvanced: MidMonthAdvancedTerms) => {
        const updatedMidMonthAdvances = updateMidMonthAdvanced.midMonthAdvances.map(advance => ({
            ...advance,
            amount: Number(advance.amount),
            lineTitle: advance.lineTitle || null,
            invoiceGroup: Number(advance.invoiceGroup)
        }));

        form.setValue("midMonthAdvance", {
            ...updateMidMonthAdvanced,
            midMonthAdvances: updatedMidMonthAdvances
        });
    }

    const handleDepositedRevenueUpdate = (updatedDepositedRevenue: DepositedRevenueTerms) => {
        const updatedDepositedRevenues = updatedDepositedRevenue.depositData.map(revenue => ({
            ...revenue,
            towneParkResponsibleForParkingTax: revenue.towneParkResponsibleForParkingTax === true,
            invoiceGroup: Number(revenue.invoiceGroup)
        }));
        form.setValue("depositedRevenue", {
            ...updatedDepositedRevenue,
            depositData: updatedDepositedRevenues
        });
    }

    const handlePayrollAccountsUpdate = (updatedBillablePayroll: BillablePayrollTerms) => {
        const updatedBillablePayrolls = updatedBillablePayroll.billableAccountsData.map(payroll => ({
            ...payroll,
            payrollAccountsData: payroll.payrollAccountsData || "",
            payrollAccountsLineTitle: payroll.payrollAccountsLineTitle || "",
            payrollAccountsInvoiceGroup: Number(payroll.payrollAccountsInvoiceGroup),
            payrollTaxesEnabled: payroll.payrollTaxesEnabled === true,
            payrollTaxesBillingType: payroll.payrollTaxesBillingType || PTEBBillingType.ACTUAL,
            payrollTaxesPercentage: payroll.payrollTaxesPercentage || null,
            payrollTaxesLineTitle: payroll.payrollTaxesLineTitle || "",
            payrollSupportEnabled: payroll.payrollSupportEnabled || false,
            payrollSupportBillingType: payroll.payrollSupportBillingType || SupportServicesType.FIXED,
            payrollSupportPayrollType: payroll.payrollSupportPayrollType || SupportPayrollType.BILLABLE,
            payrollSupportAmount: payroll.payrollSupportAmount || null,
            payrollSupportLineTitle: payroll.payrollSupportLineTitle || '',
            expenseAccountsData: payroll.payrollExpenseAccountsData || "",
            expenseAccountsLineTitle: payroll.payrollExpenseAccountsLineTitle || "",
            additionalPayrollAmount: payroll.additionalPayrollAmount || 0,
            payrollTaxesEscalatorEnable:payroll.payrollTaxesEscalatorEnable || false,
            payrollTaxesEscalatorMonth:payroll.payrollTaxesEscalatorMonth || Month.JANUARY,
            payrollTaxesEscalatorvalue:payroll.payrollTaxesEscalatorvalue || 0,
            payrollTaxesEscalatorType:payroll.payrollTaxesEscalatorType || EscalatorFormatType.PERCENTAGE,
            expenseAccountsInvoiceGroup: Number(payroll.payrollExpenseAccountsInvoiceGroup)
        }));
        form.setValue("billableAccounts", {
            ...updatedBillablePayroll,
            billableAccountsData: updatedBillablePayrolls
        }, { shouldDirty: false, shouldTouch: false, shouldValidate: false });
    }

    const handleManagementAgreementUpdate = (updatedManagementAgreement: ManagementAgreementTerms) => {
        const updatedManagementAgreements = updatedManagementAgreement.ManagementFees.map(agreement => ({
            ...agreement,
            invoiceGroup: Number(agreement.invoiceGroup),
            managementAgreementType: agreement.managementAgreementType || ManagementAgreementType.FIXED_FEE,
            managementFeeEscalatorEnabled: agreement.managementFeeEscalatorEnabled || false,
            managementFeeEscalatorMonth: agreement.managementFeeEscalatorMonth || Month.JANUARY,
            managementFeeEscalatorType: agreement.managementFeeEscalatorType || EscalatorFormatType.PERCENTAGE,
            managementFeeEscalatorValue: agreement.managementFeeEscalatorValue || 0,
            fixedFeeAmount: agreement.fixedFeeAmount || 0,
            perLaborHourJobCodeData: agreement.perLaborHourJobCodeData.map(jobCode => ({
                code: jobCode.code ?? "",
                description: jobCode.description ?? "",
                standardRate: jobCode.standardRate ?? 0,
                overtimeRate: jobCode.overtimeRate ?? 0,
                standardRateEscalatorValue: jobCode.standardRateEscalatorValue ?? 0,
                overtimeRateEscalatorValue: jobCode.overtimeRateEscalatorValue ?? 0,
            })),
            laborHourRate: agreement.laborHourRate || 0,
            laborHourOvertimeRate: agreement.laborHourOvertimeRate || 0,
            revenuePercentageAmount: agreement.revenuePercentageAmount || 0,
            insuranceEnabled: agreement.insuranceEnabled || false,
            insuranceType: agreement.insuranceType || InsuranceType.BASED_ON_BILLABLE_ACCOUNTS,
            insuranceAdditionalPercentage: agreement.insuranceAdditionalPercentage || 0,
            insuranceFixedFeeAmount: agreement.insuranceFixedFeeAmount || 0,
            claimsEnabled: agreement.claimsEnabled || false,
            claimsType: agreement.claimsType || ClaimsType.ANNUALLY_ANIVERSARY,
            claimsCapAmount: agreement.claimsCapAmount || 0,
            claimsLineTitle: agreement.claimsLineTitle || null,
            profitShareEnabled: agreement.profitShareEnabled || false,
            profitShareAccumulationType: agreement.profitShareAccumulationType || ProfitShareAccumulationType.ANNUALLY_ANIVERSARY,
            profitShareTierData: agreement.profitShareTierData.map(tier => ({
                sharePercentage: tier.sharePercentage ?? 0,
                amount: tier.amount ?? 0,
                escalatorValue: tier.escalatorValue ?? 0,
            })),
            profitShareEscalatorEnabled: agreement.profitShareEscalatorEnabled || false,
            profitShareEscalatorMonth: agreement.profitShareEscalatorMonth || Month.JANUARY,
            profitShareEscalatorType: agreement.profitShareEscalatorType || EscalatorFormatType.PERCENTAGE,
            validationThresholdEnabled: agreement.validationThresholdEnabled || false,
            validationThresholdAmount: agreement.validationThresholdAmount || 0,
            validationThresholdType: agreement.validationThresholdType || ValidationThresholdType.VALIDATION_AMOUNT,
            nonGlBillableExpenses: agreement.nonGlBillableExpenses?.map((expense) => ({
                id:expense.id || '',
                nonglexpensetype: expense.nonglexpensetype || NonGlExpensetype.FIXEDAMOUNT,
                expensepayrolltype: expense.expensepayrolltype || ExpensetPayrollType.BILLABLE,
                expenseamount: expense.expenseamount ?? 0,
                expensetitle: expense.expensetitle || "Default Expense Title",
              finalperiodbilled: expense.finalperiodbilled ?? null,
              sequenceNumber:expense.sequenceNumber ?? 1,
              
            })) || [], 
        }));
        form.setValue("managementAgreement", {
            ...updatedManagementAgreement,
            ManagementFees: updatedManagementAgreements
        });
        form.trigger("managementAgreement");
    }

    const [isEditable, setIsEditable] = useState(false);

    const [originalRevenueShareData, setOriginalRevenueShareData] = useState({
        enabled: contractDetails?.revenueShare?.enabled || false,
        thresholdStructures: contractDetails?.revenueShare?.thresholdStructures || []
    });

    useEffect(() => {
        if (contractDetails) {
            setOriginalRevenueShareData({
                enabled: contractDetails.revenueShare?.enabled || false,
                thresholdStructures: contractDetails.revenueShare?.thresholdStructures || []
            });
            setRevenueShareEnabled(contractDetails.revenueShare?.enabled || false);
            setThresholdStructures(contractDetails.revenueShare?.thresholdStructures || []);
        }
    }, [contractDetails]);

    useEffect(() => {
        if (!isEditable) {
            setRevenueShareEnabled(originalRevenueShareData.enabled);
            setThresholdStructures([...originalRevenueShareData.thresholdStructures]);
        }
    }, [isEditable]);

    const handleEditClick = () => setIsEditable(true);

    const handleSaveClick = () => {
        form.trigger().then((isValid) => {
            if (!isValid) {
             
                
                toast({
                    title: "Validation Error",
                    description: "Please fix the validation errors before saving.",
        });
        return;
}

            if (revenueShareEnabled && !validateRevenueCodesAllocation()) {
                return;
            }

            if (watchInvoiceGroupingEnabled) {
                const isValid = invoiceGroups.every(group => group.title !== "");
                if (!isValid) {
                    toast({
                        title: "Validation Error",
                        description: "All invoice groups must have a title.",
                    });
                    return;
                }

                const isValidTitle = invoiceGroups.every(group => group.title.length > 0 && group.title.length <= 100)
                if (!isValidTitle) {
                    toast({
                        title: "Form Error",
                        description: "Please fix the validation errors before saving.",
                    });
                    return;
                }
            }

            if (watchFixedFeeEnabled && (!form.getValues("fixedFee.serviceRates") || form.getValues("fixedFee.serviceRates").length === 0)) {
                toast({
                    title: "Validation Error",
                    description: 'Please add at least one item when "Fixed Fee" is enabled.',
                });
                return;
            }

            if (watchPerLaborHourEnabled) {
                const jobRates = form.getValues("perLaborHour.jobRates");
                if (!jobRates || jobRates.length === 0) {
                    toast({
                        title: "Validation Error",
                        description: 'Please add at least one item when "Per Labor Hour" is enabled.',
                    });
                    return;
                }

                const hasNonSalariedJob = jobRates.some((job) =>
                    nonSalariedJobsList.some(
                        (nonSalariedJob) => nonSalariedJob.name === job.name
                    )
                );

                if (!hasNonSalariedJob) {
                    toast({
                        title: "Validation Error",
                        description: 'Please add at least one non-salaried job when "Per Labor Hour" is enabled.',
                    });
                    return;
                }
            }
        });
    };

    const [isCancelDialogOpen, setIsCancelDialogOpen] = useState(false);

    const handleCancelClick = () => {
        setIsCancelDialogOpen(true);
    };

    const handleConfirmCancel = () => {
        setRevenueShareEnabled(originalRevenueShareData.enabled);
        setThresholdStructures([...originalRevenueShareData.thresholdStructures]);

        // Reset reports state to original values
        setReportsState({
            [SupportingReportsType.MIX_OF_SALES]: reportTypes.includes(SupportingReportsType.MIX_OF_SALES),
            [SupportingReportsType.HOURS_BACKUP_REPORT]: reportTypes.includes(SupportingReportsType.HOURS_BACKUP_REPORT),
            [SupportingReportsType.TAX_REPORT]: reportTypes.includes(SupportingReportsType.TAX_REPORT),
            [SupportingReportsType.LABOR_DISTRIBUTION_REPORT]: reportTypes.includes(SupportingReportsType.LABOR_DISTRIBUTION_REPORT),
            [SupportingReportsType.OTHER_EXPENSES]: reportTypes.includes(SupportingReportsType.OTHER_EXPENSES),
            [SupportingReportsType.PARKING_DEPARTMENT_REPORT]: reportTypes.includes(SupportingReportsType.PARKING_DEPARTMENT_REPORT),
            [SupportingReportsType.VALIDATION_REPORT]: reportTypes.includes(SupportingReportsType.VALIDATION_REPORT),
        });

        form.reset(defaultValues);
        setIsEditable(false);
        setIsCancelDialogOpen(false);
    };

    return (
        <TooltipProvider>
            <div className="py-8">
                <Form {...form}>
                    <form
                        onSubmit={handleSubmit(onSubmit)}
                        className="grid grid-cols-1 md:grid-cols-1 gap-6"
                    >
                        <div className="flex items-center justify-between mb-6">
                            <h2 className="text-2xl font-semibold">Contract Details</h2>
                            {isBillingAdmin && (
                                <div className="flex items-center gap-2">
                                    {!isEditable ? (
                                        <Button
                                            data-qa-id="button-editContract-contractDetails"
                                            type="button"
                                            onClick={handleEditClick}
                                            data-testid="edit-button"
                                        >
                                            {isLoading ? (
                                                <PulseLoader size={8} />
                                            ) : (
                                                "Edit"
                                            )}
                                        </Button>
                                    ) : (
                                        <>
                                           {!isLoading && (
                                                <Button
                                                data-qa-id="button-cancelEdit-contractDetails"
                                                variant="outline"
                                                type="reset"
                                                onClick={handleCancelClick}
                                                disabled={!isEditable}
                                                data-testid="cancel-button"
                                                >
                                                Cancel
                                                </Button>
                                           )}
                                            <Button
                                                data-qa-id="button-saveContract-contractDetails"
                                            onClick={handleSaveClick}
                                            type="submit"
                                            disabled={!isEditable}
                                            data-testid="save-button"
                                            >
                                            {isLoading ? <PulseLoader size={8} /> : "Save"}
                                                </Button>
                                        </>
                                    )}
                                </div>
                            )}
                        </div>
                        <section>
                            <Accordion
                                type="multiple"
                                className="w-full"
                                defaultValue={[
                                    "general-setup",
                                ]}
                            >
                                <AccordionItem value="general-setup">
                                    <AccordionTrigger data-qa-id="accordion-generalSetup-contractDetails">General Setup</AccordionTrigger>
                                    <AccordionContent>
                                        <div className="grid gap-4 p-4 border rounded-lg">
                                            <FormLabel>Site ID</FormLabel>
                                            <Input
                                                data-qa-id="input-siteId-contractDetails"
                                                disabled
                                                placeholder={customerDetail.siteNumber}
                                                type="text"
                                            />
                                            <FormField
                                                control={form.control}
                                                name="contractType"
                                                render={({ field }) => (
                                                    <FormItem>
                                                        <FormLabel>Contract Type</FormLabel>
                                                        <Input
                                                            data-qa-id="input-contractType-contractDetails"
                                                            {...field}
                                                            placeholder={contractDetails?.contractType}
                                                            type="text"
                                                            disabled={!isEditable}
                                                        />
                                                    </FormItem>
                                                )}
                                            />
                                            <FormField
                                                control={form.control}
                                                name="deposits"
                                                render={({ field }) => (
                                                    <FormItem>
                                                        <FormLabel>Deposits</FormLabel>
                                                        <Select
                                                            data-qa-id="select-deposits-contractDetails"
                                                            onValueChange={(value) => {
                                                                field.onChange(value === "true");
                                                            }}
                                                            value={field.value ? "true" : "false"}
                                                            disabled={!isEditable}
                                                        >
                                                            <SelectTrigger>
                                                                <SelectValue placeholder="Select payment terms..." />
                                                            </SelectTrigger>
                                                            <SelectContent>
                                                                <SelectItem value="true">Yes</SelectItem>
                                                                <SelectItem value="false">No</SelectItem>
                                                            </SelectContent>
                                                        </Select>
                                                    </FormItem>
                                                )}
                                            />
                                            <div>
                                                <FormLabel className="font-extrabold">
                                                    Billing Setup
                                                </FormLabel>
                                                <Separator className="my-2" />
                                            </div>
                                            <div className="flex flex-row items-center justify-start gap-6">
                                                <FormField
                                                    control={form.control}
                                                    name="paymentTerms"
                                                    render={({ field }) => (
                                                        <FormItem>
                                                            <FormLabel>
                                                                Payment Terms
                                                                <span className="ml-2 cursor-pointer">
                                                                    <Tooltip>
                                                                        <TooltipTrigger asChild>
                                                                            <Info className="inline-block text-gray-400" size={17} />
                                                                        </TooltipTrigger>
                                                                        <TooltipContent>
                                                                            <p>Used on the Invoice to communicate when payment is due.</p>
                                                                        </TooltipContent>
                                                                    </Tooltip>
                                                                </span>
                                                            </FormLabel>
                                                            <FormControl>
                                                                <Select
                                                                    data-qa-id="select-paymentTerms-contractDetails"
                                                                    onValueChange={field.onChange}
                                                                    value={field.value as PaymentTermsType}
                                                                    disabled={!isEditable}
                                                                >
                                                                    <SelectTrigger>
                                                                        <SelectValue placeholder="Select payment terms..." />
                                                                    </SelectTrigger>
                                                                    <SelectContent>
                                                                        <SelectItem value={PaymentTermsType.PAYMENT_TERM_FIRST_OF_MONTH}>
                                                                            {PaymentTermsType.PAYMENT_TERM_FIRST_OF_MONTH}
                                                                        </SelectItem>
                                                                        <SelectItem value={PaymentTermsType.PAYMENT_TERM_DUE_ON_RECEIPT}>
                                                                            {PaymentTermsType.PAYMENT_TERM_DUE_ON_RECEIPT}
                                                                        </SelectItem>
                                                                        <SelectItem value={PaymentTermsType.PAYMENT_TERM_DUE_IN}>
                                                                            Due in
                                                                        </SelectItem>
                                                                        <SelectItem value={PaymentTermsType.PAYMENT_TERM_CUSTOM_DUE_BY}>
                                                                            {PaymentTermsType.PAYMENT_TERM_CUSTOM_DUE_BY}
                                                                        </SelectItem>
                                                                    </SelectContent>
                                                                </Select>
                                                            </FormControl>
                                                            <FormMessage />
                                                        </FormItem>
                                                    )}
                                                />
                                                {watchPaymentTerms === PaymentTermsType.PAYMENT_TERM_DUE_IN && (
                                                    <FormItem ref={daysDueRef}>
                                                        <FormLabel>Number of Days</FormLabel>
                                                        <FormControl>
                                                            <Select
                                                                data-qa-id="select-paymentDays-contractDetails"
                                                                onValueChange={(value) => {
                                                                    setPaymentTermsDetails(value);
                                                                    setFormError(null);
                                                                }}
                                                                value={paymentTermsDetails}
                                                                disabled={!isEditable}
                                                            >
                                                                <SelectTrigger>
                                                                    <SelectValue placeholder="Select number of days..." />
                                                                </SelectTrigger>
                                                                <SelectContent>
                                                                    {[...Array(90).keys()].map((i) => (
                                                                        <SelectItem key={i + 1} value={(i + 1).toString()}>
                                                                            {i + 1}
                                                                        </SelectItem>
                                                                    ))}
                                                                </SelectContent>
                                                            </Select>
                                                        </FormControl>
                                                        <FormMessage />
                                                    </FormItem>
                                                )}

                                                {formError && <div className="text-red-500">{formError}</div>}

                                                {watchPaymentTerms ===
                                                    PaymentTermsType.PAYMENT_TERM_CUSTOM_DUE_BY && (
                                                        <FormItem>
                                                            <FormLabel>Details</FormLabel>
                                                            <FormControl>
                                                                <Input
                                                                    data-qa-id="input-paymentTermsDetails-contractDetails"
                                                                    required
                                                                    value={paymentTermsDetails}
                                                                    onChange={(e) =>
                                                                        setPaymentTermsDetails(e.target.value)
                                                                    }
                                                                    placeholder="Enter specific payment term"
                                                                    disabled={!isEditable}
                                                                />
                                                            </FormControl>
                                                            <FormMessage />
                                                        </FormItem>
                                                    )}
                                            </div>
                                            <FormField
                                                control={form.control}
                                                name="billingType"
                                                render={({ field }) => (
                                                    <FormItem>
                                                        <FormLabel>
                                                            Billing Type
                                                            <span className="ml-2 cursor-pointer">
                                                                <Tooltip>
                                                                    <TooltipTrigger asChild>
                                                                        <Info className="inline-block text-gray-400" size={17} />
                                                                    </TooltipTrigger>
                                                                    <TooltipContent>
                                                                        <p>
                                                                            Used to determine if the service period to
                                                                            be billed is behind or ahead of the
                                                                            billing cycle.
                                                                        </p>
                                                                    </TooltipContent>
                                                                </Tooltip>
                                                            </span>
                                                        </FormLabel>
                                                        <FormControl>
                                                            <Select
                                                                data-qa-id="select-billingType-contractDetails"
                                                                onValueChange={field.onChange}
                                                                defaultValue={field.value}
                                                                disabled={!isEditable}
                                                            >
                                                                <SelectTrigger>
                                                                    <SelectValue placeholder="Select a billing type..." />
                                                                </SelectTrigger>
                                                                <SelectContent>
                                                                    <SelectItem value={AdvancedArrearsType.ADVANCED}>
                                                                        Advanced
                                                                    </SelectItem>
                                                                    <SelectItem value={AdvancedArrearsType.ARREARS}>
                                                                        Arrears
                                                                    </SelectItem>
                                                                </SelectContent>
                                                            </Select>
                                                        </FormControl>
                                                        <FormMessage />
                                                    </FormItem>
                                                )}
                                            />
                                            <FormField
                                                control={form.control}
                                                name="purchaseOrder"
                                                render={({ field }) => (
                                                    <FormItem>
                                                        <FormLabel>
                                                            PO Number
                                                            <span className="ml-2 cursor-pointer">
                                                                <Tooltip>
                                                                    <TooltipTrigger asChild>
                                                                        <Info className="inline-block text-gray-400" size={17} />
                                                                    </TooltipTrigger>
                                                                    <TooltipContent>
                                                                        <p>Used on the invoice to document the customers purchase order number.</p>
                                                                    </TooltipContent>
                                                                </Tooltip>
                                                            </span>
                                                        </FormLabel>
                                                        <Input
                                                            data-qa-id="input-purchaseOrder-contractDetails"
                                                            {...field}
                                                            placeholder="Enter PO number"
                                                            type="text"
                                                            disabled={!isEditable}
                                                        />
                                                        <FormMessage />
                                                    </FormItem>
                                                )}
                                            />
                                            {isBillingManager && (
                                                <>
                                                    <div>
                                                        <FormLabel className="font-extrabold">
                                                            Revenue Review Threshold
                                                        </FormLabel>
                                                        <Separator className="my-2" />
                                                    </div>
                                                    <FormField
                                                        control={form.control}
                                                        name="deviationPercentage"
                                                        render={({ field }) => (
                                                            <FormItem>
                                                                <FormLabel>
                                                                    Deviation Percentage
                                                                    <span className="ml-2 cursor-pointer">
                                                                        <Tooltip>
                                                                            <TooltipTrigger asChild>
                                                                                <Info className="inline-block text-gray-400" size={17} />
                                                                            </TooltipTrigger>
                                                                            <TooltipContent>
                                                                                <p>Used in combination with Deviation Amount to determine if the invoice requires review before it is able to be sent to a customer.</p>
                                                                            </TooltipContent>
                                                                        </Tooltip>
                                                                    </span>
                                                                </FormLabel>
                                                                <NumericFormat
                                                                    data-qa-id="input-deviationPercentage-contractDetails"
                                                                    displayType="input"
                                                                    decimalScale={2}
                                                                    suffix="%"
                                                                    inputMode="numeric"
                                                                    allowNegative={false}
                                                                    placeholder="Enter deviation percentage"
                                                                    value={String(field.value)}
                                                                    onValueChange={(values) => {
                                                                        field.onChange(values.floatValue || 0);
                                                                    }}
                                                                    customInput={Input}
                                                                    disabled={!isEditable}
                                                                />
                                                                <FormMessage />
                                                            </FormItem>
                                                        )}
                                                    />
                                                    <FormField
                                                        control={form.control}
                                                        name="deviationAmount"
                                                        render={({ field }) => (
                                                            <FormItem>
                                                                <FormLabel>
                                                                    Deviation Amount
                                                                    <span className="ml-2 cursor-pointer">
                                                                        <Tooltip>
                                                                            <TooltipTrigger asChild>
                                                                                <Info className="inline-block text-gray-400" size={17} />
                                                                            </TooltipTrigger>
                                                                            <TooltipContent>
                                                                                <p>
                                                                                    Used in combination with Deviation
                                                                                    Percentage to determine if the invoice
                                                                                    requires review before it is able to be
                                                                                    sent to a customer.
                                                                                </p>
                                                                            </TooltipContent>
                                                                        </Tooltip>
                                                                    </span>
                                                                </FormLabel>
                                                                <NumericFormat
                                                                    data-qa-id="input-deviationAmount-contractDetails"
                                                                    displayType="input"
                                                                    thousandSeparator={true}
                                                                    decimalScale={2}
                                                                    fixedDecimalScale={true}
                                                                    prefix="$"
                                                                    inputMode="numeric"
                                                                    allowNegative={false}
                                                                    placeholder="Enter deviation amount"
                                                                    value={String(field.value)}
                                                                    onValueChange={(values) => {
                                                                        field.onChange(values.floatValue || 0);
                                                                    }}
                                                                    customInput={Input}
                                                                    disabled={!isEditable}
                                                                />
                                                                <FormMessage />
                                                            </FormItem>
                                                        )}
                                                    />
                                                </>
                                            )}
                                            <div>
                                                <FormLabel className="font-extrabold">
                                                    Automatic Contract Escalator
                                                </FormLabel>
                                                <Separator className="my-2" />
                                            </div>
                                            <FormField
                                                control={form.control}
                                                name="incrementMonth"
                                                render={({ field }) => (
                                                    <FormItem>
                                                        <FormLabel>
                                                            Increment Month
                                                            <span className="ml-2 cursor-pointer">
                                                                <Tooltip>
                                                                    <TooltipTrigger asChild>
                                                                        <Info className="inline-block text-gray-400" size={17} />
                                                                    </TooltipTrigger>
                                                                    <TooltipContent>
                                                                        <p>
                                                                            The month when the Rates/Fees will be
                                                                            increased automatically (<b>NOTE</b>: You
                                                                            must enter an Increment Percentage before
                                                                            the billing cycle begins in order to
                                                                            trigger the rate increase.)
                                                                        </p>
                                                                    </TooltipContent>
                                                                </Tooltip>
                                                            </span>
                                                        </FormLabel>
                                                        <Select
                                                            data-qa-id="select-incrementMonth-contractDetails"
                                                            onValueChange={field.onChange}
                                                            defaultValue={field.value}
                                                            disabled={!isEditable}
                                                        >
                                                            <SelectTrigger className="w-full">
                                                                <SelectValue />
                                                            </SelectTrigger>
                                                            <SelectContent>
                                                                <SelectItem value="January">January</SelectItem>
                                                                <SelectItem value="February">
                                                                    February
                                                                </SelectItem>
                                                                <SelectItem value="March">March</SelectItem>
                                                                <SelectItem value="April">April</SelectItem>
                                                                <SelectItem value="May">May</SelectItem>
                                                                <SelectItem value="June">June</SelectItem>
                                                                <SelectItem value="July">July</SelectItem>
                                                                <SelectItem value="August">August</SelectItem>
                                                                <SelectItem value="September">
                                                                    September
                                                                </SelectItem>
                                                                <SelectItem value="October">October</SelectItem>
                                                                <SelectItem value="November">
                                                                    November
                                                                </SelectItem>
                                                                <SelectItem value="December">
                                                                    December
                                                                </SelectItem>
                                                            </SelectContent>
                                                        </Select>
                                                    </FormItem>
                                                )}
                                            />
                                            <FormField
                                                control={form.control}
                                                name="incrementAmount"
                                                render={({ field }) => (
                                                    <FormItem>
                                                        <FormLabel className="text-inherit">
                                                            Increment Percentage
                                                            <span className="ml-2 cursor-pointer">
                                                                <Tooltip>
                                                                    <TooltipTrigger asChild>
                                                                        <Info className="inline-block text-gray-400" size={17} />
                                                                    </TooltipTrigger>
                                                                    <TooltipContent>
                                                                        <p>
                                                                            The amount to permanently increase all amounts upon
                                                                            anniversary of the Increment Month selected above.
                                                                        </p>
                                                                    </TooltipContent>
                                                                </Tooltip>
                                                            </span>
                                                        </FormLabel>
                                                        <NumericFormat
                                                            displayType="input"
                                                            decimalScale={2}
                                                            suffix="%"
                                                            inputMode="numeric"
                                                            allowNegative={false}
                                                            placeholder="Enter increment percentage"
                                                            value={String(field.value)}
                                                            onValueChange={(values) => {
                                                                field.onChange(Number(values.floatValue || 0));
                                                            }}
                                                            customInput={Input}
                                                            disabled={!isEditable}
                                                            data-qa-id="input-field-incrementAmount"
                                                        />
                                                        {form.formState.errors.incrementAmount && (
                                                            <FormMessage className="text-red-500">
                                                                {form.formState.errors.incrementAmount?.message}
                                                            </FormMessage>
                                                        )}
                                                    </FormItem>
                                                )}
                                            />
                                            <FormField
                                                control={form.control}
                                                name="consumerPriceIndex"
                                                render={({ field }) => (
                                                    <FormItem className="flex flex-row items-center justify-between">
                                                        <div className="space-y-0.5">
                                                            <FormLabel>
                                                                Consumer Price Index
                                                            </FormLabel>
                                                            <FormDescription>
                                                                Enable Consumer Price Index
                                                            </FormDescription>
                                                        </div>
                                                        <FormControl>
                                                            <Switch
                                                                checked={field.value}
                                                                onCheckedChange={field.onChange}
                                                                disabled={!isEditable}
                                                                data-qa-id="switch-component-consumerPriceIndex"
                                                            />
                                                        </FormControl>
                                                    </FormItem>
                                                )}
                                            />
                                            <FormField
                                                control={form.control}
                                                name="notes"
                                                render={({ field }) => (
                                                    <FormItem>
                                                        <FormLabel>Notes</FormLabel>
                                                        <Textarea
                                                            {...field}
                                                            placeholder="Enter notes"
                                                            disabled={!isEditable}
                                                            data-qa-id="textarea-field-notes"
                                                        />
                                                        <FormMessage />
                                                    </FormItem>
                                                )}
                                            />
                                        </div>
                                    </AccordionContent>
                                </AccordionItem>

                                <AccordionItem value="invoice-grouping">
                                    <AccordionTrigger data-qa-id="accordion-trigger-invoiceGrouping">Multiple Invoices</AccordionTrigger>
                                    <AccordionContent>
                                        <div className="grid gap-3 p-4 border rounded-lg">
                                            <FormField
                                                control={form.control}
                                                name="invoiceGrouping.enabled"
                                                render={({ field }) => (
                                                    <FormItem className="flex flex-row items-center justify-between">
                                                        <div className="space-y-0.5">
                                                            <FormLabel>Generate Multiple Invoices</FormLabel>
                                                            <FormDescription>
                                                                Allow multiple invoices to be generated, with
                                                                line-item grouping by invoice.
                                                            </FormDescription>
                                                        </div>
                                                        <FormControl>
                                                            <Switch
                                                                checked={field.value}
                                                                onCheckedChange={handleInvoiceGroupingChange}
                                                                disabled={!isEditable}
                                                                data-qa-id="switch-component-invoiceGroupingEnabled"
                                                            />
                                                        </FormControl>
                                                    </FormItem>
                                                )}
                                            />
                                            {watchInvoiceGroupingEnabled && (
                                                <div className="space-y-4">
                                                    {sortInvoiceGroups(invoiceGroups).map((group, index) => (
                                                        <div key={index} className="border rounded-lg overflow-hidden" data-qa-id={`invoice-group-${index}`}>
                                                            <div className="px-4 py-2 flex items-center">
                                                                <h3 className="text-sm font-medium">Invoice {group.groupNumber}</h3>
                                                            </div>
                                                            <div className="p-4 space-y-4">
                                                                <div className="grid grid-cols-2 gap-4">
                                                                    <FormItem>
                                                                        <FormLabel>Title</FormLabel>
                                                                        <FormControl>
                                                                            <Input
                                                                                value={group.title}
                                                                                onChange={(e) => {
                                                                                    const updatedGroups = [...invoiceGroups];
                                                                                    updatedGroups[index].title = e.target.value;
                                                                                    setInvoiceGroups(sortInvoiceGroups(updatedGroups));
                                                                                }}
                                                                                placeholder={`Title for Invoice ${group.groupNumber}`}
                                                                                disabled={!isEditable}
                                                                                data-testid={`invoice-group-title-${index}`}
                                                                                data-qa-id={`input-field-invoiceTitle-${index}`}
                                                                            />
                                                                        </FormControl>
                                                                        {group.title.length > 100 && (
                                                                            <p className="text-sm text-red-500">Title must be less than 100 characters.</p>
                                                                        )}
                                                                        <FormMessage />
                                                                    </FormItem>
                                                                    <FormItem>
                                                                        <FormLabel>Description</FormLabel>
                                                                        <FormControl>
                                                                            <Input
                                                                                value={group.description ?? ""}
                                                                                onChange={(e) => {
                                                                                    const updatedGroups = [...invoiceGroups];
                                                                                    updatedGroups[index].description = e.target.value;
                                                                                    setInvoiceGroups(sortInvoiceGroups(updatedGroups));
                                                                                }}
                                                                                placeholder={`Description for Invoice ${group.groupNumber}`}
                                                                                disabled={!isEditable}
                                                                                data-qa-id={`input-field-invoiceDescription-${index}`}
                                                                            />
                                                                        </FormControl>
                                                                        <FormMessage />
                                                                    </FormItem>
                                                                </div>
                                                                
                                                                <div>
                                                                    <div className="grid grid-cols-2 gap-4">
                                                                        <FormItem>
                                                                            <FormLabel>Vendor ID</FormLabel>
                                                                            <FormControl>
                                                                                <Input
                                                                                    value={group.vendorId || ""}
                                                                                    onChange={(e) => {
                                                                                        const updatedGroups = [...invoiceGroups];
                                                                                        updatedGroups[index].vendorId = e.target.value;
                                                                                        setInvoiceGroups(sortInvoiceGroups(updatedGroups));
                                                                                    }}
                                                                                    placeholder="Vendor ID"
                                                                                    disabled={!isEditable}
                                                                                    data-qa-id={`input-field-invoiceVendorId-${index}`}
                                                                                />
                                                                            </FormControl>
                                                                        </FormItem>
                                                                        <FormItem>
                                                                            <FormLabel>Site Number</FormLabel>
                                                                            <FormControl>
                                                                                <Input
                                                                                    value={group.siteNumber || ""}
                                                                                    onChange={(e) => {
                                                                                        const updatedGroups = [...invoiceGroups];
                                                                                        updatedGroups[index].siteNumber = validateAlphanumeric(e.target.value, 5);
                                                                                        setInvoiceGroups(sortInvoiceGroups(updatedGroups));
                                                                                    }}
                                                                                    placeholder="Site Number"
                                                                                    maxLength={5}
                                                                                    disabled={!isEditable}
                                                                                    data-qa-id={`input-field-invoiceSiteNumber-${index}`}
                                                                                />
                                                                            </FormControl>
                                                                        </FormItem>
                                                                        <FormItem>
                                                                            <FormLabel>Customer Name</FormLabel>
                                                                            <FormControl>
                                                                                <Input
                                                                                    value={group.customerName || ""}
                                                                                    onChange={(e) => {
                                                                                        const updatedGroups = [...invoiceGroups];
                                                                                        updatedGroups[index].customerName = e.target.value;
                                                                                        setInvoiceGroups(sortInvoiceGroups(updatedGroups));
                                                                                    }}
                                                                                    placeholder="Customer Name"
                                                                                    disabled={!isEditable}
                                                                                    data-qa-id={`input-field-invoiceCustomerName-${index}`}
                                                                                />
                                                                            </FormControl>
                                                                        </FormItem>
                                                                        <FormItem>
                                                                            <FormLabel>Billing Contact Emails</FormLabel>
                                                                            <FormControl>
                                                                                <Textarea
                                                                                    value={group.billingContactEmails || ""}
                                                                                    onChange={(e) => {
                                                                                        const updatedGroups = [...invoiceGroups];
                                                                                        updatedGroups[index].billingContactEmails = e.target.value;
                                                                                        setInvoiceGroups(sortInvoiceGroups(updatedGroups));
                                                                                    }}
                                                                                    placeholder="Billing Contact Emails"
                                                                                    disabled={!isEditable}
                                                                                    data-qa-id={`textarea-field-invoiceBillingEmail-${index}`}
                                                                                />
                                                                            </FormControl>
                                                                        </FormItem>
                                                                    </div>
                                                                </div>
                                                            </div>
                                                        </div>
                                                    ))}
                                                    <div className="flex items-center justify-end">
                                                        <div className="flex space-x-2">
                                                            <Button
                                                                type="button"
                                                                variant="outline"
                                                                size="sm"
                                                                onClick={addInvoiceGroup}
                                                                disabled={!isEditable}
                                                                data-qa-id="button-addInvoice"
                                                            >
                                                                Add Invoice
                                                            </Button>
                                                            {invoiceGroups.length > 100 && (
                                                                <Button
                                                                    type="button"
                                                                    variant="destructive"
                                                                    size="sm"
                                                                    onClick={removeLastInvoiceGroup}
                                                                    disabled={!isEditable}
                                                                    data-qa-id="button-removeInvoice"
                                                                >
                                                                    Remove Invoice
                                                                </Button>
                                                            )}
                                                        </div>
                                                    </div>
                                                </div>
                                            )}
                                        </div>
                                    </AccordionContent>
                                </AccordionItem>

                                <AccordionItem value="fixed-fee">
                                    <AccordionTrigger data-qa-id="accordion-trigger-fixedFee">Fixed Fee</AccordionTrigger>
                                    <AccordionContent>
                                        <div className="grid gap-4 p-4 border rounded-lg">
                                            <FormField
                                                control={form.control}
                                                name="fixedFee.enabled"
                                                render={({ field }) => (
                                                    <FormItem className="flex flex-row items-center justify-between">
                                                        <div className="space-y-0.5">
                                                            <FormLabel>Enable Fixed Fee</FormLabel>
                                                            <FormDescription>
                                                                Bill customer based on fixed monthly fee per rendered service.
                                                            </FormDescription>
                                                        </div>
                                                        <FormControl>
                                                            <Switch checked={field.value} onCheckedChange={field.onChange} disabled={!isEditable} data-qa-id="switch-component-fixedFeeEnabled" />
                                                        </FormControl>
                                                    </FormItem>
                                                )}
                                            />
                                            <Separator className="my-2" />
                                            <div>
                                                <div className="flex items-center justify-between">
                                                    <FormLabel>Monthly Fees</FormLabel>
                                                </div>
                                                {fixedFeeServicesFields.map((field, index) => (
                                                    <div key={field.id} className="space-y-4 mt-2" data-qa-id={`row-fixedFeeService-${index}`}>
                                                        <div className="flex items-start justify-between space-x-2">
                                                            <FormItem className="w-auto">
                                                                {index === 0 && <FormLabel>Services</FormLabel>}
                                                                {renderFixedFeeService(index)}
                                                            </FormItem>
                                                            <FormField
                                                                control={form.control}
                                                                name={`fixedFee.serviceRates.${index}.id`}
                                                                render={({ field }) => (
                                                                    <Input {...field} type="hidden" />
                                                                )}
                                                            />
                                                            <FormField
                                                                control={form.control}
                                                                name={`fixedFee.serviceRates.${index}.displayName`}
                                                                render={({ field }) => (
                                                                    <FormItem className="w-1/4">
                                                                        {index === 0 && <FormLabel className="text-black">Display Name</FormLabel>}
                                                                        <FormControl>
                                                                            <Input  disabled={!watchFixedFeeEnabled || !isEditable} placeholder="Display Name" {...field} data-qa-id={`input-field-fixedFeeDisplayName-${index}`} />
                                                                        </FormControl>
                                                                {watchFixedFeeEnabled && <FormMessage />}
                                                                    </FormItem>
                                                                )}
                                                            />
                                                            <FormField
                                                                control={form.control}
                                                                name={`fixedFee.serviceRates.${index}.fee`}
                                                                render={({ field }) => (
                                                                    <FormItem className="w-1/4">
                                                                        {index === 0 && <FormLabel className="text-black" >Fee</FormLabel>}
                                                                        <FormControl>
                                                                            <NumericFormat
                                                                                displayType="input"
                                                                                thousandSeparator={true}
                                                                                decimalScale={2}
                                                                                fixedDecimalScale={true}
                                                                                prefix="$"
                                                                                inputMode="numeric"
                                                                                allowNegative={false}
                                                                                placeholder="Fee"
                                                                                value={String(field.value)}
                                                                                onValueChange={(values) => {
                                                                                    field.onChange(values.floatValue || 0);
                                                                                }}
                                                                                customInput={Input}
                                                                                disabled={!watchFixedFeeEnabled || !isEditable}
                                                                                data-qa-id={`input-field-fixedFeeFee-${index}`}
                                                                            />
                                                                        </FormControl>
                                                                    {watchFixedFeeEnabled && <FormMessage />}
                                                                    </FormItem>
                                                                )}
                                                            />
                                                            {watchInvoiceGroupingEnabled && (
                                                                <FormField
                                                                    control={form.control}
                                                                    name={`fixedFee.serviceRates.${index}.invoiceGroup`}
                                                                    render={({ field }) => (
                                                                        <FormItem className="w-1/6">
                                                                            {index === 0 && (
                                                                                <FormLabel>Invoice</FormLabel>
                                                                            )}
                                                                            <FormControl>
                                                                                <Select
                                                                                    disabled={
                                                                                        !watchFixedFeeEnabled || !isEditable
                                                                                    }
                                                                                    onValueChange={
                                                                                        field.onChange
                                                                                    }
                                                                                    defaultValue={
                                                                                        field.value?.toString() || ""
                                                                                    }
                                                                                    data-qa-id={`select-fixedFeeInvoiceGroup-${index}`}
                                                                                >
                                                                                    <SelectTrigger className="w-full" data-qa-id={`select-trigger-fixedFeeInvoiceGroup-${index}`}>
                                                                                        <SelectValue />
                                                                                    </SelectTrigger>
                                                                                    <SelectContent>
                                                                                        {invoiceGroups.map(
                                                                                            (group) => (
                                                                                                <SelectItem
                                                                                                    key={
                                                                                                        group.groupNumber
                                                                                                    }
                                                                                                    value={String(
                                                                                                        group.groupNumber
                                                                                                    )}
                                                                                                    data-qa-id={`select-item-fixedFeeInvoiceGroup-${index}-${group.groupNumber}`}
                                                                                                >{`${group.groupNumber}`}</SelectItem>
                                                                                            )
                                                                                        )}
                                                                                    </SelectContent>
                                                                                </Select>
                                                                            </FormControl>
                                                          
                                                                        </FormItem>
                                                                    )}
                                                                />
                                                            )}
                                                            <FormItem className="flex flex-col items-center w-auto flex-shrink-0">
                                                                {index === 0 && <FormLabel>Action</FormLabel>}
                                                                <Button
                                                                    type="button"
                                                                    variant="ghost"
                                                                    onClick={() => removeFixedFeeService(index)}
                                                                    disabled={!isEditable}
                                                                    data-qa-id={`button-removeFixedFeeService-${index}`}
                                                                >
                                                                    <Trash className="h-4 w-4" />
                                                                </Button>
                                                            </FormItem>
                                                        </div>
                                                    </div>
                                                ))}
                                            </div>
                                            <div className="flex items-center justify-end">
                                                <DropdownMenu>
                                                    <DropdownMenuTrigger asChild>
                                                        <Button
                                                            variant="outline"
                                                            size="lg"
                                                            disabled={!watchFixedFeeEnabled || !isEditable}
                                                            data-qa-id="button-addFixedFeeServiceDropdown"
                                                        >
                                                            Add
                                                        </Button>
                                                    </DropdownMenuTrigger>
                                                    <DropdownMenuContent>
                                                        <ScrollArea className="h-72 w-48 rounded-md">
                                                            {services.map((service) => (
                                                                <DropdownMenuItem
                                                                    key={`${service.code}-${service.name}`}
                                                                    onClick={() =>
                                                                        appendFixedFeeService({
                                                                            id: "",
                                                                            name: service.name,
                                                                            displayName: service.name,
                                                                            code: service.code,
                                                                            fee: defaultFee ? defaultFee : 0,
                                                                            invoiceGroup: "1",
                                                                        })
                                                                    }
                                                                    data-qa-id={`dropdown-item-service-${service.code}`}
                                                                >
                                                                    {service.name}
                                                                </DropdownMenuItem>
                                                            ))}
                                                        </ScrollArea>
                                                    </DropdownMenuContent>
                                                </DropdownMenu>
                                            </div>
                                        </div>
                                    </AccordionContent>
                                </AccordionItem>

                                <AccordionItem value="per-labor-hour">
                                    <AccordionTrigger data-qa-id="accordion-trigger-perLaborHour">Per Labor Hour</AccordionTrigger>
                                    <AccordionContent>
                                        <div className="grid gap-4 p-4 border rounded-lg">
                                            <FormField
                                                control={form.control}
                                                name="perLaborHour.enabled"
                                                render={({ field }) => (
                                                    <FormItem className="flex flex-row items-center justify-between">
                                                        <div className="space-y-0.5">
                                                            <FormLabel>Enable Per Labor Hour</FormLabel>
                                                            <FormDescription>
                                                                Bill customer based on hourly rates, using data
                                                                from time entry and revenue spreadsheets.
                                                            </FormDescription>
                                                        </div>
                                                        <FormControl>
                                                            <Switch
                                                                checked={field.value}
                                                                onCheckedChange={field.onChange}
                                                                disabled={!isEditable}
                                                                data-qa-id="switch-component-perLaborHourEnabled"
                                                            />
                                                        </FormControl>
                                                    </FormItem>
                                                )}
                                            />
                                            <Separator className="my-2" />
                                            <div>
                                                <div className="flex items-center justify-between">
                                                    <FormLabel>Hourly Rates</FormLabel>
                                                </div>
                                                {jobRatesFields.map((field, index) => (
                                                    <div
                                                        key={field.id}
                                                        className="space-y-4 mt-2"
                                                        data-qa-id={`row-perLaborHourJob-${index}`}
                                                    >
                                                        <div className="flex items-start justify-between space-x-2">
                                                            <FormItem className="w-auto">
                                                                {index === 0 && (
                                                                    <FormLabel>Jobs / Services</FormLabel>
                                                                )}
                                                                {renderPerLaborJob(index)}
                                                            </FormItem>
                                                            <FormField
                                                                control={form.control}
                                                                name={`perLaborHour.jobRates.${index}.id`}
                                                                render={({ field }) => (
                                                                    <Input {...field} type="hidden" />
                                                                )}
                                                            />
                                                            <FormField
                                                                control={form.control}
                                                                name={`perLaborHour.jobRates.${index}.displayName`}
                                                                render={({ field }) => (
                                                                    <FormItem className="w-1/4">
                                                                        {index === 0 && (
                                                                            <FormLabel className="text-black">Display Name</FormLabel>
                                                                        )}
                                                                        <FormControl>
                                                                            <Input
                                                                                disabled={!watchPerLaborHourEnabled || !isEditable}
                                                                                placeholder="Display Name"
                                                                                {...field}
                                                                                data-qa-id={`input-field-perLaborHourDisplayName-${index}`}
                                                                            />
                                                                        </FormControl>
                                                     {watchPerLaborHourEnabled && <FormMessage />}
                                                                    </FormItem>
                                                                )}
                                                            />
                                                            <FormField
                                                                control={form.control}
                                                                name={`perLaborHour.jobRates.${index}.rate`}
                                                                render={({ field }) => (
                                                                    <FormItem className="w-1/4">
                                                                       {index === 0 && (
                                                <FormLabel className="text-black">Standard Rate</FormLabel>

)}

                                                                        <FormControl>
                                                                            <NumericFormat
                                                                                displayType="input"
                                                                                thousandSeparator={true}
                                                                                decimalScale={2}
                                                                                fixedDecimalScale={true}
                                                                                prefix="$"
                                                                                inputMode="numeric"
                                                                                allowNegative={false}
                                                                                placeholder="Standard Rate"
                                                                                value={String(field.value)}
                                                                                onValueChange={(values) => {
                                                                                    field.onChange(
                                                                                        values.floatValue || 0
                                                                                    );
                                                                                }}
                                                                                customInput={Input}
                                                                                disabled={!watchPerLaborHourEnabled || !isEditable}
                                                                                data-qa-id={`input-field-perLaborHourRate-${index}`}
                                                                            />
                                                                        </FormControl>
                                                                     {watchPerLaborHourEnabled && <FormMessage />}
                                                                    </FormItem>
                                                                )}
                                                            />
                                                            <FormField
                                                                control={form.control}
                                                                name={`perLaborHour.jobRates.${index}.overtimeRate`}
                                                                render={({ field }) => (
                                                                    <FormItem className="w-1/4">
                                                                        {index === 0 && (
                                                                            <FormLabel  className="text-black">Overtime Rate</FormLabel>
                                                                        )}
                                                                        <FormControl>
                                                                            <NumericFormat
                                                                                displayType="input"
                                                                                thousandSeparator={true}
                                                                                decimalScale={2}
                                                                                fixedDecimalScale={true}
                                                                                prefix="$"
                                                                                inputMode="numeric"
                                                                                allowNegative={false}
                                                                                placeholder="Overtime Rate"
                                                                                value={String(field.value)}
                                                                                onValueChange={(values) => {
                                                                                    field.onChange(
                                                                                        values.floatValue || 0
                                                                                    );
                                                                                }}
                                                                                customInput={Input}
                                                                                disabled={!watchPerLaborHourEnabled || !isEditable}
                                                                                data-qa-id={`input-field-perLaborHourOvertimeRate-${index}`}
                                                                            />
                                                                        </FormControl>
                                                            {watchPerLaborHourEnabled && <FormMessage />}
                                                                    </FormItem>
                                                                )}
                                                            />
                                                            <FormField
                                                                control={form.control}
                                                                name={`perLaborHour.jobRates.${index}.jobCode`}
                                                                render={({ field, fieldState }) => (
                                                                    <FormItem className="w-1/4">
                                                                        {index === 0 && (
                                                                            <FormLabel  className="text-black" >Job Code</FormLabel>
                                                                        )}
                                                                        <FormControl>
                                                                            <Input
                                                                                disabled={!watchPerLaborHourEnabled || !isEditable}
                                                                                {...field}
                                                                                placeholder="Enter Job Code"
                                                                                data-qa-id={`input-field-perLaborHourJobCode-${index}`}
                                                                            />
                                                                        </FormControl>
                                                                      {watchPerLaborHourEnabled && <FormMessage />}
                                                                    </FormItem>
                                                                )}
                                                            />
                                                            {watchInvoiceGroupingEnabled && (
                                                                <FormField
                                                                    control={form.control}
                                                                    name={`perLaborHour.jobRates.${index}.invoiceGroup`}
                                                                    render={({ field }) => (
                                                                        <FormItem className="w-1/6">
                                                                            {index === 0 && (
                                                                                <FormLabel>Invoice</FormLabel>
                                                                            )}
                                                                            <FormControl>
                                                                                <Select
                                                                                    required
                                                                                    disabled={
                                                                                        !watchPerLaborHourEnabled || !isEditable
                                                                                    }
                                                                                    onValueChange={
                                                                                        field.onChange
                                                                                    }
                                                                                    defaultValue={
                                                                                        field.value?.toString() || ""
                                                                                    }
                                                                                    data-qa-id={`select-perLaborHourInvoiceGroup-${index}`}
                                                                                >
                                                                                    <SelectTrigger className="w-full" data-qa-id={`select-trigger-perLaborHourInvoiceGroup-${index}`}>
                                                                                        <SelectValue />
                                                                                    </SelectTrigger>
                                                                                    <SelectContent>
                                                                                        {invoiceGroups.map((group) => (
                                                                                            <SelectItem
                                                                                                key={group.groupNumber}
                                                                                                value={group.groupNumber.toString()}
                                                                                                data-qa-id={`select-item-perLaborHourInvoiceGroup-${index}-${group.groupNumber}`}
                                                                                            >{`${group.groupNumber}`}</SelectItem>
                                                                                        ))}
                                                                                    </SelectContent>
                                                                                </Select>
                                                                            </FormControl>
                                                                       {watchPerLaborHourEnabled && <FormMessage />}
                                                                        </FormItem>
                                                                    )}
                                                                />
                                                            )}
                                                            <FormItem className="w-auto flex flex-col items-center flex-shrink-0">
                                                                {index === 0 && (
                                                                    <FormLabel>Action</FormLabel>
                                                                )}
                                                                <Button
                                                                    type="button"
                                                                    variant="ghost"
                                                                    onClick={() => removeJobRate(index)}
                                                                    disabled={!isEditable}
                                                                    data-qa-id={`button-removePerLaborHourJob-${index}`}
                                                                >
                                                                    <Trash className="h-4 w-4" />
                                                                </Button>
                                                            </FormItem>
                                                        </div>
                                                    </div>
                                                ))}
                                            </div>
                                            <div className="flex items-center justify-end">
                                                <DropdownMenu>
                                                    <DropdownMenuTrigger asChild>
                                                        <Button
                                                            variant="outline"
                                                            size="lg"
                                                            disabled={!watchPerLaborHourEnabled || !isEditable}
                                                            data-qa-id="button-addPerLaborHourJobDropdown"
                                                        >
                                                            Add
                                                        </Button>
                                                    </DropdownMenuTrigger>
                                                    <DropdownMenuContent>
                                                        <ScrollArea className="h-72 w-48 rounded-md">
                                                            <DropdownMenuLabel>
                                                                Salaried Jobs
                                                            </DropdownMenuLabel>
                                                            {jobs
                                                                .filter((j) => j.type == 'SalariedJob')
                                                                .map((job) => (
                                                                    <DropdownMenuItem
                                                                        key={`${job.code}-${job.name}`}
                                                                        onClick={() =>
                                                                            appendJobRate({
                                                                                name: job.name,
                                                                                displayName: job.name,
                                                                                jobCode: "",
                                                                                rate: defaultRate ? defaultRate : 20,
                                                                                overtimeRate: defaultOvertimeRate ? defaultOvertimeRate : 40,
                                                                                invoiceGroup: "1"
                                                                            })
                                                                        }
                                                                        data-qa-id={`dropdown-item-salariedJob-${job.code}`}
                                                                    >
                                                                        {job.name}
                                                                    </DropdownMenuItem>
                                                                ))}
                                                            <DropdownMenuSeparator />
                                                            <DropdownMenuLabel>
                                                                Non-Salaried Jobs
                                                            </DropdownMenuLabel>
                                                            {jobs
                                                                .filter((j) => j.type == 'NonSalariedJob')
                                                                .map((job) => (
                                                                    <DropdownMenuItem
                                                                        key={`${job.code}-${job.name}`}
                                                                        onClick={() =>
                                                                            appendJobRate({
                                                                                id: "",
                                                                                name: job.name,
                                                                                displayName: job.name,
                                                                                jobCode: "",
                                                                                rate: defaultRate ? defaultRate : 20,
                                                                                overtimeRate: defaultOvertimeRate ? defaultOvertimeRate : 40,
                                                                                invoiceGroup: "1"
                                                                            })
                                                                        }
                                                                        data-qa-id={`dropdown-item-nonSalariedJob-${job.code}`}
                                                                    >
                                                                        {job.name}
                                                                    </DropdownMenuItem>
                                                                ))}
                                                        </ScrollArea>
                                                    </DropdownMenuContent>
                                                </DropdownMenu>
                                            </div>
                                        </div>
                                    </AccordionContent>
                                </AccordionItem>

                                <AccordionItem value="per-occupied-room">
                                    <AccordionTrigger data-qa-id="accordion-trigger-perOccupiedRoom">
                                        Per Occupied Room
                                    </AccordionTrigger>
                                    <AccordionContent>
                                        <div className="grid gap-4 p-4 border rounded-lg">
                                            <FormField
                                                control={form.control}
                                                name="perOccupiedRoom.enabled"
                                                render={({ field }) => (
                                                    <FormItem className="flex flex-row items-center justify-between">
                                                        <div className="space-y-0.5">
                                                            <FormLabel>
                                                                Enable Per Occupied Room
                                                            </FormLabel>
                                                            <FormDescription>
                                                                Bill customer based on occupied rooms.
                                                            </FormDescription>
                                                        </div>
                                                        <FormControl>
                                                            <Switch
                                                                checked={field.value}
                                                                onCheckedChange={field.onChange}
                                                                disabled={!isEditable}
                                                                data-qa-id="switch-component-perOccupiedRoomEnabled"
                                                            />
                                                        </FormControl>
                                                    </FormItem>
                                                )}
                                            />
                                            <div className="gap-4 p-4 border rounded-lg flex items-start justify-start">
                                                <FormField
                                                    control={form.control}
                                                    name="perOccupiedRoom.roomRate"
                                                    render={({ field }) => (
                                                        <FormItem>
                                                            <FormLabel>Rate</FormLabel>
                                                            <FormControl>
                                                                <NumericFormat
                                                                    displayType="input"
                                                                    thousandSeparator={true}
                                                                    decimalScale={2}
                                                                    fixedDecimalScale={true}
                                                                    prefix="$"
                                                                    inputMode="numeric"
                                                                    allowNegative={false}
                                                                    placeholder="Room Rate"
                                                                    value={String(field.value)}
                                                                    onValueChange={(values) => {
                                                                        field.onChange(
                                                                            values.floatValue || 0
                                                                        );
                                                                    }}
                                                                    customInput={Input}
                                                                    disabled={!watch(
                                                                        "perOccupiedRoom.enabled"
                                                                    ) || !isEditable}
                                                                    data-qa-id="input-field-perOccupiedRoomRate"
                                                                />
                                                            </FormControl>
                                                            <FormMessage />
                                                        </FormItem>
                                                    )}
                                                />
                                                {watchInvoiceGroupingEnabled && <FormField
                                                    control={form.control}
                                                    name="perOccupiedRoom.invoiceGroup"
                                                    render={({ field }) => (
                                                        <FormItem className="w-1/4">
                                                            <FormLabel>Invoice</FormLabel>
                                                            <FormControl>
                                                                <Select
                                                                    required
                                                                    disabled={!watch(
                                                                        "perOccupiedRoom.enabled"
                                                                    ) || !isEditable}
                                                                    onValueChange={field.onChange}
                                                                    defaultValue={
                                                                        field.value?.toString() || ""
                                                                    }
                                                                    data-qa-id="select-perOccupiedRoomInvoiceGroup"
                                                                >
                                                                    <SelectTrigger className="w-full" data-qa-id="select-trigger-perOccupiedRoomInvoiceGroup">
                                                                        <SelectValue />
                                                                    </SelectTrigger>
                                                                    <SelectContent>
                                                                        {invoiceGroups.map((group) => (
                                                                            <SelectItem
                                                                                key={group.groupNumber}
                                                                                value={group.groupNumber.toString()}
                                                                                data-qa-id={`select-item-perOccupiedRoomInvoiceGroup-${group.groupNumber}`}
                                                                            >{`${group.groupNumber}`}</SelectItem>
                                                                        ))}
                                                                    </SelectContent>
                                                                </Select>
                                                            </FormControl>
                                                            <FormMessage />
                                                        </FormItem>
                                                    )}
                                                />}
                                            </div>
                                        </div>
                                    </AccordionContent>
                                </AccordionItem>

                                <AccordionItem value="revenue-share">
                                    <AccordionTrigger data-qa-id="accordion-trigger-revenueShare">Revenue Share</AccordionTrigger>
                                    <AccordionContent>
                                        <RevenueShare
                                        
                                            revenueShareEnabled={contractDetails?.revenueShare?.enabled || false}
                                            setRevenueShareEnabled={setRevenueShareEnabled}                                 
                                            thresholdStructuresvalues={contractDetails?.revenueShare?.thresholdStructures || []}                                           
                                            setThresholdStructures={setThresholdStructures}
                                            revenueCodes={revenueCodes}
                                            invoiceGroups={invoiceGroups}
                                            watchInvoiceGroupingEnabled={watchInvoiceGroupingEnabled}
                                            isEditable={isEditable}

                                        />
                                    </AccordionContent>
                                </AccordionItem>

                                <AccordionItem value="additional-fees">
                                    <AccordionTrigger data-qa-id="accordion-trigger-additionalFees">Additional Fees or Line Items</AccordionTrigger>
                                    <AccordionContent>
                                        <AdditionalFees
                                            bellServiceFee={currentBellServiceFee}
                                            onUpdateBellServiceFee={handleBellServiceFeeUpdate}
                                            invoiceGroups={invoiceGroups}
                                            watchInvoiceGroupingEnabled={watchInvoiceGroupingEnabled}
                                            midMonthAdvance={currentMidMonthAdvanced}
                                            onUpdateMidMonthAdvanced={handleMidMonthAdvanceUpdate}
                                            depositedRevenue={currentDepositedRevenue}
                                            onUpdateDepositedRevenue={handleDepositedRevenueUpdate}
                                            isEditable={isEditable}
                                        />
                                    </AccordionContent>
                                </AccordionItem>

                                <AccordionItem value="billable-accounts">
                                    <AccordionTrigger data-qa-id="accordion-trigger-billableAccounts">Billable Accounts</AccordionTrigger>
                                    <AccordionContent>
                                        <BillablePayrollComponent
                                            billableAccounts={form.watch("billableAccounts")}
                                            onUpdateBillableAccounts={handlePayrollAccountsUpdate}
                                            watchInvoiceGroupingEnabled={watchInvoiceGroupingEnabled}
                                            invoiceGroups={invoiceGroups}
                                            payrrolAccount={payrollAccounts.reduce((acc, account) => {
                                                acc[account.id] = account.name;
                                                return acc;
                                            }, {} as Record<string, string>)}
                                            expenseAccount={expenseAccounts.reduce((acc, account) => {
                                                acc[account.id] = account.name;
                                                return acc;
                                            }, {} as Record<string, string>)}
                                            errors={form.formState.errors}
                                            isEditable={isEditable}
                                        />
                                    </AccordionContent>
                                </AccordionItem>

                                <AccordionItem value="management-agreement">
                                    <AccordionTrigger data-qa-id="accordion-trigger-managementAgreement">Management Agreement</AccordionTrigger>
                                    <AccordionContent>
                                        <>
                                            <ManagementAgreement
                                                 managementAgreement={form.watch("managementAgreement")}
                                                onUpdateManagementAgreement={handleManagementAgreementUpdate}
                                                invoiceGroups={invoiceGroups}
                                                watchInvoiceGroupingEnabled={watchInvoiceGroupingEnabled}
                                                billableAccounts={form.watch("billableAccounts")}
                                                errors={form.formState.errors}
                                                isEditable={isEditable}
                                            />
                                        </>
                                    </AccordionContent>
                                </AccordionItem>


                                <AccordionItem value="supporting-reports">
                                    <AccordionTrigger data-qa-id="accordion-trigger-supportingReports">Supporting Documents</AccordionTrigger>
                                    <AccordionContent>
                                        <SupportingReports
                                            reportsState={reportsState}
                                            onReportTypeChange={handleReportTypeChange}
                                            isFixedFeeEnabled={watchFixedFeeEnabled}
                                            isPerLaborHourEnabled={watchPerLaborHourEnabled}
                                            isPerOccupiedRoomEnabled={watch("perOccupiedRoom.enabled")}
                                            isRevenueShareEnabled={revenueShareEnabled}
                                            isManagementAgreementEnabled={watchManagementAgreementEnabled}
                                            isEditable={isEditable}
                                            isBillingAdmin={isBillingAdmin}
                                            isBillablePayrollEnabled={watchBillablePayrollEnabled}
                                            isRevenueShareValidationEnabled={thresholdStructures[0]?.validationThresholdType !== null}
                                            isManagementAgreementValidationEnabled={
                                                watchManagementAgreementEnabled &&
                                                form.watch("managementAgreement.ManagementFees.0.profitShareEnabled") &&
                                                form.watch("managementAgreement.ManagementFees.0.validationThresholdEnabled")
                                            }
                                        />
                                    </AccordionContent>
                                </AccordionItem>


                            </Accordion>
                        </section>
                    </form>
                </Form>

                <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
                    <DialogContent>
                        <h2 className="text-xl font-semibold">Warning</h2>
                        <p className="mt-2">
                            Turning off multiple invoices will remove all invoice groups. Do you wish to proceed?
                        </p>
                        <DialogFooter className="mt-4">
                            <Button variant="outline" onClick={handleDialogCancel} className="ml-2" disabled={!isEditable} data-qa-id="button-dialogCancel">
                                Cancel
                            </Button>
                            <Button onClick={handleDialogConfirm} disabled={!isEditable} data-qa-id="button-dialogConfirm">
                                Yes, Proceed
                            </Button>
                        </DialogFooter>
                    </DialogContent>
                </Dialog>

                <Dialog open={isCancelDialogOpen} onOpenChange={setIsCancelDialogOpen}>
                    <DialogContent>
                        <h2 className="text-xl font-semibold">Confirm Cancel</h2>
                        <p className="mt-2">
                            Any changes since your last Save will be lost, ok to Cancel?
                        </p>
                        <DialogFooter className="mt-4">
                            <Button
                                variant="outline"
                                onClick={() => setIsCancelDialogOpen(false)}
                                className="ml-2"
                                data-qa-id="button-cancelDialogNo"
                            >
                                No
                            </Button>
                            <Button
                                onClick={handleConfirmCancel}
                                data-qa-id="button-cancelDialogYes"
                            >
                                Yes
                            </Button>
                        </DialogFooter>
                    </DialogContent>
                </Dialog>
            </div>
        </TooltipProvider>
    );
}