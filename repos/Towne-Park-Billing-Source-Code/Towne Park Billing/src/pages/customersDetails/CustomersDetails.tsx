import { StatementContext } from "@/components/BillingStatement/StatementContext";
import ContractDetailsTab from "@/components/TabsCustomersDetails/ContractDetailsTab";
import GeneralInfoTab from "@/components/TabsCustomersDetails/GeneralInfoTab";
import { Skeleton } from "@/components/ui/skeleton";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { useToast } from "@/components/ui/use-toast";
import { useAuth } from "@/contexts/AuthContext";
import { Contract } from "@/lib/models/Contract";
import { CustomerDetail } from "@/lib/models/GeneralInfo";
import { Statement } from "@/lib/models/Statement";
import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import StatementsGlobalList from "../statements/StatementsGlobalList";
import { useCustomerDetails } from "./CustomersDetailContext";

export function CustomersDetails() {
    const { customerSiteId } = useParams<{ customerSiteId: string }>();
    const { customer, setCustomer, fetchCustomerDetails, contractDetails, setContractDetails, contractId, fetchContractDetails } = useCustomerDetails();
    const [isLoading, setIsLoading] = useState(true);
    const [statements, setStatements] = useState<Statement[]>([]);
    const [reloadStatements, setReloadStatements] = useState(false);
    const { toast } = useToast();
    const { userRoles } = useAuth();
    const [rolesLoaded, setRolesLoaded] = useState(true);
    const [activeTab, setActiveTab] = useState<string>('');

    const handleContractUpdate = (updatedContractDetails: Contract) => {
        if (contractId) {
            setContractDetails({ ...updatedContractDetails, id: contractId });
        } else {
            console.warn("Contract ID is null and cannot be used for updating!");
        }
    };

    const handleGeneralInfoUpdate = async (updatedCustomer: CustomerDetail) => {
        console.log('Updated Customer Payload:', updatedCustomer);
        try {
            const response = await fetch(`/api/customers/${customerSiteId}`, {
                method: 'PATCH',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(updatedCustomer),
            });
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            setCustomer(updatedCustomer);
            toast({
                title: "Success!",
                description: "Customer's General Info updated successfully!",
            });
        } catch (error) {
            console.error('There was a problem with your fetch operation:', error);
            toast({
                title: "Error",
                description: 'An unexpected error occurred. Please try again later.',
            });
        }
    };

    useEffect(() => {
        if (customerSiteId) {
            const loadCustomerDetails = async () => {
                setIsLoading(true);
                await fetchCustomerDetails(customerSiteId);
                await fetchContractDetails(customerSiteId);
                setIsLoading(false);
            };

            loadCustomerDetails();
        }
    }, [customerSiteId]);

    useEffect(() => {
        fetch(`/api/customers/${customerSiteId}/statements`)
            .then(response => {
                if (!response.ok) {
                    throw new Error('Network response was not ok');
                }
                return response.json();
            })
            .then(data => {
                setStatements(data);
            })
            .catch(error => {
                console.error('There was a problem with your fetch operation:', error);
            });
    }, [customerSiteId, reloadStatements]);

    // Set active tab when the component loads
    useEffect(() => {
        const isBillingManager = userRoles.includes('billingManager');
        const defaultTab = isBillingManager ? "general-info" : "contract-details";
        setActiveTab(defaultTab);
    }, [userRoles]);

    if (isLoading || !rolesLoaded) {
        return (
            <div className="container mx-auto p-4 md:p-6">
                <Skeleton className="h-8 w-1/6 mb-4 md:mb-6" data-testid="loading-indicator" />
                <div className="p-6">
                    <Skeleton className="h-20 w-full" />
                </div>
            </div>
        );
    }

    const isBillingManager = userRoles.includes('billingManager');

    return (
        <div className="container mx-auto p-4 md:p-6">
        <div className="flex items-center justify-between mb-6">
            <div>
                <h1 className="text-3xl font-bold">{customer?.siteName}</h1>
                <p className="text-muted-foreground text-xl">{customer?.address}</p>
            </div>
            {/* 
            We will use this button for future enhancements
            <div className="flex items-center gap-2">
                <Button variant="outline" size="sm">
                    <MoreVerticalIcon className="w-4 h-4 mr-2" />
                    More
                </Button>
            </div> */}
        </div>
        <Tabs value={activeTab} onValueChange={setActiveTab} data-qa-id="tabs-customerDetails">
            <TabsList data-qa-id="tabsList-customerDetails">
                {isBillingManager && (
                    <TabsTrigger value="general-info" data-qa-id="tab-generalInfo" data-testid="general-info-tab">General Info</TabsTrigger>
                )}
                <TabsTrigger value="contract-details" data-qa-id="tab-contractDetails" data-testid="contract-details-tab1">Contract Details</TabsTrigger>
                <TabsTrigger value="billing-statement" data-qa-id="tab-billingStatement" data-testid="billing-statement-tab1">Statements</TabsTrigger>
            </TabsList>
            {isBillingManager && (
                <TabsContent value="general-info" data-qa-id="tabContent-generalInfo" data-testid="customer-details">
                    <GeneralInfoTab customer={customer} onSave={handleGeneralInfoUpdate} />
                </TabsContent>
            )}
            <TabsContent value="contract-details" data-qa-id="tabContent-contractDetails">
                {customer && <ContractDetailsTab contractDetails={contractDetails} customerDetail={customer} onUpdateContractDetails={handleContractUpdate} />}
            </TabsContent>
            <TabsContent value="billing-statement" data-qa-id="tabContent-billingStatement" data-testid="billing-statement-data">
                <StatementContext.Provider value={{ reloadStatementsToggle: () => setReloadStatements(prev => !prev) }}>
                    <StatementsGlobalList customerSiteId={customerSiteId}/>
                    {/* <StatementDataTable statements={statements} /> */}
                </StatementContext.Provider>
            </TabsContent>
        </Tabs>
    </div>
    );
}
