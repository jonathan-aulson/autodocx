import {
  ForecastData,
  StatisticsData,
  ParkingRateData,
  PayrollData,
  OtherRevenueData
} from '@/types/api.types';

export function generateSiteIds(count: number = 621): number[] {
  return Array.from({ length: count }, (_, i) => i + 1);
}

export function generateForecastData(
  originalData: StatisticsData[],
  editPercentage: number = 0.5
): StatisticsData[] {
  if (!Array.isArray(originalData)) {
    return originalData;
  }

  const editCount = Math.floor(originalData.length * editPercentage);
  const editedData = [...originalData];

  for (let i = 0; i < editCount; i++) {
    const randomIndex = Math.floor(Math.random() * editedData.length);
    const item = editedData[randomIndex];

    // Simulate realistic data changes
    item.value = item.value * (0.8 + Math.random() * 0.4); // ±20% variation
    item.lastModified = new Date().toISOString();
    item.modifiedBy = 'k6-test-user';
  }

  return editedData;
}

export function generateParkingRateData(count: number = 5): ParkingRateData[] {
  const rateTypes = ['hourly', 'daily', 'monthly', 'vip', 'reserved'];

  return Array.from({ length: count }, (_, i) => ({
    rateType: rateTypes[i % rateTypes.length],
    amount: Math.round((Math.random() * 50 + 10) * 100) / 100,
    effectiveDate: new Date().toISOString().split('T')[0],
    endDate: undefined
  }));
}

export function generatePayrollData(count: number = 10): PayrollData[] {
  const positions = ['Manager', 'Attendant', 'Supervisor', 'Cashier', 'Security'];

  return Array.from({ length: count }, (_, i) => ({
    employeeId: `EMP${String(i + 1).padStart(3, '0')}`,
    position: positions[i % positions.length],
    hours: Math.floor(Math.random() * 40) + 10,
    rate: Math.round((Math.random() * 20 + 15) * 100) / 100,
    date: new Date().toISOString().split('T')[0]
  }));
}

export function generateOtherRevenueData(count: number = 8): OtherRevenueData[] {
  const sources = ['Vending', 'Car Wash', 'Electric Charging', 'Advertising'];
  const categories = ['Equipment', 'Services', 'Retail', 'Digital'];

  return Array.from({ length: count }, (_, i) => ({
    source: sources[i % sources.length],
    amount: Math.round((Math.random() * 500 + 100) * 100) / 100,
    date: new Date().toISOString().split('T')[0],
    category: categories[i % categories.length]
  }));
}

export function generateInvoiceLineItems(count: number = 5) {
  return Array.from({ length: count }, (_, i) => ({
    id: `line_${i + 1}`,
    description: `Line item ${i + 1} - ${Math.random().toString(36).substring(7)}`,
    amount: Math.round((Math.random() * 1000 + 100) * 100) / 100,
    quantity: Math.floor(Math.random() * 10) + 1,
    category: ['Service', 'Equipment', 'Fee', 'Tax'][i % 4]
  }));
}

export class TestDataGenerator {
  private static instance: TestDataGenerator;

  private constructor() { }

  static getInstance(): TestDataGenerator {
    if (!TestDataGenerator.instance) {
      TestDataGenerator.instance = new TestDataGenerator();
    }
    return TestDataGenerator.instance;
  }

  generateRealisticForecastEdits(
    originalData: StatisticsData[],
    editPattern: 'light' | 'moderate' | 'heavy' = 'moderate'
  ): StatisticsData[] {
    const editPercentages = {
      light: 0.2,      // 20%
      moderate: 0.5,   // 50%
      heavy: 0.8       // 80%
    };

    return generateForecastData(originalData, editPercentages[editPattern]);
  }

  generateBulkSiteSelection(userRole: string, maxSites: number = 50): number[] {
    const roleSiteLimits = {
      'account-manager': 1,
      'district-manager': Math.min(maxSites, 50),
      'vp': Math.min(maxSites, 300),
      'c-level': maxSites
    };

    const siteCount = roleSiteLimits[userRole as keyof typeof roleSiteLimits] || 1;
    return generateSiteIds(siteCount);
  }

  generateRandomTimeRange(): { startDate: string; endDate: string } {
    const now = new Date();
    const startDate = new Date(now.getFullYear(), now.getMonth() - 1, 1);
    const endDate = new Date(now.getFullYear(), now.getMonth(), 0);

    return {
      startDate: startDate.toISOString().split('T')[0],
      endDate: endDate.toISOString().split('T')[0]
    };
  }
}