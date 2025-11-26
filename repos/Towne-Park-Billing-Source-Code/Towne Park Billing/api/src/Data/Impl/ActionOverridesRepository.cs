using api.Services;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;
using TownePark;

namespace api.Data.Impl
{
    public class ActionOverridesRepository : IActionOverridesRepository
    {
        private readonly IDataverseService _dataverseService;

        public ActionOverridesRepository(IDataverseService dataverseService)
        {
            _dataverseService = dataverseService;
        }

        public bs_ActionOverrides GetActionOverrideValueByName(string name)
        {
            var serviceClient = _dataverseService.GetServiceClient();
            var query = new QueryExpression(bs_ActionOverrides.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(
                    bs_ActionOverrides.Fields.bs_ActionOverridesId,
                    bs_ActionOverrides.Fields.bs_Name,
                    bs_ActionOverrides.Fields.bs_Description,
                    bs_ActionOverrides.Fields.bs_Value
                )
            };

            query.Criteria.AddCondition(bs_ActionOverrides.Fields.bs_Name, ConditionOperator.Equal, name);
            query.Criteria.AddCondition(bs_ActionOverrides.Fields.bs_IsActive, ConditionOperator.Equal, true);

            var entity = serviceClient.RetrieveMultiple(query).Entities.FirstOrDefault();

            return entity?.ToEntity<bs_ActionOverrides>();
        }
    }
}
