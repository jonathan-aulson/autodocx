using System;
using System.IO;
using System.Net;
using System.Text;
using api.Adapters;
using api.Functions;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace BackendTests.Functions
{
    public class JobGroupsTest
    {
        private readonly JobGroups _jobGroups;
        private readonly IJobGroupServiceAdapter _jobGroupServiceAdapterMock;

        public JobGroupsTest()
        {
            _jobGroupServiceAdapterMock = Substitute.For<IJobGroupServiceAdapter>();
            var siteAssignmentServiceAdapterMock = Substitute.For<ISiteAssignmentServiceAdapter>();
            var loggerMock = Substitute.For<ILogger<JobGroups>>();
            _jobGroups = new JobGroups(_jobGroupServiceAdapterMock, siteAssignmentServiceAdapterMock, loggerMock);
        }

        [Fact]
        public void CreateJobGroup_ShouldReturnCreated_WhenTitleIsProvided()
        {
            // Arrange
            var context = Substitute.For<FunctionContext>();
            var groupTitle = "Test Group";
            var uri = new Uri($"http://localhost:7275/api/jobgroups/create?jobGroupTitle={Uri.EscapeDataString(groupTitle)}");
            var requestData = new FakeHttpRequestData(context, uri, new MemoryStream());

            // Act
            var result = _jobGroups.CreateJobGroup(requestData);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.Created);
            _jobGroupServiceAdapterMock.Received(1).CreateJobGroup(groupTitle);

            result.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = new StreamReader(result.Body).ReadToEnd();
            responseBody.Should().Contain("Job group created successfully");
        }

        [Fact]
        public void CreateJobGroup_ShouldReturnBadRequest_WhenTitleIsMissing()
        {
            // Arrange
            var context = Substitute.For<FunctionContext>();
            var uri = new Uri("http://localhost:7275/api/jobgroups/create");
            var requestData = new FakeHttpRequestData(context, uri, new MemoryStream());

            // Act
            var result = _jobGroups.CreateJobGroup(requestData);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            _jobGroupServiceAdapterMock.DidNotReceive().CreateJobGroup(Arg.Any<string>());

            result.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = new StreamReader(result.Body).ReadToEnd();
            responseBody.Should().Contain("Group title is required");
        }

        [Fact]
        public void DeactivateJobGroup_ShouldReturnNoContent_WhenIdIsValid()
        {
            // Arrange
            var context = Substitute.For<FunctionContext>();
            var jobGroupId = Guid.NewGuid();
            var uri = new Uri($"http://localhost:7275/api/jobgroups/{jobGroupId}");
            var requestData = new FakeHttpRequestData(context, uri, new MemoryStream());

            // Act
            var result = _jobGroups.DeactivateJobGroup(requestData, jobGroupId);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.NoContent);
            _jobGroupServiceAdapterMock.Received(1).DeactivateJobGroup(jobGroupId);
        }

        [Fact]
        public void DeactivateJobGroup_ShouldReturnBadRequest_WhenIdIsEmpty()
        {
            // Arrange
            var context = Substitute.For<FunctionContext>();
            var jobGroupId = Guid.Empty;
            var uri = new Uri("http://localhost:7275/api/jobgroups/00000000-0000-0000-0000-000000000000");
            var requestData = new FakeHttpRequestData(context, uri, new MemoryStream());

            // Act
            var result = _jobGroups.DeactivateJobGroup(requestData, jobGroupId);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            _jobGroupServiceAdapterMock.DidNotReceive().DeactivateJobGroup(Arg.Any<Guid>());

            result.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = new StreamReader(result.Body).ReadToEnd();
            responseBody.Should().Contain("Invalid job group ID");
        }

        [Fact]
        public void ActivateJobGroup_ShouldReturnNoContent_WhenIdIsValid()
        {
            // Arrange
            var context = Substitute.For<FunctionContext>();
            var jobGroupId = Guid.NewGuid();
            var uri = new Uri($"http://localhost:7275/api/jobgroups/{jobGroupId}/activate");
            var requestData = new FakeHttpRequestData(context, uri, new MemoryStream());

            // Act
            var result = _jobGroups.ActivateJobGroup(requestData, jobGroupId);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.NoContent);
            _jobGroupServiceAdapterMock.Received(1).ActivateJobGroup(jobGroupId);
        }

        [Fact]
        public void ActivateJobGroup_ShouldReturnBadRequest_WhenIdIsEmpty()
        {
            // Arrange
            var context = Substitute.For<FunctionContext>();
            var jobGroupId = Guid.Empty;
            var uri = new Uri("http://localhost:7275/api/jobgroups/00000000-0000-0000-0000-000000000000/activate");
            var requestData = new FakeHttpRequestData(context, uri, new MemoryStream());

            // Act
            var result = _jobGroups.ActivateJobGroup(requestData, jobGroupId);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            _jobGroupServiceAdapterMock.DidNotReceive().ActivateJobGroup(Arg.Any<Guid>());

            result.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = new StreamReader(result.Body).ReadToEnd();
            responseBody.Should().Contain("Invalid job group ID");
        }
    }
}
