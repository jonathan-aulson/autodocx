using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api.Models.Vo
{
    public class ExpenseActualsDataVo
    {
        public Guid SiteId { get; set; }
        public decimal BillableExpenseActuals { get; set; }
        public decimal OtherExpenseActuals { get; set; }
    }

}
