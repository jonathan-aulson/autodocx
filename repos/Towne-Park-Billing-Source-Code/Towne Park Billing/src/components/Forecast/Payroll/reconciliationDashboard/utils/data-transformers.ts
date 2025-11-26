import { PayrollDto, PayrollDetailDto } from '@/lib/models/Payroll';
import { JobCode } from '@/lib/models/jobCode';
import { PayrollData } from '../types';
import { LivePayrollData } from '../hooks/usePayrollReconciliation';

/**
 * Transform PayrollDto from API to PayrollData array expected by component
 */
export function transformPayrollDtoToReconciliationData(payrollDto: PayrollDto): PayrollData[] {
  // Extract all unique dates from API data
  const dates = extractUniqueDates(payrollDto);
  
  return dates.map(date => ({
    date,
    jobs: buildJobsForDate(date, payrollDto)
  }));
}

/**
 * Extract all unique dates from nested PayrollDto structure
 * Creates dates consistently to avoid timezone issues
 */
function extractUniqueDates(payrollDto: PayrollDto): Date[] {
  const dateStrings = new Set<string>();
  
  // Collect all dates from all job group arrays
  const allGroups = [
    ...(payrollDto.forecastPayroll || []),
    ...(payrollDto.budgetPayroll || []),
    ...(payrollDto.actualPayroll || []),
    ...(payrollDto.scheduledPayroll || [])
  ];
  
  allGroups.forEach(group => {
    if (group.date) {
      // Normalize date string to ensure consistent parsing
      const dateStr = group.date.toString();
      if (dateStr.includes('T')) {
        // If it has time, take just the date part
        dateStrings.add(dateStr.split('T')[0]);
      } else {
        dateStrings.add(dateStr);
      }
    }
  });
  
  return Array.from(dateStrings)
    .map(dateStr => {
      // Create date in local timezone to match boundary calculations
      const [year, month, day] = dateStr.split('-').map(Number);
      return new Date(year, month - 1, day);
    })
    .sort((a, b) => a.getTime() - b.getTime());
}

/**
 * Build jobs object for a specific date by merging data from all sources
 */
function buildJobsForDate(targetDate: Date, payrollDto: PayrollDto): Record<string, any> {
  const jobs: Record<string, any> = {};
  
  // Get all job groups that exist in the data
  const allJobGroups = getAllJobGroupsFromDto(payrollDto);
  
  allJobGroups.forEach(jobGroupName => {
    // Find data for this job group and date from each array
    const forecastGroup = findGroupByNameAndDate(payrollDto.forecastPayroll, jobGroupName, targetDate);
    const budgetGroup = findGroupByNameAndDate(payrollDto.budgetPayroll, jobGroupName, targetDate);
    const actualGroup = findGroupByNameAndDate(payrollDto.actualPayroll, jobGroupName, targetDate);
    const scheduledGroup = findGroupByNameAndDate(payrollDto.scheduledPayroll, jobGroupName, targetDate);
    
    // Build job data object
    jobs[jobGroupName] = {
      hours: scheduledGroup?.scheduledHours || 0,
      forecast: forecastGroup?.forecastPayrollCost || 0,
      budget: budgetGroup?.budgetPayrollCost || 0,
      actual: actualGroup?.actualPayrollCost || null // null when no actual data
    };
  });
  
  return jobs;
}

/**
 * Helper function to find a job group by name and date
 */
function findGroupByNameAndDate(groups: any[] | undefined, jobGroupName: string, targetDate: Date): any {
  if (!groups) return null;
  
  return groups.find(group => 
    group.jobGroupName === jobGroupName && 
    isSameDate(group.date, targetDate)
  );
}

/**
 * Helper function to compare dates (handling DateOnly from API)
 * Uses local date comparison to match Calendar component behavior
 */
function isSameDate(apiDate: any, targetDate: Date): boolean {
  if (!apiDate) return false;

  const apiDateObj = new Date(apiDate);

  return (
    apiDateObj.getUTCFullYear() === targetDate.getUTCFullYear() &&
    apiDateObj.getUTCMonth() === targetDate.getUTCMonth() &&
    apiDateObj.getUTCDate() === targetDate.getUTCDate()
  );
}

/**
 * Get all unique job group names from PayrollDto
 */
function getAllJobGroupsFromDto(payrollDto: PayrollDto): string[] {
  const groups = new Set<string>();
  
  [payrollDto.forecastPayroll, payrollDto.budgetPayroll, payrollDto.actualPayroll, payrollDto.scheduledPayroll]
    .forEach(array => {
      array?.forEach(group => {
        if (group.jobGroupName) {
          groups.add(group.jobGroupName);
        }
      });
    });
  
  return Array.from(groups).sort();
}

/**
 * Transform live payroll data from PayrollForecast component to PayrollData array
 * This supports real-time updates when user edits timeline data
 */
export function transformLivePayrollDataToReconciliationData(liveData: LivePayrollData): PayrollData[] {
  // Extract all unique dates from live detail data
  const dates = extractUniqueDatesFromLiveData(liveData);
  
  return dates.map(date => ({
    date,
    jobs: buildJobsForDateFromLiveData(date, liveData)
  }));
}

/**
 * Extract all unique dates from live PayrollDetailDto arrays
 */
function extractUniqueDatesFromLiveData(liveData: LivePayrollData): Date[] {
  const dateStrings = new Set<string>();
  
  // Collect all dates from all detail arrays
  const allDetails = [
    ...liveData.forecastDetails,
    ...liveData.scheduledDetails,
    ...liveData.actualDetails,
    ...liveData.budgetDetails
  ];
  
  allDetails.forEach(detail => {
    if (detail.date) {
      // Normalize date string to ensure consistent parsing
      const dateStr = detail.date.toString();
      if (dateStr.includes('T')) {
        // If it has time, take just the date part
        dateStrings.add(dateStr.split('T')[0]);
      } else {
        dateStrings.add(dateStr);
      }
    }
  });
  
  return Array.from(dateStrings)
    .map(dateStr => {
      // Create date in local timezone to match boundary calculations
      const [year, month, day] = dateStr.split('-').map(Number);
      return new Date(year, month - 1, day);
    })
    .sort((a, b) => a.getTime() - b.getTime());
}

/**
 * Build jobs object for a specific date from live data
 * Calculates scheduled cost (hours × rates) instead of just hours
 */
function buildJobsForDateFromLiveData(targetDate: Date, liveData: LivePayrollData): Record<string, any> {
  const jobs: Record<string, any> = {};
  const jobCodeToGroup = buildJobCodeToGroupMap(liveData.jobCodes);
  const groupNames = new Set<string>(Object.values(jobCodeToGroup));
  
  // Get all job groups that exist in the data
  const allJobGroups = getAllJobGroupsFromLiveData(liveData);
  
  allJobGroups.forEach(jobGroupName => {
    // Find data for this job group and date from each detail array
    const forecastDetails = findDetailsByGroupAndDate(liveData.forecastDetails, jobGroupName, targetDate, liveData.jobCodes);
    const scheduledDetails = findDetailsByGroupAndDate(liveData.scheduledDetails, jobGroupName, targetDate, liveData.jobCodes);
    const actualDetails = findDetailsByGroupAndDate(liveData.actualDetails, jobGroupName, targetDate, liveData.jobCodes);
    const budgetDetails = findDetailsByGroupAndDate(liveData.budgetDetails, jobGroupName, targetDate, liveData.jobCodes);
    
    // Calculate scheduled cost using hours × rate for hourly roles,
    // and daily salary cost for salaried roles (allocatedSalaryCost/365)
    const scheduledHours = scheduledDetails.reduce((sum, detail) => sum + detail.regularHours, 0);
    const scheduledCost = calculateDetailsCost(scheduledDetails, liveData.jobCodes);
    
    // Build job data object
    jobs[jobGroupName] = {
      hours: scheduledHours, // Keep for internal use if needed
      scheduled: scheduledCost, // Cost instead of hours for display
      forecast: calculateDetailsCost(forecastDetails, liveData.jobCodes),
      budget: calculateDetailsCost(budgetDetails, liveData.jobCodes),
      actual: actualDetails.length > 0 ? calculateDetailsCost(actualDetails, liveData.jobCodes) : null
    };
  });
  
  return jobs;
}

function findJobCode(jobCodeString: string, jobCodes: JobCode[]): JobCode | undefined {
  return jobCodes.find(jc => 
    jc.jobCodeString === jobCodeString || 
    jc.name === jobCodeString ||
    jc.jobCode === jobCodeString ||
    jc.id === jobCodeString
  );
}


/**
 * Calculate scheduled cost for a set of detail records
 */
function calculateDetailsCost(details: PayrollDetailDto[], jobCodes: JobCode[]): number {
  return details.reduce((totalCost, detail) => {
    const job = findJobCode(detail.jobCode || '', jobCodes);
    const hours = detail.regularHours;

    // Salaried: allocatedSalaryCost present and no hourly rate -> use daily salary
    if (job && job.allocatedSalaryCost && job.allocatedSalaryCost > 0) {
      return totalCost + (job.allocatedSalaryCost / 365);
    }

    // Hourly default: hours × rate
    const rate = job?.averageHourlyRate || 0;
    return totalCost + (hours * rate);
  }, 0);
}


/**
 * Find detail records by job group and date
 */
function findDetailsByGroupAndDate(
  details: PayrollDetailDto[], 
  jobGroupName: string, 
  targetDate: Date, 
  jobCodes: JobCode[]
): PayrollDetailDto[] {
  return details.filter(detail => {
    // Match by date
    if (!detail.date || !isSameDate(detail.date, targetDate)) {
      return false;
    }
    
    // Match by job group (find job code's group)
    // Try multiple matching strategies for job code
    const jobCode = jobCodes.find(jc => 
      jc.jobCodeString === detail.jobCode || 
      jc.name === detail.jobCode ||
      jc.jobCode === detail.jobCode ||
      jc.id === detail.jobCode
    );
    
    return jobCode?.jobGroupName === jobGroupName;
  });
}

/**
 * Calculate scheduled cost for a set of detail records
 */
function calculateScheduledCost(scheduledDetails: PayrollDetailDto[], jobCodes: JobCode[]): number {
  return scheduledDetails.reduce((totalCost, detail) => {
    const hours = detail.regularHours;
    const rate = getJobCodeRate(detail.jobCode || '', jobCodes);
    return totalCost + (hours * rate);
  }, 0);
}

/**
 * Get hourly rate for a job code
 */
function getJobCodeRate(jobCodeString: string, jobCodes: JobCode[]): number {
  const jobCode = jobCodes.find(jc => 
    jc.jobCodeString === jobCodeString || 
    jc.name === jobCodeString ||
    jc.jobCode === jobCodeString ||
    jc.id === jobCodeString
  );
  return jobCode?.averageHourlyRate || 0;
}

/**
 * Get all unique job group names from live data
 */
function getAllJobGroupsFromLiveData(liveData: LivePayrollData): string[] {
  const groups = new Set<string>();
  
  // Get job groups from job codes
  liveData.jobCodes.forEach(jobCode => {
    if (jobCode.jobGroupName) {
      groups.add(jobCode.jobGroupName);
    }
  });
  
  return Array.from(groups).sort();
}


/**
 * Build a map of possible job code identifiers to their group name
 */
function buildJobCodeToGroupMap(jobCodes: JobCode[]): Record<string, string> {
  const map: Record<string, string> = {};
  jobCodes.forEach(jc => {
    if (jc.id) map[jc.id] = jc.jobGroupName;
    if (jc.jobCode) map[jc.jobCode] = jc.jobGroupName;
    if (jc.jobCodeString) map[jc.jobCodeString] = jc.jobGroupName;
    if (jc.name) map[jc.name] = jc.jobGroupName;
  });
  return map;
}