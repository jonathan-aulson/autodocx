using api.Adapters;
using api.Adapters.Impl;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using System.Net;
using api.Models.Dto;

namespace api.Functions
{
    public class OtherRevenue
    {
        private readonly IOtherRevenueServiceAdapter _otherRevenueService;

        public OtherRevenue(IOtherRevenueServiceAdapter otherRevenueService)
        {
            _otherRevenueService = otherRevenueService;
        }

        [Function("OtherRevenue")]
        public HttpResponseData GetOtherRevenue(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "otherRevenue/{siteId}/{billingPeriod}")]
        HttpRequestData req,
        Guid siteId,
        string billingPeriod)
        {
            var otherRevenue = _otherRevenueService.GetOtherRevenue(siteId, billingPeriod);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.WriteString(JsonConvert.SerializeObject(otherRevenue));
            return response;
        }

        [Function("SaveOtherRevenue")]
        public HttpResponseData SaveOtherRevenue(
            [HttpTrigger(AuthorizationLevel.Function, "patch", Route = "otherRevenue")]
            HttpRequestData req)
        {
            var body = req.ReadAsString();
            var update = string.IsNullOrEmpty(body) ? null
                : JsonConvert.DeserializeObject<OtherRevenueDto>(body);
            if (update == null) return req.CreateResponse(HttpStatusCode.BadRequest);

            _otherRevenueService.SaveOtherRevenueData(update);

            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}
