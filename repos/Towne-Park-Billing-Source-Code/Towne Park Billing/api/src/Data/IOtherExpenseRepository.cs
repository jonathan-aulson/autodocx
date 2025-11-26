using api.Models.Vo;
using TownePark;

namespace api.Data
{
    public interface IOtherExpenseRepository
    {
        IEnumerable<bs_OtherExpenseDetail>? GetOtherExpenseDetail(Guid siteId, string billingPeriod);
        void UpdateOtherRevenueDetails(List<bs_OtherExpenseDetail> details);

        /// <summary>
        /// Gets the forecast total for a specific expense account field for a given site and month
        /// </summary>
        /// <param name="siteId">The site ID</param>
        /// <param name="year">The year</param>
        /// <param name="monthOneBased">The month (1-based)</param>
        /// <param name="accountFieldName">The field name for the specific account (e.g., "bs_EmployeeRelations")</param>
        /// <returns>The forecast amount for the account, or 0 if not found</returns>
        decimal GetMonthlyAccountTotal(Guid siteId, int year, int monthOneBased, string accountFieldName);

        /// <summary>
        /// Gets the Actual Other Expense data for a specific site and billing period
        /// </summary>
        /// <param name="siteId">The site ID</param>
        /// <param name="billingPeriod">The billing period (e.g., "2025-07")</param>
        Task<List<OtherExpenseDetailVo>> GetActualData(string siteId, string billingPeriod);

        /// <summary>
        /// Gets the Other Expenses Budget data from EDW for a specific expense account field for a given site and month
        /// </summary>
        Task<List<OtherExpenseDetailVo>> GetBudgetData(string siteId, string billingPeriod);
    }
}
