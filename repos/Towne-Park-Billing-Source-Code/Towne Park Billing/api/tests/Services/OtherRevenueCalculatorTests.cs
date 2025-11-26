using System;
using System.Collections.Generic;
using System.Linq;
using api.Services.Impl.Calculators;
using TownePark.Models.Vo;
using api.Models.Vo;
using api.Models.Dto;
using Xunit;
using TownePark;

namespace BackendTests.Services
{
    public class OtherRevenueCalculatorTests
    {
        private readonly OtherRevenueCalculator _calculator = new OtherRevenueCalculator();

        private static InternalRevenueDataVo BuildSiteData(
            bool isManagementAgreement,
            bool profitShareEnabled,
            bool includeRevenueShareInContractTypes,
            string monthYear,
            decimal cpeAmount,
            decimal otherAmount)
        {
            var site = new InternalRevenueDataVo
            {
                SiteId = Guid.NewGuid(),
                SiteNumber = "S1",
                SiteName = "Test Site",
                Contract = new ContractDataVo()
                {
                    ContractId = Guid.NewGuid(),
                    ContractType = isManagementAgreement ? "Management" : "Other",
                    ContractTypes = new List<bs_contracttypechoices>()
                }
            };

            if (includeRevenueShareInContractTypes)
            {
                site.Contract.ContractTypes = new List<bs_contracttypechoices>() { bs_contracttypechoices.RevenueShare };
            }

            if (isManagementAgreement)
            {
                site.ManagementAgreement = new ManagementAgreementVo
                {
                    ProfitShareEnabled = profitShareEnabled
                };
            }

            // Add OtherRevenues VO with ForecastData
            var otherVo = new TownePark.Models.Vo.OtherRevenueVo
            {
                ForecastData = new List<api.Models.Vo.OtherRevenueDetailVo>
                {
                    new api.Models.Vo.OtherRevenueDetailVo
                    {
                        MonthYear = monthYear,
                        ClientPaidExpense = cpeAmount,
                        BillableExpense = otherAmount,
                        Credits = 0m,
                        GPOFees = 0m,
                        RevenueValidation = 0m,
                        SigningBonus = 0m,
                        Miscellaneous = 0m,
                        Type = OtherRevenueType.Forecast
                    }
                }
            };

            site.OtherRevenues = new List<TownePark.Models.Vo.OtherRevenueVo> { otherVo };

            return site;
        }

        private static SiteMonthlyRevenueDetailDto BuildSiteDetailDto()
        {
            return new SiteMonthlyRevenueDetailDto
            {
                SiteId = Guid.NewGuid().ToString(),
                InternalRevenueBreakdown = new InternalRevenueBreakdownDto
                {
                    OtherRevenue = new OtherRevenueInternalRevenueDto()
                }
            };
        }

        [Fact]
        public void ProfitShareOnly_ExcludesCpeFromInternalRevenue()
        {
            // month = June 2025
            int year = 2025, month = 6;
            var siteData = BuildSiteData(isManagementAgreement: true, profitShareEnabled: true, includeRevenueShareInContractTypes: false,
                monthYear: "2025-06", cpeAmount: -5000m, otherAmount: 1200m);

            var siteDetail = BuildSiteDetailDto();
            var monthValueDto = new MonthValueDto { Month = month - 1 };

            _calculator.CalculateAndApply(siteData, year, month, month, monthValueDto, siteDetail, 100000m, new List<PnlRowDto>());

            // CPE should be excluded from Internal OtherRevenue -> expected = nonCpeTotal = 1200
            Assert.Equal(1200m, siteDetail.InternalRevenueBreakdown.OtherRevenue.Total);
        }

        [Fact]
        public void ContractTypesContainsRevenueShare_IncludesCpeInInternalRevenue()
        {
            int year = 2025, month = 6;
            // Even though MA + ProfitShareEnabled, ContractTypes includes RevenueShare -> NOT profitShareOnly
            var siteData = BuildSiteData(isManagementAgreement: true, profitShareEnabled: true, includeRevenueShareInContractTypes: true,
                monthYear: "2025-06", cpeAmount: -5000m, otherAmount: 1200m);

            var siteDetail = BuildSiteDetailDto();
            var monthValueDto = new MonthValueDto { Month = month - 1 };

            _calculator.CalculateAndApply(siteData, year, month, month, monthValueDto, siteDetail, 100000m, new List<PnlRowDto>());

            // CPE should be included in Internal OtherRevenue -> expected = nonCpeTotal + cpe = 1200 - 5000 = -3800
            Assert.Equal(-3800m, siteDetail.InternalRevenueBreakdown.OtherRevenue.Total);
        }

        [Fact]
        public void NonProfitShare_IncludesCpeInInternalRevenue()
        {
            int year = 2025, month = 6;
            var siteData = BuildSiteData(isManagementAgreement:false, profitShareEnabled:false, includeRevenueShareInContractTypes:false,
                monthYear:"2025-06", cpeAmount:-2000m, otherAmount:500m);

            var siteDetail = BuildSiteDetailDto();
            var monthValueDto = new MonthValueDto { Month = month - 1 };

            _calculator.CalculateAndApply(siteData, year, month, month, monthValueDto, siteDetail, 50000m, new List<PnlRowDto>());

            Assert.Equal(-1500m, siteDetail.InternalRevenueBreakdown.OtherRevenue.Total); // 500 + (-2000)
        }
    }
}
