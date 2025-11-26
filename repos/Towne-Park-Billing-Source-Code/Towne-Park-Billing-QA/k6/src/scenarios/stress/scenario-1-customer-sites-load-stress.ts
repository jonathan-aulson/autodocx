import { getConfig } from '@/config/env.config';
import { BaseTest } from '@/core/base.test';
import { LoadCustomerSites } from './steps/loadCustomerSites';

const config = getConfig();

export class CustomerSitesLoadStress extends BaseTest {
  constructor() {
    super(config);
  }

  async execute(): Promise<void> {
    const startTime = Date.now();
    this.logTestStart('Customer Sites Load Stress');

    try {
      const client = this.getAuthenticatedClient();
      await this.executeStep('Load Customer Sites', async () => {
        await new LoadCustomerSites().execute(client);
      });

      this.logTestEnd('Customer Sites Load Stress', Date.now() - startTime);

    } catch (error) {
      this.context.logger.error('Customer Sites Load Stress failed', { error: (error as Error).message });
      throw error;
    }
  }

  // Make stepFunction async for await compatibility
  private async executeStep(stepName: string, stepFunction: () => Promise<void>): Promise<void> {
    this.logStep(`Starting: ${stepName}`);
    await stepFunction();
    this.logStep(`Completed: ${stepName}`);
    this.randomSleep(0.5, 1.5);
  }
}

// K6 expects an exported default function (entry point)
export default function () {
  // If you want to await (for multiple steps), must use async and K6 v0.48+ with --compatibility-mode=base
  // But for a single step like this, you can run synchronously:
  const test = new CustomerSitesLoadStress();
  // If K6 supports async/await entry, you could use: await test.execute();
  // But for now, just run as sync wrapper:
  test.execute();
}

// Or, if your runner is async and you want to use with K6:
export async function customerSitesLoadStress(): Promise<void> {
  const test = new CustomerSitesLoadStress();
  await test.execute();
}
