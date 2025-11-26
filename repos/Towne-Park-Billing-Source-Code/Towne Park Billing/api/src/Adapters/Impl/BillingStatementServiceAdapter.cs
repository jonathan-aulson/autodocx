using api.Models.Dto;
using api.Services;

namespace api.Adapters.Impl
{
    public class BillingStatementServiceAdapter : IBillingStatementServiceAdapter
    {
        private readonly IBillingStatementService _billingStatementService;

        public BillingStatementServiceAdapter(IBillingStatementService billingStatementService)
        {
            _billingStatementService = billingStatementService;
        }
        
        public IEnumerable<BillingStatementDto> GetCurrentBillingStatements(UserDto userAuth)
        {
            return Mappers.BillingStatementMapper.BillingStatementVoToDto(_billingStatementService.GetCurrentBillingStatements(userAuth));
        }

        public IEnumerable<BillingStatementDto> GetBillingStatements(Guid customerSiteId)
        {
            return Mappers.BillingStatementMapper.BillingStatementVoToDto(_billingStatementService.GetBillingStatements(customerSiteId));
        }

        public void UpdateStatementStatus(Guid billingStatementId, UpdateStatementStatusDto status)
        {
            _billingStatementService.UpdateStatementStatus(billingStatementId, Mappers.BillingStatementMapper.UpdateStatementStatusDtoToVo(status));
        }

        public BillingStatementPdfDto GetStatementPdfData(Guid billingStatementId)
        {
            return Mappers.BillingStatementMapper.StatementPdfVoToDto(_billingStatementService.GetStatementPdfData(billingStatementId));
        }
    }
}
