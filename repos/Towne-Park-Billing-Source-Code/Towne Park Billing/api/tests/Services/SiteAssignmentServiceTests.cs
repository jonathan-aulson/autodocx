using api.Data;
using api.Models.Vo;
using api.Services.Impl;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace BackendTests.Services
{
    public class SiteAssignmentServiceTests
    {
        private readonly ISiteAssignmentRepository _mockRepository;
        private readonly SiteAssignmentService _service;

        public SiteAssignmentServiceTests()
        {
            _mockRepository = Substitute.For<ISiteAssignmentRepository>();
            _service = new SiteAssignmentService(_mockRepository);
        }

        [Fact]
        public async Task GetSiteAssignmentsAsync_ShouldReturnAllSites()
        {
            // Arrange
            var mockSites = CreateMockSiteAssignments();
            _mockRepository.GetSiteAssignmentsAsync().Returns(mockSites);

            // Act
            var result = await _service.GetSiteAssignmentsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            await _mockRepository.Received(1).GetSiteAssignmentsAsync();
        }

        [Fact]
        public async Task GetSiteAssignmentsAsync_ShouldReturnEmptyList_WhenNoSites()
        {
            // Arrange
            var emptySites = new List<SiteAssignmentVo>();
            _mockRepository.GetSiteAssignmentsAsync().Returns(emptySites);

            // Act
            var result = await _service.GetSiteAssignmentsAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
            await _mockRepository.Received(1).GetSiteAssignmentsAsync();
        }

        private List<SiteAssignmentVo> CreateMockSiteAssignments()
        {
            return new List<SiteAssignmentVo>
            {
                new()
                {
                    SiteId = Guid.NewGuid(),
                    SiteNumber = "1002",
                    SiteName = "Airport Marriott",
                    City = "Atlanta",
                    AssignedJobGroups = new List<JobGroupAssignmentVo>
                    {
                        new() { JobGroupId = Guid.NewGuid(), JobGroupName = "Valet Services", IsActive = true },
                        new() { JobGroupId = Guid.NewGuid(), JobGroupName = "Customer Service", IsActive = true }
                    },
                    JobGroupCount = 2,
                    HasUnassignedJobCodes = false
                },
                new()
                {
                    SiteId = Guid.NewGuid(),
                    SiteNumber = "1001",
                    SiteName = "Downtown Hotel Plaza",
                    City = "Chicago",
                    AssignedJobGroups = new List<JobGroupAssignmentVo>
                    {
                        new() { JobGroupId = Guid.NewGuid(), JobGroupName = "Management", IsActive = true }
                    },
                    JobGroupCount = 1,
                    HasUnassignedJobCodes = true
                },
                new()
                {
                    SiteId = Guid.NewGuid(),
                    SiteNumber = "1008",
                    SiteName = "University Campus",
                    City = "Austin",
                    AssignedJobGroups = new List<JobGroupAssignmentVo>(),
                    JobGroupCount = 0,
                    HasUnassignedJobCodes = true
                }
            };
        }
    }
} 