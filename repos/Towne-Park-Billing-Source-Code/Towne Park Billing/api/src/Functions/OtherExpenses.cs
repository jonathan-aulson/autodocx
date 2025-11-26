using api.Adapters;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using System.Net;
using api.Models.Dto;

namespace api.Functions
{
    public class OtherExpenses
    {
        private readonly IOtherExpenseServiceAdapter _otherExpenseService;

        public OtherExpenses(IOtherExpenseServiceAdapter otherExpenseService)
        {
            _otherExpenseService = otherExpenseService;
        }

        [Function("OtherExpense")]
        public HttpResponseData GetOtherExpense(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "otherExpense/{siteId}/{billingPeriod}")]
        HttpRequestData req,
        Guid siteId,
        string billingPeriod)
        {
            var otherExpense = _otherExpenseService.GetOtherExpenseData(siteId, billingPeriod);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.WriteString(JsonConvert.SerializeObject(otherExpense));
            return response;
        }

        [Function("SaveOtherExpense")]
        public HttpResponseData SaveOtherExpense(
            [HttpTrigger(AuthorizationLevel.Function, "patch", Route = "otherExpense")]
            HttpRequestData req)
        {
            var body = req.ReadAsString();
            var update = string.IsNullOrEmpty(body) ? null
                : JsonConvert.DeserializeObject<OtherExpenseDto>(body);
            if (update == null) return req.CreateResponse(HttpStatusCode.BadRequest);

            _otherExpenseService.SaveOtherExpenseData(update);

            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}
