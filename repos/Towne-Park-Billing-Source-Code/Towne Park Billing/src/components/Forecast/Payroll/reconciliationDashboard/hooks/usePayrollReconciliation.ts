import { useMemo } from 'react';
import { PayrollDto, PayrollDetailDto } from '@/lib/models/Payroll';
import { JobCode } from '@/lib/models/jobCode';
import { transformPayrollDtoToReconciliationData, transformLivePayrollDataToReconciliationData } from '../utils/data-transformers';

export interface LivePayrollData {
  forecastDetails: PayrollDetailDto[];
  scheduledDetails: PayrollDetailDto[];
  actualDetails: PayrollDetailDto[];
  budgetDetails: PayrollDetailDto[];
  jobCodes: JobCode[];
  customerSiteId: string;
}

/**
 * Hook to transform payroll data for reconciliation dashboard
 * Supports both static PayrollDto and live data for real-time updates
 */
export function usePayrollReconciliation(
  data: PayrollDto | LivePayrollData | null, 
  billingPeriod: string,
  isLiveData: boolean = false
) {
  return useMemo(() => {
    if (!data) {
      return {
        reconciliationData: [],
        originalDto: null,
        billingPeriod,
        isLoading: false,
        error: null
      };
    }

    try {
      if (isLiveData) {
        // Handle live data from PayrollForecast component
        const liveData = data as LivePayrollData;
        return {
          reconciliationData: transformLivePayrollDataToReconciliationData(liveData),
          originalDto: null, // No original DTO for live data
          billingPeriod,
          isLoading: false,
          error: null
        };
      } else {
        // Handle static PayrollDto (existing behavior)
        const payrollDto = data as PayrollDto;
        return {
          reconciliationData: transformPayrollDtoToReconciliationData(payrollDto),
          originalDto: payrollDto,
          billingPeriod,
          isLoading: false,
          error: null
        };
      }
    } catch (error) {
      return {
        reconciliationData: [],
        originalDto: null,
        billingPeriod,
        isLoading: false,
        error: error instanceof Error ? error.message : 'Unknown error occurred'
      };
    }
  }, [data, billingPeriod, isLiveData]);
}