using System.Text.Json.Serialization;

namespace TownePark.Billing.Api.Models.Dto
{
    public class DailyActualDto
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