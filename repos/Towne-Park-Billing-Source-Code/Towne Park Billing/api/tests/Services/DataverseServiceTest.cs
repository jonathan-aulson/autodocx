using api.Services.Impl;
using FluentAssertions;
using Microsoft.Xrm.Sdk;
using NSubstitute;
using Xunit;
using IOrganizationServiceFactory = api.Services.IOrganizationServiceFactory;

namespace BackendTests.Services;

public class DataverseServiceTest
{
    private readonly IOrganizationService _organizationService;

    private readonly DataverseService _dataverseService;

    public DataverseServiceTest()
    {
        _organizationService = Substitute.For<IOrganizationService>();

        var organizationServiceFactory = Substitute.For<IOrganizationServiceFactory>();
        organizationServiceFactory.CreateService().Received(1).Returns(_organizationService);

        _dataverseService = new DataverseService(organizationServiceFactory);
    }

    [Fact]
    public void GetServiceClient_ShouldReturnServiceClient()
    {
        var result = _dataverseService.GetServiceClient();

        result.Should().BeSameAs(_organizationService);
    }
    
    [Fact]
    public void GetServiceClient_ShouldReturnServiceClient_WhenCalledMultipleTimes()
    {
        var result1 = _dataverseService.GetServiceClient();
        var result2 = _dataverseService.GetServiceClient();
        var result3 = _dataverseService.GetServiceClient();
    
        result1.Should().BeSameAs(_organizationService);
        result2.Should().BeSameAs(_organizationService);
        result3.Should().BeSameAs(_organizationService);
    }
}