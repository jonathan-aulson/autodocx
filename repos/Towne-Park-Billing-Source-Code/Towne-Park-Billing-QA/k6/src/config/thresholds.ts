import { ScenarioType, TestType } from "@/types";
import { ThresholdConfig } from "@/types/config.types";

export const globalThresholds: ThresholdConfig = {
  http_req_duration: ["p(95)<2000", "p99<5000"],
  http_req_failed: ["rate<0.05"],
  checks: ["rate>0.95"],
};

export const stressThresholds: ThresholdConfig = {
  ...globalThresholds,
  http_req_duration: ["p(95)<3000", "p99<8000"],
  http_req_failed: ["rate<0.1"],
};

export const spikeThresholds: ThresholdConfig = {
  ...globalThresholds,
  http_req_duration: ["p(95)<5000", "p99<15000"],
  http_req_failed: ["rate<0.15"],
};

export const soakThresholds: ThresholdConfig = {
  ...globalThresholds,
  http_req_duration: ["p(95)<2500", "p99<6000"],
  http_req_failed: ["rate<0.08"],
};

export const scenarioThresholds: Record<TestType, Partial<ThresholdConfig>> = {
  "powerbill-statement": {
    http_req_duration: ["p(95)<3000"],
    checks: ["rate>0.9"],
  },
  "forecast-full-setup": {
    http_req_duration: ["p(95)<2000"],
    checks: ["rate>0.95"],
  },
  "forecast-mass-edit": {
    http_req_duration: ["p(95)<4000"],
    checks: ["rate>0.9"],
  },
  "forecast-tabs-edit": {
    http_req_duration: ["p(95)<2500"],
    checks: ["rate>0.92"],
  },
  "pnl-reporting": {
    http_req_duration: ["p(95)<4000"],
    checks: ["rate>0.9"],
  },
  "forecast-tab-switching": {
    http_req_duration: ["p(95)<6000"],
    checks: ["rate>0.85"],
  },
  "customer-navigation": {
    http_req_duration: ["p(95)<3000"],
    checks: ["rate>0.9"],
  },
  "mass-invoice-email": {
    http_req_duration: ["p(95)<8000"],
    checks: ["rate>0.8"],
  },
  "statement-generation": {
    http_req_duration: ["p(95)<10000"],
    checks: ["rate>0.85"],
  },
  "forecast-extended-edit": {
    http_req_duration: ["p(95)<3500"],
    checks: ["rate>0.88"],
  },
  "full-system-load": {
    http_req_duration: ["p(95)<5000"],
    checks: ["rate>0.85"],
  },
  "site-filtering": {
    http_req_duration: ["p(95)<2000"],
    checks: ["rate>0.95"],
  },
  "repeated-save-operations": {
    http_req_duration: ["p(95)<3000"],
    checks: ["rate>0.9"],
  },
};

export function getThresholds(
  scenario: ScenarioType,
  testType?: TestType
): ThresholdConfig {
  let baseThresholds: ThresholdConfig;

  switch (scenario) {
    case "stress":
      baseThresholds = stressThresholds;
      break;
    case "spike":
      baseThresholds = spikeThresholds;
      break;
    case "soak":
      baseThresholds = soakThresholds;
      break;
    default:
      baseThresholds = globalThresholds;
  }

  if (testType && scenarioThresholds[testType]) {
    // Merge and filter out undefined values to satisfy ThresholdConfig type
    const merged = { ...baseThresholds, ...scenarioThresholds[testType] };
    Object.keys(merged).forEach((key) => {
      if (merged[key] === undefined) {
        delete merged[key];
      }
    });
    return merged as ThresholdConfig;
  }

  return baseThresholds;
}
