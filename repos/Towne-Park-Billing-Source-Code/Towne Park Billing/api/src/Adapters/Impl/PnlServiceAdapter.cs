using System.Collections.Generic;
using System.Threading.Tasks;
using api.Models.Dto;
using api.Services;
using api.Adapters.Mappers;
using api.Models.Vo;
using System;
using System.Linq;


namespace api.Adapters.Impl
{
    public class PnlServiceAdapter : IPnlServiceAdapter
    {
        private readonly ISiteStatisticService _siteStatisticService;
        public PnlServiceAdapter(ISiteStatisticService siteStatisticService)
        {

            _siteStatisticService = siteStatisticService ?? throw new ArgumentNullException(nameof(siteStatisticService));
        }

        public async Task<PnlResponseDto> GetPnlDataAsync(List<string> siteIds, int year)
        {
            if (siteIds == null || !siteIds.Any())
            {
                return new PnlResponseDto();
            }

            var yearResult = await ProcessYearDataAsync(siteIds, year);

            var response = PnlMapper.PnlVoListToDto(new List<(int Year, PnlBySiteListVo Vo)> { yearResult.PnlVoData });

            return response;
        }

        private async Task<(int Year, (int Year, PnlBySiteListVo Vo) PnlVoData, List<PnlRowDto>? ForecastRows)> ProcessYearDataAsync(List<string> siteIds, int year)
        {
            var pnlVo = await _siteStatisticService.GetPNLData(siteIds, year);
            return (year, (year, pnlVo), null);

        }
    }
}