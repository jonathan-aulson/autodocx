using Newtonsoft.Json;

namespace api.Models.Dto;

public class CustomerIdsRequestDto
{
    [JsonProperty("customerSiteIds")]
    public List<Guid>? CustomerSiteIds { get; set; }
}