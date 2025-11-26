namespace api.Models.Vo;

public class MasterCustomerDetailVo
{
    public string? SiteName { get; init; }
    public string? SiteNumber { get; init; }
    public string? AccountManager { get; set; } 
    public string? AccountManagerId { get; set; }
    public string? Address { get; set; }
    public string? BillingContactEmail { get; init; }
    public string? District { get; init; }
    public string? GlString { get; init; }
    public DateTime? StartDate { get; init; }
    public string? LegalEntity { get; set; }
    public string? PLCategory { get; set; }
    public string? SVPRegion { get; set; }
    public string? COGSegment { get; set; }
    public string? BusinessSegment { get; set; }
}