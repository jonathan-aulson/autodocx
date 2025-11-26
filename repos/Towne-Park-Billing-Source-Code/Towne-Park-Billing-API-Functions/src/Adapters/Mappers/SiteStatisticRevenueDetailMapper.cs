using TownePark.Billing.Api.Helpers;
using TownePark.Billing.Api.Models.Vo;

namespace TownePark.Billing.Api.Adapters.Mappers
{
    public static class SiteStatisticRevenueDetailMapper
    {
        public static List<SiteStatisticDetailVo> MapToSiteStatisticDetailVo(List<Dictionary<string, object>> rows)
        {
            var result = new List<SiteStatisticDetailVo>();
            
            foreach (var row in rows)
            {                
                // Convert DATE to DateOnly
                var dateValue = row["DATE"];
                DateOnly date;
                
                if (dateValue is DateOnly d)
                    date = d;
                else if (dateValue is DateTime dt)
                    date = DateOnly.FromDateTime(dt);
                else
                    DateOnly.TryParse(dateValue.ToString(), out date);
                // Extract vehicle counts
                decimal selfDaily = row.GetValue<decimal>("SelfDaily_Count");
                decimal selfMonthly = row.GetValue<decimal>("SelfMonthly_Count");
                decimal selfOvernight = row.GetValue<decimal>("SelfOvernight_Count");
                decimal valetDaily = row.GetValue<decimal>("ValetDaily_Count");
                decimal valetMonthly = row.GetValue<decimal>("ValetMonthly_Count");
                decimal valetOvernight = row.GetValue<decimal>("ValetOvernight_Count");
                
                // Extract revenues
                decimal selfDailyRevenue = row.GetValue<decimal>("SelfDaily_Revenue");
                decimal selfMonthlyRevenue = row.GetValue<decimal>("SelfMonthly_Revenue");
                decimal selfOvernightRevenue = row.GetValue<decimal>("SelfOvernight_Revenue");
                decimal valetDailyRevenue = row.GetValue<decimal>("ValetDaily_Revenue");
                decimal valetMonthlyRevenue = row.GetValue<decimal>("ValetMonthly_Revenue");
                decimal valetOvernightRevenue = row.GetValue<decimal>("ValetOvernight_Revenue");
                decimal totalRevenue = row.GetValue<decimal>("Total_Revenue");
                
                // Total vehicle count
                decimal totalVehicles = row.GetValue<decimal>("Total_VehicleCount");
                                
                // Calculate rates (if counts > 0)
                decimal selfRateDaily = selfDaily > 0 ? selfDailyRevenue / selfDaily : 0;
                decimal selfRateMonthly = selfMonthly > 0 ? selfMonthlyRevenue / selfMonthly : 0;
                decimal selfRateOvernight = selfOvernight > 0 ? selfOvernightRevenue / selfOvernight : 0;
                decimal valetRateDaily = valetDaily > 0 ? valetDailyRevenue / valetDaily : 0;
                decimal valetRateMonthly = valetMonthly > 0 ? valetMonthlyRevenue / valetMonthly : 0;
                decimal valetRateOvernight = valetOvernight > 0 ? valetOvernightRevenue / valetOvernight : 0;
                
                // Calculate service type totals
                decimal selfAggregator = selfDaily + selfMonthly + selfOvernight;
                decimal valetAggregator = valetDaily + valetMonthly + valetOvernight;

                decimal adjustmentValue = row.GetValue<decimal>("Adjustments_Revenue");
                decimal adjustmentPercentage = totalRevenue == 0 ? 0 : adjustmentValue / totalRevenue;

                // Calculate ratios
                //double driveInRatio = occupiedRooms > 0 ? (double)(totalVehicles / occupiedRooms) : 0;
                //double captureRatio = totalVehicles > 0 ? (double)(valetOvernight / totalVehicles) : 0;

                // Create the SiteStatisticDetailVo
                result.Add(new SiteStatisticDetailVo
                {
                    Id = Guid.Empty,
                    Type = SiteStatisticDetailType.Actual,
                    PeriodStart = date,
                    PeriodEnd = date,
                    PeriodLabel = null,
                    Date = date,

                    // Vehicle counts by category
                    SelfDaily = selfDaily,
                    SelfMonthly = selfMonthly,
                    SelfOvernight = selfOvernight,
                    ValetDaily = valetDaily,
                    ValetMonthly = valetMonthly,
                    ValetOvernight = valetOvernight,

                    // Revenue
                    ExternalRevenue = totalRevenue,
                    ExternalRevenueLastDate = row.ContainsKey("EXTERNAL_REVENUE_LAST_DATE") && row["EXTERNAL_REVENUE_LAST_DATE"] != null
                        ? DateOnly.FromDateTime(row.GetValue<DateTime>("EXTERNAL_REVENUE_LAST_DATE"))
                        : null,

                    // Calculated rates
                    SelfRateDaily = selfRateDaily,
                    SelfRateMonthly = selfRateMonthly,
                    SelfRateOvernight = selfRateOvernight,
                    ValetRateDaily = valetRateDaily,
                    ValetRateMonthly = valetRateMonthly,
                    ValetRateOvernight = valetRateOvernight,

                    // Aggregators
                    SelfAggregator = selfAggregator,
                    ValetAggregator = valetAggregator,

                    // Other statistics
                    OccupiedRooms = row.GetValue<decimal>("OccupiedRooms"),
                    DriveInRatio = 0,
                    CaptureRatio = 0,

                    // Default values for fields not in query
                    ValetComps = row.GetValue<decimal>("ValetComps_Count"),
                    SelfComps = row.GetValue<decimal>("SelfComps_Count"),
                    Occupancy = 0, // Requires TotalRooms to calculate
                    BaseRevenue = 0,

                    AdjustmentPercentage = adjustmentPercentage,
                    AdjustmentValue = adjustmentValue
                });
            }
            
            return result;
        }
    }
}
