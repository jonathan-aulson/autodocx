using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using TownePark.Models.Vo;
using TownePark;
using api.Models.Vo;
using api.Models.Dto;
using api.Data;
using api.Services.Impl.Calculators;
using Xunit;

namespace BackendTests.Services
{
    public class PerOccupiedRoomCalculatorTests
    {
        private readonly ISiteStatisticRepository _siteStatisticRepository = Substitute.For<ISiteStatisticRepository>();
        private readonly PerOccupiedRoomCalculator _calculator;

        public PerOccupiedRoomCalculatorTests()
        {
            _calculator = new PerOccupiedRoomCalculator(_siteStatisticRepository);
        }

        private InternalRevenueDataVo CreateSiteData(string siteNumber, decimal ratePerRoom,
            IEnumerable<TownePark.Models.Vo.SiteStatisticDetailVo> forecastRows)
        {
            return new InternalRevenueDataVo
            {
                SiteId = Guid.NewGuid(),
                SiteNumber = siteNumber,
                SiteName = "Test",
                Contract = new ContractDataVo
                {
                    OccupiedRoomRate = ratePerRoom,
                    ContractTypes = new List<bs_contracttypechoices>
                    {
                        bs_contracttypechoices.PerOccupiedRoom
                    }
                },
                SiteStatistics = forecastRows.ToList()
            };
        }

        private api.Models.Vo.SiteStatisticDetailVo ActualRow(int y, int m, int d, decimal rooms)
        {
            return new api.Models.Vo.SiteStatisticDetailVo
            {
                Date = new DateOnly(y, m, d),
                OccupiedRooms = rooms,
                Type = api.Models.Vo.SiteStatisticDetailType.Actual
            };
        }

        private TownePark.Models.Vo.SiteStatisticDetailVo ForecastRow(DateTime date, decimal rooms)
        {
            return new TownePark.Models.Vo.SiteStatisticDetailVo
            {
                Date = date,
                OccupiedRooms = rooms,
                Type = bs_sitestatisticdetailchoice.Forecast
            };
        }

        [Fact]
        public void CalculateAndApply_CurrentMonth_MixedActualsPlusForecast_CombinesCorrectly()
        {
            var year = 2025; var month = 5; var currentMonth = 5;
            var siteNumber = "S100";
            var rate = 10m;
            var billingPeriod = $"{year}-{month:D2}";

            var cutoff = new DateOnly(year, month, 10);
            var edwActuals = new List<api.Models.Vo.SiteStatisticDetailVo>
            {
                ActualRow(year, month, 1, 50),
                ActualRow(year, month, 5, 60),
                ActualRow(year, month, 10, 40)
            };
            _siteStatisticRepository.GetActualData(siteNumber, billingPeriod).Returns(edwActuals);

            var forecastRows = new List<TownePark.Models.Vo.SiteStatisticDetailVo>();
            // After cutoff: 11..31
            for (int d = 11; d <= 31; d++)
            {
                forecastRows.Add(ForecastRow(new DateTime(year, month, d), 30));
            }

            var siteData = CreateSiteData(siteNumber, rate, forecastRows);

            var monthValueDto = new MonthValueDto();
            var siteDetailDto = new SiteMonthlyRevenueDetailDto
            {
                InternalRevenueBreakdown = new InternalRevenueBreakdownDto()
            };

            _calculator.CalculateAndApply(siteData, year, month, currentMonth, monthValueDto, siteDetailDto, 0m, new List<PnlRowDto>());

            var por = siteDetailDto.InternalRevenueBreakdown.PerOccupiedRoom;
            Assert.NotNull(por);
            // Actual rooms: 50+60+40=150; Forecast days 21 * 30 = 630; total rooms = 780; total amount = 780 * 10
            Assert.Equal(150, por.ActualRooms);
            Assert.True(por.ForecastedRooms.HasValue);
            Assert.Equal(630, por.ForecastedRooms.Value);
            Assert.Equal(7800m, por.Total);
            Assert.Equal(new DateTime(year, month, cutoff.Day), por.LastActualDate);
            Assert.Equal(1500m, por.ActualAmount);
            // Split adds to total
            var forecastAmount = (por.ForecastedRooms ?? 0m) * por.FeePerRoom;
            var actualAmount = por.ActualAmount ?? 0m;
            Assert.Equal(actualAmount + forecastAmount, por.Total);
        }

        [Fact]
        public void CalculateAndApply_CurrentMonth_NoActuals_UsesFullForecast()
        {
            var year = 2025; var month = 5; var currentMonth = 5;
            var siteNumber = "S101";
            var rate = 8m;
            var billingPeriod = $"{year}-{month:D2}";

            _siteStatisticRepository.GetActualData(siteNumber, billingPeriod).Returns(new List<api.Models.Vo.SiteStatisticDetailVo>());

            var forecastRows = new List<TownePark.Models.Vo.SiteStatisticDetailVo>();
            for (int d = 1; d <= 31; d++)
            {
                forecastRows.Add(ForecastRow(new DateTime(year, month, d), 20));
            }
            var siteData = CreateSiteData(siteNumber, rate, forecastRows);

            var monthValueDto = new MonthValueDto();
            var siteDetailDto = new SiteMonthlyRevenueDetailDto { InternalRevenueBreakdown = new InternalRevenueBreakdownDto() };

            _calculator.CalculateAndApply(siteData, year, month, currentMonth, monthValueDto, siteDetailDto, 0m, new List<PnlRowDto>());

            var por = siteDetailDto.InternalRevenueBreakdown.PerOccupiedRoom;
            Assert.NotNull(por);
            Assert.Null(por.ActualRooms);
            Assert.Equal(620 * 1m, por.Total / rate); // 31 days * 20 rooms = 620; Total/rate should equal rooms
            Assert.Equal(620, por.ForecastedRooms);
            Assert.Null(por.ActualAmount);
            // Split adds to total
            var forecastAmount2 = (por.ForecastedRooms ?? 0m) * por.FeePerRoom;
            var actualAmount2 = por.ActualAmount ?? 0m;
            Assert.Equal(actualAmount2 + forecastAmount2, por.Total);
        }

        [Fact]
        public void CalculateAndApply_CurrentMonth_FullyActualized_NoForecastPortion()
        {
            var year = 2025; var month = 5; var currentMonth = 5;
            var siteNumber = "S102";
            var rate = 12m;
            var billingPeriod = $"{year}-{month:D2}";

            var edwActuals = new List<api.Models.Vo.SiteStatisticDetailVo>();
            for (int d = 1; d <= 31; d++)
            {
                edwActuals.Add(ActualRow(year, month, d, 10));
            }
            _siteStatisticRepository.GetActualData(siteNumber, billingPeriod).Returns(edwActuals);

            // Forecast rows present but should be ignored since fully actualized
            var forecastRows = new List<TownePark.Models.Vo.SiteStatisticDetailVo>();
            for (int d = 1; d <= 31; d++)
            {
                forecastRows.Add(ForecastRow(new DateTime(year, month, d), 25));
            }

            var siteData = CreateSiteData(siteNumber, rate, forecastRows);
            var monthValueDto = new MonthValueDto();
            var siteDetailDto = new SiteMonthlyRevenueDetailDto { InternalRevenueBreakdown = new InternalRevenueBreakdownDto() };

            _calculator.CalculateAndApply(siteData, year, month, currentMonth, monthValueDto, siteDetailDto, 0m, new List<PnlRowDto>());

            var por = siteDetailDto.InternalRevenueBreakdown.PerOccupiedRoom;
            Assert.NotNull(por);
            Assert.Equal(310, por.ActualRooms); // 31*10
            Assert.True(!por.ForecastedRooms.HasValue || por.ForecastedRooms.Value == 0);
            Assert.Equal(310 * rate, por.Total);
            Assert.Equal(310 * rate, por.ActualAmount);
            // Split adds to total
            var forecastAmount3 = (por.ForecastedRooms ?? 0m) * por.FeePerRoom;
            var actualAmount3 = por.ActualAmount ?? 0m;
            Assert.Equal(actualAmount3 + forecastAmount3, por.Total);
        }
    }
}