using System;
using System.Collections.Generic;
using System.Linq;
using TownePark.Models.Vo;
using api.Models.Dto;
using TownePark;

namespace api.Services.Impl.Calculators
{
    public class AdditionalPayrollAmountCalculator : IInternalRevenueCalculator
    {
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
            var additionalPayrollAmount = CalculateAdditionalPayrollAmountForSite(siteData, year, monthOneBased);
            
            if (siteDetailDto.InternalRevenueBreakdown == null)
            {
                siteDetailDto.InternalRevenueBreakdown = new InternalRevenueBreakdownDto();
            }
            
            if (siteDetailDto.InternalRevenueBreakdown.BillableAccounts == null)
            {
                siteDetailDto.InternalRevenueBreakdown.BillableAccounts = new BillableAccountsInternalRevenueDto();
            }
            
            // Set the specific breakdown field
            siteDetailDto.InternalRevenueBreakdown.BillableAccounts.AdditionalPayrollAmount = additionalPayrollAmount;
            
            // Add to existing total rather than setting it
            if (siteDetailDto.InternalRevenueBreakdown.BillableAccounts.Total.HasValue)
            {
                siteDetailDto.InternalRevenueBreakdown.BillableAccounts.Total += additionalPayrollAmount;
            }
            else
            {
                siteDetailDto.InternalRevenueBreakdown.BillableAccounts.Total = additionalPayrollAmount;
            }

            // Update the overall calculated total internal revenue
            UpdateCalculatedTotalInternalRevenue(siteDetailDto.InternalRevenueBreakdown);
        }

        public void AggregateMonthlyTotals(List<SiteMonthlyRevenueDetailDto> siteDetailsForMonth, MonthValueDto monthValueDto)
        {
            decimal totalAdditionalPayrollAmountForMonth = 0m;

            foreach (var siteDetail in siteDetailsForMonth)
            {
                if (siteDetail.InternalRevenueBreakdown?.BillableAccounts?.AdditionalPayrollAmount != null)
                {
                    totalAdditionalPayrollAmountForMonth += siteDetail.InternalRevenueBreakdown.BillableAccounts.AdditionalPayrollAmount.Value;
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

            // Set the specific aggregated breakdown field
            if (monthValueDto.InternalRevenueBreakdown.BillableAccounts.AdditionalPayrollAmount.HasValue)
            {
                monthValueDto.InternalRevenueBreakdown.BillableAccounts.AdditionalPayrollAmount += totalAdditionalPayrollAmountForMonth;
            }
            else
            {
                monthValueDto.InternalRevenueBreakdown.BillableAccounts.AdditionalPayrollAmount = totalAdditionalPayrollAmountForMonth;
            }

            // Add to existing total rather than setting it
            if (monthValueDto.InternalRevenueBreakdown.BillableAccounts.Total.HasValue)
            {
                monthValueDto.InternalRevenueBreakdown.BillableAccounts.Total += totalAdditionalPayrollAmountForMonth;
            }
            else
            {
                monthValueDto.InternalRevenueBreakdown.BillableAccounts.Total = totalAdditionalPayrollAmountForMonth;
            }
            
            // Update the calculated total internal revenue at month level
            UpdateCalculatedTotalInternalRevenue(monthValueDto.InternalRevenueBreakdown);
        }

        private decimal CalculateAdditionalPayrollAmountForSite(InternalRevenueDataVo siteData, int targetYear, int targetMonthOneBased)
        {
            // Check if billable accounts are enabled in the contract type
            // If the contract type doesn't include billable accounts, don't run any billable account calculations
            if (!IsBillableAccountsEnabledInContract(siteData))
            {
                return 0m;
            }

            // Check if billable accounts are available for this site
            if (siteData.BillableAccounts == null || !siteData.BillableAccounts.Any())
            {
                return 0m;
            }

            decimal totalAdditionalPayrollAmount = 0m;

            // Sum up all additional payroll amounts from non-excluded billable accounts
            foreach (var billableAccount in siteData.BillableAccounts)
            {
                // Skip excluded accounts
                if (billableAccount.IsExcluded)
                {
                    continue;
                }
                
                // The additional payroll amount is stored in the Amount property
                // which maps to bs_AdditionalPayrollAmount from the entity
                totalAdditionalPayrollAmount += billableAccount.Amount;
            }

            return totalAdditionalPayrollAmount;
        }

        private bool IsBillableAccountsEnabledInContract(InternalRevenueDataVo siteData)
        {
            // Check if the contract exists and has contract type information
            if (siteData.Contract == null || siteData.Contract.ContractTypes == null)
            {
                return false;
            }

            // Check if the contract type includes BillingAccount
            return siteData.Contract.ContractTypes.Contains(bs_contracttypechoices.BillingAccount);
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
    }
} 