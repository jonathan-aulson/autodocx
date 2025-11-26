using api.Models.Dto;
using api.Models.Vo;
using api.Models.Vo.Enum;
using Microsoft.Extensions.Azure;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using Riok.Mapperly.Abstractions;
using TownePark;
using static api.Models.Vo.ContractDetailVo;

namespace api.Adapters.Mappers
{
    [Mapper]
    public static partial class UpdateContractMapper
    {
        public static partial ContractDetailVo ContractDetailDtoToVo(ContractDetailDto source);

        public static bs_Contract ContractDetailVoToUpdateModel(ContractDetailVo existingContract,
            ContractDetailVo updateContract)
        {
            var newModel = new bs_Contract();

            if (existingContract.IncrementAmount != updateContract.IncrementAmount)
            {
                newModel.bs_IncrementAmount = updateContract.IncrementAmount;
            }
      
            if (existingContract.IncrementMonth != updateContract.IncrementMonth)
            {
                newModel.bs_IncrementMonth = MapIncrementMonthVoToModel(updateContract.IncrementMonth);
            }

            if (existingContract.BillingType != updateContract.BillingType)
            {
                newModel.bs_BillingType = MapBillingTypeVoToModel(updateContract.BillingType);
            }

            if (existingContract.PaymentTerms != updateContract.PaymentTerms)
            {
                newModel.bs_PaymentTerms = updateContract.PaymentTerms;
            }

            if (existingContract.PurchaseOrder != updateContract.PurchaseOrder)
            {
                newModel.bs_PurchaseOrder = updateContract.PurchaseOrder;
            }

            if (existingContract.ConsumerPriceIndex != updateContract.ConsumerPriceIndex)
            {
                newModel.bs_ConsumerPriceIndex = updateContract.ConsumerPriceIndex;
            }

            if (existingContract.Notes != updateContract.Notes)
            {
                newModel.bs_Notes = updateContract.Notes;
            }

            if (existingContract.DeviationAmount != updateContract.DeviationAmount)
            {
                newModel.bs_DeviationAmount = updateContract.DeviationAmount;
            }

            if (existingContract.DeviationPercentage != updateContract.DeviationPercentage)
            {
                newModel.bs_DeviationPercentage = updateContract.DeviationPercentage;
            }

            if (existingContract.Deposits != updateContract.Deposits)
            {
                newModel.bs_Deposits = updateContract.Deposits;
            }

            if (existingContract.ContractTypeString != updateContract.ContractTypeString)
            {
                newModel.bs_ContractTypeString = updateContract.ContractTypeString;
            }

            if (existingContract.PerOccupiedRoom.RoomRate != updateContract.PerOccupiedRoom.RoomRate)
            {
                newModel.bs_OccupiedRoomRate = updateContract.PerOccupiedRoom.RoomRate;
            }

            if (existingContract.PerOccupiedRoom.Code != updateContract.PerOccupiedRoom.Code)
            {
                newModel.bs_OccupiedRoomCode = updateContract.PerOccupiedRoom.Code;
            }

            if (existingContract.PerOccupiedRoom.InvoiceGroup != updateContract.PerOccupiedRoom.InvoiceGroup)
            {
                newModel.bs_OccupiedRoomInvoiceGroup = updateContract.PerOccupiedRoom.InvoiceGroup;
            }


            if (existingContract.SupportingReports != updateContract.SupportingReports)
            {
                var supportingReports = new List<bs_supportingreporttypes>();

                foreach (var reportType in updateContract.SupportingReports)
                {
                    supportingReports.Add((bs_supportingreporttypes)reportType);
                }

                newModel.bs_SupportingReports = supportingReports;
            }
            

            var existingContractTypes = new List<bs_contracttypechoices>();
            if (existingContract.FixedFee.Enabled) existingContractTypes.Add(bs_contracttypechoices.FixedFee);
            if (existingContract.PerLaborHour.Enabled) existingContractTypes.Add(bs_contracttypechoices.PerLaborHour);
            if (existingContract.PerOccupiedRoom.Enabled) existingContractTypes.Add(bs_contracttypechoices.PerOccupiedRoom);
            if (existingContract.RevenueShare.Enabled) existingContractTypes.Add(bs_contracttypechoices.RevenueShare);
            if (existingContract.BellServiceFee.Enabled) existingContractTypes.Add(bs_contracttypechoices.BellService);
            if (existingContract.MidMonthAdvance.Enabled) existingContractTypes.Add(bs_contracttypechoices.MidMonthAdvance);
            if (existingContract.DepositedRevenue.Enabled) existingContractTypes.Add(bs_contracttypechoices.DepositedRevenue);
            if (existingContract.BillableAccount.Enabled) existingContractTypes.Add(bs_contracttypechoices.BillingAccount);
            if (existingContract.ManagementAgreement.Enabled) existingContractTypes.Add(bs_contracttypechoices.ManagementAgreement);
            if (existingContract.InvoiceGrouping.Enabled) existingContractTypes.Add(bs_contracttypechoices.InvoiceGrouping);

            var newContractTypes = new List<bs_contracttypechoices>();
            if (updateContract.FixedFee.Enabled) newContractTypes.Add(bs_contracttypechoices.FixedFee);
            if (updateContract.PerLaborHour.Enabled) newContractTypes.Add(bs_contracttypechoices.PerLaborHour);
            if (updateContract.PerOccupiedRoom.Enabled) newContractTypes.Add(bs_contracttypechoices.PerOccupiedRoom);
            if (updateContract.RevenueShare.Enabled) newContractTypes.Add(bs_contracttypechoices.RevenueShare);
            if (updateContract.BellServiceFee.Enabled) newContractTypes.Add(bs_contracttypechoices.BellService);
            if (updateContract.MidMonthAdvance.Enabled) newContractTypes.Add(bs_contracttypechoices.MidMonthAdvance);
            if (updateContract.DepositedRevenue.Enabled) newContractTypes.Add(bs_contracttypechoices.DepositedRevenue);
            if (updateContract.BillableAccount.Enabled) newContractTypes.Add(bs_contracttypechoices.BillingAccount);
            if (updateContract.ManagementAgreement.Enabled) newContractTypes.Add(bs_contracttypechoices.ManagementAgreement);
            if (updateContract.InvoiceGrouping.Enabled) newContractTypes.Add(bs_contracttypechoices.InvoiceGrouping);

            if (!existingContractTypes.SequenceEqual(newContractTypes))
            {
                newModel.bs_ContractType = newContractTypes;
            }

            if (existingContract.PerLaborHour.HoursBackupReport != updateContract.PerLaborHour.HoursBackupReport)
            {
                newModel.bs_HoursBackupReport = updateContract.PerLaborHour.HoursBackupReport;
            }

            if (existingContract.FixedFee is { ServiceRates: not null } &&
                !existingContract.FixedFee.ServiceRates.IsNullOrEmpty())
            {
                var servicesToUpdate = existingContract.FixedFee.ServiceRates
                    .Select(
                        existingServiceRate =>
                        {
                            var newServiceRate = updateContract.FixedFee.ServiceRates
                                .FirstOrDefault(serviceRate => serviceRate.Id == existingServiceRate.Id);
                            return newServiceRate == null ? null : ServiceVoToModel(existingServiceRate, newServiceRate);
                        })
                    .Where(service => service != null)
                    .Select(e => e!)
                    .ToList();

                if (!servicesToUpdate.IsNullOrEmpty()) newModel.bs_FixedFeeService_Contract = servicesToUpdate;
            }

            if (existingContract.BellServiceFee is { BellServices: not null } &&
                !existingContract.BellServiceFee.BellServices.IsNullOrEmpty())
            {
                var bellServicesToUpdate = existingContract.BellServiceFee.BellServices
                    .Select(existingBellService =>
                    {
                        var newBellService = updateContract.BellServiceFee.BellServices
                            .FirstOrDefault(bellService => bellService.Id == existingBellService.Id);
                        return newBellService == null ? null : BellServiceVoToModel(existingBellService, newBellService);
                    })
                    .Where(bellService => bellService != null)
                    .Select(e => e!)
                    .ToList();

                if (!bellServicesToUpdate.IsNullOrEmpty()) newModel.bs_BellService_bs_Contract = bellServicesToUpdate;
            }

            if (existingContract.MidMonthAdvance is { MidMonthAdvances: not null } &&
                !existingContract.MidMonthAdvance.MidMonthAdvances.IsNullOrEmpty())
            {
                var midMonthAdvancesToUpdate = existingContract.MidMonthAdvance.MidMonthAdvances
                    .Select(existingMidMonthAdvance =>
                    {
                        var newMidMonthAdvance = updateContract.MidMonthAdvance.MidMonthAdvances
                            .FirstOrDefault(midMonthAdvance => midMonthAdvance.Id == existingMidMonthAdvance.Id);
                        return newMidMonthAdvance == null ? null : MidMonthAdvanceVoToModel(existingMidMonthAdvance, newMidMonthAdvance);
                    })
                    .Where(midMonthAdvance => midMonthAdvance != null)
                    .Select(e => e!)
                    .ToList();

                if (!midMonthAdvancesToUpdate.IsNullOrEmpty()) newModel.bs_MidMonthAdvance_bs_Contract = midMonthAdvancesToUpdate;
            }

            if (existingContract.DepositedRevenue is { DepositData: not null} &&
                !existingContract.DepositedRevenue.DepositData.IsNullOrEmpty())
            {
                var depositedRevenueToUpdate = existingContract.DepositedRevenue.DepositData
                    .Select(existingDeposit =>
                    {
                        var newDeposit = updateContract.DepositedRevenue.DepositData
                            .FirstOrDefault(deposit => deposit.Id == existingDeposit.Id);
                        return newDeposit == null ? null : DepositDataVoToModel(existingDeposit, newDeposit);
                    })
                    .Where(deposit => deposit != null)
                    .Select(e => e!)
                    .ToList();

                if (!depositedRevenueToUpdate.IsNullOrEmpty()) newModel.bs_DepositedRevenue_Contract = depositedRevenueToUpdate;
            }

            if (existingContract.BillableAccount is { BillableAccountsData: not null } &&
                !existingContract.BillableAccount.BillableAccountsData.IsNullOrEmpty())
            {
                var billableAccountsToUpdate = existingContract.BillableAccount.BillableAccountsData
                    .Select(existingBillableAccount =>
                    {
                        var newBillableAccount = updateContract.BillableAccount.BillableAccountsData
                            .FirstOrDefault(billableAccount => billableAccount.Id == existingBillableAccount.Id);
                        return newBillableAccount == null ? null : BillableAccountDataVoToModel(existingBillableAccount, newBillableAccount);
                    })
                    .Where(billableAccount => billableAccount != null)
                    .Select(e => e!)
                    .ToList();

                if (!billableAccountsToUpdate.IsNullOrEmpty()) newModel.bs_BillableAccount_Contract = billableAccountsToUpdate;
            }

            if (existingContract.ManagementAgreement is { ManagementFees: not null} &&
                !existingContract.ManagementAgreement.ManagementFees.IsNullOrEmpty())
            {
                var managementFeesToUpdate = existingContract.ManagementAgreement.ManagementFees
                    .Select(existingManagementFee =>
                    {
                        var newManagementFee = updateContract.ManagementAgreement.ManagementFees
                            .FirstOrDefault(managementFee => managementFee.Id == existingManagementFee.Id);
                        return newManagementFee == null ? null : ManagementFeeVoToModel(existingManagementFee, newManagementFee);
                    })
                    .Where(managementFee => managementFee != null)
                    .Select(e => e!)
                    .ToList();

                if (!managementFeesToUpdate.IsNullOrEmpty()) newModel.bs_ManagementAgreement_Contract = managementFeesToUpdate;
            }

            if (existingContract.PerLaborHour is { JobRates: not null } &&
                !existingContract.PerLaborHour.JobRates.IsNullOrEmpty())
            {
                var jobsToUpdate = existingContract.PerLaborHour.JobRates
                    .Select(existingJobRate =>
                    {
                        var newJobRate = updateContract.PerLaborHour.JobRates
                            .FirstOrDefault(jobRate => jobRate.Id == existingJobRate.Id);
                        return newJobRate == null ? null : JobRateVoToModel(existingJobRate, newJobRate);
                    })
                    .Where(jobRate => jobRate != null)
                    .Select(e => e!)
                    .ToList();

                if (!jobsToUpdate.IsNullOrEmpty()) newModel.bs_LaborHourJob_Contract = jobsToUpdate;
            }

            if (existingContract.RevenueShare is { ThresholdStructures: not null } &&
                !existingContract.RevenueShare.ThresholdStructures.IsNullOrEmpty())
            {
                var thresholdStructuresToUpdate = existingContract.RevenueShare.ThresholdStructures
                    .Select(existingThresholdStructure =>
                    {
                        var newThresholdStructure = updateContract.RevenueShare.ThresholdStructures
                            .FirstOrDefault(thresholdStructure => thresholdStructure.Id == existingThresholdStructure.Id);
                        return newThresholdStructure == null ? null : ThresholdStructureVoToModel(existingThresholdStructure, newThresholdStructure);
                    })
                    .Where(thresholdStructure => thresholdStructure != null)
                    .Select(e => e!)
                    .ToList();

                if (!thresholdStructuresToUpdate.IsNullOrEmpty()) newModel.bs_RevenueShareThreshold_Contract = thresholdStructuresToUpdate;
            }

            if (updateContract.InvoiceGrouping.Enabled)
            {
                if (!updateContract.InvoiceGrouping.InvoiceGroups.IsNullOrEmpty())
                {
                    var invoiceGroupsToUpdate = existingContract.InvoiceGrouping.InvoiceGroups
                        .Select(existingGroup =>
                        {
                            var newInvoiceGroup = updateContract.InvoiceGrouping.InvoiceGroups
                                .FirstOrDefault(invoiceGroup => invoiceGroup.Id == existingGroup.Id);
                            return newInvoiceGroup == null ? null : InvoiceGroupVoToModel(existingGroup, newInvoiceGroup);
                        })
                        .Where(invoiceGroup => invoiceGroup != null)
                        .Select(e => e!)
                        .ToList();

                    if (!invoiceGroupsToUpdate.IsNullOrEmpty())
                    {
                        newModel.bs_InvoiceGroup_Contract = invoiceGroupsToUpdate;
                    }
                }
            }

            return newModel;
        }

        private static partial int? MapIncrementMonthVoToModel(Month? vo);
        private static partial int? MapEscalatorMonthVoToModel(Month? vo);
        private static partial bs_billingtypechoices? MapBillingTypeVoToModel(BillingType? vo);

        private static bs_InvoiceGroup? InvoiceGroupVoToModel(InvoiceGroupVo? existingGroup, InvoiceGroupVo newGroup)
        {
            var model = new bs_InvoiceGroup();
            var changedField = false;

            if (existingGroup != null && existingGroup.Id.HasValue && existingGroup.Id.Value != Guid.Empty)
            {
                model.Id = existingGroup.Id.Value;
                model.bs_InvoiceGroupId = existingGroup.Id;
            }

            if (existingGroup == null)
            {
                model.bs_Title = newGroup.Title;
                model.bs_Description = newGroup.Description;
                model.bs_GroupNumber = newGroup.GroupNumber;
                model.bs_BillingContactEmails = newGroup.BillingContactEmails;
                model.bs_VendorId = newGroup.VendorId;
                model.bs_SiteNumber = newGroup.SiteNumber;
                model.bs_CustomerName = newGroup.CustomerName;

                changedField = true; 
            }
            else
            {
                if ((existingGroup.Title != null && newGroup.Title != null && existingGroup.Title != newGroup.Title) ||
                    (existingGroup.Title == null && newGroup.Title != null))
                {
                    model.bs_Title = newGroup.Title;
                    changedField = true;
                }

                if ((existingGroup.Description != null && newGroup.Description != null && existingGroup.Description != newGroup.Description) ||
                    (existingGroup.Description == null && newGroup.Description != null))
                {
                    model.bs_Description = newGroup.Description;
                    changedField = true;
                }

                if (existingGroup.GroupNumber != newGroup.GroupNumber)
                {
                    model.bs_GroupNumber = newGroup.GroupNumber;
                    changedField = true;
                }

                if (newGroup.BillingContactEmails != null &&
                    (existingGroup.BillingContactEmails == null ||
                     existingGroup.BillingContactEmails != newGroup.BillingContactEmails))
                {
                    model.bs_BillingContactEmails = newGroup.BillingContactEmails;
                    changedField = true;
                }

                if ((existingGroup.VendorId != null && newGroup.VendorId != null && existingGroup.VendorId != newGroup.VendorId) ||
                    (existingGroup.VendorId == null && newGroup.VendorId != null))
                {
                    model.bs_VendorId = newGroup.VendorId;
                    changedField = true;
                }

                if ((existingGroup.SiteNumber != null && newGroup.SiteNumber != null && existingGroup.SiteNumber != newGroup.SiteNumber) ||
                    (existingGroup.SiteNumber == null && newGroup.SiteNumber != null))
                {
                    model.bs_SiteNumber = newGroup.SiteNumber; 
                    changedField = true;
                }

                if ((existingGroup.CustomerName != null && newGroup.CustomerName != null && existingGroup.CustomerName != newGroup.CustomerName) ||
                    (existingGroup.CustomerName == null && newGroup.CustomerName != null))
                {
                    model.bs_CustomerName = newGroup.CustomerName;
                    changedField = true;
                }
            }

            return changedField ? model : null;
        }

        private static bs_FixedFeeService? ServiceVoToModel(ServiceRateVo existingService, ServiceRateVo newService)
        {
            var model = new bs_FixedFeeService();
            model.bs_FixedFeeServiceId = newService.Id;

            var changedField = false;
            if (existingService.DisplayName != newService.DisplayName)
            {
                model.bs_DisplayName = newService.DisplayName;
                changedField = true;
            }

            if (existingService.Fee != newService.Fee)
            {
                model.bs_Fee = newService.Fee;
                changedField = true;
            }

            if (existingService.Name != newService.Name)
            {
                model.bs_Name = newService.Name;
                changedField = true;
            }

            if (existingService.Code != newService.Code)
            {
                model.bs_Code = newService.Code;
                changedField = true;
            }

            if (existingService.InvoiceGroup != newService.InvoiceGroup)
            {
                model.bs_InvoiceGroup = newService.InvoiceGroup;
                changedField = true;
            }

            return changedField ? model : null;
        }

        private static bs_RevenueShareThreshold? ThresholdStructureVoToModel(ThresholdStructureVo existingThreshold,
            ThresholdStructureVo newThreshold)
        {
            var model = new bs_RevenueShareThreshold();
            model.bs_RevenueShareThresholdId = newThreshold.Id;

            var changedField = false;
            if (existingThreshold.AccumulationType != newThreshold.AccumulationType)
            {
                model.bs_RevenueAccumulationType = (bs_revenueaccumulationtype?)newThreshold.AccumulationType;
                changedField = true;
            }

            if (existingThreshold.Tiers != newThreshold.Tiers)
            {
                model.bs_TierData = JsonConvert.SerializeObject(newThreshold.Tiers);
                changedField = true;
            }

            if (existingThreshold.RevenueCodes != newThreshold.RevenueCodes)
            {
                model.bs_RevenueCodeData = JsonConvert.SerializeObject(newThreshold.RevenueCodes);
                changedField = true;
            }
            if (existingThreshold.InvoiceGroup != newThreshold.InvoiceGroup)
            {
                model.bs_InvoiceGroup = newThreshold.InvoiceGroup;
                changedField = true;
            }

            if (existingThreshold.ValidationThresholdAmount != newThreshold.ValidationThresholdAmount)
            {
                model.bs_ValidationThresholdAmount = newThreshold.ValidationThresholdAmount;
                changedField = true;
            }

            if (existingThreshold.ValidationThresholdType != newThreshold.ValidationThresholdType)
            {
                model.bs_ValidationThresholdType = (bs_validationthresholdtype?)newThreshold.ValidationThresholdType;
                changedField = true;
            }
            return changedField ? model : null;
        }

        private static bs_BellService? BellServiceVoToModel(BellServiceVo existingBellService, BellServiceVo newBellService)
        {
            var model = new bs_BellService();
            model.bs_BellServiceId = newBellService.Id;

            var changedField = false;
            if (existingBellService.InvoiceGroup != newBellService.InvoiceGroup)
            {
                model.bs_InvoiceGroup = newBellService.InvoiceGroup;
                changedField = true;
            }

            return changedField ? model : null;
        }

        private static bs_MidMonthAdvance? MidMonthAdvanceVoToModel(MidMonthVo existingMidMonth, MidMonthVo newMidMonth)
        {
            var model = new bs_MidMonthAdvance();
            model.bs_MidMonthAdvanceId = newMidMonth.Id;

            var changedField = false;
            if (existingMidMonth.Amount != newMidMonth.Amount)
            {
                model.bs_Amount = newMidMonth.Amount;
                changedField = true;
            }

            if (existingMidMonth.LineTitle != newMidMonth.LineTitle)
            {
                model.bs_LineTitle = (bs_lineitemtitle?)newMidMonth.LineTitle;
                changedField = true;
            }

            if (existingMidMonth.InvoiceGroup != newMidMonth.InvoiceGroup)
            {
                model.bs_InvoiceGroup = newMidMonth.InvoiceGroup;
                changedField = true;
            }

            return changedField ? model : null;
        }

        private static bs_DepositedRevenue? DepositDataVoToModel(DepositDataVo existingDepositData, DepositDataVo newDepositData)
        {
            var model = new bs_DepositedRevenue();
            model.bs_DepositedRevenueId = newDepositData.Id;

            var changedField = false;
            if (existingDepositData.TowneParkResponsibleForParkingTax != newDepositData.TowneParkResponsibleForParkingTax)
            {
                model.bs_TowneParkResponsibleForParkingTax = newDepositData.TowneParkResponsibleForParkingTax;
                changedField = true;
            }

            if (existingDepositData.InvoiceGroup != newDepositData.InvoiceGroup)
            {
                model.bs_InvoiceGroup = newDepositData.InvoiceGroup;
                changedField = true;
            }

            if (existingDepositData.DepositedRevenueEnabled != newDepositData.DepositedRevenueEnabled)
            {
                model.bs_DepositedRevenueEnabled = newDepositData.DepositedRevenueEnabled;
                changedField = true;
            }

            return changedField ? model : null;
        }

        private static bs_BillableAccount? BillableAccountDataVoToModel(BillableAccountDataVo existingBillableAccount, BillableAccountDataVo newBillableAccount)
        {
            var model = new bs_BillableAccount();
            model.bs_BillableAccountId = newBillableAccount.Id;

            var changedField = false;
            if (existingBillableAccount.PayrollAccountsInvoiceGroup != newBillableAccount.PayrollAccountsInvoiceGroup)
            {
                model.bs_PayrollAccountsInvoiceGroup = newBillableAccount.PayrollAccountsInvoiceGroup;
                changedField = true;
            }

            if (existingBillableAccount.PayrollAccountsData != newBillableAccount.PayrollAccountsData)
            {
                model.bs_PayrollAccountsData = newBillableAccount.PayrollAccountsData;
                changedField = true;
            }

            if (existingBillableAccount.PayrollAccountsLineTitle != newBillableAccount.PayrollAccountsLineTitle)
            {
                model.bs_PayrollAccountsLineTitle = newBillableAccount.PayrollAccountsLineTitle;
                changedField = true;
            }

            if (existingBillableAccount.PayrollTaxesBillingType != newBillableAccount.PayrollTaxesBillingType)
            {
                model.bs_PayrollTaxesBillingType = (bs_ptebbillingtype?)newBillableAccount.PayrollTaxesBillingType;
                changedField = true;
            }

            if (existingBillableAccount.PayrollTaxesEnabled != newBillableAccount.PayrollTaxesEnabled)
            {
                model.bs_PayrollTaxesEnabled = newBillableAccount.PayrollTaxesEnabled;
                changedField = true;
            }

            if (existingBillableAccount.PayrollTaxesPercentage != newBillableAccount.PayrollTaxesPercentage)
            {
                model.bs_PayrollTaxesPercentage = newBillableAccount.PayrollTaxesPercentage;
                changedField = true;
            }

            if (existingBillableAccount.PayrollTaxesLineTitle != newBillableAccount.PayrollTaxesLineTitle)
            {
                model.bs_PayrollTaxesLineTitle = newBillableAccount.PayrollTaxesLineTitle;
                changedField = true;
            }

            if (existingBillableAccount.PayrollSupportPayrollType != newBillableAccount.PayrollSupportPayrollType)
            {
                model.bs_PayrollSupportPayrollType = (bs_supportpayroll?)newBillableAccount.PayrollSupportPayrollType;
                changedField = true;
            }

            if (existingBillableAccount.PayrollSupportLineTitle != newBillableAccount.PayrollSupportLineTitle)
            {
                model.bs_PayrollSupportLineTitle = newBillableAccount.PayrollSupportLineTitle;
                changedField = true;
            }

            if (existingBillableAccount.PayrollSupportEnabled != newBillableAccount.PayrollSupportEnabled)
            {
                model.bs_PayrollSupportEnabled = newBillableAccount.PayrollSupportEnabled;
                changedField = true;
            }

            if (existingBillableAccount.PayrollSupportBillingType != newBillableAccount.PayrollSupportBillingType)
            {
                model.bs_PayrollSupportBillingType = (bs_supportservices?)newBillableAccount.PayrollSupportBillingType;
                changedField = true;
            }

            if (existingBillableAccount.PayrollSupportAmount != newBillableAccount.PayrollSupportAmount)
            {
                model.bs_PayrollSupportAmount = newBillableAccount.PayrollSupportAmount;
                changedField = true;
            }

            if (existingBillableAccount.PayrollExpenseAccountsData != newBillableAccount.PayrollExpenseAccountsData)
            {
                model.bs_ExpenseAccountsData = newBillableAccount.PayrollExpenseAccountsData;
                changedField = true;
            }

            if (existingBillableAccount.PayrollExpenseAccountsInvoiceGroup != newBillableAccount.PayrollExpenseAccountsInvoiceGroup)
            {
                model.bs_ExpenseAccountsInvoiceGroup = newBillableAccount.PayrollExpenseAccountsInvoiceGroup;
                changedField = true;
            }

            if (existingBillableAccount.PayrollExpenseAccountsLineTitle != newBillableAccount.PayrollExpenseAccountsLineTitle)
            {
                model.bs_ExpenseAccountsLineTitle = newBillableAccount.PayrollExpenseAccountsLineTitle;
                changedField = true;
            }
           
            if (existingBillableAccount.AdditionalPayrollAmount != newBillableAccount.AdditionalPayrollAmount)
            {
                model.bs_AdditionalPayrollAmount = newBillableAccount.AdditionalPayrollAmount;
                changedField = true;
            }
           if (existingBillableAccount.PayrollTaxesEscalatorEnable != newBillableAccount.PayrollTaxesEscalatorEnable)
            {
                model.bs_PayrollTaxesEscalatorEnable = newBillableAccount.PayrollTaxesEscalatorEnable;
                changedField = true;

            }
           if (existingBillableAccount.PayrollTaxesEscalatorMonth != newBillableAccount.PayrollTaxesEscalatorMonth)
            {
                model.bs_PayrollTaxesEscalatorMonth = MapEscalatorMonthVoToModel(newBillableAccount.PayrollTaxesEscalatorMonth);
                changedField = true;
            }
            if (existingBillableAccount.PayrollTaxesEscalatorvalue != newBillableAccount.PayrollTaxesEscalatorvalue)
            {
                model.bs_PayrollTaxesEscalatorvalue = newBillableAccount.PayrollTaxesEscalatorvalue;
                changedField = true;
            } 

            if (existingBillableAccount.PayrollTaxesEscalatorType != newBillableAccount.PayrollTaxesEscalatorType)
            {
                model.bs_PayrollTaxesEscalatorType = (bs_escalatortype?)newBillableAccount.PayrollTaxesEscalatorType;
                changedField = true;
            }  
            return changedField ? model : null;
        }

        private static bs_ManagementAgreement? ManagementFeeVoToModel(ManagementFeeVo existingManagementFee, ManagementFeeVo newManagementFee)
        {
            var model = new bs_ManagementAgreement();
            model.bs_ManagementAgreementId = newManagementFee.Id;

            var changedField = false;

            if (existingManagementFee.InvoiceGroup != newManagementFee.InvoiceGroup)
            {
                model.bs_InvoiceGroup = newManagementFee.InvoiceGroup;
                changedField = true;
            }

            if (existingManagementFee.ManagementAgreementType != newManagementFee.ManagementAgreementType)
            {
                model.bs_ManagementAgreementType = (bs_managementagreementtype?)newManagementFee.ManagementAgreementType;
                changedField = true;
            }

            if (existingManagementFee.ManagementFeeEscalatorEnabled != newManagementFee.ManagementFeeEscalatorEnabled)
            {
                model.bs_ManagementFeeEscalatorEnabled = newManagementFee.ManagementFeeEscalatorEnabled;
                changedField = true;
            }

            if (existingManagementFee.ManagementFeeEscalatorMonth != newManagementFee.ManagementFeeEscalatorMonth)
            {
                model.bs_ManagementFeeEscalatorMonth = MapEscalatorMonthVoToModel(newManagementFee.ManagementFeeEscalatorMonth);
                changedField = true;
            }

            if (existingManagementFee.ManagementFeeEscalatorType != newManagementFee.ManagementFeeEscalatorType)
            {
                model.bs_ManagementFeeEscalatorType = (bs_escalatortype?)newManagementFee.ManagementFeeEscalatorType;
                changedField = true;
            }

            if (existingManagementFee.ManagementFeeEscalatorValue != newManagementFee.ManagementFeeEscalatorValue)
            {
                model.bs_ManagementFeeEscalatorValue = newManagementFee.ManagementFeeEscalatorValue;
                changedField = true;
            }

            if (existingManagementFee.FixedFeeAmount != newManagementFee.FixedFeeAmount)
            {
                model.bs_FixedFeeAmount = newManagementFee.FixedFeeAmount;
                changedField = true;
            }

            if (existingManagementFee.LaborHourJobCode != newManagementFee.LaborHourJobCode)
            {
                model.bs_PerLaborHourJobCode = newManagementFee.LaborHourJobCode;
                changedField = true;
            }

            if (existingManagementFee.PerLaborHourJobCodeData != newManagementFee.PerLaborHourJobCodeData)
            {
                model.bs_PerLaborHourJobCodeData = JsonConvert.SerializeObject(newManagementFee.PerLaborHourJobCodeData);
                changedField = true;
            }

            if (existingManagementFee.LaborHourRate != newManagementFee.LaborHourRate)
            {
                model.bs_PerLaborHourRate = newManagementFee.LaborHourRate;
                changedField = true;
            }

            if (existingManagementFee.LaborHourOvertimeRate != newManagementFee.LaborHourOvertimeRate)
            {
                model.bs_PerLaborHourOvertimeRate = newManagementFee.LaborHourOvertimeRate;
                changedField = true;
            }

            if (existingManagementFee.RevenuePercentageAmount != newManagementFee.RevenuePercentageAmount)
            {
                model.bs_RevenuePercentageAmount = newManagementFee.RevenuePercentageAmount;
                changedField = true;
            }

            if (existingManagementFee.InsuranceEnabled != newManagementFee.InsuranceEnabled)
            {
                model.bs_InsuranceEnabled = newManagementFee.InsuranceEnabled;
                changedField = true;
            }

            if (existingManagementFee.InsuranceLineTitle != newManagementFee.InsuranceLineTitle)
            {
                model.bs_InsuranceLineTitle = newManagementFee.InsuranceLineTitle;
                changedField = true;
            }

            if (existingManagementFee.InsuranceType != newManagementFee.InsuranceType)
            {
                model.bs_InsuranceType = (bs_managementagreementinsurancetype?)newManagementFee.InsuranceType;
                changedField = true;
            }

            if (existingManagementFee.InsuranceAdditionalPercentage != newManagementFee.InsuranceAdditionalPercentage)
            {
                model.bs_InsuranceAdditionalPercentage = newManagementFee.InsuranceAdditionalPercentage;
                changedField = true;
            }

            if (existingManagementFee.InsuranceFixedFeeAmount != newManagementFee.InsuranceFixedFeeAmount)
            {
                model.bs_InsuranceFixedFeeAmount = newManagementFee.InsuranceFixedFeeAmount;
                changedField = true;
            }

            if (existingManagementFee.ClaimsCapAmount != newManagementFee.ClaimsCapAmount)
            {
                model.bs_ClaimsCapAmount = newManagementFee.ClaimsCapAmount;
                changedField = true;
            }

            if (existingManagementFee.ClaimsEnabled != newManagementFee.ClaimsEnabled)
            {
                model.bs_ClaimsEnabled = newManagementFee.ClaimsEnabled;
                changedField = true;
            }

            if (existingManagementFee.ClaimsLineTitle != newManagementFee.ClaimsLineTitle)
            {
                model.bs_ClaimsLineTitle = newManagementFee.ClaimsLineTitle;
                changedField = true;
            }

            if (existingManagementFee.ClaimsType != newManagementFee.ClaimsType)
            {
                model.bs_ClaimsType = (bs_claimtype?)newManagementFee.ClaimsType;
                changedField = true;
            }

            if (existingManagementFee.ProfitShareAccumulationType != newManagementFee.ProfitShareAccumulationType)
            {
                model.bs_ProfitShareAccumulationType = (bs_profitshareaccumulationtype?)newManagementFee.ProfitShareAccumulationType;
                changedField = true;
            }

            if (existingManagementFee.ProfitShareEnabled != newManagementFee.ProfitShareEnabled)
            {
                model.bs_ProfitShareEnabled = newManagementFee.ProfitShareEnabled;
                changedField = true;
            }

            if (existingManagementFee.ProfitShareEscalatorEnabled != newManagementFee.ProfitShareEscalatorEnabled)
            {
                model.bs_ProfitShareEscalatorEnabled = newManagementFee.ProfitShareEscalatorEnabled;
                changedField = true;
            }

            if (existingManagementFee.ProfitShareEscalatorMonth != newManagementFee.ProfitShareEscalatorMonth)
            {
                model.bs_ProfitShareEscalatorMonth = MapEscalatorMonthVoToModel(newManagementFee.ProfitShareEscalatorMonth);
                changedField = true;
            }

            if (existingManagementFee.ProfitShareEscalatorType != newManagementFee.ProfitShareEscalatorType)
            {
                model.bs_ProfitShareEscalatorType = (bs_escalatortype?)newManagementFee.ProfitShareEscalatorType;
                changedField = true;
            }

            if (existingManagementFee.ProfitShareTierData != newManagementFee.ProfitShareTierData)
            {
                model.bs_ProfitShareTierData = JsonConvert.SerializeObject(newManagementFee.ProfitShareTierData);
                changedField = true;
            }

            if (existingManagementFee.ValidationThresholdAmount != newManagementFee.ValidationThresholdAmount)
            {
                model.bs_ValidationThresholdAmount = newManagementFee.ValidationThresholdAmount;
                changedField = true;
            }

            if (existingManagementFee.ValidationThresholdEnabled != newManagementFee.ValidationThresholdEnabled)
            {
                model.bs_ValidationThresholdEnabled = newManagementFee.ValidationThresholdEnabled;
                changedField = true;
            }

            if (existingManagementFee.ValidationThresholdType != newManagementFee.ValidationThresholdType)
            {
                model.bs_ValidationThresholdType = (bs_managementagreementvalidationtype?)newManagementFee.ValidationThresholdType;
                changedField = true;
            }

            //changes123

            if (existingManagementFee.NonGlBillableExpensesEnabled != newManagementFee.NonGlBillableExpensesEnabled)
            {
                model.bs_NonGLBillableExpensesEnabled = newManagementFee.NonGlBillableExpensesEnabled;
                changedField = true;
            }

            return changedField ? model : null;
        }

        private static bs_LaborHourJob? JobRateVoToModel(JobRateVo existingJob, JobRateVo newJob)
        {
            var model = new bs_LaborHourJob();
            model.bs_LaborHourJobId = newJob.Id;

            var changedField = false;
            if (existingJob.DisplayName != newJob.DisplayName)
            {
                model.bs_DisplayName = newJob.DisplayName;
                changedField = true;
            }

            if (existingJob.Rate != newJob.Rate)
            {
                model.bs_Rate = newJob.Rate;
                changedField = true;
            }

            if (existingJob.OvertimeRate != newJob.OvertimeRate)
            {
                model.bs_OvertimeRate = newJob.OvertimeRate;
                changedField = true;
            }

            if (existingJob.Name != newJob.Name)
            {
                model.bs_Name = newJob.Name;
                changedField = true;
            }

            if (existingJob.Code != newJob.Code)
            {
                model.bs_Code = newJob.Code;
                changedField = true;
            }

            if (existingJob.JobCode != newJob.JobCode)
            {
                model.bs_JobCode = newJob.JobCode;
                changedField = true;
            }

            if (existingJob.InvoiceGroup != newJob.InvoiceGroup)
            {
                model.bs_InvoiceGroup = newJob.InvoiceGroup;
                changedField = true;
            }

            return changedField ? model : null;
        }

        [MapProperty(nameof(InvoiceGroupVo.Title), nameof(bs_InvoiceGroup.bs_Title))]
        [MapProperty(nameof(InvoiceGroupVo.Description), nameof(bs_InvoiceGroup.bs_Description))]
        [MapProperty(nameof(InvoiceGroupVo.GroupNumber), nameof(bs_InvoiceGroup.bs_GroupNumber))]
        [MapProperty(nameof(InvoiceGroupVo.BillingContactEmails), nameof(bs_InvoiceGroup.bs_BillingContactEmails))]
        [MapProperty(nameof(InvoiceGroupVo.VendorId), nameof(bs_InvoiceGroup.bs_VendorId))]
        [MapProperty(nameof(InvoiceGroupVo.SiteNumber), nameof(bs_InvoiceGroup.bs_SiteNumber))]
        [MapProperty(nameof(InvoiceGroupVo.CustomerName), nameof(bs_InvoiceGroup.bs_CustomerName))]
        private static partial bs_InvoiceGroup MapToInvoiceGroupModel(InvoiceGroupVo source);

        public static bs_InvoiceGroup InvoiceGroupVoToModel(InvoiceGroupVo source)
        {
            var model = MapToInvoiceGroupModel(source);
            if (source.Id != null && source.Id != Guid.Empty)
            {
                model.bs_InvoiceGroupId = source.Id;
            }
            return model;
        }

        [MapProperty(nameof(ServiceRateVo.Name), nameof(bs_FixedFeeService.bs_Name))]
        [MapProperty(nameof(ServiceRateVo.DisplayName), nameof(bs_FixedFeeService.bs_DisplayName))]
        [MapProperty(nameof(ServiceRateVo.Fee), nameof(bs_FixedFeeService.bs_Fee))]
        [MapProperty(nameof(ServiceRateVo.Code), nameof(bs_FixedFeeService.bs_Code))]
        [MapProperty(nameof(ServiceRateVo.InvoiceGroup), nameof(bs_FixedFeeService.bs_InvoiceGroup))]
        [MapProperty(nameof(ServiceRateVo.StartDate), nameof(bs_FixedFeeService.bs_StartDate))]
        [MapProperty(nameof(ServiceRateVo.EndDate), nameof(bs_FixedFeeService.bs_EndDate))]
        private static partial bs_FixedFeeService MapToFixedFeeServiceModel(ServiceRateVo source);

        public static bs_FixedFeeService FixedFeeServiceVoToModel(ServiceRateVo source)
        {
            var model = MapToFixedFeeServiceModel(source);
            if (source.Id != null && source.Id != Guid.Empty)
            {
                model.bs_FixedFeeServiceId = source.Id;
            }
            if (!model.bs_StartDate.HasValue)
            {
                model.bs_StartDate = DateTime.Now;
            }
            return model;
        }

        [MapProperty(nameof(ThresholdStructureVo.AccumulationType), nameof(bs_RevenueShareThreshold.bs_RevenueAccumulationType))]
        [MapProperty(nameof(ThresholdStructureVo.ValidationThresholdType), nameof(bs_RevenueShareThreshold.bs_ValidationThresholdType))]
        private static partial bs_RevenueShareThreshold MapToThresholdStructureModel(ThresholdStructureVo source);


        public static bs_RevenueShareThreshold RevenueShareThresholdVoToModel(ThresholdStructureVo source)
        {
            var model = MapToThresholdStructureModel(source);
            if (source.Id != null && source.Id != Guid.Empty)
            {
                model.bs_RevenueShareThresholdId = source.Id;
            }
            if (source.RevenueCodes != null)
            {
                model.bs_RevenueCodeData = JsonConvert.SerializeObject(source.RevenueCodes);
            }

            if (source.Tiers != null)
            {
                model.bs_TierData = JsonConvert.SerializeObject(source.Tiers);
            }

            if (source.InvoiceGroup != null)
            {
                model.bs_InvoiceGroup = source.InvoiceGroup;
            }

            if (source.ValidationThresholdAmount != null)
            {
                model.bs_ValidationThresholdAmount = source.ValidationThresholdAmount;
            }
            return model;
        }

        [MapProperty(nameof(BellServiceVo.InvoiceGroup), nameof(bs_BellService.bs_InvoiceGroup))]
        private static partial bs_BellService MapToBellServiceModel(BellServiceVo source);

        public static bs_BellService BellServiceVoToModel(BellServiceVo source)
        {
            var model = MapToBellServiceModel(source);
            if (source.Id != null && source.Id != Guid.Empty)
            {
                model.bs_BellServiceId = source.Id;
            }

            return model;
        }

        [MapProperty(nameof(MidMonthVo.Amount), nameof(bs_MidMonthAdvance.bs_Amount))]
        [MapProperty(nameof(MidMonthVo.LineTitle), nameof(bs_MidMonthAdvance.bs_LineTitle))]
        [MapProperty(nameof(MidMonthVo.InvoiceGroup), nameof(bs_MidMonthAdvance.bs_InvoiceGroup))]
        private static partial bs_MidMonthAdvance MapToMidMonthModel(MidMonthVo source);

        public static bs_MidMonthAdvance MidMonthAdvanceVoToModel(MidMonthVo source)
        {
            var model = MapToMidMonthModel(source);
            if (source.Id != null && source.Id != Guid.Empty)
            {
                model.bs_MidMonthAdvanceId = source.Id;
            }
            return model;
        }

        [MapProperty(nameof(BillableAccountDataVo.PayrollAccountsInvoiceGroup), nameof(bs_BillableAccount.bs_PayrollAccountsInvoiceGroup))]
        [MapProperty(nameof(BillableAccountDataVo.PayrollAccountsData), nameof(bs_BillableAccount.bs_PayrollAccountsData))]
        [MapProperty(nameof(BillableAccountDataVo.PayrollAccountsLineTitle), nameof(bs_BillableAccount.bs_PayrollAccountsLineTitle))]
        [MapProperty(nameof(BillableAccountDataVo.PayrollTaxesBillingType), nameof(bs_BillableAccount.bs_PayrollTaxesBillingType))]
        [MapProperty(nameof(BillableAccountDataVo.PayrollTaxesEnabled), nameof(bs_BillableAccount.bs_PayrollTaxesEnabled))]
        [MapProperty(nameof(BillableAccountDataVo.PayrollTaxesPercentage), nameof(bs_BillableAccount.bs_PayrollTaxesPercentage))]
        [MapProperty(nameof(BillableAccountDataVo.PayrollTaxesLineTitle), nameof(bs_BillableAccount.bs_PayrollTaxesLineTitle))]
        [MapProperty(nameof(BillableAccountDataVo.PayrollSupportPayrollType), nameof(bs_BillableAccount.bs_PayrollSupportPayrollType))]
        [MapProperty(nameof(BillableAccountDataVo.PayrollSupportLineTitle), nameof(bs_BillableAccount.bs_PayrollSupportLineTitle))]
        [MapProperty(nameof(BillableAccountDataVo.PayrollSupportEnabled), nameof(bs_BillableAccount.bs_PayrollSupportEnabled))]
        [MapProperty(nameof(BillableAccountDataVo.PayrollSupportBillingType), nameof(bs_BillableAccount.bs_PayrollSupportBillingType))]
        [MapProperty(nameof(BillableAccountDataVo.PayrollSupportAmount), nameof(bs_BillableAccount.bs_PayrollSupportAmount))]
        [MapProperty(nameof(BillableAccountDataVo.PayrollExpenseAccountsData), nameof(bs_BillableAccount.bs_ExpenseAccountsData))]
        [MapProperty(nameof(BillableAccountDataVo.PayrollExpenseAccountsInvoiceGroup), nameof(bs_BillableAccount.bs_ExpenseAccountsInvoiceGroup))]
        [MapProperty(nameof(BillableAccountDataVo.PayrollExpenseAccountsLineTitle), nameof(bs_BillableAccount.bs_ExpenseAccountsLineTitle))]
        [MapProperty(nameof(BillableAccountDataVo.AdditionalPayrollAmount), nameof(bs_BillableAccount.bs_AdditionalPayrollAmount))]
        
        private static partial bs_BillableAccount MapToBillableAccountModel(BillableAccountDataVo source);

        public static bs_BillableAccount BillableAccountVoToModel(BillableAccountDataVo source)
        {
            var model = MapToBillableAccountModel(source);
            if (source.Id != null && source.Id != Guid.Empty)
            {
                model.bs_BillableAccountId = source.Id;
            }
            return model;
        }

        [MapProperty(nameof(ManagementFeeVo.ManagementAgreementType), nameof(bs_ManagementAgreement.Fields.bs_ManagementAgreementType))]
        [MapProperty(nameof(ManagementFeeVo.ManagementFeeEscalatorEnabled), nameof(bs_ManagementAgreement.Fields.bs_ManagementFeeEscalatorEnabled))]
        [MapProperty(nameof(ManagementFeeVo.ManagementFeeEscalatorMonth), nameof(bs_ManagementAgreement.Fields.bs_ManagementFeeEscalatorMonth))]
        [MapProperty(nameof(ManagementFeeVo.ManagementFeeEscalatorType), nameof(bs_ManagementAgreement.Fields.bs_ManagementFeeEscalatorType))]
        [MapProperty(nameof(ManagementFeeVo.ManagementFeeEscalatorValue), nameof(bs_ManagementAgreement.Fields.bs_ManagementFeeEscalatorValue))]
        [MapProperty(nameof(ManagementFeeVo.FixedFeeAmount), nameof(bs_ManagementAgreement.Fields.bs_FixedFeeAmount))]
        [MapProperty(nameof(ManagementFeeVo.LaborHourJobCode), nameof(bs_ManagementAgreement.Fields.bs_PerLaborHourJobCode))]
        [MapProperty(nameof(ManagementFeeVo.PerLaborHourJobCodeData), nameof(bs_ManagementAgreement.Fields.bs_PerLaborHourJobCodeData))]
        [MapProperty(nameof(ManagementFeeVo.LaborHourRate), nameof(bs_ManagementAgreement.Fields.bs_PerLaborHourRate))]
        [MapProperty(nameof(ManagementFeeVo.LaborHourOvertimeRate), nameof(bs_ManagementAgreement.Fields.bs_PerLaborHourOvertimeRate))]
        [MapProperty(nameof(ManagementFeeVo.RevenuePercentageAmount), nameof(bs_ManagementAgreement.Fields.bs_RevenuePercentageAmount))]
        [MapProperty(nameof(ManagementFeeVo.InvoiceGroup), nameof(bs_ManagementAgreement.bs_InvoiceGroup))]
        [MapProperty(nameof(ManagementFeeVo.InsuranceEnabled), nameof(bs_ManagementAgreement.bs_InsuranceEnabled))]
        [MapProperty(nameof(ManagementFeeVo.InsuranceLineTitle), nameof(bs_ManagementAgreement.bs_InsuranceLineTitle))]
        [MapProperty(nameof(ManagementFeeVo.InsuranceType), nameof(bs_ManagementAgreement.bs_InsuranceType))]
        [MapProperty(nameof(ManagementFeeVo.InsuranceAdditionalPercentage), nameof(bs_ManagementAgreement.bs_InsuranceAdditionalPercentage))]
        [MapProperty(nameof(ManagementFeeVo.InsuranceFixedFeeAmount), nameof(bs_ManagementAgreement.bs_InsuranceFixedFeeAmount))]
        [MapProperty(nameof(ManagementFeeVo.ClaimsCapAmount), nameof(bs_ManagementAgreement.bs_ClaimsCapAmount))]
        [MapProperty(nameof(ManagementFeeVo.ClaimsEnabled), nameof(bs_ManagementAgreement.bs_ClaimsEnabled))]
        [MapProperty(nameof(ManagementFeeVo.ClaimsLineTitle), nameof(bs_ManagementAgreement.bs_ClaimsLineTitle))]
        [MapProperty(nameof(ManagementFeeVo.ClaimsType), nameof(bs_ManagementAgreement.bs_ClaimsType))]
        [MapProperty(nameof(ManagementFeeVo.ProfitShareAccumulationType), nameof(bs_ManagementAgreement.bs_ProfitShareAccumulationType))]
        [MapProperty(nameof(ManagementFeeVo.ProfitShareEnabled), nameof(bs_ManagementAgreement.bs_ProfitShareEnabled))]
        [MapProperty(nameof(ManagementFeeVo.ProfitShareEscalatorEnabled), nameof(bs_ManagementAgreement.bs_ProfitShareEscalatorEnabled))]
        [MapProperty(nameof(ManagementFeeVo.ProfitShareEscalatorMonth), nameof(bs_ManagementAgreement.bs_ProfitShareEscalatorMonth))]
        [MapProperty(nameof(ManagementFeeVo.ProfitShareEscalatorType), nameof(bs_ManagementAgreement.bs_ProfitShareEscalatorType))]
        [MapProperty(nameof(ManagementFeeVo.ValidationThresholdAmount), nameof(bs_ManagementAgreement.bs_ValidationThresholdAmount))]
        [MapProperty(nameof(ManagementFeeVo.ValidationThresholdEnabled), nameof(bs_ManagementAgreement.bs_ValidationThresholdEnabled))]
        [MapProperty(nameof(ManagementFeeVo.ValidationThresholdType), nameof(bs_ManagementAgreement.bs_ValidationThresholdType))]
        [MapProperty(nameof(ManagementFeeVo.NonGlBillableExpensesEnabled), nameof(bs_ManagementAgreement.bs_NonGLBillableExpensesEnabled))]

        private static partial bs_ManagementAgreement MapToManagementAgreementModel(ManagementFeeVo source);

        public static bs_ManagementAgreement ManagementAgreementVoToModel(ManagementFeeVo source)
        {
            var model = MapToManagementAgreementModel(source);
            if (source.Id != null && source.Id != Guid.Empty)
            {
                model.bs_ManagementAgreementId = source.Id;
            }
            if (source.PerLaborHourJobCodeData != null) 
            {
                model.bs_PerLaborHourJobCodeData = JsonConvert.SerializeObject(source.PerLaborHourJobCodeData);
            }
            if (source.ProfitShareTierData != null)
            {
                model.bs_ProfitShareTierData = JsonConvert.SerializeObject(source.ProfitShareTierData);
            }
            
            return model;
        }

        [MapProperty(nameof(DepositDataVo.TowneParkResponsibleForParkingTax), nameof(bs_DepositedRevenue.bs_TowneParkResponsibleForParkingTax))]
        [MapProperty(nameof(DepositDataVo.DepositedRevenueEnabled), nameof(bs_DepositedRevenue.bs_DepositedRevenueEnabled))]
        [MapProperty(nameof(DepositDataVo.InvoiceGroup), nameof(bs_DepositedRevenue.bs_InvoiceGroup))]
        private static partial bs_DepositedRevenue MapToDepositedRevenueModel(DepositDataVo source);

        public static bs_DepositedRevenue DepositedRevenueVoToModel(DepositDataVo source)
        {
            var model = MapToDepositedRevenueModel(source);
            if (source.Id != null && source.Id != Guid.Empty)
            {
                model.bs_DepositedRevenueId = source.Id;
            }

            return model;
        }

        [MapProperty(nameof(JobRateVo.Name), nameof(bs_LaborHourJob.bs_Name))]
        [MapProperty(nameof(JobRateVo.DisplayName), nameof(bs_LaborHourJob.bs_DisplayName))]
        [MapProperty(nameof(JobRateVo.Rate), nameof(bs_LaborHourJob.bs_Rate))]
        [MapProperty(nameof(JobRateVo.OvertimeRate), nameof(bs_LaborHourJob.bs_OvertimeRate))]
        [MapProperty(nameof(JobRateVo.InvoiceGroup), nameof(bs_LaborHourJob.bs_InvoiceGroup))]
        [MapProperty(nameof(JobRateVo.Code), nameof(bs_LaborHourJob.bs_Code))]
        [MapProperty(nameof(JobRateVo.JobCode), nameof(bs_LaborHourJob.bs_JobCode))]
        [MapProperty(nameof(JobRateVo.StartDate), nameof(bs_LaborHourJob.bs_StartDate))]
        [MapProperty(nameof(JobRateVo.EndDate), nameof(bs_LaborHourJob.bs_EndDate))]
        private static partial bs_LaborHourJob MapToLaborHourJobModel(JobRateVo source);

        public static bs_LaborHourJob LaborHourJobVoToModel(JobRateVo source)
        {
            var model = MapToLaborHourJobModel(source);
            if (source.Id != null && source.Id != Guid.Empty)
            {
                model.bs_LaborHourJobId = source.Id;
            }
            if (!model.bs_StartDate.HasValue)
            {
                model.bs_StartDate = DateTime.Now;
            }
            return model;
        }

        [MapProperty(nameof(NonGlBillableExpenseVo.NonGLExpenseType), nameof(bs_NonGLExpense.bs_NonGLExpenseType))]
        [MapProperty(nameof(NonGlBillableExpenseVo.ExpensePayrollType), nameof(bs_NonGLExpense.bs_ExpensePayrollType))]
        [MapProperty(nameof(NonGlBillableExpenseVo.ExpenseAmount), nameof(bs_NonGLExpense.bs_ExpenseAmount))]
        [MapProperty(nameof(NonGlBillableExpenseVo.ExpenseTitle), nameof(bs_NonGLExpense.bs_ExpenseTitle))]
        [MapProperty(nameof(NonGlBillableExpenseVo.FinalPeriodBilled), nameof(bs_NonGLExpense.bs_FinalPeriodBilled))]
        [MapProperty(nameof(NonGlBillableExpenseVo.SequenceNumber), nameof(bs_NonGLExpense.bs_SequenceNumber))]

        private static partial bs_NonGLExpense MapToNonGLExpenseModel(NonGlBillableExpenseVo source);

        public static bs_NonGLExpense NonGLExpenseVoToModel(NonGlBillableExpenseVo source)
        {
            var model = MapToNonGLExpenseModel(source);
            if (source.Id != null && source.Id != Guid.Empty)
            {
                model.bs_NonGLExpenseId = source.Id;
            }
            return model;
        }


    }
}