using api.Adapters.Mappers;
using api.Models.Dto;
using api.Models.Vo;
using api.Services;

namespace api.Adapters.Impl
{
    public class PayrollServiceAdapter : IPayrollServiceAdapter
    {
        private readonly IPayrollService _payrollService;
        private readonly ICustomerService _customerService;
        private readonly IContractService _contractService;

        public PayrollServiceAdapter(IPayrollService payrollService, ICustomerService customerService, IContractService contractService)
        {
            _payrollService = payrollService;
            _customerService = customerService;
            _contractService = contractService;
        }

        public PayrollDto? GetPayroll(Guid siteId, string billingPeriod)
        {
            var vo = _payrollService.GetPayroll(siteId, billingPeriod);
            if (vo == null)
            {
                // Fetch site/customer info for parent fields
                var customer = _customerService.GetCustomerDetail(siteId);
                var contract = _contractService.GetContractDetail(siteId);
                var forecastMode = PayrollForecastModeType.Group.ToString();
                if (contract?.ContractTypeString != null && contract.ContractTypeString.Contains("Per Labor Hour", StringComparison.OrdinalIgnoreCase))
                {
                    forecastMode = "Code";
                }
                // Handle null customer gracefully
                return new PayrollDto
                {
                    Id = null,
                    Name = customer?.SiteName,
                    SiteNumber = customer?.SiteNumber,
                    CustomerSiteId = customer?.CustomerSiteId ?? siteId,
                    BillingPeriod = billingPeriod,
                    PayrollForecastMode = forecastMode,
                    ForecastPayroll = new List<JobGroupForecastDto>(),
                    BudgetPayroll = new List<JobGroupBudgetDto>(),
                    ActualPayroll = new List<JobGroupActualDto>(),
                    ScheduledPayroll = new List<JobGroupScheduledDto>()
                };
            }
            return PayrollMapper.PayrollVoToDto(vo);
        }

        public void SavePayroll(PayrollDto updates)
        {
            _payrollService.SavePayroll(PayrollMapper.PayrollDtoToVo(updates));
        }
    }
}
