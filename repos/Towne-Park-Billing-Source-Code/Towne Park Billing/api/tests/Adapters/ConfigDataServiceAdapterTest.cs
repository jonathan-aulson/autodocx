using System.Collections.Generic;
using api.Adapters.Impl;
using api.Models.Dto;
using api.Models.Vo;
using api.Models.Vo.Enum;
using api.Services;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace BackendTests.Adapters
{
    public class ConfigDataServiceAdapterTest
    {
        private readonly IConfigDataService _glCodeServiceMock;
        private readonly ConfigDataServiceAdapter _glCodeServiceAdapter;

        public ConfigDataServiceAdapterTest()
        {
            _glCodeServiceMock = Substitute.For<IConfigDataService>();
            _glCodeServiceAdapter = new ConfigDataServiceAdapter(_glCodeServiceMock);
        }

        [Fact]
        public void GetGlCodes_ShouldCallGlCodeServiceAndReturnAdaptedResponse()
        {
            // Arrange
            var codeTypes = new List<string> { "RevenueShare", "ManagementAgreement" };
            var contractConfigVo = new ContractConfigVo 
            {
                DefaultRate = 20.00m,
                DefaultOvertimeRate =  40.00m,
                DefaultFee = 1000.00m,
                GlCodes = new List<GlCodeVo>
                {
                    new GlCodeVo
                    {
                        Code = "4790",
                        Name = "Revenue Share",
                        Type = GlCodeType.RevenueShare
                    },
                    new GlCodeVo
                    {
                        Code = "4791",
                        Name = "Management Agreement",
                        Type = GlCodeType.ManagementAgreement
                    }
                }
            };
            _glCodeServiceMock.GetGlCodes(codeTypes).Returns(contractConfigVo);

            var expectedGlCodesDto = new ContractConfigDto
            {
                DefaultRate = 20.00m,
                DefaultOvertimeRate = 40.00m,
                DefaultFee = 1000.00m,
                GlCodes = new List<GlCodeDto>
                {
                    new GlCodeDto
                    {
                        Code = "4790",
                        Name = "Revenue Share",
                        Type = "RevenueShare"
                    },
                    new GlCodeDto
                    {
                        Code = "4791",
                        Name = "Management Agreement",
                        Type = "ManagementAgreement"
                    }
                }
            };

            // Act
            var result = _glCodeServiceAdapter.GetGlCodes(codeTypes);

            // Assert
            result.Should().BeEquivalentTo(expectedGlCodesDto);
        }
    }
}
