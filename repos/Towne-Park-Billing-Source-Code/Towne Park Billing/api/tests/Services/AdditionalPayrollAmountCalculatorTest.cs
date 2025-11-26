using System;
using System.Collections.Generic;
using System.Linq;
using TownePark.Models.Vo;
using api.Models.Dto;
using api.Services.Impl.Calculators;
using Xunit;
using TownePark;

namespace BackendTests.Services
{
    public class AdditionalPayrollAmountCalculatorTest
    {
        private readonly AdditionalPayrollAmountCalculator _calculator;

        public AdditionalPayrollAmountCalculatorTest()
        {
            _calculator = new AdditionalPayrollAmountCalculator();
        }

        private InternalRevenueDataVo CreateTestSiteDataWithContract(List<BillableAccountVo> billableAccounts)
        {
            return new InternalRevenueDataVo
            {
                Contract = new ContractDataVo
                {
                    ContractId = Guid.NewGuid(),
                    ContractTypes = new[] { bs_contracttypechoices.BillingAccount }
                },
                BillableAccounts = billableAccounts
            };
        }

        [Fact]
        public void CalculateAndApply_WithValidAdditionalPayrollAmount_CalculatesCorrectly()
        {
            // Arrange
            var siteData = CreateTestSiteDataWithContract(new List<BillableAccountVo>
            {
                new BillableAccountVo
                {
                    Id = Guid.NewGuid(),
                    AccountCode = "TEST001",
                    Amount = 5000.00m,
                    IsExcluded = false
                }
            });
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();

            // Act
            _calculator.CalculateAndApply(siteData, 2025, 5, 5, monthValueDto, siteDetailDto, 0m, new List<PnlRowDto>());

            // Assert
            var billableAccounts = siteDetailDto.InternalRevenueBreakdown?.BillableAccounts;
            Assert.NotNull(billableAccounts);
            Assert.Equal(5000.00m, billableAccounts.Total);
        }

        [Fact]
        public void CalculateAndApply_WithZeroAdditionalPayrollAmount_CalculatesCorrectly()
        {
            // Arrange
            var siteData = CreateTestSiteDataWithContract(new List<BillableAccountVo>
            {
                new BillableAccountVo
                {
                    Id = Guid.NewGuid(),
                    AccountCode = "TEST002",
                    Amount = 0m,
                    IsExcluded = false
                }
            });
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();

            // Act
            _calculator.CalculateAndApply(siteData, 2025, 5, 5, monthValueDto, siteDetailDto, 0m, new List<PnlRowDto>());

            // Assert
            var billableAccounts = siteDetailDto.InternalRevenueBreakdown?.BillableAccounts;
            Assert.NotNull(billableAccounts);
            Assert.Equal(0m, billableAccounts.Total);
        }

        [Fact]
        public void CalculateAndApply_WithExcludedBillableAccount_DoesNotCalculate()
        {
            // Arrange
            var siteData = CreateTestSiteDataWithContract(new List<BillableAccountVo>
            {
                new BillableAccountVo
                {
                    Id = Guid.NewGuid(),
                    AccountCode = "TEST003",
                    Amount = 5000.00m,
                    IsExcluded = true // Excluded account
                }
            });
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();

            // Act
            _calculator.CalculateAndApply(siteData, 2025, 5, 5, monthValueDto, siteDetailDto, 0m, new List<PnlRowDto>());

            // Assert
            var billableAccounts = siteDetailDto.InternalRevenueBreakdown?.BillableAccounts;
            Assert.NotNull(billableAccounts);
            Assert.Equal(0m, billableAccounts.Total); // Should be 0 since account is excluded
        }

        [Fact]
        public void CalculateAndApply_WithNoBillableAccounts_DoesNotCalculate()
        {
            // Arrange
            var siteData = CreateTestSiteDataWithContract(new List<BillableAccountVo>()); // Empty list
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();

            // Act
            _calculator.CalculateAndApply(siteData, 2025, 5, 5, monthValueDto, siteDetailDto, 0m, new List<PnlRowDto>());

            // Assert
            var billableAccounts = siteDetailDto.InternalRevenueBreakdown?.BillableAccounts;
            Assert.NotNull(billableAccounts);
            Assert.Equal(0m, billableAccounts.Total); // Should be 0 when no accounts
        }

        [Fact]
        public void CalculateAndApply_WithNullBillableAccounts_DoesNotCalculate()
        {
            // Arrange
            var siteData = new InternalRevenueDataVo
            {
                Contract = new ContractDataVo
                {
                    ContractId = Guid.NewGuid(),
                    ContractTypes = new[] { bs_contracttypechoices.BillingAccount }
                },
                BillableAccounts = null // Null BillableAccounts
            };
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();

            // Act
            _calculator.CalculateAndApply(siteData, 2025, 5, 5, monthValueDto, siteDetailDto, 0m, new List<PnlRowDto>());

            // Assert
            var billableAccounts = siteDetailDto.InternalRevenueBreakdown?.BillableAccounts;
            Assert.NotNull(billableAccounts);
            Assert.Equal(0m, billableAccounts.Total); // Should be 0 when null
        }

        [Fact]
        public void CalculateAndApply_WithoutBillingAccountContractType_DoesNotCalculate()
        {
            // Arrange
            var siteData = new InternalRevenueDataVo
            {
                Contract = new ContractDataVo
                {
                    ContractId = Guid.NewGuid(),
                    ContractTypes = new[] { bs_contracttypechoices.FixedFee } // No BillingAccount
                },
                BillableAccounts = new List<BillableAccountVo>
                {
                    new BillableAccountVo
                    {
                        Id = Guid.NewGuid(),
                        AccountCode = "TEST009",
                        Amount = 5000.00m,
                        IsExcluded = false
                    }
                }
            };
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();

            // Act
            _calculator.CalculateAndApply(siteData, 2025, 5, 5, monthValueDto, siteDetailDto, 0m, new List<PnlRowDto>());

            // Assert
            var billableAccounts = siteDetailDto.InternalRevenueBreakdown?.BillableAccounts;
            Assert.NotNull(billableAccounts);
            Assert.Equal(0m, billableAccounts.Total); // Should be 0 when contract doesn't have BillingAccount type
        }

        [Fact]
        public void CalculateAndApply_WithLargeAdditionalPayrollAmount_CalculatesCorrectly()
        {
            // Arrange
            var siteData = CreateTestSiteDataWithContract(new List<BillableAccountVo>
            {
                new BillableAccountVo
                {
                    Id = Guid.NewGuid(),
                    AccountCode = "TEST004",
                    Amount = 999999.99m,
                    IsExcluded = false
                }
            });
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();

            // Act
            _calculator.CalculateAndApply(siteData, 2025, 5, 5, monthValueDto, siteDetailDto, 0m, new List<PnlRowDto>());

            // Assert
            var billableAccounts = siteDetailDto.InternalRevenueBreakdown?.BillableAccounts;
            Assert.NotNull(billableAccounts);
            Assert.Equal(999999.99m, billableAccounts.Total);
        }

        [Fact]
        public void CalculateAndApply_WithNegativeAdditionalPayrollAmount_CalculatesCorrectly()
        {
            // Arrange
            var siteData = CreateTestSiteDataWithContract(new List<BillableAccountVo>
            {
                new BillableAccountVo
                {
                    Id = Guid.NewGuid(),
                    AccountCode = "TEST005",
                    Amount = -2500.00m,
                    IsExcluded = false
                }
            });
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();

            // Act
            _calculator.CalculateAndApply(siteData, 2025, 5, 5, monthValueDto, siteDetailDto, 0m, new List<PnlRowDto>());

            // Assert
            var billableAccounts = siteDetailDto.InternalRevenueBreakdown?.BillableAccounts;
            Assert.NotNull(billableAccounts);
            Assert.Equal(-2500.00m, billableAccounts.Total);
        }

        [Fact]
        public void CalculateAndApply_WithExistingInternalRevenueBreakdown_PreservesOtherComponents()
        {
            // Arrange
            var siteData = CreateTestSiteDataWithContract(new List<BillableAccountVo>
            {
                new BillableAccountVo
                {
                    Id = Guid.NewGuid(),
                    AccountCode = "TEST006",
                    Amount = 3000.00m,
                    IsExcluded = false
                }
            });
            var siteDetailDto = new SiteMonthlyRevenueDetailDto
            {
                InternalRevenueBreakdown = new InternalRevenueBreakdownDto
                {
                    FixedFee = new FixedFeeInternalRevenueDto { Total = 1000m },
                    PerOccupiedRoom = new PerOccupiedRoomInternalRevenueDto { Total = 2000m }
                }
            };
            var monthValueDto = new MonthValueDto();

            // Act
            _calculator.CalculateAndApply(siteData, 2025, 5, 5, monthValueDto, siteDetailDto, 0m, new List<PnlRowDto>());

            // Assert
            var breakdown = siteDetailDto.InternalRevenueBreakdown;
            Assert.NotNull(breakdown);
            Assert.Equal(1000m, breakdown.FixedFee?.Total); // Preserved
            Assert.Equal(2000m, breakdown.PerOccupiedRoom?.Total); // Preserved
            Assert.Equal(3000m, breakdown.BillableAccounts?.Total); // Added
            Assert.Equal(6000m, breakdown.CalculatedTotalInternalRevenue); // Updated total
        }

        [Fact]
        public void CalculateAndApply_WithDecimalPrecision_MaintainsPrecision()
        {
            // Arrange
            var siteData = CreateTestSiteDataWithContract(new List<BillableAccountVo>
            {
                new BillableAccountVo
                {
                    Id = Guid.NewGuid(),
                    AccountCode = "TEST007",
                    Amount = 1234.567m,
                    IsExcluded = false
                }
            });
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();

            // Act
            _calculator.CalculateAndApply(siteData, 2025, 5, 5, monthValueDto, siteDetailDto, 0m, new List<PnlRowDto>());

            // Assert
            var billableAccounts = siteDetailDto.InternalRevenueBreakdown?.BillableAccounts;
            Assert.NotNull(billableAccounts);
            Assert.Equal(1234.567m, billableAccounts.Total);
        }

        [Fact]
        public void CalculateAndApply_WithMultipleBillableAccounts_SumsAllNonExcluded()
        {
            // Arrange
            var siteData = CreateTestSiteDataWithContract(new List<BillableAccountVo>
            {
                new BillableAccountVo
                {
                    Id = Guid.NewGuid(),
                    AccountCode = "TEST008A",
                    Amount = 1000m,
                    IsExcluded = false
                },
                new BillableAccountVo
                {
                    Id = Guid.NewGuid(),
                    AccountCode = "TEST008B",
                    Amount = 2000m,
                    IsExcluded = false
                },
                new BillableAccountVo
                {
                    Id = Guid.NewGuid(),
                    AccountCode = "TEST008C",
                    Amount = 3000m,
                    IsExcluded = true // This should be excluded
                }
            });
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();

            // Act
            _calculator.CalculateAndApply(siteData, 2025, 5, 5, monthValueDto, siteDetailDto, 0m, new List<PnlRowDto>());

            // Assert
            var billableAccounts = siteDetailDto.InternalRevenueBreakdown?.BillableAccounts;
            Assert.NotNull(billableAccounts);
            Assert.Equal(3000m, billableAccounts.Total); // 1000 + 2000, excluding the 3000 that's marked as excluded
        }

        [Fact]
        public void AggregateMonthlyTotals_WithMultipleSites_AggregatesCorrectly()
        {
            // Arrange
            var siteDetails = new List<SiteMonthlyRevenueDetailDto>
            {
                new SiteMonthlyRevenueDetailDto
                {
                    InternalRevenueBreakdown = new InternalRevenueBreakdownDto
                    {
                        BillableAccounts = new BillableAccountsInternalRevenueDto 
                        { 
                            AdditionalPayrollAmount = 1000m,
                            Total = 1000m 
                        }
                    }
                },
                new SiteMonthlyRevenueDetailDto
                {
                    InternalRevenueBreakdown = new InternalRevenueBreakdownDto
                    {
                        BillableAccounts = new BillableAccountsInternalRevenueDto 
                        { 
                            AdditionalPayrollAmount = 2500m,
                            Total = 2500m 
                        }
                    }
                },
                new SiteMonthlyRevenueDetailDto
                {
                    InternalRevenueBreakdown = new InternalRevenueBreakdownDto
                    {
                        BillableAccounts = new BillableAccountsInternalRevenueDto 
                        { 
                            AdditionalPayrollAmount = 1500m,
                            Total = 1500m 
                        }
                    }
                }
            };
            var monthValueDto = new MonthValueDto();

            // Act
            _calculator.AggregateMonthlyTotals(siteDetails, monthValueDto);

            // Assert
            var billableAccounts = monthValueDto.InternalRevenueBreakdown?.BillableAccounts;
            Assert.NotNull(billableAccounts);
            Assert.Equal(5000m, billableAccounts.AdditionalPayrollAmount); // 1000 + 2500 + 1500 - specific field
            Assert.Equal(5000m, billableAccounts.Total); // 1000 + 2500 + 1500 - total still works
        }

        [Fact]
        public void AggregateMonthlyTotals_WithNullBillableAccounts_HandlesGracefully()
        {
            // Arrange
            var siteDetails = new List<SiteMonthlyRevenueDetailDto>
            {
                new SiteMonthlyRevenueDetailDto
                {
                    InternalRevenueBreakdown = new InternalRevenueBreakdownDto
                    {
                        BillableAccounts = new BillableAccountsInternalRevenueDto 
                        { 
                            AdditionalPayrollAmount = 1000m,
                            Total = 1000m 
                        }
                    }
                },
                new SiteMonthlyRevenueDetailDto
                {
                    InternalRevenueBreakdown = new InternalRevenueBreakdownDto
                    {
                        BillableAccounts = null // Null BillableAccounts
                    }
                },
                new SiteMonthlyRevenueDetailDto
                {
                    InternalRevenueBreakdown = null // Null breakdown
                }
            };
            var monthValueDto = new MonthValueDto();

            // Act
            _calculator.AggregateMonthlyTotals(siteDetails, monthValueDto);

            // Assert
            var billableAccounts = monthValueDto.InternalRevenueBreakdown?.BillableAccounts;
            Assert.NotNull(billableAccounts);
            Assert.Equal(1000m, billableAccounts.AdditionalPayrollAmount); // Only the first site's amount - specific field
            Assert.Equal(1000m, billableAccounts.Total); // Only the first site's amount - total
        }
    }
} 