using api.Models.Dto;
using api.Models.Vo;
using Newtonsoft.Json;
using Riok.Mapperly.Abstractions;
using TownePark;
using static api.Models.Vo.ContractDetailVo;

namespace api.Adapters.Mappers
{
    [Mapper]
    public static partial class ContractMapper
    {
        // Main mapping method from VO to DTO
        public static partial ContractDetailDto ContractDetailVoToDto(ContractDetailVo vo);

        // Property mappings for bs_Contract to ContractDetailVo
        [MapProperty(nameof(bs_Contract.bs_ContractId), nameof(ContractDetailVo.Id))]
        [MapProperty(nameof(bs_Contract.bs_PurchaseOrder), nameof(ContractDetailVo.PurchaseOrder))]
        [MapProperty(nameof(bs_Contract.bs_PaymentTerms), nameof(ContractDetailVo.PaymentTerms))]
        [MapProperty(nameof(bs_Contract.bs_BillingType), nameof(ContractDetailVo.BillingType))]
        [MapProperty(nameof(bs_Contract.bs_IncrementMonth), nameof(ContractDetailVo.IncrementMonth))]
        [MapProperty(nameof(bs_Contract.bs_IncrementAmount), nameof(ContractDetailVo.IncrementAmount))]
        [MapProperty(nameof(bs_Contract.bs_ConsumerPriceIndex), nameof(ContractDetailVo.ConsumerPriceIndex))]
        [MapProperty(nameof(bs_Contract.bs_Notes), nameof(ContractDetailVo.Notes))]
        [MapProperty(nameof(bs_Contract.bs_DeviationAmount), nameof(ContractDetailVo.DeviationAmount))]
        [MapProperty(nameof(bs_Contract.bs_DeviationPercentage), nameof(ContractDetailVo.DeviationPercentage))]
        [MapProperty(nameof(bs_Contract.bs_Deposits), nameof(ContractDetailVo.Deposits))]
        [MapProperty(nameof(bs_Contract.bs_ContractTypeString), nameof(ContractDetailVo.ContractTypeString))]
        [MapProperty(nameof(bs_Contract.bs_SupportingReports), nameof(ContractDetailVo.SupportingReports))]
        private static partial ContractDetailVo MapContractModelToVo(bs_Contract model);

        public static ContractDetailVo ContractModelToVo(bs_Contract model)
        {
            var vo = MapContractModelToVo(model);

            vo.InvoiceGrouping = MapToInvoiceGroupingVo(model.bs_InvoiceGroup_Contract, model);
            vo.FixedFee = MapToFixedFeeVo(model.bs_FixedFeeService_Contract, model);
            vo.PerLaborHour = MapToPerLaborHourVo(model.bs_LaborHourJob_Contract, model);
            vo.RevenueShare = MapToRevenueShareVo(model.bs_RevenueShareThreshold_Contract, model);
            vo.PerOccupiedRoom = MapToPerOccupiedRoomVo(model);
            vo.BellServiceFee = MapToBellServiceFeeVo(model.bs_BellService_bs_Contract, model);
            vo.MidMonthAdvance = MapToMidMonthAdvanceVo(model.bs_MidMonthAdvance_bs_Contract, model);
            vo.DepositedRevenue = MapToDepositedRevenueVo(model.bs_DepositedRevenue_Contract, model);
            vo.BillableAccount = MapToBillableAccountVo(model.bs_BillableAccount_Contract, model);
            vo.ManagementAgreement = MapToManagementAgreementVo(model.bs_ManagementAgreement_Contract, model);

            return vo;
        }
        
        private static InvoiceGroupingVo MapToInvoiceGroupingVo(IEnumerable<bs_InvoiceGroup?> source, bs_Contract model)
        {
            var perOccupiedRoomVo = new InvoiceGroupingVo()
            {
                Enabled = model.bs_ContractType.Contains(bs_contracttypechoices.InvoiceGrouping),
                InvoiceGroups = source.Select(MapToInvoiceGroupVo).ToList(),
            };
            return perOccupiedRoomVo;
        }

        private static BellServiceFeeVo MapToBellServiceFeeVo(IEnumerable<bs_BellService?> source, bs_Contract model)
        {
            var bellServiceFeeVo = new BellServiceFeeVo
            {
                //set to false by default
                Enabled = model.bs_ContractType.Contains(bs_contracttypechoices.BellService),
                BellServices = source.Select(MapToBellServiceVo).ToList()
            };
            return bellServiceFeeVo;
        }

        [MapProperty(nameof(bs_BellService.bs_BellServiceId), nameof(BellServiceVo.Id))]
        [MapProperty(nameof(bs_BellService.bs_InvoiceGroup), nameof(BellServiceVo.InvoiceGroup))]
        private static partial BellServiceVo MapToBellServiceVo(bs_BellService? source);

        private static DepositedRevenueVo MapToDepositedRevenueVo(IEnumerable<bs_DepositedRevenue?> source, bs_Contract model)
        {
            var depositedRevenueVo = new DepositedRevenueVo
            {
                Enabled = model.bs_ContractType.Contains(bs_contracttypechoices.DepositedRevenue),
                DepositData = source.Select(MapToDepositedDataVo).ToList()
            };
            return depositedRevenueVo;
        }

        [MapProperty(nameof(bs_DepositedRevenue.bs_DepositedRevenueId), nameof(DepositDataVo.Id))]
        [MapProperty(nameof(bs_DepositedRevenue.bs_InvoiceGroup), nameof(DepositDataVo.InvoiceGroup))]
        [MapProperty(nameof(bs_DepositedRevenue.bs_TowneParkResponsibleForParkingTax), nameof(DepositDataVo.TowneParkResponsibleForParkingTax))]
        [MapProperty(nameof(bs_DepositedRevenue.bs_DepositedRevenueEnabled), nameof(DepositDataVo.DepositedRevenueEnabled))]
        private static partial DepositDataVo MapToDepositedDataVo(bs_DepositedRevenue? source);

        private static BillableAccountVo MapToBillableAccountVo(IEnumerable<bs_BillableAccount?> source, bs_Contract model)
        {
            var billableAccountVo = new BillableAccountVo
            {
                Enabled = model.bs_ContractType.Contains(bs_contracttypechoices.BillingAccount),
                BillableAccountsData = source.Select(MapToBillableAccountDataVo).ToList()
            };
            return billableAccountVo;
        }

        [MapProperty(nameof(bs_BillableAccount.bs_BillableAccountId), nameof(BillableAccountDataVo.Id))]
        [MapProperty(nameof(bs_BillableAccount.bs_PayrollAccountsData), nameof(BillableAccountDataVo.PayrollAccountsData))]
        [MapProperty(nameof(bs_BillableAccount.bs_PayrollAccountsInvoiceGroup), nameof(BillableAccountDataVo.PayrollAccountsInvoiceGroup))]
        [MapProperty(nameof(bs_BillableAccount.bs_PayrollAccountsLineTitle), nameof(BillableAccountDataVo.PayrollAccountsLineTitle))]
        [MapProperty(nameof(bs_BillableAccount.bs_PayrollTaxesBillingType), nameof(BillableAccountDataVo.PayrollTaxesBillingType))]
        [MapProperty(nameof(bs_BillableAccount.bs_PayrollTaxesEnabled), nameof(BillableAccountDataVo.PayrollTaxesEnabled))]
        [MapProperty(nameof(bs_BillableAccount.bs_PayrollTaxesLineTitle), nameof(BillableAccountDataVo.PayrollTaxesLineTitle))]
        [MapProperty(nameof(bs_BillableAccount.bs_PayrollTaxesPercentage), nameof(BillableAccountDataVo.PayrollTaxesPercentage))]
        [MapProperty(nameof(bs_BillableAccount.bs_PayrollSupportAmount), nameof(BillableAccountDataVo.PayrollSupportAmount))]
        [MapProperty(nameof(bs_BillableAccount.bs_PayrollSupportBillingType), nameof(BillableAccountDataVo.PayrollSupportBillingType))]
        [MapProperty(nameof(bs_BillableAccount.bs_PayrollSupportEnabled), nameof(BillableAccountDataVo.PayrollSupportEnabled))]
        [MapProperty(nameof(bs_BillableAccount.bs_PayrollSupportLineTitle), nameof(BillableAccountDataVo.PayrollSupportLineTitle))]
        [MapProperty(nameof(bs_BillableAccount.bs_PayrollSupportPayrollType), nameof(BillableAccountDataVo.PayrollSupportPayrollType))]
        [MapProperty(nameof(bs_BillableAccount.bs_ExpenseAccountsData), nameof(BillableAccountDataVo.PayrollExpenseAccountsData))]
        [MapProperty(nameof(bs_BillableAccount.bs_ExpenseAccountsInvoiceGroup), nameof(BillableAccountDataVo.PayrollExpenseAccountsInvoiceGroup))]
        [MapProperty(nameof(bs_BillableAccount.bs_ExpenseAccountsLineTitle), nameof(BillableAccountDataVo.PayrollExpenseAccountsLineTitle))]
        [MapProperty(nameof(bs_BillableAccount.bs_AdditionalPayrollAmount), nameof(BillableAccountDataVo.AdditionalPayrollAmount))]
        [MapProperty(nameof(bs_BillableAccount.bs_PayrollTaxesEscalatorEnable), nameof(BillableAccountDataVo.PayrollTaxesEscalatorEnable))]
        [MapProperty(nameof(bs_BillableAccount.bs_PayrollTaxesEscalatorMonth), nameof(BillableAccountDataVo.PayrollTaxesEscalatorMonth))]
        [MapProperty(nameof(bs_BillableAccount.bs_PayrollTaxesEscalatorvalue), nameof(BillableAccountDataVo.PayrollTaxesEscalatorvalue))]
        [MapProperty(nameof(bs_BillableAccount.bs_PayrollTaxesEscalatorType), nameof(BillableAccountDataVo.PayrollTaxesEscalatorType))]




        private static partial BillableAccountDataVo MapToBillableAccountDataVo(bs_BillableAccount? source);

        private static ManagementAgreementVo MapToManagementAgreementVo(
      IEnumerable<bs_ManagementAgreement?> source,
      bs_Contract model)
        {
            var managementAgreementVo = new ManagementAgreementVo
            {
                Enabled = model.bs_ContractType.Contains(bs_contracttypechoices.ManagementAgreement),
                ManagementFees = source.Select(ma =>
                {
                    var feeVo = MapToManagementFeeVo(ma);

                    feeVo.NonGlBillableExpenses = model.bs_nonglexpense_ContractFK_bs_contract?
                    .OrderBy(expense => expense.bs_SequenceNumber)
                        .Select(expense => new NonGlBillableExpenseVo
                        {
                            Id = expense.Id,
                            NonGLExpenseType = expense.bs_NonGLExpenseType?.ToString(),
                            ExpenseAmount = expense.bs_ExpenseAmount,
                            ExpensePayrollType = expense.bs_ExpensePayrollType?.ToString(),
                            ExpenseTitle = expense.bs_ExpenseTitle,
                            FinalPeriodBilled = expense.bs_FinalPeriodBilled,
                            SequenceNumber = expense.bs_SequenceNumber
                        }).ToList() ?? new List<NonGlBillableExpenseVo>();

                    return feeVo;
                }).ToList()
            };

            return managementAgreementVo;
        }

        [MapProperty(nameof(bs_ManagementAgreement.bs_ManagementAgreementId), nameof(ManagementFeeVo.Id))]
        [MapProperty(nameof(bs_ManagementAgreement.bs_ManagementAgreementType), nameof(ManagementFeeVo.ManagementAgreementType))]
        [MapProperty(nameof(bs_ManagementAgreement.bs_ManagementFeeEscalatorEnabled), nameof(ManagementFeeVo.ManagementFeeEscalatorEnabled))]
        [MapProperty(nameof(bs_ManagementAgreement.bs_ManagementFeeEscalatorMonth), nameof(ManagementFeeVo.ManagementFeeEscalatorMonth))]
        [MapProperty(nameof(bs_ManagementAgreement.bs_ManagementFeeEscalatorType), nameof(ManagementFeeVo.ManagementFeeEscalatorType))]
        [MapProperty(nameof(bs_ManagementAgreement.bs_ManagementFeeEscalatorValue), nameof(ManagementFeeVo.ManagementFeeEscalatorValue))]
        [MapProperty(nameof(bs_ManagementAgreement.bs_PerLaborHourJobCode), nameof(ManagementFeeVo.LaborHourJobCode))]
        [MapProperty(nameof(bs_ManagementAgreement.bs_PerLaborHourJobCodeData), nameof(ManagementFeeVo.PerLaborHourJobCodeData))]
        [MapProperty(nameof(bs_ManagementAgreement.bs_PerLaborHourOvertimeRate), nameof(ManagementFeeVo.LaborHourOvertimeRate))]
        [MapProperty(nameof(bs_ManagementAgreement.bs_PerLaborHourRate), nameof(ManagementFeeVo.LaborHourRate))]
        [MapProperty(nameof(bs_ManagementAgreement.bs_RevenuePercentageAmount), nameof(ManagementFeeVo.RevenuePercentageAmount))]
        [MapProperty(nameof(bs_ManagementAgreement.bs_InvoiceGroup), nameof(ManagementFeeVo.InvoiceGroup))]
        [MapProperty(nameof(bs_ManagementAgreement.bs_FixedFeeAmount), nameof(ManagementFeeVo.FixedFeeAmount))]
        [MapProperty(nameof(bs_ManagementAgreement.bs_InsuranceAdditionalPercentage), nameof(ManagementFeeVo.InsuranceAdditionalPercentage))]
        [MapProperty(nameof(bs_ManagementAgreement.bs_InsuranceEnabled), nameof(ManagementFeeVo.InsuranceEnabled))]
        [MapProperty(nameof(bs_ManagementAgreement.bs_InsuranceFixedFeeAmount), nameof(ManagementFeeVo.InsuranceFixedFeeAmount))]
        [MapProperty(nameof(bs_ManagementAgreement.bs_InsuranceLineTitle), nameof(ManagementFeeVo.InsuranceLineTitle))]
        [MapProperty(nameof(bs_ManagementAgreement.bs_InsuranceType), nameof(ManagementFeeVo.InsuranceType))]
        [MapProperty(nameof(bs_ManagementAgreement.bs_ClaimsCapAmount), nameof(ManagementFeeVo.ClaimsCapAmount))]
        [MapProperty(nameof(bs_ManagementAgreement.bs_ClaimsEnabled), nameof(ManagementFeeVo.ClaimsEnabled))]
        [MapProperty(nameof(bs_ManagementAgreement.bs_ClaimsLineTitle), nameof(ManagementFeeVo.ClaimsLineTitle))]
        [MapProperty(nameof(bs_ManagementAgreement.bs_ClaimsType), nameof(ManagementFeeVo.ClaimsType))]
        [MapProperty(nameof(bs_ManagementAgreement.bs_ProfitShareAccumulationType), nameof(ManagementFeeVo.ProfitShareAccumulationType))]
        [MapProperty(nameof(bs_ManagementAgreement.bs_ProfitShareEnabled), nameof(ManagementFeeVo.ProfitShareEnabled))]
        [MapProperty(nameof(bs_ManagementAgreement.bs_ProfitShareEscalatorEnabled), nameof(ManagementFeeVo.ProfitShareEscalatorEnabled))]
        [MapProperty(nameof(bs_ManagementAgreement.bs_ProfitShareEscalatorMonth), nameof(ManagementFeeVo.ProfitShareEscalatorMonth))]
        [MapProperty(nameof(bs_ManagementAgreement.bs_ProfitShareEscalatorType), nameof(ManagementFeeVo.ProfitShareEscalatorType))]
        [MapProperty(nameof(bs_ManagementAgreement.bs_ProfitShareTierData), nameof(ManagementFeeVo.ProfitShareTierData))]
        [MapProperty(nameof(bs_ManagementAgreement.bs_ValidationThresholdAmount), nameof(ManagementFeeVo.ValidationThresholdAmount))]
        [MapProperty(nameof(bs_ManagementAgreement.bs_ValidationThresholdEnabled), nameof(ManagementFeeVo.ValidationThresholdEnabled))]
        [MapProperty(nameof(bs_ManagementAgreement.bs_ValidationThresholdType), nameof(ManagementFeeVo.ValidationThresholdType))]
        //changes123

        [MapProperty(nameof(bs_ManagementAgreement.bs_NonGLBillableExpensesEnabled), nameof(ManagementFeeVo.NonGlBillableExpensesEnabled))]

        private static partial ManagementFeeVo DefaultMapToManagementFeeVo(bs_ManagementAgreement? source);

        private static ManagementFeeVo MapToManagementFeeVo(bs_ManagementAgreement? source)
        {
            if (source == null)
            {
                return new ManagementFeeVo();
            }

            var managementFeeVo = DefaultMapToManagementFeeVo(source);

            managementFeeVo.PerLaborHourJobCodeData = source.bs_PerLaborHourJobCodeData != null
                ? JsonConvert.DeserializeObject<List<ContractDetailVo.JobCodeVo>>(source.bs_PerLaborHourJobCodeData)
                : new List<ContractDetailVo.JobCodeVo>();

            managementFeeVo.ProfitShareTierData = source.bs_ProfitShareTierData != null
                ? JsonConvert.DeserializeObject<List<TierVo>>(source.bs_ProfitShareTierData)
                : new List<TierVo>();

            return managementFeeVo;
        }

        private static MidMonthAdvanceVo MapToMidMonthAdvanceVo(IEnumerable<bs_MidMonthAdvance?> source, bs_Contract model)
        {
            var midMonthAdvanceVo = new MidMonthAdvanceVo
            {
                Enabled = model.bs_ContractType.Contains(bs_contracttypechoices.MidMonthAdvance),
                MidMonthAdvances = source.Select(MapToMidMonthVo).ToList()
            };
            return midMonthAdvanceVo;
        }

        [MapProperty(nameof(bs_MidMonthAdvance.bs_MidMonthAdvanceId), nameof(MidMonthVo.Id))]
        [MapProperty(nameof(bs_MidMonthAdvance.bs_Amount), nameof(MidMonthVo.Amount))]
        [MapProperty(nameof(bs_MidMonthAdvance.bs_LineTitle), nameof(MidMonthVo.LineTitle))]
        [MapProperty(nameof(bs_MidMonthAdvance.bs_InvoiceGroup), nameof(MidMonthVo.InvoiceGroup))]
        private static partial MidMonthVo MapToMidMonthVo(bs_MidMonthAdvance? source);

        private static PerOccupiedRoomVo MapToPerOccupiedRoomVo(bs_Contract model)
        {
            var perOccupiedRoomVo = new PerOccupiedRoomVo
            {
                Enabled = model.bs_ContractType.Contains(bs_contracttypechoices.PerOccupiedRoom),
                RoomRate = model.bs_OccupiedRoomRate,
                Code = model.bs_OccupiedRoomCode,
                InvoiceGroup = model.bs_OccupiedRoomInvoiceGroup
            };
            return perOccupiedRoomVo;
        }
        
        [MapProperty(nameof(bs_InvoiceGroup.bs_InvoiceGroupId), nameof(InvoiceGroupVo.Id))]
        [MapProperty(nameof(bs_InvoiceGroup.bs_Title), nameof(InvoiceGroupVo.Title))]
        [MapProperty(nameof(bs_InvoiceGroup.bs_Description), nameof(InvoiceGroupVo.Description))]
        [MapProperty(nameof(bs_InvoiceGroup.bs_GroupNumber), nameof(InvoiceGroupVo.GroupNumber))]
        [MapProperty(nameof(bs_InvoiceGroup.bs_BillingContactEmails), nameof(InvoiceGroupVo.BillingContactEmails))]
        [MapProperty(nameof(bs_InvoiceGroup.bs_SiteNumber), nameof(InvoiceGroupVo.SiteNumber))]
        [MapProperty(nameof(bs_InvoiceGroup.bs_VendorId), nameof(InvoiceGroupVo.VendorId))]
        [MapProperty(nameof(bs_InvoiceGroup.bs_CustomerName), nameof(InvoiceGroupVo.CustomerName))]
        private static partial InvoiceGroupVo MapToInvoiceGroupVo(bs_InvoiceGroup? source);

        private static FixedFeeVo MapToFixedFeeVo(IEnumerable<bs_FixedFeeService?> source, bs_Contract model)
        {
            var fixedFeeVo = new FixedFeeVo
            {
                Enabled = model.bs_ContractType.Contains(bs_contracttypechoices.FixedFee),
                ServiceRates = source.Select(MapToServiceRateVo).ToList()
            };
            return fixedFeeVo;
        }

        private static RevenueShareVo MapToRevenueShareVo(IEnumerable<bs_RevenueShareThreshold?> source, bs_Contract model)
        {
            var revenueShareVo = new RevenueShareVo
            {
                Enabled = model.bs_ContractType.Contains(bs_contracttypechoices.RevenueShare),
                ThresholdStructures = source.Select(MapToThresholdStructureVo).ToList()
            };
            return revenueShareVo;
        }

        private static ThresholdStructureVo MapToThresholdStructureVo(bs_RevenueShareThreshold? source)
        {
            if (source == null)
            {
                return new ThresholdStructureVo();
            }

            var thresholdStructureVo = new ThresholdStructureVo
            {
                Id = source.bs_RevenueShareThresholdId ?? Guid.Empty, 
                Tiers = source.bs_TierData != null
                    ? JsonConvert.DeserializeObject<List<TierVo>>(source.bs_TierData)
                    : new List<TierVo>(), 
                RevenueCodes = source.bs_RevenueCodeData != null
                    ? JsonConvert.DeserializeObject<List<string>>(source.bs_RevenueCodeData)
                    : new List<string>(), 
                AccumulationType = source.bs_RevenueAccumulationType.HasValue
                    ? (AccumulationType)source.bs_RevenueAccumulationType.Value
                    : AccumulationType.Monthly, 
                InvoiceGroup = source.bs_InvoiceGroup ?? null, 
                ValidationThresholdAmount = source.bs_ValidationThresholdAmount ?? 0m, 
                ValidationThresholdType = source.bs_ValidationThresholdType.HasValue
                    ? (ValidationThresholdType)source.bs_ValidationThresholdType.Value
                    : null
            };

            return thresholdStructureVo;
        }

        [MapProperty(nameof(bs_FixedFeeService.bs_FixedFeeServiceId), nameof(ServiceRateVo.Id))]
        [MapProperty(nameof(bs_FixedFeeService.bs_Name), nameof(ServiceRateVo.Name))]
        [MapProperty(nameof(bs_FixedFeeService.bs_DisplayName), nameof(ServiceRateVo.DisplayName))]
        [MapProperty(nameof(bs_FixedFeeService.bs_Fee), nameof(ServiceRateVo.Fee))]
        [MapProperty(nameof(bs_FixedFeeService.bs_Code), nameof(ServiceRateVo.Code))]
        [MapProperty(nameof(bs_FixedFeeService.bs_InvoiceGroup), nameof(ServiceRateVo.InvoiceGroup))]
        [MapProperty(nameof(bs_FixedFeeService.bs_StartDate), nameof(ServiceRateVo.StartDate))]
        [MapProperty(nameof(bs_FixedFeeService.bs_EndDate), nameof(ServiceRateVo.EndDate))]
        private static partial ServiceRateVo MapToServiceRateVo(bs_FixedFeeService? source);

        private static PerLaborHourVo MapToPerLaborHourVo(IEnumerable<bs_LaborHourJob?> source, bs_Contract model)
        {
            var perLaborHourVo = new PerLaborHourVo
            {
                Enabled = model.bs_ContractType.Contains(bs_contracttypechoices.PerLaborHour),
                HoursBackupReport = model.bs_HoursBackupReport,
                JobRates = source.Select(MapToJobRateVo).ToList()
            };
            return perLaborHourVo;
        }
        
        [MapProperty(nameof(bs_LaborHourJob.bs_LaborHourJobId), nameof(JobRateVo.Id))]
        [MapProperty(nameof(bs_LaborHourJob.bs_Name), nameof(JobRateVo.Name))]
        [MapProperty(nameof(bs_LaborHourJob.bs_DisplayName), nameof(JobRateVo.DisplayName))]
        [MapProperty(nameof(bs_LaborHourJob.bs_Rate), nameof(JobRateVo.Rate))]
        [MapProperty(nameof(bs_LaborHourJob.bs_OvertimeRate), nameof(JobRateVo.OvertimeRate))]
        [MapProperty(nameof(bs_LaborHourJob.bs_InvoiceGroup), nameof(JobRateVo.InvoiceGroup))]
        [MapProperty(nameof(bs_LaborHourJob.bs_Code), nameof(JobRateVo.Code))]
        [MapProperty(nameof(bs_LaborHourJob.bs_JobCode), nameof(JobRateVo.JobCode))]
        [MapProperty(nameof(bs_LaborHourJob.bs_StartDate), nameof(JobRateVo.StartDate))]
        [MapProperty(nameof(bs_LaborHourJob.bs_EndDate), nameof(JobRateVo.EndDate))]
        private static partial JobRateVo MapToJobRateVo(bs_LaborHourJob? source);
    }
}
