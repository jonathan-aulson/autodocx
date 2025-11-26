/**
 * Site Assignment related interfaces
 */
export interface SiteAssignmentJobGroup {
  jobGroupId: string;
  jobGroupName: string;
  isActive: boolean;
}

export interface SiteAssignment {
  siteId: string;
  siteNumber: string;
  siteName: string;
  city: string;
  assignedJobGroups: SiteAssignmentJobGroup[];
  jobGroupCount: number;
  hasUnassignedJobCodes: boolean;
}

export interface SiteAssignmentsApiResponse {
  siteAssignments: SiteAssignment[];
  totalCount: number;
  success: boolean;
  errorMessage: string | null;
}