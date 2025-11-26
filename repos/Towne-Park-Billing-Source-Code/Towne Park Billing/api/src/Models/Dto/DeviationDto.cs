using Newtonsoft.Json;

namespace api.Models.Dto
{
    public class DeviationDto
    {
        [JsonProperty("contractId")]
        public Guid? ContractId { get; set; }

        [JsonProperty("deviationAmount")]
        public decimal? DeviationAmount { get; set; }

        [JsonProperty("deviationPercentage")]
        public decimal? DeviationPercentage { get; set; }

        [JsonProperty("customerSiteId")]
        public Guid? CustomerSiteId { get; set; }

        [JsonProperty("siteName")]
        public string? SiteName { get; set; }

        [JsonProperty("siteNumber")]
        public string? SiteNumber { get; set; }
    }
}
