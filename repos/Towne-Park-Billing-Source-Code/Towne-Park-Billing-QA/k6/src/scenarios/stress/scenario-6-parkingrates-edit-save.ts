import { getConfig } from "@/config/env.config";
import { BaseTest } from "@/core/base.test";
import { allSiteIds, getSitePayload } from "@/utils/payload-loader";
import { EditParkingRates } from "./steps/editParkingRates";

const config = getConfig();

export class ParkingRatesEditSaveStress extends BaseTest {
  constructor() {
    super(config);
  }

  async execute(): Promise<void> {
    const start = Date.now();
    this.logTestStart("Parking Rates Edit Save");

    try {
      const client = this.getAuthenticatedClient();

      for (const siteId of allSiteIds) {
        const body = getSitePayload(siteId, "parkingRates");
        this.context.logger.info(`Processing siteId=${siteId}`);
        if (!body) {
          this.context.logger.warn(
            `No parkingRates payload for siteId=${siteId}, skipping`
          );
          continue;
        }

        await this.executeStep(
          `Edit Parking Rates (${siteId})`,
          async () => new EditParkingRates().patch(client, body, siteId) // pass body (and siteId for tags)
        );

        this.randomSleep(1, 2);
      }

      this.logTestEnd("Parking Rates Edit Save", Date.now() - start);
    } catch (error) {
      this.context.logger.error("Parking rates edit save failed", {
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

export function parkingRatesEditSaveStress(): void {
  const test = new ParkingRatesEditSaveStress();
  test.execute();
}
