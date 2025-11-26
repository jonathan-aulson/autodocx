using api.Adapters;
using api.Adapters.Mappers;
using api.Models.Vo;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;

namespace api.Functions
{
    public class JobGroups
    {
        private readonly IJobGroupServiceAdapter _jobGroupServiceAdapter;
        private readonly ISiteAssignmentServiceAdapter _siteAssignmentServiceAdapter;
        private readonly ILogger<JobGroups> _logger;

        public JobGroups(IJobGroupServiceAdapter jobGroupServiceAdapter, ISiteAssignmentServiceAdapter siteAssignmentServiceAdapter, ILogger<JobGroups> logger)
        {
            _jobGroupServiceAdapter = jobGroupServiceAdapter;
            _siteAssignmentServiceAdapter = siteAssignmentServiceAdapter;
            _logger = logger;
        }

        [Function("CreateJobGroup")]
        public HttpResponseData CreateJobGroup(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "jobgroups/create")]
            HttpRequestData req)
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var groupTitle = query["jobGroupTitle"];

            if (string.IsNullOrWhiteSpace(groupTitle))
            {
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                response.Headers.Add("Content-Type", "application/json");
                response.WriteString("{\"error\":\"Group title is required.\"}");
                return response;
            }

            _jobGroupServiceAdapter.CreateJobGroup(groupTitle);

            var successResponse = req.CreateResponse(HttpStatusCode.Created);
            successResponse.Headers.Add("Content-Type", "application/json");
            successResponse.WriteString("{\"message\":\"Job group created successfully.\"}");
            return successResponse;
        }

        [Function("DeactivateJobGroup")]
        public HttpResponseData DeactivateJobGroup(
            [HttpTrigger(AuthorizationLevel.Function, "patch", Route = "jobgroups/{jobGroupId}")]
            HttpRequestData req,
            Guid jobGroupId)
        {
            if (jobGroupId == Guid.Empty)
            {
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                response.Headers.Add("Content-Type", "application/json");
                response.WriteString("{\"error\":\"Invalid job group ID.\"}");
                return response;
            }

            _jobGroupServiceAdapter.DeactivateJobGroup(jobGroupId);
            
            var successResponse = req.CreateResponse(HttpStatusCode.NoContent);
            return successResponse;
        }

        [Function("ActivateJobGroup")]
        public HttpResponseData ActivateJobGroup(
            [HttpTrigger(AuthorizationLevel.Function, "patch", Route = "jobgroups/{jobGroupId}/activate")]
            HttpRequestData req,
            Guid jobGroupId)
        {
            if (jobGroupId == Guid.Empty)
            {
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                response.Headers.Add("Content-Type", "application/json");
                response.WriteString("{\"error\":\"Invalid job group ID.\"}");
                return response;
            }

            _jobGroupServiceAdapter.ActivateJobGroup(jobGroupId);
            
            var successResponse = req.CreateResponse(HttpStatusCode.NoContent);
            return successResponse;
        }

        [Function("GetSiteAssignments")]
        public async Task<HttpResponseData> GetSiteAssignments(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "jobgroups/site-assignments")] HttpRequestData req)
        {
            _logger.LogInformation("GetSiteAssignments function started");

            try
            {
                // Call the service adapter
                var result = await _siteAssignmentServiceAdapter.GetSiteAssignmentsAsync();

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json");
                await response.WriteStringAsync(JsonSerializer.Serialize(result));
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSiteAssignments function");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                errorResponse.Headers.Add("Content-Type", "application/json");
                await errorResponse.WriteStringAsync("{\"error\":\"An error occurred while fetching site assignments.\"}");
                return errorResponse;
            }
        }

        [Function("GetJobGroups")]
        public HttpResponseData GetJobGroups(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "jobgroups")]
            HttpRequestData req)
        {
            var jobGroups = _jobGroupServiceAdapter.GetAllJobGroups();
            var mapper = new JobGroupMapper();
            var jobGroupDtos = mapper.Map(jobGroups.ToList());

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            response.WriteString(System.Text.Json.JsonSerializer.Serialize(jobGroupDtos));
            return response;
        }
    }
}
