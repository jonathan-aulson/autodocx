using System;
using System.Collections.Generic;
using System.Linq;
using api.Data.Impl;
using api.Services;
using TownePark;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NSubstitute;
using Xunit;

namespace BackendTests.Data
{
    public class ActionOverridesRepositoryTests
    {
        private readonly IDataverseService _dataverseService;
        private readonly IOrganizationService _organizationService;
        private readonly ActionOverridesRepository _repository;

        public ActionOverridesRepositoryTests()
        {
            _dataverseService = Substitute.For<IDataverseService>();
            _organizationService = Substitute.For<IOrganizationService>();
            _dataverseService.GetServiceClient().Returns(_organizationService);
            _repository = new ActionOverridesRepository(_dataverseService);
        }

        [Fact]
        public void GetActionOverrideValueByName_ReturnsEntity_WhenFound()
        {
            var expectedName = "TestOverride";
            var expectedValue = "123,456";
            var entity = new Entity(bs_ActionOverrides.EntityLogicalName)
            {
                [bs_ActionOverrides.Fields.bs_Name] = expectedName,
                [bs_ActionOverrides.Fields.bs_Value] = expectedValue,
                [bs_ActionOverrides.Fields.bs_ActionOverridesId] = Guid.NewGuid()
            };

            var entities = new EntityCollection(new List<Entity> { entity });
            _organizationService.RetrieveMultiple(Arg.Any<QueryExpression>()).Returns(entities);

            var result = _repository.GetActionOverrideValueByName(expectedName);

            Assert.NotNull(result);
            Assert.Equal(expectedName, result.bs_Name);
            Assert.Equal(expectedValue, result.bs_Value);
        }

        [Fact]
        public void GetActionOverrideValueByName_ReturnsNull_WhenNotFound()
        {
            var entities = new EntityCollection(new List<Entity>());
            _organizationService.RetrieveMultiple(Arg.Any<QueryExpression>()).Returns(entities);

            var result = _repository.GetActionOverrideValueByName("NonExistent");

            Assert.Null(result);
        }
    }
}
