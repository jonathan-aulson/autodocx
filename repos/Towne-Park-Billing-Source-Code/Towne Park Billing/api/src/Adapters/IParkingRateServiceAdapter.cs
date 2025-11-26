using api.Models.Dto;
using System;

namespace api.Adapters
{
    public interface IParkingRateServiceAdapter
    {
        ParkingRateDataDto? GetParkingRates(Guid siteId, int year);
        Task<ParkingRateDataDto?> SaveParkingRates(ParkingRateDataDto update);
    }
}