using api.Models.Dto;
using api.Adapters.Mappers;
using api.Services;

namespace api.Adapters.Impl
{
    public class ConfigDataServiceAdapter : IConfigDataServiceAdapter
    {
        private readonly IConfigDataService _configDataService;

        public ConfigDataServiceAdapter(IConfigDataService configDataService)
        {
            _configDataService = configDataService;
        }

        public ContractConfigDto GetGlCodes(List<string> codeTypes)
        {
            return ConfigDataMapper.GlCodeVoToDto(_configDataService.GetGlCodes(codeTypes));
        }

        public IEnumerable<InvoiceConfigDto> GetInvoiceConfig(string configGroup)
        {
            return ConfigDataMapper.InvoiceConfigVoToDto(_configDataService.GetInvoiceConfig(configGroup));
        }
    }
}
