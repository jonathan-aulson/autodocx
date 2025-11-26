using Newtonsoft.Json;
using System.Collections.Generic;

namespace api.Models.Dto
{
    public class SiteStatisticDto
    {
        [JsonProperty("siteStatisticId")]
        public Guid? Id { get; set; }

        [JsonProperty("siteNumber")]
        public string? SiteNumber { get; set; }

        [JsonProperty("customerSiteId")]
        public Guid CustomerSiteId { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("totalRooms")]
        public int TotalRooms { get; set; }

        [JsonProperty("timeRangeType")]
        public string TimeRangeType { get; set; } = "DAILY";

        [JsonProperty("periodLabel")]
        public string? PeriodLabel { get; set; }

        [JsonProperty("budgetData")]
        public List<SiteStatisticDetailDto> BudgetData { get; set; } = new List<SiteStatisticDetailDto>();

        [JsonProperty("forecastData")]
        public List<SiteStatisticDetailDto> ForecastData { get; set; } = new List<SiteStatisticDetailDto>();

        [JsonProperty("actualData")]
        public List<SiteStatisticDetailDto> ActualData { get; set; } = new List<SiteStatisticDetailDto>();
    }

    public class SiteStatisticDetailDto
    {
        [JsonProperty("siteStatisticDetailId")]
        public Guid? Id { get; set; }

        [JsonProperty("type")]
        public string? Type { get; set; }

        [JsonProperty("periodStart")]
        public DateOnly PeriodStart { get; set; } = new DateOnly();

        [JsonProperty("periodEnd")]
        public DateOnly PeriodEnd { get; set; } = new DateOnly();

        [JsonProperty("periodLabel")]
        public string? PeriodLabel { get; set; }

        [JsonProperty("externalRevenue")]
        public decimal ExternalRevenue { get; set; }

        [JsonProperty("valetRateDaily")]
        public decimal ValetRateDaily { get; set; }

        [JsonProperty("valetRateMonthly")]
        public decimal ValetRateMonthly { get; set; }

        [JsonProperty("valetRateOvernight")]
        public decimal ValetRateOvernight { get; set; }

        [JsonProperty("selfRateDaily")]
        public decimal SelfRateDaily { get; set; }

        [JsonProperty("selfRateMonthly")]
        public decimal SelfRateMonthly { get; set; }

        [JsonProperty("selfRateOvernight")]
        public decimal SelfRateOvernight { get; set; }

        [JsonProperty("baseRevenue")]
        public decimal BaseRevenue { get; set; }

        [JsonProperty("occupiedRooms")]
        public decimal? OccupiedRooms { get; set; }

        [JsonProperty("occupancy")]
        public decimal? Occupancy { get; set; }

        [JsonProperty("selfOvernight")]
        public decimal SelfOvernight { get; set; }

        [JsonProperty("valetOvernight")]
        public decimal ValetOvernight { get; set; }

        [JsonProperty("valetDaily")]
        public decimal ValetDaily { get; set; }

        [JsonProperty("valetMonthly")]
        public decimal ValetMonthly { get; set; }

        [JsonProperty("selfDaily")]
        public decimal SelfDaily { get; set; }

        [JsonProperty("selfMonthly")]
        public decimal SelfMonthly { get; set; }

        [JsonProperty("valetComps")]
        public decimal ValetComps { get; set; }

        [JsonProperty("selfComps")]
        public decimal SelfComps { get; set; }

        [JsonProperty("driveInRatio")]
        public double DriveInRatio { get; set; }

        [JsonProperty("captureRatio")]
        public double CaptureRatio { get; set; }

        [JsonProperty("selfAggregator")]
        public decimal SelfAggregator { get; set; }

        [JsonProperty("valetAggregator")]
        public decimal ValetAggregator { get; set; }

        [JsonProperty("adjustmentPercentage")]
        public decimal? AdjustmentPercentage { get; set; }

        [JsonProperty("adjustmentValue")]
        public decimal? AdjustmentValue { get; set; }

        [JsonProperty("externalRevenueLastDate")]
        public DateOnly? ExternalRevenueLastDate { get; set; }
    }
}