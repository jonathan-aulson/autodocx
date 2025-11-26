using Xunit;
using api.Services.Impl.Calculators;
using api.Models.Dto;
using TownePark.Models.Vo;
using TownePark;
using api.Data;
using NSubstitute;
using Microsoft.Extensions.Logging;

namespace BackendTests.Services
{
    public class ExpenseAccountCalculatorTest
    {
        private readonly ExpenseAccountCalculator _calculator;
        private readonly IBillableExpenseRepository _mockBillableExpenseRepository;
        private readonly IOtherExpenseRepository _mockOtherExpenseRepository;
        private readonly ILogger<ExpenseAccountCalculator> _mockLogger;

        public ExpenseAccountCalculatorTest()
        {
            _mockBillableExpenseRepository = Substitute.For<IBillableExpenseRepository>();
            _mockOtherExpenseRepository = Substitute.For<IOtherExpenseRepository>();
            _mockLogger = Substitute.For<ILogger<ExpenseAccountCalculator>>();
            
            _calculator = new ExpenseAccountCalculator(
                _mockBillableExpenseRepository,
                _mockOtherExpenseRepository,
                _mockLogger);
        }

        [Fact]
        public void CalculateAndApply_WhenContractTypeNotBillingAccount_SkipsExpenseAccountsCalculation()
        {
            // Arrange
            var siteData = CreateTestSiteDataWithoutBillingAccount();
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();

            // Act
            _calculator.CalculateAndApply(siteData, 2025, 5, 5, monthValueDto, siteDetailDto, 0m, new List<PnlRowDto>());

            // Assert
            var expenseAccounts = siteDetailDto.InternalRevenueBreakdown?.BillableAccounts?.ExpenseAccounts;
            Assert.Null(expenseAccounts); // Should be null since contract type doesn't include BillingAccount
        }

        [Fact]
        public void CalculateAndApply_WithValidBillingAccountContract_CalculatesExpenseAccounts()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var siteData = CreateTestSiteDataWithBillingAccount(siteId);
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();

            // Mock no enabled accounts configured (falls back to original behavior)
            _mockBillableExpenseRepository.GetEnabledExpenseAccounts(siteId).Returns(new List<string>());
            _mockBillableExpenseRepository.GetBillableExpenseBudget(siteId, 2025, 5).Returns(1000m);
            _mockBillableExpenseRepository.GetOtherExpenseBudget(siteId, 2025, 5).Returns(600m);
            _mockOtherExpenseRepository.GetOtherExpenseDetail(siteId, "2025-01").Returns((IEnumerable<TownePark.bs_OtherExpenseDetail>?)null);

            // Act
            _calculator.CalculateAndApply(siteData, 2025, 5, 5, monthValueDto, siteDetailDto, 0m, new List<PnlRowDto>());

            // Assert
            var expenseAccounts = siteDetailDto.InternalRevenueBreakdown?.BillableAccounts?.ExpenseAccounts;
            Assert.NotNull(expenseAccounts);
            Assert.Equal(1600m, expenseAccounts.Total); // 1000 + 600 (full budget when no forecast)
        }

        [Fact]
        public void CalculateAndApply_WithForecastData_UsesForecastOverBudget()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var siteData = CreateTestSiteDataWithBillingAccount(siteId);
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();

            // Mock no enabled accounts configured (falls back to original behavior)
            _mockBillableExpenseRepository.GetEnabledExpenseAccounts(siteId).Returns(new List<string>());
            _mockBillableExpenseRepository.GetBillableExpenseBudget(siteId, 2025, 5).Returns(1000m);
            _mockBillableExpenseRepository.GetOtherExpenseBudget(siteId, 2025, 5).Returns(600m);
            
            // Mock forecast data - create expense detail with one account having data
            var mockExpenseDetail = new TownePark.bs_OtherExpenseDetail
            {
                bs_MonthYear = "2025-05",
                bs_EmployeeRelations = 150m // Only this account has forecast data
                // All other accounts will be null, so GetFieldValue returns 0
            };
            _mockOtherExpenseRepository.GetOtherExpenseDetail(siteId, "2025-01")
                .Returns(new List<TownePark.bs_OtherExpenseDetail> { mockExpenseDetail });

            // Act
            _calculator.CalculateAndApply(siteData, 2025, 5, 5, monthValueDto, siteDetailDto, 0m, new List<PnlRowDto>());

            // Assert
            var expenseAccounts = siteDetailDto.InternalRevenueBreakdown?.BillableAccounts?.ExpenseAccounts;
            Assert.NotNull(expenseAccounts);
            // 1000 (non-forecasted) + 150 (employee relations forecast) + 0 (other accounts with no forecast) = 1150
            Assert.Equal(1150m, expenseAccounts.Total);
        }

        [Fact]
        public void CalculateAndApply_UpdatesBillableAccountsTotal()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var siteData = CreateTestSiteDataWithBillingAccount(siteId);
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();

            // Pre-populate billable accounts with other values
            siteDetailDto.InternalRevenueBreakdown = new InternalRevenueBreakdownDto
            {
                BillableAccounts = new BillableAccountsInternalRevenueDto
                {
                    Total = 500m
                }
            };

            // Mock no enabled accounts configured (falls back to original behavior)
            _mockBillableExpenseRepository.GetEnabledExpenseAccounts(siteId).Returns(new List<string>());
            _mockBillableExpenseRepository.GetBillableExpenseBudget(siteId, 2025, 5).Returns(1000m);
            _mockBillableExpenseRepository.GetOtherExpenseBudget(siteId, 2025, 5).Returns(600m);
            _mockOtherExpenseRepository.GetOtherExpenseDetail(siteId, "2025-01").Returns((IEnumerable<TownePark.bs_OtherExpenseDetail>?)null);

            // Act
            _calculator.CalculateAndApply(siteData, 2025, 5, 5, monthValueDto, siteDetailDto, 0m, new List<PnlRowDto>());

            // Assert
            var billableAccounts = siteDetailDto.InternalRevenueBreakdown?.BillableAccounts;
            Assert.NotNull(billableAccounts);
            Assert.Equal(2100m, billableAccounts.Total); // 500 + 1600
        }

        [Fact]
        public void AggregateMonthlyTotals_SumsAllSiteTotals()
        {
            // Arrange
            var monthValueDto = new MonthValueDto();
            var siteDetails = new List<SiteMonthlyRevenueDetailDto>
            {
                CreateSiteDetailWithExpenseAccounts(100m),
                CreateSiteDetailWithExpenseAccounts(200m),
                CreateSiteDetailWithExpenseAccounts(300m)
            };

            // Act
            _calculator.AggregateMonthlyTotals(siteDetails, monthValueDto);

            // Assert
            var expenseAccounts = monthValueDto.InternalRevenueBreakdown?.BillableAccounts?.ExpenseAccounts;
            Assert.NotNull(expenseAccounts);
            Assert.Equal(600m, expenseAccounts.Total);
        }

        [Fact]
        public void AggregateMonthlyTotals_HandlesNullValues()
        {
            // Arrange
            var monthValueDto = new MonthValueDto();
            var siteDetails = new List<SiteMonthlyRevenueDetailDto>
            {
                CreateSiteDetailWithExpenseAccounts(100m),
                new SiteMonthlyRevenueDetailDto(), // No internal revenue breakdown
                CreateSiteDetailWithExpenseAccounts(null) // Null expense accounts
            };

            // Act
            _calculator.AggregateMonthlyTotals(siteDetails, monthValueDto);

            // Assert
            var expenseAccounts = monthValueDto.InternalRevenueBreakdown?.BillableAccounts?.ExpenseAccounts;
            Assert.NotNull(expenseAccounts);
            Assert.Equal(100m, expenseAccounts.Total);
        }

        [Fact]
        public void CalculateAndApply_WithMultipleCallsForSameSiteYear_CachesExpenseData()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var siteData = CreateTestSiteDataWithBillingAccount(siteId);
            var siteDetailDto1 = new SiteMonthlyRevenueDetailDto();
            var siteDetailDto2 = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();

            // Mock no enabled accounts configured (falls back to original behavior)
            _mockBillableExpenseRepository.GetEnabledExpenseAccounts(siteId).Returns(new List<string>());
            _mockBillableExpenseRepository.GetBillableExpenseBudget(siteId, 2025, 5).Returns(1000m);
            _mockBillableExpenseRepository.GetBillableExpenseBudget(siteId, 2025, 6).Returns(1000m);
            _mockBillableExpenseRepository.GetOtherExpenseBudget(siteId, 2025, 5).Returns(600m);
            _mockBillableExpenseRepository.GetOtherExpenseBudget(siteId, 2025, 6).Returns(600m);
            
            // Mock forecast data for both months
            var mockExpenseDetail1 = new TownePark.bs_OtherExpenseDetail
            {
                bs_MonthYear = "2025-05",
                bs_EmployeeRelations = 150m
            };
            var mockExpenseDetail2 = new TownePark.bs_OtherExpenseDetail
            {
                bs_MonthYear = "2025-06",
                bs_FuelVehicles = 250m
            };
            _mockOtherExpenseRepository.GetOtherExpenseDetail(siteId, "2025-01")
                .Returns(new List<TownePark.bs_OtherExpenseDetail> { mockExpenseDetail1, mockExpenseDetail2 });

            // Act - Call twice for same site/year but different months
            _calculator.CalculateAndApply(siteData, 2025, 5, 5, monthValueDto, siteDetailDto1, 0m, new List<PnlRowDto>());
            _calculator.CalculateAndApply(siteData, 2025, 6, 6, monthValueDto, siteDetailDto2, 0m, new List<PnlRowDto>());

            // Assert - Repository should only be called once due to caching
            _mockOtherExpenseRepository.Received(1).GetOtherExpenseDetail(siteId, "2025-01");
            
            // Both calculations should use correct data for their respective months
            var expenseAccounts1 = siteDetailDto1.InternalRevenueBreakdown?.BillableAccounts?.ExpenseAccounts;
            var expenseAccounts2 = siteDetailDto2.InternalRevenueBreakdown?.BillableAccounts?.ExpenseAccounts;
            
            Assert.NotNull(expenseAccounts1);
            Assert.NotNull(expenseAccounts2);
            Assert.Equal(1150m, expenseAccounts1.Total); // 1000 + 150 (employee relations)
            Assert.Equal(1250m, expenseAccounts2.Total); // 1000 + 250 (fuel vehicles)
        }

        [Fact]
        public void CalculateAndApply_WithEnabledAccountsFiltering_OnlyIncludesEnabledForecastedAccounts()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var siteData = CreateTestSiteDataWithBillingAccount(siteId);
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();

            // Mock enabled accounts - only Employee Relations (7045) and Loss & Damage (7100) are enabled
            var enabledAccounts = new List<string> { "7045", "7100" };
            _mockBillableExpenseRepository.GetEnabledExpenseAccounts(siteId).Returns(enabledAccounts);
            _mockBillableExpenseRepository.GetBillableExpenseBudget(siteId, 2025, 5).Returns(1000m);
            _mockBillableExpenseRepository.GetOtherExpenseBudget(siteId, 2025, 5).Returns(600m);
            
            // Mock forecast data - multiple accounts have data but only enabled ones should be included
            var mockExpenseDetail = new TownePark.bs_OtherExpenseDetail
            {
                bs_MonthYear = "2025-05",
                bs_EmployeeRelations = 150m,    // 7045 - enabled, should be included
                bs_FuelVehicles = 200m,         // 7075 - disabled, should be excluded
                bs_LossAndDamageClaims = 100m   // 7100 - enabled, should be included
            };
            _mockOtherExpenseRepository.GetOtherExpenseDetail(siteId, "2025-01")
                .Returns(new List<TownePark.bs_OtherExpenseDetail> { mockExpenseDetail });

            // Act
            _calculator.CalculateAndApply(siteData, 2025, 5, 5, monthValueDto, siteDetailDto, 0m, new List<PnlRowDto>());

            // Assert
            var expenseAccounts = siteDetailDto.InternalRevenueBreakdown?.BillableAccounts?.ExpenseAccounts;
            Assert.NotNull(expenseAccounts);
            // 1000 (non-forecasted) + 150 (employee relations - enabled) + 100 (loss & damage - enabled) + 0 (fuel vehicles - disabled) = 1250
            Assert.Equal(1250m, expenseAccounts.Total);
        }

        [Fact]
        public void CalculateAndApply_WithEnabledAccountsFiltering_AllForecastedAccountsDisabled()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var siteData = CreateTestSiteDataWithBillingAccount(siteId);
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();

            // Mock enabled accounts - only non-forecasted accounts are enabled (none of the 12 forecasted accounts)
            var enabledAccounts = new List<string> { "7001", "7002" }; // Different account codes not in forecasted list
            _mockBillableExpenseRepository.GetEnabledExpenseAccounts(siteId).Returns(enabledAccounts);
            _mockBillableExpenseRepository.GetBillableExpenseBudget(siteId, 2025, 5).Returns(1000m);
            _mockBillableExpenseRepository.GetOtherExpenseBudget(siteId, 2025, 5).Returns(600m);
            
            // Mock forecast data - multiple accounts have data but all should be excluded
            var mockExpenseDetail = new TownePark.bs_OtherExpenseDetail
            {
                bs_MonthYear = "2025-05",
                bs_EmployeeRelations = 150m,    // 7045 - disabled, should be excluded
                bs_FuelVehicles = 200m,         // 7075 - disabled, should be excluded
                bs_LossAndDamageClaims = 100m   // 7100 - disabled, should be excluded
            };
            _mockOtherExpenseRepository.GetOtherExpenseDetail(siteId, "2025-01")
                .Returns(new List<TownePark.bs_OtherExpenseDetail> { mockExpenseDetail });

            // Act
            _calculator.CalculateAndApply(siteData, 2025, 5, 5, monthValueDto, siteDetailDto, 0m, new List<PnlRowDto>());

            // Assert
            var expenseAccounts = siteDetailDto.InternalRevenueBreakdown?.BillableAccounts?.ExpenseAccounts;
            Assert.NotNull(expenseAccounts);
            // 1000 (non-forecasted) + 0 (all forecasted accounts disabled) = 1000
            Assert.Equal(1000m, expenseAccounts.Total);
        }

        private static InternalRevenueDataVo CreateTestSiteDataWithoutBillingAccount()
        {
            return new InternalRevenueDataVo
            {
                SiteId = Guid.NewGuid(),
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.ManagementAgreement }
                }
            };
        }

        private static InternalRevenueDataVo CreateTestSiteDataWithBillingAccount(Guid siteId)
        {
            return new InternalRevenueDataVo
            {
                SiteId = siteId,
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.BillingAccount }
                }
            };
        }

        private static SiteMonthlyRevenueDetailDto CreateSiteDetailWithExpenseAccounts(decimal? total)
        {
            return new SiteMonthlyRevenueDetailDto
            {
                InternalRevenueBreakdown = new InternalRevenueBreakdownDto
                {
                    BillableAccounts = new BillableAccountsInternalRevenueDto
                    {
                        ExpenseAccounts = new ExpenseAccountsInternalRevenueDto
                        {
                            Total = total
                        }
                    }
                }
            };
        }
    }
}