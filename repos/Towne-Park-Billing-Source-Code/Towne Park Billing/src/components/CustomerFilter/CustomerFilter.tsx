import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from "@/components/ui/accordion";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from "@/components/ui/command";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Separator } from "@/components/ui/separator";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip";
import { useCustomer } from "@/contexts/CustomerContext";
import { CustomerSummary } from "@/lib/models/GeneralInfo";
import { cn } from "@/lib/utils";
import { Check, Info, PanelRightClose, PanelRightOpen, X } from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";

interface HierarchyItem {
  id: string
  name: string
  children?: HierarchyItem[]
}

const organizationalFilterLevels = [
  { id: "legalEntity", name: "Legal Entity" },
  { id: "region", name: "Region" },
  { id: "district", name: "District" },
  { id: "site", name: "Site" },
  { id: "accountManager", name: "AM / DM" },
]

const customerFilterLevels = [
  { id: "plCategory", name: "P&L Category" },
  { id: "cogSegment", name: "COG" },
  { id: "businessSegment", name: "Business Segment" },
  { id: "contractType", name: "Contract Type" },
]

export interface SelectedFilters {
  legalEntity?: string[]
  region?: string[]
  district?: string[]
  site?: string[]
  accountManager?: string[]
  plCategory?: string[]
  cogSegment?: string[]
  businessSegment?: string[]
  contractType?: string[]
}

interface CustomerFilterProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onApplyFilters: (filters: SelectedFilters) => void
  currentFilters?: SelectedFilters
  customers?: CustomerSummary[]
}

export function CustomerFilter({ open, onOpenChange, onApplyFilters, currentFilters = {}, customers = [] }: CustomerFilterProps) {
  const { selectedCustomer } = useCustomer();
  const stableCurrentFilters = useMemo(() => currentFilters, [open]);

  const [selectedFilters, setSelectedFilters] = useState<SelectedFilters>(stableCurrentFilters);
  const [filteredSiteIds, setFilteredSiteIds] = useState<string[]>([]);
  const siteListRef = useRef<HTMLDivElement>(null);
  const prevOpenRef = useRef<boolean>(open);
  const [isSiteListSidebarOpen, setIsSiteListSidebarOpen] = useState(true);

  const isMounted = useRef(true);
  useEffect(() => {
    isMounted.current = true;
    return () => { isMounted.current = false; };
  }, []);

  useEffect(() => {
    if (open && selectedCustomer && Object.keys(stableCurrentFilters).length === 0) {
      setSelectedFilters({
        site: [selectedCustomer.siteNumber]
      });
    }
  }, [open, selectedCustomer, stableCurrentFilters]);

  const getItemsForLevel = (level: string): { id: string; name: string }[] => {
    if (customers.length === 0) return [];
    
    switch (level) {
      case "legalEntity":
        return extractUniqueValues(customers, "legalEntity");
      case "region":
        return extractUniqueValues(customers, "svpRegion");
      case "district":
        return extractUniqueValues(customers, "district");
      case "site":
        return customers.map(customer => ({
          id: customer.siteNumber,
          name: `${customer.siteNumber} - ${customer.siteName}`
        }));
      case "accountManager": {
        const managers = new Set<string>();
        customers.forEach(c => {
          if (c.accountManager) managers.add(c.accountManager);
          if (c.districtManager) managers.add(c.districtManager);
        });
        return Array.from(managers).map(name => ({ id: name, name }));
      }
      case "plCategory":
        return extractUniqueValues(customers, "plCategory");
      case "cogSegment":
        return extractUniqueValues(customers, "cogSegment");
      case "businessSegment":
        return extractUniqueValues(customers, "businessSegment");
      case "contractType":
        return extractUniqueValues(customers, "contractType");
      default:
        return [];
    }
  }

  const extractUniqueValues = (data: CustomerSummary[], field: keyof CustomerSummary): { id: string; name: string }[] => {
    const uniqueValues = new Set<string>();
    
    data.forEach(item => {
      if (item[field] && typeof item[field] === 'string' && item[field] !== '') {
        uniqueValues.add(item[field] as string);
      }
    });
    
    return Array.from(uniqueValues)
      .sort()
      .map(value => ({ id: value, name: value }));
  };

  useEffect(() => {
    if (!prevOpenRef.current && open) {
      setSelectedFilters(stableCurrentFilters);
    }
    prevOpenRef.current = open;
  }, [open, stableCurrentFilters]);

  const { orgFilterCount, customerFilterCount } = useMemo(() => {
    let orgCount = 0;
    let custCount = 0;

    organizationalFilterLevels.forEach((level) => {
      if (selectedFilters[level.id as keyof typeof selectedFilters]?.length) {
        orgCount += (selectedFilters[level.id as keyof typeof selectedFilters] as string[]).length;
      }
    });

    customerFilterLevels.forEach((level) => {
      if (selectedFilters[level.id as keyof typeof selectedFilters]?.length) {
        custCount += (selectedFilters[level.id as keyof typeof selectedFilters] as string[]).length;
      }
    });

    return { orgFilterCount: orgCount, customerFilterCount: custCount };
  }, [selectedFilters]);

  const computeFilteredSiteIds = useMemo(() => {
    if (!open || customers.length === 0) return [];

    let filteredCustomers = [...customers];

    // Filter by organizational filters
    if (selectedFilters.legalEntity?.length) {
      filteredCustomers = filteredCustomers.filter(customer =>
        customer.legalEntity && selectedFilters.legalEntity?.includes(customer.legalEntity)
      );
    }

    if (selectedFilters.region?.length) {
      filteredCustomers = filteredCustomers.filter(customer =>
        customer.svpRegion && selectedFilters.region?.includes(customer.svpRegion)
      );
    }

    if (selectedFilters.district?.length) {
      filteredCustomers = filteredCustomers.filter(customer =>
        customer.district && selectedFilters.district?.includes(customer.district)
      );
    }

    if (selectedFilters.site?.length) {
      filteredCustomers = filteredCustomers.filter(customer =>
        selectedFilters.site?.includes(customer.siteNumber)
      );
    }

    if (selectedFilters.accountManager?.length) {
      filteredCustomers = filteredCustomers.filter(customer =>
        (customer.accountManager && selectedFilters.accountManager?.includes(customer.accountManager)) ||
        (customer.districtManager && selectedFilters.accountManager?.includes(customer.districtManager))
      );
    }

    // Filter by customer filters
    if (selectedFilters.plCategory?.length) {
      filteredCustomers = filteredCustomers.filter(customer =>
        customer.plCategory && selectedFilters.plCategory?.includes(customer.plCategory)
      );
    }

    if (selectedFilters.cogSegment?.length) {
      filteredCustomers = filteredCustomers.filter(customer =>
        customer.cogSegment && selectedFilters.cogSegment?.includes(customer.cogSegment)
      );
    }

    if (selectedFilters.businessSegment?.length) {
      filteredCustomers = filteredCustomers.filter(customer =>
        customer.businessSegment && selectedFilters.businessSegment?.includes(customer.businessSegment)
      );
    }

    if (selectedFilters.contractType?.length) {
      filteredCustomers = filteredCustomers.filter(customer =>
        customer.contractType && selectedFilters.contractType?.includes(customer.contractType)
      );
    }

    return filteredCustomers.map(customer => customer.siteNumber).sort();
  }, [open, customers, selectedFilters]);

  useEffect(() => {
    if (isMounted.current) {
      setFilteredSiteIds(computeFilteredSiteIds);
    }
  }, [computeFilteredSiteIds]);

  const allSelectedItems = useMemo(() => {
    const result = {
      organizational: [] as { level: string; id: string; name: string }[],
      customer: [] as { level: string; id: string; name: string }[],
    };

    organizationalFilterLevels.forEach((level) => {
      const selectedIds = selectedFilters[level.id as keyof SelectedFilters] || [];
      const items = getItemsForLevel(level.id);

      selectedIds.forEach((id) => {
        const item = items.find((item) => item.id === id);
        if (item) {
          result.organizational.push({ level: level.id, id, name: item.name });
        }
      });
    });

    customerFilterLevels.forEach((level) => {
      const selectedIds = selectedFilters[level.id as keyof SelectedFilters] || [];
      const items = getItemsForLevel(level.id);

      selectedIds.forEach((id) => {
        const item = items.find((item) => item.id === id);
        if (item) {
          result.customer.push({ level: level.id, id, name: item.name });
        }
      });
    });

    return result;
  }, [selectedFilters]);

  const filterCount = useMemo(() => {
    return allSelectedItems.organizational.length + allSelectedItems.customer.length;
  }, [allSelectedItems]);

  const toggleSelection = (level: string, itemId: string) => {
    setSelectedFilters((prev) => {
      const currentSelection = prev[level as keyof SelectedFilters] || []
      const newSelection = currentSelection.includes(itemId)
        ? currentSelection.filter((id) => id !== itemId)
        : [...currentSelection, itemId]

      return {
        ...prev,
        [level]: newSelection.length > 0 ? newSelection : undefined,
      }
    })
  }

  const clearFilters = () => {
    setSelectedFilters({})
  }

  const applyFilters = () => {
    onApplyFilters(selectedFilters)
    onOpenChange(false)
  }

  const getSelectionCount = (level: string): number => {
    return (selectedFilters[level as keyof SelectedFilters] || []).length
  }

  const removeFilter = (level: string, id: string) => {
    setSelectedFilters((prev) => {
      const currentSelection = prev[level as keyof SelectedFilters] || []
      const newSelection = currentSelection.filter((itemId) => itemId !== id)

      return {
        ...prev,
        [level]: newSelection.length > 0 ? newSelection : undefined,
      }
    })
  }

  const getFilterLevelName = (levelId: string): string => {
    const orgLevel = organizationalFilterLevels.find((l) => l.id === levelId)
    if (orgLevel) return orgLevel.name

    const custLevel = customerFilterLevels.find((l) => l.id === levelId)
    if (custLevel) return custLevel.name

    return levelId
  }

  const SiteIdsList = useMemo(() => {
    if (filteredSiteIds.length === 0) {
      return <div className="text-sm text-muted-foreground p-2">No sites match the current filters</div>;
    }

    return (
      <div
        ref={siteListRef}
        className="flex flex-wrap gap-1 p-2 rounded-md border"
        data-qa-id="filtered-sites-list"
      >
        {filteredSiteIds.map((siteId) => (
          <Badge key={siteId} variant="outline">
            {siteId}
          </Badge>
        ))}
      </div>
    );
  }, [filteredSiteIds]);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[1000px] max-h-[80vh] flex flex-col">
        <DialogHeader>
          <DialogTitle>Filters</DialogTitle>
          <DialogDescription>
            Select filters to narrow down your data view
          </DialogDescription>
        </DialogHeader>

        <div className="flex flex-row gap-4 flex-1 overflow-hidden">

          <div className="w-full md:w-1/4 border-r pr-4 overflow-y-auto">
            <Accordion type="multiple" className="w-full">
              <div className="mb-4">
                <h3 className="text-sm font-medium mb-2 text-muted-foreground px-1">Organizational Filters</h3>
                {organizationalFilterLevels.map((level) => (
                  <AccordionItem value={level.id} key={level.id}>
                    <AccordionTrigger
                      className="text-sm px-1 py-2 hover:no-underline"
                      data-qa-id={`filter-accordion-trigger-${level.id}`}
                    >
                      <div className="flex justify-between items-center w-full mr-2">
                        <span>{level.name}</span>
                        {getSelectionCount(level.id) > 0 && (
                          <Badge variant="secondary" className="ml-2">
                            {getSelectionCount(level.id)}
                          </Badge>
                        )}
                      </div>
                    </AccordionTrigger>
                    <AccordionContent className="p-0">
                      <Command className="bg-muted/50">
                        <CommandInput
                          placeholder={`Search ${level.name}...`}
                          className="h-8 text-xs"
                          data-qa-id={`filter-search-${level.id}`}
                        />
                        <CommandList className="max-h-[200px]">
                          <CommandEmpty>No items found.</CommandEmpty>
                          <CommandGroup>
                            {getItemsForLevel(level.id).map((item) => (
                              <CommandItem
                                key={item.id}
                                onSelect={() => toggleSelection(level.id, item.id)}
                                className="text-xs cursor-pointer"
                                data-qa-id={`filter-item-${level.id}-${item.id}`}
                              >
                                {item.name}
                                <Check
                                  className={cn(
                                    "ml-auto h-3 w-3",
                                    (selectedFilters[level.id as keyof SelectedFilters] || []).includes(item.id)
                                      ? "opacity-100"
                                      : "opacity-0",
                                  )}
                                />
                              </CommandItem>
                            ))}
                          </CommandGroup>
                        </CommandList>
                      </Command>
                    </AccordionContent>
                  </AccordionItem>
                ))}
              </div>

              <Separator className="my-4" />

              <div>
                <h3 className="text-sm font-medium mb-2 text-muted-foreground px-1">Customer Filters</h3>
                {customerFilterLevels.map((level) => (
                  <AccordionItem value={level.id} key={level.id}>
                    <AccordionTrigger
                      className="text-sm px-1 py-2 hover:no-underline"
                      data-qa-id={`filter-accordion-trigger-${level.id}`}
                    >
                      <div className="flex justify-between items-center w-full mr-2">
                        <span>{level.name}</span>
                        {getSelectionCount(level.id) > 0 && (
                          <Badge variant="secondary" className="ml-2">
                            {getSelectionCount(level.id)}
                          </Badge>
                        )}
                      </div>
                    </AccordionTrigger>
                    <AccordionContent className="p-0">
                      <Command className="bg-muted/50">
                        <CommandInput
                          placeholder={`Search ${level.name}...`}
                          className="h-8 text-xs"
                          data-qa-id={`filter-search-${level.id}`}
                        />
                        <CommandList className="max-h-[200px]">
                          <CommandEmpty>No items found.</CommandEmpty>
                          <CommandGroup>
                            {getItemsForLevel(level.id).map((item) => (
                              <CommandItem
                                key={item.id}
                                onSelect={() => toggleSelection(level.id, item.id)}
                                className="text-xs cursor-pointer"
                                data-qa-id={`filter-item-${level.id}-${item.id}`}
                              >
                                {item.name}
                                <Check
                                  className={cn(
                                    "ml-auto h-3 w-3",
                                    (selectedFilters[level.id as keyof SelectedFilters] || []).includes(item.id)
                                      ? "opacity-100"
                                      : "opacity-0",
                                  )}
                                />
                              </CommandItem>
                            ))}
                          </CommandGroup>
                        </CommandList>
                      </Command>
                    </AccordionContent>
                  </AccordionItem>
                ))}
              </div>
            </Accordion>
          </div>

          <div className="flex-1 flex flex-col overflow-y-auto">
            <div className="mb-4">
              <div className="flex flex-col space-y-2 rounded-md bg-muted/50 p-3 text-sm">
                <p className="font-medium">How filters work:</p>
                <div className="flex flex-col space-y-2">
                  <div className="space-y-1">
                    <p className="font-medium text-brand-navy">Organizational Filters</p>
                    <p className="text-muted-foreground text-xs">
                      Adding more organizational filters will <span className="font-medium">expand</span> your
                      results. For example, selecting multiple sites will show data for all selected locations.
                    </p>
                  </div>
                  <div className="space-y-1">
                    <p className="font-medium text-brand-navy">Customer Filters</p>
                    <p className="text-muted-foreground text-xs">
                      Adding more customer filters will <span className="font-medium">narrow</span> your results. For
                      example, selecting multiple contract types will only show data that matches all selected
                      criteria.
                    </p>
                  </div>
                </div>
              </div>
            </div>

            <div className="mb-4">
              <div className="flex justify-between items-center mb-2">
                <h3 className="text-sm font-medium">Selected Filters:</h3>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => setIsSiteListSidebarOpen(prev => !prev)}
                  className="text-muted-foreground"
                  data-qa-id="toggle-site-list-sidebar"
                >
                  {isSiteListSidebarOpen ? <PanelRightClose className="h-4 w-4 mr-1" /> : <PanelRightOpen className="h-4 w-4 mr-1" />}
                  Sites
                  {!isSiteListSidebarOpen && (
                    <Badge variant="secondary" className="ml-2">
                      {filteredSiteIds.length}
                    </Badge>
                  )}
                </Button>
              </div>

              {filterCount === 0 ? (
                <p className="text-sm text-muted-foreground">No filters selected</p>
              ) : (
                <div className="space-y-3">
                  {allSelectedItems.organizational.length > 0 && (
                    <div className="space-y-2">
                      <h4 className="text-xs font-medium text-muted-foreground">Organizational Filters:</h4>
                      <div className="flex flex-wrap gap-2">
                        {allSelectedItems.organizational.map((item) => (
                          <Badge
                            key={`${item.level}-${item.id}`}
                            variant="secondary"
                            className="flex items-center gap-1 bg-blue-50 dark:bg-blue-900/50 text-blue-800 dark:text-blue-200 border-blue-200 dark:border-blue-800"
                            data-qa-id={`selected-filter-${item.level}-${item.id}`}
                          >
                            <span className="text-xs text-muted-foreground">{getFilterLevelName(item.level)}:</span>
                            {item.name}
                            <Button
                              variant="ghost"
                              size="sm"
                              className="h-4 w-4 p-0 ml-1 text-blue-600 hover:text-blue-800 dark:text-blue-300 dark:hover:text-blue-100"
                              onClick={() => removeFilter(item.level, item.id)}
                              data-qa-id={`remove-filter-${item.level}-${item.id}`}
                            >
                              <X className="h-3 w-3" />
                            </Button>
                          </Badge>
                        ))}
                      </div>
                    </div>
                  )}

                  {allSelectedItems.customer.length > 0 && (
                    <div className="space-y-2">
                      <h4 className="text-xs font-medium text-muted-foreground">Customer Filters:</h4>
                      <div className="flex flex-wrap gap-2">
                        {allSelectedItems.customer.map((item) => (
                          <Badge
                            key={`${item.level}-${item.id}`}
                            variant="secondary"
                            className="flex items-center gap-1 bg-green-50 dark:bg-green-900/50 text-green-800 dark:text-green-200 border-green-200 dark:border-green-800"
                            data-qa-id={`selected-filter-${item.level}-${item.id}`}
                          >
                            <span className="text-xs text-muted-foreground">{getFilterLevelName(item.level)}:</span>
                            {item.name}
                            <Button
                              variant="ghost"
                              size="sm"
                              className="h-4 w-4 p-0 ml-1 text-green-600 hover:text-green-800 dark:text-green-300 dark:hover:text-green-100"
                              onClick={() => removeFilter(item.level, item.id)}
                              data-qa-id={`remove-filter-${item.level}-${item.id}`}
                            >
                              <X className="h-3 w-3" />
                            </Button>
                          </Badge>
                        ))}
                      </div>
                    </div>
                  )}
                </div>
              )}
            </div>
          </div>

          <div className={cn(
            "transition-all duration-300 ease-in-out overflow-y-auto flex flex-col border-l",
            isSiteListSidebarOpen ? "w-full md:w-1/4 pl-4" : "w-0 p-0 border-l-0"
          )}>
            <div className={cn("flex flex-col", !isSiteListSidebarOpen && "invisible")}>
              <div className="flex items-center justify-between pt-1 mb-2 sticky top-0 bg-background z-10">
                <h4 className="text-sm font-medium flex items-center gap-1">
                  Sites Included
                  <TooltipProvider>
                    <Tooltip>
                      <TooltipTrigger asChild>
                        <Info className="h-4 w-4 text-muted-foreground cursor-help" />
                      </TooltipTrigger>
                      <TooltipContent>
                        <p className="max-w-xs text-xs">
                          These are the Site IDs that match your current filter criteria. More organizational filters
                          will include more sites, while more customer filters will narrow down the list.
                        </p>
                      </TooltipContent>
                    </Tooltip>
                  </TooltipProvider>
                </h4>
                <span className="text-xs text-muted-foreground font-normal">
                  {filteredSiteIds.length} site{filteredSiteIds.length !== 1 ? "s" : ""}
                </span>
              </div>
              {SiteIdsList}
            </div>
          </div>

        </div>

        <DialogFooter className="mt-4 pt-4 border-t">
          <Button
            variant="ghost"
            onClick={clearFilters}
            data-qa-id="clear-filters-button"
          >
            Clear Filters
          </Button>
          <Button
            onClick={applyFilters}
            data-qa-id="apply-filters-button"
          >
            Apply Filters
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
