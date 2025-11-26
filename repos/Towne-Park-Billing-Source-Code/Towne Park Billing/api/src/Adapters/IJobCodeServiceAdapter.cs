using api.Models.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace api.Adapters
{
    public interface IJobCodeServiceAdapter
    {
        Task<IList<JobCodeDto>> GetJobCodesAsync();
        Task<EditJobCodeTitleResponseDto> EditJobCodeTitleAsync(EditJobCodeTitleRequestDto request);
        
        /// <summary>
        /// Gets job codes available for a specific site with configuration details
        /// </summary>
        /// <param name="siteId">The site ID to get job codes for</param>
        /// <returns>List of job codes available for the site</returns>
        Task<IList<JobCodeDto>> GetJobCodesBySiteAsync(Guid siteId);
        
        /// <summary>
        /// Assigns job codes to a job group using DTOs for API communication
        /// </summary>
        /// <param name="request">Assignment request containing job code IDs and target group ID</param>
        /// <returns>Assignment response with detailed results</returns>
        Task<AssignJobCodesToGroupResponseDto> AssignJobCodesToGroupAsync(AssignJobCodesToGroupRequestDto request);

        /// <summary>
        /// Updates the status (active/inactive) of multiple job codes using DTOs for API communication
        /// </summary>
        /// <param name="request">Status update request containing job code IDs and desired status</param>
        /// <returns>Status update response with detailed results</returns>
        Task<UpdateJobCodeStatusResponseDto> UpdateJobCodeStatusAsync(UpdateJobCodeStatusRequestDto request);
    }
}
