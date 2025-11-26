using Newtonsoft.Json;
using System.Text.Json;

namespace api.Models.Dto
{
    public class BillingStatementPdfDto
    {
        public BillingStatementPdfDto()
        {
            Invoices = new List<InvoiceDetailDto>();
        }

        [JsonProperty("id")]
        public Guid? Id { get; set; }

        [JsonProperty("createdMonth")]
        public string? CreatedMonth { get; set; }

        [JsonProperty("servicePeriodStart")]
        public DateOnly? ServicePeriodStart { get; set; }

        [JsonProperty("servicePeriodEnd")]
        public DateOnly? ServicePeriodEnd { get; set; }

        [JsonProperty("totalAmount")]
        public decimal? TotalAmount { get; set; }

        [JsonProperty("status")]
        public string? Status { get; set; }

        [JsonProperty("amNotes")]
        public string? AmNotes { get; set; }

        [JsonProperty("forecastData")]
        public string? ForecastData { get; set; }

        [JsonProperty("invoices")]
        public IEnumerable<InvoiceDetailDto> Invoices { get; set; }


        [JsonProperty("customerSiteData")]
        public CustomerDetailDto? CustomerSiteData { get; set; }

        [JsonProperty("generalConfig")]
        public List<InvoiceConfigDto>? GeneralConfig { get; set; }

        [JsonProperty("purchaseOrder")]
        public string? PurchaseOrder { get; set; }
    }

    //public class GeneralConfigDto
    //{
    //    [JsonProperty("key")]
    //    public string? Key { get; set; }

    //    [JsonProperty("value")]
    //    public string? Value { get; set; }
    //}
}
