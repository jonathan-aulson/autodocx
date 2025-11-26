using api.Models.Dto;

namespace api.Adapters
{
    public interface ISiteStatisticServiceAdapter
    {
        IEnumerable<SiteStatisticDto>? GetSiteStatistics(Guid siteNumber, string billingPeriod, string timeRange);
        void SaveSiteStatistics(SiteStatisticDto updates);
    }
}
