using Newtonsoft.Json;

namespace api.Models.Dto;

public class LineItemDto
{
    [JsonProperty("title")]
    public string? Title { get; set; }

    [JsonProperty("description")]
    public string? Description { get; set; }

    [JsonProperty("code")]
    public string? Code { get; set; }

    [JsonProperty("amount")]
    public decimal? Amount { get; set; }

    [JsonProperty("metaData")]
    public MetaDataDto? MetaData { get; set; } // MetaData DTO property
}

public class MetaDataDto
{
    [JsonProperty("lineItemId")]
    public string? LineItemId { get; set; }

    [JsonProperty("lineItemType")]
    public string? LineItemType { get; set; }

    [JsonProperty("isAdhoc")]
    public bool? IsAdhoc { get; set; }

    [JsonProperty("isClaims")]
    public bool? IsClaims { get; set; }

    [JsonProperty("isInsurance")]
    public bool? IsInsurance { get; set; }

    [JsonProperty("isManagementFee")]
    public bool? IsManagementFee { get; set; }

    [JsonProperty("isBillablePayrollAccounts")]
    public bool? IsBillablePayrollAccounts { get; set; }

    [JsonProperty("isPTEB")]
    public bool? IsPTEB { get; set; }

    [JsonProperty("isSupportServices")]
    public bool? IsSupportServices { get; set; }

    [JsonProperty("isBillableExpenseAccounts")]
    public bool? IsBillableExpenseAccounts { get; set; }

    [JsonProperty("isClientPaidExpense")]
    public bool? IsClientPaidExpense { get; set; }

    [JsonProperty("isProfitDeduction")]
    public bool? IsProfitDeduction { get; set; }

    [JsonProperty("isNonBillableExpense")]
    public bool? IsNonBillableExpense { get; set; }

    [JsonProperty("isProfitShare")]
    public bool? IsProfitShare { get; set; }

    [JsonProperty("monthlyProfit")]
    public decimal? MonthlyProfit { get; set; }

    [JsonProperty("invoiceGroup")]
    public string? InvoiceGroup { get; set; }

    [JsonProperty("isDepositedRevenue")]
    public bool? IsDepositedRevenue { get; set; }

    [JsonProperty("isPaidParkingTax")]
    public bool? IsPaidParkingTax { get; set; }

    [JsonProperty("taxesPaid")]
    public decimal? TaxesPaid { get; set; }
}