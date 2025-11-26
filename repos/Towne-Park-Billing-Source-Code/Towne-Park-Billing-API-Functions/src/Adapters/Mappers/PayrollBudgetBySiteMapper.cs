using System.Collections.Generic;
using System.Linq;
using TownePark.Billing.Api.Models.Dto;
using TownePark.Billing.Api.Helpers;

namespace TownePark.Billing.Api.Adapters.Mappers
{
    public static class PayrollBudgetBySiteMapper
    {
        public static PayrollBudgetBySiteDto MapToPayrollBudgetBySiteDto(this IDictionary<string, object> row)
        {
            return new PayrollBudgetBySiteDto
            {
                COST_CENTER = row.GetValue<string>("COST_CENTER"),
                YEAR = row.GetValue<int>("YEAR"),
                MONTH = row.GetValue<int>("MONTH"),
                JOB_PROFILE = row.GetValue<string>("JOB_PROFILE"),
                TOTAL_HOURS = row.GetValue<decimal>("TOTAL_HOURS"),
                TOTAL_COST = row.GetValue<decimal>("TOTAL_COST")
            };
        }

        public static List<PayrollBudgetBySiteDto> MapToPayrollBudgetBySiteDtoList(List<Dictionary<string, object>> rawResults)
        {
            return rawResults.Select(MapToPayrollBudgetBySiteDto).ToList();
        }
    }
}
