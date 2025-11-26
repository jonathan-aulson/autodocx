using api.Services;
using Microsoft.Xrm.Sdk;
using TownePark;
using Microsoft.Xrm.Sdk.Query;
using api.Models.Vo;
using Grpc.Core;

namespace api.Data.Impl
{
    public class BillingStatementRepository : IBillingStatementRepository
    {
        private const string CustomerSiteEntityAlias = "customer_site";
        private const string InvoiceEntityAlias = "invoice";
        private const string ReadyForInvoiceEntityAlias = "ready_for_invoice";
        private const string ContractEntityAlias = "contract";
        private readonly IDataverseService _dataverseService;

        public BillingStatementRepository(IDataverseService dataverseService)
        {
            _dataverseService = dataverseService;
        }

        public IEnumerable<bs_BillingStatement> GetCurrentBillingStatements()
        {
            return GetBillingStatements(
                (query) =>
                {
                    AddCustomerSiteLinkToQuery(query);
                    AddInvoiceLinkToQuery(query);
                    AddReadyForInvoiceLinkToQuery(query);
                },
                AddIsCurrentConditionToQuery,
                (statement, group) =>
                {
                    ParseCustomerSiteAttributeValues(statement);
                    ParseInvoiceAttributeValues(statement, group);
                    ParseReadyForInvoiceAttributeValues(statement);
                }); 
        }

        public IEnumerable<bs_BillingStatement> GetBillingStatementsByCustomerSite(Guid customerSiteId)
        {
            return GetBillingStatements(
                (query) => {
                    AddCustomerSiteLinkToQuery(query);
                    AddInvoiceLinkToQuery(query);
                    AddReadyForInvoiceLinkToQuery(query);
                },
                (query => AddCustomerSiteIdConditionToQuery(query, customerSiteId)),
                (statement, group) =>
                {
                    ParseCustomerSiteAttributeValues(statement);
                    ParseInvoiceAttributeValues(statement, group);
                    ParseReadyForInvoiceAttributeValues(statement);
                });
        }


        private void AddCustomerSiteLinkToQuery(QueryExpression query)
        {
            var customerSiteLink = query.AddLink(
                linkFromAttributeName: bs_BillingStatement.Fields.bs_CustomerSiteFK,
                linkToEntityName: bs_CustomerSite.EntityLogicalName,
                linkToAttributeName: bs_CustomerSite.Fields.bs_CustomerSiteId,
                joinOperator: JoinOperator.LeftOuter
            );
            customerSiteLink.Columns = new ColumnSet(
                bs_CustomerSite.Fields.bs_CustomerSiteId,
                bs_CustomerSite.Fields.bs_SiteNumber,
                bs_CustomerSite.Fields.bs_SiteName
            );
            customerSiteLink.EntityAlias = CustomerSiteEntityAlias;
        }

        private void AddReadyForInvoiceLinkToQuery(QueryExpression query)
        {
            var readyForInvoiceLink = query.AddLink(
                linkFromAttributeName: "bs_customersitefkname",
                linkToEntityName: cr9e8_readyforinvoice.EntityLogicalName,
                linkToAttributeName: cr9e8_readyforinvoice.Fields.cr9e8_site,
                joinOperator: JoinOperator.LeftOuter
                );
            readyForInvoiceLink.Columns = new ColumnSet(
                cr9e8_readyforinvoice.Fields.cr9e8_comments,
                cr9e8_readyforinvoice.Fields.cr9e8_period);
            readyForInvoiceLink.EntityAlias = ReadyForInvoiceEntityAlias;
        }

        private void AddInvoiceLinkToQuery(QueryExpression query)
        {
            var invoiceLink = query.AddLink(
                linkFromAttributeName: bs_BillingStatement.Fields.bs_BillingStatementId,
                linkToEntityName: bs_Invoice.EntityLogicalName,
                linkToAttributeName: bs_Invoice.Fields.bs_BillingStatementFK,
                joinOperator: JoinOperator.LeftOuter
            );
            invoiceLink.Columns = new ColumnSet(
                bs_Invoice.Fields.bs_InvoiceId,
                bs_Invoice.Fields.bs_InvoiceNumber,
                bs_Invoice.Fields.bs_Amount
            );
            invoiceLink.EntityAlias = InvoiceEntityAlias;
        }
        
        private void AddIsCurrentConditionToQuery(QueryExpression query)
        {
            // Condition 1: bs_BillingStatement.Fields.bs_StatementStatus is anything other than bs_billingstatementstatuschoices.SENT
            var statusCondition = new ConditionExpression(bs_BillingStatement.Fields.bs_StatementStatus, ConditionOperator.NotEqual, (int) bs_billingstatementstatuschoices.SENT);

            // Condition 2: bs_BillingStatement.Fields.bs_ServicePeriodStart matches the current month and year
            var currentDate = DateTime.Now;
            var previousMonthStart = new DateTime(currentDate.Year, currentDate.Month - 1, 1);
            var previousMonthEnd = previousMonthStart.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59); // Last day of the previous month

            var nextMonthStart = new DateTime(currentDate.Year, currentDate.Month + 1, 1);
            var nextMonthEnd = nextMonthStart.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59); // Last day of the next month

            var dateConditionPreviousMonth = new ConditionExpression(bs_BillingStatement.Fields.bs_ServicePeriodStart,
                ConditionOperator.Between, new object[] { previousMonthStart, previousMonthEnd });         

            var dateConditionNextMonth = new ConditionExpression(bs_BillingStatement.Fields.bs_ServicePeriodStart,
                ConditionOperator.Between, new object[] { nextMonthStart, nextMonthEnd });

            // Creating filter to combine conditions with OR logic
            var filter = new FilterExpression(LogicalOperator.Or);
            filter.AddCondition(statusCondition);
            filter.AddCondition(dateConditionPreviousMonth);
            filter.AddCondition(dateConditionNextMonth);

            // Adding the filter to the query
            query.Criteria.AddFilter(filter);
        }

        private void AddCustomerSiteIdConditionToQuery(QueryExpression query, Guid customerSiteId)
        {
            query.Criteria.AddCondition(bs_BillingStatement.Fields.bs_CustomerSiteFK, ConditionOperator.Equal,
                customerSiteId);
        }

        private void ParseReadyForInvoiceAttributeValues(bs_BillingStatement statement)
        {
            var readyForInvoice = new cr9e8_readyforinvoice();

            var amNotes = statement
                .GetAttributeValue<AliasedValue>(ReadyForInvoiceEntityAlias + "." +
                                                cr9e8_readyforinvoice.Fields.cr9e8_comments)?.Value;
            if (amNotes is not null) readyForInvoice.cr9e8_comments = (string)amNotes;

            statement.bs_BillingStatement_cr9e8_readyforinvoice = new[] { readyForInvoice };
        }

        private void ParseCustomerSiteAttributeValues(bs_BillingStatement statement)
        {
            statement.bs_BillingStatement_CustomerSite = new bs_CustomerSite()
            {
                bs_CustomerSiteId = (Guid) statement
                    .GetAttributeValue<AliasedValue>(CustomerSiteEntityAlias + "." +
                                                     bs_CustomerSite.Fields.bs_CustomerSiteId).Value,
                bs_SiteNumber = (string) statement
                    .GetAttributeValue<AliasedValue>(CustomerSiteEntityAlias + "." +
                                                     bs_CustomerSite.Fields.bs_SiteNumber).Value,
                bs_SiteName = (string) statement
                    .GetAttributeValue<AliasedValue>(CustomerSiteEntityAlias + "." +
                                                     bs_CustomerSite.Fields.bs_SiteName).Value,
            };
        }

        private void ParseInvoiceAttributeValues(bs_BillingStatement statement, IGrouping<Guid, Entity> group)
        {
            statement.bs_Invoice_BillingStatement = group.Select(invoiceEntity =>
            {
                var aliasedInvoiceId = invoiceEntity.GetAttributeValue<AliasedValue>(InvoiceEntityAlias + "." + bs_Invoice.Fields.bs_InvoiceId);
                if (aliasedInvoiceId == null)
                {
                    // There is no linked invoice for this statement
                    return new bs_Invoice();
                }

                return new bs_Invoice
                {
                    bs_InvoiceId = (Guid)aliasedInvoiceId.Value,
                    bs_InvoiceNumber = (string)invoiceEntity.GetAttributeValue<AliasedValue>(InvoiceEntityAlias + "." + bs_Invoice.Fields.bs_InvoiceNumber)?.Value,
                    bs_Amount = (decimal?)invoiceEntity.GetAttributeValue<AliasedValue>(InvoiceEntityAlias + "." + bs_Invoice.Fields.bs_Amount)?.Value ?? 0
                };
            }
            );
        }

        private IEnumerable<bs_BillingStatement> GetBillingStatements(Action<QueryExpression> addLinks, Action<QueryExpression> addConditions, Action<bs_BillingStatement, IGrouping<Guid, Entity>> parseAttributeValues)
        {
            var serviceClient = _dataverseService.GetServiceClient();

            var query = new QueryExpression(bs_BillingStatement.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(
                    bs_BillingStatement.Fields.bs_BillingStatementId,
                    bs_BillingStatement.Fields.CreatedOn,
                    bs_BillingStatement.Fields.bs_ServicePeriodStart,
                    bs_BillingStatement.Fields.bs_ServicePeriodEnd,
                    bs_BillingStatement.Fields.bs_StatementStatus,
                    bs_BillingStatement.Fields.bs_PurchaseOrder,
                    bs_BillingStatement.Fields.bs_ForecastData
                ),
                PageInfo = new PagingInfo
                {
                    PageNumber = 1,
                    Count = 5000    // Maximum number of records per page (Dataverse limit is 5000)
                }
            };

            addLinks(query);
            addConditions(query);

            var allResults = new List<Entity>();
            EntityCollection result;

            do
            {
                result = serviceClient.RetrieveMultiple(query);

                allResults.AddRange(result.Entities);

                if (result.MoreRecords)
                {
                    query.PageInfo.PageNumber++;

                    query.PageInfo.PagingCookie = result.PagingCookie;
                }
            }
            while (result.MoreRecords);

            var filteredResults = FilterUniqueAndMatch(new EntityCollection(allResults));

            return filteredResults
                .GroupBy(entity => entity.Id)
                .Select(group =>
                {
                    var statement = group.First().ToEntity<bs_BillingStatement>();
                    parseAttributeValues(statement, group);
                    return statement;
                });
        }

        public List<Entity> FilterUniqueAndMatch(EntityCollection entityCollection)
        {
            var billingStatementMap = new Dictionary<Guid, List<Entity>>(); // To track all entities per billing statement
            var uniqueInvoiceNumbers = new HashSet<string>(); // To ensure unique invoices
            var finalInvoiceEntities = new List<Entity>(); // To compile the final list of unique invoices

            foreach (var entity in entityCollection.Entities)
            {
                // Use null conditional and coalescing operators to safely get values or default to empty
                var bsInvoiceNumber = entity
                    .GetAttributeValue<AliasedValue>(InvoiceEntityAlias + "." + bs_Invoice.Fields.bs_InvoiceNumber)?.Value as string ?? string.Empty;
                var cr9e8Period = entity
                    .GetAttributeValue<AliasedValue>(ReadyForInvoiceEntityAlias + "." + cr9e8_readyforinvoice.Fields.cr9e8_period)?.Value as string ?? string.Empty;

                var billingStatementId = entity.Id;

                // Create a unique key for uniqueness combining billing statement and invoice number
                var uniqueKey = $"{billingStatementId}_{bsInvoiceNumber}";

                // Ensure a list exists for each billing statement
                if (!billingStatementMap.ContainsKey(billingStatementId))
                {
                    billingStatementMap[billingStatementId] = new List<Entity>();
                }

                // Add the full entity to the billing statement list
                billingStatementMap[billingStatementId].Add(entity);

                // Process matches first: attempt to add unique invoices based on matches
                if (bsInvoiceNumber.Contains(cr9e8Period) && uniqueInvoiceNumbers.Add(uniqueKey))
                {
                    finalInvoiceEntities.Add(entity); // Add the matching invoice
                }
            }

            // Ensure every statement has elements from the billingStatementMap
            foreach (var statementId in billingStatementMap.Keys)
            {
                var invoices = billingStatementMap[statementId];

                // Add non-matching but unique invoices if no matches were added
                if (!invoices.Any(invoice =>
                    finalInvoiceEntities.Contains(invoice)))
                {
                    foreach (var invoice in invoices)
                    {
                        // Safely retrieve bsInvoiceNumber again
                        var bsInvoiceNumber = invoice
                            .GetAttributeValue<AliasedValue>(InvoiceEntityAlias + "." + bs_Invoice.Fields.bs_InvoiceNumber)?.Value as string ?? string.Empty;
                        var uniqueKey = $"{statementId}_{bsInvoiceNumber}";

                        if (uniqueInvoiceNumbers.Add(uniqueKey))
                        {
                            // Safely set cr9e8_comments to null for non-matching invoices
                            var cr9eCommentsAlias = ReadyForInvoiceEntityAlias + "." + cr9e8_readyforinvoice.Fields.cr9e8_comments;
                            if (invoice.Attributes.ContainsKey(cr9eCommentsAlias))
                            {
                                invoice.Attributes[cr9eCommentsAlias] = null;
                            }

                            finalInvoiceEntities.Add(invoice);
                        }
                    }
                }
            }

            return finalInvoiceEntities;
        }

        public void UpdateStatementStatus(Guid billingStatementId, bs_BillingStatement status)
        {
            // Get the service client
            var serviceClient = _dataverseService.GetServiceClient();

            // Create an instance of the entity you want to update
            Entity billingStatement = new Entity("bs_billingstatement");

            // Set the ID of the entity record you want to update
            billingStatement.Id = billingStatementId;

            // Retrieve the integer value of the choice from the entity
            if (status.bs_StatementStatus.HasValue)
            {
                billingStatement["bs_statementstatus"] = new OptionSetValue((int)status.bs_StatementStatus.Value);
            }
            else
            {
                throw new ArgumentException("The status value must not be null.");
            }

            // Update the record
            serviceClient.Update(billingStatement);
        }

        public bs_BillingStatement GetBillingStatementById(Guid billingStatementId)
        {
            var serviceClient = _dataverseService.GetServiceClient();
            var query = new QueryExpression(bs_BillingStatement.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(bs_BillingStatement.Fields.bs_BillingStatementId,
                    bs_BillingStatement.Fields.CreatedOn,
                    bs_BillingStatement.Fields.bs_ServicePeriodStart,
                    bs_BillingStatement.Fields.bs_ServicePeriodEnd,
                    bs_BillingStatement.Fields.bs_StatementStatus,
                    bs_BillingStatement.Fields.bs_ForecastData,
                    bs_BillingStatement.Fields.bs_PurchaseOrder)
            };

            AddCustomerSiteLinkToQueryForPdf(query);
            AddInvoiceLinkToQueryForPdf(query);

            query.Criteria.AddCondition(bs_BillingStatement.Fields.bs_BillingStatementId, ConditionOperator.Equal, billingStatementId);

            var result = serviceClient.RetrieveMultiple(query);

            return result.Entities
                .GroupBy(entity => entity.Id)
                .Select(group =>
                {
                    var statement = group.First().ToEntity<bs_BillingStatement>();
                    ParseCustomerSiteAttributeValuesForPdf(statement);
                    ParseInvoiceAttributeValuesForSingleStatement(statement, group);
                    ParseContractAttributeValues(statement, group);
                    return statement;
                })
                .FirstOrDefault();
        }

        private void ParseCustomerSiteAttributeValuesForPdf(bs_BillingStatement statement)
        {
            if (statement == null) throw new ArgumentNullException(nameof(statement));

            var customerSiteId = statement.GetAttributeValue<AliasedValue>(CustomerSiteEntityAlias + "." + bs_CustomerSite.Fields.bs_CustomerSiteId)?.Value;
            var siteNumber = statement.GetAttributeValue<AliasedValue>(CustomerSiteEntityAlias + "." + bs_CustomerSite.Fields.bs_SiteNumber)?.Value;
            var siteName = statement.GetAttributeValue<AliasedValue>(CustomerSiteEntityAlias + "." + bs_CustomerSite.Fields.bs_SiteName)?.Value;
            var accountManager = statement.GetAttributeValue<AliasedValue>(CustomerSiteEntityAlias + "." + bs_CustomerSite.Fields.bs_AccountManager)?.Value;
            var accountManagerId = statement.GetAttributeValue<AliasedValue>(CustomerSiteEntityAlias + "." + bs_CustomerSite.Fields.bs_AccountManagerId)?.Value;
            var address = statement.GetAttributeValue<AliasedValue>(CustomerSiteEntityAlias + "." + bs_CustomerSite.Fields.bs_Address)?.Value;
            var billingContactEmail = statement.GetAttributeValue<AliasedValue>(CustomerSiteEntityAlias + "." + bs_CustomerSite.Fields.bs_BillingContactEmail)?.Value;
            var closeDate = statement.GetAttributeValue<AliasedValue>(CustomerSiteEntityAlias + "." + bs_CustomerSite.Fields.bs_CloseDate)?.Value;
            var district = statement.GetAttributeValue<AliasedValue>(CustomerSiteEntityAlias + "." + bs_CustomerSite.Fields.bs_District)?.Value;
            var glString = statement.GetAttributeValue<AliasedValue>(CustomerSiteEntityAlias + "." + bs_CustomerSite.Fields.bs_GLString)?.Value;
            var invoiceRecipient = statement.GetAttributeValue<AliasedValue>(CustomerSiteEntityAlias + "." + bs_CustomerSite.Fields.bs_InvoiceRecipient)?.Value;
            var startDate = statement.GetAttributeValue<AliasedValue>(CustomerSiteEntityAlias + "." + bs_CustomerSite.Fields.bs_StartDate)?.Value;

            statement.bs_BillingStatement_CustomerSite = new bs_CustomerSite
            {
                bs_CustomerSiteId = customerSiteId != null ? (Guid)customerSiteId : Guid.Empty,
                bs_SiteNumber = siteNumber as string,
                bs_SiteName = siteName as string,
                bs_AccountManager = accountManager as string,
                bs_AccountManagerId = accountManagerId as string,
                bs_Address = address as string,
                bs_BillingContactEmail = billingContactEmail as string,
                bs_CloseDate = closeDate != null ? (DateTime)closeDate : DateTime.MinValue,
                bs_District = district as string,
                bs_GLString = glString as string,
                bs_InvoiceRecipient = invoiceRecipient as string,
                bs_StartDate = startDate != null ? (DateTime)startDate : DateTime.MinValue
            };
        }

        private void ParseContractAttributeValues(bs_BillingStatement statement, IGrouping<Guid, Entity> group)
        {
            var purchaseOrder = group.First()
                .GetAttributeValue<AliasedValue>(ContractEntityAlias + "." + bs_Contract.Fields.bs_PurchaseOrder)?.Value as string;

            if (purchaseOrder != null)
            {
                List<bs_Contract> contracts = new List<bs_Contract>();
                contracts.Add(new bs_Contract
                {
                    bs_PurchaseOrder = purchaseOrder
                });
                statement.bs_BillingStatement_CustomerSite.bs_Contract_CustomerSite = contracts;
            }
        }

        private void ParseInvoiceAttributeValuesForSingleStatement(bs_BillingStatement statement, IGrouping<Guid, Entity> group)
        {
            if (statement == null) throw new ArgumentNullException(nameof(statement));

            statement.bs_Invoice_BillingStatement = group.Select(invoiceEntity =>
            {
                var invoiceId = invoiceEntity.GetAttributeValue<AliasedValue>(InvoiceEntityAlias + "." + bs_Invoice.Fields.bs_InvoiceId)?.Value;
                var invoiceNumber = invoiceEntity.GetAttributeValue<AliasedValue>(InvoiceEntityAlias + "." + bs_Invoice.Fields.bs_InvoiceNumber)?.Value;
                var amount = invoiceEntity.GetAttributeValue<AliasedValue>(InvoiceEntityAlias + "." + bs_Invoice.Fields.bs_Amount)?.Value;
                var description = invoiceEntity.GetAttributeValue<AliasedValue>(InvoiceEntityAlias + "." + bs_Invoice.Fields.bs_Description)?.Value;
                var invoiceDate = invoiceEntity.GetAttributeValue<AliasedValue>(InvoiceEntityAlias + "." + bs_Invoice.Fields.bs_InvoiceDate)?.Value;
                var paymentTerms = invoiceEntity.GetAttributeValue<AliasedValue>(InvoiceEntityAlias + "." + bs_Invoice.Fields.bs_PaymentTerms)?.Value;
                var title = invoiceEntity.GetAttributeValue<AliasedValue>(InvoiceEntityAlias + "." + bs_Invoice.Fields.bs_Title)?.Value;
                var invoiceData = invoiceEntity.GetAttributeValue<AliasedValue>(InvoiceEntityAlias + "." + bs_Invoice.Fields.bs_InvoiceData)?.Value;


                return new bs_Invoice
                {
                    bs_InvoiceId = invoiceId != null ? (Guid)invoiceId : Guid.Empty,
                    bs_InvoiceNumber = invoiceNumber as string,
                    bs_Amount = amount != null ? (decimal)amount : 0,
                    bs_Description = description as string,
                    bs_InvoiceDate = invoiceDate != null ? (DateTime)invoiceDate : DateTime.MinValue,
                    bs_PaymentTerms = paymentTerms as string,
                    bs_Title = title as string,
                    bs_InvoiceData = invoiceData as string
                };
            }
            );
        }

        private void AddInvoiceLinkToQueryForPdf(QueryExpression query)
        {
            var invoiceLink = query.AddLink(
                linkFromAttributeName: bs_BillingStatement.Fields.bs_BillingStatementId,
                linkToEntityName: bs_Invoice.EntityLogicalName,
                linkToAttributeName: bs_Invoice.Fields.bs_BillingStatementFK,
                joinOperator: JoinOperator.LeftOuter
            );
            invoiceLink.Columns = new ColumnSet(
                bs_Invoice.Fields.bs_InvoiceId,
                bs_Invoice.Fields.bs_InvoiceNumber,
                bs_Invoice.Fields.bs_Amount,
                bs_Invoice.Fields.bs_Description,
                bs_Invoice.Fields.bs_InvoiceDate,
                bs_Invoice.Fields.bs_PaymentTerms,
                bs_Invoice.Fields.bs_Title,
                bs_Invoice.Fields.bs_InvoiceData
            );
            invoiceLink.EntityAlias = InvoiceEntityAlias;
        }

        private void AddCustomerSiteLinkToQueryForPdf(QueryExpression query)
        {
            var customerSiteLink = query.AddLink(
                linkFromAttributeName: bs_BillingStatement.Fields.bs_CustomerSiteFK,
                linkToEntityName: bs_CustomerSite.EntityLogicalName,
                linkToAttributeName: bs_CustomerSite.Fields.bs_CustomerSiteId,
                joinOperator: JoinOperator.LeftOuter
            );
            customerSiteLink.Columns = new ColumnSet(
                bs_CustomerSite.Fields.bs_CustomerSiteId,
                bs_CustomerSite.Fields.bs_SiteNumber,
                bs_CustomerSite.Fields.bs_SiteName,
                bs_CustomerSite.Fields.bs_AccountManager,
                bs_CustomerSite.Fields.bs_AccountManagerId,
                bs_CustomerSite.Fields.bs_Address,
                bs_CustomerSite.Fields.bs_BillingContactEmail,
                bs_CustomerSite.Fields.bs_CloseDate,
                bs_CustomerSite.Fields.bs_District,
                bs_CustomerSite.Fields.bs_GLString,
                bs_CustomerSite.Fields.bs_InvoiceRecipient,
                bs_CustomerSite.Fields.bs_StartDate
            );
            customerSiteLink.EntityAlias = CustomerSiteEntityAlias;

            var contractLink = customerSiteLink.AddLink(
                linkFromAttributeName: bs_CustomerSite.Fields.bs_CustomerSiteId, 
                linkToEntityName: bs_Contract.EntityLogicalName, 
                linkToAttributeName: bs_Contract.Fields.bs_CustomerSiteFK, 
                joinOperator: JoinOperator.LeftOuter
            );
            contractLink.Columns = new ColumnSet(bs_Contract.Fields.bs_PurchaseOrder);
            contractLink.EntityAlias = ContractEntityAlias;
        }

        public void UpdateForecastData(Guid billingStatementId, string forecastData)
        {
            var serviceClient = _dataverseService.GetServiceClient();
            Entity billingStatement = new Entity("bs_billingstatement");

            billingStatement.Id = billingStatementId;
            billingStatement["bs_forecastdata"] = forecastData;
            serviceClient.Update(billingStatement);
        }

        public IEnumerable<bs_BillingStatement> GetBillingStatementsByIds(IEnumerable<Guid> billingStatementIds)
        {
            var serviceClient = _dataverseService.GetServiceClient();

            if (billingStatementIds == null || !billingStatementIds.Any())
                return new List<bs_BillingStatement>();

            var query = new QueryExpression(bs_BillingStatement.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(
                    bs_BillingStatement.Fields.bs_BillingStatementId,
                    bs_BillingStatement.Fields.CreatedOn,
                    bs_BillingStatement.Fields.bs_ServicePeriodStart,
                    bs_BillingStatement.Fields.bs_ServicePeriodEnd,
                    bs_BillingStatement.Fields.bs_StatementStatus,
                    bs_BillingStatement.Fields.bs_ForecastData
                ),
                PageInfo = new PagingInfo
                {
                    PageNumber = 1,
                    Count = 5000 // Dataverse max page size
                }
            };

            AddCustomerSiteLinkToQuery(query);
            AddInvoiceLinkToQuery(query);
            AddReadyForInvoiceLinkToQuery(query);

            query.Criteria.AddCondition(bs_BillingStatement.Fields.bs_BillingStatementId, ConditionOperator.In, billingStatementIds.Cast<object>().ToArray());

            var allResults = new List<Entity>();
            EntityCollection result;

            do
            {
                result = serviceClient.RetrieveMultiple(query);
                allResults.AddRange(result.Entities);

                if (result.MoreRecords)
                {
                    query.PageInfo.PageNumber++;
                    query.PageInfo.PagingCookie = result.PagingCookie;
                }
            }
            while (result.MoreRecords);

            var filteredResults = FilterUniqueAndMatch(new EntityCollection(allResults));

            return filteredResults
                .GroupBy(entity => entity.Id)
                .Select(group =>
                {
                    var statement = group.First().ToEntity<bs_BillingStatement>();
                    ParseCustomerSiteAttributeValues(statement);
                    ParseInvoiceAttributeValues(statement, group);
                    ParseReadyForInvoiceAttributeValues(statement);
                    return statement;
                });
        }

        public IEnumerable<Guid> GetBillingStatementIdsByCustomerSite(IEnumerable<Guid> customerSiteIds)
        {
            var serviceClient = _dataverseService.GetServiceClient();
            var query = new QueryExpression(bs_BillingStatement.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(bs_BillingStatement.Fields.bs_BillingStatementId),
                PageInfo = new PagingInfo
                {
                    PageNumber = 1,
                    Count = 5000 // Maximum allowed per page
                }
            };
            query.Criteria.AddCondition(bs_BillingStatement.Fields.bs_CustomerSiteFK, ConditionOperator.In, customerSiteIds.Cast<object>().ToArray());
            AddIsCurrentConditionToQuery(query);

            var moreRecords = true;
            var billingStatementIds = new List<Guid>();

            while (moreRecords)
            {
                var result = serviceClient.RetrieveMultiple(query);
                billingStatementIds.AddRange(result.Entities.Select(entity => entity.Id));

                if (result.MoreRecords)
                {
                    query.PageInfo.PageNumber++;
                    query.PageInfo.PagingCookie = result.PagingCookie;
                }
                else
                {
                    moreRecords = false;
                }
            }

            return billingStatementIds;
        }
    }
}
