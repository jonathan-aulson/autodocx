// ClaimsCalculator.cs
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
    /// Calculates claims component for Management Agreement Internal Revenue.
    /// Handles Annual Calendar, Annual Anniversary, and Per Claim cap types.
    /// Claims data is retrieved from bs_billableexpense.bs_claimsbudget column
    /// using period-based filtering (YYYYMM format).
    /// </summary>
    public class ClaimsCalculator : IManagementAgreementCalculator
    {
        private readonly IInternalRevenueRepository _internalRevenueRepository;
        private readonly IBillableExpenseRepository _billableExpenseRepository;
        private readonly ILogger<ClaimsCalculator> _logger;

        public int Order => 3;

        public ClaimsCalculator(
            IInternalRevenueRepository internalRevenueRepository,
            IBillableExpenseRepository billableExpenseRepository,
            ILogger<ClaimsCalculator> logger)
        {
            _internalRevenueRepository = internalRevenueRepository;
            _billableExpenseRepository = billableExpenseRepository;
            _logger = logger;
        }

        /// <summary>
        /// Calculates and applies claims for a specific site and billing period.
        /// Handles Annual Calendar, Annual Anniversary, and Per Claim cap types.
        /// Note: Per Claim calculations sum all claims without cap enforcement 
        /// as per Jon Aulson's confirmation that billing managers handle per-claim caps manually.
        /// </summary>
        /// <param name="siteData">Site data containing management agreement configuration</param>
        /// <param name="year">Billing year</param>
        /// <param name="monthOneBased">Billing month (1-12)</param>
        /// <param name="monthValueDto">Monthly value DTO to update</param>
        /// <param name="siteDetailDto">Site detail DTO to update with calculated claims</param>
        /// <param name="calculatedExternalRevenue">External revenue amount</param>
        /// <param name="budgetRows">Budget rows for P&L</param>
        /// <summary>
        /// Calculates and applies claims for a specific site and billing period.
        /// Handles Annual Calendar, Annual Anniversary, and Per Claim cap types.
        /// Note: Per Claim calculations sum all claims without cap enforcement 
        /// as per Jon Aulson's confirmation that billing managers handle per-claim caps manually.
        /// </summary>
        /// <param name="siteData">Site data containing management agreement configuration</param>
        /// <param name="year">Billing year</param>
        /// <param name="monthOneBased">Billing month (1-12)</param>
        /// <param name="currentMonth">Current month for current month logic</param>
        /// <param name="monthValueDto">Monthly value DTO to update</param>
        /// <param name="siteDetailDto">Site detail DTO to update with calculated claims</param>
        /// <param name="calculatedExternalRevenue">External revenue amount</param>
        /// <param name="budgetRows">Budget rows for P&L</param>
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
            try
            {
                // Check if ManagementAgreement is in the enabled contract types
                if (siteData.Contract?.ContractTypes == null || 
                    !siteData.Contract.ContractTypes.Contains(bs_contracttypechoices.ManagementAgreement))
                    return;
                
                var config = siteData?.ManagementAgreement;
                if (config == null || config.ClaimsEnabled != true || config.ClaimsType == null)
                {return;
                }

                decimal claimsSum = 0m;
                var today = DateTime.Today;
                var isCurrentMonth = (year == today.Year && monthOneBased == today.Month);if (config.ClaimsType == bs_claimtype.AnnualCalendar)
                {
                    if (isCurrentMonth)
                    {
                        // Current month: if any actuals > 0 in InternalActuals, sum actuals only; otherwise use full-month forecast
                        claimsSum = CalculateCurrentMonthClaims(siteData.SiteId, year, monthOneBased, siteDetailDto);
                    }
                    else
                    {
                        // Sum all claims for the calendar year (January through current month)
                        var yearStart = $"{year:D4}01"; // e.g., "202501"
                        var currentPeriod = $"{year:D4}{monthOneBased:D2}"; // e.g., "202507"
                        claimsSum = _billableExpenseRepository.GetClaimsBudgetForPeriodRange(
                            siteData.SiteId, yearStart, currentPeriod);
                    }
                        
                    if (config.ClaimsCapAmount.HasValue)
                        claimsSum = Math.Min(claimsSum, config.ClaimsCapAmount.Value);}
                else if (config.ClaimsType == bs_claimtype.AnnualAnniversary && config.AnniversaryDate.HasValue)
                {
                    if (isCurrentMonth)
                    {
                        // Current month: if any actuals > 0 in InternalActuals, sum actuals only; otherwise use full-month forecast
                        claimsSum = CalculateCurrentMonthClaims(siteData.SiteId, year, monthOneBased, siteDetailDto);
                    }
                    else
                    {
                        // Calculate anniversary period start based on business rule
                        var anniversaryMonth = config.AnniversaryDate.Value.Month;
                        string startPeriod;
                        
                        if (anniversaryMonth <= monthOneBased) 
                        {
                            // Anniversary month is before/same as current month - use current year
                            startPeriod = $"{year:D4}{anniversaryMonth:D2}"; // e.g., "202503"
                        }
                        else 
                        {
                            // Anniversary month is after current month - use previous year  
                            startPeriod = $"{year-1:D4}{anniversaryMonth:D2}"; // e.g., "202409"
                        }
                        
                        var currentPeriod = $"{year:D4}{monthOneBased:D2}"; // e.g., "202507"
                        claimsSum = _billableExpenseRepository.GetClaimsBudgetForPeriodRange(
                            siteData.SiteId, startPeriod, currentPeriod);
                    }
                        
                    if (config.ClaimsCapAmount.HasValue)
                        claimsSum = Math.Min(claimsSum, config.ClaimsCapAmount.Value);}
                else if (config.ClaimsType == bs_claimtype.PerClaim)
                {
                    if (isCurrentMonth)
                    {
                        // Current month: if any actuals > 0 in InternalActuals, sum actuals only; otherwise use full-month forecast
                        claimsSum = CalculateCurrentMonthClaims(siteData.SiteId, year, monthOneBased, siteDetailDto);
                    }
                    else
                    {
                        // Per Claim: sum current month only (billing managers handle caps manually per Jon Aulson)
                        var currentPeriod = $"{year:D4}{monthOneBased:D2}"; // e.g., "202507"
                        claimsSum = _billableExpenseRepository.GetClaimsBudgetForPeriod(
                            siteData.SiteId, currentPeriod);
                    }}

                // Update DTO
                siteDetailDto.InternalRevenueBreakdown ??= new InternalRevenueBreakdownDto();
                siteDetailDto.InternalRevenueBreakdown.ManagementAgreement ??= new ManagementAgreementInternalRevenueDto();
                siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.CalculatedClaims = claimsSum;}
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating claims for site {SiteNumber}", 
                    siteData?.SiteNumber ?? "Unknown");
                // Continue without throwing to prevent calculation failures
            }
        }

        /// <summary>
        /// Calculates claims for current month per business rules:
        /// - If any actuals (>0) exist in InternalActuals.DailyActuals for the month: sum actuals only; ignore forecast
        /// - If no actuals (>0): use full-month forecast
        /// </summary>
        private decimal CalculateCurrentMonthClaims(Guid siteId, int year, int month, SiteMonthlyRevenueDetailDto siteDetailDto)
        {
            try
            {
                decimal actualsTotal = 0m;
                var hadAnyActuals = false;

                var internalActuals = siteDetailDto.InternalActuals;
                if (internalActuals?.DailyActuals != null && internalActuals.DailyActuals.Count > 0)
                {
                    foreach (var d in internalActuals.DailyActuals)
                    {
                        if (DateTime.TryParse(d.Date, out var dayDate) && dayDate.Year == year && dayDate.Month == month)
                        {
                            if (d.Claims > 0)
                            {
                                hadAnyActuals = true;
                                actualsTotal += d.Claims;
                            }
                        }
                    }
                }

                if (hadAnyActuals)
                {return actualsTotal;
                }

                // No actuals > 0: use full-month forecast (claims budget for the month)
                var period = $"{year:D4}{month:D2}";
                var monthlyForecast = _billableExpenseRepository.GetClaimsBudgetForPeriod(siteId, period);return monthlyForecast;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating current month claims for site {SiteId}, year {Year}, month {Month}", siteId, year, month);
                return 0m;
            }
        }

        /// <summary>
        /// Gets actual claims up to the maximum available date (>0)
        /// </summary>
        private (decimal Total, DateTime? MaxDate) GetActualClaimsUpToMaxAvailableDate(Guid siteId, int year, int month)
        {
            try
            {
                var today = DateTime.Today;
                var daysInMonth = DateTime.DaysInMonth(year, month);
                var currentDay = today.Day;
                
                decimal totalActuals = 0m;
                DateTime? maxDate = null;
                
                // Check each day up to current day to find actuals
                for (int day = 1; day <= currentDay; day++)
                {
                    var checkDate = new DateTime(year, month, day);
                    var period = $"{year:D4}{month:D2}"; // e.g., "202507"
                    
                    // Get actuals for this specific day (this would need to be implemented in the repository)
                    var dailyActuals = _billableExpenseRepository.GetClaimsBudgetForPeriod(siteId, period);
                    
                    if (dailyActuals > 0)
                    {
                        totalActuals += dailyActuals;
                        maxDate = checkDate;
                    }
                }
                
                return (totalActuals, maxDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting actual claims up to max available date for site {SiteId}, year {Year}, month {Month}", siteId, year, month);
                return (0m, null);
            }
        }

        /// <summary>
        /// Calculates forecast claims for remaining days of the month
        /// </summary>
        private decimal CalculateForecastForRemainingDays(Guid siteId, int year, int month, int startDay, int endDay)
        {
            try
            {
                var period = $"{year:D4}{month:D2}"; // e.g., "202507"
                
                // Get forecast for the entire month
                var monthlyForecast = _billableExpenseRepository.GetClaimsBudgetForPeriod(siteId, period);
                
                // Calculate daily average
                var daysInMonth = DateTime.DaysInMonth(year, month);
                var dailyAverage = monthlyForecast / daysInMonth;
                
                // Calculate forecast for remaining days
                var remainingDays = endDay - startDay + 1;
                var forecastForRemainingDays = dailyAverage * remainingDays;
                
                return forecastForRemainingDays;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating forecast for remaining days for site {SiteId}, year {Year}, month {Month}, days {StartDay}-{EndDay}", siteId, year, month, startDay, endDay);
                return 0m;
            }
        }

        /// <summary>
        /// Aggregates calculated claims from all sites for monthly totals.
        /// Sums up the CalculatedClaims values from individual site calculations.
        /// </summary>
        /// <param name="siteDetailsForMonth">All site details for the month</param>
        /// <param name="monthValueDto">Monthly totals DTO to update</param>
        public async Task AggregateMonthlyTotalsAsync(
            List<SiteMonthlyRevenueDetailDto> siteDetailsForMonth,
            MonthValueDto monthValueDto)
        {
            try
            {
                // Sum up calculated claims from all sites for the month
                var totalClaims = siteDetailsForMonth
                    .Sum(s => s.InternalRevenueBreakdown?.ManagementAgreement?.CalculatedClaims ?? 0m);

                // Initialize nested DTO structure if needed
                monthValueDto.InternalRevenueBreakdown ??= new InternalRevenueBreakdownDto();
                monthValueDto.InternalRevenueBreakdown.ManagementAgreement ??= new ManagementAgreementInternalRevenueDto();
                
                // Set the aggregated total
                monthValueDto.InternalRevenueBreakdown.ManagementAgreement.CalculatedClaims = totalClaims;}
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error aggregating monthly claims totals");
                throw; // Re-throw for aggregation errors as they indicate system issues
            }
        }
    }
}