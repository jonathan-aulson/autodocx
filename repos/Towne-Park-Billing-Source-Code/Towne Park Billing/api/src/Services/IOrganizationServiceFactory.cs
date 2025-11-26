using Microsoft.Xrm.Sdk;

namespace api.Services;

public interface IOrganizationServiceFactory
{
    IOrganizationService CreateService();
}