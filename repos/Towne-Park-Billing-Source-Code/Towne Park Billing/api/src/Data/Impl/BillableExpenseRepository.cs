using System.Text.Json;
using System.Linq;
using api.Services;
using TownePark;
using TownePark.Models.Vo;
using Microsoft.Xrm.Sdk.Query;
using api.Data; // For interfaces
using api.Models.Vo; // For ExpenseActualsDataVo

namespace api.Data.Impl
{
    public class BillableExpenseRepository : IBillableExpenseRepository
    {
        private readonly IDataverseService _dataverseService;

        public BillableExpenseRepository(IDataverseService dataverseService)
        {
            _dataverseService = dataverseService;
        }

        public decimal GetPayrollExpenseBudget(Guid siteId, int year, int monthOneBased)
        {
            try
            {
                var serviceClient = _dataverseService.GetServiceClient();
                
                // Format period as "yyyy-mm" to match the expected format in bs_period
                var period = $"{year:D4}{monthOneBased:D2}";
                
                // Build query to find the billable expense record for this site and period
                var query = new QueryExpression(bs_BillableExpense.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(bs_BillableExpense.Fields.bs_PayrollExpenseBudget),
                    Criteria = new FilterExpression(LogicalOperator.And)
                };
                
                // Filter by site ID
                query.Criteria.AddCondition(bs_BillableExpense.Fields.bs_SiteId, ConditionOperator.Equal, siteId);
                
                // Filter by period
                query.Criteria.AddCondition(bs_BillableExpense.Fields.bs_Period, ConditionOperator.Equal, period);
                
                var results = serviceClient.RetrieveMultiple(query);
                
                if (results.Entities.Count > 0)
                {
                    var billableExpense = results.Entities[0].ToEntity<bs_BillableExpense>();
                    return billableExpense.bs_PayrollExpenseBudget ?? 0m;
                }
                
                return 0m;
            }
            catch (Exception)
            {
                // Log the exception if logging is available
                // For now, return 0 to prevent calculation failures
                return 0m;
            }
        }

        public decimal GetBillableExpenseBudget(Guid siteId, int year, int monthOneBased)
        {
            try
            {
                var serviceClient = _dataverseService.GetServiceClient();
                
                // Format period as "YYYYMM" to match the expected format in bs_period
                var period = $"{year:D4}{monthOneBased:D2}";
                
                // Build query to find the billable expense record for this site and period
                var query = new QueryExpression(bs_BillableExpense.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(bs_BillableExpense.Fields.bs_BillableExpenseBudget),
                    Criteria = new FilterExpression(LogicalOperator.And)
                };
                
                // Filter by site ID
                query.Criteria.AddCondition(bs_BillableExpense.Fields.bs_SiteId, ConditionOperator.Equal, siteId);
                
                // Filter by period
                query.Criteria.AddCondition(bs_BillableExpense.Fields.bs_Period, ConditionOperator.Equal, period);
                
                var results = serviceClient.RetrieveMultiple(query);
                
                if (results.Entities.Count > 0)
                {
                    var billableExpense = results.Entities[0].ToEntity<bs_BillableExpense>();
                    return billableExpense.bs_BillableExpenseBudget ?? 0m;
                }
                
                return 0m;
            }
            catch (Exception)
            {
                // Log the exception if logging is available
                // For now, return 0 to prevent calculation failures
                return 0m;
            }
        }

        public decimal GetOtherExpenseBudget(Guid siteId, int year, int monthOneBased)
        {
            try
            {
                var serviceClient = _dataverseService.GetServiceClient();
                
                // Format period as "YYYYMM" to match the expected format in bs_period
                var period = $"{year:D4}{monthOneBased:D2}";
                
                // Build query to find the billable expense record for this site and period
                var query = new QueryExpression(bs_BillableExpense.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(bs_BillableExpense.Fields.bs_OtherExpenseBudget),
                    Criteria = new FilterExpression(LogicalOperator.And)
                };
                
                // Filter by site ID
                query.Criteria.AddCondition(bs_BillableExpense.Fields.bs_SiteId, ConditionOperator.Equal, siteId);
                
                // Filter by period
                query.Criteria.AddCondition(bs_BillableExpense.Fields.bs_Period, ConditionOperator.Equal, period);
                
                var results = serviceClient.RetrieveMultiple(query);
                
                if (results.Entities.Count > 0)
                {
                    var billableExpense = results.Entities[0].ToEntity<bs_BillableExpense>();
                    return billableExpense.bs_OtherExpenseBudget ?? 0m;
                }
                
                return 0m;
            }
            catch (Exception)
            {
                // Log the exception if logging is available
                // For now, return 0 to prevent calculation failures
                return 0m;
            }
        }

        public decimal GetVehicleInsuranceBudget(Guid siteId, int year, int monthOneBased)
        {
            try
            {
                var serviceClient = _dataverseService.GetServiceClient();
                
                // Format period as "YYYYMM" to match the expected format in bs_period
                var period = $"{year:D4}{monthOneBased:D2}";
                
                // Build query to find the billable expense record for this site and period
                var query = new QueryExpression(bs_BillableExpense.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(bs_BillableExpense.Fields.bs_VehicleInsuranceBudget),
                    Criteria = new FilterExpression(LogicalOperator.And)
                };
                
                // Filter by site ID
                query.Criteria.AddCondition(bs_BillableExpense.Fields.bs_SiteId, ConditionOperator.Equal, siteId);
                
                // Filter by period
                query.Criteria.AddCondition(bs_BillableExpense.Fields.bs_Period, ConditionOperator.Equal, period);
                
                var results = serviceClient.RetrieveMultiple(query);
                
                if (results.Entities.Count > 0)
                {
                    var billableExpense = results.Entities[0].ToEntity<bs_BillableExpense>();
                    return billableExpense.bs_VehicleInsuranceBudget ?? 0m;
                }
                
                return 0m;
            }
            catch (Exception)
            {
                // Log the exception if logging is available
                // For now, return 0 to prevent calculation failures
                return 0m;
            }
        }

        public Dictionary<Guid, decimal> GetVehicleInsuranceBudgetForSites(List<Guid> siteIds, int year, int monthOneBased)
        {
            var result = new Dictionary<Guid, decimal>();
            try
            {
                var serviceClient = _dataverseService.GetServiceClient();
                var period = $"{year:D4}{monthOneBased:D2}";

                var query = new QueryExpression(bs_BillableExpense.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(bs_BillableExpense.Fields.bs_SiteId, bs_BillableExpense.Fields.bs_VehicleInsuranceBudget),
                    Criteria = new FilterExpression(LogicalOperator.And)
                };

                query.Criteria.AddCondition(bs_BillableExpense.Fields.bs_Period, ConditionOperator.Equal, period);
                if (siteIds != null && siteIds.Count > 0)
                {
                    query.Criteria.AddCondition(bs_BillableExpense.Fields.bs_SiteId, ConditionOperator.In, siteIds.Cast<object>().ToArray());
                }

                var results = serviceClient.RetrieveMultiple(query);
                foreach (var e in results.Entities)
                {
                    var be = e.ToEntity<bs_BillableExpense>();
                    var sid = be.bs_SiteId?.Id;
                    if (sid.HasValue)
                    {
                        result[sid.Value] = be.bs_VehicleInsuranceBudget ?? 0m;
                    }
                }

                // Ensure all requested sites have an entry
                if (siteIds != null)
                {
                    foreach (var sid in siteIds)
                    {
                        if (!result.ContainsKey(sid)) result[sid] = 0m;
                    }
                }
            }
            catch
            {
                if (siteIds != null)
                {
                    foreach (var sid in siteIds)
                    {
                        if (!result.ContainsKey(sid)) result[sid] = 0m;
                    }
                }
            }
            return result;
        }

        public Dictionary<(Guid siteId, int monthOneBased), decimal> GetVehicleInsuranceBudgetForSitesForYear(List<Guid> siteIds, int year)
        {
            var result = new Dictionary<(Guid, int), decimal>();
            try
            {
                var serviceClient = _dataverseService.GetServiceClient();
                // Query all months for the year for provided sites
                var periodStart = $"{year:D4}01";
                var periodEnd = $"{year:D4}12";

                var query = new QueryExpression(bs_BillableExpense.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(
                        bs_BillableExpense.Fields.bs_SiteId,
                        bs_BillableExpense.Fields.bs_Period,
                        bs_BillableExpense.Fields.bs_VehicleInsuranceBudget
                    ),
                    Criteria = new FilterExpression(LogicalOperator.And)
                };

                if (siteIds != null && siteIds.Count > 0)
                {
                    query.Criteria.AddCondition(bs_BillableExpense.Fields.bs_SiteId, ConditionOperator.In, siteIds.Cast<object>().ToArray());
                }
                query.Criteria.AddCondition(bs_BillableExpense.Fields.bs_Period, ConditionOperator.GreaterEqual, periodStart);
                query.Criteria.AddCondition(bs_BillableExpense.Fields.bs_Period, ConditionOperator.LessEqual, periodEnd);

                var results = serviceClient.RetrieveMultiple(query);
                foreach (var e in results.Entities)
                {
                    var be = e.ToEntity<bs_BillableExpense>();
                    var sid = be.bs_SiteId?.Id;
                    var period = be.bs_Period;
                    if (sid.HasValue && !string.IsNullOrWhiteSpace(period) && period.Length >= 6)
                    {
                        var monthPart = int.Parse(period.Substring(4, 2));
                        result[(sid.Value, monthPart)] = be.bs_VehicleInsuranceBudget ?? 0m;
                    }
                }

                // Ensure all requested (site, month) combos exist
                if (siteIds != null)
                {
                    for (int m = 1; m <= 12; m++)
                    {
                        foreach (var sid in siteIds)
                        {
                            var key = (sid, m);
                            if (!result.ContainsKey(key)) result[key] = 0m;
                        }
                    }
                }
            }
            catch
            {
                if (siteIds != null)
                {
                    for (int m = 1; m <= 12; m++)
                    {
                        foreach (var sid in siteIds)
                        {
                            var key = (sid, m);
                            if (!result.ContainsKey(key)) result[key] = 0m;
                        }
                    }
                }
            }
            return result;
        }

        public decimal GetClaimsBudgetForPeriodRange(Guid siteId, string startPeriod, string endPeriod)
        {
            try
            {
                var serviceClient = _dataverseService.GetServiceClient();
                
                var query = new QueryExpression(bs_BillableExpense.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(bs_BillableExpense.Fields.bs_ClaimsBudget),
                    Criteria = new FilterExpression(LogicalOperator.And)
                };
                
                // Filter by site ID
                query.Criteria.AddCondition(bs_BillableExpense.Fields.bs_SiteId, ConditionOperator.Equal, siteId);
                
                // Filter by period range (string comparison works for YYYYMM format)
                query.Criteria.AddCondition(bs_BillableExpense.Fields.bs_Period, ConditionOperator.GreaterEqual, startPeriod);
                query.Criteria.AddCondition(bs_BillableExpense.Fields.bs_Period, ConditionOperator.LessEqual, endPeriod);
                
                // Only active records
                query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
                
                var results = serviceClient.RetrieveMultiple(query);
                
                return results.Entities
                    .Select(e => e.ToEntity<bs_BillableExpense>())
                    .Sum(be => be.bs_ClaimsBudget ?? 0m);
            }
            catch (Exception)
            {
                // Log the exception if logging is available
                return 0m;
            }
        }

        public decimal GetClaimsBudgetForPeriod(Guid siteId, string period)
        {
            try
            {
                var serviceClient = _dataverseService.GetServiceClient();
                
                var query = new QueryExpression(bs_BillableExpense.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(bs_BillableExpense.Fields.bs_ClaimsBudget),
                    Criteria = new FilterExpression(LogicalOperator.And)
                };
                
                query.Criteria.AddCondition(bs_BillableExpense.Fields.bs_SiteId, ConditionOperator.Equal, siteId);
                query.Criteria.AddCondition(bs_BillableExpense.Fields.bs_Period, ConditionOperator.Equal, period);
                query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
                
                var results = serviceClient.RetrieveMultiple(query);
                
                if (results.Entities.Count > 0)
                {
                    var billableExpense = results.Entities[0].ToEntity<bs_BillableExpense>();
                    return billableExpense.bs_ClaimsBudget ?? 0m;
                }
                
                return 0m;
            }
            catch (Exception)
            {
                return 0m;
            }
        }

        public ExpenseActualsDataVo[] GetExpenseActualsForSites(List<Guid> siteIds, int year, int monthOneBased)
        {
            try
            {
                if (siteIds == null || !siteIds.Any())
                {
                    return Array.Empty<ExpenseActualsDataVo>();
                }

                var serviceClient = _dataverseService.GetServiceClient();
                
                // Format period as "YYYYMM" to match the expected format in bs_period
                var period = $"{year:D4}{monthOneBased:D2}";
                
                // Build query to find the billable expense records for multiple sites and period
                var query = new QueryExpression(bs_BillableExpense.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(
                        bs_BillableExpense.Fields.bs_SiteId, 
                        bs_BillableExpense.Fields.bs_BillableExpenseActuals,
                        bs_BillableExpense.Fields.bs_OtherExpenseActuals),
                    Criteria = new FilterExpression(LogicalOperator.And)
                };
                
                // Filter by multiple site IDs
                query.Criteria.AddCondition(bs_BillableExpense.Fields.bs_SiteId, ConditionOperator.In, siteIds.Cast<object>().ToArray());
                
                // Filter by period
                query.Criteria.AddCondition(bs_BillableExpense.Fields.bs_Period, ConditionOperator.Equal, period);
                
                var results = serviceClient.RetrieveMultiple(query);
                
                var expenseActualsList = new List<ExpenseActualsDataVo>();
                
                // Create a dictionary to track which sites we found data for
                var foundSites = new HashSet<Guid>();
                
                // Process results from database
                foreach (var entity in results.Entities)
                {
                    var billableExpense = entity.ToEntity<bs_BillableExpense>();
                    var siteId = billableExpense.bs_SiteId?.Id ?? Guid.Empty;
                    if (siteId != Guid.Empty)
                    {
                        expenseActualsList.Add(new ExpenseActualsDataVo
                        {
                            SiteId = siteId,
                            BillableExpenseActuals = billableExpense.bs_BillableExpenseActuals ?? 0m,
                            OtherExpenseActuals = billableExpense.bs_OtherExpenseActuals ?? 0m
                        });
                        foundSites.Add(siteId);
                    }
                }
                
                // Add entries for sites that weren't found in the database (with 0 values)
                foreach (var siteId in siteIds)
                {
                    if (!foundSites.Contains(siteId))
                    {
                        expenseActualsList.Add(new ExpenseActualsDataVo
                        {
                            SiteId = siteId,
                            BillableExpenseActuals = 0m,
                            OtherExpenseActuals = 0m
                        });
                    }
                }
                
                return expenseActualsList.ToArray();
            }
            catch (Exception)
            {
                // Log the exception if logging is available
                // Return empty array to prevent calculation failures
                return Array.Empty<ExpenseActualsDataVo>();
            }
        }

        public List<string> GetEnabledExpenseAccounts(Guid siteId)
        {
            try
            {
                var serviceClient = _dataverseService.GetServiceClient();
                
                // Query billable account for this site via contract
                var query = new QueryExpression(bs_BillableAccount.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(bs_BillableAccount.Fields.bs_ExpenseAccountsData),
                    LinkEntities =
                    {
                        new LinkEntity
                        {
                            LinkFromEntityName = bs_BillableAccount.EntityLogicalName,
                            LinkFromAttributeName = bs_BillableAccount.Fields.bs_ContractFK,
                            LinkToEntityName = bs_Contract.EntityLogicalName,
                            LinkToAttributeName = bs_Contract.Fields.bs_ContractId,
                            LinkCriteria = new FilterExpression(LogicalOperator.And)
                            {
                                Conditions =
                                {
                                    new ConditionExpression(bs_Contract.Fields.bs_CustomerSiteFK, ConditionOperator.Equal, siteId)
                                }
                            }
                        }
                    }
                };
                
                var results = serviceClient.RetrieveMultiple(query);
                
                if (results.Entities.Count > 0)
                {
                    var billableAccount = results.Entities[0].ToEntity<bs_BillableAccount>();
                    return ParseEnabledExpenseAccounts(billableAccount.bs_ExpenseAccountsData);
                }
                
                return new List<string>();
            }
            catch (Exception)
            {
                // Log the exception if logging is available
                // For now, return empty list to prevent calculation failures
                return new List<string>();
            }
        }

        private List<string> ParseEnabledExpenseAccounts(string? expenseAccountsData)
        {
            if (string.IsNullOrEmpty(expenseAccountsData))
            {
                return new List<string>();
            }

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var expenseAccounts = JsonSerializer.Deserialize<List<ExpenseAccountConfigVo>>(expenseAccountsData, options);
                return expenseAccounts?
                    .Where(account => account.IsEnabled)
                    .Select(account => account.Code)
                    .Where(code => !string.IsNullOrEmpty(code))
                    .ToList() ?? new List<string>();
            }
            catch (Exception)
            {
                // Handle malformed JSON gracefully
                return new List<string>();
            }
        }
    }
} 