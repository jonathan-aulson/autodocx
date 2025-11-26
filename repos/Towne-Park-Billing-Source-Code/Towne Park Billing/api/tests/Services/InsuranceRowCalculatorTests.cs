using System;
using System.Collections.Generic;
using System.Linq;
using api.Models.Dto;
using api.Models.Vo;
using api.Services.Impl.Calculators;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TownePark.Models.Vo;
using TownePark; // for bs_contracttypechoices
using Xunit;

namespace BackendTests.Services
{
    public class InsuranceRowCalculatorTests
    {
        private readonly ILogger<InsuranceRowCalculator> _logger;
        private readonly InsuranceRowCalculator _calculator;

        public InsuranceRowCalculatorTests()
        {
            _logger = Substitute.For<ILogger<InsuranceRowCalculator>>();
            _calculator = new InsuranceRowCalculator(_logger);
        }

        [Fact]
        public void ComputeForMonth_UsesActual_WhenPresent()
        {
            var siteMA = CreateSite("0170", isMA: true, addl: 0m);
            var siteNonMA = CreateSite("0200", isMA: false);
            var pnl = CreatePnlResponseWithRows(new[] { siteMA.SiteNumber, siteNonMA.SiteNumber }, monthZeroBased: 6);

            // Actual Insurance present for MA site
            SetActualSiteValue(pnl, "Insurance", monthZeroBased: 6, siteMA.SiteNumber, 5058.19m);

            // Budget rows needed for non-MA derived rate
            SetBudgetSiteValue(pnl, "Insurance", monthZeroBased: 6, siteNonMA.SiteNumber, 4502.25m);
            SetBudgetSiteValue(pnl, "Payroll", monthZeroBased: 6, siteNonMA.SiteNumber, 67981.33m);

            // Forecasted payroll inputs
            var forecastedPayroll = new Dictionary<string, decimal>
            {
                [siteMA.SiteNumber] = 60000m,
                [siteNonMA.SiteNumber] = 70000m
            };

            _calculator.ComputeForMonth(pnl, new List<InternalRevenueDataVo> { siteMA, siteNonMA }, 2025, 7, 6, forecastedPayroll);

            var fcRow = pnl.ForecastRows.First(r => r.ColumnName == "Insurance");
            var month = fcRow.MonthlyValues.First(m => m.Month == 6);
            var maDetail = month.SiteDetails!.First(sd => sd.SiteId == siteMA.SiteNumber);
            var nonMaDetail = month.SiteDetails!.First(sd => sd.SiteId == siteNonMA.SiteNumber);

            Assert.False(maDetail.IsForecast);
            Assert.Equal(5058.19m, maDetail.Value);
            Assert.True(nonMaDetail.IsForecast);
            Assert.True(month.Value.HasValue && month.Value.Value > 5058.19m);
        }

        [Fact]
        public void ComputeForMonth_MA_Uses577Percent_PlusVehicle7082_Excludes_Addl_ForPnL()
        {
            var siteMA = CreateSite("0170", isMA: true, addl: 300m);
            var pnl = CreatePnlResponseWithRows(new[] { siteMA.SiteNumber }, monthZeroBased: 6);

            var forecastedPayroll = new Dictionary<string, decimal> { [siteMA.SiteNumber] = 100000m };
            // Pre-attach 7082 budget to forecast row site detail (simulating PnlService prefetch)
            SetInsurance7082(pnl, monthZeroBased: 6, siteMA.SiteNumber, 1500m);

            _calculator.ComputeForMonth(pnl, new List<InternalRevenueDataVo> { siteMA }, 2025, 7, 6, forecastedPayroll);

            var month = pnl.ForecastRows.First(r => r.ColumnName == "Insurance").MonthlyValues.First(m => m.Month == 6);
            var sd = month.SiteDetails!.First();

            // P&L row should EXCLUDE additional insurance
            var expected = Math.Round(100000m * 0.0577m + 1500m, 2, MidpointRounding.AwayFromZero);
            Assert.Equal(expected, sd.Value);
            Assert.True(sd.IsForecast);
            Assert.Equal(expected, month.Value);
        }

        [Fact]
        public void ComputeForMonth_NonMA_UsesBudgetDerivedRate()
        {
            var siteNonMA = CreateSite("0200", isMA: false);
            var pnl = CreatePnlResponseWithRows(new[] { siteNonMA.SiteNumber }, monthZeroBased: 6);

            // Budget: Insurance / Payroll = 4500 / 100000 = 4.5%
            SetBudgetSiteValue(pnl, "Insurance", 6, siteNonMA.SiteNumber, 4500m);
            SetBudgetSiteValue(pnl, "Payroll", 6, siteNonMA.SiteNumber, 100000m);

            var forecastedPayroll = new Dictionary<string, decimal> { [siteNonMA.SiteNumber] = 120000m };

            _calculator.ComputeForMonth(pnl, new List<InternalRevenueDataVo> { siteNonMA }, 2025, 7, 6, forecastedPayroll);

            var month = pnl.ForecastRows.First(r => r.ColumnName == "Insurance").MonthlyValues.First(m => m.Month == 6);
            var sd = month.SiteDetails!.First();

            var expected = Math.Round(120000m * 0.045m, 2, MidpointRounding.AwayFromZero);
            Assert.Equal(expected, sd.Value);
            Assert.True(sd.IsForecast);
            Assert.Equal(expected, month.Value);
        }

        [Fact]
        public void ComputeForMonth_NonMA_FallbacksToDefaultRate_WhenBudgetMissing()
        {
            var siteNonMA = CreateSite("0200", isMA: false);
            var pnl = CreatePnlResponseWithRows(new[] { siteNonMA.SiteNumber }, monthZeroBased: 6);

            // Budget payroll missing (0) → fallback 4.45%
            SetBudgetSiteValue(pnl, "Insurance", 6, siteNonMA.SiteNumber, 0m);
            SetBudgetSiteValue(pnl, "Payroll", 6, siteNonMA.SiteNumber, 0m);

            var forecastedPayroll = new Dictionary<string, decimal> { [siteNonMA.SiteNumber] = 90000m };

            _calculator.ComputeForMonth(pnl, new List<InternalRevenueDataVo> { siteNonMA }, 2025, 7, 6, forecastedPayroll);

            var month = pnl.ForecastRows.First(r => r.ColumnName == "Insurance").MonthlyValues.First(m => m.Month == 6);
            var sd = month.SiteDetails!.First();

            var expected = Math.Round(90000m * 0.0445m, 2, MidpointRounding.AwayFromZero);
            Assert.Equal(expected, sd.Value);
            Assert.True(sd.IsForecast);
            Assert.Equal(expected, month.Value);
        }

        private static InternalRevenueDataVo CreateSite(string siteNumber, bool isMA, decimal addl = 0m)
        {
            return new InternalRevenueDataVo
            {
                SiteId = Guid.NewGuid(),
                SiteNumber = siteNumber,
                Contract = new ContractDataVo { ContractTypes = isMA ? new[] { bs_contracttypechoices.ManagementAgreement } : Array.Empty<bs_contracttypechoices>() },
                ManagementAgreement = isMA ? new ManagementAgreementVo { InsuranceAddlAmount = addl } : null
            };
        }

        private static PnlResponseDto CreatePnlResponseWithRows(IEnumerable<string> sites, int monthZeroBased)
        {
            var response = new PnlResponseDto
            {
                BudgetRows = new List<PnlRowDto>(),
                ActualRows = new List<PnlRowDto>(),
                ForecastRows = new List<PnlRowDto>()
            };

            foreach (var name in new[] { "Insurance", "Payroll" })
            {
                response.BudgetRows.Add(CreateRow(name, sites, monthZeroBased));
                response.ActualRows.Add(CreateRow(name, sites, monthZeroBased));
                response.ForecastRows.Add(CreateRow(name, sites, monthZeroBased));
            }
            return response;
        }

        private static PnlRowDto CreateRow(string name, IEnumerable<string> sites, int monthZeroBased)
        {
            return new PnlRowDto
            {
                ColumnName = name,
                MonthlyValues = new List<MonthValueDto>
                {
                    new MonthValueDto
                    {
                        Month = monthZeroBased,
                        SiteDetails = sites.Select(s => new SiteMonthlyRevenueDetailDto { SiteId = s, Value = 0m }).ToList(),
                        Value = 0m
                    }
                }
            };
        }

        private static void SetBudgetSiteValue(PnlResponseDto pnl, string rowName, int monthZeroBased, string siteId, decimal value)
        {
            var row = pnl.BudgetRows.First(r => r.ColumnName == rowName);
            var month = row.MonthlyValues.First(m => m.Month == monthZeroBased);
            var sd = month.SiteDetails!.First(s => s.SiteId == siteId);
            sd.Value = value;
        }

        private static void SetActualSiteValue(PnlResponseDto pnl, string rowName, int monthZeroBased, string siteId, decimal value)
        {
            var row = pnl.ActualRows.First(r => r.ColumnName == rowName);
            var month = row.MonthlyValues.First(m => m.Month == monthZeroBased);
            var sd = month.SiteDetails!.First(s => s.SiteId == siteId);
            sd.Value = value;
        }

        private static void SetInsurance7082(PnlResponseDto pnl, int monthZeroBased, string siteId, decimal vehicle7082)
        {
            var row = pnl.ForecastRows.First(r => r.ColumnName == "Insurance");
            var month = row.MonthlyValues.First(m => m.Month == monthZeroBased);
            var sd = month.SiteDetails!.First(s => s.SiteId == siteId);
            sd.InsuranceBreakdown ??= new InsuranceBreakdownDto();
            sd.InsuranceBreakdown.VehicleInsurance7082 = vehicle7082;
        }
    }
}


