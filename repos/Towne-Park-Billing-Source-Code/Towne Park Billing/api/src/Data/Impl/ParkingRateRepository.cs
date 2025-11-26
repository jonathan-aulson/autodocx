using api.Services;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;
using api.Models.Vo;
using TownePark;

namespace api.Data.Impl
{
    public class ParkingRateRepository : IParkingRateRepository
    {
        private readonly IDataverseService _dataverseService;

        public ParkingRateRepository(IDataverseService dataverseService)
        {
            _dataverseService = dataverseService ?? throw new ArgumentNullException(nameof(dataverseService));
        }

        // Helper methods to ensure proper casting
        private static int ToInt(bs_parkingrate_statecode value) => (int)value;
        private static int ToInt(bs_parkingratedetail_statecode value) => (int)value;

        public bs_ParkingRate? GetParkingRateWithDetails(Guid siteId, int year)
        {
            var serviceClient = _dataverseService.GetServiceClient();

            // Query 1: Get the main ParkingRate record
            var rateQuery = new QueryExpression(bs_ParkingRate.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(
                    bs_ParkingRate.Fields.bs_ParkingRateId,
                    bs_ParkingRate.Fields.bs_Name,
                    bs_ParkingRate.Fields.bs_CustomerSiteFK,
                    bs_ParkingRate.Fields.bs_Year
                    ),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression(bs_ParkingRate.Fields.bs_CustomerSiteFK, ConditionOperator.Equal, siteId),
                        new ConditionExpression(bs_ParkingRate.Fields.bs_Year, ConditionOperator.Equal, year),
                    }
                },
                PageInfo = new PagingInfo { Count = 1, PageNumber = 1 } // Ensure only one is retrieved
            };

            try
            {
                EntityCollection rateResults = serviceClient.RetrieveMultiple(rateQuery);

                if (rateResults.Entities.Count == 0)
                {
                    return null; // No matching record found
                }

                var parkingRateModel = rateResults.Entities.First().ToEntity<bs_ParkingRate>();

                // Query 2: Get the related ParkingRateDetail records
                var detailQuery = new QueryExpression(bs_ParkingRateDetail.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(
                        bs_ParkingRateDetail.Fields.bs_ParkingRateDetailId,
                        bs_ParkingRateDetail.Fields.bs_Month,
                        bs_ParkingRateDetail.Fields.bs_Rate,
                        bs_ParkingRateDetail.Fields.bs_rate_Base,
                        bs_ParkingRateDetail.Fields.bs_Type,
                        bs_ParkingRateDetail.Fields.bs_RateCategory,
                        bs_ParkingRateDetail.Fields.bs_IsIncrease,
                        bs_ParkingRateDetail.Fields.bs_IncreaseAmount,
                        bs_ParkingRateDetail.Fields.bs_increaseamount_Base,
                        bs_ParkingRateDetail.Fields.bs_ParkingRateFK // Include FK for completeness, though filtered by it
                    ),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression(bs_ParkingRateDetail.Fields.bs_ParkingRateFK, ConditionOperator.Equal, parkingRateModel.Id)
                        }
                    }
                };

                EntityCollection detailResults = serviceClient.RetrieveMultiple(detailQuery);

                // Assign the details to the main model
                parkingRateModel.bs_parkingratedetail_ParkingRateFK_bs_parkingrate = detailResults.Entities
                    .Select(e => e.ToEntity<bs_ParkingRateDetail>())
                    .ToList();

                return parkingRateModel;
            }
            catch (Exception ex)
            {
                // Consider using a proper logging framework instead of Console.WriteLine
                Console.WriteLine($"Error in ParkingRateRepository.GetParkingRateWithDetails: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                // Depending on requirements, you might want to re-throw, return null, or handle differently
                return null;
            }
        }

        public void SaveParkingRates(bs_ParkingRate update)
        {
            var serviceClient = _dataverseService.GetServiceClient();
            serviceClient.Update(update);
        }

        public void CreateParkingRates(bs_ParkingRate model)
        {
            var serviceClient = _dataverseService.GetServiceClient();

            model.Id = Guid.NewGuid();
            var details = model.bs_parkingratedetail_ParkingRateFK_bs_parkingrate?.ToList();
            model.bs_parkingratedetail_ParkingRateFK_bs_parkingrate = null;

            Guid newId = serviceClient.Create(model);

            if (details != null && details.Any())
            {
                foreach (var detail in details)
                {
                    detail.Id = Guid.NewGuid();
                    detail.bs_ParkingRateFK = new EntityReference(bs_ParkingRate.EntityLogicalName, newId);
                    serviceClient.Create(detail);
                }
            }
        }

        public List<bs_ParkingRate> GetParkingRatesWithDetails(IEnumerable<Guid> siteIds, int year)
        {
            var serviceClient = _dataverseService.GetServiceClient();

            // 1. Fetch all parent rates
            var rateQuery = new QueryExpression(bs_ParkingRate.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(
                    bs_ParkingRate.Fields.bs_ParkingRateId,
                    bs_ParkingRate.Fields.bs_CustomerSiteFK,
                    bs_ParkingRate.Fields.bs_Year
                ),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression(bs_ParkingRate.Fields.bs_CustomerSiteFK, ConditionOperator.In, siteIds.Cast<object>().ToArray()),
                        new ConditionExpression(bs_ParkingRate.Fields.bs_Year, ConditionOperator.Equal, year)
                    }
                }
            };

            var rateResults = serviceClient.RetrieveMultiple(rateQuery);
            var parkingRates = rateResults.Entities.Select(e => e.ToEntity<bs_ParkingRate>()).ToList();
            var rateIds = parkingRates.Select(r => r.Id).ToList();

            if (!rateIds.Any())
                return parkingRates;

            // 2. Fetch all details for these rates in one query
            var detailQuery = new QueryExpression(bs_ParkingRateDetail.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(
                    bs_ParkingRateDetail.Fields.bs_ParkingRateDetailId,
                    bs_ParkingRateDetail.Fields.bs_Month,
                    bs_ParkingRateDetail.Fields.bs_Rate,
                    bs_ParkingRateDetail.Fields.bs_RateCategory,
                    bs_ParkingRateDetail.Fields.bs_ParkingRateFK
                ),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression(bs_ParkingRateDetail.Fields.bs_ParkingRateFK, ConditionOperator.In, rateIds.Cast<object>().ToArray())
                    }
                }
            };

            var detailResults = serviceClient.RetrieveMultiple(detailQuery);
            var allDetails = detailResults.Entities.Select(e => e.ToEntity<bs_ParkingRateDetail>()).ToList();

            // 3. Group details by parent and assign
            foreach (var rate in parkingRates)
            {
                rate.bs_parkingratedetail_ParkingRateFK_bs_parkingrate = allDetails
                    .Where(d => d.bs_ParkingRateFK != null && d.bs_ParkingRateFK.Id == rate.Id)
                    .ToList();
            }

            return parkingRates;
        }

        /// <summary>
        /// Fetches parking rate data (Budget and Actual) via EDW stored procedure.
        /// </summary>
        /// <param name="siteNumber">Site number (string)</param>
        /// <param name="year">Year (int)</param>
        /// <returns>ParkingRateDataVo or null</returns>
        public async Task<ParkingRateDataVo?> GetParkingRateDataFromEDW(string siteNumber, int year)
        {
            var flowUrl = api.Config.Configuration.getEDWDataAPIEndpoint();
            if (string.IsNullOrEmpty(flowUrl))
            {
                throw new Exception("Power Automate flow URL is not configured.");
            }

            var token = await api.Config.Configuration.getAccessTokenAsync(true);

            using (var httpClient = new System.Net.Http.HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var payload = new
                {
                    storedProcedureId = (int)api.Models.Common.StoredProcedureId.spBUDGET_AND_ACTUAL_PARKING_RATES_DETAIL,
                    storedProcedureParameters = new
                    {
                        COST_CENTER = siteNumber,
                        YEAR = year.ToString()
                    }
                };

                var content = new System.Net.Http.StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json");
                var httpResponse = await httpClient.PostAsync(flowUrl, content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var data = await httpResponse.Content.ReadAsStringAsync();

                    if (string.IsNullOrWhiteSpace(data))
                        return null;

                    try
                    {
                        var parkingRateData = Newtonsoft.Json.JsonConvert.DeserializeObject<ParkingRateDataVo>(data);
                        if (parkingRateData != null && (!string.IsNullOrEmpty(parkingRateData.SiteNumber) || parkingRateData.BudgetRates?.Any() == true || parkingRateData.ActualRates?.Any() == true))
                        {
                            return parkingRateData;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to parse as ParkingRateDataVo: {ex.Message}");
                    }

                    // Legacy fallback removed per refactor instructions
                    return null;
                }
                else
                {
                    var errorBody = await httpResponse.Content.ReadAsStringAsync();
                    throw new Exception($"Error calling EDW for ParkingRateData: {httpResponse.StatusCode} - {errorBody}");
                }
            }
        }

        public string? GetSiteNumber(Guid siteId)
        {
            try
            {
                var customerDetail = _dataverseService.GetServiceClient()
                    .Retrieve("bs_customersite", siteId, new Microsoft.Xrm.Sdk.Query.ColumnSet("bs_sitenumber"))
                    ?.GetAttributeValue<string>("bs_sitenumber");
                return customerDetail;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting site number: {ex.Message}");
                return null;
            }
        }
    }
}
