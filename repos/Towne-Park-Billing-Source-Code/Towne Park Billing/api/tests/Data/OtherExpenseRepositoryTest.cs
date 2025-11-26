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
    public class OtherExpenseRepositoryTest
    {
        private readonly IOrganizationService _organizationService;
        private readonly OtherExpenseRepository _otherExpenseRepository;
        private readonly IMonthRangeGenerator _monthRangeGenerator;

        public OtherExpenseRepositoryTest()
        {
            var dataverseService = Substitute.For<IDataverseService>();
            _monthRangeGenerator = Substitute.For<IMonthRangeGenerator>();
            _organizationService = Substitute.For<IOrganizationService>();
            dataverseService.GetServiceClient().Returns(_organizationService);
            _otherExpenseRepository = new OtherExpenseRepository(dataverseService, _monthRangeGenerator);
        }

        [Fact]
        public void GetOtherExpenseDetail_ShouldReturnOtherExpenseDetail()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var billingPeriod = "2025-07";
            var months = new List<string> { "2025-07", "2025-08" };

            _monthRangeGenerator.GenerateMonthRange(billingPeriod, 12).Returns(months);

            var expectedDetail = new bs_OtherExpenseDetail
            {
                Id = Guid.NewGuid(),
                bs_EmployeeRelations = 100.00m,
                bs_FuelVehicles = 200.00m,
                bs_LossAndDamageClaims = 50.00m,
                bs_OfficeSupplies = 75.00m,
                bs_OutsideServices = 80.00m,
                bs_RentsParking = 120.00m,
                bs_RepairsAndMaintenance = 60.00m,
                bs_RepairsAndMaintenanceVehicle = 30.00m,
                bs_Signage = 25.00m,
                bs_SuppliesAndEquipment = 40.00m,
                bs_TicketsAndPrintedMaterial = 15.00m,
                bs_Uniforms = 10.00m,
                bs_MonthYear = "2025-07",
                bs_MiscOtherExpenses = 0.00m,
                bs_TotalOtherExpenses = 0.00m,
                bs_CustomerSiteFK = new EntityReference("bs_customersite", siteId)
            };

            var entity = expectedDetail.ToEntity<bs_OtherExpenseDetail>();
            var entityCollection = new EntityCollection(new List<Entity> { entity });

            QueryExpression capturedQuery = null;
            _organizationService.RetrieveMultiple(Arg.Do<QueryExpression>(q => capturedQuery = q))
                .Returns(entityCollection);

            // Act
            var result = _otherExpenseRepository.GetOtherExpenseDetail(siteId, billingPeriod);

            // Assert
            // Check the query columns
            capturedQuery.Should().NotBeNull();
            capturedQuery.ColumnSet.Columns.Should().BeEquivalentTo(new[]
            {
                bs_OtherExpenseDetail.Fields.bs_OtherExpenseDetailId,
                bs_OtherExpenseDetail.Fields.bs_EmployeeRelations,
                bs_OtherExpenseDetail.Fields.bs_FuelVehicles,
                bs_OtherExpenseDetail.Fields.bs_LossAndDamageClaims,
                bs_OtherExpenseDetail.Fields.bs_OfficeSupplies,
                bs_OtherExpenseDetail.Fields.bs_OutsideServices,
                bs_OtherExpenseDetail.Fields.bs_RentsParking,
                bs_OtherExpenseDetail.Fields.bs_RepairsAndMaintenance,
                bs_OtherExpenseDetail.Fields.bs_RepairsAndMaintenanceVehicle,
                bs_OtherExpenseDetail.Fields.bs_Signage,
                bs_OtherExpenseDetail.Fields.bs_SuppliesAndEquipment,
                bs_OtherExpenseDetail.Fields.bs_TicketsAndPrintedMaterial,
                bs_OtherExpenseDetail.Fields.bs_Uniforms,
                bs_OtherExpenseDetail.Fields.bs_MonthYear,
                bs_OtherExpenseDetail.Fields.bs_MiscOtherExpenses,
                bs_OtherExpenseDetail.Fields.bs_TotalOtherExpenses,
                bs_OtherExpenseDetail.Fields.bs_CustomerSiteFK
            });

            // Check the query criteria
            var siteIdCondition = capturedQuery.Criteria.Conditions
               .FirstOrDefault(c => c.AttributeName == bs_OtherExpenseDetail.Fields.bs_CustomerSiteFK);
            siteIdCondition.Should().NotBeNull();
            siteIdCondition.Values.Should().Contain(siteId);

            var monthYearCondition = capturedQuery.Criteria.Conditions
               .FirstOrDefault(c => c.AttributeName == bs_OtherExpenseDetail.Fields.bs_MonthYear);
            monthYearCondition.Should().NotBeNull();
            monthYearCondition.Values.Should().BeEquivalentTo(months);

            // Check the result
            result.Should().NotBeNull();
            result.Should().ContainSingle();
            var actualDetail = result.First();
            actualDetail.Should().BeEquivalentTo(expectedDetail, options => options
                .ExcludingMissingMembers());
        }

        [Fact]
        public void UpdateOtherExpenseDetails_ShouldUpdateOtherExpenseDetails()
        {
            // Arrange
            var details = new List<bs_OtherExpenseDetail>
            {
                new bs_OtherExpenseDetail
                {
                    Id = Guid.NewGuid(),
                    bs_EmployeeRelations = 100.00m,
                    bs_FuelVehicles = 200.00m,
                    bs_LossAndDamageClaims = 50.00m,
                    bs_OfficeSupplies = 75.00m,
                    bs_OutsideServices = 80.00m,
                    bs_RentsParking = 120.00m,
                    bs_RepairsAndMaintenance = 60.00m,
                    bs_RepairsAndMaintenanceVehicle = 30.00m,
                    bs_Signage = 25.00m,
                    bs_SuppliesAndEquipment = 40.00m,
                    bs_TicketsAndPrintedMaterial = 15.00m,
                    bs_Uniforms = 10.00m,
                    bs_MonthYear = "2025-07",
                }
            };
            // Act
            _otherExpenseRepository.UpdateOtherRevenueDetails(details);
            // Assert
            foreach (var detail in details)
            {
                _organizationService.Received(1).Update(Arg.Is<Entity>(e => e.Id == detail.Id));
            }
        }
    }
}
