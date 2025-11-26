import { HttpClient } from '@/core/http.client';
import { check } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';

const customerSitesLoadTime = new Trend('customer_sites_load_time', true);
const customerSitesLoadSuccess = new Rate('customer_sites_load_success');
const customerSitesLoadError429 = new Rate('customer_sites_load_error_429');
const customerSitesLoadError500 = new Rate('customer_sites_load_error_500');
const customerSitesLoadTotal = new Counter('customer_sites_load_total');
const customerSitesLoadErrorCount = new Counter('customer_sites_load_error_count');

export class LoadCustomerSites {
  private readonly endpoint = '/api/customers'; // use relative endpoint for consistency

  async execute(client: HttpClient): Promise<void> {
    const startTime = Date.now();
    customerSitesLoadTotal.add(1);

    try {
      const response = await client.get(this.endpoint, {
        tags: {
          operation: 'load_customers',
          step: 'customer_sites_load'
        }
      });

      const isSuccess = check(response, {
        'customer sites: status is 200': (r) => r.status === 200,
        'customer sites: response time < 2s': (r) => r.timings.duration < 2000,
      });

      customerSitesLoadTime.add(Date.now() - startTime, {
        status: response.status.toString(),
        operation: 'load_customers'
      });
      customerSitesLoadSuccess.add(isSuccess ? 1 : 0);

      customerSitesLoadError429.add(response.status === 429 ? 1 : 0);
      customerSitesLoadError500.add(response.status === 500 ? 1 : 0);
      if (!isSuccess) {
        customerSitesLoadErrorCount.add(1);
      }

    } catch (error) {
      customerSitesLoadSuccess.add(0);
      customerSitesLoadErrorCount.add(1);
      throw error;
    }
  }
}
