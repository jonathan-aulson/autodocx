using System.Net;
using System.Text;
using api.Adapters;
using api.Functions;
using api.Models.Dto;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using NSubstitute;
using Xunit;

namespace BackendTests.Functions
{
    public class OtherExpensesTest
    {
        private readonly OtherExpenses _otherExpenses;
        private readonly IOtherExpenseServiceAdapter _otherExpensesServiceAdapter;

        public OtherExpensesTest()
        {
            _otherExpensesServiceAdapter = Substitute.For<IOtherExpenseServiceAdapter>();
            _otherExpenses = new OtherExpenses(_otherExpensesServiceAdapter);
        }

        [Fact]
        public void SaveOtherExpense_ShouldSaveAndReturnOk()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var dto = new OtherExpenseDto
            {
                Id = Guid.NewGuid(),
                CustomerSiteId = siteId,
                BillingPeriod = "2025-07",
                SiteNumber = "0111",
                BudgetData = new List<OtherExpenseDetailDto>(),
                ActualData = new List<OtherExpenseDetailDto>(),
                ForecastData = new List<OtherExpenseDetailDto>
                {
                    new OtherExpenseDetailDto
                    {
                        Id = Guid.NewGuid(),
                        MonthYear = "2025-07",
                        EmployeeRelations = 123.45m,
                        FuelVehicles = 234.56m,
                        LossAndDamageClaims = 12.34m,
                        OfficeSupplies = 56.78m,
                        OutsideServices = 90.12m,
                        RentsParking = 345.67m,
                        RepairsAndMaintenance = 78.90m,
                        RepairsAndMaintenanceVehicle = 23.45m,
                        Signage = 67.89m,
                        SuppliesAndEquipment = 45.67m,
                        TicketsAndPrintedMaterial = 89.01m,
                        Uniforms = 34.56m
                    }
                }
            };

            var json = JsonConvert.SerializeObject(dto);
            var body = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var context = Substitute.For<FunctionContext>();
            var requestData = new FakeHttpRequestData(
                context,
                new Uri("http://localhost:7275/api/otherExpense"),
                body);

            // Act
            var result = _otherExpenses.SaveOtherExpense(requestData);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.OK);
            _otherExpensesServiceAdapter.Received(1).SaveOtherExpenseData(Arg.Is<OtherExpenseDto>(x =>
                x.Id == dto.Id &&
                x.CustomerSiteId == dto.CustomerSiteId &&
                x.BillingPeriod == dto.BillingPeriod &&
                x.SiteNumber == dto.SiteNumber
            ));
        }

        [Fact]
        public void GetOtherExpense_ShouldReturnExpectedDto()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var billingPeriod = "2025-07";
            var expectedDto = new OtherExpenseDto
            {
                Id = Guid.NewGuid(),
                CustomerSiteId = siteId,
                BillingPeriod = billingPeriod,
                SiteNumber = "0111",
                BudgetData = new List<OtherExpenseDetailDto>(),
                ActualData = new List<OtherExpenseDetailDto>(),
                ForecastData = new List<OtherExpenseDetailDto>
        {
            new OtherExpenseDetailDto
            {
                Id = Guid.NewGuid(),
                MonthYear = billingPeriod,
                EmployeeRelations = 123.45m,
                FuelVehicles = 234.56m,
                LossAndDamageClaims = 12.34m,
                OfficeSupplies = 56.78m,
                OutsideServices = 90.12m,
                RentsParking = 345.67m,
                RepairsAndMaintenance = 78.90m,
                RepairsAndMaintenanceVehicle = 23.45m,
                Signage = 67.89m,
                SuppliesAndEquipment = 45.67m,
                TicketsAndPrintedMaterial = 89.01m,
                Uniforms = 34.56m
            }
        }
            };

            _otherExpensesServiceAdapter.GetOtherExpenseData(siteId, billingPeriod).Returns(expectedDto);

            var context = Substitute.For<FunctionContext>();
            var requestData = new FakeHttpRequestData(
                context,
                new Uri($"http://localhost:7275/api/otherExpense/{siteId}/{billingPeriod}"),
                null);

            // Act
            var response = _otherExpenses.GetOtherExpense(requestData, siteId, billingPeriod);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = new StreamReader(response.Body).ReadToEnd();
            var actualDto = JsonConvert.DeserializeObject<OtherExpenseDto>(responseBody);
            actualDto.Should().BeEquivalentTo(expectedDto);
            _otherExpensesServiceAdapter.Received(1).GetOtherExpenseData(siteId, billingPeriod);
        }

    }
}