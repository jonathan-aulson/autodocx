import { PayrollDto } from '@/lib/models/Payroll';

export interface PayrollData {
  date: Date;
  jobs?: Record<string, {
    hours?: number;
    scheduled?: number;
    budget?: number;
    actual?: number | null;
    forecast?: number;
  }>;
}

export type TimeHorizon = "daily" | "weekly" | "monthly" | "quarterly";
export type ComparisonType = "actual-vs-budget" | "forecast-vs-budget" | "actual-vs-forecast";
export type JobGroup = string; // Dynamic from API, not hardcoded

export interface JobMapping {
  jobCode: string;
  displayName: string;
  hourlyRate?: number;
}

export interface ReconciliationDashboardProps {
  data: PayrollData[];
  payrollDto?: PayrollDto | null; // Optional for live data scenarios
  availableJobGroups?: string[]; // Direct job groups for live data
  billingPeriod: string; // "2024-01" - constrains date range to single month
  timeHorizon: TimeHorizon;
  showComparison: boolean;
  comparisonType: ComparisonType;
}