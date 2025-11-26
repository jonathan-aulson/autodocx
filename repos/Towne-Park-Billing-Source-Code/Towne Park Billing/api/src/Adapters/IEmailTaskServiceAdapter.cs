using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownePark;

namespace api.Adapters
{
    public interface IEmailTaskServiceAdapter
    {
        Guid AddTask(Guid billingStatementId, bs_sendactionchoices? sendAction = null);
        IEnumerable<Guid> AddTasks(IEnumerable<Guid> billingStatementIds);
    }
}
