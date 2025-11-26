namespace api.Services;

public interface ILockService
{
    void ObtainLockAndExecute<T>(string resourceId, Func<T> action, out T? actionResult);
}