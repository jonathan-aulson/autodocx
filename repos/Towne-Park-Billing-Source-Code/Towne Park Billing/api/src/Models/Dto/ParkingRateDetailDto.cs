using System;
using Newtonsoft.Json;

namespace api.Models.Dto
{
    public class ParkingRateDetailDto
    {
        [JsonProperty("parkingRateDetailId")]
        public Guid? Id { get; set; } = Guid.Empty;
        
        [JsonProperty("rate")]
        public decimal Rate { get; set; } = 0;
        
        [JsonProperty("month")]
        public int Month { get; set; } = 0;
        
        [JsonProperty("rateCategory")]
        public string RateCategory { get; set; } = string.Empty; // Maps to RateCategoryType enum in VO
        
        [JsonProperty("isIncrease")]
        public bool IsIncrease { get; set; } = false;
        
        [JsonProperty("increaseAmount")]
        public decimal IncreaseAmount { get; set; } = 0;
    }
} 