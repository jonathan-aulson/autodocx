using api.Models.Dto;

namespace api.Adapters;

public interface IInvoiceServiceAdapter
{
    InvoiceDetailDto GetInvoiceDetail(Guid invoiceId);
    void AddAdhocLineItems(Guid invoiceId, IEnumerable<LineItemDto> adhocLineItems);
    void DeleteAdhocLineItem(Guid invoiceId, Guid lineItemId);
}