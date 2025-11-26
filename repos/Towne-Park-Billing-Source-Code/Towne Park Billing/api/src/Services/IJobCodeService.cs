using api.Models.Vo;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace api.Services
{
    public interface IJobCodeService
    {
        Task<IList<JobCodeVo>> GetJobCodesAsync();
        Task<IList<JobCodeVo>> GetJobCodesBySiteAsync(System.Guid siteId);
        Task<(bool Success, string? ErrorMessage, string? OldTitle)> EditJobCodeTitleAsync(System.Guid jobCodeId, string newTitle);
        
        /// <summary>
        /// Assigns multiple job codes to a target job group with business logic validation
        /// </summary>
        /// <param name="jobCodeIds">List of job code IDs to assign</param>
        /// <param name="targetGroupId">Target job group ID</param>
        /// <returns>Assignment result with detailed information</returns>
        Task<(bool Success, string? ErrorMessage, int ProcessedCount, List<(System.Guid JobCodeId, bool Success, string? ErrorMessage, System.Guid? PreviousGroupId)> Results)> AssignJobCodesToGroupAsync(List<System.Guid> jobCodeIds, System.Guid targetGroupId);

        /// <summary>
        /// Updates the status (active/inactive) of multiple job codes with business logic validation
        /// </summary>
        /// <param name="jobCodeIds">List of job code IDs to update</param>
        /// <param name="isActive">Desired status (true for active, false for inactive)</param>
        /// <returns>Update result with detailed information for each job code</returns>
        Task<(bool Success, string? ErrorMessage, int ProcessedCount, List<(System.Guid JobCodeId, bool Success, string? ErrorMessage, bool PreviousStatus, string JobTitle)> Results)> UpdateJobCodeStatusAsync(List<System.Guid> jobCodeIds, bool isActive);
    }
}
