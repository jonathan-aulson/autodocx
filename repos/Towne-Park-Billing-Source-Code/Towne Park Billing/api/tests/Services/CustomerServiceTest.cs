using api.Data;
using api.Models.Dto;
using api.Models.Vo;
using api.Models.Vo.Enum;
using api.Services;
using api.Services.Impl;
using FluentAssertions;
using NSubstitute;
using TownePark;
using Xunit;

namespace BackendTests.Services;

public class CustomerServiceTest
{
    private readonly ICustomerRepository _customerRepository;
    
    private readonly IContractService _contractService;

    private readonly CustomerService _customerService;

    private readonly IRoleService _roleService;

    private readonly IActionOverridesRepository _actionOverridesRepository;


    public CustomerServiceTest()
    {
        _customerRepository = Substitute.For<ICustomerRepository>();
        _contractService = Substitute.For<IContractService>();
        _roleService = Substitute.For<IRoleService>();
        _actionOverridesRepository = Substitute.For<IActionOverridesRepository>();

        _customerService = new CustomerService(_customerRepository, _contractService, _roleService, _actionOverridesRepository);
    }

    [Fact]
    public void GetCustomersSummary_ShouldCallCustomerRepositoryAndReturnAdaptedResponse()
    {
        UserDto userAuth = new UserDto();
        var customersModel = new List<bs_CustomerSite>
        {
            new bs_CustomerSite
            {
                bs_SiteNumber = "SiteNum1",
                bs_SiteName = "Company A",
                bs_District = "District A",
                bs_Contract_CustomerSite = new List<bs_Contract>
                {
                    new bs_Contract()
                    {
                        bs_ContractTypeString = "Rev Share A",
                        bs_Deposits = true,
                        bs_BillingType = bs_billingtypechoices.Arrears
                    }
                },
                bs_CustomerSite_cr9e8_readyforinvoice = new List<cr9e8_readyforinvoice>
                {
                    new cr9e8_readyforinvoice()
                    {
                        cr9e8_comments = "Comment A",
                        cr9e8_invoicestatus = "Status A",
                        cr9e8_period = "Period A"
                    }
                },
                bs_BillingStatement_CustomerSite = new List<bs_BillingStatement>
                {
                    new bs_BillingStatement()
                    {
                        bs_BillingStatementId = Guid.Empty
                    }
                }
            },
            new bs_CustomerSite
            {
                bs_SiteNumber = "SiteNum2",
                bs_SiteName = "Company B",
                bs_District = "District B",
                bs_Contract_CustomerSite = new List<bs_Contract>
                {
                    new bs_Contract()
                    {
                        bs_ContractTypeString = "Rev Share B",
                        bs_Deposits = true,
                        bs_BillingType = bs_billingtypechoices.Advanced
                    }
                },
                bs_CustomerSite_cr9e8_readyforinvoice = new List<cr9e8_readyforinvoice>
                {
                    new cr9e8_readyforinvoice()
                    {
                        cr9e8_comments = "Comment B",
                        cr9e8_invoicestatus = "Status B",
                        cr9e8_period = "Period B"
                    }
                },
                bs_BillingStatement_CustomerSite = new List<bs_BillingStatement>
                {
                    new bs_BillingStatement()
                    {
                        bs_BillingStatementId = Guid.NewGuid()
                    }
                }
            }
        };
        _customerRepository.GetCustomersSummary().Returns(customersModel);
        
        var customersVo = new List<CustomerSummaryVo>
        {
            new CustomerSummaryVo
            {
                SiteNumber = "SiteNum1",
                SiteName = "Company A",
                District = "District A",
                ContractType = "Rev Share A",
                Deposits = true,
                BillingType = BillingType.Arrears.ToString(),
                Period = "Period A",
                ReadyForInvoiceStatus = "Status A",
                IsStatementGenerated = false
            },
            new CustomerSummaryVo
            {
                SiteNumber = "SiteNum2",
                SiteName = "Company B",
                District = "District B",
                ContractType = "Rev Share B",
                Deposits = true,
                BillingType = BillingType.Advanced.ToString(),
                Period = "Period B",
                ReadyForInvoiceStatus = "Status B",
                IsStatementGenerated = true
            }
        };
        var result = _customerService.GetCustomersSummary(userAuth, false);
        result.Should().BeEquivalentTo(customersVo);
    }
    
    [Fact]
    public void GetCustomerDetail_ShouldCallCustomerRepositoryAndReturnAdaptedResponse()
    {
        var customerId = Guid.NewGuid();
        var customerDetailModel = new bs_CustomerSite()
        {
            bs_CustomerSiteId = customerId,
            bs_Address = "1234 Elm St",
            bs_SiteName = "Marriot Bethesda",
            bs_AccountManager = "Lucas Vaz",
            bs_SiteNumber = "0419",
            bs_InvoiceRecipient = "John Doe",
            bs_BillingContactEmail = "john_doe@gmail.com",
            bs_TotalRoomsAvailable = "100",
            bs_TotalAvailableParking = "50",
            bs_DistrictManager = "Jane Smith",
            bs_AssistantDistrictManager = "Bob Wilson",
            bs_AssistantAccountManager = "Alice Brown",
            bs_VendorId = "V12345"
        };
        _customerRepository.GetCustomerDetail(customerId).Returns(customerDetailModel);

        var customerDetailVo = new CustomerDetailVo
        {
            CustomerSiteId = customerId,
            Address = "1234 Elm St",
            SiteName = "Marriot Bethesda",
            AccountManager = "Lucas Vaz",
            SiteNumber = "0419",
            InvoiceRecipient = "John Doe",
            BillingContactEmail = "john_doe@gmail.com",
            TotalRoomsAvailable = "100",
            TotalAvailableParking = "50",
            DistrictManager = "Jane Smith",
            AssistantDistrictManager = "Bob Wilson",
            AssistantAccountManager = "Alice Brown",
            VendorId = "V12345"
        };
        var result = _customerService.GetCustomerDetail(customerId);
        result.Should().BeEquivalentTo(customerDetailVo);
    }
    
    [Fact]
    public void UpdateCustomerDetail_ShouldUpdateCustomerDetails()
    {
        var customerSiteId = Guid.NewGuid();
        var existingCustomerDetailModel = new bs_CustomerSite
        {
            bs_CustomerSiteId = customerSiteId,
            bs_Address = "1234 Elm St",
            bs_SiteName = "Old Site",
            bs_AccountManager = "Old Manager",
            bs_SiteNumber = "1234",
            bs_InvoiceRecipient = "Old Contact",
            bs_BillingContactEmail = "old.contact@example.com"
        };

        var newCustomerDetailVo = new CustomerDetailVo
        {
            CustomerSiteId = customerSiteId,
            Address = "5678 Elm St",
            SiteName = "New Site",
            AccountManager = "New Manager",
            SiteNumber = "5678",
            InvoiceRecipient = "New Contact",
            BillingContactEmail = "new.contact@example.com"
        };

        _customerRepository.GetCustomerDetail(customerSiteId).Returns(existingCustomerDetailModel);

        _customerService.UpdateCustomerDetail(customerSiteId, newCustomerDetailVo);

        _customerRepository.Received(1).UpdateCustomerDetail(customerSiteId, Arg.Is<bs_CustomerSite>(model =>
            model.bs_SiteName == newCustomerDetailVo.SiteName
            && model.bs_Address == newCustomerDetailVo.Address
            && model.bs_AccountManager == newCustomerDetailVo.AccountManager
            && model.bs_SiteNumber == newCustomerDetailVo.SiteNumber
            && model.bs_InvoiceRecipient == newCustomerDetailVo.InvoiceRecipient
            && model.bs_BillingContactEmail == newCustomerDetailVo.BillingContactEmail
        ));
    }
    
    [Fact]
    public void UpdateCustomerDetail_ShouldUpdateCustomerDetails_WhenNotAllFieldsHaveChanged()
    {
        var customerSiteId = Guid.NewGuid();
        var existingCustomerDetailModel = new bs_CustomerSite
        {
            bs_CustomerSiteId = customerSiteId,
            bs_SiteName = "Old Site",
            bs_Address = "1234 Elm St",
            bs_AccountManager = "Old Manager",
            bs_SiteNumber = "1234",
            bs_InvoiceRecipient = "Old Contact",
            bs_BillingContactEmail = "old.contact@example.com"
        };

        var newCustomerDetailVo = new CustomerDetailVo
        {
            CustomerSiteId = customerSiteId,
            Address = "5678 Elm St",
            SiteName = "New Site",
            AccountManager = "New Manager",
            SiteNumber = "1234",
            InvoiceRecipient = "New Contact",
            BillingContactEmail = "old.contact@example.com"
        };

        _customerRepository.GetCustomerDetail(customerSiteId).Returns(existingCustomerDetailModel);

        _customerService.UpdateCustomerDetail(customerSiteId, newCustomerDetailVo);

        _customerRepository.Received(1).UpdateCustomerDetail(customerSiteId, Arg.Is<bs_CustomerSite>(model =>
            model.bs_SiteName == newCustomerDetailVo.SiteName
            && model.bs_AccountManager == newCustomerDetailVo.AccountManager
            && model.bs_SiteNumber == null
            && model.bs_InvoiceRecipient == newCustomerDetailVo.InvoiceRecipient
            && model.bs_BillingContactEmail == null
        ));
    }
}