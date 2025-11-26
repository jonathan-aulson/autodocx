using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TownePark;

namespace api.Data
{
    public interface IProfitShareRepository
    {
        /// <summary>
        /// Retrieves profit share records for a specific site within a date range
        /// </summary>
        /// <param name="siteNumber">The site number (4-digit string)</param>
        /// <param name="year">The year to query</param>
        /// <param name="startMonth">The starting month (inclusive)</param>
        /// <param name="endMonth">The ending month (exclusive)</param>
        /// <returns>List of profit share records</returns>
        Task<List<bs_ProfitShareByPercentage>> GetProfitSharesByDateRangeAsync(
            string siteNumber, int year, int startMonth, int endMonth);

        /// <summary>
        /// Retrieves profit share records for multiple sites for an entire year in a single batch
        /// </summary>
        /// <param name="siteNumbers">List of site numbers (4-digit strings)</param>
        /// <param name="year">The year to query</param>
        /// <returns>Dictionary mapping site numbers to their profit share records for the year</returns>
        Task<Dictionary<string, List<bs_ProfitShareByPercentage>>> GetProfitSharesBatchAsync(
            List<string> siteNumbers, int year);
    }
}