import { getConfig } from "@/config/env.config";
import { BaseTest } from "@/core/base.test";
import { allSiteIds, getSitePayload } from "@/utils/payload-loader";
import { EditOtherExpense } from "./steps/editOtherExpense";

const config = getConfig();

export class OtherExpenseEditSaveStress extends BaseTest {
  constructor() {
    super(config);
  }

  async execute(): Promise<void> {
    const start = Date.now();
    this.logTestStart("Other Expense Edit Save");

    try {
      const client = this.getAuthenticatedClient();

      for (const siteId of allSiteIds) {
        const body = getSitePayload(siteId, "otherExpense");
        this.context.logger.info(`Processing siteId=${siteId}`);
        this.context.logger.debug(
          `OtherExpense payload for siteId=${siteId}:`,
          body
        );

        if (!body) {
          this.context.logger.warn(
            `No otherExpense payload for siteId=${siteId}, skipping`
          );
          continue;
        }

        await this.executeStep(
          `Edit Other Expense (${siteId})`,
          async () => new EditOtherExpense().patch(client, body, siteId) // Pass body & siteId for tags
        );

        this.randomSleep(1, 2);
      }

      this.logTestEnd("Other Expense Edit Save", Date.now() - start);
    } catch (error) {
      this.context.logger.error("Other expense edit save failed", {
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

export function otherExpenseEditSaveStress(): void {
  const test = new OtherExpenseEditSaveStress();
  test.execute();
}
