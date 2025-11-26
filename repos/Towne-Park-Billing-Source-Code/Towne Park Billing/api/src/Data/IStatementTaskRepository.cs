using TownePark;

namespace api.Data;

public interface IStatementTaskRepository
{
    Guid AddTask(bs_StatementGenerationProcess model);
    IEnumerable<bs_StatementGenerationProcess> FetchInProgressOrPendingTasksByContract(Guid contractId);
    IEnumerable<Guid> AddTasks(IEnumerable<bs_StatementGenerationProcess> models);
    IEnumerable<bs_StatementGenerationProcess> FetchInProgressOrPendingTasks();
}