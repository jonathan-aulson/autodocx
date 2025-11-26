using System.Net;
using api.Adapters;
using api.Models.Dto;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace api.Functions;

public class StatementTasks
{
    private readonly ILogger _logger;

    private readonly IStatementTaskServiceAdapter _statementTaskService;

    public StatementTasks(ILoggerFactory loggerFactory, IStatementTaskServiceAdapter statementTaskService)
    {
        _logger = loggerFactory.CreateLogger<Invoice>();
        _statementTaskService = statementTaskService;
    }
    
    [Function("AddBulkStatementGenerationTask")]
    public HttpResponseData AddBulkStatementGenerationTask(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "customers/statements")]
        HttpRequestData req)
    {
        // TODO encapsulate body deserialization logic into service.
        var requestBody = new StreamReader(req.Body).ReadToEnd();
        var requestData = JsonConvert.DeserializeObject<CustomerIdsRequestDto>(requestBody);
        var customerSiteIds = requestData?.CustomerSiteIds;
        var taskIds = _statementTaskService.AddTasks(customerSiteIds ?? Enumerable.Empty<Guid>());

        // TODO encapsulate body serialization logic into service.
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        response.WriteString(JsonConvert.SerializeObject(taskIds));

        _logger.LogInformation("Bulk statement generation tasks scheduled successfully.");
        return response;
    }

    [Function("AddStatementGenerationTask")]
    public HttpResponseData AddStatementGenerationTask(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "customers/{customerSiteId}/statement")]
    HttpRequestData req,
        Guid customerSiteId)
    {
        // Extract query parameters
        var query = req.Url.Query;
        DateOnly? servicePeriodStart = null;

        // Check if the optional parameter 'servicePeriodStart' is provided
        if (System.Web.HttpUtility.ParseQueryString(query).Get("servicePeriodStart") is string servicePeriodStartParam)
        {
            if (DateOnly.TryParse(servicePeriodStartParam, out var parsedDateOnly))
            {
                servicePeriodStart = parsedDateOnly;
            }
            else
            {
                // If parsing fails, return a bad request response.
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                badRequestResponse.WriteString("Invalid date format for 'servicePeriodStart'. Expected format is yyyy-MM-dd");
                return badRequestResponse;
            }
        }

        // Pass the optional date parameter to the service layer or handle it as needed
        var taskId = _statementTaskService.AddTask(customerSiteId, servicePeriodStart);

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        response.WriteString(JsonConvert.SerializeObject(taskId));

        _logger.LogInformation($"Statement generation task scheduled successfully. Service Period Start: {servicePeriodStart}");
        return response;
    }

}