using api.Services;
using Microsoft.Xrm.Sdk.Query;
using TownePark;

namespace api.Data.Impl;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly IDataverseService _dataverseService;

    public InvoiceRepository(IDataverseService dataverseService)
    {
        _dataverseService = dataverseService;
    }

    public bs_Invoice GetInvoiceDetail(Guid invoiceId)
    {
        var columnSet = new ColumnSet(bs_Invoice.Fields.bs_InvoiceId, bs_Invoice.Fields.bs_InvoiceNumber,
            bs_Invoice.Fields.bs_InvoiceDate, bs_Invoice.Fields.bs_Amount, bs_Invoice.Fields.bs_InvoiceData,
            bs_Invoice.Fields.bs_PaymentTerms, bs_Invoice.Fields.bs_Title, bs_Invoice.Fields.bs_Description,
            bs_Invoice.Fields.bs_PurchaseOrder, bs_Invoice.Fields.bs_InvoiceGroupFK);
        
        return GetInvoiceDetailWithColumnSet(invoiceId, columnSet);
    }

    public bs_Invoice GetInvoiceData(Guid invoiceId)
    {
        var columnSet = new ColumnSet(bs_Invoice.Fields.bs_InvoiceData, bs_Invoice.Fields.bs_BillingStatementFK);
        return GetInvoiceDetailWithColumnSet(invoiceId, columnSet);
    }

    private bs_Invoice GetInvoiceDetailWithColumnSet(Guid invoiceId, ColumnSet columnSet)
    {
        var serviceClient = _dataverseService.GetServiceClient();

        return (bs_Invoice) serviceClient.Retrieve(bs_Invoice.EntityLogicalName, invoiceId, columnSet);
    }

    public void UpdateInvoiceData(Guid invoiceId, decimal updatedTotal, string updatedInvoiceData)
    {
        var serviceClient = _dataverseService.GetServiceClient();
        var invoice = new bs_Invoice()
        {
            Id = invoiceId,
            bs_Amount = updatedTotal,
            bs_InvoiceData = updatedInvoiceData
        };
        serviceClient.Update(invoice);
    }
}