using api.Adapters.Mappers;
using api.Adapters.Impl;
using api.Models.Dto;
using api.Models.Vo;
using api.Services;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace BackendTests.Adapters
{
    public class InvoiceServiceAdapterTest
    {
        private readonly IInvoiceService _invoiceService;
        private readonly InvoiceServiceAdapter _invoiceServiceAdapter;

        public InvoiceServiceAdapterTest()
        {
            _invoiceService = Substitute.For<IInvoiceService>();
            _invoiceServiceAdapter = new InvoiceServiceAdapter(_invoiceService);
        }

        [Fact]
        public void GetInvoiceDetail_ShouldCallInvoiceServiceAndReturnAdaptedResponse()
        {
            var invoiceId = Guid.NewGuid();
            var invoiceDetailVo = new InvoiceDetailVo
            {
                Amount = 672.00m,
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
            _invoiceService.GetInvoiceDetail(invoiceId).Returns(invoiceDetailVo);

            var expectedInvoiceDto = new InvoiceDetailDto
            {
                Amount = 672.00m,
                InvoiceDate = "2024-07-01",
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

            var result = _invoiceServiceAdapter.GetInvoiceDetail(invoiceId);
            result.Should().BeEquivalentTo(expectedInvoiceDto);
        }

        [Fact]
        public void AddAdhocLineItems_ShouldCallInvoiceServiceWithAdaptedVoModel()
        {
            // Arrange
            var invoiceId = Guid.NewGuid();
            var adhocLineItemDtos = new List<LineItemDto>
            {
                new LineItemDto { Title = "Service2", Description = "", Code = "4765", Amount = 222 },
                new LineItemDto { Title = "Service3", Description = "", Code = "4700", Amount = 300 },
                new LineItemDto { Title = "Service1", Description = "", Code = "4710", Amount = 150 },
            };

            // Act
            _invoiceServiceAdapter.AddAdhocLineItems(invoiceId, adhocLineItemDtos);

            // Assert
            _invoiceService.Received(1).AddAdHocLineItems(
                invoiceId,
                Arg.Is<IEnumerable<LineItemVo>>(vos =>
                    vos.SequenceEqual(InvoiceDetailMapper.LineItemsDtoToVo(adhocLineItemDtos), new AdHocLineItemVoComparer()))
            );
        }

        private class AdHocLineItemVoComparer : IEqualityComparer<LineItemVo>
        {
            public bool Equals(LineItemVo x, LineItemVo y)
            {
                if (x == null || y == null) return false;
                return x.Title == y.Title && x.Description == y.Description && x.Code == y.Code && x.Amount == y.Amount;
            }

            public int GetHashCode(LineItemVo obj)
            {
                return HashCode.Combine(obj.Title, obj.Description, obj.Code, obj.Amount);
            }
        }
    }
}