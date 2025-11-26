using api.Services;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using TownePark;

namespace api.Data.Impl
{
    public class ContractRepository : IContractRepository
    {
        private readonly IDataverseService _dataverseService;
        private const string CustomerSiteEntityAlias = "customer_site";

        public ContractRepository(IDataverseService dataverseService)
        {
            _dataverseService = dataverseService;
        }

        public IEnumerable<Guid> GetContractIdsByCustomerSite(IEnumerable<Guid> customerSiteIds)
        {
            var serviceClient = _dataverseService.GetServiceClient();
            var query = new QueryExpression(bs_Contract.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(bs_Contract.Fields.bs_ContractId)
            };

            query.Criteria.AddCondition(bs_Contract.Fields.bs_CustomerSiteFK, ConditionOperator.In,
                customerSiteIds.Cast<object>().ToArray());

            var result = serviceClient.RetrieveMultiple(query);
            return result.Entities.Select(entity => entity.Id);
        }

        public bs_Contract GetContractByCustomerSite(Guid customerSiteId)
        {
            var target = new EntityReference(bs_Contract.EntityLogicalName, bs_Contract.Fields.bs_CustomerSiteFK, customerSiteId);
            return GetContractByTarget(target);
        }

        public string GetContractTypeStringByCustomerSite(Guid customerSiteId)
        {
            var serviceClient = _dataverseService.GetServiceClient();

            var query = new QueryExpression(bs_Contract.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(bs_Contract.Fields.bs_ContractTypeString),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression(bs_Contract.Fields.bs_CustomerSiteFK, ConditionOperator.Equal, customerSiteId)
                    }
                }
            };

            var result = serviceClient.RetrieveMultiple(query);
            var contract = result.Entities.FirstOrDefault()?.ToEntity<bs_Contract>();
            if (contract == null)
            {
                return string.Empty; // or throw an exception if preferred
            }
            // Return the contract type string or an empty string if not found
            if (string.IsNullOrEmpty(contract.bs_ContractTypeString))
            {
                return string.Empty; // or handle as needed
            }

            return contract?.bs_ContractTypeString ?? string.Empty;
        }

        public void UpdateContractDetail(Guid contractId, bs_Contract updates)
        {
            var serviceClient = _dataverseService.GetServiceClient();
            
            updates.Id = contractId;
            serviceClient.Update(updates); 
        }

        public void UpdateDeviationThreshold(IEnumerable<bs_Contract> deviationUpdate)
        {
            var serviceClient = _dataverseService.GetServiceClient();
            var requestWithResults = new ExecuteMultipleRequest()
            {
                Settings = new ExecuteMultipleSettings()
                {
                    ContinueOnError = false,
                    ReturnResponses = true
                },
                Requests = new OrganizationRequestCollection()
            };

            // TODO how do I update many?
            foreach (var deviation in deviationUpdate)
            {
                var updateRequest = new UpdateRequest { Target = deviation };
                requestWithResults.Requests.Add(updateRequest);
            }

            var organizationResponse = serviceClient.Execute(requestWithResults);
            organizationResponse.Results.TryGetValue("IsFaulted", out bool isFaulted);
            if (isFaulted) throw new Exception("Error updating deviation threshold.");
        }

        public IEnumerable<bs_Contract> GetDeviations()
        {
            var serviceClient = _dataverseService.GetServiceClient();
            var query = new QueryExpression(bs_Contract.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(
                    bs_Contract.Fields.bs_ContractId, 
                    bs_Contract.Fields.bs_DeviationAmount, 
                    bs_Contract.Fields.bs_DeviationPercentage)
            };

            var customerSiteLink = query.AddLink(
                linkFromAttributeName: bs_Contract.Fields.bs_CustomerSiteFK,
                linkToEntityName: bs_CustomerSite.EntityLogicalName,
                linkToAttributeName: bs_CustomerSite.Fields.bs_CustomerSiteId,
                joinOperator: JoinOperator.Inner
                );
            customerSiteLink.Columns = new ColumnSet(  
                bs_CustomerSite.Fields.bs_CustomerSiteId,
                bs_CustomerSite.Fields.bs_SiteName,
                bs_CustomerSite.Fields.bs_SiteNumber
            );
            customerSiteLink.EntityAlias = CustomerSiteEntityAlias;

            var result = serviceClient.RetrieveMultiple(query);
            return result.Entities.Select(entity => 
            {
                var contract = entity.ToEntity<bs_Contract>();
                ParseCustomerSiteAttributeValues(contract);
                return contract;
            });
        }

        private void ParseCustomerSiteAttributeValues(bs_Contract contract)
        {
            contract.bs_Contract_CustomerSite = new bs_CustomerSite()
            {
                bs_CustomerSiteId = (Guid)contract
                    .GetAttributeValue<AliasedValue>(CustomerSiteEntityAlias + "." +
                                                     bs_CustomerSite.Fields.bs_CustomerSiteId).Value,
                bs_SiteNumber = (string)contract
                    .GetAttributeValue<AliasedValue>(CustomerSiteEntityAlias + "." +
                                                     bs_CustomerSite.Fields.bs_SiteNumber).Value,
                bs_SiteName = (string)contract
                    .GetAttributeValue<AliasedValue>(CustomerSiteEntityAlias + "." +
                                                     bs_CustomerSite.Fields.bs_SiteName).Value,
            };
        }

        public void UpdateContractRelatedEntities(UpdateContractDao changes)
        {
            var serviceClient = _dataverseService.GetServiceClient();

            var requestWithResults = new ExecuteMultipleRequest()
            {
                Settings = new ExecuteMultipleSettings()
                {
                    ContinueOnError = false,
                    ReturnResponses = true
                },
                Requests = new OrganizationRequestCollection()
            };

            foreach (var bellService in changes.BellServicesToCreate)
            {
                bellService.bs_ContractFK = new EntityReference(bs_Contract.EntityLogicalName, changes.ContractId);
                var createRequest = new CreateRequest { Target = bellService };
                requestWithResults.Requests.Add(createRequest);
            }

            foreach (var midMonth in changes.MidMonthsToCreate)
            {
                midMonth.bs_ContractFK = new EntityReference(bs_Contract.EntityLogicalName, changes.ContractId);
                var createRequest = new CreateRequest { Target = midMonth };
                requestWithResults.Requests.Add(createRequest);
            }

            foreach (var billableAccount in changes.BillableAccountsToCreate)
            {
                billableAccount.bs_ContractFK = new EntityReference(bs_Contract.EntityLogicalName, changes.ContractId);
                var createRequest = new CreateRequest { Target = billableAccount };
                requestWithResults.Requests.Add(createRequest);
            }

            foreach (var managementFee in changes.ManagementFeesToCreate)
            {
                managementFee.bs_ContractFK = new EntityReference(bs_Contract.EntityLogicalName, changes.ContractId);
                var createRequest = new CreateRequest { Target = managementFee };
                requestWithResults.Requests.Add(createRequest);
            }

            foreach (var depositedRevenue in changes.DepositedRevenuesToCreate)
            {
                depositedRevenue.bs_ContractFK = new EntityReference(bs_Contract.EntityLogicalName, changes.ContractId);
                var createRequest = new CreateRequest { Target = depositedRevenue };
                requestWithResults.Requests.Add(createRequest);
            }

            foreach (var service in changes.ServicesToCreate)
            {
                service.bs_ContractFK = new EntityReference(bs_Contract.EntityLogicalName, changes.ContractId);
                var createRequest = new CreateRequest { Target = service };
                requestWithResults.Requests.Add(createRequest);
            }
            
            // TODO similar code can be simplified.
            foreach (var job in changes.JobsToCreate)
            {
                job.bs_ContractFK = new EntityReference(bs_Contract.EntityLogicalName, changes.ContractId);
                var createRequest = new CreateRequest { Target = job };
                requestWithResults.Requests.Add(createRequest);
            }
            
            foreach (var invoiceGroup in changes.InvoiceGroupsToCreate)
            {
                invoiceGroup.bs_ContractFK = new EntityReference(bs_Contract.EntityLogicalName, changes.ContractId);
                var createRequest = new CreateRequest { Target = invoiceGroup };
                requestWithResults.Requests.Add(createRequest);
            }
            
            foreach (var invoiceGroup in changes.InvoiceGroupsToUpdate)
            {
                if (invoiceGroup.Id != Guid.Empty && invoiceGroup.bs_InvoiceGroupId.HasValue)
                {
                    if (invoiceGroup.bs_ContractFK == null)
                    {
                        invoiceGroup.bs_ContractFK = new EntityReference(bs_Contract.EntityLogicalName, changes.ContractId);
                    }
                    
                    var updateRequest = new UpdateRequest { Target = invoiceGroup };
                    requestWithResults.Requests.Add(updateRequest);
                }
            }
            
            foreach (var service in changes.ServicesToDelete)
            {
                var deleteRequest = new DeleteRequest() { Target = service.ToEntityReference() };
                requestWithResults.Requests.Add(deleteRequest);
            }

            foreach (var bellService in changes.BellServicesToDelete)
            {
                var deleteRequest = new DeleteRequest() { Target = bellService.ToEntityReference() };
                requestWithResults.Requests.Add(deleteRequest);
            }

            foreach (var midMonth in changes.MidMonthsToDelete)
            {
                var deleteRequest = new DeleteRequest() { Target = midMonth.ToEntityReference() };
                requestWithResults.Requests.Add(deleteRequest);
            }

            foreach (var billableAccount in changes.BillableAccountsToDelete)
            {
                var deleteRequest = new DeleteRequest() { Target = billableAccount.ToEntityReference() };
                requestWithResults.Requests.Add(deleteRequest);
            }

            foreach (var managementFee in changes.ManagementFeesToDelete)
            {
                var deleteRequest = new DeleteRequest() { Target = managementFee.ToEntityReference() };
                requestWithResults.Requests.Add(deleteRequest);
            }

            foreach (var depositedRevenue in changes.DepositedRevenuesToDelete)
            {
                var deleteRequest = new DeleteRequest() { Target = depositedRevenue.ToEntityReference() };
                requestWithResults.Requests.Add(deleteRequest);
            }

            foreach (var job in changes.JobsToDelete)
            {
                var deleteRequest = new DeleteRequest() { Target = job.ToEntityReference() };
                requestWithResults.Requests.Add(deleteRequest);
            }
            
            foreach (var invoiceGroup in changes.InvoiceGroupsToDelete)
            {
                var deleteRequest = new DeleteRequest() { Target = invoiceGroup.ToEntityReference() };
                requestWithResults.Requests.Add(deleteRequest);
            }

            foreach (var threshold in changes.ThresholdStructuresToCreate)
            {
                threshold.bs_Contract = new EntityReference(bs_Contract.EntityLogicalName, changes.ContractId);
                var createRequest = new CreateRequest { Target = threshold };
                requestWithResults.Requests.Add(createRequest);
            }

            foreach (var threshold in changes.ThresholdStructuresToDelete)
            {
                var deleteRequest = new DeleteRequest() { Target = threshold.ToEntityReference() };
                requestWithResults.Requests.Add(deleteRequest);
            }
            foreach (var nonGLExpense in changes.NonGLExpenseToCreate)
            {
                nonGLExpense.bs_ContractFK = new EntityReference(bs_Contract.EntityLogicalName, changes.ContractId);
                var createRequest = new CreateRequest { Target = nonGLExpense };
                requestWithResults.Requests.Add(createRequest);
            }

            foreach (var nonGLExpense in changes.NonGLExpenseToUpdate)
            {
                if (nonGLExpense.Id != Guid.Empty && nonGLExpense.bs_NonGLExpenseId.HasValue)
                {
                    if (nonGLExpense.bs_ContractFK == null)
                    {
                        nonGLExpense.bs_ContractFK = new EntityReference(bs_Contract.EntityLogicalName, changes.ContractId);
                    }

                    var updateRequest = new UpdateRequest { Target = nonGLExpense };
                    requestWithResults.Requests.Add(updateRequest);
                }
            }
            foreach (var nonGLExpense in changes.NonGLExpenseToDelete)
            {
                var deleteRequest = new DeleteRequest() { Target = nonGLExpense.ToEntityReference() };
                requestWithResults.Requests.Add(deleteRequest);
            }
            var organizationResponse = serviceClient.Execute(requestWithResults);
            organizationResponse.Results.TryGetValue("IsFaulted", out bool isFaulted);
            if (isFaulted) throw new Exception("Error updating contract related entities.");
        }

        public bs_Contract GetContract(Guid contractId)
        {
            var target = new EntityReference(bs_Contract.EntityLogicalName,
                bs_Contract.Fields.bs_ContractId, contractId);
            return GetContractByTarget(target);
        }

        private bs_Contract GetContractByTarget(EntityReference target, bool withBuildingBlocks = true,
            bool withCustomerSite = false)
        {
            var serviceClient = _dataverseService.GetServiceClient();
            
            var columnSet = new ColumnSet(
                bs_Contract.Fields.bs_ContractId,
                bs_Contract.Fields.bs_ContractType,
                bs_Contract.Fields.bs_PaymentTerms,
                bs_Contract.Fields.bs_PurchaseOrder,
                bs_Contract.Fields.bs_BillingType,
                bs_Contract.Fields.bs_IncrementAmount,
                bs_Contract.Fields.bs_IncrementMonth,
                bs_Contract.Fields.bs_ConsumerPriceIndex,
                bs_Contract.Fields.bs_Notes,
                bs_Contract.Fields.bs_HoursBackupReport,
                bs_Contract.Fields.bs_OccupiedRoomRate,
                bs_Contract.Fields.bs_OccupiedRoomCode,
                bs_Contract.Fields.bs_OccupiedRoomInvoiceGroup,
                bs_Contract.Fields.bs_DeviationAmount, 
                bs_Contract.Fields.bs_DeviationPercentage,
                bs_Contract.Fields.bs_Deposits,
                bs_Contract.Fields.bs_ContractTypeString,
                bs_Contract.Fields.bs_SupportingReports
            );

            // create relationship with bs_FixedFeeService and bs_ContractNote
            var relationshipQueryCollection = new RelationshipQueryCollection();

            if (withCustomerSite)
            {
                var customerSiteColumnSet = new ColumnSet(
                    bs_CustomerSite.Fields.bs_CustomerSiteId,
                    bs_CustomerSite.Fields.bs_SiteNumber);
                var relatedCustomerSite = new QueryExpression(bs_CustomerSite.EntityLogicalName)
                {
                    ColumnSet = customerSiteColumnSet
                };
                var customerSiteRelationship = new Relationship("bs_Contract_CustomerSite");
                relationshipQueryCollection.Add(customerSiteRelationship, relatedCustomerSite);
            }
            
            if (withBuildingBlocks)
            {
                var fixedFeeColumnSet = new ColumnSet(
                    bs_FixedFeeService.Fields.bs_FixedFeeServiceId,
                    bs_FixedFeeService.Fields.bs_Code,
                    bs_FixedFeeService.Fields.bs_Name,
                    bs_FixedFeeService.Fields.bs_DisplayName,
                    bs_FixedFeeService.Fields.bs_Fee,
                    bs_FixedFeeService.Fields.bs_InvoiceGroup,
                    bs_FixedFeeService.Fields.bs_StartDate,
                    bs_FixedFeeService.Fields.bs_EndDate
                );

                // Get the current date and time
                var currentDate = DateTime.UtcNow;

                var filter = new FilterExpression(LogicalOperator.And);
                filter.AddCondition(bs_FixedFeeService.Fields.bs_StartDate, ConditionOperator.LessEqual, currentDate);
                var endDateFilter = new FilterExpression(LogicalOperator.Or);
                endDateFilter.AddCondition(bs_FixedFeeService.Fields.bs_EndDate, ConditionOperator.GreaterEqual, currentDate);
                endDateFilter.AddCondition(bs_FixedFeeService.Fields.bs_EndDate, ConditionOperator.Null);

                // Add the nested filter to the main filter
                filter.AddFilter(endDateFilter);

                // Define the query expression with criteria
                var relatedFixedFees = new QueryExpression(bs_FixedFeeService.EntityLogicalName)
                {
                    ColumnSet = fixedFeeColumnSet,
                    Criteria = filter
                };

                var fixedFeeRelationship = new Relationship("bs_FixedFeeService_Contract");
                relationshipQueryCollection.Add(fixedFeeRelationship, relatedFixedFees);

                var laborHourColumnSet = new ColumnSet(
                    bs_LaborHourJob.Fields.bs_LaborHourJobId,
                    bs_LaborHourJob.Fields.bs_Name,
                    bs_LaborHourJob.Fields.bs_DisplayName,
                    bs_LaborHourJob.Fields.bs_Rate,
                    bs_LaborHourJob.Fields.bs_OvertimeRate,
                    bs_LaborHourJob.Fields.bs_Code,
                    bs_LaborHourJob.Fields.bs_JobCode,
                    bs_LaborHourJob.Fields.bs_InvoiceGroup
                );

                var jobsFilter = new FilterExpression(LogicalOperator.And);
                jobsFilter.AddCondition(bs_LaborHourJob.Fields.bs_StartDate, ConditionOperator.LessEqual, currentDate);
                var jobsEndDateFilter = new FilterExpression(LogicalOperator.Or);
                jobsEndDateFilter.AddCondition(bs_LaborHourJob.Fields.bs_EndDate, ConditionOperator.GreaterEqual, currentDate);
                jobsEndDateFilter.AddCondition(bs_LaborHourJob.Fields.bs_EndDate, ConditionOperator.Null);

                jobsFilter.AddFilter(jobsEndDateFilter);

                var relatedLaborHourJobs = new QueryExpression(bs_LaborHourJob.EntityLogicalName)
                {
                    ColumnSet = laborHourColumnSet,
                    Criteria = jobsFilter
                };
                var laborHourRelationship = new Relationship("bs_LaborHourJob_Contract");
                relationshipQueryCollection.Add(laborHourRelationship, relatedLaborHourJobs);

                var revenueShareColumnSet = new ColumnSet(
                    bs_RevenueShareThreshold.Fields.bs_RevenueShareThresholdId,
                    bs_RevenueShareThreshold.Fields.bs_Name,
                    bs_RevenueShareThreshold.Fields.bs_RevenueAccumulationType,
                    bs_RevenueShareThreshold.Fields.bs_RevenueCodeData,
                    bs_RevenueShareThreshold.Fields.bs_TierData,
                    bs_RevenueShareThreshold.Fields.bs_InvoiceGroup,
                    bs_RevenueShareThreshold.Fields.bs_ValidationThresholdAmount,
                    bs_RevenueShareThreshold.Fields.bs_ValidationThresholdType
                );
                var relatedRevenueShares = new QueryExpression(bs_RevenueShareThreshold.EntityLogicalName)
                {
                    ColumnSet = revenueShareColumnSet
                };
                var revenueShareRelationship = new Relationship("bs_RevenueShareThreshold_Contract");
                relationshipQueryCollection.Add(revenueShareRelationship, relatedRevenueShares);


                var bellServiceColumnSet = new ColumnSet(
                    bs_BellService.Fields.bs_BellServiceId,
                    bs_BellService.Fields.bs_InvoiceGroup);
                var relatedBellServices = new QueryExpression(bs_BellService.EntityLogicalName)
                {
                    ColumnSet = bellServiceColumnSet
                };
                var bellServiceRelationship = new Relationship("bs_BellService_bs_Contract");
                relationshipQueryCollection.Add(bellServiceRelationship, relatedBellServices);

                var midMonthColumnSet = new ColumnSet(
                    bs_MidMonthAdvance.Fields.bs_MidMonthAdvanceId,
                    bs_MidMonthAdvance.Fields.bs_Amount,
                    bs_MidMonthAdvance.Fields.bs_LineTitle,
                    bs_MidMonthAdvance.Fields.bs_InvoiceGroup);
                var relatedMidMonths = new QueryExpression(bs_MidMonthAdvance.EntityLogicalName)
                {
                    ColumnSet = midMonthColumnSet
                };
                var midMonthRelationship = new Relationship("bs_MidMonthAdvance_bs_Contract");
                relationshipQueryCollection.Add(midMonthRelationship, relatedMidMonths);

                var depositedRevenueColumnSet = new ColumnSet(
                    bs_DepositedRevenue.Fields.bs_DepositedRevenueId,
                    bs_DepositedRevenue.Fields.bs_InvoiceGroup,
                    bs_DepositedRevenue.Fields.bs_TowneParkResponsibleForParkingTax,
                    bs_DepositedRevenue.Fields.bs_DepositedRevenueEnabled);
                var relatedDepositedRevenues = new QueryExpression(bs_DepositedRevenue.EntityLogicalName)
                {
                    ColumnSet = depositedRevenueColumnSet
                };
                var depositedRevenueRelationship = new Relationship("bs_DepositedRevenue_Contract");
                relationshipQueryCollection.Add(depositedRevenueRelationship, relatedDepositedRevenues);

                var billableAccountColumnSet = new ColumnSet(
                    bs_BillableAccount.Fields.bs_BillableAccountId,
                    bs_BillableAccount.Fields.bs_PayrollAccountsData,
                    bs_BillableAccount.Fields.bs_PayrollAccountsInvoiceGroup,
                    bs_BillableAccount.Fields.bs_PayrollAccountsLineTitle,
                    bs_BillableAccount.Fields.bs_PayrollTaxesBillingType,
                    bs_BillableAccount.Fields.bs_PayrollTaxesEnabled,
                    bs_BillableAccount.Fields.bs_PayrollTaxesLineTitle,
                    bs_BillableAccount.Fields.bs_PayrollTaxesPercentage,
                    bs_BillableAccount.Fields.bs_PayrollSupportAmount,
                    bs_BillableAccount.Fields.bs_PayrollSupportBillingType,
                    bs_BillableAccount.Fields.bs_PayrollSupportEnabled,
                    bs_BillableAccount.Fields.bs_PayrollSupportLineTitle,
                    bs_BillableAccount.Fields.bs_PayrollSupportPayrollType,
                    bs_BillableAccount.Fields.bs_ExpenseAccountsData,
                    bs_BillableAccount.Fields.bs_ExpenseAccountsInvoiceGroup,
                    bs_BillableAccount.Fields.bs_ExpenseAccountsLineTitle,
                    bs_BillableAccount.Fields.bs_PayrollTaxesEscalatorEnable,
                    bs_BillableAccount.Fields.bs_PayrollTaxesEscalatorvalue,
                    bs_BillableAccount.Fields.bs_PayrollTaxesEscalatorMonth,
                    bs_BillableAccount.Fields.bs_PayrollTaxesEscalatorType,

                    bs_BillableAccount.Fields.bs_AdditionalPayrollAmount);
                var relatedBillableAccounts = new QueryExpression(bs_BillableAccount.EntityLogicalName)
                {
                    ColumnSet = billableAccountColumnSet
                };
                var billableAccountRelationship = new Relationship("bs_BillableAccount_Contract");
                relationshipQueryCollection.Add(billableAccountRelationship, relatedBillableAccounts);

                var managementAgreementColumnSet = new ColumnSet(
                    bs_ManagementAgreement.Fields.bs_ManagementAgreementId,
                    bs_ManagementAgreement.Fields.bs_ManagementAgreementType,
                    bs_ManagementAgreement.Fields.bs_ManagementFeeEscalatorEnabled,
                    bs_ManagementAgreement.Fields.bs_ManagementFeeEscalatorMonth,
                    bs_ManagementAgreement.Fields.bs_ManagementFeeEscalatorType,
                    bs_ManagementAgreement.Fields.bs_ManagementFeeEscalatorValue,
                    bs_ManagementAgreement.Fields.bs_PerLaborHourJobCode,
                    bs_ManagementAgreement.Fields.bs_PerLaborHourJobCodeData,
                    bs_ManagementAgreement.Fields.bs_PerLaborHourOvertimeRate,
                    bs_ManagementAgreement.Fields.bs_PerLaborHourRate,
                    bs_ManagementAgreement.Fields.bs_RevenuePercentageAmount,
                    bs_ManagementAgreement.Fields.bs_InvoiceGroup,
                    bs_ManagementAgreement.Fields.bs_FixedFeeAmount,
                    bs_ManagementAgreement.Fields.bs_InsuranceAdditionalPercentage,
                    bs_ManagementAgreement.Fields.bs_InsuranceEnabled,
                    bs_ManagementAgreement.Fields.bs_InsuranceFixedFeeAmount,
                    bs_ManagementAgreement.Fields.bs_InsuranceLineTitle,
                    bs_ManagementAgreement.Fields.bs_InsuranceType,
                    bs_ManagementAgreement.Fields.bs_ClaimsCapAmount,
                    bs_ManagementAgreement.Fields.bs_ClaimsEnabled,
                    bs_ManagementAgreement.Fields.bs_ClaimsLineTitle,
                    bs_ManagementAgreement.Fields.bs_ClaimsType,
                    bs_ManagementAgreement.Fields.bs_ProfitShareAccumulationType,
                    bs_ManagementAgreement.Fields.bs_ProfitShareEnabled,
                    bs_ManagementAgreement.Fields.bs_ProfitShareEscalatorEnabled,
                    bs_ManagementAgreement.Fields.bs_ProfitShareEscalatorMonth,
                    bs_ManagementAgreement.Fields.bs_ProfitShareEscalatorType,
                    bs_ManagementAgreement.Fields.bs_ProfitShareTierData,
                    bs_ManagementAgreement.Fields.bs_ValidationThresholdAmount,
                    bs_ManagementAgreement.Fields.bs_ValidationThresholdEnabled,
                    bs_ManagementAgreement.Fields.bs_ValidationThresholdType,
                    bs_ManagementAgreement.Fields.bs_NonGLBillableExpensesEnabled);
                    
                var relatedManagementAgreements = new QueryExpression(bs_ManagementAgreement.EntityLogicalName)
                {
                    ColumnSet = managementAgreementColumnSet
                };
                var managementAgreementRelationship = new Relationship("bs_ManagementAgreement_Contract");
                relationshipQueryCollection.Add(managementAgreementRelationship, relatedManagementAgreements);
            }
            
            var invoiceGroupColumnSet = new ColumnSet(
                bs_InvoiceGroup.Fields.bs_InvoiceGroupId,
                bs_InvoiceGroup.Fields.bs_GroupNumber,
                bs_InvoiceGroup.Fields.bs_Title,
                bs_InvoiceGroup.Fields.bs_Description,
                bs_InvoiceGroup.Fields.bs_BillingContactEmails,
                bs_InvoiceGroup.Fields.bs_CustomerName,
                bs_InvoiceGroup.Fields.bs_SiteNumber,
                bs_InvoiceGroup.Fields.bs_VendorId

            );
            var relatedInvoiceGroups = new QueryExpression(bs_InvoiceGroup.EntityLogicalName)
            {
                ColumnSet = invoiceGroupColumnSet
            };
            // add here
            var invoiceGroupRelationship = new Relationship("bs_InvoiceGroup_Contract");
            relationshipQueryCollection.Add(invoiceGroupRelationship, relatedInvoiceGroups);
            var nonGlExpenseColumnSet = new ColumnSet(
               bs_NonGLExpense.Fields.bs_NonGLExpenseId,
               bs_NonGLExpense.Fields.bs_NonGLExpenseType,
               bs_NonGLExpense.Fields.bs_ExpenseAmount,
               bs_NonGLExpense.Fields.bs_ExpensePayrollType,
               bs_NonGLExpense.Fields.bs_ExpenseTitle,
               bs_NonGLExpense.Fields.bs_FinalPeriodBilled,
               bs_NonGLExpense.Fields.bs_SequenceNumber
           );

            var relatedNonGlExpenses = new QueryExpression(bs_NonGLExpense.EntityLogicalName)
            {
                ColumnSet = nonGlExpenseColumnSet
            };

            var nonGlExpenseRelationship = new Relationship("bs_nonglexpense_ContractFK_bs_contract");
            relationshipQueryCollection.Add(nonGlExpenseRelationship, relatedNonGlExpenses);

            var request = new RetrieveRequest()
            {
                ColumnSet = columnSet,
                RelatedEntitiesQuery = relationshipQueryCollection,
                Target = target
            };
            var response = (RetrieveResponse)serviceClient.Execute(request);
            return response.Entity.ToEntity<bs_Contract>();
        }

        public Guid AddContract(bs_Contract contract)
        {
            var serviceClient = _dataverseService.GetServiceClient();
            return serviceClient.Create(contract); 
        }
    }
}
