export interface PayrollDto {
    id?: string;
    siteNumber?: string;
    customerSiteId: string;
    name?: string;
    billingPeriod?: string;
    payrollForecastMode?: string;
    forecastPayroll?: JobGroupForecastDto[];
    budgetPayroll?: JobGroupBudgetDto[];
    actualPayroll?: JobGroupActualDto[];
    scheduledPayroll?: JobGroupScheduledDto[];
}

export interface PayrollDetailDto {
    id?: string;
    jobCodeId?: string;
    date?: string;
    displayName?: string;
    jobCode?: string;
    regularHours: number;
}

export interface JobGroupForecastDto {
    id?: string;
    jobGroupId: string;
    jobGroupName?: string;
    forecastHours: number;
    date?: string;
    jobCodes?: JobCodeForecastDto[];
    forecastPayrollCost?: number;
    forecastPayrollRevenue?: number;
}

export interface JobGroupBudgetDto {
    id?: string;
    jobGroupId: string;
    jobGroupName?: string;
    budgetHours: number;
    date?: string;
    jobCodes?: JobCodeBudgetDto[];
    budgetPayrollCost?: number;
    budgetPayrollRevenue?: number;
}

export interface JobGroupActualDto {
    id?: string;
    jobGroupId: string;
    jobGroupName?: string;
    actualHours: number;
    date?: string;
    jobCodes?: JobCodeActualDto[];
    actualPayrollCost?: number;
    actualPayrollRevenue?: number;
}

export interface JobGroupScheduledDto {
    id?: string;
    jobGroupId: string;
    jobGroupName?: string;
    scheduledHours: number;
    date?: string;
    jobCodes?: JobCodeScheduledDto[];
    scheduledPayrollCost?: number;
    scheduledPayrollRevenue?: number;
}

export interface JobCodeForecastDto {
    id?: string;
    jobCodeId: string;
    jobCode?: string;
    displayName?: string;
    forecastHours: number;
    date?: string;
    forecastPayrollCost?: number;
    forecastPayrollRevenue?: number;
}

export interface JobCodeBudgetDto {
    id?: string;
    jobCodeId: string;
    jobCode?: string;
    displayName?: string;
    budgetHours: number;
    date?: string;
    budgetPayrollCost?: number;
    budgetPayrollRevenue?: number;
}

export interface JobCodeActualDto {
    id?: string;
    jobCodeId: string;
    jobCode?: string;
    displayName?: string;
    actualHours: number;
    date?: string;
    actualPayrollCost?: number;
    actualPayrollRevenue?: number;
}

export interface JobCodeScheduledDto {
    id?: string;
    jobCodeId: string;
    jobCode?: string;
    displayName?: string;
    scheduledHours: number;
    date?: string;
    scheduledPayrollCost?: number;
    scheduledPayrollRevenue?: number;
}

