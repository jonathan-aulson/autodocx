using System.Collections.Generic;
using System;

namespace api.Models.Vo
{
    public class ParkingRateDataVo
    {
        public Guid Id { get; set; } = Guid.Empty;
        public string Name { get; set; } = string.Empty;
        public Guid CustomerSiteId { get; set; }
        public string SiteNumber { get; set; } = string.Empty;
        public int Year { get; set; } = 0;

        public List<ParkingRateDetailVo> ForecastRates { get; set; } = new List<ParkingRateDetailVo>();
        public List<ParkingRateDetailVo> ActualRates { get; set; } = new List<ParkingRateDetailVo>();
        public List<ParkingRateDetailVo> BudgetRates { get; set; } = new List<ParkingRateDetailVo>();
    }
} 