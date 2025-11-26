import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { useToast } from "@/components/ui/use-toast";
import { adHocLineItemSchema, AdHocLineItemType, GLCode, UpdateLineItems } from "@/lib/models/Invoice";
import { zodResolver } from "@hookform/resolvers/zod";
import React, { useEffect, useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { Form, FormLabel } from "../ui/form";
import { Dialog, DialogContent, DialogFooter, DialogTitle } from "../ui/dialog";
import { DialogDescription } from "@radix-ui/react-dialog";

interface AdHocLineItemFormProps {
    onAddLineItem: (item: UpdateLineItems) => void;
    invoiceNumber: string;
}

const AdHocLineItemForm: React.FC<AdHocLineItemFormProps> = ({ onAddLineItem, invoiceNumber }) => {
    const [glCodes, setGlCodes] = useState<GLCode[]>([]);
    const [selectedCode, setSelectedCode] = useState<string>("");
    const [selectedTitle, setSelectedTitle] = useState<string>("");
    const { toast } = useToast();

    useEffect(() => {
        const fetchGlCodes = async () => {
            const codeTypes = ['ClientPaidExpense', 'ReimbursableExpense', 'MiscellaneousItem', 'NonBillableExpense'];
            const params = new URLSearchParams();
    
            codeTypes.forEach(type => params.append('codeTypes', type));
    
            try {
                const response = await fetch(`/api/gl-codes?${params.toString()}`);
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                const data: { glCodes: GLCode[] } = await response.json();
                setGlCodes(data.glCodes);
            } catch (error) {
                console.error("Failed to fetch GL codes:", error);
            }
        };
    
        fetchGlCodes();
    }, []);

    const form = useForm({
        resolver: zodResolver(adHocLineItemSchema),
        defaultValues: {
            type: "",
            title: "",
            description: "",
            amount: "",
        },
    });

    const { handleSubmit, control, setValue, watch, reset, formState: { errors } } = form;
    const type = watch("type");

    const handleTypeChange = (value: AdHocLineItemType) => {
        setValue("type", value);

        const displayTitle = getDisplayTitle(value);

        if (value === AdHocLineItemType.MiscellaneousItem || value === AdHocLineItemType.ReimbursableExpense || value === AdHocLineItemType.NonBillableExpense) {
            setValue("title", displayTitle);
            const selectedCode = glCodes.find(c => c.type === value);
            if (selectedCode) {
                setSelectedCode(selectedCode.code);
                setSelectedTitle(selectedCode.name);
            }
        } else if (value === AdHocLineItemType.ClientPaidExpense) {
            setSelectedTitle("");
            setSelectedCode("");
            setValue("title", "");
        }
    };

    const handleCodeChange = (code: string) => {
        setSelectedCode(code);
        const selectedCode = glCodes.find(c => c.code === code);
        if (selectedCode) {
            setSelectedTitle(selectedCode.name);
            setValue("title", selectedCode.name);
        }
    };

    const getInvoiceGroup = (invoiceNumber: string): string => {
        const parts = invoiceNumber.split('-');
        if (parts.length >= 3) {
            const group = parts[2].substring(0, 2);
            return parseInt(group, 10).toString();
        }
        return '';
    };

    const onSubmit = (data: any) => {
        const transformedAmount = data.type === AdHocLineItemType.ClientPaidExpense || data.type === AdHocLineItemType.NonBillableExpense
            ? -Math.abs(data.amount)
            : data.amount;
    
        const { type, ...submitData } = data;
        const payload = {
            code: selectedCode,
            title: selectedTitle,
            ...submitData,
            amount: transformedAmount,
            metaData: {
                isAdhoc: true,
                lineItemType: 'adhoc',
                invoiceGroup: getInvoiceGroup(invoiceNumber),
                ...((type === AdHocLineItemType.ClientPaidExpense && { isProfitDeduction: true }) || type === AdHocLineItemType.NonBillableExpense && { isNonBillableExpense : true})
            }
        };
    
        onAddLineItem(payload as UpdateLineItems);
        reset();
        setSelectedCode("");
        setSelectedTitle("");
    };

    const getDisplayTitle = (type: AdHocLineItemType): string => {
        switch (type) {
            case AdHocLineItemType.MiscellaneousItem:
                return "Miscellaneous";
            case AdHocLineItemType.ClientPaidExpense:
                return "Client Paid Expense";
            case AdHocLineItemType.ReimbursableExpense:
                return "Reimbursable Expense";
            case AdHocLineItemType.NonBillableExpense:
                return "";
            default:
                return "";
        }
    };


    return (
    <Form {...form}>
        <form onSubmit={handleSubmit(onSubmit)} className="mb-4 w-96 space-y-4">
            <h2 className="text-lg font-semibold mb-4">Add Ad-Hoc Line Item</h2>

            <div className="mb-4 space-y-2">
                <FormLabel>Type</FormLabel>
                <Controller
                    name="type"
                    control={control}
                    render={({ field }) => (
                        <Select data-qa-id="select-lineItemType-adhoc" onValueChange={handleTypeChange} value={field.value}>
                            <SelectTrigger>
                                <SelectValue placeholder="Select Type" />
                            </SelectTrigger>
                            <SelectContent>
                                <SelectItem value={AdHocLineItemType.MiscellaneousItem}>Miscellaneous</SelectItem>
                                <SelectItem value={AdHocLineItemType.ClientPaidExpense}>Client Paid Expense</SelectItem>
                                <SelectItem value={AdHocLineItemType.ReimbursableExpense}>Reimbursable Expense</SelectItem>
                                <SelectItem value={AdHocLineItemType.NonBillableExpense}>Non-billable Expense</SelectItem>
                            </SelectContent>
                        </Select>
                    )}
                />
                {errors.type && <p className="text-red-500">{errors.type.message}</p>}
            </div>

            {type && (
                <>
                    {/* Exclude Title for ReimbursableExpense */}
                    {type !== AdHocLineItemType.ReimbursableExpense && (
                        <div className="mb-4 space-y-2">
                            <FormLabel>Title</FormLabel>
                            {(type === AdHocLineItemType.MiscellaneousItem || type === AdHocLineItemType.NonBillableExpense) && (
                                <Controller
                                    name="title"
                                    control={control}
                                    render={({ field }) => (
                                        <Input
                                            data-qa-id="input-lineItemTitle-adhoc"
                                            {...field}
                                            placeholder={`Enter ${type} Title`}
                                            className="w-full border px-2 py-1 mb-2"
                                        />
                                    )}
                                />
                            )}
                            {type === AdHocLineItemType.ClientPaidExpense && (
                                <Controller
                                    name="title"
                                    control={control}
                                    render={({ field }) => (
                                        <Select
                                            data-qa-id="select-expenseCode-adhoc"
                                            onValueChange={(val) => {
                                                handleCodeChange(val);
                                            }}
                                            value={selectedCode}
                                        >
                                            <SelectTrigger className="w-full">
                                                <SelectValue
                                                    placeholder={selectedTitle || "Select concept"}
                                                />
                                            </SelectTrigger>
                                            <SelectContent>
                                                {glCodes.filter(({ type }) => type === AdHocLineItemType.ClientPaidExpense)
                                                    .map(({ code, name }) => (
                                                        <SelectItem key={code} value={code}>
                                                            {name}
                                                        </SelectItem>
                                                    ))}
                                            </SelectContent>
                                        </Select>
                                    )}
                                />
                            )}
                            {errors.title && <p className="text-red-500">{errors.title.message}</p>}
                        </div>
                    )}    
                    <div className="mb-4 space-y-2">
                        <FormLabel>Description</FormLabel>
                        <Controller
                            name="description"
                            control={control}
                            render={({ field }) => (
                                <Textarea
                                    data-qa-id="textarea-lineItemDescription-adhoc"
                                    {...field}
                                    placeholder="Enter Description"
                                    className="w-full border px-2 py-1 mb-2"
                                />
                            )}
                        />
                        {errors.description && <p className="text-red-500">{errors.description.message}</p>}
                    </div>
                </>
            )}

            <div className="mb-8 space-y-2">
                <FormLabel>Amount</FormLabel>
                <Controller
                    name="amount"
                    control={control}
                    render={({ field }) => (
                        <Input
                            data-qa-id="input-lineItemAmount-adhoc"
                            {...field}
                            type="number"
                            placeholder="Amount"
                            className="w-full border px-2 py-1 mb-2"
                            onChange={(e) => field.onChange(e.target.value)}
                        />
                    )}
                />
                {type === AdHocLineItemType.MiscellaneousItem && (
                    <small className="text-muted-foreground">Negative value will credit the client's invoice.</small>
                )}
                {errors.amount && <p className="text-red-500">{errors.amount.message}</p>}
            </div>
            <div className="flex justify-end">
            <Button data-qa-id="button-addLineItem-adhoc" type="submit" className="mt-2">
                Add Line Item
            </Button>
            </div>
        </form>
    </Form>
    );
};

export default AdHocLineItemForm;
