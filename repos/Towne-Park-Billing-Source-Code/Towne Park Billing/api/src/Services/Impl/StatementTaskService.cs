using api.Data;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Xrm.Sdk;
using TownePark;

namespace api.Services.Impl;

public class StatementTaskService : IStatementTaskService
{
    private readonly IStatementTaskRepository _statementTaskRepository;

    private readonly IContractRepository _contractRepository;

    private readonly ILockService _lockService;
    
    private const string LockResourceId = bs_StatementGenerationProcess.EntityLogicalCollectionName;

    public StatementTaskService(IStatementTaskRepository statementTaskRepository,
        IContractRepository contractRepository, ILockService lockService)
    {
        _statementTaskRepository = statementTaskRepository;
        _contractRepository = contractRepository;
        _lockService = lockService;
    }

    public IEnumerable<Guid> AddTasks(IEnumerable<Guid> customerSiteIds)
    {
        var contractIds = _contractRepository.GetContractIdsByCustomerSite(customerSiteIds);
        _lockService.ObtainLockAndExecute(LockResourceId, () => AddTasksHelper(contractIds), out var result);
        return result ?? Enumerable.Empty<Guid>();
    }

    private IEnumerable<Guid> AddTasksHelper(IEnumerable<Guid> contractIds)
    {
        var contractIdList = contractIds.ToList();
        var inProgressOrPending = GetTasksInProgressOrPending(contractIdList).ToList();
        if (!inProgressOrPending.IsNullOrEmpty())
        {
            var pendingTasks = string.Join(", ", inProgressOrPending);
            throw new InvalidOperationException($"A task is already in progress or pending for the following contracts: {pendingTasks}.");
        }

        var tasks = contractIdList.Select(contractId => BuildTaskModel(contractId, null));
        return _statementTaskRepository.AddTasks(tasks);
    }

    private IEnumerable<string> GetTasksInProgressOrPending(IEnumerable<Guid> contractIds)
    {
        var tasks = _statementTaskRepository.FetchInProgressOrPendingTasks();
        return tasks
            .Where(task => contractIds.Contains(task.bs_ContractFK.Id))
            .Select(task => task.bs_ContractFK.Name);
    }

    public Guid AddTask(Guid customerSiteId, DateOnly? servicePeriodStart = null)
    {
        var contractIds = _contractRepository.GetContractIdsByCustomerSite(new[] { customerSiteId });
        var contractId = contractIds.First();
        _lockService.ObtainLockAndExecute(LockResourceId, () => AddTaskHelper(contractId, servicePeriodStart), out var result);
        return result;
    }

    private Guid AddTaskHelper(Guid contractId, DateOnly? servicePeriodStart = null)
    {
        if (IsTaskInProgressOrPending(contractId))
        {
            throw new InvalidOperationException("A task is already in progress or pending for this contract.");
        }

        return _statementTaskRepository.AddTask(BuildTaskModel(contractId, servicePeriodStart));
    }

    private bs_StatementGenerationProcess BuildTaskModel(Guid contractId, DateOnly? servicePeriodStart = null)
    {
        return new bs_StatementGenerationProcess
        {
            bs_ContractFK = new EntityReference(bs_StatementGenerationProcess.EntityLogicalName, contractId),
            bs_Status = bs_statementprocessstatuschoices.Pending,
            bs_Source = bs_statementprocesssourcechoices.Manual,
            // Convert to DateTime? for DataVerse compatibility
            bs_ServicePeriodStart = servicePeriodStart.HasValue
            ? servicePeriodStart.Value.ToDateTime(TimeOnly.MinValue)
            : null
        };
    }

    private bool IsTaskInProgressOrPending(Guid contractId)
    {
        var tasks = _statementTaskRepository.FetchInProgressOrPendingTasksByContract(contractId);
        return !tasks.IsNullOrEmpty();
    }
}