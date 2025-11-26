/**
 * Unified Job Group interface for both API responses and frontend usage
 */
export interface JobGroup {
  /** Unique identifier for the job group (GUID from API) */
  id: string;
  /** Display name of the job group */
  name: string;
  /** Whether the job group is active */
  active: boolean;
  /** Display title/name of the job group (from API) */
  title: string;
  /** Whether the job group is active (from API) */
  isActive: boolean;
  /** Associated job codes (optional for API responses) */
  jobCodes?: import('./jobCode').JobCode[];
}