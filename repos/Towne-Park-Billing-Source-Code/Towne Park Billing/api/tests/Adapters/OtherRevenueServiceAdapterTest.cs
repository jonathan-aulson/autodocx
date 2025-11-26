using api.Adapters.Impl;
using api.Models.Dto;
using api.Models.Vo;
using api.Services;
using FluentAssertions;
using NSubstitute;
using Xunit;
using System;

namespace BackendTests.Adapters
{
    public class OtherRevenueServiceAdapterTest
    {
        private readonly IOtherRevenueService _otherRevenueService;
        private readonly OtherRevenueServiceAdapter _adapter;

        public OtherRevenueServiceAdapterTest()
        {
            _otherRevenueService = Substitute.For<IOtherRevenueService>();
            _adapter = new OtherRevenueServiceAdapter(_otherRevenueService);
        }

        [Fact]
        public void GetOtherRevenue_ShouldReturnDto()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var period = "2025-07";
            var vo = new OtherRevenueVo
            {
                CustomerSiteId = siteId,
                BillingPeriod = period,
                ForecastData = new System.Collections.Generic.List<OtherRevenueDetailVo>
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

            _otherRevenueService.GetOtherRevenueData(siteId, period).Returns(vo);

            // Act
            var result = _adapter.GetOtherRevenue(siteId, period);

            // Assert
            result.Should().NotBeNull();
            result.CustomerSiteId.Should().Be(siteId);
            result.BillingPeriod.Should().Be(period);
            result.ForecastData.Should().NotBeNull();
            result.ForecastData.Should().HaveCount(1);
        }

        [Fact]
        public void SaveOtherRevenueData_ShouldCallServiceWithMappedVo()
        {
            // Arrange
            var dto = new OtherRevenueDto
            {
                CustomerSiteId = Guid.NewGuid(),
                BillingPeriod = "2025-07",
                ForecastData = new System.Collections.Generic.List<OtherRevenueDetailDto>
                {
                    new OtherRevenueDetailDto
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
            _adapter.SaveOtherRevenueData(dto);

            // Assert
            _otherRevenueService.Received(1).SaveOtherRevenueData(Arg.Any<OtherRevenueVo>());
        }
    }
}
