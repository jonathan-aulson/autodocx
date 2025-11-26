

using api.Models.Vo;

namespace api.Services
{
    public interface IPayrollService
    {
        PayrollVo? GetPayroll(Guid siteId, string billingPeriod);
        void SavePayroll(PayrollVo updates);
    }
}
