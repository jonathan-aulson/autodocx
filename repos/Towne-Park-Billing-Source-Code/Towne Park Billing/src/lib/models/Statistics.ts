export enum TimeRangeType {
    DAILY = "DAILY",
    WEEKLY = "WEEKLY",
    MONTHLY = "MONTHLY",
    QUARTERLY = "QUARTERLY"
}

export interface SiteStatisticData {
    siteStatisticId: string | null;
    siteNumber: string | null;
    customerSiteId: string;
    name: string | null;
    totalRooms: number;
    timeRangeType: TimeRangeType;
    periodLabel?: string | null;
    budgetData: SiteStatisticDetailData[];
    forecastData: SiteStatisticDetailData[];
    actualData: SiteStatisticDetailData[];
}

export interface SiteStatisticDetailData {
    siteStatisticDetailId?: string | null;
    type?: string | null;
    periodStart: string;
    periodEnd: string;
    periodLabel?: string | null;
    externalRevenue: number;
    externalRevenueLastDate?: string | null;
    valetRateDaily: number;
    valetRateMonthly: number;
    valetRateOvernight: number;
    selfRateDaily: number;
    selfRateMonthly: number;
    selfRateOvernight: number;
    baseRevenue: number;
    occupiedRooms: number | null;
    occupancy: number | null;
    selfOvernight: number;
    valetOvernight: number;
    valetDaily: number;
    valetMonthly: number;
    selfDaily: number;
    selfMonthly: number;
    valetComps: number;
    selfComps: number;
    selfAggregator: number;
    valetAggregator: number;
    driveInRatio: number;
    captureRatio: number;
    adjustmentPercentage?: number;
    adjustmentValue?: number;
}

export interface StatisticDefinition {
  id: string;
  name: string;
  type: "number" | "percent" | "string";
  step: string;
  placeholder: string;
}

// Interface for customer data from API
export interface Customer {
    customerSiteId: string;
    siteName: string;
    siteNumber: string;
}

// Interface for form values by date
export interface FormValuesByDate {
    [dateKey: string]: {
        [statId: string]: number;
    };
}

export interface DetailIds {
    [dateKey: string]: string;
}

export const ALL_STATISTICS: StatisticDefinition[] = [
    { id: "type", name: "Type", type: "string", step: "1", placeholder: "" },
    { id: "date", name: "Date", type: "string", step: "1", placeholder: "" },
    { id: "external-revenue", name: "External Revenue", type: "number", step: "0.01", placeholder: "0.00" },
    { id: "valet-rate-daily", name: "Valet Rate Daily", type: "number", step: "0.01", placeholder: "0.00" },
    { id: "valet-rate-monthly", name: "Valet Rate Monthly", type: "number", step: "0.01", placeholder: "0.00" },
    { id: "valet-rate-overnight", name: "Valet Rate Overnight", type: "number", step: "0.01", placeholder: "0.00" },
    { id: "self-rate-daily", name: "Self Rate Daily", type: "number", step: "0.01", placeholder: "0.00" },
    { id: "self-rate-monthly", name: "Self Rate Monthly", type: "number", step: "0.01", placeholder: "0.00" },
    { id: "self-rate-overnight", name: "Self Rate Overnight", type: "number", step: "0.01", placeholder: "0.00" },
    { id: "base-revenue", name: "Base Revenue", type: "number", step: "0.01", placeholder: "0.00" },
    { id: "occupied-rooms", name: "Occupied Rooms", type: "number", step: "1", placeholder: "0" },
    { id: "occupancy", name: "Occupancy %", type: "percent", step: "0.01", placeholder: "0.00" },
    { id: "drive-in-ratio", name: "Overnight Self", type: "percent", step: "0.01", placeholder: "0.00" },
    {
        id: "overnight-valet-capture",
        name: "Overnight Valet",
        type: "percent",
        step: "0.01",
        placeholder: "0.00",
    },
    { id: "valet-daily", name: "Valet Daily", type: "number", step: "1", placeholder: "0" },
    { id: "valet-monthly", name: "Valet Monthly", type: "number", step: "1", placeholder: "0" },
    { id: "valet-overnight", name: "Valet Overnight", type: "number", step: "1", placeholder: "0" },
    { id: "valet-comps", name: "Valet Comps", type: "number", step: "1", placeholder: "0" },
    { id: "valet-aggregator", name: "Valet Aggregator", type: "percent", step: "0.01", placeholder: "0.00" },
    { id: "self-daily", name: "Self Daily", type: "number", step: "1", placeholder: "0" },
    { id: "self-monthly", name: "Self Monthly", type: "number", step: "1", placeholder: "0" },
    { id: "self-overnight", name: "Self Overnight", type: "number", step: "1", placeholder: "0" },
    { id: "self-comps", name: "Self Comps", type: "number", step: "1", placeholder: "0" },
    { id: "self-aggregator", name: "Self Aggregator", type: "percent", step: "0.01", placeholder: "0.00" },
    // Add ratio input fields
    { id: "drive-in-ratio-input", name: "Drive In Ratio", type: "percent", step: "0.01", placeholder: "0.00" },
    { id: "capture-ratio-input", name: "Capture Ratio", type: "percent", step: "0.01", placeholder: "0.00" },
];
