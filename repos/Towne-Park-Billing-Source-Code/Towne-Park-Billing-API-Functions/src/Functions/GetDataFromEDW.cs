using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TownePark.Billing.Api.Helpers;
using TownePark.Billing.Api.Services;
using TownePark.Billing.Api.Models.Dto;

public class GetDataFromEDW
{
    private readonly IEDWService _edwService;
    private readonly ILogger<GetDataFromEDW> _logger;

    public GetDataFromEDW(IEDWService edwService, ILogger<GetDataFromEDW> logger)
    {
        _edwService = edwService;
        _logger = logger;
    }

    [Function("GetDataFromEDW")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "get-data")] HttpRequestData req)
    {
        _logger.LogInformation("Received request to execute stored procedure.");

        EDWDataRequest requestBody;
        try
        {
            requestBody = await JsonSerializer.DeserializeAsync<EDWDataRequest>(req.Body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Invalid request body: {ex.Message}");
            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            await errorResponse.WriteStringAsync("Invalid request body.");
            return errorResponse;
        }

        if (requestBody == null || requestBody.StoredProcedureParameters == null)
        {
            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
            await errorResponse.WriteStringAsync("Missing required parameters.");
            return errorResponse;
        }

        try
        {
            var parameters = requestBody.StoredProcedureParameters.ToDictionary(
                kvp => kvp.Key,
                kvp => SqlParameterHelper.GetValueFromJsonElement(kvp.Value)
            );

            var results = await _edwService.GetEDWDataAsync(
                requestBody.StoredProcedureId,
                parameters);

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(results));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error executing stored procedure: {ex.Message}");
            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error: {ex.Message}");
            return errorResponse;
        }
    }
}

