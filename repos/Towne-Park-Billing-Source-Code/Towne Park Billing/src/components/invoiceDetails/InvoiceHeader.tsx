import { Card, CardContent, CardHeader } from "@/components/ui/card";
import React from "react";
import { useCustomerDetails } from "../../pages/customersDetails/CustomersDetailContext";
import { formatAddress, splitAddress } from "@/lib/utils";

interface InvoiceHeaderProps {
    content: { [key: string]: string };
}

const InvoiceHeader: React.FC<InvoiceHeaderProps> = ({ content }) => {
    const { customer } = useCustomerDetails();

    return (
        <Card>
            <CardHeader>
                <div className="flex justify-between items-center">
                    <div></div>
                    <div className={customer?.glString.startsWith("22") ? "flex items-start mr-36" : "flex items-start mr-14"}>
                        <img src={customer?.glString.startsWith("22") ? "/upp-logo.png" : "/tp-logo.png"} alt="Company Logo" className={customer?.glString.startsWith("22") ? "h-24" : "h-8 m-2"} />
                    </div>
                </div>
                <div className="flex justify-between">
                    <div>
                        <CardContent className="font-semibold" style={{ marginTop: -42 }}>
                            <strong>Bill to:</strong>
                            <p>{customer?.siteName}</p>
                            <p>{customer?.invoiceRecipient}</p>
                            <p className="whitespace-pre-line">{customer?.address ? splitAddress(customer?.address, true) : ""}</p>
                        </CardContent>
                    </div>
                    <div className="flex">
                        <CardContent>
                            <p className="whitespace-pre-line">{formatAddress(content["TowneParksAddress"])}</p>
                            <p>{content["TowneParksEmail"]}</p>
                        </CardContent>
                    </div>
                </div>
            </CardHeader>
        </Card>
    );
};

export default InvoiceHeader;
