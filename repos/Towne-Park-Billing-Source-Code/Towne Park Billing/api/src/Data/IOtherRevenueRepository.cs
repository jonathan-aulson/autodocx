using TownePark;

namespace api.Data
{
    public interface IOtherRevenueRepository
    {
        IEnumerable<bs_OtherRevenueDetail>? GetOtherRevenueDetail(Guid siteId, string startingMonth);
        void UpdateOtherRevenueDetails(List<bs_OtherRevenueDetail> details);
    }
}
