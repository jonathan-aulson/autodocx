using Newtonsoft.Json;

namespace api.Models.Dto;

public class ErrorMessageDto
{
    [JsonProperty("errorCode")]
    public int? ErrorCode { get; set; }
    
    [JsonProperty("errorMessage")]
    public string? ErrorMessage { get; set; }
}