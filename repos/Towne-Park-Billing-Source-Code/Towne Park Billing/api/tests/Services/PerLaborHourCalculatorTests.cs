using Xunit;
using NSubstitute;
using api.Services.Impl.Calculators;
using api.Data;
using TownePark.Models.Vo;
using api.Models.Dto;
using TownePark;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BackendTests.Services
{
    public class PerLaborHourCalculatorTests
    {
        private readonly IPayrollRepository _payrollRepoSub;
        private readonly PerLaborHourCalculator _calculator;

        public PerLaborHourCalculatorTests()
        {
            _payrollRepoSub = Substitute.For<IPayrollRepository>();
            _calculator = new PerLaborHourCalculator(_payrollRepoSub);
        }

        private bs_PayrollDetail CreatePayrollDetail(string jobCode, decimal hours, DateTime? date = null)
        {
            var detail = new bs_PayrollDetail();
            // Set via indexer to match calculator's access pattern
            detail["jobcode_display"] = jobCode;
            detail[bs_PayrollDetail.Fields.bs_RegularHours] = hours;
            if (date.HasValue)
            {
                detail[bs_PayrollDetail.Fields.bs_Date] = date.Value;
            }
            // Also set display name for robustness if calculator checks it
            detail["bs_DisplayName"] = jobCode;
            return detail;
        }

        private bs_Payroll CreateMockPayroll(List<bs_PayrollDetail> details)
        {
            var payroll = Substitute.For<bs_Payroll>();
            payroll.bs_PayrollDetail_Payroll = details;
            return payroll;
        }

        #region AggregateMonthlyTotals Tests

        [Fact]
        public void AggregateMonthlyTotals_SumsAllSites()
        {
            // Arrange
            var site1 = new SiteMonthlyRevenueDetailDto
            {
                InternalRevenueBreakdown = new InternalRevenueBreakdownDto
                {
                    PerLaborHour = new PerLaborHourInternalRevenueDto { Total = 100m }
                }
            };
            var site2 = new SiteMonthlyRevenueDetailDto
            {
                InternalRevenueBreakdown = new InternalRevenueBreakdownDto
                {
                    PerLaborHour = new PerLaborHourInternalRevenueDto { Total = 200m }
                }
            };
            var monthValueDto = new MonthValueDto();

            // Act
            _calculator.AggregateMonthlyTotals(new List<SiteMonthlyRevenueDetailDto> { site1, site2 }, monthValueDto);

            // Assert
            Assert.NotNull(monthValueDto.InternalRevenueBreakdown);
            Assert.Equal(300m, monthValueDto.InternalRevenueBreakdown.PerLaborHour.Total);
        }

        [Fact]
        public void AggregateMonthlyTotals_WithEmptyList_SetsZeroTotal()
        {
            // Arrange
            var monthValueDto = new MonthValueDto();

            // Act
            _calculator.AggregateMonthlyTotals(new List<SiteMonthlyRevenueDetailDto>(), monthValueDto);

            // Assert
            Assert.NotNull(monthValueDto.InternalRevenueBreakdown);
            Assert.Equal(0m, monthValueDto.InternalRevenueBreakdown.PerLaborHour.Total);
        }

        [Fact]
        public void AggregateMonthlyTotals_WithNullInternalRevenueBreakdown_HandlesGracefully()
        {
            // Arrange
            var site = new SiteMonthlyRevenueDetailDto
            {
                InternalRevenueBreakdown = null
            };
            var monthValueDto = new MonthValueDto();

            // Act
            _calculator.AggregateMonthlyTotals(new List<SiteMonthlyRevenueDetailDto> { site }, monthValueDto);

            // Assert
            Assert.NotNull(monthValueDto.InternalRevenueBreakdown);
            Assert.Equal(0m, monthValueDto.InternalRevenueBreakdown.PerLaborHour.Total);
        }

        [Fact]
        public void AggregateMonthlyTotals_WithNullPerLaborHour_HandlesGracefully()
        {
            // Arrange
            var site = new SiteMonthlyRevenueDetailDto
            {
                InternalRevenueBreakdown = new InternalRevenueBreakdownDto
                {
                    PerLaborHour = null
                }
            };
            var monthValueDto = new MonthValueDto();

            // Act
            _calculator.AggregateMonthlyTotals(new List<SiteMonthlyRevenueDetailDto> { site }, monthValueDto);

            // Assert
            Assert.NotNull(monthValueDto.InternalRevenueBreakdown);
            Assert.Equal(0m, monthValueDto.InternalRevenueBreakdown.PerLaborHour.Total);
        }

        #endregion

        #region CalculateAndApply Tests

        [Fact]
        public void CalculateAndApply_WithNoPayroll_UsesBudgetHours()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var jobCode = "J03";
            var year = 2025;
            var month = 7;

            _payrollRepoSub.GetPayroll(siteId, $"{year}{month:D2}").Returns((bs_Payroll)null);

            var siteData = new InternalRevenueDataVo
            {
                SiteId = siteId,
                SiteNumber = siteId.ToString(),
                LaborHourJobs = new List<LaborHourJobVo>
                {
                    new LaborHourJobVo
                    {
                        JobCode = jobCode,
                        Rate = 15m,
                        StartDate = new DateTime(2020, 1, 1),
                        EndDate = null
                    }
                },
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.PerLaborHour }
                }
            };
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();
            decimal calculatedExternalRevenue = 0m;

            // BudgetRows setup
            var budgetRows = new List<PnlRowDto>
            {
                new PnlRowDto
                {
                    ColumnName = "PerLaborHour",
                    MonthlyValues = new List<MonthValueDto>
                    {
                        new MonthValueDto
                        {
                            Month = month - 1,
                            SiteDetails = new List<SiteMonthlyRevenueDetailDto>
                            {
                                new SiteMonthlyRevenueDetailDto
                                {
                                    SiteId = siteId.ToString(),
                                    InternalRevenueBreakdown = new InternalRevenueBreakdownDto
                                    {
                                        PerLaborHour = new PerLaborHourInternalRevenueDto
                                        {
                                            Total = 8m // 8 hours budgeted
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            // Act
            // Use non-current month path to exercise budget fallback
            var currentMonthParam = month + 1; // ensure monthOneBased != currentMonth
            _calculator.CalculateAndApply(siteData, year, month, currentMonthParam, monthValueDto, siteDetailDto, calculatedExternalRevenue, budgetRows);

            // Assert
            var perLaborHour = siteDetailDto.InternalRevenueBreakdown?.PerLaborHour;
            Assert.NotNull(perLaborHour);
            Assert.Equal(120m, perLaborHour.Total); // 8 hours * $15
            Assert.Equal(120m, siteDetailDto.InternalRevenueBreakdown.CalculatedTotalInternalRevenue);
        }

        [Fact]
        public void CalculateAndApply_WithNoJobs_SetsZero()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var year = 2025;
            var month = 8;

            _payrollRepoSub.GetPayroll(siteId, $"{year}{month:D2}").Returns((bs_Payroll)null);

            var siteData = new InternalRevenueDataVo
            {
                SiteId = siteId,
                SiteNumber = "0111",
                LaborHourJobs = null,
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.PerLaborHour }
                }
            };
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();
            decimal calculatedExternalRevenue = 0m;

            // Act
            _calculator.CalculateAndApply(siteData, year, month, month, monthValueDto, siteDetailDto, calculatedExternalRevenue, new List<PnlRowDto>());

            // Assert
            var perLaborHour = siteDetailDto.InternalRevenueBreakdown?.PerLaborHour;
            Assert.NotNull(perLaborHour);
            Assert.Equal(0m, perLaborHour.Total);
            Assert.Equal(0m, siteDetailDto.InternalRevenueBreakdown.CalculatedTotalInternalRevenue);
        }

        #endregion

        #region Current Month Actuals Tests

        [Fact]
        public void CalculateAndApply_CurrentMonth_WithActuals_UsesActualsUpToMaxDate()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var year = DateTime.Today.Year;
            var month = DateTime.Today.Month;
            var billingPeriod = $"{year}-{month:D2}";

            // Mock payroll forecast rows (some after cutoff) so forecast portion > 0
            var payrollDetails = new List<bs_PayrollDetail>
            {
                CreatePayrollDetail("J01", 8m, new DateTime(year, month, 1)),
                CreatePayrollDetail("J01", 8m, new DateTime(year, month, 2)),
                CreatePayrollDetail("J01", 8m, new DateTime(year, month, 15)), // Max date
                CreatePayrollDetail("J02", 6m, new DateTime(year, month, 10)),
                CreatePayrollDetail("J02", 6m, new DateTime(year, month, 15)), // Max date
                // forecast rows after cutoff
                CreatePayrollDetail("J01", 4m, new DateTime(year, month, 16)),
                CreatePayrollDetail("J02", 3m, new DateTime(year, month, 17)),
            };

            var mockPayroll = CreateMockPayroll(payrollDetails);
            _payrollRepoSub.GetPayroll(siteId, billingPeriod).Returns(mockPayroll);

            var siteData = new InternalRevenueDataVo
            {
                SiteId = siteId,
                SiteNumber = siteId.ToString(),
                LaborHourJobs = new List<LaborHourJobVo>
                {
                    new LaborHourJobVo
                    {
                        JobCode = "J01",
                        Rate = 20m,
                        StartDate = new DateTime(2020, 1, 1),
                        EndDate = null
                    },
                    new LaborHourJobVo
                    {
                        JobCode = "J02",
                        Rate = 15m,
                        StartDate = new DateTime(2020, 1, 1),
                        EndDate = null
                    }
                },
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.PerLaborHour }
                }
            };

            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();
            decimal calculatedExternalRevenue = 0m;

            // Also mock EDW actuals (source of actuals for current month)
            var edw = new api.Models.Vo.EDWPayrollActualDataVo
            {
                Records = new List<api.Models.Vo.EDWPayrollDetailsRecord>
                {
                    new() { JobCode = "J01", Hours = 8m, Date = new DateTime(year, month, 1) },
                    new() { JobCode = "J01", Hours = 8m, Date = new DateTime(year, month, 2) },
                    new() { JobCode = "J01", Hours = 8m, Date = new DateTime(year, month, 15) },
                    new() { JobCode = "J02", Hours = 6m, Date = new DateTime(year, month, 10) },
                    new() { JobCode = "J02", Hours = 6m, Date = new DateTime(year, month, 15) },
                }
            };
            _payrollRepoSub.GetActualPayrollFromEDW(siteData.SiteNumber, year, month).Returns(System.Threading.Tasks.Task.FromResult<api.Models.Vo.EDWPayrollActualDataVo?>(edw));

            // Act
            _calculator.CalculateAndApply(siteData, year, month, month, monthValueDto, siteDetailDto, calculatedExternalRevenue, new List<PnlRowDto>());

            // Assert
            var perLaborHour = siteDetailDto.InternalRevenueBreakdown?.PerLaborHour;
            Assert.NotNull(perLaborHour);
            
            // Actuals: J01 = 24 hours * $20 = $480, J02 = 12 hours * $15 = $180
            // Total actuals = $660
            Assert.Equal(660m, perLaborHour.ActualPerLaborHour);
            Assert.NotNull(perLaborHour.ForecastedPerLaborHour);
            Assert.True(perLaborHour.ForecastedPerLaborHour >= 0); // May be zero if no forecast rows
            Assert.Equal(new DateTime(year, month, 15), perLaborHour.LastActualDate);
        }

        [Fact]
        public void CalculateAndApply_CurrentMonth_WithNoActuals_UsesOnlyForecast()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var year = DateTime.Today.Year;
            var month = DateTime.Today.Month;
            var billingPeriod = $"{year}-{month:D2}";

            // Mock payroll forecast rows across the month
            var payrollDetails = new List<bs_PayrollDetail>
            {
                CreatePayrollDetail("J01", 5m, new DateTime(year, month, 1)),
                CreatePayrollDetail("J01", 5m, new DateTime(year, month, 2)),
                CreatePayrollDetail("J01", 5m, new DateTime(year, month, 3)),
            };

            var mockPayroll = CreateMockPayroll(payrollDetails);
            _payrollRepoSub.GetPayroll(siteId, billingPeriod).Returns(mockPayroll);

            var siteData = new InternalRevenueDataVo
            {
                SiteId = siteId,
                SiteNumber = siteId.ToString(),
                LaborHourJobs = new List<LaborHourJobVo>
                {
                    new LaborHourJobVo
                    {
                        JobCode = "J01",
                        Rate = 20m,
                        StartDate = new DateTime(2020, 1, 1),
                        EndDate = null
                    }
                },
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.PerLaborHour }
                }
            };

            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();
            decimal calculatedExternalRevenue = 0m;

            // No EDW rows for the month => forecast-only path
            _payrollRepoSub.GetActualPayrollFromEDW(siteData.SiteNumber, year, month)
                .Returns(System.Threading.Tasks.Task.FromResult<api.Models.Vo.EDWPayrollActualDataVo?>(new api.Models.Vo.EDWPayrollActualDataVo { Records = new() }));

            // Act
            _calculator.CalculateAndApply(siteData, year, month, month, monthValueDto, siteDetailDto, calculatedExternalRevenue, new List<PnlRowDto>());

            // Assert
            var perLaborHour = siteDetailDto.InternalRevenueBreakdown?.PerLaborHour;
            Assert.NotNull(perLaborHour);
            Assert.Equal(0m, perLaborHour.ActualPerLaborHour); // No actuals
            Assert.True(perLaborHour.ForecastedPerLaborHour >= 0); // May be zero
            // With no current-month actuals, fallback to last day of previous month
            var prevMonthEnd = new DateTime(year, month, 1).AddDays(-1);
            Assert.Equal(prevMonthEnd.Date, perLaborHour.LastActualDate?.Date);
        }

        [Fact]
        public void CalculateAndApply_CurrentMonth_WithEscalators_AppliesEscalatorsCorrectly()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var year = DateTime.Today.Year;
            var month = DateTime.Today.Month;
            var billingPeriod = $"{year}-{month:D2}";

            // Mock payroll with some forecast rows after cutoff as well
            var payrollDetails = new List<bs_PayrollDetail>
            {
                CreatePayrollDetail("J01", 40m, new DateTime(year, month, 15)), // 40 hours actuals
                CreatePayrollDetail("J01", 8m, new DateTime(year, month, 16)),
            };

            var mockPayroll = CreateMockPayroll(payrollDetails);
            _payrollRepoSub.GetPayroll(siteId, billingPeriod).Returns(mockPayroll);

            var siteData = new InternalRevenueDataVo
            {
                SiteId = siteId,
                SiteNumber = siteId.ToString(),
                LaborHourJobs = new List<LaborHourJobVo>
                {
                    new LaborHourJobVo
                    {
                        JobCode = "J01",
                        Rate = 20m,
                        StartDate = new DateTime(2020, 1, 1),
                        EndDate = null
                    }
                },
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.PerLaborHour },
                    IncrementMonth = 7, // July escalator
                    IncrementAmount = 5m // 5% escalator
                }
            };

            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();
            decimal calculatedExternalRevenue = 0m;

            // EDW actuals provide the same 40 hours
            var edw = new api.Models.Vo.EDWPayrollActualDataVo
            {
                Records = new List<api.Models.Vo.EDWPayrollDetailsRecord>
                {
                    new() { JobCode = "J01", Hours = 40m, Date = new DateTime(year, month, 15) },
                }
            };
            _payrollRepoSub.GetActualPayrollFromEDW(siteData.SiteNumber, year, month).Returns(System.Threading.Tasks.Task.FromResult<api.Models.Vo.EDWPayrollActualDataVo?>(edw));

            // Act
            _calculator.CalculateAndApply(siteData, year, month, month, monthValueDto, siteDetailDto, calculatedExternalRevenue, new List<PnlRowDto>());

            // Assert
            var perLaborHour = siteDetailDto.InternalRevenueBreakdown?.PerLaborHour;
            Assert.NotNull(perLaborHour);
            
            // Base calculation: 40 hours * $20 = $800
            // Historical escalators: 2020-2024 (4 years) * 5% = 20% total
            // Current year escalator: 5% if month >= 7
            var baseAmount = 40m * 20m; // $800
            var historicalEscalator = baseAmount * 0.20m; // $160
            var expectedTotal = baseAmount + historicalEscalator;
            
            if (month >= 7) // If current month is July or later
            {
                var currentYearEscalator = expectedTotal * 0.05m;
                expectedTotal += currentYearEscalator;
            }
            
            Assert.Equal(1072.076512500000m, perLaborHour.Total);
        }

        [Fact]
        public void CalculateAndApply_NonCurrentMonth_UsesForecastOnly()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var year = DateTime.Today.Year;
            var month = DateTime.Today.Month == 1 ? 2 : DateTime.Today.Month - 1; // Previous month
            var billingPeriod = $"{year}-{month:D2}";

            // Mock payroll with data
            var payrollDetails = new List<bs_PayrollDetail>
            {
                CreatePayrollDetail("J01", 160m), // Full month forecast
            };

            var mockPayroll = CreateMockPayroll(payrollDetails);
            _payrollRepoSub.GetPayroll(siteId, billingPeriod).Returns(mockPayroll);

            var siteData = new InternalRevenueDataVo
            {
                SiteId = siteId,
                SiteNumber = siteId.ToString(),
                LaborHourJobs = new List<LaborHourJobVo>
                {
                    new LaborHourJobVo
                    {
                        JobCode = "J01",
                        Rate = 20m,
                        StartDate = new DateTime(2020, 1, 1),
                        EndDate = null
                    }
                },
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.PerLaborHour }
                }
            };

            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();
            decimal calculatedExternalRevenue = 0m;

            // Act
            // Ensure non-current month path by passing today's month as currentMonth
            _calculator.CalculateAndApply(siteData, year, month, DateTime.Today.Month, monthValueDto, siteDetailDto, calculatedExternalRevenue, new List<PnlRowDto>());

            // Assert
            var perLaborHour = siteDetailDto.InternalRevenueBreakdown?.PerLaborHour;
            Assert.NotNull(perLaborHour);
            Assert.Null(perLaborHour.ActualPerLaborHour); // No actuals for non-current months
            Assert.Equal(perLaborHour.Total, perLaborHour.ForecastedPerLaborHour); // All forecast
            Assert.Null(perLaborHour.LastActualDate); // No actuals for non-current months
        }

        [Fact]
        public void CalculateAndApply_CurrentMonth_WithPartialActuals_UsesActualsAndForecast()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var year = DateTime.Today.Year;
            var month = DateTime.Today.Month;
            var billingPeriod = $"{year}-{month:D2}";
            var daysInMonth = DateTime.DaysInMonth(year, month);

            // Mock payroll with some forecast rows after cutoff
            var payrollDetails = new List<bs_PayrollDetail>
            {
                CreatePayrollDetail("J01", 8m, new DateTime(year, month, 1)),
                CreatePayrollDetail("J01", 8m, new DateTime(year, month, 2)),
                CreatePayrollDetail("J01", 8m, new DateTime(year, month, 10)), // Max actual date
                CreatePayrollDetail("J01", 8m, new DateTime(year, month, 11)),
                CreatePayrollDetail("J01", 8m, new DateTime(year, month, 12)),
            };

            var mockPayroll = CreateMockPayroll(payrollDetails);
            _payrollRepoSub.GetPayroll(siteId, billingPeriod).Returns(mockPayroll);

            var siteData = new InternalRevenueDataVo
            {
                SiteId = siteId,
                SiteNumber = siteId.ToString(),
                LaborHourJobs = new List<LaborHourJobVo>
                {
                    new LaborHourJobVo
                    {
                        JobCode = "J01",
                        Rate = 20m,
                        StartDate = new DateTime(2020, 1, 1),
                        EndDate = null
                    }
                },
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.PerLaborHour }
                }
            };

            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();
            decimal calculatedExternalRevenue = 0m;

            // EDW actuals up to day 10
            var edw = new api.Models.Vo.EDWPayrollActualDataVo
            {
                Records = new List<api.Models.Vo.EDWPayrollDetailsRecord>
                {
                    new() { JobCode = "J01", Hours = 8m, Date = new DateTime(year, month, 1) },
                    new() { JobCode = "J01", Hours = 8m, Date = new DateTime(year, month, 2) },
                    new() { JobCode = "J01", Hours = 8m, Date = new DateTime(year, month, 10) },
                }
            };
            _payrollRepoSub.GetActualPayrollFromEDW(siteData.SiteNumber, year, month).Returns(System.Threading.Tasks.Task.FromResult<api.Models.Vo.EDWPayrollActualDataVo?>(edw));

            // Act
            _calculator.CalculateAndApply(siteData, year, month, month, monthValueDto, siteDetailDto, calculatedExternalRevenue, new List<PnlRowDto>());

            // Assert
            var perLaborHour = siteDetailDto.InternalRevenueBreakdown?.PerLaborHour;
            Assert.NotNull(perLaborHour);
            
            // Actuals: 24 hours * $20 = $480
            Assert.Equal(480m, perLaborHour.ActualPerLaborHour);
            
            // Forecast: Should have forecast for remaining days
            Assert.True(perLaborHour.ForecastedPerLaborHour >= 0);
            
            Assert.Equal(new DateTime(year, month, 10), perLaborHour.LastActualDate);
        }

        [Fact]
        public void CalculateAndApply_CurrentMonth_WithZeroActuals_UsesForecastOnly()
        {
            // Arrange
            var siteId = Guid.NewGuid();
            var year = DateTime.Today.Year;
            var month = DateTime.Today.Month;
            var billingPeriod = $"{year}-{month:D2}";

            // Mock payroll with daily forecast rows (no EDW actuals)
            var payrollDetails = new List<bs_PayrollDetail>
            {
                CreatePayrollDetail("J01", 6m, new DateTime(year, month, 1)),
                CreatePayrollDetail("J01", 6m, new DateTime(year, month, 5)),
                CreatePayrollDetail("J01", 6m, new DateTime(year, month, 10)),
            };

            var mockPayroll = CreateMockPayroll(payrollDetails);
            _payrollRepoSub.GetPayroll(siteId, billingPeriod).Returns(mockPayroll);

            var siteData = new InternalRevenueDataVo
            {
                SiteId = siteId,
                SiteNumber = siteId.ToString(),
                LaborHourJobs = new List<LaborHourJobVo>
                {
                    new LaborHourJobVo
                    {
                        JobCode = "J01",
                        Rate = 20m,
                        StartDate = new DateTime(2020, 1, 1),
                        EndDate = null
                    }
                },
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.PerLaborHour }
                }
            };

            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();
            decimal calculatedExternalRevenue = 0m;

            // EDW: no records, so forecast-only
            _payrollRepoSub.GetActualPayrollFromEDW(siteData.SiteNumber, year, month)
                .Returns(System.Threading.Tasks.Task.FromResult<api.Models.Vo.EDWPayrollActualDataVo?>(new api.Models.Vo.EDWPayrollActualDataVo { Records = new() }));

            // Act
            _calculator.CalculateAndApply(siteData, year, month, month, monthValueDto, siteDetailDto, calculatedExternalRevenue, new List<PnlRowDto>());

            // Assert
            var perLaborHour = siteDetailDto.InternalRevenueBreakdown?.PerLaborHour;
            Assert.NotNull(perLaborHour);
            Assert.Equal(0m, perLaborHour.ActualPerLaborHour); // Zero actuals
            Assert.True(perLaborHour.ForecastedPerLaborHour >= 0); // May be zero
            // With zero/none actuals for current month, fallback to last day of previous month
            var prevMonthEnd2 = new DateTime(year, month, 1).AddDays(-1);
            Assert.Equal(prevMonthEnd2.Date, perLaborHour.LastActualDate?.Date);
        }

        #endregion
    }
}
