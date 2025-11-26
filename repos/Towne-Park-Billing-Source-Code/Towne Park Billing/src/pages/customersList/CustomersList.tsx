import { routes } from "@/authConfig";
import { Icons } from "@/components/Icons/Icons";
import { DataTableFacetedFilter } from "@/components/TableFacetedFilter";
import { GlobalPagination } from "@/components/TablePagination";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip";
import { useToast } from "@/components/ui/use-toast";
import { useAuth } from "@/contexts/AuthContext";
import { Statement } from "@/lib/models/Statement";
import { Label } from "@radix-ui/react-label";
import {
  ColumnDef,
  ColumnFiltersState,
  SortingState,
  VisibilityState,
  flexRender,
  getCoreRowModel,
  getFilteredRowModel,
  getPaginationRowModel,
  getSortedRowModel,
  useReactTable
} from "@tanstack/react-table";
import { CheckCircle, EyeIcon, File, FileText, Hourglass, Info } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { PulseLoader } from "react-spinners";
import { DataTableColumnHeader } from "../../components/TableHeaderOptions";
import { DataTableViewOptions } from "../../components/TableViewOptions";

enum BillingStatus {
  Ready = "Ready for Invoice",
  Generated = "Generated",
  Waiting = "Pending",
}

const statusIcons = {
  [BillingStatus.Ready]: <CheckCircle />,
  [BillingStatus.Generated]: <FileText />,
  [BillingStatus.Waiting]: <Hourglass />,
};

interface CustomerSummary {
  customerSiteId: string;
  siteNumber: string;
  siteName: string;
  district: string;
  billingType: string;
  contractType: string;
  deposits: boolean;
  readyForInvoiceStatus: BillingStatus | null;
}

export function CustomersList() {
  const [customers, setCustomers] = useState<CustomerSummary[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [shouldRefetchCustomers, setShouldRefetchCustomers] = useState(false);
  const [sorting, setSorting] = useState<SortingState>([{ id: "siteNumber", desc: false }]);
  const [columnFilters, setColumnFilters] = useState<ColumnFiltersState>([]);
  const [columnVisibility, setColumnVisibility] = useState<VisibilityState>({});
  const [rowSelection, setRowSelection] = useState<Record<string, boolean>>({});
  const [selectedCustomerId, setSelectedCustomerId] = useState<string | null>(null);
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [bulkDialogOpen, setBulkDialogOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [isNewCustomerDialogOpen, setIsNewCustomerDialogOpen] = useState(false);
  const [siteId, setSiteId] = useState<string>("");
  const navigate = useNavigate();
  const { toast } = useToast();
  const { userRoles } = useAuth();

  useEffect(() => {
    const fetchData = async () => {
      var claimsHeader = JSON.stringify(userRoles);
      try {
        const [customersResponse, statementsResponse] = await Promise.all([
          fetch("/api/customers", {
            headers: {
              "x-client-roles": claimsHeader,
            },
          }),
          fetch("/api/customers/statements", {
            headers: {
              "x-client-roles": claimsHeader,
            },
          }),
        ]);
        const customersData: CustomerSummary[] = await customersResponse.json();
        const statementsData: Statement[] = await statementsResponse.json();

        const currentMonth = new Date().toISOString().substring(0, 7);

        const customerData = customersData.map((customer) => {
          const billingType = customer.billingType ? customer.billingType.toLowerCase() : "";
          const contractType = customer.contractType ?? "";
          const deposits = customer.deposits ?? false;
          const customerStatement = statementsData.find(
            (statement) => statement.customerSiteId === customer.customerSiteId && statement.createdMonth === currentMonth
          );

          let status;
          if (customerStatement) {
            status = BillingStatus.Generated;
          } else if (billingType === "advanced") {
            status = BillingStatus.Ready;
          } else if (
            billingType === "arrears" &&
            customer.readyForInvoiceStatus === BillingStatus.Ready
          ) {
            status = BillingStatus.Ready;
          } else {
            status = BillingStatus.Waiting;
          }

          return {
            ...customer,
            billingType,
            contractType,
            deposits,
            readyForInvoiceStatus: status,
          };
        });

        setCustomers(customerData);
        setIsLoading(false);
      } catch (error) {
        console.error("Error fetching data:", error);
        setIsLoading(false);
      }
    };

    fetchData();
  }, [shouldRefetchCustomers, userRoles]);

  const isBillingAdmin = userRoles.includes('billingAdmin');
  const isBillingManager = userRoles.includes('billingManager');

  const handleDialogOpen = (customerSiteId: string) => {
    setSelectedCustomerId(customerSiteId);
    setIsDialogOpen(true);
  };

  const handleDialogClose = () => {
    setIsDialogOpen(false);
    setSelectedCustomerId(null);
  };

  const selectedCustomers = Object.keys(rowSelection);

  const selectedCustomer = customers.find(
    (customer) => customer.customerSiteId === selectedCustomerId
  );
  
  const customerStatusMessage =
    selectedCustomer?.readyForInvoiceStatus !== BillingStatus.Ready
      ? "This customer is not in Ready status, this may only be generated by an Admin."
      : null;
  
  const handleGenerateStatement = () => {
    if (selectedCustomerId) {
      if (customerStatusMessage) {
        toast({
          title: "Status Not Ready",
          description: customerStatusMessage,
        });
        return;
      }
  
      setSaving(true);
      fetch(`/api/customers/${selectedCustomerId}/statement`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
      })
        .then(() => {
          toast({
            title: "Request accepted",
            description: "Statement generation was successfully requested.",
          });
  
          table.resetRowSelection();
  
          handleDialogClose();
          setSaving(false);
        })
        .catch(() => {
          toast({
            title: "Error",
            description: "An error occurred while generating the statement.",
          });
          handleDialogClose();
          setSaving(false);
        });
    }
  };
  
  const handleAdminGenerateStatement = () => {
    if (selectedCustomerId) {
      setSaving(true);
      fetch(`/api/customers/${selectedCustomerId}/statement`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
      })
        .then(() => {
          toast({
            title: "Request accepted",
            description: "Statement generation was successfully requested by Admin.",
          });

          table.resetRowSelection();

          handleDialogClose();
          setSaving(false);
        })
        .catch(() => {
          toast({
            title: "Error",
            description: "An error occurred while generating the statement.",
          });
          handleDialogClose();
          setSaving(false);
        });
    }
  };

  const handleBulkGenerateStatements = useCallback(() => {
    const selectedCustomerSiteIds = Object.entries(rowSelection)
      .filter(([, isSelected]) => isSelected)
      .map(([key]) => {
        const customerIndex = parseInt(key, 10);
        const customer = customers[customerIndex];

        return customer?.readyForInvoiceStatus === BillingStatus.Ready ? customer.customerSiteId : null;
      })
      .filter((id) => id !== undefined && id !== null);

    if (selectedCustomerSiteIds.length < 2) {
      toast({
        title: "Error",
        description: "Please select at least 2 'Ready' sites for bulk generation.",
      });
      return;
    }

    setSaving(true);

    fetch("/api/customers/statements", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ customerSiteIds: selectedCustomerSiteIds }),
    })
      .then(() => {
        toast({
          title: "Bulk Request Accepted",
          description: "Statement generation was successfully requested for selected sites.",
        });

        table.resetRowSelection();

        setBulkDialogOpen(false);
        setSaving(false);
      })
      .catch(() => {
        toast({
          title: "Error",
          description: "An error occurred while generating bulk statements.",
        });
        setBulkDialogOpen(false);
        setSaving(false);
      });
  }, [customers, rowSelection, toast]);

  const handleAdminBulkGenerateStatements = useCallback(() => {
    const selectedCustomerSiteIds = Object.entries(rowSelection)
      .filter(([, isSelected]) => isSelected)
      .map(([key]) => {
        const customerIndex = parseInt(key, 10);
        const customer = customers[customerIndex];
        return customer?.customerSiteId;
      })
      .filter((id) => id !== undefined && id !== null);

    setSaving(true);

    fetch("/api/customers/statements", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ customerSiteIds: selectedCustomerSiteIds }),
    })
      .then(() => {
        toast({
          title: "Bulk Request Accepted by Admin",
          description: "Statement generation was successfully forced for selected sites.",
        });

        table.resetRowSelection();

        setBulkDialogOpen(false);
        setSaving(false);
      })
      .catch(() => {
        toast({
          title: "Error",
          description: "An error occurred while generating bulk statements.",
        });
        setBulkDialogOpen(false);
        setSaving(false);
      });
  }, [customers, rowSelection, toast]);

  const handleNewCustomerDialogOpen = () => {
    setIsNewCustomerDialogOpen(true);
  };

  const handleNewCustomerDialogClose = () => {
    setIsNewCustomerDialogOpen(false);
  };

  const handleAddCustomerSite = () => {
    if (siteId.trim()) {
      setSaving(true);

      fetch(`/api/customers/${siteId}`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
      })
        .then((response) => {
          if (response.status === 404) {
            return response.json().then((error) => {
              toast({
                title: "Error",
                description: error.errorMessage,
                duration: Infinity,
              });
            });
          } else if (response.status === 409) {
            return response.json().then((error) => {
              toast({
                title: "Error",
                description: error.errorMessage,
                duration: Infinity,
              });
            });
          } else if (response.ok) {
            toast({
              title: "Success",
              description: "Customer site added successfully.",
            });
            setShouldRefetchCustomers(true);
          }
        })
        .catch(() => {
          toast({
            title: "Error",
            description: "An unexpected error occurred while creating Customer Site.",
          });
        })
        .finally(() => {
          setSaving(false);
          setIsNewCustomerDialogOpen(false);
        });
    } else {
      toast({
        title: "Error",
        description: "Site ID cannot be empty.",
      });
    }
  };

  const columns: ColumnDef<CustomerSummary>[] = [
    {
      id: "select",
      header: ({ table }) => (
        <Checkbox
          checked={
            table.getIsAllPageRowsSelected() ||
            (table.getIsSomePageRowsSelected() && "indeterminate")
          }
          onCheckedChange={(value) => table.toggleAllPageRowsSelected(!!value)}
          data-qa-id="checkbox-selectAllRows"
        />
      ),
      cell: ({ row }) => (
        <Checkbox
          checked={row.getIsSelected()}
          onCheckedChange={(value) => row.toggleSelected(!!value)}
          data-qa-id={`checkbox-selectRow-${row.index}`}
        />
      ),
      enableHiding: false,
    },
    {
      accessorKey: "siteNumber",
      header: ({ column }) => <DataTableColumnHeader column={column} title="Site ID" />,
      cell: ({ row }) => <div className="lowercase">{row.getValue("siteNumber")}</div>,
      enableHiding: false,
    },
    {
      accessorKey: "siteName",
      header: ({ column }) => <DataTableColumnHeader column={column} title="Customer" />,
      cell: ({ row }) => <div className="capitalize">{row.getValue("siteName")}</div>,
      enableHiding: false,
    },
    {
      accessorKey: "readyForInvoiceStatus",
      header: ({ column }) => (
        <div className="flex items-center gap-2">
          <DataTableColumnHeader column={column} title="Billing Status" />
          <TooltipProvider>
            <Tooltip>
              <TooltipTrigger asChild>
                <span tabIndex={0} data-qa-id="tooltip-trigger-billingStatusInfo">
                  <Info className="inline-block text-gray-400" size={17} />
                </span>
              </TooltipTrigger>
              <TooltipContent>
                <p className="text-sm">Filter by billing status:</p>
                <ul className="flex flex-col items-start space-y-1 text-sm">
                  <li className="flex items-center">
                    <div>{statusIcons[BillingStatus.Ready]}</div>
                    <span className="ml-2">Ready</span>
                  </li>
                  <li className="flex items-center">
                    <div>{statusIcons[BillingStatus.Generated]}</div>
                    <span className="ml-2">Generated</span>
                  </li>
                  <li className="flex items-center">
                    <div>{statusIcons[BillingStatus.Waiting]}</div>
                    <span className="ml-2">Waiting</span>
                  </li>
                </ul>
              </TooltipContent>
            </Tooltip>
          </TooltipProvider>
        </div>
      ),
      cell: ({ row }) => {
        const status = row.getValue("readyForInvoiceStatus") as BillingStatus;
        return (
          <div className="flex items-center justify-center">
            <TooltipProvider>
              <Tooltip>
                <TooltipTrigger asChild>
                  <div data-qa-id={`tooltip-trigger-billingStatus-${row.index}`}>
                    {statusIcons[status]}
                  </div>
                </TooltipTrigger>
                <TooltipContent>
                  {status === BillingStatus.Ready && "Ready"}
                  {status === BillingStatus.Generated && "Statement Generated"}
                  {status === BillingStatus.Waiting && "Waiting"}
                </TooltipContent>
              </Tooltip>
            </TooltipProvider>
          </div>
        );
      },
      filterFn: (row, columnId, filterValues) => {
        if (!filterValues || filterValues.length === 0) return true;
        return filterValues.includes(row.getValue(columnId));
      },
    },
    {
      accessorKey: "district",
      header: ({ column }) => <DataTableColumnHeader column={column} title="District" />,
      cell: ({ row }) => <div className="capitalize">{row.getValue("district")}</div>,
    },
    {
      accessorKey: "billingType",
      header: ({ column }) => <DataTableColumnHeader column={column} title="Billing Type" />,
      cell: ({ row }) => <div className="capitalize">{row.getValue("billingType")}</div>,
    },
    {
      accessorKey: "contractType",
      header: ({ column }) => <DataTableColumnHeader column={column} title="Contract Type" />,
      cell: ({ row }) => <div className="capitalize">{row.getValue("contractType")}</div>,
    },
    {
      accessorKey: "deposits",
      header: ({ column }) => <DataTableColumnHeader column={column} title="Deposits" />,
      cell: ({ row }) => <div className="capitalize">{row.getValue("deposits") ? "Yes" : "No"}</div>,
    },
    {
      id: "actions",
      header: "Actions",
      cell: ({ row }) => (
        <div className="flex space-x-4">
          <TooltipProvider>
            {isBillingManager && (
            <Tooltip>
              <TooltipTrigger asChild>
                <Button
                  variant="ghost"
                  className="h-8 w-8 p-0"
                  onClick={() => handleDialogOpen(row.original.customerSiteId)}
                  data-qa-id={`button-generateStatement-${row.original.customerSiteId}`}
                >
                  <File className="h-4 w-4" />
                </Button>
              </TooltipTrigger>
              <TooltipContent>
                <p>Generate Statement</p>
              </TooltipContent>
            </Tooltip>
            )}
            <Tooltip>
              <TooltipTrigger asChild>
                <Button
                  variant="ghost"
                  className="h-8 w-8 p-0"
                  onClick={() => navigate(routes.customersDetailsWithId(row.original.customerSiteId))}
                  data-qa-id={`button-viewDetails-${row.original.customerSiteId}`}
                >
                  <EyeIcon className="h-4 w-4" />
                </Button>
              </TooltipTrigger>
              <TooltipContent>
                <p>View Details</p>
              </TooltipContent>
            </Tooltip>
          </TooltipProvider>
        </div>
      ),
      enableHiding: false,
    },
  ];
  const table = useReactTable({
    data: customers,
    columns,
    state: {
      sorting,
      columnFilters,
      columnVisibility,
      rowSelection,
    },
    onSortingChange: setSorting,
    onColumnFiltersChange: setColumnFilters,
    onColumnVisibilityChange: setColumnVisibility,
    onRowSelectionChange: setRowSelection,
    getCoreRowModel: getCoreRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
    getSortedRowModel: getSortedRowModel(),
  });

  const openBulkDialog = () => {
    const selectedCustomerIds = Object.keys(rowSelection).filter((key) => rowSelection[key]);

    if (selectedCustomerIds.length < 2) {
      toast({
        title: "Error",
        description: "Please select at least 2 sites for bulk generation.",
      });
      return;
    }

    setBulkDialogOpen(true);
  };

  const selectedCustomerSiteIds = Object.entries(rowSelection)
    .filter(([, isSelected]) => isSelected)
    .map(([key]) => customers[parseInt(key)]?.customerSiteId)
    .filter((id) => id !== undefined && id !== null);

  const notReadyCustomers = customers.filter(
    (cust) =>
      selectedCustomerSiteIds.includes(cust.customerSiteId) &&
      cust.readyForInvoiceStatus !== BillingStatus.Ready
  );
 
  return (
    <TooltipProvider>
    <div className="container mx-auto p-4">
      <div className="flex flex-col py-4 space-y-4">
        <div className="flex space-x-2 justify-end">
          {isBillingAdmin && (
            <Button variant="secondary" onClick={handleNewCustomerDialogOpen} data-qa-id="button-addCustomer">
              Add Customer
            </Button>
          )}
          {isBillingManager && (
            <Button onClick={openBulkDialog} data-qa-id="button-generateStatements">Generate Statements</Button>
          )}
        </div>
        <div className="flex items-center justify-between space-x-4">
          <div className="flex items-center space-x-4">
            <Input
              placeholder="Search..."
              value={table.getState().globalFilter ?? ""}
              onChange={(event) => table.setGlobalFilter(event.target.value)}
              className="max-w-sm"
              data-qa-id="input-searchCustomers"
            />
            <DataTableFacetedFilter
              column={table.getColumn("readyForInvoiceStatus")}
              options={[
                { label: "Ready", value: BillingStatus.Ready, icon: Icons.checkCircle },
                { label: "Generated", value: BillingStatus.Generated, icon: Icons.fileText },
                { label: "Waiting", value: BillingStatus.Waiting, icon: Icons.hourglass },
              ]}
              title="Billing Status"
              data-qa-id="filter-billingStatus"
            />
          </div>
          <div className="flex items-center space-x-4">
            <DataTableViewOptions table={table} data-qa-id="table-viewOptions" />
          </div>
        </div>
      </div>
      <div className="rounded-md border mb-4">
        <Table data-qa-id="table-customers">
          <TableHeader>
            {table.getHeaderGroups().map((headerGroup) => (
              <TableRow key={headerGroup.id}>
                {headerGroup.headers.map((header) => (
                  <TableHead key={header.id}>
                    {header.isPlaceholder
                      ? null
                      : flexRender(header.column.columnDef.header, header.getContext())}
                  </TableHead>
                ))}
              </TableRow>
            ))}
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableRow>
                <TableCell colSpan={columns.length}>
                  <Skeleton className="h-lvh w-full" data-test-id="skeleton" />
                </TableCell>
              </TableRow>
            ) : (
              <>
                {table.getRowModel().rows.length ? (
                  table.getRowModel().rows.map((row) => (
                    <TableRow key={row.id} data-state={row.getIsSelected() && "selected"} data-qa-id={`row-customer-${row.id}`}>
                      {row.getVisibleCells().map((cell) => (
                        <TableCell key={cell.id}>
                          {flexRender(cell.column.columnDef.cell, cell.getContext())}
                        </TableCell>
                      ))}
                    </TableRow>
                  ))
                ) : (
                  <TableRow>
                    <TableCell colSpan={columns.length} className="h-24 text-center">
                      No results.
                    </TableCell>
                  </TableRow>
                )}
              </>
            )}
          </TableBody>
        </Table>
      </div>
      <GlobalPagination table={table} />

      <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen} data-qa-id="dialog-generateStatement">
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Confirm Statement Generation</DialogTitle>
          </DialogHeader>
          <DialogDescription>
            Are you sure you want to generate a statement for this site?
            {customerStatusMessage && (
      <p className="text-xs mt-5">{customerStatusMessage}</p>
    )}
          </DialogDescription>
          <DialogFooter>
            <Button variant="outline" onClick={handleDialogClose} disabled={saving} data-qa-id="button-cancelGenerateStatement">
              Cancel
            </Button>
            <Button onClick={handleGenerateStatement} disabled={saving} data-qa-id="button-confirmGenerateStatement">
              {saving ? <PulseLoader color="#2563EB" size={8} margin={2} /> : "Generate Statement"}
            </Button>
          </DialogFooter>
          {isBillingAdmin && (
            <Button
              onClick={handleAdminGenerateStatement}
              variant="secondary"
              disabled={saving}
              className="w-min"
              data-qa-id="button-adminGenerateStatement"
            >
              Admin Proceed
            </Button>
          )}
        </DialogContent>
      </Dialog>

      <Dialog open={bulkDialogOpen} onOpenChange={setBulkDialogOpen} data-qa-id="dialog-bulkGenerateStatements">
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              Generate {selectedCustomers.length - notReadyCustomers.length} Statements
            </DialogTitle>
          </DialogHeader>
          <DialogDescription>
            <p>
              You are about to generate statements for{" "}
              {selectedCustomers.length - notReadyCustomers.length} customers.
            </p>
            <p className="text-xs mt-5">
              {notReadyCustomers.length} customers aren't yet in 'Ready' status and will
              not have statements generated.
            </p>
          </DialogDescription>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setBulkDialogOpen(false)}
              disabled={saving}
              data-qa-id="button-cancelBulkGenerateStatements"
            >
              Cancel
            </Button>
            <Button onClick={handleBulkGenerateStatements} disabled={saving} data-qa-id="button-confirmBulkGenerateStatements">
              {saving ? <PulseLoader size={8} margin={2} /> : "Yes, proceed"}
            </Button>
          </DialogFooter>
          {isBillingAdmin && notReadyCustomers.length > 0 && (
            <Button
              onClick={handleAdminBulkGenerateStatements}
              variant="secondary"
              disabled={saving}
              className="w-min"
              data-qa-id="button-adminBulkGenerateStatements"
            >
              Admin Proceed
            </Button>
          )}
        </DialogContent>
      </Dialog>

      <Dialog open={isNewCustomerDialogOpen} onOpenChange={setIsNewCustomerDialogOpen} data-qa-id="dialog-addCustomer">
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Add a Customer</DialogTitle>
          </DialogHeader>
          <DialogDescription>
            Enter a Site ID to fetch information and add the Customer record.
          </DialogDescription>
          <div className="">
            <Label htmlFor="siteId">Site ID</Label>
            <Input
              id="siteId"
              placeholder="e.g, 0846"
              value={siteId}
              onChange={(e) => setSiteId(e.target.value)}
              data-qa-id="input-siteId"
            />
          </div>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={handleNewCustomerDialogClose} disabled={saving} data-qa-id="button-cancelAddCustomer">
              Cancel
            </Button>
            <Button type="submit" onClick={handleAddCustomerSite} data-qa-id="button-confirmAddCustomer">
              {saving ? <PulseLoader size={8} margin={2} /> : "Add Site"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  </TooltipProvider>
  );
}
