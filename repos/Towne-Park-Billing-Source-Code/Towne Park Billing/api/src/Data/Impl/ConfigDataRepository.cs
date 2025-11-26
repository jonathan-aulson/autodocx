using api.Services;
using TownePark;
using api.Models.Vo.Enum;
using Microsoft.Xrm.Sdk.Query;

namespace api.Data.Impl
{
    public class ConfigDataRepository : IConfigDataRepository
    {
        private readonly IDataverseService _dataverseService;

        public ConfigDataRepository(IDataverseService dataverseService)
        {
            _dataverseService = dataverseService;
        }

        public IEnumerable<bs_GLCodeConfig> GetGlCodes(IEnumerable<bs_glcodetypechoices> codeTypes)
        {
            var serviceClient = _dataverseService.GetServiceClient();

            var query = new QueryExpression(bs_GLCodeConfig.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(bs_GLCodeConfig.Fields.bs_Code, bs_GLCodeConfig.Fields.bs_Name, bs_GLCodeConfig.Fields.bs_Type,
                bs_GLCodeConfig.Fields.bs_Data)
            };

            var codeTypeInts = codeTypes.Select(ct => (int)ct).ToList();

            query.Criteria.AddCondition(bs_GLCodeConfig.Fields.bs_Type, ConditionOperator.In, codeTypeInts.Cast<object>().ToArray());

            return serviceClient.RetrieveMultiple(query).Entities.Cast<bs_GLCodeConfig>();
        }

        public IEnumerable<bs_GeneralConfig> GetInvoiceConfigData(bs_generalconfiggroupchoices configGroup)
        {

           var serviceClient = _dataverseService.GetServiceClient();

            var query = new QueryExpression(bs_GeneralConfig.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(bs_GeneralConfig.Fields.bs_Key, bs_GeneralConfig.Fields.bs_Value)
            };

            query.Criteria.AddCondition(bs_GeneralConfig.Fields.bs_Group, ConditionOperator.In, (int)configGroup);

            return serviceClient.RetrieveMultiple(query).Entities.Cast<bs_GeneralConfig>();
        }
    }
}
