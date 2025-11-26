using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using api.Adapters;
using api.Functions;
using api.Models.Dto;
using api.Models.Vo;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace BackendTests.Functions
{
    // Simple fake adapter for test control
    public class FakeJobGroupServiceAdapter : IJobGroupServiceAdapter
    {
        public IEnumerable<JobGroupVo> JobGroupsToReturn { get; set; } = new List<JobGroupVo>();
        public Exception ExceptionToThrow { get; set; }
        public string CreatedGroupTitle { get; set; }
        public Guid? DeactivatedGroupId { get; set; }
        public Guid? ActivatedGroupId { get; set; }

        public void CreateJobGroup(string groupTitle)
        {
            if (ExceptionToThrow != null)
                throw ExceptionToThrow;
            CreatedGroupTitle = groupTitle;
        }

        public void DeactivateJobGroup(Guid jobGroupId)
        {
            if (ExceptionToThrow != null)
                throw ExceptionToThrow;
            DeactivatedGroupId = jobGroupId;
        }

        public void ActivateJobGroup(Guid jobGroupId)
        {
            if (ExceptionToThrow != null)
                throw ExceptionToThrow;
            ActivatedGroupId = jobGroupId;
        }

        public IEnumerable<JobGroupVo> GetAllJobGroups()
        {
            if (ExceptionToThrow != null)
                throw ExceptionToThrow;
            return JobGroupsToReturn;
        }
    }

    public class JobGroupsFunctionTests
    {
        private readonly IJobGroupServiceAdapter _fakeJobGroupAdapter;
        private readonly ISiteAssignmentServiceAdapter _fakeSiteAssignmentAdapter;
        private readonly JobGroups _function;

        public JobGroupsFunctionTests()
        {
            _fakeJobGroupAdapter = Substitute.For<IJobGroupServiceAdapter>();
            _fakeSiteAssignmentAdapter = Substitute.For<ISiteAssignmentServiceAdapter>();
            _function = new JobGroups(_fakeJobGroupAdapter, _fakeSiteAssignmentAdapter, Substitute.For<ILogger<JobGroups>>());
        }

        private static HttpRequestData CreateHttpRequest(string url = "http://localhost/")
        {
            var context = Substitute.For<FunctionContext>();
            var request = Substitute.For<HttpRequestData>(context);
            request.Url.Returns(new Uri(url));
            request.CreateResponse().Returns(callInfo => 
            {
                var response = Substitute.For<HttpResponseData>(context);
                response.Body.Returns(new MemoryStream());
                response.Headers.Returns(new HttpHeadersCollection());
                return response;
            });
            return request;
        }

        private static string GetHeader(HttpResponseData response, string key)
        {
            return response.Headers.TryGetValues(key, out var values) 
                ? string.Join(",", values) 
                : string.Empty;
        }

        private static string ReadResponseBody(HttpResponseData response)
        {
            response.Body.Position = 0;
            return new StreamReader(response.Body).ReadToEnd();
        }

        [Fact]
        public async Task GetSiteAssignments_ReturnsSuccessfulResponse()
        {
            // Arrange
            var expectedResponse = new GetSiteAssignmentsResponseDto
            {
                SiteAssignments = new List<SiteAssignmentDto>
                {
                    new SiteAssignmentDto
                    {
                        SiteId = Guid.NewGuid(),
                        SiteNumber = "1001",
                        SiteName = "Downtown Hotel Plaza",
                        City = "Chicago",
                        AssignedJobGroups = new List<JobGroupAssignmentDto>
                        {
                            new JobGroupAssignmentDto { JobGroupId = Guid.NewGuid(), JobGroupName = "Management", IsActive = true }
                        },
                        JobGroupCount = 1,
                        HasUnassignedJobCodes = true
                    }
                },
                TotalCount = 1,
                Success = true
            };

            _fakeSiteAssignmentAdapter.GetSiteAssignmentsAsync().Returns(expectedResponse);

            var req = CreateHttpRequest();

            // Act
            var response = await _function.GetSiteAssignments(req);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", GetHeader(response, "Content-Type"));
            await _fakeSiteAssignmentAdapter.Received(1).GetSiteAssignmentsAsync();
        }

        [Fact]
        public async Task GetSiteAssignments_HandlesException_ReturnsInternalServerError()
        {
            // Arrange
            _fakeSiteAssignmentAdapter.GetSiteAssignmentsAsync().ThrowsAsync(new Exception("Database error"));

            var req = CreateHttpRequest();

            // Act
            var response = await _function.GetSiteAssignments(req);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Equal("application/json", GetHeader(response, "Content-Type"));
            await _fakeSiteAssignmentAdapter.Received(1).GetSiteAssignmentsAsync();
        }

        [Fact]
        public void GetJobGroups_ReturnsJobGroupsList()
        {
            var fakeAdapter = new FakeJobGroupServiceAdapter
            {
                JobGroupsToReturn = new List<JobGroupVo>
                {
                    new JobGroupVo
                    {
                        Id = Guid.NewGuid(),
                        Title = "Group A",
                        IsActive = true,
                        JobCodes = new List<JobCodeVo>
                        {
                            new JobCodeVo
                            {
                                JobCodeId = Guid.NewGuid(),
                                JobTitle = "Code 1",
                                IsActive = true
                            }
                        }
                    },
                    new JobGroupVo
                    {
                        Id = Guid.NewGuid(),
                        Title = "Group B",
                        IsActive = false,
                        JobCodes = new List<JobCodeVo>()
                    }
                }
            };

            var function = new JobGroups(fakeAdapter, Substitute.For<ISiteAssignmentServiceAdapter>(), Substitute.For<ILogger<JobGroups>>());
            var req = CreateHttpRequest();

            var response = function.GetJobGroups(req);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", GetHeader(response, "Content-Type"));
            var body = ReadResponseBody(response);
            var result = JsonSerializer.Deserialize<List<JobGroupVo>>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Group A", result[0].Title);
            Assert.Equal("Group B", result[1].Title);
        }

        [Fact]
        public void GetJobGroups_ReturnsEmptyList()
        {
            var fakeAdapter = new FakeJobGroupServiceAdapter
            {
                JobGroupsToReturn = new List<JobGroupVo>()
            };
            var function = new JobGroups(fakeAdapter, Substitute.For<ISiteAssignmentServiceAdapter>(), Substitute.For<ILogger<JobGroups>>());
            var req = CreateHttpRequest();

            var response = function.GetJobGroups(req);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", GetHeader(response, "Content-Type"));
            var body = ReadResponseBody(response);
            Assert.Equal("[]", body.Trim());
            var result = JsonSerializer.Deserialize<List<JobGroupVo>>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void GetJobGroups_HandlesException()
        {
            var fakeAdapter = new FakeJobGroupServiceAdapter
            {
                ExceptionToThrow = new Exception("Database error")
            };
            var function = new JobGroups(fakeAdapter, Substitute.For<ISiteAssignmentServiceAdapter>(), Substitute.For<ILogger<JobGroups>>());
            var req = CreateHttpRequest();

            Assert.Throws<Exception>(() => function.GetJobGroups(req));
        }

        [Fact]
        public void CreateJobGroup_Success()
        {
            var fakeAdapter = new FakeJobGroupServiceAdapter();
            var function = new JobGroups(fakeAdapter, Substitute.For<ISiteAssignmentServiceAdapter>(), Substitute.For<ILogger<JobGroups>>());
            var req = CreateHttpRequest("http://localhost/jobgroups/create?jobGroupTitle=TestGroup");

            var response = function.CreateJobGroup(req);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var body = ReadResponseBody(response);
            Assert.Contains("Job group created successfully", body);
            Assert.Equal("TestGroup", fakeAdapter.CreatedGroupTitle);
        }

        [Fact]
        public void CreateJobGroup_MissingTitle_ReturnsBadRequest()
        {
            var fakeAdapter = new FakeJobGroupServiceAdapter();
            var function = new JobGroups(fakeAdapter, Substitute.For<ISiteAssignmentServiceAdapter>(), Substitute.For<ILogger<JobGroups>>());
            var req = CreateHttpRequest("http://localhost/jobgroups/create");

            var response = function.CreateJobGroup(req);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = ReadResponseBody(response);
            Assert.Contains("Group title is required", body);
        }

        [Fact]
        public void CreateJobGroup_HandlesException()
        {
            var fakeAdapter = new FakeJobGroupServiceAdapter { ExceptionToThrow = new Exception("fail") };
            var function = new JobGroups(fakeAdapter, Substitute.For<ISiteAssignmentServiceAdapter>(), Substitute.For<ILogger<JobGroups>>());
            var req = CreateHttpRequest("http://localhost/jobgroups/create?jobGroupTitle=TestGroup");

            Assert.Throws<Exception>(() => function.CreateJobGroup(req));
        }

        [Fact]
        public void DeactivateJobGroup_Success()
        {
            var fakeAdapter = new FakeJobGroupServiceAdapter();
            var function = new JobGroups(fakeAdapter, Substitute.For<ISiteAssignmentServiceAdapter>(), Substitute.For<ILogger<JobGroups>>());
            var req = CreateHttpRequest("http://localhost/jobgroups/123");
            var groupId = Guid.NewGuid();

            var response = function.DeactivateJobGroup(req, groupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(groupId, fakeAdapter.DeactivatedGroupId);
        }

        [Fact]
        public void DeactivateJobGroup_InvalidId_ReturnsBadRequest()
        {
            var fakeAdapter = new FakeJobGroupServiceAdapter();
            var function = new JobGroups(fakeAdapter, Substitute.For<ISiteAssignmentServiceAdapter>(), Substitute.For<ILogger<JobGroups>>());
            var req = CreateHttpRequest("http://localhost/jobgroups/invalid");

            var response = function.DeactivateJobGroup(req, Guid.Empty);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = ReadResponseBody(response);
            Assert.Contains("Invalid job group ID", body);
        }

        [Fact]
        public void DeactivateJobGroup_HandlesException()
        {
            var fakeAdapter = new FakeJobGroupServiceAdapter { ExceptionToThrow = new Exception("fail") };
            var function = new JobGroups(fakeAdapter, Substitute.For<ISiteAssignmentServiceAdapter>(), Substitute.For<ILogger<JobGroups>>());
            var req = CreateHttpRequest("http://localhost/jobgroups/123");
            var groupId = Guid.NewGuid();

            Assert.Throws<Exception>(() => function.DeactivateJobGroup(req, groupId));
        }

        [Fact]
        public void ActivateJobGroup_Success()
        {
            var fakeAdapter = new FakeJobGroupServiceAdapter();
            var function = new JobGroups(fakeAdapter, Substitute.For<ISiteAssignmentServiceAdapter>(), Substitute.For<ILogger<JobGroups>>());
            var req = CreateHttpRequest("http://localhost/jobgroups/123/activate");
            var groupId = Guid.NewGuid();

            var response = function.ActivateJobGroup(req, groupId);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(groupId, fakeAdapter.ActivatedGroupId);
        }

        [Fact]
        public void ActivateJobGroup_InvalidId_ReturnsBadRequest()
        {
            var fakeAdapter = new FakeJobGroupServiceAdapter();
            var function = new JobGroups(fakeAdapter, Substitute.For<ISiteAssignmentServiceAdapter>(), Substitute.For<ILogger<JobGroups>>());
            var req = CreateHttpRequest("http://localhost/jobgroups/invalid/activate");

            var response = function.ActivateJobGroup(req, Guid.Empty);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = ReadResponseBody(response);
            Assert.Contains("Invalid job group ID", body);
        }

        [Fact]
        public void ActivateJobGroup_HandlesException()
        {
            var fakeAdapter = new FakeJobGroupServiceAdapter { ExceptionToThrow = new Exception("fail") };
            var function = new JobGroups(fakeAdapter, Substitute.For<ISiteAssignmentServiceAdapter>(), Substitute.For<ILogger<JobGroups>>());
            var req = CreateHttpRequest("http://localhost/jobgroups/123/activate");
            var groupId = Guid.NewGuid();

            Assert.Throws<Exception>(() => function.ActivateJobGroup(req, groupId));
        }
    }
}
