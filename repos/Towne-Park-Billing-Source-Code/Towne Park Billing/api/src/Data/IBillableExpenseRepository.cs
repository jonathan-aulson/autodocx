using api.Models.Vo;

namespace api.Data
{

    public interface IBillableExpenseRepository
    {
        /// <summary>
        /// Gets the payroll expense budget for a specific site and period
        /// </summary>
        /// <param name="siteId">The site ID</param>
        /// <param name="year">The year</param>
        /// <param name="monthOneBased">The month (1-based)</param>
        /// <returns>The payroll expense budget amount, or 0 if not found</returns>
        decimal GetPayrollExpenseBudget(Guid siteId, int year, int monthOneBased);

        /// <summary>
        /// Gets the billable expense budget for a specific site and period
        /// Includes all enabled 7000-range accounts except the 12 other expense accounts
        /// </summary>
        /// <param name="siteId">The site ID</param>
        /// <param name="year">The year</param>
        /// <param name="monthOneBased">The month (1-based)</param>
        /// <returns>The billable expense budget amount, or 0 if not found</returns>
        decimal GetBillableExpenseBudget(Guid siteId, int year, int monthOneBased);

        /// <summary>
        /// Gets the other expense budget for a specific site and period
        /// Includes budget values for the 12 specific other expense accounts
        /// </summary>
        /// <param name="siteId">The site ID</param>
        /// <param name="year">The year</param>
        /// <param name="monthOneBased">The month (1-based)</param>
        /// <returns>The other expense budget amount, or 0 if not found</returns>
        decimal GetOtherExpenseBudget(Guid siteId, int year, int monthOneBased);

        /// <summary>
        /// Gets billable expense actuals and other expense actuals for multiple sites and period
        /// </summary>
        /// <param name="siteIds">The site IDs</param>
        /// <param name="year">The year</param>
        /// <param name="monthOneBased">The month (1-based)</param>
        /// <returns>Array of expense actuals data for each site</returns>
        ExpenseActualsDataVo[] GetExpenseActualsForSites(List<Guid> siteIds, int year, int monthOneBased);

        /// <summary>
        /// Gets the list of enabled expense account codes for a specific site
        /// Parses the bs_ExpenseAccountsData JSON column from billable accounts
        /// </summary>
        /// <param name="siteId">The site ID</param>
        /// <returns>List of enabled expense account codes, or empty list if none found</returns>
        List<string> GetEnabledExpenseAccounts(Guid siteId);

        /// <summary>
        /// Gets the vehicle insurance budget for a specific site and period
        /// </summary>
        /// <param name="siteId">The site ID</param>
        /// <param name="year">The year</param>
        /// <param name="monthOneBased">The month (1-based)</param>
        /// <returns>The vehicle insurance budget amount, or 0 if not found</returns>
        decimal GetVehicleInsuranceBudget(Guid siteId, int year, int monthOneBased);

        /// <summary>
        /// Gets the vehicle insurance budget for multiple sites for a specific period (batched)
        /// </summary>
        /// <param name="siteIds">List of site IDs</param>
        /// <param name="year">Year</param>
        /// <param name="monthOneBased">Month (1-based)</param>
        /// <returns>Dictionary mapping siteId to vehicle insurance budget (0 if not found)</returns>
        Dictionary<Guid, decimal> GetVehicleInsuranceBudgetForSites(List<Guid> siteIds, int year, int monthOneBased);

        /// <summary>
        /// Gets the vehicle insurance budget for multiple sites for all months of a given year (single batched call)
        /// </summary>
        /// <param name="siteIds">List of site IDs</param>
        /// <param name="year">Year</param>
        /// <returns>Dictionary keyed by (siteId, monthOneBased) mapping to budget (0 if not found)</returns>
        Dictionary<(Guid siteId, int monthOneBased), decimal> GetVehicleInsuranceBudgetForSitesForYear(List<Guid> siteIds, int year);

        /// <summary>
        /// Gets the total claims budget for a site within a period range
        /// </summary>
        /// <param name="siteId">The site ID</param>
        /// <param name="startPeriod">Start period in YYYYMM format (e.g., "202503")</param>
        /// <param name="endPeriod">End period in YYYYMM format (e.g., "202507")</param>
        /// <returns>Sum of claims budget for the period range</returns>
        decimal GetClaimsBudgetForPeriodRange(Guid siteId, string startPeriod, string endPeriod);

        /// <summary>
        /// Gets the claims budget for a specific site and period
        /// </summary>
        /// <param name="siteId">The site ID</param>
        /// <param name="period">Period in YYYYMM format</param>
        /// <returns>Claims budget for the period, or 0 if not found</returns>
        decimal GetClaimsBudgetForPeriod(Guid siteId, string period);
    }
} 