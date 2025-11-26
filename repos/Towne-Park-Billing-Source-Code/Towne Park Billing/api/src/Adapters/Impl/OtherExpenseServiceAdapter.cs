using api.Adapters.Mappers;
using api.Models.Dto;
using api.Services;

namespace api.Adapters.Impl
{
    public class OtherExpenseServiceAdapter : IOtherExpenseServiceAdapter
    {
        private readonly IOtherExpenseService _otherExpenseService;

        public OtherExpenseServiceAdapter(IOtherExpenseService otherExpenseService)
        {
            _otherExpenseService = otherExpenseService;
        }

        public OtherExpenseDto? GetOtherExpenseData(Guid siteId, string period)
        {
            var otherExpense = _otherExpenseService.GetOtherExpenseData(siteId, period);
            if (otherExpense == null)
                return new OtherExpenseDto();
            return OtherExpenseMapper.OtherExpenseVoToDto(otherExpense);
        }

        public void SaveOtherExpenseData(OtherExpenseDto otherExpense)
        {
            var otherExpenseVo = OtherExpenseMapper.OtherExpenseDtoToVo(otherExpense);
            if (otherExpenseVo == null)
                return;
            _otherExpenseService.SaveOtherExpenseData(otherExpenseVo);
        }
    }
}
