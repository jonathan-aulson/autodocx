using api.Services; // Changed from api.Adapters
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using System.Net;
// using api.Models.Dto; // PnlResponseDto was here, not used for now
using TownePark.Models.Vo; // For InternalRevenueDataVo
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq; // For Any()

namespace api.Functions
{
    public class PnlRequest
    {
        public List<string>? SiteIds { get; set; }
        public int? Year { get; set; }
    }

    public class Pnl
    {
        private readonly IPnlService _pnlService; // Changed from IPnlServiceAdapter

        public Pnl(IPnlService pnlService) // Changed from IPnlServiceAdapter
        {
            _pnlService = pnlService;
        }

        [Function("Pnl")]
        public async Task<HttpResponseData> GetPnl(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "pnl")] HttpRequestData req)
        {
            var body = await req.ReadAsStringAsync();
            PnlRequest? requestPayload = null;
            try
            {
                requestPayload = string.IsNullOrEmpty(body) ? null
                    : JsonConvert.DeserializeObject<PnlRequest>(body);
            }
            catch (JsonException ex)
            {
                var badReqResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                badReqResponse.WriteString($"Invalid JSON format: {ex.Message}");
                return badReqResponse;
            }

            if (requestPayload == null || requestPayload.SiteIds == null || !requestPayload.SiteIds.Any() || !requestPayload.Year.HasValue)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                badResponse.WriteString("Missing or invalid 'siteIds' or 'year' in request body.");
                return badResponse;
            }

            // Pass both siteIds and year to the service, get VO list
            var internalRevenueData = await _pnlService.GetPnlInternalRevenueDataAsync(requestPayload.SiteIds, requestPayload.Year.Value);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            // Serialize the List<InternalRevenueDataVo> directly
            response.WriteString(JsonConvert.SerializeObject(internalRevenueData));
            return response;
        }
    }
}
