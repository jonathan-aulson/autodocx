using api.Models.Dto;

namespace api.Adapters
{
    public interface ISiteAssignmentServiceAdapter
    {
        /// <summary>
        /// Gets all site assignments using DTOs for API communication
        /// </summary>
        /// <returns>Response with site assignments and metadata</returns>
        Task<GetSiteAssignmentsResponseDto> GetSiteAssignmentsAsync();
    }
} 