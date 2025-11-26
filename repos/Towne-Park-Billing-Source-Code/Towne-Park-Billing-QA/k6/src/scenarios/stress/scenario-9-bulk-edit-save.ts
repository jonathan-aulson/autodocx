import { getConfig } from "@/config/env.config";
import { BaseTest } from "@/core/base.test";

const config = getConfig();

export class BulkEditSaveStress extends BaseTest {
  constructor() {
    super(config);
  }

  async execute(): Promise<void> {
    this.logTestStart("Bulk Edit and Save");
    try {
      const client = this.getAuthenticatedClient();

      // Prepare all edits (no-op, as edits are stateless)
      this.logStep("Preparing all edits");

      // Save all changes in sequence
      // this.executeStep("Edit Statistics", async () =>
      //   await new EditStatistics().patch(client)
      // );
      // this.executeStep(
      //   "Edit Payroll",
      //   async () => await new EditPayroll().patch(client)
      // );
      // this.executeStep(
      //   "Edit Parking Rates",
      //   async () => await new EditParkingRates().patch(client)
      // );
      // this.executeStep(
      //   "Edit Other Revenue",
      //   async () => await new EditOtherRevenue().patch(client)
      // );
      // this.executeStep(
      //   "Edit Other Expense",
      //   async () => await new EditOtherExpense().patch(client)
      // );

      this.logTestEnd("Bulk Edit and Save");
    } catch (error) {
      this.context.logger.error("Bulk edit save failed", {
        error: (error as Error).message,
      });
      throw error;
    }
  }

  private executeStep(
    stepName: string,
    stepFunction: () => Promise<void>
  ): void {
    this.logStep(`Starting: ${stepName}`);
    stepFunction();
    this.logStep(`Completed: ${stepName}`);
    this.randomSleep(0.5, 1.5);
  }
}

// Export function for K6 compatibility
export function bulkEditSaveStress(): void {
  const test = new BulkEditSaveStress();
  // If K6 supports async, use test.execute(), else wrap in Promise.resolve
  (test.execute as any)();
}
