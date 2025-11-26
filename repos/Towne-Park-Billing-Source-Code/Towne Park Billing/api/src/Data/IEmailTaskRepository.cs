using TownePark;

namespace api.Data
{
    public interface IEmailTaskRepository
    {
        Guid AddTask(bs_EmailGenerationProcess model);
        IEnumerable<bs_EmailGenerationProcess> FetchInProgressOrPendingTasksByBillingStatement(Guid billingStatementId);
        IEnumerable<Guid> AddTasks(IEnumerable<bs_EmailGenerationProcess> models);
        IEnumerable<bs_EmailGenerationProcess> FetchInProgressOrPendingTasks();
    }
}
