using api.Models.Vo.Enum;

namespace api.Models.Vo
{
    public class ContractConfigVo
    {
        public decimal? DefaultRate { get; set; }
        public decimal? DefaultOvertimeRate { get; set; }
        public decimal? DefaultFee { get; set; }
        public IEnumerable<GlCodeVo>? GlCodes { get; set; }
    }

    public class GlCodeVo
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public GlCodeType? Type { get; set; }
    }
}
