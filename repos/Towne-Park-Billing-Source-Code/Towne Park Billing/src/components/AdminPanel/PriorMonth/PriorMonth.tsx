import { Button } from "@/components/ui/button";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Skeleton } from "@/components/ui/skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip";
import { useToast } from "@/components/ui/use-toast";
import { format } from "date-fns";
import { File } from "lucide-react";
import { useEffect, useState } from "react";
import { PulseLoader } from "react-spinners";

import { DataTableColumnHeader } from "@/components/TableHeaderOptions";
import { GlobalPagination } from "@/components/TablePagination";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { useAuth } from "@/contexts/AuthContext";
import {
  ColumnDef,
  ColumnFiltersState,
  flexRender,
  getCoreRowModel,
  getFilteredRowModel,
  getPaginationRowModel,
  getSortedRowModel,
  SortingState,
  useReactTable,
  VisibilityState,
} from "@tanstack/react-table";

enum BillingStatus {
  Ready = "Ready for Invoice",
  Generated = "Generated",
  Waiting = "Pending",
}

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
export default function PriorMonthWithGrid() {
  const [date, setDate] = useState<Date>(new Date());
  const months = [
    "January", "February", "March", "April", "May", "June",
    "July", "August", "September", "October", "November", "December"
  ];
  const currentYear = new Date().getFullYear();
  const currentMonth = new Date().getMonth(); 
  const selectedYear = date.getFullYear();
  const generateYearOptions = () => {
    const startYear = 2024;
    const numYears = currentYear - startYear + 1;
    return Array.from({ length: numYears }, (_, i) => startYear + i);
};

const years = generateYearOptions();
  const availableMonths = selectedYear === currentYear
  ? months.slice(0, currentMonth + 1)
  : months;

  const handleMonthYearSelect = (month: number, year: number) => {
    const newDate = new Date(year, month, 1);
    setDate(newDate);
  };
  const servicePeriodStart = format(date, "yyyy-MM-dd");
  return (
    <div>
      <h1 className="text-2xl font-bold py-8">Prior Month Statement</h1>
      <div className="mb-6 space-y-2">
        <h3 className="text-lg font-medium">Select Billing Period</h3>
        <p className="text-sm text-muted-foreground">Select the month and year for which you want to generate statements.</p>
      </div>

      <div className="flex flex-col sm:flex-row gap-4 mb-4">
        <Select
          value={date.getMonth().toString()}
          onValueChange={(val) => handleMonthYearSelect(parseInt(val), date.getFullYear())}
        >
          <SelectTrigger className="w-[180px]">
            <SelectValue placeholder="Select Month" />
          </SelectTrigger>
          <SelectContent>
            {months.map((month, index) => (
              <SelectItem key={month} value={index.toString()}>
                {month}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        <Select
          value={date.getFullYear().toString()}
          onValueChange={(val) => {
       
            let newMonth = date.getMonth();
            if (parseInt(val) === currentYear && newMonth > currentMonth) {
              newMonth = currentMonth;
            }
            handleMonthYearSelect(newMonth, parseInt(val));
          }}
        >
          <SelectTrigger className="w-[120px]">
            <SelectValue placeholder="Select Year" />
          </SelectTrigger>
          <SelectContent>
            {years.map((year) => (
              <SelectItem key={year} value={year.toString()}>
                {year}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>
      <div className="mt-8">
        <CustomersList servicePeriodStart={
          servicePeriodStart
        } />
      </div>
    </div>
  );
}

function CustomersList({ servicePeriodStart }: { servicePeriodStart: string }) {
  const { toast } = useToast();
  const [customers, setCustomers] = useState<CustomerSummary[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [sorting, setSorting] = useState<SortingState>([{ id: "siteNumber", desc: false }]);
  const [columnFilters, setColumnFilters] = useState<ColumnFiltersState>([]);
  const [columnVisibility, setColumnVisibility] = useState<VisibilityState>({});
  const [rowSelection, setRowSelection] = useState<Record<string, boolean>>({});
  const [selectedCustomerId, setSelectedCustomerId] = useState<string | null>(null);
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const { userRoles } = useAuth();
  
  useEffect(() => {
    setIsLoading(true);
    setSelectedCustomerId(null);

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
        const statementsData: any[] = await statementsResponse.json();

        const customerData = customersData.map((customer) => {
          const customerStatement = statementsData.find(
            (statement) => statement.customerSiteId === customer.customerSiteId
          );

          let status;
          if (customerStatement) {
            status = BillingStatus.Generated;
          } else if (customer.billingType.toLowerCase() === "advance") {
            status = BillingStatus.Ready;
          } else if (
            customer.billingType.toLowerCase() === "arrears" &&
            customer.readyForInvoiceStatus === BillingStatus.Ready
          ) {
            status = BillingStatus.Ready;
          } else {
            status = BillingStatus.Waiting;
          }
          return {
            ...customer,
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
  },[]);
  const handleDialogOpen = (customerSiteId: string) => {
    setSelectedCustomerId(customerSiteId);
    setIsDialogOpen(true);
  };
  const handleDialogClose = () => {
    setIsDialogOpen(false);
    setSelectedCustomerId(null);
  };
  const columns: ColumnDef<CustomerSummary>[] = [
    {
      accessorKey: "siteNumber",
       header: ({ column }) => <DataTableColumnHeader column={column} title="Site ID" />,
            cell: ({ row }) => <div className="lowercase">{row.getValue("siteNumber")}</div>,
 
    },
    {
      accessorKey: "siteName",
      header: "Customer",
      cell: ({ row }) => <div className="capitalize">{row.getValue("siteName")}</div>,
      enableHiding: false,
    },
    {
      accessorKey: "billingType",
      header: "Billing Type",
      cell: ({ row }) => <div className="capitalize">{row.getValue("billingType")}</div>,
    },
    {
      accessorKey: "contractType",
      header: "Contract Type",
      cell: ({ row }) => <div className="capitalize">{row.getValue("contractType")}</div>,
    },
       {
          id: "actions",
          header: "Actions",
          cell: ({ row }) => (
            <div className="flex space-x-4">
              <TooltipProvider>
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
  const handleGenerateStatement = () => {
    if (selectedCustomerId) {
      if (!selectedCustomerId) return;
  
      setSaving(true);
      fetch(`/api/customers/${selectedCustomerId}/statement?servicePeriodStart=${servicePeriodStart}`, {
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
  return (
    <TooltipProvider>
      <div className="container mx-auto p-4">
        <div className="flex flex-col py-4 space-y-4">
          <div className="flex space-x-2 justify-Start">
                <Input
                          placeholder="Search Site ID"
                          value={table.getState().globalFilter ?? ""}
                          onChange={(event) => table.setGlobalFilter(event.target.value)}
                          className="max-w-sm"
                          data-qa-id="input-searchCustomers"
                        />
          </div>
        </div>
        <div className="rounded-md border mb-4">
          <Table>
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
                    <Skeleton className="h-lvh w-full" />
                  </TableCell>
                </TableRow>
              ) : (
                <>
                  {table.getRowModel().rows.length ? (
                    table.getRowModel().rows.map((row) => (
                      <TableRow key={row.id}
                        data-state={row.getIsSelected() && "selected"}
                        className={row.getIsSelected() ? "" : ""}
                      >
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
                  </DialogDescription>
                  <DialogFooter>
                    <Button variant="outline" onClick={handleDialogClose} disabled={saving} data-qa-id="button-cancelGenerateStatement">
                      Cancel
                    </Button>
                    <Button onClick={handleGenerateStatement} disabled={saving} data-qa-id="button-confirmGenerateStatement">
                      {saving ? <PulseLoader color="#2563EB" size={8} margin={2} /> : "Generate Statement"}
                    </Button>
                  </DialogFooter>
                    <Button
                      onClick={handleGenerateStatement}
                      variant="secondary"
                      disabled={saving}
                      className="w-min"
                      data-qa-id="button-adminGenerateStatement"
                    >
                      Admin Proceed
                    </Button>
                </DialogContent>
              </Dialog>
        
      </div>
    </TooltipProvider>
  );
}
