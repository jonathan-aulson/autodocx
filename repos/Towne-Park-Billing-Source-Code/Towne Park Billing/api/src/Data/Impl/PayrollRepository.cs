using api.Services;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
using System.ServiceModel;
using TownePark;
using api.Models.Common;
using api.Models.Vo;

namespace api.Data.Impl
{
    public class PayrollRepository : IPayrollRepository
    {
        private readonly IDataverseService _dataverseService;

        public PayrollRepository(IDataverseService dataverseService)
        {
            _dataverseService = dataverseService;
        }

        public bs_Payroll? GetPayroll(Guid siteId, string billingPeriod)
        {
            var serviceClient = _dataverseService.GetServiceClient();

            var query = new QueryExpression(bs_Payroll.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(
                    bs_Payroll.Fields.bs_PayrollId,
                    bs_Payroll.Fields.bs_CustomerSiteFK,
                    bs_Payroll.Fields.bs_Period,
                    bs_Payroll.Fields.bs_Name,
                    bs_Payroll.Fields.bs_PayrollForecastMode
                ),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression(
                            bs_Payroll.Fields.bs_CustomerSiteFK,
                            ConditionOperator.Equal,
                            siteId
                            ),
                        new ConditionExpression(
                            bs_Payroll.Fields.bs_Period,
                            ConditionOperator.Equal,
                            billingPeriod
                            )
                    }
                },
                PageInfo = new PagingInfo
                {
                    Count = 1, // Only retrieve one record
                    PageNumber = 1
                }
            };

            var result = serviceClient.RetrieveMultiple(query);

            if (result.Entities.Count == 0)
                return null;

            var payroll = result.Entities[0].ToEntity<bs_Payroll>();

            var detailsQuery = new QueryExpression(bs_PayrollDetail.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(
                    bs_PayrollDetail.Fields.bs_PayrollDetailId,
                    bs_PayrollDetail.Fields.bs_PayrollFK,
                    bs_PayrollDetail.Fields.bs_DisplayName,
                    bs_PayrollDetail.Fields.bs_RegularHours,
                    bs_PayrollDetail.Fields.bs_Date,
                    bs_PayrollDetail.Fields.bs_JobCodeFK,
                    bs_PayrollDetail.Fields.bs_ForecastPayrollCost,
                    bs_PayrollDetail.Fields.bs_ForecastPayrollRevenue
                ),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression(
                            bs_PayrollDetail.Fields.bs_PayrollFK,
                            ConditionOperator.Equal,
                            payroll.Id
                            )
                    }
                }
            };

            var detailsResult = serviceClient.RetrieveMultiple(detailsQuery);

            // Get job codes assigned to this site from JobCodesBySite
            var jobCodesBySiteQuery = new QueryExpression("bs_jobcodesbysite")
            {
                ColumnSet = new ColumnSet("bs_jobcode"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("bs_customersite", ConditionOperator.Equal, siteId)
                    }
                }
            };
            var jobCodesBySiteResult = serviceClient.RetrieveMultiple(jobCodesBySiteQuery);
            var assignedJobCodeIds = jobCodesBySiteResult.Entities
                .Where(e => e.Contains("bs_jobcode") && e["bs_jobcode"] is EntityReference)
                .Select(e => ((EntityReference)e["bs_jobcode"]).Id)
                .ToHashSet();

            // Filter payroll details to only those with JobCodeFK assigned to the site
            var filteredDetails = detailsResult.Entities
                .Where(e =>
                    e.Contains(bs_PayrollDetail.Fields.bs_JobCodeFK) &&
                    assignedJobCodeIds.Contains(((EntityReference)e[bs_PayrollDetail.Fields.bs_JobCodeFK]).Id)
                )
                .ToList();

            // Optionally, retrieve related JobCode and JobGroup entities for display fields
            var jobCodeIds = filteredDetails
                .Where(e => e.Contains(bs_PayrollDetail.Fields.bs_JobCodeFK))
                .Select(e => ((EntityReference)e[bs_PayrollDetail.Fields.bs_JobCodeFK]).Id)
                .Distinct()
                .ToList();

            var jobCodeDict = new Dictionary<Guid, Entity>();
            var jobGroupDict = new Dictionary<Guid, Entity>();
            if (jobCodeIds.Any())
            {
                var jobCodeQuery = new QueryExpression("bs_jobcode")
                {
                    ColumnSet = new ColumnSet("bs_jobcodeid", "bs_jobcode", "bs_jobtitle", "bs_jobgroupfk"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression("bs_jobcodeid", ConditionOperator.In, jobCodeIds.Cast<object>().ToArray())
                        }
                    }
                };
                var jobCodeResults = serviceClient.RetrieveMultiple(jobCodeQuery);
                foreach (var entity in jobCodeResults.Entities)
                {
                    jobCodeDict[entity.Id] = entity;
                    // Collect job group from job code
                    if (entity.Contains("bs_jobgroupfk") && entity["bs_jobgroupfk"] is EntityReference groupRef)
                    {
                        if (!jobGroupDict.ContainsKey(groupRef.Id))
                        {
                            // Fetch job group entity if not already fetched
                            var jobGroupEntity = serviceClient.Retrieve("bs_jobgroup", groupRef.Id, new ColumnSet("bs_jobgroupid", "bs_jobgrouptitle"));
                            if (jobGroupEntity != null)
                            {
                                jobGroupDict[groupRef.Id] = jobGroupEntity;
                            }
                        }
                    }
                }
            }

            // Transform the filtered details only if there are any
            var payrollDetails = filteredDetails
                .Select(e =>
                {
                    var detail = e.ToEntity<bs_PayrollDetail>();

                    // Attach extra fields for aggregation
                    if (e.Contains(bs_PayrollDetail.Fields.bs_JobCodeFK) && e[bs_PayrollDetail.Fields.bs_JobCodeFK] is EntityReference codeRef)
                    {
                        if (jobCodeDict.TryGetValue(codeRef.Id, out var codeEntity))
                        {
                            detail["jobcode_display"] = codeEntity.GetAttributeValue<string>("bs_jobcode");
                            detail["jobcode_displayname"] = codeEntity.GetAttributeValue<string>("bs_jobtitle");
                            // Attach job group from job code
                            var jobGroupRef = codeEntity.Contains("bs_jobgroupfk") && codeEntity["bs_jobgroupfk"] is EntityReference gr ? gr.Id : (Guid?)null;
                            if (jobGroupRef.HasValue && jobGroupDict.TryGetValue(jobGroupRef.Value, out var groupEntity))
                            {
                                detail["jobgroup_title"] = groupEntity.GetAttributeValue<string>("bs_jobgrouptitle");
                            }
                        }
                    }
                    return detail;
                })
                .ToList();

            // Only set the property if there are details, otherwise leave it null to avoid generated entity setter issues
            if (payrollDetails.Any())
            {
                payroll.bs_PayrollDetail_Payroll = payrollDetails;
            }
            // If there are no details, we'll leave bs_PayrollDetail_Payroll as null
            // The service layer should handle null collections appropriately

            return payroll;
        }

    

        public async Task<Dictionary<Guid, bs_Payroll>> GetPayrollBatchAsync(List<Guid> siteIds, string billingPeriod)
        {
            var result = new Dictionary<Guid, bs_Payroll>();
            
            if (siteIds == null || !siteIds.Any())
                return result;

            var serviceClient = _dataverseService.GetServiceClient();

            // Query for all payroll records for the given sites and period
            var query = new QueryExpression(bs_Payroll.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(
                    bs_Payroll.Fields.bs_PayrollId,
                    bs_Payroll.Fields.bs_CustomerSiteFK,
                    bs_Payroll.Fields.bs_Period,
                    bs_Payroll.Fields.bs_Name,
                    bs_Payroll.Fields.bs_PayrollForecastMode
                ),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression(
                            bs_Payroll.Fields.bs_CustomerSiteFK,
                            ConditionOperator.In,
                            siteIds.Cast<object>().ToArray()
                        ),
                        new ConditionExpression(
                            bs_Payroll.Fields.bs_Period,
                            ConditionOperator.Equal,
                            billingPeriod
                        )
                    }
                }
            };

            var payrollResults = await Task.Run(() => serviceClient.RetrieveMultiple(query));

            if (payrollResults.Entities.Count == 0)
                return result;

            // Get all payroll IDs for detail query
            var payrollIds = payrollResults.Entities.Select(e => e.Id).ToList();

            // Query for all payroll details for all payrolls in one batch
            var detailsQuery = new QueryExpression(bs_PayrollDetail.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(
                    bs_PayrollDetail.Fields.bs_PayrollDetailId,
                    bs_PayrollDetail.Fields.bs_PayrollFK,
                    bs_PayrollDetail.Fields.bs_DisplayName,
                    bs_PayrollDetail.Fields.bs_RegularHours,
                    bs_PayrollDetail.Fields.bs_Date,
                    bs_PayrollDetail.Fields.bs_JobCodeFK,
                    bs_PayrollDetail.Fields.bs_ForecastPayrollCost,
                    bs_PayrollDetail.Fields.bs_ForecastPayrollRevenue
                ),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression(
                            bs_PayrollDetail.Fields.bs_PayrollFK,
                            ConditionOperator.In,
                            payrollIds.Cast<object>().ToArray()
                        )
                    }
                }
            };

            var detailsResult = await Task.Run(() => serviceClient.RetrieveMultiple(detailsQuery));

            // Get all unique site IDs from payroll records to query job codes
            var actualSiteIds = payrollResults.Entities
                .Where(e => e.Contains(bs_Payroll.Fields.bs_CustomerSiteFK))
                .Select(e => ((EntityReference)e[bs_Payroll.Fields.bs_CustomerSiteFK]).Id)
                .Distinct()
                .ToList();

            // Get job codes assigned to these sites
            var jobCodesBySiteQuery = new QueryExpression("bs_jobcodesbysite")
            {
                ColumnSet = new ColumnSet("bs_jobcode", "bs_customersite"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("bs_customersite", ConditionOperator.In, actualSiteIds.Cast<object>().ToArray())
                    }
                }
            };
            
            var jobCodesBySiteResult = await Task.Run(() => serviceClient.RetrieveMultiple(jobCodesBySiteQuery));
            
            // Create a lookup of job codes by site
            var jobCodesBySite = new Dictionary<Guid, HashSet<Guid>>();
            foreach (var entity in jobCodesBySiteResult.Entities)
            {
                if (entity.Contains("bs_customersite") && entity["bs_customersite"] is EntityReference siteRef &&
                    entity.Contains("bs_jobcode") && entity["bs_jobcode"] is EntityReference jobCodeRef)
                {
                    if (!jobCodesBySite.ContainsKey(siteRef.Id))
                        jobCodesBySite[siteRef.Id] = new HashSet<Guid>();
                    
                    jobCodesBySite[siteRef.Id].Add(jobCodeRef.Id);
                }
            }

            // Get all unique job code IDs from details
            var jobCodeIds = detailsResult.Entities
                .Where(e => e.Contains(bs_PayrollDetail.Fields.bs_JobCodeFK))
                .Select(e => ((EntityReference)e[bs_PayrollDetail.Fields.bs_JobCodeFK]).Id)
                .Distinct()
                .ToList();

            // Batch fetch job codes and job groups
            var jobCodeDict = new Dictionary<Guid, Entity>();
            var jobGroupDict = new Dictionary<Guid, Entity>();
            
            if (jobCodeIds.Any())
            {
                var jobCodeQuery = new QueryExpression("bs_jobcode")
                {
                    ColumnSet = new ColumnSet("bs_jobcodeid", "bs_jobcode", "bs_jobtitle", "bs_jobgroupfk"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression("bs_jobcodeid", ConditionOperator.In, jobCodeIds.Cast<object>().ToArray())
                        }
                    }
                };
                
                var jobCodeResults = await Task.Run(() => serviceClient.RetrieveMultiple(jobCodeQuery));
                
                // Collect job codes and unique job group IDs
                var jobGroupIds = new HashSet<Guid>();
                foreach (var entity in jobCodeResults.Entities)
                {
                    jobCodeDict[entity.Id] = entity;
                    if (entity.Contains("bs_jobgroupfk") && entity["bs_jobgroupfk"] is EntityReference groupRef)
                    {
                        jobGroupIds.Add(groupRef.Id);
                    }
                }

                // Batch fetch job groups
                if (jobGroupIds.Any())
                {
                    var jobGroupQuery = new QueryExpression("bs_jobgroup")
                    {
                        ColumnSet = new ColumnSet("bs_jobgroupid", "bs_jobgrouptitle"),
                        Criteria = new FilterExpression
                        {
                            Conditions =
                            {
                                new ConditionExpression("bs_jobgroupid", ConditionOperator.In, jobGroupIds.Cast<object>().ToArray())
                            }
                        }
                    };
                    
                    var jobGroupResults = await Task.Run(() => serviceClient.RetrieveMultiple(jobGroupQuery));
                    foreach (var entity in jobGroupResults.Entities)
                    {
                        jobGroupDict[entity.Id] = entity;
                    }
                }
            }

            // Group details by payroll ID
            var detailsByPayrollId = detailsResult.Entities
                .Where(e => e.Contains(bs_PayrollDetail.Fields.bs_PayrollFK))
                .GroupBy(e => ((EntityReference)e[bs_PayrollDetail.Fields.bs_PayrollFK]).Id)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Process each payroll record
            foreach (var payrollEntity in payrollResults.Entities)
            {
                var payroll = payrollEntity.ToEntity<bs_Payroll>();
                var siteId = ((EntityReference)payrollEntity[bs_Payroll.Fields.bs_CustomerSiteFK]).Id;
                
                // Get the assigned job codes for this site
                var assignedJobCodeIds = jobCodesBySite.ContainsKey(siteId) ? jobCodesBySite[siteId] : new HashSet<Guid>();

                // Get details for this payroll
                if (detailsByPayrollId.TryGetValue(payroll.Id, out var payrollDetails))
                {
                    // Filter to only include details with job codes assigned to the site
                    var filteredDetails = payrollDetails
                        .Where(e =>
                            e.Contains(bs_PayrollDetail.Fields.bs_JobCodeFK) &&
                            assignedJobCodeIds.Contains(((EntityReference)e[bs_PayrollDetail.Fields.bs_JobCodeFK]).Id)
                        )
                        .Select(e =>
                        {
                            var detail = e.ToEntity<bs_PayrollDetail>();

                            // Attach extra fields for aggregation
                            if (e.Contains(bs_PayrollDetail.Fields.bs_JobCodeFK) && e[bs_PayrollDetail.Fields.bs_JobCodeFK] is EntityReference codeRef)
                            {
                                if (jobCodeDict.TryGetValue(codeRef.Id, out var codeEntity))
                                {
                                    detail["jobcode_display"] = codeEntity.GetAttributeValue<string>("bs_jobcode");
                                    detail["jobcode_displayname"] = codeEntity.GetAttributeValue<string>("bs_jobtitle");
                                    
                                    // Attach job group from job code
                                    var jobGroupRef = codeEntity.Contains("bs_jobgroupfk") && codeEntity["bs_jobgroupfk"] is EntityReference gr ? gr.Id : (Guid?)null;
                                    if (jobGroupRef.HasValue && jobGroupDict.TryGetValue(jobGroupRef.Value, out var groupEntity))
                                    {
                                        detail["jobgroup_title"] = groupEntity.GetAttributeValue<string>("bs_jobgrouptitle");
                                    }
                                }
                            }
                            return detail;
                        })
                        .ToList();

                    if (filteredDetails.Any())
                    {
                        payroll.bs_PayrollDetail_Payroll = filteredDetails;
                    }
                }

                result[siteId] = payroll;
            }

            return result;
        }

        // Removed per-year sum method; using GetPayrollBatchForYearAsync and summing in service instead

        public async Task<Dictionary<string, Dictionary<Guid, bs_Payroll>>> GetPayrollBatchForYearAsync(List<Guid> siteIds, int year)
        {
            var result = new Dictionary<string, Dictionary<Guid, bs_Payroll>>();
            if (siteIds == null || siteIds.Count == 0)
                return result;

            var serviceClient = _dataverseService.GetServiceClient();

            // Build list of periods for the year (YYYY-MM)
            var periods = Enumerable.Range(1, 12)
                .Select(m => $"{year:D4}-{m:D2}")
                .ToArray();

            // Query all payrolls for all requested sites and periods in one go
            var payrollQuery = new QueryExpression(bs_Payroll.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(
                    bs_Payroll.Fields.bs_PayrollId,
                    bs_Payroll.Fields.bs_CustomerSiteFK,
                    bs_Payroll.Fields.bs_Period,
                    bs_Payroll.Fields.bs_Name,
                    bs_Payroll.Fields.bs_PayrollForecastMode
                ),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression(
                            bs_Payroll.Fields.bs_CustomerSiteFK,
                            ConditionOperator.In,
                            siteIds.Cast<object>().ToArray()
                        ),
                        new ConditionExpression(
                            bs_Payroll.Fields.bs_Period,
                            ConditionOperator.In,
                            periods
                        )
                    }
                }
            };

            var payrollResults = await Task.Run(() => serviceClient.RetrieveMultiple(payrollQuery));
            if (payrollResults.Entities.Count == 0)
                return result;

            // Map payroll metadata and collect payroll IDs
            var payrollMeta = new Dictionary<Guid, (Guid siteId, string period)>();
            foreach (var entity in payrollResults.Entities)
            {
                var siteRef = (EntityReference)entity[bs_Payroll.Fields.bs_CustomerSiteFK];
                var period = (string)entity[bs_Payroll.Fields.bs_Period];
                payrollMeta[entity.Id] = (siteRef.Id, period);
            }

            var payrollIds = payrollMeta.Keys.ToList();

            // Query all details for these payrolls in a single batch
            var detailsQuery = new QueryExpression(bs_PayrollDetail.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(
                    bs_PayrollDetail.Fields.bs_PayrollDetailId,
                    bs_PayrollDetail.Fields.bs_PayrollFK,
                    bs_PayrollDetail.Fields.bs_DisplayName,
                    bs_PayrollDetail.Fields.bs_RegularHours,
                    bs_PayrollDetail.Fields.bs_Date,
                    bs_PayrollDetail.Fields.bs_JobCodeFK,
                    bs_PayrollDetail.Fields.bs_ForecastPayrollCost,
                    bs_PayrollDetail.Fields.bs_ForecastPayrollRevenue
                ),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression(
                            bs_PayrollDetail.Fields.bs_PayrollFK,
                            ConditionOperator.In,
                            payrollIds.Cast<object>().ToArray()
                        )
                    }
                }
            };

            var detailsResult = await Task.Run(() => serviceClient.RetrieveMultiple(detailsQuery));

            // Get job codes assigned to each site to filter details
            var jobCodesBySiteQuery = new QueryExpression("bs_jobcodesbysite")
            {
                ColumnSet = new ColumnSet("bs_jobcode", "bs_customersite"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("bs_customersite", ConditionOperator.In, siteIds.Cast<object>().ToArray())
                    }
                }
            };
            var jobCodesBySiteResult = await Task.Run(() => serviceClient.RetrieveMultiple(jobCodesBySiteQuery));
            var jobCodesBySite = new Dictionary<Guid, HashSet<Guid>>();
            foreach (var entity in jobCodesBySiteResult.Entities)
            {
                if (entity.Contains("bs_customersite") && entity["bs_customersite"] is EntityReference siteRef &&
                    entity.Contains("bs_jobcode") && entity["bs_jobcode"] is EntityReference jobCodeRef)
                {
                    if (!jobCodesBySite.ContainsKey(siteRef.Id))
                        jobCodesBySite[siteRef.Id] = new HashSet<Guid>();
                    jobCodesBySite[siteRef.Id].Add(jobCodeRef.Id);
                }
            }

            // Optionally enrich details with job code and job group display fields (batch fetch similar to monthly)
            var jobCodeIds = detailsResult.Entities
                .Where(e => e.Contains(bs_PayrollDetail.Fields.bs_JobCodeFK))
                .Select(e => ((EntityReference)e[bs_PayrollDetail.Fields.bs_JobCodeFK]).Id)
                .Distinct()
                .ToList();

            var jobCodeDict = new Dictionary<Guid, Entity>();
            var jobGroupDict = new Dictionary<Guid, Entity>();
            if (jobCodeIds.Any())
            {
                var jobCodeQuery = new QueryExpression("bs_jobcode")
                {
                    ColumnSet = new ColumnSet("bs_jobcodeid", "bs_jobcode", "bs_jobtitle", "bs_jobgroupfk"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression("bs_jobcodeid", ConditionOperator.In, jobCodeIds.Cast<object>().ToArray())
                        }
                    }
                };
                var jobCodeResults = await Task.Run(() => serviceClient.RetrieveMultiple(jobCodeQuery));
                var jobGroupIds = new HashSet<Guid>();
                foreach (var entity in jobCodeResults.Entities)
                {
                    jobCodeDict[entity.Id] = entity;
                    if (entity.Contains("bs_jobgroupfk") && entity["bs_jobgroupfk"] is EntityReference groupRef)
                    {
                        jobGroupIds.Add(groupRef.Id);
                    }
                }
                if (jobGroupIds.Any())
                {
                    var jobGroupQuery = new QueryExpression("bs_jobgroup")
                    {
                        ColumnSet = new ColumnSet("bs_jobgroupid", "bs_jobgrouptitle"),
                        Criteria = new FilterExpression
                        {
                            Conditions =
                            {
                                new ConditionExpression("bs_jobgroupid", ConditionOperator.In, jobGroupIds.Cast<object>().ToArray())
                            }
                        }
                    };
                    var jobGroupResults = await Task.Run(() => serviceClient.RetrieveMultiple(jobGroupQuery));
                    foreach (var entity in jobGroupResults.Entities)
                    {
                        jobGroupDict[entity.Id] = entity;
                    }
                }
            }

            // Group details by payroll and filter to assigned job codes
            var detailsByPayrollId = detailsResult.Entities
                .Where(e => e.Contains(bs_PayrollDetail.Fields.bs_PayrollFK))
                .GroupBy(e => ((EntityReference)e[bs_PayrollDetail.Fields.bs_PayrollFK]).Id)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var pr in payrollResults.Entities)
            {
                var payrollId = pr.Id;
                var period = (string)pr[bs_Payroll.Fields.bs_Period];
                var siteId = ((EntityReference)pr[bs_Payroll.Fields.bs_CustomerSiteFK]).Id;
                var payroll = pr.ToEntity<bs_Payroll>();

                // Attach filtered/enriched details
                if (detailsByPayrollId.TryGetValue(payrollId, out var rawDetails))
                {
                    var allowedCodes = jobCodesBySite.ContainsKey(siteId) ? jobCodesBySite[siteId] : new HashSet<Guid>();
                    var filteredDetails = rawDetails
                        .Where(e => e.Contains(bs_PayrollDetail.Fields.bs_JobCodeFK) && allowedCodes.Contains(((EntityReference)e[bs_PayrollDetail.Fields.bs_JobCodeFK]).Id))
                        .Select(e =>
                        {
                            var detail = e.ToEntity<bs_PayrollDetail>();
                            var codeRef = e.Contains(bs_PayrollDetail.Fields.bs_JobCodeFK) ? (EntityReference)e[bs_PayrollDetail.Fields.bs_JobCodeFK] : null;
                            if (codeRef != null && jobCodeDict.TryGetValue(codeRef.Id, out var codeEntity))
                            {
                                detail["jobcode_display"] = codeEntity.GetAttributeValue<string>("bs_jobcode");
                                detail["jobcode_displayname"] = codeEntity.GetAttributeValue<string>("bs_jobtitle");
                                var groupRef = codeEntity.Contains("bs_jobgroupfk") && codeEntity["bs_jobgroupfk"] is EntityReference gr ? gr.Id : (Guid?)null;
                                if (groupRef.HasValue && jobGroupDict.TryGetValue(groupRef.Value, out var groupEntity))
                                {
                                    detail["jobgroup_title"] = groupEntity.GetAttributeValue<string>("bs_jobgrouptitle");
                                }
                            }
                            return detail;
                        })
                        .ToList();

                    if (filteredDetails.Any())
                    {
                        payroll.bs_PayrollDetail_Payroll = filteredDetails;
                    }
                }

                if (!result.ContainsKey(period))
                    result[period] = new Dictionary<Guid, bs_Payroll>();
                result[period][siteId] = payroll;
            }

            return result;
        }

        public void SavePayroll(bs_Payroll payroll)
        {
            var serviceClient = _dataverseService.GetServiceClient();

            // Store the details separately and clear them from the parent
            var details = payroll.bs_PayrollDetail_Payroll?.ToList() ?? new List<bs_PayrollDetail>();
            payroll.bs_PayrollDetail_Payroll = null;

            // Update the main payroll record first (without details)
            serviceClient.Update(payroll);

            // Handle the detail records separately
            if (details.Any())
            {
                var batchRequest = new ExecuteMultipleRequest
                {
                    Requests = new OrganizationRequestCollection(),
                    Settings = new ExecuteMultipleSettings
                    {
                        ContinueOnError = false,
                        ReturnResponses = false
                    }
                };

                var existingQuery = new QueryExpression(bs_PayrollDetail.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(
                        bs_PayrollDetail.Fields.bs_PayrollDetailId,
                        bs_PayrollDetail.Fields.bs_JobCodeFK,
                        bs_PayrollDetail.Fields.bs_Date
                    ),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression(
                                bs_PayrollDetail.Fields.bs_PayrollFK,
                                ConditionOperator.Equal,
                                payroll.Id
                            )
                        }
                    }
                };

                var existingDetails = serviceClient.RetrieveMultiple(existingQuery);

                // Create a dictionary of existing records by business key (JobCodeId + Date)
                var existingDetailsByKey = new Dictionary<string, Entity>();
                var existingDetailIds = new HashSet<Guid>();

                foreach (var existing in existingDetails.Entities)
                {
                    var jobCodeId = existing.GetAttributeValue<EntityReference>(bs_PayrollDetail.Fields.bs_JobCodeFK)?.Id;
                    var date = existing.GetAttributeValue<DateTime?>(bs_PayrollDetail.Fields.bs_Date);

                    if (jobCodeId.HasValue)
                    {
                        var key = $"{jobCodeId}_{date?.ToString("yyyy-MM-dd") ?? "null"}";
                        existingDetailsByKey[key] = existing;
                        existingDetailIds.Add(existing.Id);
                    }
                }

                var processedExistingIds = new HashSet<Guid>();

                // Process each incoming detail record
                foreach (var detail in details)
                {
                    detail.bs_PayrollFK = new EntityReference(bs_Payroll.EntityLogicalName, payroll.Id);

                    // Create business key for this detail
                    var jobCodeId = detail.bs_JobCodeFK?.Id;
                    var date = detail.bs_Date;

                    if (jobCodeId.HasValue)
                    {
                        var key = $"{jobCodeId}_{date?.ToString("yyyy-MM-dd") ?? "null"}";

                        if (existingDetailsByKey.TryGetValue(key, out var existingDetail))
                        {
                            // Update existing record
                            detail.bs_PayrollDetailId = existingDetail.Id;
                            batchRequest.Requests.Add(new UpdateRequest { Target = detail });
                            processedExistingIds.Add(existingDetail.Id);
                        }
                        else
                        {
                            // Create new record (ensure it has a valid GUID)
                            if (detail.Id == Guid.Empty)
                            {
                                detail.Id = Guid.NewGuid();
                            }
                            batchRequest.Requests.Add(new CreateRequest { Target = detail });
                        }
                    }
                    else
                    {
                        // Fallback: if business key components are missing, treat as new record
                        if (detail.Id == Guid.Empty)
                        {
                            detail.Id = Guid.NewGuid();
                        }
                        batchRequest.Requests.Add(new CreateRequest { Target = detail });
                    }
                }

                // Delete any existing details that weren't updated
                var detailsToDelete = existingDetailIds.Except(processedExistingIds);
                foreach (var detailId in detailsToDelete)
                {
                    batchRequest.Requests.Add(new DeleteRequest
                    {
                        Target = new EntityReference(bs_PayrollDetail.EntityLogicalName, detailId)
                    });
                }

                if (batchRequest.Requests.Count > 0)
                {
                    serviceClient.Execute(batchRequest);
                }
            }
        }

        public void CreatePayroll(bs_Payroll payroll)
        {
            var serviceClient = _dataverseService.GetServiceClient();

            // Create the main payroll record first
            var payrollId = serviceClient.Create(payroll);
            payroll.Id = payrollId;

            if (payroll.bs_PayrollDetail_Payroll != null && payroll.bs_PayrollDetail_Payroll.Any())
            {
                var batchRequest = new ExecuteMultipleRequest
                {
                    Requests = new OrganizationRequestCollection(),
                    Settings = new ExecuteMultipleSettings
                    {
                        ContinueOnError = false,
                        ReturnResponses = false
                    }
                };

                // Create detail records
                foreach (var detail in payroll.bs_PayrollDetail_Payroll)
                {
                    detail.bs_PayrollFK = new EntityReference(bs_Payroll.EntityLogicalName, payrollId);
                    batchRequest.Requests.Add(new CreateRequest { Target = detail });
                }

                if (batchRequest.Requests.Count > 0)
                {
                    serviceClient.Execute(batchRequest);
                }
            }
        }

        public void UpsertPayroll(bs_Payroll payroll, Guid customerSiteId, string billingPeriod)
        {
            var serviceClient = _dataverseService.GetServiceClient();

            // Store the details separately and clear them from the parent
            var details = payroll.bs_PayrollDetail_Payroll?.ToList() ?? new List<bs_PayrollDetail>();
            payroll.bs_PayrollDetail_Payroll = null;

            // First, try to find existing payroll record
            var existingPayroll = GetPayroll(customerSiteId, billingPeriod);
            
            if (existingPayroll != null)
            {
                // Update existing record
                payroll.Id = existingPayroll.bs_PayrollId ?? Guid.Empty;
                payroll.bs_PayrollId = existingPayroll.bs_PayrollId;
                serviceClient.Update(payroll);
            }
            else
            {
                // Create new record with a specific ID to avoid race conditions
                var payrollId = Guid.NewGuid();
                payroll.Id = payrollId;
                payroll.bs_PayrollId = payrollId;
                
                try
                {
                    serviceClient.Create(payroll);
                }
                catch (FaultException<OrganizationServiceFault> ex) when (ex.Detail?.ErrorCode == -2147220937) // Duplicate key error
                {
                    // Another process created the record, try to get it and update instead
                    var newExisting = GetPayroll(customerSiteId, billingPeriod);
                    if (newExisting != null)
                    {
                        payroll.Id = newExisting.bs_PayrollId ?? Guid.Empty;
                        payroll.bs_PayrollId = newExisting.bs_PayrollId;
                        serviceClient.Update(payroll);
                    }
                    else
                    {
                        throw; // Unexpected error, re-throw
                    }
                }
            }

            // Handle the detail records separately (same logic as SavePayroll)
            if (details.Any())
            {
                var batchRequest = new ExecuteMultipleRequest
                {
                    Requests = new OrganizationRequestCollection(),
                    Settings = new ExecuteMultipleSettings
                    {
                        ContinueOnError = false,
                        ReturnResponses = false
                    }
                };

                var existingQuery = new QueryExpression(bs_PayrollDetail.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(
                        bs_PayrollDetail.Fields.bs_PayrollDetailId,
                        bs_PayrollDetail.Fields.bs_JobCodeFK,
                        bs_PayrollDetail.Fields.bs_Date
                    ),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression(
                                bs_PayrollDetail.Fields.bs_PayrollFK,
                                ConditionOperator.Equal,
                                payroll.Id
                            )
                        }
                    }
                };

                var existingDetails = serviceClient.RetrieveMultiple(existingQuery);

                // Create a dictionary of existing records by business key (JobCodeId + Date)
                var existingDetailsByKey = new Dictionary<string, Entity>();
                var existingDetailIds = new HashSet<Guid>();

                foreach (var existing in existingDetails.Entities)
                {
                    var jobCodeId = existing.GetAttributeValue<EntityReference>(bs_PayrollDetail.Fields.bs_JobCodeFK)?.Id;
                    var date = existing.GetAttributeValue<DateTime?>(bs_PayrollDetail.Fields.bs_Date);

                    if (jobCodeId.HasValue)
                    {
                        var key = $"{jobCodeId}_{date?.ToString("yyyy-MM-dd") ?? "null"}";
                        existingDetailsByKey[key] = existing;
                        existingDetailIds.Add(existing.Id);
                    }
                }

                var processedExistingIds = new HashSet<Guid>();

                // Process each incoming detail record
                foreach (var detail in details)
                {
                    detail.bs_PayrollFK = new EntityReference(bs_Payroll.EntityLogicalName, payroll.Id);

                    // Create business key for this detail
                    var jobCodeId = detail.bs_JobCodeFK?.Id;
                    var date = detail.bs_Date;

                    if (jobCodeId.HasValue)
                    {
                        var key = $"{jobCodeId}_{date?.ToString("yyyy-MM-dd") ?? "null"}";

                        if (existingDetailsByKey.TryGetValue(key, out var existingDetail))
                        {
                            // Update existing record
                            detail.bs_PayrollDetailId = existingDetail.Id;
                            batchRequest.Requests.Add(new UpdateRequest { Target = detail });
                            processedExistingIds.Add(existingDetail.Id);
                        }
                        else
                        {
                            // Create new record (ensure it has a valid GUID)
                            if (detail.Id == Guid.Empty)
                            {
                                detail.Id = Guid.NewGuid();
                            }
                            batchRequest.Requests.Add(new CreateRequest { Target = detail });
                        }
                    }
                    else
                    {
                        // Fallback: if business key components are missing, treat as new record
                        if (detail.Id == Guid.Empty)
                        {
                            detail.Id = Guid.NewGuid();
                        }
                        batchRequest.Requests.Add(new CreateRequest { Target = detail });
                    }
                }

                // Delete any existing details that weren't updated
                var detailsToDelete = existingDetailIds.Except(processedExistingIds);
                foreach (var detailId in detailsToDelete)
                {
                    batchRequest.Requests.Add(new DeleteRequest
                    {
                        Target = new EntityReference(bs_PayrollDetail.EntityLogicalName, detailId)
                    });
                }

                if (batchRequest.Requests.Count > 0)
                {
                    serviceClient.Execute(batchRequest);
                }
            }
        }

        public async System.Threading.Tasks.Task<api.Models.Vo.EDWPayrollBudgetDataVo?> GetBudgetPayrollFromEDW(string costCenter, int year, int month)
        {
            var flowUrl = Config.Configuration.getEDWDataAPIEndpoint();
            if (string.IsNullOrEmpty(flowUrl))
            {
                throw new Exception("Power Automate flow URL is not configured.");
            }

            var token = await Config.Configuration.getAccessTokenAsync(true);

            using (var httpClient = new System.Net.Http.HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var payload = new
                {
                    storedProcedureId = StoredProcedureId.Payroll_Budget,
                    storedProcedureParameters = new
                    {
                        COST_CENTER = costCenter,
                        YEAR = year,
                        MONTH = month
                    }
                };

                var content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json");
                var httpResponse = await httpClient.PostAsync(flowUrl, content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var data = await httpResponse.Content.ReadAsStringAsync();

                    if (string.IsNullOrWhiteSpace(data))
                        return null;

                    try
                    {
                        var records = Newtonsoft.Json.JsonConvert.DeserializeObject<List<EDWPayrollBudgetRecord>>(data);
                        if (records != null && records.Count > 0)
                        {
                            return new EDWPayrollBudgetDataVo { Records = records };
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to parse as EDWPayrollBudgetRecord list: {ex.Message}");
                    }

                    return null;
                }
                else
                {
                    var errorBody = await httpResponse.Content.ReadAsStringAsync();
                    throw new Exception($"Error calling EDW for PayrollBudgetData: {httpResponse.StatusCode} - {errorBody}");
                }
            }
        }

        public async System.Threading.Tasks.Task<api.Models.Vo.EDWPayrollActualDataVo?> GetActualPayrollFromEDW(string costCenter, int year, int month)
        {
            var flowUrl = Config.Configuration.getEDWDataAPIEndpoint();
            if (string.IsNullOrEmpty(flowUrl))
            {
                throw new Exception("Power Automate flow URL is not configured.");
            }

            var token = await Config.Configuration.getAccessTokenAsync(true);

            using (var httpClient = new System.Net.Http.HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var payload = new
                {
                    storedProcedureId = StoredProcedureId.Payroll_Actuals,
                    storedProcedureParameters = new
                    {
                        WORK_LOCATION = costCenter,
                        YEAR = year,
                        MONTH = month
                    }
                };

                var content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json");
                var httpResponse = await httpClient.PostAsync(flowUrl, content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var data = await httpResponse.Content.ReadAsStringAsync();

                    if (string.IsNullOrWhiteSpace(data))
                        return null;

                    try
                    {
                        var records = Newtonsoft.Json.JsonConvert.DeserializeObject<List<api.Models.Vo.EDWPayrollDetailsRecord>>(data);
                        if (records != null && records.Count > 0)
                        {
                            return new api.Models.Vo.EDWPayrollActualDataVo { Records = records };
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to parse as EDWPayrollDetailsRecord list: {ex.Message}");
                    }

                    return null;
                }
                else
                {
                    var errorBody = await httpResponse.Content.ReadAsStringAsync();
                    throw new Exception($"Error calling EDW for PayrollActualData: {httpResponse.StatusCode} - {errorBody}");
                }
            }
        }

        public async System.Threading.Tasks.Task<api.Models.Vo.EDWPayrollActualDataVo?> GetSchedulePayrollFromEDW(string costCenter, int year, int month)
        {
            var flowUrl = Config.Configuration.getEDWDataAPIEndpoint();
            if (string.IsNullOrEmpty(flowUrl))
            {
                throw new Exception("Power Automate flow URL is not configured.");
            }

            var token = await Config.Configuration.getAccessTokenAsync(true);

            using (var httpClient = new System.Net.Http.HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var payload = new
                {
                    storedProcedureId = StoredProcedureId.Payroll_Schedule,
                    storedProcedureParameters = new
                    {
                        WORK_LOCATION = costCenter,
                        YEAR = year,
                        MONTH = month
                    }
                };

                var content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(payload), System.Text.Encoding.UTF8, "application/json");
                var httpResponse = await httpClient.PostAsync(flowUrl, content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var data = await httpResponse.Content.ReadAsStringAsync();

                    if (string.IsNullOrWhiteSpace(data))
                        return null;

                    try
                    {
                        var records = Newtonsoft.Json.JsonConvert.DeserializeObject<List<api.Models.Vo.EDWPayrollDetailsRecord>>(data);
                        if (records != null && records.Count > 0)
                        {
                            return new api.Models.Vo.EDWPayrollActualDataVo { Records = records };
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to parse as EDWPayrollDetailsRecord list: {ex.Message}");
                    }

                    return null;
                }
                else
                {
                    var errorBody = await httpResponse.Content.ReadAsStringAsync();
                    throw new Exception($"Error calling EDW for PayrollScheduleData: {httpResponse.StatusCode} - {errorBody}");
                }
            }
        }
    }
}
