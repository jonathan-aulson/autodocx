using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using NSubstitute;
using FluentAssertions;
using api.Services.Impl.Calculators;
using api.Models.Dto;
using api.Models.Vo;
using TownePark.Models.Vo;
using Microsoft.Extensions.Logging;
using TownePark;
using TownePark.Data;
using api.Data;

namespace BackendTests.Services
{
    public class ClaimsCalculatorTests
    {
        private readonly IInternalRevenueRepository _internalRevenueRepository;
        private readonly IBillableExpenseRepository _billableExpenseRepository;
        private readonly ILogger<ClaimsCalculator> _logger;

        public ClaimsCalculatorTests()
        {
            _internalRevenueRepository = Substitute.For<IInternalRevenueRepository>();
            _billableExpenseRepository = Substitute.For<IBillableExpenseRepository>();
            _logger = Substitute.For<ILogger<ClaimsCalculator>>();
        }

        private ClaimsCalculator CreateCalculator()
        {
            return new ClaimsCalculator(_internalRevenueRepository, _billableExpenseRepository, _logger);
        }

        [Fact]
        public async Task CalculateAndApplyAsync_WithClaimsDisabled_ShouldReturnEarly()
        {
            var calc = CreateCalculator();
            var siteData = new InternalRevenueDataVo
            {
                SiteId = Guid.NewGuid(),
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.ManagementAgreement }
                },
                ManagementAgreement = new ManagementAgreementVo
                {
                    ClaimsEnabled = false
                }
            };
            var dto = new SiteMonthlyRevenueDetailDto();
            await calc.CalculateAndApplyAsync(siteData, 2025, 7, 7, new MonthValueDto(), dto, 0, new List<PnlRowDto>());
            dto.InternalRevenueBreakdown?.ManagementAgreement?.CalculatedClaims.Should().BeNull();
        }

        [Fact]
        public async Task CalculateAndApplyAsync_AnnualCalendar_UnderCap()
        {
            var calc = CreateCalculator();
            var siteData = new InternalRevenueDataVo
            {
                SiteId = Guid.NewGuid(),
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.ManagementAgreement }
                },
                ManagementAgreement = new ManagementAgreementVo
                {
                    ClaimsEnabled = true,
                    ClaimsType = bs_claimtype.AnnualCalendar,
                    ClaimsCapAmount = 1000m
                }
            };

            // Mock repository to return claims sum for year
            _billableExpenseRepository
                .GetClaimsBudgetForPeriodRange(siteData.SiteId, "202501", "202507")
                .Returns(500m);

            var dto = new SiteMonthlyRevenueDetailDto();
            await calc.CalculateAndApplyAsync(siteData, 2025, 7, 7, new MonthValueDto(), dto, 0, new List<PnlRowDto>());

            dto.InternalRevenueBreakdown.ManagementAgreement.CalculatedClaims.Should().Be(500m);
            
            // Verify repository was called correctly
            _billableExpenseRepository
                .Received(1)
                .GetClaimsBudgetForPeriodRange(siteData.SiteId, "202501", "202507");
        }

        [Fact]
        public async Task CalculateAndApplyAsync_AnnualCalendar_OverCap()
        {
            var calc = CreateCalculator();
            var siteData = new InternalRevenueDataVo
            {
                SiteId = Guid.NewGuid(),
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.ManagementAgreement }
                },
                ManagementAgreement = new ManagementAgreementVo
                {
                    ClaimsEnabled = true,
                    ClaimsType = bs_claimtype.AnnualCalendar,
                    ClaimsCapAmount = 400m
                }
            };

            // Mock repository to return claims sum that exceeds cap
            _billableExpenseRepository
                .GetClaimsBudgetForPeriodRange(siteData.SiteId, "202501", "202507")
                .Returns(550m);

            var dto = new SiteMonthlyRevenueDetailDto();
            await calc.CalculateAndApplyAsync(siteData, 2025, 7, 7, new MonthValueDto(), dto, 0, new List<PnlRowDto>());

            dto.InternalRevenueBreakdown.ManagementAgreement.CalculatedClaims.Should().Be(400m); // Capped
        }

        [Fact]
        public async Task CalculateAndApplyAsync_AnnualAnniversary_BeforeAnniversaryDate()
        {
            var calc = CreateCalculator();
            var anniversaryDate = new DateTime(2025, 3, 15); // March 15
            var siteData = new InternalRevenueDataVo
            {
                SiteId = Guid.NewGuid(),
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.ManagementAgreement }
                },
                ManagementAgreement = new ManagementAgreementVo
                {
                    ClaimsEnabled = true,
                    ClaimsType = bs_claimtype.AnnualAnniversary,
                    ClaimsCapAmount = 1000m,
                    AnniversaryDate = anniversaryDate
                }
            };

            // Mock repository to return claims for March-July 2025
            _billableExpenseRepository
                .GetClaimsBudgetForPeriodRange(siteData.SiteId, "202503", "202507")
                .Returns(700m);

            var dto = new SiteMonthlyRevenueDetailDto();
            await calc.CalculateAndApplyAsync(siteData, 2025, 7, 7, new MonthValueDto(), dto, 0, new List<PnlRowDto>());

            dto.InternalRevenueBreakdown.ManagementAgreement.CalculatedClaims.Should().Be(700m);
            
            // Verify correct period range was used
            _billableExpenseRepository
                .Received(1)
                .GetClaimsBudgetForPeriodRange(siteData.SiteId, "202503", "202507");
        }

        [Fact]
        public async Task CalculateAndApplyAsync_AnnualAnniversary_AfterAnniversaryDate()
        {
            var calc = CreateCalculator();
            var anniversaryDate = new DateTime(2025, 9, 1); // September 1
            var siteData = new InternalRevenueDataVo
            {
                SiteId = Guid.NewGuid(),
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.ManagementAgreement }
                },
                ManagementAgreement = new ManagementAgreementVo
                {
                    ClaimsEnabled = true,
                    ClaimsType = bs_claimtype.AnnualAnniversary,
                    ClaimsCapAmount = 1000m,
                    AnniversaryDate = anniversaryDate
                }
            };

            // Mock repository to return claims for Sept 2024 - July 2025
            _billableExpenseRepository
                .GetClaimsBudgetForPeriodRange(siteData.SiteId, "202409", "202507")
                .Returns(1200m);

            var dto = new SiteMonthlyRevenueDetailDto();
            await calc.CalculateAndApplyAsync(siteData, 2025, 7, 7, new MonthValueDto(), dto, 0, new List<PnlRowDto>());

            dto.InternalRevenueBreakdown.ManagementAgreement.CalculatedClaims.Should().Be(1000m); // Capped
            
            // Verify correct period range was used (previous year)
            _billableExpenseRepository
                .Received(1)
                .GetClaimsBudgetForPeriodRange(siteData.SiteId, "202409", "202507");
        }

        [Fact]
        public async Task CalculateAndApplyAsync_PerClaim_CurrentMonthOnly()
        {
            var calc = CreateCalculator();
            var siteData = new InternalRevenueDataVo
            {
                SiteId = Guid.NewGuid(),
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.ManagementAgreement }
                },
                ManagementAgreement = new ManagementAgreementVo
                {
                    ClaimsEnabled = true,
                    ClaimsType = bs_claimtype.PerClaim
                }
            };

            // Mock repository to return claims for current month only
            _billableExpenseRepository
                .GetClaimsBudgetForPeriod(siteData.SiteId, "202507")
                .Returns(300m);

            var dto = new SiteMonthlyRevenueDetailDto();
            await calc.CalculateAndApplyAsync(siteData, 2025, 7, 7, new MonthValueDto(), dto, 0, new List<PnlRowDto>());

            dto.InternalRevenueBreakdown.ManagementAgreement.CalculatedClaims.Should().Be(300m);
            
            // Verify only current period was requested
            _billableExpenseRepository
                .Received(1)
                .GetClaimsBudgetForPeriod(siteData.SiteId, "202507");
        }

        [Fact]
        public async Task CalculateAndApplyAsync_WithNullClaimsConfig_ShouldReturnEarly()
        {
            var calc = CreateCalculator();
            var siteData = new InternalRevenueDataVo
            {
                SiteId = Guid.NewGuid(),
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.ManagementAgreement }
                },
                ManagementAgreement = new ManagementAgreementVo
                {
                    ClaimsEnabled = true,
                    ClaimsType = null // Null claims type
                }
            };
            var dto = new SiteMonthlyRevenueDetailDto();
            await calc.CalculateAndApplyAsync(siteData, 2025, 7, 7, new MonthValueDto(), dto, 0, new List<PnlRowDto>());
            dto.InternalRevenueBreakdown?.ManagementAgreement?.CalculatedClaims.Should().BeNull();
        }

        [Fact]
        public async Task CalculateAndApplyAsync_AnnualCalendar_AtCap_ShouldReturnCap()
        {
            var calc = CreateCalculator();
            var siteData = new InternalRevenueDataVo
            {
                SiteId = Guid.NewGuid(),
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.ManagementAgreement }
                },
                ManagementAgreement = new ManagementAgreementVo
                {
                    ClaimsEnabled = true,
                    ClaimsType = bs_claimtype.AnnualCalendar,
                    ClaimsCapAmount = 500m // Exactly at cap
                }
            };

            // Mock repository to return exactly the cap amount
            _billableExpenseRepository
                .GetClaimsBudgetForPeriodRange(siteData.SiteId, "202501", "202507")
                .Returns(500m);

            var dto = new SiteMonthlyRevenueDetailDto();
            await calc.CalculateAndApplyAsync(siteData, 2025, 7, 7, new MonthValueDto(), dto, 0, new List<PnlRowDto>());
            dto.InternalRevenueBreakdown.ManagementAgreement.CalculatedClaims.Should().Be(500m);
        }

        [Fact]
        public async Task CalculateAndApplyAsync_AnnualAnniversary_EdgeCase_SameMonth()
        {
            var calc = CreateCalculator();
            var anniversaryDate = new DateTime(2025, 7, 15); // July 15 (same month as calculation)
            var siteData = new InternalRevenueDataVo
            {
                SiteId = Guid.NewGuid(),
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.ManagementAgreement }
                },
                ManagementAgreement = new ManagementAgreementVo
                {
                    ClaimsEnabled = true,
                    ClaimsType = bs_claimtype.AnnualAnniversary,
                    ClaimsCapAmount = 1000m,
                    AnniversaryDate = anniversaryDate
                }
            };

            // Mock repository to return claims for July 2025 only
            _billableExpenseRepository
                .GetClaimsBudgetForPeriodRange(siteData.SiteId, "202507", "202507")
                .Returns(400m);

            var dto = new SiteMonthlyRevenueDetailDto();
            await calc.CalculateAndApplyAsync(siteData, 2025, 7, 7, new MonthValueDto(), dto, 0, new List<PnlRowDto>());

            dto.InternalRevenueBreakdown.ManagementAgreement.CalculatedClaims.Should().Be(400m);
            
            // Verify correct period range was used (same month)
            _billableExpenseRepository
                .Received(1)
                .GetClaimsBudgetForPeriodRange(siteData.SiteId, "202507", "202507");
        }

        [Fact]
        public async Task AggregateMonthlyTotalsAsync_ShouldSumAllSiteClaims()
        {
            var calc = CreateCalculator();
            var siteDetails = new List<SiteMonthlyRevenueDetailDto>
            {
                new SiteMonthlyRevenueDetailDto
                {
                    InternalRevenueBreakdown = new InternalRevenueBreakdownDto
                    {
                        ManagementAgreement = new ManagementAgreementInternalRevenueDto
                        {
                            CalculatedClaims = 100m
                        }
                    }
                },
                new SiteMonthlyRevenueDetailDto
                {
                    InternalRevenueBreakdown = new InternalRevenueBreakdownDto
                    {
                        ManagementAgreement = new ManagementAgreementInternalRevenueDto
                        {
                            CalculatedClaims = 200m
                        }
                    }
                },
                new SiteMonthlyRevenueDetailDto
                {
                    InternalRevenueBreakdown = new InternalRevenueBreakdownDto
                    {
                        ManagementAgreement = new ManagementAgreementInternalRevenueDto
                        {
                            CalculatedClaims = 300m
                        }
                    }
                }
            };

            var monthValueDto = new MonthValueDto();
            await calc.AggregateMonthlyTotalsAsync(siteDetails, monthValueDto);
            
            monthValueDto.InternalRevenueBreakdown.ManagementAgreement.CalculatedClaims.Should().Be(600m);
        }

        [Fact]
        public async Task AggregateMonthlyTotalsAsync_WithEmptyList_ShouldReturnZero()
        {
            var calc = CreateCalculator();
            var siteDetails = new List<SiteMonthlyRevenueDetailDto>();
            var monthValueDto = new MonthValueDto();
            
            await calc.AggregateMonthlyTotalsAsync(siteDetails, monthValueDto);
            
            monthValueDto.InternalRevenueBreakdown.ManagementAgreement.CalculatedClaims.Should().Be(0m);
        }

        [Fact]
        public void Order_ShouldReturn3()
        {
            var calc = CreateCalculator();
            calc.Order.Should().Be(3);
        }

        [Fact]
        public async Task CalculateAndApplyAsync_CurrentMonth_WithActuals_SumsActualsOnly_IgnoresForecast()
        {
            var calc = CreateCalculator();

            var year = DateTime.Today.Year;
            var month = DateTime.Today.Month; // ensure isCurrentMonth path
            var period = $"{year}{month:D2}";

            var siteData = new InternalRevenueDataVo
            {
                SiteId = Guid.NewGuid(),
                SiteNumber = "0170",
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.ManagementAgreement }
                },
                ManagementAgreement = new ManagementAgreementVo
                {
                    ClaimsEnabled = true,
                    ClaimsType = bs_claimtype.AnnualCalendar
                }
            };

            // Internal actuals: include zeros and positives; only positives count
            var dto = new SiteMonthlyRevenueDetailDto
            {
                InternalActuals = new InternalRevenueActualsVo
                {
                    DailyActuals = new List<DailyActualVo>
                    {
                        new DailyActualVo { Date = new DateTime(year, month, 1).ToString("yyyy-MM-dd"), Claims = 0m },
                        new DailyActualVo { Date = new DateTime(year, month, 2).ToString("yyyy-MM-dd"), Claims = 10m },
                        new DailyActualVo { Date = new DateTime(year, month, 3).ToString("yyyy-MM-dd"), Claims = 15m },
                        // Different month should be ignored
                        new DailyActualVo { Date = new DateTime(year, month == 1 ? 12 : month - 1, 28).ToString("yyyy-MM-dd"), Claims = 99m }
                    }
                }
            };

            // Stub a forecast to ensure it is NOT used when actuals exist
            _billableExpenseRepository
                .GetClaimsBudgetForPeriod(siteData.SiteId, period)
                .Returns(9999m);

            await calc.CalculateAndApplyAsync(siteData, year, month, month, new MonthValueDto(), dto, 0, new List<PnlRowDto>());

            dto.InternalRevenueBreakdown.Should().NotBeNull();
            dto.InternalRevenueBreakdown!.ManagementAgreement!.CalculatedClaims.Should().Be(25m);

            // Verify forecast was not called when actuals exist
            _billableExpenseRepository
                .DidNotReceive()
                .GetClaimsBudgetForPeriod(siteData.SiteId, period);
        }

        [Fact]
        public async Task CalculateAndApplyAsync_CurrentMonth_WithNoActuals_UsesFullForecast()
        {
            var calc = CreateCalculator();

            var year = DateTime.Today.Year;
            var month = DateTime.Today.Month; // ensure isCurrentMonth path
            var period = $"{year}{month:D2}";

            var siteData = new InternalRevenueDataVo
            {
                SiteId = Guid.NewGuid(),
                SiteNumber = "0170",
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.ManagementAgreement }
                },
                ManagementAgreement = new ManagementAgreementVo
                {
                    ClaimsEnabled = true,
                    ClaimsType = bs_claimtype.AnnualCalendar
                }
            };

            // Internal actuals: all zeros (treated as no actuals > 0)
            var dto = new SiteMonthlyRevenueDetailDto
            {
                InternalActuals = new InternalRevenueActualsVo
                {
                    DailyActuals = new List<DailyActualVo>
                    {
                        new DailyActualVo { Date = new DateTime(year, month, 1).ToString("yyyy-MM-dd"), Claims = 0m },
                        new DailyActualVo { Date = new DateTime(year, month, 2).ToString("yyyy-MM-dd"), Claims = 0m }
                    }
                }
            };

            _billableExpenseRepository
                .GetClaimsBudgetForPeriod(siteData.SiteId, period)
                .Returns(1234m);

            await calc.CalculateAndApplyAsync(siteData, year, month, month, new MonthValueDto(), dto, 0, new List<PnlRowDto>());

            dto.InternalRevenueBreakdown.Should().NotBeNull();
            dto.InternalRevenueBreakdown!.ManagementAgreement!.CalculatedClaims.Should().Be(1234m);

            _billableExpenseRepository
                .Received(1)
                .GetClaimsBudgetForPeriod(siteData.SiteId, period);
        }
    }
}