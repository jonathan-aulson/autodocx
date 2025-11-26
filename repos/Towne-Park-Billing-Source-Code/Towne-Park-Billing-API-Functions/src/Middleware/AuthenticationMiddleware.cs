using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using TownePark.Billing.Api.Config;

public class AuthenticationMiddleware : IFunctionsWorkerMiddleware
{
    private static readonly string TenantId = Configuration.GetAzureServiceClientTenant();
    private static readonly string ClientId = Configuration.GetAzureServiceClientId();

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var req = await context.GetHttpRequestDataAsync();

        if (req != null)
        {
            bool isValid = await ValidateTokenAsync(req);
            if (!isValid)
            {
                var response = req.CreateResponse(HttpStatusCode.Unauthorized);
                await response.WriteStringAsync("Unauthorized");
                context.GetInvocationResult().Value = response;
                return;
            }
        }

        await next(context);
    }

    private static async Task<bool> ValidateTokenAsync(Microsoft.Azure.Functions.Worker.Http.HttpRequestData req)
    {
        string? authHeader = req.Headers.TryGetValues("Authorization", out var values) ? values.FirstOrDefault() : null;

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string token = authHeader.Substring("Bearer ".Length).Trim();

        try
        {
            var handler = new JwtSecurityTokenHandler();

            var discoveryDocument = $"https://login.microsoftonline.com/{TenantId}/v2.0/.well-known/openid-configuration";
            var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                discoveryDocument,
                new OpenIdConnectConfigurationRetriever());
            var config = await configManager.GetConfigurationAsync();

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuers = new[]
                {
                    $"https://sts.windows.net/{TenantId}/",
                    $"https://login.microsoftonline.com/{TenantId}/v2.0"
                },
                ValidateAudience = true,
                ValidAudiences = new[]
                {
                    ClientId,
                    $"api://{ClientId}"
                },
                ValidateLifetime = true,
                IssuerSigningKeys = config.SigningKeys
            };

            handler.ValidateToken(token, tokenValidationParameters, out var validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var tokenTenantId = jwtToken.Claims.FirstOrDefault(c => c.Type == "tid")?.Value;

            return tokenTenantId == TenantId;
        }
        catch
        {
            return false;
        }
    }
}
