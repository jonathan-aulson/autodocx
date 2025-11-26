import React from "react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Eye, EyeOff } from "lucide-react";
import { TimeRangeType } from "@/lib/models/Statistics";

interface StatisticsControlsProps {
    inputType: string;
    onInputTypeChange: (value: string) => void;
    showingBudget: boolean;
    onToggleBudgetForecast: () => void;
    selectedSite: string;
    selectedPeriod: string;
    isLoadingStatistics: boolean;
    timePeriod: TimeRangeType;
}

export const StatisticsControls: React.FC<StatisticsControlsProps> = ({
    inputType,
    onInputTypeChange,
    showingBudget,
    onToggleBudgetForecast,
    selectedSite,
    selectedPeriod,
    isLoadingStatistics,
    timePeriod
}) => {
    return (
        <>
            {timePeriod !== "DAILY" && (
                <div className="mb-4">
                    <Alert>
                        <AlertDescription>
                            <strong>Note:</strong> Editing and saving statistics is only available in the <b>Daily View</b> time period. Other views are read-only.
                        </AlertDescription>
                    </Alert>
                </div>
            )}
            
            <Card>
                <CardHeader className="flex flex-row items-center justify-between">
                    <CardTitle>Statistics for Selected Dates</CardTitle>
                    <div className="flex flex-col gap-4 items-end">
                        <RadioGroup
                            value={inputType}
                            onValueChange={onInputTypeChange}
                            className="flex gap-4"
                            disabled={!selectedSite || !selectedPeriod || isLoadingStatistics}
                            data-qa-id="radio-group-input-type"
                        >
                            <div className="flex items-center space-x-2">
                                <RadioGroupItem value="occupancy" id="occupancy" data-qa-id="radio-occupancy" />
                                <Label htmlFor="occupancy">Percentage</Label>
                            </div>
                            <div className="flex items-center space-x-2">
                                <RadioGroupItem value="occupied-rooms" id="occupied-rooms" data-qa-id="radio-occupied-rooms" />
                                <Label htmlFor="occupied-rooms">Occupied Rooms</Label>
                            </div>
                        </RadioGroup>
                        <Button
                            onClick={onToggleBudgetForecast}
                            disabled={!selectedSite || !selectedPeriod || isLoadingStatistics}
                            data-qa-id="button-toggle-budget-forecast"
                            size="sm"
                            variant="outline"
                        >
                            {showingBudget ? (
                                <>
                                    <Eye className="mr-2 h-4 w-4" />
                                    Show Forecast
                                </>
                            ) : (
                                <>
                                    <EyeOff className="mr-2 h-4 w-4" />
                                    Show Comparison
                                </>
                            )}
                        </Button>
                    </div>
                </CardHeader>
                <CardContent>
                    {/* Content will be passed as children */}
                </CardContent>
            </Card>
        </>
    );
}; 