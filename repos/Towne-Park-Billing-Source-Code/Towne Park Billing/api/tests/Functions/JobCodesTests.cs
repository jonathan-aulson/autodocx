using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using api.Adapters;
using api.Functions;
using api.Models.Dto;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace BackendTests.Functions
{
    public class JobCodesTests
    {
        private readonly IJobCodeServiceAdapter _jobCodeServiceAdapter;
        private readonly ILogger<JobCodes> _logger;
        private readonly JobCodes _function;

        public JobCodesTests()
        {
            _jobCodeServiceAdapter = Substitute.For<IJobCodeServiceAdapter>();
            _logger = Substitute.For<ILogger<JobCodes>>();
            _function = new JobCodes(_jobCodeServiceAdapter, _logger);
        }

        private HttpRequestData CreateMockRequest(string json = null)
        {
            var context = Substitute.For<FunctionContext>();
            Stream body = null;
            
            if (json != null)
            {
                body = new MemoryStream(Encoding.UTF8.GetBytes(json));
            }

            return new FakeHttpRequestData(
                context, 
                new Uri("https://example.com/api/jobcodes/assign"), 
                body);
        }

        [Fact]
        public async Task AssignJobCodesToGroup_WithValidRequest_ShouldReturnOk()
        {
            // Arrange
            var jobCodeIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var targetGroupId = Guid.NewGuid();
            var requestDto = new AssignJobCodesToGroupRequestDto
            {
                JobCodeIds = jobCodeIds,
                TargetGroupId = targetGroupId
            };

            var json = JsonSerializer.Serialize(requestDto);
            var request = CreateMockRequest(json);

            var expectedResponse = new AssignJobCodesToGroupResponseDto
            {
                Success = true,
                ProcessedCount = 2,
                Results = new List<JobCodeAssignmentDto>
                {
                    new JobCodeAssignmentDto { JobCodeId = jobCodeIds[0], Success = true, NewGroupId = targetGroupId },
                    new JobCodeAssignmentDto { JobCodeId = jobCodeIds[1], Success = true, NewGroupId = targetGroupId }
                }
            };

            _jobCodeServiceAdapter.AssignJobCodesToGroupAsync(Arg.Any<AssignJobCodesToGroupRequestDto>())
                .Returns(expectedResponse);

            // Act
            var result = await _function.AssignJobCodesToGroup(request);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.OK);
            await _jobCodeServiceAdapter.Received(1).AssignJobCodesToGroupAsync(
                Arg.Is<AssignJobCodesToGroupRequestDto>(r => 
                    r.JobCodeIds.SequenceEqual(jobCodeIds) && 
                    r.TargetGroupId == targetGroupId));
        }

        [Fact]
        public async Task AssignJobCodesToGroup_WithEmptyBody_ShouldReturnBadRequest()
        {
            // Arrange
            var request = CreateMockRequest("");

            // Act
            var result = await _function.AssignJobCodesToGroup(request);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            await _jobCodeServiceAdapter.DidNotReceive().AssignJobCodesToGroupAsync(Arg.Any<AssignJobCodesToGroupRequestDto>());
        }

        [Fact]
        public async Task AssignJobCodesToGroup_WithInvalidJson_ShouldReturnBadRequest()
        {
            // Arrange
            var request = CreateMockRequest("{ invalid json }");

            // Act
            var result = await _function.AssignJobCodesToGroup(request);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            await _jobCodeServiceAdapter.DidNotReceive().AssignJobCodesToGroupAsync(Arg.Any<AssignJobCodesToGroupRequestDto>());
        }

        [Fact]
        public async Task AssignJobCodesToGroup_WithNullJobCodeIds_ShouldReturnBadRequest()
        {
            // Arrange
            var requestDto = new AssignJobCodesToGroupRequestDto
            {
                JobCodeIds = null,
                TargetGroupId = Guid.NewGuid()
            };

            var json = JsonSerializer.Serialize(requestDto);
            var request = CreateMockRequest(json);

            // Act
            var result = await _function.AssignJobCodesToGroup(request);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            await _jobCodeServiceAdapter.DidNotReceive().AssignJobCodesToGroupAsync(Arg.Any<AssignJobCodesToGroupRequestDto>());
        }

        [Fact]
        public async Task AssignJobCodesToGroup_WithEmptyJobCodeIds_ShouldReturnBadRequest()
        {
            // Arrange
            var requestDto = new AssignJobCodesToGroupRequestDto
            {
                JobCodeIds = new List<Guid>(),
                TargetGroupId = Guid.NewGuid()
            };

            var json = JsonSerializer.Serialize(requestDto);
            var request = CreateMockRequest(json);

            // Act
            var result = await _function.AssignJobCodesToGroup(request);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            await _jobCodeServiceAdapter.DidNotReceive().AssignJobCodesToGroupAsync(Arg.Any<AssignJobCodesToGroupRequestDto>());
        }

        [Fact]
        public async Task AssignJobCodesToGroup_WithTooManyJobCodes_ShouldReturnBadRequest()
        {
            // Arrange
            var requestDto = new AssignJobCodesToGroupRequestDto
            {
                JobCodeIds = Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToList(),
                TargetGroupId = Guid.NewGuid()
            };

            var json = JsonSerializer.Serialize(requestDto);
            var request = CreateMockRequest(json);

            // Act
            var result = await _function.AssignJobCodesToGroup(request);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            await _jobCodeServiceAdapter.DidNotReceive().AssignJobCodesToGroupAsync(Arg.Any<AssignJobCodesToGroupRequestDto>());
        }

        [Fact]
        public async Task AssignJobCodesToGroup_WithEmptyTargetGroupId_ShouldReturnBadRequest()
        {
            // Arrange
            var requestDto = new AssignJobCodesToGroupRequestDto
            {
                JobCodeIds = new List<Guid> { Guid.NewGuid() },
                TargetGroupId = Guid.Empty
            };

            var json = JsonSerializer.Serialize(requestDto);
            var request = CreateMockRequest(json);

            // Act
            var result = await _function.AssignJobCodesToGroup(request);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            await _jobCodeServiceAdapter.DidNotReceive().AssignJobCodesToGroupAsync(Arg.Any<AssignJobCodesToGroupRequestDto>());
        }

        [Fact]
        public async Task AssignJobCodesToGroup_WithDuplicateJobCodes_ShouldReturnBadRequest()
        {
            // Arrange
            var jobCodeId = Guid.NewGuid();
            var requestDto = new AssignJobCodesToGroupRequestDto
            {
                JobCodeIds = new List<Guid> { jobCodeId, jobCodeId }, // Duplicate
                TargetGroupId = Guid.NewGuid()
            };

            var json = JsonSerializer.Serialize(requestDto);
            var request = CreateMockRequest(json);

            // Act
            var result = await _function.AssignJobCodesToGroup(request);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            await _jobCodeServiceAdapter.DidNotReceive().AssignJobCodesToGroupAsync(Arg.Any<AssignJobCodesToGroupRequestDto>());
        }

        [Fact]
        public async Task AssignJobCodesToGroup_WithValidationError_ShouldReturnBadRequest()
        {
            // Arrange
            var jobCodeIds = new List<Guid> { Guid.NewGuid() };
            var targetGroupId = Guid.NewGuid();
            var requestDto = new AssignJobCodesToGroupRequestDto
            {
                JobCodeIds = jobCodeIds,
                TargetGroupId = targetGroupId
            };

            var json = JsonSerializer.Serialize(requestDto);
            var request = CreateMockRequest(json);

            var errorResponse = new AssignJobCodesToGroupResponseDto
            {
                Success = false,
                ErrorMessage = "Target job group not found or is inactive.",
                ProcessedCount = 0,
                Results = new List<JobCodeAssignmentDto>()
            };

            _jobCodeServiceAdapter.AssignJobCodesToGroupAsync(Arg.Any<AssignJobCodesToGroupRequestDto>())
                .Returns(errorResponse);

            // Act
            var result = await _function.AssignJobCodesToGroup(request);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            await _jobCodeServiceAdapter.Received(1).AssignJobCodesToGroupAsync(Arg.Any<AssignJobCodesToGroupRequestDto>());
        }

        [Fact]
        public async Task AssignJobCodesToGroup_WithServerError_ShouldReturnInternalServerError()
        {
            // Arrange
            var jobCodeIds = new List<Guid> { Guid.NewGuid() };
            var targetGroupId = Guid.NewGuid();
            var requestDto = new AssignJobCodesToGroupRequestDto
            {
                JobCodeIds = jobCodeIds,
                TargetGroupId = targetGroupId
            };

            var json = JsonSerializer.Serialize(requestDto);
            var request = CreateMockRequest(json);

            var errorResponse = new AssignJobCodesToGroupResponseDto
            {
                Success = false,
                ErrorMessage = "Database connection failed.",
                ProcessedCount = 0,
                Results = new List<JobCodeAssignmentDto>()
            };

            _jobCodeServiceAdapter.AssignJobCodesToGroupAsync(Arg.Any<AssignJobCodesToGroupRequestDto>())
                .Returns(errorResponse);

            // Act
            var result = await _function.AssignJobCodesToGroup(request);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            await _jobCodeServiceAdapter.Received(1).AssignJobCodesToGroupAsync(Arg.Any<AssignJobCodesToGroupRequestDto>());
        }

        [Fact]
        public async Task AssignJobCodesToGroup_WithException_ShouldReturnInternalServerError()
        {
            // Arrange
            var jobCodeIds = new List<Guid> { Guid.NewGuid() };
            var targetGroupId = Guid.NewGuid();
            var requestDto = new AssignJobCodesToGroupRequestDto
            {
                JobCodeIds = jobCodeIds,
                TargetGroupId = targetGroupId
            };

            var json = JsonSerializer.Serialize(requestDto);
            var request = CreateMockRequest(json);

            _jobCodeServiceAdapter.AssignJobCodesToGroupAsync(Arg.Any<AssignJobCodesToGroupRequestDto>())
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _function.AssignJobCodesToGroup(request);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            await _jobCodeServiceAdapter.Received(1).AssignJobCodesToGroupAsync(Arg.Any<AssignJobCodesToGroupRequestDto>());
        }

        [Fact]
        public async Task AssignJobCodesToGroup_WithPartialSuccess_ShouldReturnOkWithResults()
        {
            // Arrange
            var validJobCodeId = Guid.NewGuid();
            var invalidJobCodeId = Guid.NewGuid();
            var jobCodeIds = new List<Guid> { validJobCodeId, invalidJobCodeId };
            var targetGroupId = Guid.NewGuid();
            var requestDto = new AssignJobCodesToGroupRequestDto
            {
                JobCodeIds = jobCodeIds,
                TargetGroupId = targetGroupId
            };

            var json = JsonSerializer.Serialize(requestDto);
            var request = CreateMockRequest(json);

            var expectedResponse = new AssignJobCodesToGroupResponseDto
            {
                Success = true,
                ProcessedCount = 1,
                Results = new List<JobCodeAssignmentDto>
                {
                    new JobCodeAssignmentDto { JobCodeId = validJobCodeId, Success = true, NewGroupId = targetGroupId },
                    new JobCodeAssignmentDto { JobCodeId = invalidJobCodeId, Success = false, ErrorMessage = "Job code is inactive.", NewGroupId = targetGroupId }
                }
            };

            _jobCodeServiceAdapter.AssignJobCodesToGroupAsync(Arg.Any<AssignJobCodesToGroupRequestDto>())
                .Returns(expectedResponse);

            // Act
            var result = await _function.AssignJobCodesToGroup(request);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.OK);
            await _jobCodeServiceAdapter.Received(1).AssignJobCodesToGroupAsync(Arg.Any<AssignJobCodesToGroupRequestDto>());
        }

        [Fact]
        public async Task GetJobCodesBySite_WithValidSiteId_ReturnsJobCodes()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var expectedJobCodes = new List<JobCodeDto>
           {
               new JobCodeDto { JobCodeId = Guid.NewGuid(), JobCode = "JC1", JobTitle = "Job Title 1", IsActive = true },
               new JobCodeDto { JobCodeId = Guid.NewGuid(), JobCode = "JC2", JobTitle = "Job Title 2", IsActive = false }
           };
            _jobCodeServiceAdapter.GetJobCodesBySiteAsync(siteId).Returns(expectedJobCodes);
            var context = Substitute.For<FunctionContext>();
            var req = new FakeHttpRequestData(context, new Uri($"https://example.com/api/job-codes/by-site/{siteId}"), null);

            // Act
            var response = await _function.GetJobCodesBySite(req, siteId);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Headers.TryGetValues("Content-Type", out var contentTypeValues).Should().BeTrue();
            contentTypeValues.Should().ContainSingle().Which.Should().Be("application/json");
            var body = await ((FakeHttpResponseData)response).ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<JobCodeDto>>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result[0].JobCode.Should().Be("JC1");
            result[1].JobTitle.Should().Be("Job Title 2");
        }

        [Fact]
        public async Task GetJobCodesBySite_WithEmptySiteId_ReturnsBadRequest()
        {
            // Arrange
            var siteId = Guid.Empty;
            var context = Substitute.For<FunctionContext>();
            var req = new FakeHttpRequestData(context, new Uri($"https://example.com/api/job-codes/by-site/{siteId}"), null);

            // Act
            var response = await _function.GetJobCodesBySite(req, siteId);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var body = await ((FakeHttpResponseData)response).ReadAsStringAsync();
            body.Should().Contain("Site ID is required");
        }

        [Fact]
        public async Task GetJobCodesBySite_WhenAdapterThrowsArgumentException_ReturnsBadRequest()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            _jobCodeServiceAdapter.GetJobCodesBySiteAsync(siteId).Throws(new ArgumentException("Invalid site id!"));
            var context = Substitute.For<FunctionContext>();
            var req = new FakeHttpRequestData(context, new Uri($"https://example.com/api/job-codes/by-site/{siteId}"), null);

            // Act
            var response = await _function.GetJobCodesBySite(req, siteId);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var body = await ((FakeHttpResponseData)response).ReadAsStringAsync();
            body.Should().Contain("Invalid site id!");
        }

        [Fact]
        public async Task GetJobCodesBySite_WhenAdapterThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            _jobCodeServiceAdapter.GetJobCodesBySiteAsync(siteId).Throws(new Exception("Unexpected error!"));
            var context = Substitute.For<FunctionContext>();
            var req = new FakeHttpRequestData(context, new Uri($"https://example.com/api/job-codes/by-site/{siteId}"), null);

            // Act
            var response = await _function.GetJobCodesBySite(req, siteId);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            var body = await ((FakeHttpResponseData)response).ReadAsStringAsync();
            body.Should().Contain("Unexpected error!");
        }
    }
}
