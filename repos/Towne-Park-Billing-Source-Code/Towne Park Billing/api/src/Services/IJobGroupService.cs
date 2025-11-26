using System.Collections.Generic;
using api.Models.Vo;

namespace api.Services
{
    public interface IJobGroupService
    {
        /// <summary>
        /// Creates a new job group with the specified title.
        /// </summary>
        /// <param name="groupTitle">The title of the job group.</param>
        void CreateJobGroup(string groupTitle);
        /// <summary>
        /// Deactivates an existing job group by its ID.
        /// </summary>
        /// <param name="jobGroupId">The ID of the job group to deactivate.</param>
        void DeactivateJobGroup(Guid jobGroupId);

        /// <summary>
        /// Activates an existing job group by its ID.
        /// </summary>
        /// <param name="jobGroupId">The ID of the job group to activate.</param>
        void ActivateJobGroup(Guid jobGroupId);

        /// <summary>
        /// Gets all job groups.
        /// </summary>
        /// <returns>A list of job group VOs.</returns>
        IEnumerable<JobGroupVo> GetAllJobGroups();
    }
}
