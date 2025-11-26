import BillingStatementDetails from "@/components/BillingStatement/billingStatementDetails";
import { StatementContext } from "@/components/BillingStatement/StatementContext";
import StatementStatusBadge from "@/components/BillingStatement/statementStatusBadge";
import { InvoiceCacheProvider } from "@/components/invoiceDetails/InvoiceCacheContext";
import SupportingDocuments from "@/components/SupportingDocuments/SupportingDocuments";
import { DataTableFacetedFilter } from '@/components/TableFacetedFilter';
import { DataTableColumnHeader } from '@/components/TableHeaderOptions';
import { GlobalPagination } from '@/components/TablePagination';
import { DataTableViewOptions } from "@/components/TableViewOptions";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Dialog, DialogContent, DialogFooter, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import { Skeleton } from "@/components/ui/skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Textarea } from "@/components/ui/textarea";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip";
import { useToast } from "@/components/ui/use-toast";
import { useAuth } from "@/contexts/AuthContext";
import { ForecastData, Statement, StatementStatus } from "@/lib/models/Statement";
import { formatCurrency } from "@/lib/utils";
import { DialogDescription } from "@radix-ui/react-dialog";
import { ColumnDef, ColumnFiltersState, flexRender, getCoreRowModel, getFilteredRowModel, getPaginationRowModel, getSortedRowModel, SortingState, useReactTable } from "@tanstack/react-table";
import { ChevronDown, ChevronUp, Download, Mail, MailCheck, NotepadText, RefreshCw, UserCheck } from 'lucide-react';
import React, { Fragment, useEffect, useMemo, useState } from 'react';
import { ClipLoader } from "react-spinners";
import PulseLoader from "react-spinners/PulseLoader";
import { useCustomerDetails } from '../customersDetails/CustomersDetailContext';
import { Invoice } from "@/lib/models/Invoice";

enum SendAction {
    SEND_ALL = "SendAll",
    SEND_EMAIL = "SendEmail",
    SEND_TO_GP = "SendToGP"
}

interface StatementsGlobalListProps {
    customerSiteId?: string;
}

const formatServicePeriod = (input: string) => {
    const match = input.match(/([A-Za-z]+)\s\d+\s-\s[A-Za-z]+\s\d+,\s(\d{4})/);
    if (match) {
        const [, month, year] = match;
        return `${month} ${year}`;
    }
    return input;
};

const useFormattedServicePeriod = (input: string) => {
    return useMemo(() => formatServicePeriod(input), [input]);
};


const StatementsGlobalList: React.FC<StatementsGlobalListProps> = ({ customerSiteId }) => {
    const [statements, setStatements] = useState<Statement[]>([]);
    const [sorting, setSorting] = useState<SortingState>([{ id: "createdMonth", desc: false }]);
    const [expandedStatements, setExpandedStatements] = useState<string[]>([]);
    const [columnFilters, setColumnFilters] = useState<ColumnFiltersState>([]);
    const [loading, setLoading] = useState(false);
    const [showNote, setShowNote] = useState(false);
    const [selectedNote, setSelectedNote] = useState<string | null>(null);
    const { fetchCustomerDetails, fetchContractDetails } = useCustomerDetails();
    const [isDialogOpen, setIsDialogOpen] = useState(false);
    const [selectedStatement, setSelectedStatement] = useState<Statement | null>(null);
    const [reloadStatements, setReloadStatements] = useState(false);
    const [saving, setSaving] = useState(false);
    const [pdfLoadingStates, setPdfLoadingStates] = useState<{ [key: string]: boolean }>({});
    const { toast } = useToast();
    const [isEmailDialogOpen, setIsEmailDialogOpen] = useState(false);
    const [isResendDialogOpen, setIsResendDialogOpen] = useState(false);
    const [isBulkEmailDialogOpen, setIsBulkEmailDialogOpen] = useState(false);
    const [selectedSendAction, setSelectedSendAction] = useState<SendAction | "">("");
    const [selectedEmailStatement, setSelectedEmailStatement] = useState<Statement | null>(null);
    const [isRegenerateDialogOpen, setIsRegenerateDialogOpen] = useState(false);
    const [selectedRegenerateStatement, setSelectedRegenerateStatement] = useState<Statement | null>(null);
    const { contractDetails } = useCustomerDetails();
    const { userRoles } = useAuth();
    const [error, setError] = useState<string | null>(null);
    const [invoice, setInvoice] = useState<Invoice>();

    const HIDDEN_COLUMNS = ['forecastDeviationPercent', 'forecastDeviationDollar'];

    const hideColumns = (keys: string[]): { [key: string]: boolean } => {
        return keys.reduce((acc, key) => {
            acc[key] = false;
            return acc;
        }, {} as { [key: string]: boolean });
    };

    useEffect(() => {
        const fetchStatements = async () => {
            setLoading(true);
            var claimsHeader = JSON.stringify(userRoles);
            try {
                const response = await fetch(
                    customerSiteId
                        ? `/api/customers/${customerSiteId}/statements`
                        : '/api/customers/statements', {
                            headers: {
                                "x-client-roles": claimsHeader,
                            },
                        },
                );
                const data = await response.json();
                setStatements(data.map((item: Statement) => ({
                    ...item,
                    status: item.status as StatementStatus,
                })));
            } catch (error) {
                console.error("Failed to fetch statements:", error);
            } finally {
                setLoading(false);
            }
        };
        fetchStatements();
    }, [reloadStatements, customerSiteId, userRoles]);

    const isBillingManager = userRoles.includes('billingManager');

    const toggleStatement = async (id: string, customerSiteId: string) => {
        setExpandedStatements(prev => (
            prev.includes(id) ? prev.filter(sid => sid !== id) : [...prev, id]
        ));

        if (!expandedStatements.includes(id)) {
            try {
                await fetchCustomerDetails(customerSiteId);
                await fetchContractDetails(customerSiteId);
            } catch (error) {
                console.error(`Failed to fetch details for site ID: ${customerSiteId}`, error);
            }
        }
    };

    const parseHtmlString = (htmlString: string): string => {
        const parser = new DOMParser();
        const doc = parser.parseFromString(htmlString, 'text/html');
        return doc.body.textContent || '';
    };

    const openNoteDialog = (note: string) => {
        const cleanNote = parseHtmlString(note);
        setSelectedNote(cleanNote);
        setShowNote(true);
    };

    const handleApprove = (statement: Statement) => {
        setSelectedStatement(statement);
        setIsDialogOpen(true);
    };

    const printPDF = async (statement: Statement) => {
        setPdfLoadingStates(prev => ({ ...prev, [statement.id]: true }));

        try {
            const response = await fetch(`/api/statement/${statement.id}/generate-pdf`, {
                method: "GET",
                headers: { "Content-Type": "application/json" },
            });
            if (!response.ok) {
                throw new Error("Failed to generate PDF");
            }

            const highestInvoiceNumber = statement.invoices.reduce((max, invoice) => {
                return invoice.invoiceNumber > max ? invoice.invoiceNumber : max;
            }, statement.invoices[0].invoiceNumber);

            const blob = await response.blob();
            const url = window.URL.createObjectURL(blob);

            const link = document.createElement('a');
            link.href = url;
            link.download = `${highestInvoiceNumber.substring(0-10)}_Invoice.pdf`; // Name your file here
            link.style.display = 'none';

            document.body.appendChild(link);
            link.click();

            window.URL.revokeObjectURL(url); // Clean up the URL object
            document.body.removeChild(link);

        } catch (error) {
            console.error("Error downloading PDF:", error);
            toast({
                title: "Error",
                description: "An error occurred while generating the PDF.",
            });
        } finally {
            setPdfLoadingStates(prev => ({ ...prev, [statement.id]: false }));
        }
    };

    const checkDeviationExceeded = (forecastData: ForecastData): boolean => {
        return (
            (contractDetails?.deviationPercentage !== undefined &&
                forecastData.forecastDeviationPercentage > contractDetails.deviationPercentage) &&
            (contractDetails?.deviationAmount !== undefined &&
                forecastData.forecastDeviationAmount > contractDetails.deviationAmount)
        );
    };

    const handleDialogConfirm = () => {
        if (selectedStatement) {
            setSaving(true);

            const forecastData = mapToForecastData(JSON.parse(selectedStatement.forecastData));

            const isDeviationExceeded = checkDeviationExceeded(forecastData);

            const nextStatus = selectedStatement.status === StatementStatus.AR_REVIEW
                ? (isDeviationExceeded ? StatementStatus.APPROVAL_TEAM : StatementStatus.READY_TO_SEND)
                : StatementStatus.READY_TO_SEND;

            selectedStatement.status = nextStatus;
            setStatements(prev =>
                prev.map(s =>
                    s.customerSiteId === selectedStatement.customerSiteId && s.id === selectedStatement.id
                        ? { ...s, status: nextStatus }
                        : s
                )
            );

            fetch(`/api/statement/${selectedStatement.id}/update`, {
                method: "PATCH",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    status: nextStatus.toString(),
                    customerSiteId: selectedStatement.customerSiteId,
                }),
            })
                .then(() => {
                    toast({
                        title: "Statement Status Updated",
                        description: nextStatus === StatementStatus.APPROVAL_TEAM
                            ? "The statement has been sent to the Approval Team due to exceeded deviation thresholds."
                            : "The statement has been marked as Ready to Send.",
                    });

                    table.resetRowSelection();
                    setIsDialogOpen(false);
                    setSelectedStatement(null);
                    setSaving(false);
                })
                .catch(() => {
                    toast({
                        title: "Error",
                        description: "An error occurred while updating the statement status.",
                    });
                    setIsDialogOpen(false);
                    setSelectedStatement(null);
                    setSaving(false);
                });
        }
    };


    const handleDialogCancel = () => {
        setIsDialogOpen(false);
        setSelectedStatement(null);
    };

    const handleEmailSend = async (statement: Statement) => {
        setSelectedEmailStatement(statement);
        let toastConditionMet = false;
        try{
            // Prevent direct email send for profit share invoice having Non-billable expense
            for (const invoiceSummary of statement.invoices){
                const response = await fetch(`/api/invoice/${invoiceSummary.id}/detail`, {
                    method: 'GET',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                });
                if(!response.ok){
                    throw new Error('Network response was not ok');
                }

                const data: Invoice = await response.json();
                setInvoice(data);

                if (data?.lineItems?.some((item) => item.title?.startsWith("Towne Park Profit Share")) 
                    && data?.lineItems.some((item) => item.metaData?.isNonBillableExpense == true) && 
                    !data?.lineItems.some((profitShareItem) => profitShareItem.title?.startsWith("Towne Park Profit Share") && 
                    data.lineItems?.filter((nonBillableItem) => nonBillableItem.metaData?.isNonBillableExpense == true)
                    .every((nonBillableItem) => profitShareItem.description?.includes(nonBillableItem.title)))) {
                    toast({
                        title: "Warning",
                        description: "Statement must be regenerated to send email.",
                    });
                    toastConditionMet = true;
                }
            }
            if(!toastConditionMet){
                setIsEmailDialogOpen(true);
            }
        }
        catch(error) {
            console.error('Fetch error:', error);
            setError('An unexpected error occurred. Please try again later.');
        }
    };

    const handleOpenResendDialog = (statement:any) => { 
        setSelectedRegenerateStatement(statement);
        setIsResendDialogOpen(true)
    };

    const handleCloseDialog = () => {
        setIsDialogOpen(false);
        setIsResendDialogOpen(false);
        setIsEmailDialogOpen(false);

    };

    const handleResendDialogConfirme = async () => {
        if (selectedRegenerateStatement && selectedSendAction) {
            setSaving(true);
            try {
                const response = await fetch(`/api/billingStatement/${selectedRegenerateStatement.id}/email`, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({
                        sendAction: selectedSendAction
                    }),
                });

                if (!response.ok) throw new Error();

                toast({
                    title: "Action Completed",
                    description: `Statement has been processed with action: ${selectedSendAction}`,
                });

                setIsResendDialogOpen(false);
                setSelectedSendAction("");
                setSelectedRegenerateStatement(null);
                setReloadStatements(prev => !prev);
            } catch (error) {
                toast({
                    title: "Error",
                    description: `An error occurred while processing the action: ${selectedSendAction}`,
                });
            } finally {
                setSaving(false);
            }
        }
    };

    const handleEmailDialogConfirm = async () => {
        if (selectedEmailStatement) {
            setSaving(true);
            try {
                const response = await fetch(`/api/billingStatement/${selectedEmailStatement.id}/email`, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({
                        sendAction: SendAction.SEND_ALL
                    }),
                });

                if (!response.ok) throw new Error();

                toast({
                    title: "Email Sent",
                    description: "Statement has been sent successfully.",
                });

                table.resetRowSelection();
                setIsEmailDialogOpen(false);
                setSelectedEmailStatement(null);
                setReloadStatements(prev => !prev);
            } catch (error) {
                toast({
                    title: "Error",
                    description: "An error occurred while sending the email.",
                });
            } finally {
                setSaving(false);
            }
        }
    };

    const handleBulkEmailSend = () => {
        const selectedStatementIds = Object.entries(table.getState().rowSelection)
            .filter(([, selected]) => selected)
            .map(([index]) => statements[parseInt(index)]);

        if (selectedStatementIds.length < 2) {
            toast({
                title: "Error",
                description: "Please select at least 2 statements for bulk email sending.",
            });
            return;
        }

        const notReadyStatements = selectedStatementIds.filter(
            statement => statement.status !== 'Ready To Send'
        );

        if (selectedStatementIds.length - notReadyStatements.length < 2) {
            toast({
                title: "Error",
                description: "Please select at least 2 statements in 'Ready To Send' status.",
            });
            return;
        }

        setIsBulkEmailDialogOpen(true);
    };

    const handleBulkEmailDialogConfirm = async () => {
        const selectedStatements = Object.entries(table.getState().rowSelection)
            .filter(([, selected]) => selected)
            .map(([index]) => statements[parseInt(index)])
            .filter(statement => statement.status === 'Ready To Send');

        const statementIds = selectedStatements.map(statement => statement.id);

        setSaving(true);
        try {
            const response = await fetch('/api/billingStatements/email', {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    statementIds,
                    sendAction: SendAction.SEND_ALL
                }),
            });

            if (!response.ok) throw new Error();

            toast({
                title: "Emails Sent",
                description: "Statements have been sent successfully.",
            });

            table.resetRowSelection();
            setIsBulkEmailDialogOpen(false);
            setReloadStatements(prev => !prev);
        } catch (error) {
            toast({
                title: "Error",
                description: "An error occurred while sending the emails.",
            });
        } finally {
            setSaving(false);
        }
    };

    const handleRegenerate = async (statement: Statement) => {
        setSelectedRegenerateStatement(statement);
        setIsRegenerateDialogOpen(true);
    };

    const handleRegenerateConfirm = async () => {
        if (!selectedRegenerateStatement) return;

        setSaving(true);
        try {
            const response = await fetch(`/api/customers/${selectedRegenerateStatement.customerSiteId}/statement?servicePeriodStart=${selectedRegenerateStatement.servicePeriodStart}`, {
                method: 'POST',
            });

            if (!response.ok) throw new Error();

            toast({
                title: "Success",
                description: "Statement regeneration initiated.",
            });

            setIsRegenerateDialogOpen(false);
            setReloadStatements(prev => !prev);
        } catch (error) {
            toast({
                title: "Error",
                description: "Failed to regenerate statement.",
            });
            setIsRegenerateDialogOpen(false);
        } finally {
            setSaving(false);
        }
    };

    function mapToForecastData(data: any): ForecastData {
        return {
            forecastedRevenue: data?.forecastedRevenue,
            postedRevenue: data?.postedRevenue,
            invoicedRevenue: data?.invoicedRevenue,
            totalActualRevenue: data?.totalActualRevenue,
            forecastDeviationPercentage: data?.forecastDeviationPercentage,
            forecastDeviationAmount: data?.forecastDeviationAmount,
            forecastLastUpdated: data?.forecastLastUpdated,
        };
    }

    const columns: ColumnDef<Statement>[] = [
        ...(!customerSiteId ? [{
            id: "select",
            header: ({ table }: { table: any }) => (
                <Checkbox
                    checked={
                        table.getIsAllPageRowsSelected() ||
                        (table.getIsSomePageRowsSelected() && "indeterminate")
                    }
                    onCheckedChange={(value) => table.toggleAllPageRowsSelected(!!value)}
                    onClick={(e) => e.stopPropagation()}
                    data-qa-id="checkbox-selectAllStatements"
                />
            ),
            cell: ({ row }: { row: any }) => (
                <Checkbox
                    checked={row.getIsSelected()}
                    onCheckedChange={(value) => row.toggleSelected(!!value)}
                    onClick={(e) => e.stopPropagation()}
                    data-qa-id={`checkbox-selectStatement-${row.id}`}
                />
            ),
            enableHiding: false,
        }] : []),
        { accessorKey: 'createdMonth', header: ({ column }) => <DataTableColumnHeader column={column} title="Billing Cycle" /> },
        ...(!customerSiteId ? [{ accessorKey: 'siteNumber', header: ({ column }: { column: any }) => <DataTableColumnHeader column={column} title="Site ID" /> }] : []),
        { accessorKey: 'siteName', 
            header: ({ column }) => <DataTableColumnHeader column={column} title="Customer" />,
            cell: ({ row }) => <div className="truncate w-[200px]">{row.original.siteName}</div>,
        },
        {
            accessorKey: 'amNotes',
            header: ({ column }) => <DataTableColumnHeader column={column} title="AM Note" />,
            cell: ({ row }) => (
                row.original.amNotes ? (
                    <NotepadText
                        className="cursor-pointer"
                        onClick={(event) => {
                            event.stopPropagation();
                            openNoteDialog(row.original.amNotes);
                        }}
                        data-qa-id={`button-viewNote-${row.id}`}
                    />
                ) : null
            ),
        },
        {
            accessorKey: 'servicePeriod',
            header: ({ column }) => <DataTableColumnHeader column={column} title="Service Period" />,
            cell: ({ row }) => <div>{useFormattedServicePeriod(row.original.servicePeriod)}</div>,
        },
        {
            accessorKey: 'status',
            header: ({ column }) => <DataTableColumnHeader column={column} title="Status" />,
            cell: ({ row }) => <StatementStatusBadge status={row.original.status} />,
            filterFn: (row, columnId, filterValues) => {
                if (!filterValues || filterValues.length === 0) return true;
                return filterValues.includes(row.getValue(columnId));
            },
        },
        { accessorKey: 'totalAmount', header: ({ column }) => <DataTableColumnHeader column={column} title="Total" className="justify-end" />, cell: ({ row }) => <div className="text-right">{formatCurrency(row.original.totalAmount)}</div> },
        {
            accessorKey: 'forecastDeviationPercent',
            header: ({ column }) => <DataTableColumnHeader column={column} title="Forecast Deviation %" className="justify-end" />,
            cell: ({ row }) => {
                const forecastData = mapToForecastData(JSON.parse(row.original.forecastData));
                return <div className="text-right">{`${forecastData?.forecastDeviationPercentage?.toFixed(2) || 0}%`}</div>;
            },
            sortingFn: (rowA, rowB) => {
                const aData = mapToForecastData(JSON.parse(rowA.original.forecastData));
                const bData = mapToForecastData(JSON.parse(rowB.original.forecastData));
                return (aData?.forecastDeviationPercentage || 0) - (bData?.forecastDeviationPercentage || 0);
            }
        },
        {
            accessorKey: 'forecastDeviationDollar',
            header: ({ column }) => <DataTableColumnHeader column={column} title="Forecast Deviation $" className="justify-end" />,
            cell: ({ row }) => {
                const forecastData = mapToForecastData(JSON.parse(row.original.forecastData));
                return <div className="text-right">{formatCurrency(forecastData?.forecastDeviationAmount || 0)}</div>;
            },
            sortingFn: (rowA, rowB) => {
                const aData = mapToForecastData(JSON.parse(rowA.original.forecastData));
                const bData = mapToForecastData(JSON.parse(rowB.original.forecastData));
                return (aData?.forecastDeviationAmount || 0) - (bData?.forecastDeviationAmount || 0);
            }
        },
        {
            id: 'actions',
            cell: ({ row }) => {
                const statement = row.original;
                const isExpandable = ['AR Review', 'Approval Team'].includes(statement.status);
                const isPrintable = ['Ready To Send','Sent'].includes(statement.status);
                const isEmailable = statement.status === 'Ready To Send';
                const isRegeneratable = statement.status === 'Sent';
                const isLoading = pdfLoadingStates[statement.id];

                return (
                    <div className="flex items-center space-x-2">
                        {isPrintable && (
                            <>
                                <TooltipProvider>
                                    <Tooltip>
                                        <TooltipTrigger asChild>
                                            <Button
                                                variant="ghost"
                                                onClick={(event) => {
                                                    event.stopPropagation();
                                                    printPDF(statement);
                                                }}
                                                className="btn-download"
                                                disabled={isLoading}
                                                data-qa-id={`button-downloadPDF-${statement.id}`}
                                            >
                                                {isLoading ? <ClipLoader size={15} speedMultiplier={2} color="#7ac8f3" /> : <Download className="h-4 w-4" />}
                                            </Button>
                                        </TooltipTrigger>
                                        <TooltipContent>
                                            <p>Download PDF</p>
                                        </TooltipContent>
                                    </Tooltip>
                                </TooltipProvider>
                                {isBillingManager && isEmailable && (
                                    <TooltipProvider>
                                        <Tooltip>
                                            <TooltipTrigger asChild>
                                                <Button
                                                    variant="ghost"
                                                    onClick={(event) => {
                                                        event.stopPropagation();
                                                        handleEmailSend(statement);
                                                    }}
                                                    className="btn-send"
                                                    data-qa-id={`button-sendEmail-${statement.id}`}
                                                >
                                                    <Mail className="h-4 w-4" />
                                                </Button>
                                            </TooltipTrigger>
                                            <TooltipContent>
                                                <p>Send Email</p>
                                            </TooltipContent>
                                        </Tooltip>
                                    </TooltipProvider>
                                )}
                            </>
                        )}
                        {isExpandable && isBillingManager && (
                            <TooltipProvider>
                                <Tooltip>
                                    <TooltipTrigger asChild>
                                        <Button
                                            variant="ghost"
                                            onClick={(event) => {
                                                event.stopPropagation();
                                                handleApprove(statement);
                                            }}
                                            className="btn-approve"
                                            data-qa-id={`button-approveStatement-${statement.id}`}
                                        >
                                            <UserCheck className="h-4 w-4" />
                                        </Button>
                                    </TooltipTrigger>
                                    <TooltipContent>
                                        <p>Approve Statement</p>
                                    </TooltipContent>
                                </Tooltip>
                            </TooltipProvider>
                        )}
                        {isRegeneratable && isBillingManager && (
                            <>
                                <TooltipProvider>
                                    <Tooltip>
                                        <TooltipTrigger asChild>
                                            <Button
                                                variant="ghost"
                                                onClick={(event) => {
                                                    event.stopPropagation();
                                                    handleRegenerate(statement);
                                                }}
                                                className="btn-regenerate"
                                                data-qa-id={`button-regenerateStatement-${statement.id}`}
                                            >
                                                <RefreshCw className="h-4 w-4" />
                                            </Button>
                                        </TooltipTrigger>
                                        <TooltipContent>

                                            <p> Revise Statement</p>
                                        </TooltipContent>
                                    </Tooltip>
                                </TooltipProvider>
                                <Button
                                    variant="ghost"
                                    onClick={(event) => {
                                        event.stopPropagation();
                                        handleOpenResendDialog(statement);
                                    }}
                                    className="btn-send"
                                    data-qa-id={`button-resendStatement-${statement.id}`}
                                >
                                                <MailCheck className="h-4 w-4"/>
                                </Button>
                            </>
                        )}
                        <div data-qa-id={`indicator-expandRow-${statement.id}`}>
                            {expandedStatements.includes(row.original.id) ? (
                                <ChevronUp />
                            ) : (
                                <ChevronDown />
                            )}
                        </div>
                    </div>
                );
            },
        },
    ];

    const table = useReactTable({
        data: statements,
        columns,
        state: {
            sorting,
            columnFilters,
        },
        onSortingChange: setSorting,
        onColumnFiltersChange: setColumnFilters,
        getCoreRowModel: getCoreRowModel(),
        getPaginationRowModel: getPaginationRowModel(),
        getSortedRowModel: getSortedRowModel(),
        getFilteredRowModel: getFilteredRowModel(),
    });

    useEffect(() => {
        table.setColumnVisibility(hideColumns(HIDDEN_COLUMNS));
    }, [table]);

    return (
        <InvoiceCacheProvider>
            <StatementContext.Provider value={{ reloadStatementsToggle: () => setReloadStatements(prev => !prev) }}>
                <div className="container mx-auto p-4">
                    <div className="flex flex-col py-4 space-y-4">
                        {!customerSiteId && isBillingManager && (
                            <div className="flex space-x-2 justify-end">
                                <Button onClick={handleBulkEmailSend} data-qa-id="button-sendEmails">Send Emails</Button>
                            </div>
                        )}
                        <div className="flex items-center justify-between space-x-4">
                            <div className="flex items-center space-x-4">
                                <Input
                                    placeholder="Search..."
                                    value={table.getState().globalFilter ?? ""}
                                    onChange={(event) => table.setGlobalFilter(event.target.value)}
                                    className="max-w-sm"
                                    data-qa-id="input-searchStatements"
                                />
                                <DataTableFacetedFilter
                                    column={table.getColumn('status')}
                                    options={[
                                        { label: 'Generating', value: StatementStatus.GENERATING },
                                        { label: 'Sent', value: StatementStatus.SENT },
                                        { label: 'AR Review', value: StatementStatus.AR_REVIEW },
                                        { label: 'Approval Team', value: StatementStatus.APPROVAL_TEAM },
                                        { label: 'Ready To Send', value: StatementStatus.READY_TO_SEND },
                                        { label: 'Failed', value: StatementStatus.FAILED },
                                    ]}
                                    title="Status"
                                    data-qa-id="filter-statementStatus"
                                />
                            </div>
                            <div className="flex items-center space-x-4">
                                <DataTableViewOptions table={table} data-qa-id="table-viewOptions" />
                            </div>
                        </div>
                    </div>
                    <div className="rounded-md border mb-4">
                        <Table data-qa-id="table-statements">
                            <TableHeader>
                                {table.getHeaderGroups().map(headerGroup => (
                                    <TableRow key={headerGroup.id}>
                                        {headerGroup.headers.map(header => (
                                            <TableHead key={header.id}>
                                                {header.isPlaceholder ? null : flexRender(header.column.columnDef.header, header.getContext())}
                                            </TableHead>
                                        ))}
                                    </TableRow>
                                ))}
                            </TableHeader>
                            <TableBody>
                                {loading ? (
                                    <TableRow>
                                        <TableCell colSpan={columns.length}>
                                            <Skeleton className="h-lvh w-full" data-test-id="skeleton" />
                                        </TableCell>
                                    </TableRow>
                                ) : (
                                    table.getRowModel().rows.map(row => (
                                        <Fragment key={row.id}>
                                            <TableRow
                                                className={`cursor-pointer ${expandedStatements.includes(row.original.id) ? "bg-muted" : ""}`}
                                                data-state={row.getIsSelected() && "selected"}
                                                onClick={() => toggleStatement(row.original.id, row.original.customerSiteId)}
                                                data-qa-id={`row-statement-${row.original.id}`}
                                            >
                                                {row.getVisibleCells().map(cell => (
                                                    <TableCell key={cell.id}>
                                                        {flexRender(cell.column.columnDef.cell, cell.getContext())}
                                                    </TableCell>
                                                ))}
                                            </TableRow>
                                            {expandedStatements.includes(row.original.id) && (
                                                <>
                                                    <TableRow data-qa-id={`row-statementDetails-${row.original.id}`}>
                                                        <TableCell colSpan={columns.length} className="p-0">
                                                            <BillingStatementDetails invoices={row.original.invoices} statementStatus={row.original.status} userRoles={userRoles} />
                                                        </TableCell>
                                                    </TableRow>
                                                    {row.original.forecastData && row.original.forecastData.length > 0 && (
                                                        <TableRow data-qa-id={`row-supportingDocuments-${row.original.id}`}>
                                                            <TableCell colSpan={columns.length} className="p-0">
                                                                <SupportingDocuments reportStatement={row.original} forecastData={mapToForecastData(JSON.parse(row.original.forecastData))} />
                                                            </TableCell>
                                                        </TableRow>
                                                    )}

                                                </>
                                            )}
                                        </Fragment>
                                    ))
                                )}
                            </TableBody>
                        </Table>
                    </div>
                    <GlobalPagination table={table} data-qa-id="pagination-statements" />
                    <Dialog open={showNote} onOpenChange={setShowNote} data-qa-id="dialog-viewNote">
                        <DialogContent className="w-[90%] max-w-[30vw] h-full max-h-96">
                            <DialogTitle>
                                AM Notes
                            </DialogTitle>
                            <DialogDescription>
                                <Textarea
                                    disabled={true}
                                    value={selectedNote || ''}
                                    onChange={(e) => setSelectedNote(e.target.value)}
                                    className="w-full  h-72 p-2 border rounded overflow-auto resize-none"
                                    placeholder="Enter AM note..."
                                    data-qa-id="textarea-amNote"
                                />
                            </DialogDescription>
                        </DialogContent>
                    </Dialog>
                    <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen} data-qa-id="dialog-approveStatement">
                        <DialogContent>
                            <DialogTitle>
                                <h2 className="text-xl font-semibold">Confirm Approval</h2>
                            </DialogTitle>
                            <DialogDescription>
                                {selectedStatement && (
                                    <p className="mt-2">
                                        Are you sure you want to approve the statement for:<br />
                                        <strong>Site ID:</strong> {selectedStatement.siteNumber}<br />
                                        <strong>Site Name:</strong> {selectedStatement.siteName}
                                    </p>
                                )}
                            </DialogDescription>
                            <DialogFooter className="mt-4">
                                <Button variant="outline" onClick={handleDialogCancel} className="ml-2" data-qa-id="button-cancelApprove">Cancel</Button>
                                <Button onClick={handleDialogConfirm} data-qa-id="button-confirmApprove">
                                    {saving ? <PulseLoader size={8} margin={2} /> : "Yes, Approve"}
                                </Button>
                            </DialogFooter>
                        </DialogContent>
                    </Dialog>
                    <Dialog open={isEmailDialogOpen} onOpenChange={setIsEmailDialogOpen} data-qa-id="dialog-sendEmail">
                        <DialogContent>
                            <DialogTitle>
                                Confirm Email Send
                            </DialogTitle>
                            <DialogDescription>
                                {selectedEmailStatement && (
                                    <p>

                                        <strong>Site ID:</strong> {selectedEmailStatement.siteNumber}<br />
                                        <strong>Site Name:</strong> {selectedEmailStatement.siteName}
                                    </p>
                                )}
                            </DialogDescription>
                            <DialogFooter>
                                <Button variant="outline" onClick={handleCloseDialog}
                                    disabled={saving}
                                    data-qa-id="button-cancelSendEmail">
                                    Cancel
                                </Button>
                                <Button onClick={handleEmailDialogConfirm} disabled={saving} data-qa-id="button-confirmSendEmail">
                                    {saving ? <PulseLoader size={8} margin={2} /> : "Send Email"}
                                </Button>
                            </DialogFooter>
                        </DialogContent>
                    </Dialog>

                    <Dialog open={isResendDialogOpen} onOpenChange={setIsResendDialogOpen} data-qa-id="dialog-resendStatement">
                        <DialogContent>
                            <DialogTitle>
                                Resend Statement to Customer or Great Plains?
                            </DialogTitle>
                            <RadioGroup
                                value={selectedSendAction}
                                onValueChange={(value) => setSelectedSendAction(value as SendAction)}
                                className="space-y-4"
                                data-qa-id="radioGroup-resendOptions"
                            >
                                <div className="flex items-center space-x-2">
                                    <RadioGroupItem id="SendEmail" value={SendAction.SEND_EMAIL} data-qa-id="radio-sendToCustomer" />
                                    <Label htmlFor="SendEmail" className="font-bold">Resend to Customer</Label>
                                </div>
                                <div className="flex items-center space-x-2">
                                    <RadioGroupItem id="SendToGP" value={SendAction.SEND_TO_GP} data-qa-id="radio-sendToGP" />
                                    <Label htmlFor="SendToGP" className="font-bold">Resend to Great Plains</Label>
                                </div>
                            </RadioGroup>
                            <DialogFooter>
                                {!saving &&
                                    <Button variant="outline"
                                        onClick={() => {
                                            setIsResendDialogOpen(false);
                                            setSelectedSendAction("");
                                        }}
                                        disabled={saving}
                                        data-qa-id="button-cancelResend">
                                        Cancel
                                    </Button>
                                }
                                <Button
                                    disabled={!selectedSendAction || saving}
                                    onClick={handleResendDialogConfirme}
                                    data-qa-id="button-confirmResend"
                                >
                                    {saving ? <PulseLoader size={8} margin={2} /> : "Resend"}
                                </Button>
                            </DialogFooter>
                        </DialogContent>
                    </Dialog>

                    <Dialog open={isBulkEmailDialogOpen} onOpenChange={setIsBulkEmailDialogOpen} data-qa-id="dialog-bulkEmail">
                        <DialogContent>
                            <DialogTitle>
                                Send Multiple Statement Emails
                            </DialogTitle>
                            <DialogDescription>
                                {(() => {
                                    const selectedStatements = Object.entries(table.getState().rowSelection)
                                        .filter(([, selected]) => selected)
                                        .map(([index]) => statements[parseInt(index)]);
                                    const notReadyStatements = selectedStatements.filter(
                                        statement => statement.status !== 'Ready To Send'
                                    );
                                    return (
                                        <>
                                            <p>
                                                You are about to send emails for{' '}
                                                {selectedStatements.length - notReadyStatements.length} statements.
                                            </p>
                                            {notReadyStatements.length > 0 && (
                                                <p className="text-xs mt-5">
                                                    {notReadyStatements.length} statement/s without 'Ready To Send' status and will
                                                    not have emails sent.
                                                </p>
                                            )}
                                        </>
                                    );
                                })()}
                            </DialogDescription>
                            <DialogFooter>
                                <Button variant="outline" onClick={() => setIsBulkEmailDialogOpen(false)} disabled={saving} data-qa-id="button-cancelBulkEmail">
                                    Cancel
                                </Button>
                                <Button onClick={handleBulkEmailDialogConfirm} disabled={saving} data-qa-id="button-confirmBulkEmail">
                                    {saving ? <PulseLoader size={8} margin={2} /> : "Yes, proceed"}
                                </Button>
                            </DialogFooter>
                        </DialogContent>
                    </Dialog>
                    <Dialog open={isRegenerateDialogOpen} onOpenChange={setIsRegenerateDialogOpen} data-qa-id="dialog-regenerateStatement">
                        <DialogContent>
                            <DialogTitle>
                                Confirm Regeneration
                            </DialogTitle>
                            <DialogDescription>
                                {selectedRegenerateStatement && (
                                    <p>
                                        Are you sure you want to regenerate the statement for:<br />
                                        <strong>Site ID:</strong> {selectedRegenerateStatement.siteNumber}<br />
                                        <strong>Billing Cycle:</strong> {selectedRegenerateStatement.createdMonth}
                                    </p>
                                )}
                            </DialogDescription>
                            <DialogFooter>
                                <Button
                                    variant="outline"
                                    onClick={() => setIsRegenerateDialogOpen(false)}
                                    disabled={saving}
                                    data-qa-id="button-cancelRegenerate"
                                >
                                    Cancel
                                </Button>
                                <Button
                                    onClick={handleRegenerateConfirm}
                                    disabled={saving}
                                    data-qa-id="button-confirmRegenerate"
                                >
                                    {saving ? <PulseLoader size={8} margin={2} /> : "Yes, Regenerate"}
                                </Button>
                            </DialogFooter>
                        </DialogContent>
                    </Dialog>
                </div>
            </StatementContext.Provider>
        </InvoiceCacheProvider>
    );
};

export default StatementsGlobalList;

