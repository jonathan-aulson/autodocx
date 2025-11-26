using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TownePark.Billing.Api.Models.Enums
{
    public enum ParkingRateDetailTypes
    {
        [System.Runtime.Serialization.EnumMemberAttribute()]
        Forecast = 126840000,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        Actual = 126840001,

        [System.Runtime.Serialization.EnumMemberAttribute()]
        Budget = 126840002,
    }
}
