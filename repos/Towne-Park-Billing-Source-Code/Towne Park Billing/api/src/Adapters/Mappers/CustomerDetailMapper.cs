using api.Models.Dto;
using api.Models.Vo;
using Riok.Mapperly.Abstractions;
using TownePark;

namespace api.Adapters.Mappers
{
    [Mapper]
    public static partial class CustomerDetailMapper
    {
        private const string DefaultInvoiceRecipient = "ATTN: Accounts Payable";
        
        public static CustomerDetailDto CustomerDetailVoToDto(CustomerDetailVo model)
        {
            var vo = MapCustomerDetailVoToDto(model);

            if (DateTime.TryParse(vo.StartDate, out var parsedStartDate))
            {
                vo.StartDate = parsedStartDate.ToString("yyyy-MM-dd");
            }

            if (DateTime.TryParse(vo.CloseDate, out var parsedCloseDate))
            {
                vo.CloseDate = parsedCloseDate.ToString("yyyy-MM-dd");
            }

            return vo;
        }

        private static partial CustomerDetailDto MapCustomerDetailVoToDto(CustomerDetailVo vo);

        [MapProperty(nameof(bs_CustomerSite.bs_CustomerSiteId), nameof(CustomerDetailVo.CustomerSiteId))]
        [MapProperty(nameof(bs_CustomerSite.bs_Address), nameof(CustomerDetailVo.Address))]
        [MapProperty(nameof(bs_CustomerSite.bs_SiteNumber), nameof(CustomerDetailVo.SiteNumber))]
        [MapProperty(nameof(bs_CustomerSite.bs_SiteName), nameof(CustomerDetailVo.SiteName))]
        [MapProperty(nameof(bs_CustomerSite.bs_AccountManager), nameof(CustomerDetailVo.AccountManager))]
        [MapProperty(nameof(bs_CustomerSite.bs_InvoiceRecipient), nameof(CustomerDetailVo.InvoiceRecipient))]
        [MapProperty(nameof(bs_CustomerSite.bs_BillingContactEmail), nameof(CustomerDetailVo.BillingContactEmail))]
        [MapProperty(nameof(bs_CustomerSite.bs_AccountManagerId), nameof(CustomerDetailVo.AccountManagerId))]
        [MapProperty(nameof(bs_CustomerSite.bs_StartDate), nameof(CustomerDetailVo.StartDate))]
        [MapProperty(nameof(bs_CustomerSite.bs_CloseDate), nameof(CustomerDetailVo.CloseDate))]
        [MapProperty(nameof(bs_CustomerSite.bs_District), nameof(CustomerDetailVo.District))]
        [MapProperty(nameof(bs_CustomerSite.bs_GLString), nameof(CustomerDetailVo.GlString))]
        [MapProperty(nameof(bs_CustomerSite.bs_TotalRoomsAvailable), nameof(CustomerDetailVo.TotalRoomsAvailable))]
        [MapProperty(nameof(bs_CustomerSite.bs_TotalAvailableParking), nameof(CustomerDetailVo.TotalAvailableParking))]
        [MapProperty(nameof(bs_CustomerSite.bs_DistrictManager), nameof(CustomerDetailVo.DistrictManager))]
        [MapProperty(nameof(bs_CustomerSite.bs_AssistantDistrictManager), nameof(CustomerDetailVo.AssistantDistrictManager))]
        [MapProperty(nameof(bs_CustomerSite.bs_AssistantAccountManager), nameof(CustomerDetailVo.AssistantAccountManager))]
        [MapProperty(nameof(bs_CustomerSite.bs_VendorId), nameof(CustomerDetailVo.VendorId))]
        [MapProperty(nameof(bs_CustomerSite.bs_LegalEntity), nameof(CustomerDetailVo.LegalEntity))]
        [MapProperty(nameof(bs_CustomerSite.bs_PLCategory), nameof(CustomerDetailVo.PLCategory))]
        [MapProperty(nameof(bs_CustomerSite.bs_SVPRegion), nameof(CustomerDetailVo.SVPRegion))]
        [MapProperty(nameof(bs_CustomerSite.bs_COGSegment), nameof(CustomerDetailVo.COGSegment))]
        [MapProperty(nameof(bs_CustomerSite.bs_BusinessSegment), nameof(CustomerDetailVo.BusinessSegment))]
        public static partial CustomerDetailVo CustomerDetailModelToVo(bs_CustomerSite model);

        public static partial CustomerDetailVo CustomerDetailDtoToVo(CustomerDetailDto vo);

        public static bs_CustomerSite UpdateCustomerDetailVoToModel(CustomerDetailVo existingCustomer, CustomerDetailVo newCustomer)
        {
            var newModel = new bs_CustomerSite();

            if (existingCustomer.SiteName != newCustomer.SiteName) newModel.bs_SiteName = newCustomer.SiteName;
            if (existingCustomer.Address != newCustomer.Address) newModel.bs_Address = newCustomer.Address;
            if (existingCustomer.SiteNumber != newCustomer.SiteNumber) newModel.bs_SiteNumber = newCustomer.SiteNumber;
            if (existingCustomer.AccountManager != newCustomer.AccountManager) newModel.bs_AccountManager = newCustomer.AccountManager;
            if (existingCustomer.InvoiceRecipient != newCustomer.InvoiceRecipient) newModel.bs_InvoiceRecipient = newCustomer.InvoiceRecipient;
            if (existingCustomer.BillingContactEmail != newCustomer.BillingContactEmail) newModel.bs_BillingContactEmail = newCustomer.BillingContactEmail;
            if (existingCustomer.GlString != newCustomer.GlString) newModel.bs_GLString = newCustomer.GlString;
            if (existingCustomer.District != newCustomer.District) newModel.bs_District = newCustomer.District;
            if (existingCustomer.AccountManagerId != newCustomer.AccountManagerId) newModel.bs_AccountManagerId = newCustomer.AccountManagerId;
            if (existingCustomer.StartDate != newCustomer.StartDate) newModel.bs_StartDate = newCustomer.StartDate;
            if (existingCustomer.CloseDate != newCustomer.CloseDate) newModel.bs_CloseDate = newCustomer.CloseDate;
            if (existingCustomer.TotalRoomsAvailable != newCustomer.TotalRoomsAvailable) newModel.bs_TotalRoomsAvailable = newCustomer.TotalRoomsAvailable;
            if (existingCustomer.TotalAvailableParking != newCustomer.TotalAvailableParking) newModel.bs_TotalAvailableParking = newCustomer.TotalAvailableParking;
            if (existingCustomer.DistrictManager != newCustomer.DistrictManager) newModel.bs_DistrictManager = newCustomer.DistrictManager;
            if (existingCustomer.AssistantDistrictManager != newCustomer.AssistantDistrictManager) newModel.bs_AssistantDistrictManager = newCustomer.AssistantDistrictManager;
            if (existingCustomer.AssistantAccountManager != newCustomer.AssistantAccountManager) newModel.bs_AssistantAccountManager = newCustomer.AssistantAccountManager;
            if (existingCustomer.VendorId != newCustomer.VendorId) newModel.bs_VendorId = newCustomer.VendorId;
            if (existingCustomer.LegalEntity != newCustomer.LegalEntity) newModel.bs_LegalEntity = newCustomer.LegalEntity;
            if (existingCustomer.PLCategory != newCustomer.PLCategory) newModel.bs_PLCategory = newCustomer.PLCategory;
            if (existingCustomer.SVPRegion != newCustomer.SVPRegion) newModel.bs_SVPRegion = newCustomer.SVPRegion;
            if (existingCustomer.COGSegment != newCustomer.COGSegment) newModel.bs_COGSegment = newCustomer.COGSegment;
            if (existingCustomer.BusinessSegment != newCustomer.BusinessSegment) newModel.bs_BusinessSegment = newCustomer.BusinessSegment;

            return newModel;
        }

        public static bs_CustomerSite MasterCustomerSiteToModel(bs_MasterCustomerSite model)
        {
            // We need to go through a value object to avoid copying all the hidden properties of a dataverse model.
            var masterCustomerSiteVo = MapMasterCustomerSiteToVo(model);
            var customerSite = MapMasterCustomerSiteVoToModel(masterCustomerSiteVo);
            customerSite.bs_InvoiceRecipient = DefaultInvoiceRecipient;
            return customerSite;
        }

        [MapProperty(nameof(bs_MasterCustomerSite.bs_SiteName), nameof(MasterCustomerDetailVo.SiteName))]
        [MapProperty(nameof(bs_MasterCustomerSite.bs_SiteNumber), nameof(MasterCustomerDetailVo.SiteNumber))]
        [MapProperty(nameof(bs_MasterCustomerSite.bs_AccountManager), nameof(MasterCustomerDetailVo.AccountManager))]
        [MapProperty(nameof(bs_MasterCustomerSite.bs_AccountManagerId), nameof(MasterCustomerDetailVo.AccountManagerId))]
        [MapProperty(nameof(bs_MasterCustomerSite.bs_Address), nameof(MasterCustomerDetailVo.Address))]
        [MapProperty(nameof(bs_MasterCustomerSite.bs_BillingContactEmail), nameof(MasterCustomerDetailVo.BillingContactEmail))]
        [MapProperty(nameof(bs_MasterCustomerSite.bs_District), nameof(MasterCustomerDetailVo.District))]
        [MapProperty(nameof(bs_MasterCustomerSite.bs_GLString), nameof(MasterCustomerDetailVo.GlString))]
        [MapProperty(nameof(bs_MasterCustomerSite.bs_StartDate), nameof(MasterCustomerDetailVo.StartDate))]
        [MapProperty(nameof(bs_MasterCustomerSite.bs_LegalEntity), nameof(MasterCustomerDetailVo.LegalEntity))]
        [MapProperty(nameof(bs_MasterCustomerSite.bs_PLCategory), nameof(MasterCustomerDetailVo.PLCategory))]
        [MapProperty(nameof(bs_MasterCustomerSite.bs_SVPRegion), nameof(MasterCustomerDetailVo.SVPRegion))]
        [MapProperty(nameof(bs_MasterCustomerSite.bs_COGSegment), nameof(MasterCustomerDetailVo.COGSegment))]
        [MapProperty(nameof(bs_MasterCustomerSite.bs_BusinessSegment), nameof(MasterCustomerDetailVo.BusinessSegment))]
        private static partial MasterCustomerDetailVo MapMasterCustomerSiteToVo(bs_MasterCustomerSite model);

        [MapProperty(nameof(MasterCustomerDetailVo.SiteName), nameof(bs_CustomerSite.bs_SiteName))]
        [MapProperty(nameof(MasterCustomerDetailVo.SiteNumber), nameof(bs_CustomerSite.bs_SiteNumber))]
        [MapProperty(nameof(MasterCustomerDetailVo.AccountManager), nameof(bs_CustomerSite.bs_AccountManager))]
        [MapProperty(nameof(MasterCustomerDetailVo.AccountManagerId), nameof(bs_CustomerSite.bs_AccountManagerId))]
        [MapProperty(nameof(MasterCustomerDetailVo.Address), nameof(bs_CustomerSite.bs_Address))]
        [MapProperty(nameof(MasterCustomerDetailVo.BillingContactEmail), nameof(bs_CustomerSite.bs_BillingContactEmail))]
        [MapProperty(nameof(MasterCustomerDetailVo.District), nameof(bs_CustomerSite.bs_District))]
        [MapProperty(nameof(MasterCustomerDetailVo.GlString), nameof(bs_CustomerSite.bs_GLString))]
        [MapProperty(nameof(MasterCustomerDetailVo.StartDate), nameof(bs_CustomerSite.bs_StartDate))]
        [MapProperty(nameof(MasterCustomerDetailVo.LegalEntity), nameof(bs_CustomerSite.bs_LegalEntity))]
        [MapProperty(nameof(MasterCustomerDetailVo.PLCategory), nameof(bs_CustomerSite.bs_PLCategory))]
        [MapProperty(nameof(MasterCustomerDetailVo.SVPRegion), nameof(bs_CustomerSite.bs_SVPRegion))]
        [MapProperty(nameof(MasterCustomerDetailVo.COGSegment), nameof(bs_CustomerSite.bs_COGSegment))]
        [MapProperty(nameof(MasterCustomerDetailVo.BusinessSegment), nameof(bs_CustomerSite.bs_BusinessSegment))]
        private static partial bs_CustomerSite MapMasterCustomerSiteVoToModel(MasterCustomerDetailVo model);
    }
}
