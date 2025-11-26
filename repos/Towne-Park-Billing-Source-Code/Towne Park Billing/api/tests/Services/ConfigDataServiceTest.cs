using System.Collections.Generic;
using api.Models.Vo;
using api.Data;
using api.Services.Impl;
using FluentAssertions;
using NSubstitute;
using TownePark;
using Xunit;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using api.Models.Vo.Enum;

namespace BackendTests.Services
{
    public class ConfigDataServiceTest
    {
        private readonly IConfigDataRepository _glCodeRepositoryMock;
        private readonly ConfigDataService _glCodeService;

        public ConfigDataServiceTest()
        {
            _glCodeRepositoryMock = Substitute.For<IConfigDataRepository>();
            _glCodeService = new ConfigDataService(_glCodeRepositoryMock);
        }

        [Fact]
        public void GetGlCodes_ShouldCallGlCodeRepositoryAndReturnAdaptedResponse()
        {
            // Arrange
            var codeTypes = new List<string> { "RevenueShare", "ManagementAgreement" };
            var codeTypeEnums = new List<bs_glcodetypechoices>
            {
                bs_glcodetypechoices.RevenueShare,
                bs_glcodetypechoices.ManagementAgreement
            };

            var glCodesModel = new List<bs_GLCodeConfig>
            {
                new bs_GLCodeConfig
                {
                    bs_Code = "4790",
                    bs_Name = "Revenue Share",
                    bs_Type = bs_glcodetypechoices.RevenueShare,
                    bs_Data = null
                },
                new bs_GLCodeConfig
                {
                    bs_Code = "4791",
                    bs_Name = "Management Agreement",
                    bs_Type = bs_glcodetypechoices.ManagementAgreement,
                    bs_Data = null
                },
                new bs_GLCodeConfig
                {
                    bs_Code = "0000",
                    bs_Name = "Default Rates & Fees",
                    bs_Type = bs_glcodetypechoices.RateAndFeeData,
                    bs_Data = "{\"fee\":1000.0,\"rate\":20.0,\"overtimeRate\":40.0}"
                }
            };

            _glCodeRepositoryMock.GetGlCodes(Arg.Is<IEnumerable<bs_glcodetypechoices>>(x => x.SequenceEqual(codeTypeEnums)))
                .Returns(glCodesModel.AsEnumerable());

            var expected = new ContractConfigVo
            {
                DefaultFee = 1000.0m,
                DefaultRate = 20.0m,
                DefaultOvertimeRate = 40.0m,
                GlCodes = new List<GlCodeVo>
                {
                    new GlCodeVo
                    {
                        Code = "4790",
                        Name = "Revenue Share",
                        Type = GlCodeType.RevenueShare,
                    },
                    new GlCodeVo
                    {
                        Code = "4791",
                        Name = "Management Agreement",
                        Type = GlCodeType.ManagementAgreement,
                    },
                    new GlCodeVo
                    {
                        Code = "0000",
                        Name = "Default Rates & Fees",
                        Type = GlCodeType.RateAndFeeData,
                    }
                }
            };

            // Act
            var result = _glCodeService.GetGlCodes(codeTypes);

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void GetGlCodes_ShouldThrowArgumentException_WhenCodeTypeNotValid()
        {
            // Arrange
            var codeTypes = new List<string> { "InvalidCodeType" };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _glCodeService.GetGlCodes(codeTypes));
        }
    }
}
