using api.Models.Vo;
using TownePark;

namespace api.Services
{
    public interface IParkingRateService
    {
        Task<ParkingRateDataVo> GetParkingRatesAsync(Guid siteId, int year);
        Task<ParkingRateDataVo> SaveParkingRates(ParkingRateDataVo update);
    }
} 