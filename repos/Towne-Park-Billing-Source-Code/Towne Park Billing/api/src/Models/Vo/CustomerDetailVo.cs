namespace api.Models.Vo;

public class CustomerDetailVo
{
    public Guid CustomerSiteId { get; set; }
    public string? Address { get; set; }
    public string? SiteName { get; init; }
    public string? AccountManager { get; set; } 
    public string? SiteNumber { get; init; }
    public string? InvoiceRecipient { get; init; }
    public string? BillingContactEmail { get; init; }
    public string? AccountManagerId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? CloseDate { get; set; }
    public string? District { get; set; }
    public string? GlString { get; set; }
    public string? TotalRoomsAvailable { get; set; }
    public string? TotalAvailableParking { get; set; }
    public string? DistrictManager { get; set; }
    public string? AssistantDistrictManager { get; set; }
    public string? AssistantAccountManager { get; set; }
    public string? VendorId { get; set; }
    public string? LegalEntity { get; set; }
    public string? PLCategory { get; set; }
    public string? SVPRegion { get; set; }
    public string? COGSegment { get; set; }
    public string? BusinessSegment { get; set; }
}