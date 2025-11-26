using api.Adapters.Mappers;
using api.Models.Dto;
using api.Services;
using System; // Added for Guid

namespace api.Adapters.Impl
{
    public class ParkingRateServiceAdapter : IParkingRateServiceAdapter
    {
        private readonly IParkingRateService _parkingRateService;

        public ParkingRateServiceAdapter(IParkingRateService parkingRateService)
        {
            _parkingRateService = parkingRateService ?? throw new ArgumentNullException(nameof(parkingRateService));
        }

        public ParkingRateDataDto? GetParkingRates(Guid siteId, int year)
        {
            var parkingRateVo = _parkingRateService.GetParkingRatesAsync(siteId, year).Result;
            return ParkingRateMapper.ParkingRateVoToDto(parkingRateVo);
        }

        public async Task<ParkingRateDataDto?> SaveParkingRates(ParkingRateDataDto update)
        {
            var result = await _parkingRateService.SaveParkingRates(ParkingRateMapper.ParkingRateDtoToVo(update));
            return ParkingRateMapper.ParkingRateVoToDto(result);
        }
    }
} 