using api.Models.Vo;
using api.Models.Vo.Enum;
using api.Services;
using api.Usecases.Impl;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace BackendTests.Usecases
{
    public class ValidateAndPopulateGlCodesTest
    {
        private readonly IConfigDataService _configDataService;
        private readonly ValidateAndPopulateGlCodes _validateAndPopulateGlCodes;

        public ValidateAndPopulateGlCodesTest()
        {
            _configDataService = Substitute.For<IConfigDataService>();
            _validateAndPopulateGlCodes = new ValidateAndPopulateGlCodes(_configDataService);
        }

        [Fact]
        public void Apply_ShouldThrowArgumentException_WhenInvalidServiceCodeProvided()
        {
            var glCodes = new List<GlCodeVo>
            {
                new GlCodeVo { Code = "ValidServiceCode", Type = GlCodeType.Service }
            };
            _configDataService.GetGlCodes(Arg.Any<List<string>>()).Returns(new ContractConfigVo { GlCodes = glCodes });

            var contractDetailVo = new ContractDetailVo
            {
                FixedFee = new ContractDetailVo.FixedFeeVo
                {
                    ServiceRates = new List<ContractDetailVo.ServiceRateVo>
                    {
                        new ContractDetailVo.ServiceRateVo { Code = "InvalidServiceCode" }
                    }
                }
            };

            Action act = () => _validateAndPopulateGlCodes.Apply(contractDetailVo);

            act.Should().Throw<ArgumentException>().WithMessage("Invalid fixed fee service GL Code: InvalidServiceCode");
        }

        [Fact]
        public void Apply_ShouldThrowArgumentException_WhenInvalidJobNameProvided()
        {
            var glCodes = new List<GlCodeVo>
            {
                new GlCodeVo { Code = "JobCode1", Name = "ValidJobName", Type = GlCodeType.SalariedJob }
            };
            _configDataService.GetGlCodes(Arg.Any<List<string>>()).Returns(new ContractConfigVo { GlCodes = glCodes });

            var contractDetailVo = new ContractDetailVo
            {
                PerLaborHour = new ContractDetailVo.PerLaborHourVo
                {
                    JobRates = new List<ContractDetailVo.JobRateVo>
                    {
                        new ContractDetailVo.JobRateVo { Name = "InvalidJobName" }
                    }
                }
            };

            Action act = () => _validateAndPopulateGlCodes.Apply(contractDetailVo);

            act.Should().Throw<ArgumentException>().WithMessage("Invalid per labor hour job: InvalidJobName");
        }
        
        [Fact]
        public void Apply_ShouldPopulateJobCodes_WhenAllJobNamesAreValid()
        {
            var glCodes = new List<GlCodeVo>
            {
                new GlCodeVo { Code = "JobCode1", Name = "ValidJobName1", Type = GlCodeType.SalariedJob },
                new GlCodeVo { Code = "JobCode2", Name = "ValidJobName2", Type = GlCodeType.NonSalariedJob }
            };
            _configDataService.GetGlCodes(Arg.Any<List<string>>()).Returns(new ContractConfigVo { GlCodes = glCodes });

            var contractDetailVo = new ContractDetailVo
            {
                PerLaborHour = new ContractDetailVo.PerLaborHourVo
                {
                    JobRates = new List<ContractDetailVo.JobRateVo>
                    {
                        new ContractDetailVo.JobRateVo { Name = "ValidJobName1" },
                        new ContractDetailVo.JobRateVo { Name = "ValidJobName2" }
                    }
                }
            };

            _validateAndPopulateGlCodes.Apply(contractDetailVo);

            contractDetailVo.PerLaborHour.JobRates[0].Code.Should().Be("JobCode1");
            contractDetailVo.PerLaborHour.JobRates[1].Code.Should().Be("JobCode2");
        }

        [Fact]
        public void Apply_ShouldPopulateOccupiedRoomCode_WhenValidCodeExists()
        {
            var glCodes = new List<GlCodeVo>
            {
                new GlCodeVo { Code = "OccupiedRoomCode", Type = GlCodeType.PerOccupiedRoom }
            };
            _configDataService.GetGlCodes(Arg.Any<List<string>>()).Returns(new ContractConfigVo { GlCodes = glCodes });

            var contractDetailVo = new ContractDetailVo
            {
                PerOccupiedRoom = new ContractDetailVo.PerOccupiedRoomVo()
            };

            _validateAndPopulateGlCodes.Apply(contractDetailVo);

            contractDetailVo.PerOccupiedRoom.Code.Should().Be("OccupiedRoomCode");
        }

        [Fact]
        public void Apply_ShouldProcessSuccessfully_WhenAllCodesAndNamesAreValid()
        {
            var glCodes = new List<GlCodeVo>
            {
                new GlCodeVo { Code = "ValidServiceCode1", Type = GlCodeType.Service },
                new GlCodeVo { Code = "ValidServiceCode2", Type = GlCodeType.Service },
                new GlCodeVo { Code = "ValidServiceCode1", Type = GlCodeType.Service }, // Duplicate
                new GlCodeVo { Code = "ValidJobCode1", Name = "ValidJobName1", Type = GlCodeType.SalariedJob },
                new GlCodeVo { Code = "ValidJobCode2", Name = "ValidJobName2", Type = GlCodeType.NonSalariedJob },
                new GlCodeVo { Code = "ValidJobCode1", Name = "ValidJobName1", Type = GlCodeType.SalariedJob }, // Duplicate
                new GlCodeVo { Code = "OccupiedRoomCode", Type = GlCodeType.PerOccupiedRoom }
            };
            _configDataService.GetGlCodes(Arg.Any<List<string>>()).Returns(new ContractConfigVo { GlCodes = glCodes });

            var contractDetailVo = new ContractDetailVo
            {
                FixedFee = new ContractDetailVo.FixedFeeVo
                {
                    ServiceRates = new List<ContractDetailVo.ServiceRateVo>
                    {
                        new ContractDetailVo.ServiceRateVo { Code = "ValidServiceCode1" }
                    }
                },
                PerLaborHour = new ContractDetailVo.PerLaborHourVo
                {
                    JobRates = new List<ContractDetailVo.JobRateVo>
                    {
                        new ContractDetailVo.JobRateVo { Name = "ValidJobName1" },
                        new ContractDetailVo.JobRateVo { Name = "ValidJobName2" }
                    }
                },
                PerOccupiedRoom = new ContractDetailVo.PerOccupiedRoomVo()
            };

            Action act = () => _validateAndPopulateGlCodes.Apply(contractDetailVo);

            act.Should().NotThrow();
            contractDetailVo.PerLaborHour.JobRates[0].Code.Should().Be("ValidJobCode1");
            contractDetailVo.PerLaborHour.JobRates[1].Code.Should().Be("ValidJobCode2");
            contractDetailVo.PerOccupiedRoom.Code.Should().Be("OccupiedRoomCode");
        }
    }
}