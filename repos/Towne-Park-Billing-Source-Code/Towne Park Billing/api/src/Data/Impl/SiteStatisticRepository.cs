using api.Config;
using api.Models.Vo;
using api.Services;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using TownePark;
using System.Text.Json;
using api.Models.Common;
using api.Usecases;

namespace api.Data.Impl
{
    public class SiteStatisticRepository : ISiteStatisticRepository
    {
        private readonly IDataverseService _dataverseService;
        private readonly IMonthRangeGenerator _monthRangeGenerator;
        private const string CustomerSiteEntityAlias = "customersite";

        public SiteStatisticRepository(IDataverseService dataverseService, IMonthRangeGenerator monthRangeGenerator)
        {
            _dataverseService = dataverseService;
            _monthRangeGenerator = monthRangeGenerator;
        }

        public bs_SiteStatistic? GetSiteStatistics(Guid siteId, string billingPeriod)
        {
            var serviceClient = _dataverseService.GetServiceClient();

            var query = new QueryExpression("bs_sitestatistic")
            {
                ColumnSet = new ColumnSet(
                    bs_SiteStatistic.Fields.bs_SiteStatisticId,
                    bs_SiteStatistic.Fields.bs_CustomerSiteFK,
                    bs_SiteStatistic.Fields.bs_BillingPeriod,
                    bs_SiteStatistic.Fields.bs_Name
                ),
                Criteria = new FilterExpression
                {
                    Conditions =
                {
                    new ConditionExpression(bs_SiteStatistic.Fields.bs_CustomerSiteFK, ConditionOperator.Equal, siteId),
                    new ConditionExpression(bs_SiteStatistic.Fields.bs_BillingPeriod, ConditionOperator.Equal, billingPeriod)
                }
                },
                PageInfo = new PagingInfo
                {
                    Count = 1, 
                    PageNumber = 1
                }
            };

            var link = query.AddLink(
                linkToAttributeName: bs_CustomerSite.Fields.bs_CustomerSiteId,
                linkFromAttributeName: bs_SiteStatistic.Fields.bs_CustomerSiteFK,
                linkToEntityName: bs_CustomerSite.EntityLogicalName
            );
            link.Columns = new ColumnSet(bs_CustomerSite.Fields.bs_TotalRoomsAvailable);
            link.EntityAlias = "customersite";

            var result = serviceClient.RetrieveMultiple(query);

            if (result.Entities.Count == 0)
                return null;

            var siteStatistic = result.Entities[0].ToEntity<bs_SiteStatistic>();

            var detailsQuery = new QueryExpression(bs_SiteStatisticDetail.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(
                    bs_SiteStatisticDetail.Fields.bs_Type,
                    bs_SiteStatisticDetail.Fields.bs_SiteStatisticDetailId,
                    bs_SiteStatisticDetail.Fields.bs_SiteStatisticFK,
                    bs_SiteStatisticDetail.Fields.bs_Date,
                    bs_SiteStatisticDetail.Fields.bs_ValetRateDaily,
                    bs_SiteStatisticDetail.Fields.bs_ValetRateMonthly,
                    bs_SiteStatisticDetail.Fields.bs_SelfRateDaily,
                    bs_SiteStatisticDetail.Fields.bs_SelfRateMonthly,
                    bs_SiteStatisticDetail.Fields.bs_BaseRevenue,
                    bs_SiteStatisticDetail.Fields.bs_OccupiedRooms,
                    bs_SiteStatisticDetail.Fields.bs_Occupancy,
                    bs_SiteStatisticDetail.Fields.bs_SelfOvernight,
                    bs_SiteStatisticDetail.Fields.bs_ValetOvernight,
                    bs_SiteStatisticDetail.Fields.bs_ValetDaily,
                    bs_SiteStatisticDetail.Fields.bs_ValetMonthly,
                    bs_SiteStatisticDetail.Fields.bs_SelfDaily,
                    bs_SiteStatisticDetail.Fields.bs_SelfMonthly,
                    bs_SiteStatisticDetail.Fields.bs_ValetComps,
                    bs_SiteStatisticDetail.Fields.bs_SelfComps,
                    bs_SiteStatisticDetail.Fields.bs_DriveInRatio,
                    bs_SiteStatisticDetail.Fields.bs_CaptureRatio,
                    bs_SiteStatisticDetail.Fields.bs_SelfAggregator,
                    bs_SiteStatisticDetail.Fields.bs_ValetAggregator,
                    bs_SiteStatisticDetail.Fields.bs_ExternalRevenue,
                    bs_SiteStatisticDetail.Fields.bs_AdjustmentValue,
                    bs_SiteStatisticDetail.Fields.bs_AdjustmentPercentage
                    ),
                Criteria = new FilterExpression
                {
                    Conditions = {
                    new ConditionExpression(
                        bs_SiteStatisticDetail.Fields.bs_SiteStatisticFK,
                        ConditionOperator.Equal,
                        siteStatistic.Id
                        )}
                }
            };

            var detailsResult = serviceClient.RetrieveMultiple(detailsQuery);
            siteStatistic.bs_SiteStatistic_SiteStatisticDetail = detailsResult.Entities
                .Select(e => e.ToEntity<bs_SiteStatisticDetail>())
                .ToList();

            ParseCustomerSiteAttributeValues(siteStatistic);
            return siteStatistic;
        }

        public IEnumerable<bs_SiteStatistic>? GetSiteStatisticsByRange(Guid siteId, string startingMonth, int monthCount)
        {
            var serviceClient = _dataverseService.GetServiceClient();
            var months = _monthRangeGenerator.GenerateMonthRange(startingMonth, monthCount);

            var query = new QueryExpression(bs_SiteStatistic.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(
                    bs_SiteStatistic.Fields.bs_SiteStatisticId,
                    bs_SiteStatistic.Fields.bs_CustomerSiteFK,
                    bs_SiteStatistic.Fields.bs_BillingPeriod,
                    bs_SiteStatistic.Fields.bs_Name
                ),
                Criteria = new FilterExpression(LogicalOperator.And)
            };

            query.Criteria.AddCondition(
                bs_SiteStatistic.Fields.bs_CustomerSiteFK,
                ConditionOperator.Equal,
                siteId);

            query.Criteria.AddCondition(
                bs_SiteStatistic.Fields.bs_BillingPeriod,
                ConditionOperator.In,
                months.ToArray());

            var customerLink = query.AddLink(
                linkToEntityName: bs_CustomerSite.EntityLogicalName,
                linkFromAttributeName: bs_SiteStatistic.Fields.bs_CustomerSiteFK,
                linkToAttributeName: bs_CustomerSite.Fields.bs_CustomerSiteId,
                joinOperator: JoinOperator.Inner
            );
            customerLink.Columns = new ColumnSet(
                        bs_CustomerSite.Fields.bs_TotalRoomsAvailable,
                        bs_CustomerSite.Fields.bs_SiteNumber
                    );
            customerLink.EntityAlias = CustomerSiteEntityAlias;

            var result = serviceClient.RetrieveMultiple(query);

            if (result.Entities.Count == 0)
                return null;

            var siteStatistics = result.Entities
                .Select(e => e.ToEntity<bs_SiteStatistic>())
                .ToList();

            foreach (var siteStatistic in siteStatistics)
            {
                var detailsQuery = new QueryExpression(bs_SiteStatisticDetail.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(true),
                    Criteria = new FilterExpression
                    {
                        Conditions = {
                    new ConditionExpression(
                        bs_SiteStatisticDetail.Fields.bs_SiteStatisticFK,
                        ConditionOperator.Equal,
                        siteStatistic.Id
                        )}
                    }
                };

                var detailsResult = serviceClient.RetrieveMultiple(detailsQuery);
                siteStatistic.bs_SiteStatistic_SiteStatisticDetail = detailsResult.Entities
                    .Select(e => e.ToEntity<bs_SiteStatisticDetail>())
                    .ToList();

                ParseCustomerSiteAttributeValues(siteStatistic);
            }

            return siteStatistics;
        }


        private SiteStatisticDetailVo CreateEmptyBudgetDetail(string period)
        {
            // Parse period format YYYY-MM to create DateOnly
            var parts = period.Split('-');
            var year = int.Parse(parts[0]);
            var month = int.Parse(parts[1]);
            var date = new DateOnly(year, month, 1);

            return new SiteStatisticDetailVo
            {
                Id = Guid.NewGuid(),
                Type = SiteStatisticDetailType.Budget,
                Date = date,
                BaseRevenue = 0,
                CaptureRatio = 0,
                DriveInRatio = 0,
                ExternalRevenue = 0,
                Occupancy = 0,
                OccupiedRooms = 0,
                SelfAggregator = 0,
                SelfComps = 0,
                SelfDaily = 0,
                SelfMonthly = 0,
                SelfOvernight = 0,
                SelfRateDaily = 0,
                SelfRateMonthly = 0,
                ValetAggregator = 0,
                ValetComps = 0,
                ValetDaily = 0,
                ValetMonthly = 0,
                ValetOvernight = 0,
                ValetRateDaily = 0,
                ValetRateMonthly = 0,
                AdjustmentValue = 0,
                AdjustmentPercentage = 0
            };
        }

        public async Task<List<SiteStatisticDetailVo>> GetBudgetData(string siteId, string billingPeriod, int totalRooms = 0)
        {
            var flowUrl = Configuration.getEDWDataAPIEndpoint();
            if (string.IsNullOrEmpty(flowUrl))
            {
                throw new Exception("Power Automate flow URL is not configured.");
            }

            var token = await Configuration.getAccessTokenAsync(true);

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var payload = new
                {
                    storedProcedureId = (int)StoredProcedureId.spBUDGET_DAILY_DETAIL,
                    storedProcedureParameters = new
                    {
                        COST_CENTER = siteId,
                        PERIOD = billingPeriod.Replace("-", "")
                    }
                };

                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                var httpResponse = await httpClient.PostAsync(flowUrl, content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var budgetData = await httpResponse.Content.ReadAsStringAsync();
                    var budgetVo = JsonConvert.DeserializeObject<List<SiteStatisticDetailVo>>(budgetData);

                    if (budgetVo != null)
                    {
                        foreach (var detail in budgetVo)
                        {
                            var totalOvernights = detail.SelfOvernight + detail.ValetOvernight;

                            if (detail.OccupiedRooms > 0)
                            {
                                detail.DriveInRatio = (double)(totalOvernights / detail.OccupiedRooms) * 100;
                            }
                            else
                            {
                                detail.DriveInRatio = 0;
                            }

                            if (totalOvernights > 0)
                            {
                                detail.CaptureRatio = (double)(detail.ValetOvernight / totalOvernights) * 100;
                            }
                            else
                            {
                                detail.CaptureRatio = 0;
                            }

                            if(totalRooms > 0)
                            {
                                detail.Occupancy = detail.OccupiedRooms / totalRooms;
                            }
                        }
                    }

                    return budgetVo;
                }
                else
                {
                    throw new Exception($"Error calling Power Automate: {httpResponse.StatusCode}");
                }
            }
        }

        public async Task<List<SiteStatisticDetailVo>> GetActualData(string siteId, string billingPeriod)
        {
            var flowUrl = Configuration.getEDWDataAPIEndpoint();
            if (string.IsNullOrEmpty(flowUrl))
            {
                throw new Exception("Power Automate flow URL is not configured.");
            }

            var token = await Configuration.getAccessTokenAsync(true);

            string[] billingPeriodParts = billingPeriod.Split('-');
            string year = billingPeriodParts[0];
            string month = billingPeriodParts[1];

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var payload = new
                {
                    storedProcedureId = (int)StoredProcedureId.Statistics_Actual,
                    storedProcedureParameters = new
                    {
                        SITE = siteId,
                        YEAR = year,
                        MONTH = month
                    }
                };
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var httpResponse = await httpClient.PostAsync(flowUrl, content);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var actualData = await httpResponse.Content.ReadAsStringAsync();
                    var actualVo = JsonConvert.DeserializeObject<List<SiteStatisticDetailVo>>(actualData);
                    if (actualVo != null)
                    {
                        foreach (var detail in actualVo)
                        {
                            var totalOvernights = detail.SelfOvernight + detail.ValetOvernight;

                            if (detail.OccupiedRooms > 0)
                            {
                                detail.DriveInRatio = (double)(totalOvernights / detail.OccupiedRooms) * 100;
                            }
                            else
                            {
                                detail.DriveInRatio = 0;
                            }

                            if (totalOvernights > 0)
                            {
                                detail.CaptureRatio = (double)(detail.ValetOvernight / totalOvernights) * 100;
                            }
                            else
                            {
                                detail.CaptureRatio = 0;
                            }
                        }
                    }
                    return actualVo;
                }
                else
                {
                    throw new Exception($"Error calling Power Automate: {httpResponse.StatusCode}");
                }
            }
        }


        public async Task<List<SiteStatisticDetailVo>> GetBudgetDataForRange(string siteId, List<string> billingPeriods, int totalRooms = 0)
        {
            // If only one period, use the existing method
            if (billingPeriods.Count == 1)
            {
                return await GetBudgetData(siteId, billingPeriods[0], totalRooms);
            }

            // For multiple periods, make parallel requests and combine results
            var tasks = billingPeriods.Select(period => GetBudgetData(siteId, period, totalRooms)).ToList();
            var results = await Task.WhenAll(tasks);
            
            // Combine all results into a single list
            return results.SelectMany(r => r ?? new List<SiteStatisticDetailVo>()).ToList();
        }

        public async Task<List<SiteStatisticDetailVo>> GetActualDataForRange(string siteId, List<string> billingPeriods)
        {
            if (billingPeriods.Count == 1)
            {
                return await GetActualData(siteId, billingPeriods[0]);
            }

            // For multiple periods, make parallel requests and combine results
            var tasks = billingPeriods.Select(period => GetActualData(siteId, period)).ToList();
            var results = await Task.WhenAll(tasks);

            // Combine all results into a single list
            return results.SelectMany(r => r ?? new List<SiteStatisticDetailVo>()).ToList();
        }

        private void ParseCustomerSiteAttributeValues(bs_SiteStatistic siteStatistic)
        {
            if (siteStatistic == null)
            {
                return; 
            }
            var totalRoomsValue = siteStatistic.GetAttributeValue<AliasedValue>(CustomerSiteEntityAlias + "." + bs_CustomerSite.Fields.bs_TotalRoomsAvailable);
            var siteNumberValue = siteStatistic.GetAttributeValue<AliasedValue>(CustomerSiteEntityAlias + "." + bs_CustomerSite.Fields.bs_SiteNumber); // Get SiteNumber too

           
            if (siteStatistic.bs_SiteStatistic_CustomerSite == null)
            {
                 siteStatistic.bs_SiteStatistic_CustomerSite = new bs_CustomerSite();
            }
           
            
            if (totalRoomsValue?.Value is string totalRoomsStr && !string.IsNullOrEmpty(totalRoomsStr))
            {
                siteStatistic.bs_SiteStatistic_CustomerSite.bs_TotalRoomsAvailable = totalRoomsStr;
            }
            else
            {
                siteStatistic.bs_SiteStatistic_CustomerSite.bs_TotalRoomsAvailable = siteStatistic.bs_SiteStatistic_CustomerSite.bs_TotalRoomsAvailable ?? "0"; 
            }

             if (siteNumberValue?.Value is string siteNumberStr && !string.IsNullOrEmpty(siteNumberStr))
            {
                siteStatistic.bs_SiteStatistic_CustomerSite.bs_SiteNumber = siteNumberStr;
            }
             else
            {
                 siteStatistic.bs_SiteStatistic_CustomerSite.bs_SiteNumber = siteStatistic.bs_SiteStatistic_CustomerSite.bs_SiteNumber ?? string.Empty; 
            }
        }

        public void SaveSiteStatistics(bs_SiteStatistic update)
        {
            var serviceClient = _dataverseService.GetServiceClient();

            var executeMultiple = new ExecuteMultipleRequest
            {
                Settings = new ExecuteMultipleSettings
                {
                    ContinueOnError = false,
                    ReturnResponses = false
                },
                Requests = new OrganizationRequestCollection()
            };

            // Batch child detail creates/updates
            foreach (var detail in update.bs_SiteStatistic_SiteStatisticDetail)
            {
                if (detail.Id == Guid.Empty)
                {
                    detail.Id = Guid.NewGuid();
                    detail.bs_SiteStatisticFK = new EntityReference(bs_SiteStatistic.EntityLogicalName, update.Id);
                    executeMultiple.Requests.Add(new CreateRequest { Target = detail });
                }
                else
                {
                    executeMultiple.Requests.Add(new UpdateRequest { Target = detail });
                }
            }

            // Execute batched detail operations first
            if (executeMultiple.Requests.Count > 0)
            {
                serviceClient.Execute(executeMultiple);
            }

            // Update parent separately to preserve existing behavior and satisfy tests
            serviceClient.Update(update);
        }

        public void CreateSiteStatistics(bs_SiteStatistic model)
        {
            var serviceClient = _dataverseService.GetServiceClient();
            

            model.Id = Guid.NewGuid();
            var details = model.bs_SiteStatistic_SiteStatisticDetail?.ToList();
            model.bs_SiteStatistic_SiteStatisticDetail = null;

            var newId = serviceClient.Create(model);

            // create related records with proper relationship
            if (details != null && details.Any())
            {
                foreach (var detail in details)
                {
                    detail.Id = Guid.NewGuid();
                    detail.bs_SiteStatisticFK = new EntityReference(bs_SiteStatistic.EntityLogicalName, newId);
                    serviceClient.Create(detail);
                }
            }
            else
            {
            }
        }

        private const string DetailEntityAlias = "detail";

        public List<bs_SiteStatistic> GetSiteStatisticsBatch(List<string> siteNumbers, List<string> billingPeriods)
        {
            var serviceClient = _dataverseService.GetServiceClient();

            var query = new QueryExpression(bs_SiteStatistic.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(
                    bs_SiteStatistic.Fields.bs_SiteStatisticId,
                    bs_SiteStatistic.Fields.bs_CustomerSiteFK,
                    bs_SiteStatistic.Fields.bs_BillingPeriod,
                    bs_SiteStatistic.Fields.bs_Name
                ),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression(bs_SiteStatistic.Fields.bs_BillingPeriod, ConditionOperator.In, billingPeriods.Cast<object>().ToArray())
                    }
                }
            };

            // Add customer site link
            var customerLink = query.AddLink(
                linkToEntityName: bs_CustomerSite.EntityLogicalName,
                linkFromAttributeName: bs_SiteStatistic.Fields.bs_CustomerSiteFK,
                linkToAttributeName: bs_CustomerSite.Fields.bs_CustomerSiteId,
                joinOperator: JoinOperator.Inner
            );
            customerLink.Columns = new ColumnSet(
                bs_CustomerSite.Fields.bs_TotalRoomsAvailable,
                bs_CustomerSite.Fields.bs_SiteNumber
            );
            customerLink.EntityAlias = CustomerSiteEntityAlias;
            customerLink.LinkCriteria = new FilterExpression
            {
                Conditions =
                {
                    new ConditionExpression(bs_CustomerSite.Fields.bs_SiteNumber, ConditionOperator.In, siteNumbers.Cast<object>().ToArray())
                }
            };

            // Add detail link with outer join
            var detailLink = query.AddLink(
                linkToEntityName: bs_SiteStatisticDetail.EntityLogicalName,
                linkFromAttributeName: bs_SiteStatistic.Fields.bs_SiteStatisticId,
                linkToAttributeName: bs_SiteStatisticDetail.Fields.bs_SiteStatisticFK,
                joinOperator: JoinOperator.LeftOuter
            );
            detailLink.EntityAlias = DetailEntityAlias;
            detailLink.Columns = new ColumnSet(
                bs_SiteStatisticDetail.Fields.bs_Type,
                bs_SiteStatisticDetail.Fields.bs_SiteStatisticDetailId,
                bs_SiteStatisticDetail.Fields.bs_Date,
                bs_SiteStatisticDetail.Fields.bs_ValetRateDaily,
                bs_SiteStatisticDetail.Fields.bs_ValetRateMonthly,
                bs_SiteStatisticDetail.Fields.bs_SelfRateDaily,
                bs_SiteStatisticDetail.Fields.bs_SelfRateMonthly,
                bs_SiteStatisticDetail.Fields.bs_BaseRevenue,
                bs_SiteStatisticDetail.Fields.bs_OccupiedRooms,
                bs_SiteStatisticDetail.Fields.bs_Occupancy,
                bs_SiteStatisticDetail.Fields.bs_SelfOvernight,
                bs_SiteStatisticDetail.Fields.bs_ValetOvernight,
                bs_SiteStatisticDetail.Fields.bs_ValetDaily,
                bs_SiteStatisticDetail.Fields.bs_ValetMonthly,
                bs_SiteStatisticDetail.Fields.bs_SelfDaily,
                bs_SiteStatisticDetail.Fields.bs_SelfMonthly,
                bs_SiteStatisticDetail.Fields.bs_ValetComps,
                bs_SiteStatisticDetail.Fields.bs_SelfComps,
                bs_SiteStatisticDetail.Fields.bs_DriveInRatio,
                bs_SiteStatisticDetail.Fields.bs_CaptureRatio,
                bs_SiteStatisticDetail.Fields.bs_SelfAggregator,
                bs_SiteStatisticDetail.Fields.bs_ValetAggregator,
                bs_SiteStatisticDetail.Fields.bs_ExternalRevenue,
                bs_SiteStatisticDetail.Fields.bs_AdjustmentValue,
                bs_SiteStatisticDetail.Fields.bs_AdjustmentPercentage
            );

            var result = serviceClient.RetrieveMultiple(query);
            var statisticsById = result.Entities
                .GroupBy(e => e.Id)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var firstEntity = g.First();
                        var statistic = firstEntity.ToEntity<bs_SiteStatistic>();
                        ParseCustomerSiteAttributeValues(statistic);

                        var details = g.Select(e => ExtractDetailFromAliasedValues(e))
                                     .Where(d => d != null)
                                     .ToList();

                        statistic.bs_SiteStatistic_SiteStatisticDetail = details.Count > 0
                            ? details
                            : new List<bs_SiteStatisticDetail>();

                        return statistic;
                    }
                );

            return statisticsById.Values.ToList();
        }

        private bs_SiteStatisticDetail ExtractDetailFromAliasedValues(Entity entity)
        {
            // Check if we have any detail data
            var detailId = entity.GetAttributeValue<AliasedValue>($"{DetailEntityAlias}.{bs_SiteStatisticDetail.Fields.bs_SiteStatisticDetailId}")?.Value as Guid?;
            if (!detailId.HasValue) return null;

            var detail = new bs_SiteStatisticDetail { Id = detailId.Value };

            // Map all aliased values to detail properties
            foreach (var field in typeof(bs_SiteStatisticDetail.Fields).GetFields())
            {
                var fieldName = field.GetValue(null) as string;
                if (fieldName == null) continue;

                var aliasedValue = entity.GetAttributeValue<AliasedValue>($"{DetailEntityAlias}.{fieldName}")?.Value;
                if (aliasedValue != null)
                {
                    detail[fieldName] = aliasedValue;
                }
            }

            return detail;
        }

        private string GetSiteNumber(Guid siteId)
        {
            var serviceClient = _dataverseService.GetServiceClient();
            var query = new QueryExpression(bs_CustomerSite.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(bs_CustomerSite.Fields.bs_SiteNumber),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression(bs_CustomerSite.Fields.bs_CustomerSiteId, ConditionOperator.Equal, siteId)
                    }
                }
            };

            var result = serviceClient.RetrieveMultiple(query);
            if (result.Entities.Count == 0)
                throw new Exception($"Customer site not found for ID: {siteId}");

            var customerSite = result.Entities[0].ToEntity<bs_CustomerSite>();
            return customerSite.bs_SiteNumber;
        }

        private List<string> GetConsecutiveMonths(string startingMonth, int monthCount = 3)
        {
            // Parse the billing period format (e.g., "2024-02")
            var dateParts = startingMonth.Split('-');
            if (dateParts.Length != 2 || !int.TryParse(dateParts[0], out int year) || !int.TryParse(dateParts[1], out int month))
            {
                throw new ArgumentException("Invalid billing period format. Expected YYYY-MM", nameof(startingMonth));
            }

            // Generate consecutive months starting from the given month
            var result = new List<string>();
            var currentDate = new DateTime(year, month, 1);

            for (int i = 0; i < monthCount; i++)
            {
                result.Add($"{currentDate.Year}-{currentDate.Month:D2}");
                currentDate = currentDate.AddMonths(1);
            }

            return result;
        }

        private bs_SiteStatistic CreateEmptySiteStatistic(Guid siteId, string billingPeriod)
        {
            var stat = new bs_SiteStatistic
            {
                bs_BillingPeriod = billingPeriod,
                bs_CustomerSiteFK = new EntityReference(bs_CustomerSite.EntityLogicalName, siteId)
            };
            return stat;
        }

        public IEnumerable<bs_SiteStatistic>? GetMonthlySiteStatistics(Guid siteId, string startingMonth)
        {
            var consecutiveMonths = GetConsecutiveMonths(startingMonth);
            var siteNumber = GetSiteNumber(siteId);
            var existingStats = GetSiteStatisticsBatch(new List<string> { siteNumber }, consecutiveMonths);

            // Create dictionary of existing stats by billing period, taking the first if there are duplicates
            var statsByPeriod = existingStats
                .GroupBy(s => s.bs_BillingPeriod)
                .ToDictionary(g => g.Key, g => g.First());

            // Ensure we have stats for all consecutive months
            var result = new List<bs_SiteStatistic>();
            foreach (var month in consecutiveMonths)
            {
                if (statsByPeriod.TryGetValue(month, out var stat))
                {
                    result.Add(stat);
                }
                else
                {
                    result.Add(CreateEmptySiteStatistic(siteId, month));
                }
            }

            return result.OrderBy(s => s.bs_BillingPeriod);
        }

        public async Task<PnlBySiteListVo> GetPNLData(List<string> siteIds, int year)
        {
            var flowUrl = Configuration.getEDWDataAPIEndpoint();
            if (string.IsNullOrEmpty(flowUrl))
            {
                throw new Exception("Power Automate flow URL is not configured.");
            }

            var token = await Configuration.getAccessTokenAsync(true);
            var siteNumbers = string.Join(",", siteIds);
            var payload = new
            {
                storedProcedureId = StoredProcedureId.spBudget_Actual_Summary_BySite,
                storedProcedureParameters = new
                {
                    SiteNumbers = siteNumbers,
                    Year = year.ToString()
                }
            };

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                var content = new System.Net.Http.StringContent(System.Text.Json.JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var httpResponse = await httpClient.PostAsync(flowUrl, content);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseString = await httpResponse.Content.ReadAsStringAsync();
                    var pnlVo = System.Text.Json.JsonSerializer.Deserialize<PnlBySiteListVo>(responseString, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return pnlVo ?? new PnlBySiteListVo();
                }
                else
                {
                    throw new Exception($"Error calling Power Automate: {httpResponse.StatusCode}");
                }
            }
        }
    }
}

