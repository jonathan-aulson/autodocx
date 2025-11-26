
import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from "@/components/ui/accordion";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "@/components/ui/tooltip";
import { InvoiceGroup, RevenueAccumulation, ThresholdStructure, Tier, ValidationThresholdType } from "@/lib/models/Contract";
import { Info, MinusCircle, PlusCircle } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { NumericFormat } from "react-number-format";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "../ui/select";
import { Switch } from "../ui/switch";

const DEFAULT_ACCUMULATION_TYPE = "Monthly";

interface RevenueShareProps {
    revenueShareEnabled: boolean;
    setRevenueShareEnabled: (enabled: boolean) => void;
    thresholdStructuresvalues: ThresholdStructure[];
    setThresholdStructures: React.Dispatch<React.SetStateAction<ThresholdStructure[]>>;
    revenueCodes: string[];
    invoiceGroups: InvoiceGroup[];
    watchInvoiceGroupingEnabled: boolean;
    isEditable: boolean;
}

export default function RevenueShare({
    revenueShareEnabled,
    setRevenueShareEnabled,
    setThresholdStructures,
    revenueCodes,
    invoiceGroups,
    watchInvoiceGroupingEnabled,
    isEditable,
    thresholdStructuresvalues,


}: RevenueShareProps) {
    const [state, setState] = useState({
        enabled: revenueShareEnabled,
        structures: thresholdStructuresvalues,
        expandedItems: [] as string[],
        globalAccumulationType: RevenueAccumulation.MONTHLY,
        sharePercentage: thresholdStructuresvalues[0]?.tiers[0]?.sharePercentage || 0,
        createThresholdStructures: thresholdStructuresvalues.length > 1 || thresholdStructuresvalues.some(structure =>
            structure.tiers.length > 1 && !(structure.tiers.length === 2 && structure.tiers[1].sharePercentage === 0)
        )
    });
    const [isDirty, setIsDirty] = useState(false);
    const originalStateRef = useRef(state);
    const [isThresholdStructures, setThresholdStructure] = useState(false);
    const [isValidationThreshold, setValidationThreshold] = useState(false);
    const [currentInvoiceGroup, setCurrentInvoiceGroup] = useState<number>(
        thresholdStructuresvalues[0]?.invoiceGroup ?? (invoiceGroups[0]?.groupNumber ?? 1)
    );
    const prevValidationRef = useRef<boolean | null>(null);
    useEffect(() => {
        if (!isEditable) {
            if (isDirty) {
                // Reset to original state when cancelling changes
                setState(originalStateRef.current);
                setRevenueShareEnabled(originalStateRef.current.enabled);
                setThresholdStructures(originalStateRef.current.structures);
                setCurrentInvoiceGroup(originalStateRef.current.structures[0]?.invoiceGroup ?? (invoiceGroups[0]?.groupNumber ?? 1));
                setIsDirty(false);
            } else {
                // Update state with new props when not dirty
                setState(prevState => ({
                    ...prevState,
                    enabled: revenueShareEnabled,
                    structures: thresholdStructuresvalues,
                    sharePercentage: thresholdStructuresvalues[0]?.tiers[0]?.sharePercentage || 0,
                    createThresholdStructures: thresholdStructuresvalues.length > 1 || thresholdStructuresvalues.some(structure =>
                        structure.tiers.length > 1 && !(structure.tiers.length === 2 && structure.tiers[1].sharePercentage === 0)
                    )
                }));

                setCurrentInvoiceGroup(thresholdStructuresvalues[0]?.invoiceGroup ?? (invoiceGroups[0]?.groupNumber ?? 1));
            }
        }
    }, [isEditable, revenueShareEnabled, thresholdStructuresvalues, invoiceGroups, isDirty]);




    const updateState = (updates: Partial<typeof state>) => {
        if (!isDirty) {
            setIsDirty(true);
            originalStateRef.current = state; // Save original state for reset
        }

        const newState = { ...state, ...updates };
        setState(newState);

        setRevenueShareEnabled(newState.enabled);
        setThresholdStructures(newState.structures);
    };

    const handleRevenueShareToggle = (checked: boolean) => {
        updateState({ enabled: checked });
    };

    const handleThresholdStructureChange = (structures: ThresholdStructure[]) => {
        updateState({ structures });
    };

    const handleSharePercentageChange = (values: { floatValue?: number }) => {
        const percentage = values.floatValue || 0;

        const updatedStructures = state.structures.map(structure => ({
            ...structure,
            tiers: structure.tiers.map((tier, index) => ({
                ...tier,
                sharePercentage: index === 0 ? percentage : tier.sharePercentage
            }))
        }));
        updateState({
            sharePercentage: percentage,
            structures: updatedStructures
        });
    };

    const setDefaultThresholdStructure = () => {
        if (!state.structures || state.structures.length === 0 || (!state.createThresholdStructures && state.structures.length === 1)) {
            const defaultStructure: ThresholdStructure = {
                tiers: [{ sharePercentage: state.sharePercentage, amount: 0 }],
                revenueCodes: [...revenueCodes],
                accumulationType: RevenueAccumulation.MONTHLY,
                invoiceGroup: currentInvoiceGroup,
                validationThresholdType: state.structures[0]?.validationThresholdType || null,
                validationThresholdAmount: state.structures[0]?.validationThresholdAmount || 0,
            };
            updateState({ structures: [defaultStructure] });
        }
    };

    const handleAddTier = (structureIndex: number) => {
        const newStructures = [...state.structures];
        newStructures[structureIndex].tiers.push({ sharePercentage: 0, amount: 0 });
        handleThresholdStructureChange(newStructures);
    };

    const handleRemoveTier = (structureIndex: number, tierIndex: number) => {
        const newStructures = [...state.structures];
        const tiers = newStructures[structureIndex].tiers;

        if (tierIndex >= 0 && tierIndex < tiers.length) {
            tiers.splice(tierIndex, 1);

            if (tierIndex > 0) {
                tiers[tierIndex - 1].amount = 0;
            }

            handleThresholdStructureChange(newStructures);
        }
    };

    const handleAddStructure = () => {
        const newStructure: ThresholdStructure = {
            tiers: [{ sharePercentage: 0, amount: 0 }],
            revenueCodes: [...revenueCodes],
            accumulationType: state.globalAccumulationType,
            invoiceGroup: currentInvoiceGroup,
            validationThresholdType: null,
            validationThresholdAmount: 0
        };

        const updatedStructures = [...state.structures, newStructure];
        const updatedExpandedItems = [
            ...state.expandedItems,
            `item-${state.structures.length}`
        ];

        updateState({
            structures: updatedStructures,
            expandedItems: updatedExpandedItems
        });
    };

    const handleRemoveStructure = (index: number) => {
        if (state.structures.length > 1) {
            const newStructures = state.structures.filter((_, i) => i !== index).map((structure) => ({
                ...structure,
                tiers: structure.tiers.map((tier) => ({ ...tier })),
            }));
            if (newStructures.length === 1) {
                newStructures[0] = {
                    ...newStructures[0],
                    revenueCodes: [...revenueCodes],
                };
            }
            const updatedExpandedItems = state.expandedItems
                .filter((item) => item !== `item-${index}`)
                .map((item) => {
                    const itemIndex = parseInt(item.split('-')[1], 10);
                    return itemIndex > index ? `item-${itemIndex - 1}` : item;
                });
            updateState({
                structures: [...newStructures],
                expandedItems: updatedExpandedItems,
            });
        }
    };


    const handleTierChange = (structureIndex: number, tierIndex: number, field: keyof Tier, value: number) => {
        const newStructures = [...state.structures];
        newStructures[structureIndex].tiers[tierIndex][field] = value;
        handleThresholdStructureChange(newStructures);
    };

    const handleRevenueCodeChange = (structureIndex: number, code: string) => {
        const newStructures = [...state.structures];
        const currentCodes = newStructures[structureIndex].revenueCodes;
        if (currentCodes.includes(code)) {
            newStructures[structureIndex].revenueCodes = currentCodes.filter(c => c !== code);
        } else {
            newStructures[structureIndex].revenueCodes = [...currentCodes, code];
        }
        handleThresholdStructureChange(newStructures);
    };

    const isAmountValid = (structureIndex: number, tierIndex: number) => {
        if (tierIndex > 0 && tierIndex < state.structures[structureIndex].tiers.length - 1) {
            const prevAmount = state.structures[structureIndex].tiers[tierIndex - 1]?.amount;
            const currentAmount = state.structures[structureIndex].tiers[tierIndex]?.amount;
            return prevAmount !== undefined && currentAmount !== undefined ? currentAmount > prevAmount : false;
        }
        return true;
    };

    const handleAccumulationTypeChange = (value: RevenueAccumulation) => {
        const newStructures = state.structures.map(structure => ({
            ...structure,
            accumulationType: value,
        }));
        updateState({
            globalAccumulationType: value,
            structures: newStructures
        });
    };


    const handleInvoiceGroupChange = (value: string) => {
        const newInvoiceGroup = parseInt(value, 10);
        setCurrentInvoiceGroup(newInvoiceGroup);
        const newStructures = [...state.structures];
        newStructures[0].invoiceGroup = newInvoiceGroup;
        handleThresholdStructureChange(newStructures);
    };

    const handleCreateThresholdStructures = (checked: boolean) => {
        const updatedStructures = checked ? [{
            tiers: [
                { sharePercentage: state.sharePercentage, amount: 0 },
                { sharePercentage: 0, amount: 0 }
            ],
            revenueCodes: [...revenueCodes],
            accumulationType: DEFAULT_ACCUMULATION_TYPE,
            invoiceGroup: currentInvoiceGroup,
            validationThresholdType: state.structures[0]?.validationThresholdType || null,
            validationThresholdAmount: state.structures[0]?.validationThresholdAmount || 0,
        }] : [{
            tiers: [{ sharePercentage: state.sharePercentage, amount: 0 }],
            revenueCodes: [...revenueCodes],
            accumulationType: DEFAULT_ACCUMULATION_TYPE,
            invoiceGroup: currentInvoiceGroup,
            validationThresholdType: state.structures[0]?.validationThresholdType || null,
            validationThresholdAmount: state.structures[0]?.validationThresholdAmount || 0,
        }];

        updateState({
            createThresholdStructures: checked,
            structures: updatedStructures
        });
    };

    useEffect(() => {
        setDefaultThresholdStructure();
    }, [state.createThresholdStructures, state.enabled]);

    return (
        <TooltipProvider>
            <Card className="w-full border rounded-lg">
                <CardContent className="space-y-6 p-4">
                    <div className="flex items-center justify-between">
                        <div className="space-y-0.5">
                            <Label>Enable Revenue Share</Label>
                            <p className="text-sm text-muted-foreground">
                                Options to configure revenue share contract billing items.
                            </p>
                        </div>
                        <Switch
                            data-qa-id="switch-enableRevenueShare-revenue"
                            checked={state.enabled}
                            onCheckedChange={handleRevenueShareToggle}
                            disabled={!isEditable}
                        />
                    </div>

                    {state.enabled && (
                        <div className="space-y-4">
                            {watchInvoiceGroupingEnabled && (
                                <div className="space-y-2">
                                    <Label htmlFor="invoice-group-0">Invoice</Label>
                                    <Select
                                        data-qa-id="select-invoiceGroup-revenue"
                                        value={currentInvoiceGroup.toString()}
                                        onValueChange={handleInvoiceGroupChange}
                                        disabled={!isEditable}
                                    >
                                        <SelectTrigger id="invoice-group-0">
                                            <SelectValue placeholder="Select Invoice Group" />
                                        </SelectTrigger>
                                        <SelectContent>
                                            {invoiceGroups.map(group => (
                                                <SelectItem key={group.id} value={group.groupNumber.toString()}>
                                                    {group.groupNumber}
                                                </SelectItem>
                                            ))}
                                        </SelectContent>
                                    </Select>
                                </div>
                            )}
                            {
                                !state.createThresholdStructures && (
                                    <div className="space-y-2">
                                        <div className="flex items-center space-x-2">
                                            <Label htmlFor="sharePercentage">Share Percentage</Label>
                                            <Tooltip>
                                                <TooltipTrigger>
                                                    <Info className="w-4 h-4 text-muted-foreground" />
                                                </TooltipTrigger>
                                                <TooltipContent>
                                                    <p>This is the percentage of revenue that Towne Park receives. By default, this will apply to all revenue. You may also create Structures and/or associate different Revenue Codes at different Amounts.</p>
                                                </TooltipContent>
                                            </Tooltip>
                                        </div>
                                        <NumericFormat
                                            data-qa-id="input-sharePercentage-revenue"
                                            required
                                            id="sharePercentage"
                                            displayType="input"
                                            decimalScale={2}
                                            fixedDecimalScale={true}
                                            suffix="%"
                                            inputMode="decimal"
                                            allowNegative={false}
                                            placeholder="Enter percentage"
                                            value={String(state.sharePercentage)}
                                            onValueChange={handleSharePercentageChange}
                                            customInput={Input}
                                            disabled={!isEditable}
                                        />
                                    </div>
                                )}
                            {          /* Create Threshold Structures */}
                            <div className="flex items-center space-x-2">
                                <Checkbox
                                    data-qa-id="checkbox-enableThresholdStructures-revenue"
                                    id="createThresholdStructures"
                                    checked={state.createThresholdStructures}
                                    data-test-id="create-threshold-structures"
                                    onCheckedChange={(checked: boolean) => {
                                        if (isThresholdStructures) {
                                            handleCreateThresholdStructures(checked);
                                        }
                                        setThresholdStructure(checked);

                                    }}
                                    onFocus={() => setThresholdStructure(true)}
                                    onBlur={() => setThresholdStructure(false)}
                                    disabled={!isEditable}
                                />
                                <Label htmlFor="createThresholdStructures">Create Threshold Structures</Label>
                            </div>

                            {state.createThresholdStructures && (
                                <>
                                    <div className="space-y-2">
                                        <div className="flex items-center space-x-2">
                                            <Label>Revenue Accumulation</Label>
                                            <Tooltip>
                                                <TooltipTrigger>
                                                    <Info className="w-4 h-4 text-muted-foreground" />
                                                </TooltipTrigger>
                                                <TooltipContent>
                                                    <p>This setting determines the duration for the revenue to accumulate before triggering the threshold.</p>
                                                </TooltipContent>
                                            </Tooltip>
                                        </div>
                                        <RadioGroup
                                            value={state.structures[0].accumulationType || RevenueAccumulation.MONTHLY}
                                            onValueChange={handleAccumulationTypeChange}
                                            disabled={!isEditable}
                                        >
                                            <div className="flex items-center space-x-2">
                                                <RadioGroupItem data-qa-id="radio-accumulationMonthly-revenue" value={RevenueAccumulation.MONTHLY} id="monthly" />
                                                <Label htmlFor="monthly">Monthly</Label>
                                            </div>
                                            <div className="flex items-center space-x-2">
                                                <RadioGroupItem data-qa-id="radio-accumulationAnnualCalendar-revenue" value={RevenueAccumulation.ANNUALLY_CALENDAR} id="annualCalendar" />
                                                <Label htmlFor="annualCalendar">Annually (Calendar)</Label>
                                            </div>
                                            <div className="flex items-center space-x-2">
                                                <RadioGroupItem data-qa-id="radio-accumulationAnnualAnniversary-revenue" value={RevenueAccumulation.ANNUALLY_ANIVERSARY} id="annualAnniversary" />
                                                <Label htmlFor="annualAnniversary">Annually (Anniversary)</Label>
                                            </div>
                                        </RadioGroup>
                                        <p className="text-sm text-muted-foreground">Revenue accumulation type will apply to all threshold structures.</p>
                                    </div>
                                    <div>
                                        <h3 className="text-base font-bold">Threshold Structures</h3>
                                        <p className="text-sm text-muted-foreground">
                                            Structure for distinguishing separate share percentages at different revenue totals.
                                        </p>
                                    </div>
                                    <Accordion type="multiple" defaultValue={state.expandedItems}>
                                        {state.structures.map((structure, structureIndex) => (
                                            <AccordionItem key={structureIndex} value={`item-${structureIndex}`}>
                                                <AccordionTrigger data-test-id="accordion-trigger">Threshold Structure {structureIndex + 1}</AccordionTrigger>
                                                <AccordionContent>
                                                    {structure.tiers.map((tier, tierIndex) => (
                                                        <div key={tierIndex} className="space-y-2 p-4 border rounded mt-4">
                                                            <h5 className="font-medium">Tier {tierIndex + 1}</h5>
                                                            <div className="space-y-2">
                                                                <div className="flex items-center space-x-2">
                                                                    <Label htmlFor={`sharePercentage-${structureIndex}-${tierIndex}`}>Share Percentage</Label>
                                                                    <Tooltip>
                                                                        <TooltipTrigger>
                                                                            <Info className="w-4 h-4 text-muted-foreground" />
                                                                        </TooltipTrigger>
                                                                        <TooltipContent>
                                                                            <p>This percentage applies to the revenue of the specified tier.</p>
                                                                        </TooltipContent>
                                                                    </Tooltip>
                                                                </div>
                                                                <NumericFormat
                                                                    data-qa-id={`input-tierSharePercentage-${structureIndex}-${tierIndex}-revenue`}
                                                                    required
                                                                    id={`sharePercentage-${structureIndex}-${tierIndex}`}
                                                                    displayType="input"
                                                                    decimalScale={2}
                                                                    fixedDecimalScale={true}
                                                                    suffix="%"
                                                                    inputMode="decimal"
                                                                    allowNegative={false}
                                                                    placeholder="Enter percentage"
                                                                    value={String(tier.sharePercentage)}
                                                                    onValueChange={(values) => handleTierChange(structureIndex, tierIndex, "sharePercentage", values.floatValue || 0)}
                                                                    customInput={Input}
                                                                    disabled={!isEditable}
                                                                />
                                                            
                                                            </div>
                                                            {tierIndex < structure.tiers.length - 1 && structure.tiers.length > 1 && (
                                                                <div className="space-y-2">
                                                                    <div className="flex items-center space-x-2">
                                                                        <Label htmlFor={`amount-${structureIndex}-${tierIndex}`}>Amount</Label>
                                                                        <Tooltip>
                                                                            <TooltipTrigger>
                                                                                <Info className="w-4 h-4 text-muted-foreground" />
                                                                            </TooltipTrigger>
                                                                            <TooltipContent>
                                                                                <p>Revenue amount up to which this share percentage applies.</p>
                                                                            </TooltipContent>
                                                                        </Tooltip>
                                                                    </div>
                                                                    <NumericFormat
                                                                        data-qa-id={`input-tierAmount-${structureIndex}-${tierIndex}-revenue`}
                                                                        required
                                                                        id={`amount-${structureIndex}-${tierIndex}`}
                                                                        displayType="input"
                                                                        thousandSeparator={true}
                                                                        decimalScale={2}
                                                                        fixedDecimalScale={true}
                                                                        prefix="$"
                                                                        inputMode="decimal"
                                                                        allowNegative={false}
                                                                        placeholder="Enter amount"
                                                                        value={String(tier.amount)}
                                                                        onValueChange={(values) => handleTierChange(structureIndex, tierIndex, "amount", values.floatValue || 0)}
                                                                        customInput={Input}
                                                                        disabled={!isEditable}
                                                                    />
                                                                    {!isAmountValid(structureIndex, tierIndex) && (
                                                                        <p className="text-sm text-red-500">Amount must be greater than the previous tier's amount.</p>
                                                                    )}
                                                                </div>
                                                            )}
                                                            {structure.tiers.length > 1 && (
                                                                <Button
                                                                    data-qa-id={`button-removeTier-${structureIndex}-${tierIndex}-revenue`}
                                                                    variant="outline"
                                                                    size="sm"
                                                                    type="button"
                                                                    onClick={() => handleRemoveTier(structureIndex, tierIndex)}
                                                                    className="mt-2"
                                                                    disabled={!isEditable}
                                                                >
                                                                    <MinusCircle className="w-4 h-4 mr-2" />
                                                                    Remove Tier
                                                                </Button>
                                                            )}
                                                        </div>
                                                    ))}
                                                    <Button
                                                        data-qa-id={`button-addTier-${structureIndex}-revenue`}
                                                        variant="outline"
                                                        type="button"
                                                        onClick={() => handleAddTier(structureIndex)}
                                                        className="mt-4"
                                                        data-test-id="add-tier"
                                                        disabled={!isEditable}
                                                    >
                                                        <PlusCircle className="w-4 h-4 mr-2" />
                                                        Add Tier
                                                    </Button>
                                                    {state.structures.length > 1 && (
                                                        <div className="space-y-2 mt-4">
                                                            <div className="flex items-center space-x-2">
                                                                <Label>Revenue Code(s)</Label>
                                                                <Tooltip>
                                                                    <TooltipTrigger>
                                                                        <Info className="w-4 h-4 text-muted-foreground" />
                                                                    </TooltipTrigger>
                                                                    <TooltipContent>
                                                                        <p>Select which revenue codes belong to this structure. All revenue codes must be mapped to a structure.</p>
                                                                    </TooltipContent>
                                                                </Tooltip>
                                                            </div>
                                                            <div className="grid grid-cols-3 gap-2">
                                                                {revenueCodes.map((code) => (
                                                                    <div key={code} className="flex items-center space-x-2">
                                                                        <Checkbox
                                                                            data-qa-id={`checkbox-revenueCode-${code}-${structureIndex}-revenue`}
                                                                            id={`${code}-${structureIndex}`}
                                                                            checked={structure.revenueCodes.includes(code)}
                                                                            onCheckedChange={() => handleRevenueCodeChange(structureIndex, code)}
                                                                            disabled={!isEditable}
                                                                        />
                                                                        <Label htmlFor={`${code}-${structureIndex}`}>{code}</Label>
                                                                    </div>
                                                                ))}
                                                            </div>
                                                        </div>
                                                    )}
                                                    {state.structures.length > 1 && (
                                                        <Button
                                                            data-qa-id={`button-removeStructure-${structureIndex}-revenue`}
                                                            variant="outline"
                                                            size="sm"
                                                            type="button"
                                                            onClick={() => handleRemoveStructure(structureIndex)}
                                                            className="mt-4"
                                                            disabled={!isEditable}
                                                        >
                                                            <MinusCircle className="w-4 h-4 mr-2" />
                                                            Remove Structure
                                                        </Button>
                                                    )}
                                                </AccordionContent>
                                            </AccordionItem>
                                        ))}
                                    </Accordion>
                                    <Button
                                        data-qa-id="button-addStructure-revenue"
                                        variant="outline"
                                        type="button"
                                        onClick={handleAddStructure}
                                        className="mt-4"
                                        disabled={!isEditable}
                                    >
                                        <PlusCircle className="w-4 h-4 mr-2" />
                                        Add Threshold Structure
                                    </Button>
                                </>
                            )}
                            {/* end */}
                            {/* Threshold Validation Section */}
                            <div className="space-y-4 border-t pt-4">
                                <h3 className="text-base font-bold">Validations</h3>
                                <div className="space-y-4 border p-4 rounded-md">
                                    <div className="flex items-center space-x-2">
                                        <Checkbox
                                            data-qa-id="checkbox-enableValidationThreshold-revenue"
                                            id="setValidationThreshold"
                                            checked={state.structures[0]?.validationThresholdType !== null}
                                            onCheckedChange={(checked: boolean) => {
                                                if (isValidationThreshold) {
                                                    const newStructures = [...state.structures];
                                                    if (checked) {
                                                        if (newStructures[0].validationThresholdType === null) {
                                                            newStructures[0].validationThresholdType = ValidationThresholdType.VEHICLE_COUNT;
                                                            newStructures[0].validationThresholdAmount = 0;
                                                        }
                                                    } else {
                                                        newStructures[0].validationThresholdType = null;
                                                        newStructures[0].validationThresholdAmount = 0;
                                                    }
                                                    handleThresholdStructureChange(newStructures);
                                                }
                                                setValidationThreshold(checked);
                                            }}
                                            onFocus={() => setValidationThreshold(true)}
                                            onBlur={() => setValidationThreshold(false)}
                                            disabled={!isEditable}
                                        />

                                        <Label htmlFor="setValidationThreshold">Set Validation Threshold</Label>
                                    </div>

                                    {state.structures[0]?.validationThresholdType !== null && (
                                        <div className="space-y-4">
                                            <div className="space-y-2">
                                                <Label htmlFor="thresholdType">Threshold Type</Label>
                                                <Select
                                                    data-qa-id="select-validationThresholdType-revenue"
                                                    value={state.structures[0]?.validationThresholdType || ''}
                                                    onValueChange={(value) => {
                                                        const newStructures = [...state.structures];
                                                        newStructures[0].validationThresholdType = value as ValidationThresholdType;
                                                        handleThresholdStructureChange(newStructures);
                                                    }}
                                                    disabled={!isEditable}
                                                >
                                                    <SelectTrigger id="thresholdType">
                                                        <SelectValue placeholder="Select threshold type" />
                                                    </SelectTrigger>
                                                    <SelectContent>
                                                        <SelectItem value={ValidationThresholdType.REVENUE_PERCENTAGE}>Revenue Percentage</SelectItem>
                                                        <SelectItem value={ValidationThresholdType.VALIDATION_AMOUNT}>Validation Amount</SelectItem>
                                                        <SelectItem value={ValidationThresholdType.VEHICLE_COUNT}>Vehicle Count</SelectItem>
                                                    </SelectContent>
                                                </Select>
                                            </div>

                                            {state.structures[0]?.validationThresholdType === ValidationThresholdType.REVENUE_PERCENTAGE && (
                                                <div className="space-y-2">
                                                    <Label htmlFor="thresholdPercentage">Percentage</Label>
                                                    <NumericFormat
                                                        data-qa-id="input-validationThresholdPercentage-revenue"
                                                        id="thresholdPercentage"
                                                        required
                                                        displayType="input"
                                                        decimalScale={2}
                                                        fixedDecimalScale={true}
                                                        suffix="%"
                                                        inputMode="decimal"
                                                        allowNegative={false}
                                                        placeholder="Enter percentage"
                                                        value={String(state.structures[0].validationThresholdAmount)}
                                                        onValueChange={(values) => {
                                                            const newStructures = [...state.structures];
                                                            newStructures[0].validationThresholdAmount = values.floatValue || 0;
                                                            handleThresholdStructureChange(newStructures);
                                                        }}
                                                        customInput={Input}
                                                        disabled={!isEditable}
                                                    />
                                                </div>
                                            )}

                                            {state.structures[0]?.validationThresholdType === ValidationThresholdType.VALIDATION_AMOUNT && (
                                                <div className="space-y-2">
                                                    <Label htmlFor="thresholdAmount">Amount</Label>
                                                    <NumericFormat
                                                        data-qa-id="input-validationThresholdAmount-revenue"
                                                        id="thresholdAmount"
                                                        required
                                                        displayType="input"
                                                        decimalScale={2}
                                                        fixedDecimalScale={true}
                                                        prefix="$"
                                                        inputMode="decimal"
                                                        allowNegative={false}
                                                        placeholder="Enter amount"
                                                        value={String(state.structures[0].validationThresholdAmount)}
                                                        onValueChange={(values) => {
                                                            const newStructures = [...state.structures];
                                                            newStructures[0].validationThresholdAmount = values.floatValue || 0;
                                                            handleThresholdStructureChange(newStructures);
                                                        }}
                                                        customInput={Input}
                                                        disabled={!isEditable}
                                                    />
                                                </div>
                                            )}

                                            {state.structures[0]?.validationThresholdType === ValidationThresholdType.VEHICLE_COUNT && (
                                                <div className="space-y-2">
                                                    <Label htmlFor="vehicleCount">Monthly Number of Vehicles</Label>
                                                    <NumericFormat
                                                        data-qa-id="input-validationVehicleCount-revenue"
                                                        id="vehicleCount"
                                                        required
                                                        displayType="input"
                                                        decimalScale={0}
                                                        inputMode="numeric"
                                                        allowNegative={false}
                                                        placeholder="Enter vehicle count"
                                                        value={String(state.structures[0].validationThresholdAmount)}
                                                        onValueChange={(values) => {
                                                            const newStructures = [...state.structures];
                                                            newStructures[0].validationThresholdAmount = values.floatValue || 0;
                                                            handleThresholdStructureChange(newStructures);
                                                        }}
                                                        customInput={Input}
                                                        disabled={!isEditable}
                                                    />
                                                </div>
                                            )}
                                        </div>
                                    )}
                                </div>
                            </div>
                        </div>
                    )}
                </CardContent>
            </Card>
        </TooltipProvider>
    );
}
