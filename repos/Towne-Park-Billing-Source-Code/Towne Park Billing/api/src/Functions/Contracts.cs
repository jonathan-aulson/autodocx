using System.Net;
using api.Adapters;
using api.Models.Dto;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace api.Functions
{
    public class Contracts
    {
        private readonly ILogger _logger;
        private readonly IContractServiceAdapter _contractService;

        public Contracts(ILoggerFactory loggerFactory, IContractServiceAdapter contractService)
        {
            _logger = loggerFactory.CreateLogger<Contracts>();
            _contractService = contractService;
        }

        [Function("ContractDetail")]
        public HttpResponseData GetContractDetail(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "customers/{customerSiteId}/contract")]
            HttpRequestData req,
            Guid customerSiteId)
        {
            var contract = _contractService.GetContractDetail(customerSiteId);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.WriteString(JsonConvert.SerializeObject(contract));
            return response;
        }

        [Function("UpdateContract")]
        public HttpResponseData UpdateContract(
           [HttpTrigger(AuthorizationLevel.Function, "patch", Route = "contracts/{contractId}")]
           HttpRequestData req,
           Guid contractId)
        {
            var body = req.ReadAsString();
            var updateContract = string.IsNullOrEmpty(body) ? null
                : JsonConvert.DeserializeObject<ContractDetailDto>(body);
            if (updateContract == null) return req.CreateResponse(HttpStatusCode.BadRequest);
 
            _contractService.UpdateContract(contractId, updateContract);

            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function("UpdateDeviationThreshold")]
        public HttpResponseData UpdateDeviationThreshold(
            [HttpTrigger(AuthorizationLevel.Function, "patch", Route = "deviations")]
            HttpRequestData req)
        {
            var body = req.ReadAsString();
            var deviationUpdate = string.IsNullOrEmpty(body) ? null
                : JsonConvert.DeserializeObject<List<DeviationDto>>(body);
            if (deviationUpdate == null) return req.CreateResponse(HttpStatusCode.BadRequest);

            _contractService.UpdateDeviationThreshold(deviationUpdate);

            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function("GetAllDeviationData")]
        public HttpResponseData GetDeviationData(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "deviations")]
            HttpRequestData req)
        {
            IEnumerable<DeviationDto> deviations = _contractService.GetDeviations();

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.WriteString(JsonConvert.SerializeObject(deviations));

            return response;
        }
    }
}