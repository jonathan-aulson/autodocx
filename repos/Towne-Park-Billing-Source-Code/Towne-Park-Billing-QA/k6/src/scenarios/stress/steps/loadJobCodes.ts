import { HttpClient } from '@/core/http.client';
import { check } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';

// --- Metrics ---
const jobCodesLoadTime = new Trend('job_codes_load_time', true);
const jobCodesLoadSuccess = new Rate('job_codes_load_success');
const jobCodesLoadError429 = new Rate('job_codes_load_error_429');
const jobCodesLoadError500 = new Rate('job_codes_load_error_500');
const jobCodesLoadTotal = new Counter('job_codes_load_total');
const jobCodesLoadErrorCount = new Counter('job_codes_load_error_count');

export class LoadJobCodes {
  async execute(client: HttpClient, siteId = '59b0f1f2-2aed-ef11-be21-6045bd096814'): Promise<void> {
    const endpoint = `/api/job-codes/by-site/${siteId}`;
    const startTime = Date.now();
    jobCodesLoadTotal.add(1);

    try {
      const response = await client.get(endpoint, {
        tags: {
          operation: 'load_job_codes',
          step: 'forecasting_flow',
          siteId: siteId,
        }
      });

      const isSuccess = check(response, {
        'jobcodes: status is 200': (r) => r.status === 200,
        'jobcodes: response time < 2s': (r) => r.timings.duration < 2000,
      });

      jobCodesLoadTime.add(Date.now() - startTime, {
        status: response.status.toString(),
        siteId: siteId,
        operation: 'load_job_codes'
      });
      jobCodesLoadSuccess.add(isSuccess ? 1 : 0);

      jobCodesLoadError429.add(response.status === 429 ? 1 : 0);
      jobCodesLoadError500.add(response.status === 500 ? 1 : 0);
      if (!isSuccess) {
        jobCodesLoadErrorCount.add(1);
      }

    } catch (error) {
      jobCodesLoadSuccess.add(0);
      jobCodesLoadErrorCount.add(1);
      throw error;
    }
  }
  /**
   * Executes job codes load for all time periods: DAILY, WEEKLY, MONTHLY.
   * Since the API does not take a period, this simply calls execute once.
   */
  async executeAllTimePeriods(
    client: HttpClient,
    siteId = '59b0f1f2-2aed-ef11-be21-6045bd096814'
  ): Promise<void> {
    await this.execute(client, siteId);
  }

  async executePastYear(
    client: HttpClient,
    siteId = '59b0f1f2-2aed-ef11-be21-6045bd096814'
  ): Promise<void> {
    await this.execute(client, siteId);
  }

  async executePastMonth(
    client: HttpClient,
    siteId = '59b0f1f2-2aed-ef11-be21-6045bd096814'
  ): Promise<void> {
    await this.execute(client, siteId);
  }

  async executeFutureYear(
    client: HttpClient,
    siteId = '59b0f1f2-2aed-ef11-be21-6045bd096814'
  ): Promise<void> {
    await this.execute(client, siteId);
  }

  async executeFutureMonth(
    client: HttpClient,
    siteId = '59b0f1f2-2aed-ef11-be21-6045bd096814'
  ): Promise<void> {
    await this.execute(client, siteId);
  }
}