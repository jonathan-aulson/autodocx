// Main component
export { default as Statistics } from "./Statistics";

// Components
export { StatisticsTableComponent as StatisticsTable } from "./components/StatisticsTable";
export { StatisticsControls } from "./components/StatisticsControls";
export { StatisticsGuide } from "./components/StatisticsGuide";
export { MonthPagination } from "./components/MonthPagination";

// Hooks
export { useStatisticsData, useStatisticsForm } from "./hooks/useStatistics";
export { usePeriodEntries, PeriodEntriesGenerator } from "./hooks/usePeriodEntries";
export type { PeriodEntry } from "./hooks/usePeriodEntries";

// Services
export { StatisticsDataManager } from "./services/StatisticsDataManager";
export { StatisticsDataProcessor } from "./services/StatisticsDataProcessor";
export type { ProcessedMonthData } from "./services/StatisticsDataProcessor";

// Library utilities
export { StatisticsCalculations } from "./lib/calculations";
export * from "./lib/helpers";
export * from "./lib/constants"; 