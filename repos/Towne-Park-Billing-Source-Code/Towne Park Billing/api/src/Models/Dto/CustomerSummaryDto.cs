using Newtonsoft.Json;

namespace api.Models.Dto;

public class CustomerSummaryDto
{
    [JsonProperty("customerSiteId")]
    public Guid CustomerSiteId { get; set; }

    [JsonProperty("siteNumber")]
    public string? SiteNumber { get; set; }

    [JsonProperty("siteName")]
    public string? SiteName { get; set; }
    
    [JsonProperty("district")]
    public string? District { get; set; }
    
    [JsonProperty("billingType")]
    public string? BillingType { get; set; }
    
    [JsonProperty("contractType")]
    public string? ContractType { get; set; }
    
    [JsonProperty("deposits")]
    public bool? Deposits { get; set; }

    [JsonProperty("readyForInvoiceStatus")]
    public string? ReadyForInvoiceStatus { get; set; }

    [JsonProperty("period")]
    public string? Period { get; set; }

    [JsonProperty("isStatementGenerated")]
    public bool? IsStatementGenerated { get; set; }

    [JsonProperty("accountManager")]
    public string? AccountManager { get; set; }

    [JsonProperty("districtManager")]
    public string? DistrictManager { get; set; }

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