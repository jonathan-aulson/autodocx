using System;
using System.Collections.Generic;
using System.Linq;
using api.Models.Dto;
using api.Services.Impl.Calculators;
using api.Models.Vo;
using TownePark.Models.Vo;
using Xunit;
using TownePark;

namespace BackendTests.Services
{
    public class RevenueShareCalculatorTest
    {
        private readonly RevenueShareCalculator _calculator;

        public RevenueShareCalculatorTest()
        {
            _calculator = new RevenueShareCalculator();
        }

        [Fact]
        public void CalculateAndApply_CurrentMonth_WithDailyActualsAndForecast_CombinesCorrectly()
        {
            // Arrange
            var year = DateTime.Today.Year;
            var month = DateTime.Today.Month;

            var siteData = new InternalRevenueDataVo
            {
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.RevenueShare }
                },
                RevenueShareThresholds = new List<RevenueShareThresholdVo>
                {
                    new RevenueShareThresholdVo
                    {
                        ThresholdStructure = new ThresholdStructureVo
                        {
                            Tiers = new List<ThresholdTierVo>
                            {
                                new ThresholdTierVo
                                {
                                    Amount = 0m,
                                    SharePercentage = 50.0m, // 50% stored as whole number
                                    EffectiveFrom = new DateTime(year, 1, 1),
                                    EffectiveTo = new DateTime(year, 12, 31)
                                }
                            }
                        }
                    }
                },
                SiteStatistics = new List<TownePark.Models.Vo.SiteStatisticDetailVo>()
            };

            // Actuals: first 3 days have actual external revenue: 100, 200, 300
            var siteDetailDto = new SiteMonthlyRevenueDetailDto
            {
                InternalActuals = new InternalRevenueActualsVo
                {
                    Year = year,
                    Month = month,
                    DailyActuals = new List<DailyActualVo>
                    {
                        new DailyActualVo { Date = new DateTime(year, month, 1).ToString("yyyy-MM-dd"), ExternalRevenue = 100m },
                        new DailyActualVo { Date = new DateTime(year, month, 2).ToString("yyyy-MM-dd"), ExternalRevenue = 200m },
                        new DailyActualVo { Date = new DateTime(year, month, 3).ToString("yyyy-MM-dd"), ExternalRevenue = 300m }
                    }
                }
            };

            // Forecast: days 4-5 have forecast external revenue 400 and 500
            siteData.SiteStatistics.Add(new TownePark.Models.Vo.SiteStatisticDetailVo {
                Date = new DateTime(year, month, 4),
                Type = bs_sitestatisticdetailchoice.Forecast,
                ExternalRevenue = 400m
            });
            siteData.SiteStatistics.Add(new TownePark.Models.Vo.SiteStatisticDetailVo {
                Date = new DateTime(year, month, 5),
                Type = bs_sitestatisticdetailchoice.Forecast,
                ExternalRevenue = 500m
            });

            var monthValueDto = new MonthValueDto();

            // Act
            _calculator.CalculateAndApply(siteData, year, month, month, monthValueDto, siteDetailDto, 0m, new List<PnlRowDto>());

            // Assert
            var revenueShare = siteDetailDto.InternalRevenueBreakdown?.RevenueShare;
            Assert.NotNull(revenueShare);
            // Split external revenue: Actual = 100+200+300 = 600; Forecast = 400+500 = 900; Combined = 1500
            Assert.Equal(600m, revenueShare.ActualExternalRevenue);
            Assert.Equal(900m, revenueShare.ForecastedExternalRevenue);
            // With a single 50% tier, revenue share total should be 750
            Assert.Equal(750m, revenueShare.Total);
        }

        [Fact]
        public void CalculateAndApply_CurrentMonth_NoActuals_UsesAllForecast()
        {
            // Arrange
            var year = DateTime.Today.Year;
            var month = DateTime.Today.Month;

            var siteData = new InternalRevenueDataVo
            {
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.RevenueShare }
                },
                RevenueShareThresholds = new List<RevenueShareThresholdVo>
                {
                    new RevenueShareThresholdVo
                    {
                        ThresholdStructure = new ThresholdStructureVo
                        {
                            Tiers = new List<ThresholdTierVo>
                            {
                                new ThresholdTierVo
                                {
                                    Amount = 0m,
                                    SharePercentage = 30.0m, // 30% stored as whole number
                                    EffectiveFrom = new DateTime(year, 1, 1),
                                    EffectiveTo = new DateTime(year, 12, 31)
                                }
                            }
                        }
                    }
                },
                SiteStatistics = new List<TownePark.Models.Vo.SiteStatisticDetailVo>()
            };

            // No actuals provided
            var siteDetailDto = new SiteMonthlyRevenueDetailDto { InternalActuals = new InternalRevenueActualsVo { Year = year, Month = month } };

            // Forecast for whole month: three days 100, 200, 300
            siteData.SiteStatistics.Add(new TownePark.Models.Vo.SiteStatisticDetailVo { Date = new DateTime(year, month, 1), Type = bs_sitestatisticdetailchoice.Forecast, ExternalRevenue = 100m });
            siteData.SiteStatistics.Add(new TownePark.Models.Vo.SiteStatisticDetailVo { Date = new DateTime(year, month, 2), Type = bs_sitestatisticdetailchoice.Forecast, ExternalRevenue = 200m });
            siteData.SiteStatistics.Add(new TownePark.Models.Vo.SiteStatisticDetailVo { Date = new DateTime(year, month, 3), Type = bs_sitestatisticdetailchoice.Forecast, ExternalRevenue = 300m });

            var monthValueDto = new MonthValueDto();

            // Act
            _calculator.CalculateAndApply(siteData, year, month, month, monthValueDto, siteDetailDto, 0m, new List<PnlRowDto>());

            // Assert
            var revenueShare = siteDetailDto.InternalRevenueBreakdown?.RevenueShare;
            Assert.NotNull(revenueShare);
            // Combined total external revenue should be 600
            Assert.Equal(600m, revenueShare.ForecastedExternalRevenue);
            // With a single 30% tier, revenue share total should be 180
            Assert.Equal(180m, revenueShare.Total);
        }

        [Fact]
        public void CalculateAndApply_CurrentMonth_FullyActualized_UsesAllActuals()
        {
            // Arrange
            var year = DateTime.Today.Year;
            var month = DateTime.Today.Month;

            var siteData = new InternalRevenueDataVo
            {
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.RevenueShare }
                },
                RevenueShareThresholds = new List<RevenueShareThresholdVo>
                {
                    new RevenueShareThresholdVo
                    {
                        ThresholdStructure = new ThresholdStructureVo
                        {
                            Tiers = new List<ThresholdTierVo>
                            {
                                new ThresholdTierVo
                                {
                                    Amount = 0m,
                                    SharePercentage = 25.0m, // 25% stored as whole number
                                    EffectiveFrom = new DateTime(year, 1, 1),
                                    EffectiveTo = new DateTime(year, 12, 31)
                                }
                            }
                        }
                    }
                },
                SiteStatistics = new List<TownePark.Models.Vo.SiteStatisticDetailVo>()
            };

            var siteDetailDto = new SiteMonthlyRevenueDetailDto
            {
                InternalActuals = new InternalRevenueActualsVo
                {
                    Year = year,
                    Month = month,
                    DailyActuals = new List<DailyActualVo>
                    {
                        new DailyActualVo { Date = new DateTime(year, month, 1).ToString("yyyy-MM-dd"), ExternalRevenue = 100m },
                        new DailyActualVo { Date = new DateTime(year, month, 2).ToString("yyyy-MM-dd"), ExternalRevenue = 200m },
                        new DailyActualVo { Date = new DateTime(year, month, 3).ToString("yyyy-MM-dd"), ExternalRevenue = 300m }
                    }
                }
            };

            // No forecast after cutoff; fully actualized
            var monthValueDto = new MonthValueDto();

            // Act
            _calculator.CalculateAndApply(siteData, year, month, month, monthValueDto, siteDetailDto, 0m, new List<PnlRowDto>());

            // Assert
            var revenueShare = siteDetailDto.InternalRevenueBreakdown?.RevenueShare;
            Assert.NotNull(revenueShare);
            // Fully actualized: Actual = 600; Forecast = 0
            Assert.Equal(600m, revenueShare.ActualExternalRevenue);
            Assert.Equal(0m, revenueShare.ForecastedExternalRevenue);
            // With a single 25% tier, revenue share total should be 150
            Assert.Equal(150m, revenueShare.Total);
        }

        [Fact]
        public void CalculateAndApply_WithPassedExternalRevenue_UsesPassedValue()
        {
            // Arrange
            var siteData = new InternalRevenueDataVo
            {
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.RevenueShare }
                },
                RevenueShareThresholds = new List<RevenueShareThresholdVo>
                {
                    new RevenueShareThresholdVo
                    {
                        ThresholdStructure = new ThresholdStructureVo
                        {
                            Tiers = new List<ThresholdTierVo>
                            {
                                new ThresholdTierVo
                                {
                                    Amount = 0m,
                                    SharePercentage = 50.0m, // 50% stored as whole number
                                    EffectiveFrom = new DateTime(2025, 1, 1),
                                    EffectiveTo = new DateTime(2025, 12, 31)
                                }
                            }
                        }
                    }
                }
            };
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();
            decimal passedExternalRevenue = 2000m;

            // Act
            _calculator.CalculateAndApply(siteData, 2025, 5, 1, monthValueDto, siteDetailDto, passedExternalRevenue, new List<PnlRowDto>());

            // Assert
            var revenueShare = siteDetailDto.InternalRevenueBreakdown?.RevenueShare;
            Assert.NotNull(revenueShare);
            Assert.Equal(passedExternalRevenue, revenueShare.ForecastedExternalRevenue);
            Assert.Equal(1000m, revenueShare.Total); // 50% of 2000 = 1000
        }

        [Fact]
        public void CalculateAndApply_WithZeroExternalRevenue_UsesZero()
        {
            // Arrange
            var siteData = new InternalRevenueDataVo
            {
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.RevenueShare }
                },
                RevenueShareThresholds = new List<RevenueShareThresholdVo>
                {
                    new RevenueShareThresholdVo
                    {
                        ThresholdStructure = new ThresholdStructureVo
                        {
                            Tiers = new List<ThresholdTierVo>
                            {
                                new ThresholdTierVo
                                {
                                    Amount = 0m,
                                    SharePercentage = 50.0m, // 50% stored as whole number
                                    EffectiveFrom = new DateTime(2025, 1, 1),
                                    EffectiveTo = new DateTime(2025, 12, 31)
                                }
                            }
                        }
                    }
                }
            };
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();
            decimal passedExternalRevenue = 0m;

            // Act
            _calculator.CalculateAndApply(siteData, 2025, 5, 1, monthValueDto, siteDetailDto, passedExternalRevenue, new List<PnlRowDto>());

            // Assert
            var revenueShare = siteDetailDto.InternalRevenueBreakdown?.RevenueShare;
            Assert.NotNull(revenueShare);
            Assert.Equal(0m, revenueShare.ForecastedExternalRevenue);
            Assert.Equal(0m, revenueShare.Total);
        }

        [Fact]
        public void CalculateAndApply_WithMultipleTiers_CalculatesCorrectly()
        {
            // Arrange
            var siteData = new InternalRevenueDataVo
            {
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.RevenueShare }
                },
                RevenueShareThresholds = new List<RevenueShareThresholdVo>
                {
                    new RevenueShareThresholdVo
                    {
                        ThresholdStructure = new ThresholdStructureVo
                        {
                            Tiers = new List<ThresholdTierVo>
                            {
                                new ThresholdTierVo
                                {
                                    Amount = 1000m, // First tier ceiling = 1000
                                    SharePercentage = 30.0m, // 30% for first 1000 (stored as whole number)
                                    EffectiveFrom = new DateTime(2025, 1, 1),
                                    EffectiveTo = new DateTime(2025, 12, 31)
                                },
                                new ThresholdTierVo
                                {
                                    Amount = 0m, // Open-ended final tier
                                    SharePercentage = 50.0m, // 50% for amounts above 1000 (stored as whole number)
                                    EffectiveFrom = new DateTime(2025, 1, 1),
                                    EffectiveTo = new DateTime(2025, 12, 31)
                                }
                            }
                        }
                    }
                }
            };
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();
            decimal passedExternalRevenue = 1500m; // First 1000 at 30%, next 500 at 50%

            // Act
            _calculator.CalculateAndApply(siteData, 2025, 5, 1, monthValueDto, siteDetailDto, passedExternalRevenue, new List<PnlRowDto>());

            // Assert
            var revenueShare = siteDetailDto.InternalRevenueBreakdown?.RevenueShare;
            Assert.NotNull(revenueShare);
            Assert.Equal(passedExternalRevenue, revenueShare.ForecastedExternalRevenue);
            
            // Expected calculation: (1000 * 0.30) + (500 * 0.50) = 300 + 250 = 550
            Assert.Equal(550m, revenueShare.Total);
            Assert.Equal(2, revenueShare.Tiers.Count);
        }

        [Fact]
        public void CalculateAndApply_WithRealWorldExample_CalculatesCorrectly()
        {
            // Arrange - Using the example from the user query
            var siteData = new InternalRevenueDataVo
            {
                Contract = new ContractDataVo
                {
                    ContractTypes = new List<bs_contracttypechoices> { bs_contracttypechoices.RevenueShare }
                },
                RevenueShareThresholds = new List<RevenueShareThresholdVo>
                {
                    new RevenueShareThresholdVo
                    {
                        ThresholdStructure = new ThresholdStructureVo
                        {
                            Tiers = new List<ThresholdTierVo>
                            {
                                new ThresholdTierVo
                                {
                                    Amount = 0m,
                                    SharePercentage = 30.0m, // 30% stored as whole number
                                    EffectiveFrom = new DateTime(2025, 1, 1),
                                    EffectiveTo = new DateTime(2025, 12, 31)
                                }
                            }
                        }
                    }
                }
            };
            var siteDetailDto = new SiteMonthlyRevenueDetailDto();
            var monthValueDto = new MonthValueDto();
            decimal passedExternalRevenue = 133509635.10m; // From user query

            // Act
            _calculator.CalculateAndApply(siteData, 2025, 5, 1, monthValueDto, siteDetailDto, passedExternalRevenue, new List<PnlRowDto>());

            // Assert
            var revenueShare = siteDetailDto.InternalRevenueBreakdown?.RevenueShare;
            Assert.NotNull(revenueShare);
            Assert.Equal(passedExternalRevenue, revenueShare.ForecastedExternalRevenue);
            
            // Expected calculation: 133,509,635.10 * 0.30 = 40,052,890.53
            Assert.Equal(40052890.53m, revenueShare.Total);
            Assert.Single(revenueShare.Tiers);
            Assert.Equal(40052890.53m, revenueShare.Tiers[0].ShareAmount);
        }
    }
} 