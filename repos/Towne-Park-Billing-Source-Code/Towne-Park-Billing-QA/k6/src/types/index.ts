import { HttpClient } from "@/core/http.client";
import { Logger } from "@/utils/logger";
import { TestConfig } from "./config.types";

// Global type definitions
export interface K6Response {
  status: number;
  body: string;
  headers: Record<string, string>;
  timings: {
    duration: number;
    blocked: number;
    connecting: number;
    tls_handshaking: number;
    sending: number;
    waiting: number;
    receiving: number;
  };
}

export interface K6Check {
  [key: string]: (response: K6Response) => boolean;
}

export interface K6Options {
  stages?: Stage[];
  thresholds?: Record<string, string[]>;
  setupTimeout?: string;
  teardownTimeout?: string;
  executor?: string;
  gracefulRampDown?: string;
}

export interface Stage {
  duration: string;
  target: number;
}

export interface MetricOptions {
  tags?: { [name: string]: string };
}

export interface AuthData {
  token: string;
  refreshToken?: string;
  expiresIn?: number;
  user: UserInfo;
}

export interface UserInfo {
  id: string;
  username: string;
  role: UserRole;
  sites: number[] | 'all';
  permissions: string[];
}

export type UserRole = 'account-manager' | 'district-manager' | 'vp' | 'c-level' | 'admin';

export type TestType =
  | 'powerbill-statement'
  | 'forecast-full-setup'
  | 'forecast-mass-edit'
  | 'forecast-tabs-edit'
  | 'pnl-reporting'
  | 'forecast-tab-switching'
  | 'customer-navigation'
  | 'mass-invoice-email'
  | 'statement-generation'
  | 'forecast-extended-edit'
  | 'full-system-load'
  | 'site-filtering'
  | 'repeated-save-operations';

export type ScenarioType = 'stress' | 'spike' | 'soak' | 'forecast-visble' | 'customer-load';

export type LoadLevel = 'very-low' | 'low' | 'medium' | 'high' | 'peak';

export interface TestContext {
  config: TestConfig;
  httpClient: HttpClient;
  logger: Logger;
}