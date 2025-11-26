using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TownePark.Models.Vo;
using api.Models.Dto;
using TownePark;
using TownePark.Data;
using api.Data;
using api.Models.Vo;

namespace api.Services.Impl.Calculators
{
    public class ManagementFeeCalculator : IManagementAgreementCalculator
    {
        public int Order => 1;

        private readonly IInternalRevenueRepository _internalRevenueRepository;
        private readonly IPayrollRepository _payrollRepository;
        private readonly IJobCodeRepository _jobCodeRepository;
        private readonly Dictionary<Guid, IList<api.Models.Vo.JobCodeVo>> _siteJobCodeCache = new Dictionary<Guid, IList<api.Models.Vo.JobCodeVo>>();
        private readonly Dictionary<string, Dictionary<Guid, bs_Payroll>> _payrollCache = new Dictionary<string, Dictionary<Guid, bs_Payroll>>();

        public ManagementFeeCalculator(
            IInternalRevenueRepository internalRevenueRepository,
            IPayrollRepository payrollRepository,
            IJobCodeRepository jobCodeRepository)
        {
            _internalRevenueRepository = internalRevenueRepository ?? throw new ArgumentNullException(nameof(internalRevenueRepository));
            _payrollRepository = payrollRepository ?? throw new ArgumentNullException(nameof(payrollRepository));
            _jobCodeRepository = jobCodeRepository ?? throw new ArgumentNullException(nameof(jobCodeRepository));
        }

        public async Task CalculateAndApplyAsync(
            InternalRevenueDataVo siteData,
            int year,
            int monthOneBased,
            int currentMonth, // NEW: Current month parameter
            MonthValueDto monthValueDto,
            SiteMonthlyRevenueDetailDto siteDetailDto,
            decimal calculatedExternalRevenue,
            List<PnlRowDto> budgetRows)
        {
            if (siteData?.ManagementAgreement == null)
                return;

            // Check if ManagementAgreement is in the enabled contract types
            if (siteData.Contract?.ContractTypes == null || 
                !siteData.Contract.ContractTypes.Contains(bs_contracttypechoices.ManagementAgreement))
                return;

            var managementAgreement = siteData.ManagementAgreement;
            if (managementAgreement == null)
                return;

            var components = new List<ManagementAgreementComponentDto>();
            var escalators = new List<EscalatorDto>();
            decimal totalManagementFee = 0;

            // Determine fee type and calculate base fee
            var (baseFee, feeType) = await CalculateBaseFeeAsync(managementAgreement, siteData, year, monthOneBased, siteDetailDto, calculatedExternalRevenue);
            
            if (baseFee > 0)
            {
                components.Add(new ManagementAgreementComponentDto
                {
                    Name = feeType,
                    Value = baseFee
                });

                // Apply escalators if enabled
                var escalatedFee = ApplyCompoundEscalators(baseFee, managementAgreement, year, monthOneBased, escalators);
                totalManagementFee = escalatedFee;
            }

            // Initialize or update management agreement breakdown
            if (siteDetailDto.InternalRevenueBreakdown == null)
                siteDetailDto.InternalRevenueBreakdown = new InternalRevenueBreakdownDto();

            siteDetailDto.InternalRevenueBreakdown.ManagementAgreement = new ManagementAgreementInternalRevenueDto
            {
                Components = components,
                Escalators = escalators,
                Total = totalManagementFee
            };

            // No need to update monthly total here - that's handled by PnlService
        }

        public async Task AggregateMonthlyTotalsAsync(
            List<SiteMonthlyRevenueDetailDto> siteDetailsForMonth,
            MonthValueDto monthValueDto)
        {
            decimal totalManagementFee = 0;

            foreach (var siteDetail in siteDetailsForMonth)
            {
                if (siteDetail.InternalRevenueBreakdown?.ManagementAgreement?.Total.HasValue == true)
                {
                    totalManagementFee += siteDetail.InternalRevenueBreakdown.ManagementAgreement.Total.Value;
                }
            }

            // Initialize breakdown if needed
            if (monthValueDto.InternalRevenueBreakdown == null)
            {
                monthValueDto.InternalRevenueBreakdown = new InternalRevenueBreakdownDto();
            }

            // Set the aggregated management agreement total
            monthValueDto.InternalRevenueBreakdown.ManagementAgreement = new ManagementAgreementInternalRevenueDto
            {
                Total = totalManagementFee
            };

            await Task.CompletedTask; // Keep async method signature
        }


        private async Task<(decimal baseFee, string feeType)> CalculateBaseFeeAsync(
            ManagementAgreementVo agreement,
            InternalRevenueDataVo siteData,
            int year,
            int monthOneBased,
            SiteMonthlyRevenueDetailDto siteDetailDto,
            decimal externalRevenue)
        {
            // Priority order: Fixed Fee > Revenue % > Per Labor Hour
            
            // 1. Fixed Fee
            if (agreement.ConfiguredFee.HasValue && agreement.ConfiguredFee.Value > 0)
            {
                return (agreement.ConfiguredFee.Value, "Fixed Fee");
            }

            // 2. Revenue Percentage
            if (agreement.RevenuePercentageAmount.HasValue && agreement.RevenuePercentageAmount.Value > 0)
            {
                var revenueFee = externalRevenue * (agreement.RevenuePercentageAmount.Value / 100);
                return (revenueFee, $"Revenue % ({agreement.RevenuePercentageAmount.Value}%)");
            }

            // 3. Per Labor Hour
            // Check if we have job codes in the new structure
            if (agreement.PerLaborHourJobCodes?.Any() == true)
            {
                decimal totalLaborFee = 0;
                var jobCodeDetails = new List<string>();

                foreach (var jobCode in agreement.PerLaborHourJobCodes)
                {
                    if (jobCode.StandardRate > 0)
                    {
                        var forecastedHours = await GetForecastedHoursAsync(siteData, year, monthOneBased, jobCode.Code);
                        var jobCodeFee = forecastedHours * jobCode.StandardRate;
                        totalLaborFee += jobCodeFee;
                        
                        if (jobCodeFee > 0)
                        {
                            jobCodeDetails.Add($"{jobCode.Code}: {forecastedHours.ToString("F2", CultureInfo.InvariantCulture)} hrs @ ${jobCode.StandardRate.ToString("F2", CultureInfo.InvariantCulture)}/hr");
                        }
                    }
                }

                if (totalLaborFee > 0)
                {
                    var description = jobCodeDetails.Count > 1 
                        ? $"Per Labor Hour (Multiple Codes)"
                        : $"Per Labor Hour ({jobCodeDetails.FirstOrDefault() ?? "No hours"})";
                    return (totalLaborFee, description);
                }
            }
            // Fallback to legacy single job code if no new job codes are configured
            else if (agreement.PerLaborHourRate.HasValue && agreement.PerLaborHourRate.Value > 0)
            {
                var forecastedHours = await GetForecastedHoursAsync(siteData, year, monthOneBased, agreement.PerLaborHourJobCode);
                var laborFee = forecastedHours * agreement.PerLaborHourRate.Value;
                return (laborFee, $"Per Labor Hour (${agreement.PerLaborHourRate.Value.ToString("F2", CultureInfo.InvariantCulture)}/hour)");
            }

            return (0, "No Management Fee Configured");
        }

        private async Task<decimal> GetForecastedHoursAsync(InternalRevenueDataVo siteData, int year, int monthOneBased, string jobCode)
        {
            // Get forecasted hours from payroll data for the specified job code
            if (string.IsNullOrEmpty(jobCode))
                return 0;

            // Check if the job code exists for this site (with caching)
            if (!_siteJobCodeCache.TryGetValue(siteData.SiteId, out var siteJobCodes))
            {
                // Fetch and cache job codes for this site
                siteJobCodes = await _jobCodeRepository.GetJobCodesBySiteAsync(siteData.SiteId);
                _siteJobCodeCache[siteData.SiteId] = siteJobCodes;
            }

            var isValidJobCodeForSite = siteJobCodes.Any(jc => jc.JobCode == jobCode);
            
            if (!isValidJobCodeForSite)
                return 0;

            // Construct billing period in format YYYY-MM
            var billingPeriod = $"{year:D4}-{monthOneBased:D2}";
            
            // Try to get payroll data from cache
            bs_Payroll payroll = null;
            if (_payrollCache.TryGetValue(billingPeriod, out var monthCache))
            {
                monthCache.TryGetValue(siteData.SiteId, out payroll);
            }
            
            
            if (payroll?.bs_PayrollDetail_Payroll == null || !payroll.bs_PayrollDetail_Payroll.Any())
                return 0;

            // Sum regular hours for the specified job code
            var totalHours = payroll.bs_PayrollDetail_Payroll
                .Where(detail => 
                    detail.Attributes.ContainsKey("jobcode_display") && 
                    detail.Attributes["jobcode_display"]?.ToString() == jobCode &&
                    detail.bs_RegularHours.HasValue)
                .Sum(detail => detail.bs_RegularHours.Value);

            return totalHours;
        }

        private decimal ApplyCompoundEscalators(
            decimal baseFee,
            ManagementAgreementVo agreement,
            int targetYear,
            int targetMonth,
            List<EscalatorDto> escalators)
        {
            if (!agreement.ManagementFeeEscalatorEnabled.GetValueOrDefault() ||
                !agreement.ManagementFeeEscalatorValue.HasValue ||
                !agreement.ManagementFeeEscalatorMonth.HasValue ||
                !agreement.ManagementFeeEscalatorType.HasValue)
            {
                return baseFee;
            }

            decimal escalatedAmount = baseFee;
            var escalatorMonth = agreement.ManagementFeeEscalatorMonth.Value;
            var escalatorValue = agreement.ManagementFeeEscalatorValue.Value;
            var escalatorType = agreement.ManagementFeeEscalatorType.Value;

            // Apply historical escalators for compound effect (for multi-year projections)
            var currentYear = DateTime.Now.Year;
            
            // Apply escalators from current year to target year (for future projections)
            for (int year = currentYear; year < targetYear; year++)
            {
                var escalatorAmount = CalculateEscalatorAmount(escalatedAmount, escalatorType, escalatorValue);
                escalatedAmount += escalatorAmount;

                escalators.Add(new EscalatorDto
                {
                    Description = $"Management Fee Escalator ({escalatorMonth}/{year}) - {GetEscalatorTypeDescription(escalatorType, escalatorValue)}",
                    Amount = escalatorAmount,
                    IsApplied = true
                });
            }

            // Apply current year escalator if we're past the escalator month
            if (targetYear == currentYear && targetMonth >= escalatorMonth)
            {
                var currentYearEscalatorAmount = CalculateEscalatorAmount(escalatedAmount, escalatorType, escalatorValue);
                escalatedAmount += currentYearEscalatorAmount;

                escalators.Add(new EscalatorDto
                {
                    Description = $"Management Fee Escalator ({escalatorMonth}/{targetYear}) - {GetEscalatorTypeDescription(escalatorType, escalatorValue)}",
                    Amount = currentYearEscalatorAmount,
                    IsApplied = true
                });
            }
            else if (targetYear > currentYear)
            {
                // For future years, apply the escalator for the target year
                var futureYearEscalatorAmount = CalculateEscalatorAmount(escalatedAmount, escalatorType, escalatorValue);
                escalatedAmount += futureYearEscalatorAmount;

                escalators.Add(new EscalatorDto
                {
                    Description = $"Management Fee Escalator ({escalatorMonth}/{targetYear}) - {GetEscalatorTypeDescription(escalatorType, escalatorValue)}",
                    Amount = futureYearEscalatorAmount,
                    IsApplied = targetMonth >= escalatorMonth
                });
            }

            return escalatedAmount;
        }

        private decimal CalculateEscalatorAmount(decimal baseAmount, bs_escalatortype escalatorType, decimal escalatorValue)
        {
            return escalatorType switch
            {
                bs_escalatortype.Percentage => baseAmount * (escalatorValue / 100),
                bs_escalatortype.FixedAmount => escalatorValue,
                _ => 0
            };
        }

        private string GetEscalatorTypeDescription(bs_escalatortype escalatorType, decimal escalatorValue)
        {
            return escalatorType switch
            {
                bs_escalatortype.Percentage => $"{escalatorValue}%",
                bs_escalatortype.FixedAmount => $"${escalatorValue:F2}",
                _ => escalatorValue.ToString()
            };
        }
        
        /// <summary>
        /// Clears the job code cache. Should be called when processing a new batch of calculations.
        /// </summary>
        public void ClearJobCodeCache()
        {
            _siteJobCodeCache.Clear();
        }

        /// <summary>
        /// Preloads payroll data for multiple sites for a specific month to optimize performance.
        /// </summary>
        /// <param name="siteIds">List of site IDs to preload payroll data for</param>
        /// <param name="year">Year for the payroll period</param>
        /// <param name="month">Month for the payroll period (1-12)</param>
        public async Task PreloadPayrollDataAsync(List<Guid> siteIds, int year, int month)
        {
            if (siteIds == null || !siteIds.Any())
                return;

            var billingPeriod = $"{year:D4}-{month:D2}";
            
            // Check if we already have data for this period
            if (_payrollCache.ContainsKey(billingPeriod))
                return;

            // Batch fetch payroll data for all sites
            var payrollData = await _payrollRepository.GetPayrollBatchAsync(siteIds, billingPeriod);
            
            // Store in cache
            _payrollCache[billingPeriod] = payrollData;
        }

        /// <summary>
        /// Clears all cached data including payroll and job codes.
        /// </summary>
        public void ClearAllCaches()
        {
            _siteJobCodeCache.Clear();
            _payrollCache.Clear();
        }

        /// <summary>
        /// Clears payroll cache for a specific billing period.
        /// </summary>
        /// <param name="billingPeriod">Billing period in YYYY-MM format</param>
        public void ClearPayrollCache(string billingPeriod = null)
        {
            if (string.IsNullOrEmpty(billingPeriod))
            {
                _payrollCache.Clear();
            }
            else if (_payrollCache.ContainsKey(billingPeriod))
            {
                _payrollCache.Remove(billingPeriod);
            }
        }

        

        /// <summary>
        /// Loads pre-fetched yearly payroll batch into the internal cache (no repository calls).
        /// </summary>
        public void LoadPayrollCacheForYear(Dictionary<string, Dictionary<Guid, bs_Payroll>> yearly)
        {
            if (yearly == null || yearly.Count == 0) return;
            foreach (var kvp in yearly)
            {
                _payrollCache[kvp.Key] = kvp.Value;
            }
        }
    }
}