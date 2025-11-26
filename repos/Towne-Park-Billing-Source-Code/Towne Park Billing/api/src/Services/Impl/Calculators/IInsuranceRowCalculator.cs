using System.Collections.Generic;
using api.Models.Dto;
using TownePark.Models.Vo;

namespace api.Services.Impl.Calculators
{
    public interface IInsuranceRowCalculator
    {
        void ComputeForMonth(
            PnlResponseDto pnlResponse,
            List<InternalRevenueDataVo> allSitesRevenueData,
            int targetYear,
            int targetMonthOneBased,
            int targetMonthZeroBased,
            Dictionary<string, decimal> forecastedPayrollBySiteNumber);
    }
}


