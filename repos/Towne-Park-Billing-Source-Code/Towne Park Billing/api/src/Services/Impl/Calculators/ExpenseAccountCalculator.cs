using api.Data;
using api.Models.Dto;
using api.Models.Vo;
using api.Models.Vo.Enum;
using Microsoft.Extensions.Logging;
using TownePark;
using TownePark.Models.Vo;

namespace api.Services.Impl.Calculators
{
    public class ExpenseAccountCalculator : IInternalRevenueCalculator
    {
        private readonly IBillableExpenseRepository _billableExpenseRepository;
        private readonly IOtherExpenseRepository _otherExpenseRepository;
        private readonly ILogger<ExpenseAccountCalculator> _logger;
        private readonly Dictionary<(Guid siteId, int year), List<bs_OtherExpenseDetail>> _yearlyExpenseCache;

        private static readonly List<string> ForecastedAccountFields = new()
        {
            "bs_EmployeeRelations",
            "bs_FuelVehicles", 
            "bs_LossAndDamageClaims",
            "bs_OfficeSupplies",
            "bs_OutsideServices",
            "bs_RentsParking",
            "bs_RepairsAndMaintenance",
            "bs_RepairsAndMaintenanceVehicle",
            "bs_Signage",
            "bs_SuppliesAndEquipment",
            "bs_TicketsAndPrintedMaterial",
            "bs_Uniforms"
        };
        
        public ExpenseAccountCalculator(
            IBillableExpenseRepository billableExpenseRepository,
            IOtherExpenseRepository otherExpenseRepository,
            ILogger<ExpenseAccountCalculator> logger)
        {
            _billableExpenseRepository = billableExpenseRepository;
            _otherExpenseRepository = otherExpenseRepository;
            _logger = logger;
            _yearlyExpenseCache = new Dictionary<(Guid, int), List<bs_OtherExpenseDetail>>();
        }

        public void CalculateAndApply(
            InternalRevenueDataVo siteData, 
            int year, 
            int monthOneBased, 
            int currentMonth, // NEW: Current month parameter
            MonthValueDto monthValueDto, 
            SiteMonthlyRevenueDetailDto siteDetailDto,
            decimal calculatedExternalRevenue, 
            List<PnlRowDto> budgetRows)
        {
            try
            {
                if (!IsContractTypeBillingAccount(siteData))
                {
                    return;
                }

                var today = DateTime.Today;
                var isCurrentMonth = (year == today.Year && monthOneBased == today.Month);
                
                ExpenseAccountsInternalRevenueDto expenseAccountsRevenue;
                
                if (isCurrentMonth)
                {
                    // Current month: use monthly actuals from PnL response when present per category, otherwise forecast
                    expenseAccountsRevenue = CalculateCurrentMonthExpenseAccountsUsingActuals(siteData, year, monthOneBased, siteDetailDto);
                }
                else
                {
                    // Use existing forecast logic for non-current months
                    expenseAccountsRevenue = CalculateMonthlyExpenseAccountsForSite(siteData, year, monthOneBased, calculatedExternalRevenue, monthValueDto, budgetRows);
                }

                InitializeDtoStructure(siteDetailDto);
                
                siteDetailDto.InternalRevenueBreakdown!.BillableAccounts!.ExpenseAccounts = expenseAccountsRevenue;

                UpdateBillableAccountsTotal(siteDetailDto, expenseAccountsRevenue.Total ?? 0m);
                UpdateCalculatedTotalInternalRevenue(siteDetailDto.InternalRevenueBreakdown);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating expense accounts for site {SiteId} for {Year}-{Month}", 
                    siteData.SiteId, year, monthOneBased);
            }
        }

        public void AggregateMonthlyTotals(List<SiteMonthlyRevenueDetailDto> siteDetailsForMonth, MonthValueDto monthValueDto)
        {
            try
            {
                var totalExpenseAccounts = siteDetailsForMonth
                    .Sum(s => s.InternalRevenueBreakdown?.BillableAccounts?.ExpenseAccounts?.Total ?? 0m);

                InitializeMonthDtoStructure(monthValueDto);
                
                monthValueDto.InternalRevenueBreakdown!.BillableAccounts!.ExpenseAccounts = new ExpenseAccountsInternalRevenueDto
                {
                    Total = totalExpenseAccounts
                };

                UpdateMonthBillableAccountsTotal(monthValueDto, totalExpenseAccounts);
                UpdateCalculatedTotalInternalRevenue(monthValueDto.InternalRevenueBreakdown);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error aggregating monthly expense accounts totals");
            }
        }

        private decimal CalculateExpenseAccountsForSite(InternalRevenueDataVo siteData, int year, int monthOneBased)
        {
            var siteId = siteData.SiteId;
            if (siteId == Guid.Empty)
            {
                return 0m;
            }

            var total = 0m;

            // Get enabled expense accounts from billable account configuration
            var enabledAccounts = _billableExpenseRepository.GetEnabledExpenseAccounts(siteId);
            
            // If no enabled accounts are configured, fall back to original behavior
            if (!enabledAccounts.Any())
            {
                // Get non-forecasted expense accounts total (all 7000-range except the 12 forecasted ones)
                var nonForecastedTotal = _billableExpenseRepository.GetBillableExpenseBudget(siteId, year, monthOneBased);
                total += nonForecastedTotal;
            }
            else
            {
                // Only process accounts that are in the enabled list
                // For now, we'll apply the filtering logic by checking if forecasted accounts are enabled
                // and only include them if they are in the enabled list
                
                // Get non-forecasted expense accounts total - for enabled accounts this would need custom logic
                // For now, we'll use the original method and note this needs refinement
                var nonForecastedTotal = _billableExpenseRepository.GetBillableExpenseBudget(siteId, year, monthOneBased);
                total += nonForecastedTotal;
            }

            // Get forecasted expense accounts (12 specific accounts) - filter based on enabled accounts
            var forecastedTotal = CalculateForecastedExpenseAccounts(siteData, siteId, year, monthOneBased, enabledAccounts);
            total += forecastedTotal;

            return total;
        }

        private decimal CalculateForecastedExpenseAccounts(InternalRevenueDataVo siteData, Guid siteId, int year, int monthOneBased)
        {
            // Call the overloaded method with null enabled accounts (no filtering)
            return CalculateForecastedExpenseAccounts(siteData, siteId, year, monthOneBased, null);
        }

        private decimal CalculateForecastedExpenseAccounts(InternalRevenueDataVo siteData, Guid siteId, int year, int monthOneBased, List<string>? enabledAccounts)
        {
            var total = 0m;

            // Get cached year data or fetch it if not cached
            var yearlyExpenseDetails = GetYearlyExpenseDetails(siteId, year);
            var targetMonthYear = $"{year:D4}-{monthOneBased:D2}";
            var expenseDetail = yearlyExpenseDetails?.FirstOrDefault(x => x.bs_MonthYear == targetMonthYear);

            if (expenseDetail != null)
            {
                // Extract values from the single record - much more efficient than 12 separate calls
                // Only include forecasted accounts that are in the enabled accounts list (if filtering is enabled)
                total += GetFieldValueIfEnabled(expenseDetail, "bs_EmployeeRelations", "7045", enabledAccounts);
                total += GetFieldValueIfEnabled(expenseDetail, "bs_FuelVehicles", "7075", enabledAccounts);
                total += GetFieldValueIfEnabled(expenseDetail, "bs_LossAndDamageClaims", "7100", enabledAccounts);
                total += GetFieldValueIfEnabled(expenseDetail, "bs_OfficeSupplies", "7125", enabledAccounts);
                total += GetFieldValueIfEnabled(expenseDetail, "bs_OutsideServices", "7150", enabledAccounts);
                total += GetFieldValueIfEnabled(expenseDetail, "bs_RentsParking", "7175", enabledAccounts);
                total += GetFieldValueIfEnabled(expenseDetail, "bs_RepairsAndMaintenance", "7200", enabledAccounts);
                total += GetFieldValueIfEnabled(expenseDetail, "bs_RepairsAndMaintenanceVehicle", "7225", enabledAccounts);
                total += GetFieldValueIfEnabled(expenseDetail, "bs_Signage", "7250", enabledAccounts);
                total += GetFieldValueIfEnabled(expenseDetail, "bs_SuppliesAndEquipment", "7275", enabledAccounts);
                total += GetFieldValueIfEnabled(expenseDetail, "bs_TicketsAndPrintedMaterial", "7300", enabledAccounts);
                total += GetFieldValueIfEnabled(expenseDetail, "bs_Uniforms", "7325", enabledAccounts);
            }
            else
            {
                // Fallback to budget allocation when no forecast data exists
                var otherExpenseBudget = _billableExpenseRepository.GetOtherExpenseBudget(siteId, year, monthOneBased);
                total = otherExpenseBudget; // Use the full budget rather than dividing by account count
            }

            return total;
        }

        private List<bs_OtherExpenseDetail>? GetYearlyExpenseDetails(Guid siteId, int year)
        {
            var cacheKey = (siteId, year);
            
            if (_yearlyExpenseCache.TryGetValue(cacheKey, out var cachedData))
            {
                return cachedData;
            }

            // Fetch all 12 months of data for the year in a single call
            // Using January as the starting point, GetOtherExpenseDetail will fetch the full year
            var yearStartPeriod = $"{year:D4}-01";
            var yearlyDetails = _otherExpenseRepository.GetOtherExpenseDetail(siteId, yearStartPeriod)?.ToList();
            
            // Cache the result for future use during this request lifecycle
            _yearlyExpenseCache[cacheKey] = yearlyDetails ?? new List<bs_OtherExpenseDetail>();
            
            return yearlyDetails;
        }

        /// <summary>
        /// Gets the forecasted Parking Rents values for all months in a year for the given site.
        /// Returns a dictionary: monthOneBased (1-12) → forecasted value.
        /// </summary>
        public Dictionary<int, decimal> GetForecastedParkingRents(Guid siteId, int year)
        {
            var yearlyExpenseDetails = GetYearlyExpenseDetails(siteId, year);
            var result = new Dictionary<int, decimal>();
            if (yearlyExpenseDetails != null)
            {
                foreach (var detail in yearlyExpenseDetails)
                {
                    if (!string.IsNullOrEmpty(detail.bs_MonthYear) && detail.bs_RentsParking.HasValue)
                    {
                        // bs_MonthYear format: "YYYY-MM"
                        var parts = detail.bs_MonthYear.Split('-');
                        if (parts.Length == 2 && int.TryParse(parts[1], out int monthOneBased))
                        {
                            result[monthOneBased] = detail.bs_RentsParking.Value;
                        }
                    }
                }
            }
            return result;
        }

        private static decimal GetFieldValue(bs_OtherExpenseDetail expenseDetail, string fieldName)
        {
            return fieldName switch
            {
                "bs_EmployeeRelations" => expenseDetail.bs_EmployeeRelations ?? 0m,
                "bs_FuelVehicles" => expenseDetail.bs_FuelVehicles ?? 0m,
                "bs_LossAndDamageClaims" => expenseDetail.bs_LossAndDamageClaims ?? 0m,
                "bs_OfficeSupplies" => expenseDetail.bs_OfficeSupplies ?? 0m,
                "bs_OutsideServices" => expenseDetail.bs_OutsideServices ?? 0m,
                "bs_RentsParking" => expenseDetail.bs_RentsParking ?? 0m,
                "bs_RepairsAndMaintenance" => expenseDetail.bs_RepairsAndMaintenance ?? 0m,
                "bs_RepairsAndMaintenanceVehicle" => expenseDetail.bs_RepairsAndMaintenanceVehicle ?? 0m,
                "bs_Signage" => expenseDetail.bs_Signage ?? 0m,
                "bs_SuppliesAndEquipment" => expenseDetail.bs_SuppliesAndEquipment ?? 0m,
                "bs_TicketsAndPrintedMaterial" => expenseDetail.bs_TicketsAndPrintedMaterial ?? 0m,
                "bs_Uniforms" => expenseDetail.bs_Uniforms ?? 0m,
                _ => 0m
            };
        }

        private static decimal GetFieldValueIfEnabled(bs_OtherExpenseDetail expenseDetail, string fieldName, string accountCode, List<string>? enabledAccounts)
        {
            // If no enabled accounts list is provided, include all accounts (original behavior)
            if (enabledAccounts == null || !enabledAccounts.Any())
            {
                return GetFieldValue(expenseDetail, fieldName);
            }

            // Only include the account if it's in the enabled accounts list
            if (enabledAccounts.Contains(accountCode))
            {
                return GetFieldValue(expenseDetail, fieldName);
            }

            return 0m;
        }



        private static bool IsContractTypeBillingAccount(InternalRevenueDataVo siteData)
        {
            return siteData.Contract?.ContractTypes?.Contains(bs_contracttypechoices.BillingAccount) == true;
        }

        private static void InitializeDtoStructure(SiteMonthlyRevenueDetailDto siteDetailDto)
        {
            siteDetailDto.InternalRevenueBreakdown ??= new InternalRevenueBreakdownDto();
            siteDetailDto.InternalRevenueBreakdown.BillableAccounts ??= new BillableAccountsInternalRevenueDto();
        }

        private static void InitializeMonthDtoStructure(MonthValueDto monthValueDto)
        {
            monthValueDto.InternalRevenueBreakdown ??= new InternalRevenueBreakdownDto();
            monthValueDto.InternalRevenueBreakdown.BillableAccounts ??= new BillableAccountsInternalRevenueDto();
        }

        private static void UpdateBillableAccountsTotal(SiteMonthlyRevenueDetailDto siteDetailDto, decimal amount)
        {
            var billableAccounts = siteDetailDto.InternalRevenueBreakdown!.BillableAccounts!;
            
            if (billableAccounts.Total.HasValue)
                billableAccounts.Total += amount;
            else
                billableAccounts.Total = amount;
        }

        private static void UpdateMonthBillableAccountsTotal(MonthValueDto monthValueDto, decimal amount)
        {
            var billableAccounts = monthValueDto.InternalRevenueBreakdown!.BillableAccounts!;
            
            if (billableAccounts.Total.HasValue)
                billableAccounts.Total += amount;
            else
                billableAccounts.Total = amount;
        }

        private static void UpdateCalculatedTotalInternalRevenue(InternalRevenueBreakdownDto internalRevenueBreakdown)
        {
            var total = 0m;

            if (internalRevenueBreakdown.BillableAccounts?.Total.HasValue == true)
                total += internalRevenueBreakdown.BillableAccounts.Total.Value;

            if (internalRevenueBreakdown.ManagementAgreement?.Total.HasValue == true)
                total += internalRevenueBreakdown.ManagementAgreement.Total.Value;

            if (internalRevenueBreakdown.RevenueShare?.Total.HasValue == true)
                total += internalRevenueBreakdown.RevenueShare.Total.Value;

            if (internalRevenueBreakdown.FixedFee?.Total.HasValue == true)
                total += internalRevenueBreakdown.FixedFee.Total.Value;

            if (internalRevenueBreakdown.PerOccupiedRoom?.Total.HasValue == true)
                total += internalRevenueBreakdown.PerOccupiedRoom.Total.Value;

            internalRevenueBreakdown.CalculatedTotalInternalRevenue = total;
        }

        private ExpenseAccountsInternalRevenueDto CalculateCurrentMonthExpenseAccountsWithQa(
            InternalRevenueDataVo siteData, int targetYear, int targetMonthOneBased, decimal calculatedExternalRevenue, MonthValueDto monthValueDto, List<PnlRowDto> budgetRows)
        {var siteId = siteData.SiteId;
            
            // Step 1: Get actuals for the month
            var actualsData = GetActualExpensesUpToMaxAvailableDate(siteId, targetYear, targetMonthOneBased);// Step 2: Get forecast for the month
            var forecastData = GetForecastExpensesForRestOfMonth(siteId, targetYear, targetMonthOneBased, actualsData.MaxDate);// Step 3: Use actuals if present and > 0, otherwise use forecast
            decimal finalAmountForMonth;
            decimal actualAmount = 0m;
            decimal forecastAmount = 0m;
            
            if (actualsData.Total > 0)
            {
                // Use actuals if they exist and are > 0
                finalAmountForMonth = actualsData.Total;
                actualAmount = actualsData.Total;}
            else
            {
                // Use forecast if no actuals or actuals are 0
                finalAmountForMonth = forecastData;
                forecastAmount = forecastData;}return new ExpenseAccountsInternalRevenueDto
            {
                Total = finalAmountForMonth,
                ActualExpenseAccounts = actualAmount > 0 ? actualAmount : null,
                ForecastedExpenseAccounts = forecastAmount > 0 ? forecastAmount : null,
                LastActualDate = actualsData.MaxDate != DateTime.MinValue ? actualsData.MaxDate : null
            };
        }

        private ExpenseAccountsInternalRevenueDto CalculateMonthlyExpenseAccountsForSite(
            InternalRevenueDataVo siteData, int targetYear, int targetMonthOneBased, decimal calculatedExternalRevenue, MonthValueDto monthValueDto, List<PnlRowDto> budgetRows)
        {
            var totalExpenseAccounts = CalculateExpenseAccountsForSite(siteData, targetYear, targetMonthOneBased);
            
            return new ExpenseAccountsInternalRevenueDto
            {
                Total = totalExpenseAccounts,
                ActualExpenseAccounts = null, // No actuals for non-current months
                ForecastedExpenseAccounts = totalExpenseAccounts, // All forecast for non-current months
                LastActualDate = null // No actuals for non-current months
            };
        }

        private ExpenseAccountsInternalRevenueDto CalculateCurrentMonthExpenseAccountsUsingActuals(
            InternalRevenueDataVo siteData,
            int year,
            int monthOneBased,
            SiteMonthlyRevenueDetailDto siteDetailDto)
        {
            var siteId = siteData.SiteId;

            // Forecast components from existing sources
            var forecastBillable = _billableExpenseRepository.GetBillableExpenseBudget(siteId, year, monthOneBased);
            var forecastOther = CalculateForecastedExpenseAccounts(siteData, siteId, year, monthOneBased);

            decimal usedActual = 0m;
            decimal usedForecast = 0m;

            // Default to no actuals present
            decimal finalBillable;
            decimal finalOther;

            var expenseActuals = siteDetailDto.ExpenseActuals;
            var hasEntry = expenseActuals != null;

            if (hasEntry && expenseActuals!.BillableExpenseActuals.HasValue)
            {
                finalBillable = expenseActuals.BillableExpenseActuals!.Value;
                usedActual += finalBillable;
            }
            else
            {
                finalBillable = forecastBillable;
                usedForecast += finalBillable;
            }

            if (hasEntry && expenseActuals!.OtherExpenseActuals.HasValue)
            {
                finalOther = expenseActuals.OtherExpenseActuals!.Value;
                usedActual += finalOther;
            }
            else
            {
                finalOther = forecastOther;
                usedForecast += finalOther;
            }

            var total = finalBillable + finalOther;

            DateTime? lastActualDate = null;
            if (hasEntry && (expenseActuals!.BillableExpenseActuals.HasValue || expenseActuals.OtherExpenseActuals.HasValue))
            {
                // Expense actuals are monthly; use month end as the last actual date when any actual provided
                lastActualDate = new DateTime(year, monthOneBased, DateTime.DaysInMonth(year, monthOneBased));
            }
            else
            {
                // If calculation ran but no actuals for current month, set to last day of previous month
                lastActualDate = new DateTime(year, monthOneBased, 1).AddDays(-1);
            }

            return new ExpenseAccountsInternalRevenueDto
            {
                Total = total,
                ActualExpenseAccounts = usedActual > 0 ? usedActual : null,
                ForecastedExpenseAccounts = usedForecast > 0 ? usedForecast : null,
                LastActualDate = lastActualDate
            };
        }

        private decimal CalculateCurrentMonthExpenseAccounts(InternalRevenueDataVo siteData, int year, int monthOneBased)
        {
            var siteId = siteData.SiteId;
            
            // Get actuals up to max available date and forecast for rest of month
            var actualsData = GetActualExpensesUpToMaxAvailableDate(siteId, year, monthOneBased);
            var forecastData = GetForecastExpensesForRestOfMonth(siteId, year, monthOneBased, actualsData.MaxDate);
            
            // Combine actuals and forecast
            var totalExpenseAccounts = actualsData.Total + forecastData;
            
            return totalExpenseAccounts;
        }

        private (decimal Total, DateTime MaxDate) GetActualExpensesUpToMaxAvailableDate(Guid siteId, int year, int month)
        {
            var expenseDetails = GetYearlyExpenseDetails(siteId, year);
            if (expenseDetails == null || !expenseDetails.Any())
                return (0m, DateTime.MinValue);

            // Filter for the target month with amounts > 0
            var targetMonthYear = $"{year:D4}-{month:D2}";
            var actualExpenses = expenseDetails
                .Where(e => e.bs_MonthYear == targetMonthYear && GetTotalExpenseAmount(e) > 0)
                .ToList();

            if (!actualExpenses.Any())
                return (0m, DateTime.MinValue);

            // For expense accounts, we use the month end date as the max date since data is monthly
            var maxDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            var totalAmount = actualExpenses.Sum(e => GetTotalExpenseAmount(e));return (totalAmount, maxDate);
        }

        private decimal GetForecastExpensesForRestOfMonth(Guid siteId, int year, int month, DateTime maxActualsDate)
        {
            // For expense accounts, get the full month forecast amount
            // since expense data is monthly, not daily
            var expenseDetails = GetYearlyExpenseDetails(siteId, year);
            if (expenseDetails == null || !expenseDetails.Any())
                return 0m;

            var targetMonthYear = $"{year:D4}-{month:D2}";
            var monthlyExpenses = expenseDetails
                .Where(e => e.bs_MonthYear == targetMonthYear)
                .ToList();

            if (!monthlyExpenses.Any())
                return 0m;

            // Get the full monthly forecast amount
            var totalMonthlyAmount = monthlyExpenses.Sum(e => GetTotalExpenseAmount(e));return totalMonthlyAmount;
        }

        private static decimal GetTotalExpenseAmount(bs_OtherExpenseDetail expenseDetail)
        {
            return ForecastedAccountFields.Sum(field => GetFieldValue(expenseDetail, field));
        }
    }
}
