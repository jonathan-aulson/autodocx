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
const pnlMultiSitesLoadTime = new Trend('pnl_multi_sites_load_time', true);
const pnlMultiSitesLoadSuccess = new Rate('pnl_multi_sites_load_success');
const pnlMultiSitesLoadError429 = new Rate('pnl_multi_sites_load_error_429');
const pnlMultiSitesLoadError500 = new Rate('pnl_multi_sites_load_error_500');
const pnlMultiSitesLoadTotal = new Counter('pnl_multi_sites_load_total');
const pnlMultiSitesLoadErrorCount = new Counter('pnl_multi_sites_load_error_count');

export class LoadPnLMultipleSites {
  async execute(client: HttpClient): Promise<void> {
    const currentYear = new Date().getFullYear();
    const years = [
      { label: 'current', year: currentYear },
      { label: 'past', year: currentYear - 1 },
      { label: 'future', year: currentYear + 1 },
    ];

    for (const { label, year } of years) {
      const payload: PnLPayload = {
        siteIds: pnlData.filterSiteIds,
        year
      };
      const startTime = Date.now();
      pnlMultiSitesLoadTotal.add(1);

      try {
        const response = await client.post('/api/pnl', JSON.stringify(payload), {
          tags: {
            operation: `load_pnl_multiple_sites_${label}`,
            step: 'forecasting_flow',
            year: year.toString(),
            siteCount: pnlData.filterSiteIds.length.toString(),
          }
        });

        const isSuccess = check(response, {
          [`${label} year multiple sites P&L loaded successfully`]: (r) => r.status === 200,
          [`${label} year multiple sites P&L contains data`]: (r) => r.body !== null && r.body !== undefined,
          [`${label} year multiple sites P&L response time OK`]: (r) => r.timings.duration < 3000,
          [`${label} year multiple sites P&L has correct site count`]: (r) => {
            try {
              const data = JSON.parse(r.body);
              return data && Array.isArray(data.siteIds) && data.siteIds.length === pnlData.filterSiteIds.length;
            } catch {
              return false;
            }
          }
        });

        pnlMultiSitesLoadTime.add(Date.now() - startTime, {
          status: response.status.toString(),
          year: year.toString(),
          siteCount: pnlData.filterSiteIds.length.toString(),
          operation: `load_pnl_multiple_sites_${label}`
        });
        pnlMultiSitesLoadSuccess.add(isSuccess ? 1 : 0);

        pnlMultiSitesLoadError429.add(response.status === 429 ? 1 : 0);
        pnlMultiSitesLoadError500.add(response.status === 500 ? 1 : 0);
        if (!isSuccess) {
          pnlMultiSitesLoadErrorCount.add(1);
        }

      } catch (error) {
        pnlMultiSitesLoadSuccess.add(0);
        pnlMultiSitesLoadErrorCount.add(1);
        throw error;
      }
    }
  }

  // Helper for specific year
  static async executeWithYear(client: HttpClient, year: number): Promise<void> {
    const payload: PnLPayload = {
      siteIds: pnlData.filterSiteIds,
      year
    };
    const startTime = Date.now();
    pnlMultiSitesLoadTotal.add(1);

    try {
      const response = await client.post('/api/pnl', JSON.stringify(payload), {
        tags: {
          operation: 'load_pnl_multiple_sites_specific_year',
          step: 'forecasting_flow',
          year: year.toString(),
          siteCount: pnlData.filterSiteIds.length.toString(),
        }
      });

      const isSuccess = check(response, {
        [`Multiple sites P&L for year ${year} loaded successfully`]: (r) => r.status === 200,
        [`Multiple sites P&L for year ${year} contains data`]: (r) => r.body !== null && r.body !== undefined,
        [`Multiple sites P&L for year ${year} response time OK`]: (r) => r.timings.duration < 3000,
        [`Multiple sites P&L for year ${year} has correct sites`]: (r) => {
          try {
            const data = JSON.parse(r.body);
            return data &&
              Array.isArray(data.siteIds) &&
              data.siteIds.length === pnlData.filterSiteIds.length &&
              pnlData.filterSiteIds.every(siteId => data.siteIds.includes(siteId));
          } catch {
            return false;
          }
        }
      });

      pnlMultiSitesLoadTime.add(Date.now() - startTime, {
        status: response.status.toString(),
        year: year.toString(),
        siteCount: pnlData.filterSiteIds.length.toString(),
        operation: 'load_pnl_multiple_sites_specific_year'
      });
      pnlMultiSitesLoadSuccess.add(isSuccess ? 1 : 0);

      pnlMultiSitesLoadError429.add(response.status === 429 ? 1 : 0);
      pnlMultiSitesLoadError500.add(response.status === 500 ? 1 : 0);
      if (!isSuccess) {
        pnlMultiSitesLoadErrorCount.add(1);
      }

    } catch (error) {
      pnlMultiSitesLoadSuccess.add(0);
      pnlMultiSitesLoadErrorCount.add(1);
      throw error;
    }
  }
}