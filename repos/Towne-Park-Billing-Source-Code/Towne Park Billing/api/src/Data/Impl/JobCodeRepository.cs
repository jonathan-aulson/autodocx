using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Models.Vo;
using api.Services;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using TownePark;

namespace api.Data.Impl
{
    public class JobCodeRepository : IJobCodeRepository
    {
        private readonly IDataverseService _dataverseService;

        public JobCodeRepository(IDataverseService dataverseService)
        {
            _dataverseService = dataverseService;
        }

        public async Task<IList<JobCodeVo>> GetJobCodesAsync()
        {
            var service = _dataverseService.GetServiceClient();

            // Fetch all job codes with job group foreign key
            var codeQuery = new QueryExpression("bs_jobcode")
            {
                ColumnSet = new ColumnSet("bs_jobcodeid", "bs_jobcode", "bs_jobtitle", "bs_name", "bs_isactive", "bs_jobgroupfk")
            };
            var codeResults = await Task.Run(() => service.RetrieveMultiple(codeQuery));

            // Fetch all job groups for lookup
            var groupQuery = new QueryExpression("bs_jobgroup")
            {
                ColumnSet = new ColumnSet("bs_jobgroupid", "bs_jobgrouptitle")
            };
            var groupResults = await Task.Run(() => service.RetrieveMultiple(groupQuery));

            // Create a lookup dictionary for job groups
            var jobGroupLookup = groupResults.Entities.ToDictionary(
                g => g.Id,
                g => new
                {
                    Id = g.Id,
                    Title = g.GetAttributeValue<string>("bs_jobgrouptitle") ?? string.Empty
                }
            );

            return codeResults.Entities.Select(e =>
            {
                var vo = new JobCodeVo();
                vo.JobCodeId = e.Id;
                vo.JobCode = e.GetAttributeValue<string>("bs_jobcode") ?? string.Empty;
                vo.JobTitle = e.GetAttributeValue<string>("bs_jobtitle") ?? string.Empty;
                vo.Name = e.GetAttributeValue<string>("bs_name") ?? string.Empty;
                vo.IsActive = e.GetAttributeValue<bool?>("bs_isactive") ?? false;

                // Handle job group relationship
                var jobGroupRef = e.Contains("bs_jobgroupfk") && e["bs_jobgroupfk"] is EntityReference er ? er.Id : (Guid?)null;
                if (jobGroupRef.HasValue && jobGroupLookup.ContainsKey(jobGroupRef.Value))
                {
                    var jobGroup = jobGroupLookup[jobGroupRef.Value];
                    vo.JobGroupId = jobGroup.Id.ToString();
                    vo.JobGroupName = jobGroup.Title;
                }
                else
                {
                    vo.JobGroupId = string.Empty;
                    vo.JobGroupName = "unassigned";
                }

                return vo;
            }).ToList();
        }

        public async Task<IList<JobCodeVo>> GetJobCodesBySiteAsync(Guid siteId)
        {
            var service = _dataverseService.GetServiceClient();

            // Query bs_jobcodesbysite for the specified site
            var query = new QueryExpression(bs_JobCodesBySite.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(
                    bs_JobCodesBySite.Fields.bs_JobCodesBySiteId,
                    bs_JobCodesBySite.Fields.bs_CustomerSite,
                    bs_JobCodesBySite.Fields.bs_JobCode,
                    bs_JobCodesBySite.Fields.bs_ActiveEmployeeCount,
                    bs_JobCodesBySite.Fields.bs_AllocatedSalaryCost,
                    bs_JobCodesBySite.Fields.bs_AverageHourlyRate
                ),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression(bs_JobCodesBySite.Fields.bs_CustomerSite, ConditionOperator.Equal, siteId)
                    }
                }
            };

            var results = await Task.Run(() => service.RetrieveMultiple(query));

            var jobCodeVos = new List<JobCodeVo>();

            foreach (var entity in results.Entities)
            {
                var jobCodeRef = entity.GetAttributeValue<EntityReference>(bs_JobCode.EntityLogicalName);
                if (jobCodeRef == null)
                    continue;

                // Retrieve job code details
                var jobCodeEntity = service.Retrieve(bs_JobCode.EntityLogicalName, jobCodeRef.Id, new ColumnSet(
                    bs_JobCode.Fields.bs_JobCodeId,
                    bs_JobCode.Fields.bs_JobCode1,
                    bs_JobCode.Fields.bs_JobTitle,
                    bs_JobCode.Fields.bs_Name,
                    bs_JobCode.Fields.bs_IsActive,
                    bs_JobCode.Fields.bs_JobGroupFK
                    ));
                if (jobCodeEntity == null)
                    continue;

                var vo = new JobCodeVo
                {
                    JobCodeId = jobCodeEntity.Id,
                    JobCode = jobCodeEntity.GetAttributeValue<string>(bs_JobCode.Fields.bs_JobCode1) ?? string.Empty,
                    JobTitle = jobCodeEntity.GetAttributeValue<string>(bs_JobCode.Fields.bs_JobTitle) ?? string.Empty,
                    Name = jobCodeEntity.GetAttributeValue<string>(bs_JobCode.Fields.bs_Name) ?? string.Empty,
                    IsActive = jobCodeEntity.GetAttributeValue<bool?>(bs_JobCode.Fields.bs_IsActive) ?? false,
                    ActiveEmployeeCount = entity.GetAttributeValue<decimal>(bs_JobCodesBySite.Fields.bs_ActiveEmployeeCount),
                    AllocatedSalaryCost = entity.GetAttributeValue<decimal?>(bs_JobCodesBySite.Fields.bs_AllocatedSalaryCost),
                    AverageHourlyRate = entity.GetAttributeValue<decimal?>(bs_JobCodesBySite.Fields.bs_AverageHourlyRate)
                };

                // Handle job group relationship
                var jobGroupRef = jobCodeEntity.Contains(bs_JobCode.Fields.bs_JobGroupFK) && jobCodeEntity[bs_JobCode.Fields.bs_JobGroupFK] is EntityReference er ? er.Id : (Guid?)null;
                if (jobGroupRef.HasValue)
                {
                    vo.JobGroupId = jobGroupRef.Value.ToString();

                    // Fetch job group details to get the title
                    try
                    {
                        var jobGroupEntity = service.Retrieve(bs_JobGroup.EntityLogicalName, jobGroupRef.Value, new ColumnSet(bs_JobGroup.Fields.bs_JobGroupTitle));
                        vo.JobGroupName = jobGroupEntity?.GetAttributeValue<string>(bs_JobGroup.Fields.bs_JobGroupTitle) ?? "Unknown Group";
                    }
                    catch
                    {
                        vo.JobGroupName = "Unknown Group";
                    }
                }
                else
                {
                    vo.JobGroupId = string.Empty;
                    vo.JobGroupName = "unassigned";
                }

                jobCodeVos.Add(vo);
            }

            return jobCodeVos;
        }

        public async Task<(bool Success, string? OldTitle)> UpdateJobCodeTitleAsync(Guid jobCodeId, string newTitle)
        {
            var service = _dataverseService.GetServiceClient();
            var entity = service.Retrieve("bs_jobcode", jobCodeId, new ColumnSet("bs_jobtitle", "bs_isactive"));
            if (entity == null)
                return (false, null);

            var isActive = entity.GetAttributeValue<bool?>("bs_isactive") ?? false;
            if (!isActive)
                return (false, null);

            var oldTitle = entity.GetAttributeValue<string>("bs_jobtitle");
            entity["bs_jobtitle"] = newTitle;
            service.Update(entity);

            return (true, oldTitle);
        }

        public async Task<(bool Success, string? ErrorMessage, int ProcessedCount, List<(Guid JobCodeId, bool Success, string? ErrorMessage, Guid? PreviousGroupId)> Results)> AssignJobCodesToGroupAsync(List<Guid> jobCodeIds, Guid targetGroupId)
        {
            var service = _dataverseService.GetServiceClient();
            var results = new List<(Guid JobCodeId, bool Success, string? ErrorMessage, Guid? PreviousGroupId)>();
            int processedCount = 0;

            try
            {
                // Validate inputs
                if (jobCodeIds == null || !jobCodeIds.Any())
                {
                    return (false, "No job codes provided for assignment.", 0, results);
                }

                if (jobCodeIds.Count > 100)
                {
                    return (false, "Maximum of 100 job codes can be assigned in a single request.", 0, results);
                }

                if (targetGroupId == Guid.Empty)
                {
                    return (false, "Target group ID is required.", 0, results);
                }

                // Validate target job group exists and is active
                var targetGroupValid = await ValidateJobGroupExistsAndActiveAsync(targetGroupId);
                if (!targetGroupValid)
                {
                    return (false, "Target job group not found or is inactive.", 0, results);
                }

                // Process each job code
                foreach (var jobCodeId in jobCodeIds)
                {
                    try
                    {
                        // Fetch the job code to check if it exists and is active
                        var jobCodeEntity = await Task.Run(() => service.Retrieve("bs_jobcode", jobCodeId, new ColumnSet("bs_jobtitle", "bs_isactive", "bs_jobgroupfk")));

                        if (jobCodeEntity == null)
                        {
                            results.Add((jobCodeId, false, "Job code not found.", null));
                            continue;
                        }

                        var isActive = jobCodeEntity.GetAttributeValue<bool?>("bs_isactive") ?? false;
                        if (!isActive)
                        {
                            results.Add((jobCodeId, false, "Job code is inactive.", null));
                            continue;
                        }

                        // Get previous group ID if any
                        var previousGroupRef = jobCodeEntity.Contains("bs_jobgroupfk") && jobCodeEntity["bs_jobgroupfk"] is EntityReference er ? er.Id : (Guid?)null;

                        // Update the job group assignment
                        var updateEntity = new bs_JobCode
                        {
                            Id = jobCodeId,
                            bs_JobGroupFK = new EntityReference("bs_jobgroup", targetGroupId)
                        };

                        await Task.Run(() => service.Update(updateEntity));

                        results.Add((jobCodeId, true, null, previousGroupRef));
                        processedCount++;
                    }
                    catch (Exception ex)
                    {
                        results.Add((jobCodeId, false, $"Error updating job code: {ex.Message}", null));
                    }
                }

                return (true, null, processedCount, results);
            }
            catch (Exception ex)
            {
                return (false, $"Unexpected error during assignment: {ex.Message}", processedCount, results);
            }
        }

        public async Task<(bool AllValid, List<Guid> InvalidJobCodeIds)> ValidateJobCodesExistAndActiveAsync(List<Guid> jobCodeIds)
        {
            if (jobCodeIds == null || !jobCodeIds.Any())
            {
                return (true, new List<Guid>());
            }

            var service = _dataverseService.GetServiceClient();
            var invalidIds = new List<Guid>();

            try
            {
                // Fetch all job codes in a single query for efficiency
                var query = new QueryExpression("bs_jobcode")
                {
                    ColumnSet = new ColumnSet("bs_jobcodeid", "bs_isactive"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression("bs_jobcodeid", ConditionOperator.In, jobCodeIds.Cast<object>().ToArray())
                        }
                    }
                };

                var results = await Task.Run(() => service.RetrieveMultiple(query));
                var foundActiveIds = results.Entities
                    .Where(e => e.GetAttributeValue<bool?>("bs_isactive") == true)
                    .Select(e => e.Id)
                    .ToHashSet();

                // Find job codes that are either not found or inactive
                invalidIds = jobCodeIds.Where(id => !foundActiveIds.Contains(id)).ToList();

                return (invalidIds.Count == 0, invalidIds);
            }
            catch (Exception)
            {
                // On error, consider all job codes invalid
                return (false, jobCodeIds.ToList());
            }
        }

        public async Task<bool> ValidateJobGroupExistsAndActiveAsync(Guid jobGroupId)
        {
            if (jobGroupId == Guid.Empty)
            {
                return false;
            }

            var service = _dataverseService.GetServiceClient();

            try
            {
                var entity = await Task.Run(() => service.Retrieve("bs_jobgroup", jobGroupId, new ColumnSet("bs_isactive")));
                return entity != null && (entity.GetAttributeValue<bool?>("bs_isactive") ?? false);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<(bool Success, string? ErrorMessage, int ProcessedCount, List<(Guid JobCodeId, bool Success, string? ErrorMessage, bool PreviousStatus, string JobTitle)> Results)> UpdateJobCodeStatusAsync(List<Guid> jobCodeIds, bool isActive)
        {
            var service = _dataverseService.GetServiceClient();
            var results = new List<(Guid JobCodeId, bool Success, string? ErrorMessage, bool PreviousStatus, string JobTitle)>();
            int processedCount = 0;

            try
            {
                // Validate inputs
                if (jobCodeIds == null || !jobCodeIds.Any())
                {
                    return (false, "No job codes provided for status update.", 0, results);
                }

                if (jobCodeIds.Count > 100)
                {
                    return (false, "Maximum of 100 job codes can be updated in a single request.", 0, results);
                }

                // Process each job code
                foreach (var jobCodeId in jobCodeIds)
                {
                    try
                    {
                        // Fetch the job code to check if it exists
                        var jobCodeEntity = await Task.Run(() => service.Retrieve("bs_jobcode", jobCodeId, new ColumnSet("bs_jobtitle", "bs_isactive")));

                        if (jobCodeEntity == null)
                        {
                            results.Add((jobCodeId, false, "Job code not found.", false, string.Empty));
                            continue;
                        }

                        var currentStatus = jobCodeEntity.GetAttributeValue<bool?>("bs_isactive") ?? false;
                        var jobTitle = jobCodeEntity.GetAttributeValue<string>("bs_jobtitle") ?? string.Empty;

                        // Check if already in desired state
                        if (currentStatus == isActive)
                        {
                            var statusText = isActive ? "active" : "inactive";
                            results.Add((jobCodeId, false, $"Job code is already {statusText}.", currentStatus, jobTitle));
                            continue;
                        }

                        // Update the status
                        var updateEntity = new bs_JobCode
                        {
                            Id = jobCodeId,
                            bs_IsActive = isActive
                        };

                        await Task.Run(() => service.Update(updateEntity));

                        results.Add((jobCodeId, true, null, currentStatus, jobTitle));
                        processedCount++;

                    }
                    catch (Exception ex)
                    {
                        results.Add((jobCodeId, false, $"Error updating job code: {ex.Message}", false, string.Empty));
                    }
                }

                return (true, null, processedCount, results);
            }
            catch (Exception ex)
            {
                return (false, $"Unexpected error during status update: {ex.Message}", processedCount, results);
            }
        }
        
        public async Task<JobCodeVo?> GetJobCodeByIdAsync(Guid jobCodeId)
        {
            var service = _dataverseService.GetServiceClient();

            var entity = await Task.Run(() => service.Retrieve("bs_jobcode", jobCodeId, new ColumnSet("bs_jobcodeid", "bs_jobcode", "bs_jobtitle", "bs_name", "bs_isactive", "bs_jobgroupfk")));
            if (entity == null)
                return null;

            var vo = new JobCodeVo();
            vo.JobCodeId = entity.Id;
            vo.JobCode = entity.GetAttributeValue<string>("bs_jobcode") ?? string.Empty;
            vo.JobTitle = entity.GetAttributeValue<string>("bs_jobtitle") ?? string.Empty;
            vo.Name = entity.GetAttributeValue<string>("bs_name") ?? string.Empty;
            vo.IsActive = entity.GetAttributeValue<bool?>("bs_isactive") ?? false;

            var jobGroupRef = entity.Contains("bs_jobgroupfk") && entity["bs_jobgroupfk"] is EntityReference er ? er.Id : (Guid?)null;
            if (jobGroupRef.HasValue)
            {
                vo.JobGroupId = jobGroupRef.Value.ToString();
            }
            else
            {
                vo.JobGroupId = string.Empty;
            }

            return vo;
        }
    }
}
