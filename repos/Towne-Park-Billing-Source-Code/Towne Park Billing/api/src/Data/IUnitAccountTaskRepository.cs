namespace api.Data
{
    public interface IUnitAccountTaskRepository
    {
        Guid AddTask(string servicePeriod);
        bool FetchInProgressOrPendingTask(string servicePeriod);
    }
}
