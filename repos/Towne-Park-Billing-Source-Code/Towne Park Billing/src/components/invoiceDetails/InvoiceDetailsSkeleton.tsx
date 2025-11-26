import { Card, CardContent, CardFooter, CardHeader } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";

const InvoiceDetailsSkeleton: React.FC = () => {
    return (
        <div data-testid="skeleton" className="flex flex-col gap-6 mx-auto max-w-4xl p-6 md:p-8 lg:p-10">
            <div className="grid grid-cols-4 gap-4 items-center">
                <Skeleton data-qa-id="skeleton-invoiceNumber-loading" className="h-12 w-48 border-2 p-4 rounded-lg" />
                <Skeleton data-qa-id="skeleton-invoiceDate-loading" className="h-12 w-48 border-2 p-4 rounded-xl" />
                <Skeleton data-qa-id="skeleton-paymentTerms-loading" className="h-12 w-48 border-2 p-4 rounded-xl" />
                <Skeleton data-qa-id="skeleton-amountDue-loading" className="h-12 w-48 border-2 p-4 rounded-xl" />
            </div>
            <Card className="max-w-full">
                <CardHeader>
                    <Skeleton data-qa-id="skeleton-cardTitle-loading" className="h-8 w-40" />
                </CardHeader>
                <CardContent>
                    <Table>
                        <TableHeader>
                            <TableRow>
                                <TableHead>
                                    <Skeleton data-qa-id="skeleton-headerTitle-loading" className="h-6 w-24" />
                                </TableHead>
                                <TableHead>
                                    <Skeleton data-qa-id="skeleton-headerDescription-loading" className="h-6 w-32" />
                                </TableHead>
                                <TableHead className="text-right">
                                    <Skeleton data-qa-id="skeleton-headerAmount-loading" className="h-6 w-20" />
                                </TableHead>
                            </TableRow>
                        </TableHeader>
                        <TableBody>
                            {[...Array(2)].map((_, index) => (
                                <TableRow key={index}>
                                    <TableCell>
                                        <Skeleton data-qa-id={`skeleton-itemTitle-${index}-loading`} className="h-6 w-24" />
                                    </TableCell>
                                    <TableCell>
                                        <Skeleton data-qa-id={`skeleton-itemDescription-${index}-loading`} className="h-6 w-32" />
                                    </TableCell>
                                    <TableCell className="text-right">
                                        <Skeleton data-qa-id={`skeleton-itemAmount-${index}-loading`} className="h-6 w-20" />
                                    </TableCell>
                                </TableRow>
                            ))}
                        </TableBody>
                    </Table>
                </CardContent>
                <CardFooter className="flex items-center justify-between">
                    <Skeleton data-qa-id="skeleton-footerLabel-loading" className="h-6 w-32" />
                    <Skeleton data-qa-id="skeleton-footerTotal-loading" className="h-6 w-20" />
                </CardFooter>
            </Card>
        </div>
    );
};

export default InvoiceDetailsSkeleton;
