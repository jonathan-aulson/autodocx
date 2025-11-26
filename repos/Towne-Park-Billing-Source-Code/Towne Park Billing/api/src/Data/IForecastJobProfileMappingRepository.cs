using TownePark;

namespace api.Data
{
    public interface IForecastJobProfileMappingRepository
    {
        IEnumerable<bs_ForecastJobProfileMapping> GetForecastJobProfileMappingsByCustomerSite(Guid customerSiteId);
    }
}
