using System.Text.Json.Serialization;

namespace api.Models.Vo
{
    public class DailyActualVo
    {
        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("externalRevenue")]
        public decimal ExternalRevenue { get; set; }

        [JsonPropertyName("occupiedRooms")]
        public int OccupiedRooms { get; set; }

        [JsonPropertyName("payrollHours")]
        public decimal PayrollHours { get; set; }

        [JsonPropertyName("payrollCost")]
        public decimal PayrollCost { get; set; }

        [JsonPropertyName("claims")]
        public decimal Claims { get; set; }
    }
}