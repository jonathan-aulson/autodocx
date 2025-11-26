namespace api.Services;

public interface IStatementTaskService
{
    Guid AddTask(Guid customerSiteId, DateOnly? servicePeriodStart = null);
    IEnumerable<Guid> AddTasks(IEnumerable<Guid> customerSiteIds);
}