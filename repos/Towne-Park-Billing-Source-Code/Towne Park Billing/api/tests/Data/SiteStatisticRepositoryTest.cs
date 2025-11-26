using api.Data.Impl;
using api.Services;
using api.Usecases;
using FluentAssertions;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NSubstitute;
using System.Linq;
using TownePark;
using Xunit;

namespace BackendTests.Data
{
    public class SiteStatisticRepositoryTest
    {
        private readonly IDataverseService _dataverseService;
        private readonly IOrganizationService _organizationService;
        private readonly SiteStatisticRepository _siteStatisticRepository;
        private readonly IMonthRangeGenerator _monthRangeGenerator;

        public SiteStatisticRepositoryTest()
        {
            _dataverseService = Substitute.For<IDataverseService>();
            _organizationService = Substitute.For<IOrganizationService>();
            _monthRangeGenerator = Substitute.For<IMonthRangeGenerator>();
            _dataverseService.GetServiceClient().Returns(_organizationService);

            _siteStatisticRepository = new SiteStatisticRepository(_dataverseService, _monthRangeGenerator);
        }

        [Fact]
        public void GetSiteStatistics_ShouldReturnStatRecord()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var siteStatisticId = Guid.NewGuid();
            var billingPeriod = "2025-07";

            // Create entity for site statistic response
            var siteStatisticEntity = new Entity("bs_sitestatistic")
            {
                Id = siteStatisticId,
                ["bs_customersiteFK"] = new EntityReference("bs_customersite", siteId),
                ["bs_billingperiod"] = billingPeriod,
                ["bs_name"] = "Hotel Parking"
            };

            // Add aliased value for CustomerSite.TotalRoomsAvailable
            siteStatisticEntity["customersite.bs_totalroomsavailable"] = new AliasedValue(
                "bs_customersite",
                "bs_totalroomsavailable",
                "100"
            );

            // Create entity collection for first query response
            var siteStatisticCollection = new EntityCollection
            {
                Entities = { siteStatisticEntity }
            };

            // Create entities for forecast and budget details
            var forecastDetail = new Entity("bs_sitestatisticdetail")
            {
                Id = Guid.NewGuid(),
                ["bs_sitestatisticfk"] = new EntityReference("bs_sitestatistic", siteStatisticId),
                ["bs_type"] = new OptionSetValue(283590000), // Forecast type
                ["bs_date"] = DateTime.Today
                // Add other relevant fields...
            };

            var budgetDetail = new Entity("bs_sitestatisticdetail")
            {
                Id = Guid.NewGuid(),
                ["bs_sitestatisticfk"] = new EntityReference("bs_sitestatistic", siteStatisticId),
                ["bs_type"] = new OptionSetValue(283590001), // Budget type
                ["bs_date"] = DateTime.Today
                // Add other relevant fields...
            };

            var actualDetail = new Entity("bs_sitestatisticdetail")
            {
                Id = Guid.NewGuid(),
                ["bs_sitestatisticfk"] = new EntityReference("bs_sitestatistic", siteStatisticId),
                ["bs_type"] = new OptionSetValue(283590002), // Actual type
                ["bs_date"] = DateTime.Today
                // Add other relevant fields...
            };

            // Create entity collection for second query response
            var detailsCollection = new EntityCollection
            {
                Entities = { forecastDetail, budgetDetail }
            };

            // Mock the service client
            var mockServiceClient = Substitute.For<IOrganizationServiceAsync>();

            // Set up the mock to return different responses based on the query
            mockServiceClient.RetrieveMultiple(Arg.Is<QueryExpression>(q => q.EntityName == "bs_sitestatistic"))
                .Returns(siteStatisticCollection);

            mockServiceClient.RetrieveMultiple(Arg.Is<QueryExpression>(q => q.EntityName == "bs_sitestatisticdetail"))
                .Returns(detailsCollection);

            // Mock dataverseService to return our mockServiceClient
            _dataverseService.GetServiceClient().Returns(mockServiceClient);

            // Act
            var result = _siteStatisticRepository.GetSiteStatistics(siteId, billingPeriod);

            // Assert
            // First verify the first query
            mockServiceClient.Received().RetrieveMultiple(Arg.Is<QueryExpression>(query =>
                query.EntityName == "bs_sitestatistic" &&
                query.Criteria.Conditions.Any(c =>
                    c.AttributeName == "bs_customersitefk" &&
                    c.Operator == ConditionOperator.Equal &&
                    c.Values.Contains(siteId)) &&
                query.Criteria.Conditions.Any(c =>
                    c.AttributeName == "bs_billingperiod" &&
                    c.Operator == ConditionOperator.Equal &&
                    c.Values.Contains(billingPeriod))
            ));

            // Then verify the second query
            mockServiceClient.Received().RetrieveMultiple(Arg.Is<QueryExpression>(query =>
                query.EntityName == "bs_sitestatisticdetail" &&
                query.Criteria.Conditions.Any(c =>
                    c.AttributeName == "bs_sitestatisticfk" &&
                    c.Operator == ConditionOperator.Equal &&
                    c.Values.Contains(siteStatisticId))
            ));

            // Finally verify the result
            result.Should().NotBeNull();
            result.bs_BillingPeriod.Should().Be(billingPeriod);
            result.bs_SiteStatistic_SiteStatisticDetail.Should().NotBeNull();
            result.bs_SiteStatistic_SiteStatisticDetail.Should().HaveCount(2);
            result.bs_SiteStatistic_CustomerSite.Should().NotBeNull();
            result.bs_SiteStatistic_CustomerSite.bs_TotalRoomsAvailable.Should().Be("100");
        }

        [Fact]
        public void GetSiteStatistics_ShouldReturnNull_WhenNoRecordFound()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var billingPeriod = "2025-07";
            // Mock the service client
            var mockServiceClient = Substitute.For<IOrganizationServiceAsync>();
            // Set up the mock to return an empty collection
            mockServiceClient.RetrieveMultiple(Arg.Is<QueryExpression>(q => q.EntityName == "bs_sitestatistic"))
                .Returns(new EntityCollection());
            // Mock dataverseService to return our mockServiceClient
            _dataverseService.GetServiceClient().Returns(mockServiceClient);
            // Act
            var result = _siteStatisticRepository.GetSiteStatistics(siteId, billingPeriod);
            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void SaveSiteStatistics_ShouldUpdate_WhenUpdateIsSuccessful()
        {
            var siteStatistic = new bs_SiteStatistic
            {
                Id = Guid.NewGuid(),
                bs_Name = "1001",
                bs_CustomerSiteFK = new EntityReference("bs_customersite", Guid.NewGuid()),
                bs_BillingPeriod = "2025-07",
                bs_SiteStatistic_CustomerSite = new bs_CustomerSite
                {
                    bs_TotalRoomsAvailable = "100"
                },
                bs_SiteStatistic_SiteStatisticDetail = new List<bs_SiteStatisticDetail>
                {
                    new bs_SiteStatisticDetail
                    {
                        bs_Type = bs_sitestatisticdetailchoice.Forecast,
                        bs_Date = new DateTime(2025, 7, 1),
                        bs_SiteStatisticDetailId = Guid.NewGuid(),
                        bs_SiteStatisticFK = new EntityReference("bs_sitestatistic", Guid.NewGuid()),
                        bs_ValetRateDaily = 10,
                        bs_ValetRateMonthly = 200,
                        bs_SelfRateDaily = 5,
                        bs_SelfRateMonthly = 100,
                        bs_BaseRevenue = 7000,
                        bs_OccupiedRooms = 70,
                        bs_Occupancy = 0.91m,
                        bs_SelfOvernight = 50,
                        bs_ValetOvernight = 20,
                        bs_ValetDaily = 40,
                        bs_ValetMonthly = 60,
                        bs_SelfDaily = 30,
                        bs_SelfMonthly = 10,
                        bs_ValetComps = 5,
                        bs_SelfComps = 5,
                        bs_DriveInRatio = (decimal)0.5,
                        bs_CaptureRatio = (decimal)0.87,
                        bs_Name = "Hotel Parking"
                    },
                    new bs_SiteStatisticDetail
                    {
                        bs_Type = bs_sitestatisticdetailchoice.Budget,
                        bs_Date = new DateTime(2025, 7, 1),
                        bs_SiteStatisticDetailId = Guid.NewGuid(),
                        bs_SiteStatisticFK = new EntityReference("bs_sitestatistic", Guid.NewGuid()),
                        bs_ValetRateDaily = 10,
                        bs_ValetRateMonthly = 200,
                        bs_SelfRateDaily = 5,
                        bs_SelfRateMonthly = 100,
                        bs_BaseRevenue = 7000,
                        bs_OccupiedRooms = 70,
                        bs_Occupancy = 0.91m,
                        bs_SelfOvernight = 50,
                        bs_ValetOvernight = 20,
                        bs_ValetDaily = 40,
                        bs_ValetMonthly = 60,
                        bs_SelfDaily = 30,
                        bs_SelfMonthly = 10,
                        bs_ValetComps = 5,
                        bs_SelfComps = 5,
                        bs_DriveInRatio = (decimal)0.5,
                        bs_CaptureRatio = (decimal)0.87,
                        bs_Name = "Hotel Parking"
                    }
                }
            };

            _siteStatisticRepository.SaveSiteStatistics(siteStatistic);

            _organizationService.Received().Update(Arg.Any<bs_SiteStatistic>());
        }

        [Fact]
        public void SaveSiteStatistics_ShouldSaveAdjustmentFields_WhenProvided()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var siteStatisticId = Guid.NewGuid();
            var detailId = Guid.NewGuid();

            var siteStatistic = new bs_SiteStatistic
            {
                bs_SiteStatisticId = siteStatisticId,
                bs_CustomerSiteFK = new EntityReference("bs_customersite", siteId),
                bs_BillingPeriod = "2025-07",
                bs_Name = "Hotel Parking",
                bs_SiteStatistic_CustomerSite = new bs_CustomerSite
                {
                    bs_TotalRoomsAvailable = "100"
                },
                bs_SiteStatistic_SiteStatisticDetail = new List<bs_SiteStatisticDetail>
                {
                    new bs_SiteStatisticDetail
                    {
                        bs_Type = bs_sitestatisticdetailchoice.Forecast,
                        bs_Date = new DateTime(2025, 7, 1),
                        bs_SiteStatisticDetailId = detailId,
                        bs_SiteStatisticFK = new EntityReference("bs_sitestatistic", siteStatisticId),
                        bs_ExternalRevenue = 10000,
                        bs_AdjustmentPercentage = -3.42m,
                        bs_AdjustmentValue = -342m,
                        bs_ValetRateDaily = 10,
                        bs_SelfRateDaily = 5,
                        bs_OccupiedRooms = 70,
                        bs_Occupancy = 0.91m
                    }
                }
            };

            // Act
            _siteStatisticRepository.SaveSiteStatistics(siteStatistic);

            // Assert
            _organizationService.Received().Update(Arg.Is<bs_SiteStatistic>(s => 
                s.bs_SiteStatistic_SiteStatisticDetail.Count() == 1 &&
                s.bs_SiteStatistic_SiteStatisticDetail.First().bs_AdjustmentPercentage == -3.42m &&
                s.bs_SiteStatistic_SiteStatisticDetail.First().bs_AdjustmentValue == -342m
            ));
        }

        [Fact]
        public void CreateSiteStatistics_ShouldCreateWithAdjustmentFields()
        {
            // Arrange
            var siteId = Guid.NewGuid();

            var siteStatistic = new bs_SiteStatistic
            {
                bs_CustomerSiteFK = new EntityReference("bs_customersite", siteId),
                bs_BillingPeriod = "2025-07",
                bs_Name = "Hotel Parking",
                bs_SiteStatistic_CustomerSite = new bs_CustomerSite
                {
                    bs_TotalRoomsAvailable = "100"
                },
                bs_SiteStatistic_SiteStatisticDetail = new List<bs_SiteStatisticDetail>
                {
                    new bs_SiteStatisticDetail
                    {
                        bs_Type = bs_sitestatisticdetailchoice.Budget,
                        bs_Date = new DateTime(2025, 7, 1),
                        bs_ExternalRevenue = 12000,
                        bs_AdjustmentPercentage = -5.09m,
                        bs_AdjustmentValue = -610.8m,
                        bs_ValetRateDaily = 15,
                        bs_SelfRateDaily = 10,
                        bs_OccupiedRooms = 80,
                        bs_Occupancy = 0.80m
                    }
                }
            };

            // Act
            _siteStatisticRepository.CreateSiteStatistics(siteStatistic);

            // Assert
            // Verify parent entity is created without details
            _organizationService.Received(1).Create(Arg.Is<bs_SiteStatistic>(s => 
                s.bs_SiteStatistic_SiteStatisticDetail == null &&
                s.bs_BillingPeriod == "2025-07" &&
                s.bs_Name == "Hotel Parking"
            ));
            
            // Verify detail entity is created separately with adjustment fields
            _organizationService.Received(1).Create(Arg.Is<bs_SiteStatisticDetail>(d =>
                d.bs_Type == bs_sitestatisticdetailchoice.Budget &&
                d.bs_AdjustmentPercentage == -5.09m &&
                d.bs_AdjustmentValue == -610.8m
            ));
        }

        [Fact(Skip = "Requires EDW_DATA_API_ENDPOINT environment variable")]
        public async Task GetBudgetDataForRange_ShouldReturnAdjustmentFields()
        {
            // Arrange
            var siteNumber = "0111";
            var months = new List<string> { "2025-07" };
            
            var budgetDetail = new Entity("bs_sitestatisticdetail")
            {
                Id = Guid.NewGuid(),
                ["bs_type"] = new OptionSetValue(126840000), // Budget type
                ["bs_date"] = new DateTime(2025, 7, 1),
                ["bs_externalrevenue"] = 10000m,
                ["bs_adjustmentpercentage"] = -3.42m,
                ["bs_adjustmentvalue"] = -342m,
                ["bs_valetratedaily"] = 10m,
                ["bs_selfratedaily"] = 5m,
                ["bs_occupiedrooms"] = 70m,
                ["bs_occupancy"] = 0.91m
            };
            
            var detailsCollection = new EntityCollection
            {
                Entities = { budgetDetail }
            };
            
            _organizationService.RetrieveMultiple(Arg.Any<QueryExpression>())
                .Returns(detailsCollection);
            
            // Act
            var result = await _siteStatisticRepository.GetBudgetDataForRange(siteNumber, months);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            
            var firstDetail = result.First();
            firstDetail.ExternalRevenue.Should().Be(10000m);
            firstDetail.AdjustmentPercentage.Should().Be(-3.42m);
            firstDetail.AdjustmentValue.Should().Be(-342m);
        }

        [Fact(Skip = "Requires EDW_DATA_API_ENDPOINT environment variable")]
        public async Task GetActualDataForRange_ShouldHandleNullAdjustmentFields()
        {
            // Arrange
            var siteNumber = "0111";
            var months = new List<string> { "2025-07" };
            
            var actualDetail = new Entity("bs_sitestatisticdetail")
            {
                Id = Guid.NewGuid(),
                ["bs_type"] = new OptionSetValue(126840002), // Actual type
                ["bs_date"] = new DateTime(2025, 7, 1),
                ["bs_externalrevenue"] = 8000m,
                ["bs_adjustmentpercentage"] = null,
                ["bs_adjustmentvalue"] = null,
                ["bs_valetratedaily"] = 8m,
                ["bs_selfratedaily"] = 4m,
                ["bs_occupiedrooms"] = 60m,
                ["bs_occupancy"] = 0.60m
            };
            
            var detailsCollection = new EntityCollection
            {
                Entities = { actualDetail }
            };
            
            _organizationService.RetrieveMultiple(Arg.Any<QueryExpression>())
                .Returns(detailsCollection);
            
            // Act
            var result = await _siteStatisticRepository.GetActualDataForRange(siteNumber, months);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            
            var firstDetail = result.First();
            firstDetail.ExternalRevenue.Should().Be(8000m);
            firstDetail.AdjustmentPercentage.Should().BeNull();
            firstDetail.AdjustmentValue.Should().BeNull();
        }
    }
}
