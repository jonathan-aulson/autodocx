using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TownePark.Models.Vo;

namespace api.Services
{
    /// <summary>
    /// Service for calculating historical profit data by running all revenue and expense calculators
    /// for previous periods. Used primarily for anniversary-based profit share accumulation.
    /// </summary>
    public interface IHistoricalProfitService
    {
        /// <summary>
        /// Calculates historical profits for specified sites and date range by running
        /// all revenue and expense calculators (excluding ProfitShareCalculator).
        /// </summary>
        /// <param name="siteIds">List of site IDs to calculate profits for</param>
        /// <param name="year">Year to calculate profits for</param>
        /// <param name="startMonth">Start month (1-12)</param>
        /// <param name="endMonth">End month (1-12)</param>
        /// <returns>Dictionary of calculated profits keyed by (siteId, year, month)</returns>
        Task<Dictionary<(Guid siteId, int year, int month), decimal>> 
            GetHistoricalProfitsAsync(List<InternalRevenueDataVo> siteDataList, int year, int startMonth, int endMonth);
    }
}