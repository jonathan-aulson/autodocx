import { getConfig } from '@/config/env.config';
import { Logger } from '@/utils/logger';
import dotenv from 'dotenv';
import fs from 'fs';
import path from 'path';
import puppeteer, { Browser, Page } from 'puppeteer';

dotenv.config();

export interface AuthCookies {
  cookieHeader: string;
  cookies: Array<{
    name: string;
    value: string;
    domain: string;
    path: string;
    expires?: number;
  }>;
  timestamp: number;
  expiresAt: number;
}

export interface BrowserAuthOptions {
  headless?: boolean;
  timeout?: number;
  cookieExpiryHours?: number;
}

export class BrowserAuthenticator {
  private readonly config = getConfig();
  private readonly logger = new Logger(true, 'browser-auth');
  private readonly options: Required<BrowserAuthOptions>;

  constructor(options: BrowserAuthOptions = {}) {
    this.options = {
      headless: options.headless ?? true,
      timeout: options.timeout ?? 30000,
      cookieExpiryHours: options.cookieExpiryHours ?? 8
    };
  }

  /**
   * Perform Microsoft OAuth authentication and extract cookies
   */
  async authenticate(): Promise<AuthCookies> {
    this.logger.info('Starting browser authentication');

    let browser: Browser | null = null;

    try {

      const baseUrl = process.env.BASE_URL || this.config.baseUrl;

      // Launch browser
      browser = await puppeteer.launch({
        headless: this.options.headless,
        args: ['--no-sandbox', '--disable-setuid-sandbox']
      });

      const page = await browser.newPage();
      await page.setDefaultTimeout(this.options.timeout);

      // Navigate to application
      this.logger.info(`Navigating to ${baseUrl}`);
      await page.goto(baseUrl, { waitUntil: 'networkidle0' });

      // Start Microsoft OAuth flow
      await this.clickSignInButton(page);
      await this.enterCredentials(page);
      await this.handleMfaAndConsent(page);

      // Extract cookies
      const authCookies = await this.extractCookies(page);

      this.logger.info('Authentication successful', {
        cookieCount: authCookies.cookies.length,
        expiresAt: new Date(authCookies.expiresAt).toISOString()
      });

      return authCookies;

    } catch (error: any) {
      this.logger.error('Browser authentication failed', { error: error.message });
      throw new Error(`Authentication failed: ${error.message}`);
    } finally {
      if (browser) {
        await browser.close();
      }
    }
  }

  /**
   * Click the Microsoft sign-in button
   */
  private async clickSignInButton(page: Page): Promise<void> {
    this.logger.debug('Clicking Microsoft sign-in button');

    try {
      await page.waitForSelector('[data-qa-id="button-signInWithMicrosoft"]', { timeout: 10000 });
      await page.click('[data-qa-id="button-signInWithMicrosoft"]');
      await page.waitForNavigation({ waitUntil: 'networkidle0' });
    } catch (error) {
      throw new Error('Failed to find or click Microsoft sign-in button');
    }
  }

  /**
   * Enter email and password credentials
   */
  private async enterCredentials(page: Page): Promise<void> {
    const email = process.env.LOGIN_EMAIL || process.env.K6_LOGIN_EMAIL;
    const password = process.env.LOGIN_PASSWORD || process.env.K6_LOGIN_PASSWORD;

    if (!email || !password) {
      throw new Error('LOGIN_EMAIL and LOGIN_PASSWORD environment variables are required');
    }

    this.logger.debug('Entering credentials');

    try {
      // Enter email
      await page.waitForSelector('#i0116', { timeout: 10000 });
      await page.type('#i0116', email);
      await page.click('#idSIButton9');

      // Wait and enter password
      await this.sleep(2000);
      await page.waitForSelector('#i0118', { timeout: 10000 });
      await page.type('#i0118', password);
      await page.click('#idSIButton9');

    } catch (error) {
      throw new Error('Failed to enter credentials');
    }
  }

  /**
   * Handle MFA and consent screens
   */
  private async handleMfaAndConsent(page: Page): Promise<void> {
    this.logger.debug('Handling MFA and consent');

    try {
      await page.waitForNavigation({ waitUntil: 'networkidle0' });
      await this.sleep(2000);

      // Handle "Stay signed in?" prompt
      const staySignedInButton = await page.$('#idSIButton9');
      if (staySignedInButton) {
        await page.click('#idSIButton9');
        await page.waitForNavigation({ waitUntil: 'networkidle0' });
      }

      // Additional wait for app to fully load
      await this.sleep(3000);

    } catch (error) {
      throw new Error('Failed to handle MFA/consent flow');
    }
  }

  /**
   * Extract cookies from the authenticated session
   */
  private async extractCookies(page: Page): Promise<AuthCookies> {
    const cookies = await page.cookies();
    const cookieHeader = cookies.map(c => `${c.name}=${c.value}`).join('; ');

    const now = Date.now();
    const expiresAt = now + (this.options.cookieExpiryHours * 60 * 60 * 1000);

    return {
      cookieHeader,
      cookies: cookies.map(c => ({
        name: c.name,
        value: c.value,
        domain: c.domain,
        path: c.path,
        expires: c.expires
      })),
      timestamp: now,
      expiresAt
    };
  }

  /**
   * Save cookies to file for K6 consumption
   */
  async saveCookiesToFile(authCookies: AuthCookies, filePath?: string): Promise<string> {
    const outputPath = filePath || path.resolve(__dirname, '../utils/auth-cookies.ts');

    const cookieFileContent = `// Auto-generated authentication cookies
// Generated at: ${new Date().toISOString()}
// Expires at: ${new Date(authCookies.expiresAt).toISOString()}

export interface AuthCookieData {
  cookieHeader: string;
  timestamp: number;
  expiresAt: number;
  isValid: boolean;
}

export const authCookies: AuthCookieData = {
  cookieHeader: \`${authCookies.cookieHeader}\`,
  timestamp: ${authCookies.timestamp},
  expiresAt: ${authCookies.expiresAt},
  isValid: Date.now() < ${authCookies.expiresAt}
};

export function getAuthHeaders(): Record<string, string> {
  if (!authCookies.isValid) {
    throw new Error('Authentication cookies have expired. Please re-authenticate.');
  }
  
  return {
    'Cookie': authCookies.cookieHeader,
    'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36',
    'Accept': 'application/json, text/plain, */*',
    'Accept-Language': 'en-US,en;q=0.9',
    'Referer': '${this.config.baseUrl}'
  };
}

export default authCookies;
`;

    fs.writeFileSync(outputPath, cookieFileContent);
    this.logger.info(`Cookies saved to ${outputPath}`);

    return outputPath;
  }

  private sleep(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }
}

/**
 * Standalone function for quick authentication
 */
export async function authenticateAndSaveCookies(options?: BrowserAuthOptions): Promise<string> {
  const authenticator = new BrowserAuthenticator(options);
  const authCookies = await authenticator.authenticate();
  return await authenticator.saveCookiesToFile(authCookies);
}