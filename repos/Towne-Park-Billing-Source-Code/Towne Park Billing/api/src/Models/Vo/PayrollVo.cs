namespace api.Models.Vo
{
    public class PayrollVo
    {
        public Guid Id { get; set; }
        public string? SiteNumber { get; set; }
        public Guid CustomerSiteId { get; set; }
        public string? Name { get; set; }
        public string? BillingPeriod { get; set; }
        public PayrollForecastModeType PayrollForecastMode { get; set; }
        public List<JobGroupForecastVo>? ForecastPayroll { get; set; } = new List<JobGroupForecastVo>();
        public List<JobGroupBudgetVo>? BudgetPayroll { get; set; } = new List<JobGroupBudgetVo>();
        public List<JobGroupActualVo>? ActualPayroll { get; set; } = new List<JobGroupActualVo>();
        public List<JobGroupScheduledVo>? ScheduledPayroll { get; set; } = new List<JobGroupScheduledVo>();
    }

    public class JobGroupForecastVo
    {
        public Guid? Id { get; set; }
        public Guid JobGroupId { get; set; }
        public string? JobGroupName { get; set; }
        public decimal ForecastHours { get; set; }
        public DateOnly? Date { get; set; }
        public List<JobCodeForecastVo>? JobCodes { get; set; }
        public decimal? ForecastPayrollCost { get; set; }
        public decimal? ForecastPayrollRevenue { get; set; }
    }

    public class JobCodeForecastVo
    {
        public Guid? Id { get; set; }
        public Guid JobCodeId { get; set; }
        public string? JobCode { get; set; }
        public string? DisplayName { get; set; }
        public decimal ForecastHours { get; set; }
        public DateOnly? Date { get; set; }
        public decimal? ForecastPayrollCost { get; set; }
        public decimal? ForecastPayrollRevenue { get; set; }
    }

    public class JobGroupBudgetVo
    {
        public Guid? Id { get; set; }
        public Guid JobGroupId { get; set; }
        public string? JobGroupName { get; set; }
        public decimal BudgetHours { get; set; }
        public DateOnly? Date { get; set; }
        public List<JobCodeBudgetVo>? JobCodes { get; set; }
        public decimal? BudgetPayrollCost { get; set; }
        public decimal? BudgetPayrollRevenue { get; set; }
    }

    public class JobCodeBudgetVo
    {
        public Guid? Id { get; set; }
        public Guid JobCodeId { get; set; }
        public string? JobCode { get; set; }
        public string? DisplayName { get; set; }
        public decimal BudgetHours { get; set; }
        public DateOnly? Date { get; set; }
        public decimal? BudgetPayrollCost { get; set; }
        public decimal? BudgetPayrollRevenue { get; set; }
    }

    public class JobGroupActualVo
    {
        public Guid? Id { get; set; }
        public Guid JobGroupId { get; set; }
        public string? JobGroupName { get; set; }
        public decimal ActualHours { get; set; }
        public DateOnly? Date { get; set; }
        public List<JobCodeActualVo>? JobCodes { get; set; }
        public decimal? ActualPayrollCost { get; set; }
        public decimal? ActualPayrollRevenue { get; set; }
    }

    public class JobCodeActualVo
    {
        public Guid? Id { get; set; }
        public Guid JobCodeId { get; set; }
        public string? JobCode { get; set; }
        public string? DisplayName { get; set; }
        public decimal ActualHours { get; set; }
        public DateOnly? Date { get; set; }
        public decimal? ActualPayrollCost { get; set; }
        public decimal? ActualPayrollRevenue { get; set; }
    }

    public class JobGroupScheduledVo
    {
        public Guid? Id { get; set; }
        public Guid JobGroupId { get; set; }
        public string? JobGroupName { get; set; }
        public decimal ScheduledHours { get; set; }
        public DateOnly? Date { get; set; }
        public List<JobCodeScheduledVo>? JobCodes { get; set; }
        public decimal? ScheduledPayrollCost { get; set; }
        public decimal? ScheduledPayrollRevenue { get; set; }
    }

    public class JobCodeScheduledVo
    {
        public Guid? Id { get; set; }
        public Guid JobCodeId { get; set; }
        public string? JobCode { get; set; }
        public string? DisplayName { get; set; }
        public decimal ScheduledHours { get; set; }
        public DateOnly? Date { get; set; }
        public decimal? ScheduledPayrollCost { get; set; }
        public decimal? ScheduledPayrollRevenue { get; set; }
    }

    public class PayrollDetailVo
    {
        public Guid Id { get; set; }
        public DateOnly? Date { get; set; }
        public string? DisplayName { get; set; }
        public string? JobCode { get; set; }
        public decimal RegularHours { get; set; }
    }

    public enum PayrollForecastModeType
    {
        Code = 126840000,
        Group = 126840001
    }
}
