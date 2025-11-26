using api.Adapters.Mappers;
using api.Models.Dto;
using api.Services;

namespace api.Adapters.Impl
{
    public class OtherRevenueServiceAdapter : IOtherRevenueServiceAdapter
    {
        private readonly IOtherRevenueService _otherRevenueService;

        public OtherRevenueServiceAdapter(IOtherRevenueService otherRevenueService)
        {
            _otherRevenueService = otherRevenueService;
        }

        public OtherRevenueDto? GetOtherRevenue(Guid siteId, string period)
        {
            var otherRevenue = _otherRevenueService.GetOtherRevenueData(siteId, period);
            if (otherRevenue == null)
                return new OtherRevenueDto();

            return OtherRevenueMapper.OtherRevenueVoToDto(otherRevenue);
        }

        public void SaveOtherRevenueData(OtherRevenueDto otherRevenue)
        {
            _otherRevenueService.SaveOtherRevenueData(OtherRevenueMapper.MapOtherRevenueDtoToVo(otherRevenue));
        }
    } 
}
