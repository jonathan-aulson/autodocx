using api.Models.Dto;
using api.Models.Vo;
using Newtonsoft.Json;
using Riok.Mapperly.Abstractions;
using TownePark;

namespace api.Adapters.Mappers
{
    [Mapper]
    public static partial class ConfigDataMapper
    {
        // GL Code Mappings

        public static partial ContractConfigDto GlCodeVoToDto(ContractConfigVo vo);

        [MapProperty(nameof(bs_GLCodeConfig.bs_Code), nameof(GlCodeVo.Code))]
        [MapProperty(nameof(bs_GLCodeConfig.bs_Name), nameof(GlCodeVo.Name))]
        [MapProperty(nameof(bs_GLCodeConfig.bs_Type), nameof(GlCodeVo.Type))]
        private static partial IEnumerable<GlCodeVo> MapGlCodeModelToVo(IEnumerable<bs_GLCodeConfig> model);

        public static ContractConfigVo GlCodeModelToVo(IEnumerable<bs_GLCodeConfig> models)
        {
            var vo = new ContractConfigVo();
            var glCodeConfigs = models.ToList();
            vo.GlCodes = MapGlCodeModelToVo(glCodeConfigs);

            foreach (var model in glCodeConfigs)
            {
                if (string.IsNullOrWhiteSpace(model.bs_Data)) continue;
                var data = JsonConvert.DeserializeObject<Dictionary<string, decimal>>(model.bs_Data);
                if (data == null) continue;
                if (data.TryGetValue("rate", out var rate))
                {
                    vo.DefaultRate = rate;
                }
                if (data.TryGetValue("overtimeRate", out var overtimeRate))
                {
                    vo.DefaultOvertimeRate = overtimeRate;
                }
                if (data.TryGetValue("fee", out var defaultFee))
                {
                    vo.DefaultFee = defaultFee;
                }
            }
            return vo;
        }

        [MapProperty(nameof(bs_GLCodeConfig.bs_Code), nameof(GlCodeVo.Code))]
        [MapProperty(nameof(bs_GLCodeConfig.bs_Name), nameof(GlCodeVo.Name))]
        [MapProperty(nameof(bs_GLCodeConfig.bs_Type), nameof(GlCodeVo.Type))]
        private static partial GlCodeVo MapGlCodeToVo(bs_GLCodeConfig model);

        public static partial IEnumerable<bs_glcodetypechoices> GlCodeTypeChoicesToModel(List<string> codeTypes);

        // General Config Mappings

        public static partial IEnumerable<InvoiceConfigDto> InvoiceConfigVoToDto(IEnumerable<InvoiceConfigVo> vo);

        [MapProperty(nameof(bs_GeneralConfig.bs_Key), nameof(InvoiceConfigVo.Key))]
        [MapProperty(nameof(bs_GeneralConfig.bs_Value), nameof(InvoiceConfigVo.Value))]
        public static partial IEnumerable<InvoiceConfigVo> InvoiceConfigModelToVo(IEnumerable<bs_GeneralConfig> model);


        [MapProperty(nameof(bs_GeneralConfig.bs_Key), nameof(InvoiceConfigVo.Key))]
        [MapProperty(nameof(bs_GeneralConfig.bs_Value), nameof(InvoiceConfigVo.Value))]
        private static partial InvoiceConfigVo MapInvoiceConfigToVo(bs_GeneralConfig model);

        public static partial bs_generalconfiggroupchoices InvoiceConfigGroupToModel(string configGroup);
    }
}
