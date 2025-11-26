using System.Text.Json.Serialization;

namespace TownePark.Billing.Api.Models.Dto
{
    public class InternalRevenueActualsResponseDto
    {
        [JsonPropertyName("siteId")]
        public string SiteId { get; set; } = string.Empty;

        [JsonPropertyName("year")]
        public int Year { get; set; }

        [JsonPropertyName("month")]
        public int Month { get; set; }

        [JsonPropertyName("dailyActuals")]
        public List<DailyActualDto> DailyActuals { get; set; } = new();

        [JsonPropertyName("lastActualizedDate")]
        public string? LastActualizedDate { get; set; }

        [JsonPropertyName("totalActualDays")]
        public int TotalActualDays { get; set; }
    }

    public class InternalRevenueActualsMultiSiteResponseDto
    {
        [JsonPropertyName("year")]
        public int Year { get; set; }

        [JsonPropertyName("month")]
        public int Month { get; set; }

        [JsonPropertyName("siteResults")]
        public List<InternalRevenueActualsResponseDto> SiteResults { get; set; } = new();

        [JsonPropertyName("totalSites")]
        public int TotalSites { get; set; }
    }
}