using Newtonsoft.Json;

namespace api.Models.Dto;

public class CustomerDetailDto
{
    [JsonProperty("customerSiteId")]
    public Guid CustomerSiteId { get; set; }

    [JsonProperty("address")]
    public string? Address { get; set; }

    [JsonProperty("siteName")]
    public string? SiteName { get; set; }

    [JsonProperty("accountManager")]
    public string? AccountManager { get; set; }

    [JsonProperty("siteNumber")]
    public string? SiteNumber { get; set; }

    [JsonProperty("invoiceRecipient")]
    public string? InvoiceRecipient { get; set; }

    [JsonProperty("billingContactEmail")]
    public string? BillingContactEmail { get; set; }

    [JsonProperty("accountManagerId")]
    public string? AccountManagerId { get; set; }

    [JsonProperty("startDate")]
    public string? StartDate { get; set; }

    [JsonProperty("closeDate")]
    public string? CloseDate { get; set; }

    [JsonProperty("district")]
    public string? District { get; set; }

    [JsonProperty("glString")]
    public string? GlString { get; set; }

    [JsonProperty("totalRoomsAvailable")]
    public string? TotalRoomsAvailable { get; set; }

    [JsonProperty("totalAvailableParking")]
    public string? TotalAvailableParking { get; set; }

    [JsonProperty("districtManager")]
    public string? DistrictManager { get; set; }

    [JsonProperty("assistantDistrictManager")]
    public string? AssistantDistrictManager { get; set; }

    [JsonProperty("assistantAccountManager")]
    public string? AssistantAccountManager { get; set; }

    [JsonProperty("vendorId")]
    public string? VendorId { get; set; }

    [JsonProperty("legalEntity")]
    public string? LegalEntity { get; set; }

    [JsonProperty("plCategory")]
    public string? PLCategory { get; set; }

    [JsonProperty("svpRegion")]
    public string? SVPRegion { get; set; }

    [JsonProperty("cogSegment")]
    public string? COGSegment { get; set; }

    [JsonProperty("businessSegment")]
    public string? BusinessSegment { get; set; }
}