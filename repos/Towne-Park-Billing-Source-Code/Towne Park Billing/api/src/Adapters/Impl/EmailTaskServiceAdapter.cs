using api.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownePark;

namespace api.Adapters.Impl
{
    public class EmailTaskServiceAdapter : IEmailTaskServiceAdapter
    {
        private readonly IEmailTaskService _emailTaskService;

        public EmailTaskServiceAdapter(IEmailTaskService emailTaskService)
        {
            _emailTaskService = emailTaskService;
        }

        public Guid AddTask(Guid billingStatementId, bs_sendactionchoices? sendAction = null)
        {
            return _emailTaskService.AddTask(billingStatementId, sendAction);
        }

        public IEnumerable<Guid> AddTasks(IEnumerable<Guid> billingStatementIds)
        {
            return _emailTaskService.AddTasks(billingStatementIds);
        }
    }
}
