import Pagination from "@/components/Pagination";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Statement } from "@/lib/models/Statement";
import { formatCurrency } from "@/lib/utils";
import { ChevronDown, ChevronUp } from "lucide-react";
import React, { Fragment, useState } from "react";
import { InvoiceCacheProvider } from "../invoiceDetails/InvoiceCacheContext";
import BillingStatementDetails from "./billingStatementDetails";
import StatementStatusBadge from "./statementStatusBadge";

const StatementTableHeader = () => (
    <TableHeader>
        <TableRow>
            <TableHead className="w-1/5">Billing Cycle</TableHead>
            <TableHead className="w-2/5">Service Period</TableHead>
            <TableHead className="w-1/5">Status</TableHead>
            <TableHead className="w-1/5">Total</TableHead>
            <TableHead className="w-10"></TableHead>
        </TableRow>
    </TableHeader>
);

const StatementDataTable: React.FC<{ statements: Statement[] }> = ({ statements }) => {
    const [currentPage, setCurrentPage] = useState(1);
    const [expandedStatements, setExpandedStatements] = useState<string[]>([]);
    const statementsPerPage = 12;

    const toggleStatement = (id: string) => {
        setExpandedStatements((prev) =>
            prev.includes(id) ? prev.filter((sid) => sid !== id) : [...prev, id]
        );
    };

    const paginatedStatements = statements.slice(
        (currentPage - 1) * statementsPerPage,
        currentPage * statementsPerPage
    );

    return (
<InvoiceCacheProvider>
        <div className="py-8">
            <div className="flex items-center justify-between mb-6">
                <h2 className="text-2xl font-semibold">Statements</h2>
                <div className="flex items-center gap-2">
                    {/* <Button variant="outline" size="sm">
                    <MoreVerticalIcon className="w-4 h-4 mr-2" />
                    More
                </Button> */}

                </div>
            </div>
            <Card>
                {statements.length === 0 ? (
                    <h2 className="text-center">No statements found</h2>
                ) : (
                    <>
                        <Table className="w-full table-auto">
                            <StatementTableHeader />
                            <TableBody>
                                {paginatedStatements.map((statement) => (
                                    <Fragment key={statement.id}>
                                        <TableRow
                                            data-qa-id="row-statementItem-statements"
                                            className={`cursor-pointer ${expandedStatements.includes(statement.id) ? "bg-gray-100 dark:bg-gray-900" : ""}`}
                                            onClick={() => toggleStatement(statement.id)}
                                        >
                                            <TableCell>{statement.createdMonth}</TableCell>
                                            <TableCell>{statement.servicePeriod}</TableCell>
                                            <TableCell>
                                                <StatementStatusBadge status={statement.status} />
                                            </TableCell>
                                            <TableCell>{formatCurrency(statement.totalAmount)}</TableCell>
                                            <TableCell className="w-10 text-center" onClick={(e) => e.stopPropagation()}>
                                                <Button
                                                    data-qa-id="button-toggleStatement-statements"
                                                    variant="ghost"
                                                    size="icon"
                                                    onClick={() => toggleStatement(statement.id)}
                                                >
                                                    {expandedStatements.includes(statement.id) ? (
                                                        <ChevronUp className="h-4 w-4" />
                                                    ) : (
                                                        <ChevronDown className="h-4 w-4" />
                                                    )}
                                                </Button>
                                            </TableCell>
                                        </TableRow>
                                        {expandedStatements.includes(statement.id) && (
                                            <TableRow>
                                                <TableCell colSpan={5} className="p-0">
                                                    <BillingStatementDetails invoices={statement.invoices} statementStatus={statement.status} userRoles={['']} />
                                                </TableCell>
                                            </TableRow>
                                        )}
                                    </Fragment>
                                ))}
                            </TableBody>
                        </Table>
                        {statements.length > 0 && (
                            <Pagination
                                data-qa-id="pagination-controls-statements"
                                totalItems={statements.length}
                                itemsPerPage={statementsPerPage}
                                currentPage={currentPage}
                                onPageChange={setCurrentPage}
                            />
                        )}
                    </>
                )}
            </Card>
        </div>
    </InvoiceCacheProvider>
    );
};

export default StatementDataTable;
