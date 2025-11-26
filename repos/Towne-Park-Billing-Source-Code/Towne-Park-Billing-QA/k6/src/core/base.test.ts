import { TestContext } from '@/types';
import { TestConfig } from '@/types/config.types';
import { getAuthHeaders } from '@/utils/auth-cookies';
import { Logger } from '@/utils/logger';
import { sleep } from 'k6';
import { HttpClient } from './http.client';

export abstract class BaseTest {
  protected readonly config: TestConfig;
  protected readonly context: TestContext;

  constructor(config: TestConfig) {
    this.config = config;
    this.context = this.initializeContext();
  }

  private initializeContext(): TestContext {
    const logger = new Logger(this.config.enableDetailedLogs);

    // Get auth headers directly from cookies
    const authHeaders = getAuthHeaders();

    const httpClient = new HttpClient({
      baseUrl: this.config.baseUrl,
      timeout: this.config.timeout,
      defaultHeaders: authHeaders
    }, logger);

    return {
      config: this.config,
      httpClient,
      logger
    };
  }

  abstract execute(): Promise<void> | void;

  protected getAuthenticatedClient(): HttpClient {
    return this.context.httpClient;
  }

  protected randomSleep(min: number = 1, max: number = 3): void {
    const sleepTime = Math.random() * (max - min) + min;
    sleep(sleepTime);
  }

  protected getRandomElement<T>(array: T[]): T {
    return array[Math.floor(Math.random() * array.length)];
  }

  protected logTestStart(testName: string): void {
    this.context.logger.info(`Starting test: ${testName}`, {
      scenario: this.config.scenario,
      loadLevel: this.config.loadLevel,
      environment: this.config.environment
    });
  }

  protected logTestEnd(testName: string, duration?: number): void {
    this.context.logger.info(`Completed test: ${testName}`, {
      duration: duration ? `${duration}ms` : 'unknown'
    });
  }

  protected logStep(step: string, data?: any): void {
    this.context.logger.info(`Step: ${step}`, data);
  }
}
