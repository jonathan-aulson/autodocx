using System.Collections.Generic;
using TownePark.Models.Vo;
using api.Models.Dto;

namespace api.Services.Impl.Calculators
{
    public interface IExternalRevenueCalculator
    {
        string TargetColumnName { get; } // Will be "ExternalRevenue"
        // Methods will operate on DTOs relevant to external revenue,
        // which might be the same as IInternalRevenueCalculator for now,
        // but allows for future divergence if needed.
        void CalculateAndApply(InternalRevenueDataVo siteData, int year, int monthOneBased, MonthValueDto monthValueDto, SiteMonthlyRevenueDetailDto siteDetailDto);
        void AggregateMonthlyTotals(List<SiteMonthlyRevenueDetailDto> siteDetailsForMonth, MonthValueDto monthValueDto);
    }
}
