using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TownePark.Models.Vo;
using api.Models.Dto;
using TownePark;
using api.Adapters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using api.Data;
using api.Services;

namespace api.Services.Impl.Calculators
{
    public class ProfitShareCalculator : IManagementAgreementCalculator
    {
        public int Order => 100; // Runs last after all other calculators

        private readonly IInternalRevenueMapper _mapper;
        private readonly ILogger<ProfitShareCalculator> _logger;
        private readonly IServiceProvider _serviceProvider;
        private IProfitShareRepository _profitShareRepository;

        // Cache for calculated profits within current run
        private readonly Dictionary<(Guid siteId, int year, int month), decimal> _profitCache = new();
        // Secondary cache by site number for easier lookups
        private readonly Dictionary<(string siteNumber, int year, int month), decimal> _profitCacheBySiteNumber = new();
        // Cache for tracking historical months
        private readonly Dictionary<(Guid siteId, int year, int month), bool> _historicalMonthCache = new();
        // Cache for historical profit share data to reduce database calls
        private readonly Dictionary<int, Dictionary<string, List<bs_ProfitShareByPercentage>>> _historicalProfitShareCache = new();
        // Cache for historical profit calculations from HistoricalProfitService
        private readonly Dictionary<(Guid siteId, int year), Dictionary<(Guid siteId, int year, int month), decimal>> _historicalProfitCalculationCache = new();

        public ProfitShareCalculator(
            IInternalRevenueMapper mapper,
            ILogger<ProfitShareCalculator> logger,
            IServiceProvider serviceProvider)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

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
            EnsureDtoStructure(siteDetailDto);

            var debug = new ProfitShareDebugDto
            {
                MonthlyProfitBreakdown = new List<MonthlyProfitDto>(),
                HistoricalMonths = new List<int>()
            };

            siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.ProfitShareEnabled =
                siteData?.ManagementAgreement?.ProfitShareEnabled;

            if (siteData?.ManagementAgreement?.ProfitShareEnabled != true)
            {
                AddProfitShareComponent(siteDetailDto, 0);
                siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.ProfitShareDebug = debug;
                return;
            }

            if (siteData.Contract?.ContractTypes == null ||
                !siteData.Contract.ContractTypes.Contains(bs_contracttypechoices.ManagementAgreement))
            {
                AddProfitShareComponent(siteDetailDto, 0);
                siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.ProfitShareDebug = debug;
                return;
            }

            if (string.IsNullOrWhiteSpace(siteData.ManagementAgreement.ProfitShareTierData))
            {
                AddProfitShareComponent(siteDetailDto, 0);
                siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.ProfitShareDebug = debug;
                return;
            }

            var tiers = _mapper.ParseProfitShareTierData(siteData.ManagementAgreement.ProfitShareTierData);
            if (tiers == null || !tiers.Any())
            {
                AddProfitShareComponent(siteDetailDto, 0);
                siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.ProfitShareDebug = debug;
                return;
            }

            debug.AllTiers = tiers.OrderBy(t => t.Amount).Select(t => new TierDto
            {
                Amount = t.Amount,
                Percentage = t.SharePercentage
            }).ToList();

            var currentDate = DateTime.Now;
            var currentMonthLocal = currentDate.Month;
            var currentYear = currentDate.Year;

            // Bare-bones current-month path: compute profit using actuals + forecast rules
            if (year == currentYear && monthOneBased == currentMonthLocal)
            {
                await CalculateCurrentMonth(siteData, year, monthOneBased, siteDetailDto, debug);
                siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.ProfitShareDebug = debug;
                return;
            }

            // === Phase 1: Check for historical data (for past months only)
            if (year < currentYear || (year == currentYear && monthOneBased < currentMonthLocal - 1))
            {
                var historicalProfitShare = await GetHistoricalProfitShareForMonth(
                    siteData.SiteNumber, year, monthOneBased);

                debug.DataSource = "Historical";
                debug.HistoricalMonths.Add(monthOneBased);
                debug.ProfitShareAmount = historicalProfitShare ?? 0;
                siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.ProfitShareCalculationMethod = "Historical";
                AddProfitShareComponent(siteDetailDto, historicalProfitShare ?? 0);
                siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.ProfitShareDebug = debug;
                return;
            }

            // === Phase 2: Calculate profit share for current/future months

            bool isManagementAgreement = siteData?.ManagementAgreement != null;
            bool hasProfitShare = siteData?.ManagementAgreement?.ProfitShareEnabled == true;
            bool hasAnyRevenueShare = siteData?.Contract?.ContractTypes?.Contains(bs_contracttypechoices.RevenueShare) == true;
            bool profitShareOnly = isManagementAgreement && hasProfitShare && !hasAnyRevenueShare;
            decimal cpeContra = profitShareOnly ? GetCpeContraForMonth(siteData, year, monthOneBased) : 0m;
            decimal totalRevenue = calculatedExternalRevenue + cpeContra;
            decimal otherExpensesForecast = GetOtherExpensesForecast(budgetRows, monthOneBased);
            decimal managementTotal = siteDetailDto.InternalRevenueBreakdown?.ManagementAgreement?.Total ?? 0;
            decimal billableTotal = siteDetailDto.InternalRevenueBreakdown?.BillableAccounts?.Total ?? 0;
            decimal claimsAmount = siteDetailDto.InternalRevenueBreakdown?.ManagementAgreement?.CalculatedClaims ?? 0;
            decimal totalExpenses = managementTotal + billableTotal + claimsAmount + otherExpensesForecast;
            decimal currentMonthProfit = totalRevenue - totalExpenses;

            debug.FormulaBreakdown = new FormulaBreakdownDto
            {
                ExternalRevenue = totalRevenue,
                ManagementTotal = managementTotal,
                BillableTotal = billableTotal,
                ClaimsAmount = claimsAmount,
                OtherExpenses = otherExpensesForecast,
                TotalExpenses = totalExpenses,
                Formula = $"Profit = {totalRevenue:F2} - ({managementTotal:F2} + {billableTotal:F2} + {claimsAmount:F2} + {otherExpensesForecast:F2}) = {currentMonthProfit:F2}"
            };

            debug.CurrentMonthProfit = currentMonthProfit;
            siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.TotalExpensesUsedInProfitCalc = totalExpenses;
            siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.ProfitShareCalculationMethod = "Calculated";

            _profitCache[(siteData.SiteId, year, monthOneBased)] = currentMonthProfit;
            _profitCacheBySiteNumber[(siteData.SiteNumber, year, monthOneBased)] = currentMonthProfit;

            decimal accumulatedProfit = currentMonthProfit;
            decimal accumulatedProfitFromHistory = 0;

            debug.AccumulationType = siteData.ManagementAgreement.ProfitShareAccumulationType?.ToString() ?? "Monthly";



            // === BEGIN ANNUAL ANNIVERSARY LOGIC ===
            if (siteData.ManagementAgreement.ProfitShareAccumulationType == bs_profitshareaccumulationtype.AnnualAnniversary
                && siteData.ManagementAgreement.AnniversaryDate.HasValue)
            {
                int anniversaryMonth = siteData.ManagementAgreement.AnniversaryDate.Value.Month;
                debug.AccumulationStartMonth = anniversaryMonth;

                // Helper: calculate month diff from anniversary month
                int monthsInYear = 12;
                int startMonth = anniversaryMonth;
                int endMonth = monthOneBased;
                int targetYear = year;
                int anniversaryYear = year;
                bool needsPrevYear = false;

                // If anniversaryMonth > queried month, we need months from previous year up to anniversaryMonth-1
                if (anniversaryMonth > monthOneBased)
                {
                    needsPrevYear = true;
                    anniversaryYear = year - 1;
                }

                // Case 1: anniversary month <= queried month (easy case, accumulation within current year)
                if (!needsPrevYear)
                {
                    // Accumulate from anniversary month up to (including) current month in current year
                    accumulatedProfitFromHistory = 0;
                    var monthlyBreakdown = new List<MonthlyProfitDto>();
                    for (int m = startMonth; m < endMonth; m++)
                    {
                        // Get historical profit amount (not profit share) for accumulation
                        var pastProfit = await GetHistoricalProfitAmountForMonth(siteData.SiteNumber, year, m);
                        accumulatedProfitFromHistory += pastProfit;
                        monthlyBreakdown.Add(new MonthlyProfitDto
                        {
                            Month = m,
                            Profit = pastProfit,
                            IsHistorical = true
                        });
                        if (pastProfit != 0)
                        {
                            debug.HistoricalMonths.Add(m);
                            _historicalMonthCache[(siteData.SiteId, year, m)] = true;
                        }
                    }
                    debug.MonthlyProfitBreakdown = monthlyBreakdown;
                    debug.AccumulatedProfitFromHistory = accumulatedProfitFromHistory;
                    debug.DataSource = monthlyBreakdown.Any() ? "Mixed" : "Calculated";
                }
                else // Case 2: needs previous year
                {
                    // The months from anniversaryMonth up to December are in previous year
                    int prevYearStartMonth = anniversaryMonth;
                    int prevYearEndMonth = monthsInYear; // December
                    int currYearStartMonth = 1;
                    int currYearEndMonth = monthOneBased - 1;

                    // Find out if the months in previous year are in future or past
                    bool isFutureYear = (year > currentYear) || (year == currentYear && monthOneBased > currentMonth);

                    var monthlyBreakdown = new List<MonthlyProfitDto>();
                    decimal prevYearAccum = 0;

                    // SCENARIO 2.1: future year query (months from previous year are in the future)
                    if (isFutureYear)
                    {
                        // Use HistoricalProfitService to recalculate previous year months (run all calculators except profit share)
                        var cacheKey = (siteData.SiteId, anniversaryYear);
                        Dictionary<(Guid, int, int), decimal> prevYearProfits = null;

                        if (!_historicalProfitCalculationCache.TryGetValue(cacheKey, out prevYearProfits))
                        {
                            var histProfitService = _serviceProvider.GetService<IHistoricalProfitService>();
                            if (histProfitService != null)
                            {
                                // HistoricalProfitService should run PNL calculators (except this one) and return profits
                                prevYearProfits = await histProfitService.GetHistoricalProfitsAsync(
                                    new List<InternalRevenueDataVo> { siteData },
                                    anniversaryYear, prevYearStartMonth, prevYearEndMonth);

                                _historicalProfitCalculationCache[cacheKey] = prevYearProfits;
                            }
                        }
                        // Accumulate profits for previous year (treat missing as 0)
                        for (int m = prevYearStartMonth; m <= prevYearEndMonth; m++)
                        {
                            decimal profit = 0;
                            if (prevYearProfits != null && prevYearProfits.TryGetValue((siteData.SiteId, anniversaryYear, m), out var v))
                                profit = v;
                            else
                                prevYearAccum += profit;
                            monthlyBreakdown.Add(new MonthlyProfitDto
                            {
                                Month = m,
                                Profit = profit,
                                IsHistorical = true
                            });
                        }
                        debug.MonthlyProfitBreakdown = monthlyBreakdown;
                        debug.DataSource = "HistoricalProfitService";
                        debug.AccumulatedProfitFromHistory = prevYearAccum;
                    }
                    else // SCENARIO 2.2: current year query (previous year months are in the past)
                    {
                        // Use repository to get profit shares for previous year months
                        var repo = GetProfitShareRepository();
                        var siteNumber = siteData.SiteNumber;
                        // months from anniversaryMonth to December, previous year
                        var repoResults = await repo.GetProfitSharesByDateRangeAsync(siteNumber, anniversaryYear, prevYearStartMonth, prevYearEndMonth + 1);

                        // Create a map for lookup
                        var prevYearMap = repoResults
                            .Where(r => int.TryParse(r.bs_Month, out _))
                            .ToDictionary(r => int.Parse(r.bs_Month), r => (decimal)(r.bs_TotalDueToTownePark ?? 0));
                           

                        for (int m = prevYearStartMonth; m <= prevYearEndMonth; m++)
                        {
                            decimal amt = prevYearMap.TryGetValue(m, out var val) ? val : 0;
                            if (!prevYearMap.ContainsKey(m))prevYearAccum += amt;
                            monthlyBreakdown.Add(new MonthlyProfitDto
                            {
                                Month = m,
                                Profit = amt,
                                IsHistorical = true
                            });
                        }
                        debug.MonthlyProfitBreakdown = monthlyBreakdown;
                        debug.DataSource = "Repository";
                        debug.AccumulatedProfitFromHistory = prevYearAccum;
                    }

                    // Also accumulate months from January up to (but not including) current month in queried year
                    decimal currYearAccum = 0;
                    for (int m = currYearStartMonth; m <= currYearEndMonth; m++)
                    {
                        var amt = await GetHistoricalProfitAmountForMonth(siteData.SiteNumber, year, m);
                        currYearAccum += amt;
                        debug.MonthlyProfitBreakdown.Add(new MonthlyProfitDto
                        {
                            Month = m,
                            Profit = amt,
                            IsHistorical = true
                        });
                    }
                    debug.AccumulatedProfitFromHistory = prevYearAccum + currYearAccum;
                }
                // Always include current month profit in accumulation
                accumulatedProfit = (decimal)(debug.AccumulatedProfitFromHistory + currentMonthProfit);
                debug.AccumulatedProfit = accumulatedProfit;
                
            }
            // === END ANNUAL ANNIVERSARY LOGIC ===

            // === Annual Calendar
            else if (siteData.ManagementAgreement.ProfitShareAccumulationType == bs_profitshareaccumulationtype.AnnualCalendar)
            {
                debug.AccumulationStartMonth = 1;
                decimal accumulatedProfitShareFromHistory = 0;
                var monthlyBreakdown = new List<MonthlyProfitDto>();

                for (int month = 1; month < monthOneBased; month++)
                {
                    var historicalProfit = await GetHistoricalProfitAmountForMonth(
                        siteData.SiteNumber, year, month);

                    accumulatedProfitShareFromHistory += historicalProfit;

                    monthlyBreakdown.Add(new MonthlyProfitDto
                    {
                        Month = month,
                        Profit = historicalProfit,
                        IsHistorical = true
                    });

                    if (historicalProfit != 0)
                    {
                        debug.HistoricalMonths.Add(month);
                        _historicalMonthCache[(siteData.SiteId, year, month)] = true;
                    }
                }

                // Add current month to breakdown
                monthlyBreakdown.Add(new MonthlyProfitDto
                {
                    Month = monthOneBased,
                    Profit = currentMonthProfit,
                    IsHistorical = false
                });

                debug.AccumulatedProfitFromHistory = accumulatedProfitShareFromHistory;
                accumulatedProfit = accumulatedProfitShareFromHistory + currentMonthProfit;
                debug.AccumulatedProfit = accumulatedProfit;
                debug.MonthlyProfitBreakdown = monthlyBreakdown;
                debug.DataSource = monthlyBreakdown.Any(m => m.IsHistorical) ? "Mixed" : "Calculated";
                
            }
            else
            {
                // Monthly - use current month profit only
                debug.DataSource = "Calculated";
            }

            // Find applicable tier (will always return a tier, even for negative/zero profit)
            var applicableTier = FindApplicableTier(tiers, accumulatedProfit);

            if (applicableTier != null)
            {
                debug.ApplicableTier = new ApplicableTierDto
                {
                    ThresholdAmount = applicableTier.Amount,
                    SharePercentage = applicableTier.SharePercentage,
                    TierIndex = tiers.IndexOf(applicableTier)
                };
            }

            decimal currentMonthProfitShare = accumulatedProfit * (applicableTier?.SharePercentage ?? 0) / 100m;
            decimal profitShare = currentMonthProfitShare;

            debug.ProfitShareBeforeEscalator = profitShare;

            // Apply escalators if enabled
            if (siteData.ManagementAgreement.ProfitShareEscalatorEnabled == true &&
                monthOneBased >= siteData.ManagementAgreement.ProfitShareEscalatorMonth)
            {
                debug.EscalatorApplied = true;
                debug.EscalatorType = siteData.ManagementAgreement.ProfitShareEscalatorType?.ToString();
                debug.EscalatorValue = applicableTier?.EscalatorValue ?? 0;

                profitShare = ApplyEscalator(
                    profitShare,
                    siteData.ManagementAgreement.ProfitShareEscalatorType,
                    applicableTier?.EscalatorValue ?? 0);
            }

            debug.ProfitShareAmount = profitShare;

            if (siteData.ManagementAgreement.ProfitShareAccumulationType == bs_profitshareaccumulationtype.AnnualAnniversary)
            {
                debug.ProfitShareCalculated = profitShare;
            }

            AddProfitShareComponent(siteDetailDto, profitShare);
            siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.ProfitShareDebug = debug;
        }

        private async Task CalculateCurrentMonth(
            InternalRevenueDataVo siteData,
            int year,
            int monthOneBased,
            SiteMonthlyRevenueDetailDto siteDetailDto,
            ProfitShareDebugDto debug)
        {
            // 1) External revenue: actuals up to last actual date + forecast remainder
            DateTime monthStart = new DateTime(year, monthOneBased, 1);
            DateTime monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var dailyActuals = siteDetailDto?.InternalActuals?.DailyActuals ?? new List<api.Models.Vo.DailyActualVo>();

            DateTime? lastActualDate = null;
            if (dailyActuals.Any())
            {
                var dates = dailyActuals
                    .Select(a => DateTime.TryParse(a.Date, out var dt) ? dt : (DateTime?)null)
                    .Where(d => d.HasValue && d.Value.Year == year && d.Value.Month == monthOneBased)
                    .Select(d => d!.Value)
                    .ToList();
                if (dates.Any()) lastActualDate = dates.Max();
            }

            decimal actualExternal = 0m;
            if (lastActualDate.HasValue)
            {
                actualExternal = dailyActuals
                    .Select(a => new { a.ExternalRevenue, Parsed = DateTime.TryParse(a.Date, out var dt) ? dt : (DateTime?)null })
                    .Where(x => x.Parsed.HasValue && x.Parsed.Value.Year == year && x.Parsed.Value.Month == monthOneBased && x.Parsed.Value.Date <= lastActualDate.Value.Date)
                    .Sum(x => x.ExternalRevenue);
            }

            DateTime cutoff = lastActualDate ?? monthStart.AddDays(-1);
            var forecastDaily = (siteData.SiteStatistics ?? new List<TownePark.Models.Vo.SiteStatisticDetailVo>())
                .Where(s => s.Date.Year == year && s.Date.Month == monthOneBased &&
                            s.Type == bs_sitestatisticdetailchoice.Forecast && s.Date > cutoff)
                .ToList();
            decimal forecastExternal = forecastDaily.Sum(s => s.ExternalRevenue ?? 0m);

            // If the sum of external revenue actuals for the month is 0, use full-month forecast only
            decimal totalExternalRevenue;
            if (actualExternal == 0m)
            {
                var forecastFullMonthDaily = (siteData.SiteStatistics ?? new List<TownePark.Models.Vo.SiteStatisticDetailVo>())
                    .Where(s => s.Date.Year == year && s.Date.Month == monthOneBased &&
                                s.Type == bs_sitestatisticdetailchoice.Forecast)
                    .ToList();
                totalExternalRevenue = forecastFullMonthDaily.Sum(s => s.ExternalRevenue ?? 0m);
            }
            else
            {
                totalExternalRevenue = actualExternal + forecastExternal;
            }

            // 2) Claims: use value already computed by ClaimsCalculator for current month if present; otherwise apply its rule
            decimal claimsTotal = siteDetailDto?.InternalRevenueBreakdown?.ManagementAgreement?.CalculatedClaims ?? 0m;
           

            // 3) Billable & Other: prefer monthly actuals from pnlResponse.ExpenseActuals; fallback to full-month forecast
            decimal billableTotal;
            decimal otherTotal;
            var expenseActuals = siteDetailDto?.ExpenseActuals;
            if (expenseActuals != null)
            {
                billableTotal = expenseActuals.BillableExpenseActuals ?? 0m;
                otherTotal = expenseActuals.OtherExpenseActuals ?? 0m;
            }
            else
            {
                var billableRepo = _serviceProvider.GetService<IBillableExpenseRepository>();
                var otherRepo = _serviceProvider.GetService<IBillableExpenseRepository>();
                billableTotal = billableRepo?.GetBillableExpenseBudget(siteData.SiteId, year, monthOneBased) ?? 0m;
                // For "Other" fallback, reuse billable repo OtherExpenseBudget as used elsewhere
                otherTotal = billableRepo?.GetOtherExpenseBudget(siteData.SiteId, year, monthOneBased) ?? 0m;
            }

            // 4) Management components total as currently computed
            decimal managementComponentsTotal = siteDetailDto?.InternalRevenueBreakdown?.ManagementAgreement?.Total ?? 0m;

            // Adjust external revenue for MA Profit Share ONLY by applying CPE as external contra
            {
                bool isManagementAgreement = siteData?.ManagementAgreement != null;
                bool hasProfitShare = siteData?.ManagementAgreement?.ProfitShareEnabled == true;
                bool hasAnyRevenueShare = siteData?.Contract?.ContractTypes?.Contains(bs_contracttypechoices.RevenueShare) == true;
                bool profitShareOnly = isManagementAgreement && hasProfitShare && !hasAnyRevenueShare;
                if (profitShareOnly)
                {
                    var cpeContra = GetCpeContraForMonth(siteData, year, monthOneBased);
                    totalExternalRevenue += cpeContra; // cpeContra is negative; reduces external revenue
                }
            }

            // 5) Profit for current month
            decimal currentMonthProfit = totalExternalRevenue - (claimsTotal + billableTotal + otherTotal + managementComponentsTotal);

            // 6) Load tiers (unchanged)
            var tiers = _mapper.ParseProfitShareTierData(siteData.ManagementAgreement?.ProfitShareTierData);

            // Debug: formula and profit before escalator
            debug.FormulaBreakdown = new FormulaBreakdownDto
            {
                ExternalRevenue = totalExternalRevenue,
                ManagementTotal = managementComponentsTotal,
                BillableTotal = billableTotal,
                ClaimsAmount = claimsTotal,
                OtherExpenses = otherTotal,
                TotalExpenses = (managementComponentsTotal + billableTotal + claimsTotal + otherTotal),
                Formula = $"Profit = {totalExternalRevenue:F2} - ({managementComponentsTotal:F2} + {billableTotal:F2} + {claimsTotal:F2} + {otherTotal:F2}) = {currentMonthProfit:F2}"
            };
            debug.CurrentMonthProfit = currentMonthProfit;
            debug.AccumulationType = siteData.ManagementAgreement?.ProfitShareAccumulationType?.ToString();
            debug.DataSource = "Calculated";
            siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.TotalExpensesUsedInProfitCalc = debug.FormulaBreakdown.TotalExpenses;
            siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.ProfitShareCalculationMethod = "Calculated";

            // 7) Accumulation logic identical to non-current path
            decimal accumulatedProfit = currentMonthProfit;
            decimal accumulatedProfitFromHistory = 0m;
            var now = DateTime.Now;
            int currentYear = now.Year;
            int currentMonth = now.Month;

            if (siteData.ManagementAgreement?.ProfitShareAccumulationType == bs_profitshareaccumulationtype.AnnualAnniversary
                && siteData.ManagementAgreement.AnniversaryDate.HasValue)
            {
                int anniversaryMonth = siteData.ManagementAgreement.AnniversaryDate.Value.Month;
                debug.AccumulationStartMonth = anniversaryMonth;

                int monthsInYear = 12;
                int startMonth = anniversaryMonth;
                int endMonth = monthOneBased;
                int targetYear = year;
                int anniversaryYear = year;
                bool needsPrevYear = false;

                if (anniversaryMonth > monthOneBased)
                {
                    needsPrevYear = true;
                    anniversaryYear = year - 1;
                }

                if (!needsPrevYear)
                {
                    accumulatedProfitFromHistory = 0;
                    var monthlyBreakdown = new List<MonthlyProfitDto>();
                    for (int m = startMonth; m < endMonth; m++)
                    {
                        var pastProfit = await GetHistoricalProfitAmountForMonth(siteData.SiteNumber, year, m);
                        accumulatedProfitFromHistory += pastProfit;
                        monthlyBreakdown.Add(new MonthlyProfitDto { Month = m, Profit = pastProfit, IsHistorical = true });
                        if (pastProfit != 0)
                        {
                            debug.HistoricalMonths.Add(m);
                            _historicalMonthCache[(siteData.SiteId, year, m)] = true;
                        }
                    }
                    debug.MonthlyProfitBreakdown = monthlyBreakdown;
                    debug.AccumulatedProfitFromHistory = accumulatedProfitFromHistory;
                    debug.DataSource = monthlyBreakdown.Any() ? "Mixed" : "Calculated";
                }
                else
                {
                    int prevYearStartMonth = anniversaryMonth;
                    int prevYearEndMonth = monthsInYear;
                    int currYearStartMonth = 1;
                    int currYearEndMonth = monthOneBased - 1;

                    bool isFutureYear = (year > currentYear) || (year == currentYear && monthOneBased > currentMonth);

                    var monthlyBreakdown = new List<MonthlyProfitDto>();
                    decimal prevYearAccum = 0;

                    if (isFutureYear)
                    {
                        var cacheKey = (siteData.SiteId, anniversaryYear);
                        Dictionary<(Guid, int, int), decimal> prevYearProfits = null;
                        if (!_historicalProfitCalculationCache.TryGetValue(cacheKey, out prevYearProfits))
                        {
                            var histProfitService = _serviceProvider.GetService<IHistoricalProfitService>();
                            if (histProfitService != null)
                            {
                                prevYearProfits = await histProfitService.GetHistoricalProfitsAsync(
                                    new List<InternalRevenueDataVo> { siteData }, anniversaryYear, prevYearStartMonth, prevYearEndMonth);
                                _historicalProfitCalculationCache[cacheKey] = prevYearProfits;
                            }
                        }
                        for (int m = prevYearStartMonth; m <= prevYearEndMonth; m++)
                        {
                            decimal profit = 0;
                            if (prevYearProfits != null && prevYearProfits.TryGetValue((siteData.SiteId, anniversaryYear, m), out var v))
                                profit = v;
                            else
                                prevYearAccum += profit;
                            monthlyBreakdown.Add(new MonthlyProfitDto { Month = m, Profit = profit, IsHistorical = true });
                        }
                        debug.MonthlyProfitBreakdown = monthlyBreakdown;
                        debug.DataSource = "HistoricalProfitService";
                        debug.AccumulatedProfitFromHistory = prevYearAccum;
                    }
                    else
                    {
                        var repo = GetProfitShareRepository();
                        var siteNumber = siteData.SiteNumber;
                        var repoResults = await repo.GetProfitSharesByDateRangeAsync(siteNumber, anniversaryYear, prevYearStartMonth, prevYearEndMonth + 1);
                        var prevYearMap = repoResults.Where(r => int.TryParse(r.bs_Month, out _))
                            .ToDictionary(r => int.Parse(r.bs_Month), r => (decimal)(r.bs_TotalDueToTownePark ?? 0));
                        for (int m = prevYearStartMonth; m <= prevYearEndMonth; m++)
                        {
                            decimal amt = prevYearMap.TryGetValue(m, out var val) ? val : 0;
                            if (!prevYearMap.ContainsKey(m)) prevYearAccum += amt;
                            monthlyBreakdown.Add(new MonthlyProfitDto { Month = m, Profit = amt, IsHistorical = true });
                        }
                        debug.MonthlyProfitBreakdown = monthlyBreakdown;
                        debug.DataSource = "Repository";
                        debug.AccumulatedProfitFromHistory = prevYearAccum;
                    }

                    decimal currYearAccum = 0;
                    for (int m = currYearStartMonth; m <= currYearEndMonth; m++)
                    {
                        var amt = await GetHistoricalProfitAmountForMonth(siteData.SiteNumber, year, m);
                        currYearAccum += amt;
                        debug.MonthlyProfitBreakdown.Add(new MonthlyProfitDto { Month = m, Profit = amt, IsHistorical = true });
                    }
                    debug.AccumulatedProfitFromHistory = debug.AccumulatedProfitFromHistory + currYearAccum;
                }

                accumulatedProfit = (decimal)(debug.AccumulatedProfitFromHistory + currentMonthProfit);
                debug.AccumulatedProfit = accumulatedProfit;
            }
            else if (siteData.ManagementAgreement?.ProfitShareAccumulationType == bs_profitshareaccumulationtype.AnnualCalendar)
            {
                debug.AccumulationStartMonth = 1;
                decimal accumulatedProfitShareFromHistory = 0;
                var monthlyBreakdown = new List<MonthlyProfitDto>();
                for (int month = 1; month < monthOneBased; month++)
                {
                    var historicalProfit = await GetHistoricalProfitAmountForMonth(siteData.SiteNumber, year, month);
                    accumulatedProfitShareFromHistory += historicalProfit;
                    monthlyBreakdown.Add(new MonthlyProfitDto { Month = month, Profit = historicalProfit, IsHistorical = true });
                    if (historicalProfit != 0)
                    {
                        debug.HistoricalMonths.Add(month);
                        _historicalMonthCache[(siteData.SiteId, year, month)] = true;
                    }
                }
                monthlyBreakdown.Add(new MonthlyProfitDto { Month = monthOneBased, Profit = currentMonthProfit, IsHistorical = false });
                debug.AccumulatedProfitFromHistory = accumulatedProfitShareFromHistory;
                accumulatedProfit = accumulatedProfitShareFromHistory + currentMonthProfit;
                debug.AccumulatedProfit = accumulatedProfit;
                debug.MonthlyProfitBreakdown = monthlyBreakdown;
                debug.DataSource = monthlyBreakdown.Any(m => m.IsHistorical) ? "Mixed" : "Calculated";
            }
            else
            {
                debug.DataSource = "Calculated";
            }

            // 8) Profit share based on accumulated profit
            var applicableTier = FindApplicableTier(tiers, accumulatedProfit);
            if (applicableTier != null)
            {
                var tierIndex = tiers != null ? tiers.OrderBy(t => t.Amount).ToList().FindIndex(t => t.Amount == applicableTier.Amount && t.SharePercentage == applicableTier.SharePercentage) : -1;
                debug.ApplicableTier = new ApplicableTierDto
                {
                    ThresholdAmount = applicableTier.Amount,
                    SharePercentage = applicableTier.SharePercentage,
                    TierIndex = tierIndex
                };
            }
            decimal profitShare = accumulatedProfit * (applicableTier?.SharePercentage ?? 0) / 100m;
            debug.ProfitShareBeforeEscalator = profitShare;

            if (siteData.ManagementAgreement?.ProfitShareEscalatorEnabled == true &&
                monthOneBased >= siteData.ManagementAgreement.ProfitShareEscalatorMonth)
            {
                profitShare = ApplyEscalator(profitShare, siteData.ManagementAgreement.ProfitShareEscalatorType, applicableTier?.EscalatorValue ?? 0);
                debug.EscalatorApplied = true;
                debug.EscalatorType = siteData.ManagementAgreement.ProfitShareEscalatorType?.ToString();
                debug.EscalatorValue = applicableTier?.EscalatorValue ?? 0;
            }

            debug.ProfitShareAmount = profitShare;
            debug.ProfitShareCalculated = profitShare;

            AddProfitShareComponent(siteDetailDto, profitShare);
        }

        /// <summary>
        /// Preloads historical profit share data for multiple sites for an entire year to optimize performance
        /// </summary>
        public async Task PreloadProfitShareDataAsync(List<string> siteNumbers, int year)
        {
            if (siteNumbers == null || !siteNumbers.Any())
                return;

            if (_historicalProfitShareCache.ContainsKey(year))
                return;


            try
            {
                var repository = GetProfitShareRepository();
                var profitShareData = await repository.GetProfitSharesBatchAsync(siteNumbers, year);
                _historicalProfitShareCache[year] = profitShareData;
            }
            catch (Exception ex)
            {_historicalProfitShareCache[year] = new Dictionary<string, List<bs_ProfitShareByPercentage>>();
            }
        }

        public Task AggregateMonthlyTotalsAsync(
            List<SiteMonthlyRevenueDetailDto> siteDetailsForMonth,
            MonthValueDto monthValueDto)
        {
            decimal totalProfitShare = 0;
            decimal totalManagementAgreement = 0;

            foreach (var siteDetail in siteDetailsForMonth)
            {
                if (siteDetail.InternalRevenueBreakdown?.ManagementAgreement != null)
                {
                    if (siteDetail.InternalRevenueBreakdown.ManagementAgreement.Total.HasValue)
                        totalManagementAgreement += siteDetail.InternalRevenueBreakdown.ManagementAgreement.Total.Value;

                    var profitShareComponent = siteDetail.InternalRevenueBreakdown.ManagementAgreement.Components?
                        .FirstOrDefault(c => c.Name == "Profit Share");

                    if (profitShareComponent?.Value.HasValue == true)
                        totalProfitShare += profitShareComponent.Value.Value;
                }
            }

            monthValueDto.InternalRevenueBreakdown ??= new InternalRevenueBreakdownDto();
            monthValueDto.InternalRevenueBreakdown.ManagementAgreement ??= new ManagementAgreementInternalRevenueDto
            {
                Components = new List<ManagementAgreementComponentDto>()
            };

            monthValueDto.InternalRevenueBreakdown.ManagementAgreement.Components
                .RemoveAll(c => c.Name == "Profit Share");

            monthValueDto.InternalRevenueBreakdown.ManagementAgreement.Components.Add(
                new ManagementAgreementComponentDto
                {
                    Name = "Profit Share",
                    Value = totalProfitShare
                });

            monthValueDto.InternalRevenueBreakdown.ManagementAgreement.Total = totalManagementAgreement;

            return Task.CompletedTask;
        }

        // Helper: sum CPE (Client Paid Expense) for the given site and month (returns negative or zero)
        private decimal GetCpeContraForMonth(InternalRevenueDataVo siteData, int year, int monthOneBased)
        {
            if (siteData?.OtherRevenues == null) return 0m;

            decimal total = 0m;
            foreach (var orVo in siteData.OtherRevenues)
            {
                if (orVo?.ForecastData == null) continue;

                foreach (var detail in orVo.ForecastData)
                {
                    if (!MatchesTargetMonth(detail.MonthYear, year, monthOneBased))
                        continue;

                    total += detail.ClientPaidExpense; // negative or zero
                }
            }
            return total;
        }

        // Helper: determine if a MonthYear string matches the target year/month
        private static bool MatchesTargetMonth(string? monthYear, int targetYear, int targetMonth)
        {
            if (string.IsNullOrWhiteSpace(monthYear)) return false;
            var s = monthYear.Trim();

            // Common explicit formats
            if (string.Equals(s, $"{targetYear}{targetMonth:D2}", StringComparison.OrdinalIgnoreCase)) return true;     // yyyyMM
            if (string.Equals(s, $"{targetYear}-{targetMonth:D2}", StringComparison.OrdinalIgnoreCase)) return true;    // yyyy-MM
            if (string.Equals(s, $"{targetMonth:D2}/{targetYear}", StringComparison.OrdinalIgnoreCase)) return true;    // MM/yyyy

            // Numeric yyyyMM
            if (s.All(char.IsDigit) && s.Length == 6)
            {
                if (int.TryParse(s.Substring(0, 4), out var y) &&
                    int.TryParse(s.Substring(4, 2), out var m))
                {
                    return y == targetYear && m == targetMonth;
                }
            }

            // Fallback: let DateTime parse
            if (DateTime.TryParse(s, out var dt))
                return dt.Year == targetYear && dt.Month == targetMonth;

            return false;
        }

        private decimal GetOtherExpensesForecast(List<PnlRowDto> budgetRows, int monthOneBased)
        {
            if (budgetRows == null || !budgetRows.Any())
                return 0;

            var otherExpenseRow = budgetRows.FirstOrDefault(r => r.ColumnName == "OtherExpense");
            if (otherExpenseRow == null)
                return 0;

            var monthValue = otherExpenseRow.MonthlyValues?.FirstOrDefault(m => m.Month == monthOneBased - 1);
            return monthValue?.Value ?? 0;
        }

        /// <summary>
        /// Gets the profit amount (not profit share) for a historical month.
        /// First checks the in-memory cache, then falls back to database.
        /// </summary>
        private async Task<decimal> GetHistoricalProfitAmountForMonth(string siteNumber, int year, int month)
        {
            // Check cache first
            if (_profitCacheBySiteNumber.TryGetValue((siteNumber, year, month), out var cachedProfit))
            {
                return cachedProfit;
            }

            // If not in cache, try to get from database
            try
            {
                List<bs_ProfitShareByPercentage> historicalData = null;

                if (_historicalProfitShareCache.TryGetValue(year, out var yearCache) &&
                    yearCache.TryGetValue(siteNumber, out var siteData))
                {
                    historicalData = siteData
                        .Where(d => d.bs_Month == month.ToString("D2"))
                        .ToList();
                }
                else
                {
                    var repository = GetProfitShareRepository();
                    historicalData = await repository.GetProfitSharesByDateRangeAsync(
                        siteNumber, year, month, month + 1);
                }

                if (historicalData != null && historicalData.Any())
                {
                    // Look for the unlimited tier to get the profit amount
                    var unlimitedRow = historicalData
                        .FirstOrDefault(d =>
                            d.bs_TierLimitAmount == null ||
                            d.bs_TierLimitAmount == "0" ||
                            d.bs_TierLimitAmount == decimal.Zero.ToString() ||
                            d.bs_TierLimitAmount.Contains("∞") ||
                            d.bs_TierLimitAmount.ToLower().Contains("unlimited"));

                    if (unlimitedRow != null && unlimitedRow.bs_ProfitAmount.HasValue)
                    {
                        return (decimal)unlimitedRow.bs_ProfitAmount.Value;
                    }
                }
            }
            catch (Exception ex)
            {}

            return 0;
        }

        private async Task<decimal?> GetHistoricalProfitShareForMonth(string siteNumber, int year, int month)
        {
            try
            {
                List<bs_ProfitShareByPercentage> historicalData = null;

                if (_historicalProfitShareCache.TryGetValue(year, out var yearCache) &&
                    yearCache.TryGetValue(siteNumber, out var siteData))
                {
                    historicalData = siteData
                        .Where(d => d.bs_Month == month.ToString("D2"))
                        .ToList();
                }
                else
                {
                    var repository = GetProfitShareRepository();
                    historicalData = await repository.GetProfitSharesByDateRangeAsync(
                        siteNumber, year, month, month + 1);
                }

                if (historicalData != null && historicalData.Any())
                {
                    var totalProfitShare = historicalData
                        .Where(d => d.bs_TotalDueToTownePark.HasValue)
                        .Sum(d => (decimal)d.bs_TotalDueToTownePark.Value);

                    return totalProfitShare;
                }
            }
            catch (Exception ex)
            {}

            return 0;
        }

        private ProfitShareTierVo FindApplicableTier(List<ProfitShareTierVo> tiers, decimal profit)
        {
            if (tiers == null || !tiers.Any())
                return null;

            var unlimitedTier = tiers
                .Where(t => t.Amount == 0)
                .FirstOrDefault();

            var regularTiers = tiers
                .Where(t => t.Amount > 0)
                .OrderBy(t => t.Amount)
                .ToList();

            if (!regularTiers.Any())
                return unlimitedTier;

            if (profit <= 0)
                return regularTiers.First();

            for (int i = 0; i < regularTiers.Count; i++)
            {
                var currentTier = regularTiers[i];

                if (i == regularTiers.Count - 1)
                {
                    if (profit < currentTier.Amount)
                        return i > 0 ? regularTiers[i - 1] : regularTiers[0];
                    else if (unlimitedTier != null)
                        return unlimitedTier;
                    else
                        return currentTier;
                }
                else
                {
                    var nextTier = regularTiers[i + 1];
                    if (profit >= currentTier.Amount && profit < nextTier.Amount)
                        return currentTier;
                }
            }

            return regularTiers.First();
        }

        private decimal ApplyEscalator(decimal baseAmount, bs_escalatortype? escalatorType, decimal escalatorValue)
        {
            if (!escalatorType.HasValue || escalatorValue == 0)
                return baseAmount;

            switch (escalatorType.Value)
            {
                case bs_escalatortype.Percentage:
                    return baseAmount * (1 + escalatorValue / 100m);

                case bs_escalatortype.FixedAmount:
                    return baseAmount + escalatorValue;

                default:
                    return baseAmount;
            }
        }

        private void EnsureDtoStructure(SiteMonthlyRevenueDetailDto siteDetailDto)
        {
            if (siteDetailDto.InternalRevenueBreakdown == null)
            {
                siteDetailDto.InternalRevenueBreakdown = new InternalRevenueBreakdownDto();
            }

            if (siteDetailDto.InternalRevenueBreakdown.ManagementAgreement == null)
            {
                siteDetailDto.InternalRevenueBreakdown.ManagementAgreement = new ManagementAgreementInternalRevenueDto
                {
                    Components = new List<ManagementAgreementComponentDto>(),
                    Total = 0
                };
            }

            if (siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Components == null)
            {
                siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Components = new List<ManagementAgreementComponentDto>();
            }
        }

        private void AddProfitShareComponent(SiteMonthlyRevenueDetailDto siteDetailDto, decimal profitShare)
        {
            siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Components
                .RemoveAll(c => c.Name == "Profit Share");

            siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Components.Add(
                new ManagementAgreementComponentDto
                {
                    Name = "Profit Share",
                    Value = profitShare
                });

            siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Total += profitShare;
        }

        private IProfitShareRepository GetProfitShareRepository()
        {
            if (_profitShareRepository == null)
            {
                _profitShareRepository = _serviceProvider.GetRequiredService<IProfitShareRepository>();
            }
            return _profitShareRepository;
        }

        public void ClearAllCaches()
        {
            _profitCache.Clear();
            _profitCacheBySiteNumber.Clear();
            _historicalMonthCache.Clear();
            _historicalProfitShareCache.Clear();
            _historicalProfitCalculationCache.Clear();
        }

        public void ClearProfitShareCache(int? year = null)
        {
            if (year.HasValue)
            {
                if (_historicalProfitShareCache.ContainsKey(year.Value))
                {
                    _historicalProfitShareCache.Remove(year.Value);
                }

                var keysToRemove = _profitCache.Keys
                    .Where(k => k.year == year.Value)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    _profitCache.Remove(key);
                }

                var siteNumberKeysToRemove = _profitCacheBySiteNumber.Keys
                    .Where(k => k.year == year.Value)
                    .ToList();

                foreach (var key in siteNumberKeysToRemove)
                {
                    _profitCacheBySiteNumber.Remove(key);
                }

                var historicalKeysToRemove = _historicalMonthCache.Keys
                    .Where(k => k.year == year.Value)
                    .ToList();

                foreach (var key in historicalKeysToRemove)
                {
                    _historicalMonthCache.Remove(key);
                }
            }
            else
            {
                _historicalProfitShareCache.Clear();
                _profitCache.Clear();
                _profitCacheBySiteNumber.Clear();
                _historicalMonthCache.Clear();
                _historicalProfitCalculationCache.Clear();
                }
        }
    }
}
