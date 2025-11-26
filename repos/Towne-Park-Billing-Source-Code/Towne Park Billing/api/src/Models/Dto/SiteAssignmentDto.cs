using System.Text.Json.Serialization;

namespace api.Models.Dto
{
    public class SiteAssignmentDto
    {
        [JsonPropertyName("siteId")]
        public Guid SiteId { get; set; }
        
        [JsonPropertyName("siteNumber")]
        public string SiteNumber { get; set; } = string.Empty;
        
        [JsonPropertyName("siteName")]
        public string SiteName { get; set; } = string.Empty;
        
        [JsonPropertyName("city")]
        public string City { get; set; } = string.Empty;
        
        [JsonPropertyName("assignedJobGroups")]
        public List<JobGroupAssignmentDto> AssignedJobGroups { get; set; } = new();
        
        [JsonPropertyName("jobGroupCount")]
        public int JobGroupCount { get; set; }
        
        [JsonPropertyName("hasUnassignedJobCodes")]
        public bool HasUnassignedJobCodes { get; set; }
    }

    public class JobGroupAssignmentDto
    {
        [JsonPropertyName("jobGroupId")]
        public Guid JobGroupId { get; set; }
        
        [JsonPropertyName("jobGroupName")]
        public string JobGroupName { get; set; } = string.Empty;
        
        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }
    }

    public class GetSiteAssignmentsResponseDto
    {
        [JsonPropertyName("siteAssignments")]
        public List<SiteAssignmentDto> SiteAssignments { get; set; } = new();
        
        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }
        
        [JsonPropertyName("success")]
        public bool Success { get; set; } = true;
        
        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }
    }
} 