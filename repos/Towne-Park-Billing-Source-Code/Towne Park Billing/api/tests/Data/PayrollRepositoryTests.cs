using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using api.Data.Impl;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using TownePark;
using api.Services;

namespace BackendTests.Data
{
    public class FakeDataverseService : IDataverseService
    {
        public IOrganizationService ServiceClient { get; set; }
        public IOrganizationService GetServiceClient() => ServiceClient;
    }

    public class FakeOrganizationService : IOrganizationService
    {
        public List<Entity> PayrollDetails { get; set; } = new();
        public List<Entity> JobCodes { get; set; } = new();
        public List<Entity> JobGroups { get; set; } = new();
        public List<Entity> JobCodesBySite { get; set; } = new();

        public EntityCollection RetrieveMultiple(QueryBase query)
        {
            var q = query as QueryExpression;
            if (q.EntityName == bs_PayrollDetail.EntityLogicalName)
                return new EntityCollection(PayrollDetails);
            if (q.EntityName == "bs_jobcode")
                return new EntityCollection(JobCodes);
            if (q.EntityName == "bs_jobgroup")
                return new EntityCollection(JobGroups);
            if (q.EntityName == "bs_jobcodesbysite")
                return new EntityCollection(JobCodesBySite);
            if (q.EntityName == bs_Payroll.EntityLogicalName)
                return new EntityCollection(new List<Entity>
                {
                    new Entity(bs_Payroll.EntityLogicalName)
                    {
                        Id = Guid.NewGuid(),
                        [bs_Payroll.Fields.bs_CustomerSiteFK] = new EntityReference("bs_customersite", Guid.NewGuid()),
                        [bs_Payroll.Fields.bs_Period] = "2025-06",
                        [bs_Payroll.Fields.bs_Name] = "Test Payroll"
                    }
                });
            return new EntityCollection();
        }

        // Unused methods for this test
        public Guid Create(Entity entity) => throw new NotImplementedException();
        public void Update(Entity entity) => throw new NotImplementedException();
        public void Delete(string entityName, Guid id) => throw new NotImplementedException();
        public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet) => throw new NotImplementedException();
        public OrganizationResponse Execute(OrganizationRequest request) => throw new NotImplementedException();
        public void Associate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities) => throw new NotImplementedException();
        public void Disassociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities) => throw new NotImplementedException();
    }

    public class FakeBatchOrganizationService : IOrganizationService
    {
        public List<Entity> PayrollEntities { get; set; } = new();
        public List<Entity> PayrollDetails { get; set; } = new();
        public List<Entity> JobCodes { get; set; } = new();
        public List<Entity> JobGroups { get; set; } = new();
        public List<Entity> JobCodesBySite { get; set; } = new();

        public EntityCollection RetrieveMultiple(QueryBase query)
        {
            var q = query as QueryExpression;
            if (q.EntityName == bs_PayrollDetail.EntityLogicalName)
                return new EntityCollection(PayrollDetails);
            if (q.EntityName == "bs_jobcode")
                return new EntityCollection(JobCodes);
            if (q.EntityName == "bs_jobgroup")
                return new EntityCollection(JobGroups);
            if (q.EntityName == "bs_jobcodesbysite")
                return new EntityCollection(JobCodesBySite);
            if (q.EntityName == bs_Payroll.EntityLogicalName)
                return new EntityCollection(PayrollEntities);
            return new EntityCollection();
        }

        // Unused methods for this test
        public Guid Create(Entity entity) => throw new NotImplementedException();
        public void Update(Entity entity) => throw new NotImplementedException();
        public void Delete(string entityName, Guid id) => throw new NotImplementedException();
        public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet) => throw new NotImplementedException();
        public OrganizationResponse Execute(OrganizationRequest request) => throw new NotImplementedException();
        public void Associate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities) => throw new NotImplementedException();
        public void Disassociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities) => throw new NotImplementedException();
    }

    public class PayrollRepositoryTests
    {
        [Fact]
        public void GetPayroll_AttachesJobCodeAndGroupDisplayFields()
        {
            // Arrange
            var jobCodeId = Guid.NewGuid();
            var jobGroupId = Guid.NewGuid();
            var siteId = Guid.NewGuid();

            var payrollDetail = new Entity(bs_PayrollDetail.EntityLogicalName)
            {
                Id = Guid.NewGuid(),
                [bs_PayrollDetail.Fields.bs_JobCodeFK] = new EntityReference("bs_jobcode", jobCodeId),
                [bs_PayrollDetail.Fields.bs_JobGroupFK] = new EntityReference("bs_jobgroup", jobGroupId),
                [bs_PayrollDetail.Fields.bs_RegularHours] = 10m
            };

            var jobCode = new Entity("bs_jobcode")
            {
                Id = jobCodeId,
                ["bs_jobcode"] = "4321",
                ["bs_jobtitle"] = "Valet Attendant"
            };

            var jobGroup = new Entity("bs_jobgroup")
            {
                Id = jobGroupId,
                ["bs_jobgrouptitle"] = "Valet"
            };

            // Add the job code assignment to site
            var jobCodeBySite = new Entity("bs_jobcodesbysite")
            {
                Id = Guid.NewGuid(),
                ["bs_customersite"] = new EntityReference("bs_customersite", siteId),
                ["bs_jobcode"] = new EntityReference("bs_jobcode", jobCodeId)
            };

            var fakeOrgService = new FakeOrganizationService
            {
                PayrollDetails = new List<Entity> { payrollDetail },
                JobCodes = new List<Entity> { jobCode },
                JobGroups = new List<Entity> { jobGroup },
                JobCodesBySite = new List<Entity> { jobCodeBySite }
            };

            var fakeDataverse = new FakeDataverseService { ServiceClient = fakeOrgService };
            var repo = new PayrollRepository(fakeDataverse);

            // Act
            var payroll = repo.GetPayroll(siteId, "2025-06");

            // Assert
            Assert.NotNull(payroll);
            Assert.NotNull(payroll.bs_PayrollDetail_Payroll);
            Assert.Single(payroll.bs_PayrollDetail_Payroll);

            var detail = System.Linq.Enumerable.First(payroll.bs_PayrollDetail_Payroll);
            Assert.True(detail.Contains("jobcode_display"));
            Assert.True(detail.Contains("jobcode_displayname"));
            Assert.Equal("4321", detail["jobcode_display"]);
            Assert.Equal("Valet Attendant", detail["jobcode_displayname"]);
        }

        [Fact]
        public void GetPayroll_WithNoPayrollDetails_ShouldReturnPayrollWithEmptyDetails()
        {
            // Arrange
            var siteId = Guid.NewGuid();

            var fakeOrgService = new FakeOrganizationService
            {
                PayrollDetails = new List<Entity>(), // Empty payroll details
                JobCodes = new List<Entity>(),
                JobGroups = new List<Entity>(),
                JobCodesBySite = new List<Entity>() // No job codes assigned to site
            };

            var fakeDataverse = new FakeDataverseService { ServiceClient = fakeOrgService };
            var repo = new PayrollRepository(fakeDataverse);

            // Act & Assert - This should not throw an exception
            var payroll = repo.GetPayroll(siteId, "2025-06");

            // Assert
            Assert.NotNull(payroll);
            // When no payroll details are found, the property should be null (not an empty collection)
            // This avoids the "Sequence contains no elements" error in the generated entity setter
            Assert.Null(payroll.bs_PayrollDetail_Payroll);
        }

        [Fact]
        public async Task GetPayrollBatchAsync_SingleSite_AttachesJobCodeAndGroupDisplayFields()
        {
            // Arrange
            var jobCodeId = Guid.NewGuid();
            var jobGroupId = Guid.NewGuid();
            var siteId = Guid.NewGuid();
            var billingPeriod = "2025-06";

            var payrollEntity = new Entity(bs_Payroll.EntityLogicalName)
            {
                Id = Guid.NewGuid(),
                [bs_Payroll.Fields.bs_CustomerSiteFK] = new EntityReference("bs_customersite", siteId),
                [bs_Payroll.Fields.bs_Period] = billingPeriod,
                [bs_Payroll.Fields.bs_Name] = "Test Payroll"
            };

            var payrollDetail = new Entity(bs_PayrollDetail.EntityLogicalName)
            {
                Id = Guid.NewGuid(),
                [bs_PayrollDetail.Fields.bs_PayrollFK] = new EntityReference(bs_Payroll.EntityLogicalName, payrollEntity.Id),
                [bs_PayrollDetail.Fields.bs_JobCodeFK] = new EntityReference("bs_jobcode", jobCodeId),
                [bs_PayrollDetail.Fields.bs_JobGroupFK] = new EntityReference("bs_jobgroup", jobGroupId),
                [bs_PayrollDetail.Fields.bs_RegularHours] = 10m
            };

            var jobCode = new Entity("bs_jobcode")
            {
                Id = jobCodeId,
                ["bs_jobcode"] = "4321",
                ["bs_jobtitle"] = "Valet Attendant",
                ["bs_jobgroupfk"] = new EntityReference("bs_jobgroup", jobGroupId)
            };

            var jobGroup = new Entity("bs_jobgroup")
            {
                Id = jobGroupId,
                ["bs_jobgrouptitle"] = "Valet"
            };

            // Add the job code assignment to site
            var jobCodeBySite = new Entity("bs_jobcodesbysite")
            {
                Id = Guid.NewGuid(),
                ["bs_customersite"] = new EntityReference("bs_customersite", siteId),
                ["bs_jobcode"] = new EntityReference("bs_jobcode", jobCodeId)
            };

            var fakeOrgService = new FakeBatchOrganizationService
            {
                PayrollEntities = new List<Entity> { payrollEntity },
                PayrollDetails = new List<Entity> { payrollDetail },
                JobCodes = new List<Entity> { jobCode },
                JobGroups = new List<Entity> { jobGroup },
                JobCodesBySite = new List<Entity> { jobCodeBySite }
            };

            var fakeDataverse = new FakeDataverseService { ServiceClient = fakeOrgService };
            var repo = new PayrollRepository(fakeDataverse);

            // Act
            var result = await repo.GetPayrollBatchAsync(new List<Guid> { siteId }, billingPeriod);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.True(result.ContainsKey(siteId));
            
            var payroll = result[siteId];
            Assert.NotNull(payroll);
            Assert.NotNull(payroll.bs_PayrollDetail_Payroll);
            Assert.Single(payroll.bs_PayrollDetail_Payroll);

            var detail = System.Linq.Enumerable.First(payroll.bs_PayrollDetail_Payroll);
            Assert.True(detail.Contains("jobcode_display"));
            Assert.True(detail.Contains("jobcode_displayname"));
            Assert.Equal("4321", detail["jobcode_display"]);
            Assert.Equal("Valet Attendant", detail["jobcode_displayname"]);
            Assert.Equal("Valet", detail["jobgroup_title"]);
        }

        [Fact]
        public async Task GetPayrollBatchAsync_WithNoPayrollDetails_ShouldReturnEmptyDictionary()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var billingPeriod = "2025-06";

            var fakeOrgService = new FakeBatchOrganizationService
            {
                PayrollEntities = new List<Entity>(), // No payroll entities
                PayrollDetails = new List<Entity>(),  // No payroll details
                JobCodes = new List<Entity>(),
                JobGroups = new List<Entity>(),
                JobCodesBySite = new List<Entity>() // No job codes assigned to site
            };

            var fakeDataverse = new FakeDataverseService { ServiceClient = fakeOrgService };
            var repo = new PayrollRepository(fakeDataverse);

            // Act
            var result = await repo.GetPayrollBatchAsync(new List<Guid> { siteId }, billingPeriod);

            // Assert
            Assert.NotNull(result);
            // When no payroll is found for a site, it should not be in the dictionary
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetPayrollBatchAsync_WithMultipleSites_ShouldReturnDictionaryOfPayrolls()
        {
            // Arrange
            var siteId1 = Guid.NewGuid();
            var siteId2 = Guid.NewGuid();
            var siteId3 = Guid.NewGuid();
            var billingPeriod = "2025-06";
            
            var jobCodeId1 = Guid.NewGuid();
            var jobCodeId2 = Guid.NewGuid();
            var jobGroupId = Guid.NewGuid();

            // Create payroll entities for each site
            var payrollEntities = new List<Entity>
            {
                new Entity(bs_Payroll.EntityLogicalName)
                {
                    Id = Guid.NewGuid(),
                    [bs_Payroll.Fields.bs_CustomerSiteFK] = new EntityReference("bs_customersite", siteId1),
                    [bs_Payroll.Fields.bs_Period] = billingPeriod,
                    [bs_Payroll.Fields.bs_Name] = "Test Payroll 1"
                },
                new Entity(bs_Payroll.EntityLogicalName)
                {
                    Id = Guid.NewGuid(),
                    [bs_Payroll.Fields.bs_CustomerSiteFK] = new EntityReference("bs_customersite", siteId2),
                    [bs_Payroll.Fields.bs_Period] = billingPeriod,
                    [bs_Payroll.Fields.bs_Name] = "Test Payroll 2"
                }
                // Note: siteId3 has no payroll data
            };

            // Create payroll details
            var payrollDetails = new List<Entity>
            {
                new Entity(bs_PayrollDetail.EntityLogicalName)
                {
                    Id = Guid.NewGuid(),
                    [bs_PayrollDetail.Fields.bs_PayrollFK] = new EntityReference(bs_Payroll.EntityLogicalName, payrollEntities[0].Id),
                    [bs_PayrollDetail.Fields.bs_JobCodeFK] = new EntityReference("bs_jobcode", jobCodeId1),
                    [bs_PayrollDetail.Fields.bs_RegularHours] = 10m
                },
                new Entity(bs_PayrollDetail.EntityLogicalName)
                {
                    Id = Guid.NewGuid(),
                    [bs_PayrollDetail.Fields.bs_PayrollFK] = new EntityReference(bs_Payroll.EntityLogicalName, payrollEntities[1].Id),
                    [bs_PayrollDetail.Fields.bs_JobCodeFK] = new EntityReference("bs_jobcode", jobCodeId2),
                    [bs_PayrollDetail.Fields.bs_RegularHours] = 20m
                }
            };

            // Create job codes
            var jobCodes = new List<Entity>
            {
                new Entity("bs_jobcode")
                {
                    Id = jobCodeId1,
                    ["bs_jobcode"] = "VALET",
                    ["bs_jobtitle"] = "Valet Attendant",
                    ["bs_jobgroupfk"] = new EntityReference("bs_jobgroup", jobGroupId)
                },
                new Entity("bs_jobcode")
                {
                    Id = jobCodeId2,
                    ["bs_jobcode"] = "CASH",
                    ["bs_jobtitle"] = "Cashier",
                    ["bs_jobgroupfk"] = new EntityReference("bs_jobgroup", jobGroupId)
                }
            };

            // Create job group
            var jobGroups = new List<Entity>
            {
                new Entity("bs_jobgroup")
                {
                    Id = jobGroupId,
                    ["bs_jobgrouptitle"] = "Parking Services"
                }
            };

            // Create job code assignments
            var jobCodesBySite = new List<Entity>
            {
                new Entity("bs_jobcodesbysite")
                {
                    Id = Guid.NewGuid(),
                    ["bs_customersite"] = new EntityReference("bs_customersite", siteId1),
                    ["bs_jobcode"] = new EntityReference("bs_jobcode", jobCodeId1)
                },
                new Entity("bs_jobcodesbysite")
                {
                    Id = Guid.NewGuid(),
                    ["bs_customersite"] = new EntityReference("bs_customersite", siteId2),
                    ["bs_jobcode"] = new EntityReference("bs_jobcode", jobCodeId2)
                }
            };

            var fakeOrgService = new FakeBatchOrganizationService
            {
                PayrollEntities = payrollEntities,
                PayrollDetails = payrollDetails,
                JobCodes = jobCodes,
                JobGroups = jobGroups,
                JobCodesBySite = jobCodesBySite
            };

            var fakeDataverse = new FakeDataverseService { ServiceClient = fakeOrgService };
            var repo = new PayrollRepository(fakeDataverse);

            // Act
            var result = await repo.GetPayrollBatchAsync(new List<Guid> { siteId1, siteId2, siteId3 }, billingPeriod);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count); // Only 2 sites have payroll data
            
            // Verify site 1
            Assert.True(result.ContainsKey(siteId1));
            Assert.NotNull(result[siteId1]);
            Assert.NotNull(result[siteId1].bs_PayrollDetail_Payroll);
            Assert.Single(result[siteId1].bs_PayrollDetail_Payroll);
            var detail1 = result[siteId1].bs_PayrollDetail_Payroll.First();
            Assert.Equal("VALET", detail1["jobcode_display"]);
            Assert.Equal("Valet Attendant", detail1["jobcode_displayname"]);
            Assert.Equal("Parking Services", detail1["jobgroup_title"]);
            
            // Verify site 2
            Assert.True(result.ContainsKey(siteId2));
            Assert.NotNull(result[siteId2]);
            Assert.NotNull(result[siteId2].bs_PayrollDetail_Payroll);
            Assert.Single(result[siteId2].bs_PayrollDetail_Payroll);
            var detail2 = result[siteId2].bs_PayrollDetail_Payroll.First();
            Assert.Equal("CASH", detail2["jobcode_display"]);
            Assert.Equal("Cashier", detail2["jobcode_displayname"]);
            Assert.Equal("Parking Services", detail2["jobgroup_title"]);
            
            // Verify site 3 is not in results
            Assert.False(result.ContainsKey(siteId3));
        }

        [Fact]
        public async Task GetPayrollBatchAsync_WithEmptySiteList_ShouldReturnEmptyDictionary()
        {
            // Arrange
            var fakeOrgService = new FakeOrganizationService();
            var fakeDataverse = new FakeDataverseService { ServiceClient = fakeOrgService };
            var repo = new PayrollRepository(fakeDataverse);

            // Act
            var result = await repo.GetPayrollBatchAsync(new List<Guid>(), "2025-06");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetPayrollBatchAsync_WithNullSiteList_ShouldReturnEmptyDictionary()
        {
            // Arrange
            var fakeOrgService = new FakeOrganizationService();
            var fakeDataverse = new FakeDataverseService { ServiceClient = fakeOrgService };
            var repo = new PayrollRepository(fakeDataverse);

            // Act
            var result = await repo.GetPayrollBatchAsync(null, "2025-06");

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        // --- Helpers for EDW mocking ---
        private PayrollRepository CreatePayrollRepositoryWithMockedHttp(string responseContent, System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK)
        {
            var handler = new MockHttpMessageHandler(responseContent, statusCode);
            var httpClient = new HttpClient(handler);

            // Patch HttpClient in PayrollRepository via reflection (since it's internal)
            // Instead, use a testable subclass if needed, or patch System.Net.Http.HttpClient.DefaultProxy

            // Use real dataverse service, but it won't be called
            var fakeDataverse = new FakeDataverseService();
            return new PayrollRepository(fakeDataverse);
        }

        private class MockHttpMessageHandler : System.Net.Http.HttpMessageHandler
        {
            private readonly string _responseContent;
            private readonly System.Net.HttpStatusCode _statusCode;
            public MockHttpMessageHandler(string responseContent, System.Net.HttpStatusCode statusCode)
            {
                _responseContent = responseContent;
                _statusCode = statusCode;
            }
            protected override System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
            {
                var response = new System.Net.Http.HttpResponseMessage(_statusCode)
                {
                    Content = new System.Net.Http.StringContent(_responseContent)
                };
                return System.Threading.Tasks.Task.FromResult(response);
            }
        }
    }
}
