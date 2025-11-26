using api.Data.Impl;
using api.Services;
using api.Usecases;
using FluentAssertions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NSubstitute;
using TownePark;
using Xunit;

namespace BackendTests.Data
{
    public class OtherRevenueRepositoryTest
    {
        private readonly IOrganizationService _organizationService;
        private readonly OtherRevenueRepository _otherRevenueRepository;
        private readonly IMonthRangeGenerator _monthRangeGenerator;

        public OtherRevenueRepositoryTest()
        {
            var dataverseService = Substitute.For<IDataverseService>();
            _monthRangeGenerator = Substitute.For<IMonthRangeGenerator>();
            _organizationService = Substitute.For<IOrganizationService>();
            dataverseService.GetServiceClient().Returns(_organizationService);
            _otherRevenueRepository = new OtherRevenueRepository(dataverseService, _monthRangeGenerator);
        }

        [Fact]
        public void GetOtherRevenueDetail_ShouldReturnOtherRevenueDetail()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var billingPeriod = "2025-07";
            var months = new List<string> { "2025-07", "2025-08" };

            _monthRangeGenerator.GenerateMonthRange(billingPeriod, 12).Returns(months);

            var expectedDetail = new bs_OtherRevenueDetail
            {
                Id = Guid.NewGuid(),
                bs_BillableExpense = 100.00m,
                bs_Credits = 200.00m,
                bs_GPOFees = 50.00m,
                bs_RevenueValidation = 75.00m,
                bs_SigningBonus = 80.00m,
                bs_ClientPaidExpense = 120.00m,
                bs_Miscellaneous = 30.00m,
                bs_MonthYear = "2025-07",
                bs_CustomerSiteFK = new EntityReference("bs_customersite", siteId)
            };

            var entity = expectedDetail.ToEntity<bs_OtherRevenueDetail>();
            var entityCollection = new EntityCollection(new List<Entity> { entity });

            QueryExpression capturedQuery = null;
            _organizationService.RetrieveMultiple(Arg.Do<QueryExpression>(q => capturedQuery = q))
                .Returns(entityCollection);

            // Act
            var result = _otherRevenueRepository.GetOtherRevenueDetail(siteId, billingPeriod);

            // Assert
            capturedQuery.Should().NotBeNull();
            capturedQuery.ColumnSet.Columns.Should().BeEquivalentTo(new[]
            {
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
            });

            var siteIdCondition = capturedQuery.Criteria.Conditions
               .FirstOrDefault(c => c.AttributeName == bs_OtherRevenueDetail.Fields.bs_CustomerSiteFK);
            siteIdCondition.Should().NotBeNull();
            siteIdCondition.Values.Should().Contain(siteId);

            var monthYearCondition = capturedQuery.Criteria.Conditions
               .FirstOrDefault(c => c.AttributeName == bs_OtherRevenueDetail.Fields.bs_MonthYear);
            monthYearCondition.Should().NotBeNull();
            monthYearCondition.Values.Should().BeEquivalentTo(months);

            result.Should().NotBeNull();
            result.Should().ContainSingle();
            var actualDetail = result.First();
            actualDetail.Should().BeEquivalentTo(expectedDetail, options => options
                .ExcludingMissingMembers());
        }

        [Fact]
        public void UpdateOtherRevenueDetails_ShouldUpdateOtherRevenueDetails()
        {
            // Arrange
            var details = new List<bs_OtherRevenueDetail>
            {
                new bs_OtherRevenueDetail
                {
                    Id = Guid.NewGuid(),
                    bs_BillableExpense = 100.00m,
                    bs_Credits = 200.00m,
                    bs_GPOFees = 50.00m,
                    bs_RevenueValidation = 75.00m,
                    bs_SigningBonus = 80.00m,
                    bs_ClientPaidExpense = 120.00m,
                    bs_MonthYear = "2025-07",
                }
            };
            // Act
            _otherRevenueRepository.UpdateOtherRevenueDetails(details);
            // Assert
            foreach (var detail in details)
            {
                _organizationService.Received(1).Update(Arg.Is<Entity>(e => e.Id == detail.Id));
            }
        }
    }
}
