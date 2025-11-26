using System.Data;
using TownePark.Billing.Api.Helpers;
using TownePark.Billing.Api.Models.Dto;

namespace TownePark.Billing.Api.Adapters.Mappers
{
    public static class InternalRevenueActualsMapper
    {
        public static InternalRevenueActualsResponseDto MapToInternalRevenueActualsResponse(
            List<Dictionary<string, object>> results, 
            string siteId, 
            int year, 
            int month)
        {
            var dailyActuals = new List<DailyActualDto>();
            string? lastActualizedDate = null;

            foreach (var row in results)
            {
                var dailyActual = new DailyActualDto
                {
                    Date = row.GetValue<DateTime>("Date").ToString("yyyy-MM-dd"),
                    ExternalRevenue = row.GetValue<decimal>("ExternalRevenue"),
                    OccupiedRooms = row.GetValue<int>("OccupiedRooms"),
                    PayrollHours = row.GetValue<decimal>("PayrollHours"),
                    PayrollCost = row.GetValue<decimal>("PayrollCost"),
                    Claims = row.GetValue<decimal>("Claims")
                };

                dailyActuals.Add(dailyActual);
                lastActualizedDate = dailyActual.Date;
            }

            return new InternalRevenueActualsResponseDto
            {
                SiteId = siteId,
                Year = year,
                Month = month,
                DailyActuals = dailyActuals.OrderBy(x => x.Date).ToList(),
                LastActualizedDate = lastActualizedDate,
                TotalActualDays = dailyActuals.Count
            };
        }

        public static InternalRevenueActualsMultiSiteResponseDto MapToInternalRevenueActualsResponseForMultipleSites(
            List<Dictionary<string, object>> results, 
            int year, 
            int month)
        {
            var siteGroups = results.GroupBy(r => r.GetValue<string>("SITE")).ToList();
            var siteResults = new List<InternalRevenueActualsResponseDto>();

            foreach (var siteGroup in siteGroups)
            {
                var siteId = siteGroup.Key;
                var siteData = siteGroup.ToList();
                var dailyActuals = new List<DailyActualDto>();
                string? lastActualizedDate = null;

                foreach (var row in siteData)
                {
                    var dailyActual = new DailyActualDto
                    {
                        Date = row.GetValue<DateTime>("Date").ToString("yyyy-MM-dd"),
                        ExternalRevenue = row.GetValue<decimal>("ExternalRevenue"),
                        OccupiedRooms = row.GetValue<int>("OccupiedRooms"),
                        PayrollHours = row.GetValue<decimal>("PayrollHours"),
                        PayrollCost = row.GetValue<decimal>("PayrollCost"),
                        Claims = row.GetValue<decimal>("Claims")
                    };

                    dailyActuals.Add(dailyActual);
                    lastActualizedDate = dailyActual.Date;
                }

                var siteResult = new InternalRevenueActualsResponseDto
                {
                    SiteId = siteId,
                    Year = year,
                    Month = month,
                    DailyActuals = dailyActuals.OrderBy(x => x.Date).ToList(),
                    LastActualizedDate = lastActualizedDate,
                    TotalActualDays = dailyActuals.Count
                };

                siteResults.Add(siteResult);
            }

            return new InternalRevenueActualsMultiSiteResponseDto
            {
                Year = year,
                Month = month,
                SiteResults = siteResults.OrderBy(x => x.SiteId).ToList(),
                TotalSites = siteResults.Count
            };
        }
    }
}