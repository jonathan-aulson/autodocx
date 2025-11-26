using api.Models.Dto;
using api.Models.Vo;
using Microsoft.Xrm.Sdk;
using Riok.Mapperly.Abstractions;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using TownePark;

namespace api.Adapters.Mappers
{
    [Mapper]
    public static partial class SiteStatisticMapper
    {
  
        public static IEnumerable<SiteStatisticVo>? SiteStatisticModelToVo(List<bs_SiteStatistic> models)
        {
            var voList = new List<SiteStatisticVo>();
            if (models.Count == 0) return null;

            foreach (var model in models)
            {
                var vo = new SiteStatisticVo();

                vo.Id = model.bs_SiteStatisticId;
                vo.SiteNumber = model.bs_CustomerSiteFK.Name;
                vo.CustomerSiteId = model.bs_CustomerSiteFK.Id;
                vo.PeriodLabel = model.bs_BillingPeriod;


                if (model.bs_SiteStatistic_CustomerSite != null &&
                    !string.IsNullOrEmpty(model.bs_SiteStatistic_CustomerSite.bs_TotalRoomsAvailable) &&
                    int.TryParse(model.bs_SiteStatistic_CustomerSite.bs_TotalRoomsAvailable, out int totalRoomsParsed))
                {
                    vo.TotalRooms = totalRoomsParsed;
                }
                else
                {
                    vo.TotalRooms = 0;
                }

                vo.Name = model.bs_Name;


                foreach (var item in model.bs_SiteStatistic_SiteStatisticDetail ?? Enumerable.Empty<bs_SiteStatisticDetail>())
                {

                    if (item == null) continue;

                    var detail = SiteStatisticDetailModelToVo(item);
                    if (detail != null)
                    {
                        if (detail.Type == SiteStatisticDetailType.Forecast)
                        {
                            vo.ForecastData ??= new List<SiteStatisticDetailVo>();
                            vo.ForecastData.Add(detail);
                        }
                        else if (detail.Type == SiteStatisticDetailType.Budget)
                        {
                            vo.BudgetData ??= new List<SiteStatisticDetailVo>();
                            vo.BudgetData.Add(detail);
                        }
                        else if (detail.Type == SiteStatisticDetailType.Actual)
                        {
                            vo.ActualData ??= new List<SiteStatisticDetailVo>();
                            vo.ActualData.Add(detail);
                        }

                        detail.PeriodLabel = detail.Date.ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                    }
                }

                voList.Add(vo);
            }

            return voList;
        }

        public static partial IEnumerable<SiteStatisticDto>? MapSiteStatisticToDto(List<SiteStatisticVo>? vo);

        public static partial SiteStatisticDetailDto MapSiteStatisticDetailToDto(SiteStatisticDetailVo vo);

        public static IEnumerable<SiteStatisticDto>? SiteStatisticVoToDto(List<SiteStatisticVo>? vos)
        {
            var dtoList = new List<SiteStatisticDto>();
            if (vos == null || vos.Count == 0) return null;


            foreach (var vo in vos)
            {
                var dto = new SiteStatisticDto
                {
                    Id = vo.Id,
                    SiteNumber = vo.SiteNumber,
                    CustomerSiteId = vo.CustomerSiteId,
                    Name = vo.Name,
                    TotalRooms = vo.TotalRooms,
                    TimeRangeType = vo.TimeRangeType.ToString(),
                    PeriodLabel = vo.PeriodLabel
                };

                foreach (var item in vo.ForecastData ?? new List<SiteStatisticDetailVo>())
                {
                    var detailDto = MapSiteStatisticDetailToDto(item);
                    if (detailDto != null)
                    {
                        dto.ForecastData.Add(detailDto);
                    }
                }

                foreach (var item in vo.BudgetData ?? new List<SiteStatisticDetailVo>())
                {
                    var detailDto = MapSiteStatisticDetailToDto(item);
                    if (detailDto != null)
                    {
                        dto.BudgetData.Add(detailDto);
                    }
                }


                foreach (var item in vo.ActualData ?? new List<SiteStatisticDetailVo>())
                {
                    // actual data returned from edw can be null
                    // check if actuals are null before mapping
                    if (item != null)
                    {
                        var detailDto = MapSiteStatisticDetailToDto(item);
                        if (detailDto != null)
                        {
                            dto.ActualData.Add(detailDto);
                        }
                    }
                }
                dtoList.Add(dto);
            }

            return dtoList;
        }

        [MapProperty(nameof(bs_SiteStatisticDetail.bs_SiteStatisticDetailId), nameof(SiteStatisticDetailVo.Id))]
        [MapProperty(nameof(bs_SiteStatisticDetail.bs_Type), nameof(SiteStatisticDetailVo.Type))]
        [MapProperty(nameof(bs_SiteStatisticDetail.bs_Date), nameof(SiteStatisticDetailVo.Date))]
        [MapProperty(nameof(bs_SiteStatisticDetail.bs_ValetRateDaily), nameof(SiteStatisticDetailVo.ValetRateDaily))]
        [MapProperty(nameof(bs_SiteStatisticDetail.bs_ValetRateMonthly), nameof(SiteStatisticDetailVo.ValetRateMonthly))]
        [MapProperty(nameof(bs_SiteStatisticDetail.bs_SelfRateDaily), nameof(SiteStatisticDetailVo.SelfRateDaily))]
        [MapProperty(nameof(bs_SiteStatisticDetail.bs_SelfRateMonthly), nameof(SiteStatisticDetailVo.SelfRateMonthly))]
        [MapProperty(nameof(bs_SiteStatisticDetail.bs_BaseRevenue), nameof(SiteStatisticDetailVo.BaseRevenue))]
        [MapProperty(nameof(bs_SiteStatisticDetail.bs_OccupiedRooms), nameof(SiteStatisticDetailVo.OccupiedRooms))]
        [MapProperty(nameof(bs_SiteStatisticDetail.bs_Occupancy), nameof(SiteStatisticDetailVo.Occupancy))]
        [MapProperty(nameof(bs_SiteStatisticDetail.bs_SelfOvernight), nameof(SiteStatisticDetailVo.SelfOvernight))]
        [MapProperty(nameof(bs_SiteStatisticDetail.bs_ValetOvernight), nameof(SiteStatisticDetailVo.ValetOvernight))]
        [MapProperty(nameof(bs_SiteStatisticDetail.bs_ValetDaily), nameof(SiteStatisticDetailVo.ValetDaily))]
        [MapProperty(nameof(bs_SiteStatisticDetail.bs_ValetMonthly), nameof(SiteStatisticDetailVo.ValetMonthly))]
        [MapProperty(nameof(bs_SiteStatisticDetail.bs_SelfDaily), nameof(SiteStatisticDetailVo.SelfDaily))]
        [MapProperty(nameof(bs_SiteStatisticDetail.bs_SelfMonthly), nameof(SiteStatisticDetailVo.SelfMonthly))]
        [MapProperty(nameof(bs_SiteStatisticDetail.bs_ValetComps), nameof(SiteStatisticDetailVo.ValetComps))]
        [MapProperty(nameof(bs_SiteStatisticDetail.bs_SelfComps), nameof(SiteStatisticDetailVo.SelfComps))]
        [MapProperty(nameof(bs_SiteStatisticDetail.bs_DriveInRatio), nameof(SiteStatisticDetailVo.DriveInRatio))]
        [MapProperty(nameof(bs_SiteStatisticDetail.bs_CaptureRatio), nameof(SiteStatisticDetailVo.CaptureRatio))]
        [MapProperty(nameof(bs_SiteStatisticDetail.bs_SelfAggregator), nameof(SiteStatisticDetailVo.SelfAggregator))]
        [MapProperty(nameof(bs_SiteStatisticDetail.bs_ValetAggregator), nameof(SiteStatisticDetailVo.ValetAggregator))]
        [MapProperty(nameof(bs_SiteStatisticDetail.bs_ExternalRevenue), nameof(SiteStatisticDetailVo.ExternalRevenue))]
        [MapProperty(nameof(bs_SiteStatisticDetail.bs_AdjustmentPercentage), nameof(SiteStatisticDetailVo.AdjustmentPercentage))]
        [MapProperty(nameof(bs_SiteStatisticDetail.bs_AdjustmentValue), nameof(SiteStatisticDetailVo.AdjustmentValue))]
        private static partial SiteStatisticDetailVo? SiteStatisticDetailModelToVo(bs_SiteStatisticDetail model);

        public static bs_SiteStatistic SiteStatisticVoToModel(SiteStatisticVo vo)
        {
            var model = new bs_SiteStatistic();
            model.bs_SiteStatisticId = vo.Id;

            if (!string.IsNullOrEmpty(vo.SiteNumber))
            {
                model.bs_CustomerSiteFK = new EntityReference("bs_customersite", vo.CustomerSiteId)
                {
                    Name = vo.SiteNumber
                };
            }

            model.bs_BillingPeriod = vo.PeriodLabel;
            model.bs_Name = vo.Name;

            var details = new List<bs_SiteStatisticDetail>();

            foreach (var item in vo.ForecastData ?? new List<SiteStatisticDetailVo>())
            {
                var detail = SiteStatisticDetailVoToModel(item);
                if (detail != null)
                {
                    
                    if (!string.IsNullOrEmpty(item.PeriodLabel))
                    {
                        bool parsed = false;
                        
                        // Always prioritize US format (MM/dd/yyyy) first
                        try
                        {
                            var dateOnly = DateOnly.ParseExact(item.PeriodLabel, "MM/dd/yyyy", CultureInfo.InvariantCulture);
                            detail.bs_Date = dateOnly.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                            parsed = true;
                        }
                        catch
                        {
                            // Only use yyyy-MM-dd format if MM/dd/yyyy fails and the format looks like ISO date (contains dashes)
                            if (item.PeriodLabel.Contains('-') && item.PeriodLabel.Length == 10)
                            {
                                try
                                {
                                    var dateOnly = DateOnly.ParseExact(item.PeriodLabel, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                                    detail.bs_Date = dateOnly.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                                    parsed = true;
                                }
                                catch
                                {
                                    // yyyy-MM-dd format also failed
                                }
                            }
                        }
                        
                        if (!parsed)
                        {
                            if (item.Date != DateOnly.MinValue)
                            {
                                detail.bs_Date = item.Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                            }
                            else
                            {
                                continue; // Skip if no valid date is available
                            }
                        }
                    }
                    else if (item.Date != DateOnly.MinValue)
                    {
                        detail.bs_Date = item.Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                    }
                    else
                    {
                        continue; // Skip if no valid date is available
                    }
                    details.Add(detail);
                }
            }           

            model.bs_SiteStatistic_SiteStatisticDetail = details;

            return model;
        }

        [MapProperty(nameof(SiteStatisticDto.ActualData), nameof(SiteStatisticVo.ActualData))]
        public static partial SiteStatisticVo? SiteStatisticDtoToVo(SiteStatisticDto? dto);

        [MapProperty(nameof(SiteStatisticDetailVo.Id), nameof(bs_SiteStatisticDetail.bs_SiteStatisticDetailId))]
        [MapProperty(nameof(SiteStatisticDetailVo.Type), nameof(bs_SiteStatisticDetail.bs_Type))]
        [MapProperty(nameof(SiteStatisticDetailVo.ValetRateDaily), nameof(bs_SiteStatisticDetail.bs_ValetRateDaily))]
        [MapProperty(nameof(SiteStatisticDetailVo.ValetRateMonthly), nameof(bs_SiteStatisticDetail.bs_ValetRateMonthly))]
        [MapProperty(nameof(SiteStatisticDetailVo.SelfRateDaily), nameof(bs_SiteStatisticDetail.bs_SelfRateDaily))]
        [MapProperty(nameof(SiteStatisticDetailVo.SelfRateMonthly), nameof(bs_SiteStatisticDetail.bs_SelfRateMonthly))]
        [MapProperty(nameof(SiteStatisticDetailVo.BaseRevenue), nameof(bs_SiteStatisticDetail.bs_BaseRevenue))]
        [MapProperty(nameof(SiteStatisticDetailVo.OccupiedRooms), nameof(bs_SiteStatisticDetail.bs_OccupiedRooms))]
        [MapProperty(nameof(SiteStatisticDetailVo.Occupancy), nameof(bs_SiteStatisticDetail.bs_Occupancy))]
        [MapProperty(nameof(SiteStatisticDetailVo.SelfOvernight), nameof(bs_SiteStatisticDetail.bs_SelfOvernight))]
        [MapProperty(nameof(SiteStatisticDetailVo.ValetOvernight), nameof(bs_SiteStatisticDetail.bs_ValetOvernight))]
        [MapProperty(nameof(SiteStatisticDetailVo.ValetDaily), nameof(bs_SiteStatisticDetail.bs_ValetDaily))]
        [MapProperty(nameof(SiteStatisticDetailVo.ValetMonthly), nameof(bs_SiteStatisticDetail.bs_ValetMonthly))]
        [MapProperty(nameof(SiteStatisticDetailVo.SelfDaily), nameof(bs_SiteStatisticDetail.bs_SelfDaily))]
        [MapProperty(nameof(SiteStatisticDetailVo.SelfMonthly), nameof(bs_SiteStatisticDetail.bs_SelfMonthly))]
        [MapProperty(nameof(SiteStatisticDetailVo.ValetComps), nameof(bs_SiteStatisticDetail.bs_ValetComps))]
        [MapProperty(nameof(SiteStatisticDetailVo.SelfComps), nameof(bs_SiteStatisticDetail.bs_SelfComps))]
        [MapProperty(nameof(SiteStatisticDetailVo.DriveInRatio), nameof(bs_SiteStatisticDetail.bs_DriveInRatio))]
        [MapProperty(nameof(SiteStatisticDetailVo.CaptureRatio), nameof(bs_SiteStatisticDetail.bs_CaptureRatio))]
        [MapProperty(nameof(SiteStatisticDetailVo.SelfAggregator), nameof(bs_SiteStatisticDetail.bs_SelfAggregator))]
        [MapProperty(nameof(SiteStatisticDetailVo.ValetAggregator), nameof(bs_SiteStatisticDetail.bs_ValetAggregator))]
        [MapProperty(nameof(SiteStatisticDetailVo.ExternalRevenue), nameof(bs_SiteStatisticDetail.bs_ExternalRevenue))]
        [MapProperty(nameof(SiteStatisticDetailVo.AdjustmentPercentage), nameof(bs_SiteStatisticDetail.bs_AdjustmentPercentage))]
        [MapProperty(nameof(SiteStatisticDetailVo.AdjustmentValue), nameof(bs_SiteStatisticDetail.bs_AdjustmentValue))]
        private static partial bs_SiteStatisticDetail? SiteStatisticDetailVoToModel(SiteStatisticDetailVo vo);
    }
}
