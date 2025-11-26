//Generate period options (12 months back, current month, and 12 months forward)
export function generatePeriodOptions() {
    const options = [];
    const today = new Date();
    const currentMonth = today.getMonth();
    const currentYear = today.getFullYear();

    // Add 12 months back
    for (let i = 12; i > 0; i--) {
        let month = currentMonth - i;
        let year = currentYear;

        if (month < 0) {
            month = month + 12;
            year = year - 1;
        }

        const value = `${year}-${String(month + 1).padStart(2, "0")}`;
        options.push({ value, label: value });
    }

    // Add current month
    const currentValue = `${currentYear}-${String(currentMonth + 1).padStart(2, "0")}`;
    options.push({ value: currentValue, label: currentValue });

    // Add 12 months forward
    for (let i = 1; i <= 12; i++) {
        let month = currentMonth + i;
        let year = currentYear;

        if (month > 11) {
            month = month - 12;
            year = year + 1;
        }

        const value = `${year}-${String(month + 1).padStart(2, "0")}`;
        options.push({ value, label: value });
    }

    return options;
}

//Check if a period is in the past
export function isPeriodInPast(period: string): boolean {
    const today = new Date();
    const currentYear = today.getFullYear();
    const currentMonth = today.getMonth() + 1;

    const [year, month] = period.split("-").map(Number);

    return year < currentYear || (year === currentYear && month < currentMonth);
}

//Check if a specific date is before today's date
export function isDateBeforeToday(dateString: string): boolean {
    const today = new Date();
    today.setHours(0, 0, 0, 0); // Set to start of today
    
    // Parse the date string (format: YYYY-MM-DD)
    const [yearStr, monthStr, dayStr] = dateString.split("-");
    const year = parseInt(yearStr, 10);
    const month = parseInt(monthStr, 10) - 1; // Month is 0-based in Date constructor
    const day = parseInt(dayStr, 10);
    
    // Create date object for the given date
    const targetDate = new Date(year, month, day);
    
    return targetDate < today;
}
