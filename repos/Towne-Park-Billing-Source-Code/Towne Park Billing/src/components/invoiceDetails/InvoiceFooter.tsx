import { Card, CardContent } from "@/components/ui/card";
import React from "react";
import { useCustomerDetails } from "../../pages/customersDetails/CustomersDetailContext";
import { formatAddress, splitAddress } from "@/lib/utils";

interface InvoiceFooterProps {
    content: { [key: string]: string };
}

const InvoiceFooter: React.FC<InvoiceFooterProps> = ({ content }) => {
    const { customer } = useCustomerDetails();

    return (
        <Card>
            <CardContent>
                <h2 className="font-semibold mb-4 mt-6">
                    For information regarding your account, please contact Towne Park Accounting at {content["TowneParksEmail"]}
                </h2>
                <div className="flex justify-between">
                    <div className="whitespace-pre-line">
                        <p className="font-bold">Check Payments:</p>
                        {content["UPPGlobalLegalName"] && customer?.glString.startsWith("22") ? `Please make checks payable to ${content["UPPGlobalLegalName"]}\n` : content["TowneParksLegalName"] && `Please make checks payable to ${content["TowneParksLegalName"]}\n`}
                        {content["TowneParksPOBox"] &&
                            `Remit payments to:\n${customer?.glString.startsWith("22") ? formatAddress(content["TowneParksAddress"]) : `P.O. Box ${splitAddress(content["TowneParksPOBox"], false)}`
                            }\n`
                        }
                    </div>
                    <div className="whitespace-pre-line">
                        <p className="font-bold">ACH Payments:</p>
                        {content["TowneParksAccountNumber"] && `Account number: ${content["TowneParksAccountNumber"]}\n`}
                        {content["TowneParksABA"] && `ABA: ${content["TowneParksABA"]}\n`}
                        {content["TowneParksEmail"] && `${content["TowneParksEmail"]}\n`}
                    </div>
                </div>
            </CardContent>
        </Card>
    );
};

export default InvoiceFooter;
