import React from "react";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import { Label } from "@/components/ui/label";

interface ViewModeRadioGroupProps {
    viewMode: 'flash' | 'budget' | 'priorYear';
    onViewModeChange: (mode: 'flash' | 'budget' | 'priorYear') => void;
    disabled?: boolean;
}

export function ViewModeRadioGroup({ viewMode, onViewModeChange, disabled }: ViewModeRadioGroupProps) {
    return (
        <RadioGroup 
            value={viewMode} 
            onValueChange={onViewModeChange} 
            disabled={disabled}
            className="flex items-center space-x-4"
        >
            <div className="flex items-center space-x-2">
                <RadioGroupItem value="flash" id="flash" data-qa-id="radio-show-flash" />
                <Label htmlFor="flash" className="whitespace-nowrap">Show Flash</Label>
            </div>
            <div className="flex items-center space-x-2">
                <RadioGroupItem value="budget" id="budget" data-qa-id="radio-show-budget" />
                <Label htmlFor="budget" className="whitespace-nowrap">Show Budget</Label>
            </div>
            <div className="flex items-center space-x-2">
                <RadioGroupItem value="priorYear" id="priorYear" disabled data-qa-id="radio-show-prior-year" />
                <Label htmlFor="priorYear" className="text-muted-foreground whitespace-nowrap">Show Prior Year</Label>
            </div>
        </RadioGroup>
    );
}
