using api.Services;

namespace api.Adapters.Impl;

public class StatementTaskServiceAdapter : IStatementTaskServiceAdapter
{
    private readonly IStatementTaskService _statementTaskService;

    public StatementTaskServiceAdapter(IStatementTaskService statementTaskService)
    {
        _statementTaskService = statementTaskService;
    }

    public IEnumerable<Guid> AddTasks(IEnumerable<Guid> customerSiteIds)
    {
        return _statementTaskService.AddTasks(customerSiteIds);
    }

    public Guid AddTask(Guid customerSiteId, DateOnly? servicePeriodStart = null)
    {
        return _statementTaskService.AddTask(customerSiteId, servicePeriodStart);
    }
}