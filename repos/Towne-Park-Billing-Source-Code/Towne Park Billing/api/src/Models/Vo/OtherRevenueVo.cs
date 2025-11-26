namespace api.Models.Vo
{
    public class OtherRevenueVo
    {
        public Guid Id { get; set; }
        public Guid CustomerSiteId { get; set; }
        public string? SiteNumber { get; set; }
        public string? Name { get; set; }
        public string? BillingPeriod { get; set; }
        public List<OtherRevenueDetailVo> ForecastData { get; set; } = new();
    }

    public class OtherRevenueDetailVo
    {
        public Guid Id { get; set; }
        public string? MonthYear { get; set; }
        public decimal BillableExpense { get; set; }
        public decimal Credits { get; set; }
        public decimal GPOFees { get; set; }
        public decimal RevenueValidation { get; set; }
        public decimal SigningBonus { get; set; }
        public decimal ClientPaidExpense { get; set; }
        public decimal Miscellaneous { get; set; }
        public OtherRevenueType? Type { get; set; }
    }

    public enum OtherRevenueType
    {
        Actual = 126840000,
        Forecast = 126840001,
        Budget = 126840002
    }
}
