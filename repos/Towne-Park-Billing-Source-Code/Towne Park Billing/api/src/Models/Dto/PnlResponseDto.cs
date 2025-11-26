using api.Models.Vo;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace api.Models.Dto
{
    public class PnlResponseDto
    {
        [JsonProperty("year")]
        public int Year { get; set; }

        [JsonProperty("actualRows")]
        public List<PnlRowDto> ActualRows { get; set; } = new List<PnlRowDto>();

        [JsonProperty("budgetRows")]
        public List<PnlRowDto> BudgetRows { get; set; } = new List<PnlRowDto>();

        [JsonProperty("forecastRows")]
        public List<PnlRowDto> ForecastRows { get; set; } = new List<PnlRowDto>();

        [JsonProperty("varianceRows")]
        public List<PnlVarianceRowDto> VarianceRows { get; set; } = new List<PnlVarianceRowDto>();
   
        // Internal-only: expense actuals for current month (for calculators). Not serialized.
        [JsonIgnore]
        public ExpenseActualsDataVo[]? ExpenseActuals { get; set; }
    
    }


    public class SiteMonthlyRevenueDetailDto
    {
        [JsonProperty("siteId")]
        public string SiteId { get; set; } = string.Empty;

        // Internal helper for calculators: attach current-month internal revenue actuals per site
        // Not serialized in API response
        [JsonIgnore]
        public api.Models.Vo.InternalRevenueActualsVo? InternalActuals { get; set; }

        // Expense actuals (current month only) for internal use only (not serialized)
        [JsonIgnore]
        public ExpenseActualsDto? ExpenseActuals { get; set; }

        [JsonProperty("internalRevenueBreakdown")]
        public InternalRevenueBreakdownDto? InternalRevenueBreakdown { get; set; } 

        // Current-month split of internal revenue into actual vs forecast for this site
        // Included in API for calendar/UX scenarios
        [JsonProperty("internalRevenueCurrentMonthSplit")]
        public InternalRevenueCurrentMonthSplitDto? InternalRevenueCurrentMonthSplit { get; set; }

        [JsonProperty("externalRevenueBreakdown")]
        public ExternalRevenueBreakdownDto? ExternalRevenueBreakdown { get; set; }

        [JsonProperty("payrollBreakdown")]
        public PayrollBreakdownDto? PayrollBreakdown { get; set; }

        [JsonProperty("value")]
        public decimal? Value { get; set; }

        [JsonProperty("isForecast")]
        public bool IsForecast { get; set; }

        // Insurance breakdown for tooltip rendering
        [JsonProperty("insuranceBreakdown")] 
        public InsuranceBreakdownDto? InsuranceBreakdown { get; set; }

        // PTEB breakdown for tooltip rendering
        [JsonProperty("ptebBreakdown")]
        public PtebBreakdownDto? PtebBreakdown { get; set; }

    }

    public class ExpenseActualsDto
    {
        [JsonProperty("billableExpenseActuals")]
        public decimal? BillableExpenseActuals { get; set; }

        [JsonProperty("otherExpenseActuals")]
        public decimal? OtherExpenseActuals { get; set; }
    }

    public class InternalRevenueCurrentMonthSplitDto
    {
        [JsonProperty("actualTotal")]
        public decimal Actual { get; set; }

        [JsonProperty("forecastTotal")]
        public decimal Forecast { get; set; }

        [JsonProperty("lastActualDate")]
        public DateTime? LastActualDate { get; set; }

        [JsonProperty("forecastStartDate")]
        public DateTime? ForecastStartDate { get; set; }
    }

    public class PayrollBreakdownDto
    {
        [JsonProperty("actualPayroll")]
        public decimal? ActualPayroll { get; set; }

        [JsonProperty("forecastedPayroll")]
        public decimal? ForecastedPayroll { get; set; }

        [JsonProperty("actualPayrollLastDate")]
        public DateTime? ActualPayrollLastDate { get; set; }

        [JsonProperty("totalPayroll")]
        public decimal? TotalPayroll { get; set; }
    }

    public class InsuranceBreakdownDto
    {
        [JsonProperty("ratePercent")] 
        public decimal? RatePercent { get; set; }

        [JsonProperty("basePayroll")] 
        public decimal? BasePayroll { get; set; }

        // Indicates whether BasePayroll is sourced from Forecast or Budget
        [JsonProperty("basePayrollSource")] 
        public string? BasePayrollSource { get; set; }

        [JsonProperty("vehicleInsurance7082")] 
        public decimal? VehicleInsurance7082 { get; set; }

        [JsonProperty("additionalInsurance")] 
        public decimal? AdditionalInsurance { get; set; }

        [JsonProperty("isManagementAgreement")] 
        public bool IsManagementAgreement { get; set; }

        [JsonProperty("source")] 
        public string Source { get; set; } = string.Empty; // Forecast | Actual

        [JsonProperty("actualizationDate")] 
        public DateTime? ActualizationDate { get; set; }
    }

    public class PtebBreakdownDto
    {
        [JsonProperty("ratePercent")] 
        public decimal? RatePercent { get; set; }

        [JsonProperty("basePayroll")] 
        public decimal? BasePayroll { get; set; }

        // e.g., "Forecast" or "Actual" (typically Forecast when derived)
        [JsonProperty("source")] 
        public string? Source { get; set; }
    }

    public class PnlRowDto
    {
        [JsonProperty("columnName")]
        public string ColumnName { get; init; } = string.Empty;

        [JsonProperty("monthlyValues")]
        public List<MonthValueDto> MonthlyValues { get; init; } = new List<MonthValueDto>();

        [JsonProperty("total")]
        public decimal? Total { get; init; }

        [JsonProperty("percentOfInternalRevenue")]
        public decimal? PercentOfInternalRevenue { get; set; }
    }

    public class PnlVarianceRowDto
    {
        [JsonProperty("columnName")]
        public string ColumnName { get; init; } = default!;

        [JsonProperty("monthlyVariances")]
        public List<MonthVarianceDto> MonthlyVariances { get; init; } = new List<MonthVarianceDto>();

        [JsonProperty("totalVarianceAmount")]
        public decimal? TotalVarianceAmount { get; init; }

        [JsonProperty("totalVariancePercent")]
        public decimal? TotalVariancePercent { get; init; }

        public decimal Total { get; set; }
    }

    public class MonthValueDto
    {
        [JsonProperty("month")]
        public int Month { get; init; } 

        [JsonProperty("value")]
        public decimal? Value { get; set; }

        [JsonProperty("internalRevenueBreakdownAggregate")]
        public InternalRevenueBreakdownDto? InternalRevenueBreakdown { get; set; }

        // Aggregated current-month split for InternalRevenue (only populated for current month and single-site requests)
        [JsonProperty("internalRevenueCurrentMonthSplit")]
        public InternalRevenueCurrentMonthSplitDto? InternalRevenueCurrentMonthSplit { get; set; }

        // NEW: Site-level breakdowns for this month
        [JsonProperty("siteDetails")]
        public List<SiteMonthlyRevenueDetailDto>? SiteDetails { get; set; }

    }

    public class MonthVarianceDto
    {
        [JsonProperty("month")]
        public int Month { get; init; } 

        [JsonProperty("amount")]
        public decimal? Amount { get; init; }

        [JsonProperty("percentage")]
        public decimal? Percentage { get; init; }

        public decimal Value { get; set; }
    }

    public class RevenueComponentDto
    {
        [JsonProperty("statistic")]
        public decimal? Statistic { get; set; }

        [JsonProperty("rate")]
        public decimal? Rate { get; set; }

        [JsonProperty("value")]
        public decimal? Value { get; set; } 
    }

    // DTOs for Internal Revenue Breakdown
    public class EscalatorDto
    {
        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("isApplied")]
        public bool IsApplied { get; set; } 
    }

    public class FixedFeeInternalRevenueDto
    {
        [JsonProperty("baseAmount")]
        public decimal? BaseAmount { get; set; }

        [JsonProperty("escalators")]
        public List<EscalatorDto> Escalators { get; set; } = new List<EscalatorDto>();

        [JsonProperty("total")] 
        public decimal? Total { get; set; }
    }

    public class PerOccupiedRoomInternalRevenueDto
    {
        [JsonProperty("feePerRoom")]
        public decimal FeePerRoom { get; set; }

        [JsonProperty("forecastedRooms")]
        public decimal? ForecastedRooms { get; set; }

        [JsonProperty("budgetRooms")]
        public decimal? BudgetRooms { get; set; }

        [JsonProperty("actualRooms")]
        public decimal? ActualRooms { get; set; }

        [JsonProperty("lastActualDate")]
        public DateTime? LastActualDate { get; set; }

        [JsonProperty("baseRevenue")] 
        public decimal? BaseRevenue { get; set; }

        [JsonProperty("actualAmount")]
        public decimal? ActualAmount { get; set; }

        [JsonProperty("escalators")]
        public List<EscalatorDto> Escalators { get; set; } = new List<EscalatorDto>();

        [JsonProperty("total")] 
        public decimal? Total { get; set; }
    }

    public class PerLaborHourInternalRevenueDto
    {
        [JsonProperty("total")] 
        public decimal? Total { get; set; }

        [JsonProperty("actualPerLaborHour")]
        public decimal? ActualPerLaborHour { get; set; }

        [JsonProperty("forecastedPerLaborHour")]
        public decimal? ForecastedPerLaborHour { get; set; }

        [JsonProperty("lastActualDate")]
        public DateTime? LastActualDate { get; set; }
    }

    public class RevenueShareTierDto
    {
        [JsonProperty("thresholdStart")]
        public decimal? ThresholdStart { get; set; }

        [JsonProperty("thresholdEnd")]
        public decimal? ThresholdEnd { get; set; }

        [JsonProperty("percentage")]
        public decimal? Percentage { get; set; }

        [JsonProperty("revenueInTier")]
        public decimal? RevenueInTier { get; set; }

        [JsonProperty("shareAmount")] 
        public decimal? ShareAmount { get; set; }
    }

    public class RevenueShareInternalRevenueDto
    {
        [JsonProperty("actualExternalRevenue")]
        public decimal? ActualExternalRevenue { get; set; }

        [JsonProperty("forecastedExternalRevenue")]
        public decimal? ForecastedExternalRevenue { get; set; }

        // Split share amounts for current month
        [JsonProperty("actualShareAmount")]
        public decimal? ActualShareAmount { get; set; }

        [JsonProperty("forecastedShareAmount")]
        public decimal? ForecastedShareAmount { get; set; }

        [JsonProperty("tiers")]
        public List<RevenueShareTierDto> Tiers { get; set; } = new List<RevenueShareTierDto>();

        [JsonProperty("escalators")]
        public List<EscalatorDto> Escalators { get; set; } = new List<EscalatorDto>();

        [JsonProperty("total")]
        public decimal? Total { get; set; }
    }

    public class BillableAccountsInternalRevenueDto
    {
       
        [JsonProperty("total")]
        public decimal? Total { get; set; }

        [JsonProperty("pteb")]
        public PtebInternalRevenueDto? Pteb { get; set; }

        [JsonProperty("additionalPayrollAmount")]
        public decimal? AdditionalPayrollAmount { get; set; }

        [JsonProperty("supportServices")]
        public SupportServicesInternalRevenueDto? SupportServices { get; set; }

        [JsonProperty("expenseAccounts")]
        public ExpenseAccountsInternalRevenueDto? ExpenseAccounts { get; set; }
    }

    public class PtebInternalRevenueDto
    {
        [JsonProperty("total")] 
        public decimal? Total { get; set; }
        
        [JsonProperty("calculationType")] 
        public string CalculationType { get; set; } = string.Empty;
        
        [JsonProperty("baseAmount")] 
        public decimal? BaseAmount { get; set; }
        
        [JsonProperty("appliedPercentage")] 
        public decimal? AppliedPercentage { get; set; }

        [JsonProperty("actualPteb")]
        public decimal? ActualPteb { get; set; }

        [JsonProperty("forecastedPteb")]
        public decimal? ForecastedPteb { get; set; }

        [JsonProperty("lastActualDate")]
        public DateTime? LastActualDate { get; set; }
    }

    public class SupportServicesInternalRevenueDto
    {
        [JsonProperty("total")]
        public decimal? Total { get; set; }

        [JsonProperty("actualSupportServices")]
        public decimal? ActualSupportServices { get; set; }

        [JsonProperty("forecastedSupportServices")]
        public decimal? ForecastedSupportServices { get; set; }

        [JsonProperty("lastActualDate")]
        public DateTime? LastActualDate { get; set; }
    }

    public class ExpenseAccountsInternalRevenueDto
    {
        [JsonProperty("total")]
        public decimal? Total { get; set; }

        [JsonProperty("actualExpenseAccounts")]
        public decimal? ActualExpenseAccounts { get; set; }

        [JsonProperty("forecastedExpenseAccounts")]
        public decimal? ForecastedExpenseAccounts { get; set; }

        [JsonProperty("lastActualDate")]
        public DateTime? LastActualDate { get; set; }
    }

    public class ManagementAgreementComponentDto
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        [JsonProperty("value")]
        public decimal? Value { get; set; }
    }

    public class ManagementAgreementInternalRevenueDto
    {
       
        [JsonProperty("forecastedExternalRevenue")]
        public decimal? ForecastedExternalRevenue { get; set; }

        [JsonProperty("forecastedPayroll")]
        public decimal? ForecastedPayroll { get; set; }

        [JsonProperty("calculatedInsurance")]
        public decimal? CalculatedInsurance { get; set; }

        // Split insurance amounts for current month
        [JsonProperty("actualInsurance")]
        public decimal? ActualInsurance { get; set; }

        [JsonProperty("forecastedInsurance")]
        public decimal? ForecastedInsurance { get; set; }

        [JsonProperty("otherExpensesForecast")]
        public decimal? OtherExpensesForecast { get; set; }

        [JsonProperty("billableExpenseDelta")]
        public decimal? BillableExpenseDelta { get; set; }

        [JsonProperty("components")] 
        public List<ManagementAgreementComponentDto> Components { get; set; } = new List<ManagementAgreementComponentDto>();

        [JsonProperty("escalators")]
        public List<EscalatorDto> Escalators { get; set; } = new List<EscalatorDto>();

        [JsonProperty("total")]
        public decimal? Total { get; set; }

        [JsonProperty("calculatedClaims")]
        public decimal? CalculatedClaims { get; set; }

        [JsonProperty("profitShareEnabled")]
        public bool? ProfitShareEnabled { get; set; }

        [JsonProperty("totalExpensesUsedInProfitCalc")]
        public decimal? TotalExpensesUsedInProfitCalc { get; set; }

        [JsonProperty("profitShareCalculationMethod")]
        public string? ProfitShareCalculationMethod { get; set; }

        [JsonProperty("profitShareDebug")]
        public ProfitShareDebugDto? ProfitShareDebug { get; set; }
    }

    public class ProfitShareDebugDto
    {
        [JsonProperty("currentMonthProfit")]
        public decimal? CurrentMonthProfit { get; set; }
        
        [JsonProperty("accumulatedProfit")]
        public decimal? AccumulatedProfit { get; set; }
        
        [JsonProperty("accumulatedProfitFromHistory")]
        public decimal? AccumulatedProfitFromHistory { get; set; }
        
        [JsonProperty("profitShareAmount")]
        public decimal? ProfitShareAmount { get; set; }
        
        [JsonProperty("profitShareCalculated")]
        public decimal? ProfitShareCalculated { get; set; }
        
        [JsonProperty("accumulationType")]
        public string? AccumulationType { get; set; }
        
        [JsonProperty("accumulationStartMonth")]
        public int? AccumulationStartMonth { get; set; }
        
        [JsonProperty("monthlyProfitBreakdown")]
        public List<MonthlyProfitDto>? MonthlyProfitBreakdown { get; set; }
        
        [JsonProperty("applicableTier")]
        public ApplicableTierDto? ApplicableTier { get; set; }
        
        [JsonProperty("allTiers")]
        public List<TierDto>? AllTiers { get; set; }
        
        [JsonProperty("dataSource")]
        public string? DataSource { get; set; }
        
        [JsonProperty("historicalMonths")]
        public List<int>? HistoricalMonths { get; set; }
        
        [JsonProperty("escalatorApplied")]
        public bool EscalatorApplied { get; set; }
        
        [JsonProperty("escalatorType")]
        public string? EscalatorType { get; set; }
        
        [JsonProperty("escalatorValue")]
        public decimal? EscalatorValue { get; set; }
        
        [JsonProperty("profitShareBeforeEscalator")]
        public decimal? ProfitShareBeforeEscalator { get; set; }
        
        [JsonProperty("formulaBreakdown")]
        public FormulaBreakdownDto? FormulaBreakdown { get; set; }
    }

    public class MonthlyProfitDto
    {
        [JsonProperty("month")]
        public int Month { get; set; }
        
        [JsonProperty("profit")]
        public decimal Profit { get; set; }
        
        [JsonProperty("isHistorical")]
        public bool IsHistorical { get; set; }
    }

    public class ApplicableTierDto
    {
        [JsonProperty("thresholdAmount")]
        public decimal ThresholdAmount { get; set; }
        
        [JsonProperty("sharePercentage")]
        public decimal SharePercentage { get; set; }
        
        [JsonProperty("tierIndex")]
        public int TierIndex { get; set; }
    }

    public class TierDto
    {
        [JsonProperty("amount")]
        public decimal Amount { get; set; }
        
        [JsonProperty("percentage")]
        public decimal Percentage { get; set; }
    }

    public class FormulaBreakdownDto
    {
        [JsonProperty("externalRevenue")]
        public decimal ExternalRevenue { get; set; }
        
        [JsonProperty("managementTotal")]
        public decimal ManagementTotal { get; set; }
        
        [JsonProperty("billableTotal")]
        public decimal BillableTotal { get; set; }
        
        [JsonProperty("claimsAmount")]
        public decimal ClaimsAmount { get; set; }
        
        [JsonProperty("otherExpenses")]
        public decimal OtherExpenses { get; set; }
        
        [JsonProperty("totalExpenses")]
        public decimal TotalExpenses { get; set; }
        
        [JsonProperty("formula")]
        public string Formula { get; set; } = string.Empty;
    }

    public class OtherRevenueInternalRevenueDto
    {
     
        [JsonProperty("total")]
        public decimal? Total { get; set; }
    }

    public class InternalRevenueBreakdownDto
    {
        [JsonProperty("fixedFee")]
        public FixedFeeInternalRevenueDto? FixedFee { get; set; }

        [JsonProperty("perOccupiedRoom")]
        public PerOccupiedRoomInternalRevenueDto? PerOccupiedRoom { get; set; }

        [JsonProperty("perLaborHour")]
        public PerLaborHourInternalRevenueDto? PerLaborHour { get; set; }

        [JsonProperty("revenueShare")]
        public RevenueShareInternalRevenueDto? RevenueShare { get; set; }

        [JsonProperty("billableAccounts")]
        public BillableAccountsInternalRevenueDto? BillableAccounts { get; set; }

        [JsonProperty("managementAgreement")]
        public ManagementAgreementInternalRevenueDto? ManagementAgreement { get; set; }

        [JsonProperty("otherRevenue")]
        public OtherRevenueInternalRevenueDto? OtherRevenue { get; set; }

        [JsonProperty("calculatedTotalInternalRevenue")] 
        public decimal? CalculatedTotalInternalRevenue { get; set; }
    }
    public class ExternalRevenueBreakdownDto
    {
        [JsonProperty("valetDaily")]
        public RevenueComponentDto? ValetDaily { get; set; }

        [JsonProperty("valetMonthly")]
        public RevenueComponentDto? ValetMonthly { get; set; }

        [JsonProperty("valetOvernight")]
        public RevenueComponentDto? ValetOvernight { get; set; }

        [JsonProperty("valetAggregator")]
        public RevenueComponentDto? ValetAggregator { get; set; }

        [JsonProperty("selfDaily")]
        public RevenueComponentDto? SelfDaily { get; set; }

        [JsonProperty("selfMonthly")]
        public RevenueComponentDto? SelfMonthly { get; set; }

        [JsonProperty("selfOvernight")]
        public RevenueComponentDto? SelfOvernight { get; set; }

        [JsonProperty("selfAggregator")]
        public RevenueComponentDto? SelfAggregator { get; set; }

        [JsonProperty("calculatedTotalExternalRevenue")]
        public decimal? CalculatedTotalExternalRevenue { get; set; }

        [JsonProperty("budgetExternalRevenue")]
        public decimal? BudgetTotalExternalRevenue { get; set; }

        [JsonProperty("actualExternalRevenue")]
        public decimal? ActualExternalRevenue { get; set; }

        [JsonProperty("forecastedExternalRevenue")]
        public decimal? ForecastedExternalRevenue { get; set; }

        [JsonProperty("lastActualRevenueDate")]
        public DateTime? LastActualRevenueDate { get; set; }
    }
}
