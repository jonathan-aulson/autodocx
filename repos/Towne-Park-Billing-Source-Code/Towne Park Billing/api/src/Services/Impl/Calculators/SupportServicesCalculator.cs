using System;
using System.Collections.Generic;
using System.Linq;
using TownePark.Models.Vo;
using api.Models.Dto;
using TownePark;
using api.Data;

namespace api.Services.Impl.Calculators
{
    /// <summary>
    /// Calculator for Support Services within Billable Accounts
    /// Supports both fixed amount and percentage-based calculations (billable or total payroll)
    /// </summary>
    public class SupportServicesCalculator : IInternalRevenueCalculator
    {
        private readonly IPayrollRepository _payrollRepository;

        public SupportServicesCalculator(IPayrollRepository payrollRepository)
        {
            _payrollRepository = payrollRepository;
        }

        public void CalculateAndApply(
            InternalRevenueDataVo siteData,
            int year,
            int monthOneBased,
            int currentMonth, // NEW: Current month parameter
            MonthValueDto monthValueDto,
            SiteMonthlyRevenueDetailDto siteDetailDto,
            decimal calculatedExternalRevenue,
            List<PnlRowDto> budgetRows)
        {
            var today = DateTime.Today;
            var isCurrentMonth = (year == today.Year && monthOneBased == today.Month);
            
            SupportServicesInternalRevenueDto supportServicesDto;
            
            if (isCurrentMonth)
            {
                // Use actuals up to max available date, forecast for rest of month
                supportServicesDto = CalculateCurrentMonthSupportServices(siteData, year, monthOneBased, siteDetailDto, budgetRows);
            }
            else
            {
                // Use existing forecast logic for non-current months
                supportServicesDto = CalculateMonthlySupportServicesForSite(siteData, year, monthOneBased, siteDetailDto, budgetRows);
            }
            
            if (siteDetailDto.InternalRevenueBreakdown == null)
            {
                siteDetailDto.InternalRevenueBreakdown = new InternalRevenueBreakdownDto();
            }
            
            if (siteDetailDto.InternalRevenueBreakdown.BillableAccounts == null)
            {
                siteDetailDto.InternalRevenueBreakdown.BillableAccounts = new BillableAccountsInternalRevenueDto();
            }
            
            // Set Support Services breakdown
            siteDetailDto.InternalRevenueBreakdown.BillableAccounts.SupportServices = supportServicesDto;
            
            // Add Support Services to existing BillableAccounts total
            decimal supportServicesAmount = supportServicesDto?.Total ?? 0m;
            if (siteDetailDto.InternalRevenueBreakdown.BillableAccounts.Total.HasValue)
            {
                siteDetailDto.InternalRevenueBreakdown.BillableAccounts.Total += supportServicesAmount;
            }
            else
            {
                siteDetailDto.InternalRevenueBreakdown.BillableAccounts.Total = supportServicesAmount;
            }

            // Update the overall calculated total internal revenue
            UpdateCalculatedTotalInternalRevenue(siteDetailDto.InternalRevenueBreakdown);
        }

        public void AggregateMonthlyTotals(List<SiteMonthlyRevenueDetailDto> siteDetailsForMonth, MonthValueDto monthValueDto)
        {
            decimal totalSupportServicesForMonth = 0m;

            foreach (var siteDetail in siteDetailsForMonth)
            {
                if (siteDetail.InternalRevenueBreakdown?.BillableAccounts?.SupportServices?.Total != null)
                {
                    totalSupportServicesForMonth += siteDetail.InternalRevenueBreakdown.BillableAccounts.SupportServices.Total.Value;
                }
            }

            if (monthValueDto.InternalRevenueBreakdown == null)
            {
                monthValueDto.InternalRevenueBreakdown = new InternalRevenueBreakdownDto();
            }

            if (monthValueDto.InternalRevenueBreakdown.BillableAccounts == null)
            {
                monthValueDto.InternalRevenueBreakdown.BillableAccounts = new BillableAccountsInternalRevenueDto();
            }

            if (monthValueDto.InternalRevenueBreakdown.BillableAccounts.SupportServices == null)
            {
                monthValueDto.InternalRevenueBreakdown.BillableAccounts.SupportServices = new SupportServicesInternalRevenueDto();
            }

            // Set aggregated Support Services total
            if (monthValueDto.InternalRevenueBreakdown.BillableAccounts.SupportServices.Total.HasValue)
            {
                monthValueDto.InternalRevenueBreakdown.BillableAccounts.SupportServices.Total += totalSupportServicesForMonth;
            }
            else
            {
                monthValueDto.InternalRevenueBreakdown.BillableAccounts.SupportServices.Total = totalSupportServicesForMonth;
            }
            
            // Add Support Services to existing BillableAccounts total at month level
            if (monthValueDto.InternalRevenueBreakdown.BillableAccounts.Total.HasValue)
            {
                monthValueDto.InternalRevenueBreakdown.BillableAccounts.Total += totalSupportServicesForMonth;
            }
            else
            {
                monthValueDto.InternalRevenueBreakdown.BillableAccounts.Total = totalSupportServicesForMonth;
            }
            
            // Update calculated total internal revenue at month level
            UpdateCalculatedTotalInternalRevenue(monthValueDto.InternalRevenueBreakdown);
        }

        private SupportServicesInternalRevenueDto CalculateMonthlySupportServicesForSite(
            InternalRevenueDataVo siteData,
            int year,
            int monthOneBased,
            SiteMonthlyRevenueDetailDto siteDetailDto,
            List<PnlRowDto> budgetRows)
        {
            // Step 1: Check if contract type includes BillingAccount (feature flag guard)
            if (!IsContractTypeBillingAccount(siteData))
            {
                return null; // Skip Support Services calculation
            }

            // Step 2: Get Support Services configuration
            var config = GetSupportServicesConfiguration(siteData);
            if (config == null || config.PayrollSupportEnabled != true)
            {
                return null; // Skip if Support Services not enabled
            }

            decimal supportServicesAmount = 0m;

            // Step 3: Calculate based on billing type
            if (config.PayrollSupportBillingType == "Fixed")
            {
                // Fixed amount
                supportServicesAmount = config.PayrollSupportAmount ?? 0m;
            }
            else if (config.PayrollSupportBillingType == "Percentage")
            {
                // Percentage of payroll - distinguish between TOTAL and BILLABLE
                var payrollTotal = GetPayrollTotalBasedOnType(siteData, year, monthOneBased, config.PayrollSupportPayrollType, siteDetailDto, budgetRows);
                
                if (payrollTotal > 0 && config.PayrollSupportAmount.HasValue)
                {
                    // Percentage stored as whole number (e.g., 10 for 10%)
                    supportServicesAmount = payrollTotal * (config.PayrollSupportAmount.Value / 100m);
                }
            }

            return new SupportServicesInternalRevenueDto
            {
                Total = supportServicesAmount
            };
        }

        private bool IsContractTypeBillingAccount(InternalRevenueDataVo siteData)
        {
            return siteData.Contract?.ContractTypes?.Contains(bs_contracttypechoices.BillingAccount) == true;
        }

        private BillableAccountConfigVo GetSupportServicesConfiguration(InternalRevenueDataVo siteData)
        {
            // Get the first billable account configuration (same pattern as PTEB)
            return siteData.Contract?.BillableAccountsData?.FirstOrDefault();
        }

        private decimal GetPayrollTotalBasedOnType(
            InternalRevenueDataVo siteData,
            int year,
            int monthOneBased,
            string payrollType,
            SiteMonthlyRevenueDetailDto siteDetailDto,
            List<PnlRowDto> budgetRows)
        {
            // Get billable payroll monthly budget from provided budgetRows
            var billablePayroll = GetSiteBudgetValue(budgetRows, "Payroll", siteData.SiteNumber, monthOneBased);
            
            if (payrollType == "Total")
            {
                // 'TOTAL' = billable expense table payroll expense budget column + PTEB value
                var ptebAmount = siteDetailDto?.InternalRevenueBreakdown?.BillableAccounts?.Pteb?.Total ?? 0m;
                return billablePayroll + ptebAmount;
            }
            else // "Billable" or any other value defaults to billable only
            {
                // 'BILLABLE' = billable expense table payroll expense budget column only
                return billablePayroll;
            }
        }

        // Note: No repository accessors are used in current-month path.

        private void UpdateCalculatedTotalInternalRevenue(InternalRevenueBreakdownDto breakdown)
        {
            decimal total = 0m;
            if (breakdown.FixedFee?.Total != null) total += breakdown.FixedFee.Total.Value;
            if (breakdown.PerOccupiedRoom?.Total != null) total += breakdown.PerOccupiedRoom.Total.Value;
            if (breakdown.RevenueShare?.Total != null) total += breakdown.RevenueShare.Total.Value;
            if (breakdown.BillableAccounts?.Total != null) total += breakdown.BillableAccounts.Total.Value;
            if (breakdown.ManagementAgreement?.Total != null) total += breakdown.ManagementAgreement.Total.Value;
            if (breakdown.OtherRevenue?.Total != null) total += breakdown.OtherRevenue.Total.Value;
            breakdown.CalculatedTotalInternalRevenue = total;
        }

        private SupportServicesInternalRevenueDto CalculateCurrentMonthSupportServices(
            InternalRevenueDataVo siteData,
            int year,
            int monthOneBased,
            SiteMonthlyRevenueDetailDto siteDetailDto,
            List<PnlRowDto> budgetRows)
        {
            // Current-month strict mode:
            // - Actuals come from siteDetailDto.PayrollBreakdown.ActualPayroll (Resolved Payroll actual-to-date)
            // - Forecast comes from bs_PayrollDetail for remaining days after ActualPayrollLastDate
            // - PTEB is 100% forecast (never counted as actual)
            // Step 1: Check if contract type includes BillingAccount (feature flag guard)
            if (!IsContractTypeBillingAccount(siteData))
            {
                return null; // Skip Support Services calculation
            }

            // Step 2: Get Support Services configuration
            var config = GetSupportServicesConfiguration(siteData);
            if (config == null || config.PayrollSupportEnabled != true)
            {
                return null; // Skip if Support Services not enabled
            }// Step 3: Calculate based on billing type
            if (config.PayrollSupportBillingType == "Fixed")
            {
                var fixedAmount = config.PayrollSupportAmount ?? 0m;return new SupportServicesInternalRevenueDto
                {
                    Total = fixedAmount,
                    ActualSupportServices = null,
                    ForecastedSupportServices = fixedAmount,
                    // No daily actuals when fixed; set to last day of previous month to indicate calculation ran
                    LastActualDate = new DateTime(year, monthOneBased, 1).AddDays(-1)
                };
            }

            // Percentage of payroll - use daily actuals from PnlService (InternalActuals) + forecast remainder
            var percent = (config.PayrollSupportAmount ?? 0m) / 100m; // whole number percentage to decimal
            var payrollType = config.PayrollSupportPayrollType;

            var maxActualDate = siteDetailDto?.PayrollBreakdown?.ActualPayrollLastDate
                ?? new DateTime(year, monthOneBased, 1).AddDays(-1);
            decimal actualPayrollExpense = siteDetailDto?.PayrollBreakdown?.ActualPayroll ?? 0m;

            // Forecast payroll from daily forecast in bs_PayrollDetail for remaining days
            decimal forecastPayrollExpense = 0m;
            var billingPeriod = $"{year}-{monthOneBased:D2}";
            var payrollEntity = _payrollRepository.GetPayroll(siteData.SiteId, billingPeriod);
            var detailRows = payrollEntity?.bs_PayrollDetail_Payroll ?? Enumerable.Empty<bs_PayrollDetail>();
            foreach (var detail in detailRows)
            {
                var detailDate = detail.bs_Date;
                if (!detailDate.HasValue) continue;
                if (detailDate.Value.Year != year || detailDate.Value.Month != monthOneBased) continue;
                if (detailDate.Value.Date <= maxActualDate.Date) continue;

                decimal dailyCost = 0m;
                if (detail.bs_ForecastPayrollCost.HasValue)
                {
                    dailyCost = detail.bs_ForecastPayrollCost.Value;
                }
                else if (detail.bs_RegularHours.HasValue)
                {
                    // No rate/amount field available; treat as 0 and log for QA visibility
                    Console.WriteLine($"SupportServicesCalculator: Missing forecast cost for {detailDate.Value:yyyy-MM-dd}; treating as 0.");
                    dailyCost = 0m;
                }
                else
                {
                    Console.WriteLine($"SupportServicesCalculator: Missing daily forecast fields for {detailDate.Value:yyyy-MM-dd}; treating as 0.");
                    dailyCost = 0m;
                }

                forecastPayrollExpense += dailyCost;
            }

            // PTEB handling: 100% forecast if payroll type is Total
            if (string.Equals(payrollType, "Total", StringComparison.OrdinalIgnoreCase))
            {
                var ptebAmount = siteDetailDto?.InternalRevenueBreakdown?.BillableAccounts?.Pteb?.Total ?? 0m;
                forecastPayrollExpense += ptebAmount;
            }

            var actualSupportServices = actualPayrollExpense * percent;
            var forecastSupportServices = forecastPayrollExpense * percent;
            var totalSupportServices = actualSupportServices + forecastSupportServices;return new SupportServicesInternalRevenueDto
            {
                Total = totalSupportServices,
                ActualSupportServices = actualSupportServices > 0 ? actualSupportServices : null,
                ForecastedSupportServices = forecastSupportServices > 0 ? forecastSupportServices : null,
                LastActualDate = maxActualDate != DateTime.MinValue ? maxActualDate : null
            };
        }

        private static decimal GetSiteBudgetValue(List<PnlRowDto> budgetRows, string columnName, string siteNumber, int monthOneBased)
        {
            if (budgetRows == null) return 0m;
            var row = budgetRows.FirstOrDefault(r => string.Equals(r.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));
            if (row?.MonthlyValues == null) return 0m;
            var monthZeroBased = monthOneBased - 1;
            var mv = row.MonthlyValues.FirstOrDefault(m => m.Month == monthZeroBased);
            if (mv?.SiteDetails == null) return 0m;
            var site = mv.SiteDetails.FirstOrDefault(sd => sd.SiteId == siteNumber);
            return site?.Value ?? 0m;
        }
    }
} 