using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TownePark;
using api.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace api.Data.Impl
{
    public class ProfitShareRepository : IProfitShareRepository
    {
        private readonly IDataverseService _dataverseService;
        private readonly ILogger<ProfitShareRepository> _logger;

        public ProfitShareRepository(
            IDataverseService dataverseService,
            ILogger<ProfitShareRepository> logger)
        {
            _dataverseService = dataverseService ?? throw new ArgumentNullException(nameof(dataverseService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<bs_ProfitShareByPercentage>> GetProfitSharesByDateRangeAsync(
            string siteNumber, int year, int startMonth, int endMonth)
        {
            try
            {
                var orgService = _dataverseService.GetServiceClient();
                
                var query = new QueryExpression("bs_profitsharebypercentage")
                {
                    ColumnSet = new ColumnSet(
                        "bs_year", 
                        "bs_month", 
                        "bs_site", 
                        "bs_totalduetotownepark", 
                        "bs_totalduetoowner",
                        "bs_percenttocharge"),
                    Criteria = new FilterExpression(LogicalOperator.And)
                    {
                        Conditions =
                        {
                            new ConditionExpression("bs_site", ConditionOperator.Equal, siteNumber),
                            new ConditionExpression("bs_year", ConditionOperator.Equal, year.ToString()),
                            new ConditionExpression("bs_month", ConditionOperator.GreaterEqual, startMonth.ToString("D2")),
                            new ConditionExpression("bs_month", ConditionOperator.LessThan, endMonth.ToString("D2"))
                        }
                    }
                };

                var results = await Task.Run(() => orgService.RetrieveMultiple(query));
                
                _logger.LogDebug("Retrieved {Count} profit share records for site {SiteNumber}, year {Year}, months {StartMonth}-{EndMonth}", 
                    results.Entities.Count, siteNumber, year, startMonth, endMonth - 1);
                
                return results.Entities.Select(e => e.ToEntity<bs_ProfitShareByPercentage>()).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving profit share records for site {SiteNumber}, year {Year}, months {StartMonth}-{EndMonth}", 
                    siteNumber, year, startMonth, endMonth - 1);
                return new List<bs_ProfitShareByPercentage>();
            }
        }

        public async Task<Dictionary<string, List<bs_ProfitShareByPercentage>>> GetProfitSharesBatchAsync(
            List<string> siteNumbers, int year)
        {
            try
            {
                var orgService = _dataverseService.GetServiceClient();
                
                // Create a filter for all site numbers
                var siteFilter = new FilterExpression(LogicalOperator.Or);
                foreach (var siteNumber in siteNumbers)
                {
                    siteFilter.AddCondition("bs_site", ConditionOperator.Equal, siteNumber);
                }

                var query = new QueryExpression("bs_profitsharebypercentage")
                {
                    ColumnSet = new ColumnSet(
                        "bs_year", 
                        "bs_month", 
                        "bs_site", 
                        "bs_totalduetotownepark", 
                        "bs_totalduetoowner",
                        "bs_percenttocharge"),
                    Criteria = new FilterExpression(LogicalOperator.And)
                    {
                        Conditions =
                        {
                            new ConditionExpression("bs_year", ConditionOperator.Equal, year.ToString())
                        },
                        Filters = { siteFilter }
                    }
                };

                var results = await Task.Run(() => orgService.RetrieveMultiple(query));
                
                _logger.LogDebug("Retrieved {Count} profit share records for {SiteCount} sites in year {Year}", 
                    results.Entities.Count, siteNumbers.Count, year);
                
                // Group results by site number
                var groupedResults = results.Entities
                    .Select(e => e.ToEntity<bs_ProfitShareByPercentage>())
                    .GroupBy(ps => ps.bs_Site)
                    .ToDictionary(
                        g => g.Key, 
                        g => g.ToList()
                    );

                // Ensure all requested sites are in the dictionary (even if empty)
                foreach (var siteNumber in siteNumbers)
                {
                    if (!groupedResults.ContainsKey(siteNumber))
                    {
                        groupedResults[siteNumber] = new List<bs_ProfitShareByPercentage>();
                    }
                }

                return groupedResults;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving batch profit share records for {SiteCount} sites in year {Year}", 
                    siteNumbers.Count, year);
                
                // Return empty lists for all sites on error
                return siteNumbers.ToDictionary(
                    siteNumber => siteNumber, 
                    siteNumber => new List<bs_ProfitShareByPercentage>()
                );
            }
        }
    }
}