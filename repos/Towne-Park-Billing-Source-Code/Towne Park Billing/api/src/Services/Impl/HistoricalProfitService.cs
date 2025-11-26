using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TownePark.Data;
using TownePark.Models.Vo;
using api.Models.Vo;
using api.Models.Dto;
using api.Services.Impl.Calculators;

namespace api.Services.Impl
{
    /// <summary>
    /// Service for calculating historical profit data using EDW actual data.
    /// Uses actual data from EDW (External Revenue, Insurance, Claims, PTEB, Other Expenses)
    /// and only calculates the management fee (% of payroll).
    /// </summary>
    public class HistoricalProfitService : IHistoricalProfitService
    {
        private readonly IInternalRevenueRepository _internalRevenueRepository;
        private readonly ISiteStatisticService _siteStatisticService;
        private readonly ILogger<HistoricalProfitService> _logger;
        private readonly List<IManagementAgreementCalculator> _managementAgreementCalculators;
        
        // Cache for calculated profits to avoid redundant calculations
        private readonly Dictionary<(int year, List<Guid> siteIds), Dictionary<(Guid siteId, int year, int month), decimal>> _profitCache = new();

        public HistoricalProfitService(
            IInternalRevenueRepository internalRevenueRepository,
            ISiteStatisticService siteStatisticService,
            ILogger<HistoricalProfitService> logger,
            IEnumerable<IManagementAgreementCalculator> managementAgreementCalculators)
        {
            _internalRevenueRepository = internalRevenueRepository ?? throw new ArgumentNullException(nameof(internalRevenueRepository));
            _siteStatisticService = siteStatisticService ?? throw new ArgumentNullException(nameof(siteStatisticService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Get all calculators except ProfitShareCalculator, ordered by Order property
            _managementAgreementCalculators = managementAgreementCalculators?
                .Where(c => !(c is ProfitShareCalculator))
                .OrderBy(c => c.Order)
                .ToList() 
                ?? throw new ArgumentNullException(nameof(managementAgreementCalculators));
        }

        public async Task<Dictionary<(Guid siteId, int year, int month), decimal>> 
            GetHistoricalProfitsAsync(List<InternalRevenueDataVo> siteDataList, int year, int startMonth, int endMonth)
        {
            _logger.LogInformation("Calculating historical profits for {SiteCount} sites, year {Year}, months {StartMonth}-{EndMonth}", 
            siteDataList.Count, year, startMonth, endMonth);

            // Prepare siteNumbers and siteIds from the provided siteDataList
            var siteNumbers = siteDataList.Select(s => s.SiteNumber).Distinct().ToList();
            var siteIds = siteDataList.Select(s => s.SiteId).Distinct().ToList();

                // Check if we have cached results for this exact request
                var cacheKey = (year, siteIds);
                if (_profitCache.TryGetValue(cacheKey, out var cachedResults))
                {
                    _logger.LogDebug("Using cached historical profits for {SiteCount} sites in year {Year}", 
                        siteIds.Count, year);

                    // Filter cached results to requested range
                    var filteredResults = new Dictionary<(Guid siteId, int year, int month), decimal>();
                    foreach (var kvp in cachedResults)
                    {
                        if (kvp.Key.month >= startMonth && kvp.Key.month <= endMonth)
                        {
                            filteredResults[kvp.Key] = kvp.Value;
                        }
                    }
                    return filteredResults;
                }

                var profitResults = new Dictionary<(Guid siteId, int year, int month), decimal>();
                var allSitesRevenueData = await _internalRevenueRepository.GetInternalRevenueDataAsync(siteNumbers, year) 
                ?? new List<InternalRevenueDataVo>();

                // Get actual data from EDW
                // EDW provides: External Revenue, Insurance, Claims, PTEB, Other Expenses
                // We only need to calculate: Management Fee (% of payroll)
                PnlBySiteListVo edwPnlData;
                try
                {
                    edwPnlData = await _siteStatisticService.GetPNLData(siteNumbers, year);
                    _logger.LogInformation("Retrieved EDW PNL data for {SiteCount} sites", siteNumbers.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to retrieve EDW PNL data for year {Year}", year);
                    throw new InvalidOperationException($"Unable to retrieve EDW data for historical profit calculation: {ex.Message}", ex);
                }

                allSitesRevenueData = allSitesRevenueData
                .Where(s => siteNumbers.Contains(s.SiteNumber))
                .ToList();
                if (!allSitesRevenueData.Any())
                {
                    _logger.LogWarning("No revenue data found for sites in year {Year}", year);
                    // Return zero profits for all requested site/month combinations
                    foreach (var siteId in siteIds)
                    {
                        for (int month = startMonth; month <= endMonth; month++)
                        {
                            profitResults[(siteId, year, month)] = 0m;
                        }
                    }
                    return profitResults;
                }


            // Process each month in the requested range
            for (int monthOneBased = startMonth; monthOneBased <= endMonth; monthOneBased++)
            {

                // Process each site
                foreach (var siteData in allSitesRevenueData)
                {
                    // Get EDW data for this site and month
                    var edwSiteData = edwPnlData?.PnlBySite?.FirstOrDefault(p => p.SiteNumber == siteData.SiteNumber);
                    var edwMonthData = edwSiteData?.Pnl?.Actual?.FirstOrDefault(m => m.MonthNum == monthOneBased);

                    decimal profit = 0m;
                    if (edwMonthData != null && edwMonthData.ExternalRevenue > 0)
                    {
                        // Calculate profit using EDW data
                        profit = await CalculateSiteProfitFromEdwAsync(
                            edwMonthData, siteData, year, monthOneBased);
                    }
                    else
                    {
                        // No EDW data available for this site/month
                        _logger.LogWarning("No EDW data found for site {SiteNumber} {Year}-{Month}, setting profit to 0",
                            siteData.SiteNumber, year, monthOneBased);
                        profit = 0m;
                    }

                    profitResults[(siteData.SiteId, year, monthOneBased)] = profit;

                    _logger.LogDebug("Calculated profit for site {SiteId} {Year}-{Month}: {Profit}",
                        siteData.SiteId, year, monthOneBased, profit);
                }
            }

            // Fill in zeros for any missing site/month combinations
            foreach (var siteId in siteIds)
            {
                for (int month = startMonth; month <= endMonth; month++)
                {
                    var key = (siteId, year, month);
                    if (!profitResults.ContainsKey(key))
                    {
                        profitResults[key] = 0m;
                    }
                }
            }
            
            // Cache the results if we calculated the full year (months 1-12)
            if (startMonth == 1 && endMonth == 12)
            {
                _profitCache[cacheKey] = new Dictionary<(Guid siteId, int year, int month), decimal>(profitResults);
                _logger.LogDebug("Cached historical profits for {SiteCount} sites in year {Year}", 
                    siteIds.Count, year);
            }

            return profitResults;
        }


        private async Task<decimal> CalculateSiteProfitFromEdwAsync(
            PnlMonthDetailVo edwData,
            InternalRevenueDataVo siteData,
            int year,
            int monthOneBased)
        {
            // Create minimal DTOs required by the calculators
            var monthValueDto = new MonthValueDto { Month = monthOneBased - 1 }; // Zero-based month
            var siteDetailDto = new SiteMonthlyRevenueDetailDto 
            { 
                SiteId = siteData.SiteId.ToString(),
                InternalRevenueBreakdown = new InternalRevenueBreakdownDto()
            };
            
            // Create budget rows with EDW data (Other Expenses for forecast)
            var budgetRows = new List<PnlRowDto>
            {
                new PnlRowDto
                {
                    ColumnName = "OtherExpense",
                    MonthlyValues = new List<MonthValueDto>
                    {
                        new MonthValueDto 
                        { 
                            Month = monthOneBased - 1,
                            Value = edwData.OtherExpense
                        }
                    }
                }
            };
            
            // Run all management agreement calculators (except ProfitShareCalculator) in order
            foreach (var calculator in _managementAgreementCalculators)
            {
                await calculator.CalculateAndApplyAsync(
                    siteData,
                    year,
                    monthOneBased,
                    DateTime.Today.Month, // currentMonth parameter
                    monthValueDto,
                    siteDetailDto,
                    edwData.ExternalRevenue,
                    budgetRows
                );
            }
            
            // Extract calculated management agreement total (includes all components)
            decimal managementAgreementTotal = siteDetailDto.InternalRevenueBreakdown?.ManagementAgreement?.Total ?? 0m;
            decimal billableAccountsTotal = siteDetailDto.InternalRevenueBreakdown?.BillableAccounts?.Total ?? 0m;

            // Calculate total expenses including EDW actuals and calculated components
            decimal totalExpenses = managementAgreementTotal + billableAccountsTotal + edwData.OtherExpense;

            // Calculate profit
            decimal profit = edwData.ExternalRevenue - totalExpenses;

            _logger.LogDebug("Historical Profit for site {SiteNumber} {Year}-{Month}: Revenue={Revenue}, MgmtAgr={MgmtAgr}, Billable={Billable}, OtherExp={OtherExp}, Profit={Profit}",
                siteData.SiteNumber, year, monthOneBased, edwData.ExternalRevenue, managementAgreementTotal, billableAccountsTotal, edwData.OtherExpense, profit);

            return await Task.FromResult(profit);
        }
    }
}