/**
 * Interface representing a month-year pair
 */
export interface MonthYear {
  /** Month as a zero-padded string (01-12) */
  month: string;
  /** Year as a 4-digit number */
  year: number;
}

/**
 * Interface for date period calculations
 */
export interface DatePeriod {
  /** Starting month-year */
  start: MonthYear;
  /** Ending month-year */
  end: MonthYear;
}

/**
 * Gets the current month and year
 * @returns {MonthYear} Object containing current month (zero-padded) and year
 * @example
 * const current = getCurrentMonthYear();
 * // Returns: { month: "07", year: 2025 }
 */
export function getCurrentMonthYear(): MonthYear {
  const now = new Date();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const year = now.getFullYear();

  return { month, year };
}

/**
 * Gets the previous month and year
 * @returns {MonthYear} Object containing previous month (zero-padded) and year
 * @example
 * const past = getPastMonthYear();
 * // If current is July 2025, returns: { month: "06", year: 2025 }
 * // If current is January 2025, returns: { month: "12", year: 2024 }
 */
export function getPastMonthYear(): MonthYear {
  const now = new Date();
  let month = now.getMonth(); // 0-based, so this is actually previous month
  let year = now.getFullYear();

  if (month === 0) {
    month = 12;
    year -= 1;
  }

  return {
    month: String(month).padStart(2, '0'),
    year
  };
}

/**
 * Gets the next month and year
 * @returns {MonthYear} Object containing next month (zero-padded) and year
 * @example
 * const future = getFutureMonthYear();
 * // If current is July 2025, returns: { month: "08", year: 2025 }
 * // If current is December 2025, returns: { month: "01", year: 2026 }
 */
export function getFutureMonthYear(): MonthYear {
  const now = new Date();
  let month = now.getMonth() + 2; // next month (0-based + 2)
  let year = now.getFullYear();

  if (month > 12) {
    month = 1;
    year += 1;
  }

  return {
    month: String(month).padStart(2, '0'),
    year
  };
}

/**
 * Gets a month-year combination offset by a specified number of months
 * @param {number} monthOffset - Number of months to offset (positive for future, negative for past)
 * @param {Date} [baseDate] - Base date to calculate from (defaults to current date)
 * @returns {MonthYear} Object containing the calculated month and year
 * @example
 * const threeMonthsAgo = getMonthYearOffset(-3);
 * const sixMonthsFromNow = getMonthYearOffset(6);
 */
export function getMonthYearOffset(monthOffset: number, baseDate?: Date): MonthYear {
  const date = baseDate ? new Date(baseDate) : new Date();

  // Add the offset to the current month
  date.setMonth(date.getMonth() + monthOffset);

  const month = String(date.getMonth() + 1).padStart(2, '0');
  const year = date.getFullYear();

  return { month, year };
}

/**
 * Creates a MonthYear object from separate month and year values
 * @param {number} month - Month (1-12)
 * @param {number} year - Year
 * @returns {MonthYear} Object containing formatted month and year
 * @throws {Error} If month is not between 1 and 12
 * @example
 * const monthYear = createMonthYear(7, 2025);
 * // Returns: { month: "07", year: 2025 }
 */
export function createMonthYear(month: number, year: number): MonthYear {
  if (month < 1 || month > 12) {
    throw new Error(`Invalid month: ${month}. Month must be between 1 and 12.`);
  }

  return {
    month: String(month).padStart(2, '0'),
    year
  };
}

/**
 * Converts a MonthYear object to a Date object (first day of the month)
 * @param {MonthYear} monthYear - MonthYear object to convert
 * @returns {Date} Date object set to the first day of the specified month/year
 * @example
 * const date = monthYearToDate({ month: "07", year: 2025 });
 * // Returns: Date object for July 1, 2025
 */
export function monthYearToDate(monthYear: MonthYear): Date {
  const monthNum = parseInt(monthYear.month, 10);
  return new Date(monthYear.year, monthNum - 1, 1);
}

/**
 * Converts a Date object to a MonthYear object
 * @param {Date} date - Date to convert
 * @returns {MonthYear} MonthYear object
 * @example
 * const monthYear = dateToMonthYear(new Date(2025, 6, 15));
 * // Returns: { month: "07", year: 2025 }
 */
export function dateToMonthYear(date: Date): MonthYear {
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const year = date.getFullYear();

  return { month, year };
}

/**
 * Compares two MonthYear objects
 * @param {MonthYear} a - First MonthYear object
 * @param {MonthYear} b - Second MonthYear object
 * @returns {number} -1 if a is before b, 0 if equal, 1 if a is after b
 * @example
 * const result = compareMonthYear(
 *   { month: "06", year: 2025 },
 *   { month: "07", year: 2025 }
 * );
 * // Returns: -1 (June 2025 is before July 2025)
 */
export function compareMonthYear(a: MonthYear, b: MonthYear): number {
  if (a.year !== b.year) {
    return a.year - b.year;
  }

  const monthA = parseInt(a.month, 10);
  const monthB = parseInt(b.month, 10);

  return monthA - monthB;
}

/**
 * Checks if two MonthYear objects represent the same month and year
 * @param {MonthYear} a - First MonthYear object
 * @param {MonthYear} b - Second MonthYear object
 * @returns {boolean} True if both represent the same month and year
 * @example
 * const isSame = isMonthYearEqual(
 *   { month: "07", year: 2025 },
 *   { month: "07", year: 2025 }
 * );
 * // Returns: true
 */
export function isMonthYearEqual(a: MonthYear, b: MonthYear): boolean {
  return a.month === b.month && a.year === b.year;
}

/**
 * Gets a range of MonthYear objects between two dates (inclusive)
 * @param {MonthYear} start - Starting month-year
 * @param {MonthYear} end - Ending month-year
 * @returns {MonthYear[]} Array of MonthYear objects in the range
 * @throws {Error} If start date is after end date
 * @example
 * const range = getMonthYearRange(
 *   { month: "01", year: 2025 },
 *   { month: "03", year: 2025 }
 * );
 * // Returns: [
 * //   { month: "01", year: 2025 },
 * //   { month: "02", year: 2025 },
 * //   { month: "03", year: 2025 }
 * // ]
 */
export function getMonthYearRange(start: MonthYear, end: MonthYear): MonthYear[] {
  if (compareMonthYear(start, end) > 0) {
    throw new Error('Start date cannot be after end date');
  }

  const range: MonthYear[] = [];
  let current = { ...start };

  while (compareMonthYear(current, end) <= 0) {
    range.push({ ...current });

    // Move to next month
    let month = parseInt(current.month, 10) + 1;
    let year = current.year;

    if (month > 12) {
      month = 1;
      year += 1;
    }

    current = {
      month: String(month).padStart(2, '0'),
      year
    };
  }

  return range;
}

/**
 * Formats a MonthYear object as a readable string
 * @param {MonthYear} monthYear - MonthYear object to format
 * @param {string} [format='MM/YYYY'] - Format string ('MM/YYYY', 'YYYY-MM', 'Month YYYY')
 * @returns {string} Formatted string
 * @example
 * const formatted = formatMonthYear({ month: "07", year: 2025 }, 'Month YYYY');
 * // Returns: "July 2025"
 */
export function formatMonthYear(
  monthYear: MonthYear,
  format: 'MM/YYYY' | 'YYYY-MM' | 'Month YYYY' = 'MM/YYYY'
): string {
  const { month, year } = monthYear;

  switch (format) {
    case 'MM/YYYY':
      return `${month}/${year}`;
    case 'YYYY-MM':
      return `${year}-${month}`;
    case 'Month YYYY':
      const monthNames = [
        'January', 'February', 'March', 'April', 'May', 'June',
        'July', 'August', 'September', 'October', 'November', 'December'
      ];
      const monthIndex = parseInt(month, 10) - 1;
      return `${monthNames[monthIndex]} ${year}`;
    default:
      return `${month}/${year}`;
  }
}

/**
 * Utility class for working with MonthYear objects
 */
export class MonthYearUtils {
  /**
   * Creates a new MonthYearUtils instance
   * @param {MonthYear} [baseMonthYear] - Base month-year to work with (defaults to current)
   */
  constructor(private baseMonthYear: MonthYear = getCurrentMonthYear()) { }

  /**
   * Gets the base MonthYear
   */
  get base(): MonthYear {
    return { ...this.baseMonthYear };
  }

  /**
   * Sets a new base MonthYear
   * @param {MonthYear} monthYear - New base month-year
   */
  setBase(monthYear: MonthYear): void {
    this.baseMonthYear = { ...monthYear };
  }

  /**
   * Gets MonthYear offset from the base
   * @param {number} monthOffset - Number of months to offset
   */
  getOffset(monthOffset: number): MonthYear {
    const baseDate = monthYearToDate(this.baseMonthYear);
    return getMonthYearOffset(monthOffset, baseDate);
  }

  /**
   * Gets the previous month from base
   */
  getPrevious(): MonthYear {
    return this.getOffset(-1);
  }

  /**
   * Gets the next month from base
   */
  getNext(): MonthYear {
    return this.getOffset(1);
  }

  /**
   * Checks if the given MonthYear is in the past relative to base
   * @param {MonthYear} monthYear - MonthYear to check
   */
  isPast(monthYear: MonthYear): boolean {
    return compareMonthYear(monthYear, this.baseMonthYear) < 0;
  }

  /**
   * Checks if the given MonthYear is in the future relative to base
   * @param {MonthYear} monthYear - MonthYear to check
   */
  isFuture(monthYear: MonthYear): boolean {
    return compareMonthYear(monthYear, this.baseMonthYear) > 0;
  }

  /**
   * Checks if the given MonthYear is the same as base
   * @param {MonthYear} monthYear - MonthYear to check
   */
  isCurrent(monthYear: MonthYear): boolean {
    return isMonthYearEqual(monthYear, this.baseMonthYear);
  }
}

// Export a default instance for convenience
export const monthYearUtils = new MonthYearUtils();

/**
 * Type guard to check if an object is a valid MonthYear
 * @param {any} obj - Object to check
 * @returns {obj is MonthYear} True if object is a valid MonthYear
 */
export function isMonthYear(obj: any): obj is MonthYear {
  return (
    typeof obj === 'object' &&
    obj !== null &&
    typeof obj.month === 'string' &&
    typeof obj.year === 'number' &&
    /^(0[1-9]|1[0-2])$/.test(obj.month) &&
    obj.year > 0
  );
}