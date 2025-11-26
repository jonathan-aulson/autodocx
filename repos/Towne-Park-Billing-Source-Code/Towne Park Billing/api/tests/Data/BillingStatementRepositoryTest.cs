using api.Data.Impl;
using api.Services;
using FluentAssertions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NSubstitute;
using TownePark;
using Xunit;

namespace BackendTests.Data
{
    public class BillingStatementRepositoryTest
    {
        private readonly IDataverseService _dataverseService;
        private readonly IOrganizationService _organizationService;
        private readonly BillingStatementRepository _billingStatementRepository;

        public BillingStatementRepositoryTest()
        {
            _dataverseService = Substitute.For<IDataverseService>();
            _organizationService = Substitute.For<IOrganizationService>();
            _dataverseService.GetServiceClient().Returns(_organizationService);

            _billingStatementRepository = new BillingStatementRepository(_dataverseService);
        }

        [Fact]
        public void GetBillingStatementsByCustomerSite_ShouldReturnListOfBillingStatements()
        {
            var customerSiteId = Guid.NewGuid();
            var billingStatementId1 = Guid.NewGuid();
            var billingStatementId2 = Guid.NewGuid();
            var invoiceId1 = Guid.NewGuid();
            var invoiceId2 = Guid.NewGuid();

            var CustomerSiteEntityAlias = "customer_site";
            var InvoiceEntityAlias = "invoice";

            var customerSiteReference = new EntityReference(bs_CustomerSite.EntityLogicalName, customerSiteId);

            var entities = new EntityCollection(new List<Entity>
            {
                new Entity(bs_BillingStatement.EntityLogicalName)
                {
                    Id = billingStatementId1,
                    [bs_BillingStatement.Fields.bs_BillingStatementId] = billingStatementId1,
                    [bs_BillingStatement.Fields.bs_ServicePeriodStart] = new DateTime(2021, 7, 1),
                    [bs_BillingStatement.Fields.bs_ServicePeriodEnd] = new DateTime(2021, 7, 31),
                    [bs_BillingStatement.Fields.bs_StatementStatus] = new OptionSetValue((int)bs_billingstatementstatuschoices.SENT),
                    [bs_BillingStatement.Fields.bs_CustomerSiteFK] = customerSiteReference,

                    // Mocked link attributes with AliasedValue
                    [$"{CustomerSiteEntityAlias}.{bs_CustomerSite.Fields.bs_CustomerSiteId}"] = new AliasedValue(CustomerSiteEntityAlias, bs_CustomerSite.Fields.bs_CustomerSiteId, customerSiteId),
                    [$"{CustomerSiteEntityAlias}.{bs_CustomerSite.Fields.bs_SiteNumber}"] = new AliasedValue(CustomerSiteEntityAlias, bs_CustomerSite.Fields.bs_SiteNumber, "Site#123"),
                    [$"{CustomerSiteEntityAlias}.{bs_CustomerSite.Fields.bs_SiteName}"] = new AliasedValue(CustomerSiteEntityAlias, bs_CustomerSite.Fields.bs_SiteName, "Main Site"),

                    [$"{InvoiceEntityAlias}.{bs_Invoice.Fields.bs_InvoiceId}"] = new AliasedValue("invoice", bs_Invoice.Fields.bs_InvoiceId, invoiceId1),
                    [$"{InvoiceEntityAlias}.{bs_Invoice.Fields.bs_InvoiceNumber}"] = new AliasedValue("invoice", bs_Invoice.Fields.bs_InvoiceNumber, "INV-2021-01-01"),
                    [$"{InvoiceEntityAlias}.{bs_Invoice.Fields.bs_Amount}"] = new AliasedValue("invoice", bs_Invoice.Fields.bs_Amount, 500.00m)
                }
            });

            _organizationService.RetrieveMultiple(Arg.Any<QueryExpression>()).Returns(entities);

            var result = _billingStatementRepository.GetBillingStatementsByCustomerSite(customerSiteId);

            var expectedBillingStatements = new List<bs_BillingStatement>
            {
                new bs_BillingStatement
                {
                    bs_BillingStatementId = billingStatementId1,
                    bs_ServicePeriodStart = new DateTime(2021, 7, 1),
                    bs_ServicePeriodEnd = new DateTime(2021, 7, 31),
                    bs_StatementStatus = bs_billingstatementstatuschoices.SENT,
                    bs_CustomerSiteFK = customerSiteReference,
                    bs_Invoice_BillingStatement = new List<bs_Invoice>
                    {
                        new bs_Invoice
                        {
                            bs_InvoiceId = invoiceId1,
                            bs_InvoiceNumber = "INV-2021-01-01",
                            bs_Amount = 500.00m
                        }
                    }
                }
            };

            // Assert
            result.Should().NotBeNull();
            var resultArray = result.ToArray();
            resultArray.Length.Should().Be(1);
            resultArray[0].Should().NotBeNull();

            var result1 = resultArray[0];
            result1.Should().NotBeNull();
            result1.bs_BillingStatementId.Should().Be(billingStatementId1);
            result1.bs_ServicePeriodStart.Should().Be(new DateTime(2021, 7, 1));
            result1.bs_ServicePeriodEnd.Should().Be(new DateTime(2021, 7, 31));
            result1.bs_StatementStatus.Should().Be(bs_billingstatementstatuschoices.SENT);
            result1.bs_Invoice_BillingStatement.Should().NotBeNull();
            var result1Invoice = result1.bs_Invoice_BillingStatement.ToArray();
            var result1Invoice1 = result1Invoice[0];
            result1Invoice1.bs_InvoiceId.Should().Be(invoiceId1);
            result1Invoice1.bs_InvoiceNumber.Should().Be("INV-2021-01-01");
            result1Invoice1.bs_Amount.Should().Be(500.00m);
        }

        [Fact]
        public void GetCurrentBillingStatements_ShouldReturnListOfBillingStatements()
        {
            // Arrange
            var customerSiteId = Guid.NewGuid();
            var billingStatementId1 = Guid.NewGuid();
            var invoiceId1 = Guid.NewGuid();

            var CustomerSiteEntityAlias = "customer_site";
            var InvoiceEntityAlias = "invoice";

            var entities = new EntityCollection(new List<Entity>
        {
            new Entity(bs_BillingStatement.EntityLogicalName)
            {
                Id = billingStatementId1,
                [bs_BillingStatement.Fields.bs_BillingStatementId] = billingStatementId1,
                [bs_BillingStatement.Fields.CreatedOn] = DateTime.Now,
                [bs_BillingStatement.Fields.bs_StatementStatus] = new OptionSetValue((int)bs_billingstatementstatuschoices.APPROVED),
                [bs_BillingStatement.Fields.bs_ServicePeriodStart] = new DateTime(2021, 7, 1),
                [bs_BillingStatement.Fields.bs_ServicePeriodEnd] = new DateTime(2021, 7, 31),

                // Mocked link attributes with AliasedValue
                [$"{CustomerSiteEntityAlias}.{bs_CustomerSite.Fields.bs_CustomerSiteId}"] = new AliasedValue(CustomerSiteEntityAlias, bs_CustomerSite.Fields.bs_CustomerSiteId, customerSiteId),
                [$"{CustomerSiteEntityAlias}.{bs_CustomerSite.Fields.bs_SiteNumber}"] = new AliasedValue(CustomerSiteEntityAlias, bs_CustomerSite.Fields.bs_SiteNumber, "Site#123"),
                [$"{CustomerSiteEntityAlias}.{bs_CustomerSite.Fields.bs_SiteName}"] = new AliasedValue(CustomerSiteEntityAlias, bs_CustomerSite.Fields.bs_SiteName, "Main Site"),

                [$"{InvoiceEntityAlias}.{bs_Invoice.Fields.bs_InvoiceId}"] = new AliasedValue(InvoiceEntityAlias, bs_Invoice.Fields.bs_InvoiceId, invoiceId1),
                [$"{InvoiceEntityAlias}.{bs_Invoice.Fields.bs_InvoiceNumber}"] = new AliasedValue(InvoiceEntityAlias, bs_Invoice.Fields.bs_InvoiceNumber, "INV-2021-01-01"),
                [$"{InvoiceEntityAlias}.{bs_Invoice.Fields.bs_Amount}"] = new AliasedValue(InvoiceEntityAlias, bs_Invoice.Fields.bs_Amount, 500.00m),
            }
        });

            var serviceClient = Substitute.For<IOrganizationService>();
            serviceClient.RetrieveMultiple(Arg.Any<QueryExpression>()).Returns(entities);

            _dataverseService.GetServiceClient().Returns(serviceClient);

            // Act
            var result = _billingStatementRepository.GetCurrentBillingStatements();

            // Assert
            result.Should().NotBeNull();
            var resultArray = result.ToArray();
            resultArray.Length.Should().Be(1);

            var result1 = resultArray[0];
            result1.bs_BillingStatementId.Should().Be(billingStatementId1);
            result1.bs_ServicePeriodStart.Should().Be(new DateTime(2021, 7, 1));
            result1.bs_ServicePeriodEnd.Should().Be(new DateTime(2021, 7, 31));
            result1.bs_StatementStatus.Should().Be(bs_billingstatementstatuschoices.APPROVED);

            result1.bs_BillingStatement_CustomerSite.Should().NotBeNull();
            result1.bs_BillingStatement_CustomerSite.bs_CustomerSiteId.Should().Be(customerSiteId);
            result1.bs_BillingStatement_CustomerSite.bs_SiteNumber.Should().Be("Site#123");
            result1.bs_BillingStatement_CustomerSite.bs_SiteName.Should().Be("Main Site");

            result1.bs_Invoice_BillingStatement.Should().NotBeNull();
            var invoices = result1.bs_Invoice_BillingStatement.ToArray();
            invoices.Should().HaveCount(1);

            var invoice = invoices[0];
            invoice.bs_InvoiceId.Should().Be(invoiceId1);
            invoice.bs_InvoiceNumber.Should().Be("INV-2021-01-01");
            invoice.bs_Amount.Should().Be(500.00m);
        }
    }
}
