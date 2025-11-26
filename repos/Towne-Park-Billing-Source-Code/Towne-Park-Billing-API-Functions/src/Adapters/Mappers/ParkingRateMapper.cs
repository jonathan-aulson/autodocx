using System;
using System.Collections.Generic;
using System.Linq;
using TownePark.Billing.Api.Models.Vo;
using TownePark.Billing.Api.Models.Enums;
using TownePark.Billing.Api.Helpers;

namespace TownePark.Billing.Api.Adapters.Mappers
{
    public static class ParkingRateMapper
    {
        public static ParkingRateDetailVo MapToParkingRateDetailVo(this IDictionary<string, object> row)
        {
            return new ParkingRateDetailVo
            {
                Id = row.GetValue<Guid>("Id"),
                Rate = row.GetValue<decimal>("Rate"),
                Month = row.GetValue<int>("Month"),
                RateCategory = row.GetValue<RateCategoryTypes>("RateCategory"),
                Type = row.GetValue<ParkingRateDetailTypes>("Type"),
                IsIncrease = row.GetValue<bool>("IsIncrease"),
                IncreaseAmount = row.GetValue<decimal>("IncreaseAmount")
            };
        }

        public static List<ParkingRateDetailVo> MapToParkingRateDetailVoList(List<Dictionary<string, object>> rawResults)
        {
            return rawResults.Select(MapToParkingRateDetailVo).ToList();
        }

        public static List<ParkingRateDetailVo> MapRowToParkingRateDetails(Dictionary<string, object> row)
        {
            var period = row.GetValue<string>("PERIOD");
            int month = 0;
            if (!string.IsNullOrEmpty(period) && period.Length >= 6)
                int.TryParse(period.Substring(4, 2), out month);

            var typeString = row.GetValue<string>("TYPE");
            ParkingRateDetailTypes type = typeString?.ToUpper() == "ACTUAL"
                ? ParkingRateDetailTypes.Actual
                : ParkingRateDetailTypes.Budget;

            var details = new List<ParkingRateDetailVo>();

            details.Add(new ParkingRateDetailVo
            {
                Month = month,
                Rate = row.GetValue<decimal>("Valet_Daily_Rate"),
                RateCategory = RateCategoryTypes.ValetDaily,
                Type = type
            });
            details.Add(new ParkingRateDetailVo
            {
                Month = month,
                Rate = row.GetValue<decimal>("Valet_Overnight_Rate"),
                RateCategory = RateCategoryTypes.ValetOvernight,
                Type = type
            });
            details.Add(new ParkingRateDetailVo
            {
                Month = month,
                Rate = row.GetValue<decimal>("Valet_Monthly_Rate"),
                RateCategory = RateCategoryTypes.ValetMonthly,
                Type = type
            });
            details.Add(new ParkingRateDetailVo
            {
                Month = month,
                Rate = row.GetValue<decimal>("Self_Daily_Rate"),
                RateCategory = RateCategoryTypes.SelfDaily,
                Type = type
            });
            details.Add(new ParkingRateDetailVo
            {
                Month = month,
                Rate = row.GetValue<decimal>("Self_Overnight_Rate"),
                RateCategory = RateCategoryTypes.SelfOvernight,
                Type = type
            });
            details.Add(new ParkingRateDetailVo
            {
                Month = month,
                Rate = row.GetValue<decimal>("Self_Monthly_Rate"),
                RateCategory = RateCategoryTypes.SelfMonthly,
                Type = type
            });

            return details;
        }

        public static ParkingRateDataVo MapToParkingRateDataVo(List<Dictionary<string, object>> rawResults)
        {
            var vo = new ParkingRateDataVo();

            if (rawResults.Count > 0)
            {
                var firstRow = rawResults[0];

                
                var period = firstRow.GetValue<string>("PERIOD");
                if (!string.IsNullOrEmpty(period) && period.Length >= 4)
                {
                    int year;
                    if (int.TryParse(period.Substring(0, 4), out year))
                        vo.Year = year;
                }
            }

            var allDetails = rawResults.SelectMany(MapRowToParkingRateDetails).ToList();

            vo.BudgetRates = allDetails
                .Where(d => d.Type == ParkingRateDetailTypes.Budget)
                .ToList();

            vo.ActualRates = allDetails
                .Where(d => d.Type == ParkingRateDetailTypes.Actual)
                .ToList();

            // ForecastRates remains null (default)
            return vo;
        }
    }
} 