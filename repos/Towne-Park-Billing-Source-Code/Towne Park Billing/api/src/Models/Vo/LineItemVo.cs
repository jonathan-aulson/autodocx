namespace api.Models.Vo;

public class LineItemVo
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Code { get; set; }
    public decimal? Amount { get; set; }
    public MetaDataVo? MetaData { get; set; }
}

public class MetaDataVo
{
    public Guid? LineItemId { get; set; }
    public string? LineItemType { get; set; }
    public bool? IsAdhoc { get; set; }
    public bool? IsClaims { get; set; }
    public bool? IsInsurance { get; set; }

    public bool? IsManagementFee { get; set; }
    public bool? IsBillablePayrollAccounts { get; set; }
    public bool? IsPTEB { get; set; }
    public bool? IsSupportServices { get; set; }
    public bool? IsBillableExpenseAccounts { get; set; }
    public bool? IsClientPaidExpense { get; set; }
    public bool? IsProfitDeduction { get; set; }
    public bool? IsNonBillableExpense { get; set; }
    public bool? IsProfitShare { get; set; }
    public decimal? MonthlyProfit { get; set; }
    public string? InvoiceGroup { get; set; }
    public bool? IsDepositedRevenue { get; set; }
    public bool? IsPaidParkingTax { get; set; }
    public decimal? TaxesPaid { get; set; }
}