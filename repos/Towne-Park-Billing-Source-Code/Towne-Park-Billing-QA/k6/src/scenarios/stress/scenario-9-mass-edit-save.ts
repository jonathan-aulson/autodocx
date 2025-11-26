import { getConfig } from "@/config/env.config";
import { BaseTest } from "@/core/base.test";
import { allSiteIds, getSitePayload } from "@/utils/payload-loader";

import { EditOtherExpense } from "./steps/editOtherExpense";
import { EditOtherRevenue } from "./steps/editOtherRevenue";
import { EditParkingRates } from "./steps/editParkingRates";
import { EditPayroll } from "./steps/editPayroll";
import { EditStatistics } from "./steps/editStatistics";

const config = getConfig();

export class MassEditSaveStress extends BaseTest {
  constructor() {
    super(config);
  }

  async execute(): Promise<void> {
    const start = Date.now();
    this.logTestStart("Mass Edit and Save");

    try {
      const client = this.getAuthenticatedClient();

      // One site at a time; do all five edits per site.
      for (const siteId of allSiteIds) {
        this.context.logger.info(`Mass edit start for siteId=${siteId}`);

        // Collect payloads upfront to log once
        const statisticsBody = getSitePayload(siteId, "statistics");
        const payrollBody = getSitePayload(siteId, "payroll");
        const parkingRatesBody = getSitePayload(siteId, "parkingRates");
        const otherRevenueBody = getSitePayload(siteId, "otherRevenue");
        const otherExpenseBody = getSitePayload(siteId, "otherExpense");

        // Helpful debug lines (safe if large payloads? toggle as needed)
        this.context.logger.debug(`Payloads present for ${siteId}`, {
          statistics: !!statisticsBody,
          payroll: !!payrollBody,
          parkingRates: !!parkingRatesBody,
          otherRevenue: !!otherRevenueBody,
          otherExpense: !!otherExpenseBody,
        });

        // Statistics
        if (statisticsBody) {
          await this.executeStep(`Edit Statistics (${siteId})`, async () =>
            new EditStatistics().patch(client, statisticsBody, siteId)
          );
        } else {
          this.context.logger.warn(
            `Missing statistics payload for ${siteId}, skipping`
          );
        }

        // Payroll
        if (payrollBody) {
          await this.executeStep(`Edit Payroll (${siteId})`, async () =>
            new EditPayroll().patch(client, payrollBody, siteId)
          );
        } else {
          this.context.logger.warn(
            `Missing payroll payload for ${siteId}, skipping`
          );
        }

        // Parking Rates
        if (parkingRatesBody) {
          await this.executeStep(`Edit Parking Rates (${siteId})`, async () =>
            new EditParkingRates().patch(client, parkingRatesBody, siteId)
          );
        } else {
          this.context.logger.warn(
            `Missing parkingRates payload for ${siteId}, skipping`
          );
        }

        // Other Revenue
        if (otherRevenueBody) {
          await this.executeStep(`Edit Other Revenue (${siteId})`, async () =>
            new EditOtherRevenue().patch(client, otherRevenueBody, siteId)
          );
        } else {
          this.context.logger.warn(
            `Missing otherRevenue payload for ${siteId}, skipping`
          );
        }

        // Other Expense
        if (otherExpenseBody) {
          await this.executeStep(`Edit Other Expense (${siteId})`, async () =>
            new EditOtherExpense().patch(client, otherExpenseBody, siteId)
          );
        } else {
          this.context.logger.warn(
            `Missing otherExpense payload for ${siteId}, skipping`
          );
        }

        // Small breather between sites so you don’t nuke the backend
        this.randomSleep(1, 3);
        this.context.logger.info(`Mass edit end for siteId=${siteId}`);
      }

      this.logTestEnd("Mass Edit and Save", Date.now() - start);
    } catch (error) {
      this.context.logger.error("Mass edit save failed", {
        error: (error as Error).message,
      });
      throw error;
    }
  }

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
export function massEditSaveStress(): void {
  const test = new MassEditSaveStress();
  test.execute();
}
