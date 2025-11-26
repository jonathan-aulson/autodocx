import Pagination from "@/components/Pagination";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Dialog, DialogContent, DialogOverlay } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { useToast } from "@/components/ui/use-toast";
import { useAuth } from "@/contexts/AuthContext";
import { UpdateDeviation } from "@/lib/models/Contract";
import { CheckIcon, PencilIcon, SearchIcon, XIcon } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { NumericFormat } from "react-number-format";
import { PulseLoader } from "react-spinners";

export interface CustomerSiteDeviation {
    customerSiteId: string;
    contractId: string;
    siteNumber: string;
    siteName: string;
    deviationPercentage?: number | null;
    deviationAmount?: number | null;
    isSelected?: boolean;
}

export default function RevenueReviewThreshold() {
    const [deviations, setDeviations] = useState<CustomerSiteDeviation[]>([]);
    const [deviationsMap, setDeviationsMap] = useState<Record<string, CustomerSiteDeviation>>({});
    const [bulkEditValue, setBulkEditValue] = useState<number | null>(null);
    const [bulkEditAmountValue, setBulkEditAmountValue] = useState<number | null>(null);
    const [showBulkEditModal, setShowBulkEditModal] = useState(false);
    const [showConfirmDialog, setShowConfirmDialog] = useState(false);
    const [selectedDeviationCustomerIds, setSelectedDeviationCustomerIds] = useState<string[]>([]);
    const [editingDeviationCustomerId, setEditingDeviationCustomerId] = useState<string | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [selectAllFiltered, setSelectAllFiltered] = useState(false);
    const { toast } = useToast();
    const [currentPage, setCurrentPage] = useState(1);
    const [customersPerPage, setCustomersPerPage] = useState(5);
    const [sortColumn, setSortColumn] = useState<string | null>("name");
    const [sortDirection, setSortDirection] = useState<string | null>("asc");
    const [searchDeviationPercentage, setSearchDeviationPercentage] = useState("");
    const [searchDeviationAmount, setSearchDeviationDollar] = useState("");
    const [searchCustomerName, setSearchCustomerName] = useState("");
    const [showCustomerNameSearch, setShowCustomerNameSearch] = useState(false);
    const [showDeviationPercentageSearch, setShowDeviationPercentageSearch] = useState(false);
    const [showDeviationDollarSearch, setShowDeviationDollarSearch] = useState(false);
    const [isEditing, setIsEditing] = useState(false);
    const [isUpdating, setIsUpdating] = useState(false);
    const [originalDeviations, setOriginalDeviations] = useState<Record<string, CustomerSiteDeviation>>({});

    const customerNameInputRef = useRef<HTMLInputElement>(null);
    const deviationPercentageInputRef = useRef<HTMLInputElement>(null);
    const deviationDollarInputRef = useRef<HTMLInputElement>(null);

    const { userRoles } = useAuth();

    const isBillingAdmin = userRoles.includes('billingAdmin');

    useEffect(() => {
        setIsLoading(true);
        const fetchData = async () => {
            try {
                const deviationResponse = await fetch("/api/deviations");
                if (!deviationResponse.ok) throw new Error("Failed to fetch deviations");
                const deviationsData: CustomerSiteDeviation[] = await deviationResponse.json();
                const deviationsMap = deviationsData.reduce((acc: Record<string, CustomerSiteDeviation>, deviation) => {
                    acc[deviation.customerSiteId] = deviation;
                    return acc;
                }, {});
                setDeviationsMap(deviationsMap);
                setDeviations(deviationsData);
                setIsLoading(false);
            } catch (error) {
                console.error("Error fetching data:", error);
                setIsLoading(false);
                toast({
                    title: "Error",
                    description: 'An unexpected error occurred. Please try again later.',
                });
            }
        };
        fetchData();
    }, [toast]);

    const handleDeviationChange = (key: string, customerId: string, newValue: number) => {
        if (key === "deviationPercentage" && newValue > 100) {
            return;
        }
        const newDeviation = { ...deviationsMap[customerId], [key]: newValue };
        setDeviations(prevCustomers =>
            prevCustomers.map(customer => customer.customerSiteId === customerId ? newDeviation : customer)
        );
        deviationsMap[customerId] = newDeviation;
        setDeviationsMap(deviationsMap);
    };

    const handleBulkEdit = () => {
        setShowBulkEditModal(false);
        setShowConfirmDialog(true);
    };

    const bulkUpdateDeviationThreshold = async (updates: UpdateDeviation[]) => {
        try {
            const response = await fetch('/api/deviations', {
                method: 'PATCH',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(updates),
            });
            if (!response.ok) throw new Error('Failed to update deviation thresholds');
            toast({
                title: "Success",
                description: "Deviation thresholds updated successfully.",
            });
        } catch (error) {
            console.error(error);
            toast({
                title: 'Error',
                description: 'An error occurred while updating deviation thresholds. Please try again.',
            });
        }
    };

    const updateIndividualDeviationThreshold = async (update: UpdateDeviation) => {
        try {
            const response = await fetch(`/api/deviations`, {
                method: 'PATCH',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify([update]),
            });

            if (!response.ok) throw new Error('Failed to update deviation threshold');
            toast({
                title: "Success",
                description: "Deviation threshold updated successfully.",
            });
        } catch (error) {
            console.error(error);
            toast({
                title: 'Error',
                description: 'An error occurred while updating the deviation threshold. Please try again.',
            });
        }
    };

    const confirmBulkEdit = async () => {
        setIsUpdating(true);

        try {
            const updates: UpdateDeviation[] = selectedDeviationCustomerIds.map(customerSiteId => {
                const deviation = deviationsMap[customerSiteId];
                return {
                    contractId: deviation?.contractId,
                    deviationPercentage: bulkEditValue,
                    deviationAmount: bulkEditAmountValue,
                };
            });
            await bulkUpdateDeviationThreshold(updates);
            setDeviations(prevCustomers =>
                prevCustomers.map(customer =>
                    selectedDeviationCustomerIds.includes(customer.customerSiteId)
                        ? {
                            ...customer,
                            deviationPercentage: bulkEditValue,
                            deviationAmount: bulkEditAmountValue,
                            isSelected: false
                        }
                        : customer
                )
            );
            setBulkEditValue(null);
            setBulkEditAmountValue(null);
            setSelectedDeviationCustomerIds([]);
            setSelectAllFiltered(false);
            setCurrentPage(1);
            setSearchDeviationPercentage("");
            setSearchDeviationDollar("");
            setSearchCustomerName("");
            setShowConfirmDialog(false);
        } catch (error) {
            console.error(error);
            toast({
                title: 'Error',
                description: 'An error occurred while updating deviation thresholds. Please try again.',
            });
        } finally {
            setIsUpdating(false);
        }
    };

    const handleSaveEdit = async () => {
        if (!editingDeviationCustomerId) return;
        const deviation = deviationsMap[editingDeviationCustomerId];
        if (!deviation) return;
        const update: UpdateDeviation = {
            contractId: deviation.contractId,
            deviationAmount: deviation.deviationAmount,
            deviationPercentage: deviation.deviationPercentage,
        };

        setIsUpdating(true);
        await updateIndividualDeviationThreshold(update);
        setIsUpdating(false);
        setEditingDeviationCustomerId(null);
        setIsEditing(false);
    };

    const cancelBulkEdit = () => {
        setShowConfirmDialog(false);
    };

    const handleStartEdit = (customerId: string) => {
        const originalDeviationValues = { ...deviationsMap[customerId] };
        setOriginalDeviations(prevOriginals => ({
            ...prevOriginals,
            [customerId]: originalDeviationValues
        }));
        setEditingDeviationCustomerId(customerId);
        setIsEditing(true);
    };

    const handleCancelEdit = () => {
        if (editingDeviationCustomerId) {
            const originalValues = originalDeviations[editingDeviationCustomerId];
            if (originalValues) {
                setDeviations(prevCustomers =>
                    prevCustomers.map(customer =>
                        customer.customerSiteId === editingDeviationCustomerId
                            ? originalValues
                            : customer
                    )
                );
                delete deviationsMap[editingDeviationCustomerId];
                deviationsMap[editingDeviationCustomerId] = originalValues;
                setDeviationsMap(deviationsMap);
            }
        }
        setEditingDeviationCustomerId(null);
        setIsEditing(false);
    };

    const handleSelectCustomer = (customerId: string) => {
        setDeviations(prevCustomers =>
            prevCustomers.map(customer =>
                customer.customerSiteId === customerId ? { ...customer, isSelected: !customer.isSelected } : customer
            )
        );

        setSelectedDeviationCustomerIds(prevSelected => {
            const isSelected = prevSelected.includes(customerId);
            return isSelected
                ? prevSelected.filter(id => id !== customerId)
                : [...prevSelected, customerId];
        });
    };

    const handleSelectAllCurrentPage = () => {
        const allSelected = currentCustomers.every(customer =>
            selectedDeviationCustomerIds.includes(customer.customerSiteId)
        );

        const newSelectedCustomerIds = allSelected
            ? selectedDeviationCustomerIds.filter(id => !currentCustomers.some(customer => customer.customerSiteId === id))
            : [...new Set([...selectedDeviationCustomerIds, ...currentCustomers.map(customer => customer.customerSiteId)])];

        setSelectedDeviationCustomerIds(newSelectedCustomerIds);
        setDeviations(prevCustomers =>
            prevCustomers.map(customer => ({
                ...customer,
                isSelected: newSelectedCustomerIds.includes(customer.customerSiteId),
            }))
        );

        if (allSelected) {
            setSelectAllFiltered(false);
        }
    };

    const handleToggleSelectAllFiltered = () => {
        if (selectAllFiltered) {
            const newSelectedCustomerIds = selectedDeviationCustomerIds.filter(
                id => !filteredCustomers.some(customer => customer.customerSiteId === id)
            );
            setSelectedDeviationCustomerIds(newSelectedCustomerIds);
            setDeviations(prevCustomers =>
                prevCustomers.map(customer => ({
                    ...customer,
                    isSelected: newSelectedCustomerIds.includes(customer.customerSiteId),
                }))
            );
        } else {
            const newSelectedCustomerIds = [
                ...new Set([...selectedDeviationCustomerIds, ...filteredCustomers.map(customer => customer.customerSiteId)]),
            ];
            setSelectedDeviationCustomerIds(newSelectedCustomerIds);
            setDeviations(prevCustomers =>
                prevCustomers.map(customer => ({
                    ...customer,
                    isSelected: newSelectedCustomerIds.includes(customer.customerSiteId),
                }))
            );
        }
        setSelectAllFiltered(!selectAllFiltered);
    };

    const handlePageChange = (pageNumber: number) => {
        setCurrentPage(pageNumber);
    };

    const handleItemsPerPageChange = (value: number) => {
        setCustomersPerPage(value);
        setCurrentPage(1);
    };

    const handleSort = (column: string) => {
        if (sortColumn === column) {
            setSortDirection(sortDirection === "asc" ? "desc" : "asc");
        } else {
            setSortColumn(column);
            setSortDirection("asc");
        }
    };

    const handleDeviationPercentageSearch = (value: string) => {
        setSearchDeviationPercentage(value);
    };

    const handleDeviationDollarSearch = (value: string) => {
        setSearchDeviationDollar(value);
    };

    const handleCustomerNameSearch = (value: string) => {
        setSearchCustomerName(value);
    };

    const toggleCustomerNameSearch = (e: React.MouseEvent<HTMLButtonElement>) => {
        e.stopPropagation();
        setShowCustomerNameSearch(!showCustomerNameSearch);
        if (!showCustomerNameSearch) {
            setTimeout(() => customerNameInputRef.current?.focus(), 0);
        }
    };

    const toggleDeviationPercentageSearch = (e: React.MouseEvent<HTMLButtonElement>) => {
        e.stopPropagation();
        setShowDeviationPercentageSearch(!showDeviationPercentageSearch);
        if (!showDeviationPercentageSearch) {
            setTimeout(() => deviationPercentageInputRef.current?.focus(), 0);
        }
    };

    const toggleDeviationDollarSearch = (e: React.MouseEvent<HTMLButtonElement>) => {
        e.stopPropagation();
        setShowDeviationDollarSearch(!showDeviationDollarSearch);
        if (!showDeviationDollarSearch) {
            setTimeout(() => deviationDollarInputRef.current?.focus(), 0);
        }
    };

    const filteredCustomers = deviations.filter(customer => {
        const matchesCustomerName = searchCustomerName === "" || customer.siteName.toLowerCase().includes(searchCustomerName.toLowerCase());
        const matchesDeviationPercentage = searchDeviationPercentage === "" || customer.deviationPercentage?.toString() === searchDeviationPercentage || (isEditing && customer.customerSiteId === editingDeviationCustomerId);
        const matchesDeviationDollar = searchDeviationAmount === "" || customer.deviationAmount?.toString() === searchDeviationAmount || (isEditing && customer.customerSiteId === editingDeviationCustomerId);
        return matchesCustomerName && matchesDeviationPercentage && matchesDeviationDollar;
    });

    const sortedCustomers = filteredCustomers.sort((a, b) => {
        if (sortColumn === "name") {
            return sortDirection === "asc" ? a.siteName.localeCompare(b.siteName) : b.siteName.localeCompare(a.siteName);
        } else if (sortColumn === "siteNumber") {
            return sortDirection === "asc"
                ? a.siteNumber.localeCompare(b.siteNumber)
                : b.siteNumber.localeCompare(a.siteNumber);
        } else if (sortColumn === "deviationPercentage") {
            return sortDirection === "asc"
                ? (a.deviationPercentage || 0) - (b.deviationPercentage || 0)
                : (b.deviationPercentage || 0) - (a.deviationPercentage || 0);
        } else if (sortColumn === "deviationDollar") {
            return sortDirection === "asc"
                ? (a.deviationAmount || 0) - (b.deviationAmount || 0)
                : (b.deviationAmount || 0) - (a.deviationAmount || 0);
        } else {
            return 0;
        }
    });

    const indexOfLastCustomer = currentPage * customersPerPage;
    const indexOfFirstCustomer = indexOfLastCustomer - customersPerPage;
    const currentCustomers = sortedCustomers.slice(indexOfFirstCustomer, indexOfLastCustomer);
    const totalItems = filteredCustomers.length;
    const selectedCustomers = deviations.filter(customer => customer.isSelected);

    if (isLoading) {
        return (
            <div className="flex items-center justify-center h-full">
                <div>Loading...</div>
            </div>
        );
    }

    return (
        <div>
            <h1 className="text-2xl font-bold py-8">Revenue Review Threshold</h1>
            <header className="my-2">
                {isBillingAdmin && (
                    <div className="flex justify-between items-center gap-4">
                        <div className="flex items-center gap-2">
                            <Label htmlFor="itemsPerPage">Items per page:</Label>
                            <Select
                                data-qa-id="select-itemsPerPage-revenueManagement"
                                onValueChange={(value) => {
                                    handleItemsPerPageChange(Number(value));
                                }}
                                value={`${customersPerPage}`}
                            >
                                <SelectTrigger className="h-8 w-[70px]">
                                    <SelectValue placeholder={customersPerPage} />
                                </SelectTrigger>
                                <SelectContent side="top">
                                    {[5, 10, 20, 50, 100].map((pageSize) => (
                                        <SelectItem key={pageSize} value={`${pageSize}`}>
                                            {pageSize}
                                        </SelectItem>
                                    ))}
                                </SelectContent>
                            </Select>
                        </div>
                        <div className="flex items-center space-x-2">
                            <Button
                                data-qa-id="button-selectAll-revenueManagement"
                                variant="outline"
                                onClick={handleToggleSelectAllFiltered}
                                title={selectAllFiltered ? "Click to unselect the items of all the current pages." : "Click to select items of all the current pages"}
                            >
                                {selectAllFiltered ? "Unselect All" : "Select All"}
                            </Button>
                            <div title={selectedCustomers.length <= 1 ? "Select more than 1 customer to activate" : ""}>
                                <Button
                                    data-qa-id="button-bulkEdit-revenueManagement"
                                    onClick={() => setShowBulkEditModal(true)}
                                    disabled={selectedCustomers.length <= 1}
                                >
                                    Bulk Edit
                                </Button>
                            </div>
                        </div>
                    </div>
                )}
            </header>
            <div className="flex-1 overflow-auto">
                <Table>
                    <TableHeader>
                        <TableRow>
                            <TableHead className="w-0">
                                <Checkbox
                                    data-qa-id="checkbox-selectAll-revenueManagement"
                                    checked={currentCustomers.every(customer => selectedDeviationCustomerIds.includes(customer.customerSiteId))}
                                    onCheckedChange={handleSelectAllCurrentPage}
                                />
                            </TableHead>
                            <TableHead className="w-[45%]" onClick={() => handleSort("name")}>
                                <div className="flex items-center gap-2">
                                    Customer Name
                                    {sortColumn === "name" && <span className="ml-1">{sortDirection === "asc" ? "\u2191" : "\u2193"}</span>}
                                    <Button
                                        data-qa-id="button-searchToggleCustomerName-revenueManagement"
                                        variant="ghost"
                                        size="icon"
                                        onClick={toggleCustomerNameSearch}
                                        className={`${showCustomerNameSearch ? "bg-muted" : ""}`}
                                    >
                                        <SearchIcon className="w-4 h-4" />
                                    </Button>
                                    {showCustomerNameSearch && (
                                        <Input
                                            data-qa-id="input-searchCustomerName-revenueManagement"
                                            ref={customerNameInputRef}
                                            type="text"
                                            value={searchCustomerName}
                                            onChange={(e) => handleCustomerNameSearch(e.target.value)}
                                            onClick={(e) => e.stopPropagation()}
                                            placeholder="Search"
                                            className="ml-2 w-28"
                                        />
                                    )}
                                </div>
                            </TableHead>
                            <TableHead className="w-[10%]" onClick={() => handleSort("siteNumber")} data-qa-id="tableHead-siteNumber-revenueManagement">
                                <div className="flex items-center justify-end">
                                    Site ID
                                    {sortColumn === "siteNumber" && (
                                        <span className="ml-1">{sortDirection === "asc" ? "\u2191" : "\u2193"}</span>
                                    )}
                                </div>
                            </TableHead>
                            <TableHead className="w-[17.5%]" onClick={() => handleSort("deviationPercentage")} data-qa-id="tableHead-deviationPercentage-revenueManagement">
                                <div className="flex items-center justify-end gap-2 ">
                                    Deviation %
                                    {sortColumn === "deviationPercentage" && (
                                        <span className="ml-1">{sortDirection === "asc" ? "\u2191" : "\u2193"}</span>
                                    )}
                                    <Button
                                        data-qa-id="button-searchToggleDeviationPercentage-revenueManagement"
                                        variant="ghost"
                                        size="icon"
                                        onClick={(e) => toggleDeviationPercentageSearch(e)}
                                        className={`${showDeviationPercentageSearch ? "bg-muted" : ""}`}
                                    >
                                        <SearchIcon className="w-4 h-4" />
                                    </Button>
                                    {showDeviationPercentageSearch && (
                                        <Input
                                            data-qa-id="input-searchDeviationPercentage-revenueManagement"
                                            ref={deviationPercentageInputRef}
                                            type="text"
                                            value={searchDeviationPercentage}
                                            onChange={(e) => handleDeviationPercentageSearch(e.target.value)}
                                            onClick={(e) => e.stopPropagation()}
                                            placeholder="Search"
                                            className="ml-2 w-28"
                                        />
                                    )}
                                </div>
                            </TableHead>
                            <TableHead className="w-[17.5%]" onClick={() => handleSort("deviationDollar")} data-qa-id="tableHead-deviationDollar-revenueManagement">
                                <div className="flex items-center justify-end gap-2">
                                    Deviation $
                                    {sortColumn === "deviationDollar" && (
                                        <span className="ml-1">{sortDirection === "asc" ? "\u2191" : "\u2193"}</span>
                                    )}
                                    <Button
                                        data-qa-id="button-searchToggleDeviationDollar-revenueManagement"
                                        variant="ghost"
                                        size="icon"
                                        onClick={(e) => toggleDeviationDollarSearch(e)}
                                        className={`${showDeviationDollarSearch ? "bg-muted" : ""}`}
                                    >
                                        <SearchIcon className="w-4 h-4" />
                                    </Button>
                                    {showDeviationDollarSearch && (
                                        <Input
                                            data-qa-id="input-searchDeviationDollar-revenueManagement"
                                            ref={deviationDollarInputRef}
                                            type="text"
                                            value={searchDeviationAmount}
                                            onChange={(e) => handleDeviationDollarSearch(e.target.value)}
                                            onClick={(e) => e.stopPropagation()}
                                            placeholder="Search"
                                            className="ml-2 w-28"
                                        />
                                    )}
                                </div>
                            </TableHead>
                            {isBillingAdmin && <TableHead className="w-[10%]" data-qa-id="tableHead-actions-revenueManagement">Actions</TableHead>}
                        </TableRow>
                    </TableHeader>
                    <TableBody>
                        {currentCustomers.map((customer, index) => (
                            <TableRow key={customer.customerSiteId} data-qa-id={`row-customer-${index}-revenueManagement`}>
                                <TableCell>
                                    <Checkbox
                                        data-qa-id={`checkbox-selectCustomer-${customer.customerSiteId}-revenueManagement`}
                                        checked={customer.isSelected || false}
                                        onCheckedChange={() => handleSelectCustomer(customer.customerSiteId)}
                                    />
                                </TableCell>
                                <TableCell className="w-[45%]">{customer.siteName}</TableCell>
                                <TableCell className="w-[10%] text-right">{customer.siteNumber}</TableCell>
                                <TableCell className="w-[17.5%] text-right">
                                    {editingDeviationCustomerId === customer.customerSiteId ? (
                                        <NumericFormat
                                            data-qa-id={`input-deviationPercentage-${customer.customerSiteId}-revenueManagement`}
                                            displayType="input"
                                            decimalScale={2}
                                            suffix="%"
                                            inputMode="numeric"
                                            allowNegative={false}
                                            isAllowed={(values) => values.floatValue ? values.floatValue <= 100 : true}
                                            placeholder="Enter deviation percentage"
                                            value={customer.deviationPercentage !== null ? customer.deviationPercentage?.toString() : ''}
                                            onValueChange={(values) => handleDeviationChange("deviationPercentage", customer.customerSiteId, values.floatValue || 0)}
                                            customInput={Input}
                                        />
                                    ) : (
                                        customer.deviationPercentage !== null ? `${customer.deviationPercentage?.toString()}%` : ""
                                    )}
                                </TableCell>
                                <TableCell className="w-[17.5%] text-right">
                                    {editingDeviationCustomerId === customer.customerSiteId ? (
                                        <NumericFormat
                                            data-qa-id={`input-deviationAmount-${customer.customerSiteId}-revenueManagement`}
                                            displayType="input"
                                            decimalScale={2}
                                            prefix="$"
                                            inputMode="numeric"
                                            allowNegative={false}
                                            placeholder="Enter deviation amount"
                                            value={customer.deviationAmount !== null ? customer.deviationAmount?.toString() : ''}
                                            onValueChange={(values) => handleDeviationChange("deviationAmount", customer.customerSiteId, values.floatValue || 0)}
                                            customInput={Input}
                                        />
                                    ) : (
                                        customer.deviationAmount !== null ? `$${customer.deviationAmount?.toString()}` : ""
                                    )}
                                </TableCell>
                                {isBillingAdmin && (
                                    <TableCell className="w-[10%]">
                                        {editingDeviationCustomerId === customer.customerSiteId ? (
                                            <div className="flex gap-2">
                                                <Button data-qa-id={`button-cancelEdit-${customer.customerSiteId}-revenueManagement`} size="icon" variant="outline" onClick={handleCancelEdit}>
                                                    <XIcon className="h-4 w-4" />
                                                </Button>
                                                <Button data-qa-id={`button-saveEdit-${customer.customerSiteId}-revenueManagement`} size="icon" onClick={handleSaveEdit}>
                                                    <CheckIcon className="h-4 w-4" />
                                                </Button>
                                            </div>
                                        ) : (
                                            <Button data-qa-id={`button-startEdit-${customer.customerSiteId}-revenueManagement`} size="icon" variant="outline" onClick={() => handleStartEdit(customer.customerSiteId)}>
                                                <PencilIcon className="h-4 w-4" />
                                            </Button>
                                        )}
                                    </TableCell>
                                )}
                            </TableRow>
                        ))}
                    </TableBody>
                </Table>
            </div>
            <div className="bg-background border-t px-6 py-4 flex justify-center">
                <Pagination
                    data-qa-id="pagination-controls-revenueManagement"
                    currentPage={currentPage}
                    totalItems={totalItems}
                    itemsPerPage={customersPerPage}
                    onPageChange={handlePageChange}
                />
            </div>
            {showConfirmDialog && (
                <Dialog open={showConfirmDialog} onOpenChange={setShowConfirmDialog}>
                    <DialogOverlay />
                    <DialogContent className="w-full" data-qa-id="dialog-confirmBulkEdit-revenueManagement">
                        {!isUpdating && (
                            <div className="flex flex-col gap-10">
                                <div>
                                    <p className="text-xl font-bold">Confirm Bulk Edit</p>
                                </div>
                                <div>
                                    <p>You are about to make bulk edits to the selected customers. This action cannot be undone. Are you sure you want to proceed?</p>
                                </div>
                                <div className="flex justify-between items-center">
                                    <h2 className="text-red-700">
                                        {selectedCustomers.length} customers selected for bulk edit
                                    </h2>
                                    <div className="flex justify-end gap-2">
                                        <Button data-qa-id="button-cancelConfirm-revenueManagement" variant="outline" onClick={cancelBulkEdit}>Cancel</Button>
                                        <Button data-qa-id="button-confirmBulkEdit-revenueManagement" onClick={confirmBulkEdit}>Save</Button>
                                    </div>
                                </div>
                            </div>
                        )}
                        {isUpdating && (
                            <div>
                                <h2 className="text-xl">Updating {selectedCustomers.length} customers</h2>
                                <div className="flex justify-center items-center gap-2">
                                    <PulseLoader color="#3b82f6" loading={true} size={10} />
                                </div>
                            </div>
                        )}
                    </DialogContent>
                </Dialog>
            )}
            {showBulkEditModal && (
                <Dialog open={showBulkEditModal} onOpenChange={setShowBulkEditModal}>
                    <DialogOverlay />
                    <DialogContent className="w-full" data-qa-id="dialog-bulkEdit-revenueManagement">
                        <div className="flex flex-col gap-10">
                            <div>
                                <p className="text-xl font-bold">Bulk Edit</p>
                            </div>
                            <div>
                                <p>
                                    You are about to make bulk edits to the selected customers. This action cannot be undone. Are you sure you want to proceed?
                                </p>
                            </div>
                            <div className="grid gap-2">
                                <Label htmlFor="bulkEditValue">Deviation %</Label>
                                <NumericFormat
                                    data-qa-id="input-bulkEditPercentage-revenueManagement"
                                    id="bulkEditValue"
                                    displayType="input"
                                    decimalScale={2}
                                    suffix="%"
                                    inputMode="numeric"
                                    allowNegative={false}
                                    isAllowed={(values) => values.floatValue ? values.floatValue <= 100 : true}
                                    placeholder="Bulk edit percentage"
                                    value={bulkEditValue !== null ? bulkEditValue.toString() : ""}
                                    onValueChange={(value) => setBulkEditValue(Number(value.floatValue))}
                                    customInput={Input}
                                />
                            </div>
                            <div className="grid gap-2">
                                <Label htmlFor="bulkEditDollarValue">Deviation $</Label>
                                <NumericFormat
                                    data-qa-id="input-bulkEditAmount-revenueManagement"
                                    id="bulkEditDollarValue"
                                    displayType="input"
                                    decimalScale={2}
                                    prefix="$"
                                    inputMode="numeric"
                                    allowNegative={false}
                                    placeholder="Bulk edit amount"
                                    value={bulkEditAmountValue !== null ? bulkEditAmountValue.toString() : ""}
                                    onValueChange={(value) => setBulkEditAmountValue(value.floatValue || null)}
                                    customInput={Input}
                                />
                            </div>
                            <div className="flex justify-between items-center">
                                <h2 className="text-red-700">
                                    {selectedCustomers.length} customers selected for bulk edit
                                </h2>
                                <div className="flex justify-end gap-2">
                                    <Button data-qa-id="button-cancelBulkEdit-revenueManagement" variant="outline" onClick={() => setShowBulkEditModal(false)}>Cancel</Button>
                                    <Button data-qa-id="button-saveBulkEdit-revenueManagement" onClick={handleBulkEdit}>Save</Button>
                                </div>
                            </div>
                        </div>
                    </DialogContent>
                </Dialog>
            )}
        </div>
    );
}