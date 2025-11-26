using System;
using System.Collections.Generic;
using System.Linq;
using TownePark.Models.Vo;
using api.Models.Dto;
using api.Data;
using TownePark;

namespace api.Services.Impl.Calculators
{
    public class PerLaborHourCalculator : IInternalRevenueCalculator
    {
        private readonly IPayrollRepository _payrollRepository;

        public PerLaborHourCalculator(IPayrollRepository payrollRepository)
        {
            _payrollRepository = payrollRepository;
        }

        private PerLaborHourInternalRevenueDto CalculateCurrentMonthPerLaborHourUsingEdw(
            InternalRevenueDataVo siteData,
            int targetYear,
            int targetMonthOneBased)
        {
            // Fetch EDW actual payroll rows for the site/month
            string billingPeriod = $"{targetYear}-{targetMonthOneBased:D2}";
            var payroll = _payrollRepository.GetPayroll(siteData.SiteId, billingPeriod);
            // Pull EDW actuals directly from repository
            var edw = _payrollRepository.GetActualPayrollFromEDW(siteData.SiteNumber, targetYear, targetMonthOneBased).GetAwaiter().GetResult();
            var edwActuals = edw?.Records ?? new List<api.Models.Vo.EDWPayrollDetailsRecord>();

            // Determine cutoff as the max actual date in the target month
            DateTime? cutoff = null;
            foreach (var r in edwActuals)
            {
                var d = r.Date;
                if (d.Year == targetYear && d.Month == targetMonthOneBased)
                {
                    if (!cutoff.HasValue || d > cutoff.Value) cutoff = d;
                }
            }

            // Fallback: if calculation ran but there are no current-month actuals,
            // set last-actual date to the last day of the previous month
            if (!cutoff.HasValue)
            {
                cutoff = new DateTime(targetYear, targetMonthOneBased, 1).AddDays(-1);
            }

            // Build actual hours by job code (EDW) up to cutoff
            var actualHoursByJob = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            if (cutoff.HasValue)
            {
                foreach (var r in edwActuals)
                {
                    var d = r.Date;
                    if (d.Year == targetYear && d.Month == targetMonthOneBased && d.Date <= cutoff.Value)
                    {
                        if (!actualHoursByJob.ContainsKey(r.JobCode)) actualHoursByJob[r.JobCode] = 0m;
                        actualHoursByJob[r.JobCode] += r.Hours;
                    }
                }
            }

            // Build forecast hours by job code from daily forecast rows (bs_PayrollDetail)
            var forecastHoursByJob = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            if (payroll?.bs_PayrollDetail_Payroll != null)
            {
                foreach (var detail in payroll.bs_PayrollDetail_Payroll)
                {
                    var d = detail.Contains(bs_PayrollDetail.Fields.bs_Date)
                        ? (DateTime?)detail[bs_PayrollDetail.Fields.bs_Date]
                        : null;
                    if (!d.HasValue) continue;
                    if (d.Value.Year != targetYear || d.Value.Month != targetMonthOneBased) continue;
                    if (cutoff.HasValue && d.Value.Date <= cutoff.Value) continue; // only after cutoff
                    // if no cutoff, include entire month
                    var jobCode = detail.Contains("jobcode_display") ? detail["jobcode_display"]?.ToString() : null;
                    if (string.IsNullOrWhiteSpace(jobCode)) continue;
                    var hours = detail.Contains(bs_PayrollDetail.Fields.bs_RegularHours) ? (decimal?)detail[bs_PayrollDetail.Fields.bs_RegularHours] : null;
                    if (!hours.HasValue) continue;
                    if (!forecastHoursByJob.ContainsKey(jobCode)) forecastHoursByJob[jobCode] = 0m;
                    forecastHoursByJob[jobCode] += hours.Value;
                }
            }

            // Apply rate logic
            decimal totalAmount;
            decimal actualAmount;
            decimal forecastAmount;

            if (siteData.LaborHourJobs == null || siteData.LaborHourJobs.Count == 0)
            {
                // Single rate at site-level via management agreement
                var rate = siteData.ManagementAgreement?.PerLaborHourRate ?? 0m;
                var actualHoursTotal = actualHoursByJob.Values.Sum();
                var forecastHoursTotal = forecastHoursByJob.Values.Sum();
                totalAmount = (actualHoursTotal + forecastHoursTotal) * rate;
                actualAmount = actualHoursTotal * rate;
                forecastAmount = forecastHoursTotal * rate;
            }
            else
            {
                // Job-based rates: apply configured job code rates; escalators apply to actual portion only
                var contract = siteData.Contract;
                bool hasEscalatorRule = contract != null && contract.IncrementMonth.HasValue && contract.IncrementAmount.HasValue && contract.IncrementAmount.Value != 0;
                decimal escalatorPercent = hasEscalatorRule ? contract.IncrementAmount.Value / 100m : 0m;

                DateTime firstDayOfCalculationMonth = new DateTime(targetYear, targetMonthOneBased, 1);
                actualAmount = 0m;
                forecastAmount = 0m;

                // Base amounts
                foreach (var job in siteData.LaborHourJobs)
                {
                    if (!(job.StartDate.Date <= firstDayOfCalculationMonth && (job.EndDate == null || job.EndDate >= firstDayOfCalculationMonth)))
                        continue;

                    var actualJobHours = actualHoursByJob.TryGetValue(job.JobCode, out var ah) ? ah : 0m;
                    var forecastJobHours = forecastHoursByJob.TryGetValue(job.JobCode, out var fh) ? fh : 0m;

                    actualAmount += actualJobHours * job.Rate;
                    forecastAmount += forecastJobHours * job.Rate;
                }

                // Historical escalators on actual portion only
                decimal escalatedActual = actualAmount;
                if (hasEscalatorRule && actualAmount > 0)
                {
                    decimal accum = 0m;
                    foreach (var job in siteData.LaborHourJobs)
                    {
                        if (!(job.StartDate.Date <= firstDayOfCalculationMonth && (job.EndDate == null || job.EndDate >= firstDayOfCalculationMonth)))
                            continue;
                        var actualJobHours = actualHoursByJob.TryGetValue(job.JobCode, out var ah) ? ah : 0m;
                        if (actualJobHours <= 0) continue;
                        var laborValueAfterHistoricalEsc = actualJobHours * job.Rate;
                        for (int escalationYear = job.StartDate.Year; escalationYear < targetYear; escalationYear++)
                        {
                            var escalatorApplicationDate = new DateTime(escalationYear, contract!.IncrementMonth!.Value, 1);
                            if (job.StartDate <= escalatorApplicationDate && (job.EndDate == null || job.EndDate >= escalatorApplicationDate))
                            {
                                laborValueAfterHistoricalEsc += laborValueAfterHistoricalEsc * escalatorPercent;
                            }
                        }
                        accum += laborValueAfterHistoricalEsc;
                    }
                    escalatedActual = accum;
                }

                // Current year escalator on actual portion if applicable
                if (hasEscalatorRule && contract!.IncrementMonth!.Value <= targetMonthOneBased && escalatedActual > 0)
                {
                    escalatedActual += escalatedActual * escalatorPercent;
                }

                totalAmount = escalatedActual + forecastAmount;
            }

            return new PerLaborHourInternalRevenueDto
            {
                Total = totalAmount,
                ActualPerLaborHour = actualAmount,
                ForecastedPerLaborHour = forecastAmount,
                LastActualDate = cutoff
            };
        }
        public void CalculateAndApply(
            InternalRevenueDataVo siteData,
            int year,
            int monthOneBased,
            int currentMonth, // NEW: Current month parameter
            MonthValueDto monthValueDto,
            SiteMonthlyRevenueDetailDto siteDetailDto,
            decimal calculatedExternalRevenue,
            List<PnlRowDto> budgetRows)
        {
            //Check if PerLaborHour is in the enabled contract types
            if (siteData.Contract?.ContractTypes == null || 
                !siteData.Contract.ContractTypes.Contains(bs_contracttypechoices.PerLaborHour))
                return;
            
            // Use the provided currentMonth parameter to determine current month behavior
            var isCurrentMonth = (monthOneBased == currentMonth);
            
            PerLaborHourInternalRevenueDto perLaborHourRevenue;
            
            if (isCurrentMonth)
            {
                // Current month: use EDW actuals up to cutoff + daily forecast remainder
                perLaborHourRevenue = CalculateCurrentMonthPerLaborHourUsingEdw(siteData, year, monthOneBased);
            }
            else
            {
                // Use existing forecast logic for non-current months
                perLaborHourRevenue = CalculateMonthlyPerLaborHourRevenueForSite(siteData, year, monthOneBased, calculatedExternalRevenue, monthValueDto, budgetRows);
            }

            if (siteDetailDto.InternalRevenueBreakdown == null)
            {
                siteDetailDto.InternalRevenueBreakdown = new InternalRevenueBreakdownDto();
            }
            siteDetailDto.InternalRevenueBreakdown.PerLaborHour = perLaborHourRevenue;

            // Hybrid approach: set CalculatedTotalInternalRevenue at site level
            var breakdown = siteDetailDto.InternalRevenueBreakdown;
            decimal total = 0m;
            if (breakdown.FixedFee?.Total != null) total += breakdown.FixedFee.Total.Value;
            if (breakdown.PerOccupiedRoom?.Total != null) total += breakdown.PerOccupiedRoom.Total.Value;
            if (breakdown.RevenueShare?.Total != null) total += breakdown.RevenueShare.Total.Value;
            if (breakdown.PerLaborHour?.Total != null) total += breakdown.PerLaborHour.Total.Value;
            if (breakdown.BillableAccounts?.Total != null) total += breakdown.BillableAccounts.Total.Value;
            if (breakdown.ManagementAgreement?.Total != null) total += breakdown.ManagementAgreement.Total.Value;
            if (breakdown.OtherRevenue?.Total != null) total += breakdown.OtherRevenue.Total.Value;
            breakdown.CalculatedTotalInternalRevenue = total;


        }

        public void AggregateMonthlyTotals(List<SiteMonthlyRevenueDetailDto> siteDetailsForMonth, MonthValueDto monthValueDto)
        {
            decimal totalPerLaborHourForMonth = 0m;

            foreach (var siteDetail in siteDetailsForMonth)
            {
                if (siteDetail.InternalRevenueBreakdown?.PerLaborHour != null)
                {
                    var perLaborHour = siteDetail.InternalRevenueBreakdown.PerLaborHour;
                    totalPerLaborHourForMonth += perLaborHour.Total ?? 0m;
                }
            }

            if (monthValueDto.InternalRevenueBreakdown == null)
                monthValueDto.InternalRevenueBreakdown = new InternalRevenueBreakdownDto();

            monthValueDto.InternalRevenueBreakdown.PerLaborHour = new PerLaborHourInternalRevenueDto
            {
                Total = totalPerLaborHourForMonth
            };

            monthValueDto.Value = (monthValueDto.InternalRevenueBreakdown.PerLaborHour?.Total ?? 0m)
                + (monthValueDto.InternalRevenueBreakdown.FixedFee?.Total ?? 0m)
                + (monthValueDto.InternalRevenueBreakdown.PerOccupiedRoom?.Total ?? 0m)
                + (monthValueDto.InternalRevenueBreakdown.RevenueShare?.Total ?? 0m);
        }

        private PerLaborHourInternalRevenueDto CalculateMonthlyPerLaborHourRevenueForSite(
            InternalRevenueDataVo siteData, int targetYear, int targetMonthOneBased, decimal calculatedExternalRevenue, MonthValueDto monthValueDto, List<PnlRowDto> budgetRows)
        {
            DateTime firstDayOfCalculationMonth = new DateTime(targetYear, targetMonthOneBased, 1);
            var contract = siteData.Contract;
            bool hasEscalatorRule = contract != null && contract.IncrementMonth.HasValue && contract.IncrementAmount.HasValue && contract.IncrementAmount.Value != 0;
            decimal escalatorPercent = hasEscalatorRule ? contract.IncrementAmount.Value / 100m : 0m;

            decimal totalOriginalBaseLaborThisMonth = 0m;
            decimal totalEscalatedValueAtStartOfTargetYear = 0m;

            // Get payroll for the site and period (format: yyyyMM)
            string billingPeriod = $"{targetYear}-{targetMonthOneBased:D2}";
            var payroll = _payrollRepository.GetPayroll(siteData.SiteId, billingPeriod);

            // Aggregate hours by job code for the month
            var jobCodeToHours = new Dictionary<string, decimal>();
            if (payroll != null && payroll.bs_PayrollDetail_Payroll != null)
            {
                foreach (var detail in payroll.bs_PayrollDetail_Payroll)
                {
                    var jobCode = detail.Contains("jobcode_display") ? detail["jobcode_display"]?.ToString() : null;
                    if (string.IsNullOrEmpty(jobCode)) continue;

                    var hours = detail.Contains(bs_PayrollDetail.Fields.bs_RegularHours) ? (decimal?)detail[bs_PayrollDetail.Fields.bs_RegularHours] : null;
                    if (!hours.HasValue) continue;

                    if (!jobCodeToHours.ContainsKey(jobCode))
                        jobCodeToHours[jobCode] = 0m;
                    jobCodeToHours[jobCode] += hours.Value;
                }
            }

            if (siteData.LaborHourJobs != null)
            {
                foreach (var job in siteData.LaborHourJobs)
                {
                    // Only consider jobs active for this month
                    if (!(job.StartDate.Date <= firstDayOfCalculationMonth && (job.EndDate == null || job.EndDate >= firstDayOfCalculationMonth)))
                        continue;

                    // Match job code
                    decimal jobHours = 0m;
                    if (jobCodeToHours.TryGetValue(job.JobCode, out var hours))
                        jobHours = hours;
                    else
                    {
                        // Fallback to budget hours if no payroll hours found
                        // this does not currently work since it tries to get the 'expense' value for the month instead of hours budgeted for each job code
                        jobHours = GetBudgetHoursForJobCode(budgetRows, job.JobCode, siteData.SiteNumber, targetMonthOneBased);
                    }

                    decimal baseLabor = jobHours * job.Rate;
                    totalOriginalBaseLaborThisMonth += baseLabor;
                    decimal laborValueAfterHistoricalEsc = baseLabor;

                    // Compound escalators for each year from job start up to (but not including) targetYear
                    if (hasEscalatorRule)
                    {
                        for (int escalationYear = job.StartDate.Year; escalationYear < targetYear; escalationYear++)
                        {
                            var escalatorApplicationDate = new DateTime(escalationYear, contract.IncrementMonth.Value, 1);
                            // Only escalate if job was active during the increment month of that year
                            if (job.StartDate <= escalatorApplicationDate && (job.EndDate == null || job.EndDate >= escalatorApplicationDate))
                            {
                                decimal historicalEscalatorAmount = laborValueAfterHistoricalEsc * escalatorPercent;
                                laborValueAfterHistoricalEsc += historicalEscalatorAmount;
                                // No need to add to escalatorsList
                            }
                        }
                    }
                    totalEscalatedValueAtStartOfTargetYear += laborValueAfterHistoricalEsc;
                }
            }

            decimal finalAmountForMonth = totalEscalatedValueAtStartOfTargetYear;

            // Apply the targetYear's own escalator if this month is on/after increment month
            if (hasEscalatorRule && targetMonthOneBased >= contract.IncrementMonth.Value)
            {
                if (totalEscalatedValueAtStartOfTargetYear > 0)
                {
                    decimal escalatorValueAppliedThisMonth = totalEscalatedValueAtStartOfTargetYear * escalatorPercent;
                    finalAmountForMonth += escalatorValueAppliedThisMonth;
                    // No need to add to escalatorsList
                }
            }

            return new PerLaborHourInternalRevenueDto
            {
                Total = finalAmountForMonth,
                ActualPerLaborHour = null, // No actuals for non-current months
                ForecastedPerLaborHour = finalAmountForMonth, // All forecast for non-current months
                LastActualDate = null // No actuals for non-current months
            };
        }

        // Helper to get budget hours for a job code from budgetRows
        private decimal GetBudgetHoursForJobCode(List<PnlRowDto> budgetRows, string jobCode, string siteNumber, int monthOneBased)
        {
            if (budgetRows == null)
                return 0m;

            // Find the budget row for PerLaborHour (column name may need to match your actual config)
            var laborHourRow = budgetRows.FirstOrDefault(r => r.ColumnName == "PerLaborHour");
            if (laborHourRow == null)
                return 0m;

            var monthValue = laborHourRow.MonthlyValues?.FirstOrDefault(mv => mv.Month == monthOneBased - 1);
            if (monthValue == null)
                return 0m;

            // Find the site detail for this site
            var siteDetail = monthValue.SiteDetails?.FirstOrDefault(sd => sd.SiteId == siteNumber);
            if (siteDetail == null || siteDetail.InternalRevenueBreakdown?.PerLaborHour == null)
                return 0m;

            // If your DTO supports job code breakdown, extract it here. Otherwise, fallback to total.
            // For now, fallback to total budget hours for PerLaborHour for the site/month.
            return siteDetail.InternalRevenueBreakdown.PerLaborHour.Total ?? 0m;
        }

        private PerLaborHourInternalRevenueDto CalculateCurrentMonthPerLaborHourRevenueWithQa(
            InternalRevenueDataVo siteData, int targetYear, int targetMonthOneBased, decimal calculatedExternalRevenue, MonthValueDto monthValueDto, List<PnlRowDto> budgetRows)
        {DateTime firstDayOfCalculationMonth = new DateTime(targetYear, targetMonthOneBased, 1);
            var contract = siteData.Contract;
            bool hasEscalatorRule = contract != null && contract.IncrementMonth.HasValue && contract.IncrementAmount.HasValue && contract.IncrementAmount.Value != 0;
            decimal escalatorPercent = hasEscalatorRule ? contract.IncrementAmount.Value / 100m : 0m;// Get payroll for the site and period (format: yyyyMM)
            string billingPeriod = $"{targetYear}-{targetMonthOneBased:D2}";
            var payroll = _payrollRepository.GetPayroll(siteData.SiteId, billingPeriod);

            // Get actuals up to max available date
            var actualsData = GetActualPayrollUpToMaxAvailableDate(siteData.SiteId, targetYear, targetMonthOneBased, payroll);// Calculate remaining days for forecast
            var daysInMonth = DateTime.DaysInMonth(targetYear, targetMonthOneBased);
            var remainingDays = 0;
            if (actualsData.MaxDate != DateTime.MinValue)
            {
                remainingDays = daysInMonth - actualsData.MaxDate.Day;
            }decimal totalOriginalBaseLaborThisMonth = 0m;
            decimal totalEscalatedValueAtStartOfTargetYear = 0m;
            decimal actualTotal = 0m;
            decimal forecastTotal = 0m;

            // Step 1: Process actuals (up to max available date)
            var jobCodeToActualHours = new Dictionary<string, decimal>();
            
            if (actualsData.PayrollDetails != null)
            {
                foreach (var detail in actualsData.PayrollDetails)
                {
                    var jobCode = detail.Contains("jobcode_display") ? detail["jobcode_display"]?.ToString() : null;
                    if (string.IsNullOrEmpty(jobCode)) continue;

                    var hours = detail.Contains(bs_PayrollDetail.Fields.bs_RegularHours) ? (decimal?)detail[bs_PayrollDetail.Fields.bs_RegularHours] : null;
                    if (!hours.HasValue || hours.Value <= 0) continue; // Only include non-zero actuals

                    if (!jobCodeToActualHours.ContainsKey(jobCode))
                        jobCodeToActualHours[jobCode] = 0m;
                    jobCodeToActualHours[jobCode] += hours.Value;
                }
            }// Step 2: Calculate forecast for remaining days using the same logic as non-current month
            var jobCodeToForecastHours = new Dictionary<string, decimal>();
            
            if (remainingDays > 0)
            {
                // Get full month payroll data for forecast calculation
                var jobCodeToFullMonthHours = new Dictionary<string, decimal>();
                if (payroll != null && payroll.bs_PayrollDetail_Payroll != null)
                {
                    foreach (var detail in payroll.bs_PayrollDetail_Payroll)
                    {
                        var jobCode = detail.Contains("jobcode_display") ? detail["jobcode_display"]?.ToString() : null;
                        if (string.IsNullOrEmpty(jobCode)) continue;

                        var hours = detail.Contains(bs_PayrollDetail.Fields.bs_RegularHours) ? (decimal?)detail[bs_PayrollDetail.Fields.bs_RegularHours] : null;
                        if (!hours.HasValue) continue;

                        if (!jobCodeToFullMonthHours.ContainsKey(jobCode))
                            jobCodeToFullMonthHours[jobCode] = 0m;
                        jobCodeToFullMonthHours[jobCode] += hours.Value;
                    }
                }// Calculate forecast hours for remaining days
                if (siteData.LaborHourJobs != null)
                {
                    foreach (var job in siteData.LaborHourJobs)
                    {
                        // Only consider jobs active for this month
                        if (!(job.StartDate.Date <= firstDayOfCalculationMonth && (job.EndDate == null || job.EndDate >= firstDayOfCalculationMonth)))
                            continue;

                        decimal forecastJobHours = 0m;
                        if (jobCodeToFullMonthHours.TryGetValue(job.JobCode, out var fullMonthHours))
                        {
                            // Calculate daily rate based on full month data
                            var dailyRate = fullMonthHours / daysInMonth;
                            forecastJobHours = dailyRate * remainingDays;}
                        else
                        {
                            // Fallback to budget hours if no payroll hours found
                            var budgetHours = GetBudgetHoursForJobCode(budgetRows, job.JobCode, siteData.SiteNumber, targetMonthOneBased);
                            var dailyRate = budgetHours / daysInMonth;
                            forecastJobHours = dailyRate * remainingDays;}

                        if (!jobCodeToForecastHours.ContainsKey(job.JobCode))
                            jobCodeToForecastHours[job.JobCode] = 0m;
                        jobCodeToForecastHours[job.JobCode] += forecastJobHours;


                    }
                }
            }// Step 3: Combine actuals and forecast, then apply escalators
            if (siteData.LaborHourJobs != null)
            {
                foreach (var job in siteData.LaborHourJobs)
                {
                    // Only consider jobs active for this month
                    if (!(job.StartDate.Date <= firstDayOfCalculationMonth && (job.EndDate == null || job.EndDate >= firstDayOfCalculationMonth)))
                        continue;

                    // Get actual hours for this job
                    decimal actualJobHours = 0m;
                    if (jobCodeToActualHours.TryGetValue(job.JobCode, out var actualHours))
                        actualJobHours = actualHours;

                    // Get forecast hours for this job
                    decimal forecastJobHours = 0m;
                    if (jobCodeToForecastHours.TryGetValue(job.JobCode, out var forecastHours))
                        forecastJobHours = forecastHours;

                    // Combine actuals + forecast
                    decimal totalJobHours = actualJobHours + forecastJobHours;
                    decimal actualJobLabor = actualJobHours * job.Rate;
                    decimal forecastJobLabor = forecastJobHours * job.Rate;
                    decimal totalJobLabor = totalJobHours * job.Rate;

                    actualTotal += actualJobLabor;
                    forecastTotal += forecastJobLabor;totalOriginalBaseLaborThisMonth += totalJobLabor;
                    decimal laborValueAfterHistoricalEsc = totalJobLabor;



                    // Compound escalators for each year from job start up to (but not including) targetYear
                    if (hasEscalatorRule)
                    {
                        for (int escalationYear = job.StartDate.Year; escalationYear < targetYear; escalationYear++)
                        {
                            var escalatorApplicationDate = new DateTime(escalationYear, contract.IncrementMonth.Value, 1);
                            // Only escalate if job was active during the increment month of that year
                            if (job.StartDate <= escalatorApplicationDate && (job.EndDate == null || job.EndDate >= escalatorApplicationDate))
                            {
                                decimal historicalEscalatorAmount = laborValueAfterHistoricalEsc * escalatorPercent;
                                laborValueAfterHistoricalEsc += historicalEscalatorAmount;}
                        }
                    }
                    totalEscalatedValueAtStartOfTargetYear += laborValueAfterHistoricalEsc;
                }
            }

            decimal finalAmountForMonth = totalEscalatedValueAtStartOfTargetYear;
            decimal? escalatorValueAppliedThisMonth = null;

            // Apply the targetYear's own escalator if this month is on/after increment month
            if (hasEscalatorRule && targetMonthOneBased >= contract.IncrementMonth.Value)
            {
                if (totalEscalatedValueAtStartOfTargetYear > 0)
                {
                    escalatorValueAppliedThisMonth = totalEscalatedValueAtStartOfTargetYear * escalatorPercent;
                    finalAmountForMonth += escalatorValueAppliedThisMonth.Value;}
            }return new PerLaborHourInternalRevenueDto 
            { 
                Total = finalAmountForMonth,
                ActualPerLaborHour = actualTotal,
                ForecastedPerLaborHour = forecastTotal,
                LastActualDate = actualsData.MaxDate != DateTime.MinValue ? actualsData.MaxDate : null
            };
        }

        private PerLaborHourInternalRevenueDto CalculateCurrentMonthPerLaborHourRevenue(
            InternalRevenueDataVo siteData, int targetYear, int targetMonthOneBased, decimal calculatedExternalRevenue, MonthValueDto monthValueDto, List<PnlRowDto> budgetRows)
        {
            DateTime firstDayOfCalculationMonth = new DateTime(targetYear, targetMonthOneBased, 1);
            var contract = siteData.Contract;
            bool hasEscalatorRule = contract != null && contract.IncrementMonth.HasValue && contract.IncrementAmount.HasValue && contract.IncrementAmount.Value != 0;
            decimal escalatorPercent = hasEscalatorRule ? contract.IncrementAmount.Value / 100m : 0m;

            // Get payroll for the site and period (format: yyyyMM)
            string billingPeriod = $"{targetYear}-{targetMonthOneBased:D2}";
            var payroll = _payrollRepository.GetPayroll(siteData.SiteId, billingPeriod);

            // Get actuals up to max available date
            var actualsData = GetActualPayrollUpToMaxAvailableDate(siteData.SiteId, targetYear, targetMonthOneBased, payroll);
            
            // Calculate remaining days for forecast
            var daysInMonth = DateTime.DaysInMonth(targetYear, targetMonthOneBased);
            var remainingDays = 0;
            if (actualsData.MaxDate != DateTime.MinValue)
            {
                remainingDays = daysInMonth - actualsData.MaxDate.Day;
            }

            decimal totalOriginalBaseLaborThisMonth = 0m;
            decimal totalEscalatedValueAtStartOfTargetYear = 0m;

            // Step 1: Process actuals (up to max available date)
            var jobCodeToActualHours = new Dictionary<string, decimal>();
            if (actualsData.PayrollDetails != null)
            {
                foreach (var detail in actualsData.PayrollDetails)
                {
                    var jobCode = detail.Contains("jobcode_display") ? detail["jobcode_display"]?.ToString() : null;
                    if (string.IsNullOrEmpty(jobCode)) continue;

                    var hours = detail.Contains(bs_PayrollDetail.Fields.bs_RegularHours) ? (decimal?)detail[bs_PayrollDetail.Fields.bs_RegularHours] : null;
                    if (!hours.HasValue || hours.Value <= 0) continue; // Only include non-zero actuals

                    if (!jobCodeToActualHours.ContainsKey(jobCode))
                        jobCodeToActualHours[jobCode] = 0m;
                    jobCodeToActualHours[jobCode] += hours.Value;
                }
            }

            // Step 2: Calculate forecast for remaining days using the same logic as non-current month
            var jobCodeToForecastHours = new Dictionary<string, decimal>();
            if (remainingDays > 0)
            {
                // Get full month payroll data for forecast calculation
                var jobCodeToFullMonthHours = new Dictionary<string, decimal>();
                if (payroll != null && payroll.bs_PayrollDetail_Payroll != null)
                {
                    foreach (var detail in payroll.bs_PayrollDetail_Payroll)
                    {
                        var jobCode = detail.Contains("jobcode_display") ? detail["jobcode_display"]?.ToString() : null;
                        if (string.IsNullOrEmpty(jobCode)) continue;

                        var hours = detail.Contains(bs_PayrollDetail.Fields.bs_RegularHours) ? (decimal?)detail[bs_PayrollDetail.Fields.bs_RegularHours] : null;
                        if (!hours.HasValue) continue;

                        if (!jobCodeToFullMonthHours.ContainsKey(jobCode))
                            jobCodeToFullMonthHours[jobCode] = 0m;
                        jobCodeToFullMonthHours[jobCode] += hours.Value;
                    }
                }

                // Calculate forecast hours for remaining days
                if (siteData.LaborHourJobs != null)
                {
                    foreach (var job in siteData.LaborHourJobs)
                    {
                        // Only consider jobs active for this month
                        if (!(job.StartDate.Date <= firstDayOfCalculationMonth && (job.EndDate == null || job.EndDate >= firstDayOfCalculationMonth)))
                            continue;

                        decimal forecastJobHours = 0m;
                        if (jobCodeToFullMonthHours.TryGetValue(job.JobCode, out var fullMonthHours))
                        {
                            // Calculate daily rate based on full month data
                            var dailyRate = fullMonthHours / daysInMonth;
                            forecastJobHours = dailyRate * remainingDays;
                        }
                        else
                        {
                            // Fallback to budget hours if no payroll hours found
                            var budgetHours = GetBudgetHoursForJobCode(budgetRows, job.JobCode, siteData.SiteNumber, targetMonthOneBased);
                            var dailyRate = budgetHours / daysInMonth;
                            forecastJobHours = dailyRate * remainingDays;
                        }

                        if (!jobCodeToForecastHours.ContainsKey(job.JobCode))
                            jobCodeToForecastHours[job.JobCode] = 0m;
                        jobCodeToForecastHours[job.JobCode] += forecastJobHours;
                    }
                }
            }

            // Step 3: Combine actuals and forecast, then apply escalators
            if (siteData.LaborHourJobs != null)
            {
                foreach (var job in siteData.LaborHourJobs)
                {
                    // Only consider jobs active for this month
                    if (!(job.StartDate.Date <= firstDayOfCalculationMonth && (job.EndDate == null || job.EndDate >= firstDayOfCalculationMonth)))
                        continue;

                    // Get actual hours for this job
                    decimal actualJobHours = 0m;
                    if (jobCodeToActualHours.TryGetValue(job.JobCode, out var actualHours))
                        actualJobHours = actualHours;

                    // Get forecast hours for this job
                    decimal forecastJobHours = 0m;
                    if (jobCodeToForecastHours.TryGetValue(job.JobCode, out var forecastHours))
                        forecastJobHours = forecastHours;

                    // Combine actuals + forecast
                    decimal totalJobHours = actualJobHours + forecastJobHours;

                    decimal baseLabor = totalJobHours * job.Rate;
                    totalOriginalBaseLaborThisMonth += baseLabor;
                    decimal laborValueAfterHistoricalEsc = baseLabor;

                    // Compound escalators for each year from job start up to (but not including) targetYear
                    if (hasEscalatorRule)
                    {
                        for (int escalationYear = job.StartDate.Year; escalationYear < targetYear; escalationYear++)
                        {
                            var escalatorApplicationDate = new DateTime(escalationYear, contract.IncrementMonth.Value, 1);
                            // Only escalate if job was active during the increment month of that year
                            if (job.StartDate <= escalatorApplicationDate && (job.EndDate == null || job.EndDate >= escalatorApplicationDate))
                            {
                                decimal historicalEscalatorAmount = laborValueAfterHistoricalEsc * escalatorPercent;
                                laborValueAfterHistoricalEsc += historicalEscalatorAmount;
                            }
                        }
                    }
                    totalEscalatedValueAtStartOfTargetYear += laborValueAfterHistoricalEsc;
                }
            }

            decimal finalAmountForMonth = totalEscalatedValueAtStartOfTargetYear;

            // Apply the targetYear's own escalator if this month is on/after increment month
            if (hasEscalatorRule && targetMonthOneBased >= contract.IncrementMonth.Value)
            {
                if (totalEscalatedValueAtStartOfTargetYear > 0)
                {
                    decimal escalatorValueAppliedThisMonth = totalEscalatedValueAtStartOfTargetYear * escalatorPercent;
                    finalAmountForMonth += escalatorValueAppliedThisMonth;
                }
            }

            return new PerLaborHourInternalRevenueDto
            {
                Total = finalAmountForMonth
            };
        }

        private (decimal Total, DateTime MaxDate, List<bs_PayrollDetail> PayrollDetails) GetActualPayrollUpToMaxAvailableDate(Guid siteId, int year, int month, bs_Payroll payroll)
        {
            if (payroll?.bs_PayrollDetail_Payroll == null)
                return (0m, DateTime.MinValue, new List<bs_PayrollDetail>());

            var actualDetails = new List<bs_PayrollDetail>();
            var maxDate = DateTime.MinValue;
            decimal totalHours = 0m;

            foreach (var detail in payroll.bs_PayrollDetail_Payroll)
            {
                var hours = detail.Contains(bs_PayrollDetail.Fields.bs_RegularHours) ? (decimal?)detail[bs_PayrollDetail.Fields.bs_RegularHours] : null;
                if (!hours.HasValue || hours.Value <= 0) continue; // Only include non-zero actuals

                // Assuming there's a date field in the payroll detail
                // This would need to be adjusted based on your actual data structure
                var detailDate = detail.Contains("date") ? (DateTime?)detail["date"] : null;
                if (detailDate.HasValue && detailDate.Value.Year == year && detailDate.Value.Month == month)
                {
                    if (detailDate.Value > maxDate)
                        maxDate = detailDate.Value;
                    
                    actualDetails.Add(detail);
                    totalHours += hours.Value;
                }
            }

            return (totalHours, maxDate, actualDetails);
        }
    }
}
