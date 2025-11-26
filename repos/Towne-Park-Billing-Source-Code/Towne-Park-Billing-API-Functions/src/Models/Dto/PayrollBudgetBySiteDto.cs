using System;

namespace TownePark.Billing.Api.Models.Dto
{
    public class PayrollBudgetBySiteDto
    {
        public string COST_CENTER { get; set; }
        public int YEAR { get; set; }
        public int MONTH { get; set; }
        public string JOB_PROFILE { get; set; }
        public decimal TOTAL_HOURS { get; set; }
        public decimal TOTAL_COST { get; set; }
    }
}
