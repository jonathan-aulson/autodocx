using api.Models.Vo;

namespace api.Services;

public interface IInvoiceService
{
    InvoiceDetailVo GetInvoiceDetail(Guid invoiceId);
    void AddAdHocLineItems(Guid invoiceId, IEnumerable<LineItemVo> adHocLineItems);
    void DeleteAdhocLineItem(Guid invoiceId, Guid lineItemId);
}