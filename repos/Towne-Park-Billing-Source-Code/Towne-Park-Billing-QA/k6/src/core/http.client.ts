import { K6Check, K6Response } from '@/types';
import { Logger } from '@/utils/logger';
import { check } from 'k6';
import http from 'k6/http';

export interface HttpClientOptions {
  timeout?: string;
  defaultHeaders?: Record<string, string>;
  baseUrl?: string;
  retryAttempts?: number;
  retryDelay?: number;
}

export interface RequestOptions {
  headers?: Record<string, string>;
  timeout?: string;
  params?: Record<string, any>;
  tags?: Record<string, string>;
  validateResponse?: boolean;
}

export class HttpClient {
  private readonly baseUrl: string;
  private readonly defaultHeaders: Record<string, string>;
  private readonly timeout: string;
  private readonly retryAttempts: number;
  private readonly retryDelay: number;
  private readonly logger: Logger;

  constructor(options: HttpClientOptions = {}, logger: Logger) {
    this.baseUrl = options.baseUrl || '';
    this.defaultHeaders = {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
      'User-Agent': 'k6-performance-test',
      ...options.defaultHeaders
    };
    this.timeout = options.timeout || '30s';
    this.retryAttempts = options.retryAttempts || 0;
    this.retryDelay = options.retryDelay || 1000;
    this.logger = logger;
  }

  private buildUrl(endpoint: string): string {
    if (endpoint.startsWith('http')) {
      return endpoint;
    }
    return `${this.baseUrl}${endpoint.startsWith('/') ? endpoint : '/' + endpoint}`;
  }

  private mergeHeaders(options: RequestOptions = {}): Record<string, string> {
    return { ...this.defaultHeaders, ...options.headers };
  }

  private validateResponse(
    response: K6Response,
    method: string,
    endpoint: string,
    customChecks: K6Check = {}
  ): boolean {
    const defaultChecks: K6Check = {
      [`${method} ${endpoint} - status is 2xx`]: (r) => r.status >= 200 && r.status < 300,
      [`${method} ${endpoint} - response time < 5s`]: (r) => r.timings.duration < 5000,
      [`${method} ${endpoint} - has response body`]: (r) => r.body.length > 0,
    };

    const allChecks = { ...defaultChecks, ...customChecks };
    const result = check(response, allChecks);

    if (!result) {
      this.logger.error(`Request validation failed: ${method} ${endpoint}`, {
        status: response.status,
        responseTime: response.timings.duration,
        bodyLength: response.body.length
      });
    }

    return result;
  }

  private async executeRequest(
    method: string,
    endpoint: string,
    payload: any = null,
    options: RequestOptions = {}
  ): Promise<K6Response> {
    const url = this.buildUrl(endpoint);
    const headers = this.mergeHeaders(options);
    const timeout = options.timeout || this.timeout;

    const requestParams = {
      headers,
      timeout,
      tags: options.tags,
      ...options.params
    };

    this.logger.debug(`${method} ${url}`, {
      payload: payload ? 'Present' : 'None',
      headers: Object.keys(headers)
    });

    let lastError: Error | null = null;

    for (let attempt = 0; attempt <= this.retryAttempts; attempt++) {
      try {
        const response = http.request(method, url, payload, requestParams) as K6Response;

        if (options.validateResponse !== false) {
          this.validateResponse(response, method, endpoint);
        }

        if (response.status >= 500 && attempt < this.retryAttempts) {
          this.logger.warn(`Server error on attempt ${attempt + 1}, retrying...`, {
            status: response.status,
            url
          });
          continue;
        }

        return response;
      } catch (error) {
        lastError = error as Error;
        if (attempt < this.retryAttempts) {
          this.logger.warn(`Request failed on attempt ${attempt + 1}, retrying...`, {
            error: error instanceof Error ? error.message : String(error),
            url
          });
          // Simple delay implementation
          const start = Date.now();
          while (Date.now() - start < this.retryDelay) {
            // Busy wait (k6 doesn't have native sleep in setup/teardown)
          }
        }
      }
    }

    throw lastError || new Error(`Request failed after ${this.retryAttempts + 1} attempts`);
  }

  async get(endpoint: string, options: RequestOptions = {}): Promise<K6Response> {
    return this.executeRequest('GET', endpoint, null, options);
  }

  async post<T = any>(endpoint: string, payload: T, options: RequestOptions = {}): Promise<K6Response> {
    const body = typeof payload === 'object' ? JSON.stringify(payload) : payload;
    return this.executeRequest('POST', endpoint, body, options);
  }

  async put<T = any>(endpoint: string, payload: T, options: RequestOptions = {}): Promise<K6Response> {
    const body = typeof payload === 'object' ? JSON.stringify(payload) : payload;
    return this.executeRequest('PUT', endpoint, body, options);
  }

  async patch<T = any>(endpoint: string, payload: T, options: RequestOptions = {}): Promise<K6Response> {
    const body = typeof payload === 'object' ? JSON.stringify(payload) : payload;
    return this.executeRequest('PATCH', endpoint, body, options);
  }

  async delete(endpoint: string, options: RequestOptions = {}): Promise<K6Response> {
    return this.executeRequest('DELETE', endpoint, null, options);
  }

  // Utility methods for common patterns
  async getJson<T = any>(endpoint: string, options: RequestOptions = {}): Promise<T> {
    const response = await this.get(endpoint, options);
    try {
      return JSON.parse(response.body) as T;
    } catch (error) {

      throw new Error(`Failed to parse JSON response: ${error instanceof Error ? error.message : String(error)}`);
    }
  }

  async postJson<TRequest = any, TResponse = any>(
    endpoint: string,
    payload: TRequest,
    options: RequestOptions = {}
  ): Promise<TResponse> {
    const response = await this.post(endpoint, payload, options);
    try {
      return JSON.parse(response.body) as TResponse;
    } catch (error) {
      throw new Error(`Failed to parse JSON response: ${error instanceof Error ? error.message : String(error)}`);
    }
  }
}
