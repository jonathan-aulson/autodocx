using Newtonsoft.Json;

namespace api.Models.Dto
{
    public class BillingStatementDto
    {
        public BillingStatementDto()
        {
            Invoices = new List<InvoiceSummaryDto>();
        }

        [JsonProperty("id")]
        public Guid? Id { get; set; }

        [JsonProperty("createdMonth")]
        public string? CreatedMonth { get; set; }

        [JsonProperty("servicePeriod")]
        public string? ServicePeriod { get; set; }

        [JsonProperty("totalAmount")]
        public decimal? TotalAmount { get; set; }

        [JsonProperty("status")]
        public string? Status { get; set; }

        [JsonProperty("customerSiteId")]
        public Guid? CustomerSiteId { get; set; }

        [JsonProperty("siteNumber")]
        public string? SiteNumber { get; set; }
        
        [JsonProperty("siteName")]
        public string? SiteName { get; set; }

        [JsonProperty("amNotes")]
        public string? AmNotes { get; set; }

        [JsonProperty("forecastData")]
        public string? ForecastData { get; set; }

        [JsonProperty("servicePeriodStart")]
        public DateOnly? ServicePeriodStart { get; set; }

        [JsonProperty("statementVersionNumber")]
        public string? StatementVersionNumber { get; set; }

        [JsonProperty("purchaseOrder")]
        public string? PurchaseOrder { get; set; }

        [JsonProperty("invoices")]
        public IEnumerable<InvoiceSummaryDto> Invoices { get; set; }
    }
    
    public class InvoiceSummaryDto
    {
        [JsonProperty("id")]
        public Guid? Id { get; set; }

        [JsonProperty("invoiceNumber")]
        public string? InvoiceNumber { get; set; }

        [JsonProperty("amount")]
        public decimal? Amount { get; set; }
    }
}
