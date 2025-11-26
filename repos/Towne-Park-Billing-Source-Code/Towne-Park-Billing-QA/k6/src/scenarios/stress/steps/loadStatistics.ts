import { HttpClient } from "@/core/http.client";
import { check } from "k6";
import { Counter, Rate, Trend } from "k6/metrics";

// --- Metrics ---
const statisticsLoadTime = new Trend("statistics_load_time", true);
const statisticsLoadSuccess = new Rate("statistics_load_success");
const statisticsLoadError429 = new Rate("statistics_load_error_429");
const statisticsLoadError500 = new Rate("statistics_load_error_500");
const statisticsLoadTotal = new Counter("statistics_load_total");
const statisticsLoadErrorCount = new Counter("statistics_load_error_count");

function getNormalizedYearMonth(offset: number = 0): { year: number; month: string } {
  const now = new Date();
  let year = now.getFullYear();
  let month = now.getMonth() + 1 + offset; // JS months are 0-based

  while (month > 12) {
    month -= 12;
    year += 1;
  }
  while (month < 1) {
    month += 12;
    year -= 1;
  }
  return { year, month: month.toString().padStart(2, "0") };
}

export class LoadStatistics {
  async execute(
    client: HttpClient,
    siteId = "59b0f1f2-2aed-ef11-be21-6045bd096814",
    year = getNormalizedYearMonth().year,
    month = getNormalizedYearMonth().month,
    timeRange = "DAILY"
  ): Promise<void> {
    const endpoint = `/api/siteStatistics/${siteId}/${year}-${month}?timeRange=${timeRange}`;
    const startTime = Date.now();
    statisticsLoadTotal.add(1);

    try {
      const response = await client.get(endpoint, {
        tags: {
          operation: "load_statistics",
          site_id: siteId.toString(),
          year: year.toString(),
          month: month.toString(),
          time_range: timeRange,
          step: "forecasting_flow",
        },
      });

      const isSuccess = check(response, {
        "Statistics loaded successfully": (r) => r.status === 200,
        "Statistics has forecast data": (r) => r.body.length > 0,
        "Statistics response time OK": (r) => r.timings.duration < 3000,
      });

      statisticsLoadTime.add(Date.now() - startTime, {
        status: response.status.toString(),
        site_id: siteId,
        year: year.toString(),
        month: month.toString(),
        time_range: timeRange,
        operation: "load_statistics",
      });
      statisticsLoadSuccess.add(isSuccess ? 1 : 0);

      statisticsLoadError429.add(response.status === 429 ? 1 : 0);
      statisticsLoadError500.add(response.status === 500 ? 1 : 0);
      if (!isSuccess) {
        statisticsLoadErrorCount.add(1);
      }
    } catch (error) {
      statisticsLoadSuccess.add(0);
      statisticsLoadErrorCount.add(1);
      throw error;
    }
  }
  async executePastYear(
    client: HttpClient,
    siteId = "59b0f1f2-2aed-ef11-be21-6045bd096814",
    year = getNormalizedYearMonth().year - 1,
    month = getNormalizedYearMonth().month,
    timeRange = "DAILY"
  ): Promise<void> {
    const endpoint = `/api/siteStatistics/${siteId}/${year}-${month}?timeRange=${timeRange}`;
    const startTime = Date.now();
    statisticsLoadTotal.add(1);

    try {
      const response = await client.get(endpoint, {
        tags: {
          operation: "load_statistics",
          site_id: siteId.toString(),
          year: year.toString(),
          month: month.toString(),
          time_range: timeRange,
          step: "forecasting_flow",
        },
      });

      const isSuccess = check(response, {
        "Statistics loaded successfully": (r) => r.status === 200,
        "Statistics has forecast data": (r) => r.body.length > 0,
        "Statistics response time OK": (r) => r.timings.duration < 3000,
      });

      statisticsLoadTime.add(Date.now() - startTime, {
        status: response.status.toString(),
        site_id: siteId,
        year: year.toString(),
        month: month.toString(),
        time_range: timeRange,
        operation: "load_statistics",
      });
      statisticsLoadSuccess.add(isSuccess ? 1 : 0);

      statisticsLoadError429.add(response.status === 429 ? 1 : 0);
      statisticsLoadError500.add(response.status === 500 ? 1 : 0);
      if (!isSuccess) {
        statisticsLoadErrorCount.add(1);
      }
    } catch (error) {
      statisticsLoadSuccess.add(0);
      statisticsLoadErrorCount.add(1);
      throw error;
    }
  }
  async executePastMonth(
    client: HttpClient,
    siteId = "59b0f1f2-2aed-ef11-be21-6045bd096814",
    year = getNormalizedYearMonth(-1).year,
    month = getNormalizedYearMonth(-1).month,
    timeRange = "DAILY"
  ): Promise<void> {
    const endpoint = `/api/siteStatistics/${siteId}/${year}-${month}?timeRange=${timeRange}`;
    const startTime = Date.now();
    statisticsLoadTotal.add(1);

    try {
      const response = await client.get(endpoint, {
        tags: {
          operation: "load_statistics",
          site_id: siteId.toString(),
          year: year.toString(),
          month: month.toString(),
          time_range: timeRange,
          step: "forecasting_flow",
        },
      });

      const isSuccess = check(response, {
        "Statistics loaded successfully": (r) => r.status === 200,
        "Statistics has forecast data": (r) => r.body.length > 0,
        "Statistics response time OK": (r) => r.timings.duration < 3000,
      });

      statisticsLoadTime.add(Date.now() - startTime, {
        status: response.status.toString(),
        site_id: siteId,
        year: year.toString(),
        month: month.toString(),
        time_range: timeRange,
        operation: "load_statistics",
      });
      statisticsLoadSuccess.add(isSuccess ? 1 : 0);

      statisticsLoadError429.add(response.status === 429 ? 1 : 0);
      statisticsLoadError500.add(response.status === 500 ? 1 : 0);
      if (!isSuccess) {
        statisticsLoadErrorCount.add(1);
      }
    } catch (error) {
      statisticsLoadSuccess.add(0);
      statisticsLoadErrorCount.add(1);
      throw error;
    }
  }
  async executeFutureYear(
    client: HttpClient,
    siteId = "59b0f1f2-2aed-ef11-be21-6045bd096814",
    year = getNormalizedYearMonth().year + 1,
    month = getNormalizedYearMonth().month,
    timeRange = "DAILY"
  ): Promise<void> {
    const endpoint = `/api/siteStatistics/${siteId}/${year}-${month}?timeRange=${timeRange}`;
    const startTime = Date.now();
    statisticsLoadTotal.add(1);

    try {
      const response = await client.get(endpoint, {
        tags: {
          operation: "load_statistics",
          site_id: siteId.toString(),
          year: year.toString(),
          month: month.toString(),
          time_range: timeRange,
          step: "forecasting_flow",
        },
      });

      const isSuccess = check(response, {
        "Statistics loaded successfully": (r) => r.status === 200,
        "Statistics has forecast data": (r) => r.body.length > 0,
        "Statistics response time OK": (r) => r.timings.duration < 3000,
      });

      statisticsLoadTime.add(Date.now() - startTime, {
        status: response.status.toString(),
        site_id: siteId,
        year: year.toString(),
        month: month.toString(),
        time_range: timeRange,
        operation: "load_statistics",
      });
      statisticsLoadSuccess.add(isSuccess ? 1 : 0);

      statisticsLoadError429.add(response.status === 429 ? 1 : 0);
      statisticsLoadError500.add(response.status === 500 ? 1 : 0);
      if (!isSuccess) {
        statisticsLoadErrorCount.add(1);
      }
    } catch (error) {
      statisticsLoadSuccess.add(0);
      statisticsLoadErrorCount.add(1);
      throw error;
    }
  }
  async executeFutureMonth(
    client: HttpClient,
    siteId = "59b0f1f2-2aed-ef11-be21-6045bd096814",
    year = getNormalizedYearMonth(1).year,
    month = getNormalizedYearMonth(1).month,
    timeRange = "DAILY"
  ): Promise<void> {
    const endpoint = `/api/siteStatistics/${siteId}/${year}-${month}?timeRange=${timeRange}`;
    const startTime = Date.now();
    statisticsLoadTotal.add(1);

    try {
      const response = await client.get(endpoint, {
        tags: {
          operation: "load_statistics",
          site_id: siteId.toString(),
          year: year.toString(),
          month: month.toString(),
          time_range: timeRange,
          step: "forecasting_flow",
        },
      });

      const isSuccess = check(response, {
        "Statistics loaded successfully": (r) => r.status === 200,
        "Statistics has forecast data": (r) => r.body.length > 0,
        "Statistics response time OK": (r) => r.timings.duration < 3000,
      });

      statisticsLoadTime.add(Date.now() - startTime, {
        status: response.status.toString(),
        site_id: siteId,
        year: year.toString(),
        month: month.toString(),
        time_range: timeRange,
        operation: "load_statistics",
      });
      statisticsLoadSuccess.add(isSuccess ? 1 : 0);

      statisticsLoadError429.add(response.status === 429 ? 1 : 0);
      statisticsLoadError500.add(response.status === 500 ? 1 : 0);
      if (!isSuccess) {
        statisticsLoadErrorCount.add(1);
      }
    } catch (error) {
      statisticsLoadSuccess.add(0);
      statisticsLoadErrorCount.add(1);
      throw error;
    }
  }

  /**
   * Executes statistics load for all time periods: DAILY, WEEKLY, MONTHLY.
   */
  async executeAllTimePeriods(
    client: HttpClient,
    siteId = "59b0f1f2-2aed-ef11-be21-6045bd096814",
    year = getNormalizedYearMonth().year,
    month = getNormalizedYearMonth().month
  ): Promise<void> {
    const timePeriods = ["DAILY", "WEEKLY", "MONTHLY"];
    for (const timeRange of timePeriods) {
      await this.execute(client, siteId, year, month, timeRange);
    }
  }
}
