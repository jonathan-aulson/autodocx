using System.Collections.Generic;
using api.Models.Vo;

namespace api.Adapters
{
    public interface IJobGroupServiceAdapter
    {
        void CreateJobGroup(string groupTitle);
        void DeactivateJobGroup(Guid jobGroupId);
        void ActivateJobGroup(Guid jobGroupId);
        IEnumerable<JobGroupVo> GetAllJobGroups();
    }
}
