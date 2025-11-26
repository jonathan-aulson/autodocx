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

export class ChangeYearMonthAndTimePeriod extends BaseTest {
  constructor() {
    super(config);
  }

  async execute(): Promise<void> {
    const startTime = Date.now();
    this.logTestStart("Change Year Month And Time Period");

    try {
      const client = this.getAuthenticatedClient();

      for (const site of siteDataJson.siteData) {
        const siteId = site.siteId;
        this.context.logger.info(`Processing Site ID: ${siteId}`);

        this.executeStep("Load Sites", () => new LoadSites().execute(client));

        // Load Statistics for all time periods
        this.executeStep("Load Statistics - Future Month", () =>
          new LoadStatistics().executeFutureMonth(client, siteId)
        );
        this.executeStep("Load Statistics - Future Year", () =>
          new LoadStatistics().executeFutureYear(client, siteId)
        );
        this.executeStep("Load Statistics - Past Month", () =>
          new LoadStatistics().executePastMonth(client, siteId)
        );
        this.executeStep("Load Statistics - Past Year", () =>
          new LoadStatistics().executePastYear(client, siteId)
        );

        // Load Parking Rates for all time periods
        this.executeStep("Load Parking Rates - Future Month", () =>
          new LoadParkingRates().executeFutureMonth(client, siteId)
        );
        this.executeStep("Load Parking Rates - Future Year", () =>
          new LoadParkingRates().executeFutureYear(client, siteId)
        );
        this.executeStep("Load Parking Rates - Past Month", () =>
          new LoadParkingRates().executePastMonth(client, siteId)
        );
        this.executeStep("Load Parking Rates - Past Year", () =>
          new LoadParkingRates().executePastYear(client, siteId)
        );

        // Load Payroll for all time periods
        this.executeStep("Load Payroll - Future Month", () =>
          new LoadPayroll().executeFutureMonth(client, siteId)
        );
        this.executeStep("Load Payroll - Future Year", () =>
          new LoadPayroll().executeFutureYear(client, siteId)
        );
        this.executeStep("Load Payroll - Past Month", () =>
          new LoadPayroll().executePastMonth(client, siteId)
        );
        this.executeStep("Load Payroll - Past Year", () =>
          new LoadPayroll().executePastYear(client, siteId)
        );

        // Load Other Revenue for all time periods
        this.executeStep("Load Other Revenue - Future Month", () =>
          new LoadOtherRevenue().executeFutureMonth(client, siteId)
        );
        this.executeStep("Load Other Revenue - Future Year", () =>
          new LoadOtherRevenue().executeFutureYear(client, siteId)
        );
        this.executeStep("Load Other Revenue - Past Month", () =>
          new LoadOtherRevenue().executePastMonth(client, siteId)
        );
        this.executeStep("Load Other Revenue - Past Year", () =>
          new LoadOtherRevenue().executePastYear(client, siteId)
        );

        // Load Other Expense for all time periods
        this.executeStep("Load Other Expense - Future Month", () =>
          new LoadOtherExpense().executeFutureMonth(client, siteId)
        );
        this.executeStep("Load Other Expense - Future Year", () =>
          new LoadOtherExpense().executeFutureYear(client, siteId)
        );
        this.executeStep("Load Other Expense - Past Month", () =>
          new LoadOtherExpense().executePastMonth(client, siteId)
        );
        this.executeStep("Load Other Expense - Past Year", () =>
          new LoadOtherExpense().executePastYear(client, siteId)
        );

        // Load Job Codes for all time periods
        this.executeStep("Load Job Codes - Future Month", () =>
          new LoadJobCodes().executeFutureMonth(client, siteId)
        );
        this.executeStep("Load Job Codes - Future Year", () =>
          new LoadJobCodes().executeFutureYear(client, siteId)
        );
        this.executeStep("Load Job Codes - Past Month", () =>
          new LoadJobCodes().executePastMonth(client, siteId)
        );
        this.executeStep("Load Job Codes - Past Year", () =>
          new LoadJobCodes().executePastYear(client, siteId)
        );

        this.executeStep("Load Contract", () =>
          new LoadContract().execute(client, siteId)
        );

        this.randomSleep(1, 3);
      }

      this.logTestEnd(
        "Change Year Month And Time Period",
        Date.now() - startTime
      );
    } catch (error) {
      this.context.logger.error("Change Year Month And Time Period failed", {
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
export function changeYearMonthAndTimePeriod(): void {
  const test = new ChangeYearMonthAndTimePeriod();
  test.execute();
}
