using api.Models.Dto;
using api.Models.Vo;
using Microsoft.Xrm.Sdk;
using Riok.Mapperly.Abstractions;
using System.Security;
using TownePark;
using System.Collections.Generic;
using System.Linq;

namespace api.Adapters.Mappers
{
    [Mapper]
    public static partial class PayrollMapper
    {
        [MapProperty(nameof(bs_Payroll.Fields.bs_PayrollId), nameof(PayrollVo.Id))]
        [MapProperty(nameof(bs_Payroll.Fields.bs_Name), nameof(PayrollVo.Name))]
        [MapProperty(nameof(bs_Payroll.Fields.bs_Period), nameof(PayrollVo.BillingPeriod))]
        [MapProperty(nameof(bs_Payroll.Fields.bs_PayrollForecastMode), nameof(PayrollVo.PayrollForecastMode))]
        [MapperIgnoreTarget(nameof(PayrollVo.ForecastPayroll))]
        [MapperIgnoreTarget(nameof(PayrollVo.BudgetPayroll))]
        [MapperIgnoreTarget(nameof(PayrollVo.ActualPayroll))]
        [MapperIgnoreTarget(nameof(PayrollVo.ScheduledPayroll))]
        private static partial PayrollVo MapPayrollModelToVo(bs_Payroll model);

        public static PayrollVo? PayrollModelToVo(bs_Payroll? model)
        {
            if (model == null)
                return null;

            var vo = MapPayrollModelToVo(model);

            vo.CustomerSiteId = model.bs_CustomerSiteFK.Id;
            vo.SiteNumber = model.bs_CustomerSiteFK.Name;

            // Map the PayrollForecastMode from the model
            vo.PayrollForecastMode = model.bs_PayrollForecastMode.HasValue 
                ? (PayrollForecastModeType)model.bs_PayrollForecastMode.Value
                : PayrollForecastModeType.Group; // Default fallback

            // ForecastPayroll, BudgetPayroll, etc. are set in PayrollService aggregation logic

            return vo;
        }

        // --- Custom mappings for VO to DTO conversion ---
        [MapperIgnoreTarget(nameof(PayrollDto.PayrollForecastMode))]
        private static partial PayrollDto MapPayrollVoToDto(PayrollVo vo);

        public static PayrollDto PayrollVoToDto(PayrollVo vo)
        {
            if (vo == null)
                throw new ArgumentNullException(nameof(vo), "PayrollVo cannot be null.");
            var dto = MapPayrollVoToDto(vo);

            // Convert enum to string, default to Group if null
            dto.PayrollForecastMode = vo.PayrollForecastMode.ToString();

            return dto;
        }

        [MapperIgnoreTarget(nameof(PayrollVo.PayrollForecastMode))]
        [MapperIgnoreTarget(nameof(PayrollVo.ForecastPayroll))]
        [MapperIgnoreTarget(nameof(PayrollVo.BudgetPayroll))]
        [MapperIgnoreTarget(nameof(PayrollVo.ActualPayroll))]
        [MapperIgnoreTarget(nameof(PayrollVo.ScheduledPayroll))]
        private static partial PayrollVo MapPayrollDtoToVo(PayrollDto dto);

        public static PayrollVo PayrollDtoToVo(PayrollDto dto)
        {
            var vo = MapPayrollDtoToVo(dto);
            
            // Convert string to enum
            vo.PayrollForecastMode = Enum.TryParse<PayrollForecastModeType>(dto.PayrollForecastMode, out var mode) 
                ? mode 
                : PayrollForecastModeType.Group; // Default fallback
            
            // Manually convert nested collections
            vo.ForecastPayroll = JobGroupForecastDtoListToVoList(dto.ForecastPayroll);
            vo.BudgetPayroll = JobGroupBudgetDtoListToVoList(dto.BudgetPayroll);
            vo.ActualPayroll = JobGroupActualDtoListToVoList(dto.ActualPayroll);
            vo.ScheduledPayroll = JobGroupScheduledDtoListToVoList(dto.ScheduledPayroll);
            
            return vo;
        }

        // Forecast mappings
        public static partial JobGroupForecastDto JobGroupForecastVoToDto(JobGroupForecastVo vo);
        public static partial JobGroupForecastVo JobGroupForecastDtoToVo(JobGroupForecastDto dto);
        public static partial JobCodeForecastDto JobCodeForecastVoToDto(JobCodeForecastVo vo);
        public static partial JobCodeForecastVo JobCodeForecastDtoToVo(JobCodeForecastDto dto);

        // Budget mappings
        public static partial JobGroupBudgetDto JobGroupBudgetVoToDto(JobGroupBudgetVo vo);
        public static partial JobGroupBudgetVo JobGroupBudgetDtoToVo(JobGroupBudgetDto dto);
        public static partial JobCodeBudgetDto JobCodeBudgetVoToDto(JobCodeBudgetVo vo);
        public static partial JobCodeBudgetVo JobCodeBudgetDtoToVo(JobCodeBudgetDto dto);

        // Actual mappings
        public static partial JobGroupActualDto JobGroupActualVoToDto(JobGroupActualVo vo);
        public static partial JobGroupActualVo JobGroupActualDtoToVo(JobGroupActualDto dto);
        public static partial JobCodeActualDto JobCodeActualVoToDto(JobCodeActualVo vo);
        public static partial JobCodeActualVo JobCodeActualDtoToVo(JobCodeActualDto dto);

        // Scheduled mappings
        public static partial JobGroupScheduledDto JobGroupScheduledVoToDto(JobGroupScheduledVo vo);
        public static partial JobGroupScheduledVo JobGroupScheduledDtoToVo(JobGroupScheduledDto dto);
        public static partial JobCodeScheduledDto JobCodeScheduledVoToDto(JobCodeScheduledVo vo);
        public static partial JobCodeScheduledVo JobCodeScheduledDtoToVo(JobCodeScheduledDto dto);

        // List conversion methods
        public static List<JobGroupForecastVo>? JobGroupForecastDtoListToVoList(List<JobGroupForecastDto>? dtos)
        {
            return dtos?.Select(JobGroupForecastDtoToVo).ToList();
        }

        public static List<JobGroupBudgetVo>? JobGroupBudgetDtoListToVoList(List<JobGroupBudgetDto>? dtos)
        {
            return dtos?.Select(JobGroupBudgetDtoToVo).ToList();
        }

        public static List<JobGroupActualVo>? JobGroupActualDtoListToVoList(List<JobGroupActualDto>? dtos)
        {
            return dtos?.Select(JobGroupActualDtoToVo).ToList();
        }

        public static List<JobGroupScheduledVo>? JobGroupScheduledDtoListToVoList(List<JobGroupScheduledDto>? dtos)
        {
            return dtos?.Select(JobGroupScheduledDtoToVo).ToList();
        }

        [MapperIgnoreTarget(nameof(bs_Payroll.Fields.bs_PayrollId))] // Don't auto-map ID - handle manually for new records
        [MapperIgnoreTarget(nameof(bs_Payroll.Id))] // Also ignore the base Id property
        [MapProperty(nameof(PayrollVo.Name), nameof(bs_Payroll.Fields.bs_Name))]
        [MapProperty(nameof(PayrollVo.BillingPeriod), nameof(bs_Payroll.Fields.bs_Period))]
        [MapProperty(nameof(PayrollVo.PayrollForecastMode), nameof(bs_Payroll.Fields.bs_PayrollForecastMode))]
        private static partial bs_Payroll MapPayrollVoToModelSimple(PayrollVo vo);

        /// <summary>
        /// Maps a PayrollVo (with nested JobGroupForecastVo/JobCodeForecastVo) to a bs_Payroll,
        /// flattening the nested structure into bs_PayrollDetail records.
        /// Always expects job codes to be provided within job groups.
        /// </summary>
        public static bs_Payroll PayrollVoToModel(PayrollVo vo)
        {
            var model = MapPayrollVoToModelSimple(vo);
            model.bs_CustomerSiteFK = new EntityReference(bs_CustomerSite.EntityLogicalName, vo.CustomerSiteId);
            
            // Set the PayrollForecastMode from the VO
            model.bs_PayrollForecastMode = (bs_payrollforecastmodetype)vo.PayrollForecastMode;

            // Handle ID for payroll record
            if (vo.Id != Guid.Empty)
            {
                // Existing record - set the ID
                model.bs_PayrollId = vo.Id; // This will automatically set model.Id via the entity property setter
            }
            // For new records (vo.Id == Guid.Empty), don't touch the bs_PayrollId property at all
            // This leaves the entity in its default state where Dataverse can generate the ID

            var details = new List<bs_PayrollDetail>();
            foreach (var jobGroup in vo.ForecastPayroll ?? new List<JobGroupForecastVo>())
            {
                foreach (var jobCode in jobGroup.JobCodes ?? new List<JobCodeForecastVo>())
                {
                    var detail = new bs_PayrollDetail
                    {
                        bs_JobCodeFK = new EntityReference("bs_jobcode", jobCode.JobCodeId),
                        bs_JobGroupFK = new EntityReference("bs_jobgroup", jobGroup.JobGroupId),
                        bs_RegularHours = jobCode.ForecastHours,
                        bs_Date = jobCode.Date?.ToDateTime(TimeOnly.MinValue),
                        bs_ForecastPayrollCost = jobCode.ForecastPayrollCost,
                        bs_ForecastPayrollRevenue = jobCode.ForecastPayrollRevenue,
                        bs_DisplayName = jobCode.DisplayName
                    };

                    // Only set ID for existing records (updates), never for new records (creates)
                    if (jobCode.Id.HasValue && jobCode.Id != Guid.Empty)
                    {
                        // Existing detail - set the ID for update
                        detail.bs_PayrollDetailId = jobCode.Id;
                    }
                    // For new details, don't set any ID property - let Dataverse generate it

                    details.Add(detail);
                }
            }

            model.bs_PayrollDetail_Payroll = details.ToArray();

            return model;
        }
    }
}
