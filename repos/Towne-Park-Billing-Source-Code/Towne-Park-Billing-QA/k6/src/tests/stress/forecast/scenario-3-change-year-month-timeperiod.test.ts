import { getConfig } from "@/config/env.config";
import { getThresholds } from "@/config/thresholds";
import { changeYearMonthAndTimePeriod } from "@/scenarios/stress/scenario-3-change-year-month-timeperiod"; // Adjust path as needed
import { Options } from "k6/options";

// --- Config ---
const config = getConfig();

// --- K6 Options (VU, duration, thresholds) ---
export const options: Options = {
  vus: config.maxUsers || 10,
  duration: config.duration || "5m",
  thresholds: {
    ...getThresholds("stress", "forecast-full-setup"),
    // Test-specific thresholds
    statistics_load_time: ["p(95)<3000"],
    parking_rates_load_time: ["p(95)<3000"],
    payroll_load_time: ["p(95)<3000"],
    contract_load_time: ["p(95)<3000"],
    job_codes_load_time: ["p(95)<3000"],
    other_revenue_load_time: ["p(95)<3000"],
    other_expense_load_time: ["p(95)<3000"],
    step_success_rate: ["rate>0.95"],
    statistics_load_success: ["rate>0.98"],
    parking_rates_load_success: ["rate>0.98"],
    payroll_load_success: ["rate>0.98"],
    contract_load_success: ["rate>0.98"],
    job_codes_load_success: ["rate>0.98"],
    other_revenue_load_success: ["rate>0.98"],
    other_expense_load_success: ["rate>0.98"],
  },
};

// --- K6 setup: runs once per test (before VUs start) ---
export function setup(): void {
  console.log("🏗️ Change Year/Month/TimePeriod Stress Test Starting");
  console.log(`📊 Configuration:`);
  console.log(`   Environment:   ${config.environment}`);
  console.log(`   Base URL:      ${config.baseUrl}`);
  console.log(`   Virtual Users: ${config.maxUsers}`);
  console.log(`   Duration:      ${config.duration}`);
  console.log(`   Load Level:    ${config.loadLevel}`);
}

// --- Main test scenario: runs for each VU iteration ---
export default function (): void {
  changeYearMonthAndTimePeriod();
}

// --- K6 teardown: runs once after all VUs finish ---
export function teardown(): void {
  console.log("✅ Change Year/Month/TimePeriod Stress Test Completed");
}

// --- K6 handleSummary: control output/summary report ---
export function handleSummary(data: any): Record<string, string> {
  return {
    stdout: JSON.stringify(data, null, 2),
  };
}
