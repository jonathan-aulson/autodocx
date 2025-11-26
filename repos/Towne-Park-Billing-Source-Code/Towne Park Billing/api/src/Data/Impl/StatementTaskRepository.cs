using api.Services;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using TownePark;

namespace api.Data.Impl;

public class StatementTaskRepository : IStatementTaskRepository
{
    private readonly IDataverseService _dataverseService;

    public StatementTaskRepository(IDataverseService dataverseService)
    {
        _dataverseService = dataverseService;
    }

    public Guid AddTask(bs_StatementGenerationProcess model)
    {
        var serviceClient = _dataverseService.GetServiceClient();
        return serviceClient.Create(model);
    }

    public IEnumerable<Guid> AddTasks(IEnumerable<bs_StatementGenerationProcess> models)
    {
        var serviceClient = _dataverseService.GetServiceClient();
    
        var executeMultipleRequest = new ExecuteMultipleRequest()
        {
            Settings = new ExecuteMultipleSettings() 
            {
                ContinueOnError = true,
                ReturnResponses = true
            },
            Requests = new OrganizationRequestCollection()
        };

        foreach (var model in models)
        {
            var createRequest = new CreateRequest { Target = model };
            executeMultipleRequest.Requests.Add(createRequest);
        }

        var executeMultipleResponse = (ExecuteMultipleResponse) serviceClient.Execute(executeMultipleRequest);

        // Collect the IDs of the created records.
        var createdRecordIds = new List<Guid>();
        foreach (var response in executeMultipleResponse.Responses)
        {
            if (response.Response is CreateResponse createResponse)
            {
                createdRecordIds.Add(createResponse.id);
            }
        }
        if (executeMultipleResponse.IsFaulted) throw new Exception("Error adding statement tasks.");
        return createdRecordIds;
    }
    
    public IEnumerable<bs_StatementGenerationProcess> FetchInProgressOrPendingTasks()
    {
        return FetchInProgressOrPendingTasks(_ => {});
    }

    public IEnumerable<bs_StatementGenerationProcess> FetchInProgressOrPendingTasksByContract(Guid contractId)
    {
        return FetchInProgressOrPendingTasks(query => AddContractIdCondition(contractId, query));
    }

    private IEnumerable<bs_StatementGenerationProcess> FetchInProgressOrPendingTasks(Action<QueryExpression> queryConsumer)
    {
        var serviceClient = _dataverseService.GetServiceClient();

        var query = new QueryExpression(bs_StatementGenerationProcess.EntityLogicalName)
        {
            ColumnSet = new ColumnSet(bs_StatementGenerationProcess.Fields.bs_Status,
                bs_StatementGenerationProcess.Fields.bs_ContractFK)
        };
        queryConsumer(query);
        var statusFilter = new FilterExpression(LogicalOperator.Or);
        statusFilter.AddCondition(bs_StatementGenerationProcess.Fields.bs_Status, ConditionOperator.Equal, (int) bs_statementprocessstatuschoices.InProgress);
        statusFilter.AddCondition(bs_StatementGenerationProcess.Fields.bs_Status, ConditionOperator.Equal, (int) bs_statementprocessstatuschoices.Pending);
        query.Criteria.AddFilter(statusFilter);
        return serviceClient.RetrieveMultiple(query).Entities.Cast<bs_StatementGenerationProcess>();
    }

    private void AddContractIdCondition(Guid contractId, QueryExpression query)
    {
        query.Criteria.AddCondition(bs_StatementGenerationProcess.Fields.bs_ContractFK, ConditionOperator.Equal, contractId);
    }
}