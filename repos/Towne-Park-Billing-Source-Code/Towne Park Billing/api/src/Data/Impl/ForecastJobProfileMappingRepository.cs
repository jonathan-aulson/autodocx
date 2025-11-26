using api.Services;
using TownePark;
using Microsoft.Xrm.Sdk.Query;

namespace api.Data.Impl
{
    public class ForecastJobProfileMappingRepository : IForecastJobProfileMappingRepository
    {
        private readonly IDataverseService _dataverseService;

        public ForecastJobProfileMappingRepository(IDataverseService dataverseService)
        {
            _dataverseService = dataverseService;
        }

        public IEnumerable<bs_ForecastJobProfileMapping> GetForecastJobProfileMappingsByCustomerSite(Guid customerSiteId)
        {
            var serviceClient = _dataverseService.GetServiceClient();

            var query = new QueryExpression(bs_ForecastJobProfileMapping.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(
                    bs_ForecastJobProfileMapping.Fields.bs_ForecastJobProfileMappingId,
                    bs_ForecastJobProfileMapping.Fields.bs_JobCode,
                    bs_ForecastJobProfileMapping.Fields.bs_JobProfile,
                    bs_ForecastJobProfileMapping.Fields.bs_Name,
                    bs_ForecastJobProfileMapping.Fields.bs_CustomerSiteFK
                )
            };

            query.Criteria.AddCondition(bs_ForecastJobProfileMapping.Fields.bs_CustomerSiteFK, ConditionOperator.Equal, customerSiteId);

            return serviceClient.RetrieveMultiple(query).Entities.Cast<bs_ForecastJobProfileMapping>();
        }
    }
}
