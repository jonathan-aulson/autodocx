import { type ClassValue, clsx } from "clsx";
import { twMerge } from "tailwind-merge";

/**
 * The cn function is designed to make it easier to conditionally add Tailwind CSS classes to the components. 
 * It typically combines the use of the clsx library, which allows for conditional class name manipulation, 
 * and the tailwind-merge library, which resolves conflicts between class names to ensure that the styling 
 * remains consistent and maintainable - https://ui.shadcn.com/docs/installation/manual
 */
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

/**
 * Formats a number as USD currency
 * @param amount - The number to format as currency
 * @returns The formatted currency string
 */
export function formatCurrency(amount: number) {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
  }).format(amount)
}

/**
 * Formats a number as USD currency with no decimal places (whole dollars only)
 * @param amount - The number to format as currency
 * @returns The formatted currency string with no decimal places
 */
export function formatCurrencyWhole(amount: number) {
  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "USD",
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(Math.round(amount))
}

/**
 * Validates and sanitizes input to contain only alphanumeric characters
 * @param value - The input string to validate
 * @param maxLength - Maximum allowed length (default: no limit)
 * @param allowSpaces - Whether to allow spaces in the input (default: false)
 * @returns The sanitized string
 */
export function validateAlphanumeric(value: string, maxLength?: number, allowSpaces: boolean = false): string {
  const pattern = allowSpaces ? /[^a-zA-Z0-9\s]/g : /[^a-zA-Z0-9]/g;
  const sanitized = value.replace(pattern, '');
  return maxLength ? sanitized.slice(0, maxLength) : sanitized;
}

/**
 * Splits an address string at "Suite 300" and formats it into two lines.
 * 
 * @param address - The address string to format
 * @returns The formatted address string
 */
export function formatAddress(address: string): string {
  if (address.includes("Suite 300")) {
    const suiteIndex = address.indexOf("Suite 300") + "Suite 300".length;
    return `${address.substring(0, suiteIndex).trim()}\n${address.substring(suiteIndex).trim()}`;
  }
  return address;
}

/**
 * Splits a string at the first comma and formats it into two lines.
 * The comma is retained in the first part of the split.
 * 
 * @param input - The string to split
 * @returns A single string with \n for line breaks
 */
export function splitAddress(input: string, isNewLineNeeded:boolean): string {
  if (input.includes(",")) {
    const commaIndex = input.indexOf(",") + 1;
    return isNewLineNeeded?`${input.substring(0, commaIndex).trim()}\n${input.substring(commaIndex).trim()}`:`${input.substring(0, commaIndex).trim()} ${input.substring(commaIndex).trim()}`;
  }
  return input;
}

/**
 * Returns the current month index (0-11) for a given year.
 * If the year is not the current year, returns 11 (December).
 */
export function getCurrentMonthIdx(year: number): number {
  const now = new Date();
  return now.getFullYear() === year ? now.getMonth() : 11;
}