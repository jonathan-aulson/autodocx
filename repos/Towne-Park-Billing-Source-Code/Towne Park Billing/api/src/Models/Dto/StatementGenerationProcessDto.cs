using Newtonsoft.Json;

namespace api.Models.Dto;

public class StatementGenerationProcessDto
{
    [JsonProperty("contractId")]
    public Guid? ContractId { get; set; }
}