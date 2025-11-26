using api.Models.Vo;

namespace api.Services
{
    public interface IContractService
    {
        ContractDetailVo GetContractDetail(Guid customerSiteId);
        void UpdateContract(Guid contractId, ContractDetailVo contractDetail);
        void UpdateDeviationThreshold(IEnumerable<DeviationVo> updateDeviation);
        IEnumerable<DeviationVo> GetDeviations();
        
        /**
         * Add new blank Contract entity from required fields.
         */
        Guid AddContract(Guid customerSiteId, string contractType, bool deposits);
    }
}
