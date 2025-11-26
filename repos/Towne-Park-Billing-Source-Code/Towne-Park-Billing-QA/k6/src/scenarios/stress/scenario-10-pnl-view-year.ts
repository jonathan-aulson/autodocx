import { getConfig } from "@/config/env.config";
import { BaseTest } from "@/core/base.test";
import { LoadPnL } from "./steps/loadPnL";

import siteDataJson from "../../data/uat/sites.json";

const config = getConfig();

export class PnLViewYearStress extends BaseTest {
  constructor() {
    super(config);
  }

  async execute(): Promise<void> {
    const startTime = Date.now();
    this.logTestStart("P&L View (Year)");

    try {
      const client = this.getAuthenticatedClient();
      // Get all siteIds from the siteDataJson (using all siteNo as in scenario 11)
      const allSiteIds = siteDataJson.siteData.map(site => site.siteId);
      this.executeStep("Load P&L (All Years)", () =>
        new LoadPnL().execute(client, allSiteIds)
      );
      this.logTestEnd("P&L View (Year)", Date.now() - startTime);
    } catch (error) {
      this.context.logger.error("P&L view (year) failed", {
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
export function pnlViewYearStress(): void {
  const test = new PnLViewYearStress();
  test.execute();
}
