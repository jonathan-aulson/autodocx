using api.Models.Dto;
using api.Models.Vo;
using TownePark;

namespace api.Data
{
    public interface ISiteStatisticRepository
    {
        bs_SiteStatistic? GetSiteStatistics(Guid siteId, string billingPeriod);
        //IEnumerable<bs_SiteStatistic>? GetWeeklySiteStatistics(Guid siteId, string startingMonth);
        IEnumerable<bs_SiteStatistic>? GetMonthlySiteStatistics(Guid siteId, string startingMonth);
        IEnumerable<bs_SiteStatistic>? GetSiteStatisticsByRange(Guid siteId, string startingMonth, int monthCount);
        Task<List<SiteStatisticDetailVo>> GetBudgetData(string siteId, string billingPeriod, int totalRooms);
        Task<List<SiteStatisticDetailVo>> GetBudgetDataForRange(string siteId, List<string> billingPeriods, int totalRooms);
        Task<List<SiteStatisticDetailVo>> GetActualData(string siteId, string billingPeriod);
        Task<List<SiteStatisticDetailVo>> GetActualDataForRange(string siteId, List<string> billingPeriods);
        void SaveSiteStatistics(bs_SiteStatistic updates);
        void CreateSiteStatistics(bs_SiteStatistic model);
        List<bs_SiteStatistic> GetSiteStatisticsBatch(List<string> siteNumbers, List<string> billingPeriods);
        Task<PnlBySiteListVo> GetPNLData(List<string> siteIds, int year);
    }
}
