import { HttpClient } from '@/core/http.client';
import { check } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';

// --- Metrics ---
const otherExpenseLoadTime = new Trend('other_expense_load_time', true);
const otherExpenseLoadSuccess = new Rate('other_expense_load_success');
const otherExpenseLoadError429 = new Rate('other_expense_load_error_429');
const otherExpenseLoadError500 = new Rate('other_expense_load_error_500');
const otherExpenseLoadTotal = new Counter('other_expense_load_total');
const otherExpenseLoadErrorCount = new Counter('other_expense_load_error_count');

export class LoadOtherExpense {
  async execute(
    client: HttpClient,
    siteId = '59b0f1f2-2aed-ef11-be21-6045bd096814',
    period = '2025-07'
  ): Promise<void> {
    const endpoint = `/api/otherExpense/${siteId}/${period}`;
    const startTime = Date.now();
    otherExpenseLoadTotal.add(1);

    try {
      const response = await client.get(endpoint, {
        tags: {
          operation: 'load_other_expense',
          step: 'forecasting_flow',
          siteId: siteId,
          period: period,
        }
      });

      const isSuccess = check(response, {
        'otherExpense: status is 200': (r) => r.status === 200,
        'otherExpense: response time < 2s': (r) => r.timings.duration < 2000,
      });

      otherExpenseLoadTime.add(Date.now() - startTime, {
        status: response.status.toString(),
        siteId: siteId,
        period: period,
        operation: 'load_other_expense'
      });
      otherExpenseLoadSuccess.add(isSuccess ? 1 : 0);

      otherExpenseLoadError429.add(response.status === 429 ? 1 : 0);
      otherExpenseLoadError500.add(response.status === 500 ? 1 : 0);
      if (!isSuccess) {
        otherExpenseLoadErrorCount.add(1);
      }

    } catch (error) {
      otherExpenseLoadSuccess.add(0);
      otherExpenseLoadErrorCount.add(1);
      throw error;
    }
  }
  /**
   * Executes other expense load for all time periods: DAILY, WEEKLY, MONTHLY.
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