using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using api.Services.Impl.Calculators;
using api.Models.Dto;
using TownePark.Models.Vo;
using api.Models.Vo;
using TownePark;

namespace BackendTests.Services
{
    public class NonGLBillableExpenseCalculatorTests
    {
        private readonly NonGLBillableExpenseCalculator _calculator = new NonGLBillableExpenseCalculator();

        [Fact]
        public async Task CalculateAndApplyAsync_PercentageOfBillablePayroll_UsesPayrollColumn()
        {
            var year = 2025; var month = 6;
            var siteData = CreateSiteData(new List<NonGLExpenseVo>
            {
                new NonGLExpenseVo { Period = new DateTime(year, month, 1), ExpenseType = "Payroll", PayrollType = "Billable", Amount = 10m }
            });

            var siteDetail = new SiteMonthlyRevenueDetailDto { SiteId = siteData.SiteNumber, InternalRevenueBreakdown = new InternalRevenueBreakdownDto { BillableAccounts = new BillableAccountsInternalRevenueDto() } };
            var budgetRows = BuildBudgetRows("Payroll", siteData.SiteNumber, month, 80_000m);

            await _calculator.CalculateAndApplyAsync(siteData, year, month, month, new MonthValueDto(), siteDetail, 0m, budgetRows);

            var comp = siteDetail.InternalRevenueBreakdown.ManagementAgreement?.Components?.Find(c => c.Name == "Non-GL Billable Expenses");
            Assert.NotNull(comp);
            Assert.Equal(8_000m, comp!.Value); // 10% of 80,000
        }

        [Fact]
        public async Task CurrentMonth_Payroll_Billable_ActualsPlusDailyForecast_NoBudgetFallback()
        {
            // Arrange: pick today's month to trigger current-month path
            var today = DateTime.Today;
            int year = today.Year; int month = today.Month;

            var siteData = CreateSiteData(new List<NonGLExpenseVo>
            {
                new NonGLExpenseVo { Period = new DateTime(year, month, 1), ExpenseType = "Payroll", PayrollType = "Billable", Amount = 10m }
            });

            // Provide PayrollBreakdown for current-month logic: actual-to-date 600, cutoff on 3rd
            var siteDetail = new SiteMonthlyRevenueDetailDto
            {
                SiteId = siteData.SiteNumber,
                PayrollBreakdown = new PayrollBreakdownDto
                {
                    ActualPayroll = 600m,
                    ActualPayrollLastDate = new DateTime(year, month, 3)
                },
                InternalRevenueBreakdown = new InternalRevenueBreakdownDto { BillableAccounts = new BillableAccountsInternalRevenueDto() }
            };

            // No budget rows passed in; calculator should not use them in current month
            var budgetRows = new List<PnlRowDto>();

            // Act
            await _calculator.CalculateAndApplyAsync(siteData, year, month, month, new MonthValueDto(), siteDetail, 0m, budgetRows);

            // Assert: component exists and > 0 when actuals exist (even if no daily forecast rows)
            var comp = siteDetail.InternalRevenueBreakdown.ManagementAgreement?.Components?.Find(c => c.Name == "Non-GL Billable Expenses");
            Assert.NotNull(comp);
            Assert.True(comp!.Value > 0m);
        }

        [Fact]
        public async Task CurrentMonth_Payroll_NoActuals_UsesOnlyDailyForecast()
        {
            var today = DateTime.Today;
            int year = today.Year; int month = today.Month;

            var siteData = CreateSiteData(new List<NonGLExpenseVo>
            {
                new NonGLExpenseVo { Period = new DateTime(year, month, 1), ExpenseType = "Payroll", PayrollType = "Billable", Amount = 10m }
            });

            var siteDetail = new SiteMonthlyRevenueDetailDto
            {
                SiteId = siteData.SiteNumber,
                InternalActuals = new InternalRevenueActualsVo
                {
                    LastActualizedDate = null,
                    DailyActuals = new List<DailyActualVo>()
                },
                InternalRevenueBreakdown = new InternalRevenueBreakdownDto { BillableAccounts = new BillableAccountsInternalRevenueDto() }
            };

            // No budget rows; ensure no fallback is used
            var budgetRows = new List<PnlRowDto>();

            await _calculator.CalculateAndApplyAsync(siteData, year, month, month, new MonthValueDto(), siteDetail, 0m, budgetRows);

            var comp = siteDetail.InternalRevenueBreakdown.ManagementAgreement?.Components?.Find(c => c.Name == "Non-GL Billable Expenses");
            // With no repository injected in test, there are no forecast rows; therefore, value should be 0
            Assert.True(comp == null || comp.Value == 0m);
        }

        [Fact]
        public async Task CalculateAndApplyAsync_PercentageOfTotalPayroll_AddsPteb()
        {
            var year = 2025; var month = 6;
            var siteData = CreateSiteData(new List<NonGLExpenseVo>
            {
                new NonGLExpenseVo { Period = new DateTime(year, month, 1), ExpenseType = "Payroll", PayrollType = "Total", Amount = 10m }
            });

            var siteDetail = new SiteMonthlyRevenueDetailDto { SiteId = siteData.SiteNumber, InternalRevenueBreakdown = new InternalRevenueBreakdownDto { BillableAccounts = new BillableAccountsInternalRevenueDto { Pteb = new PtebInternalRevenueDto { Total = 20_000m } } } };
            var budgetRows = BuildBudgetRows("Payroll", siteData.SiteNumber, month, 80_000m);

            await _calculator.CalculateAndApplyAsync(siteData, year, month, month, new MonthValueDto(), siteDetail, 0m, budgetRows);

            var comp = siteDetail.InternalRevenueBreakdown.ManagementAgreement?.Components?.Find(c => c.Name == "Non-GL Billable Expenses");
            Assert.NotNull(comp);
            Assert.Equal(10_000m, comp!.Value); // 10% of (80,000 + 20,000)
        }

        [Fact]
        public async Task CalculateAndApplyAsync_PercentageOfRevenue_UsesExternalRevenue()
        {
            var year = 2025; var month = 6;
            var siteData = CreateSiteData(new List<NonGLExpenseVo>
            {
                new NonGLExpenseVo { Period = new DateTime(year, month, 1), ExpenseType = "Revenue", Amount = 5m }
            });

            var siteDetail = new SiteMonthlyRevenueDetailDto { SiteId = siteData.SiteNumber, InternalRevenueBreakdown = new InternalRevenueBreakdownDto { BillableAccounts = new BillableAccountsInternalRevenueDto() } };

            await _calculator.CalculateAndApplyAsync(siteData, year, month, month, new MonthValueDto(), siteDetail, 1_000_000m, new List<PnlRowDto>());

            var comp = siteDetail.InternalRevenueBreakdown.ManagementAgreement?.Components?.Find(c => c.Name == "Non-GL Billable Expenses");
            Assert.NotNull(comp);
            Assert.Equal(50_000m, comp!.Value); // 5% of 1,000,000
        }

        [Fact]
        public async Task CalculateAndApplyAsync_FixedAmount_UsesAmount()
        {
            var year = 2025; var month = 6;
            var siteData = CreateSiteData(new List<NonGLExpenseVo>
            {
                new NonGLExpenseVo { Period = new DateTime(year, month, 1), ExpenseType = "FixedAmount", Amount = 2_500m }
            });

            var siteDetail = new SiteMonthlyRevenueDetailDto { SiteId = siteData.SiteNumber, InternalRevenueBreakdown = new InternalRevenueBreakdownDto { BillableAccounts = new BillableAccountsInternalRevenueDto() } };

            await _calculator.CalculateAndApplyAsync(siteData, year, month, month, new MonthValueDto(), siteDetail, 0m, new List<PnlRowDto>());

            var comp = siteDetail.InternalRevenueBreakdown.ManagementAgreement?.Components?.Find(c => c.Name == "Non-GL Billable Expenses");
            Assert.NotNull(comp);
            Assert.Equal(2_500m, comp!.Value);
        }

        [Fact]
        public async Task CalculateAndApplyAsync_IgnoresItemsNotInTargetMonth()
        {
            var year = 2025; var month = 6;
            var siteData = CreateSiteData(new List<NonGLExpenseVo>
            {
                new NonGLExpenseVo { Period = new DateTime(year, month + 1, 1), ExpenseType = "FixedAmount", Amount = 1_000m }
            });

            var siteDetail = new SiteMonthlyRevenueDetailDto { SiteId = siteData.SiteNumber };

            await _calculator.CalculateAndApplyAsync(siteData, year, month, month, new MonthValueDto(), siteDetail, 0m, new List<PnlRowDto>());

            var comp = siteDetail.InternalRevenueBreakdown?.ManagementAgreement?.Components?.Find(c => c.Name == "Non-GL Billable Expenses");
            Assert.True(comp == null || comp.Value == 0m);
        }

        private static InternalRevenueDataVo CreateSiteData(List<NonGLExpenseVo> nonGl)
        {
            return new InternalRevenueDataVo
            {
                SiteId = Guid.NewGuid(),
                SiteNumber = "S-100",
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.ManagementAgreement }
                },
                OtherExpenses = nonGl
            };
        }

        private static List<PnlRowDto> BuildBudgetRows(string columnName, string siteNumber, int monthOneBased, decimal value)
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
                                new SiteMonthlyRevenueDetailDto { SiteId = siteNumber, Value = value }
                            }
                        }
                    }
                }
            };
        }
    }
}
