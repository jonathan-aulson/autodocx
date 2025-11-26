import React from "react";
import { Button } from "@/components/ui/button";
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { TimeRangeType } from "@/lib/models/Statistics";

interface MonthPaginationProps {
    currentMonthIndex: number;
    setCurrentMonthIndex: (index: number) => void;
    startingMonth: string;
    hasUnsavedChanges: boolean;
    saveChanges: () => Promise<void>;
    timePeriod?: TimeRangeType;
}

export const MonthPagination: React.FC<MonthPaginationProps> = ({
    currentMonthIndex,
    setCurrentMonthIndex,
    startingMonth,
    hasUnsavedChanges,
    saveChanges,
    timePeriod = "DAILY"
}) => {
    const [year, month] = startingMonth.split('-').map(Number);
    
    // Calculate the current month being displayed
    const currentDate = new Date(year, month - 1 + currentMonthIndex, 1);
    const currentMonthName = currentDate.toLocaleString('en-US', { month: 'long' });
    const currentYear = currentDate.getFullYear();
    
    const handlePreviousMonth = () => {
        if (currentMonthIndex > 0) {
            setCurrentMonthIndex(currentMonthIndex - 1);
        }
    };
    
    const handleNextMonth = () => {
        if (currentMonthIndex < 2) {
            setCurrentMonthIndex(currentMonthIndex + 1);
        }
    };
    
    // Calculate total days and current range for display
    const getDaysInMonthRange = () => {
        let totalDays = 0;
        let currentStart = 1;
        
        for (let i = 0; i < 3; i++) {
            const monthDate = new Date(year, month - 1 + i, 1);
            const daysInThisMonth = new Date(monthDate.getFullYear(), monthDate.getMonth() + 1, 0).getDate();
            
            if (i === currentMonthIndex) {
                currentStart = totalDays + 1;
            }
            
            totalDays += daysInThisMonth;
        }
        
        const currentMonthDays = new Date(currentDate.getFullYear(), currentDate.getMonth() + 1, 0).getDate();
        const currentEnd = currentStart + currentMonthDays - 1;
        
        return { currentStart, currentEnd, totalDays };
    };
    
    const { currentStart, currentEnd, totalDays } = getDaysInMonthRange();
    
    return (
        <div className="flex items-start justify-between w-full">
            <Button 
                variant="outline" 
                size="sm" 
                onClick={handlePreviousMonth}
                disabled={currentMonthIndex === 0}
                data-qa-id="button-previous-month"
            >
                <ChevronLeft className="h-4 w-4 mr-2" />
                Previous
            </Button>
            
            <div className="flex flex-col items-end">
                <Button 
                    variant="outline" 
                    size="sm" 
                    onClick={handleNextMonth}
                    disabled={currentMonthIndex === 2}
                    data-qa-id="button-next-month"
                >
                    Next
                    <ChevronRight className="h-4 w-4 ml-2" />
                </Button>
                <div className="text-xs text-muted-foreground mt-1">
                    Page {currentMonthIndex + 1} of 3
                </div>
            </div>
        </div>
    );
}; 