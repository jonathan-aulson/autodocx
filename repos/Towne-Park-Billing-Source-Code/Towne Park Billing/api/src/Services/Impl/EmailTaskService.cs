using api.Data;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Xrm.Sdk;
using TownePark;

namespace api.Services.Impl
{
    public class EmailTaskService : IEmailTaskService
    {
        private readonly IEmailTaskRepository _emailTaskRepository;
        private readonly IBillingStatementRepository _billingStatementRepository;
        private readonly ILockService _lockService;
        private const string LockResourceId = bs_EmailGenerationProcess.EntityLogicalCollectionName;

        public EmailTaskService(IEmailTaskRepository emailGenerationRepository,
            IBillingStatementRepository billingStatementRepository, ILockService lockService)
        {
            _emailTaskRepository = emailGenerationRepository;
            _billingStatementRepository = billingStatementRepository;
            _lockService = lockService;
        }

        public Guid AddTask(Guid billingStatementId, bs_sendactionchoices? sendAction = null)
        {
            var billingStatement = _billingStatementRepository.GetBillingStatementById(billingStatementId);
            _lockService.ObtainLockAndExecute(LockResourceId, () => AddTaskHelper(billingStatementId, sendAction), out var result);
            return result;
        }

        public IEnumerable<Guid> AddTasks(IEnumerable<Guid> billingStatementIds, bs_sendactionchoices sendAction = bs_sendactionchoices.SendAll)
        {
            var billingStatements = _billingStatementRepository.GetBillingStatementsByIds(billingStatementIds);
            _lockService.ObtainLockAndExecute(LockResourceId, () => AddTasksHelper(billingStatementIds, sendAction), out var result);
            return result ?? Enumerable.Empty<Guid>();
        }

        private IEnumerable<Guid> AddTasksHelper(IEnumerable<Guid> billingStatementIds, bs_sendactionchoices sendAction)
        {
            var billingStatementIdList = billingStatementIds.ToList();
            var inProgressOrPending = GetTasksInProgressOrPending(billingStatementIdList).ToList();
            if (!inProgressOrPending.IsNullOrEmpty())
            {
                var pendingTasks = string.Join(", ", inProgressOrPending);
                throw new InvalidOperationException($"A task is already in progress or pending for the following billing statements: {pendingTasks}.");
            }

            var tasks = billingStatementIdList.Select(id => BuildTaskModel(id, sendAction));
            return _emailTaskRepository.AddTasks(tasks);
        }

        private IEnumerable<string> GetTasksInProgressOrPending(IEnumerable<Guid> billingStatementIds)
        {
            var tasks = _emailTaskRepository.FetchInProgressOrPendingTasks();
            return tasks
                .Where(task => billingStatementIds.Contains(task.bs_BillingStatementFK.Id))
                .Select(task => task.bs_BillingStatementFK.Name);
        }

        private Guid AddTaskHelper(Guid billingStatementId, bs_sendactionchoices? sendAction = null)
        {
            if (IsTaskInProgressOrPending(billingStatementId))
            {
                throw new InvalidOperationException("A task is already in progress or pending for this billing statement.");
            }

            return _emailTaskRepository.AddTask(BuildTaskModel(billingStatementId, sendAction));
        }

        private bs_EmailGenerationProcess BuildTaskModel(Guid billingStatementId, bs_sendactionchoices? sendAction = null)
        {
            return new bs_EmailGenerationProcess
            {
                bs_BillingStatementFK = new EntityReference(bs_EmailGenerationProcess.EntityLogicalName, billingStatementId),
                bs_Status = bs_sendemailstatuschoices.Pending,
                bs_SendAction = sendAction
            };
        }

        private bool IsTaskInProgressOrPending(Guid billingStatementId)
        {
            var tasks = _emailTaskRepository.FetchInProgressOrPendingTasksByBillingStatement(billingStatementId);
            return !tasks.IsNullOrEmpty();
        }
    }
}

