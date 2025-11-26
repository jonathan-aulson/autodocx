using api.Adapters;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Net;
using Newtonsoft.Json;
using api.Adapters.Impl;
using api.Models.Dto;

namespace api.Functions
{
    public class ParkingRates
    {
        private readonly IParkingRateServiceAdapter _parkingRateServiceAdapter;

        public ParkingRates(IParkingRateServiceAdapter parkingRateServiceAdapter)
        {
            _parkingRateServiceAdapter = parkingRateServiceAdapter ?? throw new ArgumentNullException(nameof(parkingRateServiceAdapter));
            
          
        }

        [Function("ParkingRates")]
        public HttpResponseData GetParkingRates(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "parkingRates/{siteId}/{year}")]
            HttpRequestData req,
            Guid siteId,
            int year)
        {
            try
            {
                var parkingRates = _parkingRateServiceAdapter.GetParkingRates(siteId, year);

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                response.WriteString(JsonConvert.SerializeObject(parkingRates));
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetParkingRates function: {ex.Message}\nStack Trace: {ex.StackTrace}");

                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                errorResponse.Headers.Add("Content-Type", "application/json; charset=utf-8");
                errorResponse.WriteString(JsonConvert.SerializeObject(new { error = "An unexpected error occurred." }));
                return errorResponse;
            }
        }

        [Function("SaveParkingRates")]
            public async Task<HttpResponseData> SaveSiteParkingRates(
            [HttpTrigger(AuthorizationLevel.Function, "patch", Route = "parkingRates")]
            HttpRequestData req)
        {
            try
            {
                var body = req.ReadAsString();
                var update = string.IsNullOrEmpty(body) ? null
                    : JsonConvert.DeserializeObject<ParkingRateDataDto>(body);
                if (update == null) return req.CreateResponse(HttpStatusCode.BadRequest);

                var savedData = await _parkingRateServiceAdapter.SaveParkingRates(update);

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                response.WriteString(JsonConvert.SerializeObject(savedData));
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SaveSiteParkingRates function: {ex.Message}\nStack Trace: {ex.StackTrace}");

                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                errorResponse.Headers.Add("Content-Type", "application/json; charset=utf-8");
                errorResponse.WriteString(JsonConvert.SerializeObject(new { error = "An unexpected error occurred." }));
                return errorResponse;
            }
        }
    }
} 