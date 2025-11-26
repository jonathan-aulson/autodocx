using api.Models.Dto;

namespace api.Adapters
{
    public interface IPayrollServiceAdapter
    {
        PayrollDto? GetPayroll(Guid siteId, string billingPeriod);
        void SavePayroll(PayrollDto updates);
    }
}
