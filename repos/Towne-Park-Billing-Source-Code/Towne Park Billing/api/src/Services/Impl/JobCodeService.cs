using api.Data;
using api.Models.Vo;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace api.Services.Impl
{
    public class JobCodeService : IJobCodeService
    {
        private readonly IJobCodeRepository _jobCodeRepository;
        private readonly IJobGroupRepository _jobGroupRepository;

        public JobCodeService(IJobCodeRepository jobCodeRepository, IJobGroupRepository jobGroupRepository)
        {
            _jobCodeRepository = jobCodeRepository;
            _jobGroupRepository = jobGroupRepository;
        }

        public async Task<IList<JobCodeVo>> GetJobCodesAsync()
        {
            return await _jobCodeRepository.GetJobCodesAsync();
        }

        public async Task<IList<JobCodeVo>> GetJobCodesBySiteAsync(System.Guid siteId)
        {
            if (siteId == System.Guid.Empty)
            {
                throw new System.ArgumentException("Site ID cannot be empty.", nameof(siteId));
            }

            return await _jobCodeRepository.GetJobCodesBySiteAsync(siteId);
        }

        public async Task<(bool Success, string? ErrorMessage, string? OldTitle)> EditJobCodeTitleAsync(System.Guid jobCodeId, string newTitle)
        {
            if (string.IsNullOrWhiteSpace(newTitle))
                return (false, "Title cannot be empty.", null);

            var (success, oldTitle) = await _jobCodeRepository.UpdateJobCodeTitleAsync(jobCodeId, newTitle);
            if (!success)
                return (false, "Job code not found or inactive.", null);

            return (true, null, oldTitle);
        }

        public async Task<(bool Success, string? ErrorMessage, int ProcessedCount, List<(System.Guid JobCodeId, bool Success, string? ErrorMessage, System.Guid? PreviousGroupId)> Results)> AssignJobCodesToGroupAsync(List<System.Guid> jobCodeIds, System.Guid targetGroupId)
        {
            // Business logic validation
            if (jobCodeIds == null || !jobCodeIds.Any())
            {
                return (false, "At least one job code must be specified for assignment.", 0, new List<(System.Guid, bool, string?, System.Guid?)>());
            }

            // Remove duplicates to ensure each job code is processed only once
            var uniqueJobCodeIds = jobCodeIds.Distinct().ToList();
            
            if (uniqueJobCodeIds.Count != jobCodeIds.Count)
            {
                // Log that duplicates were removed, but continue processing
            }

            if (uniqueJobCodeIds.Count > 100)
            {
                return (false, "Maximum of 100 unique job codes can be assigned in a single request.", 0, new List<(System.Guid, bool, string?, System.Guid?)>());
            }

            if (targetGroupId == System.Guid.Empty)
            {
                return (false, "Target job group ID is required.", 0, new List<(System.Guid, bool, string?, System.Guid?)>());
            }

            // Delegate to repository for data access
            return await _jobCodeRepository.AssignJobCodesToGroupAsync(uniqueJobCodeIds, targetGroupId);
        }

        public async Task<(bool Success, string? ErrorMessage, int ProcessedCount, List<(System.Guid JobCodeId, bool Success, string? ErrorMessage, bool PreviousStatus, string JobTitle)> Results)> UpdateJobCodeStatusAsync(List<System.Guid> jobCodeIds, bool isActive)
        {
            // Business logic validation
            if (jobCodeIds == null || !jobCodeIds.Any())
            {
                return (false, "At least one job code must be specified for status update.", 0, new List<(System.Guid, bool, string?, bool, string)>());
            }

            if (jobCodeIds.Count > 100)
            {
                return (false, "Maximum of 100 job codes can be updated in a single request.", 0, new List<(System.Guid, bool, string?, bool, string)>());
            }

            // Check for duplicate job code IDs
            var duplicates = jobCodeIds
                .GroupBy(id => id)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicates.Any())
            {
                return (false, $"Duplicate job code IDs found: {string.Join(", ", duplicates)}", 0, new List<(System.Guid, bool, string?, bool, string)>());
            }

            // Delegate to repository
            var updateResult = await _jobCodeRepository.UpdateJobCodeStatusAsync(jobCodeIds, isActive);

            // If activating, update parent job group(s) to active
            if (isActive)
            {
                // Get affected job codes to find their group IDs
                foreach (var jobCodeId in jobCodeIds)
                {
                    var jobCode = await _jobCodeRepository.GetJobCodeByIdAsync(jobCodeId);
                    if (jobCode != null && Guid.TryParse(jobCode.JobGroupId, out var groupId) && groupId != Guid.Empty)
                    {
                        _jobGroupRepository.ActivateJobGroup(groupId);
                    }
                }
            }

            return updateResult;
        }
    }
}
