namespace api.Models.Vo
{
    public class InvoiceDetailVo
    {
        public decimal? Amount { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? PaymentTerms { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? PurchaseOrder { get; set; }
        public List<LineItemVo>? LineItems { get; set; }
        public Microsoft.Xrm.Sdk.EntityReference? BillingStatementFK { get; set; }
        public Guid? InvoiceGroupFK { get; set; }
    }
}
