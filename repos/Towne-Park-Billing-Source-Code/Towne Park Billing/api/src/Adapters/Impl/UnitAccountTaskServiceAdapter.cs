using api.Services;

namespace api.Adapters.Impl
{
    public class UnitAccountTaskServiceAdapter : IUnitAccountTaskServiceAdapter
    {
        IUnitAccountTaskService _unitAccountTaskService;

        public UnitAccountTaskServiceAdapter(IUnitAccountTaskService unitAccountTaskService)
        {
            _unitAccountTaskService = unitAccountTaskService;
        }

        public Guid AddTask(string servicePeriod)
        {
            return _unitAccountTaskService.AddTask(servicePeriod);
        }
    }
}
