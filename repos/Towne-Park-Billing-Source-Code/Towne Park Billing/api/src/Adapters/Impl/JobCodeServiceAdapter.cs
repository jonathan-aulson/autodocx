using System.Collections.Generic;
using System.Threading.Tasks;
using api.Adapters;
using api.Adapters.Mappers;
using api.Models.Dto;
using api.Models.Vo;
using api.Services;

namespace api.Adapters.Impl
{
    public class JobCodeServiceAdapter : IJobCodeServiceAdapter
    {
        private readonly IJobCodeService _jobCodeService;

        public JobCodeServiceAdapter(IJobCodeService jobCodeService)
        {
            _jobCodeService = jobCodeService;
        }

        public async Task<IList<JobCodeDto>> GetJobCodesAsync()
        {
            var vos = await _jobCodeService.GetJobCodesAsync();
            var dtos = new List<JobCodeDto>();
            var mapper = new JobCodeMapper();
            foreach (var vo in vos)
            {
                dtos.Add(mapper.MapToDto(vo));
            }
            return dtos;
        }

        public async Task<IList<JobCodeDto>> GetJobCodesBySiteAsync(Guid siteId)
        {
            var vos = await _jobCodeService.GetJobCodesBySiteAsync(siteId);
            var dtos = new List<JobCodeDto>();
            var mapper = new JobCodeMapper();
            foreach (var vo in vos)
            {
                dtos.Add(mapper.MapToDto(vo));
            }
            return dtos;
        }

        public async Task<EditJobCodeTitleResponseDto> EditJobCodeTitleAsync(EditJobCodeTitleRequestDto request)
        {
            var result = await _jobCodeService.EditJobCodeTitleAsync(request.JobCodeId, request.NewTitle);

            return new EditJobCodeTitleResponseDto
            {
                Success = result.Success,
                ErrorMessage = result.ErrorMessage,
                OldTitle = result.OldTitle,
                NewTitle = result.Success ? request.NewTitle : null
            };
        }

        public async Task<AssignJobCodesToGroupResponseDto> AssignJobCodesToGroupAsync(AssignJobCodesToGroupRequestDto request)
        {
            // Call service layer
            var serviceResult = await _jobCodeService.AssignJobCodesToGroupAsync(request.JobCodeIds, request.TargetGroupId);

            // Map service result to DTO
            var mapper = new JobCodeMapper();
            var resultDtos = mapper.MapToAssignmentDtos(serviceResult.Results, request.TargetGroupId);

            return new AssignJobCodesToGroupResponseDto
            {
                Success = serviceResult.Success,
                ErrorMessage = serviceResult.ErrorMessage,
                ProcessedCount = serviceResult.ProcessedCount,
                Results = resultDtos
            };
        }

        public async Task<UpdateJobCodeStatusResponseDto> UpdateJobCodeStatusAsync(UpdateJobCodeStatusRequestDto request)
        {
            // Call service layer
            var serviceResult = await _jobCodeService.UpdateJobCodeStatusAsync(request.JobCodeIds, request.IsActive);

            // Map service result to DTO
            var resultDtos = new List<JobCodeStatusUpdateResultDto>();
            foreach (var result in serviceResult.Results)
            {
                resultDtos.Add(new JobCodeStatusUpdateResultDto
                {
                    JobCodeId = result.JobCodeId,
                    JobTitle = result.JobTitle,
                    Success = result.Success,
                    ErrorMessage = result.ErrorMessage,
                    PreviousStatus = result.PreviousStatus,
                    NewStatus = request.IsActive
                });
            }

            return new UpdateJobCodeStatusResponseDto
            {
                Success = serviceResult.Success,
                ErrorMessage = serviceResult.ErrorMessage,
                ProcessedCount = serviceResult.ProcessedCount,
                Results = resultDtos
            };
        }
    }
}
