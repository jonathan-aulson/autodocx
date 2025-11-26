using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api.Models.Dto
{
    public class UserAuth
    {
        [JsonProperty("userDetails")]
        public string? userEmailAddress { get; set; }

        [JsonProperty("userRoles")]
        public UserRole[]? userRoles { get; set; }
    }

    public class UserRole
    {
        [JsonProperty("typ")]
        public string? Type { get; set; }

        [JsonProperty("val")]
        public string? Value { get; set; }
    }
}
