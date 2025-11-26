using api.Adapters.Mappers;
using api.Data;
using api.Models.Vo;
using TownePark;
using System.Linq;

namespace api.Services.Impl
{
    public class OtherExpenseService : IOtherExpenseService
    {
        private readonly IOtherExpenseRepository _otherExpenseRepository;
        private readonly ICustomerRepository _customerRepository;

        public OtherExpenseService(IOtherExpenseRepository otherExpenseRepository, ICustomerRepository customerRepository)
        {
            _otherExpenseRepository = otherExpenseRepository;
            _customerRepository = customerRepository;
        }

        public OtherExpenseVo? GetOtherExpenseData(Guid siteId, string period)
        {
            var model = _otherExpenseRepository.GetOtherExpenseDetail(siteId, period) ?? new List<bs_OtherExpenseDetail>();

            var otherExpense = OtherExpenseMapper.OtherExpenseModelToVo(model, siteId, period);

            otherExpense.CustomerSiteId = siteId;
            otherExpense.BillingPeriod = period;

            // Resolve SiteNumber if it's empty (when no forecast data exists)
            if (string.IsNullOrEmpty(otherExpense.SiteNumber))
            {
                try
                {
                    var customerSite = _customerRepository.GetCustomerDetail(siteId);
                    otherExpense.SiteNumber = customerSite?.bs_SiteNumber ?? string.Empty;
                }
                catch (Exception ex)
                {
                    // Log the error but continue with empty SiteNumber
                    // This prevents the service from failing completely if customer site lookup fails
                    otherExpense.SiteNumber = string.Empty;
                }
            }

            // Fetch actual data if SiteNumber is available
            if (!string.IsNullOrEmpty(otherExpense.SiteNumber))
            {
                var actualData = FormatMonthYear(_otherExpenseRepository.GetActualData(otherExpense.SiteNumber, period).Result);
                otherExpense.ActualData = actualData;
            }

            // Fetch budget data if SiteNumber is available
            if (!string.IsNullOrEmpty(otherExpense.SiteNumber))
            {
                var budgetData = FormatMonthYear(_otherExpenseRepository.GetBudgetData(otherExpense.SiteNumber, period).Result);
                otherExpense.BudgetData = budgetData;
            }
            return otherExpense;
        }

        public void SaveOtherExpenseData(OtherExpenseVo otherExpense)
        {
            List<bs_OtherExpenseDetail> model = OtherExpenseMapper.OtherExpenseVoToModel(otherExpense);
            _otherExpenseRepository.UpdateOtherRevenueDetails(model);
        }

        private List<OtherExpenseDetailVo> FormatMonthYear(List<OtherExpenseDetailVo> vos)
        {
            if (vos == null)
                return new List<OtherExpenseDetailVo>();

            List<OtherExpenseDetailVo> formattedVos = new List<OtherExpenseDetailVo>();

            foreach (var vo in vos)
            {
                if (vo != null && !string.IsNullOrEmpty(vo.MonthYear))
                {
                    string year = vo.MonthYear.Substring(0, 4);
                    string month = vo.MonthYear.Substring(4, 2);
                    vo.MonthYear = $"{year}-{month}";
                }
                formattedVos.Add(vo);
            }
            return formattedVos;
        }
    }
}
