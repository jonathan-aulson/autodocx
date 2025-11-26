using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TownePark.Billing.Api.Data
{
    public interface IEDWRepository
    {
        Task<List<Dictionary<string, object>>> ExecuteQueryAsync(
            string sqlOrProcName,
            Dictionary<string, object> parameters = null,
            CommandType commandType = CommandType.Text);
    }
}
