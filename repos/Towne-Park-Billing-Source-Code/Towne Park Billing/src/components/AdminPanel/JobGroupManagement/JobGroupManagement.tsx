import { JobCode } from "@/lib/models/jobCode";
import { JobGroup } from "@/lib/models/jobGroup";
import { SiteAssignment, SiteAssignmentsApiResponse } from "@/lib/models/siteAssignment";
import {
  DndContext,
  DragEndEvent,
  DragOverlay,
  DragStartEvent,
  useDraggable,
  useDroppable
} from '@dnd-kit/core';
import { AlertTriangle, ArrowRight, Building, CheckCircle, ChevronDown, ChevronUp, Edit, Eye, EyeOff, Info, Search, Truck } from "lucide-react";
import React, { useEffect, useMemo, useState } from "react";
import { Alert } from "../../ui/alert";
import { Badge } from "../../ui/badge";
import { Button } from "../../ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "../../ui/card";
import { Checkbox } from "../../ui/checkbox";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "../../ui/dialog";
import { Input } from "../../ui/input";
import { Label } from "../../ui/label";
import { ScrollArea } from "../../ui/scroll-area";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "../../ui/select";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "../../ui/tooltip";

// --- Draggable Job Code Component ---
interface DraggableJobCodeProps {
  jobCode: JobCode;
  children: React.ReactNode;
  disabled?: boolean;
}

const DraggableJobCode: React.FC<DraggableJobCodeProps> = ({ jobCode, children, disabled = false }) => {
  const { attributes, listeners, setNodeRef, transform, isDragging } = useDraggable({
    id: jobCode.id,
    disabled,
  });

  const style = transform ? {
    transform: `translate3d(${transform.x}px, ${transform.y}px, 0)`,
  } : undefined;

  return (
    <div
      ref={setNodeRef}
      style={style}
      className={`${isDragging ? 'opacity-50' : ''} relative`}
    >
      {/* Drag handle - only this area enables dragging */}
      <div
        {...listeners}
        {...attributes}
        className={`absolute left-0 top-0 bottom-0 w-8 flex items-center justify-center ${
          disabled ? 'cursor-not-allowed' : 'cursor-grab active:cursor-grabbing hover:bg-muted/30'
        } rounded-l-lg transition-colors`}
        title={disabled ? 'Cannot drag selected job codes' : 'Drag to assign to group'}
      >
        <div className="flex flex-col gap-0.5">
          <div className="w-1 h-1 bg-muted-foreground rounded-full opacity-60"></div>
          <div className="w-1 h-1 bg-muted-foreground rounded-full opacity-60"></div>
          <div className="w-1 h-1 bg-muted-foreground rounded-full opacity-60"></div>
          <div className="w-1 h-1 bg-muted-foreground rounded-full opacity-60"></div>
        </div>
      </div>
      {/* Content area - buttons remain clickable */}
      <div className="pl-8">
        {children}
      </div>
    </div>
  );
};

// --- Droppable Job Group Component ---
interface DroppableJobGroupProps {
  groupId: string;
  children: (isOver: boolean) => React.ReactNode;
  disabled?: boolean;
}

const DroppableJobGroup: React.FC<DroppableJobGroupProps> = ({ groupId, children, disabled = false }) => {
  const { isOver, setNodeRef } = useDroppable({
    id: `group-${groupId}`,
    disabled,
  });

  return (
    <div
      ref={setNodeRef}
      className="transition-all duration-200"
    >
      {children(isOver)}
    </div>
  );
};

const JobGroupManagement: React.FC = () => {
  const [jobCodes, setJobCodes] = useState<JobCode[]>([]);
  const [jobGroups, setJobGroups] = useState<JobGroup[]>([]);
  const [siteAssignments, setSiteAssignments] = useState<SiteAssignment[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [siteAssignmentsLoading, setSiteAssignmentsLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);
  const [siteAssignmentsError, setSiteAssignmentsError] = useState<string | null>(null);
  const [showHelpGuide, setShowHelpGuide] = useState<boolean>(false);

  const [jobCodeFilter, setJobCodeFilter] = useState<"all" | "new" | "unassigned" | "inactive">("all");
  const [jobCodeSearch, setJobCodeSearch] = useState("");
  const [selectedJobCodes, setSelectedJobCodes] = useState<string[]>([]);
  const [selectedJobGroup, setSelectedJobGroup] = useState<string>("");
  const [jobGroupSearch, setJobGroupSearch] = useState("");
  const [groupFilter, setGroupFilter] = useState<"active" | "inactive" | "all">("active");
  const [siteSearch, setSiteSearch] = useState("");

  // Drag and Drop State
  const [activeId, setActiveId] = useState<string | null>(null);
  const [draggedJobCode, setDraggedJobCode] = useState<JobCode | null>(null);

  const [moveDialogOpen, setMoveDialogOpen] = useState(false);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [createGroupDialogOpen, setCreateGroupDialogOpen] = useState(false);

  // Dialog-specific error states
  const [editDialogError, setEditDialogError] = useState<string | null>(null);
  const [createGroupDialogError, setCreateGroupDialogError] = useState<string | null>(null);

  const [jobCodeToEdit, setJobCodeToEdit] = useState<JobCode | null>(null);
  const [jobCodeToMove, setJobCodeToMove] = useState<JobCode | null>(null);
  const [newGroupName, setNewGroupName] = useState("");
  const [groupOperationLoading, setGroupOperationLoading] = useState<string | null>(null);
  const [jobCodeOperationLoading, setJobCodeOperationLoading] = useState<string | null>(null);
  const [moveGroupSelected, setMoveGroupSelected] = useState(false);

  // Helper function to calculate job code status consistently
  const calculateJobCodeStatus = (jobCode: {
    isActive: boolean;
    title?: string;
    jobTitle?: string;
    groupId?: string;
    jobGroupId?: string;
  }): "active" | "inactive" | "unassigned" | "new" => {
    if (!jobCode.isActive) return "inactive";
    
    const hasTitle = (jobCode.title || jobCode.jobTitle) && (jobCode.title || jobCode.jobTitle)!.trim() !== "";
    const hasGroup = (jobCode.groupId || jobCode.jobGroupId) && (jobCode.groupId || jobCode.jobGroupId) !== "";
    
    if (!hasTitle) return "new"; // No title assigned yet
    if (!hasGroup) return "unassigned"; // No group assigned yet
    return "active"; // Has both title and group
  };

  const fetchJobCodes = async (): Promise<JobCode[]> => {
    const response = await fetch('/api/jobcodes');
    if (!response.ok) {
      throw new Error(`Failed to fetch job codes: ${response.statusText}`);
    }
    const data: any[] = await response.json();
    
    return data.map(dto => ({
      // API properties (required) - using correct camelCase field names
      jobCodeId: dto.jobCodeId,
      jobCode: dto.jobCode,
      jobTitle: dto.jobTitle,
      jobGroupId: dto.jobGroupId || "",
      jobGroupName: dto.jobGroupName || "",
      name: dto.name,
      isActive: dto.isActive,
      
      // Frontend compatibility properties (computed)
      id: dto.name, // Using name as the display ID for UI
      title: dto.jobTitle,
      status: (() => {
        if (!dto.isActive) return "inactive";
        // If active, check for title and group assignment
        const hasTitle = dto.jobTitle && dto.jobTitle.trim() !== "";
        const hasGroup = dto.jobGroupId && dto.jobGroupId !== "";
        
        if (!hasTitle) return "new"; // No title assigned yet
        if (!hasGroup) return "unassigned"; // No group assigned yet
        return "active"; // Has both title and group
      })() as "active" | "inactive" | "unassigned" | "new",
      groupId: dto.jobGroupId && dto.jobGroupId !== "" ? dto.jobGroupId : undefined,
      jobCodeString: dto.jobCode, // Actual job code string (e.g., "CONCINODR", "LOTATTND")
    }));
  };

  const fetchJobGroups = async (): Promise<JobGroup[]> => {
    const response = await fetch('/api/jobgroups');
    if (!response.ok) {
      throw new Error(`Failed to fetch job groups: ${response.statusText}`);
    }
    const data: any[] = await response.json();
    
    return data.map(dto => ({
      id: dto.id,
      name: dto.title,
      active: dto.isActive,
      title: dto.title,
      isActive: dto.isActive
    }));
  };

  const updateJobCodeTitle = async (jobCodeId: string, newTitle: string): Promise<void> => {
    const response = await fetch('/api/jobcodes/title', {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        jobCodeId: jobCodeId,
        newTitle: newTitle
      })
    });
    
    if (!response.ok) {
      const errorData = await response.json();
      throw new Error(errorData.ErrorMessage || 'Failed to update job code title');
    }
  };

  const assignJobCodesToGroup = async (jobCodeNames: string[], targetGroupId: string): Promise<void> => {
    // Convert job code names to JobCodeIds for the API
    const jobCodeIds = jobCodeNames.map(name => {
      const jobCode = jobCodes.find(jc => jc.id === name);
      return jobCode?.jobCodeId || name;
    });

    const response = await fetch('/api/jobcodes/assign', {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        jobCodeIds: jobCodeIds,
        targetGroupId: targetGroupId
      })
    });
    
    if (!response.ok) {
      const errorData = await response.json();
      throw new Error(errorData.ErrorMessage || 'Failed to assign job codes');
    }
  };

  const updateJobCodeStatus = async (jobCodeIds: string[], isActive: boolean): Promise<void> => {
    const response = await fetch('/api/jobcodes/status', {
      method: 'PATCH',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        jobCodeIds: jobCodeIds,
        isActive: isActive
      })
    });
    
    if (!response.ok) {
      const errorData = await response.json();
      throw new Error(errorData.ErrorMessage || `Failed to ${isActive ? 'activate' : 'deactivate'} job codes`);
    }
    
    const result = await response.json();
    if (!result.Success) {
      throw new Error(result.ErrorMessage || `Failed to ${isActive ? 'activate' : 'deactivate'} job codes`);
    }
  };

  const createJobGroup = async (groupTitle: string): Promise<void> => {
    const response = await fetch(`/api/jobgroups/create?jobGroupTitle=${encodeURIComponent(groupTitle)}`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      }
    });
    
    if (!response.ok) {
      const errorData = await response.json();
      throw new Error(errorData.error || 'Failed to create job group');
    }
  };

  const deactivateJobGroup = async (jobGroupId: string): Promise<void> => {
    const response = await fetch(`/api/jobgroups/${jobGroupId}`, {
      method: 'PATCH',
      headers: {
        'Content-Type': 'application/json',
      }
    });
    
    if (!response.ok) {
      const errorData = await response.json();
      throw new Error(errorData.error || 'Failed to deactivate job group');
    }
  };

  const loadData = async () => {
    try {
      setLoading(true);
      setError(null);
      
      const [jobCodesData, jobGroupsData] = await Promise.all([
        fetchJobCodes(),
        fetchJobGroups()
      ]);
      
      setJobCodes(jobCodesData);
      setJobGroups(jobGroupsData);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load data');
    } finally {
      setLoading(false);
    }
  };

  const loadAllData = async () => {
    await Promise.all([
      loadData(),
      fetchSiteAssignments()
    ]);
  };

  useEffect(() => {
    loadAllData();
  }, []);

  const getSelectionType = (selectedCodes: string[]): "none" | "active" | "inactive" => {
    if (selectedCodes.length === 0) return "none";
    const firstSelected = jobCodes.find(jc => jc.id === selectedCodes[0]);
    return firstSelected?.status === "inactive" ? "inactive" : "active";
  };

  const isCheckboxDisabled = (jobCode: JobCode, selectedCodes: string[]): boolean => {
    const selectionType = getSelectionType(selectedCodes);
    if (selectionType === "none") return false;
    
    const isJobCodeInactive = jobCode.status === "inactive";
    return selectionType === "active" ? isJobCodeInactive : !isJobCodeInactive;
  };

  const getTooltipMessage = (): string => {
    return "Cannot mix active/inactive job codes for bulk selection. Active codes can only be assigned to active groups, while inactive codes can be assigned to any group.";
  };

  const filteredJobCodes = useMemo(() => {
  let codes = jobCodes;
  if (jobCodeFilter === "new") {
codes = codes.filter(jc => jc.status === "new");
  } else if (jobCodeFilter === "unassigned") {
    codes = codes.filter(jc => !jc.groupId || jc.groupId === "");
  } else if (jobCodeFilter === "inactive") {
    codes = codes.filter(jc => jc.status === "inactive");
  }
  if (jobCodeSearch) {
    const searchLower = jobCodeSearch.toLowerCase();
    codes = codes.filter(jc =>
      (jc.title || "").toLowerCase().includes(searchLower) ||
      (jc.jobCodeString || "").toLowerCase().includes(searchLower)
    );
  }
  return codes;
}, [jobCodes, jobCodeFilter, jobCodeSearch]);


  const filteredJobGroups = useMemo(() => {
    let groups = jobGroups;
    if (groupFilter === "active") {
       groups = groups.filter(g => g.active);
      // Show all groups, but visually indicate inactive ones
      groups = groups;
    } else if (groupFilter === "inactive") {
      groups = groups.filter(g => !g.active);
    }
    if (jobGroupSearch)
      groups = groups.filter(g => g.name.toLowerCase().includes(jobGroupSearch.toLowerCase()));
    return groups;
  }, [jobGroups, groupFilter, jobGroupSearch]);

  const getAvailableGroupsForMove = (jobCode: any) => {
    if (jobCode.status === "inactive") {
      return jobGroups;
    }
    return jobGroups.filter(g => g.active);
  };

  const activeFilteredJobCodes = filteredJobCodes.filter(jc => jc.status === "active");
  const allVisibleSelected = activeFilteredJobCodes.every(jc => selectedJobCodes.includes(jc.id));
  const toggleSelectAll = () => {
    if (allVisibleSelected) {
      setSelectedJobCodes(selectedJobCodes.filter(id => !activeFilteredJobCodes.some(jc => jc.id === id)));
    } else {
      setSelectedJobCodes(prev => [
        ...prev.filter(id =>
          !activeFilteredJobCodes.some(jc => jc.id === id)
        ),
        ...activeFilteredJobCodes
          .map(jc => jc.id)
          .filter(id => !prev.includes(id))
      ]);
    }
  };

  const toggleSelectJobCode = (id: string) => {
    setSelectedJobCodes(selectedJobCodes.includes(id)
      ? selectedJobCodes.filter(jcId => jcId !== id)
      : [...selectedJobCodes, id]);
  };

  const handleMoveJobCodes = async (targetGroupId: string) => {
    try {
      const codesToMove = selectedJobCodes.length > 0
        ? selectedJobCodes
        : jobCodeToMove ? [jobCodeToMove.id] : [];
      
      if (codesToMove.length === 0) return;

      await assignJobCodesToGroup(codesToMove, targetGroupId);
      
      setJobCodes(prev =>
        prev.map(jc => {
          if (codesToMove.includes(jc.id)) {
            const updatedJobCode = { ...jc, groupId: targetGroupId, jobGroupId: targetGroupId };
            return { ...updatedJobCode, status: calculateJobCodeStatus(updatedJobCode) };
          }
          return jc;
        })
      );
      
      setSelectedJobCodes([]);
      setMoveDialogOpen(false);
      setJobCodeToMove(null);
      setMoveGroupSelected(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to move job codes');
    }
  };

  const handleBulkAssign = async () => {
    if (!selectedJobGroup || selectedJobCodes.length === 0) return;
    
    try {
      await assignJobCodesToGroup(selectedJobCodes, selectedJobGroup);
      
      setJobCodes(prev =>
        prev.map(jc => {
          if (selectedJobCodes.includes(jc.id)) {
            const updatedJobCode = { ...jc, groupId: selectedJobGroup, jobGroupId: selectedJobGroup };
            return { ...updatedJobCode, status: calculateJobCodeStatus(updatedJobCode) };
          }
          return jc;
        })
      );
      
      setSelectedJobCodes([]);
      setSelectedJobGroup("");
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to assign job codes');
    }
  };

  const handleEditJobCode = async (id: string, newTitle: string) => {
    try {
      setEditDialogError(null);
      const jobCode = jobCodes.find(jc => jc.id === id);
      if (!jobCode) {
        throw new Error('Job code not found');
      }

      await updateJobCodeTitle(jobCode.jobCodeId, newTitle);
      
      setJobCodes(prev => prev.map(jc => {
        if (jc.id === id) {
          const updatedJobCode = { ...jc, title: newTitle, jobTitle: newTitle };
          return { ...updatedJobCode, status: calculateJobCodeStatus(updatedJobCode) };
        }
        return jc;
      }));
      
      setEditDialogOpen(false);
      setJobCodeToEdit(null);
      setEditDialogError(null); // Clear error on success
    } catch (err) {
      setEditDialogError(err instanceof Error ? err.message : 'Failed to update job code title');
    }
  };

  const handleCreateGroup = async () => {
    if (!newGroupName.trim()) return;
    
    try {
      setCreateGroupDialogError(null); // Clear any previous errors
      await createJobGroup(newGroupName.trim());
      
      await loadData();
      
      setNewGroupName("");
      setCreateGroupDialogOpen(false);
      setCreateGroupDialogError(null); // Clear error on success
    } catch (err) {
      setCreateGroupDialogError(err instanceof Error ? err.message : 'Failed to create job group');
    }
  };

  const handleDeactivateGroup = async (group: JobGroup) => {
    const hasActiveCodes = jobCodes.some(
      jc => jc.groupId === group.id && jc.status === "active"
    );
    if (hasActiveCodes) return;
    
    try {
      setGroupOperationLoading(group.id);
      await deactivateJobGroup(group.id);
      
      setJobGroups(prev =>
        prev.map(g => g.id === group.id ? { ...g, active: false, isActive: false } : g)
      );
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to deactivate job group');
    } finally {
      setGroupOperationLoading(null);
    }
  };

  const activateJobGroup = async (jobGroupId: string): Promise<void> => {
    const response = await fetch(`/api/jobgroups/${jobGroupId}/activate`, {
      method: 'PATCH',
      headers: {
        'Content-Type': 'application/json',
      }
    });
    
    if (!response.ok) {
      const errorData = await response.json();
      throw new Error(errorData.error || 'Failed to activate job group');
    }
  };

  const handleReactivateGroup = async (groupId: string) => {
    try {
      setGroupOperationLoading(groupId);
      await activateJobGroup(groupId);
      
      setJobGroups(prev =>
        prev.map(g => g.id === groupId ? { ...g, active: true, isActive: true } : g)
      );
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to reactivate job group');
    } finally {
      setGroupOperationLoading(null);
    }
  };

  const handleActivateJobCode = async (jobCode: JobCode) => {
    try {
      setJobCodeOperationLoading(jobCode.id);
      await updateJobCodeStatus([jobCode.jobCodeId], true);

      // Update the local state to reflect the change
      setJobCodes(prev =>
        prev.map(jc =>
          jc.id === jobCode.id
            ? {
                ...jc,
                isActive: true,
                status: jc.groupId ? "active" : "unassigned"
              }
            : jc
        )
      );

      // If the job code's group is inactive, activate it in the frontend state
      if (jobCode.groupId) {
        setJobGroups(prev =>
          prev.map(g =>
            g.id === jobCode.groupId && !g.active
              ? { ...g, active: true, isActive: true }
              : g
          )
        );
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to activate job code');
    } finally {
      setJobCodeOperationLoading(null);
    }
  };

  const handleDeactivateJobCode = async (jobCode: JobCode) => {
    try {
      setJobCodeOperationLoading(jobCode.id);
      await updateJobCodeStatus([jobCode.jobCodeId], false);
      
      // Update the local state to reflect the change
      setJobCodes(prev =>
        prev.map(jc =>
          jc.id === jobCode.id
            ? {
                ...jc,
                isActive: false,
                status: "inactive"
              }
            : jc
        )
      );
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to deactivate job code');
    } finally {
      setJobCodeOperationLoading(null);
    }
  };

  const handleBulkActivateJobCodes = async (jobCodes: JobCode[]) => {
    try {
      const jobCodeIds = jobCodes.map(jc => jc.jobCodeId);
      await updateJobCodeStatus(jobCodeIds, true);
      
      // Update the local state to reflect the changes
      setJobCodes(prev =>
        prev.map(jc => {
          const isSelected = jobCodes.some(selected => selected.id === jc.id);
          return isSelected
            ? {
                ...jc,
                isActive: true,
                status: (() => {
                  const hasTitle = jc.title && jc.title.trim() !== "";
                  const hasGroup = jc.groupId && jc.groupId !== "";
                  
                  if (!hasTitle) return "new"; // No title assigned yet
                  if (!hasGroup) return "unassigned"; // No group assigned yet
                  return "active"; // Has both title and group
                })() as "active" | "inactive" | "unassigned" | "new"
              }
            : jc;
        })
      );
      
      setSelectedJobCodes([]);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to activate job codes');
    }
  };

  const handleBulkDeactivateJobCodes = async (jobCodes: JobCode[]) => {
    try {
      const jobCodeIds = jobCodes.map(jc => jc.jobCodeId);
      await updateJobCodeStatus(jobCodeIds, false);
      
      // Update the local state to reflect the changes
      setJobCodes(prev =>
        prev.map(jc => {
          const isSelected = jobCodes.some(selected => selected.id === jc.id);
          return isSelected
            ? {
                ...jc,
                isActive: false,
                status: "inactive"
              }
            : jc;
        })
      );
      
      setSelectedJobCodes([]);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to deactivate job codes');
    }
  };

  // --- Drag and Drop Handlers ---
  const handleDragStart = (event: DragStartEvent) => {
    const { active } = event;
    const jobCode = jobCodes.find(jc => jc.id === active.id);
    if (jobCode) {
      setActiveId(active.id as string);
      setDraggedJobCode(jobCode);
    }
  };

  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;
    
    if (over && over.id.toString().startsWith('group-')) {
      const targetGroupId = over.id.toString().replace('group-', '');
      
      // Handle drag and drop assignment directly
      if (draggedJobCode) {
        handleDragDropAssignment(draggedJobCode.id, targetGroupId);
      }
    }
    
    setActiveId(null);
    setDraggedJobCode(null);
  };

  const handleDragDropAssignment = async (jobCodeId: string, targetGroupId: string) => {
    try {
      await assignJobCodesToGroup([jobCodeId], targetGroupId);
      
      setJobCodes(prev =>
        prev.map(jc => {
          if (jc.id === jobCodeId) {
            const updatedJobCode = { ...jc, groupId: targetGroupId, jobGroupId: targetGroupId };
            return { ...updatedJobCode, status: calculateJobCodeStatus(updatedJobCode) };
          }
          return jc;
        })
      );
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to move job code');
    }
  };

  const handleDragCancel = () => {
    setActiveId(null);
    setDraggedJobCode(null);
  };

  const fetchSiteAssignments = async () => {
    try {
      setSiteAssignmentsLoading(true);
      setSiteAssignmentsError(null);
      
      const response = await fetch('/api/jobgroups/site-assignments');
      if (!response.ok) {
        throw new Error(`Failed to fetch site assignments: ${response.status} ${response.statusText}`);
      }
      
      const data: SiteAssignmentsApiResponse = await response.json();
      
      if (!data.success) {
        throw new Error(data.errorMessage || 'Failed to fetch site assignments');
      }
      
      setSiteAssignments(data.siteAssignments);
    } catch (err) {
      console.error('Error fetching site assignments:', err);
      setSiteAssignmentsError(err instanceof Error ? err.message : 'Failed to fetch site assignments');
    } finally {
      setSiteAssignmentsLoading(false);
    }
  };

  const filteredSites = useMemo(() => {
    return siteAssignments.filter(site =>
      site.siteName.toLowerCase().includes(siteSearch.toLowerCase()) ||
      site.siteNumber.toLowerCase().includes(siteSearch.toLowerCase()) ||
      site.city.toLowerCase().includes(siteSearch.toLowerCase())
    );
  }, [siteAssignments, siteSearch]);

  return (
    <DndContext
      onDragStart={handleDragStart}
      onDragEnd={handleDragEnd}
      onDragCancel={handleDragCancel}
    >
      <TooltipProvider>
        <div>
        <h1 className="text-2xl font-bold py-8">Job Group Management</h1>
        <Button
          variant="outline"
          onClick={() => setShowHelpGuide((prev: boolean) => !prev)}
          className="flex items-center gap-2 mb-2"
          data-qa-id="button-job-group-help-guide"
        >
          <Info className="h-4 w-4" />
          {showHelpGuide ? "Hide Guide" : "Show Guide"}
          {showHelpGuide ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
        </Button>
        {loading && (
          <div className="flex items-center justify-center py-8">
            <div className="text-center">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto mb-2"></div>
              <p className="text-muted-foreground">Loading job codes and groups...</p>
            </div>
          </div>
        )}
        
        {error && (
          <Alert className="mb-4" variant="destructive">
            <AlertTriangle className="h-4 w-4" />
            <div>
              <strong>Error:</strong> {error}
              <Button
                variant="outline"
                size="sm"
                className="ml-2"
                onClick={() => {
                  setError(null);
                  loadData();
                }}
              >
                Retry
              </Button>
            </div>
          </Alert>
        )}

        {showHelpGuide && (
          <div className="space-y-4 p-4 border rounded-md bg-muted/20 mb-6">
            <div>
              <h3 className="text-lg font-medium mb-2">Getting Started</h3>
              <p>
                This page allows you to manage job codes and job groups for forecasting. You can edit job code titles, assign codes to groups, and deactivate codes or groups as needed.
              </p>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <h4 className="font-medium mb-1">Job Code Management</h4>
                <p className="text-sm text-muted-foreground">
                  Edit job code titles, assign codes to groups, and deactivate codes when needed.
                </p>
              </div>

              <div>
                <h4 className="font-medium mb-1">Group Management</h4>
                <p className="text-sm text-muted-foreground">
                  Create job groups and organize job codes into logical categories for forecasting.
                </p>
              </div>

              <div>
                <h4 className="font-medium mb-1">Automatic Site Assignment</h4>
                <p className="text-sm text-muted-foreground">
                  Sites automatically get access to job groups based on which job codes they use.
                </p>
              </div>

              <div>
                <h4 className="font-medium mb-1">Bulk Actions</h4>
                <p className="text-sm text-muted-foreground">
                  Select multiple job codes to move them to a group in one action. Active job codes can only be moved to active groups, while inactive job codes can be moved to any group (active or inactive).
                </p>
              </div>

              <div>
                <h4 className="font-medium mb-1">Deactivation Rules</h4>
                <p className="text-sm text-muted-foreground">
                  Job groups can only be deactivated when all job codes within them are deactivated. Deactivated job codes can be moved to any group (active or deactivated), but active job codes can only be moved to active groups.
                </p>
              </div>

              <div>
                <h4 className="font-medium mb-1">Unassigned Codes</h4>
                <p className="text-sm text-muted-foreground">
                  Job codes that are not assigned to any group are clearly marked as "Unassigned".
                </p>
              </div>
            </div>
            <Alert className="mb-6" variant="default" data-qa-id="alert-job-group-business-rules">
              <AlertTriangle className="h-5 w-5" />
              <div>
                <div>
                  <strong>Business Rules:</strong> Job groups can only be deactivated when all job codes within them are
                  deactivated. Deactivated job codes can be moved to any group (active or deactivated), but active job
                  codes can only be moved to active groups.
                </div>
              </div>
            </Alert>
          </div>
        )}
        
        {!loading && (
        <div>

          <div className="flex gap-6">
            <Card className="flex-1 min-w-[320px]" data-qa-id="card-job-codes">
              <CardHeader>
                <div className="flex items-center justify-between">
                  <CardTitle>Job Codes</CardTitle>
                </div>
                <div className="flex gap-2 mt-2">
                  <Select value={jobCodeFilter} onValueChange={v => setJobCodeFilter(v as any)}>
                    <SelectTrigger data-qa-id="dropdown-job-code-filter">
                      <span>
                        {jobCodeFilter === "all" && "All"}
                        {jobCodeFilter === "new" && "New"}
                        {jobCodeFilter === "unassigned" && "Unassigned"}
                        {jobCodeFilter === "inactive" && "Inactive"}
                      </span>
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="all">All</SelectItem>
                      <SelectItem value="new">New</SelectItem>
                      <SelectItem value="unassigned">Unassigned</SelectItem>
                      <SelectItem value="inactive">Inactive</SelectItem>
                    </SelectContent>
                  </Select>
                  <Input
                    placeholder="Search job codes and titles"
                    value={jobCodeSearch}
                    onChange={e => setJobCodeSearch(e.target.value)}
                    data-qa-id="input-job-code-search"
                  />
                </div>
              </CardHeader>
              <CardContent>
                <ScrollArea className="h-64 pr-2">
                  {filteredJobCodes.length === 0 && (
                    <div className="text-muted-foreground text-sm py-4 text-center">
                      No job codes found.
                    </div>
                  )}
                  {filteredJobCodes.map(jc => {
                    const isNew = jc.status === "new";
                    const isUnassigned = !jc.groupId && jc.status === "active";
                    const isInactive = jc.status === "inactive";
// Independent badge conditions - not based on computed status
                    const hasNoTitle = !jc.title || jc.title.trim() === "";
                    const hasNoGroup = !jc.groupId || jc.groupId === "";
                    const groupName = jc.groupId ? (jobGroups.find(g => g.id === jc.groupId)?.name || "Unknown") : "Unassigned";

                    const isDisabled = isCheckboxDisabled(jc, selectedJobCodes);

                    return (
                      <DraggableJobCode
                        key={jc.id}
                        jobCode={jc}
                        disabled={selectedJobCodes.includes(jc.id) || jobCodeOperationLoading === jc.id}
                      >
                        <div
                          className={`flex items-center space-x-3 p-3 border rounded-lg hover:bg-muted/50 ${selectedJobCodes.includes(jc.id) ? "bg-muted border-primary" : ""
                            }`}
                          data-qa-id={`row-job-code-${jc.id}`}
                        >
                        {isDisabled ? (
                          <Tooltip>
                            <TooltipTrigger asChild>
                              <div
                                className="cursor-not-allowed inline-flex"
                                style={{ cursor: 'not-allowed' }}
                              >
                                <Checkbox
                                  checked={selectedJobCodes.includes(jc.id)}
                                  onCheckedChange={() => toggleSelectJobCode(jc.id)}
                                  disabled={isDisabled}
                                  data-qa-id={`checkbox-job-code-${jc.id}`}
                                  className="cursor-not-allowed pointer-events-none"
                                  style={{ cursor: 'not-allowed' }}
                                />
                              </div>
                            </TooltipTrigger>
                            <TooltipContent className="max-w-sm">
                              {getTooltipMessage()}
                            </TooltipContent>
                          </Tooltip>
                        ) : (
                          <Checkbox
                            checked={selectedJobCodes.includes(jc.id)}
                            onCheckedChange={() => toggleSelectJobCode(jc.id)}
                            disabled={isDisabled}
                            data-qa-id={`checkbox-job-code-${jc.id}`}
                          />
                        )}
                        <div className={`flex-1 ${isInactive ? "opacity-60" : ""}`}>
                          <div className="flex items-center gap-2 mb-1">
                            <code className="text-sm font-mono bg-muted px-2 py-1 rounded">{jc.jobCodeString}</code>
                            {hasNoTitle && jc.isActive && (
                              <Badge variant="default" className="text-xs bg-blue-500" data-qa-id={`badge-job-code-new-${jc.id}`}>
                                New
                              </Badge>
                            )}
                            {hasNoGroup && jc.isActive && (
                              <Badge variant="destructive" className="text-xs" data-qa-id={`badge-job-code-unassigned-${jc.id}`}>
                                Unassigned
                              </Badge>
                            )}
                            {isInactive && (
                              <Badge variant="outline" className="text-xs text-gray-500" data-qa-id={`badge-job-code-inactive-${jc.id}`}>
                                Inactive
                              </Badge>
                            )}
                          </div>
                          <p className="text-sm font-medium">{jc.title}</p>
                          <div className="flex items-center gap-2 text-xs text-muted-foreground">
                            <span>Group: {groupName}</span>
                            <span>•</span>
                            <span>
                              Used at {(jc as any).workdaySites?.length || 0} sites
                            </span>
                          </div>
                        </div>
                        <div className={`flex gap-1`}>
                          <Tooltip>
                            <TooltipTrigger asChild>
                              <Button
                                size="sm"
                                variant="ghost"
                                onClick={() => {
                                  setJobCodeToEdit(jc);
                                  setEditDialogError(null);
                                  setEditDialogOpen(true);
                                }}
                                disabled={isInactive}
                                data-qa-id={`button-job-code-edit-${jc.id}`}
                              >
                                <Edit className="h-3 w-3" />
                              </Button>
                            </TooltipTrigger>
                            <TooltipContent>
                              {isInactive ? "Cannot edit inactive job codes" : "Edit Job Code Title"}
                            </TooltipContent>
                          </Tooltip>
                          <Tooltip>
                            <TooltipTrigger asChild>
                              <Button
                                size="icon"
                                variant="ghost"
                                onClick={() => {
                                  setJobCodeToMove(jc);
                                  setMoveDialogOpen(true);
                                }}
                                data-qa-id={`button-job-code-move-${jc.id}`}
                                disabled={false}
                              >
                                <Truck className="h-4 w-4" />
                              </Button>
                            </TooltipTrigger>
                            <TooltipContent>
                              {jc.status === "inactive"
                                ? "Move inactive job code (can be moved to any group)"
                                : "Move to another group"}
                            </TooltipContent>
                          </Tooltip>
                          {jc.status === "active" && (
                            <Tooltip>
                              <TooltipTrigger asChild>
                                <Button
                                  size="icon"
                                  variant="ghost"
                                  onClick={() => handleDeactivateJobCode(jc)}
                                  disabled={jobCodeOperationLoading === jc.id}
                                  data-qa-id={`button-job-code-deactivate-${jc.id}`}
                                >
                                  {jobCodeOperationLoading === jc.id ? (
                                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-current"></div>
                                  ) : (
                                    <Eye className="h-4 w-4" />
                                  )}
                                </Button>
                              </TooltipTrigger>
                              <TooltipContent>Deactivate Job Code</TooltipContent>
                            </Tooltip>
                          )}
                          {jc.status === "inactive" && (
                            <Tooltip>
                              <TooltipTrigger asChild>
                                <Button
                                  size="icon"
                                  variant="ghost"
                                  onClick={() => handleActivateJobCode(jc)}
                                  disabled={jobCodeOperationLoading === jc.id}
                                  data-qa-id={`button-job-code-reactivate-${jc.id}`}
                                >
                                  {jobCodeOperationLoading === jc.id ? (
                                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-current"></div>
                                  ) : (
                                    <EyeOff className="h-4 w-4" />
                                  )}
                                </Button>
                              </TooltipTrigger>
                              <TooltipContent>Reactivate Job Code</TooltipContent>
                            </Tooltip>
                          )}
                        </div>
                        </div>
                      </DraggableJobCode>
                    );
                  })}
                </ScrollArea>
                
                {selectedJobCodes.length > 0 && (
                  <Card className="bg-blue-50 border-blue-200 mt-4 dark:bg-blue-900 dark:border-blue-700">
                    <CardContent className="p-4">
                      <div className="flex items-center justify-between">
                        <span className="text-sm font-medium">{selectedJobCodes.length} job codes selected</span>
                        <div className="flex items-center gap-2">
                          <Select value={selectedJobGroup} onValueChange={setSelectedJobGroup}>
                            <SelectTrigger className="w-48">
                              <SelectValue placeholder="Choose group" />
                            </SelectTrigger>
                            <SelectContent>
                              {(() => {
                                const selectionType = getSelectionType(selectedJobCodes);
                                const availableGroups = selectionType === "inactive"
                                  ? jobGroups // Show all groups for inactive codes (not filtered by search)
                                  : jobGroups.filter((group) => group.active); // Only active groups for active codes (not filtered by search)
                                
                                return availableGroups.map((group) => (
                                  <SelectItem key={group.id} value={group.id}>
                                    {group.name}{!group.active ? " (Inactive)" : ""}
                                  </SelectItem>
                                ));
                              })()}
                            </SelectContent>
                          </Select>
                          <Button size="sm" onClick={handleBulkAssign} disabled={!selectedJobGroup}>
                            <ArrowRight className="h-4 w-4 mr-2" />
                            Assign
                          </Button>
                          {(() => {
                            const selectionType = getSelectionType(selectedJobCodes);
                            const selectedJobCodeObjects = jobCodes.filter(jc => selectedJobCodes.includes(jc.id));
                            
                            if (selectionType === "active") {
                              return (
                                <Button
                                  size="sm"
                                  variant="outline"
                                  onClick={() => handleBulkDeactivateJobCodes(selectedJobCodeObjects)}
                                  data-qa-id="button-bulk-deactivate-job-codes"
                                >
                                  <Eye className="h-4 w-4 mr-2" />
                                  Deactivate
                                </Button>
                              );
                            } else if (selectionType === "inactive") {
                              return (
                                <Button
                                  size="sm"
                                  variant="outline"
                                  onClick={() => handleBulkActivateJobCodes(selectedJobCodeObjects)}
                                  data-qa-id="button-bulk-activate-job-codes"
                                >
                                  <EyeOff className="h-4 w-4 mr-2" />
                                  Activate
                                </Button>
                              );
                            }
                            return null;
                          })()}
                        </div>
                      </div>
                      <p className="text-xs text-muted-foreground mt-2">
                        {(() => {
                          const selectionType = getSelectionType(selectedJobCodes);
                          if (selectionType === "inactive") {
                            return "Note: Inactive job codes can be assigned to any group (active or inactive) or activated in bulk";
                          }
                          return "Note: Active job codes can only be assigned to active job groups or deactivated in bulk";
                        })()}
                      </p>
                    </CardContent>
                  </Card>
                )}
              </CardContent>
            </Card>
            <Card className="flex-1 min-w-[320px]" data-qa-id="card-job-groups">
              <CardHeader>
                <CardTitle>Job Groups</CardTitle>
                <div className="flex gap-2 mt-2">
                  <Input
                    placeholder="Search job groups"
                    value={jobGroupSearch}
                    onChange={e => setJobGroupSearch(e.target.value)}
                    data-qa-id="input-job-group-search"
                  />
                  <Select value={groupFilter} onValueChange={(value: "active" | "all") => setGroupFilter(value)}>
                    <SelectTrigger className="w-32" data-qa-id="dropdown-job-group-filter">
                      <span>
                        {groupFilter === "active" && "Active"}
                        {groupFilter === "all" && "All"}
                      </span>
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="active">Active</SelectItem>
                      <SelectItem value="all">All</SelectItem>
                    </SelectContent>
                  </Select>
                  <Button
                    size="sm"
                    onClick={() => {
                      setCreateGroupDialogError(null);
                      setCreateGroupDialogOpen(true);
                    }}
                    data-qa-id="button-job-group-create"
                  >
                    + New Group
                  </Button>
                </div>
              </CardHeader>
              <CardContent>
                <ScrollArea className="h-64 pr-2">
                  {filteredJobGroups.length === 0 && (
                    <div className="text-muted-foreground text-sm py-4 text-center">
                      No job groups found.
                    </div>
                  )}
                  {filteredJobGroups.map(group => {
                    const jobCount = jobCodes.filter(code => code.groupId === group.id).length;
                    const activeCodesCount = jobCodes.filter(code => code.groupId === group.id && code.status === "active").length;
                    const canDeactivate = group.active && activeCodesCount === 0;
                    return (
                      <DroppableJobGroup
                        key={group.id}
                        groupId={group.id}
                        disabled={
                          // If no job code is being dragged, allow all drops
                          draggedJobCode
                            ? (
                                // If dragging an active job code, only allow drops to active groups
                                draggedJobCode.status === "active" && !group.active
                              )
                            : false
                        }
                      >
                        {(isOver) => (
                          <Card
                            className={`cursor-pointer hover:shadow-md transition-all duration-200 ${
                              !group.active ? "opacity-60" : ""
                            } ${
                              // Show disabled state when dragging active job code to inactive group
                              draggedJobCode && draggedJobCode.status === "active" && !group.active
                                ? "opacity-30 cursor-not-allowed border-red-200 bg-red-50"
                                : ""
                            } ${
                              isOver
                                ? "bg-blue-100 ring-4 ring-blue-400 shadow-lg scale-[1.02] border-blue-300"
                                : ""
                            }`}
                          >
                        <CardContent className="p-4">
                          <div className="flex items-center justify-between mb-3">
                            <h4 className="font-medium">{group.name}</h4>
                            <div className="flex items-center gap-2">
                              <Badge variant="secondary" className="text-xs">
                                {jobCount} codes
                              </Badge>
                              {activeCodesCount > 0 && (
                                <Badge variant="outline" className="text-xs text-green-600 border-green-300">
                                  {activeCodesCount} active
                                </Badge>
                              )}
                              {jobCount === 0 && group.active && (
                                <Badge variant="outline" className="text-xs text-amber-600 border-amber-300">
                                  Empty
                                </Badge>
                              )}
                              {!group.active && (
                                <Badge variant="outline" className="text-xs text-gray-500">
                                  Inactive
                                </Badge>
                              )}
                              {group.active ? (
                                <Tooltip>
                                  <TooltipTrigger asChild>
                                    <span className={!canDeactivate || groupOperationLoading === group.id ? "cursor-not-allowed" : ""}>
                                      <Button
                                        size="sm"
                                        variant="ghost"
                                        onClick={() => handleDeactivateGroup(group)}
                                        disabled={!canDeactivate || groupOperationLoading === group.id}
                                        data-qa-id={`button-job-group-deactivate-${group.id}`}
                                        className={!canDeactivate || groupOperationLoading === group.id ? "pointer-events-none" : ""}
                                      >
                                        {groupOperationLoading === group.id ? (
                                          <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-current"></div>
                                        ) : (
                                          <Eye className="h-4 w-4" />
                                        )}
                                      </Button>
                                    </span>
                                  </TooltipTrigger>
                                  <TooltipContent>
                                    {canDeactivate
                                      ? "Deactivate Job Group"
                                      : `Job Groups with active Job Codes cannot be deactivated`}
                                  </TooltipContent>
                                </Tooltip>
                              ) : (
                                <Tooltip>
                                  <TooltipTrigger asChild>
                                    <Button
                                      size="sm"
                                      variant="ghost"
                                      onClick={() => handleReactivateGroup(group.id)}
                                      disabled={groupOperationLoading === group.id}
                                      data-qa-id={`button-job-group-activate-${group.id}`}
                                    >
                                      {groupOperationLoading === group.id ? (
                                        <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-current"></div>
                                      ) : (
                                        <EyeOff className="h-4 w-4" />
                                      )}
                                    </Button>
                                  </TooltipTrigger>
                                  <TooltipContent>Activate Job Group (Make Active)</TooltipContent>
                                </Tooltip>
                              )}
                            </div>
                          </div>
                          <div className="space-y-2">
                            <p className="text-xs font-medium text-muted-foreground">Job Codes:</p>
                            {jobCount > 0 ? (
                              <div className="flex flex-wrap gap-1">
                              {jobCodes
                                  .filter(code => code.groupId === group.id)
  .map(code => (
    <div key={code.id} className="flex items-center gap-1">
      <code
        className={`text-xs px-2 py-1 rounded ${code.status === "active" ? "bg-muted" : "bg-gray-200 text-gray-500"}`}
      >
        {code.jobCodeString}
      </code>
    </div>
  ))}
                              </div>
                            ) : (
                              <div className="text-xs text-muted-foreground italic bg-muted/30 p-2 rounded">
                                No job codes assigned. Select codes and assign them to this group.
                              </div>
                            )}
                          </div>
                        </CardContent>
                          </Card>
                        )}
                      </DroppableJobGroup>
                    );
                  })}
                </ScrollArea>
              </CardContent>
            </Card>
          </div>

          <Card className="mt-6">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Building className="h-5 w-5" />
                Calculated Site Assignments
              </CardTitle>
              <CardDescription>
                Site job group assignments are automatically calculated based on which job codes are used at each
                site.
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                <div className="relative">
                  <Search className="absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
                  <Input
                    placeholder="Search sites..."
                    value={siteSearch}
                    onChange={(e) => setSiteSearch(e.target.value)}
                    className="pl-8 max-w-md"
                  />
                </div>

                {!siteAssignmentsLoading && !siteAssignmentsError && (
                  <div className="flex items-center gap-2 text-sm text-muted-foreground">
                    <span>
                      Showing {filteredSites.length} of {siteAssignments.length} sites
                    </span>
                    {siteSearch && (
                      <Badge variant="outline" className="text-xs">
                        Filtered
                      </Badge>
                    )}
                  </div>
                )}

                <div className="bg-blue-50 border border-blue-200 rounded-lg dark:bg-slate-800 dark:border-slate-600 p-3">
                  <div className="flex items-start gap-2">
                    <CheckCircle className="h-4 w-4 text-blue-600 mt-0.5" />
                    <div className="text-sm">
                      <p className="font-medium text-blue-900 dark:text-blue-600">Automatic Assignment Logic</p>
                      <p className="text-blue-700 dark:text-blue-400">
                        When you assign job codes to groups, sites automatically get access to those groups based on
                        which job codes they use. No manual site configuration needed.
                      </p>
                    </div>
                  </div>
                </div>

                {siteAssignmentsLoading ? (
                  <div className="flex items-center justify-center py-8">
                    <div className="text-center">
                      <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto mb-2"></div>
                      <p className="text-muted-foreground">Loading site assignments...</p>
                    </div>
                  </div>
                ) : siteAssignmentsError ? (
                  <Alert className="mb-4" variant="destructive">
                    <AlertTriangle className="h-4 w-4" />
                    <div>
                      <strong>Error loading site assignments:</strong> {siteAssignmentsError}
                      <Button
                        variant="outline"
                        size="sm"
                        className="ml-2"
                        onClick={() => fetchSiteAssignments()}
                      >
                        Retry
                      </Button>
                    </div>
                  </Alert>
                ) : (
                  <ScrollArea className={`pr-2 ${filteredSites.length <= 9 ? 'h-auto' : 'h-lvh'}`}>
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                      {filteredSites.length === 0 ? (
                        <div className="col-span-full text-muted-foreground text-sm py-4 text-center">
                          No sites found matching your search criteria.
                        </div>
                      ) : (
                        filteredSites.map((site) => (
                          <Card key={site.siteId} className="hover:shadow-md transition-shadow">
                            <CardContent className="p-4">
                              <div className="flex items-start justify-between mb-2">
                                <div className="text-center">
                                  <div className="text-xl font-mono font-bold text-primary">{site.siteNumber}</div>
                                  <div className="text-xs text-muted-foreground">Site</div>
                                </div>
                                <div className="flex flex-col items-end gap-1">
                                  {site.hasUnassignedJobCodes && (
                                    <Badge variant="destructive" className="text-xs">
                                      Has Unassigned
                                    </Badge>
                                  )}
                                  <Badge variant="outline" className="text-xs">
                                    {site.jobGroupCount} groups
                                  </Badge>
                                </div>
                              </div>
                              <div className="mb-3">
                                <p className="font-medium text-sm">{site.siteName}</p>
                                <p className="text-xs text-muted-foreground">{site.city}</p>
                              </div>

                              <div className="space-y-2">
                                <p className="text-xs font-medium text-muted-foreground">Assigned Job Groups:</p>
                                {site.assignedJobGroups.length > 0 ? (
                                  <div className="flex flex-wrap gap-1">
                                    {site.assignedJobGroups.map((jobGroup) => (
                                      <Badge
                                        key={jobGroup.jobGroupId}
                                        variant={jobGroup.isActive ? "secondary" : "outline"}
                                        className="text-xs"
                                      >
                                        {jobGroup.jobGroupName}
                                        {!jobGroup.isActive && " (Inactive)"}
                                      </Badge>
                                    ))}
                                  </div>
                                ) : (
                                  <div className="text-xs text-muted-foreground italic bg-muted/30 p-2 rounded">
                                    No job groups assigned to this site.
                                  </div>
                                )}
                              </div>
                            </CardContent>
                          </Card>
                        ))
                      )}
                    </div>
                  </ScrollArea>
                )}
              </div>
            </CardContent>
          </Card>

          <Dialog open={moveDialogOpen} onOpenChange={(open) => {
            setMoveDialogOpen(open);
            if (!open) {
              setMoveGroupSelected(false);
            }
          }}>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>
                  Move Job Code{selectedJobCodes.length > 1 ? "s" : ""}
                </DialogTitle>
              </DialogHeader>
              <div>
                <Label>Select target group:</Label>
                <Select
                  onValueChange={(value) => {
                    setMoveGroupSelected(true);
                    handleMoveJobCodes(value);
                  }}
                  data-qa-id="dropdown-move-job-code-group"
                >
                  <SelectTrigger>
                    <span>Select group</span>
                  </SelectTrigger>
                  <SelectContent>
                    {(() => {
                      if (jobCodeToMove && jobCodeToMove.status === "inactive") {
                        return jobGroups.map((group) => (
                          <SelectItem key={group.id} value={group.id}>
                            {group.name} {!group.active ? "(Inactive)" : ""}
                          </SelectItem>
                        ));
                      }

                      if (selectedJobCodes.length > 0 && !jobCodeToMove) {
                        const selectedCodes = jobCodes.filter(jc => selectedJobCodes.includes(jc.id));
                        const allSelectedAreInactive = selectedCodes.every(jc => jc.status === "inactive");

                        if (allSelectedAreInactive) {
                          return jobGroups.map((group) => (
                            <SelectItem key={group.id} value={group.id}>
                              {group.name} {!group.active ? "(Inactive)" : ""}
                            </SelectItem>
                          ));
                        }
                      }

                      return jobGroups
                        .filter((group) => group.active)
                        .map((group) => (
                          <SelectItem key={group.id} value={group.id}>
                            {group.name}
                          </SelectItem>
                        ));
                    })()}
                  </SelectContent>
                </Select>
                <p className="text-xs text-muted-foreground mt-2">
                  {(() => {
                    if (jobCodeToMove && jobCodeToMove.status === "inactive") {
                      return "Inactive job codes can be assigned to any group (active or inactive)";
                    }

                    if (selectedJobCodes.length > 0 && !jobCodeToMove) {
                      const selectedCodes = jobCodes.filter(jc => selectedJobCodes.includes(jc.id));
                      const allSelectedAreInactive = selectedCodes.every(jc => jc.status === "inactive");

                      if (allSelectedAreInactive) {
                        return "Inactive job codes can be assigned to any group (active or inactive)";
                      }
                    }

                    return "Note: Bulk assignment only shows active groups if all selected job codes are active. Inactive job codes can be assigned to any group.";
                  })()}
                </p>
              </div>
              <DialogFooter>
                <Button
                  variant="outline"
                  onClick={() => setMoveDialogOpen(false)}
                  disabled={moveGroupSelected}
                  data-qa-id="button-move-job-code-cancel"
                >
                  Cancel
                </Button>
              </DialogFooter>
            </DialogContent>
          </Dialog>
          <Dialog open={editDialogOpen} onOpenChange={(open) => {
            setEditDialogOpen(open);
            if (!open) setEditDialogError(null);
          }}>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>Edit Job Code Title</DialogTitle>
                <DialogDescription>
                  Update the title for this job code. The code itself cannot be changed.
                </DialogDescription>
              </DialogHeader>
              <div className="grid gap-4 py-4">
                <div className="grid gap-2">
                  <Label htmlFor="job-code-display">Job Code</Label>
                  <Input
                    id="job-code-display"
                    value={jobCodeToEdit?.id || ""}
                    disabled
                    className="bg-muted"
                  />
                  <p className="text-xs text-muted-foreground">Job code cannot be changed</p>
                </div>
                <div className="grid gap-2">
                  <Label htmlFor="job-title">Job Title</Label>
                  <Input
                    id="job-title"
                    placeholder="Enter job title"
                    value={jobCodeToEdit?.title || ""}
                    onChange={e =>
                      setJobCodeToEdit(jc =>
                        jc ? { ...jc, title: e.target.value } : jc
                      )
                    }
                  />
                  {editDialogError && (
                    <Alert variant="destructive" className="mt-2">
                      <AlertTriangle className="h-4 w-4" />
                      <span className="text-sm">{editDialogError}</span>
                    </Alert>
                  )}
                  <p className="text-xs text-muted-foreground">Enter a descriptive title for this job code</p>
                </div>
              </div>
              <DialogFooter>
                <Button variant="outline" onClick={() => {
                  setEditDialogOpen(false);
                  setEditDialogError(null);
                }}>
                  Cancel
                </Button>
                <Button
                  onClick={() =>
                    jobCodeToEdit &&
                    handleEditJobCode(jobCodeToEdit.id, jobCodeToEdit.title)
                  }
                  disabled={!jobCodeToEdit?.title?.trim()}
                  data-qa-id="button-edit-job-code-save"
                >
                  Save Changes
                </Button>
              </DialogFooter>
            </DialogContent>
          </Dialog>
          <Dialog open={createGroupDialogOpen} onOpenChange={(open) => {
            setCreateGroupDialogOpen(open);
            if (!open) setCreateGroupDialogError(null);
          }}>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>Create New Job Group</DialogTitle>
              </DialogHeader>
              <div>
                <Label htmlFor="new-group-name">Group Name</Label>
                <Input
                  id="new-group-name"
                  value={newGroupName}
                  onChange={e => setNewGroupName(e.target.value)}
                  data-qa-id="input-create-job-group-name"
                />
                {createGroupDialogError && (
                  <Alert variant="destructive" className="mt-2">
                    <AlertTriangle className="h-4 w-4" />
                    <span className="text-sm">{createGroupDialogError}</span>
                  </Alert>
                )}
              </div>
              <DialogFooter>
                <Button onClick={handleCreateGroup} data-qa-id="button-create-job-group-save">
                  Create
                </Button>
                <Button variant="outline" onClick={() => {
                  setCreateGroupDialogOpen(false);
                  setCreateGroupDialogError(null);
                }}>
                  Cancel
                </Button>
              </DialogFooter>
            </DialogContent>
          </Dialog>
        </div>
        )}
        {showHelpGuide && (
          <Card className="bg-gradient-to-r from-muted/30 to-muted/10 mb-6 border border-primary/30 shadow-sm">
            <CardHeader className="pb-3 flex flex-row items-center gap-2">
              <CardTitle className="text-base flex items-center gap-2">Help Guide</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-1 md:grid-cols-3 gap-6 text-sm">
                <div className="space-y-2">
                  <p className="font-medium">1. Job Code Management</p>
                  <p className="text-muted-foreground">
                    Edit job code titles, assign codes to groups, and deactivate codes when needed.
                  </p>
                </div>
                <div className="space-y-2">
                  <p className="font-medium">2. Group Management</p>
                  <p className="text-muted-foreground">
                    Create job groups and organize job codes into logical categories for forecasting.
                  </p>
                </div>
                <div className="space-y-2">
                  <p className="font-medium">3. Automatic Site Assignment</p>
                  <p className="text-muted-foreground">
                    Sites automatically get access to job groups based on which job codes they use.
                  </p>
                </div>
              </div>
            </CardContent>
          </Card>
        )}
      </div>
      <DragOverlay>
        {activeId && draggedJobCode ? (
          <div className="bg-white shadow-lg rounded-md p-2 opacity-90 border">
            <span className="text-sm font-medium">{draggedJobCode.title}</span>
          </div>
        ) : null}
      </DragOverlay>
    </TooltipProvider>
    </DndContext>
  );
};

export default JobGroupManagement;
