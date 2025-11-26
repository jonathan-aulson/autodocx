using api.Adapters.Impl;
using api.Models.Vo.Enum;
using api.Models.Dto;
using api.Models.Vo;
using api.Services;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace BackendTests.Adapters
{
    public class BillingStatementServiceAdapterTest
    {
        private readonly IBillingStatementService _billingStatementService;
        private readonly BillingStatementServiceAdapter _billingStatementServiceAdapter;

        public BillingStatementServiceAdapterTest()
        {
            _billingStatementService = Substitute.For<IBillingStatementService>();
            _billingStatementServiceAdapter = new BillingStatementServiceAdapter(_billingStatementService);
        }

        [Fact]
        public void GetBillingStatements_ShouldCallBillingStatementServiceAndReturnAdaptedResponse()
        {
            var customerSiteId = Guid.NewGuid();
            var billingStatementsVo = new List<BillingStatementVo>
            {
                new BillingStatementVo
                {
                    CreatedMonth = "2024-07",
                    ServicePeriodStart = new DateOnly(2024, 7, 1),
                    ServicePeriodEnd = new DateOnly(2024, 7, 31),
                    TotalAmount = 1000.00m,
                    Status = StatementStatus.Sent,
                    Invoices = new List<InvoiceSummaryVo>
                    {
                        new InvoiceSummaryVo
                        {
                            InvoiceNumber = "INV-2021-01-01",
                            Amount = 500.00m
                        },
                        new InvoiceSummaryVo
                        {
                            InvoiceNumber = "INV-2021-01-02",
                            Amount = 500.00m
                        }
                    }
                },
                new BillingStatementVo
                {
                    CreatedMonth = "2024-08",
                    ServicePeriodStart = new DateOnly(2024, 8, 1),
                    ServicePeriodEnd = new DateOnly(2024, 8, 31),
                    TotalAmount = 1500.00m,
                    Status = StatementStatus.Approved,
                    Invoices = new List<InvoiceSummaryVo>
                    {
                        new InvoiceSummaryVo
                        {
                            InvoiceNumber = "INV-2021-02-01",
                            Amount = 750.00m
                        },
                        new InvoiceSummaryVo
                        {
                            InvoiceNumber = "INV-2021-02-02",
                            Amount = 750.00m
                        }
                    }
                }
            };
            _billingStatementService.GetBillingStatements(customerSiteId).Returns(billingStatementsVo);

            var billingStatementsDto = new List<BillingStatementDto>
            {
                new BillingStatementDto
                {
                    CreatedMonth = "2024-07",
                    ServicePeriod = "July 1 - July 31, 2024",
                    ServicePeriodStart = new DateOnly(2024, 7, 1),
                    TotalAmount = 1000.00m,
                    Status = "Sent",
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
                    CreatedMonth = "2024-08",
                    ServicePeriod = "August 1 - August 31, 2024",
                    ServicePeriodStart = new DateOnly(2024, 8, 1),
                    TotalAmount = 1500.00m,
                    Status = "Approved",
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

            var result = _billingStatementServiceAdapter.GetBillingStatements(customerSiteId);
            result.Should().BeEquivalentTo(billingStatementsDto);
        }

        [Fact]
        public void CurrentBillingStatements_ShouldReturnListOfCurrentStatementDtos()
        {
            UserDto userAuth = new UserDto();
            var billingStatementsVo = new List<BillingStatementVo>
            {
                new BillingStatementVo
                {
                    CreatedMonth = "2024-07",
                    ServicePeriodStart = new DateOnly(2024, 7, 1),
                    ServicePeriodEnd = new DateOnly(2024, 7, 31),
                    TotalAmount = 1000.00m,
                    Status = StatementStatus.Sent,
                    Invoices = new List<InvoiceSummaryVo>
                    {
                        new InvoiceSummaryVo
                        {
                            InvoiceNumber = "INV-2021-01-01",
                            Amount = 500.00m
                        },
                        new InvoiceSummaryVo
                        {
                            InvoiceNumber = "INV-2021-01-02",
                            Amount = 500.00m
                        }
                    }
                },
                new BillingStatementVo
                {
                    CreatedMonth = "2024-08",
                    ServicePeriodStart = new DateOnly(2024, 8, 1),
                    ServicePeriodEnd = new DateOnly(2024, 8, 31),
                    TotalAmount = 1500.00m,
                    Status = StatementStatus.Approved,
                    Invoices = new List<InvoiceSummaryVo>
                    {
                        new InvoiceSummaryVo
                        {
                            InvoiceNumber = "INV-2021-02-01",
                            Amount = 750.00m
                        },
                        new InvoiceSummaryVo
                        {
                            InvoiceNumber = "INV-2021-02-02",
                            Amount = 750.00m
                        }
                    }
                }
            };

            _billingStatementService.GetCurrentBillingStatements(userAuth).Returns(billingStatementsVo);

            var expectedDtos = new List<BillingStatementDto>
            {
                new BillingStatementDto
                {
                    CreatedMonth = "2024-07",
                    ServicePeriod = "July 1 - July 31, 2024",
                    ServicePeriodStart = new DateOnly(2024, 7, 1),
                    TotalAmount = 1000.00m,
                    Status = "Sent",
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
                    CreatedMonth = "2024-08",
                    ServicePeriod = "August 1 - August 31, 2024",
                    ServicePeriodStart = new DateOnly(2024, 8, 1),
                    TotalAmount = 1500.00m,
                    Status = "Approved",
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

            var result = _billingStatementServiceAdapter.GetCurrentBillingStatements(userAuth);
            result.Should().BeEquivalentTo(expectedDtos);
        }
    }
}

