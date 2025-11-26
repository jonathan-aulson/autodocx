using System;
using TownePark; // Add using for Dataverse enums

namespace api.Models.Vo
{
    public class ParkingRateDetailVo
    {
        public Guid Id { get; set; } = Guid.Empty;
        public decimal Rate { get; set; } = 0;
        public int Month { get; set; } = 0;
        // Use Dataverse enum with default
        public bs_ratecategorytypes RateCategory { get; set; } = bs_ratecategorytypes.ValetOvernight;
        // Use Dataverse enum with default
        public bs_parkingratedetailtypes Type { get; set; } = bs_parkingratedetailtypes.Forecast;
        public bool IsIncrease { get; set; } = false;
        public decimal IncreaseAmount { get; set; } = 0;
    }
} 