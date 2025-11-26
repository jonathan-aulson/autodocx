using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using api.Adapters;
using api.Models.Dto;
using api.Config;
using System.Net.Http.Headers;

namespace api.Functions
{
    public class SiteStatistics
    {
        ISiteStatisticServiceAdapter _siteStatisticServiceAdapter;

        public SiteStatistics(ISiteStatisticServiceAdapter siteStatisticServiceAdapter)
        {
            _siteStatisticServiceAdapter = siteStatisticServiceAdapter;
        }

        [Function("SiteStatistics")]
        public HttpResponseData GetSiteStatistics(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "siteStatistics/{siteId}/{billingPeriod}")]
            HttpRequestData req,
            Guid siteId,
            string billingPeriod)
        {
            string? timeRange = req.Query["timeRange"];

            if (string.IsNullOrEmpty(timeRange))
            {
                timeRange = "DAILY";
            }

            var siteStatistics = _siteStatisticServiceAdapter.GetSiteStatistics(siteId, billingPeriod, timeRange);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.WriteString(JsonConvert.SerializeObject(siteStatistics));
            return response;
        }

        [Function("SaveSiteStatistics")]
        public HttpResponseData SaveSiteStatistics(
            [HttpTrigger(AuthorizationLevel.Function, "patch", Route = "siteStatistics")]
            HttpRequestData req)
        {
            var body = req.ReadAsString();
            if (string.IsNullOrEmpty(body))
                return req.CreateResponse(HttpStatusCode.BadRequest);

            // Try to deserialize as a list first, then fallback to single object
            List<SiteStatisticDto>? updates = null;
            SiteStatisticDto? singleUpdate = null;
            try  
            {
                updates = JsonConvert.DeserializeObject<List<SiteStatisticDto>>(body);
            }
            catch
            {
                // ignore
            }

            if (updates == null)
            {
                try
                {
                    singleUpdate = JsonConvert.DeserializeObject<SiteStatisticDto>(body);
                }
                catch
                {
                    // ignore
                }
            }

            if (updates != null)
            {
                foreach (var update in updates)
                {
                    _siteStatisticServiceAdapter.SaveSiteStatistics(update);
                }
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                response.WriteString(JsonConvert.SerializeObject(updates));
                return response;
            }
            else if (singleUpdate != null)
            {
                _siteStatisticServiceAdapter.SaveSiteStatistics(singleUpdate);
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                response.WriteString(JsonConvert.SerializeObject(singleUpdate));
                return response;
            }
            else
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }
        }
    }
}
