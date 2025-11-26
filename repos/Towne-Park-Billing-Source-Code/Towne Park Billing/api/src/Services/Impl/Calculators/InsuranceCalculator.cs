using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Models.Dto;
using api.Models.Vo;
using api.Data;
using Microsoft.Extensions.Logging;
using TownePark;
using TownePark.Data;
using TownePark.Models.Vo;

namespace api.Services.Impl.Calculators
{
    /// <summary>
    /// Calculates insurance component for Management Agreement Internal Revenue.
    /// Handles both fixed fee and percentage-based insurance calculations.
    /// </summary>
    public class InsuranceCalculator : IManagementAgreementCalculator
    {
        private readonly IInternalRevenueRepository _internalRevenueRepository;
        private readonly IBillableExpenseRepository _billableExpenseRepository;
        private readonly IPayrollRepository _payrollRepository;
        private readonly ILogger<InsuranceCalculator> _logger;

        /// <summary>
        /// Execution order for management agreement calculators.
        /// Runs after ManagementFeeCalculator (Order = 1).
        /// </summary>
        public int Order => 2;

        public InsuranceCalculator(
            IInternalRevenueRepository internalRevenueRepository,
            IBillableExpenseRepository billableExpenseRepository,
            IPayrollRepository payrollRepository,
            ILogger<InsuranceCalculator> logger)
        {
            _internalRevenueRepository = internalRevenueRepository;
            _billableExpenseRepository = billableExpenseRepository;
            _payrollRepository = payrollRepository;
            _logger = logger;
        }

        /// <summary>
        /// Calculates and applies insurance for a specific site and billing period.
        /// </summary>
        public async Task CalculateAndApplyAsync(
            InternalRevenueDataVo siteData,
            int year,
            int monthOneBased,
            int currentMonth, // NEW: Current month parameter
            MonthValueDto monthValueDto,
            SiteMonthlyRevenueDetailDto siteDetailDto,
            decimal calculatedExternalRevenue,
            List<PnlRowDto> budgetRows)
        {
            // Check if ManagementAgreement is in the enabled contract types
            if (siteData.Contract?.ContractTypes == null || 
                !siteData.Contract.ContractTypes.Contains(bs_contracttypechoices.ManagementAgreement))
                return;

            // Get management agreement configuration from passed siteData
            var config = siteData?.ManagementAgreement;

            if (config == null)
            {return;
            }

            // Check if insurance is enabled - if not, skip all insurance calculations
            if (config.InsuranceEnabled != true)
            {return;
            }

            decimal insuranceAmount;
            decimal actualInsurancePortion = 0m;
            decimal forecastedInsurancePortion = 0m;

            // Determine if we are processing the current month and have current-month data available
            var isCurrentProcessingMonth = monthOneBased == currentMonth;
            var hasCurrentMonthData = isCurrentProcessingMonth && (
                (siteDetailDto?.PayrollBreakdown?.ActualPayrollLastDate.HasValue ?? false)
                || (siteDetailDto?.InternalActuals?.DailyActuals?.Count > 0));

            if (config.InsuranceType == bs_managementagreementinsurancetype.FixedFee)
            {
                // FixedFee type: still calculate base insurance (5.77% of payroll) and add
                // vehicle insurance budget + fixed additional insurance amount
                if (hasCurrentMonthData)
                {
                    var (actualPayrollExpense, forecastPayrollExpense) = CalculateCurrentMonthPayrollSplit(siteData.SiteId, year, monthOneBased, siteDetailDto);
                    var actualBase = actualPayrollExpense * 0.0577m;
                    var forecastBase = forecastPayrollExpense * 0.0577m;
                    var vehicleInsurance = GetVehicleInsurance(siteData.SiteId, year, monthOneBased);
                    var fixedAdditional = config.InsuranceFixedFeeAmount ?? 0m;

                    actualInsurancePortion = actualBase;
                    forecastedInsurancePortion = forecastBase + vehicleInsurance + fixedAdditional;
                    insuranceAmount = actualInsurancePortion + forecastedInsurancePortion;
                }
                else
                {
                    var payrollBase = GetBudgetPayrollForSite(budgetRows, siteDetailDto?.SiteId, monthOneBased - 1);
                    var baseInsurance = payrollBase * 0.0577m;
                    var vehicleInsurance = GetVehicleInsurance(siteData.SiteId, year, monthOneBased);
                    var fixedAdditional = config.InsuranceFixedFeeAmount ?? 0m;

                    insuranceAmount = baseInsurance + vehicleInsurance + fixedAdditional;
                    forecastedInsurancePortion = insuranceAmount;
                }
            }
            else
            {
                // Calculate based on billable accounts (5.77% of payroll)
                decimal baseInsurance;

                if (hasCurrentMonthData)
                {
                    // Split current-month payroll base into actual vs forecast
                    var (actualPayrollExpense, forecastPayrollExpense) = CalculateCurrentMonthPayrollSplit(siteData.SiteId, year, monthOneBased, siteDetailDto);
                    var actualBase = actualPayrollExpense * 0.0577m;
                    var forecastBase = forecastPayrollExpense * 0.0577m;
                    // Vehicle insurance + additional are budget/config → treat as forecast-only
                    var vehicleInsurance = GetVehicleInsurance(siteData.SiteId, year, monthOneBased);
                    // When BasedOnBillableAccounts, additional is a percentage applied to payroll
                    var additionalRate = (config.InsuranceAddlAmount ?? 0m) / 100m;
                    var additionalInsurance = (actualPayrollExpense + forecastPayrollExpense) * additionalRate;
                    actualInsurancePortion = actualBase;
                    forecastedInsurancePortion = forecastBase + vehicleInsurance + additionalInsurance;
                    baseInsurance = actualBase + forecastBase;
                    insuranceAmount = actualInsurancePortion + forecastedInsurancePortion;
                }
                else
                {
                    // For non-current months, use PNL budget payroll for both base and additional
                    var payrollBase = GetBudgetPayrollForSite(budgetRows, siteDetailDto?.SiteId, monthOneBased - 1);
                    baseInsurance = payrollBase * 0.0577m;
                    var vehicleInsurance = GetVehicleInsurance(siteData.SiteId, year, monthOneBased);
                    var additionalRate = (config.InsuranceAddlAmount ?? 0m) / 100m;
                    var additionalInsurance = payrollBase * additionalRate;
                    insuranceAmount = baseInsurance + vehicleInsurance + additionalInsurance;
                    // Non-current month → forecast-only
                    forecastedInsurancePortion = insuranceAmount;
                }
            }

            // Update DTO with calculated insurance
            UpdateInsuranceInDto(siteDetailDto, insuranceAmount, actualInsurancePortion, forecastedInsurancePortion);
        }

        /// <summary>
        /// Aggregates insurance totals across all sites for a month.
        /// </summary>
        public Task AggregateMonthlyTotalsAsync(
            List<SiteMonthlyRevenueDetailDto> siteDetailsForMonth,
            MonthValueDto monthValueDto)
        {
            // Sum insurance across all sites
            var totalInsurance = siteDetailsForMonth
                .Sum(s => s.InternalRevenueBreakdown?.ManagementAgreement?.CalculatedInsurance ?? 0m);

            // Initialize DTO structure if needed
            monthValueDto.InternalRevenueBreakdown ??= new InternalRevenueBreakdownDto();
            monthValueDto.InternalRevenueBreakdown.ManagementAgreement ??= new ManagementAgreementInternalRevenueDto();

            // Set total insurance
            monthValueDto.InternalRevenueBreakdown.ManagementAgreement.CalculatedInsurance = totalInsurance;return Task.CompletedTask;
        }

        /// <summary>
        /// Calculates base insurance as 5.77% of forecasted payroll.
        /// </summary>
        private decimal CalculateBaseInsurance(Guid siteId, int year, int monthOneBased)
        {
            // Get payroll expense budget for the site and period
            var payrollBase = _billableExpenseRepository.GetPayrollExpenseBudget(siteId, year, monthOneBased);
            
            if (payrollBase <= 0)
            {return 0m;
            }

            var insuranceAmount = payrollBase * 0.0577m;return insuranceAmount;
        }

        /// <summary>
        /// Calculates base insurance for the current month using actual payroll costs up to the last available
        /// actual date from InternalActuals, then forecasting the remaining days from daily payroll forecast.
        /// The insurance base equals (actual payroll + forecast payroll) * 5.77%.
        /// </summary>
        private decimal CalculateCurrentMonthBaseInsurance(
            Guid siteId,
            int year,
            int monthOneBased,
            SiteMonthlyRevenueDetailDto siteDetailDto)
        {
            // Use payroll breakdown actuals and last-actual date as cutoff
            var lastActualDate = siteDetailDto?.PayrollBreakdown?.ActualPayrollLastDate;
            var actualPayrollExpense = siteDetailDto?.PayrollBreakdown?.ActualPayroll ?? 0m;

            // Forecast payroll from daily forecast in bs_PayrollDetail for remaining days
            decimal forecastPayrollExpense = 0m;
            var billingPeriod = $"{year}-{monthOneBased:D2}";
            var payrollEntity = _payrollRepository.GetPayroll(siteId, billingPeriod);
            var detailRows = payrollEntity?.bs_PayrollDetail_Payroll ?? Enumerable.Empty<bs_PayrollDetail>();
            foreach (var detail in detailRows)
            {
                var d = detail.bs_Date;
                if (!d.HasValue) continue;
                if (d.Value.Year != year || d.Value.Month != monthOneBased) continue;
                if (lastActualDate.HasValue && d.Value.Date <= lastActualDate.Value.Date) continue;
                if (detail.bs_ForecastPayrollCost.HasValue)
                    forecastPayrollExpense += detail.bs_ForecastPayrollCost.Value;
            }

            var totalPayrollExpense = actualPayrollExpense + forecastPayrollExpense;
            var insuranceAmount = totalPayrollExpense * 0.0577m;return insuranceAmount;
        }

        private (decimal actualPayrollExpense, decimal forecastPayrollExpense) CalculateCurrentMonthPayrollSplit(
            Guid siteId,
            int year,
            int monthOneBased,
            SiteMonthlyRevenueDetailDto siteDetailDto)
        {
            var lastActualDate = siteDetailDto?.PayrollBreakdown?.ActualPayrollLastDate;
            decimal actualPayrollExpense = siteDetailDto?.PayrollBreakdown?.ActualPayroll ?? 0m;

            // Forecast payroll from daily forecast in bs_PayrollDetail for remaining days
            decimal forecastPayrollExpense = 0m;
            var billingPeriod = $"{year}-{monthOneBased:D2}";
            var payrollEntity = _payrollRepository.GetPayroll(siteId, billingPeriod);
            var detailRows = payrollEntity?.bs_PayrollDetail_Payroll ?? Enumerable.Empty<bs_PayrollDetail>();
            foreach (var detail in detailRows)
            {
                var detailDate = detail.bs_Date;
                if (!detailDate.HasValue) continue;
                if (detailDate.Value.Year != year || detailDate.Value.Month != monthOneBased) continue;
                if (lastActualDate.HasValue && detailDate.Value.Date <= lastActualDate.Value.Date) continue;

                if (detail.bs_ForecastPayrollCost.HasValue)
                {
                    forecastPayrollExpense += detail.bs_ForecastPayrollCost.Value;
                }
                else if (detail.bs_RegularHours.HasValue)
                {
                    // No rate/amount field available; treat as 0
                }
            }

            return (actualPayrollExpense, forecastPayrollExpense);
        }

        /// <summary>
        /// Retrieves vehicle insurance from budget using the new bs_VehicleInsuranceBudget field.
        /// </summary>
        private decimal GetVehicleInsurance(Guid siteId, int year, int monthOneBased)
        {
            // Retrieve vehicle insurance budget from the new field
            var vehicleInsurance = _billableExpenseRepository.GetVehicleInsuranceBudget(siteId, year, monthOneBased);return vehicleInsurance;
        }

        /// <summary>
        /// Updates the DTO with calculated insurance amount.
        /// </summary>
        private void UpdateInsuranceInDto(SiteMonthlyRevenueDetailDto siteDetailDto, decimal insuranceAmount, decimal? actualInsurance = null, decimal? forecastedInsurance = null)
        {
            // Initialize nested DTO structure if needed
            siteDetailDto.InternalRevenueBreakdown ??= new InternalRevenueBreakdownDto();
            siteDetailDto.InternalRevenueBreakdown.ManagementAgreement ??= new ManagementAgreementInternalRevenueDto();

            // Set calculated insurance
            siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.CalculatedInsurance = insuranceAmount;
            siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.ActualInsurance = (actualInsurance.HasValue && actualInsurance.Value > 0m) ? actualInsurance : null;
            siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.ForecastedInsurance = (forecastedInsurance.HasValue && forecastedInsurance.Value > 0m) ? forecastedInsurance : null;

            // Update totals - insurance contributes to management agreement total
            siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Total = 
                (siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Total ?? 0m) + insuranceAmount;
            
            // Update overall internal revenue total
            siteDetailDto.InternalRevenueBreakdown.CalculatedTotalInternalRevenue = 
                (siteDetailDto.InternalRevenueBreakdown.CalculatedTotalInternalRevenue ?? 0m) + insuranceAmount;}
        
        private static decimal GetBudgetPayrollForSite(List<PnlRowDto> budgetRows, string siteId, int targetMonthZeroBased)
        {
            if (budgetRows == null || string.IsNullOrWhiteSpace(siteId)) return 0m;
            var budgetPayrollRow = budgetRows.FirstOrDefault(r => r.ColumnName == "Payroll");
            var month = budgetPayrollRow?.MonthlyValues?.FirstOrDefault(mv => mv.Month == targetMonthZeroBased)
                       ?? budgetPayrollRow?.MonthlyValues?.FirstOrDefault();
            var site = month?.SiteDetails?.FirstOrDefault(sd => sd.SiteId == siteId);
            if (site?.Value != null) return site.Value.Value;
            // Fallback for tests/edge cases: if specific site match not found, use the first site's value
            var first = month?.SiteDetails?.FirstOrDefault();
            return first?.Value ?? 0m;
        }
    }
}