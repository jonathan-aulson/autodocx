using System.Collections.Generic;
using api.Models.Vo;

namespace api.Data
{
    public interface IJobGroupRepository
    {
        void CreateJobGroup(string groupTitle);
        void DeactivateJobGroup(Guid jobGroupId);
        void ActivateJobGroup(Guid jobGroupId);
        IEnumerable<JobGroupVo> GetAllJobGroups();
    }
}
