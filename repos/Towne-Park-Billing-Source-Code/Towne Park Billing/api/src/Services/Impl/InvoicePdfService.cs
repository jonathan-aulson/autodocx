using System.Net;
using MigraDoc.Rendering;
using PdfSharp.Fonts;
using PdfSharp.Snippets.Font;

namespace api.Services.Impl;

public class InvoicePdfService : IInvoicePdfService
{
    private readonly ICustomerService _customerService;
    private readonly InvoiceDocumentTemplate _invoiceDocumentTemplate;

    public InvoicePdfService(ICustomerService customerService, InvoiceDocumentTemplate invoiceDocumentTemplate)
    {
        _customerService = customerService;
        _invoiceDocumentTemplate = invoiceDocumentTemplate;
        GlobalFontSettings.FontResolver ??= new NewFontResolver(); // TODO move to configuration
    }

    /// <summary>
    /// Generates a PDF document for a specific customer.
    /// TODO consider introducing Adapter.
    /// TODO consider returning value object instead of a tuple.
    /// </summary>
    /// <param name="customerId">The given custoer id.</param>
    /// <returns>A tuple with the name of the file and the MemoryStream object.</returns>
    public (string, MemoryStream) GeneratePdf(Guid customerId)
    {
        var customer = _customerService.GetCustomerDetail(customerId);
        var document = _invoiceDocumentTemplate.CreateDocument(customer);
            
        // Create a renderer for PDF that uses Unicode font encoding.
        var pdfRenderer = new PdfDocumentRenderer
        {
            // Set the MigraDoc document.
            Document = document
        };

        // Create the PDF document.
        pdfRenderer.RenderDocument();
            
        var ms = new MemoryStream();
        pdfRenderer.PdfDocument.Save(ms);
        ms.Position = 0;
        var filename = $@"TowneParkInvoice_{customer.SiteNumber}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.pdf";
        return (filename, ms);
    }
}