import { getConfig } from "@/config/env.config";
import { BaseTest } from "@/core/base.test";
import { LoadPnLMultipleSites } from "./steps/loadPnLMultipleSites";

const config = getConfig();

export class PnLViewMultiSitesYearStress extends BaseTest {
  constructor() {
    super(config);
  }

  execute(): void {
    const startTime = Date.now();
    this.logTestStart("P&L View with Multiple Sites (Year)");

    try {
      const client = this.getAuthenticatedClient();
      this.executeStep("Load P&L Multiple Sites (All Years)", () => new LoadPnLMultipleSites().execute(client));
      this.logTestEnd("P&L View with Multiple Sites (Year)", Date.now() - startTime);
    } catch (error) {
      this.context.logger.error("P&L view with multiple sites (year) failed", {
        error: (error as Error).message,
      });
      throw error;
    }
  }

  private executeStep(stepName: string, stepFunction: () => void): void {
    this.logStep(`Starting: ${stepName}`);
    stepFunction();
    this.logStep(`Completed: ${stepName}`);
    this.randomSleep(0.5, 1.5);
  }
}

// Export function for K6 compatibility
export function pnlViewMultiSitesYearStress(): void {
  const test = new PnLViewMultiSitesYearStress();
  test.execute();
}