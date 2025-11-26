export interface ApiResponse<T = any> {
  success: boolean;
  data: T;
  message?: string;
  errors?: string[];
  timestamp: string;
}

export interface ForecastData {
  id: string;
  siteId: number;
  month: number;
  year: number;
  statistics: StatisticsData[];
  parkingRates: ParkingRateData[];
  payroll: PayrollData[];
  otherRevenue: OtherRevenueData[];
  lastModified: string;
  modifiedBy: string;
}

export interface StatisticsData {
  date: string;
  value: number;
  type: 'daily' | 'weekly' | 'monthly';
  category: string;
  lastModified?: string;
  modifiedBy?: string;
}

export interface ParkingRateData {
  rateType: string;
  amount: number;
  effectiveDate: string;
  endDate?: string;
}

export interface PayrollData {
  employeeId: string;
  position: string;
  hours: number;
  rate: number;
  date: string;
}

export interface OtherRevenueData {
  source: string;
  amount: number;
  date: string;
  category: string;
}

export interface StatementGenerationRequest {
  siteId: number;
  invoiceType: 'monthly' | 'quarterly' | 'annual';
  month: number;
  year: number;
  includeDetails?: boolean;
}

export interface StatementGenerationResponse {
  jobId: string;
  status: 'queued' | 'processing' | 'completed' | 'failed';
  estimatedCompletion?: string;
  downloadUrl?: string;
}

export interface PnLReportRequest {
  siteIds: number[];
  startDate: string;
  endDate: string;
  viewType: 'summary' | 'detailed';
  filters?: {
    view?: 'actual' | 'trend' | 'variance' | 'budget';
    categories?: string[];
  };
}

export interface PnLReportResponse {
  reportId: string;
  sites: PnLSiteData[];
  totals: PnLTotals;
  generatedAt: string;
}

export interface PnLSiteData {
  siteId: number;
  siteName: string;
  revenue: number;
  expenses: number;
  netIncome: number;
  margin: number;
}

export interface PnLTotals {
  totalRevenue: number;
  totalExpenses: number;
  totalNetIncome: number;
  averageMargin: number;
}