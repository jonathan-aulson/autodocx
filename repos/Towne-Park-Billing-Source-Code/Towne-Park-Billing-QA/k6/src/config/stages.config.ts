import { LoadLevel, ScenarioType } from '@/types';

// Each stage type should look like: { duration: string; target: number }
export type StagePattern = { duration: string; target: number };

const stressStages = {
  veryLow: [
    { duration: '2m', target: 10 },
    { duration: '5m', target: 10 }
  ],
  low: [
    { duration: '2m', target: 50 },
    { duration: '10m', target: 50 }
  ],
  medium: [
    { duration: '3m', target: 100 },
    { duration: '15m', target: 100 }
  ],
  high: [
    { duration: '5m', target: 500 },
    { duration: '20m', target: 500 }
  ],
  peak: [
    { duration: '10m', target: 1000 },
    { duration: '30m', target: 1000 },
    { duration: '5m', target: 0 }
  ]
};

const spikeStages = {
  suddenSpike: [
    { duration: '2m', target: 50 },
    { duration: '30s', target: 500 },
    { duration: '5m', target: 500 },
    { duration: '2m', target: 50 },
    { duration: '2m', target: 0 }
  ],
  massiveSpike: [
    { duration: '1m', target: 10 },
    { duration: '10s', target: 500 },
    { duration: '3m', target: 500 },
    { duration: '1m', target: 10 }
  ]
};

const soakStages = {
  shortSoak: [
    { duration: '5m', target: 100 },
    { duration: '30m', target: 100 },
    { duration: '5m', target: 0 }
  ],
  longSoak: [
    { duration: '10m', target: 300 },
    { duration: '60m', target: 300 },
    { duration: '10m', target: 0 }
  ]
};

export function getStagesByLevel(
  loadLevel: LoadLevel,
  scenario: ScenarioType
): StagePattern[] {
  switch (scenario) {
    case 'stress':
      return (stressStages as any)[
        loadLevelToKey(loadLevel)
      ] as StagePattern[];
    case 'spike':
      // Use 'suddenSpike' for all or switch by loadLevel if needed
      return spikeStages.suddenSpike;
    case 'soak':
      // Use 'shortSoak' for low/medium, 'longSoak' for high/peak
      return ['high', 'peak'].includes(loadLevel)
        ? soakStages.longSoak
        : soakStages.shortSoak;
    default:
      return [];
  }
}

// Helper: Maps 'very-low' => 'veryLow', etc.
function loadLevelToKey(loadLevel: LoadLevel): string {
  return loadLevel
    .replace(/-(.)/g, (_, char) => char.toUpperCase()) // kebab to camel
    .replace(/^(.)/, (m) => m.toLowerCase());
}

export { soakStages, spikeStages, stressStages };


