using api.Config;
using api.Models.Common;
using api.Models.Dto;
using api.Models.Vo;
using api.Services;
using api.Usecases;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using TownePark;

namespace api.Data.Impl
{
    public class OtherExpenseRepository : IOtherExpenseRepository
    {
        private readonly IDataverseService _dataverseService;
        private readonly IMonthRangeGenerator _monthRangeGenerator;

        public OtherExpenseRepository(IDataverseService dataverseService, IMonthRangeGenerator monthRangeGenerator)
        {
            _dataverseService = dataverseService;
            _monthRangeGenerator = monthRangeGenerator;
        }
    
        public IEnumerable<bs_OtherExpenseDetail>? GetOtherExpenseDetail(Guid siteId, string billingPeriod)
        {
            var serviceClient = _dataverseService.GetServiceClient();
            var months = _monthRangeGenerator.GenerateMonthRange(billingPeriod, 12);

            var query = new QueryExpression(bs_OtherExpenseDetail.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(
                    bs_OtherExpenseDetail.Fields.bs_OtherExpenseDetailId,
                    bs_OtherExpenseDetail.Fields.bs_EmployeeRelations,
                    bs_OtherExpenseDetail.Fields.bs_FuelVehicles,
                    bs_OtherExpenseDetail.Fields.bs_LossAndDamageClaims,
                    bs_OtherExpenseDetail.Fields.bs_OfficeSupplies,
                    bs_OtherExpenseDetail.Fields.bs_OutsideServices,
                    bs_OtherExpenseDetail.Fields.bs_RentsParking,
                    bs_OtherExpenseDetail.Fields.bs_RepairsAndMaintenance,
                    bs_OtherExpenseDetail.Fields.bs_RepairsAndMaintenanceVehicle,
                    bs_OtherExpenseDetail.Fields.bs_Signage,
                    bs_OtherExpenseDetail.Fields.bs_SuppliesAndEquipment,
                    bs_OtherExpenseDetail.Fields.bs_TicketsAndPrintedMaterial,
                    bs_OtherExpenseDetail.Fields.bs_Uniforms,
                    bs_OtherExpenseDetail.Fields.bs_MonthYear,
                    bs_OtherExpenseDetail.Fields.bs_MiscOtherExpenses,
                    bs_OtherExpenseDetail.Fields.bs_TotalOtherExpenses,
                    bs_OtherExpenseDetail.Fields.bs_CustomerSiteFK
                ),
                Criteria = new FilterExpression(LogicalOperator.And)
            };

            query.Criteria.AddCondition(
                bs_OtherExpenseDetail.Fields.bs_CustomerSiteFK,
                ConditionOperator.Equal,
                siteId);
            query.Criteria.AddCondition(
                bs_OtherExpenseDetail.Fields.bs_MonthYear,
                ConditionOperator.In,
                months.ToArray());

            var result = serviceClient.RetrieveMultiple(query);

            if (result.Entities.Count == 0)
                return null;

            var otherExpenseDetails = result.Entities
                .Select(e => e.ToEntity<bs_OtherExpenseDetail>())
                .ToList();

            return otherExpenseDetails;
        }

        public void UpdateOtherRevenueDetails(List<bs_OtherExpenseDetail> details)
        {
            var serviceClient = _dataverseService.GetServiceClient();

            foreach (var detail in details)
            {
                if (detail.Id == Guid.Empty)
                {
                    // Create a new record
                    detail.Id = Guid.NewGuid();
                    serviceClient.Create(detail);
                }
                else
                {
                    // Update the existing record
                    serviceClient.Update(detail);
                }
            }
        }

        public decimal GetMonthlyAccountTotal(Guid siteId, int year, int monthOneBased, string accountFieldName)
        {
            try
            {
                var serviceClient = _dataverseService.GetServiceClient();
                
                // Format month-year as "YYYY-MM" to match bs_MonthYear field
                var monthYear = $"{year:D4}-{monthOneBased:D2}";

                var query = new QueryExpression(bs_OtherExpenseDetail.EntityLogicalName)
                {
                    ColumnSet = new ColumnSet(accountFieldName),
                    Criteria = new FilterExpression(LogicalOperator.And)
                };

                query.Criteria.AddCondition(
                    bs_OtherExpenseDetail.Fields.bs_CustomerSiteFK,
                    ConditionOperator.Equal,
                    siteId);
                query.Criteria.AddCondition(
                    bs_OtherExpenseDetail.Fields.bs_MonthYear,
                    ConditionOperator.Equal,
                    monthYear);

                var result = serviceClient.RetrieveMultiple(query);

                if (result.Entities.Count > 0)
                {
                    var otherExpenseDetail = result.Entities[0].ToEntity<bs_OtherExpenseDetail>();
                    var fieldValue = otherExpenseDetail.GetAttributeValue<decimal?>(accountFieldName);
                    return fieldValue ?? 0m;
                }

                return 0m;
            }
            catch (Exception)
            {
                // Log the exception if logging is available
                // For now, return 0 to prevent calculation failures
                return 0m;
            }
        }

        public async Task<List<OtherExpenseDetailVo>> GetActualData(string siteId, string billingPeriod)
        {
            var flowUrl = Configuration.getEDWDataAPIEndpoint();
            if (string.IsNullOrEmpty(flowUrl))
            {
                throw new Exception("Power Automate flow URL is not configured.");
            }

            var token = await Configuration.getAccessTokenAsync(true);

            string[] billingPeriodParts = billingPeriod.Split('-');
            string year = billingPeriodParts[0];
            string month = int.Parse(billingPeriodParts[1]).ToString();

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var payload = new
                {
                    storedProcedureId = (int)StoredProcedureId.Other_Expenses_Actual,
                    storedProcedureParameters = new
                    {
                        COST_CENTER = siteId,
                        YEAR = year,
                        MONTH = month
                    }
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(payload),
                    System.Text.Encoding.UTF8,
                    "application/json");

                var response = await httpClient.PostAsync(flowUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();

                    try
                    {
                        // First deserialize to OtherExpenseDto
                        var otherExpense = JsonConvert.DeserializeObject<OtherExpenseDto>(jsonResponse);

                        // Then map the DTO objects to VO objects (assuming you have a mapping method)
                        if (otherExpense?.ActualData != null)
                        {
                            // If you need to convert from DTO to VO, do it here
                            var details = MapToVoList(otherExpense.ActualData);
                            return details;
                        }
                        return new List<OtherExpenseDetailVo>();
                    }
                    catch (Exception ex)
                    {
                        // Log the exception and raw response for debugging
                        Console.WriteLine($"Deserialization error: {ex.Message}");
                        Console.WriteLine($"Raw JSON: {jsonResponse}");
                        throw new Exception($"Failed to deserialize response: {ex.Message}", ex);
                    }
                }
                else
                {
                    throw new Exception($"Failed to retrieve data: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
        }

        // Helper method to map DTOs to VOs (implement based on your mapping approach)
        private List<OtherExpenseDetailVo> MapToVoList(List<OtherExpenseDetailDto> dtoList)
        {
            // If OtherExpenseDetailVo and OtherExpenseDetailDto have the same structure:
            return dtoList.Select(dto => new OtherExpenseDetailVo
            {
                Id = dto.Id,
                MonthYear = dto.MonthYear,
                EmployeeRelations = dto.EmployeeRelations,
                FuelVehicles = dto.FuelVehicles,
                LossAndDamageClaims = dto.LossAndDamageClaims,
                OfficeSupplies = dto.OfficeSupplies,
                OutsideServices = dto.OutsideServices,
                RentsParking = dto.RentsParking,
                RepairsAndMaintenance = dto.RepairsAndMaintenance,
                RepairsAndMaintenanceVehicle = dto.RepairsAndMaintenanceVehicle,
                Signage = dto.Signage,
                SuppliesAndEquipment = dto.SuppliesAndEquipment,
                TicketsAndPrintedMaterial = dto.TicketsAndPrintedMaterial,
                Uniforms = dto.Uniforms,
                MiscOtherExpenses = dto.MiscOtherExpenses,
                TotalOtherExpenses = dto.TotalOtherExpenses
            }).ToList();
        }


        public async Task<List<OtherExpenseDetailVo>> GetBudgetData(string siteId, string billingPeriod)
        {
            var flowUrl = Configuration.getEDWDataAPIEndpoint();
            if (string.IsNullOrEmpty(flowUrl))
            {
                throw new Exception("Power Automate flow URL is not configured.");
            }
            var token = await Configuration.getAccessTokenAsync(true);
            string[] billingPeriodParts = billingPeriod.Split('-');
            string year = billingPeriodParts[0];
            string month = int.Parse(billingPeriodParts[1]).ToString();
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var payload = new
                {
                    storedProcedureId = (int)StoredProcedureId.Other_Expenses_Budget,
                    storedProcedureParameters = new
                    {
                        COST_CENTER = siteId,
                        YEAR = year,
                        MONTH = month
                    }
                };
                var content = new StringContent(
                    JsonConvert.SerializeObject(payload),
                    System.Text.Encoding.UTF8,
                    "application/json");
                var response = await httpClient.PostAsync(flowUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    try
                    {
                        var otherExpense = JsonConvert.DeserializeObject<OtherExpenseDto>(jsonResponse);
                        if (otherExpense?.BudgetData != null)
                        {
                            var details = MapToVoList(otherExpense.BudgetData);
                            return details;
                        }
                        return new List<OtherExpenseDetailVo>();
                    }
                    catch (Exception ex)
                    {
                        // Log the exception and raw response for debugging
                        Console.WriteLine($"Deserialization error: {ex.Message}");
                        Console.WriteLine($"Raw JSON: {jsonResponse}");
                        throw new Exception($"Failed to deserialize response: {ex.Message}", ex);
                    }
                }
                else
                {
                    throw new Exception($"Failed to retrieve data: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
        }

    }
}
