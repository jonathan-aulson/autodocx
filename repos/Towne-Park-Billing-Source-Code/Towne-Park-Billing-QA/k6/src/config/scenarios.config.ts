import { LoadLevel, ScenarioType, TestType } from '@/types';
import { getStagesByLevel, StagePattern } from './stages.config';
import { getThresholds } from './thresholds';

// Scenario configuration interface
export interface ScenarioConfig {
  executor: string;
  stages: StagePattern[];
  gracefulRampDown: string;
  thresholds: Record<string, string[]>;
  setupTimeout?: string;
  teardownTimeout?: string;
}

export interface ScenarioOptions {
  scenario: ScenarioType;
  testType?: TestType;
  loadLevel: LoadLevel;
}

// Main config builder function (all params typed)
export function getScenarioConfig(options: ScenarioOptions): ScenarioConfig {
  const { scenario, testType, loadLevel } = options;

  // Get stages based on load level and scenario type
  const stages = getStagesByLevel(loadLevel, scenario);

  // Get thresholds based on scenario and test type
  const thresholds = getThresholds(scenario, testType);

  const baseConfigs: Record<ScenarioType, Omit<ScenarioConfig, 'stages' | 'thresholds'>> = {
    stress: {
      executor: 'ramping-vus',
      gracefulRampDown: '30s',
      setupTimeout: '60s',
      teardownTimeout: '30s'
    },
    spike: {
      executor: 'ramping-vus',
      gracefulRampDown: '10s',
      setupTimeout: '30s',
      teardownTimeout: '15s'
    },
    soak: {
      executor: 'ramping-vus',
      gracefulRampDown: '2m',
      setupTimeout: '120s',
      teardownTimeout: '60s'
    },
    'forecast-visble': {
      executor: 'constant-vus',
      gracefulRampDown: '1m',
      setupTimeout: '90s',
      teardownTimeout: '45s'
    },
    'customer-load': {
      executor: 'constant-vus',
      gracefulRampDown: '1m',
      setupTimeout: '90s',
      teardownTimeout: '45s'
    },
  };

  const baseConfig = baseConfigs[scenario] || baseConfigs.stress;

  return {
    ...baseConfig,
    stages,
    thresholds
  };
}

// Convenience functions
export function getStressConfig(loadLevel: LoadLevel, testType?: TestType): ScenarioConfig {
  return getScenarioConfig({ scenario: 'stress', testType, loadLevel });
}

export function getSpikeConfig(loadLevel: LoadLevel, testType?: TestType): ScenarioConfig {
  return getScenarioConfig({ scenario: 'spike', testType, loadLevel });
}

export function getSoakConfig(loadLevel: LoadLevel, testType?: TestType): ScenarioConfig {
  return getScenarioConfig({ scenario: 'soak', testType, loadLevel });
}

// Advanced scenario configuration with custom overrides
export function getCustomScenarioConfig(
  options: ScenarioOptions,
  overrides: Partial<ScenarioConfig> = {}
): ScenarioConfig {
  const baseConfig = getScenarioConfig(options);

  return {
    ...baseConfig,
    ...overrides,
    thresholds: {
      ...baseConfig.thresholds,
      ...overrides.thresholds
    }
  };
}

// Multi-scenario configuration for complex tests
export interface MultiScenarioConfig {
  [scenarioName: string]: ScenarioConfig;
}

export function getMultiScenarioConfig(
  scenarios: Array<{ name: string; options: ScenarioOptions }>
): MultiScenarioConfig {
  const config: MultiScenarioConfig = {};

  scenarios.forEach(({ name, options }) => {
    config[name] = getScenarioConfig(options);
  });

  return config;
}

// Validation function
export function validateScenarioConfig(config: ScenarioConfig): boolean {
  const requiredFields: (keyof ScenarioConfig)[] = ['executor', 'stages', 'gracefulRampDown', 'thresholds'];
  return requiredFields.every(field => config[field] !== undefined);
}