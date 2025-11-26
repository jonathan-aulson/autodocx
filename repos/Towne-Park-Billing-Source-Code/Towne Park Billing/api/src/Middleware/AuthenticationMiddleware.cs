using api.Adapters;
using api.Adapters.Mappers;
using api.Data;
using api.Models.Dto;
using api.Models.Vo;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TownePark;

namespace api.Middleware
{
    public class AuthenticationMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly ILogger<AuthenticationMiddleware> _logger;
        private readonly IUserServiceAdapter _userServiceAdapter;

        // Key to store the UserDto object in FunctionContext.Items
        public const string UserDtoKey = "User"; 

        public AuthenticationMiddleware(ILogger<AuthenticationMiddleware> logger, IUserServiceAdapter userServiceAdapter)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userServiceAdapter = userServiceAdapter ?? throw new ArgumentNullException(nameof(userServiceAdapter));
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            var req = await context.GetHttpRequestDataAsync();
            var userEmail = string.Empty;

            if (req != null)
            {
                if (req.Headers.TryGetValues("x-ms-client-principal", out var principalHeaderValues))
                {
                    var clientPrincipalHeader = principalHeaderValues.FirstOrDefault();
                    if (!string.IsNullOrEmpty(clientPrincipalHeader))
                    {
                        try
                        {
                            var decodedPrincipal = Encoding.UTF8.GetString(Convert.FromBase64String(clientPrincipalHeader));
                            var jsonObject = JObject.Parse(decodedPrincipal);
                            userEmail = jsonObject["userDetails"]?.ToString(); // Populate Email
                            _logger.LogInformation("Successfully parsed x-ms-client-principal for user: {UserEmail}", userEmail ?? "Unknown");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to decode or parse x-ms-client-principal header.");
                            // Optionally handle error, e.g., return Unauthorized
                        }
                    }
                    else
                    {
                         _logger.LogWarning("x-ms-client-principal header was present but empty.");
                    }
                }
                else
                {
                     _logger.LogInformation("x-ms-client-principal header not found.");
                }
            }
            else
            {
                 _logger.LogWarning("HttpRequestData could not be obtained in AuthenticationMiddleware.");
            }

            if (!string.IsNullOrEmpty(userEmail))
            {
                UserDto userDto = _userServiceAdapter.GetUserRoles(userEmail);

                //bs_User userModel = _userRepository.GetUserRoles(userEmail);
                //UserVo userVo = UserMapper.UserModelToVo(userModel);
                //userDto = UserMapper.UserVoToDto(userVo);

                context.Items.Add(UserDtoKey, userDto);
            }

            // Call the next middleware or the function itself
            await next(context);
        }
    }

    // Helper extension method to easily retrieve the UserDto object
    public static class FunctionContextExtensions
    {
        public static UserDto? GetUserContext(this FunctionContext context) 
        {
            if (context.Items.TryGetValue(AuthenticationMiddleware.UserDtoKey, out var userDtoObject) && userDtoObject is UserDto userDto)
            {
                return userDto;
            }
            return null;
        }
    }
} 