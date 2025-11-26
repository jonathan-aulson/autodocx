import { DataEntity } from "./DataEntity";

export enum StatementStatus {
  GENERATING = "Generating",
  NEEDS_REVIEW = "Needs Review",
  APPROVED = "Approved",
  SENT = "Sent",
  AR_REVIEW = "AR Review",
  APPROVAL_TEAM = "Approval Team",
  READY_TO_SEND = "Ready To Send",
  FAILED = "Failed",
}

export interface Statement extends DataEntity {
  id: string;
  customerSiteId: string;
  createdMonth: string;
  servicePeriod: string;
  servicePeriodStart: string;
  totalAmount: number;
  status: StatementStatus;
  invoices: InvoiceSummary[];
  siteNumber: string;
  siteName: string;
  amNotes: string;
  forecastDeviationPercent?: number;
  forecastDeviationDollar?: number;
  forecastData: string;
}

export interface ForecastData {
  forecastedRevenue: number;
  postedRevenue: number;
  invoicedRevenue: number;
  totalActualRevenue: number;
  forecastDeviationPercentage: number;
  forecastDeviationAmount: number;
  forecastLastUpdated: string;
}

export interface InvoiceSummary extends DataEntity {
  id: string;
  invoiceNumber: string;
  amount: number;
}