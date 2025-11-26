import { useCustomer } from "@/contexts/CustomerContext";
import { Customer } from "@/lib/models/Statistics";
import { CardHeader } from "react-bootstrap";
import { Card, CardContent, CardTitle } from "../ui/card";
import { Label } from "../ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "../ui/select";

interface CustomerSiteWidgetProps {
    customers: Customer[];
    isLoadingCustomers: boolean;
    error: string | null;
    selectedSite: string;
    setSelectedSite: (siteId: string) => void;
    totalRooms?: number;
    isSidebarDisabled?: boolean;
}

export default function CustomerSiteWidget({
    customers,
    isLoadingCustomers,
    error,
    selectedSite,
    setSelectedSite,
    totalRooms,
    isSidebarDisabled
}: CustomerSiteWidgetProps) {
    const { setSelectedCustomerById } = useCustomer();

    const selectedCustomer = selectedSite
        ? customers.find(c => c.customerSiteId === selectedSite)
        : undefined;

const handleSiteChange = (siteId: string) => {
    setSelectedSite(siteId); 
};

    return (
        <Card>
            <CardHeader className="mt-4 ml-4">
                <CardTitle>Site Selection</CardTitle>
            </CardHeader>
            <CardContent>
                <div className="mt-4">
                    <Label htmlFor="site-select" className="font-medium">
                        Customer Site
                    </Label>
                    <Select
                        value={selectedSite}
                        onValueChange={handleSiteChange}
                        disabled={isLoadingCustomers || isSidebarDisabled}
                        data-qa-id="customer-site-widget-select"
                    >
                        <SelectTrigger className="w-full mt-2">
                            {isLoadingCustomers ? (
                                <SelectValue placeholder="Loading customer sites..." />
                            ) : (
                                <SelectValue placeholder="Select a site" />
                            )}
                        </SelectTrigger>
                        <SelectContent>
                            {customers.map((customer) => (
                                <SelectItem
                                    key={customer.customerSiteId}
                                    value={customer.customerSiteId}
                                    data-qa-id={`customer-site-widget-item-${customer.siteNumber}`}
                                >
                                   {customer.siteNumber} - {customer.siteName} 
                                </SelectItem>
                            ))}
                        </SelectContent>
                    </Select>
                </div>
                {selectedCustomer && (
                    <div className="mt-4">
                        <div className="rounded-md">
                            <div className="flex justify-between">
                                <p className="text-sm text-muted-foreground">Site number:</p>
                                <p className="text-sm">{selectedCustomer.siteNumber}</p>
                            </div>
                            {totalRooms !== undefined && (
                                <div className="flex justify-between mt-1">
                                    <p className="text-sm text-muted-foreground">Available Rooms:</p>
                                    <p className="text-sm">{totalRooms}</p>
                                </div>
                            )}
                        </div>
                    </div>
                )}
                {error && <p className="text-red-500 mt-2">{error}</p>}
            </CardContent>
        </Card>
    );
}
