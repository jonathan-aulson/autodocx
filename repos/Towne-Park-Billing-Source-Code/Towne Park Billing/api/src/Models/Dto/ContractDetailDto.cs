using Newtonsoft.Json;

namespace api.Models.Dto
{
    public class ContractDetailDto
    {
        public ContractDetailDto()
        {
            InvoiceGrouping = new InvoiceGroupingDto();
            FixedFee = new FixedFeeDto();
            PerLaborHour = new PerLaborHourDto();
            PerOccupiedRoom = new PerOccupiedRoomDto();
            RevenueShare = new RevenueShareDto();
            BellServiceFee = new BellServiceFeeDto();
            MidMonthAdvance = new MidMonthAdvanceDto();
            DepositedRevenue = new DepositedRevenueDto();
            BillableAccount = new BillableAccountDto();
            ManagementAgreement = new ManagementAgreementDto();
        }

        [JsonProperty("id")]
        public Guid? Id { get; set; }

        [JsonProperty("purchaseOrder")]
        public string? PurchaseOrder { get; set; }

        [JsonProperty("paymentTerms")]
        public string? PaymentTerms { get; set; }

        [JsonProperty("billingType")]
        public string? BillingType { get; set; }

        [JsonProperty("incrementMonth")]
        public string? IncrementMonth { get; set; }

        [JsonProperty("incrementAmount")]
        public decimal? IncrementAmount { get; set; }

        [JsonProperty("consumerPriceIndex")]
        public bool? ConsumerPriceIndex { get; set; }

        [JsonProperty("notes")]
        public string? Notes { get; set; }

        [JsonProperty("deviationAmount")]
        public decimal? DeviationAmount { get; set; }

        [JsonProperty("deviationPercentage")]
        public decimal? DeviationPercentage { get; set; }

        [JsonProperty("deposits")]
        public bool Deposits { get; set; }

        [JsonProperty("contractType")]
        public string? ContractTypeString { get; set; }

        [JsonProperty("supportingReports")]
        public List<string>? SupportingReports { get; set; }

        [JsonProperty("invoiceGrouping")]
        public InvoiceGroupingDto InvoiceGrouping { get; set; }

        [JsonProperty("fixedFee")]
        public FixedFeeDto FixedFee { get; set; }

        [JsonProperty("perLaborHour")]
        public PerLaborHourDto PerLaborHour { get; set; }

        [JsonProperty("perOccupiedRoom")]
        public PerOccupiedRoomDto PerOccupiedRoom { get; set; }

        [JsonProperty("revenueShare")]
        public RevenueShareDto RevenueShare { get; set; }

        [JsonProperty("bellServiceFee")]
        public BellServiceFeeDto BellServiceFee { get; set; }

        [JsonProperty("midMonthAdvance")]
        public MidMonthAdvanceDto MidMonthAdvance { get; set; }

        [JsonProperty("depositedRevenue")]
        public DepositedRevenueDto DepositedRevenue { get; set; }

        [JsonProperty("billableAccounts")]
        public BillableAccountDto? BillableAccount { get; set; }

        [JsonProperty("managementAgreement")]
        public ManagementAgreementDto? ManagementAgreement { get; set; }

        public class InvoiceGroupingDto
        {
            public InvoiceGroupingDto()
            {
                InvoiceGroups = new List<InvoiceGroupDto>();
            }

            [JsonProperty("enabled")]
            public bool Enabled { get; set; }

            [JsonProperty("invoiceGroups")]
            public List<InvoiceGroupDto> InvoiceGroups { get; set; }

        }

        public class PerOccupiedRoomDto
        {
            [JsonProperty("enabled")]
            public bool? Enabled { get; set; }

            [JsonProperty("roomRate")]
            public decimal? RoomRate { get; set; }

            [JsonProperty("invoiceGroup")]
            public int? InvoiceGroup { get; set; }
        }

        public class FixedFeeDto
        {
            public FixedFeeDto()
            {
                ServiceRates = new List<ServiceRateDto>();
            }

            [JsonProperty("enabled")]
            public bool? Enabled { get; set; }

            [JsonProperty("serviceRates")]
            public List<ServiceRateDto> ServiceRates { get; set; }
        }

        public class PerLaborHourDto
        {
            public PerLaborHourDto()
            {
                JobRates = new List<JobRateDto>();
            }

            [JsonProperty("enabled")]
            public bool? Enabled { get; set; }

            [JsonProperty("hoursBackupReport")]
            public bool? HoursBackupReport { get; set; }

            [JsonProperty("jobRates")]
            public List<JobRateDto> JobRates { get; set; }
        }

        public class InvoiceGroupDto
        {
            [JsonProperty("id")]
            public Guid? Id { get; set; }
            [JsonProperty("groupNumber")]
            public int? GroupNumber { get; set; }
            [JsonProperty("title")]
            public string? Title { get; set; }
            [JsonProperty("description")]
            public string? Description { get; set; }
            [JsonProperty("vendorId")]
            public string? VendorId { get; set; }
            [JsonProperty("siteNumber")]
            public string? SiteNumber { get; set; }
            [JsonProperty("customerName")]
            public string? CustomerName { get; set; }
            [JsonProperty("billingContactEmails")]
            public string? BillingContactEmails { get; set; }
        }

        public class ServiceRateDto
        {
            [JsonProperty("id")]
            public Guid? Id { get; set; }

            [JsonProperty("name")]
            public string? Name { get; set; }

            [JsonProperty("displayName")]
            public string? DisplayName { get; set; }

            [JsonProperty("code")]
            public string? Code { get; set; }

            [JsonProperty("fee")]
            public decimal? Fee { get; set; }

            [JsonProperty("startDate")]
            public DateTime? StartDate { get; set; }

            [JsonProperty("endDate")]
            public DateTime? EndDate { get; set; }

            [JsonProperty("invoiceGroup")]
            public int? InvoiceGroup { get; set; }
        }

        public class JobRateDto
        {
            [JsonProperty("id")]
            public Guid? Id { get; set; }

            [JsonProperty("name")]
            public string? Name { get; set; }

            [JsonProperty("displayName")]
            public string? DisplayName { get; set; }

            [JsonProperty("rate")]
            public decimal? Rate { get; set; }

            [JsonProperty("overtimeRate")]
            public decimal? OvertimeRate { get; set; }

            [JsonProperty("jobCode")]
            public string? JobCode { get; set; }

            [JsonProperty("startDate")]
            public DateTime? StartDate { get; set; }

            [JsonProperty("endDate")]
            public DateTime? EndDate { get; set; }

            [JsonProperty("invoiceGroup")]
            public int? InvoiceGroup { get; set; }
        }

        public class RevenueShareDto
        {
            public RevenueShareDto()
            {
                ThresholdStructures = new List<ThresholdStructureDto>();
            }

            [JsonProperty("enabled")]
            public bool Enabled { get; set; }

            [JsonProperty("thresholdStructures")]
            public List<ThresholdStructureDto> ThresholdStructures { get; set; }
        }

        public class ThresholdStructureDto
        {
            public ThresholdStructureDto()
            {
                Tiers = new List<TierDto>();
            }

            [JsonProperty("id")]
            public Guid? Id { get; set; }

            [JsonProperty("revenueCodes")]
            public List<string>? RevenueCodes { get; set; }

            [JsonProperty("accumulationType")]
            public string? AccumulationType { get; set; }

            [JsonProperty("tiers")]
            public List<TierDto> Tiers { get; set; }

            [JsonProperty("validationThresholdType")]
            public string? ValidationThresholdType { get; set; }

            [JsonProperty("validationThresholdAmount")]
            public decimal? ValidationThresholdAmount { get; set; }

            [JsonProperty("invoiceGroup")]
            public int? InvoiceGroup { get; set; }
        }

        public class TierDto
        {
            [JsonProperty("sharePercentage")]
            public decimal? SharePercentage { get; set; }

            [JsonProperty("amount")]
            public decimal? Amount { get; set; }

            [JsonProperty("escalatorValue")]
            public decimal? EscalatorValue { get; set; }
        }

        public class BellServiceFeeDto
        {
            public BellServiceFeeDto()
            {
                BellServices = new List<BellServiceDto>();
            }

            [JsonProperty("enabled")]
            public bool? Enabled { get; set; }

            [JsonProperty("bellServices")]
            public List<BellServiceDto>? BellServices { get; set; }
        }

        public class BellServiceDto
        {
            [JsonProperty("id")]
            public Guid? Id { get; set; }

            [JsonProperty("invoiceGroup")]
            public int? InvoiceGroup { get; set; }
        }

        public class MidMonthAdvanceDto
        {
            public MidMonthAdvanceDto()
            {
                MidMonthAdvances = new List<MidMonthDto>();
            }

            [JsonProperty("enabled")]
            public bool? Enabled { get; set; }

            [JsonProperty("midMonthAdvances")]
            public List<MidMonthDto>? MidMonthAdvances { get; set; }
        }

        public class MidMonthDto
        {
            [JsonProperty("id")]
            public Guid? Id { get; set; }

            [JsonProperty("amount")]
            public decimal? Amount { get; set; }

            [JsonProperty("lineTitle")]
            public string? LineTitle { get; set; }

            [JsonProperty("invoiceGroup")]
            public int? InvoiceGroup { get; set; }
        }

        public class DepositedRevenueDto
        {
            public DepositedRevenueDto()
            {
                DepositData = new List<DepositDataDto>();
            }
            [JsonProperty("enabled")]
            public bool? Enabled { get; set; }

            [JsonProperty("depositData")]
            public List<DepositDataDto>? DepositData { get; set; }
        }

        public class DepositDataDto
        {
            [JsonProperty("id")]
            public Guid? Id { get; set; }

            [JsonProperty("towneParkResponsibleForParkingTax")]
            public bool? TowneParkResponsibleForParkingTax { get; set; }

            [JsonProperty("depositedRevenueEnabled")]
            public bool? DepositedRevenueEnabled { get; set; }

            [JsonProperty("invoiceGroup")]
            public int? InvoiceGroup { get; set; }
        }

        public class BillableAccountDto
        {
            public BillableAccountDto()
            {
                BillableAccountsData = new List<BillableAccountDataDto>();
            }
            [JsonProperty("enabled")]
            public bool? Enabled { get; set; }

            [JsonProperty("billableAccountsData")]
            public List<BillableAccountDataDto>? BillableAccountsData { get; set; }
        }

        public class BillableAccountDataDto
        {
            [JsonProperty("id")]
            public Guid? Id { get; set; }

            [JsonProperty("payrollAccountsData")]
            public string? PayrollAccountsData { get; set; }

            [JsonProperty("payrollAccountsInvoiceGroup")]
            public int? PayrollAccountsInvoiceGroup { get; set; }

            [JsonProperty("payrollAccountsLineTitle")]
            public string? PayrollAccountsLineTitle { get; set; }

            [JsonProperty("payrollTaxesEnabled")]
            public bool? PayrollTaxesEnabled { get; set; }

            [JsonProperty("payrollTaxesBillingType")]
            public string? PayrollTaxesBillingType { get; set; }

            [JsonProperty("payrollTaxesLineTitle")]
            public string? PayrollTaxesLineTitle { get; set; }

            [JsonProperty("payrollTaxesPercentage")]
            public decimal? PayrollTaxesPercentage { get; set; }

            [JsonProperty("payrollSupportAmount")]
            public decimal? PayrollSupportAmount { get; set; }

            [JsonProperty("payrollSupportBillingType")]
            public string? PayrollSupportBillingType { get; set; }

            [JsonProperty("payrollSupportEnabled")]
            public bool? PayrollSupportEnabled { get; set; }

            [JsonProperty("payrollSupportLineTitle")]
            public string? PayrollSupportLineTitle { get; set; }

            [JsonProperty("payrollSupportPayrollType")]
            public string? PayrollSupportPayrollType { get; set; }

            [JsonProperty("payrollExpenseAccountsData")]
            public string? PayrollExpenseAccountsData { get; set; }

            [JsonProperty("payrollExpenseAccountsInvoiceGroup")]
            public int? PayrollExpenseAccountsInvoiceGroup { get; set; }

            [JsonProperty("payrollExpenseAccountsLineTitle")]
            public string? PayrollExpenseAccountsLineTitle { get; set; }
            [JsonProperty("additionalPayrollAmount")]
            public decimal? AdditionalPayrollAmount { get; set; }

            [JsonProperty("payrollTaxesEscalatorEnable")]
            public bool? PayrollTaxesEscalatorEnable { get; set; }
            
           [JsonProperty("payrollTaxesEscalatorMonth")]
            public string? PayrollTaxesEscalatorMonth { get; set; }
        
            [JsonProperty("payrollTaxesEscalatorvalue")]
            public decimal? PayrollTaxesEscalatorvalue { get; set; }
            
            [JsonProperty("payrollTaxesEscalatorType")]
            public string? PayrollTaxesEscalatorType { get; set; }

           
        }

        public class JobCodeDto
        {
            [JsonProperty("code")]
            public string? Code { get; set; }

            [JsonProperty("description")]
            public string? Description { get; set; }

            [JsonProperty("standardRate")]
            public decimal? StandardRate { get; set; }

            [JsonProperty("overtimeRate")]
            public decimal? OvertimeRate { get; set; }

            [JsonProperty("standardRateEscalatorValue")]
            public decimal? StandardRateEscalatorValue { get; set; }

            [JsonProperty("overtimeRateEscalatorValue")]
            public decimal? OvertimeRateEscalatorValue { get; set; }
        }

        public class ManagementAgreementDto
        {
            public ManagementAgreementDto()
            {
                ManagementFees = new List<ManagementFeeDto>();
            }
            [JsonProperty("enabled")]
            public bool? Enabled { get; set; }

            [JsonProperty("ManagementFees")]
            public List<ManagementFeeDto>? ManagementFees { get; set; }
        }

        public class ManagementFeeDto
        {
            [JsonProperty("id")]
            public Guid? Id { get; set; }

            [JsonProperty("invoiceGroup")]
            public int? InvoiceGroup { get; set; }

            [JsonProperty("managementAgreementType")]
            public string? ManagementAgreementType { get; set; }

            [JsonProperty("managementFeeEscalatorEnabled")]
            public bool? ManagementFeeEscalatorEnabled { get; set; }

            [JsonProperty("managementFeeEscalatorMonth")]
            public string? ManagementFeeEscalatorMonth { get; set; }

            [JsonProperty("managementFeeEscalatorType")]
            public string? ManagementFeeEscalatorType { get; set; }

            [JsonProperty("managementFeeEscalatorValue")]
            public decimal? ManagementFeeEscalatorValue { get; set; }

            [JsonProperty("fixedFeeAmount")]
            public decimal? FixedFeeAmount { get; set; }

            [JsonProperty("laborHourJobCode")]
            public string? LaborHourJobCode { get; set; }

            [JsonProperty("perLaborHourJobCodeData")]
            public List<JobCodeDto>? PerLaborHourJobCodeData { get; set; }

            [JsonProperty("laborHourRate")]
            public decimal? LaborHourRate { get; set; }

            [JsonProperty("laborHourOvertimeRate")]
            public decimal? LaborHourOvertimeRate { get; set; }

            [JsonProperty("revenuePercentageAmount")]
            public decimal? RevenuePercentageAmount { get; set; }

            [JsonProperty("insuranceAdditionalPercentage")]
            public decimal? InsuranceAdditionalPercentage { get; set; }

            [JsonProperty("insuranceEnabled")]
            public bool? InsuranceEnabled { get; set; }

            [JsonProperty("insuranceFixedFeeAmount")]
            public decimal? InsuranceFixedFeeAmount { get; set; }

            [JsonProperty("insuranceLineTitle")]
            public string? InsuranceLineTitle { get; set; }

            [JsonProperty("insuranceType")]
            public string? InsuranceType { get; set; }

            [JsonProperty("claimsCapAmount")]
            public decimal? ClaimsCapAmount { get; set; }

            [JsonProperty("claimsEnabled")]
            public bool? ClaimsEnabled { get; set; }

            [JsonProperty("claimsLineTitle")]
            public string? ClaimsLineTitle { get; set; }

            [JsonProperty("claimsType")]
            public string? ClaimsType { get; set; }

            [JsonProperty("profitShareAccumulationType")]
            public string? ProfitShareAccumulationType { get; set; }

            [JsonProperty("profitShareEnabled")]
            public bool? ProfitShareEnabled { get; set; }

            [JsonProperty("profitShareTierData")]
            public List<TierDto>? ProfitShareTierData { get; set; }

            [JsonProperty("profitShareEscalatorEnabled")]
            public bool? ProfitShareEscalatorEnabled { get; set; }

            [JsonProperty("profitShareEscalatorMonth")]
            public string? ProfitShareEscalatorMonth { get; set; }

            [JsonProperty("profitShareEscalatorType")]
            public string? ProfitShareEscalatorType { get; set; }

            [JsonProperty("validationThresholdAmount")]
            public decimal? ValidationThresholdAmount { get; set; }

            [JsonProperty("validationThresholdEnabled")]
            public bool? ValidationThresholdEnabled { get; set; }

            [JsonProperty("validationThresholdType")]
            public string? ValidationThresholdType { get; set; }
        
            [JsonProperty("nonGlBillableExpensesEnabled")]
            public bool? NonGlBillableExpensesEnabled { get; set; }

            [JsonProperty("nonGlBillableExpenses")]
            public List<NonGlBillableExpenseDto>? NonGlBillableExpenses { get; set; }
        }
        public class NonGlBillableExpenseDto
        {
            [JsonProperty("id")]
            public Guid? Id { get; set; }

            [JsonProperty("nonglexpensetype")]
            public string? NonGLExpenseType { get; set; } 

            [JsonProperty("expensepayrolltype")]
            public string? ExpensePayrollType { get; set; } 

            [JsonProperty("expenseamount")]
            public decimal? ExpenseAmount { get; set; } 

            [JsonProperty("expensetitle")]
            public string? ExpenseTitle { get; set; }

            [JsonProperty("finalperiodbilled")]
            public DateTime? FinalPeriodBilled { get; set; }  

               [JsonProperty("sequenceNumber")]
            public int? SequenceNumber { get; set; }
        }
    }
}