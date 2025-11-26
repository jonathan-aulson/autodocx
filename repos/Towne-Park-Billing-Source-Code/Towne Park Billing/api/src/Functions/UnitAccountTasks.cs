

using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using System.Net;
using Microsoft.Extensions.Logging;
using api.Adapters;

namespace api.Functions
{
    class UnitAccountTasks
    {
        private readonly ILogger _logger;
        private readonly IUnitAccountTaskServiceAdapter _unitAccountService;

        public UnitAccountTasks(ILoggerFactory loggerFactory, IUnitAccountTaskServiceAdapter unitAccountService)
        {
            _logger = loggerFactory.CreateLogger<UnitAccountTasks>();
            _unitAccountService = unitAccountService;
        }

        [Function("AddUnitAccountTask")]
        public HttpResponseData AddUnitAccountTask(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "unit-account/{servicePeriod}")]
        HttpRequestData req,
        string servicePeriod)
        {
            // Extract query parameters


            // Pass the optional date parameter to the service layer or handle it as needed
            var taskId = _unitAccountService.AddTask(servicePeriod);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.WriteString(JsonConvert.SerializeObject(taskId));

            _logger.LogInformation($"Unit account task scheduled successfully for service Period: {servicePeriod}");
            return response;
        }
    }
}
