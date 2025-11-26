using System;
using System.Collections.Generic;
using System.Linq;
using TownePark.Models.Vo;
using TownePark;
using api.Models.Dto;

namespace api.Services.Impl.Calculators
{
    public class FixedFeeCalculator : IInternalRevenueCalculator
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
            //Check if FixedFee is in the enabled contract types
            if (siteData.Contract?.ContractTypes == null || 
                !siteData.Contract.ContractTypes.Contains(bs_contracttypechoices.FixedFee))
                return;
            
            var fixedFeeRevenue = CalculateMonthlyFixedFeeRevenueForSite(siteData, year, monthOneBased);
            
            if (siteDetailDto.InternalRevenueBreakdown == null)
            {
                siteDetailDto.InternalRevenueBreakdown = new InternalRevenueBreakdownDto();
            }
            siteDetailDto.InternalRevenueBreakdown.FixedFee = fixedFeeRevenue;

            // Hybrid approach: set CalculatedTotalInternalRevenue at site level
            var breakdown = siteDetailDto.InternalRevenueBreakdown;
            decimal total = 0m;
            if (breakdown.FixedFee?.Total != null) total += breakdown.FixedFee.Total.Value;
            if (breakdown.PerOccupiedRoom?.Total != null) total += breakdown.PerOccupiedRoom.Total.Value;
            if (breakdown.RevenueShare?.Total != null) total += breakdown.RevenueShare.Total.Value;
            if (breakdown.BillableAccounts?.Total != null) total += breakdown.BillableAccounts.Total.Value;
            if (breakdown.ManagementAgreement?.Total != null) total += breakdown.ManagementAgreement.Total.Value;
            if (breakdown.OtherRevenue?.Total != null) total += breakdown.OtherRevenue.Total.Value;
            breakdown.CalculatedTotalInternalRevenue = total;
        }

        public void AggregateMonthlyTotals(List<SiteMonthlyRevenueDetailDto> siteDetailsForMonth, MonthValueDto monthValueDto)
        {
            decimal totalFixedFeeBaseForMonth = 0m;
            decimal totalFixedFeeEscalatorValueForMonth = 0m;
            List<EscalatorDto> aggregatedEscalatorsForMonth = new List<EscalatorDto>();

            foreach (var siteDetail in siteDetailsForMonth)
            {
                if (siteDetail.InternalRevenueBreakdown?.FixedFee != null)
                {
                    var fixedFee = siteDetail.InternalRevenueBreakdown.FixedFee;
                    totalFixedFeeBaseForMonth += fixedFee.BaseAmount ?? 0m;
                    if (fixedFee.Escalators != null && fixedFee.Escalators.Any(e => e.IsApplied))
                    {
                        totalFixedFeeEscalatorValueForMonth += fixedFee.Escalators.Where(e => e.IsApplied).Sum(e => e.Amount);
                        aggregatedEscalatorsForMonth.AddRange(fixedFee.Escalators.Where(e => e.IsApplied));
                    }
                }
            }

            if (monthValueDto.InternalRevenueBreakdown == null)
            {
                monthValueDto.InternalRevenueBreakdown = new InternalRevenueBreakdownDto();
            }

            monthValueDto.InternalRevenueBreakdown.FixedFee = new FixedFeeInternalRevenueDto
            {
                BaseAmount = totalFixedFeeBaseForMonth,
                Escalators = aggregatedEscalatorsForMonth.Any() ? aggregatedEscalatorsForMonth : new List<EscalatorDto>(),
                Total = totalFixedFeeBaseForMonth + totalFixedFeeEscalatorValueForMonth
            };
            
            
            monthValueDto.Value = monthValueDto.InternalRevenueBreakdown.FixedFee?.Total ?? 0m;
        }

        private FixedFeeInternalRevenueDto CalculateMonthlyFixedFeeRevenueForSite(InternalRevenueDataVo siteData, int targetYear, int targetMonthOneBased)
        {
            DateTime firstDayOfCalculationMonth = new DateTime(targetYear, targetMonthOneBased, 1);
            var contract = siteData.Contract;
            bool hasEscalatorRule = contract != null && contract.IncrementMonth.HasValue && contract.IncrementAmount.HasValue && contract.IncrementAmount.Value != 0;
            decimal escalatorPercent = hasEscalatorRule ? contract.IncrementAmount.Value / 100m : 0m;

            decimal totalOriginalBaseFeeThisMonth = 0m;
            decimal totalEscalatedValueAtStartOfTargetYear = 0m;
            var escalatorsList = new List<EscalatorDto>();

            if (siteData.FixedFees != null)
            {
                foreach (var feeVo in siteData.FixedFees)
                {
                    // Only consider fees active for this month (ignore those with EndDate before this month)
                    if (!(feeVo.StartDate <= firstDayOfCalculationMonth && (feeVo.EndDate == null || feeVo.EndDate >= firstDayOfCalculationMonth)))
                        continue;

                    totalOriginalBaseFeeThisMonth += feeVo.Fee;
                    decimal feeValueAfterHistoricalEsc = feeVo.Fee;

                    // Compound escalators for each year from fee start up to (but not including) targetYear
                    if (hasEscalatorRule)
                    {
                        for (int escalationYear = feeVo.StartDate.Year; escalationYear < targetYear; escalationYear++)
                        {
                            var escalatorApplicationDate = new DateTime(escalationYear, contract.IncrementMonth.Value, 1);
                            // Only escalate if fee was active during the increment month of that year
                            if (feeVo.StartDate <= escalatorApplicationDate && (feeVo.EndDate == null || feeVo.EndDate >= escalatorApplicationDate))
                            {
                                decimal historicalEscalatorAmount = feeValueAfterHistoricalEsc * escalatorPercent;
                                feeValueAfterHistoricalEsc += historicalEscalatorAmount;
                                escalatorsList.Add(new EscalatorDto
                                {
                                    Description = $"Automatic Contract Escalator of {Decimal.Round((decimal)contract.IncrementAmount, 2)}% ({contract.IncrementMonth.Value}/{escalationYear})",
                                    Amount = historicalEscalatorAmount,
                                    IsApplied = true // Assuming historical escalators are always applied if conditions met
                                });
                            }
                        }
                    }
                    totalEscalatedValueAtStartOfTargetYear += feeValueAfterHistoricalEsc;
                }
            }

            decimal finalAmountForMonth = totalEscalatedValueAtStartOfTargetYear;

            // Apply the targetYear's own escalator if this month is on/after increment month
            if (hasEscalatorRule && targetMonthOneBased >= contract.IncrementMonth.Value)
            {
                // Check if the fee was active during the increment month of the target year
                // This check needs to be more robust if fees can start/end mid-year relative to the increment month.
                // For simplicity, assuming if any fee contributes to totalEscalatedValueAtStartOfTargetYear, it's eligible for this year's escalator.
                if (totalEscalatedValueAtStartOfTargetYear > 0) // Simplified check
                {
                    decimal escalatorValueAppliedThisMonth = totalEscalatedValueAtStartOfTargetYear * escalatorPercent;
                    finalAmountForMonth += escalatorValueAppliedThisMonth;

                    escalatorsList.Add(new EscalatorDto
                    {
                        Description = $"Annual Fixed Fee Escalator ({contract.IncrementMonth.Value}/{targetYear})",
                        Amount = escalatorValueAppliedThisMonth,
                        IsApplied = true
                    });
                }
            }

            return new FixedFeeInternalRevenueDto
            {
                BaseAmount = totalOriginalBaseFeeThisMonth,
                Escalators = escalatorsList.Where(e => e.IsApplied).ToList(), // Only return applied escalators
                Total = finalAmountForMonth
            };
        }
    }
}
