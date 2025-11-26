import { PayrollDto, JobGroupForecastDto } from '@/lib/models/Payroll';
import { JobMapping } from '../types';

/**
 * Get month boundaries for date constraint (CRITICAL for single-month API)
 * Creates dates in UTC to work properly with test data
 */
export function getMonthBoundaries(billingPeriod: string): {start: Date, end: Date} {
  const [year, month] = billingPeriod.split('-');
  const yearNum = parseInt(year);
  const monthNum = parseInt(month);
  
  // Create dates in UTC to match test data format
  // Start: First day of month at midnight UTC
  const startDate = new Date(yearNum, monthNum - 1 , 1);
  
  // End: Last day of month at start of day UTC
  // Use day 0 of the NEXT month index to get last day of the CURRENT month.
  // month is 1-based in the input; Date.UTC month is 0-based.
  // Example: for June (monthNum=6), Date.UTC(year, 6, 0) => last day of June
  const endDate = new Date(yearNum, monthNum, 0);
  
  return { start: startDate, end: endDate };
}

/**
 * Extract all unique job group names from PayrollDto
 */
export function getAllJobGroups(payrollDto: PayrollDto): string[] {
  const groups = new Set<string>();
  
  // Collect job group names from all arrays
  payrollDto.forecastPayroll?.forEach(g => g.jobGroupName && groups.add(g.jobGroupName));
  payrollDto.budgetPayroll?.forEach(g => g.jobGroupName && groups.add(g.jobGroupName));
  payrollDto.actualPayroll?.forEach(g => g.jobGroupName && groups.add(g.jobGroupName));
  payrollDto.scheduledPayroll?.forEach(g => g.jobGroupName && groups.add(g.jobGroupName));
  
  return Array.from(groups).sort(); // Alphabetical order
}

/**
 * Get job codes within a specific job group
 */
export function getJobsByGroup(groupName: string, payrollDto: PayrollDto): JobMapping[] {
  // Find the group in forecast data (most likely to have complete job codes)
  const forecastGroup = payrollDto.forecastPayroll?.find(g => g.jobGroupName === groupName);
  
  if (!forecastGroup?.jobCodes) {
    return [];
  }
  
  return forecastGroup.jobCodes.map(code => ({
    jobCode: code.jobCode || '',
    displayName: code.displayName || code.jobCode || "Unknown Job Title/Job Code",
    hourlyRate: calculateHourlyRateFromCode(code)
  }));
}

/**
 * Calculate hourly rate from job code data (if available)
 */
function calculateHourlyRateFromCode(code: any): number {
  // If API provides both cost and hours, calculate rate
  if (code.forecastPayrollCost && code.forecastHours && code.forecastHours > 0) {
    return code.forecastPayrollCost / code.forecastHours;
  }
  
  // Return 0 when data is insufficient to avoid misleading calculations
  return 0;
}