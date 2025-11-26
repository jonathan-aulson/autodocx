using Newtonsoft.Json;

namespace api.Models.Dto
{
    public class ContractConfigDto
    {
        [JsonProperty("defaultRate")]
        public decimal? DefaultRate { get; set; }

        [JsonProperty("defaultOvertimeRate")]
        public decimal? DefaultOvertimeRate { get; set; }

        [JsonProperty("defaultFee")]
        public decimal? DefaultFee { get; set; }

        [JsonProperty("glCodes")]
        public IEnumerable<GlCodeDto>? GlCodes { get; set; }
    }
    
    public class GlCodeDto
    {
        [JsonProperty("code")]
        public string? Code { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("type")]
        public string? Type { get; set; }
    }
}
