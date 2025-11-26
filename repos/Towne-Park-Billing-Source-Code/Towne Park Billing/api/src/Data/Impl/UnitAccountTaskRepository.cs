using api.Services;
using TownePark;
using Microsoft.Xrm.Sdk.Query;

namespace api.Data.Impl
{
    public class UnitAccountTaskRepository : IUnitAccountTaskRepository
    {
        private readonly IDataverseService _dataverseService;

        public UnitAccountTaskRepository(IDataverseService dataverseService)
        {
            _dataverseService = dataverseService;
        }

        public Guid AddTask(string servicePeriod)
        {
            var serviceClient = _dataverseService.GetServiceClient();
            var task = new bs_UnitAccountBatchProcess
            {
                bs_Period = servicePeriod,
                bs_Status = bs_unitaccountbatchprocessstatuschoices.Pending
            };
            return serviceClient.Create(task);
        }

        public bool FetchInProgressOrPendingTask(string servicePeriod)
        {
            var serviceClient = _dataverseService.GetServiceClient();

            // Create the query expression
            var query = new QueryExpression(bs_UnitAccountBatchProcess.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(
                    bs_UnitAccountBatchProcess.Fields.bs_Period,
                    bs_UnitAccountBatchProcess.Fields.bs_Status
                )
            };

            // Add condition for the service period
            query.Criteria.AddCondition(
                bs_UnitAccountBatchProcess.Fields.bs_Period,
                ConditionOperator.Equal,
                servicePeriod
            );

            // Add a filter for the "In Progress" or "Pending" statuses
            var statusFilter = new FilterExpression(LogicalOperator.Or);
            statusFilter.AddCondition(
                bs_UnitAccountBatchProcess.Fields.bs_Status,
                ConditionOperator.Equal,
                (int)bs_unitaccountbatchprocessstatuschoices.InProgress
            );
            statusFilter.AddCondition(
                bs_UnitAccountBatchProcess.Fields.bs_Status,
                ConditionOperator.Equal,
                (int)bs_unitaccountbatchprocessstatuschoices.Pending
            );

            // Add the status filter to the query criteria
            query.Criteria.AddFilter(statusFilter);

            // Retrieve the results and check if any match the criteria
            var results = serviceClient.RetrieveMultiple(query).Entities.Cast<bs_UnitAccountBatchProcess>();
            return results.Any();
        }

    }
}
