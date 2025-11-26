import { getConfig } from "@/config/env.config";
import { getThresholds } from "@/config/thresholds";
import { customerSitesLoadStress } from "@/scenarios/stress/scenario-1-customer-sites-load-stress";
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
    customer_sites_load_time: ["p(95)<3000"],
    step_success_rate: ["rate>0.95"],
    customer_sites_load_success: ["rate>0.98"],
  },
};

// --- K6 setup: runs once per test (before VUs start) ---
export function setup(): void {
  console.log("🏗️ Customer Sites Load Stress Test Starting");
  console.log(`📊 Configuration:`);
  console.log(`   Environment:   ${config.environment}`);
  console.log(`   Base URL:      ${config.baseUrl}`);
  console.log(`   Virtual Users: ${config.maxUsers}`);
  console.log(`   Duration:      ${config.duration}`);
  console.log(`   Load Level:    ${config.loadLevel}`);
}

// --- Main test scenario: runs for each VU iteration ---
export default function (): void {
  customerSitesLoadStress();
}

// --- K6 teardown: runs once after all VUs finish ---
export function teardown(): void {
  console.log("✅ Customer Sites Load Stress Test Completed");
}

// --- K6 handleSummary: control output/summary report ---
export function handleSummary(data: any): Record<string, string> {
  return {
    stdout: JSON.stringify(data, null, 2),
  };
}
