using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Claims;
using api.Adapters;
using api.Functions;
using api.Middleware;
using api.Models.Dto;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using NSubstitute;
using Xunit;

namespace BackendTests.Functions
{
    public class BillingStatementsTest
    {
        private readonly BillingStatements _billingStatements;
        private readonly IBillingStatementServiceAdapter _billingStatementServiceAdapterMock;

        public BillingStatementsTest()
        {
            var loggerFactoryMock = Substitute.For<ILoggerFactory>();
            _billingStatementServiceAdapterMock = Substitute.For<IBillingStatementServiceAdapter>();
            _billingStatements = new BillingStatements(loggerFactoryMock.CreateLogger<BillingStatements>(), _billingStatementServiceAdapterMock);

            loggerFactoryMock.CreateLogger<BillingStatements>().Returns(NullLogger<BillingStatements>.Instance);
        }

        [Fact]
        public void GetBillingStatements_ShouldReturnBillingStatementsList()
        {
            var customerSiteId = Guid.NewGuid();
            var expectedBillingStatements = new List<BillingStatementDto>
            {
                new BillingStatementDto
                {
                    CreatedMonth = "July",
                    ServicePeriod = "July 1st - July 31st",
                    TotalAmount = 1000.00m,
                    Status = "Paid",
                    Invoices = new List<InvoiceSummaryDto>
                    {
                        new InvoiceSummaryDto
                        {
                            InvoiceNumber = "INV-2021-01-01",
                            Amount = 500.00m
                        },
                        new InvoiceSummaryDto
                        {
                            InvoiceNumber = "INV-2021-01-02",
                            Amount = 500.00m
                        }
                    }
                },
                new BillingStatementDto
                {
                    CreatedMonth = "August",
                    ServicePeriod = "August 1st - August 31st",
                    TotalAmount = 1500.00m,
                    Status = "Unpaid",
                    Invoices = new List<InvoiceSummaryDto>
                    {
                        new InvoiceSummaryDto
                        {
                            InvoiceNumber = "INV-2021-02-01",
                            Amount = 750.00m
                        },
                        new InvoiceSummaryDto
                        {
                            InvoiceNumber = "INV-2021-02-02",
                            Amount = 750.00m
                        }
                    }
                }
            };
            _billingStatementServiceAdapterMock.GetBillingStatements(customerSiteId).Returns(expectedBillingStatements);

            var body = new MemoryStream();
            var context = Substitute.For<FunctionContext>();
            var requestData = new FakeHttpRequestData(
                context,
                new Uri($"http://localhost:7275/api/customers/{customerSiteId}/statements"),
                body);

            var result = _billingStatements.GetBillingStatements(requestData, customerSiteId);
            result.StatusCode.Should().Be(HttpStatusCode.OK);

            result.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = new StreamReader(result.Body).ReadToEnd();
            var billingStatements = JsonConvert.DeserializeObject<List<BillingStatementDto>>(responseBody);

            billingStatements.Should()
                .NotBeEmpty()
                .And.HaveCount(2)
                .And.BeEquivalentTo(expectedBillingStatements);
        }

        [Fact]
        public void CurrentBillingStatements_ShouldReturnListOfCurrentStatements()
        {
            UserDto userAuth = new UserDto
            {
                Email = "test@example.com",
                Roles = new[]
                {
                    "billingAdmin"
                }
            };

            var expectedBillingStatements = new List<BillingStatementDto>
            {
                new BillingStatementDto
                {
                    CreatedMonth = "July",
                    ServicePeriod = "July 1st - July 31st",
                    TotalAmount = 1000.00m,
                    Status = "Paid",
                    Invoices = new List<InvoiceSummaryDto>
                    {
                        new InvoiceSummaryDto
                        {
                            InvoiceNumber = "INV-2021-01-01",
                            Amount = 500.00m
                        },
                        new InvoiceSummaryDto
                        {
                            InvoiceNumber = "INV-2021-01-02",
                            Amount = 500.00m
                        }
                    }
                },
                new BillingStatementDto
                {
                    CreatedMonth = "August",
                    ServicePeriod = "August 1st - August 31st",
                    TotalAmount = 1500.00m,
                    Status = "Unpaid",
                    Invoices = new List<InvoiceSummaryDto>
                    {
                        new InvoiceSummaryDto
                        {
                            InvoiceNumber = "INV-2021-02-01",
                            Amount = 750.00m
                        },
                        new InvoiceSummaryDto
                        {
                            InvoiceNumber = "INV-2021-02-02",
                            Amount = 750.00m
                        }
                    }
                }
            };
            _billingStatementServiceAdapterMock
                .GetCurrentBillingStatements(Arg.Any<UserDto>())
                .Returns(expectedBillingStatements);

            var body = new MemoryStream();
            var context = Substitute.For<FunctionContext>();
            var requestData = new FakeHttpRequestData(
                context,
                new Uri($"http://localhost:7275/api/customers/statements"),
                body);

            var items = new Dictionary<object, object>();
            items.Add(AuthenticationMiddleware.UserDtoKey, userAuth);

            context.Items.Returns(items);

            var result = _billingStatements.GetCurrentBillingStatements(requestData, context);
            result.StatusCode.Should().Be(HttpStatusCode.OK);

            result.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = new StreamReader(result.Body).ReadToEnd();
            var billingStatements = JsonConvert.DeserializeObject<List<BillingStatementDto>>(responseBody);

            billingStatements.Should()
                .NotBeEmpty()
                .And.HaveCount(2)
                .And.BeEquivalentTo(expectedBillingStatements);
        }
    }
}

