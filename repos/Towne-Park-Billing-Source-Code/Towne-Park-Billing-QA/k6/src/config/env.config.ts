import { LoadLevel, ScenarioType, TestType } from "@/types";
import { TestConfig } from "@/types/config.types";

export class ConfigManager {
  private static instance: ConfigManager;
  private config: TestConfig | null = null;

  private constructor() {}

  static getInstance(): ConfigManager {
    if (!ConfigManager.instance) {
      ConfigManager.instance = new ConfigManager();
    }
    return ConfigManager.instance;
  }

  getConfig(): TestConfig {
    if (!this.config) {
      this.config = this.parseEnvironmentConfig();
      this.validateConfig(this.config);
    }
    return this.config;
  }

  private parseEnvironmentConfig(): TestConfig {
    return {
      baseUrl: this.getEnvValue("BASE_URL", "https://app.example.com"),
      scenario: this.getEnvValue("SCENARIO", "stress") as ScenarioType,
      testType: this.getEnvValue(
        "TEST_TYPE",
        "powerbill-statement"
      ) as TestType,
      environment: this.getEnvValue("ENVIRONMENT", "dev") as any,
      loadLevel: this.getEnvValue("LOAD_LEVEL", "low") as LoadLevel,
      duration: this.getEnvValue("DURATION", "10m"),
      maxUsers: parseInt(this.getEnvValue("MAX_USERS", "100")),
      rampUpTime: this.getEnvValue("RAMP_UP_TIME", "2m"),
      apiKey: this.getEnvValue("API_KEY", ""),
      enableDetailedLogs: this.getEnvValue("DETAILED_LOGS", "false") === "true",
      reportFormat: this.getEnvValue("REPORT_FORMAT", "html") as any,
      timeout: this.getEnvValue("TIMEOUT", "30s"),
      gracefulStop: this.getEnvValue("GRACEFUL_STOP", "30s"),
      rate: {
        customerSites: parseInt(this.getEnvValue("RATE_CUSTOMER_SITES", "1")),
        loadAllTabs: parseInt(this.getEnvValue("RATE_LOAD_ALL_TABS", "1")),
        changeYMTP: parseInt(this.getEnvValue("RATE_CHANGE_YMTP", "1")),
        statistics: parseInt(this.getEnvValue("RATE_STATISTICS", "1")),
        payroll: parseInt(this.getEnvValue("RATE_PAYROLL", "1")),
        parking: parseInt(this.getEnvValue("RATE_PARKING", "1")),
        otherExpense: parseInt(this.getEnvValue("RATE_OTHER_EXPENSE", "1")),
        otherRevenue: parseInt(this.getEnvValue("RATE_OTHER_REVENUE", "1")),
        massEdit: parseInt(this.getEnvValue("RATE_MASS_EDIT", "1")),
        pnlYear: parseInt(this.getEnvValue("RATE_PNL_YEAR", "1")),
        pnlMulti: parseInt(this.getEnvValue("RATE_PNL_MULTI", "1")),
      },
    };
  }

  private getEnvValue(key: string, defaultValue: string): string {
    return (globalThis as any).__ENV?.[key] || defaultValue;
  }

  private validateConfig(config: TestConfig): void {
    const requiredFields: (keyof TestConfig)[] = [
      "baseUrl",
      "scenario",
      "testType",
    ];
    const missingFields = requiredFields.filter((field) => !config[field]);

    if (missingFields.length > 0) {
      throw new Error(
        `Missing required configuration fields: ${missingFields.join(", ")}`
      );
    }

    // Validate enum values
    const validScenarios: ScenarioType[] = [
      "stress",
      "spike",
      "soak",
      "forecast-visble",
      "customer-load",
    ];
    if (!validScenarios.includes(config.scenario)) {
      throw new Error(
        `Invalid scenario: ${
          config.scenario
        }. Must be one of: ${validScenarios.join(", ")}`
      );
    }

    const validLoadLevels: LoadLevel[] = [
      "very-low",
      "low",
      "medium",
      "high",
      "peak",
    ];
    if (!validLoadLevels.includes(config.loadLevel)) {
      throw new Error(
        `Invalid load level: ${
          config.loadLevel
        }. Must be one of: ${validLoadLevels.join(", ")}`
      );
    }

    // Validate numeric values
    if (config.maxUsers <= 0) {
      throw new Error("maxUsers must be a positive number");
    }
  }

  updateConfig(updates: Partial<TestConfig>): void {
    if (this.config) {
      this.config = { ...this.config, ...updates };
      this.validateConfig(this.config);
    }
  }

  resetConfig(): void {
    this.config = null;
  }
}

export function getConfig(): TestConfig {
  return ConfigManager.getInstance().getConfig();
}

export function validateConfig(config: TestConfig): void {
  ConfigManager.getInstance()["validateConfig"](config);
}
