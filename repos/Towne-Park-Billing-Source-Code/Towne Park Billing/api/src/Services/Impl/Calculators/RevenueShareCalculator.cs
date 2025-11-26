using System;
using System.Collections.Generic;
using System.Linq;
using TownePark.Models.Vo;
using TownePark;
using api.Models.Dto;
using api.Models.Vo;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace api.Services.Impl.Calculators
{
    public class RevenueShareCalculator : IInternalRevenueCalculator
    {
        private readonly ILogger<RevenueShareCalculator> _logger;

        public RevenueShareCalculator() : this(NullLogger<RevenueShareCalculator>.Instance) {}

        public RevenueShareCalculator(ILogger<RevenueShareCalculator> logger)
        {
            _logger = logger ?? NullLogger<RevenueShareCalculator>.Instance;
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
            //Check if RevenuShare is in the enabled contract types
            if (siteData.Contract?.ContractTypes == null || 
                !siteData.Contract.ContractTypes.Contains(bs_contracttypechoices.RevenueShare))
                return;
            
            RevenueShareInternalRevenueDto revenueShareDto;

            // Determine if this is the current processing month using the provided currentMonth parameter
            var isCurrentMonth = (monthOneBased == currentMonth);
            if (isCurrentMonth)
            {
                // Current month: use actuals up to last available date from InternalActuals, forecast for remaining days
                revenueShareDto = CalculateCurrentMonthRevenueShareForSite(siteData, year, monthOneBased, siteDetailDto);
            }
            else
            {
                // Non-current month: use existing monthly total approach based on provided external revenue
                revenueShareDto = CalculateMonthlyRevenueShareForSite(siteData, year, monthOneBased, calculatedExternalRevenue);
            }

            if (siteDetailDto.InternalRevenueBreakdown == null)
                siteDetailDto.InternalRevenueBreakdown = new InternalRevenueBreakdownDto();

            siteDetailDto.InternalRevenueBreakdown.RevenueShare = revenueShareDto;

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
            decimal totalRevenueShareForMonth = 0m;
            List<RevenueShareTierDto> aggregatedTiers = new List<RevenueShareTierDto>();
            List<EscalatorDto> aggregatedEscalators = new List<EscalatorDto>();

            foreach (var siteDetail in siteDetailsForMonth)
            {
                var revenueShare = siteDetail.InternalRevenueBreakdown?.RevenueShare;
                if (revenueShare != null)
                {
                    totalRevenueShareForMonth += revenueShare.Total ?? 0m;
                    if (revenueShare.Tiers != null)
                        aggregatedTiers.AddRange(revenueShare.Tiers);
                    if (revenueShare.Escalators != null)
                        aggregatedEscalators.AddRange(revenueShare.Escalators.Where(e => e.IsApplied));
                }
            }

            if (monthValueDto.InternalRevenueBreakdown == null)
                monthValueDto.InternalRevenueBreakdown = new InternalRevenueBreakdownDto();

            monthValueDto.InternalRevenueBreakdown.RevenueShare = new RevenueShareInternalRevenueDto
            {
                ForecastedExternalRevenue = aggregatedTiers.Sum(t => t.RevenueInTier ?? 0m), // aggregate placeholder
                Tiers = aggregatedTiers,
                Escalators = aggregatedEscalators,
                Total = totalRevenueShareForMonth
            };

            // Do not set CalculatedTotalInternalRevenue here; central aggregation will handle it.
            monthValueDto.Value = totalRevenueShareForMonth;
        }

        private RevenueShareInternalRevenueDto CalculateMonthlyRevenueShareForSite(
            InternalRevenueDataVo siteData,
            int targetYear,
            int targetMonthOneBased,
            decimal calculatedExternalRevenue)
        {
            // Use the provided external revenue directly (treated as forecast)
            decimal forecastedExternalRevenue = calculatedExternalRevenue;
            // Find the correct revenue share tiers for this month (for display)
            var tiers = GetRevenueShareTiersForMonth(siteData.RevenueShareThresholds, forecastedExternalRevenue, targetYear, targetMonthOneBased);

            // Calculate share amount for each tier and total
            decimal totalShare = 0m;
            foreach (var tier in tiers)
            {
                // Revenue in tier = min(forecastedExternalRevenue, thresholdEnd) - thresholdStart
                decimal tierStart = tier.ThresholdStart ?? 0m;
                decimal tierEnd = tier.ThresholdEnd ?? decimal.MaxValue;
                decimal revenueInTier = Math.Max(0, Math.Min(forecastedExternalRevenue, tierEnd) - tierStart);
                tier.RevenueInTier = revenueInTier;
                // Convert percentage from whole number (30.0) to decimal fraction (0.30) before calculation
                tier.ShareAmount = revenueInTier * ((tier.Percentage ?? 0m) / 100m);
                totalShare += tier.ShareAmount ?? 0m;}

            // Escalators (placeholder)
            var escalatorsList = new List<EscalatorDto>();
            decimal totalEscalators = 0m;
            // TODO: Add logic for escalators if applicable

            decimal total = totalShare + totalEscalators;
            return new RevenueShareInternalRevenueDto
            {
                ActualExternalRevenue = 0m,
                ForecastedExternalRevenue = forecastedExternalRevenue,
                ActualShareAmount = 0m,
                ForecastedShareAmount = totalShare,
                Tiers = tiers,
                Escalators = escalatorsList,
                Total = total
            };
        }

        private RevenueShareInternalRevenueDto CalculateCurrentMonthRevenueShareForSite(
            InternalRevenueDataVo siteData,
            int targetYear,
            int targetMonthOneBased,
            SiteMonthlyRevenueDetailDto siteDetailDto)
        {
            // 1) Sum actual external revenue up to last available actual date (inclusive)
            var actuals = siteDetailDto.InternalActuals != null
                ? siteDetailDto.InternalActuals.DailyActuals
                : new List<DailyActualVo>();
            DateTime monthStart = new DateTime(targetYear, targetMonthOneBased, 1);
            DateTime monthEnd = monthStart.AddMonths(1).AddDays(-1);

            DateTime? lastActualDate = null;
            if (actuals.Any())
            {
                // Parse dates and find max within month
                var actualDates = actuals
                    .Select(a => DateTime.TryParse(a.Date, out var dt) ? dt : (DateTime?)null)
                    .Where(d => d.HasValue && d.Value.Year == targetYear && d.Value.Month == targetMonthOneBased)
                    .Select(d => d!.Value)
                    .ToList();
                if (actualDates.Any())
                {
                    lastActualDate = actualDates.Max();
                }
            }

            decimal sumActualExternalRevenue = 0m;
            if (lastActualDate.HasValue)
            {
                sumActualExternalRevenue = actuals
                    .Select(a => new { a.ExternalRevenue, Parsed = DateTime.TryParse(a.Date, out var dt) ? dt : (DateTime?)null })
                    .Where(x => x.Parsed.HasValue && x.Parsed.Value <= lastActualDate.Value &&
                                x.Parsed.Value.Year == targetYear && x.Parsed.Value.Month == targetMonthOneBased)
                    .Sum(x => x.ExternalRevenue);
            }

            // 2) For remaining days after cutoff through month-end, sum forecast daily external revenue
            DateTime cutoff = lastActualDate ?? monthStart.AddDays(-1); // if no actuals, everything is forecast
            var forecastDaily = (siteData.SiteStatistics ?? new List<TownePark.Models.Vo.SiteStatisticDetailVo>())
                .Where(s => s.Date.Year == targetYear && s.Date.Month == targetMonthOneBased &&
                            s.Type == bs_sitestatisticdetailchoice.Forecast && s.Date > cutoff)
                .ToList();

            decimal sumForecastExternalRevenue = forecastDaily.Sum(s => s.ExternalRevenue ?? 0m);
            decimal totalExternalRevenue = sumActualExternalRevenue + sumForecastExternalRevenue;

            // 3) Determine effective tier structure and allocate actual/forecast sequentially across tiers
            var tiers = GetRevenueShareTiersForMonth(siteData.RevenueShareThresholds, totalExternalRevenue, targetYear, targetMonthOneBased);

            decimal totalShare = 0m;
            decimal remainingActual = sumActualExternalRevenue;
            decimal remainingForecast = sumForecastExternalRevenue;
            decimal actualShare = 0m;
            decimal forecastShare = 0m;

            foreach (var tier in tiers)
            {
                decimal tierStart = tier.ThresholdStart ?? 0m;
                decimal tierEnd = tier.ThresholdEnd ?? decimal.MaxValue;
                decimal tierCapacity = Math.Max(0, Math.Min(totalExternalRevenue, tierEnd) - tierStart);
                tier.RevenueInTier = tierCapacity;

                // Allocate actual first into the tier, then forecast
                decimal actualInTier = Math.Min(remainingActual, tierCapacity);
                remainingActual -= actualInTier;

                decimal forecastInTier = Math.Min(remainingForecast, tierCapacity - actualInTier);
                remainingForecast -= forecastInTier;

                var rate = (tier.Percentage ?? 0m) / 100m;
                var tierActualShare = actualInTier * rate;
                var tierForecastShare = forecastInTier * rate;

                actualShare += tierActualShare;
                forecastShare += tierForecastShare;
                tier.ShareAmount = (tierActualShare + tierForecastShare);
                totalShare += tier.ShareAmount ?? 0m;
            }

            // Keep escalator behavior as-is (none applied here)
            var escalatorsList = new List<EscalatorDto>();
            decimal totalEscalators = 0m;
            return new RevenueShareInternalRevenueDto
            {
                ActualExternalRevenue = sumActualExternalRevenue,
                ForecastedExternalRevenue = sumForecastExternalRevenue,
                ActualShareAmount = actualShare,
                ForecastedShareAmount = forecastShare,
                Tiers = tiers,
                Escalators = escalatorsList,
                Total = actualShare + forecastShare + totalEscalators
            };
        }

        private decimal? GetMarginalPercentageForCombinedTotal(List<RevenueShareTierDto> tiers, decimal combinedAmount)
        {
            if (tiers == null || tiers.Count == 0) return 0m;
            foreach (var tier in tiers)
            {
                var start = tier.ThresholdStart ?? 0m;
                var end = tier.ThresholdEnd ?? decimal.MaxValue;
                if (combinedAmount >= start && combinedAmount < end)
                {
                    return tier.Percentage ?? 0m;
                }
            }
            // Fallback to highest tier percentage if beyond defined ranges
            return tiers.Last().Percentage ?? 0m;
        }

        /// <summary>
        /// Returns the revenue share tiers for the given month, sorted by threshold start ascending.
        /// </summary>
        private List<RevenueShareTierDto> GetRevenueShareTiersForMonth(
            List<RevenueShareThresholdVo> thresholds,
            decimal forecastedExternalRevenue,
            int year,
            int month)
        {
            if (thresholds == null || thresholds.Count == 0)
                return new List<RevenueShareTierDto>();

            // Find the threshold structure that is effective for this month
            var effectiveThreshold = thresholds
                .FirstOrDefault(t =>
                    t.ThresholdStructure?.Tiers != null &&
                    t.ThresholdStructure.Tiers.Any(tier =>
                        (!tier.EffectiveFrom.HasValue || tier.EffectiveFrom.Value <= new DateTime(year, month, 1)) &&
                        (!tier.EffectiveTo.HasValue || tier.EffectiveTo.Value >= new DateTime(year, month, 1))
                    )
                );

            if (effectiveThreshold == null)
                return new List<RevenueShareTierDto>();

            // Map to RevenueShareTierDto using Amount as the "up to" ceiling of the tier.
            // Example: [0 -> Amount1], [Amount1 -> Amount2], [Amount2 -> +inf when null]
            var effectiveDate = new DateTime(year, month, 1);
            var activeTiers = effectiveThreshold.ThresholdStructure.Tiers
                .Where(tier =>
                    (!tier.EffectiveFrom.HasValue || tier.EffectiveFrom.Value <= effectiveDate) &&
                    (!tier.EffectiveTo.HasValue || tier.EffectiveTo.Value >= effectiveDate)
                )
                .Select(tier => new { Tier = tier, Ceiling = tier.Amount > 0m ? (decimal?)tier.Amount : null })
                .OrderBy(x => x.Ceiling ?? decimal.MaxValue)
                .ToList();

            var tierDtos = new List<RevenueShareTierDto>();
            decimal? previousCeiling = 0m;
            foreach (var item in activeTiers)
            {
                var currentCeiling = item.Ceiling; // null indicates final open-ended tier
                tierDtos.Add(new RevenueShareTierDto
                {
                    ThresholdStart = previousCeiling,
                    ThresholdEnd = currentCeiling,
                    Percentage = item.Tier.SharePercentage
                });
                // For an open-ended tier (null), keep previousCeiling unchanged; loop will end anyway
                previousCeiling = currentCeiling ?? previousCeiling;
            }

            return tierDtos;
        }

        private decimal CalculateShareForAmount(InternalRevenueDataVo siteData, decimal amount, int year, int month)
        {
            if (amount <= 0m) return 0m;
            var tiers = GetRevenueShareTiersForMonth(siteData.RevenueShareThresholds, amount, year, month);
            decimal share = 0m;
            foreach (var tier in tiers)
            {
                decimal tierStart = tier.ThresholdStart ?? 0m;
                decimal tierEnd = tier.ThresholdEnd ?? decimal.MaxValue;
                decimal revenueInTier = Math.Max(0, Math.Min(amount, tierEnd) - tierStart);
                share += revenueInTier * ((tier.Percentage ?? 0m) / 100m);
            }
            return share;
        }
    }
}
