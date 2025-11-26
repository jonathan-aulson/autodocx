export interface ReconciliationItem {
  jobCode: string;
  scheduled: number;
  forecast: number;
  budget: number;
  actual: number | null;
  variance: number;
  variancePercentage: number;
}