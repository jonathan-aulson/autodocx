using TownePark;
using Newtonsoft.Json;

namespace api.Models.Dto
{
    public class EmailTaskRequestDto
    {
        [JsonProperty("sendAction")]
        public bs_sendactionchoices? SendAction { get; set; }
    }
}
