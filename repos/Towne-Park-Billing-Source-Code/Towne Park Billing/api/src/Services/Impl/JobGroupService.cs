using api.Data;
using api.Models.Vo;
using System.Collections.Generic;

namespace api.Services.Impl
{
    public class JobGroupService : IJobGroupService
    {
        private readonly IJobGroupRepository _jobGroupRepository;

        public JobGroupService(IJobGroupRepository jobGroupRepository)
        {
            _jobGroupRepository = jobGroupRepository;
        }

        public void CreateJobGroup(string groupTitle)
        {
            _jobGroupRepository.CreateJobGroup(groupTitle);
        }

        public void DeactivateJobGroup(Guid jobGroupId)
        {
            _jobGroupRepository.DeactivateJobGroup(jobGroupId);
        }

        public void ActivateJobGroup(Guid jobGroupId)
        {
            _jobGroupRepository.ActivateJobGroup(jobGroupId);
        }

        public IEnumerable<JobGroupVo> GetAllJobGroups()
        {
            return _jobGroupRepository.GetAllJobGroups();
        }
    }
}
