using api.Adapters.Mappers;
using api.Models.Dto;
using api.Services;

namespace api.Adapters.Impl
{
    public class SiteAssignmentServiceAdapter : ISiteAssignmentServiceAdapter
    {
        private readonly ISiteAssignmentService _siteAssignmentService;
        private readonly SiteAssignmentMapper _mapper;

        public SiteAssignmentServiceAdapter(ISiteAssignmentService siteAssignmentService)
        {
            _siteAssignmentService = siteAssignmentService;
            _mapper = new SiteAssignmentMapper();
        }

        public async Task<GetSiteAssignmentsResponseDto> GetSiteAssignmentsAsync()
        {
            try
            {
                // Call the service layer
                var siteAssignmentsVo = await _siteAssignmentService.GetSiteAssignmentsAsync();

                // Map to DTOs
                var siteAssignmentsDto = _mapper.MapToDto(siteAssignmentsVo);

                return new GetSiteAssignmentsResponseDto
                {
                    SiteAssignments = siteAssignmentsDto,
                    TotalCount = siteAssignmentsDto.Count,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new GetSiteAssignmentsResponseDto
                {
                    SiteAssignments = new List<SiteAssignmentDto>(),
                    TotalCount = 0,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
} 