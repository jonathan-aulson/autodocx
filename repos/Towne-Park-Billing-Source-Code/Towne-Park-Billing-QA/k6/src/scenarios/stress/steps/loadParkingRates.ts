import { HttpClient } from '@/core/http.client';
import { check } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';

// --- Metrics ---
const parkingRatesLoadTime = new Trend('parking_rates_load_time', true);
const parkingRatesLoadSuccess = new Rate('parking_rates_load_success');
const parkingRatesLoadError429 = new Rate('parking_rates_load_error_429');
const parkingRatesLoadError500 = new Rate('parking_rates_load_error_500');
const parkingRatesLoadTotal = new Counter('parking_rates_load_total');
const parkingRatesLoadErrorCount = new Counter('parking_rates_load_error_count');

export class LoadParkingRates {
  async execute(
    client: HttpClient,
    siteId = '59b0f1f2-2aed-ef11-be21-6045bd096814',
    year = '2025'
  ): Promise<void> {
    const endpoint = `/api/parkingRates/${siteId}/${year}`;
    const startTime = Date.now();
    parkingRatesLoadTotal.add(1);

    try {
      const response = await client.get(endpoint, {
        tags: {
          operation: 'load_parking_rates',
          step: 'forecasting_flow',
          siteId: siteId,
          year: year,
        }
      });

      const isSuccess = check(response, {
        'parkingRates: status is 200': (r) => r.status === 200,
        'parkingRates: response time < 2s': (r) => r.timings.duration < 2000,
      });

      parkingRatesLoadTime.add(Date.now() - startTime, {
        status: response.status.toString(),
        siteId: siteId,
        year: year,
        operation: 'load_parking_rates'
      });
      parkingRatesLoadSuccess.add(isSuccess ? 1 : 0);

      parkingRatesLoadError429.add(response.status === 429 ? 1 : 0);
      parkingRatesLoadError500.add(response.status === 500 ? 1 : 0);
      if (!isSuccess) {
        parkingRatesLoadErrorCount.add(1);
      }

    } catch (error) {
      parkingRatesLoadSuccess.add(0);
      parkingRatesLoadErrorCount.add(1);
      throw error;
    }
  }
  /**
   * Executes parking rates load for all time periods: DAILY, WEEKLY, MONTHLY.
   * For this API, only year is used, so we call execute for the current, previous, and next year.
   */
  async executeAllTimePeriods(
    client: HttpClient,
    siteId = '59b0f1f2-2aed-ef11-be21-6045bd096814',
    baseYear = new Date().getFullYear()
  ): Promise<void> {
    // Current year
    await this.execute(client, siteId, `${baseYear}`);
    // Previous year
    await this.execute(client, siteId, `${baseYear - 1}`);
    // Next year
    await this.execute(client, siteId, `${baseYear + 1}`);
  }

  async executePastYear(
    client: HttpClient,
    siteId = '59b0f1f2-2aed-ef11-be21-6045bd096814'
  ): Promise<void> {
    const year = new Date().getFullYear() - 1;
    await this.execute(client, siteId, `${year}`);
  }

  async executePastMonth(
    client: HttpClient,
    siteId = '59b0f1f2-2aed-ef11-be21-6045bd096814'
  ): Promise<void> {
    // For parking rates, treat "past month" as previous year for consistency
    const year = new Date().getFullYear() - 1;
    await this.execute(client, siteId, `${year}`);
  }

  async executeFutureYear(
    client: HttpClient,
    siteId = '59b0f1f2-2aed-ef11-be21-6045bd096814'
  ): Promise<void> {
    const year = new Date().getFullYear() + 1;
    await this.execute(client, siteId, `${year}`);
  }

  async executeFutureMonth(
    client: HttpClient,
    siteId = '59b0f1f2-2aed-ef11-be21-6045bd096814'
  ): Promise<void> {
    // For parking rates, treat "future month" as next year for consistency
    const year = new Date().getFullYear() + 1;
    await this.execute(client, siteId, `${year}`);
  }
}