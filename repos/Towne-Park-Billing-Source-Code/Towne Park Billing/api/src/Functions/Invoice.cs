using System.Net;
using api.Adapters;
using api.Models.Dto;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace api.Functions;

public class Invoice
{
    private readonly ILogger _logger;

    private readonly IInvoiceServiceAdapter _invoiceServiceAdapter;

    public Invoice(ILoggerFactory loggerFactory, IInvoiceServiceAdapter invoiceServiceAdapter)
    {
        _logger = loggerFactory.CreateLogger<Invoice>();
        _invoiceServiceAdapter = invoiceServiceAdapter;
    }
    
    [Function("GetInvoice")]
    public HttpResponseData GetInvoiceDetail(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "invoice/{invoiceId}/detail")]
        HttpRequestData req,
        Guid invoiceId)
    {
        var invoice = _invoiceServiceAdapter.GetInvoiceDetail(invoiceId);

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        response.WriteString(JsonConvert.SerializeObject(invoice));

        _logger.LogInformation("Invoice detail retrieved successfully.");
        return response;
    }

    [Function("AdhocLineItem")]
    public HttpResponseData AdhocLineItem(
        [HttpTrigger(AuthorizationLevel.Function, "patch", Route = "invoice/{invoiceId}/adhoc")]
        HttpRequestData req,
        Guid invoiceId)
    {
        var body = new StreamReader(req.Body).ReadToEnd();
        var adhocLineItems = JsonConvert.DeserializeObject<IEnumerable<LineItemDto>>(body);

        if (adhocLineItems != null)
        {
            _invoiceServiceAdapter.AddAdhocLineItems(invoiceId, adhocLineItems);
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");

        _logger.LogInformation("Adhoc line items added successfully.");
        return response;
    }

    [Function("DeleteAdhocLineItem")]
    public HttpResponseData DeleteAdhocLineItem(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "invoice/{invoiceId}/adhoc/{lineItemId}")]
        HttpRequestData req,
        Guid invoiceId,
        Guid lineItemId)
    {
        _invoiceServiceAdapter.DeleteAdhocLineItem(invoiceId, lineItemId);

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");

        _logger.LogInformation("Adhoc line item deleted successfully.");
        return response;
    }
}