

using api.Models.Dto;

namespace api.Adapters
{
    public interface IOtherRevenueServiceAdapter
    {
        OtherRevenueDto? GetOtherRevenue(Guid siteId, string period);
        void SaveOtherRevenueData(OtherRevenueDto otherRevenue);
    }
}
