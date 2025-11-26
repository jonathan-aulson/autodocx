using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using api.Services.Impl.Calculators;
using api.Models.Dto;
using api.Models.Vo;
using NSubstitute;
using TownePark.Models.Vo;

namespace BackendTests.Services
{
    public class PtebForecastCalculatorTests
    {
        private static PnlResponseDto BuildPnlResponseWithBudget(string siteNumber, int monthZeroBased, decimal budgetPteb, decimal budgetPayroll)
        {
            var budgetPtebRow = new PnlRowDto
            {
                ColumnName = "Pteb",
                MonthlyValues = Enumerable.Range(0, 12).Select(m => new MonthValueDto
                {
                    Month = m,
                    SiteDetails = new List<SiteMonthlyRevenueDetailDto>
                    {
                        new SiteMonthlyRevenueDetailDto
                        {
                            SiteId = siteNumber,
                            Value = m == monthZeroBased ? budgetPteb : 0m
                        }
                    }
                }).ToList()
            };

            var budgetPayrollRow = new PnlRowDto
            {
                ColumnName = "Payroll",
                MonthlyValues = Enumerable.Range(0, 12).Select(m => new MonthValueDto
                {
                    Month = m,
                    SiteDetails = new List<SiteMonthlyRevenueDetailDto>
                    {
                        new SiteMonthlyRevenueDetailDto
                        {
                            SiteId = siteNumber,
                            Value = m == monthZeroBased ? budgetPayroll : 0m
                        }
                    }
                }).ToList()
            };

            return new PnlResponseDto
            {
                BudgetRows = new List<PnlRowDto> { budgetPtebRow, budgetPayrollRow },
                ForecastRows = new List<PnlRowDto>
                {
                    new PnlRowDto
                    {
                        ColumnName = "Pteb",
                        MonthlyValues = Enumerable.Range(0, 12).Select(m => new MonthValueDto
                        {
                            Month = m,
                            SiteDetails = new List<SiteMonthlyRevenueDetailDto>()
                        }).ToList()
                    }
                }
            };
        }

        [Fact]
        public void ComputeForMonth_UsesForecastedPayroll_SetsIsForecastTrue()
        {
            // Arrange
            var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<PtebForecastCalculator>>();
            var calc = new PtebForecastCalculator(null, logger);
            var site = "S001";
            int year = DateTime.UtcNow.Year;
            int monthOneBased = DateTime.UtcNow.Month; // current or future
            int monthZeroBased = monthOneBased - 1;

            var pnl = BuildPnlResponseWithBudget(site, monthZeroBased, budgetPteb: 100m, budgetPayroll: 1000m); // rate 10%
            var sites = new List<InternalRevenueDataVo> { new InternalRevenueDataVo { SiteNumber = site } };
            var forecasted = new Dictionary<string, decimal> { [site] = 2000m }; // forecast payroll

            // Act
            calc.ComputeForMonth(pnl, sites, year, monthOneBased, monthZeroBased, forecasted, new Dictionary<string, decimal>());

            // Assert
            var row = pnl.ForecastRows.First(r => r.ColumnName == "Pteb");
            var mv = row.MonthlyValues.First(m => m.Month == monthZeroBased);
            var sd = mv.SiteDetails.First(s => s.SiteId == site);
            Assert.Equal(200m, sd.Value); // 2000 * 10%
            Assert.True(sd.IsForecast);
        }

        [Fact]
        public void ComputeForMonth_FallbackToBudget_SetsIsForecastFalse()
        {
            // Arrange
            var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<PtebForecastCalculator>>();
            var calc = new PtebForecastCalculator(null, logger);
            var site = "S002";
            int year = DateTime.UtcNow.Year;
            int monthOneBased = DateTime.UtcNow.Month; // current or future
            int monthZeroBased = monthOneBased - 1;

            var pnl = BuildPnlResponseWithBudget(site, monthZeroBased, budgetPteb: 150m, budgetPayroll: 0m); // missing payroll => fallback
            var sites = new List<InternalRevenueDataVo> { new InternalRevenueDataVo { SiteNumber = site } };
            var forecasted = new Dictionary<string, decimal> { [site] = 0m }; // no forecast available

            // Act
            calc.ComputeForMonth(pnl, sites, year, monthOneBased, monthZeroBased, forecasted, new Dictionary<string, decimal>());

            // Assert
            var row = pnl.ForecastRows.First(r => r.ColumnName == "Pteb");
            var mv = row.MonthlyValues.First(m => m.Month == monthZeroBased);
            var sd = mv.SiteDetails.First(s => s.SiteId == site);
            Assert.Equal(150m, sd.Value); // budget fallback
            Assert.False(sd.IsForecast);
        }

        [Fact]
        public void ComputeForMonth_UsesPriorYearRate_WhenBudgetMissing_AndForecastPresent()
        {
            // Arrange
            var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<PtebForecastCalculator>>();
            var calc = new PtebForecastCalculator(null, logger);
            var site = "S003";
            int year = DateTime.UtcNow.Year;
            int monthOneBased = DateTime.UtcNow.Month;
            int monthZeroBased = monthOneBased - 1;

            // Budget PTEB present but payroll missing forces fallback; prior-year rate should be used
            var pnl = BuildPnlResponseWithBudget(site, monthZeroBased, budgetPteb: 0m, budgetPayroll: 0m);
            var sites = new List<InternalRevenueDataVo> { new InternalRevenueDataVo { SiteNumber = site } };
            var forecasted = new Dictionary<string, decimal> { [site] = 1000m };
            var priorYearRates = new Dictionary<string, decimal> { [site] = 0.10m }; // 10%

            // Act
            calc.ComputeForMonth(pnl, sites, year, monthOneBased, monthZeroBased, forecasted, priorYearRates);

            // Assert
            var row = pnl.ForecastRows.First(r => r.ColumnName == "Pteb");
            var mv = row.MonthlyValues.First(m => m.Month == monthZeroBased);
            var sd = mv.SiteDetails.First(s => s.SiteId == site);
            Assert.Equal(100m, sd.Value); // 1000 * 10%
            Assert.True(sd.IsForecast);
        }
    }
}


