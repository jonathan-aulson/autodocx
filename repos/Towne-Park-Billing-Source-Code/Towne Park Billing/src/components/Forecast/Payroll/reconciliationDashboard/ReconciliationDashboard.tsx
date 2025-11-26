"use client"

import { useState, useMemo, useEffect } from "react"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Button } from "@/components/ui/button"
import { Calendar } from "@/components/ui/calendar"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"
import { CalendarIcon } from "lucide-react"
import { format } from "date-fns"
import { formatCurrency } from "@/lib/utils"
import { 
  PayrollData, 
  TimeHorizon, 
  ComparisonType, 
  JobGroup,
  ReconciliationDashboardProps 
} from "./types"
import { 
  getAllJobGroups, 
  getMonthBoundaries,
  calculateReconciliationData, 
  formatJobLabel, 
  getTimeframeLabel 
} from "./utils"

export function ReconciliationDashboard({
  data,
  payrollDto,
  availableJobGroups: providedJobGroups,
  billingPeriod,
  timeHorizon,
  showComparison,
  comparisonType,
}: ReconciliationDashboardProps) {
  const [jobFilter, setJobFilter] = useState<"all" | JobGroup>("all")
  const [sortField, setSortField] = useState<"jobCode" | "variance">("jobCode")
  const [sortDirection, setSortDirection] = useState<"asc" | "desc">("asc")

  // Get available job groups from either payrollDto or provided prop (for live data)
  const availableJobGroups = useMemo(() => {
    if (providedJobGroups) {
      return providedJobGroups;
    }
    return payrollDto ? getAllJobGroups(payrollDto) : [];
  }, [payrollDto, providedJobGroups]);

  // Get month boundaries for date constraint (CRITICAL)
  const monthBoundaries = useMemo(() => 
    getMonthBoundaries(billingPeriod), [billingPeriod]
  );

  // Initialize date range constrained to billing period month
  const getDefaultDateRange = () => {
    // Start with first day of month, end with last day of month
    // This provides a full month view by default
    return {
      start: monthBoundaries.start,
      end: monthBoundaries.end
    };
  }

  const [dateRange, setDateRange] = useState(getDefaultDateRange())

  // Update date range when billing period changes
  useEffect(() => {
    setDateRange(getDefaultDateRange())
  }, [monthBoundaries])

  // Filter data based on selected date range
  const filteredData = data.filter((d) => {
    // Normalize dates to compare only the date part (year, month, day)
    const dataDay = new Date(d.date.getFullYear(), d.date.getMonth(), d.date.getDate());
    const startDay = new Date(dateRange.start.getFullYear(), dateRange.start.getMonth(), dateRange.start.getDate());
    const endDay = new Date(dateRange.end.getFullYear(), dateRange.end.getMonth(), dateRange.end.getDate());
    
    return dataDay >= startDay && dataDay <= endDay;
  })

  // Calculate reconciliation data
  const reconciliationData = calculateReconciliationData(filteredData, jobFilter, timeHorizon)

  // Sort data
  const sortedData = [...reconciliationData].sort((a, b) => {
    if (sortField === "jobCode") {
      return sortDirection === "asc" ? a.jobCode.localeCompare(b.jobCode) : b.jobCode.localeCompare(a.jobCode)
    } else {
      return sortDirection === "asc" ? a.variance - b.variance : b.variance - a.variance
    }
  })

  // Handle job filter change
  const handleJobFilterChange = (value: string) => {
    setJobFilter(value as "all" | JobGroup)
  }

  // Handle sort toggle
  const handleSortToggle = (field: "jobCode" | "variance") => {
    if (sortField === field) {
      setSortDirection(sortDirection === "asc" ? "desc" : "asc")
    } else {
      setSortField(field)
      setSortDirection("asc")
    }
  }

  // Get timeframe label
  const timeframeLabel = getTimeframeLabel(dateRange.start, dateRange.end)

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <div className="text-sm font-medium">
            Month: <span className="font-semibold">{format(new Date(parseInt(billingPeriod.split('-')[0]), parseInt(billingPeriod.split('-')[1]) - 1, 1), 'MMMM yyyy')}</span>
          </div>
          <div className="flex items-center gap-2">
            <Popover>
              <PopoverTrigger asChild>
                <Button variant="outline" size="sm" className="text-xs bg-transparent">
                  <CalendarIcon className="mr-2 h-3 w-3" />
                  {format(dateRange.start, "MMM dd")}
                </Button>
              </PopoverTrigger>
              <PopoverContent className="w-auto p-0" align="start">
                <Calendar
                  mode="single"
                  selected={dateRange.start}
                  defaultMonth={monthBoundaries.start}
                  fromMonth={monthBoundaries.start}
                  toMonth={monthBoundaries.start}
                  showOutsideDays={false}
                  fromDate={monthBoundaries.start}
                  toDate={monthBoundaries.end}
                  onSelect={(date) => {
                    if (!date) return;
                    
                    // Normalize dates to start of day for comparison
                    const dateStart = new Date(date.getFullYear(), date.getMonth(), date.getDate());
                    const monthStart = new Date(monthBoundaries.start.getFullYear(), monthBoundaries.start.getMonth(), monthBoundaries.start.getDate());
                    const monthEnd = new Date(monthBoundaries.end.getFullYear(), monthBoundaries.end.getMonth(), monthBoundaries.end.getDate());
                    
                    if (dateStart < monthStart || dateStart > monthEnd) return;
                
                    setDateRange((prev) => {
                      const newStart = date;
                      const newEnd = prev.end < newStart ? newStart : prev.end;
                      return { ...prev, start: newStart, end: newEnd }
                    })
                  }}
                  disabled={(date) => {
                    // Normalize dates to start of day for comparison
                    const dateStart = new Date(date.getFullYear(), date.getMonth(), date.getDate());
                    const monthStart = new Date(monthBoundaries.start.getFullYear(), monthBoundaries.start.getMonth(), monthBoundaries.start.getDate());
                    const monthEnd = new Date(monthBoundaries.end.getFullYear(), monthBoundaries.end.getMonth(), monthBoundaries.end.getDate());
                    
                    // Disable outside the billing month
                    return dateStart < monthStart || dateStart > monthEnd;
                  }}
                  initialFocus
                />
              </PopoverContent>
            </Popover>
            <span className="text-xs text-muted-foreground">to</span>
            <Popover>
              <PopoverTrigger asChild>
                <Button variant="outline" size="sm" className="text-xs bg-transparent">
                  <CalendarIcon className="mr-2 h-3 w-3" />
                  {format(dateRange.end, "MMM dd")}
                </Button>
              </PopoverTrigger>
              <PopoverContent className="w-auto p-0" align="start">
                <Calendar
                  mode="single"
                  selected={dateRange.end}
                  defaultMonth={monthBoundaries.start}
                  fromMonth={monthBoundaries.start}
                  toMonth={monthBoundaries.start}
                  showOutsideDays={false}
                  fromDate={monthBoundaries.start}
                  toDate={monthBoundaries.end}
                  onSelect={(date) => {
                    if (!date) return;
                    
                    // Normalize dates to start of day for comparison
                    const dateStart = new Date(date.getFullYear(), date.getMonth(), date.getDate());
                    const monthStart = new Date(monthBoundaries.start.getFullYear(), monthBoundaries.start.getMonth(), monthBoundaries.start.getDate());
                    const monthEnd = new Date(monthBoundaries.end.getFullYear(), monthBoundaries.end.getMonth(), monthBoundaries.end.getDate());
                    const rangeStart = new Date(dateRange.start.getFullYear(), dateRange.start.getMonth(), dateRange.start.getDate());
                    
                    // Must be within month boundaries and not earlier than selected start
                    if (dateStart < monthStart || dateStart > monthEnd) return;
                    if (dateStart < rangeStart) return; // ignore selection earlier than start
                    setDateRange((prev) => ({ ...prev, end: date }))
                  }}
                  disabled={(date) => {
                    // Normalize dates to start of day for comparison
                    const dateStart = new Date(date.getFullYear(), date.getMonth(), date.getDate());
                    const monthStart = new Date(monthBoundaries.start.getFullYear(), monthBoundaries.start.getMonth(), monthBoundaries.start.getDate());
                    const monthEnd = new Date(monthBoundaries.end.getFullYear(), monthBoundaries.end.getMonth(), monthBoundaries.end.getDate());
                    const rangeStart = new Date(dateRange.start.getFullYear(), dateRange.start.getMonth(), dateRange.start.getDate());
                    
                    // Disable outside the billing month OR earlier than selected start date
                    return dateStart < monthStart || dateStart > monthEnd || dateStart < rangeStart;
                  }}
                  initialFocus
                />
              </PopoverContent>
            </Popover>
          </div>
        </div>
        <div className="flex gap-2">
          <Select value={jobFilter} onValueChange={handleJobFilterChange}>
            <SelectTrigger className="w-[150px]">
              <SelectValue placeholder="Filter by job" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Jobs</SelectItem>
              {availableJobGroups.map((group) => (
                <SelectItem key={group} value={group}>
                  {formatJobLabel(group)}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      <div className="relative overflow-x-auto rounded-md border">
        <table className="w-full text-sm text-left">
          <thead className="bg-muted/50 text-muted-foreground">
            <tr>
              <th className="px-4 py-2 cursor-pointer hover:bg-muted" onClick={() => handleSortToggle("jobCode")}>
                Job Code
                {sortField === "jobCode" && <span className="ml-1">{sortDirection === "asc" ? "↑" : "↓"}</span>}
              </th>
              <th className="px-4 py-2">Scheduled</th>
              <th className="px-4 py-2">Actual</th>
              <th className="px-4 py-2">Forecast</th>
              <th className="px-4 py-2">Budget</th>
              <th
                className="px-4 py-2 cursor-pointer hover:bg-muted relative"
                onClick={() => handleSortToggle("variance")}
              >
                <div className="flex items-center gap-1">
                  Actual Variance to Budget
                  <Popover>
                    <PopoverTrigger asChild>
                      <div className="w-4 h-4 rounded-full bg-muted-foreground/20 flex items-center justify-center text-xs cursor-help">
                        i
                      </div>
                    </PopoverTrigger>
                    <PopoverContent className="w-80 p-3" align="center">
                      <div className="space-y-2">
                        <p className="text-sm">
                          An upward pointing variance indicates Actual &gt; Budget, and a downward pointing variance
                          indicates Actual &lt; Budget.
                        </p>
                        <div className="flex items-center gap-4 text-xs">
                          <div className="flex items-center gap-1">
                            <span className="text-foreground font-semibold">↑</span>
                            <span>Actual &gt; Budget</span>
                          </div>
                          <div className="flex items-center gap-1">
                            <span className="text-foreground font-semibold">↓</span>
                            <span>Actual &lt; Budget</span>
                          </div>
                        </div>
                      </div>
                    </PopoverContent>
                  </Popover>
                  {sortField === "variance" && <span className="ml-1">{sortDirection === "asc" ? "↑" : "↓"}</span>}
                </div>
              </th>
            </tr>
          </thead>
          <tbody>
            {sortedData.map((item, index) => (
              <tr key={item.jobCode} className="border-b hover:bg-muted/30">
                <td className="px-4 py-2 font-medium">{formatJobLabel(item.jobCode)}</td>
                <td className="px-4 py-2">
                  <div className="flex items-center gap-1">
                    <span className="text-foreground">{formatCurrency(item.scheduled)}</span>
                    {(() => {
                      const scheduledVariance = item.scheduled - item.budget;
                      if (scheduledVariance > 0) {
                        return <span className="text-red-600 dark:text-red-400">↑</span>;
                      } else if (scheduledVariance < 0) {
                        return <span className="text-green-600 dark:text-green-400">↓</span>;
                      }
                      return null;
                    })()}
                  </div>
                </td>
                <td className="px-4 py-2">{item.actual !== null ? formatCurrency(item.actual) : "—"}</td>
                <td className="px-4 py-2">
                  <div className="flex items-center gap-1">
                    <span className="text-foreground">{formatCurrency(item.forecast)}</span>
                    {(() => {
                      const forecastVariance = item.forecast - item.budget;
                      if (forecastVariance > 0) {
                        return <span className="text-red-600 dark:text-red-400">↑</span>;
                      } else if (forecastVariance < 0) {
                        return <span className="text-green-600 dark:text-green-400">↓</span>;
                      }
                      return null;
                    })()}
                  </div>
                </td>
                <td className="px-4 py-2">{formatCurrency(item.budget)}</td>
                <td className="px-4 py-2">
                  {item.actual !== null ? (
                    <div className={`flex items-center gap-1 ${
                      item.variance > 0 ? "text-red-600 dark:text-red-400" : 
                      item.variance < 0 ? "text-green-600 dark:text-green-400" : 
                      "text-foreground"
                    }`}>
                      <span>{item.variance > 0 ? "↑" : item.variance < 0 ? "↓" : ""}</span>
                      <span>{formatCurrency(Math.abs(item.variance))}</span>
                    </div>
                  ) : (
                    "—"
                  )}
                </td>
              </tr>
            ))}
          </tbody>
          <tfoot className="bg-muted/50 font-semibold text-base">
            <tr className="h-12">
              <td className="px-4 py-3">Total</td>
              <td className="px-4 py-3">
                <div className="flex items-center gap-1">
                  <span>{formatCurrency(sortedData.reduce((total, item) => total + item.scheduled, 0))}</span>
                  {(() => {
                    const totalScheduled = sortedData.reduce((total, item) => total + item.scheduled, 0);
                    const totalBudget = sortedData.reduce((total, item) => total + item.budget, 0);
                    const scheduledVariance = totalScheduled - totalBudget;
                    if (scheduledVariance > 0) {
                      return <span className="text-red-600 dark:text-red-400">↑</span>;
                    } else if (scheduledVariance < 0) {
                      return <span className="text-green-600 dark:text-green-400">↓</span>;
                    }
                    return null;
                  })()}
                </div>
              </td>
              <td className="px-4 py-3">
                {formatCurrency(
                  sortedData.reduce((total, item) => total + (item.actual !== null ? item.actual : 0), 0),
                )}
              </td>
              <td className="px-4 py-3">
                <div className="flex items-center gap-1">
                  <span>{formatCurrency(sortedData.reduce((total, item) => total + item.forecast, 0))}</span>
                  {(() => {
                    const totalForecast = sortedData.reduce((total, item) => total + item.forecast, 0);
                    const totalBudget = sortedData.reduce((total, item) => total + item.budget, 0);
                    const forecastVariance = totalForecast - totalBudget;
                    if (forecastVariance > 0) {
                      return <span className="text-red-600 dark:text-red-400">↑</span>;
                    } else if (forecastVariance < 0) {
                      return <span className="text-green-600 dark:text-green-400">↓</span>;
                    }
                    return null;
                  })()}
                </div>
              </td>
              <td className="px-4 py-3">
                {formatCurrency(sortedData.reduce((total, item) => total + item.budget, 0))}
              </td>
              <td className="px-4 py-3">
                <div className="flex items-center gap-1">
                  {(() => {
                    const totalVariance = sortedData.reduce((total, item) => total + item.variance, 0)
                    return (
                      <div className={`flex items-center gap-1 ${
                        totalVariance > 0 ? "text-red-600 dark:text-red-400" : 
                        totalVariance < 0 ? "text-green-600 dark:text-green-400" : 
                        "text-foreground"
                      }`}>
                        <span>{totalVariance > 0 ? "↑" : totalVariance < 0 ? "↓" : ""}</span>
                        <span>{formatCurrency(Math.abs(totalVariance))}</span>
                      </div>
                    )
                  })()}
                </div>
              </td>
            </tr>
          </tfoot>
        </table>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-5 gap-4 mt-4">
        <div className="bg-blue-50 dark:bg-blue-950/50 border border-blue-200 dark:border-blue-800 p-3 rounded-md">
          <h4 className="text-sm font-medium mb-1 text-blue-700 dark:text-blue-300">Scheduled Total</h4>
          <p className="text-lg font-bold">
            <div className="flex items-center gap-1">
              <span className="text-blue-900 dark:text-blue-100">
                {formatCurrency(sortedData.reduce((total, item) => total + item.scheduled, 0))}
              </span>
              {(() => {
                const totalScheduled = sortedData.reduce((total, item) => total + item.scheduled, 0);
                const totalBudget = sortedData.reduce((total, item) => total + item.budget, 0);
                const scheduledVariance = totalScheduled - totalBudget;
                if (scheduledVariance > 0) {
                  return <span className="text-red-600 dark:text-red-400">↑</span>;
                } else if (scheduledVariance < 0) {
                  return <span className="text-green-600 dark:text-green-400">↓</span>;
                }
                return null;
              })()}
            </div>
          </p>
        </div>
        <div className="bg-orange-50 dark:bg-orange-950/50 border border-orange-200 dark:border-orange-800 p-3 rounded-md">
          <h4 className="text-sm font-medium mb-1 text-orange-700 dark:text-orange-300">Actual Total</h4>
          <p className="text-lg font-bold text-orange-900 dark:text-orange-100">
            {formatCurrency(sortedData.reduce((total, item) => total + (item.actual !== null ? item.actual : 0), 0))}
          </p>
        </div>
        <div className="bg-green-50 dark:bg-green-950/50 border border-green-200 dark:border-green-800 p-3 rounded-md">
          <h4 className="text-sm font-medium mb-1 text-green-700 dark:text-green-300">Forecast Total</h4>
          <p className="text-lg font-bold">
            <div className="flex items-center gap-1">
              <span className="text-green-900 dark:text-green-100">
                {formatCurrency(sortedData.reduce((total, item) => total + item.forecast, 0))}
              </span>
              {(() => {
                const totalForecast = sortedData.reduce((total, item) => total + item.forecast, 0);
                const totalBudget = sortedData.reduce((total, item) => total + item.budget, 0);
                const forecastVariance = totalForecast - totalBudget;
                if (forecastVariance > 0) {
                  return <span className="text-red-600 dark:text-red-400">↑</span>;
                } else if (forecastVariance < 0) {
                  return <span className="text-green-600 dark:text-green-400">↓</span>;
                }
                return null;
              })()}
            </div>
          </p>
        </div>
        <div className="bg-red-50 dark:bg-red-950/50 border border-red-200 dark:border-red-800 p-3 rounded-md">
          <h4 className="text-sm font-medium mb-1 text-red-700 dark:text-red-300">Budget Total</h4>
          <p className="text-lg font-bold text-red-900 dark:text-red-100">
            {formatCurrency(sortedData.reduce((total, item) => total + item.budget, 0))}
          </p>
        </div>
        <div className="bg-gray-50 dark:bg-gray-950/50 border border-gray-200 dark:border-gray-800 p-3 rounded-md">
          <h4 className="text-sm font-medium mb-1 text-gray-700 dark:text-gray-300">Variance Total</h4>
          <p className="text-lg font-bold">
            {(() => {
              const totalVariance = sortedData.reduce((total, item) => total + item.variance, 0)
              return (
                <div className={`flex items-center gap-1 ${
                  totalVariance > 0 ? "text-red-600 dark:text-red-400" : 
                  totalVariance < 0 ? "text-green-600 dark:text-green-400" : 
                  "text-gray-900 dark:text-gray-100"
                }`}>
                  <span>{totalVariance > 0 ? "↑" : totalVariance < 0 ? "↓" : ""}</span>
                  <span>{formatCurrency(Math.abs(totalVariance))}</span>
                </div>
              )
            })()}
          </p>
        </div>
      </div>
    </div>
  )
}