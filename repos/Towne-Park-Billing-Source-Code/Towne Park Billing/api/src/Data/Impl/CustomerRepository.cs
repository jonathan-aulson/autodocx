using System.ServiceModel;
using api.Services;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using TownePark;

namespace api.Data.Impl;

public class CustomerRepository : ICustomerRepository
{
    private readonly IDataverseService _dataverseService;
    
    private const string ContractEntityAlias = "contract";
    private const string BillingStatementEntityAlias = "billingstatement";

    public CustomerRepository(IDataverseService dataverseService)
    {
        _dataverseService = dataverseService;
    }
    
    public IEnumerable<bs_CustomerSite> GetCustomersSummary()
    {
        var columnSet = new ColumnSet(
            bs_CustomerSite.Fields.bs_SiteNumber, 
            bs_CustomerSite.Fields.bs_SiteName,
            bs_CustomerSite.Fields.bs_District);
        return GetCustomersWithContracts(columnSet);
    }

    public bs_CustomerSite GetCustomerDetail(Guid customerSiteId)
    {
        var columnSet = new ColumnSet(bs_CustomerSite.Fields.bs_CustomerSiteId, bs_CustomerSite.Fields.bs_SiteNumber, 
            bs_CustomerSite.Fields.bs_SiteName, bs_CustomerSite.Fields.bs_AccountManager,
            bs_CustomerSite.Fields.bs_InvoiceRecipient, bs_CustomerSite.Fields.bs_BillingContactEmail, bs_CustomerSite.Fields.bs_Address,
            bs_CustomerSite.Fields.bs_AccountManagerId, bs_CustomerSite.Fields.bs_CloseDate, bs_CustomerSite.Fields.bs_District, 
            bs_CustomerSite.Fields.bs_GLString, bs_CustomerSite.Fields.bs_StartDate,
            bs_CustomerSite.Fields.bs_TotalRoomsAvailable, bs_CustomerSite.Fields.bs_TotalAvailableParking,
            bs_CustomerSite.Fields.bs_DistrictManager, bs_CustomerSite.Fields.bs_AssistantDistrictManager,
            bs_CustomerSite.Fields.bs_AssistantAccountManager, bs_CustomerSite.Fields.bs_VendorId,
            bs_CustomerSite.Fields.bs_LegalEntity, bs_CustomerSite.Fields.bs_PLCategory,
            bs_CustomerSite.Fields.bs_SVPRegion, bs_CustomerSite.Fields.bs_COGSegment,
            bs_CustomerSite.Fields.bs_BusinessSegment);
        return GetCustomerWithColumnSet(customerSiteId, columnSet);
    }

    public Guid GetIdBySiteNumber(string siteNumber)
    {
        var serviceClient = _dataverseService.GetServiceClient();
        var query = new QueryExpression(bs_CustomerSite.EntityLogicalName)
        {
            ColumnSet = new ColumnSet(bs_CustomerSite.Fields.bs_CustomerSiteId),
            Criteria = new FilterExpression
            {
                Conditions =
                {
                    new ConditionExpression(bs_CustomerSite.Fields.bs_SiteNumber, ConditionOperator.Equal, siteNumber)
                }
            }
        };
        var result = serviceClient.RetrieveMultiple(query);
        if (result.Entities.Count == 0)
        {
            throw new Exception($"No customer site found with site number: {siteNumber}");
        }
        return result.Entities[0].GetAttributeValue<Guid>(bs_CustomerSite.Fields.bs_CustomerSiteId);
    }

    public void UpdateCustomerDetail(Guid customerSiteId, bs_CustomerSite model)
    {
        var serviceClient = _dataverseService.GetServiceClient();

        model.Id = customerSiteId;
        serviceClient.Update(model);
    }

    private IEnumerable<bs_CustomerSite> GetCustomersWithContracts(ColumnSet columnSet)
    {
        var serviceClient = _dataverseService.GetServiceClient();
        var query = new QueryExpression(bs_CustomerSite.EntityLogicalName)
        {
            ColumnSet = columnSet
        };

        var contractLink = query.AddLink(
            linkToAttributeName: bs_Contract.Fields.bs_CustomerSiteFK,
            linkToEntityName: bs_Contract.EntityLogicalName,
            linkFromAttributeName: bs_CustomerSite.Fields.bs_CustomerSiteId,
            joinOperator: JoinOperator.LeftOuter
        );
        contractLink.Columns = new ColumnSet(
            bs_Contract.Fields.bs_ContractTypeString,
            bs_Contract.Fields.bs_BillingType,
            bs_Contract.Fields.bs_Deposits,
            bs_Contract.Fields.bs_CustomerSiteFK);
        contractLink.EntityAlias = ContractEntityAlias;

        // Define the link entity for ready for invoice
        var readyForInvoiceLink = query.AddLink(
            linkToAttributeName: cr9e8_readyforinvoice.Fields.cr9e8_site,
            linkToEntityName: cr9e8_readyforinvoice.EntityLogicalName,
            linkFromAttributeName: bs_CustomerSite.Fields.bs_SiteNumber,
            joinOperator: JoinOperator.LeftOuter
        );
        readyForInvoiceLink.Columns = new ColumnSet(
            cr9e8_readyforinvoice.Fields.cr9e8_comments,
            cr9e8_readyforinvoice.Fields.cr9e8_invoicestatus,
            cr9e8_readyforinvoice.Fields.cr9e8_period);

        // Calculate the previous month in "yyyyMM" format
        string previousMonth = DateTime.UtcNow.AddMonths(-1).ToString("yyyyMM");

        // Add conditions to filter the cr9e8_period
        var periodCondition = new FilterExpression(LogicalOperator.Or);
        periodCondition.AddCondition("cr9e8_period", ConditionOperator.Null);
        periodCondition.AddCondition("cr9e8_period", ConditionOperator.Equal, previousMonth);
        readyForInvoiceLink.LinkCriteria.AddFilter(periodCondition);

        var billingStatementLink = query.AddLink(
            linkToAttributeName: bs_BillingStatement.Fields.bs_CustomerSiteFK,
            linkToEntityName: bs_BillingStatement.EntityLogicalName,
            linkFromAttributeName: bs_CustomerSite.Fields.bs_CustomerSiteId,
            joinOperator: JoinOperator.LeftOuter
        );
        billingStatementLink.Columns = new ColumnSet(
            bs_BillingStatement.Fields.bs_CustomerSiteFK,
            bs_BillingStatement.Fields.bs_BillingStatementId,
            bs_BillingStatement.Fields.CreatedOn);
        billingStatementLink.EntityAlias = BillingStatementEntityAlias;

        var now = DateTime.UtcNow;
        var currentMonthStart = new DateTime(now.Year, now.Month, 1);
        var nextMonth = currentMonthStart.AddMonths(1);
        var currentMonthEnd = nextMonth.AddDays(-1);

        var billingPeriodCondition = new FilterExpression(LogicalOperator.And);
        billingPeriodCondition.AddCondition(bs_BillingStatement.Fields.CreatedOn, ConditionOperator.OnOrAfter, currentMonthStart);
        billingPeriodCondition.AddCondition(bs_BillingStatement.Fields.CreatedOn, ConditionOperator.OnOrBefore, currentMonthEnd);
        billingStatementLink.LinkCriteria.AddFilter(billingPeriodCondition);

        var result = serviceClient.RetrieveMultiple(query);

        var uniqueEntities = result.Entities
            .GroupBy(e => e.Id) // Group by the primary entity's Id
            .Select(g => g.First()) // Select the first entity from each group
            .ToList();

        return uniqueEntities.Select(entity =>
        {
            var customerSite = entity.ToEntity<bs_CustomerSite>();
            ParseContractAttributeValues(customerSite);
            ParseReadyForInvoiceAttributeValues(customerSite);
            ParseBillingStatementAttributeValues(customerSite);
            return customerSite;
        });
    }

    private void ParseBillingStatementAttributeValues(bs_CustomerSite customerSite)
    {
        var billingStatement = new bs_BillingStatement();

        var billingStatementId = customerSite
            .GetAttributeValue<AliasedValue>(BillingStatementEntityAlias + "." +
                                                    bs_BillingStatement.Fields.bs_BillingStatementId);
        if (billingStatementId is not null) billingStatement.bs_BillingStatementId = (Guid)billingStatementId.Value;

        var servicePeriodStart = customerSite
            .GetAttributeValue<AliasedValue>(BillingStatementEntityAlias + "." +
                                                    bs_BillingStatement.Fields.bs_ServicePeriodStart);
        if (servicePeriodStart is not null) billingStatement.bs_ServicePeriodStart = (DateTime)servicePeriodStart.Value;
        var servicePeriodEnd = customerSite
            .GetAttributeValue<AliasedValue>(BillingStatementEntityAlias + "." +
                                                    bs_BillingStatement.Fields.bs_ServicePeriodEnd);
        if (servicePeriodEnd is not null) billingStatement.bs_ServicePeriodEnd = (DateTime)servicePeriodEnd.Value;

        customerSite.bs_BillingStatement_CustomerSite = new[] { billingStatement };
    }

    private void ParseReadyForInvoiceAttributeValues(bs_CustomerSite customerSite)
    {
        var readyForInvoice = new cr9e8_readyforinvoice();

        var invoiceStatus = customerSite
            .GetAttributeValue<AliasedValue>("cr9e8_readyforinvoice1.cr9e8_invoicestatus");
        if (invoiceStatus is not null) readyForInvoice.cr9e8_invoicestatus = (string)invoiceStatus.Value;

        var period = customerSite
            .GetAttributeValue<AliasedValue>("cr9e8_readyforinvoice1.cr9e8_period");
        if (period is not null) readyForInvoice.cr9e8_period = (string)period.Value;

        customerSite.bs_CustomerSite_cr9e8_readyforinvoice = new[] { readyForInvoice };
    }

    private void ParseContractAttributeValues(bs_CustomerSite customerSite)
    {
        var contract = new bs_Contract();
        var contractType = customerSite
            .GetAttributeValue<AliasedValue>(ContractEntityAlias + "." +
                                             bs_Contract.Fields.bs_ContractTypeString);
        if (contractType is not null) contract.bs_ContractTypeString = (string) contractType.Value;
        var billingType = customerSite
            .GetAttributeValue<AliasedValue>(ContractEntityAlias + "." +
                                             bs_Contract.Fields.bs_BillingType);
        if (billingType is not null) contract.bs_BillingType = (bs_billingtypechoices) ((OptionSetValue) billingType.Value).Value;
        var deposits = customerSite
            .GetAttributeValue<AliasedValue>(ContractEntityAlias + "." +
                                             bs_Contract.Fields.bs_Deposits);
        if (deposits is not null) contract.bs_Deposits = (bool) deposits.Value;
        customerSite.bs_Contract_CustomerSite = new[] { contract };
    }
    
    private bs_CustomerSite GetCustomerWithColumnSet(Guid customerSiteId, ColumnSet columnSet)
    {
        var serviceClient = _dataverseService.GetServiceClient();

        return (bs_CustomerSite) serviceClient.Retrieve(bs_CustomerSite.EntityLogicalName, customerSiteId, columnSet);
    }

    public bs_MasterCustomerSite GetMasterCustomer(string siteNumber)
    {
        var serviceClient = _dataverseService.GetServiceClient();
        var target = new EntityReference(bs_MasterCustomerSite.EntityLogicalName, bs_MasterCustomerSite.Fields.bs_SiteNumber,
            siteNumber);
        var columnSet = new ColumnSet(bs_MasterCustomerSite.Fields.bs_SiteNumber,
            bs_MasterCustomerSite.Fields.bs_SiteName,
            bs_MasterCustomerSite.Fields.bs_SiteNumber,
            bs_MasterCustomerSite.Fields.bs_AccountManager,
            bs_MasterCustomerSite.Fields.bs_AccountManagerId,
            bs_MasterCustomerSite.Fields.bs_Address,
            bs_MasterCustomerSite.Fields.bs_BillingContactEmail,
            bs_MasterCustomerSite.Fields.bs_ContractTypeString,
            bs_MasterCustomerSite.Fields.bs_Deposits,
            bs_MasterCustomerSite.Fields.bs_District,
            bs_MasterCustomerSite.Fields.bs_GLString,
            bs_MasterCustomerSite.Fields.bs_StartDate,
            bs_MasterCustomerSite.Fields.bs_LegalEntity,
            bs_MasterCustomerSite.Fields.bs_PLCategory,
            bs_MasterCustomerSite.Fields.bs_SVPRegion,
            bs_MasterCustomerSite.Fields.bs_COGSegment,
            bs_MasterCustomerSite.Fields.bs_BusinessSegment);
        var request = new RetrieveRequest()
        {
            ColumnSet = columnSet,
            Target = target
        };
        try
        {
            var response = (RetrieveResponse) serviceClient.Execute(request);
            return response.Entity.ToEntity<bs_MasterCustomerSite>();
        } catch (FaultException<OrganizationServiceFault> exc)
        {
            throw new MasterCustomerSiteNotFoundExc();
        }
    }

    public Guid AddCustomer(bs_CustomerSite customerSite)
    {
        var serviceClient = _dataverseService.GetServiceClient();

        // Find an existing customer with the same site number
        var query = new QueryExpression(bs_CustomerSite.EntityLogicalName)
        {
            ColumnSet = new ColumnSet(bs_CustomerSite.Fields.bs_SiteNumber),
            Criteria = new FilterExpression
            {
                Conditions =
            {
                new ConditionExpression(bs_CustomerSite.Fields.bs_SiteNumber, ConditionOperator.Equal, customerSite.bs_SiteNumber)
            }
            }
        };

        var existingCustomers = serviceClient.RetrieveMultiple(query);

        // If any records are found, a customer with the same site number already exists
        if (existingCustomers.Entities.Any())
        {
            throw new DuplicateCustomerSiteExc();
        }

        return serviceClient.Create(customerSite);
    }

    public IEnumerable<Guid> GetSiteIdsByUser(string userEmailAddress)
    {
        var serviceClient = _dataverseService.GetServiceClient();

        var query = new QueryExpression(bs_CustomerSitesByUser.EntityLogicalName)
        {
            ColumnSet = new ColumnSet(bs_CustomerSitesByUser.Fields.bs_CustomerSiteFK),
            Criteria = new FilterExpression()
        };

        query.AddLink(bs_User.EntityLogicalName, bs_CustomerSitesByUser.Fields.bs_UserFK, bs_User.Fields.Id)
            .LinkCriteria.AddCondition(bs_User.Fields.bs_Email, ConditionOperator.Equal, userEmailAddress);

        // Execute the query to get the Customer Site FKs
        var assignedSites = serviceClient.RetrieveMultiple(query);

        var customerSiteIds = assignedSites.Entities
            .Select(e => e.GetAttributeValue<EntityReference>(bs_CustomerSitesByUser.Fields.bs_CustomerSiteFK))
            .Where(er => er != null)
            .Select(er => er.Id)
            .ToList();

        return customerSiteIds;
    }

    public IEnumerable<bs_CustomerSite> GetSitesByUser(IEnumerable<Guid> customerSiteIds, string email)
    {
        var serviceClient = _dataverseService.GetServiceClient();

        var customerSiteQuery = new QueryExpression(bs_CustomerSite.EntityLogicalName)
        {
            ColumnSet = new ColumnSet(true),
            Criteria = new FilterExpression
            {
                Conditions =
            {
                new ConditionExpression("bs_customersiteid", ConditionOperator.In, customerSiteIds.ToArray())
            }
            }
        };

        AddContractLink(customerSiteQuery);
        AddBillingStatementLink(customerSiteQuery);
        AddReadyForInvoiceLink(customerSiteQuery);

        var customerSites = serviceClient.RetrieveMultiple(customerSiteQuery);

        var uniqueEntities = customerSites.Entities
            .GroupBy(e => e.Id) // Group by the primary entity's Id
            .Select(g => g.First()) // Select the first entity from each group
            .ToList();

        return uniqueEntities.Select(entity =>
        {
            var customerSite = entity.ToEntity<bs_CustomerSite>();
            ParseContractAttributeValues(customerSite);
            ParseReadyForInvoiceAttributeValues(customerSite);
            ParseBillingStatementAttributeValues(customerSite);
            return customerSite;
        });
    }

    private void AddContractLink(QueryExpression query)
    {
        var contractLink = query.AddLink(
            linkToAttributeName: bs_Contract.Fields.bs_CustomerSiteFK,
            linkToEntityName: bs_Contract.EntityLogicalName,
            linkFromAttributeName: bs_CustomerSite.Fields.bs_CustomerSiteId,
            joinOperator: JoinOperator.LeftOuter
        );
        contractLink.Columns = new ColumnSet(
            bs_Contract.Fields.bs_ContractTypeString,
            bs_Contract.Fields.bs_BillingType,
            bs_Contract.Fields.bs_Deposits,
            bs_Contract.Fields.bs_CustomerSiteFK);
        contractLink.EntityAlias = ContractEntityAlias;
    }

    private void AddBillingStatementLink(QueryExpression query)
    {
        var billingStatementLink = query.AddLink(
            linkToAttributeName: bs_BillingStatement.Fields.bs_CustomerSiteFK,
            linkToEntityName: bs_BillingStatement.EntityLogicalName,
            linkFromAttributeName: bs_CustomerSite.Fields.bs_CustomerSiteId,
            joinOperator: JoinOperator.LeftOuter
        );
        billingStatementLink.Columns = new ColumnSet(
            bs_BillingStatement.Fields.bs_CustomerSiteFK,
            bs_BillingStatement.Fields.bs_BillingStatementId,
            bs_BillingStatement.Fields.CreatedOn);
        billingStatementLink.EntityAlias = BillingStatementEntityAlias;

        // Add bs_BillingStatement filter conditions
        var now = DateTime.UtcNow;
        var currentMonthStart = new DateTime(now.Year, now.Month, 1);
        var nextMonth = currentMonthStart.AddMonths(1);
        var currentMonthEnd = nextMonth.AddDays(-1);

        var billingPeriodCondition = new FilterExpression(LogicalOperator.And);
        billingPeriodCondition.AddCondition(bs_BillingStatement.Fields.CreatedOn, ConditionOperator.OnOrAfter, currentMonthStart);
        billingPeriodCondition.AddCondition(bs_BillingStatement.Fields.CreatedOn, ConditionOperator.OnOrBefore, currentMonthEnd);
        billingStatementLink.LinkCriteria.AddFilter(billingPeriodCondition);
    }

    private void AddReadyForInvoiceLink(QueryExpression query)
    {
        var readyForInvoiceLink = query.AddLink(
            linkToAttributeName: cr9e8_readyforinvoice.Fields.cr9e8_site,
            linkToEntityName: cr9e8_readyforinvoice.EntityLogicalName,
            linkFromAttributeName: bs_CustomerSite.Fields.bs_SiteNumber,
            joinOperator: JoinOperator.LeftOuter
        );
        readyForInvoiceLink.Columns = new ColumnSet(
            cr9e8_readyforinvoice.Fields.cr9e8_comments,
            cr9e8_readyforinvoice.Fields.cr9e8_invoicestatus,
            cr9e8_readyforinvoice.Fields.cr9e8_period);

        // Calculate the previous month in "yyyyMM" format
        string previousMonth = DateTime.UtcNow.AddMonths(-1).ToString("yyyyMM");

        // Add conditions to filter the cr9e8_period
        var periodCondition = new FilterExpression(LogicalOperator.Or);
        periodCondition.AddCondition("cr9e8_period", ConditionOperator.Null);
        periodCondition.AddCondition("cr9e8_period", ConditionOperator.Equal, previousMonth);
        readyForInvoiceLink.LinkCriteria.AddFilter(periodCondition);
    }
}

public class MasterCustomerSiteNotFoundExc : Exception {}

public class DuplicateCustomerSiteExc : Exception {}