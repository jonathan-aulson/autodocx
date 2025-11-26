using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    public class JobCodeRepositoryTests
    {
        private readonly IDataverseService _dataverseService;
        private readonly IOrganizationService _organizationService;
        private readonly JobCodeRepository _repository;

        public JobCodeRepositoryTests()
        {
            _dataverseService = Substitute.For<IDataverseService>();
            _organizationService = Substitute.For<IOrganizationService>();
            _dataverseService.GetServiceClient().Returns(_organizationService);
            _repository = new JobCodeRepository(_dataverseService);
        }

        [Fact]
        public async Task GetJobCodesAsync_ReturnsJobCodes()
        {
            var entities = new EntityCollection(new List<Entity>
            {
                CreateJobCodeEntity(Guid.NewGuid(), "JC1", "Job Code 1", "Job Title 1", true),
                CreateJobCodeEntity(Guid.NewGuid(), "JC2", "Job Code 2", "Job Title 2", false)
            });

            _organizationService.RetrieveMultiple(Arg.Any<Microsoft.Xrm.Sdk.Query.QueryExpression>()).Returns(entities);

            var result = await _repository.GetJobCodesAsync();

            result.Should().HaveCount(2);
            result[0].JobCode.Should().Be("JC1");
            result[1].IsActive.Should().BeFalse();
        }

        private Entity CreateJobCodeEntity(Guid id, string jobCode, string name, string jobTitle, bool isActive)
        {
            var entity = new Entity("bs_jobcode")
            {
                Id = id
            };
            entity["bs_jobcodeid"] = id;
            entity["bs_jobcode"] = jobCode;
            entity["bs_name"] = name;
            entity["bs_jobtitle"] = jobTitle;
            entity["bs_isactive"] = isActive;
            return entity;
        }

        [Fact]
        public async Task UpdateJobCodeTitleAsync_ReturnsSuccess_WhenActive()
        {
            var jobCodeId = Guid.NewGuid();
            var oldTitle = "Old Title";
            var newTitle = "New Title";
            var entity = CreateJobCodeEntity(jobCodeId, "JC1", "Name1", oldTitle, true);

            _organizationService.Retrieve("bs_jobcode", jobCodeId, Arg.Any<Microsoft.Xrm.Sdk.Query.ColumnSet>()).Returns(entity);

            var result = await _repository.UpdateJobCodeTitleAsync(jobCodeId, newTitle);

            result.Success.Should().BeTrue();
            result.OldTitle.Should().Be(oldTitle);
            _organizationService.Received(1).Update(Arg.Is<Entity>(e => (string)e["bs_jobtitle"] == newTitle));
        }

        [Fact]
        public async Task UpdateJobCodeTitleAsync_ReturnsFalse_WhenInactive()
        {
            var jobCodeId = Guid.NewGuid();
            var entity = CreateJobCodeEntity(jobCodeId, "JC1", "Name1", "Title", false);

            _organizationService.Retrieve("bs_jobcode", jobCodeId, Arg.Any<Microsoft.Xrm.Sdk.Query.ColumnSet>()).Returns(entity);

            var result = await _repository.UpdateJobCodeTitleAsync(jobCodeId, "New Title");

            result.Success.Should().BeFalse();
            result.OldTitle.Should().BeNull();
            _organizationService.DidNotReceive().Update(Arg.Any<Entity>());
        }

        [Fact]
        public async Task UpdateJobCodeTitleAsync_ReturnsFalse_WhenNotFound()
        {
            var jobCodeId = Guid.NewGuid();

            _organizationService.Retrieve("bs_jobcode", jobCodeId, Arg.Any<Microsoft.Xrm.Sdk.Query.ColumnSet>()).Returns((Entity)null);

            var result = await _repository.UpdateJobCodeTitleAsync(jobCodeId, "New Title");

            result.Success.Should().BeFalse();
            result.OldTitle.Should().BeNull();
            _organizationService.DidNotReceive().Update(Arg.Any<Entity>());
        }

        [Fact]
        public async Task AssignJobCodesToGroupAsync_WithValidInputs_ShouldSucceed()
        {
            // Arrange
            var jobCodeIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var targetGroupId = Guid.NewGuid();

            // Mock target group validation
            var targetGroupEntity = new Entity("bs_jobgroup", targetGroupId);
            targetGroupEntity["bs_isactive"] = true;
            _organizationService.Retrieve("bs_jobgroup", targetGroupId, Arg.Any<ColumnSet>())
                .Returns(targetGroupEntity);

            // Mock job code entities
            var jobCode1 = new Entity("bs_jobcode", jobCodeIds[0]);
            jobCode1["bs_jobtitle"] = "Job Code 1";
            jobCode1["bs_isactive"] = true;
            jobCode1["bs_jobgroupfk"] = new EntityReference("bs_jobgroup", Guid.NewGuid());

            var jobCode2 = new Entity("bs_jobcode", jobCodeIds[1]);
            jobCode2["bs_jobtitle"] = "Job Code 2";
            jobCode2["bs_isactive"] = true;

            _organizationService.Retrieve("bs_jobcode", jobCodeIds[0], Arg.Any<ColumnSet>())
                .Returns(jobCode1);
            _organizationService.Retrieve("bs_jobcode", jobCodeIds[1], Arg.Any<ColumnSet>())
                .Returns(jobCode2);

            // Act
            var result = await _repository.AssignJobCodesToGroupAsync(jobCodeIds, targetGroupId);

            // Assert
            result.Success.Should().BeTrue();
            result.ErrorMessage.Should().BeNull();
            result.ProcessedCount.Should().Be(2);
            result.Results.Should().HaveCount(2);
            result.Results.All(r => r.Success).Should().BeTrue();

            // Verify update calls
            _organizationService.Received(2).Update(Arg.Is<bs_JobCode>(jc => 
                jc.bs_JobGroupFK != null && 
                jc.bs_JobGroupFK.Id == targetGroupId));
        }

        [Fact]
        public async Task AssignJobCodesToGroupAsync_WithEmptyJobCodeIds_ShouldReturnError()
        {
            // Arrange
            var jobCodeIds = new List<Guid>();
            var targetGroupId = Guid.NewGuid();

            // Act
            var result = await _repository.AssignJobCodesToGroupAsync(jobCodeIds, targetGroupId);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("No job codes provided for assignment.");
            result.ProcessedCount.Should().Be(0);
            result.Results.Should().BeEmpty();
        }

        [Fact]
        public async Task AssignJobCodesToGroupAsync_WithTooManyJobCodes_ShouldReturnError()
        {
            // Arrange
            var jobCodeIds = Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToList();
            var targetGroupId = Guid.NewGuid();

            // Act
            var result = await _repository.AssignJobCodesToGroupAsync(jobCodeIds, targetGroupId);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Maximum of 100 job codes can be assigned in a single request.");
            result.ProcessedCount.Should().Be(0);
            result.Results.Should().BeEmpty();
        }

        [Fact]
        public async Task AssignJobCodesToGroupAsync_WithInvalidTargetGroup_ShouldReturnError()
        {
            // Arrange
            var jobCodeIds = new List<Guid> { Guid.NewGuid() };
            var targetGroupId = Guid.NewGuid();

            // Mock target group as inactive
            var targetGroupEntity = new Entity("bs_jobgroup", targetGroupId);
            targetGroupEntity["bs_isactive"] = false;
            _organizationService.Retrieve("bs_jobgroup", targetGroupId, Arg.Any<ColumnSet>())
                .Returns(targetGroupEntity);

            // Act
            var result = await _repository.AssignJobCodesToGroupAsync(jobCodeIds, targetGroupId);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Target job group not found or is inactive.");
            result.ProcessedCount.Should().Be(0);
        }

        [Fact]
        public async Task AssignJobCodesToGroupAsync_WithMixedValidAndInvalidJobCodes_ShouldReturnPartialSuccess()
        {
            // Arrange
            var validJobCodeId = Guid.NewGuid();
            var invalidJobCodeId = Guid.NewGuid();
            var jobCodeIds = new List<Guid> { validJobCodeId, invalidJobCodeId };
            var targetGroupId = Guid.NewGuid();

            // Mock target group validation
            var targetGroupEntity = new Entity("bs_jobgroup", targetGroupId);
            targetGroupEntity["bs_isactive"] = true;
            _organizationService.Retrieve("bs_jobgroup", targetGroupId, Arg.Any<ColumnSet>())
                .Returns(targetGroupEntity);

                         // Mock valid job code
            var validJobCode = new Entity("bs_jobcode", validJobCodeId);
            validJobCode["bs_jobtitle"] = "Valid Job Code";
            validJobCode["bs_isactive"] = true;
            _organizationService.Retrieve("bs_jobcode", validJobCodeId, Arg.Any<ColumnSet>())
                .Returns(validJobCode);

            // Mock invalid job code (inactive)
            var invalidJobCode = new Entity("bs_jobcode", invalidJobCodeId);
            invalidJobCode["bs_jobtitle"] = "Invalid Job Code";
            invalidJobCode["bs_isactive"] = false;
            _organizationService.Retrieve("bs_jobcode", invalidJobCodeId, Arg.Any<ColumnSet>())
                .Returns(invalidJobCode);

            // Act
            var result = await _repository.AssignJobCodesToGroupAsync(jobCodeIds, targetGroupId);

            // Assert
            result.Success.Should().BeTrue();
            result.ProcessedCount.Should().Be(1);
            result.Results.Should().HaveCount(2);
            result.Results.First(r => r.JobCodeId == validJobCodeId).Success.Should().BeTrue();
            result.Results.First(r => r.JobCodeId == invalidJobCodeId).Success.Should().BeFalse();
            result.Results.First(r => r.JobCodeId == invalidJobCodeId).ErrorMessage.Should().Be("Job code is inactive.");
        }

        [Fact]
        public async Task ValidateJobCodesExistAndActiveAsync_WithValidJobCodes_ShouldReturnAllValid()
        {
            // Arrange
            var jobCodeIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            
            var entityCollection = new EntityCollection();
            foreach (var id in jobCodeIds)
            {
                var entity = new Entity("bs_jobcode", id);
                entity["bs_isactive"] = true;
                entityCollection.Entities.Add(entity);
            }

            _organizationService.RetrieveMultiple(Arg.Any<QueryExpression>())
                .Returns(entityCollection);

            // Act
            var result = await _repository.ValidateJobCodesExistAndActiveAsync(jobCodeIds);

            // Assert
            result.AllValid.Should().BeTrue();
            result.InvalidJobCodeIds.Should().BeEmpty();
        }

        [Fact]
        public async Task ValidateJobCodesExistAndActiveAsync_WithInvalidJobCodes_ShouldReturnInvalid()
        {
            // Arrange
            var validJobCodeId = Guid.NewGuid();
            var invalidJobCodeId = Guid.NewGuid();
            var jobCodeIds = new List<Guid> { validJobCodeId, invalidJobCodeId };
            
            var entityCollection = new EntityCollection();
            var validEntity = new Entity("bs_jobcode", validJobCodeId);
            validEntity["bs_isactive"] = true;
            entityCollection.Entities.Add(validEntity);
            // Note: Invalid job code is not in the collection (simulating not found)

            _organizationService.RetrieveMultiple(Arg.Any<QueryExpression>())
                .Returns(entityCollection);

            // Act
            var result = await _repository.ValidateJobCodesExistAndActiveAsync(jobCodeIds);

            // Assert
            result.AllValid.Should().BeFalse();
            result.InvalidJobCodeIds.Should().Contain(invalidJobCodeId);
            result.InvalidJobCodeIds.Should().NotContain(validJobCodeId);
        }

        [Fact]
        public async Task ValidateJobGroupExistsAndActiveAsync_WithValidGroup_ShouldReturnTrue()
        {
            // Arrange
            var jobGroupId = Guid.NewGuid();
            var entity = new Entity("bs_jobgroup", jobGroupId);
            entity["bs_isactive"] = true;

            _organizationService.Retrieve("bs_jobgroup", jobGroupId, Arg.Any<ColumnSet>())
                .Returns(entity);

            // Act
            var result = await _repository.ValidateJobGroupExistsAndActiveAsync(jobGroupId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateJobGroupExistsAndActiveAsync_WithInactiveGroup_ShouldReturnFalse()
        {
            // Arrange
            var jobGroupId = Guid.NewGuid();
            var entity = new Entity("bs_jobgroup", jobGroupId);
            entity["bs_isactive"] = false;

            _organizationService.Retrieve("bs_jobgroup", jobGroupId, Arg.Any<ColumnSet>())
                .Returns(entity);

            // Act
            var result = await _repository.ValidateJobGroupExistsAndActiveAsync(jobGroupId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateJobGroupExistsAndActiveAsync_WithNonExistentGroup_ShouldReturnFalse()
        {
            // Arrange
            var jobGroupId = Guid.NewGuid();

            _organizationService.Retrieve("bs_jobgroup", jobGroupId, Arg.Any<ColumnSet>())
                .Returns((Entity)null);

            // Act
            var result = await _repository.ValidateJobGroupExistsAndActiveAsync(jobGroupId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateJobGroupExistsAndActiveAsync_WithEmptyGuid_ShouldReturnFalse()
        {
            // Act
            var result = await _repository.ValidateJobGroupExistsAndActiveAsync(Guid.Empty);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateJobCodeStatusAsync_WithValidJobCodes_ShouldReturnSuccess()
        {
            // Arrange
            var jobCodeId1 = Guid.NewGuid();
            var jobCodeId2 = Guid.NewGuid();
            var jobCodeIds = new List<Guid> { jobCodeId1, jobCodeId2 };

            var entity1 = new Entity("bs_jobcode", jobCodeId1);
            entity1["bs_jobtitle"] = "Job Code 1";
            entity1["bs_isactive"] = false; // Currently inactive

            var entity2 = new Entity("bs_jobcode", jobCodeId2);
            entity2["bs_jobtitle"] = "Job Code 2";
            entity2["bs_isactive"] = false; // Currently inactive

            _organizationService.Retrieve("bs_jobcode", jobCodeId1, Arg.Any<ColumnSet>())
                .Returns(entity1);
            _organizationService.Retrieve("bs_jobcode", jobCodeId2, Arg.Any<ColumnSet>())
                .Returns(entity2);

            // Act
            var result = await _repository.UpdateJobCodeStatusAsync(jobCodeIds, true);

            // Assert
            result.Success.Should().BeTrue();
            result.ProcessedCount.Should().Be(2);
            result.Results.Should().HaveCount(2);
            
            var result1 = result.Results.First(r => r.JobCodeId == jobCodeId1);
            result1.Success.Should().BeTrue();
            result1.JobTitle.Should().Be("Job Code 1");
            result1.PreviousStatus.Should().BeFalse();

            _organizationService.Received(2).Update(Arg.Is<bs_JobCode>(jc => jc.bs_IsActive == true));
        }

        [Fact]
        public async Task UpdateJobCodeStatusAsync_WithNonExistentJobCode_ShouldReturnError()
        {
            // Arrange
            var jobCodeId = Guid.NewGuid();
            var jobCodeIds = new List<Guid> { jobCodeId };

            _organizationService.Retrieve("bs_jobcode", jobCodeId, Arg.Any<ColumnSet>())
                .Returns((Entity)null);

            // Act
            var result = await _repository.UpdateJobCodeStatusAsync(jobCodeIds, true);

            // Assert
            result.Success.Should().BeTrue(); // Overall success despite individual failures
            result.ProcessedCount.Should().Be(0);
            result.Results.Should().HaveCount(1);
            result.Results[0].Success.Should().BeFalse();
            result.Results[0].ErrorMessage.Should().Be("Job code not found.");
        }

        [Fact]
        public async Task UpdateJobCodeStatusAsync_WithAlreadyActiveJobCode_ShouldReturnError()
        {
            // Arrange
            var jobCodeId = Guid.NewGuid();
            var jobCodeIds = new List<Guid> { jobCodeId };

            var entity = new Entity("bs_jobcode", jobCodeId);
            entity["bs_jobtitle"] = "Job Code 1";
            entity["bs_isactive"] = true; // Already active

            _organizationService.Retrieve("bs_jobcode", jobCodeId, Arg.Any<ColumnSet>())
                .Returns(entity);

            // Act
            var result = await _repository.UpdateJobCodeStatusAsync(jobCodeIds, true);

            // Assert
            result.Success.Should().BeTrue(); // Overall success despite individual failures
            result.ProcessedCount.Should().Be(0);
            result.Results.Should().HaveCount(1);
            result.Results[0].Success.Should().BeFalse();
            result.Results[0].ErrorMessage.Should().Be("Job code is already active.");
        }

        [Fact]
        public async Task UpdateJobCodeStatusAsync_WithEmptyList_ShouldReturnError()
        {
            // Arrange
            var jobCodeIds = new List<Guid>();

            // Act
            var result = await _repository.UpdateJobCodeStatusAsync(jobCodeIds, true);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("No job codes provided for status update.");
            result.ProcessedCount.Should().Be(0);
            result.Results.Should().BeEmpty();
        }

        [Fact]
        public async Task UpdateJobCodeStatusAsync_WithTooManyJobCodes_ShouldReturnError()
        {
            // Arrange
            var jobCodeIds = Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToList();

            // Act
            var result = await _repository.UpdateJobCodeStatusAsync(jobCodeIds, true);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Maximum of 100 job codes can be updated in a single request.");
            result.ProcessedCount.Should().Be(0);
            result.Results.Should().BeEmpty();
        }
    }
}
