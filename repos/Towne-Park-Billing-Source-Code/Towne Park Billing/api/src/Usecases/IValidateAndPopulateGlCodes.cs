using api.Models.Vo;

namespace api.Usecases;

public interface IValidateAndPopulateGlCodes
{
    public void Apply(ContractDetailVo updateContractVo);
}