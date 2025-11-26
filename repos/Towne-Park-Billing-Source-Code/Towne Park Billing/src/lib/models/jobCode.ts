/**
 * Unified Job Code interface for both API responses and frontend usage
 */
export interface JobCode {
  /** Display ID for UI (uses 'name' field from API) */
  id: string;
  /** Display title/description of the job code */
  title: string;
  /** Computed status based on isActive and jobGroupId */
  status: "active" | "inactive" | "unassigned" | "new";
  /** Reference to group ID for frontend state management */
  groupId?: string;
  /** Unique identifier for the job code (GUID from API) */
  jobCodeId: string;
  /** The job code string value (e.g., "CONCINODR", "LOTATTND") */
  jobCodeString: string;
  /** Display name for the job code */
  name: string;
  /** Whether the job code is active */
  isActive: boolean;
  /** Associated job group ID (GUID) */
  jobGroupId: string;
  /** Associated job group name */
  jobGroupName: string;
  /** Number of active employees for this job code */
  activeEmployeeCount?: number;
  /** Allocated salary cost for this job code */
  allocatedSalaryCost?: number;
  /** Average hourly rate for this job code */
  averageHourlyRate?: number;
  /** Optional job code details for frontend display */
  jobCode: string;
  /** Optional job title for frontend display */
  jobTitle: string;
}