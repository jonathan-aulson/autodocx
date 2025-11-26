using System;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace api.Models.Dto
{
    public class JobCodeDto
    {
        [JsonPropertyName("jobCodeId")]
        public Guid JobCodeId { get; set; } = Guid.Empty;

        [JsonPropertyName("jobCode")]
        public string JobCode { get; set; } = string.Empty;

        [JsonPropertyName("jobTitle")]
        public string JobTitle { get; set; } = string.Empty;

        [JsonPropertyName("jobGroupId")]
        public string JobGroupId { get; set; } = string.Empty;

        [JsonPropertyName("jobGroupName")]
        public string JobGroupName { get; set; } = "unassigned";

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = false;

        [JsonPropertyName("activeEmployeeCount")]
        public decimal ActiveEmployeeCount { get; set; }

        [JsonPropertyName("allocatedSalaryCost")]
        public decimal? AllocatedSalaryCost { get; set; }

        [JsonPropertyName("averageHourlyRate")]
        public decimal? AverageHourlyRate { get; set; }
    }

    public class EditJobCodeTitleRequestDto
    {
        [JsonPropertyName("jobCodeId")]
        public Guid JobCodeId { get; set; } = Guid.Empty;

        [JsonPropertyName("newTitle")]
        public string NewTitle { get; set; } = string.Empty;
    }

    public class EditJobCodeTitleResponseDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? OldTitle { get; set; }
        public string? NewTitle { get; set; }
    }

    public class AssignJobCodesToGroupRequestDto
    {
        [JsonPropertyName("jobCodeIds")]
        public List<Guid> JobCodeIds { get; set; } = new List<Guid>();

        [JsonPropertyName("targetGroupId")]
        public Guid TargetGroupId { get; set; } = Guid.Empty;
    }

    public class AssignJobCodesToGroupResponseDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int ProcessedCount { get; set; }
        public List<JobCodeAssignmentDto> Results { get; set; } = new List<JobCodeAssignmentDto>();
    }

    public class JobCodeAssignmentDto
    {
        public Guid JobCodeId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public Guid? PreviousGroupId { get; set; }
        public Guid NewGroupId { get; set; }
    }

    public class UpdateJobCodeStatusRequestDto
    {
        [JsonPropertyName("jobCodeIds")]
        public List<Guid> JobCodeIds { get; set; } = new List<Guid>();

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }
    }

    public class UpdateJobCodeStatusResponseDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public int ProcessedCount { get; set; }
        public List<JobCodeStatusUpdateResultDto> Results { get; set; } = new List<JobCodeStatusUpdateResultDto>();
    }

    public class JobCodeStatusUpdateResultDto
    {
        public Guid JobCodeId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public bool PreviousStatus { get; set; }
        public bool NewStatus { get; set; }
    }
}
