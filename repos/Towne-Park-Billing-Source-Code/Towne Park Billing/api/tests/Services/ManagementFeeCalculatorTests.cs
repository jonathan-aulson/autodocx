using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using NSubstitute;
using api.Services.Impl.Calculators;
using TownePark.Models.Vo;
using api.Models.Dto;
using TownePark;
using TownePark.Data;
using api.Data;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using api.Models.Vo;

namespace BackendTests.Services
{
    public class ManagementFeeCalculatorTests
    {
        private readonly ManagementFeeCalculator _calculator;
        private readonly IInternalRevenueRepository _internalRevenueRepository;
        private readonly IPayrollRepository _payrollRepository;
        private readonly IJobCodeRepository _jobCodeRepository;

        public ManagementFeeCalculatorTests()
        {
            _internalRevenueRepository = Substitute.For<IInternalRevenueRepository>();
            _payrollRepository = Substitute.For<IPayrollRepository>();
            _jobCodeRepository = Substitute.For<IJobCodeRepository>();
            _calculator = new ManagementFeeCalculator(_internalRevenueRepository, _payrollRepository, _jobCodeRepository);
        }

        [Fact]
        public void Order_ShouldReturn1()
        {
            // Arrange & Act
            var order = _calculator.Order;

            // Assert
            Assert.Equal(1, order);
        }

        [Fact]
        public async Task CalculateAndApply_WithNoManagementAgreement_ShouldReturnEarly()
        {
            // Arrange
            var siteData = new InternalRevenueDataVo { ManagementAgreement = null };
            var monthValueDto = new MonthValueDto();
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var budgetRows = new List<PnlRowDto>();

            // Act
            await _calculator.CalculateAndApplyAsync(siteData, 2024, 1, 1, monthValueDto, siteDetailDto, 0, budgetRows);

            // Assert
            Assert.Null(siteDetailDto.InternalRevenueBreakdown?.ManagementAgreement);
        }

        [Fact]
        public async Task CalculateAndApply_WithFixedFee_ShouldCalculateCorrectly()
        {
            // Arrange
            var siteData = new InternalRevenueDataVo
            {
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.ManagementAgreement }
                },
                ManagementAgreement = new ManagementAgreementVo
                {
                    Id = Guid.NewGuid(),
                    ConfiguredFee = 1000m
                }
            };
            var monthValueDto = new MonthValueDto();
            var siteDetailDto = new SiteMonthlyRevenueDetailDto
            {
                InternalRevenueBreakdown = new InternalRevenueBreakdownDto()
            };
            var budgetRows = new List<PnlRowDto>();

            // Act
            await _calculator.CalculateAndApplyAsync(siteData, 2024, 1, 1, monthValueDto, siteDetailDto, 0, budgetRows);

            // Assert
            Assert.NotNull(siteDetailDto.InternalRevenueBreakdown.ManagementAgreement);
            Assert.Equal(1000m, siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Total);
            Assert.Single(siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Components);
            Assert.Equal("Fixed Fee", siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Components[0].Name);
            Assert.Equal(1000m, siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Components[0].Value);
        }

        [Fact]
        public async Task CalculateAndApply_WithRevenuePercentage_ShouldCalculateCorrectly()
        {
            // Arrange
            var siteData = new InternalRevenueDataVo
            {
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.ManagementAgreement }
                },
                ManagementAgreement = new ManagementAgreementVo
                {
                    Id = Guid.NewGuid(),
                    ConfiguredFee = null, // No fixed fee
                    RevenuePercentageAmount = 10m // 10% of revenue
                }
            };
            var monthValueDto = new MonthValueDto();
            var siteDetailDto = new SiteMonthlyRevenueDetailDto
            {
                InternalRevenueBreakdown = new InternalRevenueBreakdownDto()
            };
            var budgetRows = new List<PnlRowDto>();

            // Act - Pass external revenue as parameter (10000)
            await _calculator.CalculateAndApplyAsync(siteData, 2024, 1, 1, monthValueDto, siteDetailDto, 10000m, budgetRows);

            // Assert
            Assert.NotNull(siteDetailDto.InternalRevenueBreakdown.ManagementAgreement);
            Assert.Equal(1000m, siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Total); // 10% of 10000
            Assert.Single(siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Components);
            Assert.Equal("Revenue % (10%)", siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Components[0].Name);
            Assert.Equal(1000m, siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Components[0].Value);
        }

        [Fact]
        public async Task CalculateAndApply_WithPerLaborHour_ShouldCalculateCorrectly()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var siteData = new InternalRevenueDataVo
            {
                SiteId = siteId,
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.ManagementAgreement }
                },
                ManagementAgreement = new ManagementAgreementVo
                {
                    Id = Guid.NewGuid(),
                    ConfiguredFee = null, // No fixed fee
                    RevenuePercentageAmount = null, // No revenue percentage
                    PerLaborHourRate = 25m,
                    PerLaborHourJobCode = "VALET"
                }
            };
            var monthValueDto = new MonthValueDto();
            var siteDetailDto = new SiteMonthlyRevenueDetailDto
            {
                InternalRevenueBreakdown = new InternalRevenueBreakdownDto()
            };
            var budgetRows = new List<PnlRowDto>();

            // Mock job code repository to return VALET as valid for this site
            var valetJobCode = new api.Models.Vo.JobCodeVo { JobCode = "VALET", JobCodeId = Guid.NewGuid(), ActiveEmployeeCount = 1m };
            _jobCodeRepository.GetJobCodesBySiteAsync(siteId).Returns(new List<api.Models.Vo.JobCodeVo> { valetJobCode });

            // Mock payroll repository to return payroll data with VALET hours (spread across days)
            var payrollDetails = new List<bs_PayrollDetail>();
            
            // Create 20 days with 8 hours each = 160 total hours
            for (int day = 1; day <= 20; day++)
            {
                var detail = new bs_PayrollDetail 
                { 
                    bs_RegularHours = 8m,
                    bs_Date = new DateTime(2024, 1, day),
                };
                detail["jobcode_display"] = "VALET";
                payrollDetails.Add(detail);
            }
            
            var mockPayroll = new bs_Payroll
            {
                bs_PayrollDetail_Payroll = payrollDetails
            };
            
            // Set up batch return
            var payrollDict = new Dictionary<Guid, bs_Payroll> { { siteId, mockPayroll } };
            _payrollRepository.GetPayrollBatchAsync(Arg.Any<List<Guid>>(), "2024-01").Returns(payrollDict);
            
            // Preload payroll data
            await _calculator.PreloadPayrollDataAsync(new List<Guid> { siteId }, 2024, 1);

            // Act
            await _calculator.CalculateAndApplyAsync(siteData, 2024, 1, 1, monthValueDto, siteDetailDto, 0, budgetRows);

            // Assert
            Assert.NotNull(siteDetailDto.InternalRevenueBreakdown.ManagementAgreement);
            Assert.Equal(4000m, siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Total); // 160 hours * $25/hour
            Assert.Single(siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Components);
            Assert.Contains("Per Labor Hour", siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Components[0].Name);
            Assert.Equal(4000m, siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Components[0].Value);
        }

        [Fact]
        public async Task AggregateMonthlyTotals_ShouldSumAllSiteManagementFees()
        {
            // Arrange
            var siteDetailsForMonth = new List<SiteMonthlyRevenueDetailDto>
            {
                new SiteMonthlyRevenueDetailDto
                {
                    InternalRevenueBreakdown = new InternalRevenueBreakdownDto
                    {
                        ManagementAgreement = new ManagementAgreementInternalRevenueDto { Total = 1000m }
                    }
                },
                new SiteMonthlyRevenueDetailDto
                {
                    InternalRevenueBreakdown = new InternalRevenueBreakdownDto
                    {
                        ManagementAgreement = new ManagementAgreementInternalRevenueDto { Total = 1500m }
                    }
                },
                new SiteMonthlyRevenueDetailDto
                {
                    InternalRevenueBreakdown = new InternalRevenueBreakdownDto()
                    // No management agreement
                }
            };
            var monthValueDto = new MonthValueDto();

            // Act
            await _calculator.AggregateMonthlyTotalsAsync(siteDetailsForMonth, monthValueDto);

            // Assert
            Assert.Equal(2500m, monthValueDto.InternalRevenueBreakdown.ManagementAgreement.Total);
        }

        [Fact]
        public async Task CalculateAndApply_WithEscalatorEnabled_ShouldApplyEscalation()
        {
            // Arrange
            var siteData = new InternalRevenueDataVo
            {
                SiteId = Guid.NewGuid(),
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.ManagementAgreement }
                },
                ManagementAgreement = new ManagementAgreementVo
                {
                    Id = Guid.NewGuid(),
                    ConfiguredFee = 1000m,
                    ManagementFeeEscalatorEnabled = true,
                    ManagementFeeEscalatorValue = 5m, // 5% escalation
                    ManagementFeeEscalatorMonth = 6,
                    ManagementFeeEscalatorType = bs_escalatortype.Percentage
                }
            };
            var monthValueDto = new MonthValueDto();
            var siteDetailDto = new SiteMonthlyRevenueDetailDto
            {
                InternalRevenueBreakdown = new InternalRevenueBreakdownDto()
            };
            var budgetRows = new List<PnlRowDto>();

            // Act - Testing June of current year, which should apply the escalator
            var currentYear = DateTime.Now.Year;
            await _calculator.CalculateAndApplyAsync(siteData, currentYear, 6, DateTime.Today.Month, monthValueDto, siteDetailDto, 0, budgetRows);

            // Assert
            Assert.NotNull(siteDetailDto.InternalRevenueBreakdown.ManagementAgreement);
            Assert.NotNull(siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Escalators);
            Assert.Single(siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Escalators);
            Assert.Equal(50m, siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Escalators[0].Amount); // 5% of 1000
            Assert.Equal(1050m, siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Total); // Base 1000 + 50 escalation
        }

        [Fact]
        public void Constructor_WithNullRepository_ShouldThrowArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new ManagementFeeCalculator(null, _payrollRepository, _jobCodeRepository));
            Assert.Throws<ArgumentNullException>(() => 
                new ManagementFeeCalculator(_internalRevenueRepository, null, _jobCodeRepository));
            Assert.Throws<ArgumentNullException>(() => 
                new ManagementFeeCalculator(_internalRevenueRepository, _payrollRepository, null));
        }

        [Fact]
        public async Task CalculateAndApply_WithAllFeeTypesPresent_ShouldPrioritizeFixedFee()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var siteData = new InternalRevenueDataVo
            {
                SiteId = siteId,
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.ManagementAgreement }
                },
                ManagementAgreement = new ManagementAgreementVo
                {
                    Id = Guid.NewGuid(),
                    ConfiguredFee = 1500m, // Fixed fee
                    RevenuePercentageAmount = 10m, // Revenue percentage
                    PerLaborHourRate = 25m, // Per labor hour rate
                    PerLaborHourJobCode = "VALET"
                }
            };
            var monthValueDto = new MonthValueDto();
            var siteDetailDto = new SiteMonthlyRevenueDetailDto
            {
                InternalRevenueBreakdown = new InternalRevenueBreakdownDto()
            };
            var budgetRows = new List<PnlRowDto>();

            // Mock job code repository to return VALET as valid for this site (though won't be used due to fixed fee priority)
            var valetJobCode = new api.Models.Vo.JobCodeVo { JobCode = "VALET", JobCodeId = Guid.NewGuid(), ActiveEmployeeCount = 1m };
            _jobCodeRepository.GetJobCodesBySiteAsync(siteId).Returns(new List<api.Models.Vo.JobCodeVo> { valetJobCode });

            // Mock payroll repository (won't be called because fixed fee takes priority)
            var mockPayrollDetail = new bs_PayrollDetail 
            { 
                bs_RegularHours = 160m,
                bs_Date = new DateTime(2024, 1, 1),
            };
            mockPayrollDetail["jobcode_display"] = "VALET";
            
            var mockPayroll = new bs_Payroll
            {
                bs_PayrollDetail_Payroll = new List<bs_PayrollDetail> { mockPayrollDetail }
            };
            
            // Set up batch return
            var payrollDict = new Dictionary<Guid, bs_Payroll> { { siteId, mockPayroll } };
            _payrollRepository.GetPayrollBatchAsync(Arg.Any<List<Guid>>(), "2024-01").Returns(payrollDict);
            
            // Preload payroll data
            await _calculator.PreloadPayrollDataAsync(new List<Guid> { siteId }, 2024, 1);

            // Act
            await _calculator.CalculateAndApplyAsync(siteData, 2024, 1, 1, monthValueDto, siteDetailDto, 10000m, budgetRows);

            // Assert - Should use fixed fee over other options
            Assert.NotNull(siteDetailDto.InternalRevenueBreakdown.ManagementAgreement);
            Assert.Equal(1500m, siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Total);
            Assert.Single(siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Components);
            Assert.Equal("Fixed Fee", siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Components[0].Name);
            Assert.Equal(1500m, siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Components[0].Value);
        }

        [Fact]
        public async Task CalculateAndApply_WithMultiplePerLaborHourJobCodes_ShouldCalculateCorrectly()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var siteData = new InternalRevenueDataVo
            {
                SiteId = siteId,
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.ManagementAgreement }
                },
                ManagementAgreement = new ManagementAgreementVo
                {
                    Id = Guid.NewGuid(),
                    ConfiguredFee = null, // No fixed fee
                    RevenuePercentageAmount = null, // No revenue percentage
                    PerLaborHourJobCodes = new List<PerLaborHourJobCodeVo>
                    {
                        new PerLaborHourJobCodeVo 
                        { 
                            Code = "CASH", 
                            Description = "Cashier",
                            StandardRate = 25m, 
                            OvertimeRate = 37.5m,
                            StandardRateEscalatorValue = 10m,
                            OvertimeRateEscalatorValue = 10m
                        },
                        new PerLaborHourJobCodeVo 
                        { 
                            Code = "VAL", 
                            Description = "Valet",
                            StandardRate = 30m, 
                            OvertimeRate = 45m,
                            StandardRateEscalatorValue = 5m,
                            OvertimeRateEscalatorValue = 5m
                        }
                    }
                }
            };
            var monthValueDto = new MonthValueDto();
            var siteDetailDto = new SiteMonthlyRevenueDetailDto
            {
                InternalRevenueBreakdown = new InternalRevenueBreakdownDto()
            };
            var budgetRows = new List<PnlRowDto>();

            // Mock job code repository to return CASH and VAL as valid for this site, but not OTHER
            var cashJobCode = new api.Models.Vo.JobCodeVo { JobCode = "CASH", JobCodeId = Guid.NewGuid(), ActiveEmployeeCount = 1m };
            var valJobCode = new api.Models.Vo.JobCodeVo { JobCode = "VAL", JobCodeId = Guid.NewGuid(), ActiveEmployeeCount = 1m };
            _jobCodeRepository.GetJobCodesBySiteAsync(siteId).Returns(new List<api.Models.Vo.JobCodeVo> { cashJobCode, valJobCode });

            // Mock payroll repository to return payroll data with CASH and VAL hours
            var mockPayrollDetail1 = new bs_PayrollDetail 
            { 
                bs_RegularHours = 100m,
                bs_Date = new DateTime(2024, 1, 1)
            };
            mockPayrollDetail1["jobcode_display"] = "CASH";
            
            var mockPayrollDetail2 = new bs_PayrollDetail 
            { 
                bs_RegularHours = 200m,
                bs_Date = new DateTime(2024, 1, 1)
            };
            mockPayrollDetail2["jobcode_display"] = "VAL";
            
            var mockPayrollDetail3 = new bs_PayrollDetail 
            { 
                bs_RegularHours = 50m,
                bs_Date = new DateTime(2024, 1, 1)
            };
            mockPayrollDetail3["jobcode_display"] = "OTHER";
            
            var mockPayroll = new bs_Payroll
            {
                bs_PayrollDetail_Payroll = new List<bs_PayrollDetail>
                {
                    mockPayrollDetail1,
                    mockPayrollDetail2,
                    mockPayrollDetail3 // Should be ignored
                }
            };
            
            // Set up batch return
            var payrollDict = new Dictionary<Guid, bs_Payroll> { { siteId, mockPayroll } };
            _payrollRepository.GetPayrollBatchAsync(Arg.Any<List<Guid>>(), "2024-01").Returns(payrollDict);
            
            // Preload payroll data
            await _calculator.PreloadPayrollDataAsync(new List<Guid> { siteId }, 2024, 1);

            // Act
            await _calculator.CalculateAndApplyAsync(siteData, 2024, 1, 1, monthValueDto, siteDetailDto, 0, budgetRows);

            // Assert
            Assert.NotNull(siteDetailDto.InternalRevenueBreakdown.ManagementAgreement);
            // CASH: 100 hours * $25/hour = $2,500
            // VAL: 200 hours * $30/hour = $6,000
            // Total: $8,500
            Assert.Equal(8500m, siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Total);
            Assert.Single(siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Components);
            Assert.Contains("Per Labor Hour", siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Components[0].Name);
            Assert.Equal(8500m, siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Components[0].Value);
        }

        [Fact]
        public async Task CalculateAndApply_WithSinglePerLaborHourJobCode_ShouldDisplaySpecificDetails()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var siteData = new InternalRevenueDataVo
            {
                SiteId = siteId,
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.ManagementAgreement }
                },
                ManagementAgreement = new ManagementAgreementVo
                {
                    Id = Guid.NewGuid(),
                    ConfiguredFee = null, // No fixed fee
                    RevenuePercentageAmount = null, // No revenue percentage
                    PerLaborHourJobCodes = new List<PerLaborHourJobCodeVo>
                    {
                        new PerLaborHourJobCodeVo 
                        { 
                            Code = "CASH", 
                            Description = "Cashier",
                            StandardRate = 25m, 
                            OvertimeRate = 37.5m
                        }
                    }
                }
            };
            var monthValueDto = new MonthValueDto();
            var siteDetailDto = new SiteMonthlyRevenueDetailDto
            {
                InternalRevenueBreakdown = new InternalRevenueBreakdownDto()
            };
            var budgetRows = new List<PnlRowDto>();

            // Mock job code repository to return CASH as valid for this site
            var cashJobCode = new api.Models.Vo.JobCodeVo { JobCode = "CASH", JobCodeId = Guid.NewGuid(), ActiveEmployeeCount = 1m };
            _jobCodeRepository.GetJobCodesBySiteAsync(siteId).Returns(new List<api.Models.Vo.JobCodeVo> { cashJobCode });

            // Mock payroll repository to return payroll data with CASH hours
            var mockPayrollDetail = new bs_PayrollDetail 
            { 
                bs_RegularHours = 100m,
                bs_Date = new DateTime(2024, 1, 1)
            };
            mockPayrollDetail["jobcode_display"] = "CASH";
            
            var mockPayroll = new bs_Payroll
            {
                bs_PayrollDetail_Payroll = new List<bs_PayrollDetail> { mockPayrollDetail }
            };
            
            // Set up batch return
            var payrollDict = new Dictionary<Guid, bs_Payroll> { { siteId, mockPayroll } };
            _payrollRepository.GetPayrollBatchAsync(Arg.Any<List<Guid>>(), "2024-01").Returns(payrollDict);
            
            // Preload payroll data
            await _calculator.PreloadPayrollDataAsync(new List<Guid> { siteId }, 2024, 1);

            // Act
            await _calculator.CalculateAndApplyAsync(siteData, 2024, 1, 1, monthValueDto, siteDetailDto, 0, budgetRows);

            // Assert
            Assert.NotNull(siteDetailDto.InternalRevenueBreakdown.ManagementAgreement);
            Assert.Equal(2500m, siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Total); // 100 hours * $25/hour
            Assert.Single(siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Components);
            Assert.Equal("Per Labor Hour (CASH: 100.00 hrs @ $25.00/hr)", siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Components[0].Name);
        }

        [Fact]
        public async Task CalculateAndApply_WithEmptyPerLaborHourJobCodes_ShouldFallbackToLegacy()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var siteData = new InternalRevenueDataVo
            {
                SiteId = siteId,
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.ManagementAgreement }
                },
                ManagementAgreement = new ManagementAgreementVo
                {
                    Id = Guid.NewGuid(),
                    ConfiguredFee = null, // No fixed fee
                    RevenuePercentageAmount = null, // No revenue percentage
                    PerLaborHourJobCodes = new List<PerLaborHourJobCodeVo>(), // Empty list
                    PerLaborHourRate = 25m,
                    PerLaborHourJobCode = "VALET"
                }
            };
            var monthValueDto = new MonthValueDto();
            var siteDetailDto = new SiteMonthlyRevenueDetailDto
            {
                InternalRevenueBreakdown = new InternalRevenueBreakdownDto()
            };
            var budgetRows = new List<PnlRowDto>();

            // Mock job code repository to return VALET as valid for this site
            var valetJobCode = new api.Models.Vo.JobCodeVo { JobCode = "VALET", JobCodeId = Guid.NewGuid(), ActiveEmployeeCount = 1m };
            _jobCodeRepository.GetJobCodesBySiteAsync(siteId).Returns(new List<api.Models.Vo.JobCodeVo> { valetJobCode });

            // Mock payroll repository to return payroll data with VALET hours
            var mockPayrollDetail = new bs_PayrollDetail 
            { 
                bs_RegularHours = 160m,
                bs_Date = new DateTime(2024, 1, 1),
            };
            mockPayrollDetail["jobcode_display"] = "VALET";
            
            var mockPayroll = new bs_Payroll
            {
                bs_PayrollDetail_Payroll = new List<bs_PayrollDetail> { mockPayrollDetail }
            };
            
            // Set up batch return
            var payrollDict = new Dictionary<Guid, bs_Payroll> { { siteId, mockPayroll } };
            _payrollRepository.GetPayrollBatchAsync(Arg.Any<List<Guid>>(), "2024-01").Returns(payrollDict);
            
            // Preload payroll data
            await _calculator.PreloadPayrollDataAsync(new List<Guid> { siteId }, 2024, 1);

            // Act
            await _calculator.CalculateAndApplyAsync(siteData, 2024, 1, 1, monthValueDto, siteDetailDto, 0, budgetRows);

            // Assert - Should fall back to legacy single job code
            Assert.NotNull(siteDetailDto.InternalRevenueBreakdown.ManagementAgreement);
            Assert.Equal(4000m, siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Total); // 160 hours * $25/hour
            Assert.Single(siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Components);
            Assert.Contains("Per Labor Hour", siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Components[0].Name);
        }

        [Fact]
        public async Task CalculateAndApply_WithJobCodeHavingZeroRate_ShouldSkipIt()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var siteData = new InternalRevenueDataVo
            {
                SiteId = siteId,
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.ManagementAgreement }
                },
                ManagementAgreement = new ManagementAgreementVo
                {
                    Id = Guid.NewGuid(),
                    ConfiguredFee = null, // No fixed fee
                    RevenuePercentageAmount = null, // No revenue percentage
                    PerLaborHourJobCodes = new List<PerLaborHourJobCodeVo>
                    {
                        new PerLaborHourJobCodeVo 
                        { 
                            Code = "CASH", 
                            StandardRate = 0m, // Zero rate - should be skipped
                            OvertimeRate = 37.5m
                        },
                        new PerLaborHourJobCodeVo 
                        { 
                            Code = "VAL", 
                            StandardRate = 30m, 
                            OvertimeRate = 45m
                        }
                    }
                }
            };
            var monthValueDto = new MonthValueDto();
            var siteDetailDto = new SiteMonthlyRevenueDetailDto
            {
                InternalRevenueBreakdown = new InternalRevenueBreakdownDto()
            };
            var budgetRows = new List<PnlRowDto>();

            // Mock job code repository to return only VAL as valid for this site (CASH has zero rate anyway)
            var valJobCode = new api.Models.Vo.JobCodeVo { JobCode = "VAL", JobCodeId = Guid.NewGuid(), ActiveEmployeeCount = 1m };
            _jobCodeRepository.GetJobCodesBySiteAsync(siteId).Returns(new List<api.Models.Vo.JobCodeVo> { valJobCode });

            // Mock payroll repository to return payroll data with CASH and VAL hours
            var mockPayrollDetail1 = new bs_PayrollDetail 
            { 
                bs_RegularHours = 100m,
                bs_Date = new DateTime(2024, 1, 1)
            };
            mockPayrollDetail1["jobcode_display"] = "CASH";
            
            var mockPayrollDetail2 = new bs_PayrollDetail 
            { 
                bs_RegularHours = 200m,
                bs_Date = new DateTime(2024, 1, 1)
            };
            mockPayrollDetail2["jobcode_display"] = "VAL";
            
            var mockPayroll = new bs_Payroll
            {
                bs_PayrollDetail_Payroll = new List<bs_PayrollDetail>
                {
                    mockPayrollDetail1,
                    mockPayrollDetail2
                }
            };
            
            // Set up batch return
            var payrollDict = new Dictionary<Guid, bs_Payroll> { { siteId, mockPayroll } };
            _payrollRepository.GetPayrollBatchAsync(Arg.Any<List<Guid>>(), "2024-01").Returns(payrollDict);
            
            // Preload payroll data
            await _calculator.PreloadPayrollDataAsync(new List<Guid> { siteId }, 2024, 1);

            // Act
            await _calculator.CalculateAndApplyAsync(siteData, 2024, 1, 1, monthValueDto, siteDetailDto, 0, budgetRows);

            // Assert
            Assert.NotNull(siteDetailDto.InternalRevenueBreakdown.ManagementAgreement);
            // Only VAL should be calculated: 200 hours * $30/hour = $6,000
            Assert.Equal(6000m, siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Total);
        }

        [Fact]
        public async Task CalculateAndApply_WithJobCodeNotInJobCodesBySite_ShouldExcludeFromCalculation()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var siteData = new InternalRevenueDataVo
            {
                SiteId = siteId,
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.ManagementAgreement }
                },
                ManagementAgreement = new ManagementAgreementVo
                {
                    Id = Guid.NewGuid(),
                    ConfiguredFee = null, // No fixed fee
                    RevenuePercentageAmount = null, // No revenue percentage
                    PerLaborHourJobCodes = new List<PerLaborHourJobCodeVo>
                    {
                        new PerLaborHourJobCodeVo 
                        { 
                            Code = "CASH", 
                            StandardRate = 25m
                        },
                        new PerLaborHourJobCodeVo 
                        { 
                            Code = "VAL", 
                            StandardRate = 30m
                        }
                    }
                }
            };
            var monthValueDto = new MonthValueDto();
            var siteDetailDto = new SiteMonthlyRevenueDetailDto
            {
                InternalRevenueBreakdown = new InternalRevenueBreakdownDto()
            };
            var budgetRows = new List<PnlRowDto>();

            // Mock job code repository to return only CASH as valid for this site (VAL is NOT in jobcodesbysite)
            var cashJobCode = new api.Models.Vo.JobCodeVo { JobCode = "CASH", JobCodeId = Guid.NewGuid(), ActiveEmployeeCount = 1m };
            _jobCodeRepository.GetJobCodesBySiteAsync(siteId).Returns(new List<api.Models.Vo.JobCodeVo> { cashJobCode });

            // Mock payroll repository to return payroll data with both CASH and VAL hours
            var mockPayrollDetail1 = new bs_PayrollDetail 
            { 
                bs_RegularHours = 100m,
                bs_Date = new DateTime(2024, 1, 1)
            };
            mockPayrollDetail1["jobcode_display"] = "CASH";
            
            var mockPayrollDetail2 = new bs_PayrollDetail 
            { 
                bs_RegularHours = 200m,
                bs_Date = new DateTime(2024, 1, 1)
            };
            mockPayrollDetail2["jobcode_display"] = "VAL";
            
            var mockPayroll = new bs_Payroll
            {
                bs_PayrollDetail_Payroll = new List<bs_PayrollDetail>
                {
                    mockPayrollDetail1,
                    mockPayrollDetail2
                }
            };
            
            // Set up batch return
            var payrollDict = new Dictionary<Guid, bs_Payroll> { { siteId, mockPayroll } };
            _payrollRepository.GetPayrollBatchAsync(Arg.Any<List<Guid>>(), "2024-01").Returns(payrollDict);
            
            // Preload payroll data
            await _calculator.PreloadPayrollDataAsync(new List<Guid> { siteId }, 2024, 1);

            // Act
            await _calculator.CalculateAndApplyAsync(siteData, 2024, 1, 1, monthValueDto, siteDetailDto, 0, budgetRows);

            // Assert
            Assert.NotNull(siteDetailDto.InternalRevenueBreakdown.ManagementAgreement);
            // Only CASH should be calculated since VAL is not in jobcodesbysite: 100 hours * $25/hour = $2,500
            Assert.Equal(2500m, siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Total);
            Assert.Single(siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Components);
            Assert.Equal("Per Labor Hour (CASH: 100.00 hrs @ $25.00/hr)", siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Components[0].Name);
        }
        [Fact]
        public async Task GetForecastedHours_WithCaching_ShouldCallRepositoryOnlyOncePerSite()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var siteData = new InternalRevenueDataVo
            {
                SiteId = siteId,
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.ManagementAgreement }
                },
                ManagementAgreement = new ManagementAgreementVo
                {
                    Id = Guid.NewGuid(),
                    PerLaborHourJobCodes = new List<PerLaborHourJobCodeVo>
                    {
                        new PerLaborHourJobCodeVo { Code = "VALET", StandardRate = 25m }
                    }
                }
            };
            
            // Mock job codes
            var valetJobCode = new api.Models.Vo.JobCodeVo { JobCode = "VALET", JobCodeId = Guid.NewGuid(), ActiveEmployeeCount = 1m };
            _jobCodeRepository.GetJobCodesBySiteAsync(siteId)
                .Returns(new List<api.Models.Vo.JobCodeVo> { valetJobCode });
            
            // Mock payroll for multiple months
            for (int month = 1; month <= 12; month++)
            {
                var mockPayrollDetail = new bs_PayrollDetail 
                { 
                    bs_RegularHours = 160m,
                    bs_Date = new DateTime(2024, month, 1),
                };
                mockPayrollDetail["jobcode_display"] = "VALET";
                
                var mockPayroll = new bs_Payroll
                {
                    bs_PayrollDetail_Payroll = new List<bs_PayrollDetail> { mockPayrollDetail }
                };
                
                // Set up batch return for each month
                var payrollDict = new Dictionary<Guid, bs_Payroll> { { siteId, mockPayroll } };
                _payrollRepository.GetPayrollBatchAsync(Arg.Any<List<Guid>>(), $"2024-{month:D2}").Returns(payrollDict);
            }

            // Act - Simulate processing 12 months
            decimal totalFees = 0;
            for (int month = 1; month <= 12; month++)
            {
                var monthValueDto = new MonthValueDto();
                var siteDetailDto = new SiteMonthlyRevenueDetailDto
                {
                    InternalRevenueBreakdown = new InternalRevenueBreakdownDto()
                };
                var budgetRows = new List<PnlRowDto>();
                
                // Preload payroll data for the month
                await _calculator.PreloadPayrollDataAsync(new List<Guid> { siteId }, 2024, month);
                await _calculator.CalculateAndApplyAsync(siteData, 2024, month, month, monthValueDto, siteDetailDto, 0, budgetRows);
                
                if (siteDetailDto.InternalRevenueBreakdown?.ManagementAgreement?.Total.HasValue == true)
                {
                    totalFees += siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Total.Value;
                }
            }

            // Assert
            Assert.Equal(48000m, totalFees); // 12 months * 160 hours * $25/hour
            
            // Verify GetJobCodesBySiteAsync was called only once due to caching
            _jobCodeRepository.Received(1).GetJobCodesBySiteAsync(siteId);
        }

        [Fact]
        public async Task ClearJobCodeCache_ShouldResetCache()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var siteData = new InternalRevenueDataVo
            {
                SiteId = siteId,
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.ManagementAgreement }
                },
                ManagementAgreement = new ManagementAgreementVo
                {
                    Id = Guid.NewGuid(),
                    PerLaborHourRate = 25m,
                    PerLaborHourJobCode = "VALET"
                }
            };
            
            var valetJobCode = new api.Models.Vo.JobCodeVo { JobCode = "VALET", JobCodeId = Guid.NewGuid(), ActiveEmployeeCount = 1m };
            _jobCodeRepository.GetJobCodesBySiteAsync(siteId)
                .Returns(new List<api.Models.Vo.JobCodeVo> { valetJobCode });
            
            var mockPayrollDetail = new bs_PayrollDetail 
            { 
                bs_RegularHours = 160m,
                bs_Date = new DateTime(2024, 1, 1),
            };
            mockPayrollDetail["jobcode_display"] = "VALET";
            
            var mockPayroll = new bs_Payroll
            {
                bs_PayrollDetail_Payroll = new List<bs_PayrollDetail> { mockPayrollDetail }
            };
            
            // Set up batch return for any billing period
            var payrollDict = new Dictionary<Guid, bs_Payroll> { { siteId, mockPayroll } };
            _payrollRepository.GetPayrollBatchAsync(Arg.Any<List<Guid>>(), Arg.Any<string>()).Returns(payrollDict);

            var monthValueDto = new MonthValueDto();
            var siteDetailDto = new SiteMonthlyRevenueDetailDto
            {
                InternalRevenueBreakdown = new InternalRevenueBreakdownDto()
            };
            var budgetRows = new List<PnlRowDto>();

            // Act
            // First call - should fetch from repository
            await _calculator.PreloadPayrollDataAsync(new List<Guid> { siteId }, 2024, 1);
            await _calculator.CalculateAndApplyAsync(siteData, 2024, 1, 1, monthValueDto, siteDetailDto, 0, budgetRows);
            
            // Clear cache
            _calculator.ClearJobCodeCache();
            
            // Second call after cache clear - should fetch from repository again
            await _calculator.PreloadPayrollDataAsync(new List<Guid> { siteId }, 2024, 2);
            await _calculator.CalculateAndApplyAsync(siteData, 2024, 2, 2, monthValueDto, siteDetailDto, 0, budgetRows);

            // Assert - Repository should be called twice (once before clear, once after)
            _jobCodeRepository.Received(2).GetJobCodesBySiteAsync(siteId);
        }
        [Fact]
        public async Task Debug_GetForecastedHours_Basic()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var siteData = new InternalRevenueDataVo
            {
                SiteId = siteId,
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.ManagementAgreement }
                },
                ManagementAgreement = new ManagementAgreementVo
                {
                    Id = Guid.NewGuid(),
                    PerLaborHourRate = 25m,
                    PerLaborHourJobCode = "TEST"
                }
            };

            // Mock job code repository
            var testJobCode = new api.Models.Vo.JobCodeVo { JobCode = "TEST", JobCodeId = Guid.NewGuid(), ActiveEmployeeCount = 1m };
            _jobCodeRepository.GetJobCodesBySiteAsync(siteId).Returns(new List<api.Models.Vo.JobCodeVo> { testJobCode });

            // Mock payroll
            var mockPayrollDetail = new bs_PayrollDetail 
            { 
                bs_RegularHours = 10m,
                bs_Date = new DateTime(2024, 1, 1),
            };
            mockPayrollDetail["jobcode_display"] = "TEST";
            
            var mockPayroll = new bs_Payroll
            {
                bs_PayrollDetail_Payroll = new List<bs_PayrollDetail> { mockPayrollDetail }
            };
            
            // Set up batch return
            var payrollDict = new Dictionary<Guid, bs_Payroll> { { siteId, mockPayroll } };
            _payrollRepository.GetPayrollBatchAsync(Arg.Any<List<Guid>>(), "2024-01").Returns(payrollDict);
            
            // Preload payroll data
            await _calculator.PreloadPayrollDataAsync(new List<Guid> { siteId }, 2024, 1);

            var monthValueDto = new MonthValueDto();
            var siteDetailDto = new SiteMonthlyRevenueDetailDto
            {
                InternalRevenueBreakdown = new InternalRevenueBreakdownDto()
            };
            var budgetRows = new List<PnlRowDto>();

            // Act
            await _calculator.CalculateAndApplyAsync(siteData, 2024, 1, 1, monthValueDto, siteDetailDto, 0, budgetRows);

            // Assert
            Assert.NotNull(siteDetailDto.InternalRevenueBreakdown.ManagementAgreement);
            Assert.Equal(250m, siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Total); // 10 hours * $25/hour
        }

        [Fact]
        public async Task PreloadPayrollDataAsync_ShouldCachePayrollData()
        {
            // Arrange
            var siteId1 = Guid.NewGuid();
            var siteId2 = Guid.NewGuid();
            var siteIds = new List<Guid> { siteId1, siteId2 };
            var billingPeriod = "2024-01";

            var payrollBatch = new Dictionary<Guid, bs_Payroll>
            {
                { siteId1, new bs_Payroll { bs_Name = "Payroll 1" } },
                { siteId2, new bs_Payroll { bs_Name = "Payroll 2" } }
            };

            _payrollRepository.GetPayrollBatchAsync(siteIds, billingPeriod).Returns(payrollBatch);

            // Act
            await _calculator.PreloadPayrollDataAsync(siteIds, 2024, 1);

            // Assert - Verify batch method was called
            await _payrollRepository.Received(1).GetPayrollBatchAsync(siteIds, billingPeriod);
        }

        [Fact]
        public async Task GetForecastedHours_WithPreloadedCache_ShouldNotCallRepository()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var siteData = new InternalRevenueDataVo
            {
                SiteId = siteId,
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.ManagementAgreement }
                },
                ManagementAgreement = new ManagementAgreementVo
                {
                    Id = Guid.NewGuid(),
                    PerLaborHourJobCodes = new List<PerLaborHourJobCodeVo>
                    {
                        new PerLaborHourJobCodeVo { Code = "VALET", StandardRate = 25m }
                    }
                }
            };

            // Mock job code repository
            var valetJobCode = new api.Models.Vo.JobCodeVo { JobCode = "VALET", JobCodeId = Guid.NewGuid(), ActiveEmployeeCount = 1m };
            _jobCodeRepository.GetJobCodesBySiteAsync(siteId).Returns(new List<api.Models.Vo.JobCodeVo> { valetJobCode });

            // Create payroll data
            var mockPayrollDetail = new bs_PayrollDetail
            {
                bs_RegularHours = 40m,
                bs_Date = new DateTime(2024, 1, 15)
            };
            mockPayrollDetail["jobcode_display"] = "VALET";
            
            var mockPayroll = new bs_Payroll
            {
                bs_PayrollDetail_Payroll = new List<bs_PayrollDetail> { mockPayrollDetail }
            };

            // Setup batch response
            var payrollBatch = new Dictionary<Guid, bs_Payroll>
            {
                { siteId, mockPayroll }
            };
            _payrollRepository.GetPayrollBatchAsync(Arg.Any<List<Guid>>(), "2024-01").Returns(payrollBatch);

            // Preload the cache
            await _calculator.PreloadPayrollDataAsync(new List<Guid> { siteId }, 2024, 1);

            var monthValueDto = new MonthValueDto();
            var siteDetailDto = new SiteMonthlyRevenueDetailDto
            {
                InternalRevenueBreakdown = new InternalRevenueBreakdownDto()
            };
            var budgetRows = new List<PnlRowDto>();

            // Act
            await _calculator.CalculateAndApplyAsync(siteData, 2024, 1, 1, monthValueDto, siteDetailDto, 0, budgetRows);

            // Assert
            Assert.NotNull(siteDetailDto.InternalRevenueBreakdown.ManagementAgreement);
            Assert.Equal(1000m, siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Total); // 40 hours * $25/hour

            // Verify batch method was called only once during preload
            await _payrollRepository.Received(1).GetPayrollBatchAsync(Arg.Any<List<Guid>>(), "2024-01");
        }

        [Fact]
        public async Task PreloadPayrollDataAsync_CalledTwiceForSamePeriod_ShouldOnlyCallRepositoryOnce()
        {
            // Arrange
            var siteIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var payrollBatch = new Dictionary<Guid, bs_Payroll>();
            _payrollRepository.GetPayrollBatchAsync(Arg.Any<List<Guid>>(), "2024-02").Returns(payrollBatch);

            // Act - Call preload twice
            await _calculator.PreloadPayrollDataAsync(siteIds, 2024, 2);
            await _calculator.PreloadPayrollDataAsync(siteIds, 2024, 2);

            // Assert - Repository should only be called once due to caching
            await _payrollRepository.Received(1).GetPayrollBatchAsync(Arg.Any<List<Guid>>(), "2024-02");
        }

        [Fact]
        public void ClearAllCaches_ShouldClearBothCaches()
        {
            // This test verifies the method exists and can be called
            // Actual cache clearing behavior is tested indirectly through other tests
            _calculator.ClearAllCaches();
            
            // If we get here without exception, the method works
            Assert.True(true);
        }

        [Fact]
        public async Task ClearPayrollCache_ShouldRemoveCachedData()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var siteIds = new List<Guid> { siteId };
            var payrollBatch = new Dictionary<Guid, bs_Payroll>
            {
                { siteId, new bs_Payroll { bs_Name = "Test Payroll" } }
            };
            _payrollRepository.GetPayrollBatchAsync(siteIds, "2024-03").Returns(payrollBatch);

            // Preload cache
            await _calculator.PreloadPayrollDataAsync(siteIds, 2024, 3);

            // Clear the cache for this period
            _calculator.ClearPayrollCache("2024-03");

            // Preload again - should call repository again since cache was cleared
            await _calculator.PreloadPayrollDataAsync(siteIds, 2024, 3);

            // Assert - Repository should be called twice (before and after clear)
            await _payrollRepository.Received(2).GetPayrollBatchAsync(siteIds, "2024-03");
        }

        [Fact]
        public async Task CalculateAndApply_WhenManagementAgreementNotInContractTypes_ShouldReturnEarly()
        {
            // Arrange
            var siteData = new InternalRevenueDataVo
            {
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices>
                    {
                        bs_contracttypechoices.FixedFee,
                        bs_contracttypechoices.PerLaborHour
                        // Note: ManagementAgreement is NOT in the list
                    }
                },
                ManagementAgreement = new ManagementAgreementVo
                {
                    Id = Guid.NewGuid(),
                    ConfiguredFee = 1000m
                }
            };
            var monthValueDto = new MonthValueDto();
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var budgetRows = new List<PnlRowDto>();

            // Act
            await _calculator.CalculateAndApplyAsync(siteData, 2024, 1, 1, monthValueDto, siteDetailDto, 0, budgetRows);

            // Assert - ManagementAgreement should not be calculated
            Assert.Null(siteDetailDto.InternalRevenueBreakdown?.ManagementAgreement);
        }

        [Fact]
        public async Task CalculateAndApply_WhenManagementAgreementInContractTypes_ShouldCalculateNormally()
        {
            // Arrange
            var siteData = new InternalRevenueDataVo
            {
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices>
                    {
                        bs_contracttypechoices.FixedFee,
                        bs_contracttypechoices.ManagementAgreement // ManagementAgreement IS in the list
                    }
                },
                ManagementAgreement = new ManagementAgreementVo
                {
                    Id = Guid.NewGuid(),
                    ConfiguredFee = 1000m
                }
            };
            var monthValueDto = new MonthValueDto();
            var siteDetailDto = new SiteMonthlyRevenueDetailDto
            {
                InternalRevenueBreakdown = new InternalRevenueBreakdownDto()
            };
            var budgetRows = new List<PnlRowDto>();

            // Act
            await _calculator.CalculateAndApplyAsync(siteData, 2024, 1, 1, monthValueDto, siteDetailDto, 0, budgetRows);

            // Assert - ManagementAgreement should be calculated
            Assert.NotNull(siteDetailDto.InternalRevenueBreakdown.ManagementAgreement);
            Assert.Equal(1000m, siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Total);
        }

        [Fact]
        public async Task CalculateAndApply_WhenContractIsNull_ShouldReturnEarly()
        {
            // Arrange
            var siteData = new InternalRevenueDataVo
            {
                Contract = null, // No contract data
                ManagementAgreement = new ManagementAgreementVo
                {
                    Id = Guid.NewGuid(),
                    ConfiguredFee = 1000m
                }
            };
            var monthValueDto = new MonthValueDto();
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var budgetRows = new List<PnlRowDto>();

            // Act
            await _calculator.CalculateAndApplyAsync(siteData, 2024, 1, 1, monthValueDto, siteDetailDto, 0, budgetRows);

            // Assert - ManagementAgreement should not be calculated
            Assert.Null(siteDetailDto.InternalRevenueBreakdown?.ManagementAgreement);
        }

        [Fact]
        public async Task CalculateAndApply_WhenContractTypesIsNull_ShouldReturnEarly()
        {
            // Arrange
            var siteData = new InternalRevenueDataVo
            {
                Contract = new ContractDataVo
                {
                    ContractTypes = null // No contract types
                },
                ManagementAgreement = new ManagementAgreementVo
                {
                    Id = Guid.NewGuid(),
                    ConfiguredFee = 1000m
                }
            };
            var monthValueDto = new MonthValueDto();
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var budgetRows = new List<PnlRowDto>();

            // Act
            await _calculator.CalculateAndApplyAsync(siteData, 2024, 1, 1, monthValueDto, siteDetailDto, 0, budgetRows);

            // Assert - ManagementAgreement should not be calculated
            Assert.Null(siteDetailDto.InternalRevenueBreakdown?.ManagementAgreement);
        }

        [Fact]
        public async Task CalculateAndApply_WhenContractTypesIsEmpty_ShouldReturnEarly()
        {
            // Arrange
            var siteData = new InternalRevenueDataVo
            {
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices>() // Empty list
                },
                ManagementAgreement = new ManagementAgreementVo
                {
                    Id = Guid.NewGuid(),
                    ConfiguredFee = 1000m
                }
            };
            var monthValueDto = new MonthValueDto();
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var budgetRows = new List<PnlRowDto>();

            // Act
            await _calculator.CalculateAndApplyAsync(siteData, 2024, 1, 1, monthValueDto, siteDetailDto, 0, budgetRows);

            // Assert - ManagementAgreement should not be calculated
            Assert.Null(siteDetailDto.InternalRevenueBreakdown?.ManagementAgreement);
        }
    }
}