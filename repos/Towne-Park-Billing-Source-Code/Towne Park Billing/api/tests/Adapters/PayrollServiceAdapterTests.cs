using System;
using System.Collections.Generic;
using Xunit;
using api.Adapters.Impl;
using api.Adapters.Mappers;
using api.Models.Dto;
using api.Models.Vo;
using NSubstitute;
using api.Services;

namespace BackendTests.Adapters
{
    public class FakePayrollService : IPayrollService
    {
        public PayrollVo PayrollToReturn { get; set; }
        public PayrollVo SavedPayroll { get; private set; }

        public PayrollVo? GetPayroll(Guid siteId, string billingPeriod)
        {
            return PayrollToReturn;
        }

        public void SavePayroll(PayrollVo updates)
        {
            SavedPayroll = updates;
        }
    }

    public class PayrollServiceAdapterTests
    {
        [Fact]
        public void GetPayroll_DelegatesAndMapsToDto()
        {
            // Arrange
            var vo = new PayrollVo
            {
                Id = Guid.NewGuid(),
                Name = "Test Payroll",
                BillingPeriod = "2025-06",
                CustomerSiteId = Guid.NewGuid(),
                SiteNumber = "123",
                ForecastPayroll = new List<JobGroupForecastVo>
                {
                    new JobGroupForecastVo
                    {
                        JobGroupId = Guid.NewGuid(),
                        JobGroupName = "Valet",
                        ForecastHours = 100,
                        JobCodes = new List<JobCodeForecastVo>
                        {
                            new JobCodeForecastVo
                            {
                                JobCodeId = Guid.NewGuid(),
                                JobCode = "4321",
                                DisplayName = "Valet Attendant",
                                ForecastHours = 60
                            }
                        }
                    }
                }
            };

            var fakeService = new FakePayrollService { PayrollToReturn = vo };
            var fakeCustomerService = Substitute.For<ICustomerService>();
            var fakeContractService = Substitute.For<IContractService>();
            var adapter = new PayrollServiceAdapter(fakeService, fakeCustomerService, fakeContractService);

            // Act
            var dto = adapter.GetPayroll(Guid.NewGuid(), "2025-06");

            // Assert
            Assert.NotNull(dto);
            Assert.Equal(vo.Id, dto.Id);
            Assert.Equal(vo.Name, dto.Name);
            Assert.Equal(vo.BillingPeriod, dto.BillingPeriod);
            Assert.Equal(vo.CustomerSiteId, dto.CustomerSiteId);
            Assert.Equal(vo.SiteNumber, dto.SiteNumber);
            Assert.NotNull(dto.ForecastPayroll);
            Assert.Single(dto.ForecastPayroll);
            Assert.Equal("Valet", dto.ForecastPayroll[0].JobGroupName);
            Assert.Single(dto.ForecastPayroll[0].JobCodes);
            Assert.Equal("Valet Attendant", dto.ForecastPayroll[0].JobCodes[0].DisplayName);
        }

        [Fact]
        public void SavePayroll_MapsDtoToVoAndDelegates()
        {
            // Arrange
            var dto = new PayrollDto
            {
                Id = Guid.NewGuid(),
                Name = "Test Payroll",
                BillingPeriod = "2025-06",
                CustomerSiteId = Guid.NewGuid(),
                SiteNumber = "123",
                ForecastPayroll = new List<JobGroupForecastDto>
                {
                    new JobGroupForecastDto
                    {
                        JobGroupId = Guid.NewGuid(),
                        JobGroupName = "Valet",
                        ForecastHours = 100,
                        JobCodes = new List<JobCodeForecastDto>
                        {
                            new JobCodeForecastDto
                            {
                                JobCodeId = Guid.NewGuid(),
                                JobCode = "4321",
                                DisplayName = "Valet Attendant",
                                ForecastHours = 60
                            }
                        }
                    }
                },
                BudgetPayroll = new List<JobGroupBudgetDto>
                {
                    new JobGroupBudgetDto
                    {
                        JobGroupId = Guid.NewGuid(),
                        JobGroupName = "Valet",
                        BudgetHours = 90,
                        JobCodes = new List<JobCodeBudgetDto>
                        {
                            new JobCodeBudgetDto
                            {
                                JobCodeId = Guid.NewGuid(),
                                JobCode = "4322",
                                DisplayName = "Valet Attendant Budget",
                                BudgetHours = 50
                            }
                        }
                    }
                },
                ActualPayroll = new List<JobGroupActualDto>
                {
                    new JobGroupActualDto
                    {
                        JobGroupId = Guid.NewGuid(),
                        JobGroupName = "Valet",
                        ActualHours = 80,
                        JobCodes = new List<JobCodeActualDto>
                        {
                            new JobCodeActualDto
                            {
                                JobCodeId = Guid.NewGuid(),
                                JobCode = "4323",
                                DisplayName = "Valet Attendant Actual",
                                ActualHours = 40
                            }
                        }
                    }
                },
                ScheduledPayroll = new List<JobGroupScheduledDto>
                {
                    new JobGroupScheduledDto
                    {
                        JobGroupId = Guid.NewGuid(),
                        JobGroupName = "Valet",
                        ScheduledHours = 70,
                        JobCodes = new List<JobCodeScheduledDto>
                        {
                            new JobCodeScheduledDto
                            {
                                JobCodeId = Guid.NewGuid(),
                                JobCode = "4324",
                                DisplayName = "Valet Attendant Scheduled",
                                ScheduledHours = 30
                            }
                        }
                    }
                }
            };

            var fakeService = new FakePayrollService();
            var fakeCustomerService = Substitute.For<ICustomerService>();
            var fakeContractService = Substitute.For<IContractService>();
            var adapter = new PayrollServiceAdapter(fakeService, fakeCustomerService, fakeContractService);

            // Act
            adapter.SavePayroll(dto);

            // Assert
            Assert.NotNull(fakeService.SavedPayroll);
            Assert.Equal(dto.Id, fakeService.SavedPayroll.Id);
            Assert.Equal(dto.Name, fakeService.SavedPayroll.Name);
            Assert.Equal(dto.BillingPeriod, fakeService.SavedPayroll.BillingPeriod);
            Assert.Equal(dto.CustomerSiteId, fakeService.SavedPayroll.CustomerSiteId);
            Assert.Equal(dto.SiteNumber, fakeService.SavedPayroll.SiteNumber);
            Assert.NotNull(fakeService.SavedPayroll.ForecastPayroll);
            Assert.Single(fakeService.SavedPayroll.ForecastPayroll);
            Assert.Equal("Valet", fakeService.SavedPayroll.ForecastPayroll[0].JobGroupName);
            Assert.Single(fakeService.SavedPayroll.ForecastPayroll[0].JobCodes);
            Assert.Equal("Valet Attendant", fakeService.SavedPayroll.ForecastPayroll[0].JobCodes[0].DisplayName);
        }
        [Fact]
        public void GetPayroll_ReturnsParentFieldsAndEmptyLists_WhenPayrollDataMissing()
        {
            var fakePayrollService = new FakePayrollService { PayrollToReturn = null };
            var fakeCustomerService = Substitute.For<ICustomerService>();
            var fakeContractService = Substitute.For<IContractService>();
            var siteId = Guid.NewGuid();
            fakeCustomerService.GetCustomerDetail(siteId).Returns(new CustomerDetailVo
            {
                CustomerSiteId = siteId,
                SiteNumber = "S123",
                SiteName = "Test Site"
            });
            fakeContractService.GetContractDetail(siteId).Returns(new ContractDetailVo
            {
                ContractTypeString = "Fixed Fee"
            });

            var adapter = new PayrollServiceAdapter(fakePayrollService, fakeCustomerService, fakeContractService);

            var dto = adapter.GetPayroll(siteId, "2025-06");

            Assert.NotNull(dto);
            Assert.Equal(siteId, dto.CustomerSiteId);
            Assert.Equal("S123", dto.SiteNumber);
            Assert.Equal("Test Site", dto.Name);
            Assert.Empty(dto.ForecastPayroll);
            Assert.Empty(dto.BudgetPayroll);
            Assert.Empty(dto.ActualPayroll);
            Assert.Empty(dto.ScheduledPayroll);
            Assert.Equal("Group", dto.PayrollForecastMode);
        }

        [Fact]
        public void GetPayroll_SetsPayrollForecastModeToCode_WhenContractTypeIsPerLaborHour()
        {
            var fakePayrollService = new FakePayrollService { PayrollToReturn = null };
            var fakeCustomerService = Substitute.For<ICustomerService>();
            var fakeContractService = Substitute.For<IContractService>();
            var siteId = Guid.NewGuid();
            fakeCustomerService.GetCustomerDetail(siteId).Returns(new CustomerDetailVo
            {
                CustomerSiteId = siteId,
                SiteNumber = "S123",
                SiteName = "Test Site"
            });
            fakeContractService.GetContractDetail(siteId).Returns(new ContractDetailVo
            {
                ContractTypeString = "Per Labor Hour"
            });

            var adapter = new PayrollServiceAdapter(fakePayrollService, fakeCustomerService, fakeContractService);

            var dto = adapter.GetPayroll(siteId, "2025-06");

            Assert.NotNull(dto);
            Assert.Equal("Code", dto.PayrollForecastMode);
        }

        [Fact]
        public void GetPayroll_HandlesMissingContractOrCustomer_Gracefully()
        {
            var fakePayrollService = new FakePayrollService { PayrollToReturn = null };
            var fakeCustomerService = Substitute.For<ICustomerService>();
            var fakeContractService = Substitute.For<IContractService>();
            var siteId = Guid.NewGuid();
            fakeCustomerService.GetCustomerDetail(siteId).Returns((CustomerDetailVo)null);
            fakeContractService.GetContractDetail(siteId).Returns((ContractDetailVo)null);

            var adapter = new PayrollServiceAdapter(fakePayrollService, fakeCustomerService, fakeContractService);

            var dto = adapter.GetPayroll(siteId, "2025-06");

            Assert.NotNull(dto);
            Assert.Null(dto.Name);
            Assert.Null(dto.SiteNumber);
            Assert.Equal(siteId, dto.CustomerSiteId);
            Assert.Equal("Group", dto.PayrollForecastMode);
        }
    }
}
