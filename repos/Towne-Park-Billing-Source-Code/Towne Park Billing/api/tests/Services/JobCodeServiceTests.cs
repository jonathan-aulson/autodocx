using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using api.Data;
using api.Models.Vo;
using api.Services;
using api.Services.Impl;
using Xunit;

namespace BackendTests.Services
{
    public class JobCodeServiceTests
    {
        private readonly IJobCodeRepository _jobCodeRepository;
        private readonly IJobGroupRepository _jobGroupRepository;
        private readonly JobCodeService _service;

        public JobCodeServiceTests()
        {
            _jobCodeRepository = Substitute.For<IJobCodeRepository>();
            _jobGroupRepository = Substitute.For<IJobGroupRepository>();
            _service = new JobCodeService(_jobCodeRepository, _jobGroupRepository);
        }

        [Fact]
        public async Task GetJobCodesAsync_ReturnsJobCodes()
        {
            var jobCodes = new List<JobCodeVo>
            {
                new JobCodeVo { JobCodeId = System.Guid.NewGuid(), JobCode = "JC1", JobTitle = "Title1", Name = "Name1", IsActive = true }
            };

            _jobCodeRepository.GetJobCodesAsync().Returns(jobCodes);

            var result = await _service.GetJobCodesAsync();

            result.Should().BeEquivalentTo(jobCodes);
        }

        [Fact]
        public async Task EditJobCodeTitleAsync_ReturnsSuccess_WhenTitleIsValidAndRepositorySucceeds()
        {
            var jobCodeId = System.Guid.NewGuid();
            var newTitle = "Updated Title";
            var oldTitle = "Old Title";
            _jobCodeRepository.UpdateJobCodeTitleAsync(jobCodeId, newTitle).Returns((true, oldTitle));

            var (success, error, returnedOldTitle) = await _service.EditJobCodeTitleAsync(jobCodeId, newTitle);

            success.Should().BeTrue();
            error.Should().BeNull();
            returnedOldTitle.Should().Be(oldTitle);
        }

        [Fact]
        public async Task EditJobCodeTitleAsync_ReturnsError_WhenTitleIsEmpty()
        {
            var jobCodeId = System.Guid.NewGuid();
            var newTitle = "";

            var (success, error, returnedOldTitle) = await _service.EditJobCodeTitleAsync(jobCodeId, newTitle);

            success.Should().BeFalse();
            error.Should().Be("Title cannot be empty.");
            returnedOldTitle.Should().BeNull();
        }

        [Fact]
        public async Task EditJobCodeTitleAsync_ReturnsError_WhenRepositoryFails()
        {
            var jobCodeId = System.Guid.NewGuid();
            var newTitle = "Valid Title";
            _jobCodeRepository.UpdateJobCodeTitleAsync(jobCodeId, newTitle).Returns((false, null));

            var (success, error, returnedOldTitle) = await _service.EditJobCodeTitleAsync(jobCodeId, newTitle);

            success.Should().BeFalse();
            error.Should().Be("Job code not found or inactive.");
            returnedOldTitle.Should().BeNull();
        }

        [Fact]
        public async Task AssignJobCodesToGroupAsync_WithValidInputs_ShouldSucceed()
        {
            // Arrange
            var jobCodeIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var targetGroupId = Guid.NewGuid();
            var repositoryResult = (
                Success: true, 
                ErrorMessage: (string?)null, 
                ProcessedCount: 2, 
                Results: new List<(Guid JobCodeId, bool Success, string? ErrorMessage, Guid? PreviousGroupId)>
                {
                    (jobCodeIds[0], true, null, null),
                    (jobCodeIds[1], true, null, Guid.NewGuid())
                }
            );

            _jobCodeRepository.AssignJobCodesToGroupAsync(Arg.Any<List<Guid>>(), targetGroupId)
                .Returns(repositoryResult);

            // Act
            var result = await _service.AssignJobCodesToGroupAsync(jobCodeIds, targetGroupId);

            // Assert
            result.Success.Should().BeTrue();
            result.ErrorMessage.Should().BeNull();
            result.ProcessedCount.Should().Be(2);
            result.Results.Should().HaveCount(2);
            result.Results.All(r => r.Success).Should().BeTrue();

            await _jobCodeRepository.Received(1).AssignJobCodesToGroupAsync(Arg.Any<List<Guid>>(), targetGroupId);
        }

        [Fact]
        public async Task AssignJobCodesToGroupAsync_WithNullJobCodeIds_ShouldReturnError()
        {
            // Arrange
            List<Guid> jobCodeIds = null;
            var targetGroupId = Guid.NewGuid();

            // Act
            var result = await _service.AssignJobCodesToGroupAsync(jobCodeIds, targetGroupId);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("At least one job code must be specified for assignment.");
            result.ProcessedCount.Should().Be(0);
            result.Results.Should().BeEmpty();

            await _jobCodeRepository.DidNotReceive().AssignJobCodesToGroupAsync(Arg.Any<List<Guid>>(), Arg.Any<Guid>());
        }

        [Fact]
        public async Task AssignJobCodesToGroupAsync_WithEmptyJobCodeIds_ShouldReturnError()
        {
            // Arrange
            var jobCodeIds = new List<Guid>();
            var targetGroupId = Guid.NewGuid();

            // Act
            var result = await _service.AssignJobCodesToGroupAsync(jobCodeIds, targetGroupId);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("At least one job code must be specified for assignment.");
            result.ProcessedCount.Should().Be(0);
            result.Results.Should().BeEmpty();

            await _jobCodeRepository.DidNotReceive().AssignJobCodesToGroupAsync(Arg.Any<List<Guid>>(), Arg.Any<Guid>());
        }

        [Fact]
        public async Task AssignJobCodesToGroupAsync_WithDuplicateJobCodeIds_ShouldRemoveDuplicatesAndSucceed()
        {
            // Arrange
            var jobCodeId1 = Guid.NewGuid();
            var jobCodeId2 = Guid.NewGuid();
            var jobCodeIds = new List<Guid> { jobCodeId1, jobCodeId2, jobCodeId1 }; // jobCodeId1 is duplicated
            var targetGroupId = Guid.NewGuid();
            var uniqueJobCodeIds = new List<Guid> { jobCodeId1, jobCodeId2 };

            var repositoryResult = (
                Success: true, 
                ErrorMessage: (string?)null, 
                ProcessedCount: 2, 
                Results: new List<(Guid JobCodeId, bool Success, string? ErrorMessage, Guid? PreviousGroupId)>
                {
                    (jobCodeId1, true, null, null),
                    (jobCodeId2, true, null, null)
                }
            );

            _jobCodeRepository.AssignJobCodesToGroupAsync(Arg.Any<List<Guid>>(), targetGroupId)
                .Returns(repositoryResult);

            // Act
            var result = await _service.AssignJobCodesToGroupAsync(jobCodeIds, targetGroupId);

            // Assert
            result.Success.Should().BeTrue();
            result.ProcessedCount.Should().Be(2);
            result.Results.Should().HaveCount(2);

            // Verify that repository was called with unique IDs only
            await _jobCodeRepository.Received(1).AssignJobCodesToGroupAsync(
                Arg.Is<List<Guid>>(ids => ids.Count == 2 && ids.Contains(jobCodeId1) && ids.Contains(jobCodeId2)), 
                targetGroupId);
        }

        [Fact]
        public async Task AssignJobCodesToGroupAsync_WithTooManyJobCodes_ShouldReturnError()
        {
            // Arrange
            var jobCodeIds = Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToList();
            var targetGroupId = Guid.NewGuid();

            // Act
            var result = await _service.AssignJobCodesToGroupAsync(jobCodeIds, targetGroupId);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Maximum of 100 unique job codes can be assigned in a single request.");
            result.ProcessedCount.Should().Be(0);
            result.Results.Should().BeEmpty();

            await _jobCodeRepository.DidNotReceive().AssignJobCodesToGroupAsync(Arg.Any<List<Guid>>(), Arg.Any<Guid>());
        }

        [Fact]
        public async Task AssignJobCodesToGroupAsync_WithEmptyTargetGroupId_ShouldReturnError()
        {
            // Arrange
            var jobCodeIds = new List<Guid> { Guid.NewGuid() };
            var targetGroupId = Guid.Empty;

            // Act
            var result = await _service.AssignJobCodesToGroupAsync(jobCodeIds, targetGroupId);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Target job group ID is required.");
            result.ProcessedCount.Should().Be(0);
            result.Results.Should().BeEmpty();

            await _jobCodeRepository.DidNotReceive().AssignJobCodesToGroupAsync(Arg.Any<List<Guid>>(), Arg.Any<Guid>());
        }

        [Fact]
        public async Task AssignJobCodesToGroupAsync_WhenRepositoryReturnsError_ShouldReturnError()
        {
            // Arrange
            var jobCodeIds = new List<Guid> { Guid.NewGuid() };
            var targetGroupId = Guid.NewGuid();
            var repositoryResult = (
                Success: false, 
                ErrorMessage: "Target job group not found or is inactive.", 
                ProcessedCount: 0, 
                Results: new List<(Guid JobCodeId, bool Success, string? ErrorMessage, Guid? PreviousGroupId)>()
            );

            _jobCodeRepository.AssignJobCodesToGroupAsync(Arg.Any<List<Guid>>(), targetGroupId)
                .Returns(repositoryResult);

            // Act
            var result = await _service.AssignJobCodesToGroupAsync(jobCodeIds, targetGroupId);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Target job group not found or is inactive.");
            result.ProcessedCount.Should().Be(0);
            result.Results.Should().BeEmpty();

            await _jobCodeRepository.Received(1).AssignJobCodesToGroupAsync(Arg.Any<List<Guid>>(), targetGroupId);
        }

        [Fact]
        public async Task AssignJobCodesToGroupAsync_WithPartialSuccess_ShouldReturnPartialResults()
        {
            // Arrange
            var validJobCodeId = Guid.NewGuid();
            var invalidJobCodeId = Guid.NewGuid();
            var jobCodeIds = new List<Guid> { validJobCodeId, invalidJobCodeId };
            var targetGroupId = Guid.NewGuid();
            var repositoryResult = (
                Success: true, 
                ErrorMessage: (string?)null, 
                ProcessedCount: 1, 
                Results: new List<(Guid JobCodeId, bool Success, string? ErrorMessage, Guid? PreviousGroupId)>
                {
                    (validJobCodeId, true, null, null),
                    (invalidJobCodeId, false, "Job code is inactive.", null)
                }
            );

            _jobCodeRepository.AssignJobCodesToGroupAsync(Arg.Any<List<Guid>>(), targetGroupId)
                .Returns(repositoryResult);

            // Act
            var result = await _service.AssignJobCodesToGroupAsync(jobCodeIds, targetGroupId);

            // Assert
            result.Success.Should().BeTrue();
            result.ProcessedCount.Should().Be(1);
            result.Results.Should().HaveCount(2);
            result.Results.First(r => r.JobCodeId == validJobCodeId).Success.Should().BeTrue();
            result.Results.First(r => r.JobCodeId == invalidJobCodeId).Success.Should().BeFalse();
            result.Results.First(r => r.JobCodeId == invalidJobCodeId).ErrorMessage.Should().Be("Job code is inactive.");

            await _jobCodeRepository.Received(1).AssignJobCodesToGroupAsync(Arg.Any<List<Guid>>(), targetGroupId);
        }

        [Fact]
        public async Task UpdateJobCodeStatusAsync_WithValidInputs_ShouldSucceed()
        {
            // Arrange
            var jobCodeIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var isActive = true;
            var repositoryResult = (
                Success: true, 
                ErrorMessage: (string?)null, 
                ProcessedCount: 2, 
                Results: new List<(Guid JobCodeId, bool Success, string? ErrorMessage, bool PreviousStatus, string JobTitle)>
                {
                    (jobCodeIds[0], true, null, false, "Job Code 1"),
                    (jobCodeIds[1], true, null, false, "Job Code 2")
                }
            );

            _jobCodeRepository.UpdateJobCodeStatusAsync(jobCodeIds, isActive)
                .Returns(repositoryResult);

            // Act
            var result = await _service.UpdateJobCodeStatusAsync(jobCodeIds, isActive);

            // Assert
            result.Success.Should().BeTrue();
            result.ErrorMessage.Should().BeNull();
            result.ProcessedCount.Should().Be(2);
            result.Results.Should().HaveCount(2);
            result.Results.All(r => r.Success).Should().BeTrue();

            await _jobCodeRepository.Received(1).UpdateJobCodeStatusAsync(jobCodeIds, isActive);
        }

        [Fact]
        public async Task UpdateJobCodeStatusAsync_ActivatesJobGroup_WhenActivatingJobCode()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var jobCodeId = Guid.NewGuid();
            var jobCodeIds = new List<Guid> { jobCodeId };
            var isActive = true;
            var repositoryResult = (
                Success: true,
                ErrorMessage: (string?)null,
                ProcessedCount: 1,
                Results: new List<(Guid JobCodeId, bool Success, string? ErrorMessage, bool PreviousStatus, string JobTitle)>
                {
                    (jobCodeId, true, null, false, "Job Code 1")
                }
            );

            _jobCodeRepository.UpdateJobCodeStatusAsync(jobCodeIds, isActive)
                .Returns(repositoryResult);

            _jobCodeRepository.GetJobCodeByIdAsync(jobCodeId)
                .Returns(new api.Models.Vo.JobCodeVo
                {
                    JobCodeId = jobCodeId,
                    JobGroupId = groupId.ToString()
                });

            // Act
            var result = await _service.UpdateJobCodeStatusAsync(jobCodeIds, isActive);

            // Assert
            _jobGroupRepository.Received(1).ActivateJobGroup(groupId);
        }

        [Fact]
        public async Task UpdateJobCodeStatusAsync_WithNullJobCodeIds_ShouldReturnError()
        {
            // Arrange
            List<Guid> jobCodeIds = null;
            var isActive = true;

            // Act
            var result = await _service.UpdateJobCodeStatusAsync(jobCodeIds, isActive);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("At least one job code must be specified for status update.");
            result.ProcessedCount.Should().Be(0);
            result.Results.Should().BeEmpty();

            await _jobCodeRepository.DidNotReceive().UpdateJobCodeStatusAsync(Arg.Any<List<Guid>>(), Arg.Any<bool>());
        }

        [Fact]
        public async Task UpdateJobCodeStatusAsync_WithEmptyJobCodeIds_ShouldReturnError()
        {
            // Arrange
            var jobCodeIds = new List<Guid>();
            var isActive = true;

            // Act
            var result = await _service.UpdateJobCodeStatusAsync(jobCodeIds, isActive);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("At least one job code must be specified for status update.");
            result.ProcessedCount.Should().Be(0);
            result.Results.Should().BeEmpty();

            await _jobCodeRepository.DidNotReceive().UpdateJobCodeStatusAsync(Arg.Any<List<Guid>>(), Arg.Any<bool>());
        }

        [Fact]
        public async Task UpdateJobCodeStatusAsync_WithDuplicateJobCodeIds_ShouldReturnError()
        {
            // Arrange
            var jobCodeId = Guid.NewGuid();
            var jobCodeIds = new List<Guid> { jobCodeId, jobCodeId }; // Duplicate
            var isActive = true;

            // Act
            var result = await _service.UpdateJobCodeStatusAsync(jobCodeIds, isActive);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Duplicate job code IDs found");
            result.ProcessedCount.Should().Be(0);
            result.Results.Should().BeEmpty();

            await _jobCodeRepository.DidNotReceive().UpdateJobCodeStatusAsync(Arg.Any<List<Guid>>(), Arg.Any<bool>());
        }

        [Fact]
        public async Task UpdateJobCodeStatusAsync_WithTooManyJobCodes_ShouldReturnError()
        {
            // Arrange
            var jobCodeIds = Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToList();
            var isActive = true;

            // Act
            var result = await _service.UpdateJobCodeStatusAsync(jobCodeIds, isActive);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Maximum of 100 job codes can be updated in a single request.");
            result.ProcessedCount.Should().Be(0);
            result.Results.Should().BeEmpty();

            await _jobCodeRepository.DidNotReceive().UpdateJobCodeStatusAsync(Arg.Any<List<Guid>>(), Arg.Any<bool>());
        }

        [Fact]
        public async Task UpdateJobCodeStatusAsync_WhenRepositoryReturnsError_ShouldReturnError()
        {
            // Arrange
            var jobCodeIds = new List<Guid> { Guid.NewGuid() };
            var isActive = true;
            var repositoryResult = (
                Success: false, 
                ErrorMessage: "Database error", 
                ProcessedCount: 0, 
                Results: new List<(Guid JobCodeId, bool Success, string? ErrorMessage, bool PreviousStatus, string JobTitle)>()
            );

            _jobCodeRepository.UpdateJobCodeStatusAsync(jobCodeIds, isActive)
                .Returns(repositoryResult);

            // Act
            var result = await _service.UpdateJobCodeStatusAsync(jobCodeIds, isActive);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Database error");
            result.ProcessedCount.Should().Be(0);
            result.Results.Should().BeEmpty();

            await _jobCodeRepository.Received(1).UpdateJobCodeStatusAsync(jobCodeIds, isActive);
        }
    }
}
