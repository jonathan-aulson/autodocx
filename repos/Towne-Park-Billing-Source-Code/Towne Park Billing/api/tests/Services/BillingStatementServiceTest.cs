using api.Data;
using api.Models.Dto;
using api.Models.Vo;
using api.Services;
using api.Services.Impl;
using FluentAssertions;
using Microsoft.Xrm.Sdk;
using NSubstitute;
using TownePark;
using Xunit;

namespace BackendTests.Services
{
    public class BillingStatementServiceTest
    {
        private readonly IBillingStatementRepository _billingStatementRepository;
        private readonly IConfigDataRepository _configRepository;
        private readonly IRoleService _roleService;
        private readonly BillingStatementService _billingStatementService;

        public BillingStatementServiceTest()
        {
            _billingStatementRepository = Substitute.For<IBillingStatementRepository>();
            _configRepository = Substitute.For<IConfigDataRepository>();
            _roleService = Substitute.For<IRoleService>();
            _billingStatementService = new BillingStatementService(_billingStatementRepository, _configRepository, _roleService);
        }

        [Fact]
        public void GetBillingStatements_ShouldCallBillingStatementRepositoryAndReturnAdaptedResponse()
        {
            var customerSiteId = Guid.NewGuid();
            var billingStatementsModel = new List<bs_BillingStatement>
            {
                new bs_BillingStatement
                {
                    bs_BillingStatementId = Guid.NewGuid(),
                    bs_ServicePeriodStart = new DateTime(2021, 7, 1),
                    bs_ServicePeriodEnd = new DateTime(2021, 7, 31),
                    bs_StatementStatus = bs_billingstatementstatuschoices.SENT,
                    bs_CustomerSiteFK = new EntityReference(bs_CustomerSite.EntityLogicalName, customerSiteId),
                    bs_Invoice_BillingStatement = new List<bs_Invoice>
                    {
                        new bs_Invoice
                        {
                            bs_InvoiceId = Guid.NewGuid(),
                            bs_InvoiceNumber = "INV-2021-01-01",
                            bs_Amount = 500.00m
                        }
                    },
                    bs_BillingStatement_cr9e8_readyforinvoice = new List<cr9e8_readyforinvoice>
                    {
                        new cr9e8_readyforinvoice
                        {
                            cr9e8_comments = "comment test 1"
                        }
                    }
                },
                new bs_BillingStatement
                {
                    bs_BillingStatementId = Guid.NewGuid(),
                    bs_ServicePeriodStart = new DateTime(2021, 8, 1),
                    bs_ServicePeriodEnd = new DateTime(2021, 8, 31),
                    bs_StatementStatus = bs_billingstatementstatuschoices.SENT,
                    bs_CustomerSiteFK = new EntityReference(bs_CustomerSite.EntityLogicalName, customerSiteId),
                    bs_Invoice_BillingStatement = new List<bs_Invoice>
                    {
                        new bs_Invoice
                        {
                            bs_InvoiceId = Guid.NewGuid(),
                            bs_InvoiceNumber = "INV-2021-02-01",
                            bs_Amount = 750.00m
                        }
                    },
                    bs_BillingStatement_cr9e8_readyforinvoice = new List<cr9e8_readyforinvoice>
                    {
                        new cr9e8_readyforinvoice
                        {
                            cr9e8_comments = "comment test 2"
                        }
                    }
                }
            };

            _billingStatementRepository.GetBillingStatementsByCustomerSite(customerSiteId).Returns(billingStatementsModel);

            var billingStatementsVo = new List<BillingStatementVo>
            {
                new BillingStatementVo
                {
                    Id = billingStatementsModel[0].bs_BillingStatementId,
                    ServicePeriodStart = new DateOnly(2021, 7, 1),
                    ServicePeriodEnd = new DateOnly(2021, 7, 31),
                    TotalAmount = 500.00m,
                    Status = StatementStatus.Sent,
                    AmNotes = "comment test 1",
                    Invoices = new List<InvoiceSummaryVo>
                    {
                        new InvoiceSummaryVo
                        {
                            Id = billingStatementsModel[0].bs_Invoice_BillingStatement.First().bs_InvoiceId,
                            InvoiceNumber = "INV-2021-01-01",
                            Amount = 500.00m
                        }
                    }
                },
                new BillingStatementVo
                {
                    Id = billingStatementsModel[1].bs_BillingStatementId,
                    ServicePeriodStart = new DateOnly(2021, 8, 1),
                    ServicePeriodEnd = new DateOnly(2021, 8, 31),
                    TotalAmount = 750.00m,
                    Status = StatementStatus.Sent,
                    AmNotes = "comment test 2",
                    Invoices = new List<InvoiceSummaryVo>
                    {
                        new InvoiceSummaryVo
                        {
                            Id = billingStatementsModel[1].bs_Invoice_BillingStatement.First().bs_InvoiceId,
                            InvoiceNumber = "INV-2021-02-01",
                            Amount = 750.00m
                        }
                    }
                }
            };

            var result = _billingStatementService.GetBillingStatements(customerSiteId);
            result.Should().BeEquivalentTo(billingStatementsVo);
        }

        [Fact]
        public void GetCurrentBillingStatements_ShouldCallBillingStatementRepositoryAndReturnAdaptedResponse()
        {
            UserDto userAuth = new UserDto();
            var billingStatementsModel = new List<bs_BillingStatement>
            {
                new bs_BillingStatement
                {
                    bs_BillingStatementId = Guid.NewGuid(),
                    bs_ServicePeriodStart = new DateTime(2021, 7, 1),
                    bs_ServicePeriodEnd = new DateTime(2021, 7, 31),
                    bs_StatementStatus = bs_billingstatementstatuschoices.SENT,
                    bs_Invoice_BillingStatement = new List<bs_Invoice>
                    {
                        new bs_Invoice
                        {
                            bs_InvoiceId = Guid.NewGuid(),
                            bs_InvoiceNumber = "INV-2021-01-01",
                            bs_Amount = 500.00m
                        }
                    },
                    bs_BillingStatement_cr9e8_readyforinvoice = new List<cr9e8_readyforinvoice>
                    {
                        new cr9e8_readyforinvoice
                        {
                            cr9e8_comments = "comment test 1"
                        }
                    }
                },
                new bs_BillingStatement
                {
                    bs_BillingStatementId = Guid.NewGuid(),
                    bs_ServicePeriodStart = new DateTime(2021, 8, 1),
                    bs_ServicePeriodEnd = new DateTime(2021, 8, 31),
                    bs_StatementStatus = bs_billingstatementstatuschoices.SENT,
                    bs_Invoice_BillingStatement = new List<bs_Invoice>
                    {
                        new bs_Invoice
                        {
                            bs_InvoiceId = Guid.NewGuid(),
                            bs_InvoiceNumber = "INV-2021-02-01",
                            bs_Amount = 750.00m
                        }
                    },
                    bs_BillingStatement_cr9e8_readyforinvoice = new List<cr9e8_readyforinvoice>
                    {
                        new cr9e8_readyforinvoice
                        {
                            cr9e8_comments = "comment test 2"
                        }
                    }
                }
            };

            _billingStatementRepository.GetCurrentBillingStatements().Returns(billingStatementsModel);

            var billingStatementsVo = new List<BillingStatementVo>
            {
                new BillingStatementVo
                {
                    Id = billingStatementsModel[0].bs_BillingStatementId,
                    ServicePeriodStart = new DateOnly(2021, 7, 1),
                    ServicePeriodEnd = new DateOnly(2021, 7, 31),
                    TotalAmount = 500.00m,
                    Status = StatementStatus.Sent,
                    AmNotes = "comment test 1",
                    Invoices = new List<InvoiceSummaryVo>
                    {
                        new InvoiceSummaryVo
                        {
                            Id = billingStatementsModel[0].bs_Invoice_BillingStatement.First().bs_InvoiceId,
                            InvoiceNumber = "INV-2021-01-01",
                            Amount = 500.00m,
                        }
                    }
                },
                new BillingStatementVo
                {
                    Id = billingStatementsModel[1].bs_BillingStatementId,
                    ServicePeriodStart = new DateOnly(2021, 8, 1),
                    ServicePeriodEnd = new DateOnly(2021, 8, 31),
                    TotalAmount = 750.00m,
                    Status = StatementStatus.Sent,
                    AmNotes = "comment test 2",
                    Invoices = new List<InvoiceSummaryVo>
                    {
                        new InvoiceSummaryVo
                        {
                            Id = billingStatementsModel[1].bs_Invoice_BillingStatement.First().bs_InvoiceId,
                            InvoiceNumber = "INV-2021-02-01",
                            Amount = 750.00m
                        }
                    }
                }
            };

            var result = _billingStatementService.GetCurrentBillingStatements(userAuth);
            result.Should().BeEquivalentTo(billingStatementsVo);
        }
    }
}
