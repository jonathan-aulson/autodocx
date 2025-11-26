using System.Collections.Generic;
using System;
using Newtonsoft.Json;

namespace api.Models.Dto
{
    public class ParkingRateDataDto
    {
        [JsonProperty("parkingRateId")]
        public Guid? Id { get; set; } = Guid.Empty;
        
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonProperty("customerSiteId")]
        public Guid CustomerSiteId { get; set; }
        
        [JsonProperty("siteNumber")]
        public string SiteNumber { get; set; } = string.Empty; 
        
        [JsonProperty("year")]
        public int Year { get; set; } = 0;

        [JsonProperty("forecastRates")]
        public List<ParkingRateDetailDto> ForecastRates { get; set; } = new List<ParkingRateDetailDto>();
        
        [JsonProperty("actualRates")]
        public List<ParkingRateDetailDto> ActualRates { get; set; } = new List<ParkingRateDetailDto>();
        
        [JsonProperty("budgetRates")]
        public List<ParkingRateDetailDto> BudgetRates { get; set; } = new List<ParkingRateDetailDto>();
    }
} 