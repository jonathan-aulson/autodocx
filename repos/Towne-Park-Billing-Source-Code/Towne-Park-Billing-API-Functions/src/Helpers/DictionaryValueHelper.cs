using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TownePark.Billing.Api.Helpers
{
    public static class DictionaryValueHelper
    {
        public static T GetValue<T>(this IDictionary<string, object> row, string key, T defaultValue = default)
        {
            if (row.TryGetValue(key, out var value) && value != null && value != DBNull.Value)
            {
                try
                {
                    if (typeof(T) == typeof(DateOnly))
                    {
                        if (value is DateTime dt)
                            return (T)(object)DateOnly.FromDateTime(dt);
                        if (DateTime.TryParse(value.ToString(), out var dt2))
                            return (T)(object)DateOnly.FromDateTime(dt2);
                    }
                    if (typeof(T) == typeof(Guid))
                        return (T)Convert.ChangeType(value, typeof(Guid));

                    var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
                    return (T)Convert.ChangeType(value, targetType);
                }
                catch { }
            }
            return defaultValue;
        }
    }
}
