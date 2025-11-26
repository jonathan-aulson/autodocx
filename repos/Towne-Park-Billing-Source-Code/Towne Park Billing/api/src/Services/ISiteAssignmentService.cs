using api.Models.Vo;

namespace api.Services
{
    public interface ISiteAssignmentService
    {
        /// <summary>
        /// Gets all site assignments with calculated job group assignments
        /// </summary>
        /// <returns>List of site assignments</returns>
        Task<IList<SiteAssignmentVo>> GetSiteAssignmentsAsync();
    }
} 