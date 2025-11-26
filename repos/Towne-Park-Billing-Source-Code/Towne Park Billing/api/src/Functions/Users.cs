using api.Adapters;
using api.Models.Dto;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using api.Middleware;

namespace api.Functions
{
    public class Users
    {
        private readonly ILogger _logger;
        private readonly IUserServiceAdapter _userServiceAdapter;

        public Users(ILoggerFactory loggerFactory, IUserServiceAdapter userServiceAdapter)
        {
            _logger = loggerFactory.CreateLogger<Users>();
            _userServiceAdapter = userServiceAdapter;
        }

        [Function("Users")]
        public HttpResponseData GetUserRoles(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "userRole")]
            HttpRequestData req,
            FunctionContext context)
        {
            var userDto = context.GetUserContext();

            if (!string.IsNullOrEmpty(userDto.Email) || userDto.Roles.Length < 1)
            {
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                response.WriteString(JsonConvert.SerializeObject(userDto));

                _logger.LogInformation("User roles retrieved successfully.");

                return response;
            }
            else
            {
                var response = req.CreateResponse(HttpStatusCode.Unauthorized);
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                response.WriteString(JsonConvert.SerializeObject(new { message = "Valid authorization token with user identity not found." }));
                _logger.LogInformation("User identity not found in token.");
                return response;
            }
        }
    }
}
