import { getConfig } from "@/config/env.config";
import { getThresholds } from "@/config/thresholds";

import { customerSitesLoadStress } from "@/scenarios/stress/scenario-1-customer-sites-load-stress";
import { pnlViewYearStress } from "@/scenarios/stress/scenario-10-pnl-view-year";
import { pnlViewMultiSitesYearStress } from "@/scenarios/stress/scenario-11-pnl-view-multisites-year";
import { loadAllTabsData } from "@/scenarios/stress/scenario-2-load-all-tabs-data";
import { changeYearMonthAndTimePeriod } from "@/scenarios/stress/scenario-3-change-year-month-timeperiod";
import { statisticsEditSaveStress } from "@/scenarios/stress/scenario-4-statistics-edit-save";
import { payrollEditSaveStress } from "@/scenarios/stress/scenario-5-payroll-edit-save";
import { parkingRatesEditSaveStress } from "@/scenarios/stress/scenario-6-parkingrates-edit-save";
import { otherExpenseEditSaveStress } from "@/scenarios/stress/scenario-7-otherexpense-edit-save";
import { otherRevenueEditSaveStress } from "@/scenarios/stress/scenario-8-otherrevenue-edit-save";
import { massEditSaveStress } from "@/scenarios/stress/scenario-9-mass-edit-save";

import {
  createComprehensiveHandleSummary,
  loadReporters,
} from "@/utils/reporting-utils";
import { Options } from "k6/options";

const config = getConfig();
loadReporters();

/** one exec per phase for k6 multi-scenarios */
export function exec_customer_sites_load() {
  customerSitesLoadStress();
}
export function exec_load_all_tabs_data() {
  loadAllTabsData();
}
export function exec_change_year_month_timeperiod() {
  changeYearMonthAndTimePeriod();
}
export function exec_statistics_edit_save() {
  statisticsEditSaveStress();
}
export function exec_payroll_edit_save() {
  payrollEditSaveStress();
}
export function exec_parking_rates_edit_save() {
  parkingRatesEditSaveStress();
}
export function exec_other_expense_edit_save() {
  otherExpenseEditSaveStress();
}
export function exec_other_revenue_edit_save() {
  otherRevenueEditSaveStress();
}
export function exec_mass_edit_save_all_tabs() {
  massEditSaveStress();
}
export function exec_pnl_view_year() {
  pnlViewYearStress();
}
export function exec_pnl_view_multisites_year() {
  pnlViewMultiSitesYearStress();
}

export const options: Options = {
  scenarios: {
    customer_sites_load: {
      exec: "exec_customer_sites_load",
      executor: "constant-arrival-rate",
      rate: 5, // or wire to config.rate.customerSites
      timeUnit: "1s",
      duration: config.duration ?? "30m",
      preAllocatedVUs: config.maxUsers ?? 10,
      tags: { phase: "customer_sites_load" },
    },
    load_all_tabs_data: {
      exec: "exec_load_all_tabs_data",
      executor: "constant-arrival-rate",
      rate: 3,
      timeUnit: "1s",
      duration: config.duration ?? "30m",
      preAllocatedVUs: config.maxUsers ?? 10,
      startTime: "0s",
      tags: { phase: "load_all_tabs_data" },
    },
    change_year_month_timeperiod: {
      exec: "exec_change_year_month_timeperiod",
      executor: "constant-arrival-rate",
      rate: 3,
      timeUnit: "1s",
      duration: config.duration ?? "30m",
      preAllocatedVUs: config.maxUsers ?? 10,
      startTime: "0s",
      tags: { phase: "change_year_month_timeperiod" },
    },
    statistics_edit_save: {
      exec: "exec_statistics_edit_save",
      executor: "constant-arrival-rate",
      rate: 2,
      timeUnit: "1s",
      duration: config.duration ?? "30m",
      preAllocatedVUs: config.maxUsers ?? 10,
      startTime: "0s",
      tags: { phase: "statistics_edit_save" },
    },
    payroll_edit_save: {
      exec: "exec_payroll_edit_save",
      executor: "constant-arrival-rate",
      rate: 2,
      timeUnit: "1s",
      duration: config.duration ?? "30m",
      preAllocatedVUs: config.maxUsers ?? 10,
      startTime: "0s",
      tags: { phase: "payroll_edit_save" },
    },
    parking_rates_edit_save: {
      exec: "exec_parking_rates_edit_save",
      executor: "constant-arrival-rate",
      rate: 2,
      timeUnit: "1s",
      duration: config.duration ?? "30m",
      preAllocatedVUs: config.maxUsers ?? 10,
      startTime: "0s",
      tags: { phase: "parking_rates_edit_save" },
    },
    other_expense_edit_save: {
      exec: "exec_other_expense_edit_save",
      executor: "constant-arrival-rate",
      rate: 2,
      timeUnit: "1s",
      duration: config.duration ?? "30m",
      preAllocatedVUs: config.maxUsers ?? 10,
      startTime: "0s",
      tags: { phase: "other_expense_edit_save" },
    },
    other_revenue_edit_save: {
      exec: "exec_other_revenue_edit_save",
      executor: "constant-arrival-rate",
      rate: 2,
      timeUnit: "1s",
      duration: config.duration ?? "30m",
      preAllocatedVUs: config.maxUsers ?? 10,
      startTime: "0s",
      tags: { phase: "other_revenue_edit_save" },
    },
    mass_edit_save_all_tabs: {
      exec: "exec_mass_edit_save_all_tabs",
      executor: "constant-arrival-rate",
      rate: 1,
      timeUnit: "1s",
      duration: config.duration ?? "30m",
      preAllocatedVUs: config.maxUsers ?? 10,
      startTime: "0s",
      tags: { phase: "mass_edit_save_all_tabs" },
    },
    pnl_view_year: {
      exec: "exec_pnl_view_year",
      executor: "constant-arrival-rate",
      rate: 2,
      timeUnit: "1s",
      duration: config.duration ?? "30m",
      preAllocatedVUs: config.maxUsers ?? 10,
      startTime: "0s",
      tags: { phase: "pnl_view_year" },
    },
    pnl_view_multisites_year: {
      exec: "exec_pnl_view_multisites_year",
      executor: "constant-arrival-rate",
      rate: 1,
      timeUnit: "1s",
      duration: config.duration ?? "30m",
      preAllocatedVUs: config.maxUsers ?? 10,
      startTime: "0s",
      tags: { phase: "pnl_view_multisites_year" },
    },
  },
  thresholds: {
    ...getThresholds("stress", "forecast-full-setup"),
    http_req_duration: ["p(95)<4000"],
    http_req_failed: ["rate<0.1"],
    // Optional, scoped:
    "http_req_duration{phase:statistics_edit_save}": ["p(95)<3000"],
    "http_req_duration{phase:load_all_tabs_data}": ["p(95)<3500"],
  },
};

/** setup logging (unchanged) */
export function setup(): void {
  console.log("🏗️ All Forecast Scenarios Parallel Stress Test Starting");
  console.log(`📊 Configuration:`);
  console.log(`   Environment: ${config.environment}`);
  console.log(`   Base URL: ${config.baseUrl}`);
  console.log(`   Virtual Users: ${config.maxUsers}`);
  console.log(`   Duration: ${config.duration}`);
  console.log(`   Load Level: ${config.loadLevel}`);
}

/** ✅ SUMMARY: restored */
export function handleSummary(data: any) {
  // This returns an object like { "summary.html": htmlString, "summary.json": jsonString, stdout: text }
  // Implementation lives in your reporting-utils.
  return createComprehensiveHandleSummary(
    config,
    "all-forecast-scenarios"
  )(data);
}
