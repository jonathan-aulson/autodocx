using api.Adapters.Mappers;
using api.Data;
using api.Models.Vo;
using TownePark;

namespace api.Services.Impl
{
    public class OtherRevenueService : IOtherRevenueService
    {
        private readonly IOtherRevenueRepository _otherRevenueRepository;

        public OtherRevenueService(IOtherRevenueRepository otherRevenueRepository)
        {
            _otherRevenueRepository = otherRevenueRepository;
        }

        public OtherRevenueVo? GetOtherRevenueData(Guid siteId, string period)
        {
            var model = _otherRevenueRepository.GetOtherRevenueDetail(siteId, period) ?? new List<bs_OtherRevenueDetail>();

            var vo = OtherRevenueMapper.OtherRevenueModelToVo(model);
            var otherRevenue = new OtherRevenueVo();

            otherRevenue.CustomerSiteId = siteId;
            otherRevenue.BillingPeriod = period;
            if (vo != null)
            {
                otherRevenue.ForecastData = vo.ToList();
            }

            return otherRevenue;
        }

        public void SaveOtherRevenueData(OtherRevenueVo otherRevenue)
        {
            // Set Type to Forecast for all forecast details
            if (otherRevenue.ForecastData != null)
            {
                foreach (var detail in otherRevenue.ForecastData)
                {
                    detail.Type = OtherRevenueType.Forecast;
                }
            }

            List<bs_OtherRevenueDetail> model = OtherRevenueMapper.OtherRevenueVoToModel(otherRevenue);

            _otherRevenueRepository.UpdateOtherRevenueDetails(model);
        }
    }
}
