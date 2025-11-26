using api.Models.Dto;
using api.Models.Vo;
using Riok.Mapperly.Abstractions;
using TownePark;
using System.Collections.Generic;

namespace api.Adapters.Mappers
{
    [Mapper]
    public partial class JobCodeMapper
    {
        public partial JobCodeDto MapToDto(bs_JobCode entity);

        [MapProperty(nameof(JobCodeVo.JobCodeId), nameof(JobCodeDto.JobCodeId))]
        [MapProperty(nameof(JobCodeVo.JobCode), nameof(JobCodeDto.JobCode))]
        [MapProperty(nameof(JobCodeVo.JobTitle), nameof(JobCodeDto.JobTitle))]
        [MapProperty(nameof(JobCodeVo.Name), nameof(JobCodeDto.Name))]
        [MapProperty(nameof(JobCodeVo.IsActive), nameof(JobCodeDto.IsActive))]
        [MapProperty(nameof(JobCodeVo.JobGroupId), nameof(JobCodeDto.JobGroupId))]
        [MapProperty(nameof(JobCodeVo.JobGroupName), nameof(JobCodeDto.JobGroupName))]
        [MapProperty(nameof(JobCodeVo.ActiveEmployeeCount), nameof(JobCodeDto.ActiveEmployeeCount))]
        [MapProperty(nameof(JobCodeVo.AllocatedSalaryCost), nameof(JobCodeDto.AllocatedSalaryCost))]
        [MapProperty(nameof(JobCodeVo.AverageHourlyRate), nameof(JobCodeDto.AverageHourlyRate))]
        public partial JobCodeDto MapToDto(JobCodeVo vo);

        public partial JobCodeVo MapToVo(bs_JobCode entity);

        public partial bs_JobCode MapToEntity(JobCodeVo vo);

        /// <summary>
        /// Maps a tuple result from service to JobCodeAssignmentDto
        /// </summary>
        public JobCodeAssignmentDto MapToAssignmentDto((System.Guid JobCodeId, bool Success, string? ErrorMessage, System.Guid? PreviousGroupId) result, System.Guid newGroupId, string jobTitle = "")
        {
            return new JobCodeAssignmentDto
            {
                JobCodeId = result.JobCodeId,
                JobTitle = jobTitle,
                Success = result.Success,
                ErrorMessage = result.ErrorMessage,
                PreviousGroupId = result.PreviousGroupId,
                NewGroupId = newGroupId
            };
        }

        /// <summary>
        /// Maps a list of tuple results to JobCodeAssignmentDto list
        /// </summary>
        public List<JobCodeAssignmentDto> MapToAssignmentDtos(List<(System.Guid JobCodeId, bool Success, string? ErrorMessage, System.Guid? PreviousGroupId)> results, System.Guid newGroupId)
        {
            var dtos = new List<JobCodeAssignmentDto>();
            foreach (var result in results)
            {
                dtos.Add(MapToAssignmentDto(result, newGroupId));
            }
            return dtos;
        }
    }
}
