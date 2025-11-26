export interface OtherRevenueDetailDto {
    id: string | null;
    monthYear?: string;
    billableExpense: number;
    revenueValidation: number;
    miscellaneous: number;
    credits: number;
    clientPaidExpense: number;
    gpoFees: number;
    signingBonus: number;
}

export interface OtherRevenueDto {
    id: string;
    customerSiteId: string;
    siteNumber?: string;
    name?: string;
    billingPeriod?: string;
    forecastData?: OtherRevenueDetailDto[];
}
