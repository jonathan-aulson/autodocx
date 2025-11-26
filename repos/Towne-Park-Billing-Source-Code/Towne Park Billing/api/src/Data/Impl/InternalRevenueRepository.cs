using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using TownePark.Models.Vo;
using api.Services; // Corrected namespace
using api.Adapters; // Corrected namespace
using api.Models.Vo;
using api.Models.Common;
using api.Config;
using Newtonsoft.Json;

namespace TownePark.Data.Impl
{
    public class InternalRevenueRepository : IInternalRevenueRepository
    {
        private readonly IDataverseService _dataverseService;
        private readonly IInternalRevenueMapper _mapper; // To be created
        private const int MaxSiteBatchSize = 500;
        private const int PageSize = 1000;

        public InternalRevenueRepository(IDataverseService dataverseService, IInternalRevenueMapper mapper)
        {
            _dataverseService = dataverseService;
            _mapper = mapper;
        }

        public async Task<InternalRevenueActualsVo?> GetInternalRevenueActualsAsync(string siteId, int year, int month)
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
                    storedProcedureId = StoredProcedureId.Internal_Revenue_Actuals,
                    storedProcedureParameters = new
                    {
                        SITE = siteId,
                        YEAR = year.ToString(),
                        MONTH = month.ToString()
                    }
                };

                var content = new StringContent(JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json");
                var httpResponse = await httpClient.PostAsync(flowUrl, content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var data = await httpResponse.Content.ReadAsStringAsync();

                    if (string.IsNullOrWhiteSpace(data))
                        return null;

                    try
                    {
                        var result = JsonConvert.DeserializeObject<InternalRevenueActualsVo>(data);
                        return result;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to parse InternalRevenueActualsVo: {ex.Message}");
                        return null;
                    }
                }
                else
                {
                    var errorBody = await httpResponse.Content.ReadAsStringAsync();
                    throw new Exception($"Error calling EDW for InternalRevenueActuals: {httpResponse.StatusCode} - {errorBody}");
                }
            }
        }

        public async Task<InternalRevenueActualsMultiSiteVo?> GetInternalRevenueActualsMultiSiteAsync(string siteIds, int year, int month)
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
                    storedProcedureId = StoredProcedureId.Internal_Revenue_Actuals,
                    storedProcedureParameters = new
                    {
                        SITE = siteIds,
                        YEAR = year.ToString(),
                        MONTH = month.ToString()
                    }
                };

                var content = new StringContent(JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json");
                var httpResponse = await httpClient.PostAsync(flowUrl, content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var data = await httpResponse.Content.ReadAsStringAsync();

                    if (string.IsNullOrWhiteSpace(data))
                        return null;

                    try
                    {
                        var result = JsonConvert.DeserializeObject<InternalRevenueActualsMultiSiteVo>(data);
                        return result;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to parse InternalRevenueActualsMultiSiteVo: {ex.Message}");
                        return null;
                    }
                }
                else
                {
                    var errorBody = await httpResponse.Content.ReadAsStringAsync();
                    throw new Exception($"Error calling EDW for InternalRevenueActualsMultiSite: {httpResponse.StatusCode} - {errorBody}");
                }
            }
        }

        public async Task<List<InternalRevenueDataVo>> GetInternalRevenueDataAsync(IEnumerable<string> siteNumbers, int year)
        {
            var allResults = new List<InternalRevenueDataVo>();
            var serviceClient = _dataverseService.GetServiceClient();

            var customerSites = await GetCustomerSitesAsync(serviceClient, siteNumbers);
            if (!customerSites.Any()) return allResults;
            var siteIdToCustomerSiteMap = customerSites.ToDictionary(cs => cs.Id, cs => cs);
            var allSiteIdsList = customerSites.Select(cs => cs.Id).ToList();

            foreach (var siteBatch in BatchSiteIds(allSiteIdsList, MaxSiteBatchSize))
            {
                var batchResults = await ProcessSiteBatchAsync(serviceClient, siteBatch, year, siteIdToCustomerSiteMap);
                allResults.AddRange(batchResults);
            }
            return allResults;
        }

        private async Task<List<bs_CustomerSite>> GetCustomerSitesAsync(IOrganizationService serviceClient, IEnumerable<string> siteNumbers)
        {
            var query = new QueryExpression(bs_CustomerSite.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(bs_CustomerSite.Fields.bs_CustomerSiteId, bs_CustomerSite.Fields.bs_SiteNumber, bs_CustomerSite.Fields.bs_SiteName, bs_CustomerSite.Fields.bs_StartDate),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression(bs_CustomerSite.Fields.bs_SiteNumber, ConditionOperator.In, siteNumbers.Cast<object>().ToArray())
                    }
                }
            };
            var result = await Task.Run(() => serviceClient.RetrieveMultiple(query));
            return result.Entities.Select(e => e.ToEntity<bs_CustomerSite>()).ToList();
        }

        private async Task<List<InternalRevenueDataVo>> ProcessSiteBatchAsync(IOrganizationService serviceClient, List<Guid> siteBatch, int year, Dictionary<Guid, bs_CustomerSite> siteIdToCustomerSiteMap)
        {
            var siteBatchArray = siteBatch.ToArray();
            var contracts = await GetContractsAsync(serviceClient, siteBatchArray);
            var groupedSiteStatistics = await GetGroupedSiteStatisticsAsync(serviceClient, siteBatchArray, year);
            var fixedFees = await GetFixedFeesAsync(serviceClient, contracts);
            var laborHourJobs = await GetLaborHourJobsAsync(serviceClient, contracts);
            var revenueShareThresholds = await GetRevenueShareThresholdsAsync(serviceClient, contracts);
            var billableAccounts = await GetBillableAccountsAsync(serviceClient, contracts);
            var managementAgreements = await GetManagementAgreementsAsync(serviceClient, contracts);
            var otherRevenues = await GetOtherRevenuesAsync(serviceClient, siteBatchArray, year);
            var nonGLExpenses = await GetNonGLExpensesAsync(serviceClient, contracts);

            var results = new List<InternalRevenueDataVo>();
            foreach (var siteId in siteBatch)
            {
                if (!siteIdToCustomerSiteMap.TryGetValue(siteId, out var customerSite)) continue;
                var siteContract = contracts.FirstOrDefault(c => c.bs_CustomerSiteFK?.Id == siteId);
                if (siteContract == null) continue;
                var currentSiteStats = groupedSiteStatistics.TryGetValue(siteId, out var statsList) ? statsList : new List<bs_SiteStatisticDetail>();
                var siteFixedFees = fixedFees.Where(f => f.bs_ContractFK?.Id == siteContract.Id).ToList();
                var siteLaborJobs = laborHourJobs.Where(j => j.bs_ContractFK?.Id == siteContract.Id).ToList();
                var siteRevShareThresholds = revenueShareThresholds.Where(r => r.bs_Contract?.Id == siteContract.Id).ToList();
                var siteBillableAccounts = billableAccounts.Where(b => b.bs_ContractFK?.Id == siteContract.Id).ToList();
                var siteMgmtAgreement = managementAgreements.FirstOrDefault(m => m.bs_ContractFK?.Id == siteContract.Id);
                var siteOtherRevenues = otherRevenues.Where(or => or.bs_CustomerSiteFK?.Id == siteId).ToList();
                var siteNonGLExpenses = nonGLExpenses.Where(e => e.bs_ContractFK?.Id == siteContract.Id).ToList();

                // Populate billable account relationship for contract mapping
                if (siteBillableAccounts.Any())
                {
                    siteContract.bs_BillableAccount_Contract = siteBillableAccounts;
                }

                results.Add(new InternalRevenueDataVo
                {
                    SiteId = siteId,
                    SiteNumber = customerSite.bs_SiteNumber,
                    SiteName = customerSite.bs_SiteName,
                    Contract = _mapper.MapContractToVo(siteContract),
                    SiteStatistics = _mapper.MapSiteStatisticsToVo(currentSiteStats),
                    FixedFees = _mapper.MapFixedFeesToVo(siteFixedFees),
                    LaborHourJobs = _mapper.MapLaborHourJobsToVo(siteLaborJobs),
                    RevenueShareThresholds = _mapper.MapRevenueShareThresholdsToVo(siteRevShareThresholds),
                    BillableAccounts = _mapper.MapBillableAccountsToVo(siteBillableAccounts),
                    ManagementAgreement = siteMgmtAgreement != null ? _mapper.MapManagementAgreementToVo(siteMgmtAgreement, customerSite) : null,
                    // Unified TownePark VO now carries ForecastData; calculators read OtherRevenues
                    OtherRevenues = _mapper.MapOtherRevenuesToVo(siteOtherRevenues),
                    OtherExpenses = _mapper.MapNonGLExpensesToVo(siteNonGLExpenses)
                });
            }
            return results;
        }

        private async Task<List<bs_Contract>> GetContractsAsync(IOrganizationService serviceClient, Guid[] siteBatchArray)
        {
            var query = new QueryExpression(bs_Contract.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression(bs_Contract.Fields.bs_CustomerSiteFK, ConditionOperator.In, siteBatchArray.Cast<object>().ToArray())
                    }
                }
            };
            var result = await Task.Run(() => serviceClient.RetrieveMultiple(query));
            return result.Entities.Select(e => e.ToEntity<bs_Contract>()).ToList();
        }

        private async Task<Dictionary<Guid, List<bs_SiteStatisticDetail>>> GetGroupedSiteStatisticsAsync(IOrganizationService serviceClient, Guid[] siteBatchArray, int year)
        {
            var groupedSiteStatistics = new Dictionary<Guid, List<bs_SiteStatisticDetail>>();
            int pageNumber = 1;
            bool moreRecords;
            string pagingCookie = null;
            do
            {
                var statsQuery = new QueryExpression(bs_SiteStatistic.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(true),
                    Criteria = new FilterExpression(LogicalOperator.And)
                    {
                        Conditions =
                        {
                            new ConditionExpression(bs_SiteStatistic.Fields.bs_CustomerSiteFK, ConditionOperator.In, siteBatchArray.Cast<object>().ToArray()),
                            new ConditionExpression(bs_SiteStatistic.Fields.bs_BillingPeriod, ConditionOperator.BeginsWith, year.ToString())
                        }
                    },
                    PageInfo = new PagingInfo
                    {
                        Count = PageSize,
                        PageNumber = pageNumber,
                        PagingCookie = pagingCookie
                    }
                };
                var linkEntity = statsQuery.AddLink(
                    bs_SiteStatisticDetail.EntityLogicalName,
                    bs_SiteStatistic.Fields.bs_SiteStatisticId,
                    bs_SiteStatisticDetail.Fields.bs_SiteStatisticFK,
                    JoinOperator.Inner
                );
                linkEntity.EntityAlias = "detail";
                linkEntity.Columns = new ColumnSet(true);
                var statsResult = await Task.Run(() => serviceClient.RetrieveMultiple(statsQuery));
                foreach (var entity_SiteStatistic_WithDetailAlias in statsResult.Entities)
                {
                    var customerSiteFk = entity_SiteStatistic_WithDetailAlias.GetAttributeValue<EntityReference>("bs_customersitefk");
                    if (customerSiteFk == null) continue;
                    Guid siteOwnerId = customerSiteFk.Id;
                    var detailAttributes = entity_SiteStatistic_WithDetailAlias.Attributes
                        .Where(a => a.Key.StartsWith("detail."))
                        .ToDictionary(
                            kvp => kvp.Key.Replace("detail.", ""),
                            kvp => kvp.Value
                        );
                    var detailEntity = new Entity(bs_SiteStatisticDetail.EntityLogicalName);
                    if (detailAttributes.Any())
                    {
                        foreach (var attr in detailAttributes)
                        {
                            if (attr.Value is AliasedValue aliasedValue)
                            {
                                if (aliasedValue.Value is EntityReference entityRef)
                                {
                                    detailEntity[attr.Key] = entityRef;
                                }
                                else
                                {
                                    detailEntity[attr.Key] = aliasedValue.Value;
                                }
                            }
                            else
                            {
                                detailEntity[attr.Key] = attr.Value;
                            }
                        }
                    }
                    if (detailEntity.Contains(bs_SiteStatisticDetail.Fields.bs_SiteStatisticDetailId) && detailEntity.GetAttributeValue<Guid>(bs_SiteStatisticDetail.Fields.bs_SiteStatisticDetailId) != Guid.Empty)
                    {
                        if (!groupedSiteStatistics.ContainsKey(siteOwnerId))
                        {
                            groupedSiteStatistics[siteOwnerId] = new List<bs_SiteStatisticDetail>();
                        }
                        groupedSiteStatistics[siteOwnerId].Add(detailEntity.ToEntity<bs_SiteStatisticDetail>());
                    }
                }
                moreRecords = statsResult.MoreRecords;
                if (moreRecords)
                {
                    pageNumber++;
                    pagingCookie = statsResult.PagingCookie;
                }
            } while (moreRecords);
            return groupedSiteStatistics;
        }

        private async Task<List<bs_FixedFeeService>> GetFixedFeesAsync(IOrganizationService serviceClient, List<bs_Contract> contracts)
        {
            if (!contracts.Any()) return new List<bs_FixedFeeService>();
            var query = new QueryExpression(bs_FixedFeeService.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression(bs_FixedFeeService.Fields.bs_ContractFK, ConditionOperator.In, contracts.Select(c => c.Id).Cast<object>().ToArray())
                    }
                }
            };
            var result = await Task.Run(() => serviceClient.RetrieveMultiple(query));
            return result.Entities.Select(e => e.ToEntity<bs_FixedFeeService>()).ToList();
        }

        private async Task<List<bs_LaborHourJob>> GetLaborHourJobsAsync(IOrganizationService serviceClient, List<bs_Contract> contracts)
        {
            if (!contracts.Any()) return new List<bs_LaborHourJob>();
            var query = new QueryExpression(bs_LaborHourJob.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression(bs_LaborHourJob.Fields.bs_ContractFK, ConditionOperator.In, contracts.Select(c => c.Id).Cast<object>().ToArray())
                    }
                }
            };
            var result = await Task.Run(() => serviceClient.RetrieveMultiple(query));
            return result.Entities.Select(e => e.ToEntity<bs_LaborHourJob>()).ToList();
        }

        private async Task<List<bs_RevenueShareThreshold>> GetRevenueShareThresholdsAsync(IOrganizationService serviceClient, List<bs_Contract> contracts)
        {
            if (!contracts.Any()) return new List<bs_RevenueShareThreshold>();
            var query = new QueryExpression(bs_RevenueShareThreshold.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression(bs_RevenueShareThreshold.Fields.bs_Contract, ConditionOperator.In, contracts.Select(c => c.Id).Cast<object>().ToArray())
                    }
                }
            };
            var result = await Task.Run(() => serviceClient.RetrieveMultiple(query));
            return result.Entities.Select(e => e.ToEntity<bs_RevenueShareThreshold>()).ToList();
        }

        private async Task<List<bs_BillableAccount>> GetBillableAccountsAsync(IOrganizationService serviceClient, List<bs_Contract> contracts)
        {
            if (!contracts.Any()) return new List<bs_BillableAccount>();
            var query = new QueryExpression(bs_BillableAccount.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression(bs_BillableAccount.Fields.bs_ContractFK, ConditionOperator.In, contracts.Select(c => c.Id).Cast<object>().ToArray())
                    }
                }
            };
            var result = await Task.Run(() => serviceClient.RetrieveMultiple(query));
            return result.Entities.Select(e => e.ToEntity<bs_BillableAccount>()).ToList();
        }

        private async Task<List<bs_ManagementAgreement>> GetManagementAgreementsAsync(IOrganizationService serviceClient, List<bs_Contract> contracts)
        {
            if (!contracts.Any()) return new List<bs_ManagementAgreement>();
            var query = new QueryExpression(bs_ManagementAgreement.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression(bs_ManagementAgreement.Fields.bs_ContractFK, ConditionOperator.In, contracts.Select(c => c.Id).Cast<object>().ToArray())
                    }
                }
            };
            var result = await Task.Run(() => serviceClient.RetrieveMultiple(query));
            return result.Entities.Select(e => e.ToEntity<bs_ManagementAgreement>()).ToList();
        }

        private async Task<List<bs_OtherRevenueDetail>> GetOtherRevenuesAsync(IOrganizationService serviceClient, Guid[] siteBatchArray, int year)
        {
            var query = new QueryExpression(bs_OtherRevenueDetail.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
                    {
                        new ConditionExpression(bs_OtherRevenueDetail.Fields.bs_CustomerSiteFK, ConditionOperator.In, siteBatchArray.Cast<object>().ToArray())
                    }
                }
            };
            var yearMonths = Enumerable.Range(1, 12).Select(month => $"{year}-{month:D2}").ToArray();
            query.Criteria.AddCondition(bs_OtherRevenueDetail.Fields.bs_MonthYear, ConditionOperator.In, yearMonths.Cast<object>().ToArray());
            var result = await Task.Run(() => serviceClient.RetrieveMultiple(query));
            return result.Entities.Select(e => e.ToEntity<bs_OtherRevenueDetail>()).ToList();
        }

        private async Task<List<bs_NonGLExpense>> GetNonGLExpensesAsync(IOrganizationService serviceClient, List<bs_Contract> contracts)
        {
            if (!contracts.Any()) return new List<bs_NonGLExpense>();
            var query = new QueryExpression(bs_NonGLExpense.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression(LogicalOperator.And)
                {
                    Conditions =
                    {
                        new ConditionExpression(bs_NonGLExpense.Fields.bs_ContractFK, ConditionOperator.In, contracts.Select(c => c.Id).Cast<object>().ToArray())
                    }
                }
            };
            var result = await Task.Run(() => serviceClient.RetrieveMultiple(query));
            return result.Entities.Select(e => e.ToEntity<bs_NonGLExpense>()).ToList();
        }

        private static IEnumerable<List<Guid>> BatchSiteIds(List<Guid> siteIds, int batchSize)
        {
            for (int i = 0; i < siteIds.Count; i += batchSize)
            {
                yield return siteIds.GetRange(i, Math.Min(batchSize, siteIds.Count - i));
            }
        }
    }
}
