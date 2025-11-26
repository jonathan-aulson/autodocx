using api.Models.Dto;
using api.Models.Vo;
using Riok.Mapperly.Abstractions;

namespace api.Adapters.Mappers
{
    [Mapper]
    public partial class SiteAssignmentMapper
    {
        [MapProperty(nameof(SiteAssignmentVo.SiteId), nameof(SiteAssignmentDto.SiteId))]
        [MapProperty(nameof(SiteAssignmentVo.SiteNumber), nameof(SiteAssignmentDto.SiteNumber))]
        [MapProperty(nameof(SiteAssignmentVo.SiteName), nameof(SiteAssignmentDto.SiteName))]
        [MapProperty(nameof(SiteAssignmentVo.City), nameof(SiteAssignmentDto.City))]
        [MapProperty(nameof(SiteAssignmentVo.AssignedJobGroups), nameof(SiteAssignmentDto.AssignedJobGroups))]
        [MapProperty(nameof(SiteAssignmentVo.JobGroupCount), nameof(SiteAssignmentDto.JobGroupCount))]
        [MapProperty(nameof(SiteAssignmentVo.HasUnassignedJobCodes), nameof(SiteAssignmentDto.HasUnassignedJobCodes))]
        public partial SiteAssignmentDto MapToDto(SiteAssignmentVo vo);

        [MapProperty(nameof(JobGroupAssignmentVo.JobGroupId), nameof(JobGroupAssignmentDto.JobGroupId))]
        [MapProperty(nameof(JobGroupAssignmentVo.JobGroupName), nameof(JobGroupAssignmentDto.JobGroupName))]
        [MapProperty(nameof(JobGroupAssignmentVo.IsActive), nameof(JobGroupAssignmentDto.IsActive))]
        public partial JobGroupAssignmentDto MapToDto(JobGroupAssignmentVo vo);

        public partial List<SiteAssignmentDto> MapToDto(IList<SiteAssignmentVo> vos);

        public partial SiteAssignmentVo MapToVo(SiteAssignmentDto dto);

        public partial JobGroupAssignmentVo MapToVo(JobGroupAssignmentDto dto);

        public partial List<SiteAssignmentVo> MapToVo(IList<SiteAssignmentDto> dtos);
    }
} 