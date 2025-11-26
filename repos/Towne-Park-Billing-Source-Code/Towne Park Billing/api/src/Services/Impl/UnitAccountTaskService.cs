using api.Data;
using TownePark;

namespace api.Services.Impl
{
    public class UnitAccountTaskService : IUnitAccountTaskService
    {
        private readonly IUnitAccountTaskRepository _unitAccountTaskRepository;
        private readonly ILockService _lockService;
        private const string LockResourceId = bs_UnitAccountBatchProcess.EntityLogicalName;

        public UnitAccountTaskService(IUnitAccountTaskRepository unitAccountTaskRepository, ILockService lockService)
        {
            _unitAccountTaskRepository = unitAccountTaskRepository;
            _lockService = lockService;
        }

        public Guid AddTask(string servicePeriod)
        {
            _lockService.ObtainLockAndExecute(LockResourceId, () => AddTaskHelper(servicePeriod), out var result);
            return result;
        }

        private Guid AddTaskHelper(string servicePeriod)
        {
            if (IsTaskInProgressOrPending(servicePeriod))
            {
                throw new InvalidOperationException("A task is already in progress or pending for this service period.");
            }

            return _unitAccountTaskRepository.AddTask(servicePeriod);
        }

        private bool IsTaskInProgressOrPending(string servicePeriod)
        {
            var isTaskInProgress = _unitAccountTaskRepository.FetchInProgressOrPendingTask(servicePeriod);
            return isTaskInProgress;
        }
    }
}
