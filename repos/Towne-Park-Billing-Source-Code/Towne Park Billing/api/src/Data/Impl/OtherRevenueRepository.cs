using api.Functions;
using api.Services;
using api.Usecases;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using TownePark;

namespace api.Data.Impl
{
    public class OtherRevenueRepository : IOtherRevenueRepository
    {
        private readonly IDataverseService _dataverseService;
        private readonly IMonthRangeGenerator _monthRangeGenerator;

        public OtherRevenueRepository(IDataverseService dataverseService, IMonthRangeGenerator monthRangeGenerator)
        {
            _dataverseService = dataverseService;
            _monthRangeGenerator = monthRangeGenerator;
        }

        public IEnumerable<bs_OtherRevenueDetail>? GetOtherRevenueDetail(Guid siteId, string startingMonth)
        {
            var serviceClient = _dataverseService.GetServiceClient();

            var months = _monthRangeGenerator.GenerateMonthRange(startingMonth, 12);

            var query = new QueryExpression(bs_OtherRevenueDetail.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(
                    bs_OtherRevenueDetail.Fields.bs_OtherRevenueDetailId,
                    bs_OtherRevenueDetail.Fields.bs_MonthYear,
                    bs_OtherRevenueDetail.Fields.bs_BillableExpense,
                    bs_OtherRevenueDetail.Fields.bs_Credits,
                    bs_OtherRevenueDetail.Fields.bs_GPOFees,
                    bs_OtherRevenueDetail.Fields.bs_RevenueValidation,
                    bs_OtherRevenueDetail.Fields.bs_SigningBonus,
                    bs_OtherRevenueDetail.Fields.bs_ClientPaidExpense,
                    bs_OtherRevenueDetail.Fields.bs_Miscellaneous,
                    bs_OtherRevenueDetail.Fields.bs_CustomerSiteFK
                ),
                Criteria = new FilterExpression(LogicalOperator.And)
            };

            query.Criteria.AddCondition(
                bs_OtherRevenueDetail.Fields.bs_CustomerSiteFK,
                ConditionOperator.Equal,
                siteId);

            query.Criteria.AddCondition(
                bs_OtherRevenueDetail.Fields.bs_MonthYear,
                ConditionOperator.In,
                months.ToArray());

            var result = serviceClient.RetrieveMultiple(query);

            if (result.Entities.Count == 0)
                return null;

            var otherRevenueDetails = result.Entities
                .Select(e => e.ToEntity<bs_OtherRevenueDetail>())
                .ToList();

            return otherRevenueDetails;
        }

        public void UpdateOtherRevenueDetails(List<bs_OtherRevenueDetail> details)
        {
            var serviceClient = _dataverseService.GetServiceClient();

            foreach (var detail in details)
            {
                if (detail.Id == Guid.Empty)
                {
                    detail.Id = Guid.NewGuid();
                    serviceClient.Create(detail);
                }
                else
                {
                    serviceClient.Update(detail);
                }
            }
        }
    }
}
