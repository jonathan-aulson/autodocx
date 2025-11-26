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
    public class SupportServicesCalculatorTest
    {
        private readonly SupportServicesCalculator _calculator;
        private readonly IBillableExpenseRepository _mockRepository;
        private readonly IPayrollRepository _mockPayrollRepository;

        public SupportServicesCalculatorTest()
        {
            _mockRepository = Substitute.For<IBillableExpenseRepository>();
            _mockPayrollRepository = Substitute.For<IPayrollRepository>();
            _calculator = new SupportServicesCalculator(_mockPayrollRepository);
        }

        [Fact]
        public void CalculateAndApply_WhenContractTypeNotBillingAccount_SkipsSupportServicesCalculation()
        {
            // Arrange
            var siteData = CreateTestSiteDataWithoutBillingAccount();
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();

            // Act
            _calculator.CalculateAndApply(siteData, 2025, 5, 5, monthValueDto, siteDetailDto, 0m, new List<PnlRowDto>());

            // Assert
            var supportServices = siteDetailDto.InternalRevenueBreakdown?.BillableAccounts?.SupportServices;
            Assert.Null(supportServices); // Should be null since contract type doesn't include BillingAccount
        }

        [Fact]
        public void CalculateAndApply_WhenSupportServicesDisabled_SkipsSupportServicesCalculation()
        {
            // Arrange
            var siteData = CreateTestSiteDataWithConfig(supportServicesEnabled: false);
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();

            // Act
            _calculator.CalculateAndApply(siteData, 2025, 5, 5, monthValueDto, siteDetailDto, 0m, new List<PnlRowDto>());

            // Assert
            var supportServices = siteDetailDto.InternalRevenueBreakdown?.BillableAccounts?.SupportServices;
            Assert.Null(supportServices); // Should be null since Support Services is disabled
        }

        [Fact]
        public void CalculateAndApply_SupportServicesAsFixedAmount_CalculatesCorrectly()
        {
            // Arrange - Scenario 7: Support Services as Fixed Amount ($2,500)
            var siteData = CreateTestSiteDataWithConfig(
                supportServicesEnabled: true,
                billingType: "Fixed",
                amount: 2500.0m
            );
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();

            // Act
            _calculator.CalculateAndApply(siteData, 2025, 5, 5, monthValueDto, siteDetailDto, 0m, new List<PnlRowDto>());

            // Assert
            var supportServices = siteDetailDto.InternalRevenueBreakdown?.BillableAccounts?.SupportServices;
            Assert.NotNull(supportServices);
            Assert.Equal(2500.0m, supportServices.Total);
            
            // Verify it's added to BillableAccounts total
            Assert.Equal(2500.0m, siteDetailDto.InternalRevenueBreakdown.BillableAccounts.Total);
            
            // Verify it's added to overall calculated total
            Assert.Equal(2500.0m, siteDetailDto.InternalRevenueBreakdown.CalculatedTotalInternalRevenue);
        }

        [Fact]
        public void CalculateAndApply_SupportServicesAsPercentageOfBillablePayroll_CalculatesCorrectly()
        {
            // Arrange - Scenario 8: Support Services as Percentage of Billable Payroll (10%)
            var siteData = CreateTestSiteDataWithConfig(
                supportServicesEnabled: true,
                billingType: "Percentage",
                amount: 10.0m,
                payrollType: "Billable"
            );
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();

            // Provide budget rows to supply billable payroll budget (used for non-current months)
            var budgetRows = BuildBudgetRows("Payroll", siteData.SiteNumber, 5, 50000.0m);

            // Act
            _calculator.CalculateAndApply(siteData, 2025, 5, 1, monthValueDto, siteDetailDto, 0m, budgetRows);

            // Assert
            var supportServices = siteDetailDto.InternalRevenueBreakdown?.BillableAccounts?.SupportServices;
            Assert.NotNull(supportServices);
            Assert.Equal(5000.0m, supportServices.Total); // 50,000 * 10% = 5,000
            
            // Verify it's added to BillableAccounts total
            Assert.Equal(5000.0m, siteDetailDto.InternalRevenueBreakdown.BillableAccounts.Total);
        }

        [Fact]
        public void CalculateAndApply_SupportServicesAsPercentageOfTotalPayroll_CalculatesCorrectly()
        {
            // Arrange - Support Services as Percentage of Total Payroll (15%)
            var siteData = CreateTestSiteDataWithConfig(
                supportServicesEnabled: true,
                billingType: "Percentage",
                amount: 15.0m,
                payrollType: "Total"
            );
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();

            // Provide budget rows for billable payroll; PTEB provided via breakdown for total payroll calculation
            var budgetRows = BuildBudgetRows("Payroll", siteData.SiteNumber, 5, 80000.0m);
            // Provide PTEB on the site breakdown
            siteDetailDto.InternalRevenueBreakdown = new InternalRevenueBreakdownDto
            {
                BillableAccounts = new BillableAccountsInternalRevenueDto
                {
                    Pteb = new PtebInternalRevenueDto { Total = 20000.0m }
                }
            };

            // Act
            _calculator.CalculateAndApply(siteData, 2025, 5, 1, monthValueDto, siteDetailDto, 0m, budgetRows);

            // Assert
            var supportServices = siteDetailDto.InternalRevenueBreakdown?.BillableAccounts?.SupportServices;
            Assert.NotNull(supportServices);
            // Total payroll = billable payroll (80,000) + PTEB (20,000) = 100,000; 15% = 15,000
            Assert.Equal(15000.0m, supportServices.Total);
        }

        [Fact]
        public void CalculateAndApply_PercentageTypeWithZeroPayroll_ReturnsZero()
        {
            // Arrange - Edge case: Zero payroll
            var siteData = CreateTestSiteDataWithConfig(
                supportServicesEnabled: true,
                billingType: "Percentage",
                amount: 10.0m
            );
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();

            // Budget rows provide zero payroll
            var budgetRows = BuildBudgetRows("Payroll", siteData.SiteNumber, 5, 0m);

            // Act
            _calculator.CalculateAndApply(siteData, 2025, 5, 5, monthValueDto, siteDetailDto, 0m, budgetRows);

            // Assert
            var supportServices = siteDetailDto.InternalRevenueBreakdown?.BillableAccounts?.SupportServices;
            Assert.NotNull(supportServices);
            Assert.Equal(0m, supportServices.Total);
        }

        [Fact]
        public void CalculateAndApply_PercentageTypeWithNullAmount_ReturnsZero()
        {
            // Arrange - Edge case: Null percentage amount
            var siteData = CreateTestSiteDataWithConfig(
                supportServicesEnabled: true,
                billingType: "Percentage",
                amount: null
            );
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();

            // Budget rows provide payroll
            var budgetRows = BuildBudgetRows("Payroll", siteData.SiteNumber, 5, 10000.0m);

            // Act
            _calculator.CalculateAndApply(siteData, 2025, 5, 5, monthValueDto, siteDetailDto, 0m, budgetRows);

            // Assert
            var supportServices = siteDetailDto.InternalRevenueBreakdown?.BillableAccounts?.SupportServices;
            Assert.NotNull(supportServices);
            Assert.Equal(0m, supportServices.Total);
        }

        [Fact]
        public void CalculateAndApply_CurrentMonth_Percentage_Billable_UsesOnlyInternalActuals()
        {
            // Arrange: current month parameters
            var today = DateTime.Today;
            var year = today.Year;
            var month = today.Month;

            var siteData = CreateTestSiteDataWithConfig(
                supportServicesEnabled: true,
                billingType: "Percentage",
                amount: 10.0m,
                payrollType: "Billable");

            var siteDetailDto = new SiteMonthlyRevenueDetailDto
            {
                SiteId = siteData.SiteNumber,
                // New implementation uses PayrollBreakdown for current month
                PayrollBreakdown = new PayrollBreakdownDto
                {
                    ActualPayroll = 300m,
                    ActualPayrollLastDate = new DateTime(year, month, 10)
                },
                InternalRevenueBreakdown = new InternalRevenueBreakdownDto
                {
                    BillableAccounts = new BillableAccountsInternalRevenueDto()
                }
            };

            var budgetRows = BuildBudgetRows("Payroll", siteData.SiteNumber, month, 0m);
            var monthValueDto = new MonthValueDto();

            // Arrange payroll repo to return empty details (no forecast)
            _mockPayrollRepository.GetPayroll(siteData.SiteId, $"{year}-{month:D2}").Returns((bs_Payroll)null);

            // Act
            _calculator.CalculateAndApply(siteData, year, month, month, monthValueDto, siteDetailDto, 0m, budgetRows);

            // Assert: Actuals = (100+200)*10% = 30; Forecast = daily forecast from repo (mocked 0 here) = 0
            var support = siteDetailDto.InternalRevenueBreakdown?.BillableAccounts?.SupportServices;
            Assert.NotNull(support);
            Assert.Equal(30m, support!.ActualSupportServices);
            Assert.Equal(0m, support.ForecastedSupportServices ?? 0m);
            Assert.Equal(30m, support.Total);
            Assert.Equal(new DateTime(year, month, 10).Date, support.LastActualDate?.Date);
        }

        [Fact]
        public void CalculateAndApply_CurrentMonth_Percentage_Total_PTEB_AllForecast()
        {
            // Arrange: current month parameters
            var today = DateTime.Today;
            var year = today.Year;
            var month = today.Month;
            var daysInMonth = DateTime.DaysInMonth(year, month);

            var siteData = CreateTestSiteDataWithConfig(
                supportServicesEnabled: true,
                billingType: "Percentage",
                amount: 10.0m,
                payrollType: "Total");

            // Internal actuals through day 15: payroll actual 300
            var maxActualDay = Math.Min(15, daysInMonth);
            var siteDetailDto = new SiteMonthlyRevenueDetailDto
            {
                SiteId = siteData.SiteNumber,
                PayrollBreakdown = new PayrollBreakdownDto
                {
                    ActualPayroll = 300m,
                    ActualPayrollLastDate = new DateTime(year, month, maxActualDay)
                },
                InternalRevenueBreakdown = new InternalRevenueBreakdownDto
                {
                    BillableAccounts = new BillableAccountsInternalRevenueDto
                    {
                        Pteb = new PtebInternalRevenueDto { Total = 300m }
                    }
                }
            };

            var budgetRows = BuildBudgetRows("Payroll", siteData.SiteNumber, month, 0m);
            var monthValueDto = new MonthValueDto();
            // Arrange payroll repo to return empty details (no forecast)
            _mockPayrollRepository.GetPayroll(siteData.SiteId, $"{year}-{month:D2}").Returns((bs_Payroll)null);

            // Act
            _calculator.CalculateAndApply(siteData, year, month, month, monthValueDto, siteDetailDto, 0m, budgetRows);

            // Assert: payroll actual=300, forecast from repo (mocked 0 here) + PTEB 300 => 300; 10%
            var expectedActual = 300m * 0.10m;
            var expectedForecast = 300m * 0.10m;
            var support = siteDetailDto.InternalRevenueBreakdown?.BillableAccounts?.SupportServices;
            Assert.NotNull(support);
            Assert.Equal(Math.Round(expectedActual, 2), Math.Round(support!.ActualSupportServices ?? 0m, 2));
            Assert.Equal(Math.Round(expectedForecast, 2), Math.Round(support.ForecastedSupportServices ?? 0m, 2));
            Assert.Equal(Math.Round(expectedActual + expectedForecast, 2), Math.Round(support.Total ?? 0m, 2));
            Assert.Equal(new DateTime(year, month, maxActualDay).Date, support.LastActualDate?.Date);
        }

        [Fact]
        public void CalculateAndApply_CurrentMonth_NoDailyActuals_AllForecast()
        {
            // Arrange
            var today = DateTime.Today;
            var year = today.Year;
            var month = today.Month;

            var siteData = CreateTestSiteDataWithConfig(
                supportServicesEnabled: true,
                billingType: "Percentage",
                amount: 10.0m,
                payrollType: "Total");

            var siteDetailDto = new SiteMonthlyRevenueDetailDto
            {
                SiteId = siteData.SiteNumber,
                InternalActuals = new api.Models.Vo.InternalRevenueActualsVo
                {
                    SiteId = siteData.SiteNumber,
                    Year = year,
                    Month = month,
                    DailyActuals = new List<api.Models.Vo.DailyActualVo>()
                },
                InternalRevenueBreakdown = new InternalRevenueBreakdownDto
                {
                    BillableAccounts = new BillableAccountsInternalRevenueDto
                    {
                        Pteb = new PtebInternalRevenueDto { Total = 500m }
                    }
                }
            };

            var budgetRows = BuildBudgetRows("Payroll", siteData.SiteNumber, month, 0m);
            var monthValueDto = new MonthValueDto();
            // Arrange payroll repo to return empty details (no forecast)
            _mockPayrollRepository.GetPayroll(siteData.SiteId, $"{year}-{month:D2}").Returns((bs_Payroll)null);

            // Act
            _calculator.CalculateAndApply(siteData, year, month, month, monthValueDto, siteDetailDto, 0m, budgetRows);

            // Assert: Actual = 0; Forecast = (repo forecast mocked 0 + 500 PTEB)*10% = 50
            var support = siteDetailDto.InternalRevenueBreakdown?.BillableAccounts?.SupportServices;
            Assert.NotNull(support);
            Assert.True((support!.ActualSupportServices ?? 0m) == 0m);
            Assert.Equal(50m, support.ForecastedSupportServices);
            Assert.Equal(50m, support.Total);
            var prevMonthEnd = new DateTime(year, month, 1).AddDays(-1);
            Assert.Equal(prevMonthEnd.Date, support.LastActualDate?.Date);
        }

        [Fact]
        public void CalculateAndApply_FixedTypeWithNullAmount_ReturnsZero()
        {
            // Arrange - Edge case: Fixed amount with null value
            var siteData = CreateTestSiteDataWithConfig(
                supportServicesEnabled: true,
                billingType: "Fixed",
                amount: null
            );
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();

            // Act
            _calculator.CalculateAndApply(siteData, 2025, 5, 5, monthValueDto, siteDetailDto, 0m, new List<PnlRowDto>());

            // Assert
            var supportServices = siteDetailDto.InternalRevenueBreakdown?.BillableAccounts?.SupportServices;
            Assert.NotNull(supportServices);
            Assert.Equal(0m, supportServices.Total);
        }

        [Fact]
        public void CalculateAndApply_NullConfiguration_SkipsSupportServicesCalculation()
        {
            // Arrange - Edge case: No billable accounts configuration
            var siteData = new InternalRevenueDataVo
            {
                SiteId = Guid.NewGuid(),
                SiteNumber = "0170",
                Contract = new ContractDataVo
                {
                    ContractId = Guid.NewGuid(),
                    ContractTypes = new[] { bs_contracttypechoices.BillingAccount },
                    BillableAccountsData = new List<BillableAccountConfigVo>() // Empty list
                }
            };
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();

            // Act
            _calculator.CalculateAndApply(siteData, 2025, 5, 5, monthValueDto, siteDetailDto, 0m, new List<PnlRowDto>());

            // Assert
            var supportServices = siteDetailDto.InternalRevenueBreakdown?.BillableAccounts?.SupportServices;
            Assert.Null(supportServices);
        }

        [Fact]
        public void CalculateAndApply_AddsToExistingBillableAccountsTotal()
        {
            // Arrange - Test that Support Services adds to existing BillableAccounts total
            var siteData = CreateTestSiteDataWithConfig(
                supportServicesEnabled: true,
                billingType: "Fixed",
                amount: 1000.0m
            );
            var siteDetailDto = new SiteMonthlyRevenueDetailDto
            {
                InternalRevenueBreakdown = new InternalRevenueBreakdownDto
                {
                    BillableAccounts = new BillableAccountsInternalRevenueDto
                    {
                        Total = 5000.0m // Existing total from other components (PTEB, Additional Payroll, etc.)
                    }
                }
            };
            var monthValueDto = new MonthValueDto();

            // Act
            _calculator.CalculateAndApply(siteData, 2025, 5, 5, monthValueDto, siteDetailDto, 0m, new List<PnlRowDto>());

            // Assert
            var supportServices = siteDetailDto.InternalRevenueBreakdown.BillableAccounts.SupportServices;
            Assert.NotNull(supportServices);
            Assert.Equal(1000.0m, supportServices.Total);
            
            // Verify it adds to existing total
            Assert.Equal(6000.0m, siteDetailDto.InternalRevenueBreakdown.BillableAccounts.Total); // 5000 + 1000
        }

        [Fact]
        public void AggregateMonthlyTotals_AggregatesSupportServicesCorrectly()
        {
            // Arrange
            var siteDetails = new List<SiteMonthlyRevenueDetailDto>
            {
                CreateSiteDetailWithSupportServices("site1", 1500.0m),
                CreateSiteDetailWithSupportServices("site2", 2000.0m),
                CreateSiteDetailWithSupportServices("site3", 750.0m)
            };
            var monthValueDto = new MonthValueDto();

            // Act
            _calculator.AggregateMonthlyTotals(siteDetails, monthValueDto);

            // Assert
            var supportServices = monthValueDto.InternalRevenueBreakdown?.BillableAccounts?.SupportServices;
            Assert.NotNull(supportServices);
            Assert.Equal(4250.0m, supportServices.Total); // 1500 + 2000 + 750
            
            // Verify it's added to BillableAccounts total at month level
            Assert.Equal(4250.0m, monthValueDto.InternalRevenueBreakdown.BillableAccounts.Total);
        }

        [Fact]
        public void AggregateMonthlyTotals_HandlesNullSupportServices()
        {
            // Arrange - Mix of sites with and without Support Services
            var siteDetails = new List<SiteMonthlyRevenueDetailDto>
            {
                CreateSiteDetailWithSupportServices("site1", 1000.0m),
                new SiteMonthlyRevenueDetailDto { SiteId = "site2" }, // No Support Services
                CreateSiteDetailWithSupportServices("site3", 500.0m)
            };
            var monthValueDto = new MonthValueDto();

            // Act
            _calculator.AggregateMonthlyTotals(siteDetails, monthValueDto);

            // Assert
            var supportServices = monthValueDto.InternalRevenueBreakdown?.BillableAccounts?.SupportServices;
            Assert.NotNull(supportServices);
            Assert.Equal(1500.0m, supportServices.Total); // 1000 + 0 + 500
        }

        [Fact]
        public void AggregateMonthlyTotals_AddsToExistingMonthlyBillableAccountsTotal()
        {
            // Arrange - Test aggregation with existing monthly totals
            var siteDetails = new List<SiteMonthlyRevenueDetailDto>
            {
                CreateSiteDetailWithSupportServices("site1", 800.0m),
                CreateSiteDetailWithSupportServices("site2", 1200.0m)
            };
            var monthValueDto = new MonthValueDto
            {
                InternalRevenueBreakdown = new InternalRevenueBreakdownDto
                {
                    BillableAccounts = new BillableAccountsInternalRevenueDto
                    {
                        Total = 10000.0m // Existing total from other components
                    }
                }
            };

            // Act
            _calculator.AggregateMonthlyTotals(siteDetails, monthValueDto);

            // Assert
            var supportServices = monthValueDto.InternalRevenueBreakdown.BillableAccounts.SupportServices;
            Assert.NotNull(supportServices);
            Assert.Equal(2000.0m, supportServices.Total); // 800 + 1200
            
            // Verify it adds to existing monthly total
            Assert.Equal(12000.0m, monthValueDto.InternalRevenueBreakdown.BillableAccounts.Total); // 10000 + 2000
        }

        private InternalRevenueDataVo CreateTestSiteDataWithoutBillingAccount()
        {
            return new InternalRevenueDataVo
            {
                SiteId = Guid.NewGuid(),
                SiteNumber = "0170",
                Contract = new ContractDataVo
                {
                    ContractId = Guid.NewGuid(),
                    ContractTypes = new[] { bs_contracttypechoices.FixedFee } // No BillingAccount
                }
            };
        }

        private InternalRevenueDataVo CreateTestSiteDataWithConfig(
            bool supportServicesEnabled = true,
            string billingType = "Fixed",
            decimal? amount = null,
            string payrollType = "Billable")
        {
            return new InternalRevenueDataVo
            {
                SiteId = Guid.NewGuid(),
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
                            PayrollSupportEnabled = supportServicesEnabled,
                            PayrollSupportBillingType = billingType,
                            PayrollSupportAmount = amount,
                            PayrollSupportPayrollType = payrollType
                        }
                    }
                }
            };
        }

        private SiteMonthlyRevenueDetailDto CreateSiteDetailWithSupportServices(string siteId, decimal supportServicesTotal)
        {
            return new SiteMonthlyRevenueDetailDto
            {
                SiteId = siteId,
                InternalRevenueBreakdown = new InternalRevenueBreakdownDto
                {
                    BillableAccounts = new BillableAccountsInternalRevenueDto
                    {
                        SupportServices = new SupportServicesInternalRevenueDto
                        {
                            Total = supportServicesTotal
                        }
                    }
                }
            };
        }

        private List<PnlRowDto> BuildBudgetRows(string columnName, string siteNumber, int monthOneBased, decimal value)
        {
            return new List<PnlRowDto>
            {
                new PnlRowDto
                {
                    ColumnName = columnName,
                    MonthlyValues = new List<MonthValueDto>
                    {
                        new MonthValueDto
                        {
                            Month = monthOneBased - 1,
                            SiteDetails = new List<SiteMonthlyRevenueDetailDto>
                            {
                                new SiteMonthlyRevenueDetailDto
                                {
                                    SiteId = siteNumber,
                                    Value = value
                                }
                            }
                        }
                    }
                }
            };
        }
    }
} 