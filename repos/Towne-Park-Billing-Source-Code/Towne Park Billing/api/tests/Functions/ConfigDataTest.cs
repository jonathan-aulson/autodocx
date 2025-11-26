using System.Net;
using api.Adapters;
using api.Functions;
using api.Models.Dto;
using api.Models.Vo;
using api.Models.Vo.Enum;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using NSubstitute;
using Xunit;

namespace BackendTests.Functions
{
    public class ConfigDataTest
    {
        private readonly ConfigData _glCodes;
        private readonly IConfigDataServiceAdapter _glCodeServiceAdapterMock;

        public ConfigDataTest()
        {
            var loggerFactoryMock = Substitute.For<ILoggerFactory>();
            _glCodeServiceAdapterMock = Substitute.For<IConfigDataServiceAdapter>();
            _glCodes = new ConfigData(loggerFactoryMock.CreateLogger<ConfigData>(), _glCodeServiceAdapterMock);
        }

        [Fact]
        public void GetGlCodes_ShouldReturnOkWithGlCodes_WhenCodeTypesAreProvided()
        {
            // Arrange
            var sampleCodeTypes = new List<string> { "RevenueShare", "ManagementAgreement" };
            var expectedGlCodes = new ContractConfigDto
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

            _glCodeServiceAdapterMock.GetGlCodes(Arg.Is<List<string>>(x => x.SequenceEqual(sampleCodeTypes)))
                .Returns(expectedGlCodes);

            var context = Substitute.For<FunctionContext>();
            var queryString = "?codeTypes=RevenueShare,ManagementAgreement";
            var requestData = new FakeHttpRequestData(
                context,
                new Uri($"http://localhost:7275/api/gl-codes{queryString}"),
                new MemoryStream()
            );

            // Act
            var result = _glCodes.GetGlCodes(requestData);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = new StreamReader(result.Body).ReadToEnd();
            var contractConfigDto = JsonConvert.DeserializeObject<ContractConfigDto>(responseBody);

            contractConfigDto.Should().NotBeNull();
            contractConfigDto.Should().BeEquivalentTo(expectedGlCodes);
        }


        [Fact]
        public void GetGlCodes_ShouldReturnBadRequest_WhenCodeTypesAreNotProvided()
        {
            var context = Substitute.For<FunctionContext>();
            var requestData = new FakeHttpRequestData(
                context,
                new Uri("http://localhost:7275/api/gl-codes"),
                new MemoryStream()
            );

            var result = _glCodes.GetGlCodes(requestData);

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = new StreamReader(result.Body).ReadToEnd();
            responseBody.Should().Be("Please provide at least one codeType query parameter.");
        }
    }
}
