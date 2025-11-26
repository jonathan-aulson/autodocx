using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using api.Adapters.Mappers;
using api.Models.Dto;
using api.Models.Vo;
using TownePark;
using Microsoft.Xrm.Sdk;
using TownePark;
using Microsoft.Xrm.Sdk;

namespace BackendTests.Adapters
{
    public class PayrollMapperTests
    {
        [Fact]
        public void JobGroupForecastVoToDto_MapsAllFields()
        {
            var vo = new JobGroupForecastVo
            {
                Id = Guid.NewGuid(),
                JobGroupId = Guid.NewGuid(),
                JobGroupName = "Valet",
                ForecastHours = 100,
                Date = new DateOnly(2025, 6, 1),
                ForecastPayrollCost = 1500.00m,
                ForecastPayrollRevenue = 2000.00m,
                JobCodes = new List<JobCodeForecastVo>
                {
                    new JobCodeForecastVo
                    {
                        Id = Guid.NewGuid(),
                        JobCodeId = Guid.NewGuid(),
                        JobCode = "4321",
                        DisplayName = "Valet Attendant",
                        ForecastHours = 60,
                        Date = new DateOnly(2025, 6, 1),
                        ForecastPayrollCost = 900.00m,
                        ForecastPayrollRevenue = 1200.00m
                    }
                }
            };

            var dto = PayrollMapper.JobGroupForecastVoToDto(vo);

            Assert.Equal(vo.Id, dto.Id);
            Assert.Equal(vo.JobGroupId, dto.JobGroupId);
            Assert.Equal(vo.JobGroupName, dto.JobGroupName);
            Assert.Equal(vo.ForecastHours, dto.ForecastHours);
            Assert.Equal(vo.Date, dto.Date);
            Assert.Equal(vo.ForecastPayrollCost, dto.ForecastPayrollCost);
            Assert.Equal(vo.ForecastPayrollRevenue, dto.ForecastPayrollRevenue);
            Assert.NotNull(dto.JobCodes);
            Assert.Single(dto.JobCodes);
            
            var jobCodeDto = dto.JobCodes[0];
            var jobCodeVo = vo.JobCodes[0];
            Assert.Equal(jobCodeVo.Id, jobCodeDto.Id);
            Assert.Equal(jobCodeVo.JobCodeId, jobCodeDto.JobCodeId);
            Assert.Equal(jobCodeVo.JobCode, jobCodeDto.JobCode);
            Assert.Equal(jobCodeVo.DisplayName, jobCodeDto.DisplayName);
            Assert.Equal(jobCodeVo.ForecastHours, jobCodeDto.ForecastHours);
            Assert.Equal(jobCodeVo.Date, jobCodeDto.Date);
            Assert.Equal(jobCodeVo.ForecastPayrollCost, jobCodeDto.ForecastPayrollCost);
            Assert.Equal(jobCodeVo.ForecastPayrollRevenue, jobCodeDto.ForecastPayrollRevenue);
        }

        [Fact]
        public void JobGroupForecastDtoToVo_MapsAllFields()
        {
            var dto = new JobGroupForecastDto
            {
                Id = Guid.NewGuid(),
                JobGroupId = Guid.NewGuid(),
                JobGroupName = "Valet",
                ForecastHours = 100,
                Date = new DateOnly(2025, 6, 1),
                ForecastPayrollCost = 1500.00m,
                ForecastPayrollRevenue = 2000.00m,
                JobCodes = new List<JobCodeForecastDto>
                {
                    new JobCodeForecastDto
                    {
                        Id = Guid.NewGuid(),
                        JobCodeId = Guid.NewGuid(),
                        JobCode = "4321",
                        DisplayName = "Valet Attendant",
                        ForecastHours = 60,
                        Date = new DateOnly(2025, 6, 1),
                        ForecastPayrollCost = 900.00m,
                        ForecastPayrollRevenue = 1200.00m
                    }
                }
            };

            var vo = PayrollMapper.JobGroupForecastDtoToVo(dto);

            Assert.Equal(dto.Id, vo.Id);
            Assert.Equal(dto.JobGroupId, vo.JobGroupId);
            Assert.Equal(dto.JobGroupName, vo.JobGroupName);
            Assert.Equal(dto.ForecastHours, vo.ForecastHours);
            Assert.Equal(dto.Date, vo.Date);
            Assert.Equal(dto.ForecastPayrollCost, vo.ForecastPayrollCost);
            Assert.Equal(dto.ForecastPayrollRevenue, vo.ForecastPayrollRevenue);
            Assert.NotNull(vo.JobCodes);
            Assert.Single(vo.JobCodes);
            
            var jobCodeVo = vo.JobCodes[0];
            var jobCodeDto = dto.JobCodes[0];
            Assert.Equal(jobCodeDto.Id, jobCodeVo.Id);
            Assert.Equal(jobCodeDto.JobCodeId, jobCodeVo.JobCodeId);
            Assert.Equal(jobCodeDto.JobCode, jobCodeVo.JobCode);
            Assert.Equal(jobCodeDto.DisplayName, jobCodeVo.DisplayName);
            Assert.Equal(jobCodeDto.ForecastHours, jobCodeVo.ForecastHours);
            Assert.Equal(jobCodeDto.Date, jobCodeVo.Date);
            Assert.Equal(jobCodeDto.ForecastPayrollCost, jobCodeVo.ForecastPayrollCost);
            Assert.Equal(jobCodeDto.ForecastPayrollRevenue, jobCodeVo.ForecastPayrollRevenue);
        }

        [Fact]
        public void PayrollVoToDto_MapsNestedCollections()
        {
            var vo = new PayrollVo
            {
                Id = Guid.NewGuid(),
                Name = "Test Payroll",
                BillingPeriod = "2025-06",
                CustomerSiteId = Guid.NewGuid(),
                SiteNumber = "123",
                PayrollForecastMode = PayrollForecastModeType.Code,
                ForecastPayroll = new List<JobGroupForecastVo>
                {
                    new JobGroupForecastVo
                    {
                        Id = null, // Group aggregates don't have IDs
                        JobGroupId = Guid.NewGuid(),
                        JobGroupName = "Valet",
                        ForecastHours = 100,
                        Date = new DateOnly(2025, 6, 1),
                        ForecastPayrollCost = 1500.00m,
                        ForecastPayrollRevenue = 2000.00m,
                        JobCodes = new List<JobCodeForecastVo>
                        {
                            new JobCodeForecastVo
                            {
                                Id = Guid.NewGuid(),
                                JobCodeId = Guid.NewGuid(),
                                JobCode = "4321",
                                DisplayName = "Valet Attendant",
                                ForecastHours = 60,
                                Date = new DateOnly(2025, 6, 1),
                                ForecastPayrollCost = 900.00m,
                                ForecastPayrollRevenue = 1200.00m
                            }
                        }
                    }
                }
            };

            var dto = PayrollMapper.PayrollVoToDto(vo);

            Assert.Equal(vo.Id, dto.Id);
            Assert.Equal(vo.Name, dto.Name);
            Assert.Equal(vo.BillingPeriod, dto.BillingPeriod);
            Assert.Equal(vo.CustomerSiteId, dto.CustomerSiteId);
            Assert.Equal(vo.SiteNumber, dto.SiteNumber);
            Assert.Equal("Code", dto.PayrollForecastMode); // Enum to string conversion
            Assert.NotNull(dto.ForecastPayroll);
            Assert.Single(dto.ForecastPayroll);
            
            var groupDto = dto.ForecastPayroll[0];
            Assert.Equal("Valet", groupDto.JobGroupName);
            Assert.Equal(100, groupDto.ForecastHours);
            Assert.Equal(1500.00m, groupDto.ForecastPayrollCost);
            Assert.Equal(2000.00m, groupDto.ForecastPayrollRevenue);
            Assert.Single(groupDto.JobCodes);
            Assert.Equal("Valet Attendant", groupDto.JobCodes[0].DisplayName);
            Assert.Equal(900.00m, groupDto.JobCodes[0].ForecastPayrollCost);
            Assert.Equal(1200.00m, groupDto.JobCodes[0].ForecastPayrollRevenue);
        }

        [Fact]
        public void PayrollDtoToVo_MapsNestedCollections()
        {
            var dto = new PayrollDto
            {
                Id = Guid.NewGuid(),
                Name = "Test Payroll",
                BillingPeriod = "2025-06",
                CustomerSiteId = Guid.NewGuid(),
                SiteNumber = "123",
                PayrollForecastMode = "Group", // String to enum conversion
                ForecastPayroll = new List<JobGroupForecastDto>
                {
                    new JobGroupForecastDto
                    {
                        Id = null,
                        JobGroupId = Guid.NewGuid(),
                        JobGroupName = "Valet",
                        ForecastHours = 100,
                        Date = new DateOnly(2025, 6, 1),
                        ForecastPayrollCost = 1500.00m,
                        ForecastPayrollRevenue = 2000.00m,
                        JobCodes = new List<JobCodeForecastDto>
                        {
                            new JobCodeForecastDto
                            {
                                Id = Guid.NewGuid(),
                                JobCodeId = Guid.NewGuid(),
                                JobCode = "4321",
                                DisplayName = "Valet Attendant",
                                ForecastHours = 60,
                                Date = new DateOnly(2025, 6, 1),
                                ForecastPayrollCost = 900.00m,
                                ForecastPayrollRevenue = 1200.00m
                            }
                        }
                    }
                }
            };

            var vo = PayrollMapper.PayrollDtoToVo(dto);

            Assert.Equal(dto.Id, vo.Id);
            Assert.Equal(dto.Name, vo.Name);
            Assert.Equal(dto.BillingPeriod, vo.BillingPeriod);
            Assert.Equal(dto.CustomerSiteId, vo.CustomerSiteId);
            Assert.Equal(dto.SiteNumber, vo.SiteNumber);
            Assert.Equal(PayrollForecastModeType.Group, vo.PayrollForecastMode); // String to enum conversion
            Assert.NotNull(vo.ForecastPayroll);
            Assert.Single(vo.ForecastPayroll);
            
            var groupVo = vo.ForecastPayroll[0];
            Assert.Equal("Valet", groupVo.JobGroupName);
            Assert.Equal(100, groupVo.ForecastHours);
            Assert.Equal(1500.00m, groupVo.ForecastPayrollCost);
            Assert.Equal(2000.00m, groupVo.ForecastPayrollRevenue);
            Assert.Single(groupVo.JobCodes);
            Assert.Equal("Valet Attendant", groupVo.JobCodes[0].DisplayName);
            Assert.Equal(900.00m, groupVo.JobCodes[0].ForecastPayrollCost);
            Assert.Equal(1200.00m, groupVo.JobCodes[0].ForecastPayrollRevenue);
        }

        [Fact]
        public void PayrollVoToModel_FlattensNestedStructureToDetails()
        {
            // Arrange
            var payrollId = Guid.NewGuid();
            var siteId = Guid.NewGuid();
            var jobGroupId = Guid.NewGuid();
            var jobCodeId1 = Guid.NewGuid();
            var jobCodeId2 = Guid.NewGuid();
            var detailId1 = Guid.NewGuid();
            var detailId2 = Guid.NewGuid();

            var vo = new PayrollVo
            {
                Id = payrollId,
                Name = "Test Payroll",
                BillingPeriod = "2025-06",
                CustomerSiteId = siteId,
                PayrollForecastMode = PayrollForecastModeType.Code,
                ForecastPayroll = new List<JobGroupForecastVo>
                {
                    new JobGroupForecastVo
                    {
                        Id = null, // Group aggregates don't have IDs
                        JobGroupId = jobGroupId,
                        JobGroupName = "Valet",
                        ForecastHours = 100,
                        Date = new DateOnly(2025, 6, 1),
                        ForecastPayrollCost = 1500.00m,
                        ForecastPayrollRevenue = 2000.00m,
                        JobCodes = new List<JobCodeForecastVo>
                        {
                            new JobCodeForecastVo
                            {
                                Id = detailId1, // Preserve existing ID
                                JobCodeId = jobCodeId1,
                                JobCode = "4321",
                                DisplayName = "Valet Lead",
                                ForecastHours = 60,
                                Date = new DateOnly(2025, 6, 1),
                                ForecastPayrollCost = 900.00m,
                                ForecastPayrollRevenue = 1200.00m
                            },
                            new JobCodeForecastVo
                            {
                                Id = detailId2, // Preserve existing ID
                                JobCodeId = jobCodeId2,
                                JobCode = "4322",
                                DisplayName = "Valet Attendant",
                                ForecastHours = 40,
                                Date = new DateOnly(2025, 6, 1),
                                ForecastPayrollCost = 600.00m,
                                ForecastPayrollRevenue = 800.00m
                            }
                        }
                    }
                }
            };

            // Act
            var model = PayrollMapper.PayrollVoToModel(vo);

            // Assert
            Assert.Equal(payrollId, model.Id);
            Assert.Equal("Test Payroll", model.bs_Name);
            Assert.Equal("2025-06", model.bs_Period);
            Assert.Equal(siteId, model.bs_CustomerSiteFK.Id);
            Assert.Equal(bs_payrollforecastmodetype.Code, model.bs_PayrollForecastMode);

            Assert.NotNull(model.bs_PayrollDetail_Payroll);
            Assert.Equal(2, model.bs_PayrollDetail_Payroll.Count()); // 2 job codes flattened to 2 details

            var detail1 = model.bs_PayrollDetail_Payroll.First(d => d.Id == detailId1);
            var detail2 = model.bs_PayrollDetail_Payroll.First(d => d.Id == detailId2);

            // Verify Detail 1
            Assert.Equal(detailId1, detail1.Id);
            Assert.Equal(jobGroupId, detail1.bs_JobGroupFK.Id);
            Assert.Equal(jobCodeId1, detail1.bs_JobCodeFK.Id);
            Assert.Equal(60, detail1.bs_RegularHours);
            Assert.Equal("Valet Lead", detail1.bs_DisplayName);
            Assert.Equal(new DateTime(2025, 6, 1), detail1.bs_Date);
            Assert.Equal(900.00m, detail1.bs_ForecastPayrollCost);
            Assert.Equal(1200.00m, detail1.bs_ForecastPayrollRevenue);

            // Verify Detail 2
            Assert.Equal(detailId2, detail2.Id);
            Assert.Equal(jobGroupId, detail2.bs_JobGroupFK.Id);
            Assert.Equal(jobCodeId2, detail2.bs_JobCodeFK.Id);
            Assert.Equal(40, detail2.bs_RegularHours);
            Assert.Equal("Valet Attendant", detail2.bs_DisplayName);
            Assert.Equal(new DateTime(2025, 6, 1), detail2.bs_Date);
            Assert.Equal(600.00m, detail2.bs_ForecastPayrollCost);
            Assert.Equal(800.00m, detail2.bs_ForecastPayrollRevenue);
        }

        [Fact]
        public void PayrollVoToModel_DoesNotSetIdsForNewRecords()
        {
            // Arrange
            var vo = new PayrollVo
            {
                Id = Guid.Empty, // New record
                CustomerSiteId = Guid.NewGuid(),
                BillingPeriod = "2025-06",
                PayrollForecastMode = PayrollForecastModeType.Group,
                ForecastPayroll = new List<JobGroupForecastVo>
                {
                    new JobGroupForecastVo
                    {
                        JobGroupId = Guid.NewGuid(),
                        JobCodes = new List<JobCodeForecastVo>
                        {
                            new JobCodeForecastVo
                            {
                                Id = null, // New record - should not set ID
                                JobCodeId = Guid.NewGuid(),
                                DisplayName = "New Job Code",
                                ForecastHours = 10
                            }
                        }
                    }
                }
            };

            // Act
            var model = PayrollMapper.PayrollVoToModel(vo);

            // Assert
            // For new records, ID should remain Guid.Empty so Dataverse can generate it
            Assert.Equal(Guid.Empty, model.Id);
            // We don't set bs_PayrollId at all for new records, so it remains in default state
            // (This could be null or Guid.Empty depending on entity initialization)
            
            Assert.NotNull(model.bs_PayrollDetail_Payroll);
            Assert.Single(model.bs_PayrollDetail_Payroll);
            
            var detail = model.bs_PayrollDetail_Payroll.First();
            Assert.Equal(Guid.Empty, detail.Id); // Should not have set an ID for new detail
        }

        [Fact]
        public void PayrollVoToModel_SkipsGroupsWithoutJobCodes()
        {
            // Arrange
            var vo = new PayrollVo
            {
                Id = Guid.NewGuid(),
                CustomerSiteId = Guid.NewGuid(),
                BillingPeriod = "2025-06",
                PayrollForecastMode = PayrollForecastModeType.Group,
                ForecastPayroll = new List<JobGroupForecastVo>
                {
                    new JobGroupForecastVo
                    {
                        JobGroupId = Guid.NewGuid(),
                        JobGroupName = "Empty Group",
                        JobCodes = null // No job codes
                    },
                    new JobGroupForecastVo
                    {
                        JobGroupId = Guid.NewGuid(),
                        JobGroupName = "Empty Group 2",
                        JobCodes = new List<JobCodeForecastVo>() // Empty list
                    },
                    new JobGroupForecastVo
                    {
                        JobGroupId = Guid.NewGuid(),
                        JobGroupName = "Valid Group",
                        JobCodes = new List<JobCodeForecastVo>
                        {
                            new JobCodeForecastVo
                            {
                                JobCodeId = Guid.NewGuid(),
                                DisplayName = "Valid Job Code",
                                ForecastHours = 10
                            }
                        }
                    }
                }
            };

            // Act
            var model = PayrollMapper.PayrollVoToModel(vo);

            // Assert
            Assert.NotNull(model.bs_PayrollDetail_Payroll);
            Assert.Single(model.bs_PayrollDetail_Payroll); // Only the group with job codes should create details
            Assert.Equal("Valid Job Code", model.bs_PayrollDetail_Payroll.First().bs_DisplayName);
        }

        [Fact]
        public void PayrollVoToDto_HandlesInvalidForecastModeString()
        {
            // Arrange
            var dto = new PayrollDto
            {
                Id = Guid.NewGuid(),
                PayrollForecastMode = "InvalidMode" // Invalid enum value
            };

            // Act
            var vo = PayrollMapper.PayrollDtoToVo(dto);

            // Assert
            Assert.Equal(PayrollForecastModeType.Group, vo.PayrollForecastMode); // Should fallback to default
        }

        [Fact]
        public void PayrollVoToDto_HandlesNullForecastModeString()
        {
            // Arrange
            var dto = new PayrollDto
            {
                Id = Guid.NewGuid(),
                PayrollForecastMode = null // Null value
            };

            // Act
            var vo = PayrollMapper.PayrollDtoToVo(dto);

            // Assert
            Assert.Equal(PayrollForecastModeType.Group, vo.PayrollForecastMode); // Should fallback to default
        }
    }
}
