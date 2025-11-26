
namespace api.Usecases.Impl
{
    public class MonthRangeGenerator : IMonthRangeGenerator
    {
        public  List<string> GenerateMonthRange(string startMonth, int count)
        {
            var result = new List<string>();

            if (!DateTime.TryParseExact(startMonth + "-01", "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out DateTime startDate))
            {
                throw new ArgumentException("Invalid startingMonth format. Expected format: YYYY-MM");
            }

            for (int i = 0; i < count; i++)
            {
                var currentMonth = startDate.AddMonths(i);
                result.Add(currentMonth.ToString("yyyy-MM"));
            }

            return result;
        }
    }
}
