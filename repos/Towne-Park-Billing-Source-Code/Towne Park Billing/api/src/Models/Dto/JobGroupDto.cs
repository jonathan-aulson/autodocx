using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace api.Models.Dto
{
    public class JobGroupDto
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }

        [JsonPropertyName("jobCodes")]
        public List<JobCodeDto> JobCodes { get; set; } = new List<JobCodeDto>();
    }
}
