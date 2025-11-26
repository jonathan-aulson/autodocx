namespace api.Services;

public interface IInvoicePdfService
{
    (string, MemoryStream) GeneratePdf(Guid customerId);
}