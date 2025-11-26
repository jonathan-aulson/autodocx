using Microsoft.Xrm.Sdk;

namespace api.Services;

public interface IDataverseService
{
    IOrganizationService GetServiceClient();
}