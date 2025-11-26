using api.Models.Vo;

namespace api.Services
{
    public interface IOtherExpenseService
    {
        OtherExpenseVo? GetOtherExpenseData(Guid siteId, string period);
        void SaveOtherExpenseData(OtherExpenseVo otherExpense);
    }
}
