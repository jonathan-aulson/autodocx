using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownePark.Billing.Api.Helpers;
using TownePark.Billing.Api.Models.Dto;

namespace TownePark.Billing.Api.Adapters.Mappers
{
    public static class PnlMapper
    {
        public static PnlMonthDetailVo MapToPnlMonthDetailVo(this IDictionary<string, object> row)
        {
            return new PnlMonthDetailVo
            {
                SiteNumber = row.GetValue<string>("SiteNumber"),
                MonthNum = row.GetValue<int>("MonthNum"),
                Period = row.GetValue<string>("PERIOD"),
                ExternalRevenue = row.GetValue<decimal>("EXTERNAL_REVENUE"),
                ExternalRevenueLastDate = row.ContainsKey("EXTERNAL_REVENUE_LAST_DATE") && row["EXTERNAL_REVENUE_LAST_DATE"] != null
                    ? row.GetValue<DateTime?>("EXTERNAL_REVENUE_LAST_DATE")
                    : null,
                InternalRevenue = row.GetValue<decimal>("INTERNAL_REVENUE"),
                Payroll = row.GetValue<decimal>("PAYROLL"),
                PayrollLastDate = row.GetValue<DateTime?>("LAST_PAYROLL_DATE"),
                Claims = row.GetValue<decimal>("CLAIMS"),
                ParkingRents = row.GetValue<decimal>("PARKING_RENTS"),
                OtherExpense = row.GetValue<decimal>("OTHER_EXPENSE"),
                Pteb = row.GetValue<decimal>("PTEB"),
                Insurance = row.GetValue<decimal>("INSURANCE"),
                Type = row.GetValue<string>("TYPE"),
                OccupiedRooms = row.GetValue<decimal>("OCCUPIED_ROOMS")
            };
        }

        public static PnlBySiteListVo MapToPnlBySiteVo(List<Dictionary<string, object>> rawResults)
        {
            var details = rawResults.Select(MapToPnlMonthDetailVo).ToList();

            var grouped = details
                .GroupBy(d => d.SiteNumber)
                .Select(g => new PnlBySiteVo
                {
                    SiteNumber = g.Key,
                    Pnl = new PnlVo
                    {
                        Actual = g.Where(d => d.Type.Equals("ACTUAL", StringComparison.OrdinalIgnoreCase)).ToList(),
                        Budget = g.Where(d => d.Type.Equals("BUDGET", StringComparison.OrdinalIgnoreCase)).ToList()
                    }
                })
                .ToList();

            return new PnlBySiteListVo { PnlBySite = grouped };
        }

        public static PnlVo MapToPnlVo(List<Dictionary<string, object>> rawResults)
        {
            var details = rawResults.Select(MapToPnlMonthDetailVo).ToList();

            return new PnlVo
            {
                Actual = details.Where(d => d.Type.Equals("ACTUAL", StringComparison.OrdinalIgnoreCase)).ToList(),
                Budget = details.Where(d => d.Type.Equals("BUDGET", StringComparison.OrdinalIgnoreCase)).ToList()
            };
        }
    }

}
