using Newtonsoft.Json;

namespace api.Models.Dto
{
    public class UnitAccountDto
    {
        [JsonProperty("date")]
        public string? Date { get; set; }
    }
}
