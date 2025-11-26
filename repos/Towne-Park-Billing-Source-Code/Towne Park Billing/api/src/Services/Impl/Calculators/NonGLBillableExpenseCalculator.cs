using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TownePark.Models.Vo;
using api.Models.Dto;
using TownePark;
using api.Data;

namespace api.Services.Impl.Calculators
{
    public class NonGLBillableExpenseCalculator : IManagementAgreementCalculator
    {
        private readonly IPayrollRepository? _payrollRepository;

        public NonGLBillableExpenseCalculator()
        {
        }

        public NonGLBillableExpenseCalculator(IPayrollRepository payrollRepository)
        {
            _payrollRepository = payrollRepository;
        }
        /// <summary>
        /// Execution order for this calculator (lower numbers execute first)
        /// </summary>
        public int Order => 10; // Execute after other management agreement calculators if any

        /// <summary>
        /// Calculate and apply Non-GL Billable Expenses for a specific site and month
        /// </summary>
        public async Task CalculateAndApplyAsync(
            InternalRevenueDataVo siteData,
            int year,
            int monthOneBased,
            int currentMonth, 
            MonthValueDto monthValueDto,
            SiteMonthlyRevenueDetailDto siteDetailDto,
            decimal calculatedExternalRevenue,
            List<PnlRowDto> budgetRows)
        {
            // Skip if site data is null or contract is null
            if (siteData == null || siteData.Contract == null)
                return;

            // Skip if not a Management Agreement
            //i f contract type does nto include "ManagementAgreement", skip this calculator
            if (siteData.Contract.ContractTypes == null || !siteData.Contract.ContractTypes.Contains(bs_contracttypechoices.ManagementAgreement))
                return;

            // Calculate Non-GL Billable Expenses (current-month aware)
            decimal totalNonGLExpenses = CalculateNonGLBillableExpenses(siteData, siteDetailDto, year, monthOneBased, calculatedExternalRevenue, budgetRows);

            // Initialize ManagementAgreement if needed
            if (siteDetailDto.InternalRevenueBreakdown == null)
                siteDetailDto.InternalRevenueBreakdown = new InternalRevenueBreakdownDto();

            if (siteDetailDto.InternalRevenueBreakdown.ManagementAgreement == null)
                siteDetailDto.InternalRevenueBreakdown.ManagementAgreement = new ManagementAgreementInternalRevenueDto
                {
                    Components = new List<ManagementAgreementComponentDto>(),
                    Escalators = new List<EscalatorDto>(),
                    Total = 0m
                };

            // Add Non-GL Billable Expenses component only when non-zero
            if (totalNonGLExpenses != 0m)
            {
                var existing = siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Components
                    .FirstOrDefault(c => c.Name == "Non-GL Billable Expenses");
                if (existing == null)
                {
                    siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Components.Add(new ManagementAgreementComponentDto
                    {
                        Name = "Non-GL Billable Expenses",
                        Value = totalNonGLExpenses
                    });
                }
                else
                {
                    existing.Value = totalNonGLExpenses;
                }
            }

            // Update total
            decimal total = siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Components.Sum(c => c.Value ?? 0m);
            siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Total = total;

            // Update CalculatedTotalInternalRevenue
            var breakdown = siteDetailDto.InternalRevenueBreakdown;
            decimal totalInternalRevenue = 0m;
            if (breakdown.FixedFee?.Total != null) totalInternalRevenue += breakdown.FixedFee.Total.Value;
            if (breakdown.PerOccupiedRoom?.Total != null) totalInternalRevenue += breakdown.PerOccupiedRoom.Total.Value;
            if (breakdown.RevenueShare?.Total != null) totalInternalRevenue += breakdown.RevenueShare.Total.Value;
            if (breakdown.BillableAccounts?.Total != null) totalInternalRevenue += breakdown.BillableAccounts.Total.Value;
            if (breakdown.ManagementAgreement?.Total != null) totalInternalRevenue += breakdown.ManagementAgreement.Total.Value;
            if (breakdown.OtherRevenue?.Total != null) totalInternalRevenue += breakdown.OtherRevenue.Total.Value;
            breakdown.CalculatedTotalInternalRevenue = totalInternalRevenue;

            await Task.CompletedTask;
        }

        /// <summary>
        /// Aggregate monthly totals across all sites for this calculator
        /// </summary>
        public async Task AggregateMonthlyTotalsAsync(
            List<SiteMonthlyRevenueDetailDto> siteDetailsForMonth,
            MonthValueDto monthValueDto)
        {
            decimal totalNonGLExpensesForMonth = 0m;
            List<ManagementAgreementComponentDto> aggregatedComponents = new List<ManagementAgreementComponentDto>();

            foreach (var siteDetail in siteDetailsForMonth)
            {
                if (siteDetail.InternalRevenueBreakdown?.ManagementAgreement != null)
                {
                    var managementAgreement = siteDetail.InternalRevenueBreakdown.ManagementAgreement;
                    var nonGLExpenseComponent = managementAgreement.Components?.FirstOrDefault(c => c.Name == "Non-GL Billable Expenses");
                    
                    if (nonGLExpenseComponent != null && nonGLExpenseComponent.Value.HasValue)
                    {
                        totalNonGLExpensesForMonth += nonGLExpenseComponent.Value.Value;
                        aggregatedComponents.Add(nonGLExpenseComponent);
                    }
                }
            }

            if (monthValueDto.InternalRevenueBreakdown == null)
                monthValueDto.InternalRevenueBreakdown = new InternalRevenueBreakdownDto();

            if (monthValueDto.InternalRevenueBreakdown.ManagementAgreement == null)
                monthValueDto.InternalRevenueBreakdown.ManagementAgreement = new ManagementAgreementInternalRevenueDto
                {
                    Components = new List<ManagementAgreementComponentDto>(),
                    Escalators = new List<EscalatorDto>(),
                    Total = 0m
                };

            // Add or update Non-GL Billable Expenses component
            if (totalNonGLExpensesForMonth != 0m)
            {
                var existingComponent = monthValueDto.InternalRevenueBreakdown.ManagementAgreement.Components
                    .FirstOrDefault(c => c.Name == "Non-GL Billable Expenses");
                if (existingComponent != null)
                {
                    existingComponent.Value = totalNonGLExpensesForMonth;
                }
                else
                {
                    monthValueDto.InternalRevenueBreakdown.ManagementAgreement.Components.Add(new ManagementAgreementComponentDto
                    {
                        Name = "Non-GL Billable Expenses",
                        Value = totalNonGLExpensesForMonth
                    });
                }
            }

            // Update total (this will be overridden by PnlService.AggregateMonthlyTotals)
            decimal total = monthValueDto.InternalRevenueBreakdown.ManagementAgreement.Components.Sum(c => c.Value ?? 0m);
            monthValueDto.InternalRevenueBreakdown.ManagementAgreement.Total = total;

            await Task.CompletedTask;
        }

        /// <summary>
        /// Calculate Non-GL Billable Expenses for a site and month
        /// </summary>
        private decimal CalculateNonGLBillableExpenses(
            InternalRevenueDataVo siteData,
            SiteMonthlyRevenueDetailDto siteDetailDto,
            int year,
            int monthOneBased,
            decimal calculatedExternalRevenue,
            List<PnlRowDto> budgetRows)
        {
            DateTime forecastPeriod = new DateTime(year, monthOneBased, 1);
            decimal nonGLTotal = 0m;

            // Get Non-GL expense items from site data
             var nonGLItems = siteData.OtherExpenses;
            if (nonGLItems == null || !nonGLItems.Any())
                return 0m;

            // For percentage-of-payroll, prefer forecasted payroll for the target month.
            // If PayrollType == "Total", add PTEB total for the site/month from the already-computed breakdown
            decimal GetSiteBudgetValue(string columnName)
            {
                if (budgetRows == null) return 0m;
                var row = budgetRows.FirstOrDefault(r => string.Equals(r.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));
                if (row?.MonthlyValues == null) return 0m;
                var monthZeroBased = monthOneBased - 1;
                var mv = row.MonthlyValues.FirstOrDefault(m => m.Month == monthZeroBased);
                if (mv?.SiteDetails == null) return 0m;
                var site = mv.SiteDetails.FirstOrDefault(sd => sd.SiteId == siteData.SiteNumber);
                return site?.Value ?? 0m;
            }

            // Helper: get forecasted payroll for target month from forecast rows passed earlier into PnL (preferred base)
            decimal GetSiteForecastValue(string columnName)
            {
                if (budgetRows == null) return 0m; // forecast access limited; fallback to budget
                // We only have budgetRows here; when forecastRows are not available, fallback to budget
                return GetSiteBudgetValue(columnName);
            }

            // Resolve current-month cutoff (if any)
            DateTime monthStart = new DateTime(year, monthOneBased, 1);
            DateTime monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var today = DateTime.Today;
            bool isCurrentMonth = (year == today.Year && monthOneBased == today.Month);
            DateTime? cutoff = siteDetailDto?.PayrollBreakdown?.ActualPayrollLastDate;

            // Process each Non-GL expense item
            foreach (var item in nonGLItems)
            {
                // If a specific period/month is provided, require it to match the target year/month
                if (item.Period.HasValue)
                {
                    var p = item.Period.Value;
                    if (p.Year != year || p.Month != monthOneBased)
                        continue;
                }

                // Apply active flag and date window filters
                // Include if active and (start <= month <= end or no end)
                if (item.IsActive.HasValue && !item.IsActive.Value)
                    continue;
                // We only have an end date from Dataverse; treat no end as open-ended.
                if (item.EndDate.HasValue && item.EndDate.Value.Date < monthStart)
                    continue;

                decimal itemAmount = 0m;

                // Calculate amount based on expense type
                if (item.ExpenseType == "FixedAmount")
                {
                    // Fixed amount
                    itemAmount = item.Amount;
                }
                else if (item.ExpenseType == "Payroll")
                {
                    // Percentage of payroll: for current month use actual-to-date + forecast remainder;
                    // for non-current months use forecasted payroll (fallback to budget when forecast unavailable)
                    decimal ResolvePayrollBase()
                    {
                        if (!isCurrentMonth)
                        {
                            decimal forecastPayrollNonCurrent = GetSiteForecastValue("Payroll");
                            if (string.Equals(item.PayrollType, "Total", StringComparison.OrdinalIgnoreCase))
                            {
                                decimal ptebTotalNonCurrent = siteDetailDto?.InternalRevenueBreakdown?.BillableAccounts?.Pteb?.Total ?? 0m;
                                return forecastPayrollNonCurrent + ptebTotalNonCurrent;
                            }
                            return forecastPayrollNonCurrent;
                        }

                        // If no cutoff/actuals available in current month, treat actuals as 0 and forecast from daily forecast only
                        if (cutoff == null)
                        {
                            // No actuals in current month: just sum daily forecast for entire month
                            decimal forecastOnly = 0m;
                            if (_payrollRepository != null)
                            {
                                var billingPeriodAll = $"{year}-{monthOneBased:D2}";
                                var payrollAll = _payrollRepository.GetPayroll(siteData.SiteId, billingPeriodAll);
                                var rowsAll = payrollAll?.bs_PayrollDetail_Payroll ?? Enumerable.Empty<bs_PayrollDetail>();
                                foreach (var detail in rowsAll)
                                {
                                    var d = detail.bs_Date;
                                    if (!d.HasValue) continue;
                                    if (d.Value.Year != year || d.Value.Month != monthOneBased) continue;
                                    if (detail.bs_ForecastPayrollCost.HasValue)
                                        forecastOnly += detail.bs_ForecastPayrollCost.Value;
                                }
                            }

                            // If Total payroll requested, add PTEB total from breakdown
                            if (string.Equals(item.PayrollType, "Total", StringComparison.OrdinalIgnoreCase))
                            {
                                decimal ptebTotal = siteDetailDto?.InternalRevenueBreakdown?.BillableAccounts?.Pteb?.Total ?? 0m;
                                forecastOnly += ptebTotal;
                            }

                            return forecastOnly;
                        }

                        // Actual payroll up to cutoff (Resolved Payroll) from PayrollBreakdown
                        decimal actualPayroll = siteDetailDto?.PayrollBreakdown?.ActualPayroll ?? 0m;

                        // Sum forecast payroll for days after cutoff to month end using repository daily forecast only
                        decimal forecastPayroll = 0m;
                        bool couldForecast = false;

                        if (_payrollRepository != null)
                        {
                            var billingPeriod = $"{year}-{monthOneBased:D2}";
                            var payrollEntity = _payrollRepository.GetPayroll(siteData.SiteId, billingPeriod);
                            var detailRows = payrollEntity?.bs_PayrollDetail_Payroll ?? Enumerable.Empty<bs_PayrollDetail>();
                            foreach (var detail in detailRows)
                            {
                                var detailDate = detail.bs_Date;
                                if (!detailDate.HasValue) continue;
                                if (detailDate.Value.Year != year || detailDate.Value.Month != monthOneBased) continue;
                                if (detailDate.Value.Date <= cutoff.Value.Date) continue;

                                if (detail.bs_ForecastPayrollCost.HasValue)
                                {
                                    forecastPayroll += detail.bs_ForecastPayrollCost.Value;
                                }
                            }
                            couldForecast = forecastPayroll > 0m;
                        }

                        // No budget proration fallback in current month: if no forecast rows, remainder contributes 0

                        var baseTotal = actualPayroll + (couldForecast ? forecastPayroll : 0m);

                        // If Total payroll requested, add PTEB total from breakdown when available
                        if (string.Equals(item.PayrollType, "Total", StringComparison.OrdinalIgnoreCase))
                        {
                            decimal ptebTotal = siteDetailDto?.InternalRevenueBreakdown?.BillableAccounts?.Pteb?.Total ?? 0m;
                            baseTotal += ptebTotal;
                        }

                        return baseTotal;
                    }

                    decimal payrollBase = ResolvePayrollBase();
                    itemAmount = payrollBase * (item.Amount / 100m);
                }
                else if (item.ExpenseType == "Revenue")
                {
                    // Percentage of revenue
                    itemAmount = calculatedExternalRevenue * (item.Amount / 100m);
                }

                // Add to total
                nonGLTotal += itemAmount;
            }

            return nonGLTotal;
        }
    }
}
