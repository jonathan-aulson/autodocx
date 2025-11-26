using api.Models.Dto;
using api.Models.Vo;
using Riok.Mapperly.Abstractions;
using TownePark;

namespace api.Adapters.Mappers
{
    [Mapper]
    public static partial class OtherExpenseMapper
    {
        // data retrieval mappers

        public static OtherExpenseVo OtherExpenseModelToVo(IEnumerable<bs_OtherExpenseDetail> models, Guid customerSiteId, string billingPeriod, string? siteNumber = null)
        {
            var otherExpenseDetails = models?.ToList() ?? new List<bs_OtherExpenseDetail>();

            var details = new List<OtherExpenseDetailVo>();
            foreach (var item in otherExpenseDetails)
            {
                var detail = MapOtherExpenseDetailModelToVo(item);
                details.Add(detail);
            }

            if (otherExpenseDetails.Count == 0)
            {
                return new OtherExpenseVo
                {
                    ForecastData = new List<OtherExpenseDetailVo>(),
                    ActualData = new List<OtherExpenseDetailVo>(),
                    BudgetData = new List<OtherExpenseDetailVo>(),
                    CustomerSiteId = customerSiteId,
                    BillingPeriod = billingPeriod,
                    SiteNumber = siteNumber ?? string.Empty,
                    Id = null,
                    Name = null
                };
            }

            var first = otherExpenseDetails.FirstOrDefault();
            var resolvedSiteNumber = siteNumber ?? (first?.bs_CustomerSiteFK?.Name ?? string.Empty);
            var resolvedCustomerSiteId = first?.bs_CustomerSiteFK?.Id ?? customerSiteId;

            var otherExpenseVo = new OtherExpenseVo
            {
                ForecastData = details,
                CustomerSiteId = resolvedCustomerSiteId,
                SiteNumber = resolvedSiteNumber,
                BillingPeriod = billingPeriod,
                ActualData = new List<OtherExpenseDetailVo>(),
                BudgetData = new List<OtherExpenseDetailVo>(),
                Id = null,
                Name = null
            };

            return otherExpenseVo;
        }

        [MapProperty(nameof(bs_OtherExpenseDetail.Id), nameof(OtherExpenseDetailVo.Id))]
        [MapProperty(nameof(bs_OtherExpenseDetail.bs_EmployeeRelations), nameof(OtherExpenseDetailVo.EmployeeRelations))]
        [MapProperty(nameof(bs_OtherExpenseDetail.bs_FuelVehicles), nameof(OtherExpenseDetailVo.FuelVehicles))]
        [MapProperty(nameof(bs_OtherExpenseDetail.bs_LossAndDamageClaims), nameof(OtherExpenseDetailVo.LossAndDamageClaims))]
        [MapProperty(nameof(bs_OtherExpenseDetail.bs_OfficeSupplies), nameof(OtherExpenseDetailVo.OfficeSupplies))]
        [MapProperty(nameof(bs_OtherExpenseDetail.bs_OutsideServices), nameof(OtherExpenseDetailVo.OutsideServices))]
        [MapProperty(nameof(bs_OtherExpenseDetail.bs_RentsParking), nameof(OtherExpenseDetailVo.RentsParking))]
        [MapProperty(nameof(bs_OtherExpenseDetail.bs_RepairsAndMaintenance), nameof(OtherExpenseDetailVo.RepairsAndMaintenance))]
        [MapProperty(nameof(bs_OtherExpenseDetail.bs_RepairsAndMaintenanceVehicle), nameof(OtherExpenseDetailVo.RepairsAndMaintenanceVehicle))]
        [MapProperty(nameof(bs_OtherExpenseDetail.bs_Signage), nameof(OtherExpenseDetailVo.Signage))]
        [MapProperty(nameof(bs_OtherExpenseDetail.bs_SuppliesAndEquipment), nameof(OtherExpenseDetailVo.SuppliesAndEquipment))]
        [MapProperty(nameof(bs_OtherExpenseDetail.bs_TicketsAndPrintedMaterial), nameof(OtherExpenseDetailVo.TicketsAndPrintedMaterial))]
        [MapProperty(nameof(bs_OtherExpenseDetail.bs_Uniforms), nameof(OtherExpenseDetailVo.Uniforms))]
        [MapProperty(nameof(bs_OtherExpenseDetail.bs_MonthYear), nameof(OtherExpenseDetailVo.MonthYear))]
        [MapProperty(nameof(bs_OtherExpenseDetail.bs_MiscOtherExpenses), nameof(OtherExpenseDetailVo.MiscOtherExpenses))]
        [MapProperty(nameof(bs_OtherExpenseDetail.bs_TotalOtherExpenses), nameof(OtherExpenseDetailVo.TotalOtherExpenses))]
        private static partial OtherExpenseDetailVo MapOtherExpenseDetailModelToVo(bs_OtherExpenseDetail model);

        public static partial OtherExpenseDto? OtherExpenseVoToDto(OtherExpenseVo? vo);

        // data save mappers

        public static List<bs_OtherExpenseDetail> OtherExpenseVoToModel(OtherExpenseVo otherExpense)
        {
            var model = new List<bs_OtherExpenseDetail>();
            if (otherExpense == null || otherExpense.ForecastData == null)
                return model;

            foreach (var item in otherExpense.ForecastData)
            {
                var detail = MapOtherExpenseDetailVoToModel(item);

                detail.bs_CustomerSiteFK = new Microsoft.Xrm.Sdk.EntityReference()
                {
                    LogicalName = bs_CustomerSite.EntityLogicalName,
                    Id = otherExpense.CustomerSiteId ?? Guid.Empty,
                    Name = otherExpense.SiteNumber
                };

                model.Add(detail);
            }
            return model;
        }

        [MapProperty(nameof(OtherExpenseDetailVo.Id), nameof(bs_OtherExpenseDetail.Id))]
        [MapProperty(nameof(OtherExpenseDetailVo.EmployeeRelations), nameof(bs_OtherExpenseDetail.bs_EmployeeRelations))]
        [MapProperty(nameof(OtherExpenseDetailVo.FuelVehicles), nameof(bs_OtherExpenseDetail.bs_FuelVehicles))]
        [MapProperty(nameof(OtherExpenseDetailVo.LossAndDamageClaims), nameof(bs_OtherExpenseDetail.bs_LossAndDamageClaims))]
        [MapProperty(nameof(OtherExpenseDetailVo.OfficeSupplies), nameof(bs_OtherExpenseDetail.bs_OfficeSupplies))]
        [MapProperty(nameof(OtherExpenseDetailVo.OutsideServices), nameof(bs_OtherExpenseDetail.bs_OutsideServices))]
        [MapProperty(nameof(OtherExpenseDetailVo.RentsParking), nameof(bs_OtherExpenseDetail.bs_RentsParking))]
        [MapProperty(nameof(OtherExpenseDetailVo.RepairsAndMaintenance), nameof(bs_OtherExpenseDetail.bs_RepairsAndMaintenance))]
        [MapProperty(nameof(OtherExpenseDetailVo.RepairsAndMaintenanceVehicle), nameof(bs_OtherExpenseDetail.bs_RepairsAndMaintenanceVehicle))]
        [MapProperty(nameof(OtherExpenseDetailVo.Signage), nameof(bs_OtherExpenseDetail.bs_Signage))]
        [MapProperty(nameof(OtherExpenseDetailVo.SuppliesAndEquipment), nameof(bs_OtherExpenseDetail.bs_SuppliesAndEquipment))]
        [MapProperty(nameof(OtherExpenseDetailVo.TicketsAndPrintedMaterial), nameof(bs_OtherExpenseDetail.bs_TicketsAndPrintedMaterial))]
        [MapProperty(nameof(OtherExpenseDetailVo.Uniforms), nameof(bs_OtherExpenseDetail.bs_Uniforms))]
        [MapProperty(nameof(OtherExpenseDetailVo.MonthYear), nameof(bs_OtherExpenseDetail.bs_MonthYear))]
        [MapProperty(nameof(OtherExpenseDetailVo.MiscOtherExpenses), nameof(bs_OtherExpenseDetail.bs_MiscOtherExpenses))]
        [MapProperty(nameof(OtherExpenseDetailVo.TotalOtherExpenses), nameof(bs_OtherExpenseDetail.bs_TotalOtherExpenses))]
        public static partial bs_OtherExpenseDetail MapOtherExpenseDetailVoToModel(OtherExpenseDetailVo vo);

        public static partial OtherExpenseVo OtherExpenseDtoToVo(OtherExpenseDto dto);
    }
}
