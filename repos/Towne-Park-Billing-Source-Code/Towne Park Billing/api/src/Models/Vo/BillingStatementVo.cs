using api.Models.Vo.Enum;
using Newtonsoft.Json;

namespace api.Models.Vo
{
    public class BillingStatementVo
    {
        public BillingStatementVo()
        {
            Invoices = new List<InvoiceSummaryVo>();
        }

        public Guid? Id { get; set; }
        public string? CreatedMonth { get; set; }
        public DateOnly? ServicePeriodStart { get; set; }
        public DateOnly? ServicePeriodEnd { get; set; }
        public decimal? TotalAmount { get; set; }
        public StatementStatus? Status { get; set; }

        public Guid? CustomerSiteId { get; set; }
        public string? SiteNumber { get; set; }
        public string? SiteName { get; set; }
        public string? AmNotes { get; set; }
        public string? ForecastData { get; set; }
        public string? StatementVersionNumber { get; set; }
        public string? PurchaseOrder { get; set; }
        public IEnumerable<InvoiceSummaryVo> Invoices { get; set; }
    }

    public enum StatementStatus
    {
        Generating = 126840000,
        NeedsReview = 126840001,
        Approved = 126840002,
        Sent = 126840003,
        ArReview = 126840004,
        ApprovalTeam = 126840005,
        ReadyToSend = 126840006,
        Failed = 126840007
    }

    public class InvoiceSummaryVo
    {
        public Guid? Id { get; set; }

        public string? InvoiceNumber { get; set; }

        public decimal? Amount { get; set; }
    }

    public class ForecastDataVo
    {
        [JsonProperty("forecastedRevenue")]
        public decimal? ForecastedRevenue { get; set; }

        [JsonProperty("postedRevenue")]
        public decimal? PostedRevenue { get; set; }

        [JsonProperty("invoicedRevenue")]
        public decimal? InvoicedRevenue { get; set; }

        [JsonProperty("totalActualRevenue")]
        public decimal? TotalActualRevenue { get; set; }

        [JsonProperty("forecastDeviationPercentage")]
        public decimal? ForecastDeviationPercentage { get; set; }

        [JsonProperty("forecastDeviationAmount")]
        public decimal? ForecastDeviationAmount { get; set; }

        [JsonProperty("forecastLastUpdated")]
        public DateTime? ForecastLastUpdated { get; set; }
    }
}
