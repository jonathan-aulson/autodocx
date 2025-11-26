using TownePark;

namespace api.Data;

public interface IInvoiceRepository
{
    bs_Invoice GetInvoiceDetail(Guid invoiceId);
    bs_Invoice GetInvoiceData(Guid invoiceId);
    void UpdateInvoiceData(Guid invoiceId, decimal updatedTotal, string updatedInvoiceData);
}