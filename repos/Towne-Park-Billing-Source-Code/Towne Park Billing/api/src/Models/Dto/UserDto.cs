using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api.Models.Dto
{
    public class UserDto
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("systemUserId")]
        public string? SystemUserId { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("firstName")]
        public string? FirstName { get; set; }

        [JsonProperty("lastName")]
        public string? LastName { get; set; }

        [JsonProperty("roles")]
        public string[]? Roles { get; set; }

        [JsonProperty("email")]
        public string? Email { get; set; }
    }
}
