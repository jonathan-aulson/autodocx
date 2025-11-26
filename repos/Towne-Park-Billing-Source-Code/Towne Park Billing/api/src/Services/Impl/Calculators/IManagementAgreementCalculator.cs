using System.Collections.Generic;
using System.Threading.Tasks;
using TownePark.Models.Vo;
using api.Models.Dto;

namespace api.Services.Impl.Calculators
{
    public interface IManagementAgreementCalculator
    {
        /// <summary>
        /// Execution order for this calculator (lower numbers execute first)
        /// </summary>
        int Order { get; }

        /// <summary>
        /// Calculate and apply management agreement revenue for a specific site and month
        /// </summary>
        Task CalculateAndApplyAsync(
            InternalRevenueDataVo siteData,
            int year,
            int monthOneBased,
            int currentMonth, // NEW: Current month parameter
            MonthValueDto monthValueDto,
            SiteMonthlyRevenueDetailDto siteDetailDto,
            decimal calculatedExternalRevenue,
            List<PnlRowDto> budgetRows);

        /// <summary>
        /// Aggregate monthly totals across all sites for this calculator
        /// </summary>
        Task AggregateMonthlyTotalsAsync(
            List<SiteMonthlyRevenueDetailDto> siteDetailsForMonth,
            MonthValueDto monthValueDto);
}
}