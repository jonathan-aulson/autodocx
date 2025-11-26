using api.Adapters;
using api.Functions;
using api.Models.Dto;
using FluentAssertions;
using Microsoft.Azure.Functions.Worker;
using Newtonsoft.Json;
using NSubstitute;
using System.Net;
using System.Text;
using Xunit;

namespace BackendTests.Functions
{
    public class SiteStatisticsTest
    {
        private readonly SiteStatistics _siteStatistics;
        private readonly ISiteStatisticServiceAdapter _siteStatisticServiceAdapter;

        public SiteStatisticsTest()
        {
            _siteStatisticServiceAdapter = Substitute.For<ISiteStatisticServiceAdapter>();
            _siteStatistics = new SiteStatistics(_siteStatisticServiceAdapter);
        }

        [Fact]
        public async Task GetSiteStatistics_ShouldReturnSiteStats()
        {
            var siteId = Guid.NewGuid();
            var billingPeriod = "2024-07";
            var SiteStatisticDto = GetTestDto();

            _siteStatisticServiceAdapter
                .GetSiteStatistics(siteId, billingPeriod, Arg.Any<string>())
                .Returns(SiteStatisticDto);

            var context = Substitute.For<FunctionContext>();
            var body = new MemoryStream();
            var requestData = new FakeHttpRequestData(
                context,
                new Uri($"http://localhost:7275/api/siteStatistics/{siteId}/{billingPeriod}"),
                body);

            var result = _siteStatistics.GetSiteStatistics(requestData, siteId, billingPeriod);
            result.StatusCode.Should().Be(HttpStatusCode.OK);

            result.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = new StreamReader(result.Body).ReadToEnd();
            var siteStats = JsonConvert.DeserializeObject<List<SiteStatisticDto>>(responseBody);

            siteStats.Should()
                .NotBeNull()
                .And.BeEquivalentTo(SiteStatisticDto);
        }

        [Fact]
        public void SaveSiteStatistics_ShouldReturnOk_WhenUpdateIsSuccessful()
        {
            var siteStatisticId = Guid.NewGuid();
            var siteStatisticDto = GetTestDto().First();

            var context = Substitute.For<FunctionContext>();
            var body = new MemoryStream();
            var requestData = new FakeHttpRequestData(
                context,
                new Uri($"http://localhost:7275/api/siteStatistics"),
                body);

            var json = JsonConvert.SerializeObject(siteStatisticDto);
            var jsonBytes = Encoding.UTF8.GetBytes(json);
            requestData.Body.Write(jsonBytes, 0, jsonBytes.Length);
            requestData.Body.Seek(0, SeekOrigin.Begin);

            var result = _siteStatistics.SaveSiteStatistics(requestData);
            result.StatusCode.Should().Be(HttpStatusCode.OK);
        }

private List<SiteStatisticDto> GetTestDto()
{
    var totalRooms = 100;
    var occupiedRooms = 70;
    var occupancy = totalRooms > 0 ? (decimal)occupiedRooms / totalRooms : 0m;

    var siteStatisticDto = new SiteStatisticDto
    {
        SiteNumber = "0111",
        Name = "Hotel Parking",
        TotalRooms = totalRooms,
        PeriodLabel = "2025-07",
        BudgetData = new List<SiteStatisticDetailDto>
        {
            new SiteStatisticDetailDto
            {
                PeriodLabel = new DateOnly(2025, 7, 1).ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture),
                ValetRateDaily = 10,
                ValetRateMonthly = 200,
                SelfRateDaily = 5,
                SelfRateMonthly = 100,
                BaseRevenue = 7000,
                OccupiedRooms = occupiedRooms,
                Occupancy = occupancy,
                SelfOvernight = 50,
                ValetOvernight = 20,
                ValetDaily = 40,
                ValetMonthly = 60,
                SelfDaily = 30,
                SelfMonthly = 10,
                ValetComps = 5,
                SelfComps = 5,
                DriveInRatio = 0.5,
                CaptureRatio = 0.87
            },
            new SiteStatisticDetailDto
            {
                PeriodLabel = new DateOnly(2025, 7, 1).ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture),
                ValetRateDaily = 10,
                ValetRateMonthly = 200,
                SelfRateDaily = 5,
                SelfRateMonthly = 100,
                BaseRevenue = 7000,
                OccupiedRooms = occupiedRooms,
                Occupancy = occupancy,
                SelfOvernight = 50,
                ValetOvernight = 20,
                ValetDaily = 40,
                ValetMonthly = 60,
                SelfDaily = 30,
                SelfMonthly = 10,
                ValetComps = 5,
                SelfComps = 5,
                DriveInRatio = 0.5,
                CaptureRatio = 0.87
            }
        },
        ForecastData = new List<SiteStatisticDetailDto>
        {
            new SiteStatisticDetailDto
            {
                PeriodLabel = new DateOnly(2025, 7, 1).ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture),
                ValetRateDaily = 10,
                ValetRateMonthly = 200,
                SelfRateDaily = 5,
                SelfRateMonthly = 100,
                BaseRevenue = 7000,
                OccupiedRooms = occupiedRooms,
                Occupancy = occupancy,
                SelfOvernight = 50,
                ValetOvernight = 20,
                ValetDaily = 40,
                ValetMonthly = 60,
                SelfDaily = 30,
                SelfMonthly = 10,
                ValetComps = 5,
                SelfComps = 5,
                DriveInRatio = 0.5,
                CaptureRatio = 0.87
            },
            new SiteStatisticDetailDto
            {
                PeriodLabel = new DateOnly(2025, 7, 1).ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture),
                ValetRateDaily = 10,
                ValetRateMonthly = 200,
                SelfRateDaily = 5,
                SelfRateMonthly = 100,
                BaseRevenue = 7000,
                OccupiedRooms = occupiedRooms,
                Occupancy = occupancy,
                SelfOvernight = 50,
                ValetOvernight = 20,
                ValetDaily = 40,
                ValetMonthly = 60,
                SelfDaily = 30,
                SelfMonthly = 10,
                ValetComps = 5,
                SelfComps = 5,
                DriveInRatio = 0.5,
                CaptureRatio = 0.87
            }
        }
    };

    return new List<SiteStatisticDto> { siteStatisticDto };
}
    }
}
