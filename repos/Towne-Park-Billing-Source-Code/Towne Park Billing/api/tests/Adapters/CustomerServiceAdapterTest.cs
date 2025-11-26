using api.Adapters.Impl;
using api.Models.Dto;
using api.Models.Vo;
using api.Services;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace BackendTests.Adapters;

public class CustomerServiceAdapterTest
{
    private readonly ICustomerService _customerService;
    private readonly CustomerServiceAdapter _customerServiceAdapter;

    public CustomerServiceAdapterTest()
    {
        _customerService = Substitute.For<ICustomerService>();
        _customerServiceAdapter = new CustomerServiceAdapter(_customerService);
    }

    [Fact]
    public void GetCustomersSummary_ShouldCallCustomerServiceAndReturnAdaptedResponse_WithIsForecastFlag()
    {
        UserDto userAuth = new UserDto();
        var customersVo = new List<CustomerSummaryVo>
        {
            new CustomerSummaryVo
            {
                SiteNumber = "SiteNum1",
                SiteName = "Company A"
            },
            new CustomerSummaryVo
            {
                SiteNumber = "SiteNum2",
                SiteName = "Company B"
            }
        };
        _customerService.GetCustomersSummary(userAuth, true).Returns(customersVo);

        var customersDto = new List<CustomerSummaryDto>
        {
            new CustomerSummaryDto
            {
                SiteNumber = "SiteNum1",
                SiteName = "Company A"
            },
            new CustomerSummaryDto
            {
                SiteNumber = "SiteNum2",
                SiteName = "Company B"
            }
        };
        var result = _customerServiceAdapter.GetCustomersSummary(userAuth, true);
        result.Should().BeEquivalentTo(customersDto);
        _customerService.Received(1).GetCustomersSummary(userAuth, true);
    }

    [Fact]
    public void GetCustomersSummary_ShouldCallCustomerServiceAndReturnAdaptedResponse_WithIsForecastFalse()
    {
        UserDto userAuth = new UserDto();
        var customersVo = new List<CustomerSummaryVo>
        {
            new CustomerSummaryVo
            {
                SiteNumber = "SiteNum1",
                SiteName = "Company A"
            }
        };
        _customerService.GetCustomersSummary(userAuth, false).Returns(customersVo);

        var customersDto = new List<CustomerSummaryDto>
        {
            new CustomerSummaryDto
            {
                SiteNumber = "SiteNum1",
                SiteName = "Company A"
            }
        };
        var result = _customerServiceAdapter.GetCustomersSummary(userAuth, false);
        result.Should().BeEquivalentTo(customersDto);
        _customerService.Received(1).GetCustomersSummary(userAuth, false);
    }
    
    [Fact]
    public void GetCustomerDetail_ShouldCallCustomerServiceAndReturnAdaptedResponse()
    {
        var customerId = Guid.NewGuid();
        var customerDetailVo = new CustomerDetailVo
        {
            CustomerSiteId = customerId,
            SiteName = "Marriot Bethesda",
            AccountManager = "Lucas Vaz",
            SiteNumber = "0419",
            InvoiceRecipient = "John Doe",
            BillingContactEmail = "john_doe@gmail.com"
        };
        _customerService.GetCustomerDetail(customerId).Returns(customerDetailVo);

        var customerDetailDto = new CustomerDetailDto
        {
            CustomerSiteId = customerId,
            SiteName = "Marriot Bethesda",
            AccountManager = "Lucas Vaz",
            SiteNumber = "0419",
            InvoiceRecipient = "John Doe",
            BillingContactEmail = "john_doe@gmail.com"
        };
        var result = _customerServiceAdapter.GetCustomerDetail(customerId);
        result.Should().BeEquivalentTo(customerDetailDto);
    }
    
    [Fact]
    public void UpdateCustomerDetail_ShouldCallCustomerServiceWithMappedDtoToVo()
    {
        var customerId = Guid.NewGuid();
        var updateCustomerDto = new CustomerDetailDto
        {
            CustomerSiteId = customerId,
            SiteName = "New Site",
            AccountManager = "New Manager",
            SiteNumber = "5678",
            InvoiceRecipient = "New Contact",
            BillingContactEmail = "new.contact@example.com"
        };

        _customerServiceAdapter.UpdateCustomerDetail(customerId, updateCustomerDto);

        _customerService.Received(1).UpdateCustomerDetail(customerId, Arg.Is<CustomerDetailVo>(vo =>
            vo.CustomerSiteId == updateCustomerDto.CustomerSiteId
            && vo.SiteName == updateCustomerDto.SiteName
            && vo.AccountManager == updateCustomerDto.AccountManager
            && vo.SiteNumber == updateCustomerDto.SiteNumber
            && vo.InvoiceRecipient == updateCustomerDto.InvoiceRecipient
            && vo.BillingContactEmail == updateCustomerDto.BillingContactEmail
        ));
    }
}
