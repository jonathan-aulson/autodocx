import { Button } from "@/components/ui/button";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { useToast } from "@/components/ui/use-toast";
import { format } from "date-fns";
import { useState } from "react";
import { PulseLoader } from "react-spinners";

export default function UnitAccounts() {
    const [date, setDate] = useState<Date>(new Date());
    const [isProcessing, setIsProcessing] = useState(false);
    const { toast } = useToast();

    const currentYear = new Date().getFullYear();
    
    const generateYearOptions = () => {
        const startYear = 2024;
        const numYears = currentYear - startYear + 1;
        return Array.from({ length: numYears }, (_, i) => startYear + i);
    };
    
    const years = generateYearOptions();
    
    const months = [
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December"
    ];

    const handleMonthYearSelect = (month: number, year: number) => {
        const newDate = new Date(year, month, 1);
        setDate(newDate);
    };

    const processUnitAccounts = async () => {
        if (!date) return;
        
        const selectedPeriod = format(date, 'yyyy-MM');
        
        setIsProcessing(true);
        try {
            const response = await fetch(`/api/unit-account/${selectedPeriod}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                }
            });
            
            if (!response.ok) {
                throw new Error('Failed to process unit accounts');
            }
            
            toast({
                title: "Success",
                description: `Unit accounts for ${format(date, 'MMMM yyyy')} have been successfully processed.`,
            });
        } catch (error) {
            console.error('Error processing unit accounts:', error);
            toast({
                title: "Error",
                description: "Failed to process unit accounts. Please try again.",
                variant: "destructive",
            });
        } finally {
            setIsProcessing(false);
        }
    };

    return (
        <div>
            <h1 className="text-2xl font-bold mb-6 py-8">Unit Accounts</h1>
            
            <div className="mb-8">
                <p className="text-sm text-muted-foreground mb-6">
                    Manually trigger a 'Unit Accounts' (statistics) batch to send to GP once the close process is complete
                </p>
                
                <div className="space-y-6">
                    <div className="space-y-2">
                        <h3 className="text-lg font-medium">Select Billing Cycle</h3>
                        <p className="text-sm text-muted-foreground">
                            Choose the month and year for which you want to process unit accounts.
                        </p>
                    </div>
                    
                    <div className="flex flex-col space-y-4 sm:flex-row sm:space-x-4 sm:space-y-0">
                        <div className="flex flex-col sm:flex-row gap-2 sm:gap-4">
                            <Select 
                                value={date.getMonth().toString()} 
                                onValueChange={(value) => handleMonthYearSelect(parseInt(value), date.getFullYear())}
                                data-qa-id="unit-accounts-month-select"
                            >
                                <SelectTrigger className="w-[180px]" data-qa-id="unit-accounts-month-trigger">
                                    <SelectValue placeholder="Month" />
                                </SelectTrigger>
                                <SelectContent>
                                    {months.map((month, index) => (
                                        <SelectItem key={month} value={index.toString()} data-qa-id={`unit-accounts-month-option-${month.toLowerCase()}`}>
                                            {month}
                                        </SelectItem>
                                    ))}
                                </SelectContent>
                            </Select>

                            <Select 
                                value={date.getFullYear().toString()} 
                                onValueChange={(value) => handleMonthYearSelect(date.getMonth(), parseInt(value))}
                                data-qa-id="unit-accounts-year-select"
                            >
                                <SelectTrigger className="w-[120px]" data-qa-id="unit-accounts-year-trigger">
                                    <SelectValue placeholder="Year" />
                                </SelectTrigger>
                                <SelectContent>
                                    {years.map((year) => (
                                        <SelectItem key={year} value={year.toString()} data-qa-id={`unit-accounts-year-option-${year}`}>
                                            {year}
                                        </SelectItem>
                                    ))}
                                </SelectContent>
                            </Select>
                        </div>
                        
                        <Button 
                            onClick={processUnitAccounts} 
                            disabled={isProcessing || !date}
                            className="sm:w-auto"
                            data-qa-id="process-unit-accounts-button"
                        >
                            {isProcessing ? (
                                <PulseLoader color="#ffffff" size={8} />
                            ) : (
                                'Process Unit Accounts'
                            )}
                        </Button>
                    </div>
                    
                    <div className="mt-4 text-sm text-muted-foreground">
                        <p>
                            This will generate and send unit accounts statistics to the accounting system for {format(date, 'MMMM yyyy')}.
                            The process may take a few minutes to complete.
                        </p>
                    </div>
                </div>
            </div>
        </div>
    );
}
