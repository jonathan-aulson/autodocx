using System;

namespace api.Models.Vo
{
    public class JobCodeVo
    {
        public Guid JobCodeId { get; set; }
        public string JobCode { get; set; }
        public string JobTitle { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public string JobGroupId { get; set; }
        public string JobGroupName { get; set; }
        public decimal ActiveEmployeeCount { get; set; }
        public decimal? AllocatedSalaryCost { get; set; }
        public decimal? AverageHourlyRate { get; set; }
    }
}
