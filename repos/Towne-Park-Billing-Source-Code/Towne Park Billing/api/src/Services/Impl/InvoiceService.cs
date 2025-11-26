using api.Adapters.Mappers;
using api.Data;
using api.Models.Vo;
using Newtonsoft.Json;

namespace api.Services.Impl;

public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IBillingStatementRepository _billingStatementRepository;

    public InvoiceService(IInvoiceRepository invoiceRepository, IBillingStatementRepository billingStatementRepository)
    {
        _invoiceRepository = invoiceRepository;
        _billingStatementRepository = billingStatementRepository;
    }

    public InvoiceDetailVo GetInvoiceDetail(Guid invoiceId)
    {
        var invoice = _invoiceRepository.GetInvoiceDetail(invoiceId);
        return InvoiceDetailMapper.InvoiceDetailModelToVo(invoice);
    }

    public void AddAdHocLineItems(Guid invoiceId, IEnumerable<LineItemVo> adHocLineItems)
    {
        var currentInvoice = InvoiceDetailMapper.InvoiceDetailModelToVo(_invoiceRepository.GetInvoiceData(invoiceId));

        adHocLineItems = adHocLineItems.ToList();

        foreach (LineItemVo lineItem in adHocLineItems)
        {
            if (lineItem.MetaData == null) lineItem.MetaData = new MetaDataVo();
            if (lineItem.MetaData.LineItemId == null)
            {
                lineItem.MetaData.LineItemId = Guid.NewGuid();
            }
        }

        // TODO Are we sure they are all new items?
        // TODO Validate the codes actually correspond to the items.
        var updatedLineItems = currentInvoice.LineItems != null
                ? currentInvoice.LineItems.Union(adHocLineItems).ToList()
                : adHocLineItems.ToList();

        var updatedTotal = updatedLineItems
            .Aggregate(0m, (sum, next) => next.Amount.HasValue ? sum + next.Amount.Value : sum);
        _invoiceRepository.UpdateInvoiceData(invoiceId, updatedTotal,
            InvoiceDetailMapper.LineItemsVoToModel(updatedLineItems));

        var adhocLineItemTotal = adHocLineItems
               .Where(item => int.TryParse(item.Code, out int code) && code >= 4000 && code <= 4999)
               .Aggregate(0m, (sum, next) => next.Amount.HasValue ? sum + next.Amount.Value : sum);

        if (currentInvoice.BillingStatementFK != null)
        {
            UpdateForecastData(currentInvoice.BillingStatementFK.Id, adhocLineItemTotal);
        }
    }

    public void UpdateForecastData(Guid billingStatementId, decimal adhocLineItemTotal)
    {
        var billingStatement = BillingStatementMapper.MapBillingStatementModelToVo(
            _billingStatementRepository.GetBillingStatementById(billingStatementId)
        );

        if (string.IsNullOrEmpty(billingStatement.ForecastData)) return;

        var forecastData = JsonConvert.DeserializeObject<ForecastDataVo>(billingStatement.ForecastData);

        if (forecastData != null) {
            // Recalculate derived fields
            forecastData.InvoicedRevenue += adhocLineItemTotal;
            forecastData.TotalActualRevenue = forecastData.PostedRevenue + forecastData.InvoicedRevenue;
            forecastData.ForecastDeviationAmount = Math.Abs((forecastData.ForecastedRevenue ?? 0) - (forecastData.TotalActualRevenue ?? 0));
            if (forecastData.ForecastedRevenue != 0)
            {
                forecastData.ForecastDeviationPercentage =
                    (forecastData.ForecastDeviationAmount / forecastData.ForecastedRevenue) * 100;
            }

            // Save the updated forecast data
            _billingStatementRepository.UpdateForecastData(billingStatementId, JsonConvert.SerializeObject(forecastData));
        }
    }

    public void DeleteAdhocLineItem(Guid invoiceId, Guid lineItemId)
    {
        var currentInvoice = InvoiceDetailMapper.InvoiceDetailModelToVo(_invoiceRepository.GetInvoiceData(invoiceId));

        var lineItems = currentInvoice.LineItems;

        if (lineItems == null || !lineItems.Any()) return;

        var deletedLineItemTotal = lineItems
            .Where(item => item.MetaData != null && item.MetaData.LineItemId == lineItemId)
            .Where(item => int.TryParse(item.Code, out int code) && code >= 4000 && code <= 4999)
            .Aggregate(0m, (sum, next) => next.Amount.HasValue ? sum + next.Amount.Value : sum);

        lineItems.RemoveAll(item => item.MetaData != null && item.MetaData.LineItemId == lineItemId);

        var updatedTotal = lineItems
            .Aggregate(0m, (sum, next) => next.Amount.HasValue ? sum + next.Amount.Value : sum);

        _invoiceRepository.UpdateInvoiceData(invoiceId, updatedTotal,
            InvoiceDetailMapper.LineItemsVoToModel(lineItems));

        var amountToSubtract = deletedLineItemTotal * -1;

        if (currentInvoice.BillingStatementFK != null)
        {
            UpdateForecastData(currentInvoice.BillingStatementFK.Id, amountToSubtract);
        }
    }
}