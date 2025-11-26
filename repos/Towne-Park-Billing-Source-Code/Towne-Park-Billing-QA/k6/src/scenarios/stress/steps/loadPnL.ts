import { HttpClient } from '@/core/http.client';
import { check } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';
import pnlData from '../../../data/pnl-data.json';

interface PnLPayload {
  siteIds: string[];
  year?: number;
  date?: string;
}

// --- Metrics ---
const pnlLoadTime = new Trend('pnl_load_time', true);
const pnlLoadSuccess = new Rate('pnl_load_success');
const pnlLoadError429 = new Rate('pnl_load_error_429');
const pnlLoadError500 = new Rate('pnl_load_error_500');
const pnlLoadTotal = new Counter('pnl_load_total');
const pnlLoadErrorCount = new Counter('pnl_load_error_count');

export class LoadPnL {
  private defaultSiteIds: string[] = pnlData.siteIds;

  async execute(client: HttpClient, siteIds?: string[]): Promise<void> {
    const selectedSiteIds = siteIds || this.defaultSiteIds;
    const currentYear = new Date().getFullYear();
    const years = [
      { label: 'current_year', year: currentYear },
      { label: 'past_year', year: currentYear - 1 },
      { label: 'future_year', year: currentYear + 1 },
    ];

    for (const { label, year } of years) {
      const payload: PnLPayload = { siteIds: selectedSiteIds, year };
      const startTime = Date.now();
      pnlLoadTotal.add(1);

      try {
        const response = await client.post('/api/pnl', JSON.stringify(payload), {
          tags: {
            operation: `load_pnl_${label}`,
            step: 'forecasting_flow',
            year: year.toString(),
            siteIds: selectedSiteIds.join(','),
          }
        });

        const isSuccess = check(response, {
          [`P&L (${label}): status is 200`]: (r) => r.status === 200,
          [`P&L (${label}): contains data`]: (r) => r.body !== null && r.body !== undefined,
          [`P&L (${label}): response time < 3s`]: (r) => r.timings.duration < 3000,
        });

        pnlLoadTime.add(Date.now() - startTime, {
          status: response.status.toString(),
          year: year.toString(),
          siteIds: selectedSiteIds.join(','),
          operation: `load_pnl_${label}`
        });
        pnlLoadSuccess.add(isSuccess ? 1 : 0);

        pnlLoadError429.add(response.status === 429 ? 1 : 0);
        pnlLoadError500.add(response.status === 500 ? 1 : 0);
        if (!isSuccess) {
          pnlLoadErrorCount.add(1);
        }

      } catch (error) {
        pnlLoadSuccess.add(0);
        pnlLoadErrorCount.add(1);
        throw error;
      }
    }
  }
}