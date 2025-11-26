using api.Adapters.Mappers;
using api.Data;
using api.Models.Vo;
using api.Services.Impl;
using FluentAssertions;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NSubstitute;
using TownePark;
using Xunit;

namespace BackendTests.Services
{
    public class InvoiceServiceTest
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly InvoiceService _invoiceService;
        private readonly IBillingStatementRepository _billingStatementRepository;

        public InvoiceServiceTest()
        {
            _invoiceRepository = Substitute.For<IInvoiceRepository>();
            _billingStatementRepository = Substitute.For<IBillingStatementRepository>();
            _invoiceService = new InvoiceService(_invoiceRepository, _billingStatementRepository);
        }

        [Fact]
        public void GetInvoiceDetail_ShouldCallInvoiceRepositoryAndReturnAdaptedResponse()
        {
            var invoiceId = Guid.NewGuid();
            var invoiceDetailModel = new bs_Invoice
            {
                bs_InvoiceId = invoiceId,
                bs_InvoiceGroupFK = new EntityReference("bs_invoicegroupfk", Guid.NewGuid()),
                bs_Amount = 672.00m,
                bs_InvoiceDate = new DateTime(2024, 7, 1),
                bs_InvoiceNumber = "01702404",
                bs_PaymentTerms = "Due by 1st Day of the Month",
                bs_Title = "Example Hospital Site #1",
                bs_Description = "Description",
                bs_InvoiceData =
                    "[{\"title\":\"Service1\",\"description\":\"\",\"code\":\"4710\",\"amount\":150},{\"title\":\"Service2\",\"description\":\"\",\"code\":\"4765\",\"amount\":222},{\"title\":\"Service3\",\"description\":\"\",\"code\":\"4700\",\"amount\":300}]"
            };
            _invoiceRepository.GetInvoiceDetail(invoiceId).Returns(invoiceDetailModel);

            var invoiceDetailVo = new InvoiceDetailVo
            {
                Amount = 672.00m,
                InvoiceGroupFK = invoiceDetailModel.bs_InvoiceGroupFK.Id,
                InvoiceDate = new DateTime(2024, 7, 1),
                InvoiceNumber = "01702404",
                PaymentTerms = "Due by 1st Day of the Month",
                Title = "Example Hospital Site #1",
                Description = "Description",
                LineItems = new List<LineItemVo>
                {
                    new LineItemVo { Title = "Service1", Description = "", Code = "4710", Amount = 150 },
                    new LineItemVo { Title = "Service2", Description = "", Code = "4765", Amount = 222 },
                    new LineItemVo { Title = "Service3", Description = "", Code = "4700", Amount = 300 }
                }
            };

            var result = _invoiceService.GetInvoiceDetail(invoiceId);
            result.Should().BeEquivalentTo(invoiceDetailVo);
        }

        [Fact]
        public void GetInvoiceDetail_ShouldReturnEmpty_WhenInvoiceDataIsNull()
        {
            var invoiceId = Guid.NewGuid();

            var invoiceDetailModel = new bs_Invoice
            {
                bs_InvoiceId = invoiceId,
                bs_InvoiceGroupFK = new EntityReference("bs_invoicegroupfk", Guid.NewGuid()),
                bs_Amount = 672.00m,
                bs_InvoiceDate = new DateTime(2024, 7, 1),
                bs_InvoiceNumber = "01702404",
                bs_PaymentTerms = "Due by 1st Day of the Month",
                bs_InvoiceData = null
            };
            _invoiceRepository.GetInvoiceDetail(invoiceId).Returns(invoiceDetailModel);

            var result = _invoiceService.GetInvoiceDetail(invoiceId);
            result.Should().BeEquivalentTo(new InvoiceDetailVo()
            {
                Amount = 672.00m,
                InvoiceGroupFK = invoiceDetailModel.bs_InvoiceGroupFK.Id,
                InvoiceDate = new DateTime(2024, 7, 1),
                InvoiceNumber = "01702404",
                PaymentTerms = "Due by 1st Day of the Month",
                LineItems = null
            });
        }

        [Fact]
        public void GetInvoiceDetail_ShouldThrowException_WhenInvoiceDataIsMalformedJson()
        {
            var invoiceId = Guid.NewGuid();
            var invoiceDetailModel = new bs_Invoice
            {
                bs_InvoiceId = invoiceId,
                bs_Amount = 672.00m,
                bs_InvoiceDate = new DateTime(2024, 7, 1),
                bs_InvoiceNumber = "01702404",
                bs_PaymentTerms = "Due by 1st Day of the Month",
                bs_InvoiceData = "{" // Malformed JSON
            };

            _invoiceRepository.GetInvoiceDetail(invoiceId).Returns(invoiceDetailModel);

            Action act = () => _invoiceService.GetInvoiceDetail(invoiceId);

            act.Should().Throw<JsonException>()
                .WithMessage("Unexpected end when reading JSON. Path '', line 1, position 1.");
        }

        [Fact]
        public void AddAdHocLineItems_ShouldAddAndUpdateInvoiceCorrectly()
        {
            // Arrange
            var invoiceId = Guid.NewGuid();

            var existingLineItems = new List<LineItemVo>
            {
                new LineItemVo { Title = "Existing Service", Description = "Description", Code = "123", Amount = 100 }
            };

            var adHocLineItems = new List<LineItemVo>
            {
                new LineItemVo { Title = "AdHoc Service", Description = "Description", Code = "456", Amount = 200 }
            };

            var invoiceVo = new bs_Invoice
            {
                bs_InvoiceData = JsonConvert.SerializeObject(existingLineItems)
            };

            var allLineItems = existingLineItems.Concat(adHocLineItems).ToList();

            _invoiceRepository.GetInvoiceData(invoiceId).Returns(invoiceVo);

            // Act
            _invoiceService.AddAdHocLineItems(invoiceId, adHocLineItems);

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            // Assert
            _invoiceRepository.Received(1).UpdateInvoiceData(invoiceId,
                300m,
                JsonConvert.SerializeObject(allLineItems, settings));
        }
    }
}