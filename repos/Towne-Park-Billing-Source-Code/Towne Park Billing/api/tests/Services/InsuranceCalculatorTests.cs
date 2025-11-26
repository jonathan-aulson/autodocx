using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Models.Dto;
using api.Models.Vo;
using api.Data;
using api.Services.Impl.Calculators;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TownePark;
using TownePark.Data;
using TownePark.Models.Vo;
using Xunit;

namespace BackendTests.Services
{
    public class InsuranceCalculatorTests
    {
        private readonly IInternalRevenueRepository _internalRevenueRepository;
        private readonly IBillableExpenseRepository _billableExpenseRepository;
        private readonly ILogger<InsuranceCalculator> _logger;
        private readonly IPayrollRepository _payrollRepository;
        private readonly InsuranceCalculator _calculator;

        public InsuranceCalculatorTests()
        {
            _internalRevenueRepository = Substitute.For<IInternalRevenueRepository>();
            _billableExpenseRepository = Substitute.For<IBillableExpenseRepository>();
            _payrollRepository = Substitute.For<IPayrollRepository>();
            _logger = Substitute.For<ILogger<InsuranceCalculator>>();
            _calculator = new InsuranceCalculator(_internalRevenueRepository, _billableExpenseRepository, _payrollRepository, _logger);
        }

        [Fact]
        public async Task CalculateAndApplyAsync_InsuranceDisabled_SkipsAllCalculations()
        {
            // Arrange
            var siteData = CreateSiteData();
            siteData.ManagementAgreement = new ManagementAgreementVo
            {
                InsuranceEnabled = false,
                InsuranceType = bs_managementagreementinsurancetype.FixedFee,
                InsuranceFixedFeeAmount = 5000m
            };
            
            var siteDetailDto = new SiteMonthlyRevenueDetailDto { SiteId = siteData.SiteId.ToString() };
            var monthValueDto = new MonthValueDto();

            // Act
            await _calculator.CalculateAndApplyAsync(
                siteData, 2024, 1, 1, monthValueDto, siteDetailDto, 100000m, new List<PnlRowDto>());

            // Assert - No insurance should be calculated when disabled
            Assert.Null(siteDetailDto.InternalRevenueBreakdown);
        }

        [Fact]
        public async Task CalculateAndApplyAsync_FixedFeeInsurance_UsesFixedAmountOnly()
        {
            // Arrange
            var siteData = CreateSiteData();
            siteData.ManagementAgreement = new ManagementAgreementVo
            {
                InsuranceEnabled = true,
                InsuranceType = bs_managementagreementinsurancetype.FixedFee,
                InsuranceFixedFeeAmount = 5000m,
                InsuranceAddlAmount = 1000m // Should be ignored for fixed fee
            };
            
            var siteDetailDto = new SiteMonthlyRevenueDetailDto { SiteId = siteData.SiteId.ToString() };
            var monthValueDto = new MonthValueDto();

            // Act
            await _calculator.CalculateAndApplyAsync(
                siteData, 2024, 1, 1, monthValueDto, siteDetailDto, 100000m, new List<PnlRowDto>());

            // Assert
            Assert.NotNull(siteDetailDto.InternalRevenueBreakdown?.ManagementAgreement);
            Assert.Equal(5000m, siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.CalculatedInsurance);
            Assert.Equal(5000m, siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Total);
            Assert.Equal(5000m, siteDetailDto.InternalRevenueBreakdown.CalculatedTotalInternalRevenue);

            // Verify no billable expense call was made for fixed fee
            _billableExpenseRepository.DidNotReceive().GetPayrollExpenseBudget(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>());
        }

        [Fact]
        public async Task CalculateAndApplyAsync_PercentageBased_Calculates577PercentOfPayroll()
        {
            // Arrange
            var siteData = CreateSiteData();
            siteData.ManagementAgreement = new ManagementAgreementVo
            {
                InsuranceEnabled = true,
                InsuranceType = bs_managementagreementinsurancetype.BasedOnBillableAccounts,
                // Additional is a percentage; 0.5% of 100,000 = 500
                InsuranceAddlAmount = 0.5m
            };

            var siteDetailDto = new SiteMonthlyRevenueDetailDto { SiteId = siteData.SiteId.ToString() };
            var monthValueDto = new MonthValueDto();

            // Provide PNL budget payroll via budgetRows (non-current month path uses PNL rows)
            var budgetRows = new List<PnlRowDto>
            {
                new PnlRowDto
                {
                    ColumnName = "Payroll",
                    MonthlyValues = new List<MonthValueDto>
                    {
                        new MonthValueDto
                        {
                            Month = 0,
                            SiteDetails = new List<SiteMonthlyRevenueDetailDto>
                            {
                                new SiteMonthlyRevenueDetailDto { SiteId = siteData.SiteId.ToString(), Value = 100000m }
                            }
                        }
                    }
                }
            };
            _billableExpenseRepository.GetVehicleInsuranceBudget(siteData.SiteId, 2024, 1)
                .Returns(0m); // No vehicle insurance in this test

            // Act
            await _calculator.CalculateAndApplyAsync(
                siteData, 2024, 1, 1, monthValueDto, siteDetailDto, 100000m, budgetRows);

            // Assert
            var expectedBaseInsurance = 100000m * 0.0577m; // 5,770
            var expectedTotal = expectedBaseInsurance + 0m + 500m; // 6,270 (no vehicle insurance in test)
            
            Assert.NotNull(siteDetailDto.InternalRevenueBreakdown?.ManagementAgreement);
            Assert.Equal(expectedTotal, siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.CalculatedInsurance);
            Assert.Equal(expectedTotal, siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Total);
            Assert.Equal(expectedTotal, siteDetailDto.InternalRevenueBreakdown.CalculatedTotalInternalRevenue);
        }

        [Fact]
        public async Task CalculateAndApplyAsync_NoManagementAgreement_LogsWarningAndReturns()
        {
            // Arrange
            var siteData = CreateSiteData();
            siteData.ManagementAgreement = null;
            
            var siteDetailDto = new SiteMonthlyRevenueDetailDto { SiteId = siteData.SiteId.ToString() };
            var monthValueDto = new MonthValueDto();

            // Act
            await _calculator.CalculateAndApplyAsync(
                siteData, 2024, 1, 1, monthValueDto, siteDetailDto, 100000m, new List<PnlRowDto>());

            // Assert
            Assert.Null(siteDetailDto.InternalRevenueBreakdown);
        }

        [Fact]
        public async Task CalculateAndApplyAsync_NoPayrollBudget_ReturnsZeroBaseInsurance()
        {
            // Arrange
            var siteData = CreateSiteData();
            siteData.ManagementAgreement = new ManagementAgreementVo
            {
                InsuranceEnabled = true,
                InsuranceType = bs_managementagreementinsurancetype.BasedOnBillableAccounts,
                // Additional is percentage; with zero payroll base, additional is zero
                InsuranceAddlAmount = 0.5m
            };

            var siteDetailDto = new SiteMonthlyRevenueDetailDto { SiteId = siteData.SiteId.ToString() };
            var monthValueDto = new MonthValueDto();

            _billableExpenseRepository.GetPayrollExpenseBudget(siteData.SiteId, 2024, 1)
                .Returns(0m);
            _billableExpenseRepository.GetVehicleInsuranceBudget(siteData.SiteId, 2024, 1)
                .Returns(0m);

            // Act
            await _calculator.CalculateAndApplyAsync(
                siteData, 2024, 1, 1, monthValueDto, siteDetailDto, 100000m, new List<PnlRowDto>());

            // Assert
            Assert.NotNull(siteDetailDto.InternalRevenueBreakdown?.ManagementAgreement);
            Assert.Equal(0m, siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.CalculatedInsurance); // Zero base & additional with zero payroll
        }

        [Fact]
        public async Task CalculateAndApplyAsync_HandlesExistingDtoValues_AddsToTotals()
        {
            // Arrange
            var siteData = CreateSiteData();
            siteData.ManagementAgreement = new ManagementAgreementVo
            {
                InsuranceEnabled = true,
                InsuranceType = bs_managementagreementinsurancetype.FixedFee,
                InsuranceFixedFeeAmount = 5000m
            };

            var siteDetailDto = new SiteMonthlyRevenueDetailDto 
            { 
                SiteId = siteData.SiteId.ToString(),
                InternalRevenueBreakdown = new InternalRevenueBreakdownDto
                {
                    ManagementAgreement = new ManagementAgreementInternalRevenueDto
                    {
                        Total = 1000m
                    },
                    CalculatedTotalInternalRevenue = 2000m
                }
            };
            var monthValueDto = new MonthValueDto();

            // Act
            await _calculator.CalculateAndApplyAsync(
                siteData, 2024, 1, 1, monthValueDto, siteDetailDto, 100000m, new List<PnlRowDto>());

            // Assert
            Assert.Equal(5000m, siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.CalculatedInsurance);
            Assert.Equal(6000m, siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Total); // 1000 + 5000
            Assert.Equal(7000m, siteDetailDto.InternalRevenueBreakdown.CalculatedTotalInternalRevenue); // 2000 + 5000
        }

        [Fact]
        public async Task AggregateMonthlyTotalsAsync_SumsInsuranceAcrossAllSites()
        {
            // Arrange
            var monthValueDto = new MonthValueDto();
            var siteDetails = new List<SiteMonthlyRevenueDetailDto>
            {
                CreateSiteDetailDtoWithInsurance(1000m),
                CreateSiteDetailDtoWithInsurance(2000m),
                CreateSiteDetailDtoWithInsurance(3000m),
                CreateSiteDetailDtoWithInsurance(null) // Test null handling
            };

            // Act
            await _calculator.AggregateMonthlyTotalsAsync(siteDetails, monthValueDto);

            // Assert
            Assert.NotNull(monthValueDto.InternalRevenueBreakdown?.ManagementAgreement);
            Assert.Equal(6000m, monthValueDto.InternalRevenueBreakdown.ManagementAgreement.CalculatedInsurance);
        }

        [Fact]
        public async Task AggregateMonthlyTotalsAsync_EmptySiteList_ReturnsZero()
        {
            // Arrange
            var monthValueDto = new MonthValueDto();
            var siteDetails = new List<SiteMonthlyRevenueDetailDto>();

            // Act
            await _calculator.AggregateMonthlyTotalsAsync(siteDetails, monthValueDto);

            // Assert
            Assert.NotNull(monthValueDto.InternalRevenueBreakdown?.ManagementAgreement);
            Assert.Equal(0m, monthValueDto.InternalRevenueBreakdown.ManagementAgreement.CalculatedInsurance);
        }

        [Fact]
        public void Order_ReturnsTwo_RunsAfterManagementFeeCalculator()
        {
            // Assert
            Assert.Equal(2, _calculator.Order);
        }

        [Fact]
        public async Task CalculateAndApplyAsync_NullInsuranceType_TreatsAsBasedOnBillableAccounts()
        {
            // Arrange
            var siteData = CreateSiteData();
            siteData.ManagementAgreement = new ManagementAgreementVo
            {
                InsuranceEnabled = true,
                InsuranceType = null, // Null type
                // 0.5% additional of 100,000 = 500
                InsuranceAddlAmount = 0.5m
            };

            var siteDetailDto = new SiteMonthlyRevenueDetailDto { SiteId = siteData.SiteId.ToString() };
            var monthValueDto = new MonthValueDto();

            // Provide PNL budget payroll via budgetRows
            var budgetRows = new List<PnlRowDto>
            {
                new PnlRowDto
                {
                    ColumnName = "Payroll",
                    MonthlyValues = new List<MonthValueDto>
                    {
                        new MonthValueDto
                        {
                            Month = 0,
                            SiteDetails = new List<SiteMonthlyRevenueDetailDto>
                            {
                                new SiteMonthlyRevenueDetailDto { SiteId = siteData.SiteId.ToString(), Value = 100000m }
                            }
                        }
                    }
                }
            };
            _billableExpenseRepository.GetVehicleInsuranceBudget(siteData.SiteId, 2024, 1)
                .Returns(0m);

            // Act
            await _calculator.CalculateAndApplyAsync(
                siteData, 2024, 1, 1, monthValueDto, siteDetailDto, 100000m, budgetRows);

            // Assert
            var expectedBaseInsurance = 100000m * 0.0577m; // 5,770
            var expectedTotal = expectedBaseInsurance + 0m + 500m; // 6,270
            
            Assert.Equal(expectedTotal, siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.CalculatedInsurance);
        }

        [Fact]
        public async Task CalculateAndApplyAsync_WithVehicleInsurance_IncludesInTotal()
        {
            // Arrange
            var siteData = CreateSiteData();
            siteData.ManagementAgreement = new ManagementAgreementVo
            {
                InsuranceEnabled = true,
                InsuranceType = bs_managementagreementinsurancetype.BasedOnBillableAccounts,
                // 0.5% of 100,000 = 500
                InsuranceAddlAmount = 0.5m
            };

            var siteDetailDto = new SiteMonthlyRevenueDetailDto { SiteId = siteData.SiteId.ToString() };
            var monthValueDto = new MonthValueDto();

            // Provide PNL budget payroll via budgetRows
            var budgetRows = new List<PnlRowDto>
            {
                new PnlRowDto
                {
                    ColumnName = "Payroll",
                    MonthlyValues = new List<MonthValueDto>
                    {
                        new MonthValueDto
                        {
                            Month = 0,
                            SiteDetails = new List<SiteMonthlyRevenueDetailDto>
                            {
                                new SiteMonthlyRevenueDetailDto { SiteId = siteData.SiteId.ToString(), Value = 100000m }
                            }
                        }
                    }
                }
            };
            _billableExpenseRepository.GetVehicleInsuranceBudget(siteData.SiteId, 2024, 1)
                .Returns(1500m); // Vehicle insurance amount

            // Act
            await _calculator.CalculateAndApplyAsync(
                siteData, 2024, 1, 1, monthValueDto, siteDetailDto, 100000m, budgetRows);

            // Assert
            var expectedBaseInsurance = 100000m * 0.0577m; // 5,770
            var expectedTotal = expectedBaseInsurance + 1500m + 500m; // 7,770
            
            Assert.NotNull(siteDetailDto.InternalRevenueBreakdown?.ManagementAgreement);
            Assert.Equal(expectedTotal, siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.CalculatedInsurance);
            Assert.Equal(expectedTotal, siteDetailDto.InternalRevenueBreakdown.ManagementAgreement.Total);
            Assert.Equal(expectedTotal, siteDetailDto.InternalRevenueBreakdown.CalculatedTotalInternalRevenue);
        }

        // Helper methods
        private InternalRevenueDataVo CreateSiteData()
        {
            return new InternalRevenueDataVo
            {
                SiteId = Guid.NewGuid(),
                SiteNumber = "0170",
                Contract = new ContractDataVo { ContractTypes = new[] { bs_contracttypechoices.ManagementAgreement } },
                SiteName = "Test Site"
            };
        }

        private SiteMonthlyRevenueDetailDto CreateSiteDetailDtoWithInsurance(decimal? insuranceAmount)
        {
            var dto = new SiteMonthlyRevenueDetailDto { SiteId = Guid.NewGuid().ToString() };
            if (insuranceAmount.HasValue)
            {
                dto.InternalRevenueBreakdown = new InternalRevenueBreakdownDto
                {
                    ManagementAgreement = new ManagementAgreementInternalRevenueDto
                    {
                        CalculatedInsurance = insuranceAmount.Value
                    }
                };
            }
            return dto;
        }
    }
}