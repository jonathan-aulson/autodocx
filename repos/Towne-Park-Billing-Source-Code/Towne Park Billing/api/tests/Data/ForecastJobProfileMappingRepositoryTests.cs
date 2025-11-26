using System;
using System.Collections.Generic;
using System.Linq;
using api.Data.Impl;
using api.Services;
using FluentAssertions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NSubstitute;
using TownePark;
using Xunit;

namespace BackendTests.Data
{
    public class ForecastJobProfileMappingRepositoryTests
    {
        private readonly IDataverseService _dataverseServiceMock;
        private readonly IOrganizationService _serviceClientMock;
        private readonly ForecastJobProfileMappingRepository _repository;

        public ForecastJobProfileMappingRepositoryTests()
        {
            _dataverseServiceMock = Substitute.For<IDataverseService>();
            _serviceClientMock = Substitute.For<IOrganizationService>();
            _dataverseServiceMock.GetServiceClient().Returns(_serviceClientMock);
            _repository = new ForecastJobProfileMappingRepository(_dataverseServiceMock);
        }

        [Fact]
        public void GetForecastJobProfileMappingsByCustomerSite_ShouldReturnMappingsForCustomerSite()
        {
            // Arrange
            var customerSiteId = Guid.NewGuid();
            var expectedEntities = new EntityCollection(new List<Entity>
            {
                new bs_ForecastJobProfileMapping
                {
                    [bs_ForecastJobProfileMapping.Fields.bs_ForecastJobProfileMappingId] = Guid.NewGuid(),
                    [bs_ForecastJobProfileMapping.Fields.bs_JobCode] = "VALET",
                    [bs_ForecastJobProfileMapping.Fields.bs_JobProfile] = "Valet Attendant",
                    [bs_ForecastJobProfileMapping.Fields.bs_Name] = "Valet Mapping",
                    [bs_ForecastJobProfileMapping.Fields.bs_CustomerSiteFK] = customerSiteId
                },
                new bs_ForecastJobProfileMapping
                {
                    [bs_ForecastJobProfileMapping.Fields.bs_ForecastJobProfileMappingId] = Guid.NewGuid(),
                    [bs_ForecastJobProfileMapping.Fields.bs_JobCode] = "CASHIER",
                    [bs_ForecastJobProfileMapping.Fields.bs_JobProfile] = "Cashier",
                    [bs_ForecastJobProfileMapping.Fields.bs_Name] = "Cashier Mapping",
                    [bs_ForecastJobProfileMapping.Fields.bs_CustomerSiteFK] = customerSiteId
                }
            });

            _serviceClientMock
                .RetrieveMultiple(Arg.Is<QueryBase>(query =>
                    ValidateQuery(query, bs_ForecastJobProfileMapping.EntityLogicalName, bs_ForecastJobProfileMapping.Fields.bs_CustomerSiteFK, customerSiteId)))
                .Returns(expectedEntities);

            // Act
            var result = _repository.GetForecastJobProfileMappingsByCustomerSite(customerSiteId);

            // Assert
            result.Should().BeEquivalentTo(expectedEntities.Entities.Cast<bs_ForecastJobProfileMapping>());
        }

        [Fact]
        public void GetForecastJobProfileMappingsByCustomerSite_ShouldCallDataverseServiceOnce()
        {
            // Arrange
            var customerSiteId = Guid.NewGuid();
            var expectedEntities = new EntityCollection();
            _serviceClientMock.RetrieveMultiple(Arg.Any<QueryExpression>()).Returns(expectedEntities);

            // Act
            _repository.GetForecastJobProfileMappingsByCustomerSite(customerSiteId);

            // Assert
            _dataverseServiceMock.Received(1).GetServiceClient();
            _serviceClientMock.Received(1).RetrieveMultiple(Arg.Any<QueryExpression>());
        }

        private bool ValidateQuery(QueryBase queryBase, string expectedEntityName, string expectedFieldName, object expectedValue)
        {
            if (queryBase is QueryExpression query)
            {
                var condition = query.Criteria.Conditions.FirstOrDefault(c => c.AttributeName == expectedFieldName);
                return query.EntityName == expectedEntityName &&
                       condition != null &&
                       condition.Operator == ConditionOperator.Equal &&
                       condition.Values.Contains(expectedValue);
            }
            return false;
        }
    }
}
