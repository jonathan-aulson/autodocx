using System.Net;
using api.Adapters;
using api.Models.Dto;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TownePark;

namespace api.Functions
{
    public class EmailTasks
    {
        private readonly ILogger<EmailTasks> _logger;
        private readonly IEmailTaskServiceAdapter _emailTaskService;

        public EmailTasks(ILogger<EmailTasks> logger, IEmailTaskServiceAdapter emailTaskService)
        {
            _logger = logger;
            _emailTaskService = emailTaskService;
        }

        [Function("AddEmailGenerationTask")]
        public HttpResponseData AddEmailGenerationTask(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "billingStatement/{billingStatementId}/email")]
        HttpRequestData req,
            Guid billingStatementId)
        {
            string requestBody = new StreamReader(req.Body).ReadToEnd();
            var requestData = JsonConvert.DeserializeObject<EmailTaskRequestDto>(requestBody);
            
            var taskId = _emailTaskService.AddTask(billingStatementId, requestData?.SendAction);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.WriteString(JsonConvert.SerializeObject(taskId));

            _logger.LogInformation("Email generation task scheduled successfully.");
            return response;
        }

        [Function("AddBulkEmailGenerationTask")]
        public HttpResponseData AddBulkEmailGenerationTask(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "billingStatements/email")]
            HttpRequestData req)
            {
            var requestBody = new StreamReader(req.Body).ReadToEnd();
                var requestData = JsonConvert.DeserializeObject<BillingStatementIdsRequestDto>(requestBody);
                var billingStatementIds = requestData?.StatementIds;
                var taskIds = _emailTaskService.AddTasks(billingStatementIds ?? Enumerable.Empty<Guid>());

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                response.WriteString(JsonConvert.SerializeObject(taskIds));

                _logger.LogInformation("Bulk email generation tasks scheduled successfully.");
                return response;
            }
    }
}
