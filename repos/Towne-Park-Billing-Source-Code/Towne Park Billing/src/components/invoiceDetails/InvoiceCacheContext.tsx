import { Invoice, InvoiceConfigDto } from "@/lib/models/Invoice";
import React, { createContext, ReactNode, useContext, useEffect, useState } from "react";

interface InvoiceCacheContextValue {
    invoiceCache: { [invoiceId: string]: Invoice };
    addInvoiceToCache: (invoiceId: string, invoice: Invoice | null) => void;
    invoiceConfig: { [key: string]: string };
}

const InvoiceCacheContext = createContext<InvoiceCacheContextValue | null>(null);

export const useInvoiceCache = () => {
    const context = useContext(InvoiceCacheContext);
    if (!context) {
        throw new Error("useInvoiceCache must be used within an InvoiceCacheProvider");
    }
    return context;
};

interface InvoiceCacheProviderProps {
    children: ReactNode;
}

export const InvoiceCacheProvider: React.FC<InvoiceCacheProviderProps> = ({ children }) => {
    const [invoiceCache, setInvoiceCache] = useState<{ [invoiceId: string]: Invoice }>({});
    const [invoiceConfig, setInvoiceConfig] = useState<{ [key: string]: string }>({});

    // Fetch invoice configuration and cache it
    useEffect(() => {
        const fetchInvoiceConfig = async () => {
            try {
                const response = await fetch(`/api/invoice-config?configGroup=InvoiceHeaderFooter`, {
                    method: 'GET',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                });
                if (!response.ok) {
                    throw new Error('Network response was not ok');
                }
                const data: InvoiceConfigDto[] = await response.json();
                
                const headerContent: { [key: string]: string } = {};
                const footerContent: { [key: string]: string } = {};
                
                data.forEach((item) => {
                    headerContent[item.key] = item.value;
                    if ([
                        invoiceConfig.TowneParksLegalName,
                        invoiceConfig.TowneParksPOBox,
                        invoiceConfig.TowneParksAccountNumber,
                        invoiceConfig.TowneParksABA,
                        invoiceConfig.TowneParksEmail,
                        invoiceConfig.UPPGlobalLegalName
                    ].includes(item.key)) {
                        footerContent[item.key] = item.value;
                    }
                });

                setInvoiceConfig({ ...headerContent, ...footerContent });
            } catch (error) {
                console.error('Error fetching invoice config:', error);
            }
        };

        fetchInvoiceConfig();
    }, []);

    const addInvoiceToCache = (invoiceId: string, invoice: Invoice | null) => {
        setInvoiceCache((prevCache) => {
            if (invoice === null) {
                const { [invoiceId]: _, ...rest } = prevCache;
                return rest;
            }
            return { ...prevCache, [invoiceId]: invoice };
        });
    };

    return (
        <InvoiceCacheContext.Provider value={{ invoiceCache, addInvoiceToCache, invoiceConfig }}>
            {children}
        </InvoiceCacheContext.Provider>
    );
};
