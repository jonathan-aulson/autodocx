using api.Adapters.Mappers;
using api.Data;
using api.Data.Impl;
using api.Models.Vo;
using api.Usecases;
using Azure;
using Microsoft.Xrm.Sdk;
using TownePark;
using static api.Models.Vo.ContractDetailVo;

namespace api.Services.Impl
{
    public class ContractService : IContractService
    {
        private readonly IContractRepository _contractRepository;
     
        private readonly IValidateAndPopulateGlCodes _validateAndPopulateGlCodes;

        public ContractService(IContractRepository contractRepository, IValidateAndPopulateGlCodes validateAndPopulateGlCodes)
        {
            _contractRepository = contractRepository;
            _validateAndPopulateGlCodes = validateAndPopulateGlCodes;
        }

        public void UpdateDeviationThreshold(IEnumerable<DeviationVo> updateDeviation)
        {
            _contractRepository.UpdateDeviationThreshold(DeviationMapper.UpdateDeviationVoToModel(updateDeviation));
        }

        public ContractDetailVo GetContractDetail(Guid customerSiteId)
        {
            var contract = _contractRepository.GetContractByCustomerSite(customerSiteId);
            return ContractMapper.ContractModelToVo(contract);
        }

        public void UpdateContract(Guid contractId, ContractDetailVo updateContractVo)
        {
            _validateAndPopulateGlCodes.Apply(updateContractVo);
            var existingContractVo = GetExistingEntity(contractId);
            var updates = UpdateContractMapper.ContractDetailVoToUpdateModel(existingContractVo, updateContractVo);
// new updated
            var existingNonGlExpenses = existingContractVo.ManagementAgreement.ManagementFees?
            .SelectMany(mf => mf.NonGlBillableExpenses ?? Enumerable.Empty<NonGlBillableExpenseVo>())
            .ToList() ?? new List<NonGlBillableExpenseVo>();

            var updatedNonGlExpenses = updateContractVo.ManagementAgreement.ManagementFees?
            .SelectMany(mf => mf.NonGlBillableExpenses ?? Enumerable.Empty<NonGlBillableExpenseVo>())
            .ToList() ?? new List<NonGlBillableExpenseVo>();

            var (invoiceGroupsToCreate, invoiceGroupsToDelete) =
                GetInvoiceGroupAdditionsAndDeletions(existingContractVo, updateContractVo);
            var (serviceRatesToCreate, serviceRatesToDelete) =
                GetFixedFeeAdditionsAndDeletions(existingContractVo, updateContractVo);
            var (jobRatesToCreate, jobRatesToDelete) =
                GetLaborHourAdditionsAndDeletions(existingContractVo, updateContractVo);
            var (thresholdStructuresToCreate, thresholdStructuresToDelete) =
                GetThresholdStructuresAdditionsAndDeletions(existingContractVo, updateContractVo);
            var (bellServicesToCreate, bellServicesToDelete) =
                GetBellServiceAdditionsAndDeletions(existingContractVo, updateContractVo);
            var (midMonthsToCreate, midMonthsToDelete) =
                GetMidMonthAdvanceAdditionsAndDeletions(existingContractVo, updateContractVo);
            var (depositedRevenuesToCreate, depositedRevenuesToDelete) =
                GetDepositedRevenueAdditionsAndDeletions(existingContractVo, updateContractVo);
            var (billableAccountToCreate, billableAccountToDelete) =
                GetBillableAccountAdditionsAndDeletions(existingContractVo, updateContractVo);
            var (managementFeesToCreate, managementFeesToDelete) =
                GetManagementAgreementAdditionsAndDeletions(existingContractVo, updateContractVo);
                // new updated
            var (NonGLExpensGroupsToCreate, NonGLExpensToDelete) =
               GetNonGLExpenseGroupAdditionsAndDeletions(existingNonGlExpenses, updatedNonGlExpenses);

            List<bs_InvoiceGroup> invoiceGroupsToUpdate = new List<bs_InvoiceGroup>();
            if (updates.bs_InvoiceGroup_Contract != null)
            {
                invoiceGroupsToUpdate.AddRange(updates.bs_InvoiceGroup_Contract.Where(ig =>
                    ig.Id != Guid.Empty &&
                    ig.bs_InvoiceGroupId.HasValue &&
                    !invoiceGroupsToCreate.Any(create => create.Id == ig.Id) &&
                    !invoiceGroupsToDelete.Any(delete => delete.Id == ig.Id)
                ));
            }
           
            var NonGLExpenseToUpdate = new List<bs_NonGLExpense>();
            foreach (var updatedExp in updatedNonGlExpenses)
            {
                var existingExp = existingNonGlExpenses.FirstOrDefault(e => e.Id == updatedExp.Id);
                if (existingExp != null)
                {
                    var updateModel = NonGlExpense(existingExp, updatedExp);
                    if (updateModel != null)
                    {
                        updateModel.bs_NonGLExpenseId = (Guid)existingExp.Id;
                        NonGLExpenseToUpdate.Add(updateModel);
                      
                    }
                
                }
            }

            // TODO is add and remove list really necessary?
         //   _nonGlBillableExpense.UpdateNonGlBillExpenseDetail(contractId, nonGlToUpdate);
            _contractRepository.UpdateContractDetail(contractId, updates);
            _contractRepository.UpdateContractRelatedEntities(new UpdateContractDao(
                contractId, invoiceGroupsToCreate, invoiceGroupsToDelete, invoiceGroupsToUpdate, serviceRatesToCreate,
                serviceRatesToDelete, jobRatesToCreate, jobRatesToDelete,
                thresholdStructuresToCreate, thresholdStructuresToDelete,
                bellServicesToCreate, bellServicesToDelete,
                midMonthsToCreate, midMonthsToDelete,
                depositedRevenuesToCreate, depositedRevenuesToDelete,
                billableAccountToCreate, billableAccountToDelete,
                managementFeesToCreate, managementFeesToDelete,
                NonGLExpensGroupsToCreate, NonGLExpenseToUpdate, NonGLExpensToDelete

                )
            {

            });
        }

        public IEnumerable<DeviationVo> GetDeviations()
        {
            var deviations = _contractRepository.GetDeviations();
            return DeviationMapper.DeviationModelToVo(deviations);
        }
// new updated
        private static bs_NonGLExpense? NonGlExpense(NonGlBillableExpenseVo existingNonGlExpense, NonGlBillableExpenseVo newNonGlExpense)
        {
            var model = new bs_NonGLExpense();
            var changedField = false;
            if (existingNonGlExpense.ExpenseTitle != newNonGlExpense.ExpenseTitle)
            {
                model.bs_ExpenseTitle = newNonGlExpense.ExpenseTitle;
                changedField = true;
            }

            if (existingNonGlExpense.ExpenseAmount != newNonGlExpense.ExpenseAmount)
            {
                model.bs_ExpenseAmount = newNonGlExpense.ExpenseAmount;
                changedField = true;
            }

            if (existingNonGlExpense.FinalPeriodBilled != newNonGlExpense.FinalPeriodBilled)
            {
                model.bs_FinalPeriodBilled = newNonGlExpense.FinalPeriodBilled;
                changedField = true;
            }

            if (existingNonGlExpense.ExpensePayrollType != newNonGlExpense.ExpensePayrollType)
            {
                if (Enum.TryParse<bs_nonglpayrolltype>(newNonGlExpense.ExpensePayrollType, out var parsedValue))
                {
                    model.bs_ExpensePayrollType = parsedValue;
                    changedField = true;
                }
            }

            if (existingNonGlExpense.NonGLExpenseType != newNonGlExpense.NonGLExpenseType)
            {
                if (Enum.TryParse<bs_nonglexpensetype>(newNonGlExpense.NonGLExpenseType, out var parsedValue))
                {
                    model.bs_NonGLExpenseType = parsedValue;
                    changedField = true;
                }
            }


            return changedField ? model : null;
        }

        public Guid AddContract(Guid customerSiteId, string contractType, bool deposits)
        {
            var contract = new bs_Contract
            {
                bs_CustomerSiteFK = new EntityReference(bs_CustomerSite.EntityLogicalName, customerSiteId),
                bs_ContractTypeString = contractType,
                bs_Deposits = deposits,
                bs_BillingType = bs_billingtypechoices.Arrears
            };
            return _contractRepository.AddContract(contract);
        }

        // Helper methods

        private (IEnumerable<bs_RevenueShareThreshold>, IEnumerable<bs_RevenueShareThreshold>) GetThresholdStructuresAdditionsAndDeletions(
            ContractDetailVo existingContract, ContractDetailVo updateContract)
        {
            var existingThresholdStructures = existingContract.RevenueShare.ThresholdStructures;
            var updatedThresholdStructures = updateContract.RevenueShare.ThresholdStructures;
            var thresholdStructuresToCreate = FindThresholdStructuresToCreate(updatedThresholdStructures);
            var thresholdStructuresToDelete = FindThresholdStructuresToDelete(existingThresholdStructures, updatedThresholdStructures);
            return (thresholdStructuresToCreate, thresholdStructuresToDelete);
        }

        private (IEnumerable<bs_BellService>, IEnumerable<bs_BellService>) GetBellServiceAdditionsAndDeletions(
            ContractDetailVo existingContract, ContractDetailVo updateContract)
        {
            var existingBellServices = existingContract.BellServiceFee.BellServices;
            var updatedBellServices = updateContract.BellServiceFee.BellServices;
            var bellServicesToCreate = updatedBellServices
                .Where(bellService => bellService.Id == null || bellService.Id == Guid.Empty)
                .Select(UpdateContractMapper.BellServiceVoToModel);
            var bellServicesToDelete = existingBellServices
                .Where(existingBellService => updatedBellServices.All(newBellService => newBellService.Id != existingBellService.Id))
                .Select(UpdateContractMapper.BellServiceVoToModel);
            return (bellServicesToCreate, bellServicesToDelete);
        }

        private (IEnumerable<bs_MidMonthAdvance>, IEnumerable<bs_MidMonthAdvance>) GetMidMonthAdvanceAdditionsAndDeletions(
            ContractDetailVo existingContract, ContractDetailVo updateContract)
        {
            var existingMidMonths = existingContract.MidMonthAdvance.MidMonthAdvances;
            var updatedMidMonths = updateContract.MidMonthAdvance.MidMonthAdvances;
            var midMonthsToCreate = updatedMidMonths
                .Where(midMonth => midMonth.Id == null || midMonth.Id == Guid.Empty)
                .Select(UpdateContractMapper.MidMonthAdvanceVoToModel);
            var midMonthsToDelete = existingMidMonths
                .Where(existingMidMonth => updatedMidMonths.All(newMidMonth => newMidMonth.Id != existingMidMonth.Id))
                .Select(UpdateContractMapper.MidMonthAdvanceVoToModel);
            return (midMonthsToCreate, midMonthsToDelete);
        }

        private (IEnumerable<bs_BillableAccount>, IEnumerable<bs_BillableAccount>) GetBillableAccountAdditionsAndDeletions(
            ContractDetailVo existingContract, ContractDetailVo updateContract)
        {
            var existingBillableAccounts = existingContract.BillableAccount.BillableAccountsData;
            var updatedBillableAccounts = updateContract.BillableAccount.BillableAccountsData;
            var billableAccountsToCreate = updatedBillableAccounts
                .Where(billableAccount => billableAccount.Id == null || billableAccount.Id == Guid.Empty)
                .Select(UpdateContractMapper.BillableAccountVoToModel);
            var billableAccountsToDelete = existingBillableAccounts
                .Where(existingBillableAccount => updatedBillableAccounts.All(newBillableAccount => newBillableAccount.Id != existingBillableAccount.Id))
                .Select(UpdateContractMapper.BillableAccountVoToModel);
            return (billableAccountsToCreate, billableAccountsToDelete);
        }

        private (IEnumerable<bs_ManagementAgreement>, IEnumerable<bs_ManagementAgreement>) GetManagementAgreementAdditionsAndDeletions(
            ContractDetailVo existingContract, ContractDetailVo updateContract)
        {
            var existingManagementAgreements = existingContract.ManagementAgreement.ManagementFees;
            var updatedManagementAgreements = updateContract.ManagementAgreement.ManagementFees;
            var managementFeesToCreate = updatedManagementAgreements
                .Where(managementAgreement => managementAgreement.Id == null || managementAgreement.Id == Guid.Empty)
                .Select(UpdateContractMapper.ManagementAgreementVoToModel);
            var managementFeesToDelete = existingManagementAgreements
                .Where(existingManagementAgreement => updatedManagementAgreements.All(newManagementAgreement => newManagementAgreement.Id != existingManagementAgreement.Id))
                .Select(UpdateContractMapper.ManagementAgreementVoToModel);
            return (managementFeesToCreate, managementFeesToDelete);
        }

        private (IEnumerable<bs_DepositedRevenue>, IEnumerable<bs_DepositedRevenue>) GetDepositedRevenueAdditionsAndDeletions(
            ContractDetailVo existingContract, ContractDetailVo updateContract)
        {
            var existingDepositedRevenues = existingContract.DepositedRevenue.DepositData;
            var updatedDepositedRevenues = updateContract.DepositedRevenue.DepositData;
            var depositedRevenuesToCreate = FindDepositedRevenuesToCreate(updatedDepositedRevenues);
            var depositedRevenuesToDelete = FindDepositedRevenuesToDelete(existingDepositedRevenues, updatedDepositedRevenues);
            return (depositedRevenuesToCreate, depositedRevenuesToDelete);
        }

        private (IEnumerable<bs_InvoiceGroup>, IEnumerable<bs_InvoiceGroup>) GetInvoiceGroupAdditionsAndDeletions(
            ContractDetailVo existingContract, ContractDetailVo updateContract)
        {
            var existingInvoiceGroups = existingContract.InvoiceGrouping.InvoiceGroups;
            var updatedInvoiceGroups = updateContract.InvoiceGrouping.InvoiceGroups;
            var invoiceGroupsToCreate = FindInvoiceGroupsToCreate(updatedInvoiceGroups);
            var invoiceGroupsToDelete = FindInvoiceGroupsToDelete(existingInvoiceGroups, updatedInvoiceGroups);
            return (invoiceGroupsToCreate, invoiceGroupsToDelete);
        }

        private (IEnumerable<bs_FixedFeeService>, IEnumerable<bs_FixedFeeService>) GetFixedFeeAdditionsAndDeletions(
            ContractDetailVo existingContract, ContractDetailVo updateContract)
        {
            var existingServiceRates = existingContract.FixedFee.ServiceRates;
            var updatedServiceRates = updateContract.FixedFee.ServiceRates;
            var serviceRatesToCreate = FindServiceRatesToCreate(updatedServiceRates);
            var serviceRatesToDelete = FindServiceRatesToDelete(existingServiceRates, updatedServiceRates);
            return (serviceRatesToCreate, serviceRatesToDelete);
        }

        private (IEnumerable<bs_LaborHourJob>, IEnumerable<bs_LaborHourJob>) GetLaborHourAdditionsAndDeletions(
            ContractDetailVo existingContract, ContractDetailVo updateContract)
        {
            var existingJobRates = existingContract.PerLaborHour.JobRates;
            var updatedJobRates = updateContract.PerLaborHour.JobRates;
            var jobRatesToCreate = FindJobRatesToCreate(updatedJobRates);
            var jobRatesToDelete = FindJobRatesToDelete(existingJobRates, updatedJobRates);
            return (jobRatesToCreate, jobRatesToDelete);
        }
        
        private IEnumerable<bs_InvoiceGroup> FindInvoiceGroupsToCreate(IEnumerable<InvoiceGroupVo> updatedInvoiceGroups)
        {
            var newInvoiceGroup = updatedInvoiceGroups
                .Where(invoiceGroup => invoiceGroup.Id == null || invoiceGroup.Id == Guid.Empty);
            var response = newInvoiceGroup
                .Select(UpdateContractMapper.InvoiceGroupVoToModel);
            return response;
        }

        private IEnumerable<bs_InvoiceGroup> FindInvoiceGroupsToDelete(
            IEnumerable<InvoiceGroupVo> existingInvoiceGroups, IReadOnlyCollection<InvoiceGroupVo> updatedInvoiceGroups)
        {
            return existingInvoiceGroups
                .Where(existingInvoiceGroup => updatedInvoiceGroups.All(newInvoiceGroup => newInvoiceGroup.Id != existingInvoiceGroup.Id))
                .Select(UpdateContractMapper.InvoiceGroupVoToModel);
        }

        private IEnumerable<bs_RevenueShareThreshold> FindThresholdStructuresToCreate(
            IEnumerable<ThresholdStructureVo> updatedThresholdStructures)
        {
            return updatedThresholdStructures
                .Where(t => t.Id == null || t.Id == Guid.Empty)
                .Select(UpdateContractMapper.RevenueShareThresholdVoToModel);
        }

        private IEnumerable<bs_RevenueShareThreshold> FindThresholdStructuresToDelete(
            IEnumerable<ThresholdStructureVo> existingThresholdStructures,
            IReadOnlyCollection<ThresholdStructureVo> updatedThresholdStructures)
        {
            return existingThresholdStructures
                .Where(existingThreshold => updatedThresholdStructures.All(updatedThreshold => updatedThreshold.Id != existingThreshold.Id))
                .Select(UpdateContractMapper.RevenueShareThresholdVoToModel);
        }

        private IEnumerable<bs_FixedFeeService> FindServiceRatesToCreate(IEnumerable<ServiceRateVo> updatedServiceRates)
        {
            return updatedServiceRates
                .Where(service => service.Id == null || service.Id == Guid.Empty)
                .Select(UpdateContractMapper.FixedFeeServiceVoToModel);
        }

        private IEnumerable<bs_FixedFeeService> FindServiceRatesToDelete(
            IEnumerable<ServiceRateVo> existingServiceRates, IReadOnlyCollection<ServiceRateVo> updatedServiceRates)
        {
            return existingServiceRates
                .Where(existingService => updatedServiceRates.All(newService => newService.Id != existingService.Id))
                .Select(UpdateContractMapper.FixedFeeServiceVoToModel);
        }

        private IEnumerable<bs_LaborHourJob> FindJobRatesToCreate(IEnumerable<JobRateVo> updatedJobRates)
        {
            return updatedJobRates
                .Where(job => job.Id == null || job.Id == Guid.Empty)
                .Select(UpdateContractMapper.LaborHourJobVoToModel);
        }

        private IEnumerable<bs_LaborHourJob> FindJobRatesToDelete(IEnumerable<JobRateVo> existingJobRates,
    IReadOnlyCollection<JobRateVo> updatedJobRates)
        {
            return existingJobRates
                .Where(existingJob => updatedJobRates.All(updatedJob => updatedJob.Id != existingJob.Id))
                .Select(UpdateContractMapper.LaborHourJobVoToModel);
        }

        private IEnumerable<bs_DepositedRevenue> FindDepositedRevenuesToCreate(IEnumerable<DepositDataVo> updatedDepositedRevenues)
        {
            return updatedDepositedRevenues
                .Where(depositedRevenue => depositedRevenue.Id == null || depositedRevenue.Id == Guid.Empty)
                .Select(UpdateContractMapper.DepositedRevenueVoToModel);
        }

        private IEnumerable<bs_DepositedRevenue> FindDepositedRevenuesToDelete(
            IEnumerable<DepositDataVo> existingDepositedRevenues, IReadOnlyCollection<DepositDataVo> updatedDepositedRevenues)
        {
            return existingDepositedRevenues
                .Where(existingDepositedRevenue => updatedDepositedRevenues.All(updatedDepositedRevenue => updatedDepositedRevenue.Id != existingDepositedRevenue.Id))
                .Select(UpdateContractMapper.DepositedRevenueVoToModel);
        }

        private ContractDetailVo GetExistingEntity(Guid contractId)
        {
            var existingContract = _contractRepository.GetContract(contractId);
            return ContractMapper.ContractModelToVo(existingContract);
        }

        private (IEnumerable<bs_NonGLExpense>, IEnumerable<bs_NonGLExpense>) GetNonGLExpenseGroupAdditionsAndDeletions(
         List<NonGlBillableExpenseVo> existingNonGLExpense, List<NonGlBillableExpenseVo> updateNonGLExpense)
        {

            var NonGLExpensGroupsToCreate = FindNonGLExpenseToCreate(updateNonGLExpense);
            var NonGLExpensToDelete = FindNonGLExpenseToDelete(existingNonGLExpense, updateNonGLExpense);
            return (NonGLExpensGroupsToCreate, NonGLExpensToDelete);
        }


        private IEnumerable<bs_NonGLExpense> FindNonGLExpenseToCreate(IEnumerable<NonGlBillableExpenseVo> updatedNonGLExpense)
        {
            var newNonGLExpense = updatedNonGLExpense
                .Where(nonGLExpense => nonGLExpense.Id == null || nonGLExpense.Id == Guid.Empty).Select(nonGLExpense =>
                {
                    nonGLExpense.Id = Guid.NewGuid(); 
                    return nonGLExpense;
                }).ToList();
            var response = newNonGLExpense
                .Select(UpdateContractMapper.NonGLExpenseVoToModel);
            return response;
        }
        
        private IEnumerable<bs_NonGLExpense> FindNonGLExpenseToDelete(
            IEnumerable<NonGlBillableExpenseVo> existingNonGLExpense, IReadOnlyCollection<NonGlBillableExpenseVo> updatedNonGLExpense)
        {
            return existingNonGLExpense
                .Where(existingNonGLExpense => updatedNonGLExpense.All(newNonGLExpense => newNonGLExpense.Id != existingNonGLExpense.Id))
                .Select(UpdateContractMapper.NonGLExpenseVoToModel);
        }
    }
   

}