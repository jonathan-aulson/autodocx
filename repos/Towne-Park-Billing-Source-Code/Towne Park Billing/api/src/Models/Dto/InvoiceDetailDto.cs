using Newtonsoft.Json;

namespace api.Models.Dto
{
    public class InvoiceDetailDto
    {
        [JsonProperty("amount")]
        public decimal? Amount { get; set; }

        [JsonProperty("invoiceDate")]
        public string? InvoiceDate { get; set; }

        [JsonProperty("invoiceNumber")]
        public string? InvoiceNumber { get; set; }

        [JsonProperty("paymentTerms")]
        public string? PaymentTerms { get; set; }
        
        [JsonProperty("title")]
        public string? Title { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }

        [JsonProperty("purchaseOrder")]
        public string? PurchaseOrder { get; set; }

        [JsonProperty("lineItems")]
        public List<LineItemDto>? LineItems { get; set; }

        [JsonProperty("invoiceGroupFK")]
        public Guid? InvoiceGroupFK { get; set; }
    }
}
