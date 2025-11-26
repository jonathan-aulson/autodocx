namespace api.Models.Vo
{
    public class OtherExpenseVo
    {
        public Guid? Id { get; set; }
        public Guid? CustomerSiteId { get; set; }
        public string? SiteNumber { get; set; }
        public string? Name { get; set; }
        public string? BillingPeriod { get; set; }
        public List<OtherExpenseDetailVo>? BudgetData { get; set; } = new List<OtherExpenseDetailVo>();
        public List<OtherExpenseDetailVo>? ForecastData { get; set; } = new List<OtherExpenseDetailVo>();
        public List<OtherExpenseDetailVo>? ActualData { get; set; } = new List<OtherExpenseDetailVo>();
    }

    public class OtherExpenseDetailVo
    {
        public Guid? Id { get; set; }
        public string? MonthYear { get; set; }
        public decimal EmployeeRelations { get; set; }
        public decimal FuelVehicles { get; set; }
        public decimal LossAndDamageClaims { get; set; }
        public decimal OfficeSupplies { get; set; }
        public decimal OutsideServices { get; set; }
        public decimal RentsParking { get; set; }
        public decimal RepairsAndMaintenance { get; set; }
        public decimal RepairsAndMaintenanceVehicle { get; set; }
        public decimal Signage { get; set; }
        public decimal SuppliesAndEquipment { get; set; }
        public decimal TicketsAndPrintedMaterial { get; set; }
        public decimal Uniforms { get; set; }
        public decimal MiscOtherExpenses { get; set; }
        public decimal TotalOtherExpenses { get; set; }
    }
}
