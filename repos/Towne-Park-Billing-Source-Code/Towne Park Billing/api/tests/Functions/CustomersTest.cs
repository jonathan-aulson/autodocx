using System.Net;
using System.Text;
using api.Adapters;
using api.Functions;
using api.Middleware;
using api.Models.Dto;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using NSubstitute;
using Xunit;

namespace BackendTests.Functions
{
    public class CustomersTest
    {
        private readonly Customers _customers;
        private readonly ICustomerServiceAdapter _customerServiceAdapterMock;

        public CustomersTest()
        {
            var loggerFactoryMock = Substitute.For<ILoggerFactory>();
            _customerServiceAdapterMock = Substitute.For<ICustomerServiceAdapter>();
            _customers = new Customers(loggerFactoryMock, _customerServiceAdapterMock);
            
            loggerFactoryMock.CreateLogger<Customers>().Returns(NullLogger<Customers>.Instance);
        }

        [Fact]
        public void GetCustomers_ShouldReturnCustomerList()
        {
            UserDto userDto = new UserDto
            {
                Email = "test@example.com",
                Roles = new[]
                {
                    "billingAdmin"
                }
            };

            var expectedCustomers = new List<CustomerSummaryDto>
            {
                new CustomerSummaryDto
                {
                    SiteNumber = "customerid1",
                    SiteName = "Company A"
                },
                new CustomerSummaryDto
                {
                    SiteNumber = "customerid2",
                    SiteName = "Company B"
                }
            };

            _customerServiceAdapterMock
                .GetCustomersSummary(Arg.Any<UserDto>(), Arg.Any<bool>())
                .Returns(expectedCustomers);

            var body = new MemoryStream();
            var context = Substitute.For<FunctionContext>();
            var requestData = new FakeHttpRequestData(
                context,
                new Uri("http://localhost:7275/api/customers?isForecast=true"),
                body);

            var items = new Dictionary<object, object>();
            items.Add(AuthenticationMiddleware.UserDtoKey, userDto);

            context.Items.Returns(items);

            var result = _customers.GetCustomers(requestData, context);
            result.StatusCode.Should().Be(HttpStatusCode.OK);

            result.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = new StreamReader(result.Body).ReadToEnd();

            var customers = JsonConvert.DeserializeObject<List<CustomerSummaryDto>>(responseBody);

            customers.Should()
                .NotBeEmpty()
                .And.HaveCount(2)
                .And.BeEquivalentTo(expectedCustomers);

            _customerServiceAdapterMock.Received(1).GetCustomersSummary(userDto, true);
        }

        [Fact]
        public void GetCustomerDetail_ShouldReturnCustomerDetail()
        {
            var customerId = Guid.NewGuid();
            var customerDetailDto = new CustomerDetailDto
            {
                CustomerSiteId = customerId
            };
            _customerServiceAdapterMock.GetCustomerDetail(customerId).Returns(customerDetailDto);

            var body = new MemoryStream();
            var context = Substitute.For<FunctionContext>();
            var requestData = new FakeHttpRequestData(
                context,
                new Uri($"http://localhost:7275/api/customers/{customerId}/detail"),
                body);

            var result = _customers.GetCustomerDetail(requestData, customerId);
            result.StatusCode.Should().Be(HttpStatusCode.OK);

            result.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = new StreamReader(result.Body).ReadToEnd();
            var customer = JsonConvert.DeserializeObject<CustomerDetailDto>(responseBody);

            customer.Should()
                .BeEquivalentTo(customerDetailDto);
        }
        
        [Fact]
        public void UpdateCustomerDetail_ShouldUpdateCustomerDetail()
        {
            var customerSiteId = Guid.NewGuid();
            var updateCustomerDto = new CustomerDetailDto
            {
                CustomerSiteId = customerSiteId,
                Address = "New Address 123",
                SiteName = "New Site",
                AccountManager = "New Manager",
                SiteNumber = "5678",
                InvoiceRecipient = "New Contact",
                BillingContactEmail = "new.contact@example.com"
            };

            var body = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(updateCustomerDto)));
            var context = Substitute.For<FunctionContext>();
            var requestData = new FakeHttpRequestData(
                context,
                new Uri($"http://localhost:7275/api/customers/{customerSiteId}/detail"),
                body);

            var result = _customers.UpdateCustomerDetail(requestData, customerSiteId);

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            _customerServiceAdapterMock.Received(1).UpdateCustomerDetail(customerSiteId, Arg.Is<CustomerDetailDto>(dto =>
                dto.CustomerSiteId == updateCustomerDto.CustomerSiteId
                && dto.SiteName == updateCustomerDto.SiteName
                && dto.Address == updateCustomerDto.Address
                && dto.AccountManager == updateCustomerDto.AccountManager
                && dto.SiteNumber == updateCustomerDto.SiteNumber
                && dto.InvoiceRecipient == updateCustomerDto.InvoiceRecipient
                && dto.BillingContactEmail == updateCustomerDto.BillingContactEmail
            ));
        }
    }
}
