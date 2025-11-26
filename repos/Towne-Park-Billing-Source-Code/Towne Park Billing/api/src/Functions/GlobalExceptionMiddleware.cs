using System.Net;
using api.Data.Impl;
using api.Models.Dto;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace api.Functions;

public class GlobalExceptionMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger _logger;

    public GlobalExceptionMiddleware(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<GlobalExceptionMiddleware>();
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception caught in middleware.");

            var httpRequest = await context.GetHttpRequestDataAsync();
            if (httpRequest is not null)
            {
                var response = httpRequest.CreateResponse(GetStatusCode(ex));
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                await response.WriteStringAsync(JsonConvert.SerializeObject(GetErrorMessage(ex)));

                context.GetInvocationResult().Value = response;
            }
        }
    }

    private HttpStatusCode GetStatusCode(Exception ex)
    {
        return ex switch
        {
            MasterCustomerSiteNotFoundExc => HttpStatusCode.NotFound,
            DuplicateCustomerSiteExc => HttpStatusCode.Conflict,
            _ => HttpStatusCode.InternalServerError
        };
    }

    // We might want to consider letting the exception thrower define the message.
    private ErrorMessageDto GetErrorMessage(Exception ex)
    {
        return ex switch
        {
            MasterCustomerSiteNotFoundExc => new ErrorMessageDto
            {
                ErrorCode = 1,
                ErrorMessage = "Unable to add Customer, provided id was invalid or site is missing any of the following required data: GL String, Address. Please contact the Finance team for more information",
            },
            DuplicateCustomerSiteExc => new ErrorMessageDto
            {
                ErrorCode = 2,
                ErrorMessage = "Unable to add Customer, Site already exists."
            },
            _ => new ErrorMessageDto
            {
                ErrorCode = 0,
                ErrorMessage = "An unexpected error occurred."
            }
        };
    }
}