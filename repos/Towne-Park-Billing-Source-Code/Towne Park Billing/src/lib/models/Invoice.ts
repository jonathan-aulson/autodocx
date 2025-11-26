import { z } from "zod";
import { DataEntity } from "./DataEntity";

export interface MetaData {
  lineItemId?: string;
  lineItemType?: string;
  isAdhoc?: boolean;
  isClaims?: boolean;
  isInsurance?: boolean;
  isManagementFee?: boolean;
  isBillablePayrollAccounts?: boolean;
  isPTEB?: boolean;
  isSupportServices?: boolean;
  isBillableExpenseAccounts?: boolean;
  isClientPaidExpense?: boolean;
  isProfitDeduction?: boolean;
  isNonBillableExpense?: boolean;
  isProfitShare?: boolean;
  monthlyProfit?: number;
  invoiceGroup: number;
  isDepositedRevenue?: boolean;
  isPaidParkingTax?: boolean;
  taxesPaid?: number;
}

export interface InvoiceLineItem {
  title: string;
  code: string;
  description: string;
  amount: number;
  metaData?: MetaData;
}

export interface UpdateLineItems extends InvoiceLineItem {
  code: string;
}

export enum AdHocLineItemType {
  MiscellaneousItem = "MiscellaneousItem",
  ClientPaidExpense = "ClientPaidExpense",
  ReimbursableExpense = "ReimbursableExpense",
  NonBillableExpense = "NonBillableExpense",
}

export interface Invoice extends DataEntity {
  invoiceNumber: string;
  amount: number;
  invoiceDate: string; // is this created date? Send?
  paymentTerms: string;
  title: string;
  description: string;
  lineItems?: InvoiceLineItem[];
  purchaseOrder?: string;
}

export interface GLCode {
  code: string;
  name: string;
  type: string;
}

// INVOICE HEADER AND FOOTER INTERFACES

export enum InvoiceConfigGroup {
  InvoiceHeaderFooter = "InvoiceHeaderFooter"
}

export enum InvoiceConfigKey {
  TowneParksAddress = "TowneParksAddress",
  TowneParksPhone = "TowneParksPhone",
  TowneParksLegalName = "TowneParksLegalName",
  TowneParksPOBox = "TowneParksPOBox",
  TowneParksAccountNumber = "TowneParksAccountNumber",
  TowneParksABA = "TowneParksABA",
  TowneParksEmail = "TowneParksEmail",
  UPPGlobalLegalName = "UPPGlobalLegalName",
}

export interface InvoiceConfigDto {
  key: InvoiceConfigKey;
  value: string;
}

// Define Zod schema
export const adHocLineItemSchema = z.object({
  type: z.nativeEnum(AdHocLineItemType, {
    required_error: "Type is required.",
    message: "Please select type.",
  }),
  title: z.string().min(1, { message: "Title is required." }),
  description: z.string().optional(),
  amount: z
    .string()
    .min(1, { message: "Amount is required." })
    .refine(val => !isNaN(Number(val)), { message: "Amount should be a valid number." })
    .transform(val => Number(val)),
}).superRefine((data, ctx) => {
  const { type, amount } = data;

  if (type === AdHocLineItemType.MiscellaneousItem && amount === 0) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      message: "Miscellaneous items should have a negative or positive amount.",
      path: ["amount"],
    });
  }

  if (type !== AdHocLineItemType.MiscellaneousItem && amount <= 0) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      message: "Amount must be higher than zero.",
      path: ["amount"],
    });
  }
});
