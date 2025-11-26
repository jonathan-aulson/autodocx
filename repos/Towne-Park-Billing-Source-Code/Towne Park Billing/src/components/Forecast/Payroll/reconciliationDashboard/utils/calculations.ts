import { PayrollData, TimeHorizon, JobGroup } from '../types';

/**
 * Calculate reconciliation data for display
 * (Copied from template with minimal modifications)
 */
export function calculateReconciliationData(
  data: PayrollData[], 
  jobFilter: "all" | JobGroup, 
  timeHorizon: TimeHorizon
) {
  // Get all job groups from the data
  const allJobGroups = getAllJobGroupsFromData(data);
  const jobGroups = jobFilter === "all" ? allJobGroups : [jobFilter];

  return jobGroups.map((jobGroup) => {
    // Calculate totals for the job group
    let totalScheduled = 0;
    let totalForecast = 0;
    let totalBudget = 0;
    let totalActual = 0;
    let hasActualData = false;

    data.forEach((d) => {
      const jobData = d.jobs?.[jobGroup];

      if (jobData) {
        // Sum up the values (handle both hours and scheduled for backward compatibility)
        totalScheduled += jobData.scheduled || jobData.hours || 0;
        totalForecast += jobData.forecast || 0;
        totalBudget += jobData.budget || 0;

        // Handle actual data (may be null)
        if (jobData.actual !== null && jobData.actual !== undefined) {
          totalActual += jobData.actual;
          hasActualData = true;
        }
      }
    });

    // Calculate variance (actual vs budget)
    const variance = (hasActualData ? totalActual : 0) - totalBudget;
    const variancePercentage = totalBudget > 0 ? (variance / totalBudget) * 100 : 0;

    return {
      jobCode: jobGroup,
      scheduled: totalScheduled,
      forecast: totalForecast,
      budget: totalBudget,
      actual: hasActualData ? totalActual : null,
      variance,
      variancePercentage,
    };
  });
}

/**
 * Extract job groups from PayrollData array
 */
function getAllJobGroupsFromData(data: PayrollData[]): string[] {
  const groups = new Set<string>();
  
  data.forEach(item => {
    if (item.jobs) {
      Object.keys(item.jobs).forEach(jobGroup => groups.add(jobGroup));
    }
  });
  
  return Array.from(groups).sort();
}

/**
 * Helper function to format job label (use job group name as-is)
 */
export function formatJobLabel(jobCode: string): string {
  return jobCode; // Use API job group name directly
}

/**
 * Helper function to get timeframe label
 */
export function getTimeframeLabel(startDate: Date, endDate: Date): string {
  if (startDate.getTime() === endDate.getTime()) {
    return startDate.toLocaleDateString("en-US", { 
      weekday: "long", 
      month: "long", 
      day: "numeric", 
      year: "numeric" 
    });
  } else {
    return `${startDate.toLocaleDateString("en-US", { 
      month: "short", 
      day: "numeric" 
    })} - ${endDate.toLocaleDateString("en-US", { 
      month: "short", 
      day: "numeric", 
      year: "numeric" 
    })}`;
  }
}