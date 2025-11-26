using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TownePark.Billing.Api.Models.Enums
{
    public enum RateCategoryTypes
    {
        [System.Runtime.Serialization.EnumMemberAttribute()]
        ValetOvernight = 126840000,
		
		[System.Runtime.Serialization.EnumMemberAttribute()]
        SelfOvernight = 126840001,
		
		[System.Runtime.Serialization.EnumMemberAttribute()]
        ValetDaily = 126840002,
		
		[System.Runtime.Serialization.EnumMemberAttribute()]
        SelfDaily = 126840003,
		
		[System.Runtime.Serialization.EnumMemberAttribute()]
        ValetMonthly = 126840004,
		
		[System.Runtime.Serialization.EnumMemberAttribute()]
        SelfMonthly = 126840005,
		
		[System.Runtime.Serialization.EnumMemberAttribute()]
        ValetAggregator = 126840006,
		
		[System.Runtime.Serialization.EnumMemberAttribute()]
        SelfAggregator = 126840007,
    }
}
