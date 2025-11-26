using System;
using System.Collections.Generic;
using System.Linq;
using api.Models.Dto;
using api.Models.Vo;
using Microsoft.Extensions.Logging;
using TownePark;
using TownePark.Models.Vo;

namespace api.Services.Impl.Calculators
{
    public class InsuranceRowCalculator : IInsuranceRowCalculator
    {
        private readonly ILogger<InsuranceRowCalculator> _logger;

        public InsuranceRowCalculator(
            ILogger<InsuranceRowCalculator> logger)
        {
            _logger = logger;
        }

        public void ComputeForMonth(
            PnlResponseDto pnlResponse,
            List<InternalRevenueDataVo> allSitesRevenueData,
            int targetYear,
            int targetMonthOneBased,
            int targetMonthZeroBased,
            Dictionary<string, decimal> forecastedPayrollBySiteNumber)
        {
            var insuranceForecastRow = pnlResponse.ForecastRows?.FirstOrDefault(r => r.ColumnName == "Insurance");
            var budgetInsuranceRow = pnlResponse.BudgetRows?.FirstOrDefault(r => r.ColumnName == "Insurance");
            var actualInsuranceRow = pnlResponse.ActualRows?.FirstOrDefault(r => r.ColumnName == "Insurance");
            if (insuranceForecastRow == null || budgetInsuranceRow == null)
            {
                return;
            }

            var monthValue = insuranceForecastRow.MonthlyValues?.FirstOrDefault(mv => mv.Month == targetMonthZeroBased);
            var actualMonthValue = actualInsuranceRow?.MonthlyValues?.FirstOrDefault(mv => mv.Month == targetMonthZeroBased);
            if (monthValue == null)
            {
                return;
            }
            monthValue.SiteDetails ??= new List<SiteMonthlyRevenueDetailDto>();

            decimal monthTotal = 0m;
            var today = DateTime.Today;
            foreach (var siteData in allSitesRevenueData)
            {
                // Determine MA vs non-MA (needed for both actual/forecast paths)
                bool isManagementAgreement = siteData?.Contract?.ContractTypes != null &&
                    siteData.Contract.ContractTypes.Contains(bs_contracttypechoices.ManagementAgreement);

                // If actualized (actual value present and > 0), use actual and skip forecast
                decimal actualVal = 0m;
                var actualSiteDetail = actualMonthValue?.SiteDetails?.FirstOrDefault(sd => sd.SiteId == siteData.SiteNumber);
                if (actualSiteDetail?.Value is decimal av && av > 0m)
                {
                    var siteDetailActualized = monthValue.SiteDetails.FirstOrDefault(sd => sd.SiteId == siteData.SiteNumber);
                    if (siteDetailActualized == null)
                    {
                        siteDetailActualized = new SiteMonthlyRevenueDetailDto { SiteId = siteData.SiteNumber };
                        monthValue.SiteDetails.Add(siteDetailActualized);
                    }
                    siteDetailActualized.Value = av;
                    siteDetailActualized.IsForecast = false;
                    siteDetailActualized.InsuranceBreakdown = new InsuranceBreakdownDto
                    {
                        IsManagementAgreement = isManagementAgreement,
                        Source = "Actual",
                        ActualizationDate = null // EDW not providing; UI will show month end
                    };
                    monthTotal += av;
                    continue;
                }

                // Forecasted payroll for site-month from precomputed dictionary
                forecastedPayrollBySiteNumber.TryGetValue(siteData.SiteNumber, out var forecastedPayroll);
                // For future months with no forecast, fallback to budget payroll as base per requirement
                var budgetPayrollRow = pnlResponse.BudgetRows?.FirstOrDefault(r => r.ColumnName == "Payroll");
                var budgetPayroll = GetBudgetSiteValue(budgetPayrollRow, siteData.SiteNumber, targetMonthZeroBased);
                // Fallback to budget payroll whenever forecasted payroll is not available or <= 0
                var usedForecast = forecastedPayroll > 0m;
                var basePayroll = usedForecast ? forecastedPayroll : budgetPayroll;

                decimal insuranceForecast = 0m;
                if (isManagementAgreement)
                {
                    // MA P&L row: 5.77% of payroll + budget 7082 vehicle insurance.
                    // Do NOT include Additional Insurance here; that belongs only to Internal Revenue.
                    var rate = 0.0577m;
                    // Reuse pre-attached 7082 from SiteDetails when available to avoid repo calls
                    decimal vehicleIns7082 = 0m;
                    var existingSd = monthValue.SiteDetails.FirstOrDefault(sd => sd.SiteId == siteData.SiteNumber);
                    if (existingSd?.InsuranceBreakdown?.VehicleInsurance7082 != null)
                    {
                        vehicleIns7082 = existingSd.InsuranceBreakdown.VehicleInsurance7082 ?? 0m;
                    }
                    // Additional Insurance is excluded from the computed value for P&L display
                    // Additional Insurance configured on MA is excluded from P&L and tooltip
                    var addlAmount = (decimal?)null;
                    insuranceForecast = Round2((basePayroll * rate) + vehicleIns7082);
                    var breakdown = new InsuranceBreakdownDto
                    {
                        RatePercent = 5.77m,
                        BasePayroll = basePayroll,
                        BasePayrollSource = usedForecast ? "Forecast" : "Budget",
                        VehicleInsurance7082 = vehicleIns7082,
                        AdditionalInsurance = addlAmount,
                        IsManagementAgreement = true,
                        Source = "Forecast",
                        ActualizationDate = null
                    };
                    AssignBreakdown(monthValue, siteData.SiteNumber, breakdown);
                }
                else
                {
                    // Non-MA: derive rate from budget (Insurance / Payroll). If missing, use default 4.45%
                    var budgetInsurance = GetBudgetSiteValue(budgetInsuranceRow, siteData.SiteNumber, targetMonthZeroBased);
                    var rate = SafeRate(budgetInsurance, budgetPayroll, 0.0445m);
                    insuranceForecast = Round2(basePayroll * rate);
                    var breakdown = new InsuranceBreakdownDto
                    {
                        RatePercent = Math.Round(rate * 100m, 2, MidpointRounding.AwayFromZero),
                        BasePayroll = basePayroll,
                        BasePayrollSource = usedForecast ? "Forecast" : "Budget",
                        VehicleInsurance7082 = 0m,
                        AdditionalInsurance = 0m,
                        IsManagementAgreement = false,
                        Source = "Forecast",
                        ActualizationDate = null
                    };
                    AssignBreakdown(monthValue, siteData.SiteNumber, breakdown);
                }

                var siteDetail = monthValue.SiteDetails.FirstOrDefault(sd => sd.SiteId == siteData.SiteNumber);
                if (siteDetail == null)
                {
                    siteDetail = new SiteMonthlyRevenueDetailDto { SiteId = siteData.SiteNumber };
                    monthValue.SiteDetails.Add(siteDetail);
                }
                // Final fallback: if forecast is 0, use budget Insurance for the site-month
                if (insuranceForecast <= 0m)
                {
                    var monthVal = budgetInsuranceRow?.MonthlyValues?.FirstOrDefault(mv => mv.Month == targetMonthZeroBased);
                    var siteBudget = monthVal?.SiteDetails?.FirstOrDefault(sd => sd.SiteId == siteData.SiteNumber)?.Value ?? 0m;
                    if (siteBudget > 0m) insuranceForecast = siteBudget;
                }
                siteDetail.Value = insuranceForecast;
                siteDetail.IsForecast = true;
                monthTotal += insuranceForecast;
            }

            monthValue.Value = monthTotal;
        }

        private static decimal GetBudgetSiteValue(PnlRowDto? budgetRow, string siteNumber, int targetMonthZeroBased)
        {
            if (budgetRow == null) return 0m;
            var month = budgetRow.MonthlyValues?.FirstOrDefault(mv => mv.Month == targetMonthZeroBased);
            var site = month?.SiteDetails?.FirstOrDefault(sd => sd.SiteId == siteNumber);
            return site?.Value ?? 0m;
        }

        private static decimal SafeRate(decimal numerator, decimal denominator, decimal fallback)
        {
            if (denominator > 0m) return numerator / denominator;
            return fallback;
        }

        private static decimal Round2(decimal v)
        {
            return Math.Round(v, 2, MidpointRounding.AwayFromZero);
        }

        private static void AssignBreakdown(MonthValueDto monthValue, string siteNumber, InsuranceBreakdownDto breakdown)
        {
            var sd = monthValue.SiteDetails!.FirstOrDefault(x => x.SiteId == siteNumber);
            if (sd == null)
            {
                sd = new SiteMonthlyRevenueDetailDto { SiteId = siteNumber };
                monthValue.SiteDetails.Add(sd);
            }
            sd.InsuranceBreakdown = breakdown;
        }
    }
}


