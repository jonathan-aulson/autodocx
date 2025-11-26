using System.Collections.Generic;
using System.Data;
using TownePark.Billing.Api.Adapters.Mappers;
using TownePark.Billing.Api.Models.Common;
using TownePark.Billing.Api.Helpers;

namespace TownePark.Billing.Api.Config
{
    public static class EDWQueryRegistry
    {
        public static readonly EDWQueryDefinition BUDGET_DAILY_DETAIL = new EDWQueryDefinition
        {
            Id = 1,
            NameOrSql = "[dbo].[spBUDGET_DAILY_DETAIL]",
            CommandType = CommandType.StoredProcedure,
            Mapper = results => results.Select(SiteStatisticDetailMapper.MapToSiteStatisticDetailVo).ToList()
        };

        public static readonly EDWQueryDefinition BUDGET_ACTUAL_SUMMARY = new EDWQueryDefinition
        {
            Id = 2,
            NameOrSql = "[dbo].[spBudget_Actual_Summary]",
            CommandType = CommandType.StoredProcedure,
            Mapper = PnlMapper.MapToPnlVo
        };

        public static readonly EDWQueryDefinition RATES_BY_YEAR = new EDWQueryDefinition
        {
            Id = 3,
            NameOrSql = @"
                        SELECT 
                        'BUDGET' AS [TYPE], 
                        [PERIOD],
                        CAST(SUM(CASE WHEN [MAIN_ACCOUNT] = '9411' THEN [BALANCE] ELSE 0 END) /
                              NULLIF(SUM(CASE WHEN [MAIN_ACCOUNT] = '9510' THEN [BALANCE] ELSE 0 END), 0) AS DECIMAL(10,2)) AS [Valet_Daily_Rate],
                        CAST(SUM(CASE WHEN [MAIN_ACCOUNT] = '9412' THEN [BALANCE] ELSE 0 END) /
                              NULLIF(SUM(CASE WHEN [MAIN_ACCOUNT] = '9520' THEN [BALANCE] ELSE 0 END), 0) AS DECIMAL(10,2)) AS [Valet_Overnight_Rate],
                        CAST(SUM(CASE WHEN [MAIN_ACCOUNT] = '9413' THEN [BALANCE] ELSE 0 END) /
                              NULLIF(SUM(CASE WHEN [MAIN_ACCOUNT] = '9530' THEN [BALANCE] ELSE 0 END), 0) AS DECIMAL(10,2)) AS [Valet_Monthly_Rate],
                        CAST(SUM(CASE WHEN [MAIN_ACCOUNT] = '9414' THEN [BALANCE] ELSE 0 END) /
                              NULLIF(SUM(CASE WHEN [MAIN_ACCOUNT] = '9550' THEN [BALANCE] ELSE 0 END), 0) AS DECIMAL(10,2)) AS [Self_Daily_Rate],
                        CAST(SUM(CASE WHEN [MAIN_ACCOUNT] = '9415' THEN [BALANCE] ELSE 0 END) /
                              NULLIF(SUM(CASE WHEN [MAIN_ACCOUNT] = '9560' THEN [BALANCE] ELSE 0 END), 0) AS DECIMAL(10,2)) AS [Self_Overnight_Rate],
                        CAST(SUM(CASE WHEN [MAIN_ACCOUNT] = '9416' THEN [BALANCE] ELSE 0 END) /
                              NULLIF(SUM(CASE WHEN [MAIN_ACCOUNT] = '9570' THEN [BALANCE] ELSE 0 END), 0) AS DECIMAL(10,2)) AS [Self_Monthly_Rate]
                    FROM [dbo].[BUDGET_FINAL]
                    WHERE LEFT([PERIOD], 4) = @YEAR
                      AND [COST_CENTER] = @COST_CENTER
                      AND [MAIN_ACCOUNT] IN ('9411','9510','9412','9520','9413','9530','9414','9550','9415','9560','9416','9570')
                    GROUP BY [PERIOD]

                    UNION ALL

                    SELECT 
                        'ACTUAL' AS [TYPE],  -- Add TYPE column
                        FORMAT(DATEADD(MONTH, DATEDIFF(MONTH, 0, [DATE]), 0), 'yyyyMM') AS [PERIOD],
                        CAST(SUM(CASE WHEN [REVENUE_CODE] IN ('VD1', 'VD2', 'VD3') THEN [NETEXTERNALREVENUE] ELSE 0 END) /
                              NULLIF(SUM(CASE WHEN [REVENUE_CODE] IN ('VD1', 'VD2', 'VD3') THEN [VEHICLECOUNT] ELSE 0 END), 0) AS DECIMAL(10,2)) AS [Valet_Daily_Rate],
                        CAST(SUM(CASE WHEN [REVENUE_CODE] IN ('VO1', 'VO2', 'VO3') THEN [NETEXTERNALREVENUE] ELSE 0 END) /
                              NULLIF(SUM(CASE WHEN [REVENUE_CODE] IN ('VO1', 'VO2', 'VO3') THEN [VEHICLECOUNT] ELSE 0 END), 0) AS DECIMAL(10,2)) AS [Valet_Overnight_Rate],
                        CAST(SUM(CASE WHEN [REVENUE_CODE] IN ('VM1', 'VM2', 'VM3') THEN [NETEXTERNALREVENUE] ELSE 0 END) /
                              NULLIF(SUM(CASE WHEN [REVENUE_CODE] IN ('VM1', 'VM2', 'VM3') THEN [VEHICLECOUNT] ELSE 0 END), 0) AS DECIMAL(10,2)) AS [Valet_Monthly_Rate],
                        CAST(SUM(CASE WHEN [REVENUE_CODE] IN ('SD1', 'SD2', 'SD3') THEN [NETEXTERNALREVENUE] ELSE 0 END) /
                              NULLIF(SUM(CASE WHEN [REVENUE_CODE] IN ('SD1', 'SD2', 'SD3') THEN [VEHICLECOUNT] ELSE 0 END), 0) AS DECIMAL(10,2)) AS [Self_Daily_Rate],
                        CAST(SUM(CASE WHEN [REVENUE_CODE] IN ('SO1', 'SO2', 'SO3') THEN [NETEXTERNALREVENUE] ELSE 0 END) /
                              NULLIF(SUM(CASE WHEN [REVENUE_CODE] IN ('SO1', 'SO2', 'SO3') THEN [VEHICLECOUNT] ELSE 0 END), 0) AS DECIMAL(10,2)) AS [Self_Overnight_Rate],
                        CAST(SUM(CASE WHEN [REVENUE_CODE] IN ('SM1', 'SM2', 'SM3') THEN [NETEXTERNALREVENUE] ELSE 0 END) /
                              NULLIF(SUM(CASE WHEN [REVENUE_CODE] IN ('SM1', 'SM2', 'SM3') THEN [VEHICLECOUNT] ELSE 0 END), 0) AS DECIMAL(10,2)) AS [Self_Monthly_Rate]
                    FROM [dbo].[vwREVENUE_DAILY_DETAIL_INVOICE]
                    WHERE YEAR([DATE]) = @YEAR
                      AND [SITE] = @COST_CENTER
                    GROUP BY FORMAT(DATEADD(MONTH, DATEDIFF(MONTH, 0, [DATE]), 0), 'yyyyMM')

                    ORDER BY [PERIOD], [TYPE]",
            CommandType = CommandType.Text,
            Mapper = ParkingRateMapper.MapToParkingRateDataVo
        };

        public static readonly EDWQueryDefinition BUDGET_ACTUAL_SUMMARY_BY_SITE = new EDWQueryDefinition
        {
            Id = 4,
            NameOrSql = "[dbo].[spBudget_Actual_Summary_BySite]",
            CommandType = CommandType.StoredProcedure,
            Mapper = PnlMapper.MapToPnlBySiteVo
        };

        public static readonly EDWQueryDefinition PAYROLL_BUDGET_BY_SITE = new EDWQueryDefinition
        {
            Id = 5,
            NameOrSql = @"
                  --budget hours and cost for the month
                  SELECT 
                  bd.COST_CENTER,
                  bd.[YEAR],
                  bd.[MONTH],
                  bd.JOB_PROFILE,
                  SUM(CASE WHEN bd.BALANCE_DESC = 'PR Hours' THEN bd.BALANCE ELSE 0 END) AS TOTAL_HOURS,
                  SUM(CASE WHEN bd.BALANCE_DESC = 'Payroll' THEN bd.BALANCE ELSE 0 END) AS TOTAL_COST
                  FROM 
                  BUDGET_DATATAB_PR bd
                  WHERE 
                  bd.BALANCE_DESC IN ('PR Hours', 'Payroll')
                  AND bd.COST_CENTER = @COST_CENTER
                  AND bd.[YEAR] = @YEAR
                  AND bd.[MONTH] = @MONTH
                  GROUP BY 
                  bd.COST_CENTER,
                  bd.[YEAR],
                  bd.[MONTH],
                  bd.JOB_PROFILE
                  ORDER BY 
                  bd.COST_CENTER,
                  bd.[MONTH],
                  bd.JOB_PROFILE
                  ",
            CommandType = CommandType.Text,
            Mapper = PayrollBudgetBySiteMapper.MapToPayrollBudgetBySiteDtoList
        };

        public static readonly EDWQueryDefinition PAYROLL_ACTUALS_BY_SITE = new EDWQueryDefinition
        {
            Id = 6,
            NameOrSql = @"
                  SELECT 
                  ps.TITLE AS JobCode,
                  ps.TOTAL_HOURS AS [Hours],
                  ps.TOTAL_DOLLARS AS [Cost],
                  ps.[DATE] AS [Date]
                  FROM 
                  [TP_LEGION].[dbo].[vwPAYROLL_SUMMARY] ps
                  WHERE 
                  ps.[WORK_LOCATION] = @WORK_LOCATION
                  AND ps.[PAY_TYPE] NOT IN ('DIFFERENTIAL', 'MEAL PREMIUM')
                  AND ps.[DATE] >= DATEFROMPARTS(@Year, @Month, 1)
                  AND ps.[DATE] <= EOMONTH(DATEFROMPARTS(@Year, @Month, 1))
                  ",
            CommandType = CommandType.Text,
            Mapper = PayrollStatisticsMapper.MapToPayrollStatisticsDtoList
        };

        public static readonly EDWQueryDefinition PAYROLL_SCHEDULE_BY_SITE = new EDWQueryDefinition
        {
            Id = 7,
            NameOrSql = @"
                    DECLARE @FirstDayOfMonth INT = DATEPART(DAYOFYEAR, DATEFROMPARTS(@Year, @Month, 1));
                    DECLARE @LastDayOfMonth INT = DATEPART(DAYOFYEAR, EOMONTH(DATEFROMPARTS(@Year, @Month, 1)));

                    SELECT 
                        se.WORK_ROLE as JobCode, 
                        DATEADD(DAY, se.DAY_OF_THE_YEAR - 1, DATEFROMPARTS(se.YEAR, 1, 1)) AS [Date],
                        ROUND( (se.REGULAR_MINUTES + se.OVERTIME_MINUTES) / 60.0, 2) AS [Hours],
                        se.EMPLOYEE_EXTERNAL_ID, 
                        se.LOCATION_EXTERNAL_ID, 
                        se.SCHEDULE_WEEK, 
                        se.YEAR, 
                        se.DAY_OF_THE_YEAR, 
                        se.START_DATE, 
                        se.END_DATE, 
                        se.REGULAR_MINUTES, 
                        se.OVERTIME_MINUTES
                    FROM 
                        [TP_LEGION].[dbo].[SHIFT_ENTITY] se
                    WHERE 
                        se.LOCATION_EXTERNAL_ID = @WORK_LOCATION
                        AND se.YEAR = @Year
                        AND se.DAY_OF_THE_YEAR BETWEEN @FirstDayOfMonth AND @LastDayOfMonth
                  ",
            CommandType = CommandType.Text,
            Mapper = PayrollStatisticsMapper.MapToPayrollStatisticsDtoList
        };

        public static readonly EDWQueryDefinition REVENUE_DAILY_STATS_PIVOTED = new EDWQueryDefinition
        {
            Id = 8,
            NameOrSql = @"
                DECLARE @Today DATE = CAST(GETDATE() AS DATE);
                DECLARE @FirstOfMonth DATE = DATEFROMPARTS(@year, @month, 1);
                DECLARE @EndOfMonth DATE = EOMONTH(@FirstOfMonth);

                DECLARE @EndBound DATE = CASE 
                    WHEN YEAR(@Today) = @year AND MONTH(@Today) = @month THEN @Today
                    ELSE @EndOfMonth
                END;

                DECLARE @LastExternalRevenueDate DATE = (
                    SELECT MAX(CAST([DATE] AS DATE))
                    FROM [TP_EDW].[dbo].[REVENUE_DAILY_DETAIL]
                    WHERE [SITE] = @SITE
                      AND [NETEXTERNALREVENUE] != 0
                      AND CAST([DATE] AS DATE) BETWEEN @FirstOfMonth AND @EndBound
                );

                -- Get revenue data with pivoted categories
                SELECT
                    r.[SITE],
                    r.[DATE],
                    @LastExternalRevenueDate AS EXTERNAL_REVENUE_LAST_DATE,
                    -- Pivoted Vehicle Counts
                    SUM(CASE WHEN r.[REVENUE_CATEGORY] = 'Self Daily' THEN r.[VEHICLECOUNT] ELSE 0 END) AS SelfDaily_Count,
                    SUM(CASE WHEN r.[REVENUE_CATEGORY] = 'Self Monthly' THEN r.[VEHICLECOUNT] ELSE 0 END) AS SelfMonthly_Count,
                    SUM(CASE WHEN r.[REVENUE_CATEGORY] = 'Self Overnight' THEN r.[VEHICLECOUNT] ELSE 0 END) AS SelfOvernight_Count,
                    SUM(CASE WHEN r.[REVENUE_CATEGORY] = 'Valet Daily' THEN r.[VEHICLECOUNT] ELSE 0 END) AS ValetDaily_Count,
                    SUM(CASE WHEN r.[REVENUE_CATEGORY] = 'Valet Monthly' THEN r.[VEHICLECOUNT] ELSE 0 END) AS ValetMonthly_Count,
                    SUM(CASE WHEN r.[REVENUE_CATEGORY] = 'Valet Overnight' THEN r.[VEHICLECOUNT] ELSE 0 END) AS ValetOvernight_Count,

                    -- Self Comps and Valet Comps Vehicle Counts
                    ISNULL(dm.SelfComps_Count, 0) AS SelfComps_Count,
                    ISNULL(dm.ValetComps_Count, 0) AS ValetComps_Count,

                    -- Pivoted Revenue
                    SUM(CASE WHEN r.[REVENUE_CATEGORY] = 'Self Daily' THEN r.[NETEXTERNALREVENUE] ELSE 0 END) AS SelfDaily_Revenue,
                    SUM(CASE WHEN r.[REVENUE_CATEGORY] = 'Self Monthly' THEN r.[NETEXTERNALREVENUE] ELSE 0 END) AS SelfMonthly_Revenue,
                    SUM(CASE WHEN r.[REVENUE_CATEGORY] = 'Self Overnight' THEN r.[NETEXTERNALREVENUE] ELSE 0 END) AS SelfOvernight_Revenue,
                    SUM(CASE WHEN r.[REVENUE_CATEGORY] = 'Valet Daily' THEN r.[NETEXTERNALREVENUE] ELSE 0 END) AS ValetDaily_Revenue,
                    SUM(CASE WHEN r.[REVENUE_CATEGORY] = 'Valet Monthly' THEN r.[NETEXTERNALREVENUE] ELSE 0 END) AS ValetMonthly_Revenue,
                    SUM(CASE WHEN r.[REVENUE_CATEGORY] = 'Valet Overnight' THEN r.[NETEXTERNALREVENUE] ELSE 0 END) AS ValetOvernight_Revenue,

                    -- Adjustments Revenue
                    SUM(CASE WHEN r.[REVENUE_CATEGORY] = 'Adjustments' THEN r.[NETEXTERNALREVENUE] ELSE 0 END) AS Adjustments_Revenue,

                    -- Total for the day
                    SUM(r.[VEHICLECOUNT]) AS Total_VehicleCount,
                    SUM(r.[NETEXTERNALREVENUE]) AS Total_Revenue,

                    -- Include Occupied Rooms
                    ISNULL(dm.OccupiedRooms, 0) AS OccupiedRooms
                FROM
                    [TP_EDW].[dbo].[REVENUE_DAILY_DETAIL] r
                LEFT JOIN (
                    -- COMBINED: Get all needed data from REVENUE_DATAMART_DAILY in single query
                    SELECT
                        [SITE],
                        [DATE],
                        SUM(CASE WHEN [VALUE_TYPE] = 'Other' AND [REVENUE_CATEGORY] = 'Occupied Rooms' 
                                 THEN [VALUE] ELSE 0 END) AS OccupiedRooms,
                        SUM(CASE WHEN [VALUE_TYPE] = 'Vehicles' AND [REVENUE_CATEGORY] = 'Self Comps' 
                                 THEN [VALUE] ELSE 0 END) AS SelfComps_Count,
                        SUM(CASE WHEN [VALUE_TYPE] = 'Vehicles' AND [REVENUE_CATEGORY] = 'Valet Comps' 
                                 THEN [VALUE] ELSE 0 END) AS ValetComps_Count
                    FROM
                        [TP_EDW].[dbo].[REVENUE_DATAMART_DAILY]
                    WHERE
                        [SITE] = @SITE
                        AND (
                            ([VALUE_TYPE] = 'Other' AND [REVENUE_CATEGORY] = 'Occupied Rooms')
                            OR ([VALUE_TYPE] = 'Vehicles' AND [REVENUE_CATEGORY] IN ('Self Comps', 'Valet Comps'))
                        )
                        AND [DATE] >= DATEFROMPARTS(@year, @month, 1)
                        AND [DATE] < CASE 
                            WHEN @month = 12 THEN DATEFROMPARTS(@year + 1, 1, 1)
                            ELSE DATEFROMPARTS(@year, @month + 1, 1)
                        END
                        AND [DATE] <= CAST(GETDATE() AS DATE)
                    GROUP BY
                        [SITE], [DATE]
                ) dm ON r.[SITE] = dm.[SITE] AND r.[DATE] = dm.[DATE]
                WHERE
                    r.[SITE] = @SITE
                    AND r.[DATE] >= DATEFROMPARTS(@year, @month, 1)
                    AND r.[DATE] < CASE 
                        WHEN @month = 12 THEN DATEFROMPARTS(@year + 1, 1, 1)
                        ELSE DATEFROMPARTS(@year, @month + 1, 1)
                    END
                    AND r.[DATE] <= CAST(GETDATE() AS DATE)
                GROUP BY
                    r.[SITE], r.[DATE], dm.OccupiedRooms, dm.SelfComps_Count, dm.ValetComps_Count
                ORDER BY
                    r.[DATE]",
            CommandType = CommandType.Text,
            Mapper = results => SiteStatisticRevenueDetailMapper.MapToSiteStatisticDetailVo(results)
        };

        public static readonly EDWQueryDefinition OTHER_EXPENSES_ACTUAL_DATA = new EDWQueryDefinition
        {
            Id = 9,
            NameOrSql = @"
                SELECT 
                    [COST_CENTER],
                    actual.[MAIN_ACCOUNT],
                    coa.[ACCOUNT_NAME],
                    [BALANCE],
                    [PERIOD]
                FROM [TP_EDW].[dbo].[ACCOUNT_SUMMARY] AS actual
                JOIN [TP_EDW].[dbo].[CHART_OF_ACCOUNT] AS coa 
                    ON actual.MAIN_ACCOUNT = coa.MAIN_ACCOUNT
                WHERE [COST_CENTER] = @COST_CENTER
                    AND [PERIOD] >= FORMAT(DATEFROMPARTS(@YEAR, @MONTH, 1), 'yyyyMM')
                    AND [PERIOD] < FORMAT(DATEADD(MONTH, 12, DATEFROMPARTS(@YEAR, @MONTH, 1)), 'yyyyMM')
                    AND coa.[IS_SUMMARY_CATEGORY] = 'OTHER EXPENSE'
                ORDER BY [PERIOD], actual.[MAIN_ACCOUNT]",
            CommandType = CommandType.Text,
            Mapper = results => OtherExpenseMapper.MapToOtherExpenseDto(results, isBudget: false)
        };

        public static readonly EDWQueryDefinition OTHER_EXPENSES_BUDGET_DATA = new EDWQueryDefinition
        {
            Id = 10,
            NameOrSql = @"
                SELECT 
                    [COST_CENTER],
                    budget.[MAIN_ACCOUNT],
                    coa.[ACCOUNT_NAME],
                    [BALANCE],
                    [PERIOD]
                FROM [TP_EDW].[dbo].[BUDGET_FINAL] AS budget
                JOIN [TP_EDW].[dbo].[CHART_OF_ACCOUNT] AS coa 
                    ON budget.MAIN_ACCOUNT = coa.MAIN_ACCOUNT
                WHERE [COST_CENTER] = @COST_CENTER
                    AND [PERIOD] >= FORMAT(DATEFROMPARTS(@YEAR, @MONTH, 1), 'yyyyMM')
                    AND [PERIOD] < FORMAT(DATEADD(MONTH, 12, DATEFROMPARTS(@YEAR, @MONTH, 1)), 'yyyyMM')
                    AND coa.[IS_SUMMARY_CATEGORY] = 'OTHER EXPENSE'
                ORDER BY [PERIOD], budget.[MAIN_ACCOUNT]",
            CommandType = CommandType.Text,
            Mapper = results => OtherExpenseMapper.MapToOtherExpenseDto(results, isBudget: true)
        };

        public static readonly EDWQueryDefinition INTERNAL_REVENUE_ACTUALS = new EDWQueryDefinition
        {
            Id = 11,
            NameOrSql = @"
                DECLARE @StartDate DATE = DATEFROMPARTS(@YEAR, @MONTH, 1);
                DECLARE @EndDate DATE = EOMONTH(@StartDate);
                DECLARE @TodayDate DATE = CAST(GETDATE() AS DATE);
                
                -- Ensure we don't go beyond today's date
                IF @EndDate > @TodayDate
                    SET @EndDate = @TodayDate;

                -- Parse the comma-separated site IDs into a table
                DECLARE @SiteIds TABLE (SiteId NVARCHAR(50));
                INSERT INTO @SiteIds (SiteId)
                SELECT value FROM STRING_SPLIT(@SITE, ',');

                WITH DailyRevenue AS (
                    -- Get daily external revenue data for all sites
                    SELECT 
                        r.[SITE],
                        r.[DATE],
                        SUM(r.[NETEXTERNALREVENUE]) AS ExternalRevenue
                    FROM [TP_EDW].[dbo].[REVENUE_DAILY_DETAIL] r
                    INNER JOIN @SiteIds s ON r.[SITE] = s.SiteId
                    WHERE r.[DATE] >= @StartDate
                        AND r.[DATE] <= @EndDate
                    GROUP BY r.[SITE], r.[DATE]
                ),
                DailyOccupiedRooms AS (
                    -- Get daily occupied rooms data for all sites
                    SELECT
                        [SITE],
                        [DATE],
                        SUM(CASE WHEN [VALUE_TYPE] = 'Other' AND [REVENUE_CATEGORY] = 'Occupied Rooms' 
                                 THEN [VALUE] ELSE 0 END) AS OccupiedRooms
                    FROM [TP_EDW].[dbo].[REVENUE_DATAMART_DAILY]
                    INNER JOIN @SiteIds s ON [SITE] = s.SiteId
                    WHERE [DATE] >= @StartDate
                        AND [DATE] <= @EndDate
                        AND [VALUE_TYPE] = 'Other' 
                        AND [REVENUE_CATEGORY] = 'Occupied Rooms'
                    GROUP BY [SITE], [DATE]
                ),
                DailyPayroll AS (
                    -- Get daily payroll hours and cost data for all sites
                    SELECT 
                        ps.[WORK_LOCATION] AS [SITE],
                        ps.[DATE],
                        SUM(ps.TOTAL_HOURS) AS PayrollHours,
                        SUM(ps.TOTAL_DOLLARS) AS PayrollCost
                    FROM [TP_LEGION].[dbo].[vwPAYROLL_SUMMARY] ps
                    INNER JOIN @SiteIds s ON ps.[WORK_LOCATION] = s.SiteId
                    WHERE ps.[PAY_TYPE] NOT IN ('DIFFERENTIAL', 'MEAL PREMIUM')
                        AND ps.[DATE] >= @StartDate
                        AND ps.[DATE] <= @EndDate
                    GROUP BY ps.[WORK_LOCATION], ps.[DATE]
                ),
                DailyExpenses AS (
                    -- Get daily claims data (prorated from monthly data) for all sites
                    SELECT 
                        actual.[COST_CENTER] AS [SITE],
                        d.[DATE],
                        -- Calculate daily claims (prorated from monthly total)
                        SUM(actual.[BALANCE]) / DAY(EOMONTH(d.[DATE])) AS Claims
                    FROM (
                        -- Generate date series for the month
                        SELECT DATEADD(DAY, number, @StartDate) AS [DATE]
                        FROM master.dbo.spt_values
                        WHERE type = 'P' 
                            AND number <= DATEDIFF(DAY, @StartDate, @EndDate)
                    ) d
                    CROSS JOIN [TP_EDW].[dbo].[ACCOUNT_SUMMARY] actual
                    INNER JOIN [TP_EDW].[dbo].[CHART_OF_ACCOUNT] coa 
                        ON actual.MAIN_ACCOUNT = coa.MAIN_ACCOUNT
                    INNER JOIN @SiteIds s ON actual.[COST_CENTER] = s.SiteId
                    WHERE actual.[PERIOD] = FORMAT(@StartDate, 'yyyyMM')
                        AND coa.[IS_SUMMARY_CATEGORY] = 'CLAIMS'
                    GROUP BY actual.[COST_CENTER], d.[DATE]
                )
                -- Final result set combining all daily data for all sites
                SELECT 
                    COALESCE(dr.[SITE], dor.[SITE], dp.[SITE], de.[SITE]) AS SITE,
                    @YEAR AS YEAR,
                    @MONTH AS MONTH,
                    COALESCE(dr.[DATE], dor.[DATE], dp.[DATE], de.[DATE]) AS [Date],
                    ISNULL(dr.ExternalRevenue, 0) AS ExternalRevenue,
                    ISNULL(dor.OccupiedRooms, 0) AS OccupiedRooms,
                    ISNULL(dp.PayrollHours, 0) AS PayrollHours,
                    ISNULL(dp.PayrollCost, 0) AS PayrollCost,
                    ISNULL(de.Claims, 0) AS Claims
                FROM DailyRevenue dr
                FULL OUTER JOIN DailyOccupiedRooms dor ON dr.[SITE] = dor.[SITE] AND dr.[DATE] = dor.[DATE]
                FULL OUTER JOIN DailyPayroll dp ON COALESCE(dr.[SITE], dor.[SITE]) = dp.[SITE] AND COALESCE(dr.[DATE], dor.[DATE]) = dp.[DATE]
                FULL OUTER JOIN DailyExpenses de ON COALESCE(dr.[SITE], dor.[SITE], dp.[SITE]) = de.[SITE] AND COALESCE(dr.[DATE], dor.[DATE], dp.[DATE]) = de.[DATE]
                WHERE COALESCE(dr.[SITE], dor.[SITE], dp.[SITE], de.[SITE]) IS NOT NULL
                    AND COALESCE(dr.[DATE], dor.[DATE], dp.[DATE], de.[DATE]) IS NOT NULL
                ORDER BY COALESCE(dr.[SITE], dor.[SITE], dp.[SITE], de.[SITE]), COALESCE(dr.[DATE], dor.[DATE], dp.[DATE], de.[DATE]);",
            CommandType = CommandType.Text,
            Mapper = results => InternalRevenueActualsMapper.MapToInternalRevenueActualsResponseForMultipleSites(
                results.ToList(), 
                results.FirstOrDefault()?.GetValue<int>("YEAR") ?? 0, 
                results.FirstOrDefault()?.GetValue<int>("MONTH") ?? 0)
        };

        public static readonly Dictionary<int, EDWQueryDefinition> Definitions = new()
        {
            { BUDGET_DAILY_DETAIL.Id, BUDGET_DAILY_DETAIL },
            { BUDGET_ACTUAL_SUMMARY.Id, BUDGET_ACTUAL_SUMMARY },
            { RATES_BY_YEAR.Id, RATES_BY_YEAR },
            { BUDGET_ACTUAL_SUMMARY_BY_SITE.Id, BUDGET_ACTUAL_SUMMARY_BY_SITE },
            { PAYROLL_BUDGET_BY_SITE.Id, PAYROLL_BUDGET_BY_SITE },
            { PAYROLL_ACTUALS_BY_SITE.Id, PAYROLL_ACTUALS_BY_SITE },
            { PAYROLL_SCHEDULE_BY_SITE.Id, PAYROLL_SCHEDULE_BY_SITE },
            { REVENUE_DAILY_STATS_PIVOTED.Id, REVENUE_DAILY_STATS_PIVOTED },
            { OTHER_EXPENSES_ACTUAL_DATA.Id, OTHER_EXPENSES_ACTUAL_DATA },
            { OTHER_EXPENSES_BUDGET_DATA.Id, OTHER_EXPENSES_BUDGET_DATA },
            { INTERNAL_REVENUE_ACTUALS.Id, INTERNAL_REVENUE_ACTUALS }
        };
    }
}
