using api.Models.Dto;
using api.Models.Vo;
using Riok.Mapperly.Abstractions;
using TownePark;

namespace api.Adapters.Mappers
{
    [Mapper]
    public static partial class UnitAccountMapper
    {
        [MapProperty(nameof(UnitAccountDto.Date), nameof(UnitAccountVo.ServicePeriod))]
        public static partial UnitAccountVo UnitAccountDtoToVo(UnitAccountDto dto);

        [MapProperty(nameof(UnitAccountVo.ServicePeriod), nameof(bs_UnitAccountBatchProcess.bs_Period))]
        public static partial bs_UnitAccountBatchProcess UnitAccountVoToModel(UnitAccountVo vo);
    }
}
