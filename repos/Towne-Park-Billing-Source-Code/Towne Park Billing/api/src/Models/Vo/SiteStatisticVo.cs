using System.Collections.Generic;

namespace api.Models.Vo
{
    public class SiteStatisticVo
    {
        public Guid? Id { get; set; }
        public string? SiteNumber { get; set; }
        public Guid CustomerSiteId { get; set; }
        public string? Name { get; set; }
        public int TotalRooms { get; set; }
        public TimeRangeType TimeRangeType { get; set; }
        public string? PeriodLabel { get; set; }
        public List<SiteStatisticDetailVo>? BudgetData { get; set; }
        public List<SiteStatisticDetailVo>? ForecastData { get; set; }
        public List<SiteStatisticDetailVo>? ActualData { get; set; }
    }

    public class SiteStatisticDetailVo
    {
        public Guid Id { get; set; }
        public SiteStatisticDetailType Type { get; set; }
        public DateOnly PeriodStart { get; set; } = new DateOnly();
        public DateOnly PeriodEnd { get; set; } = new DateOnly();
        public string? PeriodLabel { get; set; }
        public DateOnly Date { get; set; }
        public decimal ValetRateDaily { get; set; }
        public decimal ValetRateMonthly { get; set; }
        public decimal ValetRateOvernight { get; set; }
        public decimal SelfRateDaily { get; set; }
        public decimal SelfRateMonthly { get; set; }
        public decimal SelfRateOvernight { get; set; }
        public decimal BaseRevenue { get; set; }
        public decimal? OccupiedRooms { get; set; }
        public decimal? Occupancy { get; set; }
        public decimal SelfOvernight { get; set; }
        public decimal ValetOvernight { get; set; }
        public decimal ValetDaily { get; set; }
        public decimal ValetMonthly { get; set; }
        public decimal SelfDaily { get; set; }
        public decimal SelfMonthly { get; set; }
        public decimal ValetComps { get; set; }
        public decimal SelfComps { get; set; }
        public double DriveInRatio { get; set; }
        public double CaptureRatio { get; set; }
        public decimal SelfAggregator { get; set; }
        public decimal ValetAggregator { get; set; }
        public decimal? AdjustmentValue { get; set; }
        public decimal? AdjustmentPercentage { get; set; }
        public decimal ExternalRevenue { get; set; }
        public DateOnly? ExternalRevenueLastDate { get; set; }

    }

    public enum SiteStatisticDetailType
    {
        Budget = 126840000,
        Forecast = 126840001,
        Actual = 126840002
    }

    public enum TimeRangeType
    {
        DAILY,
        WEEKLY,
        MONTHLY,
        QUARTERLY
    }
}
