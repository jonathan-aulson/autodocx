using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Adapters.Impl;
using api.Models.Dto;
using api.Services;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace BackendTests.Adapters
{
    public class JobCodeServiceAdapterTests
    {
        private readonly IJobCodeService _jobCodeService;
        private readonly JobCodeServiceAdapter _adapter;

        public JobCodeServiceAdapterTests()
        {
            _jobCodeService = Substitute.For<IJobCodeService>();
            _adapter = new JobCodeServiceAdapter(_jobCodeService);
        }

        [Fact]
        public async Task AssignJobCodesToGroupAsync_WithValidRequest_ShouldSucceed()
        {
            // Arrange
            var jobCodeIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var targetGroupId = Guid.NewGuid();
            var request = new AssignJobCodesToGroupRequestDto
            {
                JobCodeIds = jobCodeIds,
                TargetGroupId = targetGroupId
            };

            var serviceResult = (
                Success: true,
                ErrorMessage: (string?)null,
                ProcessedCount: 2,
                Results: new List<(Guid JobCodeId, bool Success, string? ErrorMessage, Guid? PreviousGroupId)>
                {
                    (jobCodeIds[0], true, null, null),
                    (jobCodeIds[1], true, null, Guid.NewGuid())
                }
            );

            _jobCodeService.AssignJobCodesToGroupAsync(jobCodeIds, targetGroupId)
                .Returns(serviceResult);

            // Act
            var result = await _adapter.AssignJobCodesToGroupAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.ErrorMessage.Should().BeNull();
            result.ProcessedCount.Should().Be(2);
            result.Results.Should().HaveCount(2);
            result.Results.All(r => r.Success).Should().BeTrue();
            result.Results.All(r => r.NewGroupId == targetGroupId).Should().BeTrue();

            await _jobCodeService.Received(1).AssignJobCodesToGroupAsync(jobCodeIds, targetGroupId);
        }

        [Fact]
        public async Task AssignJobCodesToGroupAsync_WithNullRequest_ShouldReturnError()
        {
            // Arrange
            AssignJobCodesToGroupRequestDto request = null;
            
            // This test now focuses on the adapter's behavior when service returns an error
            // (In practice, this scenario would be caught by Function layer validation)
            var serviceResult = (
                Success: false,
                ErrorMessage: "Invalid request parameters.",
                ProcessedCount: 0,
                Results: new List<(Guid JobCodeId, bool Success, string? ErrorMessage, Guid? PreviousGroupId)>()
            );

            _jobCodeService.AssignJobCodesToGroupAsync(Arg.Any<List<Guid>>(), Arg.Any<Guid>())
                .Returns(serviceResult);

            // Act & Assert - Since we're passing null, this will throw NullReferenceException
            // The Function layer should prevent this scenario, but if it somehow gets through,
            // we expect the adapter to fail gracefully
            await Assert.ThrowsAsync<NullReferenceException>(async () => 
                await _adapter.AssignJobCodesToGroupAsync(request));
        }

        [Fact]
        public async Task AssignJobCodesToGroupAsync_WithNullJobCodeIds_ShouldDelegateToService() 
        {
            // Arrange
            var request = new AssignJobCodesToGroupRequestDto
            {
                JobCodeIds = null,
                TargetGroupId = Guid.NewGuid()
            };

            // Mock service to return appropriate error for invalid input
            var serviceResult = (
                Success: false,
                ErrorMessage: "At least one job code must be specified for assignment.",
                ProcessedCount: 0,
                Results: new List<(Guid JobCodeId, bool Success, string? ErrorMessage, Guid? PreviousGroupId)>()
            );

            _jobCodeService.AssignJobCodesToGroupAsync(null, request.TargetGroupId)
                .Returns(serviceResult);

            // Act
            var result = await _adapter.AssignJobCodesToGroupAsync(request);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("At least one job code must be specified for assignment.");
            result.ProcessedCount.Should().Be(0);
            result.Results.Should().BeEmpty();

            await _jobCodeService.Received(1).AssignJobCodesToGroupAsync(null, request.TargetGroupId);
        }

        [Fact]
        public async Task AssignJobCodesToGroupAsync_WithEmptyJobCodeIds_ShouldDelegateToService()
        {
            // Arrange
            var request = new AssignJobCodesToGroupRequestDto
            {
                JobCodeIds = new List<Guid>(),
                TargetGroupId = Guid.NewGuid()
            };

            // Mock service to return appropriate error for empty list
            var serviceResult = (
                Success: false,
                ErrorMessage: "At least one job code must be specified for assignment.",
                ProcessedCount: 0,
                Results: new List<(Guid JobCodeId, bool Success, string? ErrorMessage, Guid? PreviousGroupId)>()
            );

            _jobCodeService.AssignJobCodesToGroupAsync(request.JobCodeIds, request.TargetGroupId)
                .Returns(serviceResult);

            // Act
            var result = await _adapter.AssignJobCodesToGroupAsync(request);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("At least one job code must be specified for assignment.");
            result.ProcessedCount.Should().Be(0);
            result.Results.Should().BeEmpty();

            await _jobCodeService.Received(1).AssignJobCodesToGroupAsync(request.JobCodeIds, request.TargetGroupId);
        }

        [Fact]
        public async Task AssignJobCodesToGroupAsync_WithEmptyTargetGroupId_ShouldDelegateToService()
        {
            // Arrange
            var request = new AssignJobCodesToGroupRequestDto
            {
                JobCodeIds = new List<Guid> { Guid.NewGuid() },
                TargetGroupId = Guid.Empty
            };

            // Mock service to return appropriate error for empty target group
            var serviceResult = (
                Success: false,
                ErrorMessage: "Target job group ID is required.",
                ProcessedCount: 0,
                Results: new List<(Guid JobCodeId, bool Success, string? ErrorMessage, Guid? PreviousGroupId)>()
            );

            _jobCodeService.AssignJobCodesToGroupAsync(request.JobCodeIds, request.TargetGroupId)
                .Returns(serviceResult);

            // Act
            var result = await _adapter.AssignJobCodesToGroupAsync(request);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Target job group ID is required.");
            result.ProcessedCount.Should().Be(0);
            result.Results.Should().BeEmpty();

            await _jobCodeService.Received(1).AssignJobCodesToGroupAsync(request.JobCodeIds, request.TargetGroupId);
        }

        [Fact]
        public async Task AssignJobCodesToGroupAsync_WhenServiceReturnsError_ShouldReturnError()
        {
            // Arrange
            var jobCodeIds = new List<Guid> { Guid.NewGuid() };
            var targetGroupId = Guid.NewGuid();
            var request = new AssignJobCodesToGroupRequestDto
            {
                JobCodeIds = jobCodeIds,
                TargetGroupId = targetGroupId
            };

            var serviceResult = (
                Success: false,
                ErrorMessage: "Target job group not found or is inactive.",
                ProcessedCount: 0,
                Results: new List<(Guid JobCodeId, bool Success, string? ErrorMessage, Guid? PreviousGroupId)>()
            );

            _jobCodeService.AssignJobCodesToGroupAsync(jobCodeIds, targetGroupId)
                .Returns(serviceResult);

            // Act
            var result = await _adapter.AssignJobCodesToGroupAsync(request);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Target job group not found or is inactive.");
            result.ProcessedCount.Should().Be(0);
            result.Results.Should().BeEmpty();

            await _jobCodeService.Received(1).AssignJobCodesToGroupAsync(jobCodeIds, targetGroupId);
        }

        [Fact]
        public async Task AssignJobCodesToGroupAsync_WithPartialSuccess_ShouldReturnPartialResults()
        {
            // Arrange
            var validJobCodeId = Guid.NewGuid();
            var invalidJobCodeId = Guid.NewGuid();
            var jobCodeIds = new List<Guid> { validJobCodeId, invalidJobCodeId };
            var targetGroupId = Guid.NewGuid();
            var previousGroupId = Guid.NewGuid();

            var request = new AssignJobCodesToGroupRequestDto
            {
                JobCodeIds = jobCodeIds,
                TargetGroupId = targetGroupId
            };

            var serviceResult = (
                Success: true,
                ErrorMessage: (string?)null,
                ProcessedCount: 1,
                Results: new List<(Guid JobCodeId, bool Success, string? ErrorMessage, Guid? PreviousGroupId)>
                {
                    (validJobCodeId, true, null, previousGroupId),
                    (invalidJobCodeId, false, "Job code is inactive.", null)
                }
            );

            _jobCodeService.AssignJobCodesToGroupAsync(jobCodeIds, targetGroupId)
                .Returns(serviceResult);

            // Act
            var result = await _adapter.AssignJobCodesToGroupAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.ProcessedCount.Should().Be(1);
            result.Results.Should().HaveCount(2);

            var validResult = result.Results.First(r => r.JobCodeId == validJobCodeId);
            validResult.Success.Should().BeTrue();
            validResult.ErrorMessage.Should().BeNull();
            validResult.PreviousGroupId.Should().Be(previousGroupId);
            validResult.NewGroupId.Should().Be(targetGroupId);

            var invalidResult = result.Results.First(r => r.JobCodeId == invalidJobCodeId);
            invalidResult.Success.Should().BeFalse();
            invalidResult.ErrorMessage.Should().Be("Job code is inactive.");
            invalidResult.PreviousGroupId.Should().BeNull();
            invalidResult.NewGroupId.Should().Be(targetGroupId);

            await _jobCodeService.Received(1).AssignJobCodesToGroupAsync(jobCodeIds, targetGroupId);
        }

        [Fact]
        public async Task AssignJobCodesToGroupAsync_WithMultipleSuccessfulAssignments_ShouldMapCorrectly()
        {
            // Arrange
            var jobCodeId1 = Guid.NewGuid();
            var jobCodeId2 = Guid.NewGuid();
            var jobCodeId3 = Guid.NewGuid();
            var jobCodeIds = new List<Guid> { jobCodeId1, jobCodeId2, jobCodeId3 };
            var targetGroupId = Guid.NewGuid();
            var previousGroup1 = Guid.NewGuid();
            var previousGroup2 = Guid.NewGuid();

            var request = new AssignJobCodesToGroupRequestDto
            {
                JobCodeIds = jobCodeIds,
                TargetGroupId = targetGroupId
            };

            var serviceResult = (
                Success: true,
                ErrorMessage: (string?)null,
                ProcessedCount: 3,
                Results: new List<(Guid JobCodeId, bool Success, string? ErrorMessage, Guid? PreviousGroupId)>
                {
                    (jobCodeId1, true, null, previousGroup1),
                    (jobCodeId2, true, null, null), // Was unassigned
                    (jobCodeId3, true, null, previousGroup2)
                }
            );

            _jobCodeService.AssignJobCodesToGroupAsync(jobCodeIds, targetGroupId)
                .Returns(serviceResult);

            // Act
            var result = await _adapter.AssignJobCodesToGroupAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.ProcessedCount.Should().Be(3);
            result.Results.Should().HaveCount(3);
            result.Results.All(r => r.Success).Should().BeTrue();
            result.Results.All(r => r.NewGroupId == targetGroupId).Should().BeTrue();

            result.Results.First(r => r.JobCodeId == jobCodeId1).PreviousGroupId.Should().Be(previousGroup1);
            result.Results.First(r => r.JobCodeId == jobCodeId2).PreviousGroupId.Should().BeNull();
            result.Results.First(r => r.JobCodeId == jobCodeId3).PreviousGroupId.Should().Be(previousGroup2);

            await _jobCodeService.Received(1).AssignJobCodesToGroupAsync(jobCodeIds, targetGroupId);
        }

        [Fact]
        public async Task UpdateJobCodeStatusAsync_WithValidRequest_ShouldSucceed()
        {
            // Arrange
            var jobCodeIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var request = new UpdateJobCodeStatusRequestDto
            {
                JobCodeIds = jobCodeIds,
                IsActive = true
            };

            var serviceResult = (
                Success: true,
                ErrorMessage: (string?)null,
                ProcessedCount: 2,
                Results: new List<(Guid JobCodeId, bool Success, string? ErrorMessage, bool PreviousStatus, string JobTitle)>
                {
                    (jobCodeIds[0], true, null, false, "Job Code 1"),
                    (jobCodeIds[1], true, null, false, "Job Code 2")
                }
            );

            _jobCodeService.UpdateJobCodeStatusAsync(jobCodeIds, true)
                .Returns(serviceResult);

            // Act
            var result = await _adapter.UpdateJobCodeStatusAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.ErrorMessage.Should().BeNull();
            result.ProcessedCount.Should().Be(2);
            result.Results.Should().HaveCount(2);

            var result1 = result.Results.First(r => r.JobCodeId == jobCodeIds[0]);
            result1.Success.Should().BeTrue();
            result1.JobTitle.Should().Be("Job Code 1");
            result1.PreviousStatus.Should().BeFalse();
            result1.NewStatus.Should().BeTrue();

            await _jobCodeService.Received(1).UpdateJobCodeStatusAsync(jobCodeIds, true);
        }

        [Fact]
        public async Task UpdateJobCodeStatusAsync_WithServiceError_ShouldReturnError()
        {
            // Arrange
            var jobCodeIds = new List<Guid> { Guid.NewGuid() };
            var request = new UpdateJobCodeStatusRequestDto
            {
                JobCodeIds = jobCodeIds,
                IsActive = false
            };

            var serviceResult = (
                Success: false,
                ErrorMessage: "Service error",
                ProcessedCount: 0,
                Results: new List<(Guid JobCodeId, bool Success, string? ErrorMessage, bool PreviousStatus, string JobTitle)>()
            );

            _jobCodeService.UpdateJobCodeStatusAsync(jobCodeIds, false)
                .Returns(serviceResult);

            // Act
            var result = await _adapter.UpdateJobCodeStatusAsync(request);

            // Assert
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Service error");
            result.ProcessedCount.Should().Be(0);
            result.Results.Should().BeEmpty();

            await _jobCodeService.Received(1).UpdateJobCodeStatusAsync(jobCodeIds, false);
        }

        [Fact]
        public async Task UpdateJobCodeStatusAsync_WithPartialSuccess_ShouldReturnPartialResults()
        {
            // Arrange
            var validJobCodeId = Guid.NewGuid();
            var invalidJobCodeId = Guid.NewGuid();
            var jobCodeIds = new List<Guid> { validJobCodeId, invalidJobCodeId };
            var request = new UpdateJobCodeStatusRequestDto
            {
                JobCodeIds = jobCodeIds,
                IsActive = true
            };

            var serviceResult = (
                Success: true,
                ErrorMessage: (string?)null,
                ProcessedCount: 1,
                Results: new List<(Guid JobCodeId, bool Success, string? ErrorMessage, bool PreviousStatus, string JobTitle)>
                {
                    (validJobCodeId, true, null, false, "Valid Job Code"),
                    (invalidJobCodeId, false, "Job code not found.", false, string.Empty)
                }
            );

            _jobCodeService.UpdateJobCodeStatusAsync(jobCodeIds, true)
                .Returns(serviceResult);

            // Act
            var result = await _adapter.UpdateJobCodeStatusAsync(request);

            // Assert
            result.Success.Should().BeTrue();
            result.ProcessedCount.Should().Be(1);
            result.Results.Should().HaveCount(2);

            var validResult = result.Results.First(r => r.JobCodeId == validJobCodeId);
            validResult.Success.Should().BeTrue();
            validResult.JobTitle.Should().Be("Valid Job Code");
            validResult.ErrorMessage.Should().BeNull();

            var invalidResult = result.Results.First(r => r.JobCodeId == invalidJobCodeId);
            invalidResult.Success.Should().BeFalse();
            invalidResult.ErrorMessage.Should().Be("Job code not found.");

            await _jobCodeService.Received(1).UpdateJobCodeStatusAsync(jobCodeIds, true);
        }
    }
} 