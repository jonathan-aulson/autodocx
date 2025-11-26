using System.ServiceModel;
using api.Data;
using api.Data.Impl;
using api.Services;
using Azure.Core;
using FluentAssertions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using NSubstitute;
using TownePark;
using Xunit;

namespace BackendTests.Data
{
    public class ContractRepositoryTest
    {
        private readonly IOrganizationService _organizationService;
        private readonly ContractRepository _contractRepository;

        public ContractRepositoryTest()
        {
            var dataverseService = Substitute.For<IDataverseService>();

            _organizationService = Substitute.For<IOrganizationService>();
            dataverseService.GetServiceClient().Returns(_organizationService);

            _contractRepository = new ContractRepository(dataverseService);
        }

        [Fact]
        public void GetContractDetail_ShouldReturnContractDetail()
        {
            var customerSiteId = Guid.NewGuid();
            var entity = new bs_Contract
            {
                bs_ContractId = Guid.NewGuid(),
                bs_ContractType = new[] { bs_contracttypechoices.FixedFee, bs_contracttypechoices.PerOccupiedRoom, bs_contracttypechoices.RevenueShare },
                bs_PaymentTerms = "Net 30",
                bs_PurchaseOrder = "PO12345",
                bs_BillingType = bs_billingtypechoices.Advanced,
                bs_IncrementAmount = 150.0m,
                bs_IncrementMonth = 1,
                bs_ConsumerPriceIndex = true,
                bs_Notes = "Example Note",
                bs_OccupiedRoomRate = 10.0m,
                bs_HoursBackupReport = true,
                bs_SupportingReports = new[] { bs_supportingreporttypes.MixOfSales }
            };

            var retrieveResponse = new RetrieveResponse
            {
                Results = new ParameterCollection
                {
                    { "Entity", entity }
                }
            };

            var retrieveRequestCapture = Arg.Do<RetrieveRequest>(req =>
            {
                req.ColumnSet.Should().BeEquivalentTo(new ColumnSet(
                    bs_Contract.Fields.bs_ContractId,
                    bs_Contract.Fields.bs_ContractType,
                    bs_Contract.Fields.bs_PaymentTerms,
                    bs_Contract.Fields.bs_PurchaseOrder,
                    bs_Contract.Fields.bs_BillingType,
                    bs_Contract.Fields.bs_IncrementAmount,
                    bs_Contract.Fields.bs_IncrementMonth,
                    bs_Contract.Fields.bs_ConsumerPriceIndex,
                    bs_Contract.Fields.bs_Notes,
                    bs_Contract.Fields.bs_OccupiedRoomRate,
                    bs_Contract.Fields.bs_OccupiedRoomCode,
                    bs_Contract.Fields.bs_OccupiedRoomInvoiceGroup,
                    bs_Contract.Fields.bs_HoursBackupReport,
                    bs_Contract.Fields.bs_DeviationAmount, 
                    bs_Contract.Fields.bs_DeviationPercentage,
                    bs_Contract.Fields.bs_Deposits,
                    bs_Contract.Fields.bs_ContractTypeString,
                    bs_Contract.Fields.bs_SupportingReports
                ));
                req.Target.LogicalName.Should().Be(bs_Contract.EntityLogicalName);
                var keyAttributeCollection = new KeyAttributeCollection
                    { { bs_Contract.Fields.bs_CustomerSiteFK, customerSiteId } };
                req.Target.KeyAttributes.Should().BeEquivalentTo(keyAttributeCollection);
                req.RelatedEntitiesQuery.Should().ContainKey(new Relationship("bs_FixedFeeService_Contract"));
                var relatedTasksQuery =
                    req.RelatedEntitiesQuery[new Relationship("bs_FixedFeeService_Contract")] as QueryExpression;
                relatedTasksQuery.Should().NotBeNull();
                relatedTasksQuery!.ColumnSet.Should().BeEquivalentTo(new ColumnSet(
                    bs_FixedFeeService.Fields.bs_FixedFeeServiceId,
                    bs_FixedFeeService.Fields.bs_Code,
                    bs_FixedFeeService.Fields.bs_Name,
                    bs_FixedFeeService.Fields.bs_DisplayName,
                    bs_FixedFeeService.Fields.bs_Fee,
                    bs_FixedFeeService.Fields.bs_InvoiceGroup,
                    bs_FixedFeeService.Fields.bs_StartDate,
                    bs_FixedFeeService.Fields.bs_EndDate
                ));
                req.RelatedEntitiesQuery.Should().ContainKey(new Relationship("bs_RevenueShareThreshold_Contract"));
                relatedTasksQuery = 
                    req.RelatedEntitiesQuery[new Relationship("bs_RevenueShareThreshold_Contract")] as QueryExpression;
                relatedTasksQuery.Should().NotBeNull();
                relatedTasksQuery!.ColumnSet.Should().BeEquivalentTo(new ColumnSet(
                    bs_RevenueShareThreshold.Fields.bs_RevenueShareThresholdId,
                    bs_RevenueShareThreshold.Fields.bs_Name,
                    bs_RevenueShareThreshold.Fields.bs_RevenueAccumulationType,
                    bs_RevenueShareThreshold.Fields.bs_RevenueCodeData,
                    bs_RevenueShareThreshold.Fields.bs_TierData,
                    bs_RevenueShareThreshold.Fields.bs_InvoiceGroup,
                    bs_RevenueShareThreshold.Fields.bs_ValidationThresholdAmount,
                    bs_RevenueShareThreshold.Fields.bs_ValidationThresholdType
                ));
            });

            _organizationService.Execute(retrieveRequestCapture).Returns(retrieveResponse);

            var result = _contractRepository.GetContractByCustomerSite(customerSiteId);

            result.Should().BeEquivalentTo(entity);
        }

        [Fact]
        public void UpdateContractDetail_ShouldUpdateContract()
        {
            var contractId = Guid.NewGuid();
            var updates = new bs_Contract
            {
                bs_PurchaseOrder = "PO12345",
                bs_BillingType = bs_billingtypechoices.Advanced,
                bs_IncrementAmount = 200.0m,
                bs_IncrementMonth = 2,
                bs_ConsumerPriceIndex = true,
                bs_Notes = "Example Note",
                bs_PaymentTerms = "Net 30",
                bs_ContractType = new[] { bs_contracttypechoices.FixedFee },
                bs_HoursBackupReport = true,
                bs_FixedFeeService_Contract = new[]
                {
                    new bs_FixedFeeService
                    {
                        bs_FixedFeeServiceId = Guid.NewGuid(),
                        bs_Code = "Code",
                        bs_Name = "Name",
                        bs_DisplayName = "Display Name",
                        bs_Fee = 100.0m
                    }
                },
                bs_LaborHourJob_Contract = new[]
                {
                    new bs_LaborHourJob
                    {
                        bs_LaborHourJobId = Guid.NewGuid(),
                        bs_Code = "Code",
                        bs_Name = "Name",
                        bs_DisplayName = "Display Name",
                        bs_OvertimeRate = 50.0m,
                        bs_Rate = 25.0m
                    }
                },
                bs_RevenueShareThreshold_Contract = new[]
                {
                    new bs_RevenueShareThreshold
                    {
                        bs_RevenueShareThresholdId = Guid.NewGuid(),
                        bs_RevenueAccumulationType = bs_revenueaccumulationtype.AnnualAnniversary,
                        bs_RevenueCodeData = "[\"123\",\"123\"]",
                        bs_TierData = "[{\"SharePercentage\":\"10\",\"Amount\":\"1000\"},{\"SharePercentage\":\"20\",\"Amount\":\"2000\"},{\"SharePercentage\":\"30\",\"Amount\":null}]"
                    }
                }
            };

            _contractRepository.UpdateContractDetail(contractId, updates);

            _organizationService.Received().Update(Arg.Is<Entity>(entity =>
                entity.Id == contractId &&
                entity.LogicalName == bs_Contract.EntityLogicalName &&
                entity.GetAttributeValue<string>(bs_Contract.Fields.bs_PurchaseOrder) == "PO12345" &&
                entity.GetAttributeValue<OptionSetValue>(bs_Contract.Fields.bs_BillingType).Value ==
                (int)bs_billingtypechoices.Advanced &&
                entity.GetAttributeValue<decimal>(bs_Contract.Fields.bs_IncrementAmount) == 200.0m &&
                entity.GetAttributeValue<int>(bs_Contract.Fields.bs_IncrementMonth) == 2 &&
                entity.GetAttributeValue<bool>(bs_Contract.Fields.bs_ConsumerPriceIndex) &&
                entity.GetAttributeValue<string>(bs_Contract.Fields.bs_Notes) == "Example Note" &&
                entity.GetAttributeValue<string>(bs_Contract.Fields.bs_PaymentTerms) == "Net 30" &&
                entity.GetAttributeValue<OptionSetValueCollection>(bs_Contract.Fields.bs_ContractType).Count == 1 &&
                entity.GetAttributeValue<bool>(bs_Contract.Fields.bs_HoursBackupReport) == true
            ));
        }

        [Fact]
        public void UpdateContractDetail_ShouldThrowInvalidOperationException_WhenFaultExceptionOccurs()
        {
            var contractId = Guid.NewGuid();
            var updates = new bs_Contract
            {
                bs_ContractId = contractId,
                bs_PurchaseOrder = "PO12345",
                bs_BillingType = bs_billingtypechoices.Advanced,
                bs_IncrementAmount = 200.0m,
                bs_IncrementMonth = 2,
                bs_ConsumerPriceIndex = true,
                bs_Notes = "Example Note",
                bs_PaymentTerms = "Net 30",
                bs_ContractType = new[] { bs_contracttypechoices.FixedFee },
                bs_HoursBackupReport = true,
                bs_FixedFeeService_Contract = new[]
                {
                    new bs_FixedFeeService
                    {
                        bs_FixedFeeServiceId = Guid.NewGuid(),
                        bs_Code = "Code",
                        bs_Name = "Name",
                        bs_DisplayName = "Display Name",
                        bs_Fee = 100.0m
                    }
                },
                bs_LaborHourJob_Contract = new[]
                {
                    new bs_LaborHourJob
                    {
                        bs_LaborHourJobId = Guid.NewGuid(),
                        bs_Code = "Code",
                        bs_Name = "Name",
                        bs_DisplayName = "Display Name",
                        bs_OvertimeRate = 50.0m,
                        bs_Rate = 25.0m
                    }
                }
            };

            var ex = new FaultException<OrganizationServiceFault>(new OrganizationServiceFault());

            _organizationService.When(x => x.Update(Arg.Any<Entity>())).Throw(ex);

            Action action = () => _contractRepository.UpdateContractDetail(contractId, updates);

            action.Should().Throw<FaultException>()
                .WithMessage("The creator of this fault did not specify a Reason.");
        }

        [Fact]
        public void UpdateContractRelatedEntities_ShouldUpdateRelatedEntities()
        {
            var contractId = Guid.NewGuid();

            var invoiceGroupToCreate = new bs_InvoiceGroup()
            {
                bs_InvoiceGroupId = Guid.NewGuid(),
                bs_Title = "Example title",
                bs_Description = "Example Description",
                bs_GroupNumber = 1
            };

            var invoiceGroupToDelete = new bs_InvoiceGroup()
            {
                bs_InvoiceGroupId = Guid.NewGuid(),
                bs_Title = "Example title 2",
                bs_Description = "Example Description 2",
                bs_GroupNumber = 2
            };

            var invoiceGroupToUpdate = new bs_InvoiceGroup()
            {
                bs_InvoiceGroupId = Guid.NewGuid(),
                bs_Title = "Example title 3",
                bs_Description = "Example Description 3",
                bs_GroupNumber = 3,
            };

            var fixedFeeServiceToCreate = new bs_FixedFeeService
            {
                bs_FixedFeeServiceId = Guid.NewGuid(),
                bs_Code = "Code",
                bs_Name = "Name",
                bs_DisplayName = "Display Name",
                bs_Fee = 100.0m
            };

            var fixedFeeServiceToDelete = new bs_FixedFeeService
            {
                bs_FixedFeeServiceId = Guid.NewGuid(),
                bs_Code = "Code",
                bs_Name = "Name",
                bs_DisplayName = "Display Name",
                bs_Fee = 100.0m
            };

            var laborHourJobToCreate = new bs_LaborHourJob
            {
                bs_LaborHourJobId = Guid.NewGuid(),
                bs_Code = "Code",
                bs_Name = "Name",
                bs_DisplayName = "Display Name",
                bs_OvertimeRate = 50.0m,
                bs_Rate = 25.0m
            };

            var laborHourJobToDelete = new bs_LaborHourJob
            {
                bs_LaborHourJobId = Guid.NewGuid(),
                bs_Code = "Code",
                bs_Name = "Name",
                bs_DisplayName = "Display Name",
                bs_OvertimeRate = 50.0m,
                bs_Rate = 25.0m
            };

            var thresholdStructureToCreate = new bs_RevenueShareThreshold
            {
                bs_RevenueShareThresholdId = Guid.NewGuid(),
                bs_RevenueAccumulationType = bs_revenueaccumulationtype.AnnualAnniversary,
                bs_RevenueCodeData = "[\"123\",\"123\"]",
                bs_TierData = "[{\"SharePercentage\":\"10\",\"Amount\":\"1000\"},{\"SharePercentage\":\"20\",\"Amount\":\"2000\"},{\"SharePercentage\":\"30\",\"Amount\":null}]"
            };

            var thresholdStructureToDelete = new bs_RevenueShareThreshold
            {
                bs_RevenueShareThresholdId = Guid.NewGuid(),
                bs_RevenueAccumulationType = bs_revenueaccumulationtype.AnnualAnniversary,
                bs_RevenueCodeData = "[\"123\",\"123\"]",
                bs_TierData = "[{\"SharePercentage\":\"10\",\"Amount\":\"1000\"},{\"SharePercentage\":\"20\",\"Amount\":\"2000\"},{\"SharePercentage\":\"30\",\"Amount\":null}]"
            };

            var bellServicesToCreate = new bs_BellService
            {
                bs_BellServiceId = Guid.NewGuid(),
                bs_InvoiceGroup = 1
            };

            var bellServicesToDelete = new bs_BellService
            {
                bs_BellServiceId = Guid.NewGuid(),
                bs_InvoiceGroup = 1
            };

            var midMonthsToCreate = new bs_MidMonthAdvance
            {
                bs_MidMonthAdvanceId = Guid.NewGuid(),
                bs_InvoiceGroup = 1,
                bs_Amount = 100.0m,
                bs_LineTitle = bs_lineitemtitle.PreBill
            };

            var midMonthsToDelete = new bs_MidMonthAdvance
            {
                bs_MidMonthAdvanceId = Guid.NewGuid(),
                bs_InvoiceGroup = 1,
                bs_Amount = 100.0m,
                bs_LineTitle = bs_lineitemtitle.MidMonthBilling
            };

            var depositedRevenueToCreate = new bs_DepositedRevenue
            {
                bs_DepositedRevenueId = Guid.NewGuid(),
                bs_TowneParkResponsibleForParkingTax = true,
                bs_InvoiceGroup = 1
            };

            var depositedRevenueToDelete = new bs_DepositedRevenue
            {
                bs_DepositedRevenueId = Guid.NewGuid(),
                bs_TowneParkResponsibleForParkingTax = true,
                bs_InvoiceGroup = 1
            };

            var billableAccountToCreate = new bs_BillableAccount
            {
                bs_BillableAccountId = Guid.NewGuid(),
                bs_PayrollAccountsData = "[\"123\",\"123\"]",
                bs_PayrollAccountsInvoiceGroup = 1,
                bs_PayrollAccountsLineTitle = "Line Title"
            };

            var billableAccountToDelete = new bs_BillableAccount
            {
                bs_BillableAccountId = Guid.NewGuid(),
                bs_PayrollAccountsData = "[\"123\",\"123\"]",
                bs_PayrollAccountsInvoiceGroup = 1,
                bs_PayrollAccountsLineTitle = "Line Title"
            };

            var managementFeesToCreate = new bs_ManagementAgreement
            {
                bs_ManagementAgreementId = Guid.NewGuid(),
                bs_ManagementAgreementType = bs_managementagreementtype.RevenuePercentage,
                bs_RevenuePercentageAmount = 5.5m
            };

            var managementFeesToDelete = new bs_ManagementAgreement
            {
                bs_ManagementAgreementId = Guid.NewGuid(),
                bs_ManagementAgreementType = bs_managementagreementtype.FixedFee,
                bs_FixedFeeAmount = 1000.00m
            };
             var nonGLExpenseToCreate = new bs_NonGLExpense
            {
                bs_NonGLExpenseId = Guid.NewGuid(),
                bs_NonGLExpenseType = bs_nonglexpensetype.FixedAmount,
                bs_ExpensePayrollType = bs_nonglpayrolltype.Billable,
                bs_ExpenseAmount = 500.00m,
                bs_ExpenseTitle = "Non-GL Create",
              //  bs_FinalPeriodBilled ="2025-07-01T00:00:00.000Z",
            };

            var nonGLExpenseToUpdate = new bs_NonGLExpense
            {
                bs_NonGLExpenseId = Guid.NewGuid(),
                bs_NonGLExpenseType = bs_nonglexpensetype.FixedAmount,
                bs_ExpensePayrollType = bs_nonglpayrolltype.Billable,
                bs_ExpenseAmount = 750.00m,
                bs_ExpenseTitle = "Non-GL Update",
            
            };
             var nonGLExpenseToDelete = new bs_NonGLExpense
            {
                bs_NonGLExpenseId = Guid.NewGuid()
            };

            var organizationResponse = new OrganizationResponse();

            _organizationService.Execute(Arg.Any<OrganizationRequest>()).Returns(organizationResponse);

          //  Ensure serviceClient is properly set up
            _contractRepository.UpdateContractRelatedEntities(new UpdateContractDao(
               contractId,
               new[] { invoiceGroupToCreate },
               new[] { invoiceGroupToDelete },
               new[] { invoiceGroupToUpdate },
               new[] { fixedFeeServiceToCreate },
               new[] { fixedFeeServiceToDelete },
               new[] { laborHourJobToCreate },
               new[] { laborHourJobToDelete },
               new[] { thresholdStructureToCreate },
               new[] { thresholdStructureToDelete },
               new[] { bellServicesToCreate },
               new[] { bellServicesToDelete },
               new[] { midMonthsToCreate },
               new[] { midMonthsToDelete },
               new[] { depositedRevenueToCreate },
               new[] { depositedRevenueToDelete },
               new[] { billableAccountToCreate },
               new[] { billableAccountToDelete },
               new[] { managementFeesToCreate },
               new[] { managementFeesToDelete },
               new[] {nonGLExpenseToCreate},
               new[] {nonGLExpenseToUpdate},
               new[] {nonGLExpenseToDelete}

            )
            {

            });

            // Prepare expected requests
            var expectedRequests = new OrganizationRequestCollection
            {
                new CreateRequest { Target = invoiceGroupToCreate },
                new UpdateRequest { Target = invoiceGroupToUpdate },
                new CreateRequest { Target = fixedFeeServiceToCreate },
                new CreateRequest { Target = laborHourJobToCreate },
                new CreateRequest { Target = thresholdStructureToCreate },
                new CreateRequest { Target = bellServicesToCreate },
                new CreateRequest { Target = midMonthsToCreate },
                new CreateRequest { Target = depositedRevenueToCreate },
                new CreateRequest { Target = billableAccountToCreate },
                new CreateRequest { Target = managementFeesToCreate },
                new CreateRequest { Target = nonGLExpenseToCreate },
                new UpdateRequest { Target = nonGLExpenseToUpdate },
                new DeleteRequest
                {
                    Target = new EntityReference(bs_InvoiceGroup.EntityLogicalName,
                        invoiceGroupToDelete.bs_InvoiceGroupId.Value)   
                },
                new DeleteRequest
                {
                    Target = new EntityReference(bs_FixedFeeService.EntityLogicalName,
                        fixedFeeServiceToDelete.bs_FixedFeeServiceId.Value)
                },
                new DeleteRequest
                {
                    Target = new EntityReference(bs_LaborHourJob.EntityLogicalName,
                        laborHourJobToDelete.bs_LaborHourJobId.Value)
                },
                new DeleteRequest
                {
                    Target = new EntityReference(bs_RevenueShareThreshold.EntityLogicalName,
                        thresholdStructureToDelete.bs_RevenueShareThresholdId.Value)
                },
                new DeleteRequest
                {
                    Target = new EntityReference(bs_BellService.EntityLogicalName,
                        bellServicesToDelete.bs_BellServiceId.Value)
                },
                new DeleteRequest
                {
                    Target = new EntityReference(bs_MidMonthAdvance.EntityLogicalName,
                        midMonthsToDelete.bs_MidMonthAdvanceId.Value)
                },
                new DeleteRequest
                {
                    Target = new EntityReference(bs_DepositedRevenue.EntityLogicalName,
                        depositedRevenueToDelete.bs_DepositedRevenueId.Value)
                },
                new DeleteRequest
                {
                    Target = new EntityReference(bs_BillableAccount.EntityLogicalName,
                        billableAccountToDelete.bs_BillableAccountId.Value)
                },
                new DeleteRequest
                {
                    Target = new EntityReference(bs_ManagementAgreement.EntityLogicalName,
                        managementFeesToDelete.bs_ManagementAgreementId.Value)
                },
                  new DeleteRequest
                {
                    Target = new EntityReference(bs_NonGLExpense.EntityLogicalName,
                        nonGLExpenseToDelete.bs_NonGLExpenseId.Value)   
                }
            };

            // Verify the ExecuteMultipleRequest was called once with the expected requests
            _organizationService.Received(1).Execute(
                Arg.Is<ExecuteMultipleRequest>(request =>
                    request.Requests.Count == expectedRequests.Count &&
                    request.Requests.OfType<CreateRequest>().Any(r =>
                        ((CreateRequest)r).Target.Id == invoiceGroupToCreate.bs_InvoiceGroupId
                    ) &&
                    request.Requests.OfType<UpdateRequest>().Any(r =>
                        ((UpdateRequest)r).Target.Id == invoiceGroupToUpdate.bs_InvoiceGroupId
                    ) &&
                    request.Requests.OfType<CreateRequest>().Any(r =>
                        ((CreateRequest)r).Target.Id == fixedFeeServiceToCreate.bs_FixedFeeServiceId
                    ) &&
                    request.Requests.OfType<CreateRequest>().Any(r =>
                        ((CreateRequest)r).Target.Id == laborHourJobToCreate.bs_LaborHourJobId
                    ) &&
                    request.Requests.OfType<CreateRequest>().Any(r =>
                        ((CreateRequest)r).Target.Id == thresholdStructureToCreate.bs_RevenueShareThresholdId
                    ) &&
                    request.Requests.OfType<CreateRequest>().Any(r =>
                        ((CreateRequest)r).Target.Id == bellServicesToCreate.bs_BellServiceId
                    ) &&
                    request.Requests.OfType<CreateRequest>().Any(r =>
                        ((CreateRequest)r).Target.Id == midMonthsToCreate.bs_MidMonthAdvanceId
                    ) &&
                    request.Requests.OfType<CreateRequest>().Any(r =>
                        ((CreateRequest)r).Target.Id == depositedRevenueToCreate.bs_DepositedRevenueId
                    ) &&
                    request.Requests.OfType<CreateRequest>().Any(r =>
                        ((CreateRequest)r).Target.Id == managementFeesToCreate.bs_ManagementAgreementId
                    ) &&
                    request.Requests.OfType<DeleteRequest>().Any(r =>
                        ((DeleteRequest)r).Target.Id == invoiceGroupToDelete.bs_InvoiceGroupId
                    ) &&
                    request.Requests.OfType<DeleteRequest>().Any(r =>
                        ((DeleteRequest)r).Target.Id == fixedFeeServiceToDelete.bs_FixedFeeServiceId
                    ) &&
                    request.Requests.OfType<DeleteRequest>().Any(r =>
                        ((DeleteRequest)r).Target.Id == laborHourJobToDelete.bs_LaborHourJobId
                    ) &&
                    request.Requests.OfType<DeleteRequest>().Any(r =>
                        ((DeleteRequest)r).Target.Id == thresholdStructureToDelete.bs_RevenueShareThresholdId
                    ) &&
                    request.Requests.OfType<DeleteRequest>().Any(r =>
                        ((DeleteRequest)r).Target.Id == bellServicesToDelete.bs_BellServiceId
                    ) &&
                    request.Requests.OfType<DeleteRequest>().Any(r =>
                        ((DeleteRequest)r).Target.Id == midMonthsToDelete.bs_MidMonthAdvanceId
                    ) &&
                    request.Requests.OfType<DeleteRequest>().Any(r =>
                        ((DeleteRequest)r).Target.Id == depositedRevenueToDelete.bs_DepositedRevenueId
                    ) &&
                    request.Requests.OfType<DeleteRequest>().Any(r =>
                        ((DeleteRequest)r).Target.Id == managementFeesToDelete.bs_ManagementAgreementId
                    )&&
                    request.Requests.OfType<DeleteRequest>().Any(r =>
                        ((DeleteRequest)r).Target.Id == nonGLExpenseToDelete.bs_NonGLExpenseId
                    )

                )
            );    
        }


        [Fact]
        public void GetDeviations_ShouldReturnDeviations()
        {
            // Arrange
            var customerSiteId = Guid.NewGuid();
            var tpContractId = Guid.NewGuid();
            var CustomerSiteEntityAlias = "customer_site";

            var entity1 = new bs_Contract
            {
                Id = tpContractId
            };

            entity1.bs_ContractId = tpContractId;
            entity1.bs_DeviationAmount = 100.0m;
            entity1.bs_DeviationPercentage = 5.0m;

            entity1[$"{CustomerSiteEntityAlias}.{bs_CustomerSite.Fields.bs_CustomerSiteId}"] =
                new AliasedValue("bs_Customersite", bs_CustomerSite.Fields.bs_CustomerSiteId, customerSiteId);
            entity1[$"{CustomerSiteEntityAlias}.{bs_CustomerSite.Fields.bs_SiteName}"] =
                new AliasedValue("bs_Customersite", bs_CustomerSite.Fields.bs_SiteName, "Site A");
            entity1[$"{CustomerSiteEntityAlias}.{bs_CustomerSite.Fields.bs_SiteNumber}"] =
                new AliasedValue("bs_Customersite", bs_CustomerSite.Fields.bs_SiteNumber, "001");

            var retrieveMultipleResponse = new EntityCollection(new List<Entity> { entity1 });
            _organizationService.RetrieveMultiple(Arg.Any<QueryBase>()).Returns(retrieveMultipleResponse);

            // Act
            var result = _contractRepository.GetDeviations();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);

            var deviation = result.First();

            // Asserts for directly accessible values:
            deviation.bs_DeviationAmount.Should().Be(100.0m);
            deviation.bs_DeviationPercentage.Should().Be(5.0m);
            deviation.bs_ContractId.Should().Be(tpContractId);

            // Asserts for aliased values:
            deviation.bs_Contract_CustomerSite.Should().NotBeNull();
            deviation.bs_Contract_CustomerSite.bs_CustomerSiteId.Should().Be(customerSiteId);
            deviation.bs_Contract_CustomerSite.bs_SiteName.Should().Be("Site A");
            deviation.bs_Contract_CustomerSite.bs_SiteNumber.Should().Be("001");
        }


        [Fact]
        public void UpdateDeviationThreshold_ShouldUpdateDeviationThreshold()
        {
            var deviationUpdates = new List<bs_Contract>
            {
                new bs_Contract
                {
                    bs_ContractId = Guid.NewGuid(),
                    bs_DeviationAmount = 100.00m,
                    bs_DeviationPercentage = 10.5m
                },
                new bs_Contract
                {
                    bs_ContractId = Guid.NewGuid(),
                    bs_DeviationAmount = 200.00m,
                    bs_DeviationPercentage = 20.0m
                }
            };

            var organizationResponse = new OrganizationResponse();

            _organizationService.Execute(Arg.Any<OrganizationRequest>()).Returns(organizationResponse);

            // Ensure serviceClient is properly set up
            _contractRepository.UpdateDeviationThreshold(deviationUpdates);

            // Prepare expected requests
            var expectedRequests = new OrganizationRequestCollection
            {
                new UpdateRequest { Target = deviationUpdates[0] },
                new UpdateRequest { Target = deviationUpdates[1] }
            };

            // Verify the ExecuteMultipleRequest was called once with the expected requests
            _organizationService.Received(1).Execute(
                Arg.Is<ExecuteMultipleRequest>(request =>
                    request.Requests.Count == expectedRequests.Count &&
                    request.Requests.OfType<UpdateRequest>().Any(r =>
                        ((UpdateRequest)r).Target.Id == deviationUpdates[0].bs_ContractId
                    ) &&
                    request.Requests.OfType<UpdateRequest>().Any(r =>
                        ((UpdateRequest)r).Target.Id == deviationUpdates[1].bs_ContractId
                    )
                )
            );
        }
    }
}