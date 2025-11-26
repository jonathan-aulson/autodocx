using api.Models.Dto;
using api.Models.Vo;

namespace api.Services
{
    public interface IBillingStatementService
    {
        IEnumerable<BillingStatementVo> GetBillingStatements(Guid customerSiteId);
        IEnumerable<BillingStatementVo> GetCurrentBillingStatements(UserDto userAuth);
        void UpdateStatementStatus(Guid billingStatementId, UpdateStatementStatusVo status);
        BillingStatementPdfVo GetStatementPdfData(Guid billingStatementId);
    }
}
