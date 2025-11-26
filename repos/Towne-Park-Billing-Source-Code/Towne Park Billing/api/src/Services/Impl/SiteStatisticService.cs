using api.Adapters.Mappers;
using api.Data;
using api.Models.Vo;
using api.Models.Vo.Enum;
using api.Usecases;
using System.Globalization;
using TownePark;

namespace api.Services.Impl
{
    public class SiteStatisticService : ISiteStatisticService
    {
        private readonly ISiteStatisticRepository _siteStatisticRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IMonthRangeGenerator _monthRangeGenerator;
        private readonly IParkingRateRepository _parkingRateRepository;

        public SiteStatisticService(ISiteStatisticRepository siteStatisticRepository, ICustomerRepository customerRepository, IMonthRangeGenerator monthRangeGenerator, IParkingRateRepository parkingRateRepository)
        {
            _siteStatisticRepository = siteStatisticRepository;
            _customerRepository = customerRepository;
            _monthRangeGenerator = monthRangeGenerator;
            _parkingRateRepository = parkingRateRepository;
        }

        public async Task<IEnumerable<SiteStatisticVo>> GetSiteStatistics(Guid siteId, string billingPeriod, string timeRange)
        {
            IEnumerable<SiteStatisticVo>? siteStatistics;
            string range = timeRange.ToUpper();

            siteStatistics = range switch
            {
                "WEEKLY" => await GetWeeklySiteStatistics(siteId, billingPeriod),
                "MONTHLY" => await GetMonthlySiteStatistics(siteId, billingPeriod),
                _ => await GetDailySiteStatistics(siteId, billingPeriod)
            };

            return siteStatistics?.ToList() ?? new List<SiteStatisticVo>();
        }

        public async Task<SiteStatisticVo?> GetSiteStatisticsForSinglePeriod(Guid siteId, string billingPeriod)
        {
            // Get site statistics for a single period without expanding to 3-month range
            var monthlyStats = _siteStatisticRepository.GetSiteStatistics(siteId, billingPeriod);
            var siteStatisticsList = new List<bs_SiteStatistic>();
            if (monthlyStats != null)
            {
                siteStatisticsList.Add(monthlyStats);
            }

            var siteStatisticsVos = SiteStatisticMapper.SiteStatisticModelToVo(siteStatisticsList)?.ToList() ?? new List<SiteStatisticVo>();

            if (!siteStatisticsVos.Any())
            {
                var customerDetail = _customerRepository.GetCustomerDetail(siteId);
                var emptyVo = new SiteStatisticVo
                {
                    CustomerSiteId = (Guid)customerDetail.bs_CustomerSiteId,
                    SiteNumber = customerDetail.bs_SiteNumber,
                    PeriodLabel = billingPeriod,
                    TotalRooms = int.TryParse(customerDetail.bs_TotalRoomsAvailable, out var totalRooms) ? totalRooms : 0
                };
                siteStatisticsVos.Add(emptyVo);
            }

            var siteStatistic = siteStatisticsVos.FirstOrDefault();
            if (siteStatistic != null)
            {
                siteStatistic.TimeRangeType = TimeRangeType.DAILY;

                // Ensure forecast data has correct PeriodLabel format and populate rates from parking rates service
                if (siteStatistic.ForecastData != null)
                {

                    // Populate forecast data rates from parking rates service
                    await PopulateForecastRatesFromParkingRates(siteStatistic.CustomerSiteId, siteStatistic.ForecastData);
                }

                // Get budget and actual data for this specific period
                if (!string.IsNullOrEmpty(siteStatistic.SiteNumber))
                {
                    var siteNumber = siteStatistic.SiteNumber;
                    var singlePeriodList = new List<string> { billingPeriod };

                var budgetData = await _siteStatisticRepository.GetBudgetDataForRange(siteNumber, singlePeriodList, siteStatistic.TotalRooms);
                siteStatistic.BudgetData = budgetData;
                if (siteStatistic.BudgetData != null && siteStatistic.TotalRooms > 0)
                {
                    foreach (var detail in siteStatistic.BudgetData)
                    {
                        detail.Occupancy = detail.OccupiedRooms / siteStatistic.TotalRooms;
                    }
                }



                var actualData = await _siteStatisticRepository.GetActualDataForRange(siteNumber, singlePeriodList);
                siteStatistic.ActualData = actualData;
                if (siteStatistic.ActualData != null && siteStatistic.TotalRooms > 0)
                {
                    foreach (var detail in siteStatistic.ActualData)
                    {
                        detail.Occupancy = detail.OccupiedRooms / siteStatistic.TotalRooms;
                    }
                }

                }
            }

            return siteStatistic;
        }

        public async Task<SiteStatisticVo?> GetSiteStatisticsForSinglePeriodFast(Guid siteId, string billingPeriod)
        {
            // Same as GetSiteStatisticsForSinglePeriod but skips EDW budget/actual calls
            var monthlyStats = _siteStatisticRepository.GetSiteStatistics(siteId, billingPeriod);
            var siteStatisticsList = new List<bs_SiteStatistic>();
            if (monthlyStats != null)
            {
                siteStatisticsList.Add(monthlyStats);
            }

            var siteStatisticsVos = SiteStatisticMapper.SiteStatisticModelToVo(siteStatisticsList)?.ToList() ?? new List<SiteStatisticVo>();

            if (!siteStatisticsVos.Any())
            {
                var customerDetail = _customerRepository.GetCustomerDetail(siteId);
                var emptyVo = new SiteStatisticVo
                {
                    CustomerSiteId = (Guid)customerDetail.bs_CustomerSiteId,
                    SiteNumber = customerDetail.bs_SiteNumber,
                    PeriodLabel = billingPeriod,
                    TotalRooms = int.TryParse(customerDetail.bs_TotalRoomsAvailable, out var totalRooms) ? totalRooms : 0
                };
                siteStatisticsVos.Add(emptyVo);
            }

            var siteStatistic = siteStatisticsVos.FirstOrDefault();
            if (siteStatistic != null)
            {
                siteStatistic.TimeRangeType = TimeRangeType.DAILY;

                if (siteStatistic.ForecastData != null)
                {
                    await PopulateForecastRatesFromParkingRates(siteStatistic.CustomerSiteId, siteStatistic.ForecastData);
                }

                // Do not populate BudgetData/ActualData from EDW here
            }

            return siteStatistic;
        }

        private async Task<IEnumerable<SiteStatisticVo>?> GetDailySiteStatistics(Guid siteId, string billingPeriod)
        {
            // Generate a 3-month range starting from billingPeriod (format: yyyy-MM)
            var months = _monthRangeGenerator.GenerateMonthRange(billingPeriod, 3);

            var allSiteStatistics = new List<SiteStatisticVo>();

            foreach (var month in months)
            {
                var monthlyStats = _siteStatisticRepository.GetSiteStatistics(siteId, month);
                var siteStatisticsList = new List<bs_SiteStatistic>();
                if (monthlyStats != null)
                {
                    siteStatisticsList.Add(monthlyStats);
                }
                var siteStatisticsVos = SiteStatisticMapper.SiteStatisticModelToVo(siteStatisticsList)?.ToList() ?? new List<SiteStatisticVo>();

                if (!siteStatisticsVos.Any())
                {
                    var customerDetail = _customerRepository.GetCustomerDetail(siteId);
                    var emptyVo = new SiteStatisticVo
                    {
                        CustomerSiteId = (Guid)customerDetail.bs_CustomerSiteId,
                        SiteNumber = customerDetail.bs_SiteNumber,
                        PeriodLabel = month,
                        TotalRooms = int.TryParse(customerDetail.bs_TotalRoomsAvailable, out var totalRooms) ? totalRooms : 0
                    };
                    siteStatisticsVos.Add(emptyVo);
                }

                foreach (var siteStatistic in siteStatisticsVos)
                {
                    siteStatistic.TimeRangeType = TimeRangeType.DAILY;

                    if (siteStatistic.ForecastData != null)
                    {


                        // Populate forecast data rates from parking rates service
                        await PopulateForecastRatesFromParkingRates(siteStatistic.CustomerSiteId, siteStatistic.ForecastData);
                        // Ensure forecast entries are only for the month in question
                        if (!string.IsNullOrEmpty(siteStatistic.PeriodLabel) &&
                            DateTime.TryParseExact(siteStatistic.PeriodLabel, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var periodDate))
                        {
                            var year = periodDate.Year;
                            var monthNum = periodDate.Month;
                            siteStatistic.ForecastData = siteStatistic.ForecastData
                                .Where(d => d.Date.Year == year && d.Date.Month == monthNum)
                                .ToList();
                        }
                    }

                    // Compute occupancy for forecast and actuals when total rooms is available
                    if (siteStatistic.ForecastData != null && siteStatistic.TotalRooms > 0)
                    {
                        foreach (var detail in siteStatistic.ForecastData)
                        {
                            if ((detail.Occupancy == null || detail.Occupancy == 0) && detail.OccupiedRooms.HasValue)
                            {
                                detail.Occupancy = detail.OccupiedRooms / siteStatistic.TotalRooms;
                            }
                        }
                    }
                }

                allSiteStatistics.AddRange(siteStatisticsVos);
            }

            // Sort by period label to ensure correct chronological order
            allSiteStatistics = allSiteStatistics.OrderBy(s => s.PeriodLabel).ToList();

            // Get budget data for all months in a single call
            if (allSiteStatistics.Any() && !string.IsNullOrEmpty(allSiteStatistics.First().SiteNumber))
            {
                var siteNumber = allSiteStatistics.First().SiteNumber;
                var budgetData = _siteStatisticRepository.GetBudgetDataForRange(siteNumber, months, allSiteStatistics.First().TotalRooms).Result;

                // Group budget data by period
                var budgetDataByPeriod = budgetData
                    .GroupBy(b => b.Date.ToString("yyyy-MM"))
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Assign budget data to each site statistic
                foreach (var siteStatistic in allSiteStatistics)
                {
                    if (!string.IsNullOrEmpty(siteStatistic.PeriodLabel) &&
                        budgetDataByPeriod.TryGetValue(siteStatistic.PeriodLabel, out var periodBudgetData))
                    {
                        siteStatistic.BudgetData = periodBudgetData;

                    }
                }
            }

            if (allSiteStatistics.Any() && !string.IsNullOrEmpty(allSiteStatistics.First().SiteNumber))
            {
                var siteNumber = allSiteStatistics.First().SiteNumber;
                var actualData = _siteStatisticRepository.GetActualDataForRange(siteNumber, months).Result;

                // Group actual data by period
                var actualDataByPeriod = actualData
                    .GroupBy(a => a.Date.ToString("yyyy-MM"))
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var siteStatistic in allSiteStatistics)
                {
                    if (!string.IsNullOrEmpty(siteStatistic.PeriodLabel) &&
                        actualDataByPeriod.TryGetValue(siteStatistic.PeriodLabel, out var periodActualData))
                    {
                        siteStatistic.ActualData = periodActualData;

                        // Compute occupancy for actuals when total rooms is available
                        if (siteStatistic.ActualData != null && siteStatistic.TotalRooms > 0)
                        {
                            foreach (var detail in siteStatistic.ActualData)
                            {
                                if (detail.OccupiedRooms.HasValue)
                                {
                                    detail.Occupancy = detail.OccupiedRooms / siteStatistic.TotalRooms;
                                }
                            }
                        }
                    }
                }
            }

            return allSiteStatistics;
        }

        private async Task<IEnumerable<SiteStatisticVo>?> GetWeeklySiteStatistics(Guid siteId, string billingPeriod)
        {
            var weeklySiteStatistics = _siteStatisticRepository.GetSiteStatisticsByRange(siteId, billingPeriod, 3);

            var siteStatisticsList = weeklySiteStatistics?.ToList() ?? new List<bs_SiteStatistic>();
            var siteStatisticsVos = SiteStatisticMapper.SiteStatisticModelToVo(siteStatisticsList)?.ToList() ?? new List<SiteStatisticVo>();

            var months = _monthRangeGenerator.GenerateMonthRange(billingPeriod, 3);
            foreach (var month in months)
            {
                var siteStatisticsForMonth = siteStatisticsVos
                    .Where(s => s.PeriodLabel == month)
                    .ToList();
                if (!siteStatisticsForMonth.Any())
                {
                    var customerDetail = _customerRepository.GetCustomerDetail(siteId);
                    var emptyVo = new SiteStatisticVo
                    {
                        CustomerSiteId = (Guid)customerDetail.bs_CustomerSiteId,
                        SiteNumber = customerDetail.bs_SiteNumber,
                        PeriodLabel = month,
                        TotalRooms = int.TryParse(customerDetail.bs_TotalRoomsAvailable, out var totalRooms) ? totalRooms : 0
                    };
                    siteStatisticsVos.Add(emptyVo);
                }
            }

            // Sort by period label to ensure correct chronological order
            siteStatisticsVos = siteStatisticsVos.OrderBy(s => s.PeriodLabel).ToList();

            // Get budget data for all months in a single call
            if (siteStatisticsVos.Any() && !string.IsNullOrEmpty(siteStatisticsVos.First().SiteNumber))
            {
                var siteNumber = siteStatisticsVos.First().SiteNumber;
                var budgetData = await _siteStatisticRepository.GetBudgetDataForRange(siteNumber, months, siteStatisticsVos.First().TotalRooms);

                // Group budget data by period
                var budgetDataByPeriod = budgetData
                    .GroupBy(b => b.Date.ToString("yyyy-MM"))
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Assign budget data to each site statistic
                foreach (var siteStatistic in siteStatisticsVos)
                {
                    if (!string.IsNullOrEmpty(siteStatistic.PeriodLabel) &&
                        budgetDataByPeriod.TryGetValue(siteStatistic.PeriodLabel, out var periodBudgetData))
                    {
                        siteStatistic.BudgetData = periodBudgetData;
                    }
                }
            }

            // Get actual data for all months in a single call
            if (siteStatisticsVos.Any() && !string.IsNullOrEmpty(siteStatisticsVos.First().SiteNumber))
            {
                var siteNumber = siteStatisticsVos.First().SiteNumber;
                var actualData = await _siteStatisticRepository.GetActualDataForRange(siteNumber, months);

                // Group actual data by period
                var actualDataByPeriod = actualData
                    .GroupBy(a => a.Date.ToString("yyyy-MM"))
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Assign actual data to each site statistic
                foreach (var siteStatistic in siteStatisticsVos)
                {
                    if (!string.IsNullOrEmpty(siteStatistic.PeriodLabel) &&
                        actualDataByPeriod.TryGetValue(siteStatistic.PeriodLabel, out var periodActualData))
                    {
                        siteStatistic.ActualData = periodActualData;
                    }
                }
            }

            foreach (var siteStatistic in siteStatisticsVos)
            {
                siteStatistic.TimeRangeType = TimeRangeType.WEEKLY;

                if (!DateTime.TryParseExact(siteStatistic.PeriodLabel, "yyyy-MM", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out DateTime periodDate))
                {
                    throw new ArgumentException("Invalid billing period format. Expected yyyy-MM", nameof(billingPeriod));
                }

                var weeklyPeriods = GenerateWeeklyPeriods(periodDate.Year, periodDate.Month);
                var weeklyForecastDetails = new List<SiteStatisticDetailVo>();
                var weeklyBudgetDetails = new List<SiteStatisticDetailVo>();
                var weeklyActualDetails = new List<SiteStatisticDetailVo>();

                foreach (var (periodLabel, startDate, endDate) in weeklyPeriods)
                {
                    var weekForecastDetails = new List<SiteStatisticDetailVo>();
                    var weekBudgetDetails = new List<SiteStatisticDetailVo>();
                    var weekActualDetails = new List<SiteStatisticDetailVo>();


                    weekBudgetDetails = siteStatistic.BudgetData?
                        .Where(d => d.Date >= startDate && d.Date <= endDate)
                        .ToList() ?? new List<SiteStatisticDetailVo>();

                    weekForecastDetails = siteStatistic.ForecastData?
                        .Where(d => d.Date >= startDate && d.Date <= endDate)
                        .ToList() ?? new List<SiteStatisticDetailVo>();

                    weekActualDetails = siteStatistic.ActualData?
                        .Where(d => d.Date >= startDate && d.Date <= endDate)
                        .ToList() ?? new List<SiteStatisticDetailVo>();

                    var weeklyForecastAggregate = CreateAggregate(weekForecastDetails, periodLabel, startDate, endDate, SiteStatisticDetailType.Forecast);
                    var weeklyBudgetAggregate = CreateAggregate(weekBudgetDetails, periodLabel, startDate, endDate, SiteStatisticDetailType.Budget);
                    var weeklyActualAggregate = CreateAggregate(weekActualDetails, periodLabel, startDate, endDate, SiteStatisticDetailType.Actual);


                    weeklyForecastDetails.Add(weeklyForecastAggregate);
                    weeklyBudgetDetails.Add(weeklyBudgetAggregate);
                    weeklyActualDetails.Add(weeklyActualAggregate);
                }

                // Replace the original data with weekly aggregates
                siteStatistic.BudgetData = weeklyBudgetDetails;
                siteStatistic.ActualData = weeklyActualDetails;
                if (siteStatistic.ForecastData != null)
                {
                    siteStatistic.ForecastData = weeklyForecastDetails;
                }
                else
                {
                    siteStatistic.ForecastData = [];
                }
            }

            return siteStatisticsVos;
        }

        private SiteStatisticDetailVo CreateAggregate(
            List<SiteStatisticDetailVo> details,
            string periodLabel,
            DateOnly startDate,
            DateOnly endDate,
            SiteStatisticDetailType detailType)
        {
            if (details.Count() == 0)
            {
                return null;
            }
            // Weighted average for occupancy (if Occupancy and OccupiedRooms are available)
            double totalOccupiedRooms = details.Sum(d => Convert.ToDouble(d.OccupiedRooms));
            double totalAvailableRooms = details
                .Where(d => Convert.ToDouble(d.Occupancy) > 0)
                .Sum(d => Convert.ToDouble(d.OccupiedRooms) / Convert.ToDouble(d.Occupancy));

            double aggregatedOccupancy = (totalAvailableRooms > 0) ? totalOccupiedRooms / totalAvailableRooms : 0;

            // Arithmetic mean for driveInRatio and captureRatio (doubles)
            var driveInRatios = details.Select(d => d.DriveInRatio).ToList();
            var captureRatios = details.Select(d => d.CaptureRatio).ToList();

            double aggregatedDriveInRatio = driveInRatios.Count > 0 ? driveInRatios.Average() : 0;
            double aggregatedCaptureRatio = captureRatios.Count > 0 ? captureRatios.Average() : 0;

            return new SiteStatisticDetailVo
            {
                Type = detailType,
                PeriodStart = startDate,
                PeriodEnd = endDate,
                PeriodLabel = periodLabel,

                // Sum values (ensure decimal)
                BaseRevenue = Convert.ToDecimal(details.Sum(d => d.BaseRevenue)),
                OccupiedRooms = Convert.ToDecimal(details.Sum(d => d.OccupiedRooms)),
                SelfOvernight = Convert.ToDecimal(details.Sum(d => d.SelfOvernight)),
                ValetOvernight = Convert.ToDecimal(details.Sum(d => d.ValetOvernight)),
                ValetDaily = Convert.ToDecimal(details.Sum(d => d.ValetDaily)),
                ValetMonthly = Convert.ToDecimal(details.Sum(d => d.ValetMonthly)),
                SelfDaily = Convert.ToDecimal(details.Sum(d => d.SelfDaily)),
                SelfMonthly = Convert.ToDecimal(details.Sum(d => d.SelfMonthly)),
                ValetComps = Convert.ToDecimal(details.Sum(d => d.ValetComps)),
                SelfComps = Convert.ToDecimal(details.Sum(d => d.SelfComps)),
                SelfAggregator = Convert.ToDecimal(details.Sum(d => d.SelfAggregator)),
                ValetAggregator = Convert.ToDecimal(details.Sum(d => d.ValetAggregator)),
                ExternalRevenue = Convert.ToDecimal(details.Sum(d => d.ExternalRevenue)),

                SelfRateDaily = details.First().SelfRateDaily,
                SelfRateMonthly = details.First().SelfRateMonthly,
                SelfRateOvernight = details.First().SelfRateOvernight,
                ValetRateDaily = details.First().ValetRateDaily,
                ValetRateOvernight = details.First().ValetRateOvernight,
                ValetRateMonthly = details.First().ValetRateMonthly,
                Occupancy = (decimal)aggregatedOccupancy,
                DriveInRatio = aggregatedDriveInRatio,
                CaptureRatio = aggregatedCaptureRatio,

                // Include adjustment fields - use first item's adjustment values since they should be consistent
                AdjustmentValue = details.FirstOrDefault()?.AdjustmentValue,
                AdjustmentPercentage = details.FirstOrDefault()?.AdjustmentPercentage
            };
        }

        private List<(string PeriodLabel, DateOnly StartDate, DateOnly EndDate)> GenerateWeeklyPeriods(int year, int month)
        {
            var result = new List<(string PeriodLabel, DateOnly StartDate, DateOnly EndDate)>();

            // Get first and last day of month
            var firstDayOfMonth = new DateOnly(year, month, 1);
            var lastDayOfMonth = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

            // Get month name for formatting
            string monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);

            // Find the first Sunday of the month or use first day if already a Sunday
            DateOnly weekStart = firstDayOfMonth;
            if (weekStart.DayOfWeek != DayOfWeek.Sunday)
            {
                // If not starting on Sunday, use first day of month
                weekStart = firstDayOfMonth;
            }

            while (weekStart <= lastDayOfMonth)
            {
                // Find the end date (next Saturday or last day of month)
                var weekEnd = weekStart;
                while (weekEnd.DayOfWeek != DayOfWeek.Saturday && weekEnd < lastDayOfMonth)
                {
                    weekEnd = weekEnd.AddDays(1);
                }

                // Format period label - always use month name of current month
                string periodLabel = $"{monthName} {weekStart.Day} - {weekEnd.Day}";

                // Add to result
                result.Add((periodLabel, weekStart, weekEnd));

                // Move to next week (start with Sunday)
                weekStart = weekEnd.AddDays(1);

                // Check if we've moved to next month
                if (weekStart.Month != month)
                {
                    break;
                }
            }

            return result;
        }

        private List<(string PeriodLabel, DateOnly StartDate, DateOnly EndDate)> GenerateQuarterlyPeriods(int year)
        {
            var result = new List<(string PeriodLabel, DateOnly StartDate, DateOnly EndDate)>();
            var quarterNames = new[] { "Q1", "Q2", "Q3", "Q4" };

            for (int i = 0; i < 4; i++)
            {
                int startMonth = i * 3 + 1;
                int endMonth = startMonth + 2;
                var startDate = new DateOnly(year, startMonth, 1);
                var endDate = new DateOnly(year, endMonth, DateTime.DaysInMonth(year, endMonth));
                result.Add(($"{quarterNames[i]} {year}", startDate, endDate));
            }

            return result;
        }

        public void SaveSiteStatistics(SiteStatisticVo updates)
        {
            bs_SiteStatistic updateModel = SiteStatisticMapper.SiteStatisticVoToModel(updates);

            if (updateModel.Id != Guid.Empty)
            {
                _siteStatisticRepository.SaveSiteStatistics(updateModel);
            }
            else
            {
                // call repo to add new site stat record
                _siteStatisticRepository.CreateSiteStatistics(updateModel);
            }
        }
        public async Task<List<SiteStatisticVo>> GetSiteStatisticsBatch(List<string> siteNumbers, List<string> billingPeriods)
        {
            // Execute the synchronous call in a Task to maintain the async contract
            var models = await Task.Run(() => _siteStatisticRepository.GetSiteStatisticsBatch(siteNumbers, billingPeriods));

            if (models == null || !models.Any())
            {
                return new List<SiteStatisticVo>(); // Return an empty list to avoid errors downstream  
            }
            return models
                .Where(model => model != null) // Filter out potential nulls in the list  
                .Select(model => SiteStatisticMapper.SiteStatisticModelToVo(new List<bs_SiteStatistic> { model })) // Wrap the model in a list  
                .Where(vo => vo != null) // Ensure mapper didn't return null (optional but safe)  
                .SelectMany(vo => vo) // Flatten the IEnumerable<IEnumerable<SiteStatisticVo>> to IEnumerable<SiteStatisticVo>  
                .ToList();
        }

        public async Task<List<SiteStatisticDetailVo>> GetBudgetDailyData(string siteNumber, string billingPeriod)
        {
            return await _siteStatisticRepository.GetBudgetData(siteNumber, billingPeriod, 0);
        }

        private async Task<IEnumerable<SiteStatisticVo>?> GetMonthlySiteStatistics(Guid siteId, string billingPeriod)
        {
            // Mirror DAILY logic to ensure consistent data; keep TimeRangeType as MONTHLY
            var months = _monthRangeGenerator.GenerateMonthRange(billingPeriod, 3);

            var allSiteStatistics = new List<SiteStatisticVo>();

            foreach (var month in months)
            {
                var monthlyStats = _siteStatisticRepository.GetSiteStatistics(siteId, month);
                var siteStatisticsList = new List<bs_SiteStatistic>();
                if (monthlyStats != null)
                {
                    siteStatisticsList.Add(monthlyStats);
                }

                var siteStatisticsVos = SiteStatisticMapper.SiteStatisticModelToVo(siteStatisticsList)?.ToList() ?? new List<SiteStatisticVo>();

                if (!siteStatisticsVos.Any())
                {
                    var customerDetail = _customerRepository.GetCustomerDetail(siteId);
                    var emptyVo = new SiteStatisticVo
                    {
                        CustomerSiteId = (Guid)customerDetail.bs_CustomerSiteId,
                        SiteNumber = customerDetail.bs_SiteNumber,
                        PeriodLabel = month,
                        TotalRooms = int.TryParse(customerDetail.bs_TotalRoomsAvailable, out var totalRooms) ? totalRooms : 0
                    };
                    siteStatisticsVos.Add(emptyVo);
                }

                foreach (var siteStatistic in siteStatisticsVos)
                {
                    siteStatistic.TimeRangeType = TimeRangeType.MONTHLY;

                    if (siteStatistic.ForecastData != null)
                    {
                        await PopulateForecastRatesFromParkingRates(siteStatistic.CustomerSiteId, siteStatistic.ForecastData);
                        // Ensure forecast entries are only for the month in question
                        if (!string.IsNullOrEmpty(siteStatistic.PeriodLabel) &&
                            DateTime.TryParseExact(siteStatistic.PeriodLabel, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var periodDate))
                        {
                            var year = periodDate.Year;
                            var monthNum = periodDate.Month;
                            siteStatistic.ForecastData = siteStatistic.ForecastData
                                .Where(d => d.Date.Year == year && d.Date.Month == monthNum)
                                .ToList();
                        }
                    }
                }

                allSiteStatistics.AddRange(siteStatisticsVos);
            }

            // Sort by period label to ensure correct chronological order
            allSiteStatistics = allSiteStatistics.OrderBy(s => s.PeriodLabel).ToList();

            // Get budget data for all months in a single call
            if (allSiteStatistics.Any() && !string.IsNullOrEmpty(allSiteStatistics.First().SiteNumber))
            {
                var siteNumber = allSiteStatistics.First().SiteNumber;
                var budgetData = await _siteStatisticRepository.GetBudgetDataForRange(siteNumber, months, allSiteStatistics.First().TotalRooms);

                // Group budget data by period
                var budgetDataByPeriod = budgetData
                    .GroupBy(b => b.Date.ToString("yyyy-MM"))
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Assign budget data to each site statistic
                foreach (var siteStatistic in allSiteStatistics)
                {
                    if (!string.IsNullOrEmpty(siteStatistic.PeriodLabel) &&
                        budgetDataByPeriod.TryGetValue(siteStatistic.PeriodLabel, out var periodBudgetData))
                    {
                        siteStatistic.BudgetData = periodBudgetData;

                        // Ensure budget occupancy present (EDW path already computes, but backfill if needed)
                        if (siteStatistic.BudgetData != null && siteStatistic.TotalRooms > 0)
                        {
                            foreach (var detail in siteStatistic.BudgetData)
                            {
                                if ((detail.Occupancy == null || detail.Occupancy == 0) && detail.OccupiedRooms.HasValue)
                                {
                                    detail.Occupancy = detail.OccupiedRooms / siteStatistic.TotalRooms;
                                }
                            }
                        }
                    }
                }
            }

            // Get actual data for all months in a single call
            if (allSiteStatistics.Any() && !string.IsNullOrEmpty(allSiteStatistics.First().SiteNumber))
            {
                var siteNumber = allSiteStatistics.First().SiteNumber;
                var actualData = await _siteStatisticRepository.GetActualDataForRange(siteNumber, months);

                // Group actual data by period
                var actualDataByPeriod = actualData
                    .GroupBy(a => a.Date.ToString("yyyy-MM"))
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var siteStatistic in allSiteStatistics)
                {
                    if (!string.IsNullOrEmpty(siteStatistic.PeriodLabel) &&
                        actualDataByPeriod.TryGetValue(siteStatistic.PeriodLabel, out var periodActualData))
                    {
                        siteStatistic.ActualData = periodActualData;

                        // Compute occupancy for actuals when total rooms is available
                        if (siteStatistic.ActualData != null && siteStatistic.TotalRooms > 0)
                        {
                            foreach (var detail in siteStatistic.ActualData)
                            {
                                if (detail.OccupiedRooms.HasValue)
                                {
                                    detail.Occupancy = detail.OccupiedRooms / siteStatistic.TotalRooms;
                                }
                            }
                        }
                    }
                }
            }

            return allSiteStatistics;
        }

        public async Task<PnlBySiteListVo> GetPNLData(List<string> siteIds, int year)
        {
            return await _siteStatisticRepository.GetPNLData(siteIds, year);
        }


        private async Task PopulateForecastRatesFromParkingRates(Guid siteId, List<SiteStatisticDetailVo> forecastData)
        {
            if (forecastData == null || !forecastData.Any())
            {
                return;
            }

            try
            {
                // Get the year from the first forecast entry
                var firstEntry = forecastData.First();
                var year = firstEntry.Date.Year;

                // Get parking rates for this site and year directly from repository
                var dataverseParkingRate = _parkingRateRepository.GetParkingRateWithDetails(siteId, year);
                var parkingRateData = dataverseParkingRate != null
                    ? ParkingRateMapper.ParkingRateModelToVo(dataverseParkingRate)
                    : null;

                if (parkingRateData?.ForecastRates != null && parkingRateData.ForecastRates.Any())
                {
                    // Create a lookup dictionary for quick access by month
                    var ratesByMonth = parkingRateData.ForecastRates
                        .GroupBy(r => r.Month)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    // Update each forecast entry with rates from parking rates service
                    foreach (var forecast in forecastData)
                    {
                        var month = forecast.Date.Month;

                        if (ratesByMonth.TryGetValue(month, out var monthRates))
                        {
                            // Update rates from parking rates service
                            foreach (var rate in monthRates)
                            {
                                switch (rate.RateCategory)
                                {
                                    case bs_ratecategorytypes.ValetDaily:
                                        forecast.ValetRateDaily = rate.Rate;
                                        break;
                                    case bs_ratecategorytypes.ValetMonthly:
                                        forecast.ValetRateMonthly = rate.Rate;
                                        break;
                                    case bs_ratecategorytypes.ValetOvernight:
                                        forecast.ValetRateOvernight = rate.Rate;
                                        break;
                                    case bs_ratecategorytypes.SelfDaily:
                                        forecast.SelfRateDaily = rate.Rate;
                                        break;
                                    case bs_ratecategorytypes.SelfMonthly:
                                        forecast.SelfRateMonthly = rate.Rate;
                                        break;
                                    case bs_ratecategorytypes.SelfOvernight:
                                        forecast.SelfRateOvernight = rate.Rate;
                                        break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the entire operation
                Console.WriteLine($"ERROR populating forecast rates from parking rates service for site {siteId}: {ex.Message}");
            }
        }

    }
}
