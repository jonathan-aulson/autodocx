using System.Net;
using System.Text;
using api.Adapters;
using api.Middleware;
using api.Models.Dto;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace api.Functions
{
    public class Customers
    {
        private readonly ILogger _logger;
        private readonly ICustomerServiceAdapter _customerService;

        public Customers(ILoggerFactory loggerFactory, ICustomerServiceAdapter customerService)
        {
            _logger = loggerFactory.CreateLogger<Customers>();
            _customerService = customerService;
        }

        [Function("Customers")]
        public HttpResponseData GetCustomers(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "customers")]
            HttpRequestData req,
            FunctionContext context)
        {
            // Get UserDto from middleware context
            var userDto = context.GetUserContext();

            if (userDto == null || userDto.Email == null)
            {
                _logger.LogWarning("User context not found or email is missing in GetCustomers.");
                var errorResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                errorResponse.WriteString("User authentication context is missing or invalid.");
                return errorResponse;
            }

            var queryParams = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var isForecast = queryParams["isForecast"]?.ToLower() == "true";

            var customers = _customerService.GetCustomersSummary(userDto, isForecast);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.WriteString(JsonConvert.SerializeObject(customers));

            _logger.LogInformation($"Customers retrieved successfully for user {userDto.Email}.");

            return response;
        }

        [Function("FetchCustomerDetail")]
        public HttpResponseData GetCustomerDetail(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "customers/{customerSiteId}")]
            HttpRequestData req,
            Guid customerSiteId)
        {
            var customer = _customerService.GetCustomerDetail(customerSiteId);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.WriteString(JsonConvert.SerializeObject(customer));

            _logger.LogInformation("Customer Detail retrieved successfully.");

            return response;
        }
        
        [Function("UpdateCustomerDetail")]
        public HttpResponseData UpdateCustomerDetail(
            [HttpTrigger(AuthorizationLevel.Function, "patch", Route = "customers/{customerSiteId}")]
            HttpRequestData req,
            Guid customerSiteId)
        {
            var body = req.ReadAsString();
            var updateCustomer = string.IsNullOrEmpty(body) ? null
                : JsonConvert.DeserializeObject<CustomerDetailDto>(body);
            if (updateCustomer == null) return req.CreateResponse(HttpStatusCode.BadRequest);
 
            _customerService.UpdateCustomerDetail(customerSiteId, updateCustomer);

            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function("AddCustomer")]
        public HttpResponseData AddCustomer(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "customers/{siteNumber}")]
            HttpRequestData req,
            string siteNumber)
        {
            var customerSiteId = _customerService.AddCustomer(siteNumber);

            _logger.LogInformation("Customer Site created successfully.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.WriteString(JsonConvert.SerializeObject(customerSiteId));

            _logger.LogInformation("Customer Site added successfully.");
            return response;
        }
    }
}
