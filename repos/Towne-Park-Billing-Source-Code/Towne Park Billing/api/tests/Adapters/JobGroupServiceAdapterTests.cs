using System;
using System.Collections.Generic;
using api.Adapters.Impl;
using api.Models.Vo;
using api.Services;
using Xunit;

namespace BackendTests.Adapters
{
    // Simple fake service for test control
    public class FakeJobGroupService : IJobGroupService
    {
        public List<string> CreatedGroups { get; } = new();
        public List<Guid> DeactivatedGroups { get; } = new();
        public List<Guid> ActivatedGroups { get; } = new();
        public IEnumerable<JobGroupVo> JobGroupsToReturn { get; set; } = new List<JobGroupVo>();
        public Exception ExceptionToThrow { get; set; }

        public void CreateJobGroup(string groupTitle)
        {
            if (ExceptionToThrow != null)
                throw ExceptionToThrow;
            CreatedGroups.Add(groupTitle);
        }

        public void DeactivateJobGroup(Guid jobGroupId)
        {
            if (ExceptionToThrow != null)
                throw ExceptionToThrow;
            DeactivatedGroups.Add(jobGroupId);
        }

        public void ActivateJobGroup(Guid jobGroupId)
        {
            if (ExceptionToThrow != null)
                throw ExceptionToThrow;
            ActivatedGroups.Add(jobGroupId);
        }

        public IEnumerable<JobGroupVo> GetAllJobGroups()
        {
            if (ExceptionToThrow != null)
                throw ExceptionToThrow;
            return JobGroupsToReturn;
        }
    }

    public class JobGroupServiceAdapterTests
    {
        [Fact]
        public void CreateJobGroup_DelegatesToService()
        {
            var fakeService = new FakeJobGroupService();
            var adapter = new JobGroupServiceAdapter(fakeService);

            adapter.CreateJobGroup("TestGroup");

            Assert.Contains("TestGroup", fakeService.CreatedGroups);
        }

        [Fact]
        public void CreateJobGroup_HandlesException()
        {
            var fakeService = new FakeJobGroupService { ExceptionToThrow = new Exception("fail") };
            var adapter = new JobGroupServiceAdapter(fakeService);

            Assert.Throws<Exception>(() => adapter.CreateJobGroup("TestGroup"));
        }

        [Fact]
        public void DeactivateJobGroup_DelegatesToService()
        {
            var fakeService = new FakeJobGroupService();
            var adapter = new JobGroupServiceAdapter(fakeService);
            var groupId = Guid.NewGuid();

            adapter.DeactivateJobGroup(groupId);

            Assert.Contains(groupId, fakeService.DeactivatedGroups);
        }

        [Fact]
        public void DeactivateJobGroup_HandlesException()
        {
            var fakeService = new FakeJobGroupService { ExceptionToThrow = new Exception("fail") };
            var adapter = new JobGroupServiceAdapter(fakeService);

            Assert.Throws<Exception>(() => adapter.DeactivateJobGroup(Guid.NewGuid()));
        }

        [Fact]
        public void GetAllJobGroups_DelegatesToService()
        {
            var expected = new List<JobGroupVo>
            {
                new JobGroupVo { Id = Guid.NewGuid(), Title = "A", IsActive = true, JobCodes = new List<JobCodeVo>() }
            };
            var fakeService = new FakeJobGroupService { JobGroupsToReturn = expected };
            var adapter = new JobGroupServiceAdapter(fakeService);

            var result = adapter.GetAllJobGroups();

            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetAllJobGroups_HandlesException()
        {
            var fakeService = new FakeJobGroupService { ExceptionToThrow = new Exception("fail") };
            var adapter = new JobGroupServiceAdapter(fakeService);

            Assert.Throws<Exception>(() => adapter.GetAllJobGroups());
        }

        [Fact]
        public void ActivateJobGroup_DelegatesToService()
        {
            var fakeService = new FakeJobGroupService();
            var adapter = new JobGroupServiceAdapter(fakeService);
            var groupId = Guid.NewGuid();

            adapter.ActivateJobGroup(groupId);

            Assert.Contains(groupId, fakeService.ActivatedGroups);
        }

        [Fact]
        public void ActivateJobGroup_HandlesException()
        {
            var fakeService = new FakeJobGroupService { ExceptionToThrow = new Exception("fail") };
            var adapter = new JobGroupServiceAdapter(fakeService);

            Assert.Throws<Exception>(() => adapter.ActivateJobGroup(Guid.NewGuid()));
        }
    }
}
