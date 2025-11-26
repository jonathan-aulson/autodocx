using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownePark.Billing.Api.Models.Enums;

namespace TownePark.Billing.Api.Models.Vo
{
    public class ParkingRateDetailVo
    {
        public Guid Id { get; set; } = Guid.Empty;
        public decimal Rate { get; set; } = 0;
        public int Month { get; set; } = 0;
        // Use Dataverse enum with default
        public RateCategoryTypes RateCategory { get; set; } 
        // Use Dataverse enum with default
        public ParkingRateDetailTypes Type { get; set; } 
        public bool IsIncrease { get; set; } = false;
        public decimal IncreaseAmount { get; set; } = 0;
    }
}
