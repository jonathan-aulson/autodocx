using api.Adapters.Mappers;
using api.Models.Dto;
using api.Services;

namespace api.Adapters.Impl;

public class ContractServiceAdapter : IContractServiceAdapter
{
    
    private readonly IContractService _contractService;

    public ContractServiceAdapter(IContractService contractService)
    {
        _contractService = contractService;
    }

    public ContractDetailDto GetContractDetail(Guid customerSiteId)
    {
        return ContractMapper.ContractDetailVoToDto(_contractService.GetContractDetail(customerSiteId));
    }

    public void UpdateContract(Guid contractId, ContractDetailDto updateContract)
    {
        _contractService.UpdateContract(contractId, UpdateContractMapper.ContractDetailDtoToVo(updateContract));
    }

    public void UpdateDeviationThreshold(IEnumerable<DeviationDto> updateDeviation)
    {
        _contractService.UpdateDeviationThreshold(Mappers.DeviationMapper.UpdateDeviationDtoToVo(updateDeviation));
    }

    public IEnumerable<DeviationDto> GetDeviations()
    {
        return Mappers.DeviationMapper.DeviationVoToDto(_contractService.GetDeviations());
    }
}