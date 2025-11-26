namespace api.Adapters;

public interface IStatementTaskServiceAdapter
{
    Guid AddTask(Guid customerSiteId, DateOnly? servicePeriodStart = null);
    IEnumerable<Guid> AddTasks(IEnumerable<Guid> customerSiteIds);
}