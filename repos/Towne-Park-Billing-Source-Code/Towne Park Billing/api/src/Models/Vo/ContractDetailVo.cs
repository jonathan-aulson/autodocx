using api.Models.Vo.Enum;
using Microsoft.Xrm.Sdk.Metadata;
using Newtonsoft.Json;
using static api.Models.Dto.ContractDetailDto;

namespace api.Models.Vo
{
    public class ContractDetailVo
    {
        public ContractDetailVo()
        {
            InvoiceGrouping = new InvoiceGroupingVo();
            FixedFee = new FixedFeeVo();
            PerLaborHour = new PerLaborHourVo();
            PerOccupiedRoom = new PerOccupiedRoomVo();
            RevenueShare = new RevenueShareVo();
            BellServiceFee = new BellServiceFeeVo();
            MidMonthAdvance = new MidMonthAdvanceVo();
            DepositedRevenue = new DepositedRevenueVo();
            BillableAccount = new BillableAccountVo();
            ManagementAgreement = new ManagementAgreementVo();
        }
       
        public Guid? Id { get; set; }
        public string? PurchaseOrder { get; set; }
        public string? PaymentTerms { get; set; }
        public BillingType? BillingType { get; set; }
        public Month? IncrementMonth { get; set; }
        public decimal? IncrementAmount { get; set; }
        public bool ConsumerPriceIndex { get; set; }
        public string? Notes { get; set; }
        public decimal? DeviationAmount { get; set; }
        public decimal? DeviationPercentage { get; set; }
        public bool Deposits { get; set; }
        public string? ContractTypeString { get; set; }
        public List<SupportingReportType>? SupportingReports { get; set; }
        public InvoiceGroupingVo InvoiceGrouping { get; set; }
        public FixedFeeVo FixedFee { get; set; }
        public PerLaborHourVo PerLaborHour { get; set; }
        public PerOccupiedRoomVo PerOccupiedRoom { get; set; }
        public RevenueShareVo RevenueShare { get; set; }
        public BellServiceFeeVo BellServiceFee { get; set; }
        public MidMonthAdvanceVo MidMonthAdvance { get; set; }
        public DepositedRevenueVo DepositedRevenue { get; set; }
        public BillableAccountVo BillableAccount { get; set; }
        public ManagementAgreementVo ManagementAgreement { get; set; }

        public class InvoiceGroupingVo
        {
            public InvoiceGroupingVo()
            {
                InvoiceGroups = new List<InvoiceGroupVo>();
            }
            public bool Enabled { get; set; }

            public List<InvoiceGroupVo> InvoiceGroups { get; set; }
        }

        public class PerOccupiedRoomVo
        {
            public bool Enabled { get; set; }
            public decimal? RoomRate { get; set; }
            public string? Code { get; set; } // Added in service.
            public int? InvoiceGroup { get; set; }
        }

        public class FixedFeeVo
        {
            public FixedFeeVo()
            {
                ServiceRates = new List<ServiceRateVo>();
            }
            public bool Enabled { get; set; }

            public List<ServiceRateVo> ServiceRates { get; set; }
        }

        public class PerLaborHourVo
        {
            public PerLaborHourVo()
            {
                JobRates = new List<JobRateVo>();
            }
            public bool Enabled { get; set; }

            public bool? HoursBackupReport { get; set; }

            public List<JobRateVo> JobRates { get; set; }
        }

        public class InvoiceGroupVo
        {
            public Guid? Id { get; set; }
            public int? GroupNumber { get; set; }
            public string? Title { get; set; }
            public string? Description { get; set; }
            public string? VendorId { get; set; }
            public string? SiteNumber { get; set; }
            public string? CustomerName { get; set; }
            public string? BillingContactEmails { get; set; }
        }

        public class ServiceRateVo
        {
            public Guid? Id { get; set; }
            public string? Name { get; set; }
            public string? DisplayName { get; set; }
            public string? Code { get; set; }
            public decimal? Fee { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }

            public int? InvoiceGroup { get; set; }
        }

        public class JobRateVo
        {
            public Guid? Id { get; set; }
            public string? Name { get; set; }
            public string? DisplayName { get; set; }
            public decimal? Rate { get; set; }
            public decimal? OvertimeRate { get; set; }
            public string? Code { get; set; } // Added in service.
            public string? JobCode { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? EndDate { get; set; }

            public int? InvoiceGroup { get; set; }
        }

        public class RevenueShareVo
        {
            public RevenueShareVo()
            {
                ThresholdStructures = new List<ThresholdStructureVo>();
            }
            public Guid? Id { get; set; }
            public bool Enabled { get; set; }
            public List<ThresholdStructureVo> ThresholdStructures { get; set; }
        }
        public class ThresholdStructureVo
        {
            public ThresholdStructureVo()
            {
                Tiers = new List<TierVo>();
            }
            public Guid? Id { get; set; }
            public List<string>? RevenueCodes { get; set; }
            public AccumulationType AccumulationType { get; set; }
            public List<TierVo> Tiers { get; set; }
            public decimal? ValidationThresholdAmount { get; set; }
            public ValidationThresholdType? ValidationThresholdType { get; set; }
            public int? InvoiceGroup { set; get; }
        }

        public enum AccumulationType
        {
            Monthly = 126840000,
            AnnualCalendar = 126840001,
            AnnualAnniversary = 126840002
        }

        public enum ValidationThresholdType
        {
            VehicleCount = 126840000,
            RevenuePercentage = 126840001,
            ValidationAmount = 126840002
        }

        public class TierVo
        {
            public decimal? SharePercentage { get; set; }
            public decimal? Amount { get; set; }
            public decimal? EscalatorValue { get; set; }
        }

        public class BellServiceFeeVo
        {
            public BellServiceFeeVo()
            {
                BellServices = new List<BellServiceVo>();
            }
            public bool Enabled { get; set; }
            public List<BellServiceVo> BellServices { get; set; }
        }

        public class BellServiceVo
        {
            public Guid? Id { get; set; }
            public int? InvoiceGroup { get; set; }
        }

        public class MidMonthAdvanceVo
        {
            public MidMonthAdvanceVo()
            {
                MidMonthAdvances = new List<MidMonthVo>();
            }
            public bool Enabled { get; set; }
            public List<MidMonthVo> MidMonthAdvances { get; set; }
        }

        public class MidMonthVo
        {
            public Guid? Id { get; set; }
            public decimal? Amount { get; set; }
            public LineTitleType? LineTitle { get; set; }
            public int? InvoiceGroup { get; set; }
        }

        public enum LineTitleType
        {
            MidMonthBilling = 126840000,
            PreBill = 126840001
        }

        public class DepositedRevenueVo
        {
            public DepositedRevenueVo()
            {
                DepositData = new List<DepositDataVo>();
            }
            public bool Enabled { get; set; }
            public List<DepositDataVo> DepositData { get; set; }
        }

        public class DepositDataVo
        {
            public Guid? Id { get; set; }
            public bool? TowneParkResponsibleForParkingTax { get; set; }
            public bool? DepositedRevenueEnabled { get; set; }
            public int? InvoiceGroup { get; set; }
        }

        public class BillableAccountVo
        {
            public BillableAccountVo()
            {
                BillableAccountsData = new List<BillableAccountDataVo>();
            }
            public bool Enabled { get; set; }
            public List<BillableAccountDataVo> BillableAccountsData { get; set; }
        }

        public class BillableAccountDataVo
        {
            public Guid? Id { get; set; }
            public string? PayrollAccountsData { get; set; }
            public int? PayrollAccountsInvoiceGroup { get; set; }
            public string? PayrollAccountsLineTitle { get; set; }
            public bool? PayrollTaxesEnabled { get; set; }
            public PayrollTaxesBillingType? PayrollTaxesBillingType { get; set; }
            public string? PayrollTaxesLineTitle { get; set; }
            public decimal? PayrollTaxesPercentage { get; set; }
            public decimal? PayrollSupportAmount { get; set; }
            public PayrollSupportBillingType? PayrollSupportBillingType { get; set; }
            public bool? PayrollSupportEnabled { get; set; }
            public string? PayrollSupportLineTitle { get; set; }
            public PayrollSupportPayrollType? PayrollSupportPayrollType { get; set; }
            public string? PayrollExpenseAccountsData { get; set; }
            public int? PayrollExpenseAccountsInvoiceGroup { get; set; }
            public string? PayrollExpenseAccountsLineTitle { get; set; }
            public decimal? AdditionalPayrollAmount { get; set; }
            public bool? PayrollTaxesEscalatorEnable { get; set; }
            public Month? PayrollTaxesEscalatorMonth { get; set; }
            public decimal? PayrollTaxesEscalatorvalue { get; set;}
            public EscalatorType? PayrollTaxesEscalatorType { get; set; }


        }

        public enum PayrollTaxesBillingType
        {
            Actual = 126840000,
            Percentage = 126840001
        }

        public enum PayrollSupportBillingType
        {
            Fixed = 126840000,
            Percentage = 126840001
        }

        public enum PayrollSupportPayrollType
        {
            Billable = 126840000,
            Total = 126840001
        }

        public class ManagementAgreementVo
        {
            public ManagementAgreementVo()
            {
               ManagementFees = new List<ManagementFeeVo>();
            }
            public bool Enabled { get; set; }
            public List<ManagementFeeVo> ManagementFees { get; set; }
        }

        public class JobCodeVo
        {
            public string? Code { get; set; }
            public string? Description { get; set; }
            public decimal? StandardRate { get; set; }
            public decimal? OvertimeRate { get; set; }
            public decimal? StandardRateEscalatorValue { get; set; }
            public decimal? OvertimeRateEscalatorValue { get; set; }
        }

        public class ManagementFeeVo
        {
            public Guid? Id { get; set; }
            public int? InvoiceGroup { get; set; }
            public ManagementAgreementType? ManagementAgreementType { get; set; }
            public bool? ManagementFeeEscalatorEnabled { get; set; }
            public Month? ManagementFeeEscalatorMonth { get; set; }
            public EscalatorType? ManagementFeeEscalatorType { get; set; }
            public decimal? ManagementFeeEscalatorValue { get; set; }
            public decimal? FixedFeeAmount { get; set; }
            public List<JobCodeVo>? PerLaborHourJobCodeData { get; set; }
            public string? LaborHourJobCode { get; set; }
            public decimal? LaborHourRate { get; set; }
            public decimal? LaborHourOvertimeRate { get; set; }
            public decimal? RevenuePercentageAmount { get; set; }
            public decimal? InsuranceAdditionalPercentage { get; set; }
            public bool? InsuranceEnabled { get; set; }
            public decimal? InsuranceFixedFeeAmount { get; set; }
            public string? InsuranceLineTitle { get; set; }
            public InsuranceType? InsuranceType { get; set; }
            public decimal? ClaimsCapAmount { get; set; }
            public bool? ClaimsEnabled { get; set; }
            public string? ClaimsLineTitle { get; set; }
            public ClaimType? ClaimsType { get; set; }
            public ProfitShareAccumulationType? ProfitShareAccumulationType { get; set; }
            public bool? ProfitShareEnabled { get; set; }
            public List<TierVo>? ProfitShareTierData { get; set; }
            public bool? ProfitShareEscalatorEnabled { get; set; }
            public Month? ProfitShareEscalatorMonth { get; set; }
            public EscalatorType? ProfitShareEscalatorType { get; set; }
            public decimal? ValidationThresholdAmount { get; set; }
            public bool? ValidationThresholdEnabled { get; set; }
            public ManagementAgreementValidationType? ValidationThresholdType { get; set; }
            public bool? NonGlBillableExpensesEnabled { get; set; }
            public List<NonGlBillableExpenseVo>? NonGlBillableExpenses { get; set; }
        }
        // new update
        public class NonGlBillableExpenseVo
        {
            public string? NonGLExpenseType { get; set; }
            public string? ExpensePayrollType { get; set; }
            public decimal? ExpenseAmount { get; set; }
            public string? ExpenseTitle { get; set; }
            public DateTime? FinalPeriodBilled { get; set; }
            public Guid Id { get; set; }
            public int? SequenceNumber { get; set; }
        }
        public enum ManagementAgreementType
        {
            FixedFee = 126840000,
            PerLaborHour = 126840001,
            RevenuePercentage = 126840002
        }

        public enum InsuranceType
        {
            BasedOnBillableAccounts = 126840000,
            FixedFee = 126840001
        }

        public enum ClaimType
        {
            PerClaim = 126840000,
            AnnualCalendar = 126840001,
            AnnualAnniversary = 126840002
        }

        public enum ProfitShareAccumulationType
        {
            Monthly = 126840000,
            AnnualCalendar = 126840001,
            AnnualAnniversary = 126840002
        }

        public enum ManagementAgreementValidationType
        {
            VehicleCount = 126840000,
            RevenuePercentage = 126840001,
            ValidationAmount = 126840002
        }

        public enum NonGlExpenseType
        {
            FixedAmount = 126840000,
            Payroll = 126840001,
            Revenue = 126840002
        }

        public enum NonGlPayrollType
        {
            Billable = 126840000,
            Total = 126840001
        }

        public enum EscalatorType
        {
            Percentage = 126840000,
            FixedAmount = 126840001
        }
    }
}
