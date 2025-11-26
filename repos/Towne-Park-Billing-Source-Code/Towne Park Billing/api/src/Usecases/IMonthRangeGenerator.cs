

namespace api.Usecases
{
    public interface IMonthRangeGenerator
    {
        List<string> GenerateMonthRange(string startMonth, int count);
    }
}
