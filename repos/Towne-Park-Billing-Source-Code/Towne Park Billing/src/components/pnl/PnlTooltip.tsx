import React from 'react';
import { formatCurrency, formatCurrencyWhole } from '@/lib/utils';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';

/*
 * P&L GLOBAL TOOLTIP SYSTEM
 * 
 * This component provides a consistent tooltip system for all P&L rows.
 * 
 * FLEXIBLE DATA STRUCTURE:
 * The tooltip system accepts a flexible data structure:
 * {
 *   actualAmount: number,           // From existing API data
 *   forecastAmount?: number,        // Special calculation (optional for claims)
 *   actualUpToDate?: Date,         // Passed as parameter (optional for claims)
 *   forecastFromDate?: Date,       // Passed as parameter (optional for claims)
 *   rowCode: string
 * }
 * 
 * USAGE:
 * 1. Import the components you need from this file
 * 2. Use PnlTooltip as the wrapper
 * 3. Use helper components for consistent styling
 * 
 * HELPER COMPONENTS:
 * - TooltipAmountSection: Single amount display
 * - TooltipComparisonSection: Side-by-side amount comparison  
 * - TooltipDateComparisonSection: Side-by-side date comparison
 * - TooltipDivider: Visual separator
 * 
 * EASY SETUP:
 * 1. Add row code to tooltipEnabledRows array in PnlView.tsx
 * 2. Implement getTooltipDataForRow() for your specific row
 * 3. Return the appropriate data structure
 * 
 * See PnlView.tsx for implementation examples.
 */

interface PnlTooltipProps {
    title: string;
    children: React.ReactNode;
    className?: string;
}

export const PnlTooltip: React.FC<PnlTooltipProps> = ({
    title,
    children,
    className = "min-w-max"
}) => {
    return (
        <Card className={`${className} shadow-lg border-0 bg-white/95 dark:bg-gray-900/95 backdrop-blur-sm`}>
            <CardContent className="p-3 space-y-3">
                {/* Header */}
                <div className="flex items-center justify-center">
                    <h4 className="text-sm font-semibold text-gray-900 dark:text-gray-100">
                        {title}
                    </h4>
                </div>

                {/* Content */}
                {children}
            </CardContent>
        </Card>
    );
};

// Helper components for common tooltip sections
export const TooltipAmountSection: React.FC<{
    label: string;
    amount: number;
    className?: string;
}> = ({ label, amount, className = "text-blue-600 dark:text-blue-400" }) => (
    <div className="text-center">
        <div className="text-xs text-gray-600 dark:text-gray-400 font-medium mb-1">
            {label}
        </div>
        <div className={`text-base font-bold ${className}`}>
            {formatCurrencyWhole(amount)}
        </div>
    </div>
);

export const TooltipComparisonSection: React.FC<{
    leftLabel: string;
    leftAmount: number;
    leftClassName?: string;
    rightLabel: string;
    rightAmount: number;
    rightClassName?: string;
}> = ({ 
    leftLabel, 
    leftAmount, 
    leftClassName = "text-blue-600 dark:text-blue-400",
    rightLabel, 
    rightAmount, 
    rightClassName = "text-gray-700 dark:text-gray-300"
}) => (
    <div className="flex justify-between items-start">
        <TooltipAmountSection 
            label={leftLabel} 
            amount={leftAmount} 
            className={leftClassName}
        />
        <TooltipAmountSection 
            label={rightLabel} 
            amount={rightAmount} 
            className={rightClassName}
        />
    </div>
);

export const TooltipDateSection: React.FC<{
    items: Array<{
        label: string;
        value: string;
        className?: string;
    }>;
}> = ({ items }) => (
    <div className="space-y-1">
        {items.map((item, index) => (
            <div key={index} className="flex items-center justify-between">
                <span className="text-xs text-gray-600 dark:text-gray-400 font-medium">
                    {item.label}
                </span>
                <span className={`text-xs ${item.className || "text-gray-700 dark:text-gray-300"}`}>
                    {item.value}
                </span>
            </div>
        ))}
    </div>
);

export const TooltipDateComparisonSection: React.FC<{
    leftLabel: string;
    leftValue: string;
    leftClassName?: string;
    rightLabel: string;
    rightValue: string;
    rightClassName?: string;
}> = ({
    leftLabel,
    leftValue,
    leftClassName = "text-blue-600 dark:text-blue-400",
    rightLabel,
    rightValue,
    rightClassName = "text-gray-700 dark:text-gray-300"
}) => (
    <div className="flex justify-between items-start">
        {/* Left Date */}
        <div className="text-center flex-1">
            <div className="text-xs text-gray-600 dark:text-gray-400 font-medium mb-1">
                {leftLabel}
            </div>
            <div className={`text-xs ${leftClassName}`}>
                {leftValue}
            </div>
        </div>
        
        {/* Right Date */}
        <div className="text-center flex-1">
            <div className="text-xs text-gray-600 dark:text-gray-400 font-medium mb-1">
                {rightLabel}
            </div>
            <div className={`text-xs ${rightClassName}`}>
                {rightValue}
            </div>
        </div>
    </div>
);

export const TooltipDivider: React.FC = () => (
    <div className="border-t border-gray-200 dark:border-gray-700" />
);
