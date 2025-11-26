using Newtonsoft.Json;

namespace api.Models.Dto
{
    public class BillingStatementIdsRequestDto
    {
        [JsonProperty("statementIds")]
        public List<Guid>? StatementIds { get; set; }
    }
}
