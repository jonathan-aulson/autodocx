using System.Net;
using api.Adapters;
using api.Functions;
using api.Models.Dto;
using api.Models.Vo;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using NSubstitute;
using Xunit;

namespace BackendTests.Functions
{
    public class ContractsTest
    {
        private readonly Contracts _contracts;
        private readonly IContractServiceAdapter _contractServiceAdapterMock;

        public ContractsTest()
        {
            var loggerFactoryMock = Substitute.For<ILoggerFactory>();
            _contractServiceAdapterMock = Substitute.For<IContractServiceAdapter>();
            _contracts = new Contracts(loggerFactoryMock, _contractServiceAdapterMock);

            loggerFactoryMock.CreateLogger<Contracts>().Returns(NullLogger<Contracts>.Instance);
        }

        [Fact]
        public void Run_ShouldReturnContractDetail_WhenContractExists()
        {
            var customerSiteId = Guid.NewGuid();

            var expectedContract = new ContractDetailDto
            {
                Id = Guid.NewGuid(),
                PurchaseOrder = "PO-123",
                PaymentTerms = "Due in 30 days",
                BillingType = "Arrears",
                IncrementMonth = "April",
                IncrementAmount = 1.15m,
                ConsumerPriceIndex = true,
                Notes = "Some notes",
                PerOccupiedRoom = new ContractDetailDto.PerOccupiedRoomDto
                {
                    Enabled = true,
                    RoomRate = 10.00m
                },
                FixedFee = new ContractDetailDto.FixedFeeDto
                {
                    Enabled = true,
                    ServiceRates = new List<ContractDetailDto.ServiceRateDto>
                    {
                        new ContractDetailDto.ServiceRateDto
                        {
                            Id = Guid.NewGuid(),
                            Name = "Valet",
                            DisplayName = "Valet Service",
                            Fee = 1000
                        },
                        new ContractDetailDto.ServiceRateDto
                        {
                            Id = Guid.NewGuid(),
                            Name = "Shuttle",
                            DisplayName = "Shuttle Service",
                            Fee = 3000
                        },
                        new ContractDetailDto.ServiceRateDto
                        {
                            Id = Guid.NewGuid(),
                            Name = "Cashier",
                            DisplayName = "Cashier Service",
                            Fee = 1500
                        }
                    }
                },
                PerLaborHour = new ContractDetailDto.PerLaborHourDto
                {
                    Enabled = true,
                    JobRates = new List<ContractDetailDto.JobRateDto>
                    {
                        new ContractDetailDto.JobRateDto
                        {
                            Id = Guid.NewGuid(),
                            Name = "Valet",
                            DisplayName = "Valet",
                            Rate = 50.00m,
                            OvertimeRate = 75.00m,
                            JobCode = "111",
                            InvoiceGroup = 1
                        },
                        new ContractDetailDto.JobRateDto
                        {
                            Id = Guid.NewGuid(),
                            Name = "Bell",
                            DisplayName = "Bell",
                            Rate = 100.00m,
                            OvertimeRate = 125.00m,
                            JobCode = "222",
                            InvoiceGroup = 1
                        },
                        new ContractDetailDto.JobRateDto
                        {
                            Id = Guid.NewGuid(),
                            Name = "Greeter",
                            DisplayName = "Greeter",
                            Rate = 25.00m,
                            OvertimeRate = 37.50m,
                            JobCode = "333",
                            InvoiceGroup = 2
                        }
                    }
                },
                RevenueShare = new ContractDetailDto.RevenueShareDto
                {
                    Enabled = true,
                    ThresholdStructures = new List<ContractDetailDto.ThresholdStructureDto>
                    {
                        new ContractDetailDto.ThresholdStructureDto
                        {
                            Id = Guid.NewGuid(),
                            AccumulationType = "Monthly",
                            RevenueCodes = new List<string> { "123", "456" },
                            Tiers = new List<ContractDetailDto.TierDto>
                            {
                                new ContractDetailDto.TierDto
                                {
                                    SharePercentage = 0.10m,
                                    Amount = 1000.00m
                                },
                                new ContractDetailDto.TierDto
                                {
                                    SharePercentage = 2.00m,
                                    Amount = 2000
                                },
                                new ContractDetailDto.TierDto
                                {
                                    SharePercentage = 5,
                                    Amount = null
                                }
                            }
                        }
                    }
                },
            };
            _contractServiceAdapterMock.GetContractDetail(customerSiteId).Returns(expectedContract);

            var context = Substitute.For<FunctionContext>();
            var body = new MemoryStream();
            var requestData = new FakeHttpRequestData(
                context,
                new Uri($"http://localhost:7275/api/contracts/{customerSiteId}/detail"),
                body);

            var result = _contracts.GetContractDetail(requestData, customerSiteId);
            result.StatusCode.Should().Be(HttpStatusCode.OK);

            result.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = new StreamReader(result.Body).ReadToEnd();
            var contract = JsonConvert.DeserializeObject<ContractDetailDto>(responseBody);

            contract.Should()
                .NotBeNull()
                .And.BeEquivalentTo(expectedContract);
        }

        [Fact]
        public void UpdateContract_ShouldReturnOk_WhenUpdateIsSuccessful()
        {
            var contractId = Guid.NewGuid();
            var updateContractDto = new ContractDetailDto()
            {
                IncrementAmount = 1.15m,
                IncrementMonth = "April"
            };

            _contractServiceAdapterMock.UpdateContract(contractId, updateContractDto);

            var context = Substitute.For<FunctionContext>();
            var requestData = new FakeHttpRequestData(
                context,
                new Uri($"http://localhost:7275/api/contracts/{contractId}"),
                new MemoryStream(System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(updateContractDto)))
            );

            var result = _contracts.UpdateContract(requestData, contractId);

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = new StreamReader(result.Body).ReadToEnd();
            responseBody.Should().BeNullOrEmpty();

            _contractServiceAdapterMock.Received().UpdateContract(contractId, Arg.Is<ContractDetailDto>(
                c => updateContractDto.IncrementAmount.Equals(c.IncrementAmount) &&
                     c.IncrementMonth == updateContractDto.IncrementMonth
            ));
        }

        [Fact]
        public void UpdateContract_ShouldReturnBadRequest_WhenRequestBodyIsEmpty()
        {
            var context = Substitute.For<FunctionContext>();
            var contractId = Guid.NewGuid();
            var requestData = new FakeHttpRequestData(
                context,
                new Uri($"http://localhost:7275/api/contracts/{contractId}"),
                new MemoryStream()
            );

            var result = _contracts.UpdateContract(requestData, contractId);

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = new StreamReader(result.Body).ReadToEnd();
            responseBody.Should().BeNullOrEmpty();
        }

        [Fact]
        public void GetDeviationData_ReturnsOkResponseWithData()
        {
            // Arrange
            var context = Substitute.For<FunctionContext>();
            var contractId = Guid.NewGuid();
            var requestData = new FakeHttpRequestData(
                context,
                new Uri($"http://localhost:7275/api/contracts/deviations"),
                new MemoryStream()
            );
            var deviations = new List<DeviationDto>
                {
                    new DeviationDto
                    {
                        ContractId = Guid.NewGuid(),
                        DeviationAmount = 1000.00m,
                        DeviationPercentage = 10.00m,
                        CustomerSiteId = Guid.NewGuid(),
                        SiteName = "Site 1",
                        SiteNumber = "1"
                    },
                    new DeviationDto
                    {
                        ContractId = Guid.NewGuid(),
                        DeviationAmount = 2000.00m,
                        DeviationPercentage = 20.00m,
                        CustomerSiteId = Guid.NewGuid(),
                        SiteName = "Site 2",
                        SiteNumber = "2"
                    }
                };
            _contractServiceAdapterMock.GetDeviations().Returns(deviations);

            // Act
            var result = _contracts.GetDeviationData(requestData);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = new StreamReader(result.Body).ReadToEnd();
            responseBody.Should().Be(JsonConvert.SerializeObject(deviations));
            responseBody.Should().NotBeNullOrEmpty().And.BeEquivalentTo(JsonConvert.SerializeObject(deviations));
        }

        [Fact]
        public void UpdateDeviationThreshold_ShouldReturnOK_WhenSuccessful()
        {
            var contractId1 = Guid.NewGuid();
            var contractId2 = Guid.NewGuid();
            var contractId3 = Guid.NewGuid();

            var deviationUpdates = new List<DeviationDto>
            {
                new DeviationDto
                {
                    ContractId = contractId1,
                    DeviationAmount = 1000.00m,
                    DeviationPercentage = 10.00m
                },
                new DeviationDto
                {
                    ContractId = contractId2,
                    DeviationAmount = 2000.00m,
                    DeviationPercentage = 20.00m
                },
                new DeviationDto
                {
                    ContractId = contractId3,
                    DeviationAmount = 3000.00m,
                    DeviationPercentage = 30.00m
                }
            };

            var context = Substitute.For<FunctionContext>();
            var requestData = new FakeHttpRequestData(
                context,
                new Uri($"http://localhost:7275/api/contracts/deviations"),
                new MemoryStream(System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(deviationUpdates)))
            );

            _contractServiceAdapterMock.UpdateDeviationThreshold(deviationUpdates);

            var result = _contracts.UpdateDeviationThreshold(requestData);

            result.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public void UpdateDeviationThreshold_ShouldReturnBadRequest_WhenRequestBodyIsEmpty()
        {
            var context = Substitute.For<FunctionContext>();
            var requestData = new FakeHttpRequestData(
                context,
                new Uri($"http://localhost:7275/api/contracts/deviations"),
                new MemoryStream()
            );

            var result = _contracts.UpdateDeviationThreshold(requestData);

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = new StreamReader(result.Body).ReadToEnd();
            responseBody.Should().BeNullOrEmpty();
        }
    }
}
