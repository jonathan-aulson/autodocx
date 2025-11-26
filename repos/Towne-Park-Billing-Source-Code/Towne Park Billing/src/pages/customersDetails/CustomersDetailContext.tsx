import { Contract } from "@/lib/models/Contract";
import { CustomerDetail } from "@/lib/models/GeneralInfo";
import React, { createContext, ReactNode, useContext, useState } from 'react';

interface CustomerContextProps {
    customer: CustomerDetail | null;
    setCustomer: React.Dispatch<React.SetStateAction<CustomerDetail | null>>;
    fetchCustomerDetails: (customerSiteId: string) => Promise<void>;
    contractDetails: Contract | null;
    setContractDetails: React.Dispatch<React.SetStateAction<Contract | null>>;
    contractId: string | null;
    setContractId: React.Dispatch<React.SetStateAction<string | null>>;
    fetchContractDetails: (customerSiteId: string) => Promise<void>;
}

const CustomerContext = createContext<CustomerContextProps | undefined>(undefined);

const CustomerProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
    const [customer, setCustomer] = useState<CustomerDetail | null>(null);
    const [contractDetails, setContractDetails] = useState<Contract | null>(null);
    const [contractId, setContractId] = useState<string | null>(null);

    const fetchCustomerDetails = async (customerSiteId: string) => {
        try {
            const response = await fetch(`/api/customers/${customerSiteId}`);
            if (!response.ok) {
                throw new Error("Network response was not ok");
            }
            const data: CustomerDetail = await response.json();
            setCustomer(data);
        } catch (error) {
            console.error("Error fetching customer details:", error);
        }
    };

    const fetchContractDetails = async (customerSiteId: string) => {
        try {
            const response = await fetch(`/api/customers/${customerSiteId}/contract`);
            if (!response.ok) {
                throw new Error("Network response was not ok");
            }
            const data: Contract = await response.json();
            setContractDetails(data);
            setContractId(data.id);
        } catch (error) {
            console.error("Error fetching contract details:", error);
        }
    };

    return (
        <CustomerContext.Provider 
            value={{ 
                customer, 
                setCustomer, 
                fetchCustomerDetails, 
                contractDetails, 
                setContractDetails,
                contractId,
                setContractId,
                fetchContractDetails
            }}
        >
            {children}
        </CustomerContext.Provider>
    );
};

// Hook to use the context
const useCustomerDetails = () => {
    const context = useContext(CustomerContext);
    if (!context) {
        throw new Error("useCustomerDetails must be used within a CustomerProvider");
    }
    return context;
}

export { CustomerProvider, useCustomerDetails };

