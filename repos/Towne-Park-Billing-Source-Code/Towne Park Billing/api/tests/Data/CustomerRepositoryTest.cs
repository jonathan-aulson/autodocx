using api.Data.Impl;
using api.Services;
using FluentAssertions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NSubstitute;
using TownePark;
using Xunit;

namespace BackendTests.Data;

public class CustomerRepositoryTest
{
    private readonly IOrganizationService _organizationService;

    private readonly CustomerRepository _customerRepository;

    public CustomerRepositoryTest()
    {
        var dataverseService = Substitute.For<IDataverseService>();
        
        _organizationService = Substitute.For<IOrganizationService>();
        dataverseService.GetServiceClient().Returns(_organizationService);

        _customerRepository = new CustomerRepository(dataverseService);
    }

    [Fact]
    public void GetCustomersSummary_ShouldReturnListOfCustomers()
    {
        var entities = new EntityCollection(new List<Entity>
        {
            new bs_CustomerSite
            {
                bs_CustomerSiteId = Guid.NewGuid(),
                bs_SiteNumber = "SiteNum1",
                bs_SiteName = "Company A"
            }.ToEntity<bs_CustomerSite>(),
            new bs_CustomerSite
            {
                bs_CustomerSiteId = Guid.NewGuid(),
                bs_SiteNumber = "SiteNum2",
                bs_SiteName = "Company B"
            }.ToEntity<bs_CustomerSite>()
        });
        
        var queryExpressionCapture = Arg.Do<QueryExpression>(qe =>
        {
            qe.ColumnSet.AllColumns.Should().BeFalse();
            qe.ColumnSet.Columns.Should().BeEquivalentTo(new[]
            {
                bs_CustomerSite.Fields.bs_SiteNumber,
                bs_CustomerSite.Fields.bs_SiteName,
                bs_CustomerSite.Fields.bs_District
            });
        });

        _organizationService.RetrieveMultiple(queryExpressionCapture).Returns(entities);

        var result = _customerRepository.GetCustomersSummary();

        result.Should().BeEquivalentTo(new List<bs_CustomerSite>
        {
            new bs_CustomerSite
            {
                bs_CustomerSiteId = entities.Entities[0].Id,
                bs_SiteNumber = "SiteNum1",
                bs_SiteName = "Company A",
                bs_Contract_CustomerSite = new []{new bs_Contract()},
                bs_CustomerSite_cr9e8_readyforinvoice = new []{new cr9e8_readyforinvoice()},
                bs_BillingStatement_CustomerSite = new []{new bs_BillingStatement()}
            },
            new bs_CustomerSite
            {
                bs_CustomerSiteId = entities.Entities[1].Id,
                bs_SiteNumber = "SiteNum2",
                bs_SiteName = "Company B",
                bs_Contract_CustomerSite = new []{new bs_Contract()},
                bs_CustomerSite_cr9e8_readyforinvoice = new []{new cr9e8_readyforinvoice()},
                bs_BillingStatement_CustomerSite = new []{new bs_BillingStatement()}
            }
        });
    }

    [Fact]
    public void GetCustomerDetail_ShouldReturnCustomerDetail()
    {
        var customerId = Guid.NewGuid();
        var entity = new bs_CustomerSite
        {
            bs_CustomerSiteId = Guid.NewGuid(),
            bs_Address = "123 Main St",
            bs_SiteNumber = "SiteNum1",
            bs_SiteName = "Company A",
            bs_AccountManager = "John Doe",
            bs_InvoiceRecipient = "Jane Doe",
            bs_BillingContactEmail = "jane.doe@townepark.com"
        }.ToEntity<bs_CustomerSite>();

        
        var columnSetCapture = Arg.Do<ColumnSet>(cs =>
        {
            cs.AllColumns.Should().BeFalse();
            cs.Columns.Should().BeEquivalentTo(new[]
            {
                bs_CustomerSite.Fields.bs_CustomerSiteId,
                bs_CustomerSite.Fields.bs_Address,
                bs_CustomerSite.Fields.bs_SiteNumber,
                bs_CustomerSite.Fields.bs_SiteName,
                bs_CustomerSite.Fields.bs_AccountManager,
                bs_CustomerSite.Fields.bs_InvoiceRecipient,
                bs_CustomerSite.Fields.bs_BillingContactEmail,
                bs_CustomerSite.Fields.bs_StartDate,
                bs_CustomerSite.Fields.bs_CloseDate,
                bs_CustomerSite.Fields.bs_District,
                bs_CustomerSite.Fields.bs_GLString,
                bs_CustomerSite.Fields.bs_AccountManagerId,
                bs_CustomerSite.Fields.bs_TotalRoomsAvailable,
                bs_CustomerSite.Fields.bs_TotalAvailableParking,
                bs_CustomerSite.Fields.bs_DistrictManager,
                bs_CustomerSite.Fields.bs_AssistantDistrictManager,
                bs_CustomerSite.Fields.bs_AssistantAccountManager,
                bs_CustomerSite.Fields.bs_VendorId,
                bs_CustomerSite.Fields.bs_LegalEntity,
                bs_CustomerSite.Fields.bs_PLCategory,
                bs_CustomerSite.Fields.bs_SVPRegion,
                bs_CustomerSite.Fields.bs_COGSegment,
                bs_CustomerSite.Fields.bs_BusinessSegment,
            });
        });
        
        _organizationService.Retrieve(
            bs_CustomerSite.EntityLogicalName,
            customerId,
            columnSetCapture
        ).Returns(entity);

        var result = _customerRepository.GetCustomerDetail(customerId);

        result.Should().BeEquivalentTo(new bs_CustomerSite
        {
            bs_CustomerSiteId = entity.bs_CustomerSiteId,
            bs_Address = entity.bs_Address,
            bs_SiteNumber = entity.bs_SiteNumber,
            bs_SiteName = entity.bs_SiteName,
            bs_AccountManager = entity.bs_AccountManager,
            bs_InvoiceRecipient = entity.bs_InvoiceRecipient,
            bs_BillingContactEmail = entity.bs_BillingContactEmail
        });
    }
    
    [Fact]
    public void UpdateCustomerDetail_ShouldUpdateCustomer()
    {
        var customerSiteId = Guid.NewGuid();
        var model = new bs_CustomerSite
        {
            bs_Address = "NewAddress",
            bs_SiteNumber = "NewSiteNumber",
            bs_SiteName = "NewCompanyName",
            bs_AccountManager = "NewAccountManager",
            bs_InvoiceRecipient = "NewBillingContactName",
            bs_BillingContactEmail = "new.billing@townepark.com"
        };

        _customerRepository.UpdateCustomerDetail(customerSiteId, model);

        _organizationService.Received(1).Update(Arg.Is<Entity>(entity =>
            entity.Id == customerSiteId
            && entity.GetAttributeValue<string>(bs_CustomerSite.Fields.bs_Address) == model.bs_Address
            && entity.GetAttributeValue<string>(bs_CustomerSite.Fields.bs_SiteNumber) == model.bs_SiteNumber
            && entity.GetAttributeValue<string>(bs_CustomerSite.Fields.bs_SiteName) == model.bs_SiteName
            && entity.GetAttributeValue<string>(bs_CustomerSite.Fields.bs_AccountManager) == model.bs_AccountManager
            && entity.GetAttributeValue<string>(bs_CustomerSite.Fields.bs_InvoiceRecipient) == model.bs_InvoiceRecipient
            && entity.GetAttributeValue<string>(bs_CustomerSite.Fields.bs_BillingContactEmail) == model.bs_BillingContactEmail
        ));
    }
}