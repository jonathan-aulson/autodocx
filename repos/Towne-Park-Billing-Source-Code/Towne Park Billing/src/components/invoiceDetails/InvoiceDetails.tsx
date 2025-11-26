import { useStatementContext } from "@/components/BillingStatement/StatementContext";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";
import { Dialog, DialogContent, DialogFooter, DialogTitle } from '@/components/ui/dialog';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { useToast } from "@/components/ui/use-toast";
import { Invoice, UpdateLineItems } from "@/lib/models/Invoice";
import { formatCurrency } from "@/lib/utils";
import { DialogDescription } from "@radix-ui/react-dialog";
import { Plus } from "lucide-react";
import React, { useEffect, useState } from "react";
import PulseLoader from "react-spinners/PulseLoader";
import { useCustomerDetails } from "../../pages/customersDetails/CustomersDetailContext";
import Modal from "../Modal";
import AdHocLineItemForm from "./AdHocLineItemForm";
import { useInvoiceCache } from "./InvoiceCacheContext";
import InvoiceDetailsSkeleton from "./InvoiceDetailsSkeleton";
import InvoiceFooter from "./InvoiceFooter";
import InvoiceHeader from "./InvoiceHeader";

interface InvoiceDetailProps {
    invoiceId: string;
    editMode: boolean;
    onCloseModal: () => void;
}

const formatDescription = (description: string): React.ReactNode => {
    if (!description) {
        return null;
    }
    return description.split(/\n|\/n/).map((line, index) => (
        <div key={index}>
            <p className="text-sm">{line}</p>
        </div>
    ));
};

const InvoiceDetails: React.FC<InvoiceDetailProps> = ({ invoiceId, editMode, onCloseModal }) => {
    const [loading, setLoading] = useState<boolean>(true);
    const [saving, setSaving] = useState<boolean>(false);
    const [error, setError] = useState<string | null>(null);
    const { invoiceCache, addInvoiceToCache, invoiceConfig } = useInvoiceCache();
    const [invoice, setInvoice] = useState<Invoice | null>(invoiceCache[invoiceId] || null);
    const [newLineItems, setNewLineItems] = useState<UpdateLineItems[]>([]);
    const [confirmSaveModal, setConfirmSaveModal] = useState(false);
    const [confirmCanceleModal, setConfirmCanceleModal] = useState(false);
    const [showLineItemForm, setShowLineItemForm] = useState(false);
    const { reloadStatementsToggle } = useStatementContext();
    const { toast } = useToast();
    const [headerContent, setHeaderContent] = useState<{ [key: string]: string }>({});
    const [footerContent, setFooterContent] = useState<{ [key: string]: string }>({});
    const { contractDetails } = useCustomerDetails();
    const [deleteConfirmModal, setDeleteConfirmModal] = useState(false);
    const [selectedLineItemId, setSelectedLineItemId] = useState<string | null>(null);
    const [selectedNewItemIndex, setSelectedNewItemIndex] = useState<number | null>(null);
    const [nonBillableInfoModal, setNonBillableInfoModal] = useState(false);
    const [toastShown, setToastShown] = useState(false);

    useEffect(() => {
        const fetchInvoiceDetail = async () => {
            if (invoice) {
                setLoading(false);
                if (!toastShown && invoice?.lineItems?.some((item) => item.title?.startsWith("Towne Park Profit Share")) 
                    && invoice?.lineItems.some((item) => item.metaData?.isNonBillableExpense == true) && 
                    !invoice?.lineItems.some((profitShareItem) => profitShareItem.title?.startsWith("Towne Park Profit Share") && 
                    invoice.lineItems?.filter((nonBillableItem) => nonBillableItem.metaData?.isNonBillableExpense == true)
                    .every((nonBillableItem) => profitShareItem.description?.includes(nonBillableItem.title)))) {
                    toast({
                        title: "Warning",
                        description: "Statement must be regenerated to re-calculate Profit Share data.",
                    });
                    setToastShown(true);
                } 
                return;
            }

            try {
                setLoading(true);
                const response = await fetch(`/api/invoice/${invoiceId}/detail`, {
                    method: 'GET',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                });

                if (!response.ok) {
                    throw new Error('Network response was not ok');
                }

                const data: Invoice = await response.json();
                setInvoice(data);
                addInvoiceToCache(invoiceId, data);
            } catch (error) {
                console.error('Fetch error:', error);
                setError('An unexpected error occurred. Please try again later.');
            } finally {
                setLoading(false);
            }
        };

        fetchInvoiceDetail();
    }, [invoiceId, invoice, addInvoiceToCache, toastShown, toast]);

    useEffect(() => {
        if (invoiceConfig) {
            const configHeader = {
                TowneParksAddress: invoiceConfig.TowneParksAddress,
                TowneParksPhone: invoiceConfig.TowneParksPhone,
                TowneParksEmail: invoiceConfig.TowneParksEmail,
                TowneParksLegalName: invoiceConfig.TowneParksLegalName,
                UPPGlobalLegalName: invoiceConfig.UPPGlobalLegalName,
            };

            const configFooter = {
                TowneParksLegalName: invoiceConfig.TowneParksLegalName,
                TowneParksPOBox: invoiceConfig.TowneParksPOBox,
                TowneParksAccountNumber: invoiceConfig.TowneParksAccountNumber,
                TowneParksABA: invoiceConfig.TowneParksABA,
                TowneParksEmail: invoiceConfig.TowneParksEmail,
                UPPGlobalLegalName: invoiceConfig.UPPGlobalLegalName,
                TowneParksAddress: invoiceConfig.TowneParksAddress,
            };

            setHeaderContent(configHeader);
            setFooterContent(configFooter);
        }
    }, [invoiceConfig]);

    const hasProfitShareTitle = invoice?.lineItems?.some(
        (item) => item.title?.startsWith("Towne Park Profit Share")
    );

    const removeLineItem = (index: number) => {
        setNewLineItems(newLineItems.filter((_, i) => i !== index));
    };

    const getTotalAmountDue = () => {
        const existingAmount = invoice?.amount || 0;
        const newItemsAmount = newLineItems.reduce((total, item) => total + item.amount, 0);
        return existingAmount + newItemsAmount;
    };

    const saveChanges = async () => {
        try {
            setSaving(true);
            const response = await fetch(`/api/invoice/${invoiceId}/adhoc`, {
                method: "PATCH",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify(newLineItems),
            });
            if (!response.ok) {
                throw new Error("Failed to save changes.");
            }

            toast({
                title: "Invoice Updated",
                description: "Invoice details have been updated successfully.",
            });

            setSaving(false);
            addInvoiceToCache(invoiceId, null);
            setConfirmSaveModal(false);
            reloadStatementsToggle();
            onCloseModal();
        } catch (error) {
            toast({
                title: "Error saving changes",
                description: "An error occurred while saving the changes.",
            });
        }
    };

    const handleDeleteLineItem = async (lineItemId: string) => {
        try {
            const response = await fetch(`/api/invoice/${invoiceId}/adhoc/${lineItemId}`, {
                method: 'DELETE',
                headers: {
                    'Content-Type': 'application/json',
                }
            });

            if (!response.ok) {
                throw new Error('Failed to delete line item');
            }

            toast({
                title: "Line Item Deleted",
                description: "The line item has been removed successfully.",
            });

            // Refresh invoice data
            addInvoiceToCache(invoiceId, null);
            setDeleteConfirmModal(false);
            reloadStatementsToggle();
        } catch (error) {
            toast({
                title: "Error",
                description: "Failed to delete line item. Please try again.",
                variant: "destructive",
            });
        }
    };

    const handleRemoveClick = (lineItemId?: string, newItemIndex?: number) => {
        if (lineItemId) {
            setSelectedLineItemId(lineItemId);
            setSelectedNewItemIndex(null);
        } else if (newItemIndex !== undefined) {
            setSelectedLineItemId(null);
            setSelectedNewItemIndex(newItemIndex);
        }
        setDeleteConfirmModal(true);
    };

    const handleConfirmDelete = () => {
        if (selectedLineItemId) {
            handleDeleteLineItem(selectedLineItemId);
        } else if (selectedNewItemIndex !== null) {
            removeLineItem(selectedNewItemIndex);
            setDeleteConfirmModal(false);
        }
    };

    console.log("Selected line item id: ", selectedLineItemId);

    if (loading) {
        return <InvoiceDetailsSkeleton />;
    }

    if (error) {
        return <div className="text-center">{error}</div>;
    }

    const handleCancel = () => {
        if (newLineItems.length > 0) {
            setConfirmCanceleModal(true);
        } else {
            onCloseModal();
        }
    };

    const columns = invoice?.purchaseOrder ? 'grid-cols-5' : 'grid-cols-4';

    return (
        <div className="flex flex-col gap-6 mx-auto max-w-10xl p-6 md:p-8 lg:p-10 relative">
            <InvoiceHeader content={headerContent} />
            <div className={`grid ${columns} gap-4 items-center`}>
                <div className="flex flex-col gap-4 border-2 p-4 rounded-lg">
                    <span className="text-sm font-medium text-muted-foreground">Invoice Number</span>
                    <span className="font-medium">{invoice?.invoiceNumber}</span>
                </div>
                <div className="flex flex-col gap-4 border-2 p-4 rounded-xl">
                    <span className="text-sm font-medium text-muted-foreground">Invoice Date</span>
                    <span className="font-medium">{invoice?.invoiceDate}</span>
                </div>
                <div className="flex flex-col gap-4 border-2 p-4 rounded-xl">
                    <span className="text-sm font-medium text-muted-foreground">Payment Terms</span>
                    <span className="font-medium">{invoice?.paymentTerms}</span>
                </div>
                <div className="flex flex-col gap-4 border-2 p-4 rounded-xl">
                    <span className="text-sm font-medium text-muted-foreground">Amount Due</span>
                    <span className="font-medium text-primary">{formatCurrency(getTotalAmountDue())}</span>
                </div>
                {invoice?.purchaseOrder && (
                    <div className="flex flex-col gap-4 border-2 p-4 rounded-xl">
                        <span className="text-sm font-medium text-muted-foreground">PO Number</span>
                        <span className="font-medium">{invoice.purchaseOrder}</span>
                    </div>
                )}
            </div>

            {editMode && (
                <div className="flex justify-end items-center my-4">
                    <Button data-qa-id="button-addLineItem-invoice" variant="outline" onClick={() => setShowLineItemForm(true)} className="flex items-center space-x-2">
                        <Plus className="h-4 w-4" />
                        <span>Add Line Item</span>
                    </Button>
                </div>
            )}
            <Card className="max-w-full">
                <CardHeader>
                    <CardTitle>Invoice Details</CardTitle>
                </CardHeader>
                <CardContent>
                    <div className="flex flex-wrap items-center gap-9">
                        <span className="text-xl font-bold">{invoice?.title}</span>
                        <span className="text-sm text-gray-500">{invoice?.description}</span>
                    </div>
                    <Table>
                        <TableHeader>
                            <TableRow>
                                <TableHead>Title</TableHead>
                                <TableHead>Description</TableHead>
                                <TableHead className="text-right">Amount</TableHead>
                                <TableHead className="text-right">Actions</TableHead>
                            </TableRow>
                        </TableHeader>
                        <TableBody>
                            {invoice?.lineItems?.map((item, index) => (
                                <TableRow data-qa-id={`row-lineItem-${item.metaData?.lineItemId || index}-invoice`} key={index}>
                                    <TableCell>{item.title}</TableCell>
                                    <TableCell>{formatDescription(item.description)}</TableCell>
                                    <TableCell className="text-right">{formatCurrency(item.amount)}</TableCell>
                                    <TableCell className="text-right">
                                        {editMode && item.metaData?.isAdhoc && (
                                            <Button 
                                                data-qa-id={`button-removeLineItem-${item.metaData?.lineItemId}-invoice`}
                                                variant="outline" 
                                                onClick={() => handleRemoveClick(item.metaData?.lineItemId!)}
                                            >
                                                Remove
                                            </Button>
                                        )}
                                    </TableCell>
                                </TableRow>
                            ))}
                            {newLineItems.map((item, index) => (
                                <TableRow data-qa-id={`row-newLineItem-${index}-invoice`} key={index}>
                                    <TableCell>{item.title}</TableCell>
                                    <TableCell>{item.description}</TableCell>
                                    <TableCell className="text-right">{formatCurrency(item.amount)}</TableCell>
                                    <TableCell className="text-right">
                                        <Button 
                                            data-qa-id={`button-removeNewLineItem-${index}-invoice`}
                                            variant="outline" 
                                            onClick={() => handleRemoveClick(undefined, index)}
                                        >
                                            Remove
                                        </Button>
                                    </TableCell>
                                </TableRow>
                            ))}
                        </TableBody>
                    </Table>
                </CardContent>
                <CardFooter className="flex items-center justify-between">
                    <span className="text-sm font-medium text-muted-foreground">Total Amount Due</span>
                    <span className="text-2xl font-bold text-primary">{formatCurrency(getTotalAmountDue())}</span>
                </CardFooter>
            </Card>
            <InvoiceFooter content={footerContent} />

            <Dialog data-qa-id="dialog-saveConfirm-invoice" open={confirmSaveModal} onOpenChange={setConfirmSaveModal}>
                <DialogContent>
                    <DialogTitle>Are you sure you want to save changes?</DialogTitle>
                    <DialogFooter>
                        <div className="flex justify-end mt-4 space-x-2">
                            {saving ? (
                                <Button data-qa-id="button-cancelSave-invoice" variant="outline" disabled>Cancel</Button>
                            ) : (
                                <Button data-qa-id="button-cancelSave-invoice" variant="outline" onClick={() => setConfirmSaveModal(false)}>Cancel</Button>
                            )}
                            <Button data-qa-id="button-confirmSave-invoice" onClick={saveChanges}>
                                {saving ? (
                                    <PulseLoader size={8} color="#3b82f6" />
                                ) : (
                                    "Yes"
                                )}
                            </Button>
                        </div>
                    </DialogFooter>
                </DialogContent>
            </Dialog>

            <Dialog data-qa-id="dialog-cancelConfirm-invoice" open={confirmCanceleModal} onOpenChange={() => setConfirmCanceleModal(false)}>
                <DialogContent>
                    <DialogTitle>Do you want to leave without saving the changes?</DialogTitle>
                    <DialogFooter className="flex justify-end mt-4 space-x-2">
                        <Button data-qa-id="button-stayOnPage-invoice" variant="outline" onClick={() => setConfirmCanceleModal(false)}>No</Button>
                        <Button data-qa-id="button-leavePage-invoice" onClick={() => onCloseModal()}>Yes</Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>

            <Dialog data-qa-id="dialog-deleteConfirm-invoice" open={deleteConfirmModal} onOpenChange={setDeleteConfirmModal}>
                <DialogContent>
                    <DialogTitle>Confirm Delete</DialogTitle>
                    <DialogDescription>
                    <span>Are you sure you want to delete this line item? This action cannot be undone.</span>
                    </DialogDescription>
                    <DialogFooter className="flex justify-end mt-4 space-x-2">
                        <Button data-qa-id="button-cancelDelete-invoice" variant="outline" onClick={() => setDeleteConfirmModal(false)}>
                            Cancel
                        </Button>
                        <Button  
                            data-qa-id="button-confirmDelete-invoice"
                            onClick={handleConfirmDelete}
                        >
                            Delete
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>

            <Dialog data-qa-id="dialog-nonBillableExpense-info" open={nonBillableInfoModal} onOpenChange={setNonBillableInfoModal}>
                <DialogContent>
                    <DialogTitle>Warning</DialogTitle>
                    <DialogDescription>
                        <span>As Non Billable Expense Line Item is added, Statement must be regenerated to re-calculate the Profit Share data</span>
                    </DialogDescription>
                    <DialogFooter className="flex justify-end mt-4 space-x-2">
                        <Button  
                            data-qa-id="button-confirm-Info"
                            onClick={() => setNonBillableInfoModal(false)}
                        >
                        Ok
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>

            <Modal data-qa-id="modal-addLineItem-invoice" show={showLineItemForm} onClose={() => setShowLineItemForm(false)} disableBackdropClick={true}>
                <AdHocLineItemForm
                    onAddLineItem={(lineItem) => {
                        if (lineItem.metaData?.isNonBillableExpense == true && hasProfitShareTitle) {
                            setNonBillableInfoModal(true);
                        }
                        setNewLineItems([...newLineItems, lineItem]);
                        setShowLineItemForm(false);
                    }}
                    invoiceNumber={invoice?.invoiceNumber || "1"}
                />
            </Modal>

            {editMode && (
                <div className="flex justify-end">
                    <Button data-qa-id="button-cancelEdit-invoice" variant="outline" className="ml-2" onClick={handleCancel}>Cancel</Button>
                    {newLineItems.length > 0 && (
                        <Button data-qa-id="button-saveChanges-invoice" className="ml-2" onClick={() => setConfirmSaveModal(true)}>Save</Button>
                    )}
                </div>
            )}
        </div>
    );
};

export default InvoiceDetails;
