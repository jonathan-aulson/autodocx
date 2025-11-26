export interface OtherExpenseDto {
    id: null;
    customerSiteId: string | null;
    siteNumber: string | null;
    name: string | null;
    billingPeriod: string | null;
    budgetData?: OtherExpenseDetailDto[];
    forecastData?: OtherExpenseDetailDto[];
    actualData?: OtherExpenseDetailDto[];
}

export interface OtherExpenseDetailDto {
    id: string | null;
    monthYear?: string;
    employeeRelations: number;
    fuelVehicles: number;
    lossAndDamageClaims: number;
    officeSupplies: number;
    outsideServices: number;
    rentsParking: number;
    repairsAndMaintenance: number;
    repairsAndMaintenanceVehicle: number;
    signage: number;
    suppliesAndEquipment: number;
    ticketsAndPrintedMaterial: number;
    uniforms: number;
    miscOtherExpenses: number;
    totalOtherExpenses: number;
}