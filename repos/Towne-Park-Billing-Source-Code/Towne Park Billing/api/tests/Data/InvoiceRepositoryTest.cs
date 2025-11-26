using System;
using api.Data.Impl;
using api.Models.Dto;
using api.Models.Vo;
using api.Services;
using FluentAssertions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using NSubstitute;
using TownePark;
using Xunit;

namespace BackendTests.Data
{
    public class InvoiceRepositoryTest
    {
        private readonly IOrganizationService _organizationService;

        private readonly InvoiceRepository _invoiceRepository;

        public InvoiceRepositoryTest()
        {
            var dataverseService = Substitute.For<IDataverseService>();

            _organizationService = Substitute.For<IOrganizationService>();
            dataverseService.GetServiceClient().Returns(_organizationService);

            _invoiceRepository = new InvoiceRepository(dataverseService);
        }

        [Fact]
        public void GetInvoiceDetail_ShouldReturnInvoiceDetail()
        {
            var invoiceId = Guid.NewGuid();
            var expectedInvoice = new bs_Invoice
            {
                bs_InvoiceId = Guid.NewGuid(),
                bs_InvoiceGroupFK = new EntityReference("bs_invoicegroupfk", Guid.NewGuid()),
                bs_InvoiceNumber = "01702404",
                bs_InvoiceDate = new DateTime(2024, 7, 1),
                bs_Amount = 672.00m,
                bs_InvoiceData = "[{\"title\":\"Example Hospital Site #1\",\"description\":\"Description\",\"lineItems\":[{\"title\":\"Service1\",\"description\":\"\",\"code\":\"4710\",\"amount\":150},{\"title\":\"Service2\",\"description\":\"\",\"code\":\"4765\",\"amount\":222},{\"title\":\"Service3\",\"description\":\"\",\"code\":\"4700\",\"amount\":300}]}]",
                bs_PaymentTerms = "Due by 1st Day of the Month",
                bs_PurchaseOrder = "PO-12345"
            }.ToEntity<bs_Invoice>();

            var columnSetCapture = Arg.Do<ColumnSet>(cs =>
            {
                cs.AllColumns.Should().BeFalse();
                cs.Columns.Should().BeEquivalentTo(new[]
                {
                    bs_Invoice.Fields.bs_InvoiceId,
                    bs_Invoice.Fields.bs_InvoiceGroupFK,
                    bs_Invoice.Fields.bs_InvoiceNumber,
                    bs_Invoice.Fields.bs_InvoiceDate,
                    bs_Invoice.Fields.bs_Amount,
                    bs_Invoice.Fields.bs_InvoiceData,
                    bs_Invoice.Fields.bs_PaymentTerms,
                    bs_Invoice.Fields.bs_Title,
                    bs_Invoice.Fields.bs_Description,
                    bs_Invoice.Fields.bs_PurchaseOrder
                });
            });

            _organizationService.Retrieve(
                bs_Invoice.EntityLogicalName,
                invoiceId,
                columnSetCapture
            ).Returns(expectedInvoice);

            var result = _invoiceRepository.GetInvoiceDetail(invoiceId);

            result.Should().BeEquivalentTo(new bs_Invoice
            {
                bs_InvoiceId = expectedInvoice.bs_InvoiceId,
                bs_InvoiceGroupFK = expectedInvoice.bs_InvoiceGroupFK,
                bs_InvoiceNumber = expectedInvoice.bs_InvoiceNumber,
                bs_InvoiceDate = expectedInvoice.bs_InvoiceDate,
                bs_Amount = expectedInvoice.bs_Amount,
                bs_InvoiceData = expectedInvoice.bs_InvoiceData,
                bs_PaymentTerms = expectedInvoice.bs_PaymentTerms,
                bs_PurchaseOrder = expectedInvoice.bs_PurchaseOrder
            });
        }

        [Fact]
        public void UpdateInvoiceData_ShouldUpdateInvoiceData()
        {
            var invoiceId = Guid.NewGuid();
            const decimal updatedTotal = 200m;
            const string updatedInvoiceData = "[{\"title\":\"Example Hospital Site #1\",\"description\":\"Description\",\"lineItems\":[{\"title\":\"Service1\",\"description\":\"\",\"code\":\"4710\",\"amount\":150},{\"title\":\"Service2\",\"description\":\"\",\"code\":\"4765\",\"amount\":222},{\"title\":\"Service3\",\"description\":\"\",\"code\":\"4700\",\"amount\":300}]}]";

            _invoiceRepository.UpdateInvoiceData(invoiceId, updatedTotal, updatedInvoiceData);

            _organizationService.Received(1).Update(Arg.Is<bs_Invoice>(i =>
                i.bs_InvoiceId == invoiceId &&
                i.bs_Amount == updatedTotal &&
                i.bs_InvoiceData == updatedInvoiceData));
        }
    }
}

