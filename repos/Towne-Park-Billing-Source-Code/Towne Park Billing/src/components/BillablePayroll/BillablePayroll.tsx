import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { Checkbox } from "@/components/ui/checkbox";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { BillablePayrollTerms, EscalatorFormatType, InvoiceGroup, Month, PayrollAccount, PTEBBillingType, SupportPayrollType, SupportServicesType } from "@/lib/models/Contract";
import { DialogDescription } from "@radix-ui/react-dialog";
import { Info, Plus, X } from "lucide-react";
import React, { useEffect, useRef, useState } from "react";
import { NumericFormat } from "react-number-format";
import { RadioGroup, RadioGroupItem } from "../ui/radio-group";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "../ui/select";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "../ui/tooltip";

interface PayrollDataItem {
    code: string;
    title: string;
    isEnabled: boolean;
}

interface BillablePayrollComponentProps {
    billableAccounts: BillablePayrollTerms;
    onUpdateBillableAccounts: (updatedPayroll: BillablePayrollTerms) => void;
    invoiceGroups: InvoiceGroup[];
    watchInvoiceGroupingEnabled: boolean;
    payrrolAccount: Record<string, string>;
    expenseAccount: Record<string, string>;
    errors: any;
    isEditable: boolean;
}

const BillablePayrollComponent: React.FC<BillablePayrollComponentProps> = ({
    billableAccounts,
    onUpdateBillableAccounts,
    invoiceGroups,
    watchInvoiceGroupingEnabled,
    payrrolAccount,
    expenseAccount,
    errors,
    isEditable,
}) => {
    const [state, setState] = useState(() => {
        const initialPayrollDataString = billableAccounts.billableAccountsData.length > 0 ? billableAccounts.billableAccountsData[0].payrollAccountsData : '';
        const initialPayrollData: PayrollDataItem[] = initialPayrollDataString ? JSON.parse(initialPayrollDataString) : [];

        const allPayrollAccounts = Object.keys(payrrolAccount).map(code => ({
            code,
            title: payrrolAccount[code] || '',
            isEnabled: initialPayrollData.length > 0
                ? initialPayrollData.some(data => data.code === code ? data.isEnabled : true)
                : !(code === "6010" || code === "6014")
        }));

        if (initialPayrollData.length > 0) {
            initialPayrollData.forEach(savedData => {
                const index = allPayrollAccounts.findIndex(account => account.code === savedData.code);
                if (index !== -1) {
                    allPayrollAccounts[index].isEnabled = savedData.isEnabled;
                }
            });
        }

        const initialExpenseDataString = billableAccounts.billableAccountsData.length > 0 ? billableAccounts.billableAccountsData[0].payrollExpenseAccountsData : '';
        const initialExpenseData: PayrollDataItem[] = initialExpenseDataString ? JSON.parse(initialExpenseDataString) : [];

        const allExpenseAccounts = Object.keys(expenseAccount).map(code => ({
            code,
            title: expenseAccount[code] || '',
            isEnabled: initialExpenseData.length > 0
                ? initialExpenseData.some(data => data.code === code ? data.isEnabled : true)
                : !(code === "7005" || code === "7016")
        }));

        if (initialExpenseData.length > 0) {
            initialExpenseData.forEach(savedData => {
                const index = allExpenseAccounts.findIndex(account => account.code === savedData.code);
                if (index !== -1) {
                    allExpenseAccounts[index].isEnabled = savedData.isEnabled;
                }
            });
        }

        return {
            isPayrollEnabled: billableAccounts.enabled,
            payrollData: allPayrollAccounts,
            payrollLineItemDisplayName: billableAccounts.billableAccountsData[0]?.payrollAccountsLineTitle || "Site Salaries & Wages",
            invoiceGroup: billableAccounts.billableAccountsData[0]?.payrollAccountsInvoiceGroup ?? 1,
            isSupportServicesEnabled: billableAccounts.billableAccountsData[0]?.payrollSupportEnabled || false,
            supportServicesBillingType: billableAccounts.billableAccountsData[0]?.payrollSupportBillingType || SupportServicesType.FIXED,
            supportServicesPayrollType: billableAccounts.billableAccountsData[0]?.payrollSupportPayrollType || SupportPayrollType.BILLABLE,
            supportServicesFixedAmount: billableAccounts.billableAccountsData[0]?.payrollSupportAmount || null,
            supportServicesLineItemDisplayName: billableAccounts.billableAccountsData[0]?.payrollSupportLineTitle || "Support Services",
            expenseAccountsData: allExpenseAccounts,
            expenseAccountsLineTitle: billableAccounts.billableAccountsData[0]?.payrollExpenseAccountsLineTitle || "Total Other Expenses",
            expenseAccountsInvoiceGroup: billableAccounts.billableAccountsData[0]?.payrollExpenseAccountsInvoiceGroup ?? 1,
            additionalPayrollAmount: billableAccounts.billableAccountsData[0]?.additionalPayrollAmount || 0,
            payrollTaxesEscalatorEnable: billableAccounts.billableAccountsData[0]?.payrollTaxesEscalatorEnable || false,
            payrollTaxesEscalatorMonth: billableAccounts.billableAccountsData[0]?.payrollTaxesEscalatorMonth || Month.JANUARY,
            payrollTaxesEscalatorvalue: billableAccounts.billableAccountsData[0]?.payrollTaxesEscalatorvalue ?? 0,
            payrollTaxesEscalatorType: billableAccounts.billableAccountsData[0]?.payrollTaxesEscalatorType || EscalatorFormatType.PERCENTAGE
        };
    });

    const [isPayrollDialogOpen, setIsPayrollDialogOpen] = useState(false);
    const [isPTEBEnabled, setIsPTEBEnabled] = useState(billableAccounts.billableAccountsData[0]?.payrollTaxesEnabled || false);
    const [ptebBillingType, setPTEBBillingType] = useState<PTEBBillingType>(billableAccounts.billableAccountsData[0]?.payrollTaxesBillingType || PTEBBillingType.ACTUAL);
    const [ptebPercentage, setPTEBPercentage] = useState<number | null>(billableAccounts.billableAccountsData[0]?.payrollTaxesPercentage || null);
    const [ptebLineItemDisplayName, setPTEBLineItemDisplayName] = useState<string>(billableAccounts.billableAccountsData[0]?.payrollTaxesLineTitle || "PTEB");
    const [isExpenseAccountsDialogOpen, setIsExpenseAccountsDialogOpen] = useState(false);
    const [isDirty, setIsDirty] = useState(false);
    const [isEditingPTEB, setIsEditingPTEB] = useState(false);
    const [isEditingPtebEscalator, setIsEditingPtebEscalator] = useState(false);
    const [isEditingSupportServices, setIsEditingSupportServices] = useState(false);
    const [isPTEBEEscalatorEnabled, setisPTEBEEscalatorEnabled] = useState(billableAccounts.billableAccountsData[0]?.payrollTaxesEscalatorEnable || false);
    const [isPTEBEEscalatorType, setIsPTEBEEscalatorType] = useState<EscalatorFormatType>(billableAccounts.billableAccountsData[0]?.payrollTaxesEscalatorType || EscalatorFormatType.PERCENTAGE);
     const [ptebEscalatorPercentage, setptebEscalatorPercentage] = useState<number | null>(billableAccounts.billableAccountsData[0]?.payrollTaxesEscalatorvalue || 0);
    


    const originalStateRef = useRef({
        state,
        isPTEBEnabled,
        ptebBillingType,
        ptebPercentage,
        ptebLineItemDisplayName,
        isPTEBEEscalatorType,
        isPTEBEEscalatorEnabled,
        ptebEscalatorPercentage
    });

    useEffect(() => {
        if (!isEditable) {
            if (isDirty) {
                setState(originalStateRef.current.state);
                setIsPTEBEnabled(originalStateRef.current.isPTEBEnabled);
                setPTEBBillingType(originalStateRef.current.ptebBillingType);
                setIsPTEBEEscalatorType(originalStateRef.current.isPTEBEEscalatorType)
                setisPTEBEEscalatorEnabled(originalStateRef.current.isPTEBEEscalatorEnabled)
                setPTEBPercentage(originalStateRef.current.ptebPercentage);
                setptebEscalatorPercentage(originalStateRef.current.ptebEscalatorPercentage)
                setPTEBLineItemDisplayName(originalStateRef.current.ptebLineItemDisplayName);
                setIsDirty(false);
            } else {
                setState(() => {

                    const initialPayrollDataString = billableAccounts.billableAccountsData.length > 0 ? billableAccounts.billableAccountsData[0].payrollAccountsData : '';
                    const initialPayrollData: PayrollDataItem[] = initialPayrollDataString ? JSON.parse(initialPayrollDataString) : [];

                    const allPayrollAccounts = Object.keys(payrrolAccount).map(code => ({
                        code,
                        title: payrrolAccount[code] || '',
                        isEnabled: initialPayrollData.length > 0
                            ? initialPayrollData.some(data => data.code === code ? data.isEnabled : true)
                            : !(code === "6010" || code === "6014")
                    }));

                    if (initialPayrollData.length > 0) {
                        initialPayrollData.forEach(savedData => {
                            const index = allPayrollAccounts.findIndex(account => account.code === savedData.code);
                            if (index !== -1) {
                                allPayrollAccounts[index].isEnabled = savedData.isEnabled;
                            }
                        });
                    }

                    const initialExpenseDataString = billableAccounts.billableAccountsData.length > 0 ? billableAccounts.billableAccountsData[0].payrollExpenseAccountsData : '';
                    const initialExpenseData: PayrollDataItem[] = initialExpenseDataString ? JSON.parse(initialExpenseDataString) : [];

                    const allExpenseAccounts = Object.keys(expenseAccount).map(code => ({
                        code,
                        title: expenseAccount[code] || '',
                        isEnabled: initialExpenseData.length > 0
                            ? initialExpenseData.some(data => data.code === code ? data.isEnabled : true)
                            : !(code === "7005" || code === "7016")
                    }));

                    if (initialExpenseData.length > 0) {
                        initialExpenseData.forEach(savedData => {
                            const index = allExpenseAccounts.findIndex(account => account.code === savedData.code);
                            if (index !== -1) {
                                allExpenseAccounts[index].isEnabled = savedData.isEnabled;
                            }
                        });
                    }

                    return {
                        isPayrollEnabled: billableAccounts.enabled,
                        payrollData: allPayrollAccounts,
                        payrollLineItemDisplayName: billableAccounts.billableAccountsData[0]?.payrollAccountsLineTitle || "Site Salaries & Wages",
                        invoiceGroup: billableAccounts.billableAccountsData[0]?.payrollAccountsInvoiceGroup ?? 1,
                        isSupportServicesEnabled: billableAccounts.billableAccountsData[0]?.payrollSupportEnabled || false,
                        supportServicesBillingType: billableAccounts.billableAccountsData[0]?.payrollSupportBillingType || SupportServicesType.FIXED,
                        supportServicesPayrollType: billableAccounts.billableAccountsData[0]?.payrollSupportPayrollType || SupportPayrollType.BILLABLE,
                        supportServicesFixedAmount: billableAccounts.billableAccountsData[0]?.payrollSupportAmount || null,
                        supportServicesLineItemDisplayName: billableAccounts.billableAccountsData[0]?.payrollSupportLineTitle || "Support Services",
                        expenseAccountsData: allExpenseAccounts,
                        expenseAccountsLineTitle: billableAccounts.billableAccountsData[0]?.payrollExpenseAccountsLineTitle || "Total Other Expenses",
                        expenseAccountsInvoiceGroup: billableAccounts.billableAccountsData[0]?.payrollExpenseAccountsInvoiceGroup ?? 1,
                        additionalPayrollAmount: billableAccounts.billableAccountsData[0]?.additionalPayrollAmount || 0,
                        payrollTaxesEscalatorEnable: billableAccounts.billableAccountsData[0]?.payrollTaxesEscalatorEnable || false,
                        payrollTaxesEscalatorMonth: billableAccounts.billableAccountsData[0]?.payrollTaxesEscalatorMonth || Month.JANUARY,
                        payrollTaxesEscalatorvalue: billableAccounts.billableAccountsData[0]?.payrollTaxesEscalatorvalue ?? 0,
                        payrollTaxesEscalatorType: billableAccounts.billableAccountsData[0]?.payrollTaxesEscalatorType || EscalatorFormatType.PERCENTAGE
                    };
                });
                setIsPTEBEnabled(billableAccounts.billableAccountsData[0]?.payrollTaxesEnabled || false);
                setPTEBBillingType(billableAccounts.billableAccountsData[0]?.payrollTaxesBillingType || PTEBBillingType.ACTUAL);
                setIsPTEBEEscalatorType(billableAccounts.billableAccountsData[0]?.payrollTaxesEscalatorType || EscalatorFormatType.PERCENTAGE);
                setisPTEBEEscalatorEnabled(billableAccounts.billableAccountsData[0]?.payrollTaxesEscalatorEnable || false)
                setPTEBPercentage(billableAccounts.billableAccountsData[0]?.payrollTaxesPercentage || null);
                setptebEscalatorPercentage(billableAccounts.billableAccountsData[0]?.payrollTaxesEscalatorvalue || 0)
                setPTEBLineItemDisplayName(billableAccounts.billableAccountsData[0]?.payrollTaxesLineTitle || "PTEB");
            }
        }
    }, [isEditable, billableAccounts]);

    useEffect(() => {
        if (!isEditable) {
            setState(() => {
                const initialPayrollDataString = billableAccounts.billableAccountsData.length > 0 ? billableAccounts.billableAccountsData[0].payrollAccountsData : '';
                const initialPayrollData: PayrollDataItem[] = initialPayrollDataString ? JSON.parse(initialPayrollDataString) : [];

                const allPayrollAccounts = Object.keys(payrrolAccount).map(code => ({
                    code,
                    title: payrrolAccount[code] || '',
                    isEnabled: initialPayrollData.length > 0
                        ? initialPayrollData.some(data => data.code === code ? data.isEnabled : true)
                        : !(code === "6010" || code === "6014")
                }));

                if (initialPayrollData.length > 0) {
                    initialPayrollData.forEach(savedData => {
                        const index = allPayrollAccounts.findIndex(account => account.code === savedData.code);
                        if (index !== -1) {
                            allPayrollAccounts[index].isEnabled = savedData.isEnabled;
                        }
                    });
                }

                const initialExpenseDataString = billableAccounts.billableAccountsData.length > 0 ? billableAccounts.billableAccountsData[0].payrollExpenseAccountsData : '';
                const initialExpenseData: PayrollDataItem[] = initialExpenseDataString ? JSON.parse(initialExpenseDataString) : [];

                const allExpenseAccounts = Object.keys(expenseAccount).map(code => ({
                    code,
                    title: expenseAccount[code] || '',
                    isEnabled: initialExpenseData.length > 0
                        ? initialExpenseData.some(data => data.code === code ? data.isEnabled : true)
                        : !(code === "7005" || code === "7016")
                }));

                if (initialExpenseData.length > 0) {
                    initialExpenseData.forEach(savedData => {
                        const index = allExpenseAccounts.findIndex(account => account.code === savedData.code);
                        if (index !== -1) {
                            allExpenseAccounts[index].isEnabled = savedData.isEnabled;
                        }
                    });
                }

                return {
                    isPayrollEnabled: billableAccounts.enabled,
                    payrollData: allPayrollAccounts,
                    payrollLineItemDisplayName: billableAccounts.billableAccountsData[0]?.payrollAccountsLineTitle || "Site Salaries & Wages",
                    invoiceGroup: billableAccounts.billableAccountsData[0]?.payrollAccountsInvoiceGroup ?? 1,
                    isSupportServicesEnabled: billableAccounts.billableAccountsData[0]?.payrollSupportEnabled || false,
                    supportServicesBillingType: billableAccounts.billableAccountsData[0]?.payrollSupportBillingType || SupportServicesType.FIXED,
                    supportServicesPayrollType: billableAccounts.billableAccountsData[0]?.payrollSupportPayrollType || SupportPayrollType.BILLABLE,
                    supportServicesFixedAmount: billableAccounts.billableAccountsData[0]?.payrollSupportAmount || null,
                    supportServicesLineItemDisplayName: billableAccounts.billableAccountsData[0]?.payrollSupportLineTitle || "Support Services",
                    expenseAccountsData: allExpenseAccounts,
                    expenseAccountsLineTitle: billableAccounts.billableAccountsData[0]?.payrollExpenseAccountsLineTitle || "Total Other Expenses",
                    expenseAccountsInvoiceGroup: billableAccounts.billableAccountsData[0]?.payrollExpenseAccountsInvoiceGroup ?? 1,
                    additionalPayrollAmount: billableAccounts.billableAccountsData[0]?.additionalPayrollAmount || 0,
                    payrollTaxesEscalatorEnable: billableAccounts.billableAccountsData[0]?.payrollTaxesEscalatorEnable || false,
                    payrollTaxesEscalatorMonth: billableAccounts.billableAccountsData[0]?.payrollTaxesEscalatorMonth || Month.JANUARY,
                    payrollTaxesEscalatorvalue: billableAccounts.billableAccountsData[0]?.payrollTaxesEscalatorvalue || 0,
                    payrollTaxesEscalatorType: billableAccounts.billableAccountsData[0]?.payrollTaxesEscalatorType || EscalatorFormatType.PERCENTAGE
                };
            });
            setIsPTEBEnabled(billableAccounts.billableAccountsData[0]?.payrollTaxesEnabled || false);
            setPTEBBillingType(billableAccounts.billableAccountsData[0]?.payrollTaxesBillingType || PTEBBillingType.ACTUAL);
            setPTEBPercentage(billableAccounts.billableAccountsData[0]?.payrollTaxesPercentage || null);
            setPTEBLineItemDisplayName(billableAccounts.billableAccountsData[0]?.payrollTaxesLineTitle || "PTEB");
            setIsPTEBEEscalatorType(billableAccounts.billableAccountsData[0]?.payrollTaxesEscalatorType || EscalatorFormatType.PERCENTAGE);
            setisPTEBEEscalatorEnabled(billableAccounts.billableAccountsData[0]?.payrollTaxesEscalatorEnable || false)
                // setptebEscalatorPercentage(billableAccounts.billableAccountsData[0]?.payrollTaxesEscalatorvalue || 0)
        }
    }, [billableAccounts, isEditable]);

    const notifyParent = () => {
        const updatedTerms: BillablePayrollTerms = {
            ...billableAccounts,
            enabled: state.isPayrollEnabled,
            billableAccountsData: [
                {
                    ...billableAccounts.billableAccountsData[0],
                    payrollAccountsData: JSON.stringify(state.payrollData),
                    payrollAccountsLineTitle: state.payrollLineItemDisplayName,
                    payrollAccountsInvoiceGroup: state.invoiceGroup,
                    payrollTaxesEnabled: isPTEBEnabled,
                    payrollTaxesBillingType: ptebBillingType,
                    payrollTaxesPercentage: ptebPercentage || null,
                    payrollTaxesLineTitle: ptebLineItemDisplayName,
                    payrollSupportEnabled: state.isSupportServicesEnabled,
                    payrollSupportBillingType: state.supportServicesBillingType,
                    payrollSupportPayrollType: state.supportServicesPayrollType,
                    payrollSupportAmount: state.supportServicesFixedAmount || null,
                    payrollSupportLineTitle: state.supportServicesLineItemDisplayName,
                    payrollExpenseAccountsData: JSON.stringify(state.expenseAccountsData),
                    payrollExpenseAccountsLineTitle: state.expenseAccountsLineTitle,
                    payrollExpenseAccountsInvoiceGroup: state.expenseAccountsInvoiceGroup,
                    additionalPayrollAmount: state.additionalPayrollAmount || 0,
                    payrollTaxesEscalatorEnable: isPTEBEEscalatorEnabled,
                    payrollTaxesEscalatorMonth: state.payrollTaxesEscalatorMonth,
                   // payrollTaxesEscalatorvalue: ptebEscalatorPercentage || 0,
                    payrollTaxesEscalatorType: isPTEBEEscalatorType,
                     payrollTaxesEscalatorvalue: state.payrollTaxesEscalatorvalue,
        
                },
            ],
        };
        if (JSON.stringify(updatedTerms) !== JSON.stringify(billableAccounts)) {
            onUpdateBillableAccounts(updatedTerms);
        }
    };

    useEffect(() => {
        notifyParent();
    }, [
        state.payrollData,
        state.isPayrollEnabled,
        isPTEBEnabled,
        ptebBillingType,
        ptebPercentage,
        ptebLineItemDisplayName,
        isPTEBEEscalatorEnabled,
        isPTEBEEscalatorType,
       // ptebEscalatorPercentage,
        state.payrollLineItemDisplayName,
        state.invoiceGroup,
        state.isSupportServicesEnabled,
        state.supportServicesBillingType,
        state.supportServicesPayrollType,
        state.supportServicesFixedAmount,
        state.supportServicesLineItemDisplayName,
        state.expenseAccountsData,
        state.expenseAccountsLineTitle,
        state.expenseAccountsInvoiceGroup,
        state.additionalPayrollAmount,
        state.payrollTaxesEscalatorMonth,
        state.payrollTaxesEscalatorvalue
      
    ]);

    const togglePayrollEnabled = (enabled: boolean) => {
        setIsDirty(true);
        setState(prevState => ({ ...prevState, isPayrollEnabled: enabled }));
        const updatedTerms: BillablePayrollTerms = {
            ...billableAccounts,
            enabled: enabled,
            billableAccountsData: billableAccounts.billableAccountsData.map(account => ({
                ...account,
                payrollAccountsData: JSON.stringify(state.payrollData),
                payrollAccountsLineTitle: state.payrollLineItemDisplayName,
                payrollAccountsInvoiceGroup: state.invoiceGroup,
                payrollTaxesEnabled: isPTEBEnabled,
                payrollTaxesBillingType: ptebBillingType,
                payrollTaxesPercentage: ptebPercentage || null,
                payrollTaxesLineTitle: ptebLineItemDisplayName,
                payrollSupportEnabled: state.isSupportServicesEnabled,
                payrollSupportBillingType: state.supportServicesBillingType,
                payrollSupportPayrollType: state.supportServicesPayrollType,
                payrollSupportAmount: state.supportServicesFixedAmount || null,
                payrollSupportLineTitle: state.supportServicesLineItemDisplayName,
                payrollExpenseAccountsData: JSON.stringify(state.expenseAccountsData),
                payrollExpenseAccountsLineTitle: state.expenseAccountsLineTitle,
                payrollExpenseAccountsInvoiceGroup: state.expenseAccountsInvoiceGroup,
                additionalPayrollAmount: state.additionalPayrollAmount || 0,
                payrollTaxesEscalatorEnable: isPTEBEEscalatorEnabled,
                payrollTaxesEscalatorMonth: state.payrollTaxesEscalatorMonth,
                //payrollTaxesEscalatorvalue: ptebEscalatorPercentage,
                payrollTaxesEscalatorType: isPTEBEEscalatorType,
                payrollTaxesEscalatorvalue: state.payrollTaxesEscalatorvalue,
            })),
        };

        onUpdateBillableAccounts(updatedTerms);
    };

    const updatePTEBEnabled = (enabled: boolean) => {
        setIsDirty(true);
        setIsPTEBEnabled(enabled);
    };


    const handlePTEBEscalatorMonthChange = (month: Month) => {
        setIsDirty(true);
        setState(prevState => ({
            ...prevState,
            payrollTaxesEscalatorMonth: month,
        }));
    };

    const updatePTEBEscalatorEnabled = (enabled: boolean) => {
        setIsDirty(true);
        setisPTEBEEscalatorEnabled(enabled);
    };
    // const handlePTEBEscalatorValueChange = (value: number) => {
    //     setIsDirty(true);
    //    setptebEscalatorPercentage(value)
    // };

       const handlePTEBEscalatorValueChange = (value: number) => {
        setIsDirty(true);
        setState(prevState => ({
            ...prevState,
            payrollTaxesEscalatorvalue: value, 
        }));
    };

    const updatePTEBBillingType = (billingType: PTEBBillingType) => {
        setIsDirty(true);
        setPTEBBillingType(billingType);
    };

    const UpdatePTEBEscalatorTypeC = (type: EscalatorFormatType) => {
        setIsDirty(true);
        setIsPTEBEEscalatorType(type)
    };
    const updatePTEBPercentage = (percentage: number) => {
        setIsDirty(true);
        setPTEBPercentage(percentage);
    };

    const updatePTEBLineItemDisplayName = (lineItemDisplayName: string) => {
        setIsDirty(true);
        setPTEBLineItemDisplayName(lineItemDisplayName);
    };

    const handlePayrollDataChange = (account: PayrollAccount, shouldExclude: boolean) => {
        setIsDirty(true);
        setState((prevState) => ({
            ...prevState,
            payrollData: prevState.payrollData.map((data) =>
                data.code === account.id ? { ...data, isEnabled: !shouldExclude } : data
            ),
        }));
    };

    const handleExpenseDataChange = (account: PayrollAccount, shouldExclude: boolean) => {
        setIsDirty(true);
        setState((prevState) => ({
            ...prevState,
            expenseAccountsData: prevState.expenseAccountsData.map((data) =>
                data.code === account.id ? { ...data, isEnabled: !shouldExclude } : data
            ),
        }));
    };

    const handleLineTitleChange = (title: string) => {
        setIsDirty(true);
        setState((prevState) => ({
            ...prevState,
            payrollLineItemDisplayName: title,
        }));
    };

    const handleExpenseLineTitleChange = (title: string) => {
        setIsDirty(true);
        setState((prevState) => ({
            ...prevState,
            expenseAccountsLineTitle: title || "Total Other Expenses",
        }));
        console.log("Expense Line Title: ", title);
    };

    const handleInvoiceGroupChange = (groupNumber: number) => {
        setIsDirty(true);
        setState((prevState) => ({
            ...prevState,
            invoiceGroup: groupNumber,
        }));
    };

    const toggleSupportServicesEnabled = (enabled: boolean) => {
        setIsDirty(true);
        setState(prevState => ({ ...prevState, isSupportServicesEnabled: enabled }));
    };

    const handleSupportBillingTypeChange = (billingType: SupportServicesType) => {
        setIsDirty(true);
        setState(prevState => ({ ...prevState, supportServicesBillingType: billingType }));
    };

    const handleSupportPayrollTypeChange = (payrollType: SupportPayrollType) => {
        setIsDirty(true);
        setState(prevState => ({ ...prevState, supportServicesPayrollType: payrollType }));
    };

    const handleSupportFixedAmountChange = (amount: number) => {
        setIsDirty(true);
        setState(prevState => ({ ...prevState, supportServicesFixedAmount: amount }));
    };

    const handleSupportLineItemTitleChange = (title: string) => {
        setIsDirty(true);
        setState(prevState => ({ ...prevState, supportServicesLineItemDisplayName: title }));
    };
    const getSelectAllHandler = (
        accountData: Record<string, string>,
        stateData: PayrollDataItem[],
        handleDataChange: (data: { id: string; name: string }, isEnabled: boolean) => void
    ) => () => {
        const allSelected = Object.keys(accountData).every(id =>
            !stateData.find(data => data.code === id)?.isEnabled
        );

        Object.entries(accountData).forEach(([id, name]) => {
            handleDataChange({ id, name }, !allSelected);
        });
    };
    const handleAdditionalPayrollAmountChange = (amount: number) => {
        setIsDirty(true);
        setState(prevState => ({ ...prevState, additionalPayrollAmount: amount }));
    };


    return (
        <TooltipProvider>
            <Card data-testid="billable-payroll-card">
                <CardHeader>
                    <div className="flex items-center justify-between">
                        <Label>Enable Billable Accounts</Label>
                        <Switch
                            data-qa-id="switch-enableBillableAccounts-payroll"
                            data-testid="payroll-switch"
                            checked={state.isPayrollEnabled}
                            onCheckedChange={togglePayrollEnabled}
                            disabled={!isEditable}
                        />
                    </div>
                </CardHeader>
                <CardContent className="space-y-6 p-4">
                    {state.isPayrollEnabled && (
                        <>
                            <div className="space-y-4">
                                <div className="flex items-center justify-between">
                                    <h3 className="text-lg font-semibold">Payroll Accounts</h3>
                                    <Dialog open={isPayrollDialogOpen} onOpenChange={setIsPayrollDialogOpen}>
                                        <DialogTrigger asChild>
                                            <Button
                                                data-qa-id="button-addPayrollAccounts-payroll"
                                                variant="outline"
                                                size="icon"
                                                data-testid="plus-button"
                                                disabled={!isEditable}
                                            >
                                                <Plus className="h-4 w-4" />
                                            </Button>
                                        </DialogTrigger>
                                        <DialogContent className="sm:max-w-[40vw] h-[90vh]">
                                            <DialogHeader>
                                                <DialogTitle data-testid="dialog-title">Add Excluded Payroll Accounts</DialogTitle>
                                                <DialogDescription>Please select the accounts to exclude</DialogDescription>
                                            </DialogHeader>
                                            <div className="flex items-center gap-4 mb-4">
                                                <button
                                                    className="text-blue-500 no-underline hover:underline focus-visible:underline"
                                                    onClick={getSelectAllHandler(
                                                        payrrolAccount ?? {},
                                                        state?.payrollData ?? [],
                                                        handlePayrollDataChange
                                                    )}
                                                >
                                                    {Object.keys(payrrolAccount).every(id =>
                                                        !state.payrollData.find(data => data.code === id)?.isEnabled
                                                    )
                                                        ? "Deselect All"
                                                        : "Select All"}
                                                </button>
                                            </div>
                                            <div className="overflow-y-auto max-h-[65vh] grid gap-4">
                                                {Object.entries(payrrolAccount).map(([id, name]) => (
                                                    <div key={id} className="flex items-center gap-4">
                                                        <input
                                                            data-qa-id="checkbox-excludePayrollAccount-payroll"
                                                            type="checkbox"
                                                            checked={!state.payrollData.find(data => data.code === id)?.isEnabled}
                                                            onChange={(e) => handlePayrollDataChange({ id, name }, e.target.checked)}
                                                            title={name}
                                                        />
                                                        <span>{name} ({id})</span>
                                                    </div>
                                                ))}
                                            </div>
                                            <div className="flex justify-end mt-4">
                                                <Button
                                                    data-qa-id="button-closePayrollDialog-payroll"
                                                    variant="outline"
                                                    onClick={() => setIsPayrollDialogOpen(false)}
                                                    data-testid="close-dialog-button"
                                                    disabled={!isEditable}
                                                >
                                                    Close
                                                </Button>
                                            </div>
                                        </DialogContent>
                                    </Dialog>


                                </div>
                                <p className="text-sm text-muted-foreground">
                                    Select payroll accounts to exclude from billing. Excluded accounts will not be charged to the customer.
                                    Click the 'X' icon to remove an account from the exclusion list and make it billable again.
                                </p>
                                <div className="space-y-2">
                                    <Label>Excluded Payroll Accounts</Label>
                                    <div className="space-y-2">
                                        {state.payrollData.filter(data => !data.isEnabled).map((data) => (
                                            <div
                                                key={data.code}
                                                className="flex items-center justify-between bg-muted p-2 rounded-md"
                                            >
                                                <div>
                                                    <span>{data.title}</span>
                                                    <span className="block text-sm text-muted-foreground">
                                                        {data.code}
                                                    </span>
                                                </div>
                                                <Button
                                                    data-qa-id="button-removePayrollAccount-payroll"
                                                    variant="ghost"
                                                    size="icon"
                                                    onClick={() => handlePayrollDataChange({ id: data.code, name: data.title }, false)}
                                                    disabled={!isEditable}
                                                >
                                                    <X className="h-4 w-4" />
                                                    <span className="sr-only">Remove</span>
                                                </Button>
                                            </div>
                                        ))}
                                    </div>
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="payroll-line-item-display-name">
                                        Line-item Title
                                    </Label>
                                    <Input
                                        data-qa-id="input-payrollLineItemTitle-payroll"
                                        required
                                        id="payroll-line-item-display-name"
                                        value={state.payrollLineItemDisplayName}
                                        onChange={(e) => handleLineTitleChange(e.target.value)}
                                        placeholder="Enter display name for payroll total"
                                        disabled={!isEditable}
                                    />
                                </div>
                                <div>
                                    <Label>Additional Payroll Amount</Label>
                                    <Tooltip>
                                        <TooltipTrigger asChild>
                                            <Button data-qa-id="button-additionalPayroll-amount" variant="ghost" size="icon" onClick={(e) => { e.preventDefault(); }}>
                                                <Info className="h-4 w-4" />
                                            </Button>
                                        </TooltipTrigger>
                                        <TooltipContent>
                                            <p>Optional Fixed Amount that can be added to the Site Salaries & Wages line item total. This amount will also be included when calculating Total Payroll and/or Billable Payroll where these concepts are used in other calculations (PTEB, Support Services, etc).</p>
                                        </TooltipContent>
                                    </Tooltip>
                                    <NumericFormat
                                        data-qa-id="input-supportServicesAdditionalPayrollAmount-payroll"
                                        required={true}
                                        id={"additionalPayroll-amount"}
                                        data-testid="support-services-additional-payroll-amount-input"
                                        displayType="input"
                                        decimalScale={2}
                                        fixedDecimalScale={true}
                                        prefix="$"
                                        value={state.additionalPayrollAmount}
                                        onValueChange={(values) => handleAdditionalPayrollAmountChange(parseFloat(values.floatValue?.toString() || "0"))}
                                        customInput={Input}
                                        placeholder="0.00"
                                        disabled={!isEditable}
                                        allowNegative={false}
                                    />
                                </div>
                            </div>

                            {watchInvoiceGroupingEnabled && (
                                <div className="space-y-2">
                                    <div className="flex items-center space-x-2">
                                        <Label htmlFor="invoice-group">Invoice</Label>
                                        <Tooltip>
                                            <TooltipTrigger asChild>
                                                <Info className="h-4 w-4 text-muted-foreground" />
                                            </TooltipTrigger>
                                            <TooltipContent>
                                                <p>The Billable Payroll line-item, PTEB, and/or Support Services line-items will all be grouped together on the same invoice.</p>
                                            </TooltipContent>
                                        </Tooltip>
                                    </div>
                                    <Select
                                        data-qa-id="select-invoiceGroup-payroll"
                                        value={state.invoiceGroup.toString()}
                                        onValueChange={(value) =>
                                            handleInvoiceGroupChange(parseInt(value, 10))
                                        }
                                        disabled={!isEditable}
                                    >
                                        <SelectTrigger id="invoice-group">
                                            <SelectValue placeholder="Select invoice" />
                                        </SelectTrigger>
                                        <SelectContent>
                                            {invoiceGroups.map((group) => (
                                                <SelectItem
                                                    key={group.id}
                                                    value={group.groupNumber.toString()}
                                                >
                                                    {group.groupNumber}
                                                </SelectItem>
                                            ))}
                                        </SelectContent>
                                    </Select>
                                </div>
                            )}
                            <div className="space-y-4 pl-6 gap-4 p-4 border rounded-lg">
                                <Label className="text-xl font-semibold">Payroll Taxes Employee Benefits (PTEB)</Label>
                                <p className="text-sm text-muted-foreground">
                                    PTEB is the sum of three general ledger accounts: 6200 - Payroll Taxes, 6399 - Health Insurance Allocation, and 6500 - Insurance - Worker's Comp.
                                </p>
                                <div className="space-y-2">
                                    <div className="flex items-center space-x-2">
                                        <Checkbox
                                            data-qa-id="checkbox-enablePTEB-payroll"
                                            id="pteb-enabled"
                                            checked={isPTEBEnabled}
                                            onCheckedChange={(checked: boolean) => {
                                                if (isEditingPTEB) {
                                                    updatePTEBEnabled(checked);
                                                    setIsEditingPTEB(checked)
                                                }
                                            }}
                                            onFocus={() => setIsEditingPTEB(true)}
                                            onBlur={() => setIsEditingPTEB(false)}
                                            disabled={!isEditable}
                                        />
                                        <Label htmlFor="pteb-enabled">Create PTEB Line-item</Label>
                                        <Tooltip>
                                            <TooltipTrigger asChild>
                                                <Button data-qa-id="button-ptebInfo-payroll" variant="ghost" size="icon" onClick={(e) => { e.preventDefault(); }}>
                                                    <Info className="h-4 w-4" />
                                                </Button>
                                            </TooltipTrigger>
                                            <TooltipContent>
                                                <p>PTEB can be charged based on actual amounts from the general ledger or as a percentage of billable payroll accounts.</p>
                                            </TooltipContent>
                                        </Tooltip>
                                    </div>
                                </div>
                                {isPTEBEnabled && (
                                    <>
                                        <RadioGroup
                                            value={ptebBillingType}
                                            onValueChange={updatePTEBBillingType}
                                        >
                                            <div className="flex items-center space-x-2">
                                                <RadioGroupItem
                                                    data-qa-id="radio-ptebActual-payroll"
                                                    value={PTEBBillingType.ACTUAL}
                                                    id="pteb-actual"
                                                    data-testid="pteb-actual-radio"
                                                    disabled={!isEditable}
                                                />
                                                <Label htmlFor="pteb-actual">Charge Actual PTEB</Label>
                                                <Tooltip>
                                                    <TooltipTrigger asChild>
                                                        <Button data-qa-id="button-ptebActualInfo-payroll" variant="ghost" size="icon" onClick={(e) => { e.preventDefault(); }}>
                                                            <Info className="h-4 w-4" />
                                                        </Button>
                                                    </TooltipTrigger>
                                                    <TooltipContent>
                                                        <p>This setting will charge the customer the sum of PTEB accounts for this site.</p>
                                                    </TooltipContent>
                                                </Tooltip>
                                            </div>
                                            <div className="flex items-center space-x-2">
                                                <RadioGroupItem
                                                    data-qa-id="radio-ptebPercentage-payroll"
                                                    value={PTEBBillingType.PERCENTAGE}
                                                    id="pteb-percentage"
                                                    data-testid="pteb-percentage-radio"
                                                    disabled={!isEditable}
                                                />
                                                <Label htmlFor="pteb-percentage">Charge as Percentage of Billable Payroll</Label>
                                                <Tooltip>
                                                    <TooltipTrigger asChild>
                                                        <Button data-qa-id="button-ptebPercentageInfo-payroll" variant="ghost" size="icon" onClick={(e) => { e.preventDefault(); }}>
                                                            <Info className="h-4 w-4" />
                                                        </Button>
                                                    </TooltipTrigger>
                                                    <TooltipContent>
                                                        <p>Billable Payroll is defined as general ledger accounts in the range of 6000-6199 which are also not found in the Excluded Payroll Accounts list configured in the Payroll Accounts section above.</p>
                                                    </TooltipContent>
                                                </Tooltip>
                                            </div>
                                        </RadioGroup>
                                        {ptebBillingType === PTEBBillingType.PERCENTAGE && (
                                            <div className="space-y-2">
                                               
                                                <div className="flex items-center space-x-4 mb-4">
                                                    <Checkbox
                                                        data-qa-id="checkbox-enablePtebEscalator"
                                                        id="pteb-escalator-enabled"
                                                        checked={isPTEBEEscalatorEnabled}
                                                        onCheckedChange={(checked: boolean) => {
                                                            if (isEditingPtebEscalator) {
                                                                updatePTEBEscalatorEnabled(checked);
                                                            }
                                                        setIsEditingPtebEscalator(checked)
                                                        }}
                                                        onFocus={() => setIsEditingPtebEscalator(true)}
                                                        onBlur={() => setIsEditingPtebEscalator(false)}
                                                        disabled={!isEditable}
                                                    />
                                                    <Label htmlFor="pteb-escalator-enabled">Enable Escalator</Label>
                                                    <Tooltip>
                                                        <TooltipTrigger asChild>
                                                            <Button data-qa-id="button-pteb-escalator-enabled" variant="ghost" size="icon" onClick={e => e.preventDefault()}>
                                                                <Info className="h-4 w-4" />
                                                            </Button>
                                                        </TooltipTrigger>
                                                        <TooltipContent className="max-w-xs">
                                                            <p> The Escalator feature allows you to automatically increase the PTEB percentage on
                                                                a yearly basis. For Arrears Billing Type, the escalator will increase the PTEB
                                                                percentage by the specified amount or percentage on the last Friday of the month
                                                                preceding the Escalation Month. This ensures the correct billing cycles are
                                                                affected. The increase will continue to apply every anniversary of that month.
                                                            </p>
                                                        </TooltipContent>
                                                    </Tooltip>
                                                </div>
                                                {isPTEBEEscalatorEnabled && (
                                                    <>
                                                        <div className="space-y-2 ml-6">
                                                            <div className="flex items-center space-x-2">
                                                                <Label htmlFor="profit-share-escalation-month">Escalation Month</Label>
                                                                <Tooltip>
                                                                    <TooltipTrigger asChild>
                                                                        <Button data-qa-id="button-profit-share-escalation-month" variant="ghost" size="icon" onClick={e => e.preventDefault()}>
                                                                            <Info className="h-4 w-4" />
                                                                        </Button>
                                                                    </TooltipTrigger>
                                                                    <TooltipContent className="max-w-xs">
                                                                        <p>The month in which the escalation will take effect. For Arrears Billing Type, the
                                                                            escalation will be applied on the last Friday of the month preceding this selected month.
                                                                        </p>
                                                                    </TooltipContent>
                                                                </Tooltip>
                                                            </div>
                                                            <Select
                                                                data-qa-id="select-pteb-escalation-Month"

                                                                value={state.payrollTaxesEscalatorMonth}
                                                                onValueChange={handlePTEBEscalatorMonthChange}

                                                                disabled={!isEditable}
                                                                required

                                                            >
                                                                <SelectTrigger>
                                                                    <SelectValue placeholder="Select escalation Month" />
                                                                </SelectTrigger>
                                                                <SelectContent>
                                                                    {Object.values(Month).map((month) => (
                                                                        <SelectItem key={month} value={month}>
                                                                            {month}
                                                                        </SelectItem>
                                                                    ))}
                                                                </SelectContent>
                                                            </Select>
                                                        </div>
                                                        <div className="space-y-2 ml-6">
                                                            <div className="flex items-center space-x-2">
                                                                <Label htmlFor="pteb-escalator-format">Escalator Format</Label>
                                                                <Tooltip>
                                                                    <TooltipTrigger asChild>
                                                                        <Button data-qa-id="button-pteb-escalator-format" variant="ghost" size="icon" onClick={e => e.preventDefault()}>
                                                                            <Info className="h-4 w-4" />
                                                                        </Button>
                                                                    </TooltipTrigger>
                                                                    <TooltipContent className="max-w-xs">
                                                                        <p>Choose whether the escalation should be a percentage increase (e.g., 3% more each year) or
                                                                            a fixed amount increase (e.g., 2 percentage points more each year).
                                                                        </p>
                                                                    </TooltipContent>
                                                                </Tooltip>
                                                            </div>
                                                            <RadioGroup
                                                                value={isPTEBEEscalatorType}
                                                                onValueChange={UpdatePTEBEscalatorTypeC}
                                                                data-qa-id="radio-profitShareEscalatorFormat"
                                                            >
                                                                <div className="flex items-center space-x-2">
                                                                    <RadioGroupItem data-qa-id="radio-ptebEscalatorPercentage" value={EscalatorFormatType.PERCENTAGE} id="pteb-escalator-percentage" data-testid="radio-profitShareEscalatorPercentage" disabled={!isEditable} />
                                                                    <Label htmlFor="profit-share-escalator-percentage">Percentage</Label>
                                                                </div>
                                                                <div className="flex items-center space-x-2 mb-4">
                                                                    <RadioGroupItem data-qa-id="radio-profitShareEscalatorFixedAmount" value={EscalatorFormatType.FIXEDAMOUNT} id="profit-share-escalator-fixed-amount" data-testid="radio-profitShareEscalatorFixedAmount" disabled={!isEditable} />
                                                                    <Label htmlFor="profit-share-escalator-fixed-amount">Fixed Amount</Label>
                                                                </div>
                                                            </RadioGroup>


                                                            <Label htmlFor="pteb-escalator-value">
                                                                {isPTEBEEscalatorType === EscalatorFormatType.PERCENTAGE
                                                                    ? "Escalator Percentage"
                                                                    : "Escalator Amount"}
                                                            </Label>
                                                            <Tooltip>
                                                                <TooltipTrigger asChild>
                                                                    <Button data-qa-id="button-overtime-rate-escalator" variant="ghost" size="icon" onClick={e => e.preventDefault()}>
                                                                        <Info className="h-4 w-4" />
                                                                    </Button>
                                                                </TooltipTrigger>
                                                                <TooltipContent className="max-w-xs">
                                                                    <p>{isPTEBEEscalatorType === EscalatorFormatType.PERCENTAGE ? `The percentage by which the PTEB percentage will increase${state.payrollTaxesEscalatorvalue}% each year on the anniversary of the - ${state.payrollTaxesEscalatorMonth}.`
                                                                        : `The fixed amount ${state.payrollTaxesEscalatorvalue} by which the PTEB percentage will increase each year on the anniversary of the - ${state.payrollTaxesEscalatorMonth}.`}
                                                                    </p>
                                                                </TooltipContent>
                                                            </Tooltip>
                                                            <NumericFormat
                                                                data-qa-id="input-ptebEscalatorValue-payroll"
                                                                required={isPTEBEEscalatorEnabled}
                                                                id="pteb-escalator-value"
                                                                data-testid="pteb-escalator-value"
                                                              
                                                                decimalScale={2}
                                                                fixedDecimalScale={true}
                                                                suffix={isPTEBEEscalatorType === EscalatorFormatType.PERCENTAGE ? "%" : ""}
                                                                prefix={isPTEBEEscalatorType === EscalatorFormatType.FIXEDAMOUNT ? "$" : ""}
                                                                inputMode="decimal"
                                                                allowNegative={false}
                                                                placeholder={
                                                                    isPTEBEEscalatorType === EscalatorFormatType.PERCENTAGE
                                                                        ? "Enter percentage"
                                                                        : "Enter amount"
                                                                }
                                                                value={state.payrollTaxesEscalatorvalue}
                                                                onValueChange={(values) => handlePTEBEscalatorValueChange(parseFloat(values.floatValue?.toString() || "0"))}
                                                                customInput={Input}
                                                                     min={0}

                                                                    max={100}
                                                                disabled={!isEditable}
                                                            />
                                                        </div>

                                    
                                                    </>
                                                )}
                                                   <div className="space-y-2">
                                                            <Label htmlFor="pteb-percentage">Percentage of Billable Payroll</Label>
                                                            <NumericFormat
                                                                data-qa-id="input-ptebPercentage-payroll"
                                                                id="pteb-percentage"
                                                                data-testid="pteb-percentage-input"
                                                                required={ptebBillingType === PTEBBillingType.PERCENTAGE}
                                                                displayType="input"
                                                                decimalScale={2}
                                                                fixedDecimalScale={true}
                                                                suffix="%"
                                                                inputMode="decimal"
                                                                allowNegative={false}
                                                                placeholder="Enter percentage"
                                                                value={ptebPercentage ?? "0"}
                                                                onValueChange={(values) => updatePTEBPercentage(parseFloat(values.floatValue?.toString() || "0"))}
                                                                customInput={Input}
                                                                disabled={!isEditable}
                                                            />
                                                            {errors?.billableAccounts?.billableAccounts?.[0]?.payrollTaxesPercentage && (
                                                                <p className="text-red-500">
                                                                    {errors.billableAccounts.billableAccounts[0].payrollTaxesPercentage.message}
                                                                </p>
                                                            )}
                                                        </div>
                                            </div>
                                        )}

                                        <div className="space-y-2">
                                            <Label htmlFor="pteb-line-item-display-name">Line-item Title</Label>
                                            <Input
                                                data-qa-id="input-ptebLineItemTitle-payroll"
                                                id="pteb-line-item-display-name"
                                                data-testid="pteb-line-item-title-input"
                                                value={ptebLineItemDisplayName}
                                                onChange={(e) => updatePTEBLineItemDisplayName(e.target.value)}
                                                placeholder="Enter display name for PTEB"
                                                disabled={!isEditable}
                                            />
                                        </div>
                                    </>
                                )}
                            </div>

                            <div className="space-y-4 pl-6 gap-4 p-4 border rounded-lg">
                                <Label className="text-xl font-semibold">Support Services</Label>
                                <p className="text-sm text-muted-foreground">
                                    Support Services can be billed as a fixed amount every cycle or as a percentage of payroll.
                                </p>
                                <div className="flex items-center space-x-2">
                                    <Checkbox
                                        data-qa-id="checkbox-enableSupportServices-payroll"
                                        id="support-services-enabled"
                                        data-testid="support-services-enabled-checkbox"
                                        checked={state.isSupportServicesEnabled}
                                        onCheckedChange={(checked: boolean) => {
                                            if (isEditingSupportServices) {
                                                toggleSupportServicesEnabled(checked);
                                            }
                                            setIsEditingSupportServices(checked);

                                        }}
                                        onFocus={() => setIsEditingSupportServices(true)}
                                        onBlur={() => setIsEditingSupportServices(false)}
                                        disabled={!isEditable}
                                    />
                                    <Label>Create Support Services Line-item</Label>
                                </div>
                                {state.isSupportServicesEnabled && (
                                    <>
                                        <div className="space-y-2">
                                            <RadioGroup
                                                className="space-y-2"
                                                value={state.supportServicesBillingType}
                                                onValueChange={handleSupportBillingTypeChange}
                                            >
                                                <div className="flex items-center space-x-2">
                                                    <RadioGroupItem data-qa-id="radio-supportServicesFixed-payroll" value={SupportServicesType.FIXED} id="Fixed Fee" data-testid="support-services-fixed-radio" disabled={!isEditable} />
                                                    <Label htmlFor="Fixed Fee">Fixed Fee</Label>
                                                </div>
                                                <div className="flex items-center space-x-2">
                                                    <RadioGroupItem data-qa-id="radio-supportServicesPercentage-payroll" value={SupportServicesType.PERCENTAGE} id="Percentage of Payroll" data-testid="support-services-percentage-radio" disabled={!isEditable} />
                                                    <Label htmlFor="Percentage of Payroll">Percentage of Payroll</Label>
                                                    <Tooltip>
                                                        <TooltipTrigger asChild>
                                                            <Button data-qa-id="button-supportServicesInfo-payroll" variant="ghost" size="icon" onClick={(e) => { e.preventDefault(); }}>
                                                                <Info className="h-4 w-4" />
                                                            </Button>
                                                        </TooltipTrigger>
                                                        <TooltipContent>
                                                            <p>Support Services can be charged as a fixed amount or as a percentage of either Billable Payroll or Total Payroll.</p>
                                                        </TooltipContent>
                                                    </Tooltip>
                                                </div>
                                            </RadioGroup>
                                        </div>
                                        {state.supportServicesBillingType === SupportServicesType.FIXED && (
                                            <div className="space-y-2">
                                                <Label>Fixed Fee Amount</Label>
                                                <NumericFormat
                                                    data-qa-id="input-supportServicesFixedAmount-payroll"
                                                    required={true}
                                                    id={"supportServices-amount"}
                                                    data-testid="support-services-fixed-amount-input"
                                                    displayType="input"
                                                    decimalScale={2}
                                                    fixedDecimalScale={true}
                                                    prefix="$"
                                                    value={state.supportServicesFixedAmount}
                                                    onValueChange={(values) => handleSupportFixedAmountChange(parseFloat(values.floatValue?.toString() || "0"))}
                                                    customInput={Input}
                                                    placeholder="Enter fixed amount"
                                                    disabled={!isEditable}
                                                />
                                            </div>
                                        )}
                                        {state.supportServicesBillingType === SupportServicesType.PERCENTAGE && (
                                            <div className="space-y-2">
                                                <Label>Payroll Type</Label>
                                                <RadioGroup
                                                    className="space-y-2 pb-4"
                                                    value={state.supportServicesPayrollType}
                                                    onValueChange={handleSupportPayrollTypeChange}
                                                >
                                                    <div className="flex items-center space-x-2">
                                                        <RadioGroupItem data-qa-id="radio-supportServicesBillablePayroll-payroll" value={SupportPayrollType.BILLABLE} id="Billable Payroll" data-testid="support-services-topline-radio" disabled={!isEditable} />
                                                        <Label htmlFor="Billable Payroll">Billable Payroll</Label>
                                                        <Tooltip>
                                                            <TooltipTrigger asChild>
                                                                <Button data-qa-id="button-billablePayrollInfo-payroll" variant="ghost" size="icon" onClick={(e) => { e.preventDefault(); }}>
                                                                    <Info className="h-4 w-4" />
                                                                </Button>
                                                            </TooltipTrigger>
                                                            <TooltipContent>
                                                                <p>Billable Payroll is defined as general ledger accounts in the range of 6000-6199 which are also not found in the Excluded Payroll Accounts list configured in the Payroll Accounts section above.</p>
                                                            </TooltipContent>
                                                        </Tooltip>
                                                    </div>
                                                    <div className="flex items-center space-x-2">
                                                        <RadioGroupItem data-qa-id="radio-supportServicesTotalPayroll-payroll" value={SupportPayrollType.TOTAL} id="Total Payroll" disabled={!isEditable} />
                                                        <Label htmlFor="Total Payroll">Total Payroll</Label>
                                                        <Tooltip>
                                                            <TooltipTrigger asChild>
                                                                <Button data-qa-id="button-totalPayrollInfo-payroll" variant="ghost" size="icon" onClick={(e) => { e.preventDefault(); }}>
                                                                    <Info className="h-4 w-4" />
                                                                </Button>
                                                            </TooltipTrigger>
                                                            <TooltipContent>
                                                                <p>Total Payroll is defined as the sum of Billable Payroll (from Billable Accounts section) plus the amount used for Payroll Taxes Employee Benefits (from PTEB section above).</p>
                                                            </TooltipContent>
                                                        </Tooltip>
                                                    </div>
                                                </RadioGroup>
                                                <Label>Percentage of Payroll</Label>
                                                <NumericFormat
                                                    data-qa-id="input-supportServicesPercentage-payroll"
                                                    required={true}
                                                    id={"supportServices-percentage"}
                                                    data-testid="support-services-percentage-input"
                                                    displayType="input"
                                                    decimalScale={2}
                                                    fixedDecimalScale={true}
                                                    suffix="%"
                                                    value={state.supportServicesFixedAmount}
                                                    onValueChange={(values) => handleSupportFixedAmountChange(parseFloat(values.floatValue?.toString() || "0"))}
                                                    customInput={Input}
                                                    placeholder="Enter percentage"
                                                    disabled={!isEditable}
                                                />
                                            </div>
                                        )}
                                        <div className="space-y-2">
                                            <Label>Line-item Title</Label>
                                            <Input
                                                data-qa-id="input-supportServicesLineItemTitle-payroll"
                                                required
                                                data-testid="support-services-line-item-title-input"
                                                value={state.supportServicesLineItemDisplayName}
                                                onChange={(e) => handleSupportLineItemTitleChange(e.target.value)}
                                                placeholder="Enter display name for Support Services"
                                                disabled={!isEditable}
                                            />
                                        </div>
                                    </>
                                )}
                            </div>

                            <div className="space-y-4">
                                <div className="flex items-center justify-between">
                                    <h3 className="text-lg font-semibold">Expense Accounts</h3>
                                    <Dialog open={isExpenseAccountsDialogOpen} onOpenChange={setIsExpenseAccountsDialogOpen}>
                                        <DialogTrigger asChild>
                                            <Button
                                                data-qa-id="button-addExpenseAccounts-payroll"
                                                variant="outline"
                                                size="icon"
                                                data-testid="expense-accounts-dialog-button"
                                                disabled={!isEditable}
                                            >
                                                <Plus className="h-4 w-4" />
                                            </Button>
                                        </DialogTrigger>
                                        <DialogContent className="sm:max-w-[40vw] h-[90vh]">
                                            <DialogHeader>
                                                <DialogTitle data-testid="expense-dialog-title">Add Excluded Expense Accounts</DialogTitle>
                                                <DialogDescription id="expense-dialog-description">
                                                    Please select the accounts to exclude from billing.
                                                </DialogDescription>
                                            </DialogHeader>
                                            <div className="flex items-center gap-4 mb-4">
                                                <button
                                                    className="text-blue-500 no-underline hover:underline focus-visible:underline"

                                                    onClick={getSelectAllHandler(
                                                        expenseAccount ?? {},
                                                        state?.expenseAccountsData ?? [],
                                                        handleExpenseDataChange
                                                    )}
                                                >
                                                    {Object.keys(expenseAccount).every(id =>
                                                        !state.expenseAccountsData.find(data => data.code === id)?.isEnabled
                                                    )
                                                        ? "Deselect All"
                                                        : "Select All"}
                                                </button>
                                            </div>
                                            <div className="overflow-y-auto max-h-[65vh] grid gap-4">
                                                {Object.entries(expenseAccount).map(([id, name]) => (
                                                    <div key={id} className="flex items-center gap-4">
                                                        <input
                                                            data-qa-id="checkbox-excludeExpenseAccount-payroll"
                                                            type="checkbox"
                                                            checked={!state.expenseAccountsData.find(data => data.code === id)?.isEnabled}
                                                            onChange={(e) => handleExpenseDataChange({ id, name }, e.target.checked)}
                                                            title={name}
                                                        />
                                                        <span>{name} ({id})</span>
                                                    </div>
                                                ))}
                                            </div>
                                            <div className="flex justify-end mt-4">
                                                <Button
                                                    data-qa-id="button-closeExpenseDialog-payroll"
                                                    variant="outline"
                                                    onClick={() => setIsExpenseAccountsDialogOpen(false)}
                                                    data-testid="expense-accounts-dialog-close"
                                                    disabled={!isEditable}
                                                >
                                                    Close
                                                </Button>
                                            </div>
                                        </DialogContent>
                                    </Dialog>

                                </div>
                                <p className="text-sm text-muted-foreground">
                                    Select expense accounts to exclude from billing. Excluded accounts will not be charged to the customer.
                                    Click the 'X' icon to remove an account from the exclusion list and make it billable again.
                                </p>
                                <div className="space-y-2">
                                    <Label>Excluded Expense Accounts</Label>
                                    <div className="space-y-2">
                                        {state.expenseAccountsData.filter(data => !data.isEnabled).map((data) => (
                                            <div
                                                key={data.code}
                                                className="flex items-center justify-between bg-muted p-2 rounded-md"
                                            >
                                                <div>
                                                    <span>{data.title}</span>
                                                    <span className="block text-sm text-muted-foreground">
                                                        {data.code}
                                                    </span>
                                                </div>
                                                <Button
                                                    data-qa-id="button-removeExpenseAccount-payroll"
                                                    variant="ghost"
                                                    size="icon"
                                                    onClick={() => handleExpenseDataChange({ id: data.code, name: data.title }, false)}
                                                    disabled={!isEditable}
                                                >
                                                    <X className="h-4 w-4" />
                                                    <span className="sr-only">Remove</span>
                                                </Button>
                                            </div>
                                        ))}
                                    </div>
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="expense-accounts-line-item-display-name">Line-item Title</Label>
                                    <Input
                                        data-qa-id="input-expenseLineItemTitle-payroll"
                                        required
                                        id="expense-accounts-line-item-display-name"
                                        value={state.expenseAccountsLineTitle || "Total Other Expenses"}
                                        onChange={(e) => handleExpenseLineTitleChange(e.target.value)}
                                        placeholder="Enter display name for expense total"
                                        data-testid="expense-accounts-line-item-title-input"
                                        disabled={!isEditable}
                                    />
                                </div>
                                {watchInvoiceGroupingEnabled && (
                                    <div className="space-y-2">
                                        <div className="flex items-center space-x-2">
                                            <Label htmlFor="expense-invoice-group">Invoice</Label>
                                        </div>
                                        <Select
                                            data-qa-id="select-expenseInvoiceGroup-payroll"
                                            value={state.expenseAccountsInvoiceGroup.toString()}
                                            onValueChange={(value) =>
                                                setState((prevState) => ({
                                                    ...prevState,
                                                    expenseAccountsInvoiceGroup: parseInt(value, 10),
                                                }))
                                            }
                                            disabled={!isEditable}
                                        >
                                            <SelectTrigger id="expense-invoice-group">
                                                <SelectValue placeholder="Select invoice" />
                                            </SelectTrigger>
                                            <SelectContent>
                                                {invoiceGroups.map((group) => (
                                                    <SelectItem
                                                        key={group.id}
                                                        value={group.groupNumber.toString()}
                                                    >
                                                        {group.groupNumber}
                                                    </SelectItem>
                                                ))}
                                            </SelectContent>
                                        </Select>
                                    </div>
                                )}
                            </div>
                        </>
                    )}
                </CardContent>
            </Card>
        </TooltipProvider>
    );
};

export default BillablePayrollComponent;
