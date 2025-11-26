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
    /// Calculator for PTEB (Payroll Taxes and Employee Benefits) within Billable Accounts
    /// Supports both percentage-based and actual PTEB calculations
    /// Escalators only apply to percentage-based calculations
    /// </summary>
    public class BillablePtebCalculator : IInternalRevenueCalculator
    {
        private readonly IBillableExpenseRepository _billableExpenseRepository;

        public BillablePtebCalculator(IBillableExpenseRepository billableExpenseRepository)
        {
            _billableExpenseRepository = billableExpenseRepository;
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
            
            PtebInternalRevenueDto ptebDto;
            
            if (isCurrentMonth)
            {
                // Use actuals up to max available date, forecast for rest of month
                ptebDto = CalculateCurrentMonthPtebForSite(siteData, year, monthOneBased, siteDetailDto, budgetRows);
            }
            else
            {
                // Use existing forecast logic for non-current months
                ptebDto = CalculateMonthlyPtebForSite(siteData, year, monthOneBased, budgetRows);
            }
            
            if (siteDetailDto.InternalRevenueBreakdown == null)
            {
                siteDetailDto.InternalRevenueBreakdown = new InternalRevenueBreakdownDto();
            }
            
            if (siteDetailDto.InternalRevenueBreakdown.BillableAccounts == null)
            {
                siteDetailDto.InternalRevenueBreakdown.BillableAccounts = new BillableAccountsInternalRevenueDto();
            }
            
            // Set PTEB breakdown
            siteDetailDto.InternalRevenueBreakdown.BillableAccounts.Pteb = ptebDto;
            
            // Add PTEB to existing total
            if (siteDetailDto.InternalRevenueBreakdown.BillableAccounts.Total.HasValue)
            {
                siteDetailDto.InternalRevenueBreakdown.BillableAccounts.Total += ptebDto?.Total ?? 0m;
            }
            else
            {
                siteDetailDto.InternalRevenueBreakdown.BillableAccounts.Total = ptebDto?.Total ?? 0m;
            }

            // Update the overall calculated total internal revenue
            UpdateCalculatedTotalInternalRevenue(siteDetailDto.InternalRevenueBreakdown);
        }

        public void AggregateMonthlyTotals(List<SiteMonthlyRevenueDetailDto> siteDetailsForMonth, MonthValueDto monthValueDto)
        {
            decimal totalPtebForMonth = 0m;

            foreach (var siteDetail in siteDetailsForMonth)
            {
                if (siteDetail.InternalRevenueBreakdown?.BillableAccounts?.Pteb?.Total != null)
                {
                    totalPtebForMonth += siteDetail.InternalRevenueBreakdown.BillableAccounts.Pteb.Total.Value;
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

            if (monthValueDto.InternalRevenueBreakdown.BillableAccounts.Pteb == null)
            {
                monthValueDto.InternalRevenueBreakdown.BillableAccounts.Pteb = new PtebInternalRevenueDto();
            }

            // Add to existing PTEB total rather than setting it
            if (monthValueDto.InternalRevenueBreakdown.BillableAccounts.Pteb.Total.HasValue)
            {
                monthValueDto.InternalRevenueBreakdown.BillableAccounts.Pteb.Total += totalPtebForMonth;
            }
            else
            {
                monthValueDto.InternalRevenueBreakdown.BillableAccounts.Pteb.Total = totalPtebForMonth;
            }
            
            // Add PTEB to existing BillableAccounts total at month level
            if (monthValueDto.InternalRevenueBreakdown.BillableAccounts.Total.HasValue)
            {
                monthValueDto.InternalRevenueBreakdown.BillableAccounts.Total += totalPtebForMonth;
            }
            else
            {
                monthValueDto.InternalRevenueBreakdown.BillableAccounts.Total = totalPtebForMonth;
            }
            
            // Update calculated total internal revenue at month level
            UpdateCalculatedTotalInternalRevenue(monthValueDto.InternalRevenueBreakdown);
        }

        private PtebInternalRevenueDto CalculateMonthlyPtebForSite(
            InternalRevenueDataVo siteData,
            int year,
            int monthOneBased,
            List<PnlRowDto> budgetRows)
        {
            // Step 1: Check if contract type includes BillingAccount
            if (!IsContractTypeBillingAccount(siteData))
            {
                return null; // Skip PTEB calculation
            }

            // Step 2: Get PTEB configuration
            var config = GetPtebConfiguration(siteData);
            if (config == null || config.PayrollTaxesEnabled != true)
            {
                return null; // Skip if PTEB not enabled
            }

            decimal ptebAmount = 0m;
            string calculationType = config.PayrollTaxesBillingType;
            decimal? baseAmount = null;
            decimal? appliedPercentage = null;

            // Step 3: Calculate based on billing type
            if (config.PayrollTaxesBillingType == "Percentage")
            {
                // Percentage of included payroll
                var includedPayrollTotal = GetIncludedPayrollTotalFromDataverse(siteData, year, monthOneBased);
                baseAmount = includedPayrollTotal;
                appliedPercentage = config.PayrollTaxesPercentage;
                
                if (includedPayrollTotal > 0 && config.PayrollTaxesPercentage.HasValue)
                {
                    ptebAmount = includedPayrollTotal * (config.PayrollTaxesPercentage.Value / 100m);
                    
                    // Apply escalators only for percentage type
                    ptebAmount = ApplyEscalatorsToPercentageOnly(ptebAmount, config, year, monthOneBased);
                }
            }
            else if (config.PayrollTaxesBillingType == "Actual")
            {
                // Use actual PTEB from budget rows
                ptebAmount = GetActualPtebFromBudgetRows(budgetRows, siteData.SiteNumber, monthOneBased);
                // No escalators for actual type
            }

            return new PtebInternalRevenueDto
            {
                Total = ptebAmount,
                CalculationType = calculationType,
                BaseAmount = baseAmount,
                AppliedPercentage = appliedPercentage
            };
        }

        private bool IsContractTypeBillingAccount(InternalRevenueDataVo siteData)
        {
            return siteData.Contract?.ContractTypes?.Contains(bs_contracttypechoices.BillingAccount) == true;
        }

        private BillableAccountConfigVo GetPtebConfiguration(InternalRevenueDataVo siteData)
        {
            // Get the first billable account configuration (as per task specification)
            return siteData.Contract?.BillableAccountsData?.FirstOrDefault();
        }

        private decimal GetIncludedPayrollTotalFromDataverse(InternalRevenueDataVo siteData, int year, int monthOneBased)
        {
            return _billableExpenseRepository.GetPayrollExpenseBudget(siteData.SiteId, year, monthOneBased);
        }

        private decimal GetActualPtebFromBudgetRows(List<PnlRowDto> budgetRows, string siteNumber, int monthOneBased)
        {
            // Find PTEB budget row
            var ptebRow = budgetRows?.FirstOrDefault(r => r.ColumnName == "Pteb");
            if (ptebRow?.MonthlyValues == null) return 0m;

            // Get the month value (monthOneBased is 1-based, MonthlyValues.Month is 0-based)
            int monthZeroBased = monthOneBased - 1;
            var monthValue = ptebRow.MonthlyValues.FirstOrDefault(mv => mv.Month == monthZeroBased);
            if (monthValue?.SiteDetails == null) return 0m;

            // Find the site detail for this site
            var siteDetail = monthValue.SiteDetails.FirstOrDefault(sd => sd.SiteId == siteNumber);
            return siteDetail?.Value ?? 0m;
        }

        private decimal ApplyEscalatorsToPercentageOnly(
            decimal baseAmount,
            BillableAccountConfigVo config,
            int targetYear,
            int targetMonthOneBased)
        {
            // Escalators only apply to percentage-based PTEB
            if (config.PayrollTaxesBillingType != "Percentage")
                return baseAmount;

            bool hasEscalatorRule = config.PayrollTaxesEscalatorEnable == true &&
                                   config.PayrollTaxesEscalatorMonth.HasValue &&
                                   config.PayrollTaxesEscalatorValue.HasValue &&
                                   config.PayrollTaxesEscalatorValue.Value != 0;

            if (!hasEscalatorRule || targetMonthOneBased < config.PayrollTaxesEscalatorMonth.Value)
                return baseAmount;

            if (config.PayrollTaxesEscalatorType == "Amount")
            {
                return baseAmount + config.PayrollTaxesEscalatorValue.Value;
            }
            else if (config.PayrollTaxesEscalatorType == "Percentage")
            {
                decimal escalatorPercent = config.PayrollTaxesEscalatorValue.Value / 100m;
                return baseAmount * (1 + escalatorPercent);
            }

            return baseAmount;
        }

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

        private PtebInternalRevenueDto CalculateCurrentMonthPtebForSite(
            InternalRevenueDataVo siteData,
            int year,
            int monthOneBased,
            SiteMonthlyRevenueDetailDto siteDetailDto,
            List<PnlRowDto> budgetRows)
        {
            // Step 1: Check if contract type includes BillingAccount (feature flag guard)
            if (!IsContractTypeBillingAccount(siteData))
            {
                return null; // Skip PTEB calculation
            }

            // Step 2: Get PTEB configuration
            var config = GetPtebConfiguration(siteData);
            if (config == null || config.PayrollTaxesEnabled != true)
            {
                return null; // Skip if PTEB not enabled
            }

            decimal ptebAmount = 0m;
            string calculationType = "Unknown";
            decimal baseAmount = 0m;
            decimal appliedPercentage = 0m;

            // Step 3: Calculate based on billing type
            if (config.PayrollTaxesBillingType == "Percentage")
            {
                calculationType = "Percentage";
                
                // Base = actual-to-date (Resolved Payroll) + forecast remainder after cutoff
                var actualToDate = (siteDetailDto?.PayrollBreakdown?.ActualPayroll) ?? 0m;
                var forecastRemainder = (siteDetailDto?.PayrollBreakdown?.ForecastedPayroll) ?? 0m;
                var includedPayrollTotal = actualToDate + forecastRemainder;
                baseAmount = includedPayrollTotal;
                
                if (includedPayrollTotal > 0 && config.PayrollTaxesPercentage.HasValue)
                {
                    appliedPercentage = config.PayrollTaxesPercentage.Value;
                    ptebAmount = includedPayrollTotal * (config.PayrollTaxesPercentage.Value / 100m);
                    
                    // Apply escalators only for percentage type
                    ptebAmount = ApplyEscalatorsToPercentageOnly(ptebAmount, config, year, monthOneBased);
                }
            }
            else if (config.PayrollTaxesBillingType == "Actual")
            {
                calculationType = "Actual";
                
                // Use actual PTEB from budget rows (no forecast for actual type)
                ptebAmount = GetActualPtebFromBudgetRows(budgetRows, siteData.SiteNumber, monthOneBased);
                // No escalators for actual type
            }

            return new PtebInternalRevenueDto
            {
                Total = ptebAmount,
                CalculationType = calculationType,
                BaseAmount = baseAmount,
                AppliedPercentage = appliedPercentage
            };
        }

        // Daily forecast remainder not wired here; SupportServices/Insurance handle forecast via payroll repository.
    }
} 