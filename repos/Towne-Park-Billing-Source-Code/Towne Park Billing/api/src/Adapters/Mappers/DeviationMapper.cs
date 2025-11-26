using api.Models.Dto;
using api.Models.Vo;
using Riok.Mapperly.Abstractions;
using TownePark;

namespace api.Adapters.Mappers
{
    [Mapper]
    public partial class DeviationMapper
    {
        public static partial IEnumerable<DeviationVo> UpdateDeviationDtoToVo(IEnumerable<DeviationDto> dto);

        public static IEnumerable<bs_Contract> UpdateDeviationVoToModel(IEnumerable<DeviationVo> vo)
        {
            var contracts = new List<bs_Contract>();

            foreach (var item in vo)
            {
                bs_Contract contract = new bs_Contract
                {
                    Id = item.ContractId.Value,
                    bs_DeviationAmount = item.DeviationAmount,
                    bs_DeviationPercentage = item.DeviationPercentage
                };

                contracts.Add(contract);
            }

            return contracts;
        }

        public static partial IEnumerable<DeviationDto> DeviationVoToDto(IEnumerable<DeviationVo> vo);

        public static partial IEnumerable<DeviationVo> DeviationModelToVo(IEnumerable<bs_Contract> models);

        [MapProperty(nameof(bs_Contract.bs_ContractId), nameof(DeviationVo.ContractId))]
        [MapProperty(nameof(bs_Contract.bs_DeviationAmount), nameof(DeviationVo.DeviationAmount))]
        [MapProperty(nameof(bs_Contract.bs_DeviationPercentage), nameof(DeviationVo.DeviationPercentage))]
        [MapProperty($"{nameof(bs_Contract.bs_Contract_CustomerSite)}.{nameof(bs_Contract.bs_Contract_CustomerSite.bs_CustomerSiteId)}", nameof(DeviationVo.CustomerSiteId))]
        [MapProperty($"{nameof(bs_Contract.bs_Contract_CustomerSite)}.{nameof(bs_Contract.bs_Contract_CustomerSite.bs_SiteName)}", nameof(DeviationVo.SiteName))]
        [MapProperty($"{nameof(bs_Contract.bs_Contract_CustomerSite)}.{nameof(bs_Contract.bs_Contract_CustomerSite.bs_SiteNumber)}", nameof(DeviationVo.SiteNumber))]
        private static partial DeviationVo MapDeviationModelToVo(bs_Contract model);
    }
}
