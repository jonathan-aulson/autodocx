using System.ServiceModel;
using api.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using TownePark;

namespace api.Services;

public class LockService : ILockService
{
    private readonly ILockRepository _lockRepository;
    
    private readonly ILogger _logger;
    
    public LockService(ILockRepository lockRepository, ILoggerFactory loggerFactory)
    {
        _lockRepository = lockRepository;
        _logger = loggerFactory.CreateLogger<LockService>();
    }
    
    public void ObtainLockAndExecute<T>(string resourceId, Func<T> action, out T? actionResult)
    {
        if (!TryObtainLock(resourceId, out var lockId))
        {
            throw new InvalidOperationException($"Failed to obtain lock for resource: {resourceId}.");
        }

        try
        {
            actionResult = action();
        }
        finally
        {
            if (lockId.HasValue)
            {
                _lockRepository.ReleaseLock(lockId.Value);
                _logger.LogInformation("Released lock: {lockId}", lockId.Value);
            }
        }
    }
    
    private bool TryObtainLock(string resourceId, out Guid? obtainedLock)
    {
        try
        {
            var currentLock = _lockRepository.FetchLock(resourceId);
            obtainedLock = currentLock.Id;
            return currentLock.bs_IsLocked != true && TryUpdateLock(currentLock);
        }
        catch (FaultException<OrganizationServiceFault> exc)
        {
            // Lock does not exist
            _logger.LogWarning(exc.Message);
            return TryCreateLock(resourceId, out obtainedLock);
        }
    }

    private bool TryCreateLock(string resourceId, out Guid? lockId)
    {
        try
        {
            lockId = _lockRepository.CreateLock(resourceId);
            return true;
        }
        catch (FaultException<OrganizationServiceFault> exc)
        {
            // Failed to obtain lock
            _logger.LogWarning(exc.Message);
            lockId = null;
            return false;
        }
    }

    private bool TryUpdateLock(bs_ResourceLock currentLock)
    {
        try
        {
            _lockRepository.UpdateLock(currentLock);
            return true;
        }
        catch (FaultException<OrganizationServiceFault> exc)
        {
            // Failed to obtain lock
            _logger.LogWarning(exc.Message);
            return false;
        }
    }
}