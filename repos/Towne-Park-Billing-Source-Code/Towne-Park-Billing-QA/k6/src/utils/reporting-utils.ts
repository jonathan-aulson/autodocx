// Global variables to store the loaded reporters
let htmlReport: any = null;
let textSummary: any = null;
let reportersLoaded = false;

/**
 * Loads the external reporting modules once
 * This should be called at the beginning of your test
 */
export function loadReporters(): void {
  if (reportersLoaded) {
    return; // Already loaded, skip
  }

  try {
    // These will be resolved at runtime by K6, not during TypeScript compilation
    const htmlReporter = require("https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js");
    const textSummaryModule = require("https://jslib.k6.io/k6-summary/0.0.1/index.js");

    htmlReport = htmlReporter.htmlReport;
    textSummary = textSummaryModule.textSummary;
    reportersLoaded = true;

    console.log("✅ External reporting modules loaded successfully");
  } catch (error) {
    // Fallback if modules can't be loaded
    console.warn("⚠️ Could not load external reporting modules:", error);
    htmlReport = null;
    textSummary = null;
    reportersLoaded = true; // Mark as attempted to avoid retrying
  }
}

/**
 * Simple K6 handleSummary function with basic HTML and text output
 * @param data K6 summary data
 * @returns Object with basic summary outputs
 */
export function createSimpleHandleSummary() {
  return function handleSummary(data: any) {
    return {
      "summary.html": htmlReport ? htmlReport(data) : "",
      stdout: textSummary
        ? textSummary(data, { indent: " ", enableColors: true })
        : "",
    };
  };
}

/**
 * Comprehensive K6 handleSummary function with detailed reporting
 * @param config Test configuration object
 * @param customTitle Optional custom title for the report
 * @param customDescription Optional custom description for the report
 * @returns Function that handles K6 summary with multiple output formats
 */
export function createComprehensiveHandleSummary(
  config: any,
  customTitle?: string,
  customDescription?: string
) {
  return function handleSummary(data: any): Record<string, string> {
    const timestamp = new Date().toISOString().replace(/[:.]/g, "-");
    const testName = config.testType || "performance-test";
    const scenario = config.scenario || "stress";

    // Default title and description
    const title = customTitle || `${testName} Performance Test`;
    const description = customDescription || generateDefaultDescription(config);

    return {
      // HTML Report with custom filename
      [`reports/${scenario}-${testName}-${timestamp}.html`]: htmlReport
        ? htmlReport(data, {
            title: title,
            description: description,
          })
        : "",

      // JSON Report for further processing
      [`reports/${scenario}-${testName}-${timestamp}.json`]: JSON.stringify(
        data,
        null,
        2
      ),

      // Console output with colors
      stdout: textSummary
        ? textSummary(data, {
            indent: " ",
            enableColors: true,
          })
        : "",

      // Summary file for CI/CD integration
      [`reports/${scenario}-${testName}-summary.txt`]: generateCustomSummary(
        data,
        config
      ),
    };
  };
}

/**
 * Generate default HTML description for the test report
 */
function generateDefaultDescription(config: any): string {
  return `
    <h3>Test Configuration</h3>
    <ul>
      <li><strong>Environment:</strong> ${config.environment || "Unknown"}</li>
      <li><strong>Base URL:</strong> ${config.baseUrl || "Unknown"}</li>
      <li><strong>Load Level:</strong> ${config.loadLevel || "Unknown"}</li>
      <li><strong>Virtual Users:</strong> ${config.maxUsers || "Unknown"}</li>
      <li><strong>Duration:</strong> ${config.duration || "Unknown"}</li>
      <li><strong>Test Type:</strong> ${config.testType || "Unknown"}</li>
    </ul>
    <h3>Test Scope</h3>
    <p>This performance test validates system behavior under load including:</p>
    <ul>
      <li>Response time performance</li>
      <li>System reliability and stability</li>
      <li>Error rate monitoring</li>
      <li>Resource utilization</li>
    </ul>
  `;
}

/**
 * Custom summary function for additional insights
 */
function generateCustomSummary(data: any, config: any): string {
  const metrics = data?.metrics || {};
  const totalReq = metrics.http_reqs?.count ?? 0;
  const reqRate = metrics.http_reqs?.rate ?? 0;
  const avg = metrics.http_req_duration?.avg ?? 0;
  const p95 = metrics.http_req_duration?.p95 ?? 0;
  const p99 = metrics.http_req_duration?.p99 ?? 0;
  const errRate = (metrics.http_req_failed?.rate ?? 0) * 100;

  return `
K6 Performance Test Summary
===========================
Test: ${config?.testType ?? "Unknown"}
Environment: ${config?.environment ?? "Unknown"}
Timestamp: ${new Date().toISOString()}

Performance Metrics:
------------------
Total Requests: ${totalReq}
Request Rate: ${reqRate.toFixed(2)} req/s
Average Response Time: ${avg.toFixed(2)}ms
95th Percentile: ${p95.toFixed(2)}ms
99th Percentile: ${p99.toFixed(2)}ms
Error Rate: ${errRate.toFixed(2)}%

Custom Metrics:
--------------
${generateCustomMetricsSummary(metrics)}

Test Status:
-----------
${getTestStatus(metrics)}

Thresholds:
----------
${getThresholdStatus(data?.thresholds)}
`;
}

/**
 * Generate summary for custom metrics (adapts to different test types)
 */
function generateCustomMetricsSummary(metrics: any): string {
  let customSummary = "";

  const pats = [
    {
      pattern: /(.+)_load_time/,
      label: "Load Time (p95)",
      unit: "ms",
      stat: "p95",
    },
    {
      pattern: /(.+)_load_success/,
      label: "Load Success Rate",
      unit: "%",
      stat: "rate",
    },
    {
      pattern: /(.+)_success_rate/,
      label: "Success Rate",
      unit: "%",
      stat: "rate",
    },
    {
      pattern: /(.+)_duration/,
      label: "Duration (avg)",
      unit: "ms",
      stat: "avg",
    },
  ];

  for (const metricName of Object.keys(metrics)) {
    for (const { pattern, label, unit, stat } of pats) {
      const match = metricName.match(pattern);
      if (match) {
        const feature = match[1]
          .replace(/_/g, " ")
          .replace(/\b\w/g, (l) => l.toUpperCase());
        const v = metrics[metricName]?.[stat];
        if (v !== undefined) {
          const formatted =
            unit === "%"
              ? `${(v * 100).toFixed(2)}${unit}`
              : `${v.toFixed(2)}${unit}`;
          customSummary += `${feature} ${label}: ${formatted}\n`;
        }
      }
    }
  }

  return customSummary || "No custom metrics found";
}

/**
 * Determine overall test status based on metrics
 */
function getTestStatus(metrics: any): string {
  const errorRate = metrics?.http_req_failed?.rate ?? 0;
  const stepSuccessRate = metrics?.step_success_rate?.rate ?? 1;

  if (errorRate > 0.1) return "❌ FAILED - High error rate detected";
  if (stepSuccessRate < 0.95) return "⚠️  WARNING - Some steps failed";
  return "✅ PASSED - All criteria met";
}

/**
 * Generate threshold status summary
 */
function getThresholdStatus(thresholds: any): string {
  if (!thresholds) return "No thresholds configured";
  let status = "";
  for (const [metric, t] of Object.entries(thresholds as Record<string, any>)) {
    status += `${t.ok ? "✅" : "❌"} ${metric}: ${
      t.ok ? "PASSED" : "FAILED"
    }\n`;
  }
  return status;
}

/**
 * Get the loaded reporters (useful for custom summary handling)
 */
export function getReporters() {
  return {
    htmlReport,
    textSummary,
    isLoaded: true,
  };
}
