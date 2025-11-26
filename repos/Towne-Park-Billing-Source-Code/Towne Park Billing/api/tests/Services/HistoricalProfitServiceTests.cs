using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using NSubstitute;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using TownePark.Data;
using TownePark.Models.Vo;
using api.Services;
using api.Services.Impl;
using api.Services.Impl.Calculators;
using api.Adapters;
using api.Adapters.Mappers;
using api.Data;
using api.Models.Dto;
using api.Models.Vo;
using TownePark;

namespace BackendTests.Services
{
    public class HistoricalProfitServiceTests
    {
        private readonly IInternalRevenueRepository _internalRevenueRepository;
        private readonly IPnlServiceAdapter _pnlServiceAdapter;
        private readonly IParkingRateRepository _parkingRateRepository;
        private readonly IInternalRevenueMapper _internalRevenueMapper;
        private readonly ILogger<HistoricalProfitService> _logger;
        private readonly IExternalRevenueCalculator _externalRevenueCalculator;
        private readonly IInternalRevenueCalculator _internalRevenueCalculator;
        private readonly IManagementAgreementCalculator _managementAgreementCalculator;
        private readonly ISiteStatisticService _siteStatisticService;
        private readonly HistoricalProfitService _service;

        public HistoricalProfitServiceTests()
        {
            _internalRevenueRepository = Substitute.For<IInternalRevenueRepository>();
            _pnlServiceAdapter = Substitute.For<IPnlServiceAdapter>();
            _parkingRateRepository = Substitute.For<IParkingRateRepository>();
            _internalRevenueMapper = Substitute.For<IInternalRevenueMapper>();
            _logger = Substitute.For<ILogger<HistoricalProfitService>>();
            _externalRevenueCalculator = Substitute.For<IExternalRevenueCalculator>();
            _internalRevenueCalculator = Substitute.For<IInternalRevenueCalculator>();
            _managementAgreementCalculator = Substitute.For<IManagementAgreementCalculator>();
            _siteStatisticService = Substitute.For<ISiteStatisticService>();

            // Setup order for management agreement calculator (ProfitShareCalculator already has Order = 100)
            _managementAgreementCalculator.Order.Returns(1);

            var externalCalculators = new List<IExternalRevenueCalculator> { _externalRevenueCalculator };
            var internalCalculators = new List<IInternalRevenueCalculator> { _internalRevenueCalculator };
            var managementCalculators = new List<IManagementAgreementCalculator> 
            { 
                _managementAgreementCalculator
            };

            _service = new HistoricalProfitService(
                _internalRevenueRepository,
                _siteStatisticService,
                _logger,
                managementCalculators);
        }

        [Fact]
        public async Task GetHistoricalProfitsAsync_Should_Return_Profits_For_Requested_Months()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var siteIds = new List<Guid> { siteId };
            var year = 2024;
            var startMonth = 10;
            var endMonth = 12;

            var siteData = new InternalRevenueDataVo
            {
                SiteId = siteId,
                SiteNumber = "001",
                Contract = new ContractDataVo { ContractTypes = new[] { bs_contracttypechoices.ManagementAgreement } },
                ManagementAgreement = new ManagementAgreementVo()
            };

            _internalRevenueRepository.GetInternalRevenueDataAsync(Arg.Any<List<string>>(), year)
                .Returns(Task.FromResult(new List<InternalRevenueDataVo> { siteData }));

            // Setup EDW data for ISiteStatisticService
            var edwData = new PnlBySiteListVo
            {
                PnlBySite = new List<PnlBySiteVo>
                {
                    new PnlBySiteVo
                    {
                        SiteNumber = "001",
                        Pnl = new PnlVo
                        {
                            Actual = new List<PnlMonthDetailVo>
                            {
                                new PnlMonthDetailVo { MonthNum = 10, ExternalRevenue = 100000m, OtherExpense = 0m },
                                new PnlMonthDetailVo { MonthNum = 11, ExternalRevenue = 100000m, OtherExpense = 0m },
                                new PnlMonthDetailVo { MonthNum = 12, ExternalRevenue = 100000m, OtherExpense = 0m }
                            }
                        }
                    }
                }
            };
            
            _siteStatisticService.GetPNLData(Arg.Any<List<string>>(), year)
                .Returns(Task.FromResult(edwData));

            // Setup calculators to return specific values
            _externalRevenueCalculator.When(x => x.CalculateAndApply(
                Arg.Any<InternalRevenueDataVo>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<MonthValueDto>(),
                Arg.Any<SiteMonthlyRevenueDetailDto>()))
                .Do(callInfo =>
                {
                    var detail = callInfo.ArgAt<SiteMonthlyRevenueDetailDto>(4);
                    detail.ExternalRevenueBreakdown = new ExternalRevenueBreakdownDto
                    {
                        CalculatedTotalExternalRevenue = 100000m // $100k external revenue
                    };
                });

            _managementAgreementCalculator.CalculateAndApplyAsync(
                Arg.Any<InternalRevenueDataVo>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<MonthValueDto>(),
                Arg.Any<SiteMonthlyRevenueDetailDto>(),
                Arg.Any<decimal>(),
                Arg.Any<List<PnlRowDto>>())
                .Returns(callInfo =>
                {
                    var detail = callInfo.ArgAt<SiteMonthlyRevenueDetailDto>(5);
                    if (detail.InternalRevenueBreakdown == null)
                        detail.InternalRevenueBreakdown = new InternalRevenueBreakdownDto();
                    
                    detail.InternalRevenueBreakdown.ManagementAgreement = new ManagementAgreementInternalRevenueDto
                    {
                        Components = new List<ManagementAgreementComponentDto>
                        {
                            new ManagementAgreementComponentDto { Name = "Management Fee", Value = 20000m },
                            new ManagementAgreementComponentDto { Name = "Insurance", Value = 5000m }
                        },
                        Total = 25000m
                    };
                    return Task.CompletedTask;
                });

            // Act
            var dummySiteDataList = siteIds.Select(id => new InternalRevenueDataVo { SiteId = id, SiteNumber = "001" }).ToList();
            var result = await _service.GetHistoricalProfitsAsync(dummySiteDataList, year, startMonth, endMonth);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count); // Oct, Nov, Dec
            
            // Verify profit calculation: Revenue - Expenses = 100000 - 25000 = 75000
            Assert.Equal(75000m, result[(siteId, year, 10)]);
            Assert.Equal(75000m, result[(siteId, year, 11)]);
            Assert.Equal(75000m, result[(siteId, year, 12)]);

            // Verify management agreement calculator was called for each month
            await _managementAgreementCalculator.Received(3).CalculateAndApplyAsync(
                Arg.Any<InternalRevenueDataVo>(),
                year,
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<MonthValueDto>(),
                Arg.Any<SiteMonthlyRevenueDetailDto>(),
                Arg.Any<decimal>(),
                Arg.Any<List<PnlRowDto>>());
        }

        [Fact]
        public async Task GetHistoricalProfitsAsync_Should_Return_Zero_When_No_Data_Found()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var siteIds = new List<Guid> { siteId };
            var year = 2024;
            var startMonth = 10;
            var endMonth = 12;

            _internalRevenueRepository.GetInternalRevenueDataAsync(Arg.Any<List<string>>(), year)
                .Returns(Task.FromResult(new List<InternalRevenueDataVo>()));

            // Act
            var dummySiteDataList = siteIds.Select(id => new InternalRevenueDataVo { SiteId = id }).ToList();
            var result = await _service.GetHistoricalProfitsAsync(dummySiteDataList, year, startMonth, endMonth);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Equal(0m, result[(siteId, year, 10)]);
            Assert.Equal(0m, result[(siteId, year, 11)]);
            Assert.Equal(0m, result[(siteId, year, 12)]);
        }

        [Fact]
        public async Task GetHistoricalProfitsAsync_Should_Handle_Multiple_Sites()
        {
            // Arrange
            var siteId1 = Guid.NewGuid();
            var siteId2 = Guid.NewGuid();
            var siteIds = new List<Guid> { siteId1, siteId2 };
            var year = 2024;
            var startMonth = 11;
            var endMonth = 12;

            var siteData1 = new InternalRevenueDataVo
            {
                SiteId = siteId1,
                SiteNumber = "001",
                Contract = new ContractDataVo { ContractTypes = new[] { bs_contracttypechoices.ManagementAgreement } },
                ManagementAgreement = new ManagementAgreementVo()
            };

            var siteData2 = new InternalRevenueDataVo
            {
                SiteId = siteId2,
                SiteNumber = "002",
                Contract = new ContractDataVo { ContractTypes = new[] { bs_contracttypechoices.ManagementAgreement } },
                ManagementAgreement = new ManagementAgreementVo()
            };

            _internalRevenueRepository.GetInternalRevenueDataAsync(Arg.Any<List<string>>(), year)
                .Returns(Task.FromResult(new List<InternalRevenueDataVo> { siteData1, siteData2 }));

            _parkingRateRepository.GetParkingRatesWithDetails(Arg.Any<List<Guid>>(), year)
                .Returns(new List<bs_ParkingRate>());

            _internalRevenueMapper.MapParkingRatesToVo(Arg.Any<List<bs_ParkingRate>>())
                .Returns(new List<ParkingRateVo>());

            _pnlServiceAdapter.GetPnlDataAsync(Arg.Any<List<string>>(), year)
                .Returns(Task.FromResult(new PnlResponseDto { ForecastRows = new List<PnlRowDto>(), BudgetRows = new List<PnlRowDto>() }));

            // Different revenues for each site
            _externalRevenueCalculator.When(x => x.CalculateAndApply(
                Arg.Is<InternalRevenueDataVo>(s => s.SiteId == siteId1),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<MonthValueDto>(),
                Arg.Any<SiteMonthlyRevenueDetailDto>()))
                .Do(callInfo =>
                {
                    var detail = callInfo.ArgAt<SiteMonthlyRevenueDetailDto>(4);
                    detail.ExternalRevenueBreakdown = new ExternalRevenueBreakdownDto
                    {
                        CalculatedTotalExternalRevenue = 100000m
                    };
                });

            _externalRevenueCalculator.When(x => x.CalculateAndApply(
                Arg.Is<InternalRevenueDataVo>(s => s.SiteId == siteId2),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<MonthValueDto>(),
                Arg.Any<SiteMonthlyRevenueDetailDto>()))
                .Do(callInfo =>
                {
                    var detail = callInfo.ArgAt<SiteMonthlyRevenueDetailDto>(4);
                    detail.ExternalRevenueBreakdown = new ExternalRevenueBreakdownDto
                    {
                        CalculatedTotalExternalRevenue = 150000m
                    };
                });

            // Act
            var dummySiteDataList = siteIds.Select(id => new InternalRevenueDataVo { SiteId = id }).ToList();
            var result = await _service.GetHistoricalProfitsAsync(dummySiteDataList, year, startMonth, endMonth);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.Count); // 2 sites x 2 months
            Assert.True(result.ContainsKey((siteId1, year, 11)));
            Assert.True(result.ContainsKey((siteId1, year, 12)));
            Assert.True(result.ContainsKey((siteId2, year, 11)));
            Assert.True(result.ContainsKey((siteId2, year, 12)));
        }
    }
}