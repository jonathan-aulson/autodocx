import { CustomerSummary } from "@/lib/models/GeneralInfo";
import { Customer } from "@/lib/models/Statistics";
import { createContext, ReactNode, useContext, useEffect, useState } from "react";

export interface UnifiedCustomer extends Customer {
  district?: string | null;
  billingType?: string | null;
  contractType?: string | null;
  accountManager?: string | null;
  districtManager?: string | null;
  legalEntity?: string | null;
  plCategory?: string | null;
  svpRegion?: string | null;
  cogSegment?: string | null;
  businessSegment?: string | null;
}

interface CustomerContextType {
  customers: UnifiedCustomer[];
  customerSummaries: CustomerSummary[];
  isLoading: boolean;
  error: string | null;
  selectedCustomer: UnifiedCustomer | null;
  fetchCustomers: (isForecast?: boolean) => Promise<void>;
  fetchCustomerSummaries: (isForecast?: boolean) => Promise<void>;
  setSelectedCustomer: (customer: UnifiedCustomer | null) => void;
  setSelectedCustomerById: (customerId: string) => void;
}

const CustomerContext = createContext<CustomerContextType | undefined>(undefined);

export function CustomerProvider({ children }: { children: ReactNode }) {
  const [customers, setCustomers] = useState<UnifiedCustomer[]>([]);
  const [customerSummaries, setCustomerSummaries] = useState<CustomerSummary[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedCustomer, setSelectedCustomer] = useState<UnifiedCustomer | null>(null);

  const fetchCustomers = async (isForecast: boolean = false) => {
    setIsLoading(true);
    setError(null);
    try {
      const response = await fetch(`/api/customers?isForecast=${isForecast}`);
      
      if (!response.ok) {
        throw new Error(`Error fetching customers: ${response.status}`);
      }

      const data = await response.json();
      const sortedCustomers: UnifiedCustomer[] = data.sort((a: Customer, b: Customer) => 
        a.siteNumber.localeCompare(b.siteNumber)
      );
      setCustomers(sortedCustomers);
    } catch (err: any) {
      console.error('Failed to fetch customers:', err);
      setError(err.message || 'Failed to load customers');
    } finally {
      setIsLoading(false);
    }
  };

  const fetchCustomerSummaries = async (isForecast: boolean = false) => {
    try {
      const response = await fetch(`/api/customers?isForecast=${isForecast}`);
      
      if (!response.ok) {
        throw new Error(`Error fetching customer summaries: ${response.status}`);
      }

      const data = await response.json();
      setCustomerSummaries(data);
    } catch (err: any) {
      console.error('Failed to fetch customer summaries:', err);
    }
  };

  const setSelectedCustomerById = (customerId: string) => {
    let customer = customers.find(c => c.customerSiteId === customerId);
    
    if (customer) {
      setSelectedCustomer(customer);
    } else {
      const summary = customerSummaries.find(c => c.customerSiteId === customerId);
      if (summary) {
        const unifiedCustomer: UnifiedCustomer = {
          customerSiteId: summary.customerSiteId,
          siteName: summary.siteName,
          siteNumber: summary.siteNumber,
          district: summary.district,
          contractType: summary.contractType,
          accountManager: summary.accountManager,
          districtManager: summary.districtManager,
          legalEntity: summary.legalEntity,
          plCategory: summary.plCategory,
          svpRegion: summary.svpRegion,
          cogSegment: summary.cogSegment,
          businessSegment: summary.businessSegment
        };
        setSelectedCustomer(unifiedCustomer);
      } else if (customerId) {
        console.warn(`Customer with ID ${customerId} not found in current lists`);
      }
    }
  };

  useEffect(() => {
    fetchCustomers();
    fetchCustomerSummaries();
  }, []);

  const value: CustomerContextType = {
    customers,
    customerSummaries,
    isLoading,
    error,
    selectedCustomer,
    fetchCustomers,
    fetchCustomerSummaries,
    setSelectedCustomer,
    setSelectedCustomerById
  };

  return (
    <CustomerContext.Provider value={value}>
      {children}
    </CustomerContext.Provider>
  );
}

export function useCustomer() {
  const context = useContext(CustomerContext);
  if (context === undefined) {
    throw new Error("useCustomer must be used within a CustomerProvider");
  }
  return context;
}
