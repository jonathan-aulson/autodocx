/**
 * PnL (Profit & Loss) data models that align with backend DTOs
 * Note: All numeric values use JavaScript 'number' type which has precision limitations
 * compared to C# decimal types. Consider this when handling financial calculations.
 */

/**
 * Main PnL response containing all data for a specific year
 */
export interface PnlResponse {
    year: number;
    actualRows: PnlRow[];
    budgetRows: PnlRow[];
    forecastRows: PnlRow[];
    varianceRows: PnlVarianceRow[];
}

/**
 * Represents a single row in the PnL statement (actual, budget, or forecast)
 */
export interface PnlRow {
    columnName: string;
    monthlyValues: MonthValue[];
    total: number | null;
    percentOfInternalRevenue: number | null;
}

/**
 * Represents a variance row comparing actual vs budget/forecast
 */
export interface PnlVarianceRow {
    columnName: string;
    monthlyVariances: MonthVariance[];
    totalVarianceAmount: number | null;
    totalVariancePercent: number | null;
    Total: number;
}

/**
 * Monthly value data for a specific PnL line item
 */
export interface MonthValue {
    /** Month index: 0 = January, 1 = February, ..., 11 = December */
    month: number;
    value: number | null;
    /** Aggregated internal revenue breakdown for this month */
    internalRevenueBreakdownAggregate?: InternalRevenueBreakdownDto;
    /** Aggregated current-month split for InternalRevenue (only populated for current month and single-site requests) */
    internalRevenueCurrentMonthSplit?: InternalRevenueCurrentMonthSplitDto;
    /** Site-level revenue details for this month */
    siteDetails?: SiteMonthlyRevenueDetailDto[];
}

/**
 * Monthly variance data comparing actual vs budget/forecast
 */
export interface MonthVariance {
    /** Month index: 0 = January, 1 = February, ..., 11 = December */
    month: number;
    amount: number | null;
    percentage: number | null;
}

/**
 * Site-specific monthly revenue details
 */
export interface SiteMonthlyRevenueDetailDto {
    siteId: string;
    internalRevenueBreakdown?: InternalRevenueBreakdownDto;
    /** Current-month split of internal revenue into actual vs forecast for this site */
    internalRevenueCurrentMonthSplit?: InternalRevenueCurrentMonthSplitDto;
    externalRevenueBreakdown?: ExternalRevenueBreakdownDto;
    payrollBreakdown?: PayrollBreakdownDto;
    value?: number;
    isForecast: boolean;
}

/**
 * External revenue breakdown by parking type and duration
 */
export interface ExternalRevenueBreakdownDto {
    valetDaily?: RevenueComponentDto;
    valetMonthly?: RevenueComponentDto;
    valetOvernight?: RevenueComponentDto;
    valetAggregator?: RevenueComponentDto;
    selfDaily?: RevenueComponentDto;
    selfMonthly?: RevenueComponentDto;
    selfOvernight?: RevenueComponentDto;
    selfAggregator?: RevenueComponentDto;
    calculatedTotalExternalRevenue?: number;
    budgetExternalRevenue?: number;
    actualExternalRevenue?: number;
    forecastedExternalRevenue?: number;
    lastActualRevenueDate?: string;
}

/**
 * Revenue component with statistic, rate, and calculated value
 */
export interface RevenueComponentDto {
    statistic?: number;
    rate?: number;
    value?: number;
}

/**
 * Internal revenue breakdown by contract type
 */
export interface InternalRevenueBreakdownDto {
    fixedFee?: FixedFeeInternalRevenueDto;
    perOccupiedRoom?: PerOccupiedRoomInternalRevenueDto;
    perLaborHour?: PerLaborHourInternalRevenueDto;
    revenueShare?: RevenueShareInternalRevenueDto;
    billableAccounts?: BillableAccountsInternalRevenueDto;
    managementAgreement?: ManagementAgreementInternalRevenueDto;
    otherRevenue?: OtherRevenueInternalRevenueDto;
    calculatedTotalInternalRevenue?: number;
}

/**
 * Escalator applied to fees (e.g., CPI adjustments)
 */
export interface EscalatorDto {
    description: string;
    amount: number;
    isApplied: boolean;
}

/**
 * Fixed fee internal revenue calculation
 */
export interface FixedFeeInternalRevenueDto {
    baseAmount?: number;
    escalators: EscalatorDto[];
    total?: number;
}

/**
 * Per occupied room internal revenue calculation
 */
export interface PerOccupiedRoomInternalRevenueDto {
    feePerRoom: number;
    forecastedRooms?: number;
    budgetRooms?: number;
    baseRevenue?: number;
    escalators: EscalatorDto[];
    total?: number;
}

/**
 * Per labor hour internal revenue calculation
 */
export interface PerLaborHourInternalRevenueDto {
    jobCodeBreakdown: RevenueComponentDto[];
    escalators: EscalatorDto[];
    total?: number;
}

/**
 * Revenue share tier configuration
 */
export interface RevenueShareTierDto {
    thresholdStart?: number;
    thresholdEnd?: number;
    percentage?: number;
    revenueInTier?: number;
    shareAmount?: number;
}

/**
 * Revenue share internal revenue calculation
 */
export interface RevenueShareInternalRevenueDto {
    forecastedExternalRevenue?: number;
    tiers: RevenueShareTierDto[];
    escalators: EscalatorDto[];
    total?: number;
}

/**
 * Billable accounts internal revenue calculation
 */
export interface BillableAccountsInternalRevenueDto {
    total?: number;
}

/**
 * Management agreement component
 */
export interface ManagementAgreementComponentDto {
    name: string;
    value?: number;
}

/**
 * Management agreement internal revenue calculation
 */
export interface ManagementAgreementInternalRevenueDto {
    forecastedExternalRevenue?: number;
    forecastedPayroll?: number;
    calculatedInsurance?: number;
    otherExpensesForecast?: number;
    billableExpenseDelta?: number;
    components: ManagementAgreementComponentDto[];
    escalators: EscalatorDto[];
    total?: number;
}

/**
 * Other revenue internal revenue calculation
 */
export interface OtherRevenueInternalRevenueDto {
    total?: number;
}

/**
 * Current-month split of internal revenue into actual vs forecast
 */
export interface InternalRevenueCurrentMonthSplitDto {
    /** Actual internal revenue amount up to the last actualized date */
    actualTotal: number;
    /** Forecasted internal revenue amount from forecast start date to end of month */
    forecastTotal: number;
    /** Last date with actual data available */
    lastActualDate?: Date | string;
    /** First date of forecast period (typically day after last actual date) */
    forecastStartDate?: Date | string;
}

/**
 * Payroll breakdown for a site/month
 */
export interface PayrollBreakdownDto {
    actualPayroll?: number;
    forecastedPayroll?: number;
    actualPayrollLastDate?: string;
    totalPayroll?: number;
}
