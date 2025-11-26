using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TownePark.Billing.Api.Services
{
    public interface IEDWService
    {
        Task<Object> GetEDWDataAsync(
            int storedProcedureId,
            Dictionary<string, object> parameters);
    }
}
