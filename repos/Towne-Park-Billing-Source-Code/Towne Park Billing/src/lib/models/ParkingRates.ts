export interface ParkingRateData {
    parkingRateId: string;
    name: string;
    customerSiteId: string;
    siteNumber: string;
    year: number;
    forecastRates: ParkingRateDetailData[];
    actualRates: ParkingRateDetailData[];
    budgetRates: ParkingRateDetailData[];
}

export interface ParkingRateDetailData {
    parkingRateDetailId: string;
    rate: number;
    month: number;
    rateCategory: string;
    isIncrease: boolean;
    increaseAmount: number;
}

export enum ParkingRateType {
    ValetOvernight = "ValetOvernight",
    SelfOvernight = "SelfOvernight",
    ValetDaily = "ValetDaily",
    SelfDaily = "SelfDaily",
    ValetMonthly = "ValetMonthly",
    SelfMonthly = "SelfMonthly",
    ValetAggregator = "ValetAggregator",
    SelfAggregator = "SelfAggregator"
}

export interface ParkingRateTypeMapping {
    [key: string]: ParkingRateType;
}

export const PARKING_RATE_TYPE_MAPPING: ParkingRateTypeMapping = {
    "Valet Overnight": ParkingRateType.ValetOvernight,
    "Self Overnight": ParkingRateType.SelfOvernight,
    "Valet Daily": ParkingRateType.ValetDaily,
    "Self Daily": ParkingRateType.SelfDaily,
    "Valet Monthly": ParkingRateType.ValetMonthly,
    "Self Monthly": ParkingRateType.SelfMonthly,
    "Valet Aggregator": ParkingRateType.ValetAggregator,
    "Self Aggregator": ParkingRateType.SelfAggregator
};

export const PARKING_RATE_TYPE_NAMES = Object.keys(PARKING_RATE_TYPE_MAPPING);