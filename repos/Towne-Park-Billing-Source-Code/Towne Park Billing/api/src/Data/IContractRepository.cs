using TownePark;

namespace api.Data
{
    public interface IContractRepository
    {
        bs_Contract GetContractByCustomerSite(Guid customerSiteId);
        void UpdateContractDetail(Guid contractId, bs_Contract updates);
        void UpdateContractRelatedEntities(UpdateContractDao changes);
        bs_Contract GetContract(Guid contractId);
        void UpdateDeviationThreshold(IEnumerable<bs_Contract> deviationThresholdUpdates);
        IEnumerable<bs_Contract> GetDeviations();
        IEnumerable<Guid> GetContractIdsByCustomerSite(IEnumerable<Guid> customerSiteIds);
        Guid AddContract(bs_Contract contract);
        string GetContractTypeStringByCustomerSite(Guid customerSiteId);
    }
// new updated
    public class UpdateContractDao
    {
        public UpdateContractDao(Guid contractId, IEnumerable<bs_InvoiceGroup> invoiceGroupsToCreate, IEnumerable<bs_InvoiceGroup> invoiceGroupsToDelete, IEnumerable<bs_InvoiceGroup> invoiceGroupsToUpdate, IEnumerable<bs_FixedFeeService> servicesToCreate, IEnumerable<bs_FixedFeeService> servicesToDelete, IEnumerable<bs_LaborHourJob> jobsToCreate, IEnumerable<bs_LaborHourJob> jobsToDelete,
            IEnumerable<bs_RevenueShareThreshold> thresholdStructuresToCreate, IEnumerable<bs_RevenueShareThreshold> thresholdStructuresToDelete,
            IEnumerable<bs_BellService> bellServicesToCreate, IEnumerable<bs_BellService> bellServicesToDelete,
            IEnumerable<bs_MidMonthAdvance> midMonthsToCreate, IEnumerable<bs_MidMonthAdvance> midMonthsToDelete,
            IEnumerable<bs_DepositedRevenue> depositedRevenuesToCreate, IEnumerable<bs_DepositedRevenue> depositedRevenuesToDelete,
            IEnumerable<bs_BillableAccount> billableAccountsToCreate, IEnumerable<bs_BillableAccount> billableAccountsToDelete,
            IEnumerable<bs_ManagementAgreement> managementFeesToCreate, IEnumerable<bs_ManagementAgreement> managementFeesToDelete,
             IEnumerable<bs_NonGLExpense> nonGLExpenseToCreate, IEnumerable<bs_NonGLExpense> nonGLExpenseToUpdate,IEnumerable<bs_NonGLExpense> nonGLExpenseToDelete)
        {
            ContractId = contractId;
            InvoiceGroupsToCreate = invoiceGroupsToCreate;
            InvoiceGroupsToDelete = invoiceGroupsToDelete;
            InvoiceGroupsToUpdate = invoiceGroupsToUpdate;
            ServicesToCreate = servicesToCreate;
            ServicesToDelete = servicesToDelete;
            JobsToCreate = jobsToCreate;
            JobsToDelete = jobsToDelete;
            ThresholdStructuresToCreate = thresholdStructuresToCreate;
            ThresholdStructuresToDelete = thresholdStructuresToDelete;
            BellServicesToCreate = bellServicesToCreate;
            BellServicesToDelete = bellServicesToDelete;
            MidMonthsToCreate = midMonthsToCreate;
            MidMonthsToDelete = midMonthsToDelete;
            DepositedRevenuesToCreate = depositedRevenuesToCreate;
            DepositedRevenuesToDelete = depositedRevenuesToDelete;
            BillableAccountsToCreate = billableAccountsToCreate;
            BillableAccountsToDelete = billableAccountsToDelete;
            ManagementFeesToCreate = managementFeesToCreate;
            ManagementFeesToDelete = managementFeesToDelete;
            NonGLExpenseToCreate = nonGLExpenseToCreate;
            NonGLExpenseToDelete = nonGLExpenseToDelete;
            NonGLExpenseToUpdate = nonGLExpenseToUpdate;
        }

        public Guid ContractId { get; }
        public IEnumerable<bs_InvoiceGroup> InvoiceGroupsToCreate { get; }
        public IEnumerable<bs_InvoiceGroup> InvoiceGroupsToDelete { get; }
        public IEnumerable<bs_InvoiceGroup> InvoiceGroupsToUpdate { get; }
        public IEnumerable<bs_FixedFeeService> ServicesToCreate { get; }
        public IEnumerable<bs_FixedFeeService> ServicesToDelete { get; }
        public IEnumerable<bs_LaborHourJob> JobsToCreate { get; }
        public IEnumerable<bs_LaborHourJob> JobsToDelete { get; }
        public IEnumerable<bs_RevenueShareThreshold> ThresholdStructuresToCreate { get; }
        public IEnumerable<bs_RevenueShareThreshold> ThresholdStructuresToDelete { get; set; }
        public IEnumerable<bs_BellService> BellServicesToCreate { get; }
        public IEnumerable<bs_BellService> BellServicesToDelete { get; }
        public IEnumerable<bs_MidMonthAdvance> MidMonthsToCreate { get; }
        public IEnumerable<bs_MidMonthAdvance> MidMonthsToDelete { get; }
        public IEnumerable<bs_DepositedRevenue> DepositedRevenuesToCreate { get; }
        public IEnumerable<bs_DepositedRevenue> DepositedRevenuesToDelete { get; }
        public IEnumerable<bs_BillableAccount> BillableAccountsToCreate { get; }
        public IEnumerable<bs_BillableAccount> BillableAccountsToDelete { get; }
        public IEnumerable<bs_ManagementAgreement> ManagementFeesToCreate { get; }
        public IEnumerable<bs_ManagementAgreement> ManagementFeesToDelete { get; }
// new updated
        public IEnumerable<bs_NonGLExpense> NonGLExpenseToCreate { get; }
        public IEnumerable<bs_NonGLExpense> NonGLExpenseToDelete { get; }
        public IEnumerable<bs_NonGLExpense> NonGLExpenseToUpdate { get; }
    }
}
