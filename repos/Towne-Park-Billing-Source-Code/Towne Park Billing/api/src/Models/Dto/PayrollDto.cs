using Newtonsoft.Json;

namespace api.Models.Dto
{
    public class PayrollDto
    {
        [JsonProperty("id")]
        public Guid? Id { get; set; }

        [JsonProperty("siteNumber")]
        public string? SiteNumber { get; set; }

        [JsonProperty("customerSiteId")]
        public Guid CustomerSiteId { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("billingPeriod")]
        public string? BillingPeriod { get; set; }

        [JsonProperty("payrollForecastMode")]
        public string? PayrollForecastMode { get; set; }

        [JsonProperty("forecastPayroll")]
        public List<JobGroupForecastDto>? ForecastPayroll { get; set; } = new List<JobGroupForecastDto>();

        [JsonProperty("budgetPayroll")]
        public List<JobGroupBudgetDto>? BudgetPayroll { get; set; } = new List<JobGroupBudgetDto>();

        [JsonProperty("actualPayroll")]
        public List<JobGroupActualDto>? ActualPayroll { get; set; } = new List<JobGroupActualDto>();

        [JsonProperty("scheduledPayroll")]
        public List<JobGroupScheduledDto>? ScheduledPayroll { get; set; } = new List<JobGroupScheduledDto>();
    }

    public class PayrollDetailDto
    {
        [JsonProperty("id")]
        public Guid? Id { get; set; }
        [JsonProperty("date")]
        public DateOnly? Date { get; set; }
        [JsonProperty("displayName")]
        public string? DisplayName { get; set; }
        [JsonProperty("jobCode")]
        public string? JobCode { get; set; }
        [JsonProperty("regularHours")]
        public decimal RegularHours { get; set; }
    }
}
