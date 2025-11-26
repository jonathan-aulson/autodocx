using api.Models.Dto;
using api.Models.Vo;
using Microsoft.Xrm.Sdk;
using Riok.Mapperly.Abstractions;
using System.Collections.Generic;
using System.Linq;
using TownePark; 
using System; 

namespace api.Adapters.Mappers
{
    [Mapper(UseDeepCloning = true, EnumMappingStrategy = EnumMappingStrategy.ByName)]
    public static partial class ParkingRateMapper
    {

        public static ParkingRateDataVo ParkingRateModelToVo(bs_ParkingRate? model)
        {
            if (model == null) return new ParkingRateDataVo();
            var vo = MapParkingRateBaseToVo(model); // Use generated method for base properties
            vo.ForecastRates = new List<ParkingRateDetailVo>();
            vo.ActualRates = new List<ParkingRateDetailVo>();
            vo.BudgetRates = new List<ParkingRateDetailVo>();

            if (model.bs_parkingratedetail_ParkingRateFK_bs_parkingrate != null)
            {
                foreach (var detailModel in model.bs_parkingratedetail_ParkingRateFK_bs_parkingrate)
                {
                    var detailVo = MapParkingRateDetailToVo(detailModel);
                    switch (detailVo.Type)
                    {
                        case bs_parkingratedetailtypes.Forecast: vo.ForecastRates.Add(detailVo); break;
                        case bs_parkingratedetailtypes.Actual: vo.ActualRates.Add(detailVo); break;
                        case bs_parkingratedetailtypes.Budget: vo.BudgetRates.Add(detailVo); break;
                    }
                }
            }
            return vo;
        }
        
        // Base mapping for ParkingRate -> ParkingRateDataVo
        [MapProperty(nameof(bs_ParkingRate.bs_ParkingRateId), nameof(ParkingRateDataVo.Id), Use = nameof(MapGuidToGuid))]
        [MapProperty(nameof(bs_ParkingRate.bs_Name), nameof(ParkingRateDataVo.Name), Use = nameof(MapStringToStr))]
        [MapProperty(nameof(bs_ParkingRate.bs_CustomerSiteFK), nameof(ParkingRateDataVo.CustomerSiteId), Use = nameof(MapEntityReferenceToGuid))]
        [MapProperty(nameof(bs_ParkingRate.bs_CustomerSiteFK), nameof(ParkingRateDataVo.SiteNumber), Use = nameof(MapEntityReferenceToString))]
        [MapProperty(nameof(bs_ParkingRate.bs_Year), nameof(ParkingRateDataVo.Year), Use = nameof(MapNullableIntToInt))]
        private static partial ParkingRateDataVo MapParkingRateBaseToVo(bs_ParkingRate model);

        // Mapping for ParkingRateDetail -> ParkingRateDetailVo
        [MapProperty(nameof(bs_ParkingRateDetail.bs_ParkingRateDetailId), nameof(ParkingRateDetailVo.Id), Use = nameof(MapGuidToGuid))]
        [MapProperty(nameof(bs_ParkingRateDetail.bs_Month), nameof(ParkingRateDetailVo.Month), Use = nameof(MapNullableIntToInt))]
        [MapProperty(nameof(bs_ParkingRateDetail.bs_IsIncrease), nameof(ParkingRateDetailVo.IsIncrease), Use = nameof(MapNullableBoolToBool))]
        [MapProperty(nameof(bs_ParkingRateDetail.bs_Rate), nameof(ParkingRateDetailVo.Rate), Use = nameof(MapMoneyToDecimal))]
        [MapProperty(nameof(bs_ParkingRateDetail.bs_IncreaseAmount), nameof(ParkingRateDetailVo.IncreaseAmount), Use = nameof(MapMoneyToDecimal))]
        [MapProperty(nameof(bs_ParkingRateDetail.bs_Type), nameof(ParkingRateDetailVo.Type), Use = nameof(MapNullableDataverseTypeToVoType))]
        [MapProperty(nameof(bs_ParkingRateDetail.bs_RateCategory), nameof(ParkingRateDetailVo.RateCategory), Use = nameof(MapNullableDataverseCategoryToVoCategory))]
        private static partial ParkingRateDetailVo MapParkingRateDetailToVo(bs_ParkingRateDetail model);

        private static Guid MapGuidToGuid(Guid? guidValue) => guidValue ?? Guid.Empty;
        private static string MapStringToStr(string? strValue) => strValue ?? string.Empty;
        private static Guid MapEntityReferenceToGuid(EntityReference? reference) => reference?.Id ?? Guid.Empty;
        private static string MapEntityReferenceToString(EntityReference? reference) => reference?.Name ?? string.Empty;
        private static int MapNullableIntToInt(int? intValue) => intValue ?? 0;
        private static bool MapNullableBoolToBool(bool? boolValue) => boolValue ?? false;
        private static decimal MapMoneyToDecimal(Money? moneyValue) => moneyValue?.Value ?? 0;
        
        private static bs_parkingratedetailtypes MapNullableDataverseTypeToVoType(bs_parkingratedetailtypes? dataverseEnum) =>
            dataverseEnum ?? bs_parkingratedetailtypes.Forecast;

        private static bs_ratecategorytypes MapNullableDataverseCategoryToVoCategory(bs_ratecategorytypes? dataverseEnum) =>
            dataverseEnum ?? bs_ratecategorytypes.ValetOvernight;

        // --- VO to DTO ---
        public static partial ParkingRateDataDto ParkingRateVoToDto(ParkingRateDataVo vo);
        
        public static partial ParkingRateDetailDto ParkingRateDetailVoToDto(ParkingRateDetailVo vo);
       
        // --- DTO to VO ---
        public static partial ParkingRateDataVo ParkingRateDtoToVo(ParkingRateDataDto dto);

        public static partial ParkingRateDetailVo ParkingRateDetailDtoToVo(ParkingRateDetailDto dto);

        public static bs_ParkingRate ParkingRateVoToModel(ParkingRateDataVo vo)
        {
            var model = MapParkingRateVoToModel(vo);

            var detailList = new List<bs_ParkingRateDetail>();

            foreach (var detail in vo.BudgetRates)
            {
                var detailModel = MapParkingRateDetailVoToModel(detail);
                detailList.Add(detailModel);
            }
            foreach (var detail in vo.ActualRates)
            {
                var detailModel = MapParkingRateDetailVoToModel(detail);
                detailList.Add(detailModel);
            }
            foreach (var detail in vo.ForecastRates)
            {
                var detailModel = MapParkingRateDetailVoToModel(detail);
                detailList.Add(detailModel);
            }

            model.bs_parkingratedetail_ParkingRateFK_bs_parkingrate = detailList;
            model.bs_CustomerSiteFK = new EntityReference("bs_customersite", vo.CustomerSiteId);

            return model;
        }

        [MapProperty(nameof(ParkingRateDataVo.Id), nameof(bs_ParkingRate.Id))]
        [MapProperty(nameof(ParkingRateDataVo.Name), nameof(bs_ParkingRate.bs_Name))]
        [MapProperty(nameof(ParkingRateDataVo.Year), nameof(bs_ParkingRate.bs_Year))]
        private static partial bs_ParkingRate MapParkingRateVoToModel(ParkingRateDataVo vo);

        [MapProperty(nameof(ParkingRateDetailVo.Id), nameof(bs_ParkingRateDetail.bs_ParkingRateDetailId))]
        [MapProperty(nameof(ParkingRateDetailVo.Month), nameof(bs_ParkingRateDetail.bs_Month))]
        [MapProperty(nameof(ParkingRateDetailVo.IsIncrease), nameof(bs_ParkingRateDetail.bs_IsIncrease))]
        [MapProperty(nameof(ParkingRateDetailVo.Rate), nameof(bs_ParkingRateDetail.bs_Rate))]
        [MapProperty(nameof(ParkingRateDetailVo.IncreaseAmount), nameof(bs_ParkingRateDetail.bs_IncreaseAmount))]
        [MapProperty(nameof(ParkingRateDetailVo.Type), nameof(bs_ParkingRateDetail.bs_Type))]
        [MapProperty(nameof(ParkingRateDetailVo.RateCategory), nameof(bs_ParkingRateDetail.bs_RateCategory))]
        private static partial bs_ParkingRateDetail MapParkingRateDetailVoToModel(ParkingRateDetailVo vo);
    }
} 