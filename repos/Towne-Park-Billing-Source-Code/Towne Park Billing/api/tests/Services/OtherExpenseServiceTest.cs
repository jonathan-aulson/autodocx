using api.Models.Vo;
using api.Data;
using api.Services.Impl;
using FluentAssertions;
using NSubstitute;
using TownePark;
using Xunit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BackendTests.Services
{
    public class OtherExpenseServiceTest
    {
        private readonly IOtherExpenseRepository _otherExpenseRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly OtherExpenseService _otherExpenseService;

        public OtherExpenseServiceTest()
        {
            _otherExpenseRepository = Substitute.For<IOtherExpenseRepository>();
            _customerRepository = Substitute.For<ICustomerRepository>();
            _otherExpenseService = new OtherExpenseService(_otherExpenseRepository, _customerRepository);
        }

        [Fact]
        public void GetOtherExpenseData_WithForecastData_ShouldReturnOtherExpenseVo()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var period = "2025-07";
            var details = new List<bs_OtherExpenseDetail>
            {
                new bs_OtherExpenseDetail
                {
                    Id = Guid.NewGuid(),
                    bs_EmployeeRelations = 100.00m,
                    bs_FuelVehicles = 200.00m,
                    bs_LossAndDamageClaims = 50.00m,
                    bs_OfficeSupplies = 75.00m,
                    bs_OutsideServices = 80.00m,
                    bs_RentsParking = 120.00m,
                    bs_RepairsAndMaintenance = 90.00m,
                    bs_RepairsAndMaintenanceVehicle = 110.00m,
                    bs_Signage = 60.00m,
                    bs_SuppliesAndEquipment = 70.00m,
                    bs_TicketsAndPrintedMaterial = 40.00m,
                    bs_Uniforms = 85.00m,
                    bs_MonthYear = "2025-07",
                    bs_CustomerSiteFK = new Microsoft.Xrm.Sdk.EntityReference("bs_customersite", siteId)
                }
            };

            _otherExpenseRepository.GetOtherExpenseDetail(siteId, period).Returns(details);

            // Act
            var result = _otherExpenseService.GetOtherExpenseData(siteId, period);

            // Assert
            result.Should().NotBeNull();
            result.CustomerSiteId.Should().Be(siteId);
            result.BillingPeriod.Should().Be(period);
            result.ForecastData.Should().NotBeNull();
            result.ForecastData.Should().HaveCount(1);
        }

        [Fact]
        public void GetOtherExpenseData_WithoutForecastData_ShouldResolveSiteNumberFromCustomerRepository()
        {
            // Arrange - Bug 2811 scenario: no forecast data exists
            var siteId = Guid.NewGuid();
            var period = "2025-07";
            var siteNumber = "0349";

            // No forecast data exists
            _otherExpenseRepository.GetOtherExpenseDetail(siteId, period).Returns(new List<bs_OtherExpenseDetail>());

            // Customer site data for SiteNumber resolution
            var customerSite = new bs_CustomerSite
            {
                bs_CustomerSiteId = siteId,
                bs_SiteNumber = siteNumber,
                bs_SiteName = "Hilton Walt Disney World Resort"
            };
            _customerRepository.GetCustomerDetail(siteId).Returns(customerSite);

            // Mock budget and actual data responses
            var budgetData = new List<OtherExpenseDetailVo>
            {
                new OtherExpenseDetailVo
                {
                    MonthYear = "202507",
                    EmployeeRelations = 150.00m,
                    FuelVehicles = 250.00m,
                    LossAndDamageClaims = 75.00m
                }
            };
            var actualData = new List<OtherExpenseDetailVo>
            {
                new OtherExpenseDetailVo
                {
                    MonthYear = "202507",
                    EmployeeRelations = 140.00m,
                    FuelVehicles = 240.00m,
                    LossAndDamageClaims = 70.00m
                }
            };

            _otherExpenseRepository.GetBudgetData(siteNumber, period).Returns(Task.FromResult(budgetData));
            _otherExpenseRepository.GetActualData(siteNumber, period).Returns(Task.FromResult(actualData));

            // Act
            var result = _otherExpenseService.GetOtherExpenseData(siteId, period);

            // Assert
            result.Should().NotBeNull();
            result.CustomerSiteId.Should().Be(siteId);
            result.BillingPeriod.Should().Be(period);
            result.SiteNumber.Should().Be(siteNumber);
            result.BudgetData.Should().NotBeNull();
            result.BudgetData.Should().HaveCount(1);
            result.ActualData.Should().NotBeNull();
            result.ActualData.Should().HaveCount(1);

            // Verify that CustomerRepository was called to resolve SiteNumber
            _customerRepository.Received(1).GetCustomerDetail(siteId);
        }

        [Fact]
        public void GetOtherExpenseData_WithoutForecastData_CustomerRepositoryThrowsException_ShouldContinueWithEmptySiteNumber()
        {
            // Arrange - CustomerRepository throws exception
            var siteId = Guid.NewGuid();
            var period = "2025-07";

            // No forecast data exists
            _otherExpenseRepository.GetOtherExpenseDetail(siteId, period).Returns(new List<bs_OtherExpenseDetail>());

            // CustomerRepository throws exception - using a simpler approach
            _customerRepository.GetCustomerDetail(siteId).Returns(x => { throw new Exception("Customer site not found"); });

            // Act
            var result = _otherExpenseService.GetOtherExpenseData(siteId, period);

            // Assert
            result.Should().NotBeNull();
            result.CustomerSiteId.Should().Be(siteId);
            result.BillingPeriod.Should().Be(period);
            result.SiteNumber.Should().BeEmpty();
            result.BudgetData.Should().BeEmpty();
            result.ActualData.Should().BeEmpty();

            // Verify that CustomerRepository was called
            _customerRepository.Received(1).GetCustomerDetail(siteId);
        }

        [Fact]
        public void GetOtherExpenseData_WithoutForecastData_CustomerSiteIsNull_ShouldContinueWithEmptySiteNumber()
        {
            // Arrange - CustomerRepository returns null
            var siteId = Guid.NewGuid();
            var period = "2025-07";

            // No forecast data exists
            _otherExpenseRepository.GetOtherExpenseDetail(siteId, period).Returns(new List<bs_OtherExpenseDetail>());

            // CustomerRepository returns null
            _customerRepository.GetCustomerDetail(siteId).Returns((bs_CustomerSite)null);

            // Act
            var result = _otherExpenseService.GetOtherExpenseData(siteId, period);

            // Assert
            result.Should().NotBeNull();
            result.CustomerSiteId.Should().Be(siteId);
            result.BillingPeriod.Should().Be(period);
            result.SiteNumber.Should().BeEmpty();
            result.BudgetData.Should().BeEmpty();
            result.ActualData.Should().BeEmpty();

            // Verify that CustomerRepository was called
            _customerRepository.Received(1).GetCustomerDetail(siteId);
        }

        [Fact]
        public void GetOtherExpenseData_WithoutForecastData_CustomerSiteHasEmptySiteNumber_ShouldContinueWithEmptySiteNumber()
        {
            // Arrange - CustomerRepository returns customer site with empty SiteNumber
            var siteId = Guid.NewGuid();
            var period = "2025-07";

            // No forecast data exists
            _otherExpenseRepository.GetOtherExpenseDetail(siteId, period).Returns(new List<bs_OtherExpenseDetail>());

            // CustomerRepository returns customer site with empty SiteNumber
            var customerSite = new bs_CustomerSite
            {
                bs_CustomerSiteId = siteId,
                bs_SiteNumber = string.Empty,
                bs_SiteName = "Test Site"
            };
            _customerRepository.GetCustomerDetail(siteId).Returns(customerSite);

            // Act
            var result = _otherExpenseService.GetOtherExpenseData(siteId, period);

            // Assert
            result.Should().NotBeNull();
            result.CustomerSiteId.Should().Be(siteId);
            result.BillingPeriod.Should().Be(period);
            result.SiteNumber.Should().BeEmpty();
            result.BudgetData.Should().BeEmpty();
            result.ActualData.Should().BeEmpty();

            // Verify that CustomerRepository was called
            _customerRepository.Received(1).GetCustomerDetail(siteId);
        }

        [Fact]
        public void SaveOtherExpenseData_ShouldCallRepositoryWithMappedModel()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var period = "2025-07";
            var vo = new OtherExpenseVo
            {
                CustomerSiteId = siteId,
                BillingPeriod = period,
                ForecastData = new List<OtherExpenseDetailVo>
                {
                    new OtherExpenseDetailVo
                    {
                        EmployeeRelations = 100.00m,
                        FuelVehicles = 200.00m,
                        LossAndDamageClaims = 50.00m,
                        OfficeSupplies = 75.00m,
                        OutsideServices = 80.00m,
                        RentsParking = 120.00m,
                        RepairsAndMaintenance = 90.00m,
                        RepairsAndMaintenanceVehicle = 110.00m,
                        Signage = 60.00m,
                        SuppliesAndEquipment = 70.00m,
                        TicketsAndPrintedMaterial = 40.00m,
                        Uniforms = 85.00m,
                        MonthYear = "2025-07"
                    }
                }
            };

            // Act
            _otherExpenseService.SaveOtherExpenseData(vo);

            // Assert
            _otherExpenseRepository.Received(1).UpdateOtherRevenueDetails(Arg.Any<List<bs_OtherExpenseDetail>>());
        }

        [Fact]
        public void FormatMonthYear_ShouldFormatMonthYearCorrectly()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var period = "2025-07";
            var siteNumber = "0349";

            // No forecast data exists
            _otherExpenseRepository.GetOtherExpenseDetail(siteId, period).Returns(new List<bs_OtherExpenseDetail>());

            // Customer site data for SiteNumber resolution
            var customerSite = new bs_CustomerSite
            {
                bs_CustomerSiteId = siteId,
                bs_SiteNumber = siteNumber,
                bs_SiteName = "Test Site"
            };
            _customerRepository.GetCustomerDetail(siteId).Returns(customerSite);

            // Mock budget data with unformatted MonthYear
            var budgetData = new List<OtherExpenseDetailVo>
            {
                new OtherExpenseDetailVo
                {
                    MonthYear = "202507",
                    EmployeeRelations = 150.00m
                }
            };
            _otherExpenseRepository.GetBudgetData(siteNumber, period).Returns(Task.FromResult(budgetData));

            // Act
            var result = _otherExpenseService.GetOtherExpenseData(siteId, period);

            // Assert
            result.Should().NotBeNull();
            result.BudgetData.Should().HaveCount(1);
            result.BudgetData[0].MonthYear.Should().Be("2025-07");
        }
    }
}
