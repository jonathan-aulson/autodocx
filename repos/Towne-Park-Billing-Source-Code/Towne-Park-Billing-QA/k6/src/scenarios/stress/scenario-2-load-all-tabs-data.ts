import { getConfig } from "@/config/env.config";
import { BaseTest } from "@/core/base.test";
import { LoadContract } from "./steps/loadContract";
import { LoadJobCodes } from "./steps/loadJobCodes";
import { LoadOtherExpense } from "./steps/loadOtherExpense";
import { LoadOtherRevenue } from "./steps/loadOtherRevenue";
import { LoadParkingRates } from "./steps/loadParkingRates";
import { LoadPayroll } from "./steps/loadPayroll";
import { LoadSites } from "./steps/loadSites";
import { LoadStatistics } from "./steps/loadStatistics";

import siteDataJson from "../../data/uat/sites.json";

const config = getConfig();

export class LoadAllTabsData extends BaseTest {
  constructor() {
    super(config);
  }

  async execute(): Promise<void> {
    const startTime = Date.now();
    this.logTestStart("Load All Tabs Data");

    try {
      const client = this.getAuthenticatedClient();

      // Loop over each siteId from JSON
      for (const site of siteDataJson.siteData) {
        const siteId = site.siteId;

        this.context.logger.info(`Processing Site ID: ${siteId}`);

        await this.executeStep("Load Sites", async () =>
          new LoadSites().execute(client)
        );
        await this.executeStep("Load Statistics", async () =>
          new LoadStatistics().execute(client, siteId)
        );
        await this.executeStep("Load Parking Rates", async () =>
          new LoadParkingRates().execute(client, siteId)
        );
        await this.executeStep("Load Payroll", async () =>
          new LoadPayroll().execute(client, siteId)
        );
        await this.executeStep("Load Other Revenue", async () =>
          new LoadOtherRevenue().execute(client, siteId)
        );
        await this.executeStep("Load Other Expense", async () =>
          new LoadOtherExpense().execute(client, siteId)
        );
        await this.executeStep("Load Job Codes", async () =>
          new LoadJobCodes().execute(client, siteId)
        );
        await this.executeStep("Load Contract", async () =>
          new LoadContract().execute(client, siteId)
        );

        // Optional: Add sleep between sites
        this.randomSleep(1, 3);
      }

      this.logTestEnd("Load All Tabs Data", Date.now() - startTime);
    } catch (error) {
      this.context.logger.error("Load All Tabs Data failed", {
        error: (error as Error).message,
      });
      throw error;
    }
  }

  // Make stepFunction async for await compatibility
  private async executeStep(
    stepName: string,
    stepFunction: () => Promise<void>
  ): Promise<void> {
    this.logStep(`Starting: ${stepName}`);
    await stepFunction();
    this.logStep(`Completed: ${stepName}`);
    this.randomSleep(0.5, 1.5);
  }
}

// Export function for K6 compatibility
export function loadAllTabsData(): void {
  const test = new LoadAllTabsData();
  test.execute();
}

// Optionally, for K6 default export
export default function (): void {
  const test = new LoadAllTabsData();
  test.execute();
}
