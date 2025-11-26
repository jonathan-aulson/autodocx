using api.Adapters.Mappers;
using api.Data;
using api.Models.Vo;
using api.Models.Vo.Enum;
using api.Services.Impl;
using api.Usecases;
using FluentAssertions;
using NSubstitute;
using TownePark;
using Xunit;

namespace BackendTests.Services
{
    public class ContractServiceTest
    {
        private readonly IContractRepository _contractRepository;
        private readonly IValidateAndPopulateGlCodes _validateAndPopulateGlCodes;
        private readonly ContractService _contractService;

        public ContractServiceTest()
        {
            _contractRepository = Substitute.For<IContractRepository>();
            _validateAndPopulateGlCodes = Substitute.For<IValidateAndPopulateGlCodes>();
            _contractService = new ContractService(_contractRepository, _validateAndPopulateGlCodes);
        }

        [Fact]
        public void GetContractDetail_ShouldCallContractRepositoryAndReturnAdaptedResponse()
        {
            var customerSiteId = Guid.NewGuid();
            var invoiceGroupId = Guid.NewGuid();
            var valetId = Guid.NewGuid();
            var conciergeId = Guid.NewGuid();
            var cashierId = Guid.NewGuid();
            var jobId = Guid.NewGuid();
            var revShareId = Guid.NewGuid();
            var bellServiceId = Guid.NewGuid();
            var midMonthId = Guid.NewGuid();
            var depositedRevenueId = Guid.NewGuid();
            var billableAccountId = Guid.NewGuid();
            var managementFeeId = Guid.NewGuid();

            var contractModel = new bs_Contract
            {
                bs_ContractId = customerSiteId,
                bs_BillingType = bs_billingtypechoices.Advanced,
                bs_ContractType = new [] {bs_contracttypechoices.FixedFee, bs_contracttypechoices.PerOccupiedRoom},
                bs_IncrementAmount = 1.05m,
                bs_IncrementMonth = 1,
                bs_HoursBackupReport = true,
                bs_OccupiedRoomRate = 10.0m,
                bs_InvoiceGroup_Contract = new List<bs_InvoiceGroup>
                {
                    new bs_InvoiceGroup()
                    {
                        bs_InvoiceGroupId = invoiceGroupId,
                        bs_Title = "Example Title",
                        bs_Description = "Example Desc",
                        bs_GroupNumber = 1
                    }
                },
                bs_FixedFeeService_Contract = new List<bs_FixedFeeService>
                    {
                        new bs_FixedFeeService
                        {
                            bs_FixedFeeServiceId = valetId,
                            bs_Name = "Valet",
                            bs_Fee = 1000
                        },
                        new bs_FixedFeeService
                        {
                            bs_FixedFeeServiceId = conciergeId,
                            bs_Name = "Concierge",
                            bs_Fee = 500
                        },
                        new bs_FixedFeeService
                        {
                            bs_FixedFeeServiceId = cashierId,
                            bs_Name = "Cashier",
                            bs_Fee = 2000
                        }
                    },
                bs_LaborHourJob_Contract = new List<bs_LaborHourJob>
                {
                    new bs_LaborHourJob
                    {
                        bs_LaborHourJobId = jobId,
                        bs_Name = "Director",
                        bs_Rate = 1700,
                        bs_OvertimeRate = 1800,
                        bs_JobCode = "DIR",
                        bs_InvoiceGroup = 1
                    }
                },
                bs_RevenueShareThreshold_Contract = new List<bs_RevenueShareThreshold>
                {
                    new bs_RevenueShareThreshold
                    {
                        bs_RevenueShareThresholdId = revShareId,
                        bs_Name = "name",
                        bs_RevenueCodeData = "[\"123\",\"123\"]",
                        bs_RevenueAccumulationType = bs_revenueaccumulationtype.Monthly,
                        bs_TierData = "[{\"SharePercentage\":\"11\",\"Amount\":\"1000\"},{\"SharePercentage\":\"10\",\"Amount\":\"2000\"},{\"SharePercentage\":\"15\",\"Amount\":null}]",
                        bs_ValidationThresholdType = bs_validationthresholdtype.VehicleCount,
                        bs_ValidationThresholdAmount = 50,
                        bs_InvoiceGroup = 1
                    }
                },
                bs_BellService_bs_Contract = new List<bs_BellService>
                {
                    new bs_BellService
                    {
                        bs_BellServiceId = bellServiceId,
                        bs_InvoiceGroup = 1
                    }
                },
                bs_MidMonthAdvance_bs_Contract = new List<bs_MidMonthAdvance>
                {
                    new bs_MidMonthAdvance
                    {
                        bs_MidMonthAdvanceId = midMonthId,
                        bs_InvoiceGroup = 1,
                        bs_LineTitle = bs_lineitemtitle.MidMonthBilling,
                        bs_Amount = 1000.00m
                    }
                },
                bs_DepositedRevenue_Contract = new List<bs_DepositedRevenue>
                {
                    new bs_DepositedRevenue
                    {
                        bs_DepositedRevenueId = depositedRevenueId,
                        bs_InvoiceGroup = 1,
                        bs_TowneParkResponsibleForParkingTax = true
                    }
                },
                bs_BillableAccount_Contract = new List<bs_BillableAccount>
                {
                    new bs_BillableAccount
                    {
                        bs_BillableAccountId = billableAccountId,
                        bs_PayrollAccountsInvoiceGroup = 1,
                        bs_PayrollAccountsData = "[\r\n    {\r\n        \"code\": \"6010\",\r\n        \"title\": \"Salaries - Personal Leave & Sick Pay - Hourly\",\r\n        \"isEnabled\": false\r\n    },\r\n    {\r\n        \"code\": \"6014\",\r\n        \"title\": \"Salaries - Other\",\r\n        \"isEnabled\": false\r\n    }]",
                        bs_PayrollAccountsLineTitle = "Test Payroll Line Title",
                        bs_PayrollTaxesPercentage = 5.5m,
                        bs_PayrollTaxesLineTitle = "Test Payroll Taxes Line Title",
                        bs_PayrollTaxesEnabled = true,
                        bs_PayrollTaxesBillingType = bs_ptebbillingtype.Percentage
                    }
                },
                bs_ManagementAgreement_Contract = new List<bs_ManagementAgreement>
                {
                    new bs_ManagementAgreement
                    {
                        bs_ManagementAgreementId = managementFeeId,
                        bs_InvoiceGroup = 1,
                        bs_FixedFeeAmount = 1000.00m,
                        bs_RevenuePercentageAmount = 5.5m,
                        bs_ManagementAgreementType = bs_managementagreementtype.FixedFee,
                    }
                },
                bs_SupportingReports = new[] { bs_supportingreporttypes.MixOfSales }
            };
            _contractRepository.GetContractByCustomerSite(customerSiteId).Returns(contractModel);

            var expectedContractVo = new ContractDetailVo
            {
                Id = customerSiteId,
                BillingType = BillingType.Advanced,
                IncrementAmount = 1.05m,
                IncrementMonth = Month.January,
                InvoiceGrouping =
                {
                    Enabled = false,
                    InvoiceGroups = new List<ContractDetailVo.InvoiceGroupVo> {
                        new ContractDetailVo.InvoiceGroupVo()
                        {
                            Id = invoiceGroupId,
                            Title = "Example Title",
                            Description = "Example Desc",
                            GroupNumber = 1
                        }
                    }
                },
                PerOccupiedRoom = new ContractDetailVo.PerOccupiedRoomVo
                {
                    Enabled = true,
                    RoomRate = 10.00m
                },
                FixedFee = new ContractDetailVo.FixedFeeVo
                {
                    Enabled = true,
                    ServiceRates = new List<ContractDetailVo.ServiceRateVo>
                    {
                        new ContractDetailVo.ServiceRateVo
                        {
                            Id = valetId,
                            Name = "Valet",
                            Fee = 1000
                        },
                        new ContractDetailVo.ServiceRateVo
                        {
                            Id = conciergeId,
                            Name = "Concierge",
                            Fee = 500
                        },
                        new ContractDetailVo.ServiceRateVo
                        {
                            Id = cashierId,
                            Name = "Cashier",
                            Fee = 2000
                        }
                    }
                },
                PerLaborHour =
                {
                    Enabled = false,
                    HoursBackupReport = true,
                    JobRates = new List<ContractDetailVo.JobRateVo>
                    {
                        new ContractDetailVo.JobRateVo
                        {
                            Id = jobId,
                            Name = "Director",
                            Rate = 1700,
                            OvertimeRate = 1800,
                            JobCode = "DIR",
                            InvoiceGroup = 1
                        }
                    }
                },
                RevenueShare =
                {
                    Enabled = false,
                    ThresholdStructures = new List<ContractDetailVo.ThresholdStructureVo>
                    {
                        new ContractDetailVo.ThresholdStructureVo
                        {
                            Id = revShareId,
                            RevenueCodes = new List<string>{"123", "123"},
                            AccumulationType = ContractDetailVo.AccumulationType.Monthly,
                            Tiers = new List<ContractDetailVo.TierVo>
                            {
                                new ContractDetailVo.TierVo
                                {
                                    SharePercentage = 11,
                                    Amount = 1000
                                },
                                new ContractDetailVo.TierVo
                                {
                                    SharePercentage = 10,
                                    Amount = 2000
                                },
                                new ContractDetailVo.TierVo
                                {
                                    SharePercentage = 15,
                                    Amount = null
                                }
                            },
                            ValidationThresholdType = ContractDetailVo.ValidationThresholdType.VehicleCount,
                            ValidationThresholdAmount = 50,
                            InvoiceGroup = 1
                        }
                    }
                },
                BellServiceFee = new ContractDetailVo.BellServiceFeeVo
                {
                    Enabled = false,
                    BellServices = new List<ContractDetailVo.BellServiceVo>
                    {
                        new ContractDetailVo.BellServiceVo
                        {
                            Id = bellServiceId,
                            InvoiceGroup = 1
                        }
                    }
                },
                MidMonthAdvance = new ContractDetailVo.MidMonthAdvanceVo
                {
                    Enabled = false,
                    MidMonthAdvances = new List<ContractDetailVo.MidMonthVo>
                    {
                        new ContractDetailVo.MidMonthVo
                        {
                            Id = midMonthId,
                            InvoiceGroup = 1,
                            LineTitle = ContractDetailVo.LineTitleType.MidMonthBilling,
                            Amount = 1000.00m
                        }
                    }
                },
                DepositedRevenue = new ContractDetailVo.DepositedRevenueVo
                {
                    Enabled = false,
                    DepositData = new List<ContractDetailVo.DepositDataVo>
                    {
                        new ContractDetailVo.DepositDataVo
                        {
                            Id = depositedRevenueId,
                            InvoiceGroup = 1,
                            TowneParkResponsibleForParkingTax = true
                        }
                    }
                },
                BillableAccount = new ContractDetailVo.BillableAccountVo
                {
                    Enabled = false,
                    BillableAccountsData = new List<ContractDetailVo.BillableAccountDataVo>
                    {
                        new ContractDetailVo.BillableAccountDataVo
                        {
                            Id = billableAccountId,
                            PayrollAccountsData = "[\r\n    {\r\n        \"code\": \"6010\",\r\n        \"title\": \"Salaries - Personal Leave & Sick Pay - Hourly\",\r\n        \"isEnabled\": false\r\n    },\r\n    {\r\n        \"code\": \"6014\",\r\n        \"title\": \"Salaries - Other\",\r\n        \"isEnabled\": false\r\n    }]",
                            PayrollAccountsInvoiceGroup = 1,
                            PayrollAccountsLineTitle = "Test Payroll Line Title",
                            PayrollTaxesLineTitle = "Test Payroll Taxes Line Title",
                            PayrollTaxesPercentage = 5.5m,
                            PayrollTaxesEnabled = true,
                            PayrollTaxesBillingType = ContractDetailVo.PayrollTaxesBillingType.Percentage
                        }
                    }
                },
                ManagementAgreement = new ContractDetailVo.ManagementAgreementVo
                {
                    Enabled = false,
                    ManagementFees = new List<ContractDetailVo.ManagementFeeVo>
                    {
                        new ContractDetailVo.ManagementFeeVo
                        {
                            Id = managementFeeId,
                            InvoiceGroup = 1,
                            FixedFeeAmount = 1000.00m,
                            RevenuePercentageAmount = 5.5m,
                            PerLaborHourJobCodeData = new List<ContractDetailVo.JobCodeVo>(),
                            ManagementAgreementType = ContractDetailVo.ManagementAgreementType.FixedFee,
                            ProfitShareTierData = new List<ContractDetailVo.TierVo>(),
                            NonGlBillableExpenses = new List<ContractDetailVo.NonGlBillableExpenseVo>()
                        }
                    }
                },
                SupportingReports = new List<SupportingReportType> { SupportingReportType.MixOfSales }
            };

            var result = _contractService.GetContractDetail(customerSiteId);

            result.Should().BeEquivalentTo(expectedContractVo);
        }

        [Fact]
        public void UpdateContract_ShouldCallContractRepositoryWithCorrectParameters()
        {
            var contractId = Guid.NewGuid();

            var existingContract = new bs_Contract
            {
                bs_PurchaseOrder = "PO-123",
                bs_PaymentTerms = "Due by end of the month.",
                bs_BillingType = bs_billingtypechoices.Advanced,
                bs_IncrementAmount = 1.05m,
                bs_IncrementMonth = (int)Month.January,
                bs_ConsumerPriceIndex = true,
                bs_Notes = "Notes",
                bs_InvoiceGroup_Contract = new List<bs_InvoiceGroup>
                {
                    new bs_InvoiceGroup()
                    {
                        bs_InvoiceGroupId = Guid.NewGuid(),
                        bs_Title = "Example Title",
                        bs_Description = "Example Desc",
                        bs_GroupNumber = 1
                    }
                },
                bs_FixedFeeService_Contract = new List<bs_FixedFeeService>
                {
                    new bs_FixedFeeService
                    {
                        bs_FixedFeeServiceId = Guid.NewGuid(),
                        bs_Name = "Valet",
                        bs_Fee = 1000
                    },
                    new bs_FixedFeeService
                    {
                        bs_FixedFeeServiceId = Guid.NewGuid(),
                        bs_Name = "Concierge",
                        bs_Fee = 500
                    },
                    new bs_FixedFeeService
                    {
                        bs_FixedFeeServiceId = Guid.NewGuid(),
                        bs_Name = "Cashier",
                        bs_Fee = 2000
                    }
                },
                bs_LaborHourJob_Contract = new List<bs_LaborHourJob>
                {
                    new bs_LaborHourJob
                    {
                        bs_LaborHourJobId = Guid.NewGuid(),
                        bs_Name = "Director",
                        bs_Rate = 1700,
                        bs_OvertimeRate = 1800,
                        bs_JobCode = "DIR",
                        bs_InvoiceGroup = 1
                    }
                },
                bs_RevenueShareThreshold_Contract = new List<bs_RevenueShareThreshold>
                {
                    new bs_RevenueShareThreshold
                    {
                        bs_RevenueShareThresholdId = Guid.NewGuid(),
                        bs_Name = "name",
                        bs_RevenueCodeData = "[\"123\",\"123\"]",
                        bs_RevenueAccumulationType = bs_revenueaccumulationtype.Monthly,
                        bs_TierData = "[{\"SharePercentage\":\"11\",\"Amount\":\"1000\"},{\"SharePercentage\":\"10\",\"Amount\":\"2000\"},{\"SharePercentage\":\"15\",\"Amount\":null}]",
                        bs_ValidationThresholdType = bs_validationthresholdtype.VehicleCount,
                        bs_ValidationThresholdAmount = 50,
                        bs_InvoiceGroup = 1
                    }
                },
                bs_BellService_bs_Contract = new List<bs_BellService>
                {
                    new bs_BellService
                    {
                        bs_InvoiceGroup = 1,
                    }
                },
                bs_MidMonthAdvance_bs_Contract = new List<bs_MidMonthAdvance>
                {
                    new bs_MidMonthAdvance
                    {
                        bs_InvoiceGroup = 1,
                        bs_LineTitle = bs_lineitemtitle.MidMonthBilling,
                        bs_Amount = 1000.00m
                    }
                },
                bs_DepositedRevenue_Contract = new List<bs_DepositedRevenue>
                {
                    new bs_DepositedRevenue
                    {
                        bs_InvoiceGroup = 1,
                        bs_TowneParkResponsibleForParkingTax = true
                    }
                },
                bs_BillableAccount_Contract = new List<bs_BillableAccount>
                {
                    new bs_BillableAccount
                    {
                        bs_PayrollAccountsData = "[\r\n    {\r\n        \"code\": \"6010\",\r\n        \"title\": \"Salaries - Personal Leave & Sick Pay - Hourly\",\r\n        \"isEnabled\": false\r\n    },\r\n    {\r\n        \"code\": \"6014\",\r\n        \"title\": \"Salaries - Other\",\r\n        \"isEnabled\": false\r\n    }]",
                        bs_PayrollAccountsLineTitle = "Test Payroll Line Title",
                        bs_PayrollAccountsInvoiceGroup = 1
                    }
                },
                bs_ManagementAgreement_Contract = new List<bs_ManagementAgreement>
                {
                    new bs_ManagementAgreement
                    {
                        bs_InvoiceGroup = 1,
                        bs_FixedFeeAmount = 1000.00m,
                        bs_RevenuePercentageAmount = 5.5m,
                        bs_ManagementAgreementType = bs_managementagreementtype.FixedFee
                    }
                },
                bs_SupportingReports = new[] { bs_supportingreporttypes.MixOfSales }
            };

            var existingContractVo = ContractMapper.ContractModelToVo(existingContract);

            var updateContractVo = new ContractDetailVo()
            {
                PurchaseOrder = "PO-123",
                PaymentTerms = "Due by 1st of the month.",
                BillingType = BillingType.Advanced,
                IncrementAmount = 1.10m,
                IncrementMonth = Month.June,
                ConsumerPriceIndex = true,
                Notes = "Updated notes",
                InvoiceGrouping =
                {
                    Enabled = false,
                    InvoiceGroups = new List<ContractDetailVo.InvoiceGroupVo> {
                        new ContractDetailVo.InvoiceGroupVo()
                        {
                            Id = Guid.NewGuid(),
                            Title = "Example Title",
                            Description = "Example Desc",
                            GroupNumber = 1
                        }
                    }
                },
                FixedFee = new ContractDetailVo.FixedFeeVo
                {
                    Enabled = true,
                    ServiceRates = new List<ContractDetailVo.ServiceRateVo>
                {
                    new ContractDetailVo.ServiceRateVo
                    {
                        Id = Guid.NewGuid(),
                        Name = "Cashier",
                        Fee = 2000
                    }
                }
                },
                PerLaborHour = new ContractDetailVo.PerLaborHourVo
                {
                    Enabled = true,
                    HoursBackupReport = true,
                    JobRates = new List<ContractDetailVo.JobRateVo>
                    {
                        new ContractDetailVo.JobRateVo
                        {
                            Id = Guid.NewGuid(),
                            Name = "Director",
                            Rate = 35.00m,
                            OvertimeRate = 40.00m,
                            JobCode = "DIR",
                            InvoiceGroup = 1
                        }
                    }
                },
                RevenueShare = new ContractDetailVo.RevenueShareVo
                {
                    Enabled = true,
                    ThresholdStructures = new List<ContractDetailVo.ThresholdStructureVo>
                    {
                        new ContractDetailVo.ThresholdStructureVo
                        {
                            Id = Guid.NewGuid(),
                            RevenueCodes = new List<string>{"123", "123"},
                            AccumulationType = ContractDetailVo.AccumulationType.Monthly,
                            Tiers = new List<ContractDetailVo.TierVo>
                            {
                                new ContractDetailVo.TierVo
                                {
                                    SharePercentage = 11,
                                    Amount = 1000
                                },
                                new ContractDetailVo.TierVo
                                {
                                    SharePercentage = 10,
                                    Amount = 2000
                                },
                                new ContractDetailVo.TierVo
                                {
                                    SharePercentage = 15,
                                    Amount = null
                                }
                            }
                        }
                    }
                },
                BellServiceFee = new ContractDetailVo.BellServiceFeeVo
                {
                    Enabled = true,
                    BellServices = new List<ContractDetailVo.BellServiceVo>
                    {
                        new ContractDetailVo.BellServiceVo
                        {
                            Id = Guid.NewGuid(),
                            InvoiceGroup = 1
                        }
                    }
                },
                MidMonthAdvance = new ContractDetailVo.MidMonthAdvanceVo
                {
                    Enabled = true,
                    MidMonthAdvances = new List<ContractDetailVo.MidMonthVo>
                    {
                        new ContractDetailVo.MidMonthVo
                        {
                            Id = Guid.NewGuid(),
                            InvoiceGroup = 1,
                            LineTitle = ContractDetailVo.LineTitleType.MidMonthBilling,
                            Amount = 1000.00m
                        }
                    }
                },
                DepositedRevenue = new ContractDetailVo.DepositedRevenueVo
                {
                    Enabled = true,
                    DepositData = new List<ContractDetailVo.DepositDataVo>
                    {
                        new ContractDetailVo.DepositDataVo
                        {
                            Id = Guid.NewGuid(),
                            InvoiceGroup = 1,
                            TowneParkResponsibleForParkingTax = true
                        }
                    }
                },
                BillableAccount = new ContractDetailVo.BillableAccountVo
                {
                    Enabled = true,
                    BillableAccountsData = new List<ContractDetailVo.BillableAccountDataVo>
                    {
                        new ContractDetailVo.BillableAccountDataVo
                        {
                            Id = Guid.NewGuid(),
                            PayrollAccountsData = "[\r\n    {\r\n        \"code\": \"6010\",\r\n        \"title\": \"Salaries - Personal Leave & Sick Pay - Hourly\",\r\n        \"isEnabled\": false\r\n    },\r\n    {\r\n        \"code\": \"6014\",\r\n        \"title\": \"Salaries - Other\",\r\n        \"isEnabled\": false\r\n    }]",
                            PayrollAccountsInvoiceGroup = 1,
                            PayrollAccountsLineTitle = "Test Payroll Line Title"
                        }
                    }
                },
                ManagementAgreement = new ContractDetailVo.ManagementAgreementVo
                {
                    Enabled = true,
                    ManagementFees = new List<ContractDetailVo.ManagementFeeVo>
                    {
                        new ContractDetailVo.ManagementFeeVo
                        {
                            Id = Guid.NewGuid(),
                            InvoiceGroup = 1,
                            FixedFeeAmount = 1000.00m,
                            RevenuePercentageAmount = 5.5m,
                            ManagementAgreementType = ContractDetailVo.ManagementAgreementType.FixedFee
                        }
                    }
                },
                SupportingReports = new List<SupportingReportType> { SupportingReportType.MixOfSales }
            }; 

            // Mocking the repository to return an existing contract
            _contractRepository.GetContract(contractId).Returns(existingContract);
            _contractRepository.UpdateContractRelatedEntities(Arg.Any<UpdateContractDao>());

            _contractService.UpdateContract(contractId, updateContractVo);

            _contractRepository.Received().UpdateContractDetail(contractId, Arg.Any<bs_Contract>());
            _validateAndPopulateGlCodes.Received(1).Apply(updateContractVo);
        }

        [Fact]
        public void UpdateContract_WhenNewJobsAndServices_ShouldCreate()
        {
            var contractId = Guid.NewGuid();
            var existingInvoiceGroupId = Guid.NewGuid();
            var existingJobId = Guid.NewGuid();
            var existingServiceId = Guid.NewGuid();
            var existingRevenueShareId = Guid.NewGuid();
            var existingBellServiceId = Guid.NewGuid();
            var existingMidMonthId = Guid.NewGuid();
            var existingDepositedRevenueId = Guid.NewGuid();
            var existingBillableAccountId = Guid.NewGuid();
            var existingManagementAgreementId = Guid.NewGuid();

            var existingContract = new bs_Contract
            {
                bs_InvoiceGroup_Contract = new List<bs_InvoiceGroup>
                {
                    new bs_InvoiceGroup()
                    {
                        bs_InvoiceGroupId = existingInvoiceGroupId,
                        bs_Title = "Example Title",
                        bs_Description = "Example Desc",
                        bs_GroupNumber = 1
                    }
                },
                bs_FixedFeeService_Contract = new List<bs_FixedFeeService>
                {
                    new bs_FixedFeeService
                    {
                        bs_FixedFeeServiceId = existingServiceId,
                        bs_Name = "Existing Service",
                        bs_Fee = 1000
                    }
                },
                bs_LaborHourJob_Contract = new List<bs_LaborHourJob>
                {
                    new bs_LaborHourJob
                    {
                        bs_LaborHourJobId = existingJobId,
                        bs_Name = "Existing Job",
                        bs_Rate = 100.00m
                    }
                },
                bs_RevenueShareThreshold_Contract = new List<bs_RevenueShareThreshold>
                {
                    new bs_RevenueShareThreshold
                    {
                        bs_RevenueShareThresholdId = existingRevenueShareId,
                        bs_Name = "name",
                        bs_RevenueCodeData = "[\"123\",\"123\"]",
                        bs_RevenueAccumulationType = bs_revenueaccumulationtype.Monthly,
                        bs_TierData = "[{\"SharePercentage\":\"11\",\"Amount\":\"1000\"},{\"SharePercentage\":\"10\",\"Amount\":\"2000\"},{\"SharePercentage\":\"15\",\"Amount\":null}]",
                        bs_ValidationThresholdType = bs_validationthresholdtype.VehicleCount,
                        bs_ValidationThresholdAmount = 50,
                        bs_InvoiceGroup = 1
                    }
                },
                bs_BellService_bs_Contract = new List<bs_BellService>
                {
                    new bs_BellService
                    {
                        bs_BellServiceId = existingBellServiceId,
                        bs_InvoiceGroup = 1
                    }
                },
                bs_MidMonthAdvance_bs_Contract = new List<bs_MidMonthAdvance>
                {
                    new bs_MidMonthAdvance
                    {
                        bs_MidMonthAdvanceId = existingMidMonthId,
                        bs_InvoiceGroup = 1,
                        bs_LineTitle = bs_lineitemtitle.PreBill,
                        bs_Amount = 1000.00m
                    }
                },
                bs_DepositedRevenue_Contract = new List<bs_DepositedRevenue>
                {
                    new bs_DepositedRevenue
                    {
                        bs_DepositedRevenueId = existingDepositedRevenueId,
                        bs_InvoiceGroup = 1,
                        bs_TowneParkResponsibleForParkingTax = true
                    }
                },
                bs_BillableAccount_Contract = new List<bs_BillableAccount>
                {
                    new bs_BillableAccount
                    {
                        bs_BillableAccountId = existingBillableAccountId,
                        bs_PayrollAccountsData = "[\r\n    {\r\n        \"code\": \"6010\",\r\n        \"title\": \"Salaries - Personal Leave & Sick Pay - Hourly\",\r\n        \"isEnabled\": false\r\n    },\r\n    {\r\n        \"code\": \"6014\",\r\n        \"title\": \"Salaries - Other\",\r\n        \"isEnabled\": false\r\n    }]",
                        bs_PayrollAccountsInvoiceGroup = 1,
                        bs_PayrollAccountsLineTitle = "Test Payroll Line Title"
                    }
                },
                bs_ManagementAgreement_Contract = new List<bs_ManagementAgreement>
                {
                    new bs_ManagementAgreement
                    {
                        bs_ManagementAgreementId = existingManagementAgreementId,
                        bs_InvoiceGroup = 1,
                        bs_FixedFeeAmount = 1000.00m,
                        bs_RevenuePercentageAmount = 5.5m,
                        bs_ManagementAgreementType = bs_managementagreementtype.FixedFee
                    }
                },
                bs_SupportingReports = new[] { bs_supportingreporttypes.MixOfSales }
            };

            _contractRepository.GetContract(contractId).Returns(existingContract);

            var updateContractVo = new ContractDetailVo()
            {
                InvoiceGrouping =
                {
                    Enabled = false,
                    InvoiceGroups = new List<ContractDetailVo.InvoiceGroupVo>
                    {
                        new ContractDetailVo.InvoiceGroupVo()
                        {
                            Id = null,
                            Title = "New Title",
                            Description = "New Desc",
                            GroupNumber = 1
                        },
                        new ContractDetailVo.InvoiceGroupVo()
                        {
                            Id = existingInvoiceGroupId,
                            Title = "Existing Title",
                            Description = "Existing Desc",
                            GroupNumber = 1
                        }
                    }
                },
                FixedFee = new ContractDetailVo.FixedFeeVo
                {
                    Enabled = true,
                    ServiceRates = new List<ContractDetailVo.ServiceRateVo>
                    {
                        new ContractDetailVo.ServiceRateVo
                        {
                            Id = null, // null ID to simulate creation
                            Name = "New Service",
                            Fee = 1500
                        },
                        new ContractDetailVo.ServiceRateVo
                        {
                            Id = existingServiceId,
                            Name = "Existing Service",
                            Fee = 1000
                        }
                    }
                },
                PerLaborHour = new ContractDetailVo.PerLaborHourVo
                {
                    Enabled = true,
                    JobRates = new List<ContractDetailVo.JobRateVo>
                    {
                        new ContractDetailVo.JobRateVo
                        {
                            Id = null, // null ID to simulate creation
                            Name = "New Job",
                            Rate = 150.00m
                        },
                        new ContractDetailVo.JobRateVo
                        {
                            Id = existingJobId,
                            Name = "Existing Job",
                            Rate = 100.00m
                        }
                    }
                },
                RevenueShare = new ContractDetailVo.RevenueShareVo
                {
                    Enabled = true,
                    ThresholdStructures = new List<ContractDetailVo.ThresholdStructureVo>
                    {
                        new ContractDetailVo.ThresholdStructureVo{
                            Id = null,
                            RevenueCodes = new List<string>{"123", "123"},
                            AccumulationType = ContractDetailVo.AccumulationType.Monthly,
                            Tiers = new List<ContractDetailVo.TierVo>
                            {
                                new ContractDetailVo.TierVo
                                {
                                    SharePercentage = 11,
                                    Amount = 1000
                                },
                                new ContractDetailVo.TierVo
                                {
                                    SharePercentage = 10,
                                    Amount = 2000
                                },
                                new ContractDetailVo.TierVo
                                {
                                    SharePercentage = 15,
                                    Amount = null
                                }
                            }
                        }
                    }
                },
                BellServiceFee = new ContractDetailVo.BellServiceFeeVo
                {
                    Enabled = true,
                    BellServices = new List<ContractDetailVo.BellServiceVo>
                    {
                        new ContractDetailVo.BellServiceVo
                        {
                            Id = null, // null ID to simulate creation
                            InvoiceGroup = 1
                        },
                        new ContractDetailVo.BellServiceVo
                        {
                            Id = existingServiceId,
                            InvoiceGroup = 1
                        }
                    }
                },
                MidMonthAdvance = new ContractDetailVo.MidMonthAdvanceVo
                {
                    Enabled = true,
                    MidMonthAdvances = new List<ContractDetailVo.MidMonthVo>
                    {
                        new ContractDetailVo.MidMonthVo
                        {
                            Id = null, // null ID to simulate creation
                            InvoiceGroup = 1,
                            LineTitle = ContractDetailVo.LineTitleType.MidMonthBilling,
                            Amount = 1000.00m
                        }
                    }
                },
                DepositedRevenue = new ContractDetailVo.DepositedRevenueVo
                {
                    Enabled = true,
                    DepositData = new List<ContractDetailVo.DepositDataVo>
                    {
                        new ContractDetailVo.DepositDataVo
                        {
                            Id = null, // null ID to simulate creation
                            InvoiceGroup = 1,
                            TowneParkResponsibleForParkingTax = true
                        }
                    }
                },
                BillableAccount = new ContractDetailVo.BillableAccountVo
                {
                    Enabled = false,
                    BillableAccountsData = new List<ContractDetailVo.BillableAccountDataVo>
                    {
                        new ContractDetailVo.BillableAccountDataVo
                        {
                            Id = null,
                            PayrollAccountsData = "[\r\n    {\r\n        \"code\": \"6010\",\r\n        \"title\": \"Salaries - Personal Leave & Sick Pay - Hourly\",\r\n        \"isEnabled\": false\r\n    },\r\n    {\r\n        \"code\": \"6014\",\r\n        \"title\": \"Salaries - Other\",\r\n        \"isEnabled\": false\r\n    }]",
                            PayrollAccountsInvoiceGroup = 1,
                            PayrollAccountsLineTitle = "Test Payroll Line Title"
                        }
                    }
                },
                ManagementAgreement = new ContractDetailVo.ManagementAgreementVo
                {
                    Enabled = false,
                    ManagementFees = new List<ContractDetailVo.ManagementFeeVo>
                    {
                        new ContractDetailVo.ManagementFeeVo
                        {
                            Id = null,
                            InvoiceGroup = 1,
                            FixedFeeAmount = 1000.00m,
                            RevenuePercentageAmount = 5.5m,
                            ManagementAgreementType = ContractDetailVo.ManagementAgreementType.FixedFee
                        }
                    }
                },
                SupportingReports = new List<SupportingReportType> { SupportingReportType.MixOfSales }
            };

            _contractService.UpdateContract(contractId, updateContractVo);

            // Capture actual arguments passed
            _validateAndPopulateGlCodes.Received(1).Apply(updateContractVo);

            var updateContractDao = _contractRepository.ReceivedCalls().SingleOrDefault(call =>
                    call.GetMethodInfo().Name == nameof(_contractRepository.UpdateContractRelatedEntities))
                ?.GetArguments()[0] as UpdateContractDao;

            updateContractDao.Should().NotBeNull();

            var invoiceGroupCreated = updateContractDao.InvoiceGroupsToCreate;
            var invoiceGroupDeleted = updateContractDao.InvoiceGroupsToDelete;
            var fixedFeeServiceCreated = updateContractDao.ServicesToCreate;
            var fixedFeeServiceDeleted = updateContractDao.ServicesToDelete;
            var jobRatesCreated = updateContractDao.JobsToCreate;
            var jobRatesDeleted = updateContractDao.JobsToDelete;
            var midMonthCreated = updateContractDao.MidMonthsToCreate;
            var midMonthDeleted = updateContractDao.MidMonthsToDelete;
            var depostedRevenueCreated = updateContractDao.DepositedRevenuesToCreate;
            var depositedRevenueDeleted = updateContractDao.DepositedRevenuesToDelete;

            invoiceGroupCreated.Should().NotBeNull();
            invoiceGroupDeleted.Should().NotBeNull();
            fixedFeeServiceCreated.Should().NotBeNull();
            fixedFeeServiceDeleted.Should().NotBeNull();
            jobRatesCreated.Should().NotBeNull();
            jobRatesDeleted.Should().NotBeNull();
            midMonthCreated.Should().NotBeNull();
            midMonthDeleted.Should().NotBeNull();
            depostedRevenueCreated.Should().NotBeNull();
            depositedRevenueDeleted.Should().NotBeNull();
            
            invoiceGroupCreated.Select(inv => inv.bs_Title).Should().Contain("New Title");
            invoiceGroupDeleted.Should().BeEmpty();

            // Assert on the fixed fee services
            fixedFeeServiceCreated.Select(service => service.bs_Name).Should().Contain("New Service");
            fixedFeeServiceDeleted.Should().BeEmpty();

            // Assert on the job rates
            jobRatesCreated.Select(job => job.bs_Name).Should().Contain("New Job");
            jobRatesDeleted.Should().BeEmpty();
        }

        [Fact]
        public void UpdateContract_WhenMissingJobsAndServices_ShouldDelete()
        {
            var contractId = Guid.NewGuid();
            var existingJobId = Guid.NewGuid();
            var existingServiceId = Guid.NewGuid();
            var existingRevenueShareId = Guid.NewGuid();
            var existingBellServiceId = Guid.NewGuid();
            var existingMidMonthId = Guid.NewGuid();
            var existingDepositedRevenueId = Guid.NewGuid();
            var existingBillableAccountId = Guid.NewGuid();
            var existingManagementAgreementId = Guid.NewGuid();

            var existingContract = new bs_Contract
            {
                bs_InvoiceGroup_Contract = new List<bs_InvoiceGroup>
                {
                    new bs_InvoiceGroup()
                    {
                        bs_Title = "Example Title",
                        bs_Description = "Example Desc",
                        bs_GroupNumber = 1
                    }
                },
                bs_FixedFeeService_Contract = new List<bs_FixedFeeService>
                {
                    new bs_FixedFeeService
                    {
                        bs_FixedFeeServiceId = existingServiceId,
                        bs_Name = "Existing Service",
                        bs_Fee = 1000
                    }
                },
                bs_LaborHourJob_Contract = new List<bs_LaborHourJob>
                {
                    new bs_LaborHourJob
                    {
                        bs_LaborHourJobId = existingJobId,
                        bs_Name = "Existing Job",
                        bs_Rate = 100.00m
                    }
                },
                bs_RevenueShareThreshold_Contract = new List<bs_RevenueShareThreshold>
                {
                    new bs_RevenueShareThreshold
                    {
                        bs_RevenueShareThresholdId = existingRevenueShareId,
                        bs_Name = "name",
                        bs_RevenueCodeData = "[\"123\",\"123\"]",
                        bs_RevenueAccumulationType = bs_revenueaccumulationtype.Monthly,
                        bs_TierData = "[{\"SharePercentage\":\"11\",\"Amount\":\"1000\"},{\"SharePercentage\":\"10\",\"Amount\":\"2000\"},{\"SharePercentage\":\"15\",\"Amount\":null}]",
                        bs_ValidationThresholdType = bs_validationthresholdtype.VehicleCount,
                        bs_ValidationThresholdAmount = 50,
                        bs_InvoiceGroup = 1
                    }
                },
                bs_BellService_bs_Contract = new List<bs_BellService>
                {
                    new bs_BellService
                    {
                        bs_BellServiceId = existingBellServiceId,
                        bs_InvoiceGroup = 1,
                    }
                },
                bs_MidMonthAdvance_bs_Contract = new List<bs_MidMonthAdvance>
                {
                    new bs_MidMonthAdvance
                    {
                        bs_MidMonthAdvanceId = existingMidMonthId,
                        bs_InvoiceGroup = 1,
                        bs_Amount = 100.00m,
                        bs_LineTitle = bs_lineitemtitle.MidMonthBilling
                    }
                },
                bs_DepositedRevenue_Contract = new List<bs_DepositedRevenue>
                {
                    new bs_DepositedRevenue
                    {
                        bs_DepositedRevenueId = existingDepositedRevenueId,
                        bs_InvoiceGroup = 1,
                        bs_TowneParkResponsibleForParkingTax = true
                    }
                },
                bs_BillableAccount_Contract = new List<bs_BillableAccount>
                {
                    new bs_BillableAccount
                    {
                        bs_BillableAccountId = existingBillableAccountId,
                        bs_PayrollAccountsData = "[\r\n    {\r\n        \"code\": \"6010\",\r\n        \"title\": \"Salaries - Personal Leave & Sick Pay - Hourly\",\r\n        \"isEnabled\": false\r\n    },\r\n    {\r\n        \"code\": \"6014\",\r\n        \"title\": \"Salaries - Other\",\r\n        \"isEnabled\": false\r\n    }]",
                        bs_PayrollAccountsInvoiceGroup = 1,
                        bs_PayrollAccountsLineTitle = "Test Payroll Line Title"
                    }
                },
                bs_ManagementAgreement_Contract = new List<bs_ManagementAgreement>
                {
                    new bs_ManagementAgreement
                    {
                        bs_ManagementAgreementId = existingManagementAgreementId,
                        bs_InvoiceGroup = 1,
                        bs_FixedFeeAmount = 1000.00m,
                        bs_RevenuePercentageAmount = 5.5m,
                        bs_ManagementAgreementType = bs_managementagreementtype.FixedFee
                    }
                }
            };

            _contractRepository.GetContract(contractId).Returns(existingContract);

            var updateContractVo = new ContractDetailVo
            {
                InvoiceGrouping =
                {
                    Enabled = false,
                    InvoiceGroups = new List<ContractDetailVo.InvoiceGroupVo>()
                },
                FixedFee = new ContractDetailVo.FixedFeeVo
                {
                    Enabled = true,
                    ServiceRates = new List<ContractDetailVo.ServiceRateVo>()
                },
                PerLaborHour = new ContractDetailVo.PerLaborHourVo
                {
                    Enabled = true,
                    JobRates = new List<ContractDetailVo.JobRateVo>()
                },
                RevenueShare = new ContractDetailVo.RevenueShareVo
                {
                    Enabled = true,
                    ThresholdStructures = new List<ContractDetailVo.ThresholdStructureVo>()
                },
                BellServiceFee = new ContractDetailVo.BellServiceFeeVo
                {
                    Enabled = true,
                    BellServices = new List<ContractDetailVo.BellServiceVo>()
                },
                MidMonthAdvance = new ContractDetailVo.MidMonthAdvanceVo
                {
                    Enabled = true,
                    MidMonthAdvances = new List<ContractDetailVo.MidMonthVo>()
                },
                DepositedRevenue = new ContractDetailVo.DepositedRevenueVo
                {
                    Enabled = true,
                    DepositData = new List<ContractDetailVo.DepositDataVo>()
                },
                BillableAccount = new ContractDetailVo.BillableAccountVo
                {
                    Enabled = true,
                    BillableAccountsData = new List<ContractDetailVo.BillableAccountDataVo>()
                },
                ManagementAgreement = new ContractDetailVo.ManagementAgreementVo
                {
                    Enabled = true,
                    ManagementFees = new List<ContractDetailVo.ManagementFeeVo>()
                },
                SupportingReports = new List<SupportingReportType>()
            };

            _contractService.UpdateContract(contractId, updateContractVo);

            // Capture actual arguments passed
            _validateAndPopulateGlCodes.Received(1).Apply(updateContractVo);

            var updateContractDao = _contractRepository.ReceivedCalls().SingleOrDefault(call =>
                    call.GetMethodInfo().Name == nameof(_contractRepository.UpdateContractRelatedEntities))
                ?.GetArguments()[0] as UpdateContractDao;

            updateContractDao.Should().NotBeNull();

            var invoiceGroupCreated = updateContractDao.InvoiceGroupsToCreate;
            var invoiceGroupDeleted = updateContractDao.InvoiceGroupsToDelete;
            var fixedFeeServiceCreated = updateContractDao.ServicesToCreate;
            var fixedFeeServiceDeleted = updateContractDao.ServicesToDelete;
            var jobRatesCreated = updateContractDao.JobsToCreate;
            var jobRatesDeleted = updateContractDao.JobsToDelete;
            var revenueShareCreated = updateContractDao.ThresholdStructuresToCreate;
            var revenueShareDeleted = updateContractDao.ThresholdStructuresToDelete;
            var bellServiceFeeCreated = updateContractDao.ThresholdStructuresToCreate;
            var bellServiceFeeDeleted = updateContractDao.ThresholdStructuresToDelete;
            var midMonthCreated = updateContractDao.MidMonthsToCreate;
            var midMonthDeleted = updateContractDao.MidMonthsToDelete;
            var depositedRevenueCreated = updateContractDao.DepositedRevenuesToCreate;
            var depositedRevenueDeleted = updateContractDao.DepositedRevenuesToDelete;
            var managementAgreementCreated = updateContractDao.ManagementFeesToCreate;
            var managementAgreementDeleted = updateContractDao.ManagementFeesToDelete;

            invoiceGroupCreated.Should().NotBeNull();
            invoiceGroupDeleted.Should().NotBeNull();
            fixedFeeServiceCreated.Should().NotBeNull();
            fixedFeeServiceDeleted.Should().NotBeNull();
            jobRatesCreated.Should().NotBeNull();
            jobRatesDeleted.Should().NotBeNull();
            revenueShareCreated.Should().NotBeNull();
            revenueShareDeleted.Should().NotBeNull();
            bellServiceFeeCreated.Should().NotBeNull();
            bellServiceFeeDeleted.Should().NotBeNull();
            midMonthCreated.Should().NotBeNull();
            midMonthDeleted.Should().NotBeNull();
            depositedRevenueCreated.Should().NotBeNull();
            depositedRevenueDeleted.Should().NotBeNull();
            managementAgreementCreated.Should().NotBeNull();
            managementAgreementDeleted.Should().NotBeNull();

            invoiceGroupCreated.Should().BeEmpty();
            invoiceGroupDeleted.Should().HaveCount(1);

            // Assert on the fixed fee services
            fixedFeeServiceCreated.Should().BeEmpty();
            fixedFeeServiceDeleted.Should().HaveCount(1);

            // Assert on the job rates
            jobRatesCreated.Should().BeEmpty();
            jobRatesDeleted.Should().HaveCount(1);
        }

        [Fact]
        public void UpdateContract_ThrowsException_WhenContractDoesNotExist()
        {
            var contractId = Guid.NewGuid();
            _contractRepository.GetContract(Arg.Any<Guid>()).Returns((bs_Contract)null);

            var updateContractVo = new ContractDetailVo();

            Action act = () => _contractService.UpdateContract(contractId, updateContractVo);

            act.Should().Throw<NullReferenceException>();
        }

        [Fact]
        public void GetContractDetail_ThrowsException_WhenContractDoesNotExist()
        {
            var customerSiteId = Guid.NewGuid();
            _contractRepository.GetContractByCustomerSite(Arg.Any<Guid>()).Returns((bs_Contract)null);

            Action act = () => _contractService.GetContractDetail(customerSiteId);

            act.Should().Throw<NullReferenceException>();
        }

        [Fact]
        public void GetDeviations_ShouldCallContractRepositoryAndReturnAdaptedResponse()
        {
            var deviationModels = new List<bs_Contract>
            {
                new bs_Contract
                {
                    bs_ContractId = Guid.NewGuid(),
                    bs_DeviationAmount = 1000.00m,
                    bs_DeviationPercentage = 10.00m,
                    bs_Contract_CustomerSite = new bs_CustomerSite
                    {
                        bs_CustomerSiteId = Guid.NewGuid(),
                        bs_SiteName = "Site 1",
                        bs_SiteNumber = "1"
                    }
                },
                new bs_Contract
                {
                    bs_ContractId = Guid.NewGuid(),
                    bs_DeviationAmount = 2000.00m,
                    bs_DeviationPercentage = 20.00m,
                    bs_Contract_CustomerSite = new bs_CustomerSite
                    {
                        bs_CustomerSiteId = Guid.NewGuid(),
                        bs_SiteName = "Site 2",
                        bs_SiteNumber = "2"
                    }
                }
            };

            _contractRepository.GetDeviations().Returns(deviationModels);

            var expectedDeviations = new List<DeviationVo>
            {
                new DeviationVo
                {
                    ContractId = deviationModels[0].bs_ContractId,
                    DeviationAmount = deviationModels[0].bs_DeviationAmount,
                    DeviationPercentage = deviationModels[0].bs_DeviationPercentage,
                    CustomerSiteId = deviationModels[0].bs_Contract_CustomerSite.bs_CustomerSiteId,
                    SiteName = deviationModels[0].bs_Contract_CustomerSite.bs_SiteName,
                    SiteNumber = deviationModels[0].bs_Contract_CustomerSite.bs_SiteNumber
                },
                new DeviationVo
                {
                    ContractId = deviationModels[1].bs_ContractId,
                    DeviationAmount = deviationModels[1].bs_DeviationAmount,
                    DeviationPercentage = deviationModels[1].bs_DeviationPercentage,
                    CustomerSiteId = deviationModels[1].bs_Contract_CustomerSite.bs_CustomerSiteId,
                    SiteName = deviationModels[1].bs_Contract_CustomerSite.bs_SiteName,
                    SiteNumber = deviationModels[1].bs_Contract_CustomerSite.bs_SiteNumber
                }
            };

            var result = _contractService.GetDeviations();

            result.Should().BeEquivalentTo(expectedDeviations);
        }

        [Fact]
        public void UpdateDeviationThreshold_ShouldCallContractRepositoryWithCorrectParameters()
        {
            var contractId1 = Guid.NewGuid();
            var contractId2 = Guid.NewGuid();
            var contractId3 = Guid.NewGuid();

            var deviationUpdates = new List<DeviationVo>
            {
                new DeviationVo
                {
                    ContractId = contractId1,
                    DeviationAmount = 1000.00m,
                    DeviationPercentage = 10.00m
                },
                new DeviationVo
                {
                    ContractId = contractId2,
                    DeviationAmount = 2000.00m,
                    DeviationPercentage = 20.00m
                },
                new DeviationVo
                {
                    ContractId = contractId3,
                    DeviationAmount = 3000.00m,
                    DeviationPercentage = 30.00m
                }
            };

            _contractService.UpdateDeviationThreshold(deviationUpdates);

            _contractRepository.Received().UpdateDeviationThreshold(Arg.Is<List<bs_Contract>>(contracts =>
                contracts.Count == deviationUpdates.Count &&
                contracts.Any(contract => contract.bs_ContractId == contractId1 && contract.bs_DeviationAmount == 1000.00m && contract.bs_DeviationPercentage == 10.00m) &&
                contracts.Any(contract => contract.bs_ContractId == contractId2 && contract.bs_DeviationAmount == 2000.00m && contract.bs_DeviationPercentage == 20.00m) &&
                contracts.Any(contract => contract.bs_ContractId == contractId3 && contract.bs_DeviationAmount == 3000.00m && contract.bs_DeviationPercentage == 30.00m)
            ));
        }

    }
}

