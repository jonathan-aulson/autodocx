using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TownePark.Billing.Api.Models.Dto
{
    public class PnlBySiteVo
    {
        [JsonPropertyName("siteNumber")]
        public string SiteNumber { get; set; } = string.Empty;

        [JsonPropertyName("pnl")]
        public PnlVo Pnl { get; set; } = new PnlVo();
    }

    public class PnlBySiteListVo
    {
        [JsonPropertyName("pnlBySite")]
        public List<PnlBySiteVo> PnlBySite { get; set; } = new List<PnlBySiteVo>();
    }

    public class PnlVo
    {
        [JsonPropertyName("actual")]
        public List<PnlMonthDetailVo> Actual { get; set; } = new List<PnlMonthDetailVo>();
        [JsonPropertyName("budget")]
        public List<PnlMonthDetailVo> Budget { get; set; } = new List<PnlMonthDetailVo>();
    }

    public class PnlMonthDetailVo
    {
        [JsonPropertyName("siteNumber")]
        public string SiteNumber { get; set; } = string.Empty;
        [JsonPropertyName("MonthNum")]
        public int MonthNum { get; set; }
        [JsonPropertyName("PERIOD")]
        public string Period { get; set; } = string.Empty;
        [JsonPropertyName("EXTERNAL_REVENUE")]
        public decimal ExternalRevenue { get; set; }
        [JsonPropertyName("EXTERNAL_REVENUE_LAST_DATE")]
        public DateTime? ExternalRevenueLastDate { get; set; }
        [JsonPropertyName("INTERNAL_REVENUE")]
        public decimal InternalRevenue { get; set; }
        [JsonPropertyName("PAYROLL")]
        public decimal Payroll { get; set; }
        [JsonPropertyName("PAYROLL_LAST_DATE")]
        public DateTime? PayrollLastDate { get; set; }
        [JsonPropertyName("CLAIMS")]
        public decimal Claims { get; set; }
        [JsonPropertyName("PARKING_RENTS")]
        public decimal ParkingRents { get; set; }
        [JsonPropertyName("OTHER_EXPENSE")]
        public decimal OtherExpense { get; set; }
        [JsonPropertyName("PTEB")]
        public decimal Pteb { get; set; }
        [JsonPropertyName("INSURANCE")]
        public decimal Insurance { get; set; }
        [JsonPropertyName("OCCUPIED_ROOMS")]
        public decimal OccupiedRooms { get; set; }
        [JsonPropertyName("TYPE")]
        public string Type { get; set; } = string.Empty;
    }
}
