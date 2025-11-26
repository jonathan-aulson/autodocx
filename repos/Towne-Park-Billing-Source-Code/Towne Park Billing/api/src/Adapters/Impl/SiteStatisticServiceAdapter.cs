using api.Adapters.Mappers;
using api.Models.Dto;
using api.Services;

namespace api.Adapters.Impl
{
    public class SiteStatisticServiceAdapter : ISiteStatisticServiceAdapter
    {
        private readonly ISiteStatisticService _siteStatisticService;
        public SiteStatisticServiceAdapter(ISiteStatisticService siteStatisticService)
        {
            _siteStatisticService = siteStatisticService;
        }

        public IEnumerable<SiteStatisticDto>? GetSiteStatistics(Guid siteNumber, string billingPeriod, string timeRange)
        {
            return SiteStatisticMapper.SiteStatisticVoToDto(_siteStatisticService.GetSiteStatistics(siteNumber, billingPeriod, timeRange).Result.ToList());
        }

        public void SaveSiteStatistics(SiteStatisticDto updates)
        {
            _siteStatisticService.SaveSiteStatistics(SiteStatisticMapper.SiteStatisticDtoToVo(updates));
        }
    }
}
