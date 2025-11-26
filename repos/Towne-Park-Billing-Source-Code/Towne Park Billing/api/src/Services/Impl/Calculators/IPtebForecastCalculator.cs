using System.Collections.Generic;
using api.Models.Dto;
using api.Models.Vo;
using TownePark.Models.Vo;

namespace api.Services.Impl.Calculators
{
    public interface IPtebForecastCalculator
    {
        void ComputeForMonth(
            PnlResponseDto pnlResponse,
            List<InternalRevenueDataVo> allSitesRevenueData,
            int targetYear,
            int targetMonthOneBased,
            int targetMonthZeroBased,
            Dictionary<string, decimal> forecastedPayrollBySiteNumber,
            Dictionary<string, decimal> priorYearRateBySiteNumber);
    }
}


