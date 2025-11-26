import { getConfig } from "@/config/env.config";
import { getThresholds } from "@/config/thresholds";
import { parkingRatesEditSaveStress } from "@/scenarios/stress/scenario-6-parkingrates-edit-save"; // Adjust path as needed
import { Options } from "k6/options";

// --- Config ---
const config = getConfig();

// --- K6 Options (VU, duration, thresholds) ---
export const options: Options = {
  vus: config.maxUsers || 10,
  duration: config.duration || "5m",
  thresholds: {
    ...getThresholds("stress", "forecast-full-setup"),
    edit_parking_rates_get_time: ["p(95)<3000"],
    edit_parking_rates_patch_time: ["p(95)<3000"],
    step_success_rate: ["rate>0.95"],
    edit_parking_rates_get_success: ["rate>0.98"],
    edit_parking_rates_patch_success: ["rate>0.98"],
  },
};

// --- K6 setup: runs once per test (before VUs start) ---
export function setup(): void {
  console.log("🏗️ Parking Rates Edit Save Stress Test Starting");
  console.log(`📊 Configuration:`);
  console.log(`   Environment:   ${config.environment}`);
  console.log(`   Base URL:      ${config.baseUrl}`);
  console.log(`   Virtual Users: ${config.maxUsers}`);
  console.log(`   Duration:      ${config.duration}`);
  console.log(`   Load Level:    ${config.loadLevel}`);
}

// --- Main test scenario: runs for each VU iteration ---
export default function (): void {
  parkingRatesEditSaveStress();
}

// --- K6 teardown: runs once after all VUs finish ---
export function teardown(): void {
  console.log("✅ Parking Rates Edit Save Stress Test Completed");
}

// --- K6 handleSummary: control output/summary report ---
export function handleSummary(data: any): Record<string, string> {
  return {
    stdout: JSON.stringify(data, null, 2),
  };
}
