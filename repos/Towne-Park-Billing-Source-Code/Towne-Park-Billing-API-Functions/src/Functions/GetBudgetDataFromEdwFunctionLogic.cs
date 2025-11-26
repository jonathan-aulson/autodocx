using Microsoft.Data.SqlClient;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class GetBudgetDataFromEdwFunctionLogic
{
    private readonly ILogger _logger;
    private readonly IConfiguration _config;

    public GetBudgetDataFromEdwFunctionLogic(ILoggerFactory loggerFactory, IConfiguration config)
    {
        _logger = loggerFactory.CreateLogger<GetBudgetDataFromEdwFunctionLogic>();
        _config = config;
    }

    [Function("GetBudgetDataFromEdw")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "budget-data")] HttpRequestData req)
    {
        _logger.LogInformation("Checking SQL Server connectivity.");

        var connectionString = _config["SqlConnectionString"];
        string serverTime = "N/A";

        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            using var command = new SqlCommand("SELECT SYSDATETIME()", connection);
            var result = await command.ExecuteScalarAsync();
            serverTime = result?.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Connection failed: {ex.Message}");
            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error: {ex.Message}");
            return errorResponse;
        }

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteStringAsync($"Connection OK. SQL Server time is: {serverTime}");
        return response;
    }
}
