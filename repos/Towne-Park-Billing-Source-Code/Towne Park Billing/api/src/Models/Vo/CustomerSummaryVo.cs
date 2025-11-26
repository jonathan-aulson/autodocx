using api.Models.Vo.Enum;

namespace api.Models.Vo;

public class CustomerSummaryVo
{
    public Guid CustomerSiteId { get; set; }
    public string? SiteNumber { get; set; }
    public string? SiteName { get; set; }
    public string? District { get; set; }
    public string? BillingType { get; set; }
    public string? ContractType { get; set; }
    public bool? Deposits { get; set; }
    public string? ReadyForInvoiceStatus { get; set; }
    public string? Period { get; set; }
    public bool? IsStatementGenerated { get; set; }
    public string? AccountManager { get; set; }
    public string? DistrictManager { get; set; }
    public string? LegalEntity { get; set; }
    public string? PLCategory { get; set; }
    public string? SVPRegion { get; set; }
    public string? COGSegment { get; set; }
    public string? BusinessSegment { get; set; }
}