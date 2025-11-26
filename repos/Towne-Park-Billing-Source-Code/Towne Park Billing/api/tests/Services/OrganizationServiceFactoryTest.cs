using api.Functions;
using api.Services.Impl;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.PowerPlatform.Dataverse.Client.Utils;
using NSubstitute;
using Xunit;

namespace BackendTests.Services;

public class OrganizationServiceFactoryTest
{

    private readonly OrganizationServiceFactory _organizationServiceFactory;

    public OrganizationServiceFactoryTest()
    {
        var loggerFactoryMock = Substitute.For<ILoggerFactory>();
        loggerFactoryMock.CreateLogger<Customers>().Returns(NullLogger<Customers>.Instance);

        _organizationServiceFactory = new OrganizationServiceFactory(loggerFactoryMock);
    }

    // I considered using the real credentials by adding a local.settings.json project for the tests directory.
    // However this would mean the tests would fail for anyone that does not add the json to its local files.
    [Fact]
    public void GetServiceClient_ShouldFailToConnect_WhenUsingFakeCredentials()
    {
        Environment.SetEnvironmentVariable("AZURE_SERVICE_CLIENT_ID", "fake-client-id");
        Environment.SetEnvironmentVariable("AZURE_SERVICE_CLIENT_SECRET", "fake-client-secret");

        var action = () => _organizationServiceFactory.CreateService();
        action.Should().Throw<KeyNotFoundException>();
    }
}