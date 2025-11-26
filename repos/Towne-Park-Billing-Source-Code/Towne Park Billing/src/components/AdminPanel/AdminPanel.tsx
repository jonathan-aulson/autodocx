import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { useEffect, useState } from "react";
import JobGroupManagement from "./JobGroupManagement/JobGroupManagement";
import PriorMonth from "./PriorMonth/PriorMonth";
import RevenueReviewThreshold from "./RevenueReviewThreshold/RevenueReviewThreshold";
import UnitAccounts from "./UnitAccounts/UnitAccounts";

export default function AdminPanel() {
    const [activeTab, setActiveTab] = useState("revenue-review");

    useEffect(() => {
        // Get tab from URL when component mounts
        const urlParams = new URLSearchParams(window.location.search);
        const tab = urlParams.get("tab");
        if (tab === "unit-accounts") {
            setActiveTab("unit-accounts");
        } else if (tab === "prior-month") {
            setActiveTab("prior-month");
        } else if (tab === "job-group-management") {
            setActiveTab("job-group-management");
        } else {
            setActiveTab("revenue-review");
        }
    }, []);

    const handleTabChange = (value: string) => {
        setActiveTab(value);

        // Update URL without refreshing the page
        const url = new URL(window.location.href);
        url.searchParams.set("tab", value);
        window.history.pushState({}, "", url);
    };

    return (
        <div className="container mx-auto py-6">
            <h1 className="text-3xl font-bold mb-6">Admin Panel</h1>

            <Tabs value={activeTab} onValueChange={handleTabChange}>
                <TabsList>
                    <TabsTrigger value="revenue-review">Revenue Review Threshold</TabsTrigger>
                    <TabsTrigger value="unit-accounts">Unit Accounts Batch</TabsTrigger>
                    <TabsTrigger value="prior-month">Create Prior Month Statement</TabsTrigger>
                    <TabsTrigger value="job-group-management" data-qa-id="tab-job-group-management">
                        Job Group Management
                    </TabsTrigger>
                </TabsList>

                <TabsContent value="revenue-review">
                    <RevenueReviewThreshold />
                </TabsContent>

                <TabsContent value="unit-accounts">
                    <UnitAccounts />
                </TabsContent>

                <TabsContent value="prior-month">
                    <PriorMonth/>
                </TabsContent>
                
                <TabsContent value="job-group-management">
                    <JobGroupManagement />
                </TabsContent>
            </Tabs>
        </div>
    );
}
