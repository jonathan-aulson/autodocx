using api.Services;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using TownePark;

namespace api.Data.Impl;

public class LockRepository : ILockRepository
{
    private readonly IDataverseService _dataverseService;

    public LockRepository(IDataverseService dataverseService)
    {
        _dataverseService = dataverseService;
    }

    public bs_ResourceLock FetchLock(string resourceId)
    {
        var serviceClient = _dataverseService.GetServiceClient();
        var target = new EntityReference(bs_ResourceLock.EntityLogicalName,
            bs_ResourceLock.Fields.bs_ResourceId, resourceId);
        var request = new RetrieveRequest()
        {
            ColumnSet = new ColumnSet(
                bs_ResourceLock.Fields.bs_ResourceLockId,
                bs_ResourceLock.Fields.bs_IsLocked,
                bs_ResourceLock.Fields.bs_ResourceId),
            Target = target
        };
        var response = (RetrieveResponse) serviceClient.Execute(request);
        return response.Entity.ToEntity<bs_ResourceLock>();
    }

    public void UpdateLock(bs_ResourceLock resourceLock)
    {
        var serviceClient = _dataverseService.GetServiceClient();

        var updatedLock = new bs_ResourceLock
        {
            bs_ResourceLockId = resourceLock.bs_ResourceLockId,
            bs_IsLocked = true,
            RowVersion = resourceLock.RowVersion
        };
        var updateRequest = new UpdateRequest()
        {
            Target = updatedLock,
            ConcurrencyBehavior = ConcurrencyBehavior.IfRowVersionMatches
        };
        serviceClient.Execute(updateRequest);
    }
    
    public void ReleaseLock(Guid lockId)
    {
        var serviceClient = _dataverseService.GetServiceClient();
        var updatedLock = new bs_ResourceLock
        {
            bs_ResourceLockId = lockId,
            bs_IsLocked = false
        };
        serviceClient.Update(updatedLock);
    }

    public Guid CreateLock(string resourceId, bool isLocked = true)
    {
        var serviceClient = _dataverseService.GetServiceClient();
        var newLock = new bs_ResourceLock()
        {
            bs_ResourceId = resourceId,
            bs_IsLocked = isLocked
        };
        return serviceClient.Create(newLock);
    }
}