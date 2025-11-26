using api.Models.Vo;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace api.Data
{
    public interface IJobCodeRepository
    {
        Task<IList<JobCodeVo>> GetJobCodesAsync();
        Task<IList<JobCodeVo>> GetJobCodesBySiteAsync(Guid siteId);
        Task<(bool Success, string? OldTitle)> UpdateJobCodeTitleAsync(Guid jobCodeId, string newTitle);
        
        /// <summary>
        /// Assigns multiple job codes to a target job group
        /// </summary>
        /// <param name="jobCodeIds">List of job code IDs to assign</param>
        /// <param name="targetGroupId">Target job group ID</param>
        /// <returns>Success status, error message if any, and count of processed items</returns>
        Task<(bool Success, string? ErrorMessage, int ProcessedCount, List<(Guid JobCodeId, bool Success, string? ErrorMessage, Guid? PreviousGroupId)> Results)> AssignJobCodesToGroupAsync(List<Guid> jobCodeIds, Guid targetGroupId);
        
        /// <summary>
        /// Validates that all specified job codes exist and are active
        /// </summary>
        /// <param name="jobCodeIds">List of job code IDs to validate</param>
        /// <returns>Validation result with details about invalid job codes</returns>
        Task<(bool AllValid, List<Guid> InvalidJobCodeIds)> ValidateJobCodesExistAndActiveAsync(List<Guid> jobCodeIds);
        
        /// <summary>
        /// Validates that the specified job group exists and is active
        /// </summary>
        /// <param name="jobGroupId">Job group ID to validate</param>
        /// <returns>True if job group exists and is active</returns>
        Task<bool> ValidateJobGroupExistsAndActiveAsync(Guid jobGroupId);

        /// <summary>
        /// Updates the status (active/inactive) of multiple job codes
        /// </summary>
        /// <param name="jobCodeIds">List of job code IDs to update</param>
        /// <param name="isActive">Desired status (true for active, false for inactive)</param>
        /// <returns>Success status, error message if any, and detailed results for each job code</returns>
        Task<(bool Success, string? ErrorMessage, int ProcessedCount, List<(Guid JobCodeId, bool Success, string? ErrorMessage, bool PreviousStatus, string JobTitle)> Results)> UpdateJobCodeStatusAsync(List<Guid> jobCodeIds, bool isActive);

        /// <summary>
        /// Gets a job code by its ID.
        /// </summary>
        /// <param name="jobCodeId">Job code ID</param>
        /// <returns>Job code VO or null</returns>
        Task<JobCodeVo?> GetJobCodeByIdAsync(Guid jobCodeId);
    }
}
