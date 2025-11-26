using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using api.Adapters;

namespace api.Functions
{
    public class ConfigData
    {
        private readonly ILogger<ConfigData> _logger;
        private readonly IConfigDataServiceAdapter _configDataService;

        public ConfigData(ILogger<ConfigData> logger, IConfigDataServiceAdapter configDataService)
        {
            _logger = logger;
            _configDataService = configDataService;
        }

        [Function("GlCodes")]
        public HttpResponseData GetGlCodes(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "gl-codes")]
            HttpRequestData req)
        {
            var queryParams = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            string codeTypeParam = queryParams["codeTypes"];

            List<string> codeTypes = string.IsNullOrWhiteSpace(codeTypeParam)
                ? new List<string>()
                : codeTypeParam.Split(',').ToList();

            if (codeTypes.Count == 0)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                badResponse.WriteString("Please provide at least one codeType query parameter.");
                return badResponse;
            }

            var glCodes = _configDataService.GetGlCodes(codeTypes);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.WriteString(JsonConvert.SerializeObject(glCodes));

            _logger.LogInformation($"GL codes for codeType '{string.Join(",", codeTypes)}' retrieved successfully.");
            return response;
        }

        [Function("InvoiceConfig")]
        public HttpResponseData GetInvoiceConfig(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "invoice-config")]
            HttpRequestData req)
        {
            // Todo: handle multiple config groups
            var queryParams = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            string configGroupParam = queryParams["configGroup"];

            if (string.IsNullOrWhiteSpace(configGroupParam))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                badResponse.WriteString("Please provide a configGroup query parameter.");
                return badResponse;
            }

            var invoiceConfig = _configDataService.GetInvoiceConfig(configGroupParam);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.WriteString(JsonConvert.SerializeObject(invoiceConfig));

            _logger.LogInformation("Invoice config retrieved successfully.");
            return response;
        }
    }
}
