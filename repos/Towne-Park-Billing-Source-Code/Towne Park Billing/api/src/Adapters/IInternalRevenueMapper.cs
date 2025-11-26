using System.Collections.Generic;
using TownePark.Models.Vo;
// Assuming Dataverse entities are in the TownePark namespace directly or a sub-namespace like TownePark.Entities
// If they are in the global namespace or another, adjust the using statement accordingly.
// For example: using global::bs_Contract; if they are in global.
// Or: using TownePark.Dataverse.Entities; if they are in a sub-namespace.
// For now, I'll assume they are directly in TownePark namespace as per OrgContext.cs
using TownePark;

namespace api.Adapters
{
    public interface IInternalRevenueMapper
    {
        ContractDataVo MapContractToVo(bs_Contract contract);
        List<SiteStatisticDetailVo> MapSiteStatisticsToVo(IEnumerable<bs_SiteStatisticDetail> siteStatistics);
        List<FixedFeeVo> MapFixedFeesToVo(IEnumerable<bs_FixedFeeService> fixedFees);
        List<LaborHourJobVo> MapLaborHourJobsToVo(IEnumerable<bs_LaborHourJob> laborHourJobs);
        List<RevenueShareThresholdVo> MapRevenueShareThresholdsToVo(IEnumerable<bs_RevenueShareThreshold> revenueShareThresholds);
        List<BillableAccountVo> MapBillableAccountsToVo(IEnumerable<bs_BillableAccount> billableAccounts);
        ManagementAgreementVo MapManagementAgreementToVo(bs_ManagementAgreement managementAgreement, bs_CustomerSite customerSite = null);
        List<TownePark.Models.Vo.OtherRevenueVo> MapOtherRevenuesToVo(IEnumerable<bs_OtherRevenueDetail> otherRevenues); // Changed from bs_OtherRevenue to bs_OtherRevenueDetail
        List<NonGLExpenseVo> MapNonGLExpensesToVo(IEnumerable<bs_NonGLExpense> nonGLExpenses);
        List<ParkingRateVo> MapParkingRatesToVo(IEnumerable<bs_ParkingRate> rates);
        List<TownePark.Models.Vo.ProfitShareTierVo> ParseProfitShareTierData(string tierDataJson);
    }
}
