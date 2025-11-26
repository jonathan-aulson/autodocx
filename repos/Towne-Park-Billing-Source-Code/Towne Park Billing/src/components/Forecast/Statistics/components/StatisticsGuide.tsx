import React from "react";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { ChevronDown, ChevronUp, Info } from 'lucide-react';

interface StatisticsGuideProps {
    isGuideOpen: boolean;
    toggleGuide: () => void;
    error: string | null;
}

export const StatisticsGuide: React.FC<StatisticsGuideProps> = ({
    isGuideOpen,
    toggleGuide,
    error
}) => {
    return (
        <>
            <Button variant="outline" onClick={toggleGuide} className="flex items-center gap-2 mb-2" data-qa-id="button-toggle-guide">
                <Info className="h-4 w-4" />
                {isGuideOpen ? "Hide Guide" : "Show Guide"}
                {isGuideOpen ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
            </Button>
            {error && <p className="text-red-500 mb-4">{error}</p>}

            {isGuideOpen && (
                <div className="space-y-6 p-6 border-2 border-border rounded-lg bg-muted dark:bg-gray-900 text-card-foreground mb-6 shadow-sm">
                    <div className="border-b-2 border-border pb-3">
                        <h3 className="text-xl font-semibold text-foreground">Parking Stats — Guide</h3>
                    </div>

                    <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                        <div className="p-4 border border-border rounded-lg bg-card shadow-sm">
                            <h4 className="font-semibold mb-3 text-foreground border-b border-border pb-2">Purpose</h4>
                            <ul className="list-disc pl-5 space-y-1 text-muted-foreground">
                                <li>Enter projected operating volumes that drive External Revenue and inform labor needs.</li>
                                <li>Compare your Actualized volume against your Forecast by viewing the Variance Indicators</li>
                                <ul className="list-disc pl-5 space-y-1 mt-1">
                                    <li><span className="text-green-600 dark:text-green-400">Green ▲</span> if Actual &gt; Forecast</li>
                                    <li>Black ● if Actual = Forecast</li>
                                    <li><span className="text-red-600 dark:text-red-400">Red ▼</span> if Actual &lt; Forecast</li>
                                </ul>
                            </ul>
                        </div>

                        <div className="p-4 border border-border rounded-lg bg-card shadow-sm">
                            <h4 className="font-semibold mb-3 text-foreground border-b border-border pb-2">What to enter</h4>
                            <div className="mb-2">
                                <h5 className="font-medium text-sm">By site-local day:</h5>
                                <ol className="list-decimal pl-5 space-y-1 text-muted-foreground">
                                    <li>Self vs. Valet vehicles, Overnight stays, Monthly parkers.</li>
                                    <li>Events/Groups as applicable.</li>
                                    <li>Occupied Rooms or Occupancy % (based on your preference)</li>
                                </ol>
                            </div>
                        </div>

                        <div className="p-4 border border-border rounded-lg bg-card shadow-sm">
                            <h4 className="font-semibold mb-3 text-foreground border-b border-border pb-2">How your inputs are used</h4>
                            <ol className="list-decimal pl-5 space-y-1 text-muted-foreground">
                                <li>External Revenue forecast = Stats × Rates (from the Parking Rates tab).</li>
                                <li>Stats roll into KPIs (e.g., Capture Ratio) and inform labor forecasting.</li>
                            </ol>
                        </div>

                        <div className="p-4 border border-border rounded-lg bg-card shadow-sm">
                            <h4 className="font-semibold mb-3 text-foreground border-b border-border pb-2">Tips and guardrails</h4>
                            <ol className="list-decimal pl-5 space-y-1 text-muted-foreground">
                                <li>Use realistic seasonality and event impacts.</li>
                                <li>Don't try to force revenue by inflating validations here. If you need a specific IR adjustment, use the Other Revenue tab (see guide below).</li>
                            </ol>
                        </div>
                    </div>
                </div>
            )}
        </>
    );
}; 