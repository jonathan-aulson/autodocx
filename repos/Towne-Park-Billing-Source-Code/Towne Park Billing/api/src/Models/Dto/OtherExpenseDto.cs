using Newtonsoft.Json;

namespace api.Models.Dto
{
    public class OtherExpenseDto
    {
        [JsonProperty("id")]
        public Guid? Id { get; set; }

        [JsonProperty("customerSiteId")]
        public Guid? CustomerSiteId { get; set; }

        [JsonProperty("siteNumber")]
        public string? SiteNumber { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("billingPeriod")]
        public string? BillingPeriod { get; set; }

        [JsonProperty("budgetData")]
        public List<OtherExpenseDetailDto>? BudgetData { get; set; } = new List<OtherExpenseDetailDto>();

        [JsonProperty("forecastData")]
        public List<OtherExpenseDetailDto>? ForecastData { get; set; } = new List<OtherExpenseDetailDto>();

        [JsonProperty("actualData")]
        public List<OtherExpenseDetailDto>? ActualData { get; set; } = new List<OtherExpenseDetailDto>();
    }

    public class OtherExpenseDetailDto
    {
        [JsonProperty("id")]
        public Guid? Id { get; set; }

        [JsonProperty("monthYear")]
        public string? MonthYear { get; set; }

        [JsonProperty("employeeRelations")]
        public decimal EmployeeRelations { get; set; }

        [JsonProperty("fuelVehicles")]
        public decimal FuelVehicles { get; set; }

        [JsonProperty("lossAndDamageClaims")]
        public decimal LossAndDamageClaims { get; set; }

        [JsonProperty("officeSupplies")]
        public decimal OfficeSupplies { get; set; }

        [JsonProperty("outsideServices")]
        public decimal OutsideServices { get; set; }

        [JsonProperty("rentsParking")]
        public decimal RentsParking { get; set; }

        [JsonProperty("repairsAndMaintenance")]
        public decimal RepairsAndMaintenance { get; set; }

        [JsonProperty("repairsAndMaintenanceVehicle")]
        public decimal RepairsAndMaintenanceVehicle { get; set; }

        [JsonProperty("signage")]
        public decimal Signage { get; set; }

        [JsonProperty("suppliesAndEquipment")]
        public decimal SuppliesAndEquipment { get; set; }

        [JsonProperty("ticketsAndPrintedMaterial")]
        public decimal TicketsAndPrintedMaterial { get; set; }

        [JsonProperty("uniforms")]
        public decimal Uniforms { get; set; }

        [JsonProperty("miscOtherExpenses")]
        public decimal MiscOtherExpenses { get; set; }

        [JsonProperty("totalOtherExpenses")]
        public decimal TotalOtherExpenses { get; set; }
    }
}
