import { getConfig } from "@/config/env.config";
import { BaseTest } from "@/core/base.test";
import { allSiteIds, getSitePayload } from "@/utils/payload-loader";
import { EditStatistics } from "./steps/editStatistics";

const config = getConfig();

export class StatisticsEditSaveStress extends BaseTest {
  constructor() {
    super(config);
  }
  async execute(): Promise<void> {
    const start = Date.now();
    this.logTestStart("Statistics Edit Save");

    try {
      const client = this.getAuthenticatedClient();

      for (const siteId of allSiteIds) {
        const body = getSitePayload(siteId, "statistics");
        this.context.logger.info(`Processing siteId=${siteId}`);
        this.context.logger.debug(`Payload for siteId=${siteId}:`, body);
        if (!body) {
          this.context.logger.warn(
            `No statistics payload for siteId=${siteId}, skipping`
          );
          continue;
        }

        await this.executeStep(`Edit Statistics (${siteId})`, async () =>
          new EditStatistics().patch(client, body)
        );

        this.randomSleep(1, 2);
      }

      this.logTestEnd("Statistics Edit Save", Date.now() - start);
    } catch (error) {
      this.context.logger.error("Statistics edit save failed", {
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
export function statisticsEditSaveStress(): void {
  const test = new StatisticsEditSaveStress();
  test.execute();
}
