#!/usr/bin/env ts-node

import { execSync } from 'child_process';
import { existsSync, readFileSync } from 'fs';
import { join, resolve } from 'path';

interface RunConfig {
  envFile: string;
  scenario?: string;
  testType?: string;
  loadLevel?: string;
  outputFormat?: string;
}

class K6Runner {
  private readonly config: RunConfig;
  private readonly k6Dir: string;

  constructor(config: RunConfig) {
    this.config = config;
    this.k6Dir = __dirname; // Current directory is k6/
  }

  private loadEnvironmentVariables(): Record<string, string> {
    const envPath = resolve(this.k6Dir, this.config.envFile);

    if (!existsSync(envPath)) {
      throw new Error(`Environment file not found: ${envPath}`);
    }

    const envContent = readFileSync(envPath, 'utf-8');
    const envVars: Record<string, string> = {};

    envContent.split('\n').forEach(line => {
      const trimmedLine = line.trim();
      if (trimmedLine && !trimmedLine.startsWith('#')) {
        const [key, ...valueParts] = trimmedLine.split('=');
        if (key && valueParts.length > 0) {
          envVars[key.trim()] = valueParts.join('=').trim();
        }
      }
    });

    return envVars;
  }

  private buildK6Command(): string {
    const envVars = this.loadEnvironmentVariables();

    // Override with CLI parameters
    if (this.config.scenario) {
      envVars.SCENARIO = this.config.scenario;
    }
    if (this.config.testType) {
      envVars.TEST_TYPE = this.config.testType;
    }
    if (this.config.loadLevel) {
      envVars.LOAD_LEVEL = this.config.loadLevel;
    }

    // Build environment variables string
    const envString = Object.entries(envVars)
      .map(([key, value]) => `-e ${key}="${value}"`)
      .join(' ');

    // Determine test file based on scenario
    const scenario = envVars.SCENARIO || 'stress';
    let testFile: string;

    switch (scenario) {
      case 'stress':
        testFile = join(this.k6Dir, 'dist', 'stress.bundle.js');
        break;
      case 'spike':
        testFile = join(this.k6Dir, 'dist', 'spike.bundle.js');
        break;
      case 'soak':
        testFile = join(this.k6Dir, 'dist', 'soak.bundle.js');
        break;
      case 'forecast-visble':
        testFile = join(this.k6Dir, 'dist', 'forecast.bundle.js');
        break;
      case 'customer-load':
        testFile = join(this.k6Dir, 'dist', 'customer.bundle.js');
        break;
      default:
        testFile = join(this.k6Dir, 'dist', 'main.bundle.js');
    }

    // Ensure reports directory exists
    const reportsDir = join(this.k6Dir, 'reports');
    if (!existsSync(reportsDir)) {
      execSync(`mkdir -p "${reportsDir}"`);
    }

    // Output options
    const outputFormat = this.config.outputFormat || envVars.REPORT_FORMAT || 'html';
    const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
    const testName = envVars.TEST_TYPE || 'test';
    const outputFile = join(reportsDir, `${scenario}-${envVars.TEST_TYPE || 'test'}-${timestamp}.${outputFormat}`);

    let outputOption = '';
    let k6WebDashboardExport = '';
    switch (outputFormat) {
      case 'dashboard':
        // K6 experimental web dashboard
        const htmlReportFile = join(reportsDir, `${scenario}-${testName}-${timestamp}.html`);
        k6WebDashboardExport = `K6_WEB_DASHBOARD_EXPORT="${htmlReportFile}"`;
        outputOption = `--out web-dashboard`;
        console.log(`📊 Web dashboard will export HTML report to: ${htmlReportFile}`);
        break;
      case 'web':
        // K6 web dashboard (newer approach)
        outputOption = `--out web-dashboard`;
        break;
      case 'html':
        const htmlFile = join(this.k6Dir, 'reports', `${scenario}-${timestamp}.html`);
        outputOption = `--out json=${htmlFile.replace('.html', '.json')}`;
        break;
      case 'json':
        outputOption = `--out json="${outputFile}"`;
        break;
      case 'html-onlu':
        outputOption = ''; // No K6 output, rely on handleSummary
        console.log(`📄 Custom HTML report will be generated via handleSummary`);
        break;
      case 'grafana':
        // InfluxDB output for Grafana
        outputOption = `--out influxdb=http://localhost:8086/k6`;
        break;
    }

    return `k6 run ${envString} ${outputOption} "${testFile}"`;
  }

  async run(): Promise<void> {
    console.log('🚀 Starting K6 Performance Test...');
    console.log(`📁 Environment: ${this.config.envFile}`);
    console.log(`📂 K6 Directory: ${this.k6Dir}`);

    try {
      // Change to k6 directory for build
      process.chdir(this.k6Dir);

      // Ensure build is up to date
      console.log('📦 Building TypeScript...');
      execSync('npm run k6:build', { stdio: 'inherit' });

      const command = this.buildK6Command();
      console.log(`🎯 Executing: ${command}`);
      console.log('⏳ Running test...\n');

      execSync(command, { stdio: 'inherit' });

      console.log('\n✅ Test completed successfully!');
      console.log('📊 Check the reports/ directory for detailed results.');

    } catch (error: any) {
      console.error('\n❌ Test execution failed:', error.message);
      process.exit(1);
    }
  }
}

// CLI Interface
function parseArguments(): RunConfig {
  const args = process.argv.slice(2);

  if (args.length === 0) {
    console.error('Usage: ts-node run-k6.ts <env-file> [options]');
    console.error('');
    console.error('Examples:');
    console.error('  ts-node run-k6.ts env/dev.env');
    console.error('  ts-node run-k6.ts env/qa.env --scenario stress --test-type powerbill-statement');
    console.error('');
    console.error('Options:');
    console.error('  --scenario <stress|spike|soak>');
    console.error('  --test-type <test-type>');
    console.error('  --load-level <very-low|low|medium|high|peak>');
    console.error('  --output <html|json|junit>');
    process.exit(1);
  }

  const config: RunConfig = {
    envFile: args[0]
  };

  for (let i = 1; i < args.length; i += 2) {
    const flag = args[i];
    const value = args[i + 1];

    switch (flag) {
      case '--scenario':
        config.scenario = value;
        break;
      case '--test-type':
        config.testType = value;
        break;
      case '--load-level':
        config.loadLevel = value;
        break;
      case '--output':
        config.outputFormat = value;
        break;
      default:
        console.warn(`Unknown flag: ${flag}`);
    }
  }

  return config;
}

// Main execution
if (require.main === module) {
  const config = parseArguments();
  const runner = new K6Runner(config);
  runner.run().catch(console.error);
}

export { K6Runner };

