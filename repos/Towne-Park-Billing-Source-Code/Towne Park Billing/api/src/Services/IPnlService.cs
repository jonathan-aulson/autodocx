using api.Models.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;
using TownePark.Models.Vo; // For InternalRevenueDataVo

namespace api.Services
{
    public interface IPnlService
    {
        Task<PnlResponseDto> GetPnlInternalRevenueDataAsync(List<string> siteIds, int year);
    }
}
