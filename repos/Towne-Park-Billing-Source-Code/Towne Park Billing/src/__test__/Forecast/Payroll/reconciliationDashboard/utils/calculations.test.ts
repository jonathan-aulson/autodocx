import { calculateReconciliationData } from '@/components/Forecast/Payroll/reconciliationDashboard/utils/calculations';
import { PayrollData } from '@/components/Forecast/Payroll/reconciliationDashboard/types';

describe('calculateReconciliationData', () => {
  const mockData: PayrollData[] = [
    {
      date: new Date('2024-01-01'),
      jobs: {
        'Bell': { hours: 40, budget: 800, actual: 850, forecast: 820 },
        'Valet': { hours: 35, budget: 700, actual: null, forecast: 720 }
      }
    },
    {
      date: new Date('2024-01-02'),
      jobs: {
        'Bell': { hours: 45, budget: 900, actual: 875, forecast: 890 },
        'Valet': { hours: 30, budget: 600, actual: 580, forecast: 610 }
      }
    }
  ];

  it('should calculate variance correctly for jobs with actual data', () => {
    const result = calculateReconciliationData(mockData, 'all', 'monthly');
    const bellResult = result.find(item => item.jobCode === 'Bell');
    
    expect(bellResult).toBeDefined();
    expect(bellResult!.variance).toBe(25); // (850 + 875) - (800 + 900) = 1725 - 1700 = 25
    expect(bellResult!.actual).toBe(1725); // 850 + 875
    expect(bellResult!.budget).toBe(1700); // 800 + 900
  });

  it('should handle jobs without actual data', () => {
    const result = calculateReconciliationData(mockData, 'all', 'monthly');
    const valetResult = result.find(item => item.jobCode === 'Valet');
    
    expect(valetResult).toBeDefined();
    expect(valetResult!.actual).toBe(580); // Only second day has actual data
    expect(valetResult!.variance).toBe(-720); // 580 - 1300 = -720
  });

  it('should handle job filter correctly', () => {
    const result = calculateReconciliationData(mockData, 'Bell', 'monthly');
    
    expect(result).toHaveLength(1);
    expect(result[0].jobCode).toBe('Bell');
  });

  it('should calculate totals correctly', () => {
    const result = calculateReconciliationData(mockData, 'Bell', 'monthly');
    const bellResult = result[0];
    
    expect(bellResult.scheduled).toBe(85); // 40 + 45 hours
    expect(bellResult.forecast).toBe(1710); // 820 + 890
    expect(bellResult.budget).toBe(1700); // 800 + 900
    expect(bellResult.actual).toBe(1725); // 850 + 875
  });

  it('should handle empty data', () => {
    const result = calculateReconciliationData([], 'all', 'monthly');
    
    expect(result).toEqual([]);
  });

  it('should calculate variance percentage correctly', () => {
    const result = calculateReconciliationData(mockData, 'Bell', 'monthly');
    const bellResult = result[0];
    
    // Variance: 25, Budget: 1700, Percentage: (25/1700) * 100 = 1.47%
    expect(bellResult.variancePercentage).toBeCloseTo(1.47, 2);
  });
});