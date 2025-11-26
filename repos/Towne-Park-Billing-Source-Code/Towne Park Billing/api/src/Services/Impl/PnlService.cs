using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TownePark.Models.Vo; // For InternalRevenueDataVo
using TownePark.Data; // For IInternalRevenueRepository
using api.Services;
using api.Adapters;
using api.Models.Dto; // For IPnlService
using api.Services.Impl.Calculators; // Added for calculators
using api.Services.Impl.Builders;   // Added for builders
using api.Models.Vo; // For ParkingRateDataVo
using api.Adapters.Mappers; // For IInternalRevenueMapper
using api.Data; // For IPayrollRepository / IBillableExpenseRepository
using api.Data.Impl; // For PayrollRepository
using Microsoft.Extensions.Logging.Abstractions;
using TownePark;

namespace api.Services.Impl
{
    public class PnlService : IPnlService
    {
        private readonly IInternalRevenueRepository _internalRevenueRepository;
        private readonly IPnlServiceAdapter _pnlServiceAdapter;
        private readonly ISiteStatisticRepository _siteStatisticRepository;
        private readonly List<IInternalRevenueCalculator> _internalRevenueCalculators;
        private readonly List<IExternalRevenueCalculator> _externalRevenueCalculators;
        private readonly List<IManagementAgreementCalculator> _managementAgreementCalculators;
        private readonly IPayrollRepository _payrollRepository;
        private readonly IInternalRevenueMapper _internalRevenueMapper;
        private readonly IBillableExpenseRepository _billableExpenseRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IPtebForecastCalculator _ptebForecastCalculator;
        private readonly Microsoft.Extensions.Logging.ILogger<PnlService> _logger;
        private readonly IInsuranceRowCalculator _insuranceRowCalculator;
        // Removed row-level Insurance calculator dependency to reduce coupling in this service
        private readonly IOtherExpenseRepository _otherExpenseRepository;
        private readonly Dictionary<(Guid siteId, int year), List<bs_OtherExpenseDetail>> _otherExpenseYearCache = new();

        public PnlService(
            IInternalRevenueRepository internalRevenueRepository,
            IPnlServiceAdapter pnlServiceAdapter,
            IEnumerable<IInternalRevenueCalculator> internalRevenueCalculators,
            IEnumerable<IExternalRevenueCalculator> externalRevenueCalculators,
            IEnumerable<IManagementAgreementCalculator> managementAgreementCalculators,
            IPayrollRepository payrollRepository,
            IInternalRevenueMapper internalRevenueMapper,
            IBillableExpenseRepository billableExpenseRepository,
            ISiteStatisticRepository siteStatisticRepository,
            ICustomerRepository customerRepository,
            IOtherExpenseRepository otherExpenseRepository,
            Microsoft.Extensions.Logging.ILogger<PnlService> logger,
            IPtebForecastCalculator ptebForecastCalculator,
            IInsuranceRowCalculator insuranceRowCalculator)
        {
            _internalRevenueRepository = internalRevenueRepository;
            _pnlServiceAdapter = pnlServiceAdapter;
            _internalRevenueCalculators = internalRevenueCalculators.ToList();
            _externalRevenueCalculators = externalRevenueCalculators.ToList();
            _managementAgreementCalculators = managementAgreementCalculators.OrderBy(c => c.Order).ToList();
            _payrollRepository = payrollRepository;
            _internalRevenueMapper = internalRevenueMapper;
            _billableExpenseRepository = billableExpenseRepository;
            _siteStatisticRepository = siteStatisticRepository;
            _customerRepository = customerRepository;
            _otherExpenseRepository = otherExpenseRepository;
            _logger = logger;
            _ptebForecastCalculator = ptebForecastCalculator;
            _insuranceRowCalculator = insuranceRowCalculator;
        }
        private InternalRevenueCurrentMonthSplitDto ComputeInternalRevenueSplit(SiteMonthlyRevenueDetailDto siteDetail, int year, int monthOneBased)
        {
            var b = siteDetail.InternalRevenueBreakdown;
            decimal actual = 0m, forecast = 0m;

            // FixedFee: forecast-only in phase 1
            forecast += b?.FixedFee?.Total ?? 0m;

            // PerOccupiedRoom
            if (b?.PerOccupiedRoom != null)
            {
                var por = b.PerOccupiedRoom;
                var porActual = por.ActualAmount ?? 0m;
                var porForecast = (por.Total ?? 0m) - porActual;
                if (porForecast < 0m) porForecast = 0m;
                actual += porActual;
                forecast += porForecast;
            }

            // PerLaborHour (include remainder of total as forecast)
            if (b?.PerLaborHour != null)
            {
                var plhActual = b.PerLaborHour.ActualPerLaborHour ?? 0m;
                var plhForecast = b.PerLaborHour.ForecastedPerLaborHour ?? 0m;
                var plhTotal = b.PerLaborHour.Total ?? (plhActual + plhForecast);
                var plhRemainder = plhTotal - (plhActual + plhForecast);
                if (plhRemainder < 0m) plhRemainder = 0m;
                actual += plhActual;
                forecast += plhForecast + plhRemainder;
            }

            // RevenueShare: use split shares
            if (b?.RevenueShare != null)
            {
                actual += b.RevenueShare.ActualShareAmount ?? 0m;
                forecast += b.RevenueShare.ForecastedShareAmount ?? 0m;
            }

            // OtherRevenue: excluded from split in phase 1 to match CalculatedTotalInternalRevenue aggregation

            // BillableAccounts subcomponents
            if (b?.BillableAccounts != null)
            {
                var bill = b.BillableAccounts;
                var subTotals = 0m;
                if (bill.Pteb != null)
                {
                    var ptebActual = bill.Pteb.ActualPteb;
                    var ptebForecast = bill.Pteb.ForecastedPteb;
                    var ptebTotal = bill.Pteb.Total ?? 0m;
                    // If explicit split values are provided, use them
                    if ((ptebActual ?? 0m) > 0m || (ptebForecast ?? 0m) > 0m)
                    {
                        actual += ptebActual ?? 0m;
                        forecast += ptebForecast ?? 0m;
                    }
                    else if (ptebTotal > 0m)
                    {
                        // Fallback: when no explicit split exists but we know the total
                        // If the configuration uses actuals (monthly), treat the total as actual for split purposes
                        // to ensure Actual + Forecast equals the displayed month total.
                        var calcType = bill.Pteb.CalculationType;
                        if (!string.IsNullOrWhiteSpace(calcType) && calcType.Equals("Actual", StringComparison.OrdinalIgnoreCase))
                        {
                            actual += ptebTotal;
                        }
                        else
                        {
                            // Otherwise, conservatively treat as forecast
                            forecast += ptebTotal;
                        }
                    }
                    subTotals += ptebTotal;
                }
                if (bill.SupportServices != null)
                {
                    actual += bill.SupportServices.ActualSupportServices ?? 0m;
                    forecast += bill.SupportServices.ForecastedSupportServices ?? 0m;
                    subTotals += bill.SupportServices.Total ?? 0m;
                }
                if (bill.ExpenseAccounts != null)
                {
                    actual += bill.ExpenseAccounts.ActualExpenseAccounts ?? 0m;
                    forecast += bill.ExpenseAccounts.ForecastedExpenseAccounts ?? 0m;
                    subTotals += bill.ExpenseAccounts.Total ?? 0m;
                }
                // AdditionalPayrollAmount and any remainder as forecast
                var known = subTotals + (bill.AdditionalPayrollAmount ?? 0m);
                var total = bill.Total ?? known;
                var remainder = total - known;
                if (remainder > 0m) forecast += remainder;
                forecast += bill.AdditionalPayrollAmount ?? 0m;
            }

            // ManagementAgreement: use Insurance split, rest as forecast
            if (b?.ManagementAgreement != null)
            {
                var ma = b.ManagementAgreement;
                actual += ma.ActualInsurance ?? 0m;
                var maKnown = (ma.ActualInsurance ?? 0m) + (ma.ForecastedInsurance ?? 0m);
                forecast += ma.ForecastedInsurance ?? 0m;
                var maTotal = ma.Total ?? maKnown;
                var maRemainder = maTotal - maKnown;
                if (maRemainder > 0m) forecast += maRemainder;
            }

            // Determine the display last-actual date:
            // - Prefer the earliest date within the current month across all relevant sources
            DateTime? lastActual = null;
            var candidateDates = new List<DateTime>();

            void AddCandidate(DateTime? d)
            {
                if (d.HasValue)
                {
                    candidateDates.Add(d.Value.Date);
                }
            }

            // Component-level last actual dates
            AddCandidate(b?.PerOccupiedRoom?.LastActualDate);
            AddCandidate(b?.PerLaborHour?.LastActualDate);
            AddCandidate(b?.BillableAccounts?.Pteb?.LastActualDate);
            AddCandidate(b?.BillableAccounts?.SupportServices?.LastActualDate);
            AddCandidate(b?.BillableAccounts?.ExpenseAccounts?.LastActualDate);

            // External revenue last-actual date (needed for rev share and overall data freshness)
            AddCandidate(siteDetail.ExternalRevenueBreakdown?.LastActualRevenueDate);

            // Fallback: internal actuals service date if available
            var lastActualStr = siteDetail.InternalActuals?.LastActualizedDate;
            if (!string.IsNullOrWhiteSpace(lastActualStr) && DateTime.TryParse(lastActualStr, out var parsed))
            {
                AddCandidate(parsed);
            }

            if (candidateDates.Count > 0)
            {
                var monthStart = new DateTime(year, monthOneBased, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                var anyInCurrentMonth = candidateDates.Any(d => d >= monthStart && d <= monthEnd);
                if (anyInCurrentMonth)
                {
                    // At least one data source has current-month actuals: show the earliest across ALL sources
                    lastActual = candidateDates.Min();
                }
                else
                {
                    // No current-month actuals in any source: show the last day of last month
                    lastActual = monthStart.AddDays(-1);
                    
                }
            }

            var splitDto = new InternalRevenueCurrentMonthSplitDto
            {
                Actual = actual,
                Forecast = forecast,
                LastActualDate = lastActual,
                ForecastStartDate = lastActual.HasValue ? lastActual.Value.AddDays(1) : new DateTime(year, monthOneBased, 1)
            };
            return splitDto;
        }

        public async Task<PnlResponseDto> GetPnlInternalRevenueDataAsync(List<string> siteIds, int year)
        {
            // Kick off independent I/O in parallel where possible
            var siteIdsString = string.Join(",", siteIds);
            var currentMonth = DateTime.Today.Month; // Use DateTime.Today instead of DateTime.Now

            var internalRevenueDataTask = _internalRevenueRepository.GetInternalRevenueDataAsync(siteIds, year);
            var internalRevenueActualsTask = _internalRevenueRepository.GetInternalRevenueActualsMultiSiteAsync(siteIdsString, year, currentMonth);
            var pnlDataTask = _pnlServiceAdapter.GetPnlDataAsync(siteIds, year); // budget data
            var priorYearPnlTask = _pnlServiceAdapter.GetPnlDataAsync(siteIds, year - 1);

            var allSitesRevenueData = await internalRevenueDataTask ?? new List<InternalRevenueDataVo>();

            // Attach parking rates to each site
            var siteGuids = allSitesRevenueData.Select(s => s.SiteId).ToList();

            // Dependent synchronous reads (now that we have siteGuids)
            var expenseActuals = _billableExpenseRepository.GetExpenseActualsForSites(siteGuids, year, currentMonth);

            // Start dependent async calls as soon as possible
            var yearlyPayrollBatchTask = _payrollRepository.GetPayrollBatchForYearAsync(siteGuids, year);

            var pnlResponse = await pnlDataTask; // budget data
            pnlResponse.ForecastRows ??= new List<PnlRowDto>();
            // Expose expense actuals internally for calculators
            pnlResponse.ExpenseActuals = expenseActuals;
            
            // Get internal revenue actuals for all sites in one call for current month
            var internalRevenueActuals = await internalRevenueActualsTask;
        
            // Store external revenue calculations for use in internal revenue calculations
            var externalRevenueCalculations = new Dictionary<(string siteId, int month), decimal>();

            // Process PTEB and Insurance in parallel per month below; keep only dependent rows here
            var forecastRowNamesToCalculate = new List<string> { "ExternalRevenue", "InternalRevenue" };
            // One-time prefetch of full payroll detail for all sites for all months (single call reused by PTEB + Management Fee)
            var yearlyPayrollBatch = await yearlyPayrollBatchTask;
            if (yearlyPayrollBatch == null)
            {
                yearlyPayrollBatch = new Dictionary<string, Dictionary<Guid, TownePark.bs_Payroll>>();
            }
            // Build forecasted sums per (site, month) from the same dataset
            var forecastedPayrollYearSums = new Dictionary<(Guid siteId, int monthOneBased), decimal>();
            foreach (var periodKvp in yearlyPayrollBatch)
            {
                if (periodKvp.Value == null) continue;
                var monthOneBased = int.Parse(periodKvp.Key.Substring(5, 2));
                foreach (var siteKvp in periodKvp.Value)
                {
                    var payroll = siteKvp.Value;
                    var details = payroll?.bs_PayrollDetail_Payroll;
                    if (details == null) continue;
                    decimal sum = 0m;
                    foreach (var d in details)
                    {
                        if (d.bs_Date.HasValue && d.bs_Date.Value.Month == monthOneBased)
                        {
                            sum += d.bs_ForecastPayrollCost ?? 0m;
                        }
                    }
                    forecastedPayrollYearSums[(siteKvp.Key, monthOneBased)] = sum;
                }
            }

            // Overwrite Payroll forecast site/month cells only when we have forecast data (non-zero).
            // Otherwise fallback to Budget site/month value if available, or preserve any existing item.
            var payrollForecastRow = GetOrInitializeForecastRow(pnlResponse, "Payroll");
            var budgetPayrollRow = pnlResponse.BudgetRows?.FirstOrDefault(r => r.ColumnName == "Payroll");
            decimal rowTotalSum = 0m;
            var payrollNow = DateTime.Today; // unique name to avoid collision

            for (int monthZeroBased = 0; monthZeroBased < 12; monthZeroBased++)
            {
                int monthOneBased = monthZeroBased + 1;
                var monthValueDto = payrollForecastRow.MonthlyValues.First(mv => mv.Month == monthZeroBased);

                // ensure SiteDetails list exists so we can merge with adapter-provided entries
                monthValueDto.SiteDetails ??= new List<SiteMonthlyRevenueDetailDto>();

                // budget month container (may be null)
                var budgetMonthValue = budgetPayrollRow?.MonthlyValues?.FirstOrDefault(mv => mv.Month == monthZeroBased);

                foreach (var siteData in allSitesRevenueData)
                {
                    var key = (siteData.SiteId, monthOneBased);

                    // Find or create the site detail in the forecast row
                    var existing = monthValueDto.SiteDetails.FirstOrDefault(sd => sd.SiteId == siteData.SiteNumber);
                    if (existing == null)
                    {
                        existing = new SiteMonthlyRevenueDetailDto { SiteId = siteData.SiteNumber };
                        monthValueDto.SiteDetails.Add(existing);
                    }

                    // If there's forecast data for this site/month, update that site detail; otherwise try budget fallback
                    if (forecastedPayrollYearSums.TryGetValue(key, out var forecastVal) && forecastVal != 0m)
                    {
                        existing.Value = forecastVal;
                        existing.IsForecast = true;
                    }
                    else
                    {
                        // No payroll forecast data found for this site/month: prefer Budget cell (if present), else preserve existing value
                        var budgetSiteVal = budgetMonthValue?.SiteDetails?.FirstOrDefault(sd => sd.SiteId == siteData.SiteNumber)?.Value;
                        if (budgetSiteVal.HasValue)
                        {
                            existing.Value = budgetSiteVal.Value;
                            existing.IsForecast = false;
                        }
                        // else leave existing.Value as-is (may be null or previously set by adapter)
                    }
                }

                // Update the month aggregate value to total of site details
                monthValueDto.Value = monthValueDto.SiteDetails?.Sum(sd => sd.Value ?? 0m) ?? 0m;
                rowTotalSum += monthValueDto.Value ?? 0;
            }

            // Set the row-level total to the sum of month values (use reflection in case PnlRowDto doesn't expose a direct property)
            var valueProp = payrollForecastRow.GetType().GetProperty("Total");
            if (valueProp != null && valueProp.CanWrite && valueProp.PropertyType == typeof(decimal?))
            {
                valueProp.SetValue(payrollForecastRow, rowTotalSum);
            }



            // Load the yearly payroll batch into ManagementFeeCalculator's internal cache
            foreach (var calculator in _managementAgreementCalculators.OfType<ManagementFeeCalculator>())
            {
                calculator.LoadPayrollCacheForYear(yearlyPayrollBatch);
            }

            bool isSingleSiteRequest = siteIds != null && siteIds.Count == 1;

            // Prefetch yearly 7082 budgets once for the Insurance row
            var vehicle7082YearMap = _billableExpenseRepository.GetVehicleInsuranceBudgetForSitesForYear(siteGuids, year)
                ?? new Dictionary<(Guid siteId, int monthOneBased), decimal>();

            // Pre-create rows needed by parallel computations
            GetOrInitializeForecastRow(pnlResponse, "Pteb");
            GetOrInitializeForecastRow(pnlResponse, "Insurance");
            GetOrInitializeActualRow(pnlResponse, "Insurance");

            // Compute PTEB and Insurance per month in parallel (they do not depend on each other)
            for (int monthZeroBased = 0; monthZeroBased < 12; monthZeroBased++)
            {
                int monthOneBased = monthZeroBased + 1;

                // Ensure month slots are initialized for both rows
                var ptebRow = pnlResponse.ForecastRows.FirstOrDefault(r => r.ColumnName == "Pteb") ?? GetOrInitializeForecastRow(pnlResponse, "Pteb");
                var insRow = pnlResponse.ForecastRows.FirstOrDefault(r => r.ColumnName == "Insurance") ?? GetOrInitializeForecastRow(pnlResponse, "Insurance");
                var ptebMonthValueDto = ptebRow.MonthlyValues.First(mv => mv.Month == monthZeroBased);
                var insMonthValueDto = insRow.MonthlyValues.First(mv => mv.Month == monthZeroBased);
                InitializeMonthValue("Pteb", ptebMonthValueDto);
                InitializeMonthValue("Insurance", insMonthValueDto);

                // Build empty prior-year rate map; calculator will fallback to defaults as needed
                var priorYearRateBySite = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

                // Attach per-site 7082 budget to insurance site details for this month
                insMonthValueDto.SiteDetails ??= new List<SiteMonthlyRevenueDetailDto>();
                foreach (var s in allSitesRevenueData)
                {
                    var sd = insMonthValueDto.SiteDetails.FirstOrDefault(d => d.SiteId == s.SiteNumber);
                    if (sd == null) { sd = new SiteMonthlyRevenueDetailDto { SiteId = s.SiteNumber }; insMonthValueDto.SiteDetails.Add(sd); }
                    sd.InsuranceBreakdown ??= new InsuranceBreakdownDto();
                    if (vehicle7082YearMap.TryGetValue((s.SiteId, monthOneBased), out var vi))
                    {
                        sd.InsuranceBreakdown.VehicleInsurance7082 = vi;
                    }
                }

                // Build forecasted payroll by site (forecast-only as per US 2660)
                var forecastedPayrollBySite = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                foreach (var siteData in allSitesRevenueData)
                {
                    var key = (siteData.SiteId, monthOneBased);
                    var sum = forecastedPayrollYearSums.TryGetValue(key, out var s) ? s : 0m;
                    forecastedPayrollBySite[siteData.SiteNumber] = sum;
                }

                // Run both computations concurrently
                var ptebTask = Task.Run(() =>
                    _ptebForecastCalculator.ComputeForMonth(
                        pnlResponse,
                        allSitesRevenueData,
                        year,
                        monthOneBased,
                        monthZeroBased,
                        forecastedPayrollBySite,
                        priorYearRateBySite));

                var insuranceTask = Task.Run(() =>
                    _insuranceRowCalculator.ComputeForMonth(
                        pnlResponse,
                        allSitesRevenueData,
                        year,
                        monthOneBased,
                        monthZeroBased,
                        forecastedPayrollBySite));

                await Task.WhenAll(ptebTask, insuranceTask);

                // Ensure insurance month aggregate reflects sum of site details
                insMonthValueDto.Value = insMonthValueDto.SiteDetails != null
                    ? insMonthValueDto.SiteDetails.Sum(sd => sd.Value ?? 0m)
                    : 0m;

                // Only expose insurance breakdown for current month to reduce payload
                if (!(year == DateTime.Today.Year && monthOneBased == DateTime.Today.Month) && insMonthValueDto.SiteDetails != null)
                {
                    foreach (var sd in insMonthValueDto.SiteDetails)
                    {
                        sd.InsuranceBreakdown = null;
                    }
                }
            }

            foreach (var rowName in forecastRowNamesToCalculate)
            {
                var row = GetOrInitializeForecastRow(pnlResponse, rowName);
                // Prefetch prior-year PnL once if needed for PTEB
                PnlResponseDto? priorYearPnl = null;
                if (rowName == "Pteb")
                {
                    try
                    {
                        priorYearPnl = await priorYearPnlTask;
                    }
                    catch { priorYearPnl = null; }
                }

                for (int monthZeroBased = 0; monthZeroBased < 12; monthZeroBased++)
                {
                    int monthOneBased = monthZeroBased + 1;
                    var monthValueDto = row.MonthlyValues.First(mv => mv.Month == monthZeroBased);
                    InitializeMonthValue(rowName, monthValueDto);

                    // Handle PTEB as its own row; compute and continue without site-level per-row calculators
                    if (rowName == "Pteb")
                    {
                        var forecastedPayrollBySite = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                        foreach (var siteData in allSitesRevenueData)
                        {
                            var key = (siteData.SiteId, monthOneBased);
                            var sum = forecastedPayrollYearSums.TryGetValue(key, out var s) ? s : 0m;

                            // Use monthly forecasted payroll only for all months (including current),
                            // per US 2660 requirements (do not mix daily actuals into the base).

                            forecastedPayrollBySite[siteData.SiteNumber] = sum;
                        }
                        // Build prior-year rate map per site for this month from budget rows (Pteb and Payroll of prior year)
                        var priorYearRateBySite = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                        var priorBudgetPteb = priorYearPnl?.BudgetRows?.FirstOrDefault(r => r.ColumnName == "Pteb");
                        var priorBudgetPayroll = priorYearPnl?.BudgetRows?.FirstOrDefault(r => r.ColumnName == "Payroll");
                        foreach (var siteData in allSitesRevenueData)
                        {
                            var pyMonth = monthZeroBased;
                            var ptebVal = priorBudgetPteb?.MonthlyValues?.FirstOrDefault(mv => mv.Month == pyMonth)?.SiteDetails?.FirstOrDefault(sd => sd.SiteId == siteData.SiteNumber)?.Value ?? 0m;
                            var payrollVal = priorBudgetPayroll?.MonthlyValues?.FirstOrDefault(mv => mv.Month == pyMonth)?.SiteDetails?.FirstOrDefault(sd => sd.SiteId == siteData.SiteNumber)?.Value ?? 0m;
                            if (ptebVal > 0m && payrollVal > 0m)
                            {
                                priorYearRateBySite[siteData.SiteNumber] = (decimal)ptebVal / (decimal)payrollVal;
                            }
                        }
                        _ptebForecastCalculator.ComputeForMonth(
                            pnlResponse,
                            allSitesRevenueData,
                            year,
                            monthOneBased,
                            monthZeroBased,
                            forecastedPayrollBySite,
                            priorYearRateBySite);
                        continue;
                    }

                    if (rowName == "Insurance")
                    {
                        // Ensure ActualRows structure exists for Insurance to allow later logic to read actual site details
                        GetOrInitializeActualRow(pnlResponse, rowName);
                        // Attach per-site 7082 budget to site details in forecast row for this month to avoid direct repo calls in calculators
                        var insMonth = monthValueDto; // use current month's DTO; do not reinitialize the row to avoid wiping previous months
                        insMonth.SiteDetails ??= new List<SiteMonthlyRevenueDetailDto>();
                        foreach (var s in allSitesRevenueData)
                        {
                            var sd = insMonth.SiteDetails.FirstOrDefault(d => d.SiteId == s.SiteNumber);
                            if (sd == null) { sd = new SiteMonthlyRevenueDetailDto { SiteId = s.SiteNumber }; insMonth.SiteDetails.Add(sd); }
                            // Stash vehicle insurance in InsuranceBreakdown for reuse by row-level calculator
                            sd.InsuranceBreakdown ??= new InsuranceBreakdownDto();
                            if (vehicle7082YearMap.TryGetValue((s.SiteId, monthOneBased), out var vi))
                            {
                                sd.InsuranceBreakdown.VehicleInsurance7082 = vi;
                            }
                        }

                        // Build forecasted payroll by site for the month (actual-to-date + forecast remainder for current month)
                        var forecastedPayrollBySite = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                        foreach (var siteData in allSitesRevenueData)
                        {
                            var key = (siteData.SiteId, monthOneBased);
                            var sum = forecastedPayrollYearSums.TryGetValue(key, out var s) ? s : 0m;

                            // Use monthly forecasted payroll only for all months (including current),
                            // per US 2660 requirements (do not mix daily actuals into the base).
                            forecastedPayrollBySite[siteData.SiteNumber] = sum;
                        }

                        _insuranceRowCalculator.ComputeForMonth(
                            pnlResponse,
                            allSitesRevenueData,
                            year,
                            monthOneBased,
                            monthZeroBased,
                            forecastedPayrollBySite);

                        // Ensure month aggregate value reflects the sum of site details after calculation
                        insMonth.Value = insMonth.SiteDetails != null
                            ? insMonth.SiteDetails.Sum(sd => sd.Value ?? 0m)
                            : 0m;

                        // Only expose insurance breakdown for the current processing month to reduce payload size
                        if (!(year == DateTime.Today.Year && monthOneBased == DateTime.Today.Month) && insMonth.SiteDetails != null)
                        {
                            foreach (var sd in insMonth.SiteDetails)
                            {
                                sd.InsuranceBreakdown = null;
                            }
                        }

                        continue;
                    }

                    // Pre-load profit share data for all sites for the entire year (only once per year)
                    if (rowName == "InternalRevenue" && _managementAgreementCalculators.Any() && monthOneBased == 1)
                    {
                        var preloadTasks = _managementAgreementCalculators
                            .OfType<ProfitShareCalculator>()
                            .Select(c => c.PreloadProfitShareDataAsync(siteIds, year));
                        await Task.WhenAll(preloadTasks);
                    }

                    decimal currentMonthTotal = 0m;
                    // Determine if this iteration is the current month
                    var isCurrentProcessingMonth = (year == DateTime.Today.Year && monthOneBased == DateTime.Today.Month);

                    foreach (var siteData in allSitesRevenueData)
                    {
                        var siteDetailDto = new SiteMonthlyRevenueDetailDto { SiteId = siteData.SiteNumber };
                        // Attach expense actuals for current month so UI/calculators can use them
                        if (isCurrentProcessingMonth && expenseActuals != null)
                        {
                            var match = expenseActuals.FirstOrDefault(e => e.SiteId == siteData.SiteId);
                            if (match != null)
                            {
                                siteDetailDto.ExpenseActuals = new ExpenseActualsDto
                                {
                                    BillableExpenseActuals = match.BillableExpenseActuals,
                                    OtherExpenseActuals = match.OtherExpenseActuals
                                };
                            }
                        }

                        // Attach current-month internal revenue actuals for this site (if available)
                        if (isCurrentProcessingMonth)
                        {
                            var siteActuals = internalRevenueActuals?.SiteResults
                                ?.FirstOrDefault(sr => string.Equals(sr.SiteId, siteData.SiteNumber, StringComparison.OrdinalIgnoreCase));
                            siteDetailDto.InternalActuals = siteActuals;

                            // Attach current-month payroll breakdown (actual-to-date and last-actual date) from PnL ActualRows: Payroll
                            try
                            {
                                var payrollActualRow = pnlResponse.ActualRows?.FirstOrDefault(r => r.ColumnName == "Payroll");
                                var payrollMonthValue = payrollActualRow?.MonthlyValues?.FirstOrDefault(mv => mv.Month == monthZeroBased);
                                var payrollSiteDetail = payrollMonthValue?.SiteDetails?.FirstOrDefault(sd => sd.SiteId == siteData.SiteNumber);
                                if (payrollSiteDetail != null)
                                {
                                    // Build a breakdown carrying actual-to-date payroll and cutoff for calculators
                                    var bd = payrollSiteDetail.PayrollBreakdown;
                                    var actualPayroll = payrollSiteDetail.Value ?? 0m; // treat the actual row site value as actual-to-date

                                    // Compute forecasted payroll remainder for days after cutoff within the month
                                    decimal forecastRemainder = 0m;
                                    var cutoff = bd?.ActualPayrollLastDate;
                                    try
                                    {
                                        var billingPeriod = $"{year:D4}-{monthOneBased:D2}";
                                        var payrollEntity = _payrollRepository.GetPayroll(siteData.SiteId, billingPeriod);
                                        var rows = payrollEntity?.bs_PayrollDetail_Payroll ?? Enumerable.Empty<bs_PayrollDetail>();
                                        foreach (var d in rows)
                                        {
                                            var dt = d.bs_Date;
                                            if (!dt.HasValue) continue;
                                            if (dt.Value.Year != year || dt.Value.Month != monthOneBased) continue;
                                            if (cutoff.HasValue && dt.Value.Date <= cutoff.Value.Date) continue;
                                            if (d.bs_ForecastPayrollCost.HasValue)
                                                forecastRemainder += d.bs_ForecastPayrollCost.Value;
                                        }
                                    }
                                    catch { /* best-effort */ }

                                    siteDetailDto.PayrollBreakdown = new PayrollBreakdownDto
                                    {
                                        ActualPayroll = actualPayroll,
                                        ActualPayrollLastDate = bd?.ActualPayrollLastDate,
                                        TotalPayroll = bd?.TotalPayroll,
                                        ForecastedPayroll = forecastRemainder
                                    };
                                }
                            }
                            catch { /* best-effort; calculators will fallback gracefully */ }
                        }

                        // Get the calculated external revenue for this site/month if available
                        decimal? calculatedExternalRevenue = null;
                        if (rowName == "InternalRevenue" && externalRevenueCalculations.TryGetValue((siteData.SiteNumber, monthOneBased), out var extRev))
                        {
                            calculatedExternalRevenue = extRev;
                        }

                        decimal siteMonthlyValue = await CalculateSiteDetail(rowName, siteData, year, monthOneBased, monthValueDto, siteDetailDto, pnlResponse, monthZeroBased, calculatedExternalRevenue);
                        siteDetailDto.Value = siteMonthlyValue;
                        monthValueDto.SiteDetails.Add(siteDetailDto);
                        currentMonthTotal += siteMonthlyValue;

                        // Store external revenue calculations for use in internal revenue calculations
                        if (rowName == "ExternalRevenue" && siteDetailDto.ExternalRevenueBreakdown?.CalculatedTotalExternalRevenue.HasValue == true)
                        {
                            externalRevenueCalculations[(siteData.SiteNumber, monthOneBased)] = siteDetailDto.ExternalRevenueBreakdown.CalculatedTotalExternalRevenue.Value;
                        }

                        // Populate internal revenue actual/forecast split only for single-site current month
                        if (rowName == "InternalRevenue" && isSingleSiteRequest && isCurrentProcessingMonth)
                        {
                            // Surface the external revenue breakdown (and its last-actual date) for this site/month,
                            // so the split can consider it when determining the overall last-actual date.
                            var extRow = pnlResponse.ForecastRows?.FirstOrDefault(r => r.ColumnName == "ExternalRevenue");
                            var extMonth = extRow?.MonthlyValues?.FirstOrDefault(mv => mv.Month == monthZeroBased);
                            var extSiteDetail = extMonth?.SiteDetails?.FirstOrDefault(sd => sd.SiteId == siteData.SiteNumber);
                            if (extSiteDetail?.ExternalRevenueBreakdown != null)
                            {
                                siteDetailDto.ExternalRevenueBreakdown = extSiteDetail.ExternalRevenueBreakdown;
                            }

                            var split = ComputeInternalRevenueSplit(siteDetailDto, year, monthOneBased);
                            siteDetailDto.InternalRevenueCurrentMonthSplit = split;
                            // Month-level split mirrors site split for single-site
                            monthValueDto.InternalRevenueCurrentMonthSplit = split;
                        }
                    }
                    await AggregateMonthlyTotalsAsync(rowName, monthValueDto);
                    // Set month value from aggregate CalculatedTotalInternalRevenue if available
                    if (rowName == "InternalRevenue" && monthValueDto.InternalRevenueBreakdown?.CalculatedTotalInternalRevenue != null)
                        monthValueDto.Value = monthValueDto.InternalRevenueBreakdown.CalculatedTotalInternalRevenue.Value;
                    else
                        monthValueDto.Value = currentMonthTotal;

                    // After aggregation, mirror split and combined month total into ActualRows for current month InternalRevenue
                    if (rowName == "InternalRevenue" && isCurrentProcessingMonth)
                    {
                        var actualRow = GetOrInitializeActualRow(pnlResponse, rowName);
                        var actualMonthValue = actualRow.MonthlyValues.First(mv => mv.Month == monthZeroBased);
                        actualMonthValue.InternalRevenueBreakdown ??= new InternalRevenueBreakdownDto();

                        // Prefer the calculated total from the month-level breakdown when available
                        if (monthValueDto.InternalRevenueBreakdown?.CalculatedTotalInternalRevenue != null)
                        {
                            actualMonthValue.InternalRevenueBreakdown.CalculatedTotalInternalRevenue = monthValueDto.InternalRevenueBreakdown.CalculatedTotalInternalRevenue;
                        }

                        // Always set the actual row's value to the month total (actual-to-date + forecast remainder)
                        actualMonthValue.Value = monthValueDto.Value;

                        if (monthValueDto.InternalRevenueCurrentMonthSplit != null)
                        {
                            actualMonthValue.InternalRevenueCurrentMonthSplit = monthValueDto.InternalRevenueCurrentMonthSplit;
                        }
                    }

                    // No additional step here; PTEB is now processed in its own row pass
                }
            }
            if (isSingleSiteRequest)
            {
                PopulateCurrentMonthExternalRevenueActuals(pnlResponse);
                PopulateCurrentMonthPayrollActuals(pnlResponse);
            }

            // Calculate ParkingRents forecast row for each site/month
            var parkingRentsRow = GetOrInitializeForecastRow(pnlResponse, "ParkingRents");
            var expenseAccountCalculator = _internalRevenueCalculators.OfType<ExpenseAccountCalculator>().FirstOrDefault();
            if (expenseAccountCalculator != null)
            {
                // Cache all forecasted parking rents for all sites for the year
                var parkingRentsBySite = new Dictionary<string, Dictionary<int, decimal>>();
                foreach (var siteData in allSitesRevenueData)
                {
                    var forecastedDict = expenseAccountCalculator.GetForecastedParkingRents(siteData.SiteId, year);
                    parkingRentsBySite[siteData.SiteNumber] = forecastedDict;
                }

                for (int monthZeroBased = 0; monthZeroBased < 12; monthZeroBased++)
                {
                    int monthOneBased = monthZeroBased + 1;
                    var monthValueDto = parkingRentsRow.MonthlyValues.First(mv => mv.Month == monthZeroBased);
                    monthValueDto.SiteDetails = new List<SiteMonthlyRevenueDetailDto>();
                    decimal monthTotal = 0m;
                    foreach (var siteData in allSitesRevenueData)
                    {
                        decimal forecastedParkingRents = 0m;
                        if (parkingRentsBySite.TryGetValue(siteData.SiteNumber, out var siteDict))
                        {
                            siteDict.TryGetValue(monthOneBased, out forecastedParkingRents);
                        }

                        // If this is a future month and forecastedParkingRents is null or 0, fallback to budget value
                        var now = DateTime.Now;
                        bool isFutureMonth = (year > now.Year) || (year == now.Year && monthOneBased > now.Month);
                        if (isFutureMonth && (forecastedParkingRents == 0m))
                        {
                            var budgetRow = pnlResponse.BudgetRows?.FirstOrDefault(r => r.ColumnName == "ParkingRents");
                            var budgetMonthValue = budgetRow?.MonthlyValues?.FirstOrDefault(mv => mv.Month == monthZeroBased);
                            var budgetSiteDetail = budgetMonthValue?.SiteDetails?.FirstOrDefault(sd => sd.SiteId == siteData.SiteNumber);
                            if (budgetSiteDetail != null && budgetSiteDetail.Value.HasValue)
                            {
                                forecastedParkingRents = budgetSiteDetail.Value.Value;
                            }
                        }

                        var siteDetailDto = new SiteMonthlyRevenueDetailDto
                        {
                            SiteId = siteData.SiteNumber,
                            Value = forecastedParkingRents
                        };
                        monthValueDto.SiteDetails.Add(siteDetailDto);
                        monthTotal += forecastedParkingRents;
                    }
                    monthValueDto.Value = monthTotal;
                }
            }

            // Local helpers for Other Expense forecast (exclude claims 7100 and parking rents 7170)
            var otherExpenseYearCache = new Dictionary<(Guid siteId, int year), List<bs_OtherExpenseDetail>>();
            List<bs_OtherExpenseDetail>? GetYearlyOtherExpenseDetails(Guid siteId, int y)
            {
                var key = (siteId, y);
                if (otherExpenseYearCache.TryGetValue(key, out var cached)) return cached;
                var list = _otherExpenseRepository.GetOtherExpenseDetail(siteId, $"{y:D4}-01")?.ToList() ?? new List<bs_OtherExpenseDetail>();
                otherExpenseYearCache[key] = list;
                return list;
            }
            decimal SumIncludedOtherExpense(bs_OtherExpenseDetail d)
            {
                // Sum all standard Other Expense fields but exclude Loss & Damage (7100) and Rents - Parking (7170)
                return (d.bs_EmployeeRelations ?? 0m)
                    + (d.bs_FuelVehicles ?? 0m)
                    + (d.bs_OfficeSupplies ?? 0m)
                    + (d.bs_OutsideServices ?? 0m)
                    + (d.bs_RepairsAndMaintenance ?? 0m)
                    + (d.bs_RepairsAndMaintenanceVehicle ?? 0m)
                    + (d.bs_Signage ?? 0m)
                    + (d.bs_SuppliesAndEquipment ?? 0m)
                    + (d.bs_TicketsAndPrintedMaterial ?? 0m)
                    + (d.bs_Uniforms ?? 0m);
                    // Excluded: bs_LossAndDamageClaims (7100) and bs_RentsParking (7170)
            }
            decimal ComputeOtherExpenseForecast(Guid siteGuid, int y, int m)
            {
                var details = GetYearlyOtherExpenseDetails(siteGuid, y);
                var key = $"{y:D4}-{m:D2}";
                var rec = details.FirstOrDefault(e => e.bs_MonthYear == key);
                if (rec != null) return SumIncludedOtherExpense(rec);
                return _billableExpenseRepository.GetOtherExpenseBudget(siteGuid, y, m);
            }

            // Build OtherExpense rows: forecast for current and future months; current-month actuals from Account Summary
            var otherForecastRow = GetOrInitializeForecastRow(pnlResponse, "OtherExpense");
            var otherActualRow = GetOrInitializeActualRow(pnlResponse, "OtherExpense");

            for (int monthZeroBased = 0; monthZeroBased < 12; monthZeroBased++)
            {
                int monthOneBased = monthZeroBased + 1;
                bool isCurrentProcessingMonth = (year == DateTime.Today.Year && monthOneBased == DateTime.Today.Month);
                bool isCurrentOrFutureMonth = (year > DateTime.Today.Year) || 
                                            (year == DateTime.Today.Year && monthOneBased >= DateTime.Today.Month);

                // Populate forecast rows for current and future months
                if (isCurrentOrFutureMonth)
                {
                    var fMonth = otherForecastRow.MonthlyValues.First(mv => mv.Month == monthZeroBased);
                    InitializeMonthValue("OtherExpense", fMonth);
                    decimal monthForecastSum = 0m;

                    foreach (var s in allSitesRevenueData)
                    {
                        var forecastVal = ComputeOtherExpenseForecast(s.SiteId, year, monthOneBased);
                        fMonth.SiteDetails.Add(new SiteMonthlyRevenueDetailDto { SiteId = s.SiteNumber, Value = forecastVal });
                        monthForecastSum += forecastVal;
                    }
                    fMonth.Value = monthForecastSum;
                }

                // Populate actual rows for current month only with Account Summary actuals
                if (isCurrentProcessingMonth)
                {
                    var aMonth = otherActualRow.MonthlyValues.First(mv => mv.Month == monthZeroBased);
                    aMonth.SiteDetails ??= new List<SiteMonthlyRevenueDetailDto>();

                    foreach (var s in allSitesRevenueData)
                    {
                        var match = expenseActuals?.FirstOrDefault(e => e.SiteId == s.SiteId);
                        if (match?.OtherExpenseActuals != null)
                        {
                            var existing = aMonth.SiteDetails.FirstOrDefault(d => d.SiteId == s.SiteNumber);
                            if (existing == null)
                            {
                                existing = new SiteMonthlyRevenueDetailDto { SiteId = s.SiteNumber };
                                aMonth.SiteDetails.Add(existing);
                            }
                            existing.Value = match.OtherExpenseActuals;
                        }
                    }
                    aMonth.Value = aMonth.SiteDetails.Sum(sd => sd.Value ?? 0m);
                }
            }

            // Calculate variance rows after forecast rows and any current-month adjustments are applied
            CalculateVarianceRows(pnlResponse, year);

            return pnlResponse;
        }


        /// <summary>
        /// Calculates variance rows comparing actual vs budget for past/current months and forecast vs budget for future months.
        /// Month logic is primary; year guards handle non-current selected years.
        /// </summary>
        /// <param name="pnlResponse">The P&L response with populated actual, budget, and forecast rows</param>
        /// <param name="year">The year being analyzed</param>
        private void CalculateVarianceRows(PnlResponseDto pnlResponse, int year)
        {
            if (pnlResponse == null)
            {
                return;
            }

            int currentMonth = DateTime.UtcNow.Month; // 1..12
            int currentYear = DateTime.UtcNow.Year;

            string[] propertyNames =
            {
                "ExternalRevenue",
                "InternalRevenue",
                "Payroll",
                "Claims",
                "ParkingRents",
                "OtherExpense",
                "Pteb",
                "Insurance"
            };

            var varianceRows = new List<PnlVarianceRowDto>();

            foreach (var prop in propertyNames)
            {
                var monthlyVariances = new List<MonthVarianceDto>();
                decimal totalVarianceAmount = 0m;
                decimal totalBudget = 0m;
                decimal totalComparison = 0m;

                for (int monthZeroBased = 0; monthZeroBased < 12; monthZeroBased++)
                {
                    int targetMonthOneBased = monthZeroBased + 1;

                    var budgetRow = pnlResponse.BudgetRows?.FirstOrDefault(r => r.ColumnName == prop);
                    decimal budgetVal = budgetRow?.MonthlyValues?.FirstOrDefault(mv => mv.Month == monthZeroBased)?.Value ?? 0m;

                    bool useActual = DetermineUseActualForComparison(year, targetMonthOneBased, currentYear, currentMonth);
                    decimal comparisonVal = GetComparisonValue(pnlResponse, prop, monthZeroBased, useActual);

                    decimal varianceAmount = comparisonVal - budgetVal;
                    decimal? variancePercent = budgetVal != 0m ? (varianceAmount / budgetVal) * 100m : (comparisonVal != 0m ? 100m : 0m);

                    monthlyVariances.Add(new MonthVarianceDto
                    {
                        Month = monthZeroBased,
                        Amount = varianceAmount,
                        Percentage = variancePercent
                    });

                    totalVarianceAmount += varianceAmount;
                    totalBudget += budgetVal;
                    totalComparison += comparisonVal;
                }

                decimal? totalVariancePercent = totalBudget != 0m ? (totalVarianceAmount / totalBudget) * 100m : (totalComparison != 0m ? 100m : 0m);

                varianceRows.Add(new PnlVarianceRowDto
                {
                    ColumnName = prop,
                    MonthlyVariances = monthlyVariances,
                    TotalVarianceAmount = totalVarianceAmount,
                    TotalVariancePercent = totalVariancePercent,
                    Total = totalVarianceAmount
                });
            }

            pnlResponse.VarianceRows = varianceRows;
        }

        /// <summary>
        /// Determines whether to use actual or forecast data for variance comparison.
        /// If the selected year is the current year, months <= currentMonth use actual; future months use forecast.
        /// Past years use actual for all months; future years use forecast for all months.
        /// </summary>
        private bool DetermineUseActualForComparison(int targetYear, int targetMonth, int currentYear, int currentMonth)
        {
            if (targetYear < currentYear)
            {
                return true;
            }
            if (targetYear > currentYear)
            {
                return false;
            }
            return targetMonth <= currentMonth;
        }

        /// <summary>
        /// Returns the comparison value (actual or forecast) for a given property and month.
        /// </summary>
        private decimal GetComparisonValue(PnlResponseDto pnlResponse, string propertyName, int monthZeroBased, bool useActual)
        {
            if (useActual)
            {
                var actualRow = pnlResponse.ActualRows?.FirstOrDefault(r => r.ColumnName == propertyName);
                return actualRow?.MonthlyValues?.FirstOrDefault(mv => mv.Month == monthZeroBased)?.Value ?? 0m;
            }
            else
            {
                var forecastRow = pnlResponse.ForecastRows?.FirstOrDefault(r => r.ColumnName == propertyName);
                return forecastRow?.MonthlyValues?.FirstOrDefault(mv => mv.Month == monthZeroBased)?.Value ?? 0m;
            }
        }

        private PnlRowDto GetOrInitializeForecastRow(PnlResponseDto pnlResponse, string rowName)
        {
            var row = pnlResponse.ForecastRows.FirstOrDefault(r => r.ColumnName == rowName);
            if (row == null || row.MonthlyValues == null || row.MonthlyValues.Count != 12)
            {
                row = new PnlRowBuilder().WithColumnName(rowName).WithInitializedMonthlyValues(12).Build();
                var idx = pnlResponse.ForecastRows.FindIndex(r => r.ColumnName == rowName);
                if (idx >= 0) pnlResponse.ForecastRows[idx] = row; else pnlResponse.ForecastRows.Add(row);
            }
            else
            {
                foreach (var mv in row.MonthlyValues)
                {
                    mv.SiteDetails ??= new List<SiteMonthlyRevenueDetailDto>();
                    mv.Value = 0m;
                }
            }
            return row;
        }

        private PnlRowDto GetOrInitializeActualRow(PnlResponseDto pnlResponse, string rowName)
        {
            var row = pnlResponse.ActualRows.FirstOrDefault(r => r.ColumnName == rowName);
            if (row == null || row.MonthlyValues == null || row.MonthlyValues.Count != 12)
            {
                row = new PnlRowBuilder().WithColumnName(rowName).WithInitializedMonthlyValues(12).Build();
                var idx = pnlResponse.ActualRows.FindIndex(r => r.ColumnName == rowName);
                if (idx >= 0) pnlResponse.ActualRows[idx] = row; else pnlResponse.ActualRows.Add(row);
            }
            return row;
        }

        private void InitializeMonthValue(string rowName, MonthValueDto monthValueDto)
        {
            monthValueDto.Value = 0m;
            monthValueDto.SiteDetails = new List<SiteMonthlyRevenueDetailDto>();
            if (rowName == "InternalRevenue")
            {
                if (monthValueDto.InternalRevenueBreakdown == null)
                {
                    monthValueDto.InternalRevenueBreakdown = new InternalRevenueBreakdownDto();
                }

            }
            else if (rowName == "ExternalRevenue")
                monthValueDto.InternalRevenueBreakdown = null;
            else
                monthValueDto.InternalRevenueBreakdown = null;
        }

        private async Task<decimal> CalculateSiteDetail(
            string rowName,
            InternalRevenueDataVo siteData,
            int year,
            int monthOneBased,
            MonthValueDto monthValueDto,
            SiteMonthlyRevenueDetailDto siteDetailDto,
            PnlResponseDto pnlResponse,
            int monthZeroBased,
            decimal? calculatedExternalRevenue = null)
        {
            var today = DateTime.Today; // Use DateTime.Today for current day detection
            var isCurrentMonth = (year == today.Year && monthOneBased == today.Month);
            
            decimal siteMonthlyValue = 0m;
            bool forecastDataFound = false;
            if (rowName == "ExternalRevenue")
            {
                siteDetailDto.ExternalRevenueBreakdown = null;
                foreach (var calculator in _externalRevenueCalculators)
                    calculator.CalculateAndApply(siteData, year, monthOneBased, monthValueDto, siteDetailDto);
                if (siteDetailDto.ExternalRevenueBreakdown?.CalculatedTotalExternalRevenue != null)
                {
                    siteMonthlyValue = siteDetailDto.ExternalRevenueBreakdown.CalculatedTotalExternalRevenue.Value;
                    forecastDataFound = true;
                }
            }
            else if (rowName == "InternalRevenue")
            {
                siteDetailDto.InternalRevenueBreakdown = null;
                // External revenue must be provided for internal revenue calculations
                decimal externalRevenueForCalculation = calculatedExternalRevenue ?? 0m;
                foreach (var calculator in _internalRevenueCalculators)
                    calculator.CalculateAndApply(siteData, year, monthOneBased, today.Month, monthValueDto, siteDetailDto, externalRevenueForCalculation, pnlResponse.BudgetRows);
                
                // Run management agreement calculators in order
                foreach (var calculator in _managementAgreementCalculators)
                    await calculator.CalculateAndApplyAsync(siteData, year, monthOneBased, today.Month, monthValueDto, siteDetailDto, externalRevenueForCalculation, pnlResponse.BudgetRows);
                
                if (siteDetailDto.InternalRevenueBreakdown?.CalculatedTotalInternalRevenue != null)
                {
                    siteMonthlyValue = siteDetailDto.InternalRevenueBreakdown.CalculatedTotalInternalRevenue.Value;
                    forecastDataFound = true;
                }
            }
            if (!forecastDataFound)
                siteMonthlyValue = GetBudgetFallback(rowName, siteData, pnlResponse, monthZeroBased, siteDetailDto);
            else
                siteDetailDto.IsForecast = true;
            return siteMonthlyValue;
        }

        private decimal GetBudgetFallback(
            string rowName,
            InternalRevenueDataVo siteData,
            PnlResponseDto pnlResponse,
            int monthZeroBased,
            SiteMonthlyRevenueDetailDto siteDetailDto)
        {
            siteDetailDto.IsForecast = false;
            decimal budgetSiteValue = 0m;
            var budgetPnlRow = pnlResponse.BudgetRows?.FirstOrDefault(r => r.ColumnName == rowName);
            if (budgetPnlRow != null)
            {
                var budgetMonthValue = budgetPnlRow.MonthlyValues?.FirstOrDefault(mv => mv.Month == monthZeroBased);
                if (budgetMonthValue != null)
                {
                    var budgetSiteDetail = budgetMonthValue.SiteDetails?.FirstOrDefault(sd => sd.SiteId == siteData.SiteNumber);
                    if (budgetSiteDetail != null)
                    {
                        if (rowName == "InternalRevenue")
                        {
                            siteDetailDto.InternalRevenueBreakdown = null;
                            budgetSiteValue = budgetSiteDetail.Value ?? 0;
                        }
                        else if (rowName == "ExternalRevenue")
                        {
                            siteDetailDto.ExternalRevenueBreakdown = null;
                            budgetSiteValue = budgetSiteDetail.Value ?? 0;
                        }
                    }
                }
            }
            return budgetSiteValue;
        }

        private async Task AggregateMonthlyTotalsAsync(string rowName, MonthValueDto monthValueDto)
        {
            if (rowName == "InternalRevenue")
            {
                foreach (var calculator in _internalRevenueCalculators)
                    calculator.AggregateMonthlyTotals(monthValueDto.SiteDetails, monthValueDto);

                // Run management agreement aggregation in order
                foreach (var calculator in _managementAgreementCalculators)
                    await calculator.AggregateMonthlyTotalsAsync(monthValueDto.SiteDetails, monthValueDto);

                // Central aggregation for CalculatedTotalInternalRevenue
                var breakdown = monthValueDto.InternalRevenueBreakdown;
                if (breakdown != null)
                {
                    breakdown.CalculatedTotalInternalRevenue =
                        (breakdown.FixedFee?.Total ?? 0m) +
                        (breakdown.PerOccupiedRoom?.Total ?? 0m) +
                        (breakdown.RevenueShare?.Total ?? 0m) +
                        (breakdown.PerLaborHour?.Total ?? 0m)+
                        (breakdown.BillableAccounts?.Total ?? 0m) +
                        (breakdown.ManagementAgreement?.Total ?? 0m); 
                }
            }
            else if (rowName == "ExternalRevenue")
            {
                foreach (var calculator in _externalRevenueCalculators)
                    calculator.AggregateMonthlyTotals(monthValueDto.SiteDetails, monthValueDto);
            }
        }

        private PnlResponseDto PopulateCurrentMonthExternalRevenueActuals(PnlResponseDto pnlResponseDto)
        {
            // Get current month (zero-based)
            var currentMonthZeroBased = DateTime.UtcNow.Month - 1;

            // For each ActualRow for ExternalRevenue, update current month if last actual is previous month
            var actualRow = pnlResponseDto.ActualRows?.FirstOrDefault(r => r.ColumnName == "ExternalRevenue");

            if (actualRow != null)
            {
                var actualMonthValue = actualRow.MonthlyValues?.FirstOrDefault(mv => mv.Month == currentMonthZeroBased);

                if (actualMonthValue != null && actualMonthValue.SiteDetails != null)
                {
                    foreach (var siteDetail in actualMonthValue.SiteDetails)
                    {
                        var breakdown = siteDetail.ExternalRevenueBreakdown;
                        if (breakdown != null)
                        {
                            var actualSiteDetail = actualMonthValue.SiteDetails.FirstOrDefault(sd => sd.SiteId == siteDetail.SiteId);
                            var actualValue = actualSiteDetail?.Value ?? 0m;

                            breakdown.ActualExternalRevenue = actualValue;
                            // Calculate forecasted external revenue for remainder of month using SiteStatisticRepository
                            var lastActualDate = breakdown.LastActualRevenueDate ?? DateTime.MinValue;
                            var billingPeriod = DateTime.UtcNow.ToString("yyyy-MM");
                            var siteId = _customerRepository.GetIdBySiteNumber(siteDetail.SiteId);
                            var details = _siteStatisticRepository.GetSiteStatistics(siteId, billingPeriod)?.bs_SiteStatistic_SiteStatisticDetail;
                            decimal forecastedSum = 0m;
                            if (details != null)
                            {
                                forecastedSum = details
                                    .Where(d => d.bs_Date.HasValue
                                        && d.bs_Date.Value > lastActualDate
                                        && d.bs_Date.Value.Month == DateTime.UtcNow.Month
                                        && d.bs_Type != null && d.bs_Type.ToString() == "Forecast")
                                    .Sum(d => d.bs_ExternalRevenue ?? 0m);
                            }
                            breakdown.ForecastedExternalRevenue = forecastedSum;
                            breakdown.CalculatedTotalExternalRevenue = breakdown.ActualExternalRevenue + breakdown.ForecastedExternalRevenue;
                            siteDetail.Value = breakdown.CalculatedTotalExternalRevenue;
                        }
                    }
                    // Set the month value for the current month to the sum of CalculatedTotalExternalRevenue for all site details
                    actualMonthValue.Value = actualMonthValue.SiteDetails
                        .Where(sd => sd.ExternalRevenueBreakdown?.CalculatedTotalExternalRevenue != null)
                        .Sum(sd => sd.ExternalRevenueBreakdown.CalculatedTotalExternalRevenue.Value);
                }
            }
            return pnlResponseDto;
        }

        // Helper method to populate current month payroll actuals and forecast
        private PnlResponseDto PopulateCurrentMonthPayrollActuals(PnlResponseDto pnlResponseDto)
        {
            var currentMonthZeroBased = DateTime.UtcNow.Month - 1;

            var actualRow = pnlResponseDto.ActualRows?.FirstOrDefault(r => r.ColumnName == "Payroll");

            if (actualRow != null)
            {
                var actualMonthValue = actualRow.MonthlyValues?.FirstOrDefault(mv => mv.Month == currentMonthZeroBased);

                if (actualMonthValue != null && actualMonthValue.SiteDetails != null)
                {
                    foreach (var siteDetail in actualMonthValue.SiteDetails)
                    {
                        var payrollBreakdown = siteDetail.PayrollBreakdown ?? new PayrollBreakdownDto();

                        var actualSiteDetail = actualMonthValue.SiteDetails.FirstOrDefault(sd => sd.SiteId == siteDetail.SiteId);
                        var actualValue = actualSiteDetail?.Value ?? 0m;

                        payrollBreakdown.ActualPayroll = actualValue;
                        // Calculate forecasted external revenue for remainder of month using SiteStatisticRepository
                        var lastActualDate = payrollBreakdown.ActualPayrollLastDate ?? DateTime.MinValue;
                        var billingPeriod = DateTime.UtcNow.ToString("yyyy-MM");
                        var siteId = _customerRepository.GetIdBySiteNumber(siteDetail.SiteId);
                        var details = _payrollRepository.GetPayroll(siteId, billingPeriod)?.bs_PayrollDetail_Payroll;
                        decimal forecastedSum = 0m;
                        if (details != null)
                        {
                        forecastedSum = details
                            .Where(d => d.bs_Date.HasValue
                                && d.bs_Date.Value > lastActualDate
                                && d.bs_Date.Value.Month == DateTime.UtcNow.Month)
                            .Sum(d => d.bs_ForecastPayrollCost ?? 0m);
                        }

                        payrollBreakdown.ForecastedPayroll = forecastedSum;
                        payrollBreakdown.TotalPayroll = payrollBreakdown.ActualPayroll + payrollBreakdown.ForecastedPayroll;
                        siteDetail.PayrollBreakdown = payrollBreakdown;
        
                        // Optionally, set siteDetail.Value to total payroll for current month
                        siteDetail.Value = (payrollBreakdown.ActualPayroll ?? 0m) + (payrollBreakdown.ForecastedPayroll ?? 0m);
                    }
                    // Set the month value for the current month to the sum of payroll for all site details
                    actualMonthValue.Value = actualMonthValue.SiteDetails
                        .Where(sd => sd.PayrollBreakdown != null)
                        .Sum(sd => (sd.PayrollBreakdown.ActualPayroll ?? 0m) + (sd.PayrollBreakdown.ForecastedPayroll ?? 0m));
                }
            }
            return pnlResponseDto;
        }
    }
}
