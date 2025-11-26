using TownePark;

namespace api.Data;

public interface ICustomerRepository
{
    IEnumerable<bs_CustomerSite> GetCustomersSummary();

    bs_CustomerSite GetCustomerDetail(Guid customerSiteId);

    void UpdateCustomerDetail(Guid customerSiteId, bs_CustomerSite model);

    Guid AddCustomer(bs_CustomerSite customerSite);
    bs_MasterCustomerSite GetMasterCustomer(string siteNumber);
    IEnumerable<Guid> GetSiteIdsByUser(string userEmailAddress);
    IEnumerable<bs_CustomerSite> GetSitesByUser(IEnumerable<Guid> customerSiteIds, string email);
    Guid GetIdBySiteNumber(string siteNumber);
}