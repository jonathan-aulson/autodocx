using System.Text.Json.Serialization;

namespace TownePark.Billing.Api.Models.Dto
{
    public class OtherExpenseDto
    {
        [JsonPropertyName("id")]
        public Guid? Id { get; set; } = null;

        [JsonPropertyName("customerSiteId")]
        public Guid? CustomerSiteId { get; set; } = null;

        [JsonPropertyName("siteNumber")]
        public string? SiteNumber { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("billingPeriod")]
        public string? BillingPeriod { get; set; }

        [JsonPropertyName("actualData")]
        public List<OtherExpenseDetailDto>? ActualData { get; set; } = new List<OtherExpenseDetailDto>();

        [JsonPropertyName("budgetData")]
        public List<OtherExpenseDetailDto>? BudgetData { get; set; } = new List<OtherExpenseDetailDto>();
    }

    public class OtherExpenseDetailDto
    {
        [JsonPropertyName("id")]
        public Guid? Id { get; set; } = null;

        [JsonPropertyName("monthYear")]
        public string? MonthYear { get; set; }

        [JsonPropertyName("employeeRelations")]
        public decimal EmployeeRelations { get; set; }

        [JsonPropertyName("fuelVehicles")]
        public decimal FuelVehicles { get; set; }

        [JsonPropertyName("lossAndDamageClaims")]
        public decimal LossAndDamageClaims { get; set; }

        [JsonPropertyName("officeSupplies")]
        public decimal OfficeSupplies { get; set; }

        [JsonPropertyName("outsideServices")]
        public decimal OutsideServices { get; set; }

        [JsonPropertyName("rentsParking")]
        public decimal RentsParking { get; set; }

        [JsonPropertyName("repairsAndMaintenance")]
        public decimal RepairsAndMaintenance { get; set; }

        [JsonPropertyName("repairsAndMaintenanceVehicle")]
        public decimal RepairsAndMaintenanceVehicle { get; set; }

        [JsonPropertyName("signage")]
        public decimal Signage { get; set; }

        [JsonPropertyName("suppliesAndEquipment")]
        public decimal SuppliesAndEquipment { get; set; }

        [JsonPropertyName("ticketsAndPrintedMaterial")]
        public decimal TicketsAndPrintedMaterial { get; set; }

        [JsonPropertyName("uniforms")]
        public decimal Uniforms { get; set; }

        [JsonPropertyName("miscOtherExpenses")]
        public decimal MiscOtherExpenses { get; set; }

        [JsonPropertyName("totalOtherExpenses")]
        public decimal TotalOtherExpenses { get; set; }
    }
}