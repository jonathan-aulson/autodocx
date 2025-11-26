using System;
using System.Collections.Generic;

namespace api.Models.Vo
{
    public class JobGroupVo
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public bool IsActive { get; set; }
        public List<JobCodeVo> JobCodes { get; set; } = new();
    }
}
