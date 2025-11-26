using api.Config;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

namespace api.Services.Impl;

public class OrganizationServiceFactory : IOrganizationServiceFactory
{
    private readonly ILogger _logger;

    public OrganizationServiceFactory(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<OrganizationServiceFactory>();
    }
    
    public IOrganizationService CreateService()
    {
        _logger.LogInformation("Instantiated new Dataverse ServiceClient.");
        var connectionString = Configuration.GetConnectionString();
        return new ServiceClient(connectionString);
    }
}