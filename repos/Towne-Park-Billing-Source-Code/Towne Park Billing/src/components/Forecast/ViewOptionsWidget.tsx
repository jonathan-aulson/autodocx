import { routes } from "@/authConfig";
import { useCustomer } from "@/contexts/CustomerContext";
import { TimeRangeType } from "@/lib/models/Statistics";
import { BarChart3, ExternalLink } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { Button } from "../ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "../ui/card";
import { Label } from "../ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "../ui/select";

interface ViewOptionsWidgetProps {
    startingMonth: string;
    setStartingMonth: (month: string) => void;
    timePeriod: TimeRangeType;
    setTimePeriod: (period: TimeRangeType) => void;
    activeTab: string;
    isSidebarDisabled?: boolean;
}

const ViewOptionsWidget: React.FC<ViewOptionsWidgetProps> = ({
    startingMonth,
    setStartingMonth,
    timePeriod,
    setTimePeriod,
    activeTab,
    isSidebarDisabled
}) => {

    const { selectedCustomer } = useCustomer();
    const navigate = useNavigate();
    
    const timePeriodOptions: { value: TimeRangeType; label: string }[] = [
        { value: TimeRangeType.DAILY, label: "Daily View" },
        { value: TimeRangeType.WEEKLY, label: "Weekly View" },
        { value: TimeRangeType.MONTHLY, label: "Monthly View" }
    ];

    const currentYear = new Date().getFullYear();
    const yearOptions = [currentYear - 1, currentYear, currentYear + 1];

    const [selectedYear, selectedMonth] = startingMonth.split("-");

    const handlePnlViewClick = () => {
        if (selectedCustomer) {
            navigate(routes.pnlViewWithIdFunction(selectedCustomer.siteNumber));
        } else {
            navigate(routes.pnlView);
        }
    };

    return (
        <Card>
            <CardHeader>
                <CardTitle>View Options</CardTitle>
            </CardHeader>
            <CardContent>
                <div className="space-y-1">
                    <div>
                        <Label htmlFor="starting-year">Starting Year</Label>
                        <Select
                            value={selectedYear}
                            onValueChange={(year) => setStartingMonth(`${year}-${selectedMonth}`)}
                            disabled={isSidebarDisabled}
                            data-qa-id="select-starting-year"
                        >
                            <SelectTrigger id="starting-year" className="w-full mt-2" data-qa-id="trigger-starting-year">
                                <SelectValue placeholder="Select a year" />
                            </SelectTrigger>
                            <SelectContent>
                                {yearOptions.map((year) => (
                                    <SelectItem key={year} value={year.toString()} data-qa-id={`select-item-year-${year}`}>
                                        {year}
                                    </SelectItem>
                                ))}
                            </SelectContent>
                        </Select>
                    </div>
                    {(activeTab === "statistics" || activeTab === "payroll" || activeTab === "otherRevenue" || activeTab === "otherExpenses") && (
                        <div>
                            <Label htmlFor="starting-month">Starting Month</Label>
                            <Select
                                value={selectedMonth}
                                onValueChange={(month) => setStartingMonth(`${selectedYear}-${month}`)}
                                disabled={isSidebarDisabled}
                                data-qa-id="select-starting-month"
                            >
                                <SelectTrigger id="starting-month" className="w-full mt-2" data-qa-id="trigger-starting-month">
                                    <SelectValue placeholder="Select a month" />
                                </SelectTrigger>
                                <SelectContent>
                                    {Array.from({ length: 12 }, (_, i) => {
                                        const month = new Date(0, i).toLocaleString('en-US', { month: 'long' });
                                        const monthValue = String(i + 1).padStart(2, '0');
                                        return (
                                            <SelectItem key={monthValue} value={monthValue} data-qa-id={`select-item-month-${monthValue}`}>
                                                {month}
                                            </SelectItem>
                                        );
                                    })}
                                </SelectContent>
                            </Select>
                        </div>
                    )}
                    <div>
                        <Label htmlFor="time-period">Time Period</Label>
                        <Select
                            value={timePeriod}
                            onValueChange={(val) => setTimePeriod(val as TimeRangeType)}
                            disabled={activeTab === "otherRevenue" || isSidebarDisabled}
                            data-qa-id="select-time-period"
                        >
                            <SelectTrigger id="time-period" className="w-full mt-2" data-qa-id="trigger-time-period">
                                <SelectValue placeholder="Select a time period" />
                            </SelectTrigger>
                            <SelectContent>
                                {timePeriodOptions.map((option) => (
                                    <SelectItem key={option.value} value={option.value} data-qa-id={`select-item-period-${option.value.toLowerCase()}`}>
                                        {option.label}
                                    </SelectItem>
                                ))}
                            </SelectContent>
                        </Select>
                        {activeTab === "otherRevenue" && (
                            <p className="text-xs text-muted-foreground mt-1">
                                Time period locked to Monthly View for Other Revenue
                            </p>
                        )}
                    </div>

                    <div className="pt-4">
                        <div className="space-y-2">
                            <Button 
                            variant="outline" 
                            className="w-full justify-start" size="sm"
                            onClick={handlePnlViewClick}
                            data-qa-id="button-pnl-view">
                                <BarChart3 className="mr-2 h-4 w-4" />
                                P&L View
                                <ExternalLink className="ml-auto h-3 w-3" />
                            </Button>
                        </div>
                    </div>

                </div>
            </CardContent>
        </Card>
    );
};

export default ViewOptionsWidget;
