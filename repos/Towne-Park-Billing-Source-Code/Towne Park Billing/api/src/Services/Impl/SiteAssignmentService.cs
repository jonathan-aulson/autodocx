using api.Data;
using api.Models.Vo;

namespace api.Services.Impl
{
    public class SiteAssignmentService : ISiteAssignmentService
    {
        private readonly ISiteAssignmentRepository _siteAssignmentRepository;

        public SiteAssignmentService(ISiteAssignmentRepository siteAssignmentRepository)
        {
            _siteAssignmentRepository = siteAssignmentRepository;
        }

        public async Task<IList<SiteAssignmentVo>> GetSiteAssignmentsAsync()
        {
            return await _siteAssignmentRepository.GetSiteAssignmentsAsync();
        }
    }
} 