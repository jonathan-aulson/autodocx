using System;
using System.Collections.Generic;
using System.Linq;
using api.Data;
using api.Models.Dto;
using api.Models.Vo;
using Microsoft.Extensions.Logging;
using TownePark.Models.Vo;

namespace api.Services.Impl.Calculators
{
    public class PtebForecastCalculator : IPtebForecastCalculator
    {
     
        private readonly ILogger<PtebForecastCalculator> _logger;

        public PtebForecastCalculator(IPayrollRepository payrollRepository, ILogger<PtebForecastCalculator> logger)
        {
            _logger = logger;
        }

        public void ComputeForMonth(
            PnlResponseDto pnlResponse,
            List<InternalRevenueDataVo> allSitesRevenueData,
            int targetYear,
            int targetMonthOneBased,
            int targetMonthZeroBased,
            Dictionary<string, decimal> forecastedPayrollBySiteNumber,
            Dictionary<string, decimal> priorYearRateBySiteNumber)
        {
            // Always compute for the target month; actualization preference handled below

            var ptebForecastRow = pnlResponse.ForecastRows?.FirstOrDefault(r => r.ColumnName == "Pteb");
            var budgetPtebRow = pnlResponse.BudgetRows?.FirstOrDefault(r => r.ColumnName == "Pteb");
            var budgetPayrollRow = pnlResponse.BudgetRows?.FirstOrDefault(r => r.ColumnName == "Payroll");
            var actualPtebRow = pnlResponse.ActualRows?.FirstOrDefault(r => r.ColumnName == "Pteb");
            if (ptebForecastRow == null || budgetPtebRow == null || budgetPayrollRow == null)
            {
                return;
            }

            var monthValue = ptebForecastRow.MonthlyValues?.FirstOrDefault(mv => mv.Month == targetMonthZeroBased);
            var actualMonthValue = actualPtebRow?.MonthlyValues?.FirstOrDefault(mv => mv.Month == targetMonthZeroBased);
            if (monthValue == null)
            {
                return;
            }
            monthValue.SiteDetails ??= new List<SiteMonthlyRevenueDetailDto>();

            decimal monthTotal = 0m;
            foreach (var siteData in allSitesRevenueData)
            {
                decimal budgetPteb = Math.Abs(GetBudgetSiteValue(budgetPtebRow, siteData.SiteNumber, targetMonthZeroBased));
                decimal budgetPayroll = Math.Abs(GetBudgetSiteValue(budgetPayrollRow, siteData.SiteNumber, targetMonthZeroBased));
                forecastedPayrollBySiteNumber.TryGetValue(siteData.SiteNumber, out var forecastedPayroll);

                // Prefer actuals when present (> 0)
                var actualSiteDetail = actualMonthValue?.SiteDetails?.FirstOrDefault(sd => sd.SiteId == siteData.SiteNumber);
                if (actualSiteDetail?.Value is decimal av && av > 0m)
                {
                    var siteDetailActualized = monthValue.SiteDetails.FirstOrDefault(sd => sd.SiteId == siteData.SiteNumber);
                    if (siteDetailActualized == null)
                    {
                        siteDetailActualized = new SiteMonthlyRevenueDetailDto { SiteId = siteData.SiteNumber };
                        monthValue.SiteDetails.Add(siteDetailActualized);
                    }
                    siteDetailActualized.Value = Math.Abs(av);
                    siteDetailActualized.IsForecast = false;
                    // Mark breakdown as actual (no rate/base applicable)
                    siteDetailActualized.PtebBreakdown = new PtebBreakdownDto
                    {
                        RatePercent = null,
                        BasePayroll = null,
                        Source = "Actual"
                    };
                    monthTotal += Math.Abs(av);
                    continue;
                }

                bool missingInputs = (budgetPteb <= 0m) || (budgetPayroll <= 0m) || (forecastedPayroll <= 0m);
                decimal ptebForecast;
                decimal? ptebRate = null;

                if (missingInputs)
                {
                    // Fallbacks per AC: prefer prior-year derived rate if available; else global default (7.65%).
                    // Apply to forecasted payroll when available; else fallback to budget PTEB.
                    var defaultRate = 0.0765m;
                    decimal pyRate = 0m;
                    var hasPriorYearRate = priorYearRateBySiteNumber != null && priorYearRateBySiteNumber.TryGetValue(siteData.SiteNumber, out pyRate) && pyRate > 0m;
                    if (forecastedPayroll > 0m)
                    {
                        var rateToUse = hasPriorYearRate ? pyRate : defaultRate;
                        ptebForecast = Math.Round(forecastedPayroll * rateToUse, 2, MidpointRounding.AwayFromZero);
                        // Attach forecast breakdown (rate × forecasted payroll)
                        EnsureSiteDetailExists(monthValue, siteData.SiteNumber).PtebBreakdown = new PtebBreakdownDto
                        {
                            RatePercent = Math.Round(rateToUse * 100m, 2, MidpointRounding.AwayFromZero),
                            BasePayroll = forecastedPayroll,
                            Source = "Forecast"
                        };
                    }
                    else
                    {
                        ptebForecast = budgetPteb; // last resort fallback
                        // Budget fallback: no calculable rate/base
                        EnsureSiteDetailExists(monthValue, siteData.SiteNumber).PtebBreakdown = new PtebBreakdownDto
                        {
                            RatePercent = null,
                            BasePayroll = null,
                            Source = "Budget"
                        };
                    }
                }
                else
                {
                    ptebRate = budgetPayroll == 0m ? (decimal?)null : (budgetPteb / budgetPayroll);
                    ptebForecast = ptebRate.HasValue ? Math.Round(forecastedPayroll * ptebRate.Value, 2, MidpointRounding.AwayFromZero) : budgetPteb;
                    // Attach forecast breakdown when a rate is available
                    if (ptebRate.HasValue && forecastedPayroll > 0m)
                    {
                        EnsureSiteDetailExists(monthValue, siteData.SiteNumber).PtebBreakdown = new PtebBreakdownDto
                        {
                            RatePercent = Math.Round(ptebRate.Value * 100m, 2, MidpointRounding.AwayFromZero),
                            BasePayroll = forecastedPayroll,
                            Source = "Forecast"
                        };
                    }
                    else
                    {
                        EnsureSiteDetailExists(monthValue, siteData.SiteNumber).PtebBreakdown = new PtebBreakdownDto
                        {
                            RatePercent = null,
                            BasePayroll = null,
                            Source = "Budget"
                        };
                    }
                }

                var siteDetail = monthValue.SiteDetails.FirstOrDefault(sd => sd.SiteId == siteData.SiteNumber);
                if (siteDetail == null)
                {
                    siteDetail = new SiteMonthlyRevenueDetailDto { SiteId = siteData.SiteNumber };
                    monthValue.SiteDetails.Add(siteDetail);
                }
                siteDetail.Value = ptebForecast;
                // Mark as forecast when computed using a rate applied to forecasted payroll (including prior-year/default rate cases).
                // Mark as not-forecast only when falling back to budget amount.
                siteDetail.IsForecast = forecastedPayroll > 0m;
                monthTotal += ptebForecast;
            }

            monthValue.Value = monthTotal;
        }

        private static SiteMonthlyRevenueDetailDto EnsureSiteDetailExists(MonthValueDto monthValue, string siteNumber)
        {
            var siteDetail = monthValue.SiteDetails.FirstOrDefault(sd => sd.SiteId == siteNumber);
            if (siteDetail == null)
            {
                siteDetail = new SiteMonthlyRevenueDetailDto { SiteId = siteNumber };
                monthValue.SiteDetails.Add(siteDetail);
            }
            return siteDetail;
        }

        private static decimal GetBudgetSiteValue(PnlRowDto budgetRow, string siteNumber, int targetMonthZeroBased)
        {
            var month = budgetRow.MonthlyValues?.FirstOrDefault(mv => mv.Month == targetMonthZeroBased);
            var site = month?.SiteDetails?.FirstOrDefault(sd => sd.SiteId == siteNumber);
            return site?.Value ?? 0m;
        }

        // no DB calls here anymore; payroll forecasts are pre-fetched and passed in
    }
}


