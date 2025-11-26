import { z } from "zod";
import { DataEntity } from "./DataEntity";

export interface Contract extends DataEntity {
  purchaseOrder: string;
  paymentTerms: PaymentTermsType;
  billingType: AdvancedArrearsType;
  incrementMonth: string;
  incrementAmount: number;
  consumerPriceIndex: boolean;
  notes: string;
  fixedFee: FixedFeeTerms;
  perLaborHour: PerLaborHourTerms;
  perOccupiedRoom: PerOccupiedRoom;
  deviationPercentage: number;
  deviationAmount: number;
  invoiceGrouping: InvoiceGrouping;
  contractType: string;
  deposits: boolean;
  revenueShare: RevenueShareTerms;
  bellServiceFee: BellServiceFeeTerms;
  midMonthAdvance: MidMonthAdvancedTerms;
  depositedRevenue: DepositedRevenueTerms;
  billableAccounts: BillablePayrollTerms;
  managementAgreement: ManagementAgreementTerms;
  supportingReports: SupportingReportsType[];
  customerSiteId?: string;
  incrementType?: string;
  billableAccount?: BillableAccount;
}

enum PaymentTermsType {
  PAYMENT_TERM_FIRST_OF_MONTH = "Due by 1st Day of the Month",
  PAYMENT_TERM_DUE_ON_RECEIPT = "Due on Receipt",
  PAYMENT_TERM_DUE_IN = "Due in",
  PAYMENT_TERM_CUSTOM_DUE_BY = "Due by",
}

enum AdvancedArrearsType {
  ADVANCED = "Advanced",
  ARREARS = "Arrears",
}

export enum SupportingReportsType {
  MIX_OF_SALES = "MixOfSales",
  HOURS_BACKUP_REPORT = "HoursBackupReport",
  TAX_REPORT = "TaxReport",
  LABOR_DISTRIBUTION_REPORT = "LaborDistributionReport",
  OTHER_EXPENSES = "OtherExpensesReport",
  PARKING_DEPARTMENT_REPORT = "ParkingDepartmentReport",
  VALIDATION_REPORT = "ValidationReport",
}

interface ContractBuildingBlock {
  enabled: boolean;
}

interface FixedFeeTerms extends ContractBuildingBlock {
  serviceRates: ServiceRate[];
}

interface PerLaborHourTerms extends ContractBuildingBlock {
  hoursBackupReport: boolean;
  jobRates: JobRate[];
}

interface PerOccupiedRoom extends ContractBuildingBlock {
  roomRate: number;
  invoiceGroup: string;
}

export interface ServiceRate {
  id?: string;
  name: string;
  displayName: string;
  code: string;
  fee: number;
  invoiceGroup: string | null;
}

export interface JobRate {
  id?: string;
  name: string;
  displayName: string;
  jobCode: string;
  rate: number;
  overtimeRate: number;
  invoiceGroup: string | null;
}

export interface UpdateDeviation {
  contractId: string;
  deviationAmount?: number | null;
  deviationPercentage?: number | null;
}

interface InvoiceGrouping {
  enabled: boolean;
  invoiceGroups: InvoiceGroup[];
}

export interface InvoiceGroup {
  id: string;
  groupNumber: number;
  title: string;
  description: string | null;
  vendorId?: string;
  siteNumber?: string;
  customerName?: string;
  billingContactEmails?: string;
}

export interface ContractConfig {
  defaultRate: number;
  defaultOvertimeRate: number;
  defaultFee: number;
  glCodes: GLCode[];
}

export interface GLCode {
  code: string;
  name: string;
  type: string;
}

export interface PayrollAccount {
  id: string;
  name: string;
}

export interface RevenueShareTerms extends ContractBuildingBlock {
  thresholdStructures: ThresholdStructure[];
}

export enum RevenueAccumulation {
  MONTHLY = "Monthly",
  ANNUALLY_CALENDAR = "AnnualCalendar",
  ANNUALLY_ANIVERSARY = "AnnualAnniversary",
}

export interface ThresholdStructure {
  id?: string;
  tiers: Tier[];
  revenueCodes: string[];
  accumulationType?: string;
  invoiceGroup: number;
  validationThresholdType: ValidationThresholdType | null;
  validationThresholdAmount: number;
}

export interface JobCode {
  id?: string;
  code: string;
  description: string;
  standardRate: number;
  overtimeRate: number;
  standardRateEscalatorValue: number;
  overtimeRateEscalatorValue: number;
}

export interface Tier {
  sharePercentage: number;
  amount?: number;
  escalatorValue?: number;
}

export enum ValidationThresholdType {
  REVENUE_PERCENTAGE = "RevenuePercentage",
  VALIDATION_AMOUNT = "ValidationAmount",
  VEHICLE_COUNT = "VehicleCount",
}

export interface BellServiceFeeTerms extends ContractBuildingBlock {
  bellServices: BellService[];
}

export interface BellService {
  id?: string;
  invoiceGroup: number;
}

export interface MidMonthAdvancedTerms extends ContractBuildingBlock {
  midMonthAdvances: MidMonthAdvances[];
}

export interface MidMonthAdvances {
  id?: string;
  amount: number;
  lineTitle: LineTitles | null;
  invoiceGroup: number;
}

export enum LineTitles {
  LESS_MID_MONTH_BILLING = "MidMonthBilling",
  LESS_PRE_BILL = "PreBill",
}

export interface DepositedRevenueTerms extends ContractBuildingBlock {
  depositData: DepositData[];
}

export interface DepositData {
  id?: string;
  towneParkResponsibleForParkingTax: boolean;
  depositedRevenueEnabled: boolean;
  invoiceGroup: number;
}

export interface BillablePayrollTerms extends ContractBuildingBlock {
  billableAccountsData: BillableAccountsData[];
}

export interface BillableAccountsData {
  id?: string;
  payrollAccountsData: string;
  payrollAccountsInvoiceGroup: number;
  payrollAccountsLineTitle: string;
  payrollTaxesEnabled: boolean;
  payrollTaxesBillingType: PTEBBillingType;
  payrollTaxesPercentage: number | null;
  payrollTaxesLineTitle: string;
  payrollSupportEnabled: boolean;
  payrollSupportBillingType: SupportServicesType;
  payrollSupportPayrollType: SupportPayrollType;
  payrollSupportAmount: number | null;
  payrollSupportLineTitle: string;
  payrollExpenseAccountsData: string;
  payrollExpenseAccountsInvoiceGroup: number;
  payrollExpenseAccountsLineTitle: string;
  additionalPayrollAmount: number | null;
  payrollTaxesEscalatorEnable:boolean;
  payrollTaxesEscalatorMonth:Month;
  payrollTaxesEscalatorvalue:number;
  payrollTaxesEscalatorType:EscalatorFormatType;
}

export enum SupportServicesType {
  FIXED = "Fixed",
  PERCENTAGE = "Percentage",
}

export enum SupportPayrollType {
  BILLABLE = "Billable",
  TOTAL = "Total",
}

export enum PTEBBillingType {
  ACTUAL = "Actual",
  PERCENTAGE = "Percentage",
}

export interface PayrollDataItem {
  code: string;
  title: string;
  isEnabled: boolean;
}

export interface ManagementAgreementTerms extends ContractBuildingBlock {
  ManagementFees: ManagementFee[];
}

export interface ManagementFee {
  id?: string;
  invoiceGroup: number;
  managementAgreementType: ManagementAgreementType;
  managementFeeEscalatorEnabled: boolean;
  managementFeeEscalatorMonth: Month;
  managementFeeEscalatorType: EscalatorFormatType;
  managementFeeEscalatorValue: number | null;
  fixedFeeAmount: number | null;
  perLaborHourJobCodeData: JobCode[];
  laborHourJobCode: string | null;
  laborHourRate: number | null;
  laborHourOvertimeRate: number | null;
  revenuePercentageAmount: number | null;
  insuranceEnabled: boolean;
  insuranceLineTitle: string | null;
  insuranceType: InsuranceType;
  insuranceAdditionalPercentage: number | null;
  insuranceFixedFeeAmount: number | null;
  claimsEnabled: boolean;
  claimsCapAmount: number | null;
  claimsLineTitle: string | null;
  claimsType: ClaimsType;
  profitShareEnabled: boolean;
  profitShareAccumulationType: ProfitShareAccumulationType;
  profitShareTierData: Tier[];
  profitShareEscalatorEnabled: boolean,
  profitShareEscalatorMonth: Month,
  profitShareEscalatorType: EscalatorFormatType,
  validationThresholdEnabled: boolean;
  validationThresholdAmount: number | null;
  validationThresholdType: ValidationThresholdType;
  nonGlBillableExpensesEnabled:boolean;
  nonGlBillableExpenses: NonGlBillableExpenseDto[]; 
}

export interface NonGlBillableExpenseDto {
  id?: string;
  nonglexpensetype:NonGlExpensetype;
  expensepayrolltype: ExpensetPayrollType;
  expenseamount: number | null;
  expensetitle: string;
 finalperiodbilled: Date | null; 
 sequenceNumber:number | null;
 
}

export interface BillableAccount {
  enabled: boolean;
  billableAccountsData?: BillableAccountData[];
}

export interface BillableAccountData {
  accountNumber?: string;
  description?: string;
}

export enum Month {
  JANUARY = "January",
  FEBRUARY = "February",
  MARCH = "March",
  APRIL = "April",
  MAY = "May",
  JUNE = "June",
  JULY = "July",
  AUGUST = "August",
  SEPTEMBER = "September",
  OCTOBER = "October",
  NOVEMBER = "November",
  DECEMBER = "December",
}

export enum EscalatorFormatType {
  PERCENTAGE = "Percentage",
  FIXEDAMOUNT = "FixedAmount",
}

export enum NonGlExpensetype {
  FIXEDAMOUNT = "FixedAmount",
  PAYROLL = "Payroll",
  REVENUE ="Revenue",
 
}

export enum ExpensetPayrollType {
  BILLABLE = "Billable",
  TOTAL = "Total",
}
export enum ManagementAgreementType {
  FIXED_FEE = "FixedFee",
  PER_LABOR_HOUR = "PerLaborHour",
  REVENUE_PERCENTAGE = "RevenuePercentage",
}

export enum InsuranceType {
  BASED_ON_BILLABLE_ACCOUNTS = "BasedOnBillableAccounts",
  FIXED_FEE = "FixedFee",
}

export enum ClaimsType {
  PER_CLAIM = "PerClaim",
  ANNUALLY_CALENDAR = "AnnualCalendar",
  ANNUALLY_ANIVERSARY = "AnnualAnniversary",
}

export enum ProfitShareAccumulationType {
  MONTHLY = "Monthly",
  ANNUALLY_CALENDAR = "AnnualCalendar",
  ANNUALLY_ANIVERSARY = "AnnualAnniversary",
}

// FORM SCHEMAS

const ContractBuildingBlockSchema = z.object({
  enabled: z.boolean(),
});

const FixedFeeServiceRateSchema = z.object({
  id: z.string().optional(),
  name: z.string(),                               
  displayName: z.string(), 
  code: z.string(),
  fee: z.coerce.number(),  
  invoiceGroup: z.string(),
});


const StrictFixedFeeServiceRateSchema = FixedFeeServiceRateSchema
  .refine(data => data.displayName.trim().length > 0, {
    message: "Display Name is required",
    path: ["displayName"],
  })
  .refine(data => data.fee > 0, {
    message: "Number must be greater than 0",
    path: ["fee"], 
  })

const FixedFeeTermsSchema = ContractBuildingBlockSchema.extend({
  serviceRates: z.array(FixedFeeServiceRateSchema),
}).superRefine((data, ctx) => {
  if (data.enabled) {
    data.serviceRates.forEach((rate, index) => {
      const result = StrictFixedFeeServiceRateSchema.safeParse(rate);
      if (!result.success) {
  
        result.error.errors.forEach((err) => {
          ctx.addIssue({
            ...err, 
            path: ["serviceRates", index, ...err.path],
          });
        });
      }
    });
  }
});

const PerLaborHourJobRateSchema = z.object({
 id: z.string().optional(),
  name: z.string(),
  displayName: z.string(),
  jobCode: z.string(),     
  rate: z.coerce.number(), 
  overtimeRate: z.coerce.number(),
  invoiceGroup: z.string(),
});

const PerLaborHourTermsSchema = ContractBuildingBlockSchema.extend({
  hoursBackupReport: z.boolean(),
  jobRates: z.array(PerLaborHourJobRateSchema),
}).superRefine((data, ctx) => {
  if (!data.enabled) return;

  const seenCodes = new Set();
  data.jobRates.forEach((jobRate, index) => {
    if (!jobRate.displayName || jobRate.displayName.trim().length === 0) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: "Display Name is required",
        path: ["jobRates", index, "displayName"],
      });
    }
    if (!jobRate.jobCode || jobRate.jobCode.trim().length === 0) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: "Job Code is required",
        path: ["jobRates", index, "jobCode"],
      });
    }
    if (!jobRate.rate || jobRate.rate <= 0) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: "Number must be greater than 0",
        path: ["jobRates", index, "rate"],
      });
    }
    if (!jobRate.overtimeRate || jobRate.overtimeRate <= 0) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: "Number must be greater than 0",
        path: ["jobRates", index, "overtimeRate"],
      });
    }
    if (!jobRate.invoiceGroup || jobRate.invoiceGroup.trim().length === 0) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: "Invoice Group is required",
        path: ["jobRates", index, "invoiceGroup"],
      });
    }
    if (jobRate.jobCode) {
      if (seenCodes.has(jobRate.jobCode)) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: `Duplicate job code detected: ${jobRate.jobCode}.`,
          path: ["jobRates", index, "jobCode"],
        });
      } else {
        seenCodes.add(jobRate.jobCode);
      }
    }
  });
});
const PerOccupiedRoomSchema = ContractBuildingBlockSchema.extend({
  roomRate: z.number(),
  invoiceGroup: z.string(),
});

const InvoiceGroupSchema = z.object({
  id: z.string(),
  groupNumber: z.number(),
  title: z.string(),
  description: z.string().nullable(),
});

const InvoiceGroupingSchema = z.object({
  enabled: z.boolean(),
  invoiceGroups: z.array(InvoiceGroupSchema),
});

const BillableAccountsSchema = z.object({
  id: z.string().optional(),
  payrollAccountsData: z.string(),
  payrollAccountsInvoiceGroup: z.number(),
  payrollAccountsLineTitle: z.string(),
  payrollTaxesEnabled: z.boolean(),
  payrollTaxesBillingType: z.nativeEnum(PTEBBillingType),
  payrollTaxesPercentage: z.number().nullable(),
  payrollTaxesLineTitle: z.string(),
  payrollSupportEnabled: z.boolean(),
  payrollSupportBillingType: z.nativeEnum(SupportServicesType),
  payrollSupportPayrollType: z.nativeEnum(SupportPayrollType),
  payrollSupportAmount: z.number().nullable(),
  payrollSupportLineTitle: z.string(),
  payrollExpenseAccountsData: z.string(),
  payrollExpenseAccountsInvoiceGroup: z.number(),
  payrollExpenseAccountsLineTitle: z.string(),
  additionalPayrollAmount: z.number().nullable(),
  payrollTaxesEscalatorEnable: z.boolean(),
  payrollTaxesEscalatorMonth: z.nativeEnum(Month),
  payrollTaxesEscalatorvalue: z.number(),
  payrollTaxesEscalatorType: z.nativeEnum(EscalatorFormatType),
}).refine((data) => {
  if (data.payrollTaxesBillingType === PTEBBillingType.PERCENTAGE) {
    return data.payrollTaxesPercentage !== null && data.payrollTaxesPercentage > 0;
  }
  return true;
}, {
  message: "Percentage must be greater than 0%.",
  path: ["payrollTaxesPercentage"],
});
const ManagementFeeSchema = z.object({
  id: z.string().optional(),
  invoiceGroup: z.number(),
  managementAgreementType: z.nativeEnum(ManagementAgreementType),
  managementFeeEscalatorEnabled: z.boolean(),
  managementFeeEscalatorMonth: z.nativeEnum(Month),
  managementFeeEscalatorType: z.nativeEnum(EscalatorFormatType),
  managementFeeEscalatorValue: z.number().nullable(),
  fixedFeeAmount: z.number().nullable(),
  perLaborHourJobCodeData: z.array(
    z.object({
      code: z.string(),
      description: z.string(),
      standardRate: z.number(),
      overtimeRate: z.number(),
      standardRateEscalatorValue: z.number(),
      overtimeRateEscalatorValue: z.number(),
    })
  ).default([]),
  laborHourJobCode: z.string().nullable(),
  laborHourRate: z.number().nullable(),
  laborHourOvertimeRate: z.number().nullable(),
  revenuePercentageAmount: z.number().nullable(),
  insuranceEnabled: z.boolean(),
  insuranceLineTitle: z.string().nullable(),
  insuranceType: z.nativeEnum(InsuranceType),
  insuranceAdditionalPercentage: z.number().nullable(),
  insuranceFixedFeeAmount: z.number().nullable(),
  claimsEnabled: z.boolean(),
  claimsCapAmount: z.number().nullable(),
  claimsLineTitle: z.string().nullable(),
  claimsType: z.nativeEnum(ClaimsType),
  profitShareEnabled: z.boolean(),
  profitShareAccumulationType: z.nativeEnum(ProfitShareAccumulationType),
  profitShareTierData: z.array(z.object({
    sharePercentage: z.number(),
    amount: z.number(),
    escalatorValue: z.number(),
  })),
  profitShareEscalatorEnabled: z.boolean(),
  profitShareEscalatorMonth: z.nativeEnum(Month),
  profitShareEscalatorType: z.nativeEnum(EscalatorFormatType),
  validationThresholdEnabled: z.boolean(),
  validationThresholdAmount: z.number().nullable(),
  validationThresholdType: z.nativeEnum(ValidationThresholdType),
  nonGlBillableExpensesEnabled: z.boolean(),
  nonGlBillableExpenses: z.array(
    z.object({
      id: z.string().optional(),
      nonglexpensetype: z.nativeEnum(NonGlExpensetype),
      expensepayrolltype: z.nativeEnum(ExpensetPayrollType),
      expenseamount: z.coerce.number().nullable(),
      sequenceNumber: z.number().nullable(),
      expensetitle: z.string()
        .min(1, "Title is required")
        .refine(val => val.trim().length > 0, "Title cannot be empty or just whitespace"),
      finalperiodbilled: z
        .union([
          z.date(),
          z.string().refine((val) => !isNaN(Date.parse(val))),
        ])
        .nullable()
        .transform((val) => (val ? new Date(val) : null)),
    })
    .refine(
      (data) => {
        if (data.nonglexpensetype === NonGlExpensetype.FIXEDAMOUNT) {
          return data.expenseamount !== null &&
            !isNaN(data.expenseamount!) &&
            data.expenseamount! > 0;
        }
        else if (data.nonglexpensetype === NonGlExpensetype.REVENUE ||
          data.expensepayrolltype) {
          return data.expenseamount !== null &&
            !isNaN(data.expenseamount!) &&
            data.expenseamount! > 0;
        }
        return true;
      },
      (data) => {
        if (data.nonglexpensetype === NonGlExpensetype.FIXEDAMOUNT) {
          return {
            message: "Amount is required",
            path: ["expenseamount"]
          };
        }
        else if (data.nonglexpensetype === NonGlExpensetype.REVENUE ||
          data.expensepayrolltype) {
          return {
            message: "Percentage is required",
            path: ["expenseamount"]
          };
        }
        return {
          message: "Value is required",
          path: ["expenseamount"]
        };
      }
    )
  )
});
const ManagementAgreementSchema = z.object({
  enabled: z.boolean(),
  ManagementFees: z.array(ManagementFeeSchema)
}).superRefine((data, ctx) => {
  if (!data.enabled) return;
  data.ManagementFees.forEach((fee, feeIndex) => {
    if (fee.managementAgreementType === ManagementAgreementType.PER_LABOR_HOUR) {
      if (!fee.perLaborHourJobCodeData || fee.perLaborHourJobCodeData.length === 0) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: "At least one job code is required.",
          path: ["ManagementFees", feeIndex, "perLaborHourJobCodeData"],
        });
        return;
      }
      fee.perLaborHourJobCodeData.forEach((jobCode: any, index: number) => {
        if (!jobCode.code || jobCode.code.trim().length === 0) {
          ctx.addIssue({
            code: z.ZodIssueCode.custom,
            message: "Code is required.",
            path: ["ManagementFees", feeIndex, "perLaborHourJobCodeData", index, "code"],
          });
        }
        if (
          jobCode.standardRate === undefined ||
          jobCode.standardRate === null ||
          isNaN(jobCode.standardRate) ||
          jobCode.standardRate <= 0
        ) {
          ctx.addIssue({
            code: z.ZodIssueCode.custom,
            message: "Standard Rate must be greater than 0.",
            path: ["ManagementFees", feeIndex, "perLaborHourJobCodeData", index, "standardRate"],
          });
        }
        if (
          jobCode.overtimeRate === undefined ||
          jobCode.overtimeRate === null ||
          isNaN(jobCode.overtimeRate) ||
          jobCode.overtimeRate <= 0
        ) {
          ctx.addIssue({
            code: z.ZodIssueCode.custom,
            message: "Overtime Rate must be greater than 0.",
            path: ["ManagementFees", feeIndex, "perLaborHourJobCodeData", index, "overtimeRate"],
          });
        }
      });
 
      const seenCodes = new Set();
      fee.perLaborHourJobCodeData.forEach((jobCode: any, index: number) => {
        if (seenCodes.has(jobCode.code)) {
          ctx.addIssue({
            code: z.ZodIssueCode.custom,
            message: `Duplicate Job Code detected: ${jobCode.code}.`,
            path: ["ManagementFees", feeIndex, "perLaborHourJobCodeData", index, "code"],
          });
        } else {
          seenCodes.add(jobCode.code);
        }
      });
    }
  });
});
const ContractSchema = z.object({
  purchaseOrder: z.string(),
  paymentTerms: z.nativeEnum(AdvancedArrearsType).or(z.string().nullable().refine((value) => value !== '', 'Payment Terms is required')),
  billingType: z.nativeEnum(AdvancedArrearsType),
  incrementMonth: z.string(),
  incrementAmount: z.number().nonnegative().max(100),
  consumerPriceIndex: z.boolean(),
  notes: z.string(),
  contractType: z.string(),
  deposits: z.boolean(),
  fixedFee: FixedFeeTermsSchema,
  perLaborHour: PerLaborHourTermsSchema,
  perOccupiedRoom: PerOccupiedRoomSchema,
  deviationPercentage: z.number().nonnegative().max(100),
  deviationAmount: z.number().nonnegative(),
  invoiceGrouping: InvoiceGroupingSchema,
  revenueShare: z.object({
    enabled: z.boolean(),
    thresholdStructures: z.array(z.object({
      id: z.string().optional(),
      tiers: z.array(z.object({
        sharePercentage: z.number().min(0, { message: "Share Percentage must be a positive number" }),
        amount: z.number().min(0, { message: "Amount must be a positive number" }),
      })),
      revenueCodes: z.array(z.string()),
      accumulationType: z.string().optional(),
      invoiceGroup: z.number(),
      validationThresholdType: z.nativeEnum(ValidationThresholdType).nullable().or(z.string().nullable().refine((value) => value !== '', 'Validation Threshold Type is required')),
      validationThresholdAmount: z.number().nonnegative().nullable(),
    })),
  }),
  bellServiceFee: z.object({
    enabled: z.boolean(),
    bellServices: z.array(z.object({
      id: z.string().optional(),
      invoiceGroup: z.number(),
    })),
  }),
  midMonthAdvance: z.object({
    enabled: z.boolean(),
    midMonthAdvances: z.array(z.object({
      id: z.string().optional(),
      amount: z.number().nonnegative(),
      lineTitle: z.string().nullable(),
      invoiceGroup: z.number(),
    })),
  }),
  depositedRevenue: z.object({
    enabled: z.boolean(),
    depositData: z.array(z.object({
      id: z.string().optional(),
      towneParkResponsibleForParkingTax: z.boolean(),
      depositedRevenueEnabled: z.boolean(),
      invoiceGroup: z.number(),
    })),
  }),
  billableAccounts: z.object({
    enabled: z.boolean(),
    billableAccountsData: z.array(BillableAccountsSchema),
  }),
   managementAgreement: ManagementAgreementSchema,
  supportingReports: z.array(z.nativeEnum(SupportingReportsType)),
}).refine((data) => {
  if (data.contractType === "Per Labour Hour" && data.consumerPriceIndex) {
    return data.incrementAmount > 0;
  }
  return true;
}, {
  message: "Increment Percentage is required when 'Per Labour Hour' is selected and Consumer Price Index is enabled.",
  path: ["incrementAmount"]
});

const DataEntitySchema = z.object({
  id: z.string().optional(),
  createdAt: z.date().optional(),
});

const ContractWithDataEntitySchema = ContractSchema.and(DataEntitySchema);

export {
  AdvancedArrearsType, ContractSchema, ContractWithDataEntitySchema, DataEntitySchema, FixedFeeTermsSchema, PaymentTermsType, PerLaborHourTermsSchema
};

