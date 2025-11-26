using api.Models.Vo.Enum;
using TownePark;

namespace api.Data
{
    public interface IConfigDataRepository
    {
        IEnumerable<bs_GLCodeConfig> GetGlCodes(IEnumerable<bs_glcodetypechoices> codeTypes);
        IEnumerable<bs_GeneralConfig> GetInvoiceConfigData(bs_generalconfiggroupchoices configGroup);
    }
}
