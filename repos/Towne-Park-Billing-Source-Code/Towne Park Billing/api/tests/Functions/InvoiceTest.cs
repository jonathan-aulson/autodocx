using System.Net;
using System.Text;
using api.Adapters;
using api.Functions;
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
    public class InvoiceTest
    {
        private readonly Invoice _invoice;
        private readonly IInvoiceServiceAdapter _invoiceServiceAdapterMock;

        public InvoiceTest()
        {
            var loggerFactoryMock = Substitute.For<ILoggerFactory>();
            _invoiceServiceAdapterMock = Substitute.For<IInvoiceServiceAdapter>();
            _invoice = new Invoice(loggerFactoryMock, _invoiceServiceAdapterMock);

            loggerFactoryMock.CreateLogger<Invoice>().Returns(NullLogger<Invoice>.Instance);
        }

        [Fact]
        public void GetInvoiceDetail_ShouldReturnInvoiceDetail_WhenInvoiceExists()
        {
            var invoiceId = Guid.NewGuid();
            var expectedInvoice = new InvoiceDetailDto
            {
                Amount = 672.00m,
                InvoiceDate = new DateTime(2024, 7, 1).ToString(),
                InvoiceNumber = "01702404",
                PaymentTerms = "Due by 1st Day of the Month",
                Title = "Example Hospital Site #1",
                Description = "Description",
                LineItems = new List<LineItemDto>
                {
                    new LineItemDto { Title = "Service1", Description = "", Code = "4710", Amount = 150 },
                    new LineItemDto { Title = "Service2", Description = "", Code = "4765", Amount = 222 },
                    new LineItemDto { Title = "Service3", Description = "", Code = "4700", Amount = 300 }
                }
            };

            _invoiceServiceAdapterMock.GetInvoiceDetail(invoiceId).Returns(expectedInvoice);

            var context = Substitute.For<FunctionContext>();
            var body = new MemoryStream();
            var requestData = new FakeHttpRequestData(
                context,
                new Uri($"http://localhost:7275/api/invoice/{invoiceId}/detail"),
                body);

            var result = _invoice.GetInvoiceDetail(requestData, invoiceId);
            result.StatusCode.Should().Be(HttpStatusCode.OK);

            result.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = new StreamReader(result.Body).ReadToEnd();
            var invoice = JsonConvert.DeserializeObject<InvoiceDetailDto>(responseBody);

            invoice.Should()
                .NotBeNull()
                .And.BeEquivalentTo(expectedInvoice);
        }

        [Fact]
        public void AdhocLineItem_ShouldAddLineItemsAndReturnOk()
        {
            // Arrange
            var invoiceId = Guid.NewGuid();
            var adhocLineItems = new List<LineItemDto>
            {
                new LineItemDto { Code = "1111", Title = "Line Item 1", Description = "Test Item 1", Amount = 100.00m },
                new LineItemDto { Code = "2222", Title = "Line Item 2", Description = "Test Item 2", Amount = 200.00m }
            };

            var context = Substitute.For<FunctionContext>();
            var requestBody = JsonConvert.SerializeObject(adhocLineItems);
            var requestData = new FakeHttpRequestData(
                context,
                new Uri($"http://localhost:7275/api/invoice/{invoiceId}/adhoc"),
                new MemoryStream(Encoding.UTF8.GetBytes(requestBody))
            );

            // Act
            var result = _invoice.AdhocLineItem(requestData, invoiceId);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Body.Seek(0, SeekOrigin.Begin);
            result.Headers.Contains("Content-Type").Should().BeTrue();

            var contentType = result.Headers.GetValues("Content-Type");
            contentType.Should().ContainSingle().Which.Should().Be("application/json; charset=utf-8");
        }
    }
}