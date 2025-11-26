namespace api.Models.Vo
{
    public class SiteAssignmentVo
    {
        public Guid SiteId { get; set; }
        public string SiteNumber { get; set; } = string.Empty;
        public string SiteName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public List<JobGroupAssignmentVo> AssignedJobGroups { get; set; } = new();
        public int JobGroupCount { get; set; }
        public bool HasUnassignedJobCodes { get; set; }
    }

    public class JobGroupAssignmentVo
    {
        public Guid JobGroupId { get; set; }
        public string JobGroupName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
} 