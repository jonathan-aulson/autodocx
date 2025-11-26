using Microsoft.Xrm.Sdk;
using TownePark;

namespace api.Data
{
    public interface IBillingStatementRepository
    {
        IEnumerable<bs_BillingStatement> GetBillingStatementsByCustomerSite(Guid customerSiteId);
        IEnumerable<bs_BillingStatement> GetCurrentBillingStatements();
        void UpdateStatementStatus(Guid billingStatementId, bs_BillingStatement status);
        bs_BillingStatement GetBillingStatementById(Guid billingStatementId);
        IEnumerable<bs_BillingStatement> GetBillingStatementsByIds(IEnumerable<Guid> billingStatementIds);
        void UpdateForecastData(Guid billingStatementId, string forecastData);
        IEnumerable<Guid> GetBillingStatementIdsByCustomerSite(IEnumerable<Guid> customerSiteIds);
    }
}
