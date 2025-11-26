import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { InvoiceSummary, StatementStatus } from "@/lib/models/Statement";
import { formatCurrency } from "@/lib/utils";
import { Edit, ScanEye } from "lucide-react";
import React, { useState } from "react";
import InvoiceDetails from "../invoiceDetails/InvoiceDetails";
import Modal from "../Modal";

interface BillingStatementDetailsProps {
    invoices: InvoiceSummary[];
    statementStatus: string;
    userRoles: string[];
}

const BillingStatementDetails: React.FC<BillingStatementDetailsProps> = ({ invoices, statementStatus, userRoles }) => {
    const [showModal, setShowModal] = useState(false);
    const [selectedInvoiceId, setSelectedInvoiceId] = useState<string | null>(null);
    const [editMode, setEditMode] = useState(false);

    const handleModalOpen = (invoiceId: string, isEditMode: boolean) => {
        setSelectedInvoiceId(invoiceId);
        setEditMode(isEditMode);
        setShowModal(true);
    };

    const sortedInvoices = [...invoices].sort((a, b) => {
        const numA = parseInt(a.invoiceNumber.replace(/\D/g, ''));
        const numB = parseInt(b.invoiceNumber.replace(/\D/g, ''));
        return numA - numB;
    });

    const isBillingManager = userRoles.includes('billingManager');

    if (!invoices || invoices.length === 0) {
        return <h2 className="text-center">No invoices found.</h2>;
    }

    return (
<div className="p-2 rounded-lg w-full">
        <Table className="w-full table-auto">
            <TableHeader>
                <TableRow>
                    <TableHead className="w-1/4 px-4 py-3 text-left font-medium">Invoice #</TableHead>
                    <TableHead className="w-1/4 px-4 py-3 text-right font-medium">Amount Due</TableHead>
                    <TableHead className="w-1/2 px-4 py-3 text-right font-medium">Actions</TableHead>
                </TableRow>
            </TableHeader>
            <TableBody>
                {sortedInvoices.map((invoice) => (
                    <TableRow data-qa-id="row-invoiceItem-invoices" key={invoice.id} className="border-b transition-colors hover:bg-muted/50">
                        <TableCell className="w-1/4 px-4 py-3 font-medium">
                            {invoice.invoiceNumber}
                        </TableCell>
                        <TableCell className="w-1/4 px-4 py-3 text-right font-medium">
                            {formatCurrency(invoice.amount)}
                        </TableCell>
                        <TableCell className="w-1/2 px-4 py-3 text-right font-medium">
                            <div className="flex items-center justify-end gap-2">
                                <Button data-qa-id="button-viewInvoice-invoices" variant="ghost" size="icon" onClick={() => handleModalOpen(invoice.id, false)}>
                                    <ScanEye className="h-4 w-4" />
                                    <span className="sr-only">View Invoice</span>
                                </Button>
                                {statementStatus !== StatementStatus.SENT && isBillingManager && (
                                    <Button data-qa-id="button-editInvoice-invoices" variant="ghost" onClick={() => handleModalOpen(invoice.id, true)}>
                                        <Edit className="h-4 w-4" />
                                        <span className="sr-only">Edit invoice</span>
                                    </Button>
                                )}
                            </div>
                        </TableCell>
                    </TableRow>
                ))}
            </TableBody>
        </Table>

        <Modal data-qa-id="modal-invoiceDetails-invoices" show={showModal} onClose={() => setShowModal(false)} disableBackdropClick={true}>
            {selectedInvoiceId && (
                <InvoiceDetails
                    invoiceId={selectedInvoiceId}
                    editMode={editMode}
                    onCloseModal={() => setShowModal(false)}
                />
            )}
        </Modal>
    </div>
    );
};

export default BillingStatementDetails;
