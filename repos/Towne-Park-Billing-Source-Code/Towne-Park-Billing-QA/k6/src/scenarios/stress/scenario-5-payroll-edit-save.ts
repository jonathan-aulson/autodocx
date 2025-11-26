import { getConfig } from "@/config/env.config";
import { BaseTest } from "@/core/base.test";
import { allSiteIds, getSitePayload } from "@/utils/payload-loader";
import { EditPayroll } from "./steps/editPayroll";

const config = getConfig();

export class PayrollEditSaveStress extends BaseTest {
  constructor() {
    super(config);
  }

  async execute(): Promise<void> {
    const start = Date.now();
    this.logTestStart("Payroll Edit Save");

    try {
      const client = this.getAuthenticatedClient();

      for (const siteId of allSiteIds) {
        const body = getSitePayload(siteId, "payroll");
        this.context.logger.info(`Processing siteId=${siteId}`);
        this.context.logger.debug(
          `Payroll payload for siteId=${siteId}:`,
          body
        );

        if (!body) {
          this.context.logger.warn(
            `No payroll payload for siteId=${siteId}, skipping`
          );
          continue;
        }

        await this.executeStep(
          `Edit Payroll (${siteId})`,
          async () => new EditPayroll().patch(client, body) // passing payload from the scenario
        );

        this.randomSleep(1, 2);
      }

      this.logTestEnd("Payroll Edit Save", Date.now() - start);
    } catch (error) {
      this.context.logger.error("Payroll edit save failed", {
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
export function payrollEditSaveStress(): void {
  const test = new PayrollEditSaveStress();
  test.execute();
}
