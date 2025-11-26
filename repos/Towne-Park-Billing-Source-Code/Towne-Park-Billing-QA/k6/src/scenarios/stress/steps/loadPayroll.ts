import { HttpClient } from '@/core/http.client';
import { check } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';

// --- Metrics ---
const payrollLoadTime = new Trend('payroll_load_time', true);
const payrollLoadSuccess = new Rate('payroll_load_success');
const payrollLoadError429 = new Rate('payroll_load_error_429');
const payrollLoadError500 = new Rate('payroll_load_error_500');
const payrollLoadTotal = new Counter('payroll_load_total');
const payrollLoadErrorCount = new Counter('payroll_load_error_count');

export class LoadPayroll {
  async execute(
    client: HttpClient,
    siteId = '59b0f1f2-2aed-ef11-be21-6045bd096814',
    period = '2025-07'
  ): Promise<void> {
    const endpoint = `/api/payroll/${siteId}/${period}`;
    const startTime = Date.now();
    payrollLoadTotal.add(1);

    try {
      const response = await client.get(endpoint, {
        tags: {
          operation: 'load_payroll',
          step: 'forecasting_flow',
          siteId: siteId,
          period: period,
        }
      });

      const isSuccess = check(response, {
        'payroll: status is 200': (r) => r.status === 200,
        'payroll: response time < 2s': (r) => r.timings.duration < 2000,
      });

      payrollLoadTime.add(Date.now() - startTime, {
        status: response.status.toString(),
        siteId: siteId,
        period: period,
        operation: 'load_payroll'
      });
      payrollLoadSuccess.add(isSuccess ? 1 : 0);

      payrollLoadError429.add(response.status === 429 ? 1 : 0);
      payrollLoadError500.add(response.status === 500 ? 1 : 0);
      if (!isSuccess) {
        payrollLoadErrorCount.add(1);
      }

    } catch (error) {
      payrollLoadSuccess.add(0);
      payrollLoadErrorCount.add(1);
      throw error;
    }
  }
  /**
   * Executes payroll load for all time periods: DAILY, WEEKLY, MONTHLY.
   * Assumes period format "YYYY-MM" for monthly, "YYYY-[W]WW" for weekly, and "YYYY-MM-DD" for daily.
   */
  async executeAllTimePeriods(
    client: HttpClient,
    siteId = '59b0f1f2-2aed-ef11-be21-6045bd096814',
    baseYear = new Date().getFullYear(),
    baseMonth = (new Date().getMonth() + 1).toString().padStart(2, '0'),
    baseDay = new Date().getDate().toString().padStart(2, '0')
  ): Promise<void> {
    // Monthly: "YYYY-MM"
    await this.execute(client, siteId, `${baseYear}-${baseMonth}`);
    // Weekly: "YYYY-[W]WW" (ISO week, simplified as "YYYY-W01")
    await this.execute(client, siteId, `${baseYear}-W01`);
    // Daily: "YYYY-MM-DD"
    await this.execute(client, siteId, `${baseYear}-${baseMonth}-${baseDay}`);
  }

  async executePastYear(
    client: HttpClient,
    siteId = '59b0f1f2-2aed-ef11-be21-6045bd096814'
  ): Promise<void> {
    const year = new Date().getFullYear() - 1;
    const month = (new Date().getMonth() + 1).toString().padStart(2, '0');
    await this.execute(client, siteId, `${year}-${month}`);
  }

  async executePastMonth(
    client: HttpClient,
    siteId = '59b0f1f2-2aed-ef11-be21-6045bd096814'
  ): Promise<void> {
    const now = new Date();
    now.setMonth(now.getMonth() - 1);
    const year = now.getFullYear();
    const month = (now.getMonth() + 1).toString().padStart(2, '0');
    await this.execute(client, siteId, `${year}-${month}`);
  }

  async executeFutureYear(
    client: HttpClient,
    siteId = '59b0f1f2-2aed-ef11-be21-6045bd096814'
  ): Promise<void> {
    const year = new Date().getFullYear() + 1;
    const month = (new Date().getMonth() + 1).toString().padStart(2, '0');
    await this.execute(client, siteId, `${year}-${month}`);
  }

  async executeFutureMonth(
    client: HttpClient,
    siteId = '59b0f1f2-2aed-ef11-be21-6045bd096814'
  ): Promise<void> {
    const now = new Date();
    now.setMonth(now.getMonth() + 1);
    const year = now.getFullYear();
    const month = (now.getMonth() + 1).toString().padStart(2, '0');
    await this.execute(client, siteId, `${year}-${month}`);
  }
}