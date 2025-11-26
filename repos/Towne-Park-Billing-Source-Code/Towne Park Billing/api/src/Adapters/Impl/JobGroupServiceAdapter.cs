using api.Services;
using api.Adapters.Mappers;
using System.Collections.Generic;
using api.Models.Vo;

namespace api.Adapters.Impl
{
    public class JobGroupServiceAdapter : IJobGroupServiceAdapter
    {
        private readonly IJobGroupService _jobGroupService;
        private readonly JobGroupMapper _mapper = new JobGroupMapper();

        public JobGroupServiceAdapter(IJobGroupService jobGroupService)
        {
            _jobGroupService = jobGroupService;
        }

        public void CreateJobGroup(string groupTitle)
        {
            _jobGroupService.CreateJobGroup(groupTitle);
        }

        public void DeactivateJobGroup(Guid jobGroupId)
        {
            _jobGroupService.DeactivateJobGroup(jobGroupId);
        }

        public void ActivateJobGroup(Guid jobGroupId)
        {
            _jobGroupService.ActivateJobGroup(jobGroupId);
        }

        public IEnumerable<JobGroupVo> GetAllJobGroups()
        {
            return _jobGroupService.GetAllJobGroups();
        }
    }
}
