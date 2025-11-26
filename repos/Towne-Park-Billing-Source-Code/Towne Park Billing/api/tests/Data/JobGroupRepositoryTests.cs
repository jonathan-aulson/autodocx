using System;
using System.Collections.Generic;
using System.Linq;
using api.Data.Impl;
using api.Models.Dto;
using api.Services;
using FluentAssertions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using NSubstitute;
using TownePark;
using Xunit;

namespace BackendTests.Data
{
    public class JobGroupRepositoryTests
    {
        private readonly IDataverseService _dataverseService;
        private readonly IOrganizationService _organizationService;
        private readonly JobGroupRepository _repository;

        public JobGroupRepositoryTests()
        {
            _dataverseService = Substitute.For<IDataverseService>();
            _organizationService = Substitute.For<IOrganizationService>();
            _dataverseService.GetServiceClient().Returns(_organizationService);
            _repository = new JobGroupRepository(_dataverseService);
        }

        [Fact]
        public void CreateJobGroup_CreatesJobGroupWithCorrectValues()
        {
            // Arrange
            var groupTitle = "Test Group";

            // Act
            _repository.CreateJobGroup(groupTitle);

            // Assert
            _organizationService.Received(1).Create(Arg.Is<bs_JobGroup>(jg =>
                jg.bs_JobGroupTitle == groupTitle &&
                jg.bs_IsActive == true
            ));
        }

        [Fact]
        public void DeactivateJobGroup_UpdatesJobGroupWithIsActiveFalse()
        {
            // Arrange
            var jobGroupId = Guid.NewGuid();

            // Act
            _repository.DeactivateJobGroup(jobGroupId);

            // Assert
            _organizationService.Received(1).Update(Arg.Is<bs_JobGroup>(jg =>
                jg.Id == jobGroupId &&
                jg.bs_IsActive == false
            ));
        }

        [Fact]
        public void ActivateJobGroup_UpdatesJobGroupWithIsActiveTrue()
        {
            // Arrange
            var jobGroupId = Guid.NewGuid();

            // Act
            _repository.ActivateJobGroup(jobGroupId);

            // Assert
            _organizationService.Received(1).Update(Arg.Is<bs_JobGroup>(jg =>
                jg.Id == jobGroupId &&
                jg.bs_IsActive == true
            ));
        }

        [Fact]
        public void GetAllJobGroups_ReturnsMappedJobGroups()
        {
            // Arrange: Setup fake job group and job code entities
            var groupId = Guid.NewGuid();
            var codeId = Guid.NewGuid();

            var groupEntity = new Entity("bs_jobgroup", groupId)
            {
                ["bs_jobgrouptitle"] = "Group 1",
                ["bs_isactive"] = true
            };
            var codeEntity = new Entity("bs_jobcode", codeId)
            {
                ["bs_jobtitle"] = "Code 1",
                ["bs_isactive"] = true,
                ["bs_jobgroupfk"] = new EntityReference("bs_jobgroup", groupId)
            };

            var groupEntities = new EntityCollection(new List<Entity> { groupEntity });
            var codeEntities = new EntityCollection(new List<Entity> { codeEntity });

            _organizationService.RetrieveMultiple(Arg.Is<QueryExpression>(q => q.EntityName == "bs_jobgroup"))
                .Returns(groupEntities);
            _organizationService.RetrieveMultiple(Arg.Is<QueryExpression>(q => q.EntityName == "bs_jobcode"))
                .Returns(codeEntities);

            // Act
            var result = _repository.GetAllJobGroups().ToList();

            // Assert
            result.Should().HaveCount(1);
            result[0].Id.Should().Be(groupId);
            result[0].Title.Should().Be("Group 1");
            result[0].IsActive.Should().BeTrue();
            result[0].JobCodes.Should().HaveCount(1);
            result[0].JobCodes[0].JobCodeId.Should().Be(codeId);
            result[0].JobCodes[0].JobTitle.Should().Be("Code 1");
            result[0].JobCodes[0].IsActive.Should().BeTrue();
        }
    }
}
