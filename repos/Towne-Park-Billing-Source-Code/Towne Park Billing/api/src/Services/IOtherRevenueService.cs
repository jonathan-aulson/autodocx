using api.Models.Vo;

namespace api.Services
{
    public interface IOtherRevenueService
    {
        OtherRevenueVo? GetOtherRevenueData(Guid siteId, string period);
        void SaveOtherRevenueData(OtherRevenueVo otherRevenue);
    }
}
