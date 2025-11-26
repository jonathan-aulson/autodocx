using TownePark.Billing.Api.Helpers;
using TownePark.Billing.Api.Models.Dto;

namespace TownePark.Billing.Api.Adapters.Mappers
{
    public static class OtherExpenseMapper
    {
        private static readonly Dictionary<string, string> AccountMapping = new()
        {
            { "7045", nameof(OtherExpenseDetailDto.EmployeeRelations) },
            { "7075", nameof(OtherExpenseDetailDto.FuelVehicles) },
            { "7100", nameof(OtherExpenseDetailDto.LossAndDamageClaims) },
            { "7113", nameof(OtherExpenseDetailDto.OfficeSupplies) },
            { "7115", nameof(OtherExpenseDetailDto.OutsideServices) },
            { "7170", nameof(OtherExpenseDetailDto.RentsParking) },
            { "7175", nameof(OtherExpenseDetailDto.RepairsAndMaintenance) },
            { "7178", nameof(OtherExpenseDetailDto.RepairsAndMaintenanceVehicle) },
            { "7180", nameof(OtherExpenseDetailDto.Signage) },
            { "7185", nameof(OtherExpenseDetailDto.SuppliesAndEquipment) },
            { "7205", nameof(OtherExpenseDetailDto.TicketsAndPrintedMaterial) },
            { "7220", nameof(OtherExpenseDetailDto.Uniforms) }
        };

        // Note: MiscAccounts HashSet removed - now all non-mapped accounts are considered "misc"
        // This aligns with the P&L view logic that uses IS_SUMMARY_CATEGORY = 'OTHER EXPENSE'

        public static OtherExpenseDto MapToOtherExpenseDto(List<Dictionary<string, object>> rawResults, bool isBudget = false)
        {
            var monthGroups = rawResults
                .GroupBy(row => row.GetValue<string>("PERIOD"))
                .OrderBy(g => g.Key);

            var expenseData = monthGroups
                .Select(monthGroup => MapToOtherExpenseDetailDto(monthGroup.ToList()))
                .ToList();

            return new OtherExpenseDto
            {
                Id = null,
                SiteNumber = rawResults.FirstOrDefault()?.GetValue<string>("COST_CENTER"),
                ActualData = isBudget ? new List<OtherExpenseDetailDto>() : expenseData,
                BudgetData = isBudget ? expenseData : new List<OtherExpenseDetailDto>()
            };
        }

        private static OtherExpenseDetailDto MapToOtherExpenseDetailDto(List<Dictionary<string, object>> monthData)
        {
            var detail = new OtherExpenseDetailDto
            {
                Id = null,
                MonthYear = monthData.FirstOrDefault()?.GetValue<string>("PERIOD") ?? ""
            };

            decimal miscTotal = 0;
            decimal mainAccountsTotal = 0;

            foreach (var row in monthData)
            {
                var account = row.GetValue<string>("MAIN_ACCOUNT");
                var balance = row.GetValue<decimal>("BALANCE");

                if (AccountMapping.TryGetValue(account, out var propertyName))
                {
                    var property = typeof(OtherExpenseDetailDto).GetProperty(propertyName);
                    property?.SetValue(detail, balance);
                    mainAccountsTotal += balance;
                }
                else
                {
                    // All accounts not in AccountMapping are considered "misc"
                    // This aligns with P&L view logic using IS_SUMMARY_CATEGORY = 'OTHER EXPENSE'
                    miscTotal += balance;
                }
            }

            detail.MiscOtherExpenses = miscTotal;
            detail.TotalOtherExpenses = mainAccountsTotal + miscTotal;

            return detail;
        }
    }
}
