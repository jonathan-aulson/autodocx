using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using api.Adapters;
using api.Models.Dto;

namespace api.Functions
{
    public class JobCodes
    {
        private readonly IJobCodeServiceAdapter _jobCodeServiceAdapter;
        private readonly ILogger<JobCodes> _logger;

        public JobCodes(IJobCodeServiceAdapter jobCodeServiceAdapter, ILogger<JobCodes> logger)
        {
            _jobCodeServiceAdapter = jobCodeServiceAdapter;
            _logger = logger;
        }

        [Function("GetJobCodes")]
        public async Task<HttpResponseData> GetJobCodes(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "jobcodes")] HttpRequestData req)
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var jobCodes = await _jobCodeServiceAdapter.GetJobCodesAsync();
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(jobCodes));
            return response;
        }

        [Function("GetJobCodesBySite")]
        public async Task<HttpResponseData> GetJobCodesBySite(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "job-codes/by-site/{siteId}")] HttpRequestData req,
            Guid siteId)
        {
            try
            {
                if (siteId == Guid.Empty)
                {
                    return await CreateBadRequestResponse(req, "Site ID is required and must be a valid GUID.");
                }

                var jobCodes = await _jobCodeServiceAdapter.GetJobCodesBySiteAsync(siteId);
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json");
                await response.WriteStringAsync(JsonSerializer.Serialize(jobCodes));
                return response;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid site ID provided: {SiteId}", siteId);
                return await CreateBadRequestResponse(req, ex.Message);
            }
catch (System.Exception ex)
{
    _logger.LogError(ex, "Error retrieving job codes for site: {SiteId}", siteId);
    var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
    errorResponse.Headers.Add("Content-Type", "application/json");
    await errorResponse.WriteStringAsync($"{{\"error\":\"{ex.Message}\"}}");
    return errorResponse;
}
        }

        [Function("EditJobCodeTitle")]
        public async Task<HttpResponseData> EditJobCodeTitle(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "jobcodes/title")] HttpRequestData req)
        {
            string requestBody;
            try
            {
                requestBody = await req.ReadAsStringAsync();
            }
            catch (System.Text.Json.JsonException)
            {
                return await CreateBadRequestResponse(req, "Invalid request payload.");
            }

            api.Models.Dto.EditJobCodeTitleRequestDto requestDto;
            try
            {
                requestDto = JsonSerializer.Deserialize<EditJobCodeTitleRequestDto>(requestBody);
            }
            catch (System.Text.Json.JsonException)
            {
                return await CreateBadRequestResponse(req, "Invalid request payload.");
            }

            // Comprehensive validation
            if (requestDto == null ||
                requestDto.JobCodeId == Guid.Empty ||
                string.IsNullOrWhiteSpace(requestDto.NewTitle) ||
                requestDto.NewTitle.Length > 255)
            {
                return await CreateBadRequestResponse(req, "Invalid request: JobCodeId and NewTitle are required, and NewTitle must not exceed 255 characters.");
            }

            var result = await _jobCodeServiceAdapter.EditJobCodeTitleAsync(requestDto);

            if (!result.Success)
            {
                return await CreateBadRequestResponse(req, JsonSerializer.Serialize(result));
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(result));
            return response;
        }

        [Function("AssignJobCodesToGroup")]
        public async Task<HttpResponseData> AssignJobCodesToGroup(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "jobcodes/assign")] HttpRequestData req)
        {
            _logger.LogInformation("AssignJobCodesToGroup function started");

            // Parse and validate request
            var parseResult = await ParseAndValidateAssignmentRequest(req);
            if (parseResult.ErrorResponse != null)
            {
                return parseResult.ErrorResponse;
            }

            try
            {
                var result = await _jobCodeServiceAdapter.AssignJobCodesToGroupAsync(parseResult.RequestDto);
                
                _logger.LogInformation($"Assignment completed. Success: {result.Success}, Processed: {result.ProcessedCount}");

                return await CreateAssignmentResponse(req, result);
            }
            catch (System.Exception ex)
            {
                return await HandleUnexpectedError(req, ex);
            }
        }

        private async Task<(AssignJobCodesToGroupRequestDto RequestDto, HttpResponseData ErrorResponse)> ParseAndValidateAssignmentRequest(HttpRequestData req)
        {
            // Read request body
            var requestBody = await ReadRequestBody(req);
            if (requestBody.ErrorResponse != null)
            {
                return (null, requestBody.ErrorResponse);
            }

            // Deserialize JSON
            var deserializeResult = DeserializeAssignmentRequest(req, requestBody.Body);
            if (deserializeResult.ErrorResponse != null)
            {
                return (null, deserializeResult.ErrorResponse);
            }

            // Validate input
            var validationResult = await ValidateAssignmentRequest(req, deserializeResult.RequestDto);
            if (validationResult != null)
            {
                return (null, validationResult);
            }

            return (deserializeResult.RequestDto, null);
        }

        private async Task<(string Body, HttpResponseData ErrorResponse)> ReadRequestBody(HttpRequestData req)
        {
            try
            {
                var requestBody = await req.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(requestBody))
                {
                    return (null, await CreateBadRequestResponse(req, "Request body cannot be empty."));
                }
                return (requestBody, null);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error reading request body");
                return (null, await CreateBadRequestResponse(req, "Invalid request payload."));
            }
        }

        private (AssignJobCodesToGroupRequestDto RequestDto, HttpResponseData ErrorResponse) DeserializeAssignmentRequest(HttpRequestData req, string requestBody)
        {
            try
            {
                var requestDto = JsonSerializer.Deserialize<AssignJobCodesToGroupRequestDto>(requestBody);
                return (requestDto, null);
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogError(ex, "Error deserializing request payload");
                return (null, CreateBadRequestResponse(req, "Invalid JSON format in request payload.").Result);
            }
        }

        private async Task<HttpResponseData> ValidateAssignmentRequest(HttpRequestData req, AssignJobCodesToGroupRequestDto requestDto)
        {
            if (requestDto == null)
            {
                return await CreateBadRequestResponse(req, "Request payload cannot be null.");
            }

            if (requestDto.JobCodeIds == null || requestDto.JobCodeIds.Count == 0)
            {
                return await CreateBadRequestResponse(req, "At least one job code ID must be provided.");
            }

            if (requestDto.JobCodeIds.Count > 100)
            {
                return await CreateBadRequestResponse(req, "Maximum of 100 job codes can be assigned in a single request.");
            }

            if (requestDto.TargetGroupId == Guid.Empty)
            {
                return await CreateBadRequestResponse(req, "Target group ID is required and must be a valid GUID.");
            }

            // Check for duplicate job code IDs
            var duplicateIds = requestDto.JobCodeIds
                .GroupBy(id => id)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateIds.Any())
            {
                return await CreateBadRequestResponse(req, $"Duplicate job code IDs found: {string.Join(", ", duplicateIds)}");
            }

            return null; // No validation errors
        }

        private async Task<HttpResponseData> CreateAssignmentResponse(HttpRequestData req, AssignJobCodesToGroupResponseDto result)
        {
            if (!result.Success)
            {
                return await HandleAssignmentError(req, result);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(result));
            return response;
        }

        private async Task<HttpResponseData> HandleAssignmentError(HttpRequestData req, AssignJobCodesToGroupResponseDto result)
        {
            // Check if it's a validation error vs. a server error
            if (IsValidationError(result.ErrorMessage))
            {
                return await CreateBadRequestResponse(req, JsonSerializer.Serialize(result));
            }
            else
            {
                // Server error
                _logger.LogError($"Server error during assignment: {result.ErrorMessage}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                errorResponse.Headers.Add("Content-Type", "application/json");
                await errorResponse.WriteStringAsync(JsonSerializer.Serialize(result));
                return errorResponse;
            }
        }

        private static bool IsValidationError(string errorMessage)
        {
            return errorMessage?.Contains("not found") == true || 
                   errorMessage?.Contains("inactive") == true ||
                   errorMessage?.Contains("required") == true;
        }

        private async Task<HttpResponseData> HandleUnexpectedError(HttpRequestData req, System.Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during job code assignment");
            var errorResult = new AssignJobCodesToGroupResponseDto
            {
                Success = false,
                ErrorMessage = "An unexpected error occurred during assignment. Please try again later.",
                ProcessedCount = 0,
                Results = new List<JobCodeAssignmentDto>()
            };

            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            errorResponse.Headers.Add("Content-Type", "application/json");
            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(errorResult));
            return errorResponse;
        }

        private static async Task<HttpResponseData> CreateBadRequestResponse(HttpRequestData req, string errorMessage)
        {
            var response = req.CreateResponse(HttpStatusCode.BadRequest);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(
                errorMessage.StartsWith("{") ? errorMessage : $"{{\"error\":\"{errorMessage}\"}}"
            );
            return response;
        }

        [Function("UpdateJobCodeStatus")]
        public async Task<HttpResponseData> UpdateJobCodeStatus(
            [HttpTrigger(AuthorizationLevel.Function, "patch", Route = "jobcodes/status")]
            HttpRequestData req)
        {
            _logger.LogInformation("UpdateJobCodeStatus function started");

            try
            {
                // Parse and validate request
                var parseResult = await ParseAndValidateStatusUpdateRequest(req);
                if (parseResult.ErrorResponse != null)
                {
                    return parseResult.ErrorResponse;
                }

                // Call the service adapter
                var result = await _jobCodeServiceAdapter.UpdateJobCodeStatusAsync(parseResult.RequestDto);
                
                _logger.LogInformation($"Status update completed. Success: {result.Success}, Processed: {result.ProcessedCount}");

                return await CreateStatusUpdateResponse(req, result);
            }
            catch (System.Exception ex)
            {
                return await HandleUnexpectedStatusUpdateError(req, ex);
            }
        }

        private async Task<(UpdateJobCodeStatusRequestDto RequestDto, HttpResponseData ErrorResponse)> ParseAndValidateStatusUpdateRequest(HttpRequestData req)
        {
            // Read request body
            var requestBody = await ReadRequestBody(req);
            if (requestBody.ErrorResponse != null)
            {
                return (null, requestBody.ErrorResponse);
            }

            // Deserialize JSON
            var deserializeResult = DeserializeStatusUpdateRequest(req, requestBody.Body);
            if (deserializeResult.ErrorResponse != null)
            {
                return (null, deserializeResult.ErrorResponse);
            }

            // Validate input
            var validationResult = await ValidateStatusUpdateRequest(req, deserializeResult.RequestDto);
            if (validationResult != null)
            {
                return (null, validationResult);
            }

            return (deserializeResult.RequestDto, null);
        }

        private (UpdateJobCodeStatusRequestDto RequestDto, HttpResponseData ErrorResponse) DeserializeStatusUpdateRequest(HttpRequestData req, string requestBody)
        {
            try
            {
                var requestDto = JsonSerializer.Deserialize<UpdateJobCodeStatusRequestDto>(requestBody);
                if (requestDto == null)
                {
                    var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    errorResponse.Headers.Add("Content-Type", "application/json");
                    var errorResult = new UpdateJobCodeStatusResponseDto
                    {
                        Success = false,
                        ErrorMessage = "Invalid request format.",
                        ProcessedCount = 0,
                        Results = new List<JobCodeStatusUpdateResultDto>()
                    };
                    errorResponse.WriteString(JsonSerializer.Serialize(errorResult));
                    return (null, errorResponse);
                }

                return (requestDto, null);
            }
            catch (JsonException ex)
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                errorResponse.Headers.Add("Content-Type", "application/json");
                var errorResult = new UpdateJobCodeStatusResponseDto
                {
                    Success = false,
                    ErrorMessage = $"Invalid JSON format: {ex.Message}",
                    ProcessedCount = 0,
                    Results = new List<JobCodeStatusUpdateResultDto>()
                };
                errorResponse.WriteString(JsonSerializer.Serialize(errorResult));
                return (null, errorResponse);
            }
        }

        private async Task<HttpResponseData> ValidateStatusUpdateRequest(HttpRequestData req, UpdateJobCodeStatusRequestDto requestDto)
        {
            if (requestDto.JobCodeIds == null || !requestDto.JobCodeIds.Any())
            {
                return await CreateBadRequestStatusUpdateResponse(req, "At least one job code must be specified for status update.");
            }

            if (requestDto.JobCodeIds.Count > 100)
            {
                return await CreateBadRequestStatusUpdateResponse(req, "Maximum of 100 job codes can be updated in a single request.");
            }

            // Check for duplicate job code IDs
            var duplicateIds = requestDto.JobCodeIds
                .GroupBy(id => id)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateIds.Any())
            {
                return await CreateBadRequestStatusUpdateResponse(req, $"Duplicate job code IDs found: {string.Join(", ", duplicateIds)}");
            }

            return null; // No validation errors
        }

        private async Task<HttpResponseData> CreateStatusUpdateResponse(HttpRequestData req, UpdateJobCodeStatusResponseDto result)
        {
            if (!result.Success)
            {
                return await HandleStatusUpdateError(req, result);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(result));
            return response;
        }

        private async Task<HttpResponseData> HandleStatusUpdateError(HttpRequestData req, UpdateJobCodeStatusResponseDto result)
        {
            // Check if it's a validation error vs. a server error
            if (IsValidationError(result.ErrorMessage))
            {
                return await CreateBadRequestStatusUpdateResponse(req, JsonSerializer.Serialize(result));
            }
            else
            {
                // Server error
                _logger.LogError($"Server error during status update: {result.ErrorMessage}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                errorResponse.Headers.Add("Content-Type", "application/json");
                await errorResponse.WriteStringAsync(JsonSerializer.Serialize(result));
                return errorResponse;
            }
        }

        private async Task<HttpResponseData> CreateBadRequestStatusUpdateResponse(HttpRequestData req, string errorMessage)
        {
            var response = req.CreateResponse(HttpStatusCode.BadRequest);
            response.Headers.Add("Content-Type", "application/json");
            var errorResult = new UpdateJobCodeStatusResponseDto
            {
                Success = false,
                ErrorMessage = errorMessage,
                ProcessedCount = 0,
                Results = new List<JobCodeStatusUpdateResultDto>()
            };
            await response.WriteStringAsync(JsonSerializer.Serialize(errorResult));
            return response;
        }

        private async Task<HttpResponseData> HandleUnexpectedStatusUpdateError(HttpRequestData req, System.Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during job code status update");
            var errorResult = new UpdateJobCodeStatusResponseDto
            {
                Success = false,
                ErrorMessage = "An unexpected error occurred during status update. Please try again later.",
                ProcessedCount = 0,
                Results = new List<JobCodeStatusUpdateResultDto>()
            };

            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            errorResponse.Headers.Add("Content-Type", "application/json");
            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(errorResult));
            return errorResponse;
        }
    }
}
