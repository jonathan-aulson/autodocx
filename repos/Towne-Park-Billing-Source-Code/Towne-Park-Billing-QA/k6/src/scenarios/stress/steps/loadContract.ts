import { HttpClient } from '@/core/http.client';
import { check } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';

// --- Metrics ---
const contractLoadTime = new Trend('contract_load_time', true); // track percentiles
const contractLoadSuccess = new Rate('contract_load_success');
const contractLoadError429 = new Rate('contract_load_error_429');
const contractLoadError500 = new Rate('contract_load_error_500');
const contractLoadTotal = new Counter('contract_load_total');
const contractLoadErrorCount = new Counter('contract_load_error_count');

export class LoadContract {
  // You can parameterize siteId as needed
  async execute(client: HttpClient, siteId = '59b0f1f2-2aed-ef11-be21-6045bd096814'): Promise<void> {
    const endpoint = `/api/customers/${siteId}/contract`;
    const startTime = Date.now();
    contractLoadTotal.add(1);

    try {
      const response = await client.get(endpoint, {
        tags: {
          operation: 'load_contract',
          step: 'forecasting_flow'
        }
      });

      // Rich checks for more than just 200 status
      const isSuccess = check(response, {
        'contract: status is 200': (r) => r.status === 200,
        'contract: response time < 2s': (r) => r.timings.duration < 2000,
      });

      contractLoadTime.add(Date.now() - startTime, {
        status: response.status.toString(),
        siteId: siteId,
        operation: 'load_contract'
      });
      contractLoadSuccess.add(isSuccess ? 1 : 0);

      // Add error breakdowns
      contractLoadError429.add(response.status === 429 ? 1 : 0);
      contractLoadError500.add(response.status === 500 ? 1 : 0);
      if (!isSuccess) {
        contractLoadErrorCount.add(1);
      }

    } catch (error) {
      contractLoadSuccess.add(0);
      contractLoadErrorCount.add(1);
      // Optionally: log more about the error for debugging
      // console.error('LoadContract Exception:', error);
      throw error;
    }
  }
}
