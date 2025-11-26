using System;
using System.Collections.Generic;
using TownePark;

namespace api.Services
{
    public interface IEmailTaskService
    {
        Guid AddTask(Guid billingStatementId, bs_sendactionchoices? sendAction = null);
        IEnumerable<Guid> AddTasks(IEnumerable<Guid> billingStatementIds, bs_sendactionchoices sendAction = bs_sendactionchoices.SendAll);
    }
}
