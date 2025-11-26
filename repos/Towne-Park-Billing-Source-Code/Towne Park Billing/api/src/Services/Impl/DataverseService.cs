using Microsoft.Xrm.Sdk;

namespace api.Services.Impl;

public class DataverseService : IDataverseService
{
    // Singleton ensures that the ServiceClient is instantiated only when it is first needed and reused thereafter.
    private readonly IOrganizationService _serviceClient;

    public DataverseService(IOrganizationServiceFactory organizationServiceFactory)
    {
        _serviceClient = organizationServiceFactory.CreateService();
    }

    public IOrganizationService GetServiceClient()
    {
        return _serviceClient;
    }
}