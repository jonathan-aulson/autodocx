using api.Models.Dto;
using api.Models.Vo;
using Riok.Mapperly.Abstractions;
using TownePark;

namespace api.Adapters.Mappers;

/**
 * Why use a static class?
 *
 * Mappers don't have dependencies of their own, and don't have side-effects.
 * If it seems like you want to have it behind an interface, usually it's a sign that the idea of a mapper is
 * misunderstood, or the functionality that is being added belongs somewhere else.
 *
 * Reference -> https://github.com/riok/mapperly/discussions/1280
 */
[Mapper]
public static partial class CustomerSummaryMapper
{
    public static partial IEnumerable<CustomerSummaryDto> CustomersVoToDto(IEnumerable<CustomerSummaryVo> vo);

    public static partial IEnumerable<CustomerSummaryVo> CustomersModelToVo(IEnumerable<bs_CustomerSite> models);

    private static CustomerSummaryVo CustomerModelToVo(bs_CustomerSite model)
    {
        var customerSummary = MapCustomerModelToVo(model);
        var contract = model.bs_Contract_CustomerSite.First();
        var readyForInvoice = model.bs_CustomerSite_cr9e8_readyforinvoice.First();
        var billingStatement = model.bs_BillingStatement_CustomerSite.First();
        MapContractModelToVo(contract, customerSummary);
        MapReadyForInvoiceModelToVo(readyForInvoice, customerSummary);
        MapBillingStatementModelToVo(billingStatement, customerSummary);
        return customerSummary;
    }


    [MapProperty(nameof(bs_CustomerSite.bs_CustomerSiteId), nameof(CustomerSummaryVo.CustomerSiteId))]
    [MapProperty(nameof(bs_CustomerSite.bs_SiteNumber), nameof(CustomerSummaryVo.SiteNumber))]
    [MapProperty(nameof(bs_CustomerSite.bs_SiteName), nameof(CustomerSummaryVo.SiteName))]
    [MapProperty(nameof(bs_CustomerSite.bs_District), nameof(CustomerSummaryVo.District))]
    [MapProperty(nameof(bs_CustomerSite.bs_AccountManager), nameof(CustomerSummaryVo.AccountManager))]
    [MapProperty(nameof(bs_CustomerSite.bs_DistrictManager), nameof(CustomerSummaryVo.DistrictManager))]
    [MapProperty(nameof(bs_CustomerSite.bs_LegalEntity), nameof(CustomerSummaryVo.LegalEntity))]
    [MapProperty(nameof(bs_CustomerSite.bs_PLCategory), nameof(CustomerSummaryVo.PLCategory))]
    [MapProperty(nameof(bs_CustomerSite.bs_SVPRegion), nameof(CustomerSummaryVo.SVPRegion))]
    [MapProperty(nameof(bs_CustomerSite.bs_COGSegment), nameof(CustomerSummaryVo.COGSegment))]
    [MapProperty(nameof(bs_CustomerSite.bs_BusinessSegment), nameof(CustomerSummaryVo.BusinessSegment))]
    private static partial CustomerSummaryVo MapCustomerModelToVo(bs_CustomerSite model);

    [MapProperty(nameof(bs_Contract.bs_BillingType), nameof(CustomerSummaryVo.BillingType))]
    [MapProperty(nameof(bs_Contract.bs_ContractTypeString), nameof(CustomerSummaryVo.ContractType))]
    [MapProperty(nameof(bs_Contract.bs_Deposits), nameof(CustomerSummaryVo.Deposits))]
    private static partial void MapContractModelToVo(bs_Contract model, CustomerSummaryVo target);

    [MapProperty(nameof(cr9e8_readyforinvoice.cr9e8_period), nameof(CustomerSummaryVo.Period))]
    [MapProperty(nameof(cr9e8_readyforinvoice.cr9e8_invoicestatus), nameof(CustomerSummaryVo.ReadyForInvoiceStatus))]
    private static partial void MapReadyForInvoiceModelToVo(cr9e8_readyforinvoice model, CustomerSummaryVo target);

    private static void MapBillingStatementModelToVo(bs_BillingStatement model, CustomerSummaryVo target)
    {
        Guid? statementGuid = model.bs_BillingStatementId;
        bool isNullOrEmpty = statementGuid == null || statementGuid == Guid.Empty;

        if (isNullOrEmpty)
        {
            target.IsStatementGenerated = false;
        }
        else
        {
           target.IsStatementGenerated = true;
        }
    }
}