import { z } from "zod";

export interface CustomerDetail {
    siteName: string;
    address: string;
    glString: string;
    district: string;
    accountManager: string;
    accountManagerId: string;
    siteNumber: string;
    invoiceRecipient: string;
    billingContactEmail: string;
    startDate: string | null;
    closeDate: string | null;
    totalRoomsAvailable: string;
    totalAvailableParking: string;
    districtManager: string;
    assistantDistrictManager: string;
    assistantAccountManager: string;
    vendorId: string;
    legalEntity: string;
    plCategory: string;
    cogSegment: string;
    svpRegion: string;
    businessSegment: string;
}

export interface CustomerSummary {
    customerSiteId: string;
    siteNumber: string;
    siteName: string;
    district: string | null;
    billingType: string | null;
    contractType: string | null;
    deposits: boolean;
    readyForInvoiceStatus: string | null;
    period: string | null;
    isStatementGenerated: boolean;
    accountManager: string | null;
    districtManager: string | null;
    legalEntity: string | null;
    plCategory: string | null;
    svpRegion: string | null;
    cogSegment: string | null;
    businessSegment: string | null;
}

const multipleEmails = (value: string) => {
    const emails = value.split(';').map(email => email.trim());
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emails.every(email => emailRegex.test(email));
};

export const GeneralInfoSchema = z.object({
    siteName: z.string().min(1, "Site Name is required"),
    address: z.string().min(1, "Address is required"),
    glString: z.string().min(1, "GL String is required"),
    district: z.string().optional(),
    invoiceRecipient: z.string().min(1, "Invoice Recipient is required"),
    accountManager: z.string().optional(),
    accountManagerId: z.string().optional(),
    billingContactEmail: z
        .string()
        .min(1, "Billing Contact Email is required")
        .refine(multipleEmails, {
            message: "Please enter valid email addresses separated by a semicolon",
        }),
    startDate: z.date(),
    closeDate: z.date().nullable().optional(),
    totalRoomsAvailable: z.string().optional().nullable(),
    totalAvailableParking: z.string().optional().nullable(),
    districtManager: z.string().optional(),
    assistantDistrictManager: z.string().optional(),
    assistantAccountManager: z.string().optional(),
    vendorId: z.string().optional(),
    legalEntity: z.string().optional(),
    plCategory: z.string().optional(),
    svpRegion: z.string().optional(),
    cogSegment: z.string().optional(),
    businessSegment: z.string().optional(),
}).refine((data) => data.closeDate === undefined || data.closeDate === null || data.startDate < data.closeDate, {
    message: "Close Date must be greater than Start Date",
    path: ["closeDate"],
});
