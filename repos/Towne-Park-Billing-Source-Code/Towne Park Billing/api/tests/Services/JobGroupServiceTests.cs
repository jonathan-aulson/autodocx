using System;
using System.Collections.Generic;
using api.Models.Vo;
using api.Services.Impl;
using api.Data;
using Xunit;

namespace BackendTests.Services
{
    // Simple fake repository for test control
    public class FakeJobGroupRepository : IJobGroupRepository
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

    public class JobGroupServiceTests
    {
        [Fact]
        public void CreateJobGroup_CallsRepository()
        {
            var fakeRepo = new FakeJobGroupRepository();
            var service = new JobGroupService(fakeRepo);

            service.CreateJobGroup("TestGroup");

            Assert.Contains("TestGroup", fakeRepo.CreatedGroups);
        }

        [Fact]
        public void CreateJobGroup_HandlesException()
        {
            var fakeRepo = new FakeJobGroupRepository { ExceptionToThrow = new Exception("fail") };
            var service = new JobGroupService(fakeRepo);

            Assert.Throws<Exception>(() => service.CreateJobGroup("TestGroup"));
        }

        [Fact]
        public void DeactivateJobGroup_CallsRepository()
        {
            var fakeRepo = new FakeJobGroupRepository();
            var service = new JobGroupService(fakeRepo);
            var groupId = Guid.NewGuid();

            service.DeactivateJobGroup(groupId);

            Assert.Contains(groupId, fakeRepo.DeactivatedGroups);
        }

        [Fact]
        public void DeactivateJobGroup_HandlesException()
        {
            var fakeRepo = new FakeJobGroupRepository { ExceptionToThrow = new Exception("fail") };
            var service = new JobGroupService(fakeRepo);

            Assert.Throws<Exception>(() => service.DeactivateJobGroup(Guid.NewGuid()));
        }

        [Fact]
        public void GetAllJobGroups_ReturnsFromRepository()
        {
            var expected = new List<JobGroupVo>
            {
                new JobGroupVo { Id = Guid.NewGuid(), Title = "A", IsActive = true, JobCodes = new List<JobCodeVo>() }
            };
            var fakeRepo = new FakeJobGroupRepository { JobGroupsToReturn = expected };
            var service = new JobGroupService(fakeRepo);

            var result = service.GetAllJobGroups();

            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetAllJobGroups_HandlesException()
        {
            var fakeRepo = new FakeJobGroupRepository { ExceptionToThrow = new Exception("fail") };
            var service = new JobGroupService(fakeRepo);

            Assert.Throws<Exception>(() => service.GetAllJobGroups());
        }

        [Fact]
        public void ActivateJobGroup_CallsRepository()
        {
            var fakeRepo = new FakeJobGroupRepository();
            var service = new JobGroupService(fakeRepo);
            var groupId = Guid.NewGuid();

            service.ActivateJobGroup(groupId);

            Assert.Contains(groupId, fakeRepo.ActivatedGroups);
        }

        [Fact]
        public void ActivateJobGroup_HandlesException()
        {
            var fakeRepo = new FakeJobGroupRepository { ExceptionToThrow = new Exception("fail") };
            var service = new JobGroupService(fakeRepo);

            Assert.Throws<Exception>(() => service.ActivateJobGroup(Guid.NewGuid()));
        }
    }
}
