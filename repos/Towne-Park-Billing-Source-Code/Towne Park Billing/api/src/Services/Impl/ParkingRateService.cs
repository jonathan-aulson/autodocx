using api.Adapters.Mappers;
using api.Data;
using api.Data.Impl;
using api.Models.Vo;
using System;
using System.Globalization;
using TownePark; // Keep for bs_ParkingRate type if used elsewhere, maybe removable

namespace api.Services.Impl
{
    public class ParkingRateService : IParkingRateService
    {
        private readonly IParkingRateRepository _parkingRateRepository;
        private readonly ISiteStatisticService _siteStatisticService;

        public ParkingRateService(IParkingRateRepository parkingRateRepository, ISiteStatisticService siteStatisticService)
        {
            _parkingRateRepository = parkingRateRepository ?? throw new ArgumentNullException(nameof(parkingRateRepository));
            _siteStatisticService = siteStatisticService ?? throw new ArgumentNullException(nameof(siteStatisticService));
        }

        public async Task<ParkingRateDataVo> GetParkingRatesAsync(Guid siteId, int year)
        {
            // 1. Get Dataverse Parking Rate (forecast) data
            var dataverseParkingRate = _parkingRateRepository.GetParkingRateWithDetails(siteId, year);
            var result = dataverseParkingRate != null
                ? ParkingRateMapper.ParkingRateModelToVo(dataverseParkingRate)
                : new ParkingRateDataVo { CustomerSiteId = siteId, Year = year };

            var dataverseForecastData = result.ForecastRates ?? new List<ParkingRateDetailVo>();

            // 2. Get Site Number if missing
            if (string.IsNullOrEmpty(result.SiteNumber))
            {
                var siteNumber = _parkingRateRepository.GetSiteNumber(siteId);
                result.SiteNumber = siteNumber ?? siteId.ToString();
            }

            // 3. Get EDW Budget/Actual Data
            try
            {
                var edwParkingRateData = await _parkingRateRepository.GetParkingRateDataFromEDW(result.SiteNumber, year);

                if (edwParkingRateData != null)
                {
                    result.BudgetRates = edwParkingRateData.BudgetRates ?? new List<ParkingRateDetailVo>();
                    result.ActualRates = edwParkingRateData.ActualRates ?? new List<ParkingRateDetailVo>();

                    if (!string.IsNullOrEmpty(edwParkingRateData.Name))
                        result.Name = edwParkingRateData.Name;
                    if (!string.IsNullOrEmpty(edwParkingRateData.SiteNumber))
                        result.SiteNumber = edwParkingRateData.SiteNumber;
                }
                else
                {
                    result.BudgetRates = new List<ParkingRateDetailVo>();
                    result.ActualRates = new List<ParkingRateDetailVo>();
                }
            }
            catch (Exception ex)
            {
                result.BudgetRates = new List<ParkingRateDetailVo>();
                result.ActualRates = new List<ParkingRateDetailVo>();
            }

            result.ForecastRates = dataverseForecastData;
            return result;
        }
        public async Task<ParkingRateDataVo> SaveParkingRates(ParkingRateDataVo update)
        {

            
            // Get existing parking rates to determine if this is an update or create
            var existingParkingRate = _parkingRateRepository.GetParkingRateWithDetails(update.CustomerSiteId, update.Year);
            
            if (existingParkingRate != null)
            {
                // Found existing parking rate record
            }
            else
            {
                // No existing parking rate record found - this is a new creation
            }
            
            bs_ParkingRate parkingRateModel = ParkingRateMapper.ParkingRateVoToModel(update);

            if (parkingRateModel.Id != Guid.Empty)
            {
                // Update existing parking rate
                _parkingRateRepository.SaveParkingRates(parkingRateModel);
            }
            else
            {
                // Create new parking rate record
                _parkingRateRepository.CreateParkingRates(parkingRateModel);
            }

            // Update site statistics based on the new parking rates (synchronous to ensure failure surfaces to caller)
            await UpdateSiteStatisticsWithRates(update, existingParkingRate);

            // Return quickly with freshly saved Dataverse forecast rates only (skip EDW calls for responsiveness)
            var savedModel = _parkingRateRepository.GetParkingRateWithDetails(update.CustomerSiteId, update.Year);
            if (savedModel != null)
            {
                return ParkingRateMapper.ParkingRateModelToVo(savedModel);
            }

            // Fallback to echoing the incoming data if retrieval fails
            return update;
        }

        private async Task UpdateSiteStatisticsWithRates(ParkingRateDataVo parkingRateData, bs_ParkingRate existingParkingRate)
        {
            try
            {
 
                // Determine which rates to process based on whether existing rates exist
                List<ParkingRateDetailVo> ratesToProcess;
                
                if (existingParkingRate == null || existingParkingRate.bs_parkingratedetail_ParkingRateFK_bs_parkingrate?.Any() != true)
                {
                    // No existing records - process all forecast rates
                    ratesToProcess = parkingRateData.ForecastRates ?? new List<ParkingRateDetailVo>();
                                    }
                else
                {
                    // Existing records found - only process changed rates (delta)
                    ratesToProcess = GetChangedRates(parkingRateData.ForecastRates, existingParkingRate);
                }

                if (!ratesToProcess.Any())
                {
                    return;
                }

                // Get distinct periods from the rates to process
                var periods = new HashSet<string>();
                foreach (var rate in ratesToProcess)
                {
                    periods.Add($"{parkingRateData.Year:D4}-{rate.Month:D2}");
                }

                // For each period, get and update site statistics
                foreach (var period in periods)
                {
                    var siteStatistic = await _siteStatisticService.GetSiteStatisticsForSinglePeriodFast(
                        parkingRateData.CustomerSiteId,
                        period);

                    if (siteStatistic != null)
                    {
                        
                        if (siteStatistic.ForecastData != null && siteStatistic.ForecastData.Any())
                        {
                            
                            // Ensure all forecast data has correct PeriodLabel format before processing
                            foreach (var forecastDetail in siteStatistic.ForecastData)
                            {
                                // Force consistent MM/dd/yyyy format for PeriodLabel
                                forecastDetail.PeriodLabel = forecastDetail.Date.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
                            }
                            
                            // Get the corresponding rates for this period (only changed ones)
                            var periodRates = ratesToProcess
                                .Where(r => $"{parkingRateData.Year:D4}-{r.Month:D2}" == period)
                                .ToList();
                            
                            // Update forecast data with rate multipliers (only for changed rates and matching period)
                            // Filter forecast details to only include dates that match the current period (e.g., only August dates for 2025-08)
                            var periodYear = int.Parse(period.Split('-')[0]);
                            var periodMonth = int.Parse(period.Split('-')[1]);
                            
                            var filteredForecastDetails = siteStatistic.ForecastData
                                .Where(detail => detail.Date.Year == periodYear && detail.Date.Month == periodMonth)
                                .ToList();
                            
                            var changedDetails = new List<SiteStatisticDetailVo>();
                            foreach (var forecastDetail in filteredForecastDetails)
                            {
                                if (UpdateDetailWithRates(forecastDetail, periodRates))
                                {
                                    changedDetails.Add(forecastDetail);
                                }
                            }

                            if (changedDetails.Any())
                            {
                                // Persist only changed details to minimize write volume
                                siteStatistic.ForecastData = changedDetails;
                                _siteStatisticService.SaveSiteStatistics(siteStatistic);
                            }
                        }
                        else
                        {
                            // No forecast data found for site statistic in this period
                        }
                    }
                    else
                    {
                        // No site statistics found for this period
                    }
                }
            }
            catch (Exception ex)
            {
                // Log and rethrow to ensure the API does not return 200 on failure
                Console.WriteLine($"ERROR in UpdateSiteStatisticsWithRates: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private List<ParkingRateDetailVo> GetChangedRates(List<ParkingRateDetailVo> newRates, bs_ParkingRate existingParkingRate)
        {
            var changedRates = new List<ParkingRateDetailVo>();
            
            if (newRates == null || !newRates.Any())
            {
                return changedRates;
            }
                
            var existingRateDetails = existingParkingRate.bs_parkingratedetail_ParkingRateFK_bs_parkingrate?.ToList() ?? new List<bs_ParkingRateDetail>();
            var existingCount = existingRateDetails.Count;
            
            foreach (var newRate in newRates)
            {
                
                // Find the corresponding existing rate
                var existingRate = existingRateDetails.FirstOrDefault(er => 
                    er.bs_Month == newRate.Month && 
                    er.bs_RateCategory == newRate.RateCategory);
                
                if (existingRate == null)
                {
                    // This is a new rate - include it
                    changedRates.Add(newRate);
                }
                else 
                {
                    
                    if (Math.Abs((existingRate.bs_Rate?.Value ?? 0) - newRate.Rate) > 0.001m) // Using small epsilon for decimal comparison
                    {
                        // Rate has changed - include it
                        changedRates.Add(newRate);
                    }
                    else
                    {
                    }
                }
            }
            
            // Only categories that affect external revenue calculations
            var relevantCategories = new HashSet<bs_ratecategorytypes>
            {
                bs_ratecategorytypes.ValetOvernight,
                bs_ratecategorytypes.SelfOvernight,
                bs_ratecategorytypes.ValetDaily,
                bs_ratecategorytypes.SelfDaily,
                bs_ratecategorytypes.ValetMonthly,
                bs_ratecategorytypes.SelfMonthly
            };

            return changedRates.Where(r => relevantCategories.Contains(r.RateCategory)).ToList();
        }

        private bool UpdateDetailWithRates(SiteStatisticDetailVo detail, List<ParkingRateDetailVo> rates)
        {
            // Create temporary rate variables for calculation (don't update the detail properties)
            var calculationRates = new
            {
                ValetRateOvernight = detail.ValetRateOvernight,
                SelfRateOvernight = detail.SelfRateOvernight,
                ValetRateDaily = detail.ValetRateDaily,
                SelfRateDaily = detail.SelfRateDaily,
                ValetRateMonthly = detail.ValetRateMonthly,
                SelfRateMonthly = detail.SelfRateMonthly
            };

            // Apply category rates to temporary calculation variables (not to detail properties)
            var updatedRates = new Dictionary<bs_ratecategorytypes, decimal>();
            foreach (var rate in rates)
            {
                updatedRates[rate.RateCategory] = rate.Rate;
            }

            // Calculate external revenue using the new rates but keep original detail rates unchanged
            var valetOvernightRate = updatedRates.ContainsKey(bs_ratecategorytypes.ValetOvernight) ? updatedRates[bs_ratecategorytypes.ValetOvernight] : calculationRates.ValetRateOvernight;
            var selfOvernightRate = updatedRates.ContainsKey(bs_ratecategorytypes.SelfOvernight) ? updatedRates[bs_ratecategorytypes.SelfOvernight] : calculationRates.SelfRateOvernight;
            var valetDailyRate = updatedRates.ContainsKey(bs_ratecategorytypes.ValetDaily) ? updatedRates[bs_ratecategorytypes.ValetDaily] : calculationRates.ValetRateDaily;
            var selfDailyRate = updatedRates.ContainsKey(bs_ratecategorytypes.SelfDaily) ? updatedRates[bs_ratecategorytypes.SelfDaily] : calculationRates.SelfRateDaily;
            var valetMonthlyRate = updatedRates.ContainsKey(bs_ratecategorytypes.ValetMonthly) ? updatedRates[bs_ratecategorytypes.ValetMonthly] : calculationRates.ValetRateMonthly;
            var selfMonthlyRate = updatedRates.ContainsKey(bs_ratecategorytypes.SelfMonthly) ? updatedRates[bs_ratecategorytypes.SelfMonthly] : calculationRates.SelfRateMonthly;

            // Compute new values and update only if changed
            var prevBase = detail.BaseRevenue;
            var prevExternal = detail.ExternalRevenue;

            decimal grossValetOvernight = detail.ValetOvernight * valetOvernightRate;
            decimal grossSelfOvernight = detail.SelfOvernight * selfOvernightRate;
            decimal grossValetDaily = detail.ValetDaily * valetDailyRate;
            decimal grossSelfDaily = detail.SelfDaily * selfDailyRate;
            decimal grossValetMonthly = detail.ValetMonthly * valetMonthlyRate;
            decimal grossSelfMonthly = detail.SelfMonthly * selfMonthlyRate;

            decimal grossExternalRevenue = grossValetOvernight + grossSelfOvernight
                                        + grossValetDaily + grossSelfDaily
                                        + grossValetMonthly + grossSelfMonthly;

            decimal finalExternalRevenue = grossExternalRevenue * (1 + (detail.AdjustmentPercentage ?? 0m));

            var changed = Math.Abs(prevBase - grossExternalRevenue) > 0.01m || Math.Abs(prevExternal - finalExternalRevenue) > 0.01m;
            if (changed)
            {
                detail.BaseRevenue = grossExternalRevenue;
                detail.ExternalRevenue = finalExternalRevenue;
            }

            return changed;
        }

        
    }
}
