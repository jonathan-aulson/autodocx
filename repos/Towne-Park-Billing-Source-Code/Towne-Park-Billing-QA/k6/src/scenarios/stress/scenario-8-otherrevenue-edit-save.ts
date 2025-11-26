import { getConfig } from "@/config/env.config";
import { BaseTest } from "@/core/base.test";
import { allSiteIds, getSitePayload } from "@/utils/payload-loader";
import { EditOtherRevenue } from "./steps/editOtherRevenue";

const config = getConfig();

export class OtherRevenueEditSaveStress extends BaseTest {
  constructor() {
    super(config);
  }

  async execute(): Promise<void> {
    const start = Date.now();
    this.logTestStart("Other Revenue Edit Save");

    try {
      const client = this.getAuthenticatedClient();

      for (const siteId of allSiteIds) {
        const body = getSitePayload(siteId, "otherRevenue");
        this.context.logger.info(`Processing siteId=${siteId}`);
        this.context.logger.debug(
          `OtherRevenue payload for siteId=${siteId}:`,
          body
        );

        if (!body) {
          this.context.logger.warn(
            `No otherRevenue payload for siteId=${siteId}, skipping`
          );
          continue;
        }

        await this.executeStep(
          `Edit Other Revenue (${siteId})`,
          async () => new EditOtherRevenue().patch(client, body, siteId) // pass payload (and siteId for tags)
        );

        this.randomSleep(1, 2);
      }

      this.logTestEnd("Other Revenue Edit Save", Date.now() - start);
    } catch (error) {
      this.context.logger.error("Other revenue edit save failed", {
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
export function otherRevenueEditSaveStress(): void {
  const test = new OtherRevenueEditSaveStress();
  test.execute();
}
