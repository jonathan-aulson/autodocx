using System.Text.Json.Serialization;

namespace api.Models.Vo
{
    public class InternalRevenueActualsVo
    {
        [JsonPropertyName("siteId")]
        public string SiteId { get; set; } = string.Empty;

        [JsonPropertyName("year")]
        public int Year { get; set; }

        [JsonPropertyName("month")]
        public int Month { get; set; }

        [JsonPropertyName("dailyActuals")]
        public List<DailyActualVo> DailyActuals { get; set; } = new();

        [JsonPropertyName("lastActualizedDate")]
        public string? LastActualizedDate { get; set; }

        [JsonPropertyName("totalActualDays")]
        public int TotalActualDays { get; set; }
    }

    public class InternalRevenueActualsMultiSiteVo
    {
        [JsonPropertyName("year")]
        public int Year { get; set; }

        [JsonPropertyName("month")]
        public int Month { get; set; }

        [JsonPropertyName("siteResults")]
        public List<InternalRevenueActualsVo> SiteResults { get; set; } = new();

        [JsonPropertyName("totalSites")]
        public int TotalSites { get; set; }
    }
}