using System.Text.Json.Serialization;
using System.Text.Json;

namespace TownePark.Billing.Api.Models.Dto
{
    public class EDWDataRequest
    {
        [JsonPropertyName("storedProcedureId")]
        public int StoredProcedureId { get; set; }

        [JsonPropertyName("storedProcedureParameters")]
        public Dictionary<string, JsonElement>? StoredProcedureParameters { get; set; }
    }
}
