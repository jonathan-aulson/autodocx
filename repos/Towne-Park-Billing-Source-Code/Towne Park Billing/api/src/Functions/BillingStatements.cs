using api.Adapters;
using api.Config;
using api.Middleware;
using api.Models.Dto;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace api.Functions
{
    public class BillingStatements
    {
        private readonly ILogger<BillingStatements> _logger;
        private readonly IBillingStatementServiceAdapter _billingStatementService;

        public BillingStatements(ILogger<BillingStatements> logger, IBillingStatementServiceAdapter billingStatementService)
        {
            _logger = logger;
            _billingStatementService = billingStatementService;
        }
        
        [Function("CurrentBillingStatements")]
        public HttpResponseData GetCurrentBillingStatements(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "customers/statements")]
            HttpRequestData req,
            FunctionContext context)
        {
            var userDto = context.GetUserContext();

            if (userDto == null || userDto.Email == null)
            {
                _logger.LogWarning("User context not found or email is missing.");
                var errorResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                errorResponse.WriteString("User authentication context is missing or invalid.");
                return errorResponse;
            }

            // Pass UserAuth to the service adapter (which now expects it)
            var billingStatements = _billingStatementService.GetCurrentBillingStatements(userDto);

            _logger.LogInformation($"Current Billing Statements retrieved successfully for user {userDto.Email}."); // Log using userAuth email
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.WriteString(JsonConvert.SerializeObject(billingStatements));
            return response;
        }

        [Function("BillingStatements")]
        public HttpResponseData GetBillingStatements(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "customers/{customerSiteId}/statements")]
            HttpRequestData req,
            Guid customerSiteId)
        {
            var billingStatements = _billingStatementService.GetBillingStatements(customerSiteId);

            _logger.LogInformation($"Billing Statements retrieved successfully for {customerSiteId}.");
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.WriteString(JsonConvert.SerializeObject(billingStatements));
            return response;
        }

        [Function("UpdateStatementStatus")]
        public HttpResponseData UpdateStatementStatus(
            [HttpTrigger(AuthorizationLevel.Function, "patch", Route = "statement/{billingStatementId}/update")]
            HttpRequestData req,
            Guid billingStatementId)
        {
            var body = req.ReadAsString();
            var status = string.IsNullOrEmpty(body) ? null
                : JsonConvert.DeserializeObject<UpdateStatementStatusDto>(body.Replace(" ", ""));
            if (status == null) return req.CreateResponse(HttpStatusCode.BadRequest);

            _billingStatementService.UpdateStatementStatus(billingStatementId, status);

            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function("GetStatementPdfData")]
        public async Task<HttpResponseData> GetStatementPdfDataAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "statement/{billingStatementId}/generate-pdf")]
            HttpRequestData req,
            Guid billingStatementId)
        {
            var flowUrl = Configuration.getBillingStatementPdfEndpoint();
            if (string.IsNullOrEmpty(flowUrl))
            {
                _logger.LogError("Power Automate flow URL is not configured.");
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                errorResponse.WriteString("Power Automate flow URL is not configured.");
                return errorResponse;
            }

            try
            {
                var token = await Configuration.getAccessTokenAsync(false);

                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    var payload = new
                    {
                        billingStatementId = billingStatementId.ToString()
                    };

                    var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                    var httpResponse = await httpClient.PostAsync(flowUrl, content);

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        var pdfContent = await httpResponse.Content.ReadAsByteArrayAsync();
                        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                        response.Headers.Add("Content-Type", "application/pdf");
                        await response.Body.WriteAsync(pdfContent, 0, pdfContent.Length);
                        return response;
                    }
                    else
                    {
                        var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                        errorResponse.WriteString($"Error calling Power Automate: {httpResponse.StatusCode}");
                        return errorResponse;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception: {ex.Message}");
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                errorResponse.WriteString($"Error: {ex.Message}");
                return errorResponse;
            }
        }
    }
}
