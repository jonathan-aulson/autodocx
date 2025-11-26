using System;
using System.Collections.Generic;
using System.Linq;
using api.Models.Dto;
using api.Models.Vo;
using TownePark.Models.Vo;
using TownePark;

namespace api.Services.Impl.Calculators
{
    public class ExternalRevenueCalculator : IExternalRevenueCalculator
    {
        public string TargetColumnName => "ExternalRevenue";

        public void CalculateAndApply(InternalRevenueDataVo siteData, int year, int monthOneBased, MonthValueDto monthValueDto, SiteMonthlyRevenueDetailDto siteDetailDto)
        {
            var currentDate = DateTime.Today;
            var targetDate = new DateTime(year, monthOneBased, 1);
            var isPastMonth = targetDate < new DateTime(currentDate.Year, currentDate.Month, 1);
            
            var preferredType = isPastMonth ? bs_sitestatisticdetailchoice.Actual : bs_sitestatisticdetailchoice.Forecast;
            var fallbackType = isPastMonth ? bs_sitestatisticdetailchoice.Forecast : bs_sitestatisticdetailchoice.Actual;
            
            var monthlyStats = siteData.SiteStatistics
                .Where(s => s.Date.Year == year && s.Date.Month == monthOneBased 
                            && s.Type == preferredType)
                .ToList();

            if (!monthlyStats.Any())
            {
                monthlyStats = siteData.SiteStatistics
                    .Where(s => s.Date.Year == year && s.Date.Month == monthOneBased 
                                && s.Type == fallbackType)
                    .ToList();
            }

            if (!monthlyStats.Any())
            {
                // No data available - PnlService will fall back to budget
                siteDetailDto.ExternalRevenueBreakdown = null;
                return;
            }

            // Simply sum the ExternalRevenue column from all daily statistics for the month
            decimal totalExternalRevenue = monthlyStats.Sum(s => s.ExternalRevenue ?? 0m);

            // Determine last actual revenue date:
            // If there are actual-type stats for this month, use their max date.
            // Otherwise, set to last day of previous month to indicate calculation ran without current-month actuals.
            DateTime? lastActualRevenueDate = null;
            var actualStats = siteData.SiteStatistics
                .Where(s => s.Date.Year == year && s.Date.Month == monthOneBased && s.Type == bs_sitestatisticdetailchoice.Actual)
                .ToList();
            if (actualStats.Any())
            {
                lastActualRevenueDate = actualStats.Max(s => s.Date);
            }
            else
            {
                lastActualRevenueDate = new DateTime(year, monthOneBased, 1).AddDays(-1);
            }

            // Create a simplified breakdown
            var breakdown = new ExternalRevenueBreakdownDto
            {
                CalculatedTotalExternalRevenue = totalExternalRevenue,
                LastActualRevenueDate = lastActualRevenueDate
            };

            siteDetailDto.ExternalRevenueBreakdown = breakdown;
        }

        public void AggregateMonthlyTotals(List<SiteMonthlyRevenueDetailDto> siteDetails, MonthValueDto monthValueDto)
        {
            decimal totalCalculatedExternalRevenueForMonth = 0;
            foreach (var siteDetail in siteDetails.Where(sd => sd.ExternalRevenueBreakdown != null))
            {
                var siteBreakdown = siteDetail.ExternalRevenueBreakdown;
                totalCalculatedExternalRevenueForMonth += siteBreakdown.CalculatedTotalExternalRevenue ?? 0m;
            }
        }
    }
}
