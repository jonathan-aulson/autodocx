using Newtonsoft.Json;

namespace api.Config;

public static class Configuration
{
    public static string GetConnectionString()
    {
        var url = GetEnvironmentVariable("DATAVERSE_URL");
        var clientId = GetEnvironmentVariable("AZURE_SERVICE_CLIENT_ID");
        var secret = GetEnvironmentVariable("AZURE_SERVICE_CLIENT_SECRET");
        
        return $@"
            AuthType = ClientSecret;
            Url = {url};
            ClientId = {clientId};
            Secret = {secret}";
    }

    public static FormUrlEncodedContent GetAuthRequestBody(bool useServiceScope)
    {
        var clientId = GetEnvironmentVariable("AZURE_SERVICE_CLIENT_ID");
        var secret = GetEnvironmentVariable("AZURE_SERVICE_CLIENT_SECRET");
        var scope = !useServiceScope
        ? "https://service.flow.microsoft.com//.default"
        : $"api://{clientId}/.default";

        return new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", secret),
                new KeyValuePair<string, string>("scope", scope)
            });
    }

    public static string GetAzureServiceClientTenant()
    {
        var url = GetEnvironmentVariable("AZURE_SERVICE_CLIENT_TENANT");
        return url;
    }

    public static string getBillingStatementPdfEndpoint()
    {
        var url = GetEnvironmentVariable("PA_BILLING_STATEMENT_PDF_ENDPOINT");
        return url;
    }

    public static string getEDWDataAPIEndpoint()
    {
        var url = GetEnvironmentVariable("EDW_DATA_API_ENDPOINT");
        return url;
    }
  
    private static string GetEnvironmentVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name) ?? throw new KeyNotFoundException($"{name} not found");
    }

    public static async Task<string> getAccessTokenAsync(bool useServiceScope)
    {
        var tenantId = GetAzureServiceClientTenant();

        var body = GetAuthRequestBody(useServiceScope);

        using (var httpClient = new HttpClient())
        {
            var tokenUrl = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
            var response = await httpClient.PostAsync(tokenUrl, body);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to acquire token: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResult = JsonConvert.DeserializeObject<dynamic>(responseContent);

            return tokenResult.access_token;
        }
    }
}
