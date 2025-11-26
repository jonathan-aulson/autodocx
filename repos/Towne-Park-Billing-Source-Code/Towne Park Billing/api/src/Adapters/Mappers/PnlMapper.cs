using System.Collections.Generic;
using System.Linq;
using api.Models.Dto;
using api.Models.Vo;
using Riok.Mapperly.Abstractions;
using System;
using System.Text.Json;

namespace api.Adapters.Mappers
{
    [Mapper]
    public static partial class PnlMapper
    {
        private static List<SiteMonthlyRevenueDetailDto> GetSiteMonthlyRevenueDetails(
            string prop,
            List<PnlBySiteVo> sites,
            int monthKey,
            Func<PnlBySiteVo, List<PnlMonthDetailVo>> getMonthDetails)
        {
            Func<PnlMonthDetailVo, decimal> valueSelector = prop switch
            {
                nameof(PnlMonthDetailVo.ExternalRevenue) => (PnlMonthDetailVo vo) => vo.ExternalRevenue,
                nameof(PnlMonthDetailVo.InternalRevenue) => (PnlMonthDetailVo vo) => vo.InternalRevenue,
                nameof(PnlMonthDetailVo.Pteb) => (PnlMonthDetailVo vo) => vo.Pteb,
                nameof(PnlMonthDetailVo.Payroll) => (PnlMonthDetailVo vo) => vo.Payroll,
                nameof(PnlMonthDetailVo.OtherExpense) => (PnlMonthDetailVo vo) => vo.OtherExpense,
                nameof(PnlMonthDetailVo.Insurance) => (PnlMonthDetailVo vo) => vo.Insurance,
                _ => (PnlMonthDetailVo vo) => 0m
            };

            return sites.Select(site =>
            {
                var siteMonth = getMonthDetails(site).FirstOrDefault(x => (x.MonthNum != 0 ? x.MonthNum : int.Parse(x.Period.Substring(4, 2))) == monthKey);
                return new SiteMonthlyRevenueDetailDto
                {
                    SiteId = site.SiteNumber,
                    Value = siteMonth != null ? valueSelector(siteMonth) : 0m,
                    ExternalRevenueBreakdown = siteMonth != null
                        ? new ExternalRevenueBreakdownDto
                        {
                            CalculatedTotalExternalRevenue = siteMonth.ExternalRevenue,
                            BudgetTotalExternalRevenue = siteMonth.ExternalRevenue,
                            LastActualRevenueDate = siteMonth.LastActualRevenueDate
                        }

                        : null,
                    InternalRevenueBreakdown = siteMonth != null
                        ? new InternalRevenueBreakdownDto
                        {
                            CalculatedTotalInternalRevenue = siteMonth.InternalRevenue,
                            PerOccupiedRoom = siteMonth.OccupiedRooms > 0
                                ? new PerOccupiedRoomInternalRevenueDto
                                {
                                    BudgetRooms = siteMonth.OccupiedRooms,
                                    ForecastedRooms = siteMonth.OccupiedRooms,
                                }
                                : null,
                            PerLaborHour = siteMonth.Payroll > 0
                                ? new PerLaborHourInternalRevenueDto
                                {
                                    Total = siteMonth.Payroll,
                                }
                                : null,
                        }
                        : null,
                    PayrollBreakdown = siteMonth != null
                        ? new PayrollBreakdownDto
                        {
                            TotalPayroll = siteMonth.Payroll,
                            ActualPayrollLastDate = siteMonth.ActualPayrollLastDate
                        }
                        : null
                };
            }).ToList();
        }

        public static PnlResponseDto PnlVoListToDto(List<(int Year, PnlBySiteListVo Vo)> allPnlVos)
        {
            if (allPnlVos == null || !allPnlVos.Any())
            {
                return new PnlResponseDto();
            }
            var (year, pnlBySiteListVo) = allPnlVos.First();

            var allBudget = pnlBySiteListVo.PnlBySite
                .SelectMany(site => site.Pnl.Budget)
                .ToList();

            var allActual = pnlBySiteListVo.PnlBySite
                .SelectMany(site => site.Pnl.Actual)
                .ToList();

            var pnlVo = new PnlVo
            {
                Budget = allBudget,
                Actual = allActual
            };

            var response = new PnlResponseDto { Year = year };

            var propertyNames = new[]
            {
                nameof(PnlMonthDetailVo.ExternalRevenue),
                nameof(PnlMonthDetailVo.InternalRevenue),
                nameof(PnlMonthDetailVo.Payroll),
                nameof(PnlMonthDetailVo.Claims),
                nameof(PnlMonthDetailVo.ParkingRents),
                nameof(PnlMonthDetailVo.OtherExpense),
                nameof(PnlMonthDetailVo.Pteb),
                nameof(PnlMonthDetailVo.Insurance),
                nameof(PnlMonthDetailVo.OccupiedRooms)
            };
            decimal GetValue(PnlMonthDetailVo vo, string prop) => prop switch
            {
                nameof(PnlMonthDetailVo.ExternalRevenue) => vo.ExternalRevenue,
                nameof(PnlMonthDetailVo.InternalRevenue) => Math.Abs(vo.InternalRevenue),
                nameof(PnlMonthDetailVo.Payroll) => vo.Payroll,
                nameof(PnlMonthDetailVo.Claims) => vo.Claims,
                nameof(PnlMonthDetailVo.ParkingRents) => vo.ParkingRents,
                nameof(PnlMonthDetailVo.OtherExpense) => vo.OtherExpense,
                nameof(PnlMonthDetailVo.Pteb) => Math.Abs(vo.Pteb),
                nameof(PnlMonthDetailVo.Insurance) => Math.Abs(vo.Insurance),
                nameof(PnlMonthDetailVo.OccupiedRooms) => vo.OccupiedRooms,
                _ => 0m
            };

            Func<PnlMonthDetailVo, int> getMonth = vo => vo.MonthNum != 0 ? vo.MonthNum : int.Parse(vo.Period.Substring(4, 2));

            response.BudgetRows = propertyNames.Select(prop => new PnlRowDto
            {
                ColumnName = prop,
                MonthlyValues = pnlVo.Budget
                    .GroupBy(b => getMonth(b))
                    .OrderBy(g => g.Key)
                    .Select(g =>
                    {
                        var month = g.Key - 1;
                        var value = g.Sum(x => GetValue(x, prop));
                        List<SiteMonthlyRevenueDetailDto>? siteDetails = null;

                        // Populate site breakdown for columns needed by calculators (include Insurance for non-MA rate derivation)
                        if (prop == nameof(PnlMonthDetailVo.ExternalRevenue) || 
                            prop == nameof(PnlMonthDetailVo.InternalRevenue) ||
                            prop == nameof(PnlMonthDetailVo.Pteb) ||
                            prop == nameof(PnlMonthDetailVo.Payroll) ||
                            prop == nameof(PnlMonthDetailVo.OtherExpense) ||
                            prop == nameof(PnlMonthDetailVo.Insurance))
                        {
                            siteDetails = GetSiteMonthlyRevenueDetails(
                                prop,
                                pnlBySiteListVo.PnlBySite,
                                g.Key,
                                site => site.Pnl.Budget
                            );
                        }

                        return new MonthValueDto
                        {
                            Month = month,
                            Value = value,
                            SiteDetails = siteDetails
                        };
                    })
                    .ToList(),
                Total = pnlVo.Budget.Sum(x => GetValue(x, prop))
            }).ToList();

            response.ActualRows = propertyNames.Select(prop => new PnlRowDto
            {
                ColumnName = prop,
                MonthlyValues = pnlVo.Actual
                    .GroupBy(a => getMonth(a))
                    .OrderBy(g => g.Key)
                    .Select(g =>
                    {
                        var month = g.Key - 1;
                        var value = g.Sum(x => GetValue(x, prop));
                        List<SiteMonthlyRevenueDetailDto>? siteDetails = null;

                        // Populate site breakdown for columns needed by calculators (include Insurance for consistency)
                        if (prop == nameof(PnlMonthDetailVo.ExternalRevenue) || 
                            prop == nameof(PnlMonthDetailVo.InternalRevenue) ||
                            prop == nameof(PnlMonthDetailVo.Pteb) ||
                            prop == nameof(PnlMonthDetailVo.Payroll) ||
                            prop == nameof(PnlMonthDetailVo.OtherExpense) ||
                            prop == nameof(PnlMonthDetailVo.Insurance))
                        {
                            siteDetails = GetSiteMonthlyRevenueDetails(
                                prop,
                                pnlBySiteListVo.PnlBySite,
                                g.Key,
                                site => site.Pnl.Actual
                            );
                        }

                        return new MonthValueDto
                        {
                            Month = month,
                            Value = value,
                            SiteDetails = siteDetails
                        };
                    })
                    .ToList(),
                Total = pnlVo.Actual.Sum(x => GetValue(x, prop))
            }).ToList();

            // Create a lookup for budget occupied rooms by month
            var budgetOccupiedRoomsByMonth = pnlVo.Budget
                .GroupBy(b => getMonth(b))
                .ToDictionary(g => g.Key, g => g.Sum(x => GetValue(x, nameof(PnlMonthDetailVo.OccupiedRooms))));

            response.ForecastRows = response.BudgetRows.Select(budgetRow =>
                new PnlRowDto // Manual deep clone to ensure no shared references with BudgetRows
                {
                    ColumnName = budgetRow.ColumnName,
                    Total = budgetRow.Total,
                    // PercentOfInternalRevenue is typically calculated later by PnlService if needed
                    PercentOfInternalRevenue = budgetRow.PercentOfInternalRevenue,
                    MonthlyValues = budgetRow.MonthlyValues.Select(mv =>
                    {
                        // Get budget occupied rooms for this month
                        var budgetRoomsForMonth = budgetOccupiedRoomsByMonth.TryGetValue(mv.Month + 1, out var rooms) ? rooms : 0m;

                        return new MonthValueDto
                        {
                            Month = mv.Month,
                            Value = mv.Value,
                            // Ensure these are null or empty as PnlService will populate them for ForecastRows.
                            // PnlService expects SiteDetails to be at least an empty list if it's going to add to it.
                            SiteDetails = new List<SiteMonthlyRevenueDetailDto>(), // Initialize as empty list
                            InternalRevenueBreakdown = new InternalRevenueBreakdownDto
                            {
                                PerOccupiedRoom = new PerOccupiedRoomInternalRevenueDto
                                {
                                    BudgetRooms = budgetRoomsForMonth,
                                    ForecastedRooms = 0m // Initialize forecast rooms to 0, will be populated by PnlService from Dataverse
                                }
                            }
                        };
                    }).ToList()
                }).ToList();

            // Variance rows are calculated in PnlService after forecast rows are populated
            response.VarianceRows = new List<PnlVarianceRowDto>();

            return response;
        }
    }
}
