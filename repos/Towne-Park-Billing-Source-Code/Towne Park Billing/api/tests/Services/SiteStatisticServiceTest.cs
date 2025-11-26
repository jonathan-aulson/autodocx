using api.Data;
using api.Models.Vo;
using api.Services.Impl;
using api.Usecases;
using FluentAssertions;
using Microsoft.Xrm.Sdk;
using NSubstitute;
using System.Linq;
using TownePark;
using Xunit;

namespace BackendTests.Services
{
    public class SiteStatisticServiceTest
    {
        private readonly SiteStatisticService _siteStatisticService;
        private readonly ISiteStatisticRepository _siteStatisticRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IMonthRangeGenerator _monthRangeGenerator;
        private readonly IParkingRateRepository _parkingRateRepository;

        public SiteStatisticServiceTest()
        {
            _siteStatisticRepository = Substitute.For<ISiteStatisticRepository>();
            _customerRepository = Substitute.For<ICustomerRepository>();
            _monthRangeGenerator = Substitute.For<IMonthRangeGenerator>();
            _parkingRateRepository = Substitute.For<IParkingRateRepository>();
            _siteStatisticService = new SiteStatisticService(_siteStatisticRepository, _customerRepository, _monthRangeGenerator, _parkingRateRepository);
        }

        [Fact]
        public async Task GetSiteStatistics_ShouldCallRepository_AndReturnAdaptedResponse()
        {
            var siteId = Guid.NewGuid();
            var billingPeriod = "2025-07";
            var timeRange = "daily";

            // Mock month range generator to return only the billing period for test simplicity
            var months = new List<string> { billingPeriod };
            _monthRangeGenerator.GenerateMonthRange(billingPeriod, 3).Returns(months);

            // Mock budget data for range
            var budgetData = new List<SiteStatisticDetailVo>
            {
                new SiteStatisticDetailVo
                {
                    Date = new DateOnly(2025, 7, 1),
                    Type = SiteStatisticDetailType.Budget,
                    ValetRateDaily = 35.00m,
                    ValetRateMonthly = 350.00m,
                    SelfRateDaily = 25.00m,
                    SelfRateMonthly = 250.00m,
                    BaseRevenue = 6750.00m,
                    OccupiedRooms = 142,
                    Occupancy = 0.91m,
                    SelfOvernight = 47,
                    ValetOvernight = 32,
                    ValetDaily = 42,
                    ValetMonthly = 165,
                    SelfDaily = 58,
                    SelfMonthly = 205,
                    ValetComps = 8,
                    SelfComps = 12,
                    DriveInRatio = 78,
                    CaptureRatio = 87
                }
            };
            _siteStatisticRepository.GetBudgetDataForRange(Arg.Any<string>(), months, Arg.Any<int>()).Returns(Task.FromResult(budgetData));

            // Mock actual data for range
            var actualData = new List<SiteStatisticDetailVo> {
                new SiteStatisticDetailVo
                {
                    Date = new DateOnly(2025, 7, 1),
                    Type = SiteStatisticDetailType.Actual,
                    ValetRateDaily = 35.00m,
                    ValetRateMonthly = 350.00m,
                    SelfRateDaily = 25.00m,
                    SelfRateMonthly = 250.00m,
                    BaseRevenue = 6750.00m,
                    OccupiedRooms = 142,
                    Occupancy = 0.91m,
                    SelfOvernight = 47,
                    ValetOvernight = 32,
                    ValetDaily = 42,
                    ValetMonthly = 165,
                    SelfDaily = 58,
                    SelfMonthly = 205,
                    ValetComps = 8,
                    SelfComps = 12,
                    DriveInRatio = 78,
                    CaptureRatio = 87
                }
            };
            _siteStatisticRepository.GetActualDataForRange(Arg.Any<string>(), months).Returns(Task.FromResult(actualData));

            var siteStatisticModel = new bs_SiteStatistic
            {
                bs_SiteStatisticId = siteId,
                bs_CustomerSiteFK = new EntityReference("bs_customersite", siteId) { Name = "0111" },
                bs_BillingPeriod = billingPeriod,
                bs_Name = "Site Statistic",
                bs_SiteStatistic_CustomerSite = new bs_CustomerSite
                {
                    bs_TotalRoomsAvailable = "200"
                },
                bs_SiteStatistic_SiteStatisticDetail = new List<bs_SiteStatisticDetail>
                {
                    new bs_SiteStatisticDetail
                    {
                        bs_Type = bs_sitestatisticdetailchoice.Forecast,
                        bs_Date = new DateTime(2025, 7, 1),
                        bs_ValetRateDaily = 35.00m,
                        bs_ValetRateMonthly = 350.00m,
                        bs_SelfRateDaily = 25.00m,
                        bs_SelfRateMonthly = 250.00m,
                        bs_BaseRevenue = 6750.00m,
                        bs_OccupiedRooms = 142,
                        bs_Occupancy = 0.91m,
                        bs_SelfOvernight = 47,
                        bs_ValetOvernight = 32,
                        bs_ValetDaily = 42,
                        bs_ValetMonthly = 165,
                        bs_SelfDaily = 58,
                        bs_SelfMonthly = 205,
                        bs_ValetComps = 8,
                        bs_SelfComps = 12,
                        bs_DriveInRatio = 78,
                        bs_CaptureRatio = 87
                    },
                    new bs_SiteStatisticDetail
                    {
                        bs_Type = bs_sitestatisticdetailchoice.Budget,
                        bs_Date = new DateTime(2025, 7, 1),
                        bs_ValetRateDaily = 35.00m,
                        bs_ValetRateMonthly = 350.00m,
                        bs_SelfRateDaily = 25.00m,
                        bs_SelfRateMonthly = 250.00m,
                        bs_BaseRevenue = 6750.00m,
                        bs_OccupiedRooms = 142,
                        bs_Occupancy = 0.91m,
                        bs_SelfOvernight = 47,
                        bs_ValetOvernight = 32,
                        bs_ValetDaily = 42,
                        bs_ValetMonthly = 165,
                        bs_SelfDaily = 58,
                        bs_SelfMonthly = 205,
                        bs_ValetComps = 8,
                        bs_SelfComps = 12,
                        bs_DriveInRatio = 78,
                        bs_CaptureRatio = 87
                    },
                    new bs_SiteStatisticDetail
                    {
                        bs_Type = bs_sitestatisticdetailchoice.Actual,
                        bs_Date = new DateTime(2025, 7, 1),
                        bs_ValetRateDaily = 35.00m,
                        bs_ValetRateMonthly = 350.00m,
                        bs_SelfRateDaily = 25.00m,
                        bs_SelfRateMonthly = 250.00m,
                        bs_BaseRevenue = 6750.00m,
                        bs_OccupiedRooms = 142,
                        bs_Occupancy = 0.91m,
                        bs_SelfOvernight = 47,
                        bs_ValetOvernight = 32,
                        bs_ValetDaily = 42,
                        bs_ValetMonthly = 165,
                        bs_SelfDaily = 58,
                        bs_SelfMonthly = 205,
                        bs_ValetComps = 8,
                        bs_SelfComps = 12,
                        bs_DriveInRatio = 78,
                        bs_CaptureRatio = 87
                    }
                }
            };

            _siteStatisticRepository.GetSiteStatistics(siteId, billingPeriod).Returns(siteStatisticModel);

            var expectedVo = new SiteStatisticVo
            {
                Id = siteStatisticModel.Id,
                SiteNumber = siteStatisticModel.bs_CustomerSiteFK?.Name ?? "",
                CustomerSiteId = siteId,
                PeriodLabel = billingPeriod,
                Name = "Site Statistic",
                TotalRooms = siteStatisticModel.bs_SiteStatistic_CustomerSite?.bs_TotalRoomsAvailable != null
                    ? int.Parse(siteStatisticModel.bs_SiteStatistic_CustomerSite.bs_TotalRoomsAvailable)
                    : 0,
                ForecastData = new List<SiteStatisticDetailVo>
                {
                    new SiteStatisticDetailVo
                    {
                        Date = new DateOnly(2025, 7, 1),
                        PeriodLabel = new DateOnly(2025, 7, 1).ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture),
                        Type = SiteStatisticDetailType.Forecast,
                        ValetRateDaily = 35.00m,
                        ValetRateMonthly = 350.00m,
                        SelfRateDaily = 25.00m,
                        SelfRateMonthly = 250.00m,
                        BaseRevenue = 6750.00m,
                        OccupiedRooms = 142,
                        Occupancy = 0.91m,
                        SelfOvernight = 47,
                        ValetOvernight = 32,
                        ValetDaily = 42,
                        ValetMonthly = 165,
                        SelfDaily = 58,
                        SelfMonthly = 205,
                        ValetComps = 8,
                        SelfComps = 12,
                        DriveInRatio = 78,
                        CaptureRatio = 87
                    }
                },
                BudgetData = new List<SiteStatisticDetailVo>
                {
                    new SiteStatisticDetailVo
                    {
                        Date = new DateOnly(2025, 7, 1),
                        Type = SiteStatisticDetailType.Budget,
                        ValetRateDaily = 35.00m,
                        ValetRateMonthly = 350.00m,
                        SelfRateDaily = 25.00m,
                        SelfRateMonthly = 250.00m,
                        BaseRevenue = 6750.00m,
                        OccupiedRooms = 142,
                        Occupancy = 0.91m,
                        SelfOvernight = 47,
                        ValetOvernight = 32,
                        ValetDaily = 42,
                        ValetMonthly = 165,
                        SelfDaily = 58,
                        SelfMonthly = 205,
                        ValetComps = 8,
                        SelfComps = 12,
                        DriveInRatio = 78,
                        CaptureRatio = 87
                    }
                },
                ActualData = new List<SiteStatisticDetailVo>
                {
                    new SiteStatisticDetailVo
                    {
                        Date = new DateOnly(2025, 7, 1),
                        Type = SiteStatisticDetailType.Actual,
                        ValetRateDaily = 35.00m,
                        ValetRateMonthly = 350.00m,
                        SelfRateDaily = 25.00m,
                        SelfRateMonthly = 250.00m,
                        BaseRevenue = 6750.00m,
                        OccupiedRooms = 142,
                        Occupancy = 0.71m,
                        SelfOvernight = 47,
                        ValetOvernight = 32,
                        ValetDaily = 42,
                        ValetMonthly = 165,
                        SelfDaily = 58,
                        SelfMonthly = 205,
                        ValetComps = 8,
                        SelfComps = 12,
                        DriveInRatio = 78,
                        CaptureRatio = 87
                    }
                },
            };

            var result = await _siteStatisticService.GetSiteStatistics(siteId, billingPeriod, timeRange);

            result.Should().BeEquivalentTo(new List<SiteStatisticVo> { expectedVo });
        }

        [Fact]
        public void SaveSiteStatistics_ShouldCallSaveSiteStatics_WhenIdIsNotNull()
        {
            var siteStatisticId = Guid.NewGuid();
            var customerSiteId = Guid.NewGuid();

            var hotelParkingDataVo = new SiteStatisticVo
            {
                Id = siteStatisticId,
                SiteNumber = "0111",
                CustomerSiteId = customerSiteId,
                Name = "Site Statistic",
                TotalRooms = 200,
                PeriodLabel = "2025-07",
                ForecastData = new List<SiteStatisticDetailVo>
                {
                    new SiteStatisticDetailVo
                    {
                        Date = new DateOnly(2025, 7, 1),
                        Type = SiteStatisticDetailType.Forecast,
                        PeriodLabel = new DateOnly(2025, 7, 1).ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture),
                        ValetRateDaily = 35.00m,
                        ValetRateMonthly = 350.00m,
                        SelfRateDaily = 25.00m,
                        SelfRateMonthly = 250.00m,
                        BaseRevenue = 6750.00m,
                        OccupiedRooms = 142,
                        Occupancy = 0.91m,
                        SelfOvernight = 47,
                        ValetOvernight = 32,
                        ValetDaily = 42,
                        ValetMonthly = 165,
                        SelfDaily = 58,
                        SelfMonthly = 205,
                        ValetComps = 8,
                        SelfComps = 12,
                        DriveInRatio = 78,
                        CaptureRatio = 87
                    }
                },
                BudgetData = new List<SiteStatisticDetailVo>
                {
                    new SiteStatisticDetailVo
                    {
                        Date = new DateOnly(2025, 7, 1),
                        Type = SiteStatisticDetailType.Budget,
                        PeriodLabel = new DateOnly(2025, 7, 1).ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture),
                        ValetRateDaily = 35.00m,
                        ValetRateMonthly = 350.00m,
                        SelfRateDaily = 25.00m,
                        SelfRateMonthly = 250.00m,
                        BaseRevenue = 6750.00m,
                        OccupiedRooms = 142,
                        Occupancy = 0.91m,
                        SelfOvernight = 47,
                        ValetOvernight = 32,
                        ValetDaily = 42,
                        ValetMonthly = 165,
                        SelfDaily = 58,
                        SelfMonthly = 205,
                        ValetComps = 8,
                        SelfComps = 12,
                        DriveInRatio = 78,
                        CaptureRatio = 87
                    }
                }
            };

            var expectedModel = new bs_SiteStatistic
            {
                bs_SiteStatisticId = customerSiteId,
                bs_CustomerSiteFK = new EntityReference("bs_customersite", customerSiteId) { Name = "0111" },
                bs_BillingPeriod = "2025-07",
                bs_Name = "Site Statistic",
                bs_SiteStatistic_CustomerSite = new bs_CustomerSite
                {
                    bs_TotalRoomsAvailable = "200"
                },
                bs_SiteStatistic_SiteStatisticDetail = new List<bs_SiteStatisticDetail>
                {
                    new bs_SiteStatisticDetail
                    {
                        bs_Type = bs_sitestatisticdetailchoice.Forecast,
                        bs_Date = new DateTime(2025, 3, 1),
                        bs_ValetRateDaily = 35.00m,
                        bs_ValetRateMonthly = 350.00m,
                        bs_SelfRateDaily = 25.00m,
                        bs_SelfRateMonthly = 250.00m,
                        bs_BaseRevenue = 6750.00m,
                        bs_OccupiedRooms = 142,
                        bs_Occupancy = 0.91m,
                        bs_SelfOvernight = 47,
                        bs_ValetOvernight = 32,
                        bs_ValetDaily = 42,
                        bs_ValetMonthly = 165,
                        bs_SelfDaily = 58,
                        bs_SelfMonthly = 205,
                        bs_ValetComps = 8,
                        bs_SelfComps = 12,
                        bs_DriveInRatio = 78,
                        bs_CaptureRatio = 87
                    },
                    new bs_SiteStatisticDetail
                    {
                        bs_Type = bs_sitestatisticdetailchoice.Budget,
                        bs_Date = new DateTime(2025, 3, 1),
                        bs_ValetRateDaily = 35.00m,
                        bs_ValetRateMonthly = 350.00m,
                        bs_SelfRateDaily = 25.00m,
                        bs_SelfRateMonthly = 250.00m,
                        bs_BaseRevenue = 6750.00m,
                        bs_OccupiedRooms = 142,
                        bs_Occupancy = 0.91m,
                        bs_SelfOvernight = 47,
                        bs_ValetOvernight = 32,
                        bs_ValetDaily = 42,
                        bs_ValetMonthly = 165,
                        bs_SelfDaily = 58,
                        bs_SelfMonthly = 205,
                        bs_ValetComps = 8,
                        bs_SelfComps = 12,
                        bs_DriveInRatio = 78,
                        bs_CaptureRatio = 87
                    }
                }
            };

            _siteStatisticService.SaveSiteStatistics(hotelParkingDataVo);
            _siteStatisticRepository.Received(1).SaveSiteStatistics(Arg.Any<bs_SiteStatistic>());
        }

        [Fact]
        public async Task GetSiteStatistics_Weekly_ShouldReturnValidSiteStatistic()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var billingPeriod = "2025-07";
            var timeRange = "WEEKLY";
            var months = new List<string> { "2025-07", "2025-08", "2025-09" };
            
            // Mock month range generator
            _monthRangeGenerator.GenerateMonthRange(billingPeriod, 3).Returns(months);
            
            // Mock customer repository
            var customerDetail = new bs_CustomerSite
            {
                bs_CustomerSiteId = siteId,
                bs_SiteNumber = "0111",
                bs_TotalRoomsAvailable = "200"
            };
            _customerRepository.GetCustomerDetail(siteId).Returns(customerDetail);
            
            // Mock site statistics repository
            var siteStatistics = new List<bs_SiteStatistic>
            {
                new bs_SiteStatistic
                {
                    Id = Guid.NewGuid(),
                    bs_BillingPeriod = "2025-07",
                    bs_CustomerSiteFK = new EntityReference("bs_customersite", siteId),
                    bs_SiteStatistic_CustomerSite = new bs_CustomerSite
                    {
                        bs_SiteNumber = "0111",
                        bs_TotalRoomsAvailable = "200"
                    }
                }
            };
            _siteStatisticRepository.GetSiteStatisticsByRange(siteId, billingPeriod, 3).Returns(siteStatistics);
            
            // Mock budget data for range
            var budgetData = new List<SiteStatisticDetailVo>();
            foreach (var month in months)
            {
                // Add daily data for each month (simplified for test)
                var date = DateTime.ParseExact(month, "yyyy-MM", System.Globalization.CultureInfo.InvariantCulture);
                for (int day = 1; day <= 28; day++) // Just use 28 days for simplicity
                {
                    budgetData.Add(new SiteStatisticDetailVo
                    {
                        Date = new DateOnly(date.Year, date.Month, day),
                        Type = SiteStatisticDetailType.Budget,
                        BaseRevenue = 200m,
                        OccupiedRooms = 150,
                        Occupancy = 0.75m
                    });
                }
            }
            _siteStatisticRepository.GetBudgetDataForRange("0111", months, Arg.Any<int>()).Returns(Task.FromResult(budgetData));

            // Mock actual data for range
            var actualData = new List<SiteStatisticDetailVo>();
            foreach (var month in months)
            {
                // Add daily data for each month (simplified for test)
                var date = DateTime.ParseExact(month, "yyyy-MM", System.Globalization.CultureInfo.InvariantCulture);
                for (int day = 1; day <= 28; day++) // Just use 28 days for simplicity
                {
                    actualData.Add(new SiteStatisticDetailVo
                    {
                        Date = new DateOnly(date.Year, date.Month, day),
                        Type = SiteStatisticDetailType.Actual,
                        BaseRevenue = 250m,
                        OccupiedRooms = 160,
                        Occupancy = 0.80m
                    });
                }
            }

            // Act
            var result = await _siteStatisticService.GetSiteStatistics(siteId, billingPeriod, timeRange);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3); // Service returns 3 SiteStatisticVo objects for 3 months
            
            // Verify each result has weekly structure
            var expectedPeriods = new[] { "2025-07", "2025-08", "2025-09" };
            for (int i = 0; i < result.Count(); i++)
            {
                var siteStatistic = result.ElementAt(i);
                siteStatistic.TimeRangeType.Should().Be(TimeRangeType.WEEKLY);
                siteStatistic.PeriodLabel.Should().Be(expectedPeriods[i]);
                siteStatistic.BudgetData.Should().NotBeNull();
                // Note: BudgetData contains weekly entries for the month
            }
        }

        [Fact]
        public async Task GetSiteStatistics_Monthly_ShouldReturnValidSiteStatistic()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var billingPeriod = "2025-07";
            var timeRange = "MONTHLY";
            var months = new List<string> { "2025-07", "2025-08", "2025-09" };
            
            // Mock month range generator
            _monthRangeGenerator.GenerateMonthRange(billingPeriod, 3).Returns(months);
            
            // Mock customer repository
            var customerDetail = new bs_CustomerSite
            {
                bs_CustomerSiteId = siteId,
                bs_SiteNumber = "0111",
                bs_TotalRoomsAvailable = "200"
            };
            _customerRepository.GetCustomerDetail(siteId).Returns(customerDetail);
            
            // Mock site statistics repository - return 3 months of data
            var siteStatistics = new List<bs_SiteStatistic>();
            foreach (var month in months)
            {
                siteStatistics.Add(new bs_SiteStatistic
                {
                    Id = Guid.NewGuid(),
                    bs_BillingPeriod = month,
                    bs_CustomerSiteFK = new EntityReference("bs_customersite", siteId),
                    bs_SiteStatistic_CustomerSite = new bs_CustomerSite
                    {
                        bs_SiteNumber = "0111",
                        bs_TotalRoomsAvailable = "200"
                    }
                });
            }
            _siteStatisticRepository.GetSiteStatisticsByRange(siteId, billingPeriod, 3).Returns(siteStatistics);
            
            // Mock budget data for range
            var budgetData = new List<SiteStatisticDetailVo>();
            foreach (var month in months)
            {
                var date = DateTime.ParseExact(month, "yyyy-MM", System.Globalization.CultureInfo.InvariantCulture);
                budgetData.Add(new SiteStatisticDetailVo
                {
                    Date = new DateOnly(date.Year, date.Month, 1),
                    Type = SiteStatisticDetailType.Budget,
                    BaseRevenue = 5000m,
                    OccupiedRooms = 150,
                    Occupancy = 0.75m
                });
            }
            _siteStatisticRepository.GetBudgetDataForRange("0111", months, Arg.Any<int>()).Returns(Task.FromResult(budgetData));

            // Mock actual data for range
            var actualData = new List<SiteStatisticDetailVo>();
            foreach (var month in months) {
                var date = DateTime.ParseExact(month, "yyyy-MM", System.Globalization.CultureInfo.InvariantCulture);
                actualData.Add(new SiteStatisticDetailVo
                {
                    Date = new DateOnly(date.Year, date.Month, 1),
                    Type = SiteStatisticDetailType.Actual,
                    BaseRevenue = 6000m,
                    OccupiedRooms = 160,
                    Occupancy = 0.80m
                });
            }
            _siteStatisticRepository.GetActualDataForRange("0111", months).Returns(Task.FromResult(actualData));

            // Act
            var result = await _siteStatisticService.GetSiteStatistics(siteId, billingPeriod, timeRange);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3); // Service returns 3 SiteStatisticVo objects for 3 months
            
            // Verify each result has the expected structure
            var expectedPeriods = new[] { "2025-07", "2025-08", "2025-09" };
            for (int i = 0; i < result.Count(); i++)
            {
                var siteStatistic = result.ElementAt(i);
                siteStatistic.TimeRangeType.Should().Be(TimeRangeType.MONTHLY);
                siteStatistic.PeriodLabel.Should().Be(expectedPeriods[i]);
                siteStatistic.BudgetData.Should().NotBeNull();
                siteStatistic.ActualData.Should().NotBeNull();
                // Each month should have budget data
            }
        }

        [Fact]
        public void SaveSiteStatistics_ShouldCallCreateSiteStatic_WhenIdIsNull()
        {
            var siteStatisticId = Guid.NewGuid();
            var customerSiteId = Guid.NewGuid();

            var hotelParkingDataVo = new SiteStatisticVo
            {
                SiteNumber = "0111",
                CustomerSiteId = customerSiteId,
                Name = "Site Statistic",
                TotalRooms = 200,
                PeriodLabel = "2025-07",
                ForecastData = new List<SiteStatisticDetailVo>
                {
                    new SiteStatisticDetailVo
                    {
                        Date = new DateOnly(2025, 7, 1),
                        Type = SiteStatisticDetailType.Forecast,
                        PeriodLabel = new DateOnly(2025, 7, 1).ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture),
                        ValetRateDaily = 35.00m,
                        ValetRateMonthly = 350.00m,
                        SelfRateDaily = 25.00m,
                        SelfRateMonthly = 250.00m,
                        BaseRevenue = 6750.00m,
                        OccupiedRooms = 142,
                        Occupancy = 0.91m,
                        SelfOvernight = 47,
                        ValetOvernight = 32,
                        ValetDaily = 42,
                        ValetMonthly = 165,
                        SelfDaily = 58,
                        SelfMonthly = 205,
                        ValetComps = 8,
                        SelfComps = 12,
                        DriveInRatio = 78,
                        CaptureRatio = 87
                    }
                },
                BudgetData = new List<SiteStatisticDetailVo>
                {
                    new SiteStatisticDetailVo
                    {
                        Date = new DateOnly(2025, 7, 1),
                        Type = SiteStatisticDetailType.Budget,
                        PeriodLabel = new DateOnly(2025, 7, 1).ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture),
                        ValetRateDaily = 35.00m,
                        ValetRateMonthly = 350.00m,
                        SelfRateDaily = 25.00m,
                        SelfRateMonthly = 250.00m,
                        BaseRevenue = 6750.00m,
                        OccupiedRooms = 142,
                        Occupancy = 0.91m,
                        SelfOvernight = 47,
                        ValetOvernight = 32,
                        ValetDaily = 42,
                        ValetMonthly = 165,
                        SelfDaily = 58,
                        SelfMonthly = 205,
                        ValetComps = 8,
                        SelfComps = 12,
                        DriveInRatio = 78,
                        CaptureRatio = 87
                    }
                }
            };

            var expectedModel = new bs_SiteStatistic
            {
                bs_CustomerSiteFK = new EntityReference("bs_customersite", customerSiteId) { Name = "0111" },
                bs_BillingPeriod = "2025-07",
                bs_Name = "Site Statistic",
                bs_SiteStatistic_CustomerSite = new bs_CustomerSite
                {
                    bs_TotalRoomsAvailable = "200"
                },
                bs_SiteStatistic_SiteStatisticDetail = new List<bs_SiteStatisticDetail>
                {
                    new bs_SiteStatisticDetail
                    {
                        bs_Type = bs_sitestatisticdetailchoice.Forecast,
                        bs_Date = new DateTime(2025, 7, 1),
                        bs_ValetRateDaily = 35.00m,
                        bs_ValetRateMonthly = 350.00m,
                        bs_SelfRateDaily = 25.00m,
                        bs_SelfRateMonthly = 250.00m,
                        bs_BaseRevenue = 6750.00m,
                        bs_OccupiedRooms = 142,
                        bs_Occupancy = 0.91m,
                        bs_SelfOvernight = 47,
                        bs_ValetOvernight = 32,
                        bs_ValetDaily = 42,
                        bs_ValetMonthly = 165,
                        bs_SelfDaily = 58,
                        bs_SelfMonthly = 205,
                        bs_ValetComps = 8,
                        bs_SelfComps = 12,
                        bs_DriveInRatio = 78,
                        bs_CaptureRatio = 87
                    },
                    new bs_SiteStatisticDetail
                    {
                        bs_Type = bs_sitestatisticdetailchoice.Budget,
                        bs_Date = new DateTime(2025, 7, 1),
                        bs_ValetRateDaily = 35.00m,
                        bs_ValetRateMonthly = 350.00m,
                        bs_SelfRateDaily = 25.00m,
                        bs_SelfRateMonthly = 250.00m,
                        bs_BaseRevenue = 6750.00m,
                        bs_OccupiedRooms = 142,
                        bs_Occupancy = 0.91m,
                        bs_SelfOvernight = 47,
                        bs_ValetOvernight = 32,
                        bs_ValetDaily = 42,
                        bs_ValetMonthly = 165,
                        bs_SelfDaily = 58,
                        bs_SelfMonthly = 205,
                        bs_ValetComps = 8,
                        bs_SelfComps = 12,
                        bs_DriveInRatio = 78,
                        bs_CaptureRatio = 87
                    }
                }
            };

            _siteStatisticService.SaveSiteStatistics(hotelParkingDataVo);
            _siteStatisticRepository.Received(1).CreateSiteStatistics(Arg.Any<bs_SiteStatistic>());
        }

        [Fact]
        public async Task GetSiteStatistics_ShouldReturnAdjustmentFields_WhenPresentInData()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var billingPeriod = "2025-07";
            var timeRange = "daily";
            var months = new List<string> { billingPeriod };
            
            _monthRangeGenerator.GenerateMonthRange(billingPeriod, 3).Returns(months);
            
            // Mock budget data with adjustment fields
            var budgetData = new List<SiteStatisticDetailVo>
            {
                new SiteStatisticDetailVo
                {
                    Date = new DateOnly(2025, 7, 1),
                    Type = SiteStatisticDetailType.Budget,
                    ExternalRevenue = 10000m,
                    AdjustmentPercentage = -3.42m,
                    AdjustmentValue = -342m,
                    ValetRateDaily = 35.00m,
                    SelfRateDaily = 25.00m,
                    OccupiedRooms = 142,
                    Occupancy = 0.91m
                }
            };
            _siteStatisticRepository.GetBudgetDataForRange(Arg.Any<string>(), months, Arg.Any<int>()).Returns(Task.FromResult(budgetData));
            
            // Mock empty actual data
            var actualData = new List<SiteStatisticDetailVo>();
            _siteStatisticRepository.GetActualDataForRange(Arg.Any<string>(), months).Returns(Task.FromResult(actualData));
            
            // Mock site statistics
            var siteStatisticModel = new bs_SiteStatistic
            {
                bs_SiteStatisticId = Guid.NewGuid(),
                bs_CustomerSiteFK = new EntityReference("bs_customersite", siteId) { Name = "0111" },
                bs_BillingPeriod = billingPeriod,
                bs_Name = "Site Statistic",
                bs_SiteStatistic_CustomerSite = new bs_CustomerSite
                {
                    bs_TotalRoomsAvailable = "200"
                }
            };
            
            _siteStatisticRepository.GetSiteStatistics(siteId, Arg.Any<string>())
                .Returns(siteStatisticModel);
            
            // Act
            var result = await _siteStatisticService.GetSiteStatistics(siteId, billingPeriod, timeRange);
            
            // Assert
            result.Should().NotBeNull();
            var resultList = result.ToList();
            resultList.Should().HaveCount(1);
            
            var firstResult = resultList[0];
            firstResult.BudgetData.Should().HaveCount(1);
            
            var budgetDetail = firstResult.BudgetData[0];
            budgetDetail.ExternalRevenue.Should().Be(10000m);
            budgetDetail.AdjustmentPercentage.Should().Be(-3.42m);
            budgetDetail.AdjustmentValue.Should().Be(-342m);
        }

        [Fact]
        public void SaveSiteStatistics_ShouldSaveAdjustmentFields_WhenProvided()
        {
            // Arrange
            var siteStatisticId = Guid.NewGuid();
            var customerSiteId = Guid.NewGuid();
            
            var siteStatisticVo = new SiteStatisticVo
            {
                Id = siteStatisticId,
                SiteNumber = "0111",
                CustomerSiteId = customerSiteId,
                Name = "Site Statistic",
                TotalRooms = 200,
                PeriodLabel = "2025-07",
                ForecastData = new List<SiteStatisticDetailVo>
                {
                    new SiteStatisticDetailVo
                    {
                        Date = new DateOnly(2025, 7, 1),
                        Type = SiteStatisticDetailType.Forecast,
                        PeriodLabel = new DateOnly(2025, 7, 1).ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture),
                        ExternalRevenue = 12000m,
                        AdjustmentPercentage = -4.68m,
                        AdjustmentValue = -561.6m,
                        ValetRateDaily = 35.00m,
                        SelfRateDaily = 25.00m,
                        OccupiedRooms = 150,
                        Occupancy = 0.75m
                    }
                }
            };
            
            // Act
            _siteStatisticService.SaveSiteStatistics(siteStatisticVo);
            
            // Assert
            _siteStatisticRepository.Received(1).SaveSiteStatistics(
                Arg.Is<bs_SiteStatistic>(model =>
                    model.bs_SiteStatistic_SiteStatisticDetail != null &&
                    model.bs_SiteStatistic_SiteStatisticDetail.Count() == 1 &&
                    model.bs_SiteStatistic_SiteStatisticDetail.First().bs_AdjustmentPercentage == -4.68m &&
                    model.bs_SiteStatistic_SiteStatisticDetail.First().bs_AdjustmentValue == -561.6m
                )
            );
        }

        [Fact]
        public void SaveSiteStatistics_ShouldHandleNullAdjustmentFields()
        {
            // Arrange
            var customerSiteId = Guid.NewGuid();
            
            var siteStatisticVo = new SiteStatisticVo
            {
                SiteNumber = "0111",
                CustomerSiteId = customerSiteId,
                Name = "Site Statistic",
                TotalRooms = 200,
                PeriodLabel = "2025-07",
                ForecastData = new List<SiteStatisticDetailVo>
                {
                    new SiteStatisticDetailVo
                    {
                        Date = new DateOnly(2025, 7, 1),
                        Type = SiteStatisticDetailType.Budget,
                        PeriodLabel = new DateOnly(2025, 7, 1).ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture),
                        ExternalRevenue = 8000m,
                        AdjustmentPercentage = null,
                        AdjustmentValue = null,
                        ValetRateDaily = 30.00m,
                        SelfRateDaily = 20.00m,
                        OccupiedRooms = 140,
                        Occupancy = 0.70m
                    }
                }
            };
            
            // Act
            _siteStatisticService.SaveSiteStatistics(siteStatisticVo);
            
            // Assert
            _siteStatisticRepository.Received(1).CreateSiteStatistics(
                Arg.Is<bs_SiteStatistic>(model =>
                    model.bs_SiteStatistic_SiteStatisticDetail != null &&
                    model.bs_SiteStatistic_SiteStatisticDetail.Count() == 1 &&
                    model.bs_SiteStatistic_SiteStatisticDetail.First().bs_AdjustmentPercentage == null &&
                    model.bs_SiteStatistic_SiteStatisticDetail.First().bs_AdjustmentValue == null
                )
            );
        }
    }
}
