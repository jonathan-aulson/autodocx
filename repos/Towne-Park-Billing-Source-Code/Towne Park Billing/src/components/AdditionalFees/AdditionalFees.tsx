import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Switch } from '@/components/ui/switch';
import { BellService, BellServiceFeeTerms, DepositData, DepositedRevenueTerms, InvoiceGroup, LineTitles, MidMonthAdvancedTerms, MidMonthAdvances } from '@/lib/models/Contract';
import { Info, InfoIcon } from 'lucide-react';
import React, { useEffect, useState } from 'react';
import { NumericFormat } from 'react-number-format';
import { Card, CardContent } from '../ui/card';
import { FormLabel } from '../ui/form';
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '../ui/tooltip';

interface AdditionalFeesProps {
    bellServiceFee: BellServiceFeeTerms;
    midMonthAdvance: MidMonthAdvancedTerms;
    depositedRevenue: DepositedRevenueTerms;
    onUpdateBellServiceFee: (updatedService: BellServiceFeeTerms) => void;
    onUpdateMidMonthAdvanced: (updatedAdvance: MidMonthAdvancedTerms) => void;
    onUpdateDepositedRevenue: (updatedRevenue: DepositedRevenueTerms) => void;
    invoiceGroups: InvoiceGroup[];
    watchInvoiceGroupingEnabled: boolean;
    isEditable: boolean;
}

const lineTitleMap: Record<LineTitles, string> = {
    [LineTitles.LESS_MID_MONTH_BILLING]: "Less: Mid-Month Billing",
    [LineTitles.LESS_PRE_BILL]: "Less: Pre Bill"
};

const AdditionalFees: React.FC<AdditionalFeesProps> = ({
    bellServiceFee,
    midMonthAdvance,
    depositedRevenue,
    onUpdateBellServiceFee,
    onUpdateMidMonthAdvanced,
    onUpdateDepositedRevenue,
    invoiceGroups,
    watchInvoiceGroupingEnabled,
    isEditable
}) => {
    const [isBellServiceEnabled, setIsBellServiceEnabled] = useState(bellServiceFee.enabled);
    const [isMidMonthAdvanceEnabled, setIsMidMonthAdvanceEnabled] = useState(midMonthAdvance.enabled);
    const [isDepositedRevenueEnabled, setIsDepositedRevenueEnabled] = useState(depositedRevenue.enabled);
    const [midMonthAdvances, setMidMonthAdvances] = useState<MidMonthAdvances[]>(midMonthAdvance.midMonthAdvances);
    const [depositData, setDepositData] = useState<DepositData[]>(depositedRevenue.depositData);
    const [bellServices, setBellServices] = useState<BellService[]>(bellServiceFee.bellServices);

    const [isDirty, setIsDirty] = useState(false);

    useEffect(() => {
        if (!isEditable && isDirty) {
            setIsBellServiceEnabled(bellServiceFee.enabled);
            setBellServices(bellServiceFee.bellServices);
            setIsMidMonthAdvanceEnabled(midMonthAdvance.enabled);
            setMidMonthAdvances(midMonthAdvance.midMonthAdvances);
            setIsDepositedRevenueEnabled(depositedRevenue.enabled);
            setDepositData(depositedRevenue.depositData);
            setIsDirty(false);
        }
    }, [isEditable]);

    useEffect(() => {
        if (!isEditable) {
            setIsBellServiceEnabled(bellServiceFee.enabled);
            setBellServices(bellServiceFee.bellServices);
            setIsMidMonthAdvanceEnabled(midMonthAdvance.enabled);
            setMidMonthAdvances(midMonthAdvance.midMonthAdvances);
            setIsDepositedRevenueEnabled(depositedRevenue.enabled);
            setDepositData(depositedRevenue.depositData);
        }
    }, [bellServiceFee, midMonthAdvance, depositedRevenue, isEditable]);

    const handleBellServiceFeeChange = () => {
        setIsDirty(true);
        const bellServiceValue = bellServices.length === 0 ? [{ id: '', invoiceGroup: 1 }] : bellServices;
        setBellServices(bellServiceValue);
        const updatedFee: BellServiceFeeTerms = { enabled: !isBellServiceEnabled, bellServices: bellServiceValue };
        setIsBellServiceEnabled(!isBellServiceEnabled);
        onUpdateBellServiceFee(updatedFee);
    };

    const handleBellServiceInvoiceGroupChange = (index: number, value: number) => {
        setIsDirty(true);
        const updatedServices = [...bellServices];
        updatedServices[index] = { ...updatedServices[index], invoiceGroup: value };
        setBellServices(updatedServices);
        onUpdateBellServiceFee({ enabled: isBellServiceEnabled, bellServices: updatedServices });
    };

    const handleMidMonthAdvanceChange = () => {
        setIsDirty(true);
        const midMonthAdvanceValue = midMonthAdvances.length === 0 ? [{ id: '', amount: 0, lineTitle: "" as LineTitles, invoiceGroup: 1 }] : midMonthAdvances;
        setMidMonthAdvances(midMonthAdvanceValue);
        const updatedAdvance: MidMonthAdvancedTerms = { enabled: !isMidMonthAdvanceEnabled, midMonthAdvances: midMonthAdvanceValue };
        setIsMidMonthAdvanceEnabled(!isMidMonthAdvanceEnabled);
        onUpdateMidMonthAdvanced(updatedAdvance);
    };

    const handleDepositDataChange = (index: number, fieldName: keyof DepositData, value: any) => {
        setIsDirty(true);
        let updatedData = [...depositData];

        if (updatedData.length === 0) {
            updatedData = [{
                id: '',
                towneParkResponsibleForParkingTax: false,
                depositedRevenueEnabled: false,
                invoiceGroup: 1
            }];
        }

        updatedData[index] = { ...updatedData[index], [fieldName]: value };
        setDepositData(updatedData);
        onUpdateDepositedRevenue({ enabled: true, depositData: updatedData });
    };

    const handleMidMonthAdvancesChange = (index: number, fieldName: keyof MidMonthAdvances, value: any) => {
        setIsDirty(true);
        const updatedPayments = [...midMonthAdvances];
        updatedPayments[index] = { ...updatedPayments[index], [fieldName]: value };
        setMidMonthAdvances(updatedPayments);
        onUpdateMidMonthAdvanced({ enabled: isMidMonthAdvanceEnabled, midMonthAdvances: updatedPayments });
    };

    return (
        <Card className="w-full border rounded-lg">
            <CardContent className="space-y-6 p-4">
                <div className="space-y-4">

                    {/* Mid-Month Advance Section */}
                    <div className="flex items-center justify-between">
                        <div className="space-y-0.5">
                            <Label htmlFor="midMonthAdvances">
                                Mid-Month Advance
                            </Label>
                            <p className="text-sm text-muted-foreground">
                                Enable Mid-Month Advances to Towne Park.
                            </p>
                        </div>
                        <Switch data-qa-id="switch-toggle-midMonthAdvance" id="midMonthAdvances" checked={isMidMonthAdvanceEnabled} onCheckedChange={handleMidMonthAdvanceChange} disabled={!isEditable} />
                    </div>
                    {isMidMonthAdvanceEnabled && midMonthAdvances.map((payment, index) => (
                        <div key={payment.id || index} className="space-y-4 pl-6 gap-4 p-4 border rounded-lg">
                            <div className='space-y-2'>
                                <div className="flex items-center space-x-2">
                                    <FormLabel htmlFor={`amount-${index}`}>
                                        Advancement Amount
                                    </FormLabel>
                                    <TooltipProvider>
                                        <Tooltip>
                                            <TooltipTrigger asChild>
                                                <InfoIcon className="h-4 w-4 text-muted-foreground" />
                                            </TooltipTrigger>
                                            <TooltipContent>
                                                <p>The amount which is advanced to Towne Park mid-month and will be credited back to the customer on the invoice selected.</p>
                                            </TooltipContent>
                                        </Tooltip>
                                    </TooltipProvider>
                                </div>
                                <NumericFormat
                                    data-qa-id="input-amount-midMonthAdvance"
                                    id={`amount-${index}`}
                                    displayType="input"
                                    thousandSeparator={true}
                                    decimalScale={2}
                                    fixedDecimalScale={true}
                                    prefix="$"
                                    inputMode="numeric"
                                    allowNegative={false}
                                    placeholder="Amount"
                                    value={payment.amount}
                                    onValueChange={(e) => handleMidMonthAdvancesChange(index, 'amount', parseFloat(e.value))}
                                    customInput={Input}
                                    disabled={!isEditable}
                                />
                            </div>
                            <div className='space-y-2'>
                                <Label htmlFor={`lineItemTitle-${index}`}>Line-Item Title</Label>
                                <Select
                                    data-qa-id="select-lineTitle-midMonthAdvance"
                                    value={payment.lineTitle || undefined}
                                    onValueChange={(value: LineTitles) => handleMidMonthAdvancesChange(index, 'lineTitle', value)}
                                    required
                                    disabled={!isEditable}
                                >
                                    <SelectTrigger>
                                        <SelectValue placeholder="Select line-item title" />
                                    </SelectTrigger>
                                    <SelectContent>
                                        {Object.values(LineTitles).map(title => (
                                            <SelectItem key={title} value={title}>{lineTitleMap[title]}</SelectItem>
                                        ))}
                                    </SelectContent>
                                </Select>
                            </div>
                            {watchInvoiceGroupingEnabled && (
                                <div className='space-y-2'>
                                   
                                    <Label htmlFor={`midMonthAdvanceInvoice-${index}`}>Invoice</Label>
                                    
                                    
                                       


                                    <Select
                                        data-qa-id="select-invoice-midMonthAdvance"
                                        value={payment.invoiceGroup.toString()}
                                        onValueChange={(value) => handleMidMonthAdvancesChange(index, 'invoiceGroup', parseInt(value, 10))}
                                        disabled={!isEditable}
                                    >
                                        <SelectTrigger>
                                            <SelectValue placeholder="Select invoice" />
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
                        </div>
                    ))}

                    {/* Towne Park Deposited Revenue Section */}
                    <div className="space-y-4">
                            <div className="gap-4 p-2 border rounded-lg">
                                {/* Independent Toggles */}
                                <div className="flex flex-col space-y-4">
                                    <div className="flex items-center justify-between">
                                        <div className="flex items-center space-x-2">
                                            <div className="space-y-0.5">
                                                <Label htmlFor="depositedRevenueEnabled-0" className="text-base">
                                                    Towne Park Deposited Revenue
                                                </Label>
                                                <p className="text-sm text-muted-foreground">
                                                    Enable to include deposited revenue as a line item in the billing statement.
                                                </p>
                                            </div>
                                        </div>
                                        <Switch  
                                            data-qa-id="switch-toggle-depositedRevenue"
                                            id="depositedRevenueEnabled-0"
                                            checked={depositData[0]?.depositedRevenueEnabled || false}
                                            onCheckedChange={(checked) => handleDepositDataChange(0, 'depositedRevenueEnabled', checked)}
                                            disabled={!isEditable}
                                        />
                                    </div>
                                    <div className="flex items-center justify-between">
                                        <div className="flex items-center space-x-2">
                                            <div className="space-y-0.5">
                                                <Label htmlFor="parkingTaxResponsible-0" className="text-base">
                                                    Towne Park Responsible for Parking Tax
                                                </Label>
                                                <p className="text-sm text-muted-foreground">
                                                    If disabled, the system uses the Towne Park deposited external revenue as the invoice amount. If enabled, it uses the Towne Park deposited NET external revenue.
                                                </p>
                                            </div>
                                        </div>
                                        <Switch  
                                            data-qa-id="switch-toggle-parkingTax"
                                            id="parkingTaxResponsible-0"
                                            checked={depositData[0]?.towneParkResponsibleForParkingTax || false}
                                            onCheckedChange={(checked) => handleDepositDataChange(0, 'towneParkResponsibleForParkingTax', checked)}
                                            disabled={!isEditable}
                                        />
                                    </div>

                                    {/* Invoice Group Selection */}
                                    {watchInvoiceGroupingEnabled &&
                                        (depositData[0]?.depositedRevenueEnabled || depositData[0]?.towneParkResponsibleForParkingTax) && (
                                            <div className='space-y-2 mt-4'>
                                                  <div className="flex items-center space-x-2">
                                                <Label htmlFor="depositedRevenueInvoice-0">Invoice</Label>
                                                <TooltipProvider>
                                        <Tooltip>
                                            <TooltipTrigger>
                                                <Info className="w-4 h-4 text-muted-foreground" />
                                            </TooltipTrigger>
                                            <TooltipContent>
                                                <p>The Towne Park Deposited Revenue and Towne Park Responsible for Parking Tax will all be grouped together on the same invoice.</p>
                                            </TooltipContent>
                                        </Tooltip>
                                    </TooltipProvider>
                                    </div>
                                                <Select
                                                    data-qa-id="select-invoice-depositedRevenue"
                                                    value={(depositData[0]?.invoiceGroup || 1).toString()}
                                                    onValueChange={(value) => handleDepositDataChange(0, 'invoiceGroup', parseInt(value, 10))}
                                                    disabled={!isEditable}
                                                >
                                                    <SelectTrigger>
                                                        <SelectValue placeholder="Select invoice" />
                                                    </SelectTrigger>
                                                    <SelectContent>
                                                        {invoiceGroups.map(group => (
                                                            <SelectItem key={group.id} value={group.groupNumber.toString()}>
                                                                {group.groupNumber || 1}
                                                            </SelectItem>
                                                        ))}
                                                    </SelectContent>
                                                </Select>
                                            </div>
                                        )}
                                </div>
                            </div>
                    </div>

                    {/* Bell Service Fee Section */}
                    <div className="flex items-center justify-between">
                        <div className="space-y-0.5">
                            <Label htmlFor="bellServiceFee">
                                Bell Service Fee
                            </Label>
                            <p className="text-sm text-muted-foreground">
                                Enable to include bell service fee as a line item in the billing statement.
                            </p>
                        </div>
                        <Switch data-qa-id="switch-toggle-bellService" id="bellServiceFee" checked={isBellServiceEnabled} onCheckedChange={handleBellServiceFeeChange} disabled={!isEditable} />
                    </div>
                    {isBellServiceEnabled && watchInvoiceGroupingEnabled && (
                        <div className="space-y-4 pl-6 gap-4 p-4 border rounded-lg">
                            {bellServices.map((service, index) => (
                                <div key={service.id || index} className='space-y-2'>
                                    <Label htmlFor={`bellServiceInvoice-${index}`}>Invoice</Label>
                                    <Select
                                        data-testid={`bell-service-invoice-${index}`}
                                        data-qa-id="select-invoice-bellService"
                                        onValueChange={(value) => handleBellServiceInvoiceGroupChange(index, parseInt(value, 10))}
                                        value={service.invoiceGroup?.toString()}
                                        disabled={!isEditable}
                                    >
                                        <SelectTrigger>
                                            <SelectValue placeholder="Select invoice" />
                                        </SelectTrigger>
                                        <SelectContent>
                                            {invoiceGroups.map(group => (
                                                <SelectItem key={group.id} value={group.groupNumber.toString()}>
                                                    {group.groupNumber || 1}
                                                </SelectItem>
                                            ))}
                                        </SelectContent>
                                    </Select>
                                </div>
                            ))}
                        </div>
                    )}
                </div>
            </CardContent>
        </Card>
    );
};

export default AdditionalFees;
