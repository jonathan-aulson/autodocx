using api.Models.Dto;

namespace api.Adapters;

public interface IContractServiceAdapter
{
    ContractDetailDto GetContractDetail(Guid customerSiteId);
    void UpdateContract(Guid contractId, ContractDetailDto updateContract);
    void UpdateDeviationThreshold(IEnumerable<DeviationDto> updateDeviation);
    IEnumerable<DeviationDto> GetDeviations();
}