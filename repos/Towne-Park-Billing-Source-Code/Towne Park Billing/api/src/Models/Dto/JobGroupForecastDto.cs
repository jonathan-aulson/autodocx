using Newtonsoft.Json;

namespace api.Models.Dto
{
    public class JobGroupForecastDto
    {
        [JsonProperty("id")]
        public Guid? Id { get; set; }

        [JsonProperty("jobGroupId")]
        public Guid JobGroupId { get; set; }

        [JsonProperty("jobGroupName")]
        public string? JobGroupName { get; set; }

        [JsonProperty("forecastHours")]
        public decimal ForecastHours { get; set; }

        [JsonProperty("date")]
        public DateOnly? Date { get; set; }

        [JsonProperty("jobCodes")]
        public List<JobCodeForecastDto>? JobCodes { get; set; }

        [JsonProperty("forecastPayrollCost")]
        public decimal? ForecastPayrollCost { get; set; }

        [JsonProperty("forecastPayrollRevenue")]
        public decimal? ForecastPayrollRevenue { get; set; }
    }

    public class JobCodeForecastDto
    {
        [JsonProperty("id")]
        public Guid? Id { get; set; }

        [JsonProperty("jobCodeId")]
        public Guid JobCodeId { get; set; }

        [JsonProperty("jobCode")]
        public string? JobCode { get; set; }

        [JsonProperty("displayName")]
        public string? DisplayName { get; set; }

        [JsonProperty("forecastHours")]
        public decimal ForecastHours { get; set; }

        [JsonProperty("date")]
        public DateOnly? Date { get; set; }

        [JsonProperty("forecastPayrollCost")]
        public decimal? ForecastPayrollCost { get; set; }

        [JsonProperty("forecastPayrollRevenue")]
        public decimal? ForecastPayrollRevenue { get; set; }
    }

    public class JobGroupBudgetDto
    {
        [JsonProperty("id")]
        public Guid? Id { get; set; }

        [JsonProperty("jobGroupId")]
        public Guid JobGroupId { get; set; }

        [JsonProperty("jobGroupName")]
        public string? JobGroupName { get; set; }

        [JsonProperty("budgetHours")]
        public decimal BudgetHours { get; set; }

        [JsonProperty("date")]
        public DateOnly? Date { get; set; }

        [JsonProperty("jobCodes")]
        public List<JobCodeBudgetDto>? JobCodes { get; set; }

        [JsonProperty("budgetPayrollCost")]
        public decimal? BudgetPayrollCost { get; set; }

        [JsonProperty("budgetPayrollRevenue")]
        public decimal? BudgetPayrollRevenue { get; set; }
    }

    public class JobCodeBudgetDto
    {
        [JsonProperty("id")]
        public Guid? Id { get; set; }

        [JsonProperty("jobCodeId")]
        public Guid JobCodeId { get; set; }

        [JsonProperty("jobCode")]
        public string? JobCode { get; set; }

        [JsonProperty("displayName")]
        public string? DisplayName { get; set; }

        [JsonProperty("budgetHours")]
        public decimal BudgetHours { get; set; }

        [JsonProperty("date")]
        public DateOnly? Date { get; set; }

        [JsonProperty("budgetPayrollCost")]
        public decimal? BudgetPayrollCost { get; set; }

        [JsonProperty("budgetPayrollRevenue")]
        public decimal? BudgetPayrollRevenue { get; set; }
    }

    public class JobGroupActualDto
    {
        [JsonProperty("id")]
        public Guid? Id { get; set; }

        [JsonProperty("jobGroupId")]
        public Guid JobGroupId { get; set; }

        [JsonProperty("jobGroupName")]
        public string? JobGroupName { get; set; }

        [JsonProperty("actualHours")]
        public decimal ActualHours { get; set; }

        [JsonProperty("date")]
        public DateOnly? Date { get; set; }

        [JsonProperty("jobCodes")]
        public List<JobCodeActualDto>? JobCodes { get; set; }

        [JsonProperty("actualPayrollCost")]
        public decimal? ActualPayrollCost { get; set; }

        [JsonProperty("actualPayrollRevenue")]
        public decimal? ActualPayrollRevenue { get; set; }
    }

    public class JobCodeActualDto
    {
        [JsonProperty("id")]
        public Guid? Id { get; set; }

        [JsonProperty("jobCodeId")]
        public Guid JobCodeId { get; set; }

        [JsonProperty("jobCode")]
        public string? JobCode { get; set; }

        [JsonProperty("displayName")]
        public string? DisplayName { get; set; }

        [JsonProperty("actualHours")]
        public decimal ActualHours { get; set; }

        [JsonProperty("date")]
        public DateOnly? Date { get; set; }

        [JsonProperty("actualPayrollCost")]
        public decimal? ActualPayrollCost { get; set; }

        [JsonProperty("actualPayrollRevenue")]
        public decimal? ActualPayrollRevenue { get; set; }
    }

    public class JobGroupScheduledDto
    {
        [JsonProperty("id")]
        public Guid? Id { get; set; }

        [JsonProperty("jobGroupId")]
        public Guid JobGroupId { get; set; }

        [JsonProperty("jobGroupName")]
        public string? JobGroupName { get; set; }

        [JsonProperty("scheduledHours")]
        public decimal ScheduledHours { get; set; }

        [JsonProperty("date")]
        public DateOnly? Date { get; set; }

        [JsonProperty("jobCodes")]
        public List<JobCodeScheduledDto>? JobCodes { get; set; }

        [JsonProperty("scheduledPayrollCost")]
        public decimal? ScheduledPayrollCost { get; set; }

        [JsonProperty("scheduledPayrollRevenue")]
        public decimal? ScheduledPayrollRevenue { get; set; }
    }

    public class JobCodeScheduledDto
    {
        [JsonProperty("id")]
        public Guid? Id { get; set; }

        [JsonProperty("jobCodeId")]
        public Guid JobCodeId { get; set; }

        [JsonProperty("jobCode")]
        public string? JobCode { get; set; }

        [JsonProperty("displayName")]
        public string? DisplayName { get; set; }

        [JsonProperty("scheduledHours")]
        public decimal ScheduledHours { get; set; }

        [JsonProperty("date")]
        public DateOnly? Date { get; set; }

        [JsonProperty("scheduledPayrollCost")]
        public decimal? ScheduledPayrollCost { get; set; }

        [JsonProperty("scheduledPayrollRevenue")]
        public decimal? ScheduledPayrollRevenue { get; set; }
    }
}
