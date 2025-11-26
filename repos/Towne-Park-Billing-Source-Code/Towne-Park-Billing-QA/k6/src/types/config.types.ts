import { LoadLevel, ScenarioType, TestType } from "./index";

export interface TestConfig {
  baseUrl: string;
  scenario: ScenarioType;
  testType: TestType;
  environment: "dev" | "qa" | "stage" | "prod";
  loadLevel: LoadLevel;
  duration: string;
  maxUsers: number;
  rampUpTime: string;
  apiKey: string;
  enableDetailedLogs: boolean;
  reportFormat: "html" | "json" | "junit";
  timeout: string;
  gracefulStop: string;
  rate?: {
    customerSites?: number;
    loadAllTabs?: number;
    changeYMTP?: number;
    statistics?: number;
    payroll?: number;
    parking?: number;
    otherExpense?: number;
    otherRevenue?: number;
    massEdit?: number;
    pnlYear?: number;
    pnlMulti?: number;
  };
}

export interface EnvironmentConfig {
  [key: string]: string | number | boolean;
}

export type ThresholdConfig = Record<string, string[]>;

export type StageConfig = {
  [key in LoadLevel]: Stage[];
};

interface Stage {
  duration: string;
  target: number;
}
