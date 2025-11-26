using api.Models.Dto;
using api.Models.Vo.Enum;

namespace api.Adapters
{
    public interface IConfigDataServiceAdapter
    {
        ContractConfigDto GetGlCodes(List<string> codeTypes);
        IEnumerable<InvoiceConfigDto> GetInvoiceConfig(string configGroup);
    }
}
