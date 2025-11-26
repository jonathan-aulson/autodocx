using System;
using System.Collections.Generic;
using Xunit;
using api.Services.Impl.Calculators;
using api.Models.Dto;
using TownePark.Models.Vo;
using TownePark;
using api.Data;
using NSubstitute;

namespace BackendTests.Services
{
    public class BillablePtebCalculatorTest
    {
        private readonly BillablePtebCalculator _calculator;
        private readonly IBillableExpenseRepository _mockRepository;

        public BillablePtebCalculatorTest()
        {
            _mockRepository = Substitute.For<IBillableExpenseRepository>();
            _calculator = new BillablePtebCalculator(_mockRepository);
        }

        [Fact]
        public void CalculateAndApply_WhenContractTypeNotBillingAccount_SkipsPtebCalculation()
        {
            // Arrange
            var siteData = CreateTestSiteDataWithoutBillingAccount();
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();

            // Act
            _calculator.CalculateAndApply(siteData, 2025, 5, 5, monthValueDto, siteDetailDto, 0m, new List<PnlRowDto>());

            // Assert
            var pteb = siteDetailDto.InternalRevenueBreakdown?.BillableAccounts?.Pteb;
            Assert.Null(pteb); // Should be null since contract type doesn't include BillingAccount
        }

        [Fact]
        public void CalculateAndApply_WhenPayrollTaxesDisabled_SkipsPtebCalculation()
        {
            // Arrange
            var siteData = CreateTestSiteDataWithConfig(payrollTaxesEnabled: false);
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();

            // Act
            _calculator.CalculateAndApply(siteData, 2025, 5, 5, monthValueDto, siteDetailDto, 0m, new List<PnlRowDto>());

            // Assert
            var pteb = siteDetailDto.InternalRevenueBreakdown?.BillableAccounts?.Pteb;
            Assert.Null(pteb); // Should be null since PTEB is disabled
        }

        [Fact]
        public void CalculateAndApply_PercentageType_CalculatesCorrectly()
        {
            // Arrange
            var siteData = CreateTestSiteDataWithConfig(
                payrollTaxesEnabled: true,
                billingType: "Percentage",
                percentage: 15.0m
            );
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();

            // Mock repository to return known payroll expense budget
            _mockRepository.GetPayrollExpenseBudget(siteData.SiteId, 2025, 5).Returns(1000.0m);

            // Act
            _calculator.CalculateAndApply(siteData, 2025, 5, 5, monthValueDto, siteDetailDto, 0m, new List<PnlRowDto>());

            // Assert
            var pteb = siteDetailDto.InternalRevenueBreakdown?.BillableAccounts?.Pteb;
            Assert.NotNull(pteb);
            Assert.Equal("Percentage", pteb.CalculationType);
            Assert.Equal(15.0m, pteb.AppliedPercentage);
            Assert.Equal(1000.0m, pteb.BaseAmount);
            Assert.Equal(150.0m, pteb.Total); // 1000 * 15% = 150
        }

        [Fact]
        public void CalculateAndApply_ActualType_UsesExistingPnlResponse()
        {
            // Arrange
            var siteData = CreateTestSiteDataWithConfig(
                payrollTaxesEnabled: true,
                billingType: "Actual"
            );
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();
            var budgetRows = CreateMockBudgetRowsWithPteb("0170", 5, 1250.75m);

            // Act
            _calculator.CalculateAndApply(siteData, 2025, 5, 5, monthValueDto, siteDetailDto, 0m, budgetRows);

            // Assert
            var pteb = siteDetailDto.InternalRevenueBreakdown?.BillableAccounts?.Pteb;
            Assert.NotNull(pteb);
            Assert.Equal("Actual", pteb.CalculationType);
            Assert.Equal(1250.75m, pteb.Total);
        }

        [Fact]
        public void CalculateAndApply_PercentageWithEscalator_AppliesEscalatorCorrectly()
        {
            // Arrange
            var siteData = CreateTestSiteDataWithConfig(
                payrollTaxesEnabled: true,
                billingType: "Percentage",
                percentage: 10.0m,
                escalatorEnabled: true,
                escalatorMonth: 3,
                escalatorType: "Percentage",
                escalatorValue: 5.0m
            );
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();

            // Mock repository to return known payroll expense budget
            _mockRepository.GetPayrollExpenseBudget(siteData.SiteId, 2025, 5).Returns(2000.0m);

            // Act - Testing escalator logic (month 5 > escalator month 3)
            _calculator.CalculateAndApply(siteData, 2025, 5, 5, monthValueDto, siteDetailDto, 0m, new List<PnlRowDto>());

            // Assert
            var pteb = siteDetailDto.InternalRevenueBreakdown?.BillableAccounts?.Pteb;
            Assert.NotNull(pteb);
            Assert.Equal("Percentage", pteb.CalculationType);
            Assert.Equal(10.0m, pteb.AppliedPercentage);
            Assert.Equal(2000.0m, pteb.BaseAmount);
            // Base: 2000 * 10% = 200, With 5% escalator: 200 * 1.05 = 210
            Assert.Equal(210.0m, pteb.Total);
        }

        [Fact]
        public void CalculateAndApply_ActualType_IgnoresEscalators()
        {
            // Arrange
            var siteData = CreateTestSiteDataWithConfig(
                payrollTaxesEnabled: true,
                billingType: "Actual",
                escalatorEnabled: true,
                escalatorMonth: 3,
                escalatorType: "Amount",
                escalatorValue: 100.0m
            );
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();
            var budgetRows = CreateMockBudgetRowsWithPteb("0170", 5, 1000.0m);

            // Act
            _calculator.CalculateAndApply(siteData, 2025, 5, 5, monthValueDto, siteDetailDto, 0m, budgetRows);

            // Assert
            var pteb = siteDetailDto.InternalRevenueBreakdown?.BillableAccounts?.Pteb;
            Assert.NotNull(pteb);
            Assert.Equal("Actual", pteb.CalculationType);
            Assert.Equal(1000.0m, pteb.Total); // Should be unchanged despite escalator
        }

        [Fact]
        public void AggregateMonthlyTotals_AggregatesPtebCorrectly()
        {
            // Arrange
            var siteDetails = new List<SiteMonthlyRevenueDetailDto>
            {
                CreateSiteDetailWithPteb("site1", 500.0m),
                CreateSiteDetailWithPteb("site2", 750.0m),
                CreateSiteDetailWithPteb("site3", 250.0m)
            };
            var monthValueDto = new MonthValueDto();

            // Act
            _calculator.AggregateMonthlyTotals(siteDetails, monthValueDto);

            // Assert
            var pteb = monthValueDto.InternalRevenueBreakdown?.BillableAccounts?.Pteb;
            Assert.NotNull(pteb);
            Assert.Equal(1500.0m, pteb.Total); // 500 + 750 + 250
        }

        private InternalRevenueDataVo CreateTestSiteDataWithoutBillingAccount()
        {
            return new InternalRevenueDataVo
            {
                SiteNumber = "0170",
                Contract = new ContractDataVo
                {
                    ContractId = Guid.NewGuid(),
                    ContractTypes = new[] { bs_contracttypechoices.FixedFee } // No BillingAccount
                }
            };
        }

        private InternalRevenueDataVo CreateTestSiteDataWithConfig(
            bool payrollTaxesEnabled = true,
            string billingType = "Percentage",
            decimal? percentage = null,
            bool escalatorEnabled = false,
            int? escalatorMonth = null,
            string escalatorType = "Percentage",
            decimal? escalatorValue = null)
        {
            return new InternalRevenueDataVo
            {
                SiteNumber = "0170",
                Contract = new ContractDataVo
                {
                    ContractId = Guid.NewGuid(),
                    ContractTypes = new[] { bs_contracttypechoices.BillingAccount },
                    BillableAccountsData = new List<BillableAccountConfigVo>
                    {
                        new BillableAccountConfigVo
                        {
                            Id = Guid.NewGuid(),
                            PayrollTaxesEnabled = payrollTaxesEnabled,
                            PayrollTaxesBillingType = billingType,
                            PayrollTaxesPercentage = percentage,
                            PayrollTaxesEscalatorEnable = escalatorEnabled,
                            PayrollTaxesEscalatorMonth = escalatorMonth,
                            PayrollTaxesEscalatorType = escalatorType,
                            PayrollTaxesEscalatorValue = escalatorValue
                        }
                    }
                }
            };
        }

        private List<PnlRowDto> CreateMockBudgetRowsWithPteb(string siteNumber, int monthOneBased, decimal ptebValue)
        {
            return new List<PnlRowDto>
            {
                new PnlRowDto
                {
                    ColumnName = "Pteb",
                    MonthlyValues = new List<MonthValueDto>
                    {
                        new MonthValueDto
                        {
                            Month = monthOneBased - 1, // Convert to 0-based
                            SiteDetails = new List<SiteMonthlyRevenueDetailDto>
                            {
                                new SiteMonthlyRevenueDetailDto
                                {
                                    SiteId = siteNumber,
                                    Value = ptebValue
                                }
                            }
                        }
                    }
                }
            };
        }

        private SiteMonthlyRevenueDetailDto CreateSiteDetailWithPteb(string siteId, decimal ptebTotal)
        {
            return new SiteMonthlyRevenueDetailDto
            {
                SiteId = siteId,
                InternalRevenueBreakdown = new InternalRevenueBreakdownDto
                {
                    BillableAccounts = new BillableAccountsInternalRevenueDto
                    {
                        Pteb = new PtebInternalRevenueDto
                        {
                            Total = ptebTotal
                        }
                    }
                }
            };
        }
    }
} 