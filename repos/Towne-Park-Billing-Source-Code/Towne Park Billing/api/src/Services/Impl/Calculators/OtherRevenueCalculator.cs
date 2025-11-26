using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using TownePark.Models.Vo;
using api.Models.Dto;
using api.Models.Vo;
using TownePark;

namespace api.Services.Impl.Calculators
{
    /// <summary>
    /// Calculator for Other Revenue (Forecast Only) as per User Story 2420.
    /// Sums Billable Expenses, Billable Validations, Miscellaneous, Client Paid Expense, GPO Fees, and Signing Bonuses for the month.
    /// </summary>
    public class OtherRevenueCalculator : IInternalRevenueCalculator
    {
        public void CalculateAndApply(
            InternalRevenueDataVo siteData,
            int year,
            int monthOneBased,
            int currentMonth,
            MonthValueDto monthValueDto,
            SiteMonthlyRevenueDetailDto siteDetailDto,
            decimal calculatedExternalRevenue,
            List<PnlRowDto> budgetRows)
        {
            // Defensive: ensure breakdown exists
            if (siteDetailDto.InternalRevenueBreakdown == null)
                siteDetailDto.InternalRevenueBreakdown = new InternalRevenueBreakdownDto();

            if (siteDetailDto.InternalRevenueBreakdown.OtherRevenue == null)
                siteDetailDto.InternalRevenueBreakdown.OtherRevenue = new OtherRevenueInternalRevenueDto();

            // Determine site classification
            bool isManagementAgreement = siteData?.ManagementAgreement != null;
            bool hasProfitShare = siteData?.ManagementAgreement?.ProfitShareEnabled == true;
            bool hasAnyRevenueShare = siteData.Contract.ContractTypes.Contains(bs_contracttypechoices.RevenueShare);
            bool profitShareOnly = isManagementAgreement && hasProfitShare && !hasAnyRevenueShare;

            // Split Other Revenue into CPE vs non-CPE using strongly-typed ForecastData
            decimal cpeTotal = 0m;
            decimal nonCpeTotal = 0m;

             
            if (siteData?.OtherRevenues != null)
            {
                foreach (var orVo in siteData.OtherRevenues)
                {
                    if (orVo?.ForecastData == null) continue;

                    foreach (var detail in orVo.ForecastData)
                    {
                        if (!MatchesTargetMonth(detail.MonthYear, year, monthOneBased))
                            continue;

                        cpeTotal += detail.ClientPaidExpense;
                        nonCpeTotal += detail.BillableExpense
                                     + detail.Credits
                                     + detail.GPOFees
                                     + detail.RevenueValidation
                                     + detail.SigningBonus
                                     + detail.Miscellaneous;
                    }
                }
            }

            // For Profit Share ONLY, exclude CPE from Internal Revenue (external contra is handled later)
            decimal internalOtherRevenue = profitShareOnly ? nonCpeTotal : (nonCpeTotal + cpeTotal);

            siteDetailDto.InternalRevenueBreakdown.OtherRevenue.Total = internalOtherRevenue;

            // Update the overall calculated total internal revenue
            UpdateCalculatedTotalInternalRevenue(siteDetailDto.InternalRevenueBreakdown);
        }

        public void AggregateMonthlyTotals(List<SiteMonthlyRevenueDetailDto> siteDetailsForMonth, MonthValueDto monthValueDto)
        {
            decimal totalOtherRevenueForMonth = 0m;

            foreach (var siteDetail in siteDetailsForMonth)
            {
                if (siteDetail.InternalRevenueBreakdown?.OtherRevenue?.Total != null)
                {
                    totalOtherRevenueForMonth += siteDetail.InternalRevenueBreakdown.OtherRevenue.Total.Value;
                }
            }

            if (monthValueDto.InternalRevenueBreakdown == null)
                monthValueDto.InternalRevenueBreakdown = new InternalRevenueBreakdownDto();

            if (monthValueDto.InternalRevenueBreakdown.OtherRevenue == null)
                monthValueDto.InternalRevenueBreakdown.OtherRevenue = new OtherRevenueInternalRevenueDto();

            monthValueDto.InternalRevenueBreakdown.OtherRevenue.Total = totalOtherRevenueForMonth;

            // Update calculated total internal revenue at month level
            UpdateCalculatedTotalInternalRevenue(monthValueDto.InternalRevenueBreakdown);
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

        // Helper: determine if a MonthYear string matches the target year/month
        private static bool MatchesTargetMonth(string? monthYear, int targetYear, int targetMonth)
        {
            if (string.IsNullOrWhiteSpace(monthYear)) return false;
            monthYear = monthYear.Trim();

            var dt = new DateTime(targetYear, targetMonth, 1);
            var candidates = new[]
            {
                $"{targetYear}{targetMonth:D2}",
                $"{targetYear}-{targetMonth:D2}",
                $"{targetMonth:D2}/{targetYear}",
                dt.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                dt.ToString("MMMM yyyy", CultureInfo.InvariantCulture),
                dt.ToString("yyyy/MM", CultureInfo.InvariantCulture)
            };

            foreach (var cand in candidates)
            {
                if (string.Equals(monthYear, cand, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            // Try parse as a date
            if (DateTime.TryParse(monthYear, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                return parsed.Year == targetYear && parsed.Month == targetMonth;

            // Numeric yyyymm
            if (monthYear.All(char.IsDigit) && monthYear.Length == 6)
            {
                if (int.TryParse(monthYear.Substring(0, 4), out var y) &&
                    int.TryParse(monthYear.Substring(4, 2), out var m))
                {
                    return y == targetYear && m == targetMonth;
                }
            }

            return false;
        }

        // Helper: safe decimal property fetch via reflection
        private static decimal GetDecimalProp(object obj, string propName)
        {
            try
            {
                var p = obj.GetType().GetProperty(propName);
                if (p == null) return 0m;
                var val = p.GetValue(obj);
                if (val == null) return 0m;
                return Convert.ToDecimal(val, CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0m;
            }
        }

        // Helper: safe string property fetch via reflection
        private static string? GetStringProp(object obj, string propName)
        {
            try
            {
                var p = obj.GetType().GetProperty(propName);
                if (p == null) return null;
                var val = p.GetValue(obj);
                return val?.ToString();
            }
            catch
            {
                return null;
            }
        }

        // Helper: safe DateTime property fetch via reflection
        private static DateTime? GetDateTimeProp(object obj, string propName)
        {
            try
            {
                var p = obj.GetType().GetProperty(propName);
                if (p == null) return null;
                var val = p.GetValue(obj);
                if (val is DateTime dt) return dt; // Nullable<DateTime> boxes to DateTime when HasValue is true
                if (val is string s && DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                    return parsed;
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
