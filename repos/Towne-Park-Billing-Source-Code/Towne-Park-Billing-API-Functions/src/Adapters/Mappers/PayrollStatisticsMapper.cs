using System;
using System.Collections.Generic;
using System.Linq;
using TownePark.Billing.Api.Models.Dto;
using TownePark.Billing.Api.Helpers;

namespace TownePark.Billing.Api.Adapters.Mappers
{
    public static class PayrollStatisticsMapper
    {
        public static PayrollStatisticsDto MapToPayrollStatisticsDto(this IDictionary<string, object> row)
        {
            return new PayrollStatisticsDto
            {
                JobCode = row.GetValue<string>("JobCode"),
                Hours = row.GetValue<decimal>("Hours"),
                Cost = row.GetValue<decimal>("Cost"),
                Date = row.GetValue<DateTime>("Date")
            };
        }

        public static List<PayrollStatisticsDto> MapToPayrollStatisticsDtoList(List<Dictionary<string, object>> rawResults)
        {
            return rawResults.Select(MapToPayrollStatisticsDto).ToList();
        }
    }
}
