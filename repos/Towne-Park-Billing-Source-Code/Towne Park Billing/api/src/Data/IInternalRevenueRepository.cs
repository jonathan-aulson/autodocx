using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TownePark.Models.Vo;
using api.Models.Vo;

namespace TownePark.Data
{
    public interface IInternalRevenueRepository
    {
        Task<List<InternalRevenueDataVo>> GetInternalRevenueDataAsync(IEnumerable<string> siteNumbers, int year);
        Task<InternalRevenueActualsVo?> GetInternalRevenueActualsAsync(string siteId, int year, int month);
        Task<InternalRevenueActualsMultiSiteVo?> GetInternalRevenueActualsMultiSiteAsync(string siteIds, int year, int month);
    }
}
