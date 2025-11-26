using api.Adapters;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using System.Net;
using api.Models.Dto;

namespace api.Functions
{
    public class Payrolls
    {
        private readonly IPayrollServiceAdapter _payrollServiceAdapter;
        public Payrolls(IPayrollServiceAdapter payrollServiceAdapter)
        {
            _payrollServiceAdapter = payrollServiceAdapter;
        }

        [Function("Payroll")]
        public HttpResponseData GetPayroll(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "payroll/{siteId}/{billingPeriod}")]
        HttpRequestData req,
        Guid siteId,
        string billingPeriod)
        {
            var payroll = _payrollServiceAdapter.GetPayroll(siteId, billingPeriod);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.WriteString(JsonConvert.SerializeObject(payroll));
            return response;
        }

        [Function("SavePayroll")]
        public HttpResponseData SavePayroll(
        [HttpTrigger(AuthorizationLevel.Function, "patch", Route = "payroll")]
        HttpRequestData req)
        {
            var body = req.ReadAsString();
            var update = string.IsNullOrEmpty(body) ? null
                : JsonConvert.DeserializeObject<PayrollDto>(body);
            if (update == null) return req.CreateResponse(HttpStatusCode.BadRequest);

            _payrollServiceAdapter.SavePayroll(update);
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.WriteString(JsonConvert.SerializeObject(update));
            return response;
        }
    }
}
