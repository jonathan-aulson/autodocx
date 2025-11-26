using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api.Models.Common
{
    public enum StoredProcedureId
    {
        spBUDGET_DAILY_DETAIL = 1,
        spBUDGET_AND_ACTUAL_PARKING_RATES_DETAIL = 3,
        spBudget_Actual_Summary = 2,
        spBudget_Actual_Summary_BySite = 4,
        Payroll_Budget = 5,
        Payroll_Actuals = 6,
        Payroll_Schedule = 7,
        Statistics_Actual = 8,
        Other_Expenses_Actual = 9,
        Other_Expenses_Budget = 10,
        Internal_Revenue_Actuals = 11
    }
}
