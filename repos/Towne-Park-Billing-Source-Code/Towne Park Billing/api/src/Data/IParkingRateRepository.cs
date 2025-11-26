using api.Models.Vo;
using TownePark;

namespace api.Data // Assuming repositories are in the api.Data namespace
{
    public interface IParkingRateRepository
    {
        // Returns the raw entity from CRM, potentially including linked details
        bs_ParkingRate? GetParkingRateWithDetails(Guid siteId, int year);
        List<bs_ParkingRate> GetParkingRatesWithDetails(IEnumerable<Guid> siteIds, int year);
        void SaveParkingRates(bs_ParkingRate update);
        void CreateParkingRates(bs_ParkingRate model);


        /// <summary>
        /// Gets the site number for a given siteId.
        /// </summary>
        /// <param name="siteId">Site GUID</param>
        /// <returns>Site number as string, or null if not found</returns>
        string? GetSiteNumber(Guid siteId);
        /// <summary>
        /// Fetches parking rate data (Budget and Actual) via EDW stored procedure.
        /// </summary>
        /// <param name="siteNumber">Site number (string)</param>
        /// <param name="year">Year (int)</param>
        /// <returns>ParkingRateDataVo or null</returns>
        Task<ParkingRateDataVo?> GetParkingRateDataFromEDW(string siteNumber, int year);
    }
}