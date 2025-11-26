using TownePark.Billing.Api.Helpers;
using TownePark.Billing.Api.Models.Vo;

public static class SiteStatisticDetailMapper
{
    public static SiteStatisticDetailVo MapToSiteStatisticDetailVo(Dictionary<string, object> row)
    {
        var totalExternalRevenue = row.GetValue<decimal>("External Revenue");
        var adjustmentPercentage = row.GetValue<decimal>("Adjustment Percentage");
        var adjustmentValue = totalExternalRevenue * adjustmentPercentage;
        var externalRevenue = totalExternalRevenue + adjustmentValue;

        return new SiteStatisticDetailVo
        {
            Id = Guid.Empty,
            Type = SiteStatisticDetailType.Budget, 

            Date = row.GetValue<DateOnly>("Date"),
            PeriodStart = row.GetValue<DateOnly>("Date"),
            PeriodEnd = row.GetValue<DateOnly>("Date"),

            ExternalRevenue = row.GetValue<decimal>("External Revenue"),
            OccupiedRooms = row.GetValue<decimal>("Occupied Rooms"),
            ValetDaily = row.GetValue<decimal>("Valet Daily"),
            ValetOvernight = row.GetValue<decimal>("Valet Overnight"),
            ValetMonthly = row.GetValue<decimal>("Valet Monthly"),
            ValetComps = row.GetValue<decimal>("Valet Comps"),
            SelfDaily = row.GetValue<decimal>("Self Daily"),
            SelfOvernight = row.GetValue<decimal>("Self Overnight"),
            SelfMonthly = row.GetValue<decimal>("Self Monthly"),
            SelfComps = row.GetValue<decimal>("Self Comps"),

            ValetRateDaily = row.GetValue<decimal>("Valet - Daily Rate"),
            ValetRateMonthly = row.GetValue<decimal>("Valet - Monthly Rate"),
            ValetRateOvernight = row.GetValue<decimal>("Valet - Overnight Rate"),
            SelfRateDaily = row.GetValue<decimal>("Self - Daily Rate"),
            SelfRateMonthly = row.GetValue<decimal>("Self - Monthly Rate"),
            SelfRateOvernight = row.GetValue<decimal>("Self - Overnight Rate"),

            AdjustmentPercentage = adjustmentPercentage,
            AdjustmentValue = adjustmentValue,

            BaseRevenue = 0,
            Occupancy = 0,
            SelfAggregator = 0,
            ValetAggregator = 0,
            DriveInRatio = 0,
            CaptureRatio = 0
        };
    }
}
