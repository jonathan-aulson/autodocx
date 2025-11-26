using api.Adapters.Impl;
using api.Models.Dto;
using api.Models.Vo;
using api.Services;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace BackendTests.Adapters
{
    public class SiteStatisticServiceAdapterTest
    {
        private readonly ISiteStatisticService _siteStatisticService;
        private readonly SiteStatisticServiceAdapter _siteStatisticServiceAdapter;

        public SiteStatisticServiceAdapterTest()
        {
            _siteStatisticService = Substitute.For<ISiteStatisticService>();
            _siteStatisticServiceAdapter = new SiteStatisticServiceAdapter(_siteStatisticService);
        }

        [Fact]
        public async Task GetSiteStatistics_ShouldCallSiteStatisticService_AndReturnAdaptedResponse()
        {
            Guid siteId = Guid.NewGuid();
            string billingPeriod = "2025-07";
            string timeRange = "daily";

            var hotelParkingDataVo = new SiteStatisticVo
            {
                SiteNumber = "0111",
                Name = "Hotel Parking",
                TotalRooms = 100,
                PeriodLabel = billingPeriod,
                TimeRangeType = TimeRangeType.DAILY,
                BudgetData = new List<SiteStatisticDetailVo>
                {
                    new SiteStatisticDetailVo
                    {
                        Id = Guid.NewGuid(),
                        Type = SiteStatisticDetailType.Budget,
                        Date = new DateOnly(2025, 7, 1),
                        PeriodLabel = new DateOnly(2025, 7, 1).ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture),
                        ValetRateDaily = 10,
                        ValetRateMonthly = 200,
                        SelfRateDaily = 5,
                        SelfRateMonthly = 100,
                        BaseRevenue = 7000,
                        OccupiedRooms = 70,
                        Occupancy = 0.91m,
                        SelfOvernight = 50,
                        ValetOvernight = 20,
                        ValetDaily = 40,
                        ValetMonthly = 60,
                        SelfDaily = 30,
                        SelfMonthly = 10,
                        ValetComps = 5,
                        SelfComps = 5,
                        DriveInRatio = 0.5,
                        CaptureRatio = 0.87
                    }
                },
                ForecastData = new List<SiteStatisticDetailVo>
                {
                    new SiteStatisticDetailVo
                    {
                        Id = Guid.NewGuid(),
                        Type = SiteStatisticDetailType.Forecast,
                        Date = new DateOnly(2025, 7, 1),
                        PeriodLabel = new DateOnly(2025, 7, 1).ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture),
                        ValetRateDaily = 10,
                        ValetRateMonthly = 200,
                        SelfRateDaily = 5,
                        SelfRateMonthly = 100,
                        BaseRevenue = 7000,
                        OccupiedRooms = 70,
                        Occupancy = 0.91m,
                        SelfOvernight = 50,
                        ValetOvernight = 20,
                        ValetDaily = 40,
                        ValetMonthly = 60,
                        SelfDaily = 30,
                        SelfMonthly = 10,
                        ValetComps = 5,
                        SelfComps = 5,
                        DriveInRatio = 0.5,
                        CaptureRatio = 0.87
                    }
                },
                ActualData = new List<SiteStatisticDetailVo>
                {
                    new SiteStatisticDetailVo
                    {
                        Id = Guid.NewGuid(),
                        Type = SiteStatisticDetailType.Actual,
                        Date = new DateOnly(2025, 7, 1),
                        PeriodLabel = new DateOnly(2025, 7, 1).ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture),
                        ValetRateDaily = 10,
                        ValetRateMonthly = 200,
                        SelfRateDaily = 5,
                        SelfRateMonthly = 100,
                        BaseRevenue = 7000,
                        OccupiedRooms = 70,
                        Occupancy = 0.91m,
                        SelfOvernight = 50,
                        ValetOvernight = 20,
                        ValetDaily = 40,
                        ValetMonthly = 60,
                        SelfDaily = 30,
                        SelfMonthly = 10,
                        ValetComps = 5,
                        SelfComps = 5,
                        DriveInRatio = 0.5,
                        CaptureRatio = 0.87
                    }
                }
            };

            _siteStatisticService.GetSiteStatistics(siteId, billingPeriod, timeRange)
                .Returns(Task.FromResult<IEnumerable<SiteStatisticVo>>(new List<SiteStatisticVo> { hotelParkingDataVo }));

            var siteStatisticDto = new SiteStatisticDto
            {
                SiteNumber = "0111",
                Name = "Hotel Parking",
                TotalRooms = 100,
                PeriodLabel = billingPeriod,
                TimeRangeType = "DAILY",
                BudgetData = new List<SiteStatisticDetailDto>
            {
                new SiteStatisticDetailDto
                {
                    Id = hotelParkingDataVo.BudgetData[0].Id,
                    Type = "Budget",
                    PeriodStart = new DateOnly(),
                    PeriodEnd = new DateOnly(),
                    PeriodLabel = new DateOnly(2025, 7, 1).ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture),
                    ValetRateDaily = 10,
                    ValetRateMonthly = 200,
                    SelfRateDaily = 5,
                    SelfRateMonthly = 100,
                    BaseRevenue = 7000,
                    OccupiedRooms = 70,
                    Occupancy = 0.91m,
                    SelfOvernight = 50,
                    ValetOvernight = 20,
                    ValetDaily = 40,
                    ValetMonthly = 60,
                    SelfDaily = 30,
                    SelfMonthly = 10,
                    ValetComps = 5,
                    SelfComps = 5,
                    DriveInRatio = 0.5,
                    CaptureRatio = 0.87
                }
            },
                ForecastData = new List<SiteStatisticDetailDto>
            {
                new SiteStatisticDetailDto
                {
                    Id = hotelParkingDataVo.ForecastData[0].Id,
                    Type = "Forecast",
                    PeriodStart = new DateOnly(),
                    PeriodEnd = new DateOnly(),
                    PeriodLabel = new DateOnly(2025, 7, 1).ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture),
                    ValetRateDaily = 10,
                    ValetRateMonthly = 200,
                    SelfRateDaily = 5,
                    SelfRateMonthly = 100,
                    BaseRevenue = 7000,
                    OccupiedRooms = 70,
                    Occupancy = 0.91m,
                    SelfOvernight = 50,
                    ValetOvernight = 20,
                    ValetDaily = 40,
                    ValetMonthly = 60,
                    SelfDaily = 30,
                    SelfMonthly = 10,
                    ValetComps = 5,
                    SelfComps = 5,
                    DriveInRatio = 0.5,
                    CaptureRatio = 0.87
                }
            },
                ActualData = new List<SiteStatisticDetailDto>
            {
                new SiteStatisticDetailDto
                {
                    Id = hotelParkingDataVo.ActualData[0].Id,
                    Type = "Actual",
                    PeriodStart = new DateOnly(),
                    PeriodEnd = new DateOnly(),
                    PeriodLabel = new DateOnly(2025, 7, 1).ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture),
                    ValetRateDaily = 10,
                    ValetRateMonthly = 200,
                    SelfRateDaily = 5,
                    SelfRateMonthly = 100,
                    BaseRevenue = 7000,
                    OccupiedRooms = 70,
                    Occupancy = 0.91m,
                    SelfOvernight = 50,
                    ValetOvernight = 20,
                    ValetDaily = 40,
                    ValetMonthly = 60,
                    SelfDaily = 30,
                    SelfMonthly = 10,
                    ValetComps = 5,
                    SelfComps = 5,
                    DriveInRatio = 0.5,
                    CaptureRatio = 0.87
                }
            }   
            };

            var result = _siteStatisticServiceAdapter.GetSiteStatistics(siteId, billingPeriod, timeRange);

            result.Should().BeEquivalentTo(new List<SiteStatisticDto> { siteStatisticDto });
        }


        [Fact]
        public void SaveSiteStatistics_ShouldReturnOk_WhenUpdateIsSuccessful()
        {
            var siteStatisticId = Guid.NewGuid();

            var SiteStatisticDto = new SiteStatisticDto
            {
                Id = siteStatisticId,
                SiteNumber = "0111",
                CustomerSiteId = Guid.NewGuid(),
                Name = "Hotel Parking",
                TotalRooms = 100,
                PeriodLabel = "2025-07",
                BudgetData = new List<SiteStatisticDetailDto>
                {
                    new SiteStatisticDetailDto
                    {
                        Id = Guid.NewGuid(),
                        Type = "Budget",
                        PeriodLabel = new DateOnly(2025, 7, 1).ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture),
                        ValetRateDaily = 10,
                        ValetRateMonthly = 200,
                        SelfRateDaily = 5,
                        SelfRateMonthly = 100,
                        BaseRevenue = 7000,
                        OccupiedRooms = 70,
                        Occupancy = 0.91m,
                        SelfOvernight = 50,
                        ValetOvernight = 20,
                        ValetDaily = 40,
                        ValetMonthly = 60,
                        SelfDaily = 30,
                        SelfMonthly = 10,
                        ValetComps = 5,
                        SelfComps = 5,
                        DriveInRatio = 0.5,
                        CaptureRatio = 0.87
                    }
                },
                ForecastData = new List<SiteStatisticDetailDto>
                {
                    new SiteStatisticDetailDto
                    {
                        Id = Guid.NewGuid(),
                        Type = "Forecast",
                        PeriodLabel = new DateOnly(2025, 7, 1).ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture),
                        ValetRateDaily = 10,
                        ValetRateMonthly = 200,
                        SelfRateDaily = 5,
                        SelfRateMonthly = 100,
                        BaseRevenue = 7000,
                        OccupiedRooms = 70,
                        Occupancy = 0.91m,
                        SelfOvernight = 50,
                        ValetOvernight = 20,
                        ValetDaily = 40,
                        ValetMonthly = 60,
                        SelfDaily = 30,
                        SelfMonthly = 10,
                        ValetComps = 5,
                        SelfComps = 5,
                        DriveInRatio = 0.5,
                        CaptureRatio = 0.87
                    }
                }
            };

            var expectedVo = new SiteStatisticVo
            {
                SiteNumber = "0111",
                Name = "Hotel Parking",
                TotalRooms = 100,
                PeriodLabel = "2025-07",
                BudgetData = new List<SiteStatisticDetailVo>
                {
                    new SiteStatisticDetailVo
                    {
                        Id = Guid.NewGuid(),
                        Type = SiteStatisticDetailType.Budget,
                        Date = new DateOnly(2025, 7, 1),
                        ValetRateDaily = 10,
                        ValetRateMonthly = 200,
                        SelfRateDaily = 5,
                        SelfRateMonthly = 100,
                        BaseRevenue = 7000,
                        OccupiedRooms = 70,
                        Occupancy = 0.91m,
                        SelfOvernight = 50,
                        ValetOvernight = 20,
                        ValetDaily = 40,
                        ValetMonthly = 60,
                        SelfDaily = 30,
                        SelfMonthly = 10,
                        ValetComps = 5,
                        SelfComps = 5,
                        DriveInRatio = 0.5,
                        CaptureRatio = 0.87
                    }
                },
                ForecastData = new List<SiteStatisticDetailVo>
                {
                    new SiteStatisticDetailVo
                    {
                        Id = Guid.NewGuid(),
                        Type = SiteStatisticDetailType.Forecast,
                        Date = new DateOnly(2025, 7, 1),
                        ValetRateDaily = 10,
                        ValetRateMonthly = 200,
                        SelfRateDaily = 5,
                        SelfRateMonthly = 100,
                        BaseRevenue = 7000,
                        OccupiedRooms = 70,
                        Occupancy = 0.91m,
                        SelfOvernight = 50,
                        ValetOvernight = 20,
                        ValetDaily = 40,
                        ValetMonthly = 60,
                        SelfDaily = 30,
                        SelfMonthly = 10,
                        ValetComps = 5,
                        SelfComps = 5,
                        DriveInRatio = 0.5,
                        CaptureRatio = 0.87
                    }
                }
            };

            _siteStatisticServiceAdapter.SaveSiteStatistics(SiteStatisticDto);

            _siteStatisticService.Received(1).SaveSiteStatistics(Arg.Is<SiteStatisticVo>(vo => expectedVo.Equals(expectedVo)));
        }

        [Fact]
        public async Task GetSiteStatistics_ShouldReturnAdjustmentFields_WhenPresent()
        {
            // Arrange
            Guid siteId = Guid.NewGuid();
            string billingPeriod = "2025-07";
            string timeRange = "daily";

            var siteStatisticVo = new SiteStatisticVo
            {
                SiteNumber = "0111",
                Name = "Hotel Parking",
                TotalRooms = 100,
                PeriodLabel = billingPeriod,
                TimeRangeType = TimeRangeType.DAILY,
                BudgetData = new List<SiteStatisticDetailVo>
                {
                    new SiteStatisticDetailVo
                    {
                        Id = Guid.NewGuid(),
                        Type = SiteStatisticDetailType.Budget,
                        Date = new DateOnly(2025, 7, 1),
                        PeriodLabel = new DateOnly(2025, 7, 1).ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture),
                        ExternalRevenue = 10000,
                        AdjustmentPercentage = -3.42m,
                        AdjustmentValue = -342m
                    }
                },
                ForecastData = new List<SiteStatisticDetailVo>
                {
                    new SiteStatisticDetailVo
                    {
                        Id = Guid.NewGuid(),
                        Type = SiteStatisticDetailType.Forecast,
                        Date = new DateOnly(2025, 7, 1),
                        PeriodLabel = new DateOnly(2025, 7, 1).ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture),
                        ExternalRevenue = 12000,
                        AdjustmentPercentage = -3.98m,
                        AdjustmentValue = -477.6m
                    }
                }
            };

            _siteStatisticService.GetSiteStatistics(siteId, billingPeriod, timeRange)
                .Returns(Task.FromResult<IEnumerable<SiteStatisticVo>>(new List<SiteStatisticVo> { siteStatisticVo }));

            // Act
            var result = _siteStatisticServiceAdapter.GetSiteStatistics(siteId, billingPeriod, timeRange);

            // Assert
            result.Should().NotBeNull();
            var resultList = result.ToList();
            resultList.Should().HaveCount(1);
            
            var firstResult = resultList[0];
            firstResult.BudgetData.Should().HaveCount(1);
            firstResult.ForecastData.Should().HaveCount(1);
            
            // Verify Budget adjustment fields
            var budgetDetail = firstResult.BudgetData[0];
            budgetDetail.AdjustmentPercentage.Should().Be(-3.42m);
            budgetDetail.AdjustmentValue.Should().Be(-342m);
            
            // Verify Forecast adjustment fields
            var forecastDetail = firstResult.ForecastData[0];
            forecastDetail.AdjustmentPercentage.Should().Be(-3.98m);
            forecastDetail.AdjustmentValue.Should().Be(-477.6m);
        }

        [Fact]
        public void SaveSiteStatistics_ShouldSaveAdjustmentFields_WhenProvided()
        {
            // Arrange
            var siteStatisticId = Guid.NewGuid();
            var detailId = Guid.NewGuid();

            var siteStatisticDto = new SiteStatisticDto
            {
                Id = siteStatisticId,
                SiteNumber = "0111",
                CustomerSiteId = Guid.NewGuid(),
                Name = "Hotel Parking",
                TotalRooms = 100,
                PeriodLabel = "2025-07",
                BudgetData = new List<SiteStatisticDetailDto>
                {
                    new SiteStatisticDetailDto
                    {
                        Id = detailId,
                        Type = "Budget",
                        PeriodLabel = new DateOnly(2025, 7, 1).ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture),
                        ExternalRevenue = 10000,
                        AdjustmentPercentage = -5.09m,
                        AdjustmentValue = -509m,
                        ValetRateDaily = 10,
                        ValetRateMonthly = 200,
                        SelfRateDaily = 5,
                        SelfRateMonthly = 100
                    }
                }
            };

            // Act
            _siteStatisticServiceAdapter.SaveSiteStatistics(siteStatisticDto);

            // Assert
            _siteStatisticService.Received(1).SaveSiteStatistics(Arg.Is<SiteStatisticVo>(vo => 
                vo.BudgetData != null &&
                vo.BudgetData.Count == 1 &&
                vo.BudgetData[0].AdjustmentPercentage == -5.09m &&
                vo.BudgetData[0].AdjustmentValue == -509m
            ));
        }

        [Fact]
        public void SaveSiteStatistics_ShouldHandleNullAdjustmentFields()
        {
            // Arrange
            var siteStatisticDto = new SiteStatisticDto
            {
                Id = Guid.NewGuid(),
                SiteNumber = "0111",
                CustomerSiteId = Guid.NewGuid(),
                Name = "Hotel Parking",
                TotalRooms = 100,
                PeriodLabel = "2025-07",
                ForecastData = new List<SiteStatisticDetailDto>
                {
                    new SiteStatisticDetailDto
                    {
                        Id = Guid.NewGuid(),
                        Type = "Forecast",
                        PeriodLabel = new DateOnly(2025, 7, 1).ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture),
                        ExternalRevenue = 8000,
                        AdjustmentPercentage = null,
                        AdjustmentValue = null,
                        ValetRateDaily = 10,
                        SelfRateDaily = 5
                    }
                }
            };

            // Act
            _siteStatisticServiceAdapter.SaveSiteStatistics(siteStatisticDto);

            // Assert
            _siteStatisticService.Received(1).SaveSiteStatistics(Arg.Is<SiteStatisticVo>(vo => 
                vo.ForecastData != null &&
                vo.ForecastData.Count == 1 &&
                vo.ForecastData[0].AdjustmentPercentage == null &&
                vo.ForecastData[0].AdjustmentValue == null
            ));
        }
    }
}
