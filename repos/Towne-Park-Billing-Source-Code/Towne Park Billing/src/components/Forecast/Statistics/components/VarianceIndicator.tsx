import React from "react";
import { cn } from "@/lib/utils";

interface VarianceIndicatorProps {
  actualValue: number;
  forecastValue: number;
  className?: string;
  showPercentage?: boolean;
  isExpense?: boolean; // For expenses, lower actual is favorable (green), higher actual is unfavorable (red)
}

export function VarianceIndicator({ 
  actualValue, 
  forecastValue, 
  className,
  showPercentage = false,
  isExpense = false
}: VarianceIndicatorProps) {
  // Don't show indicator if we don't have both values
  if (actualValue === undefined || actualValue === null || 
      forecastValue === undefined || forecastValue === null) {
    return null;
  }

  const variance = actualValue - forecastValue;
  
  const baseClasses = "inline-flex items-center text-xs font-medium ml-1";
  
  // Show black solid bullet for exact match
  if (variance === 0) {
    return (
      <span 
        className={cn(baseClasses, "text-black dark:text-white", className)}
        data-qa-id="variance-indicator-equal"
      >
        <span className="text-xs">•</span>
      </span>
    );
  }
  
  // Calculate percentage variance, handling division by zero
  const variancePercentage = forecastValue !== 0 
    ? (variance / forecastValue) * 100 
    : 100; // When forecast is 0 and actual > 0, show as 100% variance

  const isPositive = variance > 0;
  const arrowIcon = isPositive ? "▲" : "▼";
  
  // For expenses: higher actual (positive variance) = unfavorable (red), lower actual (negative variance) = favorable (green)
  // For revenue: higher actual (positive variance) = favorable (green), lower actual (negative variance) = unfavorable (red)
  const isFavorable = isExpense ? variance < 0 : variance > 0;
  const colorClasses = isFavorable 
    ? "text-green-600 dark:text-green-400" 
    : "text-red-600 dark:text-red-400";

  return (
    <span 
      className={cn(baseClasses, colorClasses, className)}
      data-qa-id="variance-indicator"
    >
      <span className="text-xs">{arrowIcon}</span>
      {showPercentage && (
        <span className="ml-0.5">
          {variancePercentage > 0 ? '+' : ''}{Math.abs(variancePercentage).toFixed(1)}%
        </span>
      )}
    </span>
  );
}

export default VarianceIndicator;