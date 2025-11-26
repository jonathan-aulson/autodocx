using api.Models.Vo;
using api.Services;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using TownePark;

namespace api.Data.Impl
{
    public class SiteAssignmentRepository : ISiteAssignmentRepository
    {
        private readonly IDataverseService _dataverseService;

        public SiteAssignmentRepository(IDataverseService dataverseService)
        {
            _dataverseService = dataverseService;
        }

        public async Task<IList<SiteAssignmentVo>> GetSiteAssignmentsAsync()
        {
            var serviceClient = _dataverseService.GetServiceClient();

            // Query all active customer sites
            var siteQuery = new QueryExpression(bs_CustomerSite.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(
                    bs_CustomerSite.Fields.bs_CustomerSiteId,
                    bs_CustomerSite.Fields.bs_SiteNumber,
                    bs_CustomerSite.Fields.bs_SiteName,
                    bs_CustomerSite.Fields.bs_District
                ),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression(bs_CustomerSite.Fields.statecode, ConditionOperator.Equal, (int)bs_customersite_statecode.Active)
                    }
                }
            };

            var sites = await Task.Run(() => serviceClient.RetrieveMultiple(siteQuery));

            // Get all job codes by site relationships for active job codes only
            var jobCodesBySiteQuery = new QueryExpression(bs_JobCodesBySite.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(
                    bs_JobCodesBySite.Fields.bs_CustomerSite,
                    bs_JobCodesBySite.Fields.bs_JobCode
                )
            };

            // Add job code link to get job group assignments
            var jobCodeLink = jobCodesBySiteQuery.AddLink(
                bs_JobCode.EntityLogicalName,
                bs_JobCodesBySite.Fields.bs_JobCode,
                bs_JobCode.Fields.bs_JobCodeId,
                JoinOperator.Inner
            );
            jobCodeLink.Columns = new ColumnSet(
                bs_JobCode.Fields.bs_JobCodeId,
                bs_JobCode.Fields.bs_IsActive,
                bs_JobCode.Fields.bs_JobGroupFK
            );
            jobCodeLink.EntityAlias = "jobcode";
            jobCodeLink.LinkCriteria.AddCondition(bs_JobCode.Fields.bs_IsActive, ConditionOperator.Equal, true);

            // Add job group link to get job group details
            var jobGroupLink = jobCodeLink.AddLink(
                bs_JobGroup.EntityLogicalName,
                bs_JobCode.Fields.bs_JobGroupFK,
                bs_JobGroup.Fields.bs_JobGroupId,
                JoinOperator.Inner
            );
            jobGroupLink.Columns = new ColumnSet(
                bs_JobGroup.Fields.bs_JobGroupId,
                bs_JobGroup.Fields.bs_JobGroupTitle,
                bs_JobGroup.Fields.bs_IsActive
            );
            jobGroupLink.EntityAlias = "jobgroup";
            jobGroupLink.LinkCriteria.AddCondition(bs_JobGroup.Fields.bs_IsActive, ConditionOperator.Equal, true);

            var jobCodesBySite = await Task.Run(() => serviceClient.RetrieveMultiple(jobCodesBySiteQuery));

            // Get all active job codes to identify unassigned ones
            var allJobCodesQuery = new QueryExpression(bs_JobCode.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(
                    bs_JobCode.Fields.bs_JobCodeId,
                    bs_JobCode.Fields.bs_JobGroupFK,
                    bs_JobCode.Fields.bs_IsActive
                ),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression(bs_JobCode.Fields.bs_IsActive, ConditionOperator.Equal, true)
                    }
                }
            };

            // Add job codes by site link to get site associations
            var jobCodeSiteLink = allJobCodesQuery.AddLink(
                bs_JobCodesBySite.EntityLogicalName,
                bs_JobCode.Fields.bs_JobCodeId,
                bs_JobCodesBySite.Fields.bs_JobCode,
                JoinOperator.Inner
            );
            jobCodeSiteLink.Columns = new ColumnSet(bs_JobCodesBySite.Fields.bs_CustomerSite);
            jobCodeSiteLink.EntityAlias = "site";

            var allJobCodes = await Task.Run(() => serviceClient.RetrieveMultiple(allJobCodesQuery));

            // Process the results
            var siteAssignments = new List<SiteAssignmentVo>();

            foreach (var site in sites.Entities)
            {
                var siteId = site.Id;
                var siteNumber = site.GetAttributeValue<string>(bs_CustomerSite.Fields.bs_SiteNumber) ?? string.Empty;
                var siteName = site.GetAttributeValue<string>(bs_CustomerSite.Fields.bs_SiteName) ?? string.Empty;
                var district = site.GetAttributeValue<string>(bs_CustomerSite.Fields.bs_District) ?? string.Empty;

                // Find job groups assigned to this site
                var assignedJobGroups = jobCodesBySite.Entities
                    .Where(jcs => ((EntityReference)jcs.GetAttributeValue<EntityReference>(bs_JobCodesBySite.Fields.bs_CustomerSite))?.Id == siteId)
                    .Select(jcs => new
                    {
                        JobGroupId = jcs.GetAttributeValue<AliasedValue>("jobgroup." + bs_JobGroup.Fields.bs_JobGroupId)?.Value as Guid? ?? Guid.Empty,
                        JobGroupName = jcs.GetAttributeValue<AliasedValue>("jobgroup." + bs_JobGroup.Fields.bs_JobGroupTitle)?.Value as string ?? string.Empty,
                        IsActive = jcs.GetAttributeValue<AliasedValue>("jobgroup." + bs_JobGroup.Fields.bs_IsActive)?.Value as bool? ?? false
                    })
                    .Where(jg => jg.JobGroupId != Guid.Empty)
                    .GroupBy(jg => jg.JobGroupId)
                    .Select(g => g.First())
                    .Select(jg => new JobGroupAssignmentVo
                    {
                        JobGroupId = jg.JobGroupId,
                        JobGroupName = jg.JobGroupName,
                        IsActive = jg.IsActive
                    })
                    .OrderBy(jg => jg.JobGroupName)
                    .ToList();

                // Check for unassigned job codes at this site
                var hasUnassignedJobCodes = allJobCodes.Entities
                    .Where(jc => 
                    {
                        var jobCodeSiteRef = jc.GetAttributeValue<AliasedValue>("site." + bs_JobCodesBySite.Fields.bs_CustomerSite)?.Value as EntityReference;
                        return jobCodeSiteRef?.Id == siteId;
                    })
                    .Any(jc => jc.GetAttributeValue<EntityReference>(bs_JobCode.Fields.bs_JobGroupFK) == null);

                siteAssignments.Add(new SiteAssignmentVo
                {
                    SiteId = siteId,
                    SiteNumber = siteNumber,
                    SiteName = siteName,
                    City = district, // Using district as city for now
                    AssignedJobGroups = assignedJobGroups,
                    JobGroupCount = assignedJobGroups.Count,
                    HasUnassignedJobCodes = hasUnassignedJobCodes
                });
            }

            return siteAssignments.OrderBy(sa => sa.SiteNumber).ToList();
        }
    }
} 