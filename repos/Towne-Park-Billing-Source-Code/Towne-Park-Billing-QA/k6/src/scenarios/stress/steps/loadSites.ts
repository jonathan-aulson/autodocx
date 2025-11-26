import { HttpClient } from '@/core/http.client';
import { check } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';

// --- Metrics ---
const sitesLoadTime = new Trend('sites_load_time', true);
const sitesLoadSuccess = new Rate('sites_load_success');
const sitesLoadError429 = new Rate('sites_load_error_429');
const sitesLoadError500 = new Rate('sites_load_error_500');
const sitesLoadTotal = new Counter('sites_load_total');
const sitesLoadErrorCount = new Counter('sites_load_error_count');

export class LoadSites {
  async execute(client: HttpClient): Promise<void> {
    const startTime = Date.now();
    sitesLoadTotal.add(1);

    try {
      const response = await client.get('/api/customers', {
        tags: {
          operation: 'load_sites',
          step: 'forecasting_flow'
        }
      });

      const isSuccess = check(response, {
        'Sites loaded successfully': (r) => r.status === 200,
        'Sites response has data': (r) => {
          try {
            const data = JSON.parse(r.body);
            return Array.isArray(data) && data.length > 0;
          } catch {
            return false;
          }
        },
        'Sites response time OK': (r) => r.timings.duration < 2000
      });

      sitesLoadTime.add(Date.now() - startTime, {
        status: response.status.toString(),
        operation: 'load_sites'
      });
      sitesLoadSuccess.add(isSuccess ? 1 : 0);

      sitesLoadError429.add(response.status === 429 ? 1 : 0);
      sitesLoadError500.add(response.status === 500 ? 1 : 0);
      if (!isSuccess) {
        sitesLoadErrorCount.add(1);
      }

    } catch (error) {
      sitesLoadSuccess.add(0);
      sitesLoadErrorCount.add(1);
      throw error;
    }
  }
}
