using api.Services;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using TownePark;

namespace api.Data.Impl
{
    public class EmailTaskRepository : IEmailTaskRepository
    {
        private readonly IDataverseService _dataverseService;

        public EmailTaskRepository(IDataverseService dataverseService)
        {
            _dataverseService = dataverseService;
        }

        public Guid AddTask(bs_EmailGenerationProcess model)
        {
            var serviceClient = _dataverseService.GetServiceClient();
            return serviceClient.Create(model);
        }

        public IEnumerable<bs_EmailGenerationProcess> FetchInProgressOrPendingTasks()
        {
            return FetchInProgressOrPendingTasks(_ => { });
        }

        public IEnumerable<bs_EmailGenerationProcess> FetchInProgressOrPendingTasksByBillingStatement(Guid billingStatementId)
        {
            return FetchInProgressOrPendingTasks(query => AddStatementIdCondition(billingStatementId, query));
        }

        private IEnumerable<bs_EmailGenerationProcess> FetchInProgressOrPendingTasks(Action<QueryExpression> queryConsumer)
        {
            var serviceClient = _dataverseService.GetServiceClient();

            var query = new QueryExpression(bs_EmailGenerationProcess.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(bs_EmailGenerationProcess.Fields.bs_Status,
                    bs_EmailGenerationProcess.Fields.bs_BillingStatementFK)
            };
            queryConsumer(query);
            var statusFilter = new FilterExpression(LogicalOperator.Or);
            statusFilter.AddCondition(bs_EmailGenerationProcess.Fields.bs_Status, ConditionOperator.Equal, (int)bs_statementprocessstatuschoices.InProgress);
            statusFilter.AddCondition(bs_EmailGenerationProcess.Fields.bs_Status, ConditionOperator.Equal, (int)bs_statementprocessstatuschoices.Pending);
            query.Criteria.AddFilter(statusFilter);
            return serviceClient.RetrieveMultiple(query).Entities.Cast<bs_EmailGenerationProcess>();
        }

        private void AddStatementIdCondition(Guid billingStatementId, QueryExpression query)
        {
            query.Criteria.AddCondition(bs_EmailGenerationProcess.Fields.bs_BillingStatementFK, ConditionOperator.Equal, billingStatementId);
        }

        public IEnumerable<Guid> AddTasks(IEnumerable<bs_EmailGenerationProcess> models)
        {
            var serviceClient = _dataverseService.GetServiceClient();

            var executeMultipleRequest = new ExecuteMultipleRequest
            {
                Settings = new ExecuteMultipleSettings
                {
                    ContinueOnError = true,
                    ReturnResponses = true
                },
                Requests = new OrganizationRequestCollection()
            };

            foreach (var model in models)
            {
                var createfRequest = new CreateRequest { Target = model };
                executeMultipleRequest.Requests.Add(createfRequest);
            }

            var executeMultipleResponse = (ExecuteMultipleResponse)serviceClient.Execute(executeMultipleRequest);

            // Collect the IDs of the created records.
            var createdRecordIds = new List<Guid>();
            foreach (var response in executeMultipleResponse.Responses)
            {
                if (response.Response is CreateResponse createResponse)
                {
                    createdRecordIds.Add(createResponse.id);
                }
            }
            if (executeMultipleResponse.IsFaulted) throw new Exception("Error adding email tasks.");
            return createdRecordIds;
        }
    }
}
