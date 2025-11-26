using api.Models.Vo;

namespace api.Services
{
    public interface IConfigDataService
    {
        ContractConfigVo GetGlCodes(List<string> codeTypes);
        IEnumerable<InvoiceConfigVo> GetInvoiceConfig(string codeTypes);
    }
}
