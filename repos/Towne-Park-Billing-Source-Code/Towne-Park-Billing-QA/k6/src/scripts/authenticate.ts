#!/usr/bin/env ts-node

import { BrowserAuthenticator } from '@/auth/browser-auth';
import { Logger } from '@/utils/logger';

interface AuthOptions {
  headless?: boolean;
  force?: boolean;
}

async function main() {
  const args = process.argv.slice(2);
  const options: AuthOptions = {};

  // Parse command line arguments
  args.forEach((arg, index) => {
    switch (arg) {
      case '--no-headless':
        options.headless = false;
        break;
      case '--force':
        options.force = true;
        break;
    }
  });

  const logger = new Logger(true, 'auth-script');

  try {
    logger.info('🚀 Starting browser authentication...');

    const authenticator = new BrowserAuthenticator({
      headless: options.headless ?? true,
      timeout: 60000, // 60 seconds
      cookieExpiryHours: 8
    });

    const authCookies = await authenticator.authenticate();
    const cookieFilePath = await authenticator.saveCookiesToFile(authCookies);

    logger.info('✅ Authentication completed successfully!');
    logger.info(`📁 Cookies saved to: ${cookieFilePath}`);
    logger.info(`⏰ Cookies expire at: ${new Date(authCookies.expiresAt).toLocaleString()}`);

  } catch (error: any) {
    logger.error('❌ Authentication failed:', { error: error.message });
    process.exit(1);
  }
}

if (require.main === module) {
  main().catch(console.error);
}
