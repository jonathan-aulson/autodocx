using api.Services;
using TownePark;

namespace api.Data.Impl
{
    public class JobGroupRepository : IJobGroupRepository
    {
        private readonly IDataverseService _dataverseService;

        public JobGroupRepository(IDataverseService dataverseService)
        {
            _dataverseService = dataverseService;
        }

        public void CreateJobGroup(string groupTitle)
        {
            var serviceClient = _dataverseService.GetServiceClient();

            bs_JobGroup jobGroup = new bs_JobGroup
            {
                bs_JobGroupTitle = groupTitle,
                bs_IsActive = true,
            };

            serviceClient.Create(jobGroup);
        }

        public void DeactivateJobGroup(Guid jobGroupId)
        {
            var serviceClient = _dataverseService.GetServiceClient();

            // Create an entity reference with only the fields you want to update
            bs_JobGroup jobGroup = new bs_JobGroup
            {
                Id = jobGroupId,
                bs_IsActive = false
            };

            serviceClient.Update(jobGroup);
        }

        public void ActivateJobGroup(Guid jobGroupId)
        {
            var serviceClient = _dataverseService.GetServiceClient();

            // Create an entity reference with only the fields you want to update
            bs_JobGroup jobGroup = new bs_JobGroup
            {
                Id = jobGroupId,
                bs_IsActive = true
            };

            serviceClient.Update(jobGroup);
        }

        public IEnumerable<api.Models.Vo.JobGroupVo> GetAllJobGroups()
        {
            var serviceClient = _dataverseService.GetServiceClient();

            // Fetch all job groups
            var groupQuery = new Microsoft.Xrm.Sdk.Query.QueryExpression("bs_jobgroup")
            {
                ColumnSet = new Microsoft.Xrm.Sdk.Query.ColumnSet("bs_jobgrouptitle", "bs_isactive")
            };
            var groupResults = serviceClient.RetrieveMultiple(groupQuery);

            // Fetch all job codes
            var codeQuery = new Microsoft.Xrm.Sdk.Query.QueryExpression("bs_jobcode")
            {
                ColumnSet = new Microsoft.Xrm.Sdk.Query.ColumnSet("bs_jobcodeid", "bs_jobtitle", "bs_isactive", "bs_jobgroupfk")
            };
            var codeResults = serviceClient.RetrieveMultiple(codeQuery);

            // Map job codes to their group
            var jobCodesByGroup = new Dictionary<Guid, List<api.Models.Dto.JobCodeDto>>();
            foreach (var codeEntity in codeResults.Entities)
            {
                var groupRef = codeEntity.Contains("bs_jobgroupfk") && codeEntity["bs_jobgroupfk"] is Microsoft.Xrm.Sdk.EntityReference er ? er.Id : (Guid?)null;
                if (groupRef.HasValue)
                {
                    if (!jobCodesByGroup.ContainsKey(groupRef.Value))
                        jobCodesByGroup[groupRef.Value] = new List<api.Models.Dto.JobCodeDto>();

                    jobCodesByGroup[groupRef.Value].Add(new api.Models.Dto.JobCodeDto
                    {
                        JobCodeId = codeEntity.Id,
                        JobCode = codeEntity.Contains("bs_jobcodeid") ? codeEntity["bs_jobcodeid"].ToString() : codeEntity.Id.ToString(),
                        JobTitle = codeEntity.Contains("bs_jobtitle") ? codeEntity["bs_jobtitle"].ToString() : string.Empty,
                        JobGroupId = groupRef.Value.ToString(),
                        JobGroupName = "", // Will be set in the next loop when group is known
                        Name = codeEntity.Contains("bs_jobtitle") ? codeEntity["bs_jobtitle"].ToString() : string.Empty,
                        IsActive = codeEntity.Contains("bs_isactive") && codeEntity["bs_isactive"] is bool b && b
                    });
                }
            }

            foreach (var entity in groupResults.Entities)
            {
                var groupId = entity.Id;
                var groupTitle = entity.Contains("bs_jobgrouptitle") ? entity["bs_jobgrouptitle"].ToString() : string.Empty;
                var codes = jobCodesByGroup.ContainsKey(groupId) ? jobCodesByGroup[groupId] : new List<api.Models.Dto.JobCodeDto>();
                // Map DTO codes to VOs (manual for now, but should use Mapperly if available)
                var voCodes = new List<api.Models.Vo.JobCodeVo>();
                foreach (var code in codes)
                {
                    voCodes.Add(new api.Models.Vo.JobCodeVo
                    {
                        JobCodeId = code.JobCodeId,
                        JobCode = code.JobCode,
                        JobTitle = code.JobTitle,
                        JobGroupId = code.JobGroupId,
                        JobGroupName = string.IsNullOrWhiteSpace(groupTitle) ? "unassigned" : groupTitle,
                        Name = code.Name,
                        IsActive = code.IsActive
                    });
                }
                yield return new api.Models.Vo.JobGroupVo
                {
                    Id = groupId,
                    Title = groupTitle,
                    IsActive = entity.Contains("bs_isactive") && entity["bs_isactive"] is bool b && b,
                    JobCodes = voCodes
                };
            }
        }
    }
}
