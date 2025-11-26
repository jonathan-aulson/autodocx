using TownePark;

namespace api.Data;

public interface ILockRepository
{
    bs_ResourceLock FetchLock(string resourceId);
    void UpdateLock(bs_ResourceLock resourceLock);
    Guid CreateLock(string resourceId, bool isLocked = true);
    void ReleaseLock(Guid lockId);
}