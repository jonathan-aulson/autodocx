using api.Models.Vo;
using api.Data;
using api.Services.Impl;
using FluentAssertions;
using NSubstitute;
using TownePark;
using Xunit;
using System;
using System.Collections.Generic;

namespace BackendTests.Services
{
    public class OtherRevenueServiceTest
    {
        private readonly IOtherRevenueRepository _otherRevenueRepository;
        private readonly OtherRevenueService _otherRevenueService;

        public OtherRevenueServiceTest()
        {
            _otherRevenueRepository = Substitute.For<IOtherRevenueRepository>();
            _otherRevenueService = new OtherRevenueService(_otherRevenueRepository);
        }

        [Fact]
        public void GetOtherRevenueData_ShouldReturnOtherRevenueVo()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var period = "2025-07";
            var details = new List<bs_OtherRevenueDetail>
            {
                new bs_OtherRevenueDetail
                {
                    Id = Guid.NewGuid(),
                    bs_BillableExpense = 100.00m,
                    bs_Credits = 200.00m,
                    bs_GPOFees = 50.00m,
                    bs_RevenueValidation = 75.00m,
                    bs_SigningBonus = 80.00m,
                    bs_ClientPaidExpense = 120.00m,
                    bs_MonthYear = "2025-07",
                    bs_CustomerSiteFK = new Microsoft.Xrm.Sdk.EntityReference("bs_customersite", siteId)
                }
            };

            _otherRevenueRepository.GetOtherRevenueDetail(siteId, period).Returns(details);

            // Act
            var result = _otherRevenueService.GetOtherRevenueData(siteId, period);

            // Assert
            result.Should().NotBeNull();
            result.CustomerSiteId.Should().Be(siteId);
            result.BillingPeriod.Should().Be(period);
            result.ForecastData.Should().NotBeNull();
            result.ForecastData.Should().HaveCount(1);
        }

        [Fact]
        public void SaveOtherRevenueData_SetsTypeToForecast()
        {
            // Arrange
            var repoSub = NSubstitute.Substitute.For<IOtherRevenueRepository>();
            var service = new OtherRevenueService(repoSub);

            var forecastDetails = new List<OtherRevenueDetailVo>
            {
                new OtherRevenueDetailVo { Id = Guid.NewGuid(), MonthYear = "2025-07", BillableExpense = 100 },
                new OtherRevenueDetailVo { Id = Guid.NewGuid(), MonthYear = "2025-07", BillableExpense = 200 }
            };

            var otherRevenue = new OtherRevenueVo
            {
                CustomerSiteId = Guid.NewGuid(),
                BillingPeriod = "2025-07",
                ForecastData = forecastDetails
            };

            // Act
            service.SaveOtherRevenueData(otherRevenue);

            // Assert
            foreach (var detail in otherRevenue.ForecastData)
            {
                Assert.Equal(OtherRevenueType.Forecast, detail.Type);
            }
        }

        [Fact]
        public void SaveOtherRevenueData_ShouldCallRepositoryWithMappedModel()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var period = "2025-07";
            var vo = new OtherRevenueVo
            {
                CustomerSiteId = siteId,
                BillingPeriod = period,
                ForecastData = new List<OtherRevenueDetailVo>
                {
                    new OtherRevenueDetailVo
                    {
                        BillableExpense = 100.00m,
                        Credits = 200.00m,
                        GPOFees = 50.00m,
                        RevenueValidation = 75.00m,
                        SigningBonus = 80.00m,
                        ClientPaidExpense = 120.00m,
                        MonthYear = "2025-07"
                    }
                }
            };

            // Act
            _otherRevenueService.SaveOtherRevenueData(vo);

            // Assert
            _otherRevenueRepository.Received(1).UpdateOtherRevenueDetails(Arg.Any<List<bs_OtherRevenueDetail>>());
        }
    }
}
