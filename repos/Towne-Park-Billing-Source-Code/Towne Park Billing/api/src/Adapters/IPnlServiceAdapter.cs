using System.Collections.Generic;
using System.Threading.Tasks;
using api.Models.Dto;

namespace api.Adapters
{
    public interface IPnlServiceAdapter
    {
        Task<PnlResponseDto> GetPnlDataAsync(List<string> siteIds, int year);
    }
} 