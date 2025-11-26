using api.Models.Vo;
using api.Data;
using api.Adapters.Mappers;
using api.Models.Vo.Enum;
using System.Net;
using TownePark;
using Riok.Mapperly.Abstractions;

namespace api.Services.Impl
{
    public class ConfigDataService : IConfigDataService
    {
        private readonly IConfigDataRepository _configDataRepository;

        public ConfigDataService(IConfigDataRepository configDataRepository)
        {
            _configDataRepository = configDataRepository;
        }

        public ContractConfigVo GetGlCodes(List<string> codeTypes)
        {
            IEnumerable<bs_glcodetypechoices> codeTypeEnums = ConfigDataMapper.GlCodeTypeChoicesToModel(codeTypes);

            return ConfigDataMapper.GlCodeModelToVo(_configDataRepository.GetGlCodes(codeTypeEnums));
        }

        public IEnumerable<InvoiceConfigVo> GetInvoiceConfig(string configGroup)
        {
            bs_generalconfiggroupchoices group = ConfigDataMapper.InvoiceConfigGroupToModel(configGroup);
            return ConfigDataMapper.InvoiceConfigModelToVo(_configDataRepository.GetInvoiceConfigData(group));
        }
    }
}
