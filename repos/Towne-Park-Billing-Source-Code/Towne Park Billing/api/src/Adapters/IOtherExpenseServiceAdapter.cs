using api.Models.Dto;

namespace api.Adapters
{
    public interface IOtherExpenseServiceAdapter
    {
        OtherExpenseDto? GetOtherExpenseData(Guid siteId, string period);
        void SaveOtherExpenseData(OtherExpenseDto otherExpense);
    }
}
