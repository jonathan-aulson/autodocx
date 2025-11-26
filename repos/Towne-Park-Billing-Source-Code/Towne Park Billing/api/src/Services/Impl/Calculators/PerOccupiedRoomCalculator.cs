using System;
using System.Collections.Generic;
using System.Linq;
using TownePark.Models.Vo;
using TownePark;
using api.Data;
using api.Models.Dto;

namespace api.Services.Impl.Calculators
{
    public class PerOccupiedRoomCalculator : IInternalRevenueCalculator
    {
        private readonly ISiteStatisticRepository _siteStatisticRepository;

        public PerOccupiedRoomCalculator(ISiteStatisticRepository siteStatisticRepository)
        {
            _siteStatisticRepository = siteStatisticRepository;
        }
        public void CalculateAndApply(
            InternalRevenueDataVo siteData,
            int year,
            int monthOneBased,
            int currentMonth, // NEW: Current month parameter
            MonthValueDto monthValueDto,
            SiteMonthlyRevenueDetailDto siteDetailDto,
            decimal calculatedExternalRevenue,
            List<PnlRowDto> budgetRows)
        {
            //Check if PerOccupiedRoom is in the enabled contract types
            if (siteData.Contract?.ContractTypes == null || 
                !siteData.Contract.ContractTypes.Contains(bs_contracttypechoices.PerOccupiedRoom))
                return;

            // Use explicit currentMonth to branch current-month behavior
            bool isCurrentMonth = (monthOneBased == currentMonth);

            PerOccupiedRoomInternalRevenueDto perOccupiedRoomRevenue;
            if (isCurrentMonth)
            {
                perOccupiedRoomRevenue = CalculateCurrentMonthPerOccupiedRoomUsingActuals(siteData, year, monthOneBased, calculatedExternalRevenue);
            }
            else
            {
                perOccupiedRoomRevenue = CalculateMonthlyPerOccupiedRoomRevenueForSite(siteData, year, monthOneBased, calculatedExternalRevenue, monthValueDto, budgetRows);
            }

            if (siteDetailDto.InternalRevenueBreakdown == null)
            {
                siteDetailDto.InternalRevenueBreakdown = new InternalRevenueBreakdownDto();
            }
            siteDetailDto.InternalRevenueBreakdown.PerOccupiedRoom = perOccupiedRoomRevenue;

            // Hybrid approach: set CalculatedTotalInternalRevenue at site level
            var breakdown = siteDetailDto.InternalRevenueBreakdown;
            decimal total = 0m;
            if (breakdown.FixedFee?.Total != null) total += breakdown.FixedFee.Total.Value;
            if (breakdown.PerOccupiedRoom?.Total != null) total += breakdown.PerOccupiedRoom.Total.Value;
            if (breakdown.RevenueShare?.Total != null) total += breakdown.RevenueShare.Total.Value;
            if (breakdown.BillableAccounts?.Total != null) total += breakdown.BillableAccounts.Total.Value;
            if (breakdown.ManagementAgreement?.Total != null) total += breakdown.ManagementAgreement.Total.Value;
            if (breakdown.OtherRevenue?.Total != null) total += breakdown.OtherRevenue.Total.Value;
            breakdown.CalculatedTotalInternalRevenue = total;
        }

        public void AggregateMonthlyTotals(List<SiteMonthlyRevenueDetailDto> siteDetailsForMonth, MonthValueDto monthValueDto)
        {
            decimal totalPerOccupiedRoomBaseForMonth = 0m;
            decimal totalForecastedRoomsForMonth = 0m;
            decimal totalBudgetRoomsForMonth = 0m;
            decimal averageFeePerRoom = 0m;

            foreach (var siteDetail in siteDetailsForMonth)
            {
                if (siteDetail.InternalRevenueBreakdown?.PerOccupiedRoom != null)
                {
                    var perOccupiedRoom = siteDetail.InternalRevenueBreakdown.PerOccupiedRoom;
                    totalPerOccupiedRoomBaseForMonth += perOccupiedRoom.Total ?? 0m;
                    totalForecastedRoomsForMonth += perOccupiedRoom.ForecastedRooms ?? 0m;
                    totalBudgetRoomsForMonth += perOccupiedRoom.BudgetRooms ?? 0m;
                    
                    if (averageFeePerRoom == 0m && perOccupiedRoom.FeePerRoom > 0m)
                    {
                        averageFeePerRoom = perOccupiedRoom.FeePerRoom;
                    }
                }
            }
            
            if (monthValueDto.InternalRevenueBreakdown == null)
            {
                monthValueDto.InternalRevenueBreakdown = new InternalRevenueBreakdownDto();
            }

            monthValueDto.InternalRevenueBreakdown.PerOccupiedRoom = new PerOccupiedRoomInternalRevenueDto
            {
                FeePerRoom = averageFeePerRoom,
                ForecastedRooms = totalForecastedRoomsForMonth > 0 ? totalForecastedRoomsForMonth : null,
                BudgetRooms = totalForecastedRoomsForMonth > 0 ? null : totalBudgetRoomsForMonth,
                BaseRevenue = totalPerOccupiedRoomBaseForMonth,
                Escalators = new List<EscalatorDto>(),
                Total = totalPerOccupiedRoomBaseForMonth
            };


            monthValueDto.Value = monthValueDto.InternalRevenueBreakdown.PerOccupiedRoom?.Total ?? 0m;
        }

        private PerOccupiedRoomInternalRevenueDto CalculateMonthlyPerOccupiedRoomRevenueForSite(
            InternalRevenueDataVo siteData, int targetYear, int targetMonthOneBased, decimal calculatedExternalRevenue, MonthValueDto monthValueDto, List<PnlRowDto> budgetRows)
        {
            var perOccupiedRoomFee = siteData.Contract?.OccupiedRoomRate ?? 0m;
            decimal forecastedOccupiedRooms = 0m;

            if (siteData.SiteStatistics != null)
            {
                forecastedOccupiedRooms = siteData.SiteStatistics
                    .Where(s => s.Date.Year == targetYear && s.Date.Month == targetMonthOneBased && s.OccupiedRooms.HasValue)
                    .Sum(s => s.OccupiedRooms.Value);
            }

            var budgetOccupiedRooms = budgetRows[1].MonthlyValues[targetMonthOneBased - 1]?.SiteDetails?.FirstOrDefault(x => x.SiteId == siteData.SiteNumber)?.InternalRevenueBreakdown?.PerOccupiedRoom?.BudgetRooms ?? 0;
            decimal roomsForCalculation = forecastedOccupiedRooms > 0 ? forecastedOccupiedRooms : budgetOccupiedRooms;
            var baseRevenue = perOccupiedRoomFee * roomsForCalculation;
            
            var escalatorAmount = 0m; 
            var escalators = new List<EscalatorDto>();

            // Conditional escalator check as per Task 2118: only apply escalators if external revenue > 0
            if (calculatedExternalRevenue > 0)
            {
                // Future escalator logic will go here
                escalatorAmount = 0m;
            }

            return new PerOccupiedRoomInternalRevenueDto
            {
                FeePerRoom = perOccupiedRoomFee,
                ForecastedRooms = forecastedOccupiedRooms > 0 ? forecastedOccupiedRooms : null,
                BudgetRooms = forecastedOccupiedRooms > 0 ? null : budgetOccupiedRooms,
                BaseRevenue = baseRevenue,
                Escalators = escalators, 
                Total = baseRevenue + escalatorAmount
            };
        }

        private PerOccupiedRoomInternalRevenueDto CalculateCurrentMonthPerOccupiedRoomUsingActuals(
            InternalRevenueDataVo siteData, int targetYear, int targetMonthOneBased, decimal calculatedExternalRevenue)
        {
            var ratePerRoom = siteData.Contract?.OccupiedRoomRate ?? 0m;

            // Determine cutoff and actual rooms from EDW actuals for this site/month (api.Models.Vo)
            DateOnly? cutoff = null;
            decimal actualRooms = 0m;
            var billingPeriod = $"{targetYear}-{targetMonthOneBased:D2}";
            var edwActuals = _siteStatisticRepository
                .GetActualData(siteData.SiteNumber, billingPeriod)
                .GetAwaiter().GetResult() ?? new List<api.Models.Vo.SiteStatisticDetailVo>();

            var monthlyActuals = edwActuals
                .Where(a => a.Date.Year == targetYear && a.Date.Month == targetMonthOneBased)
                .ToList();
            if (monthlyActuals.Count > 0)
            {
                cutoff = monthlyActuals.Max(a => a.Date);
                actualRooms = monthlyActuals
                    .Where(a => a.Date <= cutoff.Value)
                    .Sum(a => a.OccupiedRooms ?? 0m);
            }
            else
            {
                // Fallback: use last day of previous month to indicate data ran but no current-month actuals
                var prevMonthEnd = new DateTime(targetYear, targetMonthOneBased, 1).AddDays(-1);
                cutoff = DateOnly.FromDateTime(prevMonthEnd);
            }

            decimal forecastRooms = 0m;

            // For forecast, use existing daily forecast rows from site statistics for the remaining days
            if (siteData.SiteStatistics != null)
            {
                if (cutoff.HasValue)
                {
                    // Forecast after cutoff through month end
                    var cutoffDateTime = cutoff.Value.ToDateTime(TimeOnly.MinValue);
                    forecastRooms = siteData.SiteStatistics
                        .Where(s => s.Date.Year == targetYear && s.Date.Month == targetMonthOneBased &&
                                    s.Type == bs_sitestatisticdetailchoice.Forecast && s.OccupiedRooms.HasValue && s.Date.Date > cutoffDateTime.Date)
                        .Sum(s => s.OccupiedRooms!.Value);
                }
                else
                {
                    // No actuals for the month → whole month is forecast
                    forecastRooms = siteData.SiteStatistics
                        .Where(s => s.Date.Year == targetYear && s.Date.Month == targetMonthOneBased &&
                                    s.Type == bs_sitestatisticdetailchoice.Forecast && s.OccupiedRooms.HasValue)
                        .Sum(s => s.OccupiedRooms!.Value);
                }
            }

            var actualAmount = actualRooms * ratePerRoom;
            var forecastAmount = forecastRooms * ratePerRoom;

            // Keep escalator logic exactly as-is: currently gated on external revenue and not applied
            var escalators = new List<EscalatorDto>();
            var total = actualAmount + forecastAmount; // no escalators applied in current implementation

            return new PerOccupiedRoomInternalRevenueDto
            {
                FeePerRoom = ratePerRoom,
                ForecastedRooms = forecastRooms > 0 ? forecastRooms : null,
                BudgetRooms = null,
                ActualRooms = actualRooms > 0 ? actualRooms : null,
                LastActualDate = cutoff.HasValue ? cutoff.Value.ToDateTime(TimeOnly.MinValue) : null,
                BaseRevenue = total,
                ActualAmount = actualAmount > 0 ? actualAmount : null,
                Escalators = escalators,
                Total = total
            };
        }

        // Example placeholder for future escalator logic for this component type
        // private List<EscalatorDto> CalculateEscalatorsForPerOccupiedRoom(InternalRevenueDataVo siteData, int year, int month, decimal baseRevenue)
        // {
        //    // ... logic to calculate escalators based on contract terms ...
        //    return new List<EscalatorDto>();
        // }
    }
}
