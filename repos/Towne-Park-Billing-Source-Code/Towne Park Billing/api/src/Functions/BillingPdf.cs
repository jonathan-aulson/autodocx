using System.Net;
using api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace api.Functions
{
    public class BillingPdf
    {
        private readonly ILogger _logger;
        private readonly IInvoicePdfService _invoicePdfService;

        public BillingPdf(ILoggerFactory loggerFactory, IInvoicePdfService invoicePdfService)
        {
            _logger = loggerFactory.CreateLogger<BillingPdf>();
            _invoicePdfService = invoicePdfService;
        }

        [Function("Billing")]
        public HttpResponseData Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "customers/{customerSiteId}/billing")]
            HttpRequestData req,
            Guid customerSiteId)
        {
            var (fileName, memoryStream) = _invoicePdfService.GeneratePdf(customerSiteId);
            var response = req.CreateResponse(HttpStatusCode.OK);

            response.Headers.Add("Content-Type", "application/pdf");
            response.Headers.Add("Content-Disposition", "attachment;filename="+fileName);
            response.Body = memoryStream;
            
            _logger.LogInformation("Invoice PFD generated successfully.");
            return response;
        }
    }
}