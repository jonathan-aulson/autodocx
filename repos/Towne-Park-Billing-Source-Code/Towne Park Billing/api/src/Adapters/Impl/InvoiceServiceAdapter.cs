using api.Adapters.Mappers;
using api.Models.Dto;
using api.Services;

namespace api.Adapters.Impl;

public class InvoiceServiceAdapter : IInvoiceServiceAdapter
{
    private readonly IInvoiceService _invoiceService;

    public InvoiceServiceAdapter(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    public InvoiceDetailDto GetInvoiceDetail(Guid invoiceId)
    {
        return Mappers.InvoiceDetailMapper.InvoiceDetailVoToDto(_invoiceService.GetInvoiceDetail(invoiceId));
    }

    public void AddAdhocLineItems(Guid invoiceId, IEnumerable<LineItemDto> adhocLineItems)
    {
        _invoiceService.AddAdHocLineItems(invoiceId, InvoiceDetailMapper.LineItemsDtoToVo(adhocLineItems));
    }

    public void DeleteAdhocLineItem(Guid invoiceId, Guid lineItemId)
    {
        _invoiceService.DeleteAdhocLineItem(invoiceId, lineItemId);
    }
}