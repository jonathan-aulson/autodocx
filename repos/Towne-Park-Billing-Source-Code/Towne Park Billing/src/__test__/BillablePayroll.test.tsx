import { EscalatorFormatType, Month, PTEBBillingType, SupportPayrollType, SupportServicesType } from '@/lib/models/Contract';
import '@testing-library/jest-dom';
import { fireEvent, render, screen } from '@testing-library/react';
import React, { act } from 'react';
import { FormProvider, useForm } from 'react-hook-form';
import BillablePayrollComponent from '../components/BillablePayroll/BillablePayroll';

// Mock data and functions
const mockBillableAccounts = {
    enabled: false,
    billableAccountsData: [
        {
            payrollAccountsData: "[]",
            payrollAccountsLineTitle: '',
            payrollAccountsInvoiceGroup: 1,
            payrollTaxesEnabled: false,
            payrollTaxesBillingType: PTEBBillingType.ACTUAL,
            payrollTaxesPercentage: 12,
            payrollTaxesLineTitle: 'Test Line Title',
            payrollSupportEnabled: false,
            payrollSupportBillingType: SupportServicesType.FIXED,
            payrollSupportPayrollType: SupportPayrollType.BILLABLE,
            payrollSupportAmount: 100,
            payrollSupportLineTitle: 'Test Line Title 2',
            payrollExpenseAccountsData: "[]",
            payrollExpenseAccountsLineTitle: 'Test Line Title 3',
            payrollExpenseAccountsInvoiceGroup: 1,
            additionalPayrollAmount: 100,
            payrollTaxesEscalatorEnable:false,
            payrollTaxesEscalatorMonth:Month.JANUARY,
            payrollTaxesEscalatorvalue:0,
            payrollTaxesEscalatorType:EscalatorFormatType.PERCENTAGE

        }
    ],
};

const mockInvoiceGroups = [
    { id: '1', groupNumber: 101, title: 'Invoice 101', description: 'First invoice group' },
    { id: '2', groupNumber: 102, title: 'Invoice 102', description: 'Second invoice group' }
];

const mockOnUpdateBillableAccounts = jest.fn();

const TestFormProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const methods = useForm();
    return <FormProvider {...methods}>{children}</FormProvider>;
};

const setupComponent = (enabled = false, groupingEnabled = true) => {
    render(
        <TestFormProvider>
            <BillablePayrollComponent
                billableAccounts={{ ...mockBillableAccounts, enabled }}
                onUpdateBillableAccounts={mockOnUpdateBillableAccounts}
                invoiceGroups={mockInvoiceGroups}
                watchInvoiceGroupingEnabled={groupingEnabled}
                payrrolAccount={{ id: "1", name: 'Test Account' }}
                expenseAccount={{ id: "1", name: 'Test Expense Account' }}
                errors={undefined}
                isEditable={true}
            />
        </TestFormProvider>
    );
};

// Test suite
describe('BillablePayrollComponent', () => {

    beforeEach(() => {
        mockOnUpdateBillableAccounts.mockReset();
    });

    test('renders the component correctly', () => {
        setupComponent();
        expect(screen.getByText('Enable Billable Accounts')).toBeInTheDocument();
        expect(screen.getByTestId('billable-payroll-card')).toBeInTheDocument();
    });

    test('toggles the payroll accounts switch', () => {
        setupComponent();

        const switchElement = screen.getByTestId('payroll-switch');

        act(() => {
            fireEvent.click(switchElement);
        });

        expect(mockOnUpdateBillableAccounts).toHaveBeenCalledTimes(3);
        expect(mockOnUpdateBillableAccounts).toHaveBeenCalledWith(
            expect.objectContaining({ enabled: true })
        );
    });

    test('opens and closes the add payroll account dialog', () => {
        setupComponent(true);

        const dialogButton = screen.getByTestId('plus-button');
        fireEvent.click(dialogButton);

        expect(screen.getByTestId('dialog-title')).toBeInTheDocument();

        const closeButton = screen.getByTestId('close-dialog-button');
        fireEvent.click(closeButton);
        expect(screen.queryByTestId('dialog-title')).not.toBeInTheDocument();
    });

    test('updates the line-item title', () => {
        setupComponent(true);

        const inputElement = screen.getByPlaceholderText('Enter display name for payroll total');
        fireEvent.change(inputElement, { target: { value: 'New Title' } });

        expect(screen.getByDisplayValue('New Title')).toBeInTheDocument();
    });

    test('does not display invoice selection when watchInvoiceGroupingEnabled is false', () => {
        setupComponent(true, false);

        expect(screen.queryByText(/invoice/i)).not.toBeInTheDocument();
    });

    test('toggles the PTEB enabled switch', () => {
        setupComponent(true);

        const ptebCheckbox = screen.getByLabelText('Create PTEB Line-item');
        fireEvent.focus(ptebCheckbox);
        fireEvent.click(ptebCheckbox);

        expect(mockOnUpdateBillableAccounts).toHaveBeenCalledWith(
            expect.objectContaining({
                billableAccountsData: expect.arrayContaining([
                    expect.objectContaining({ payrollTaxesEnabled: true })
                ])
            })
        );
    });

    test('updates PTEB billing type to percentage', () => {
        setupComponent(true, true);

        const ptebSwitch = screen.getByLabelText('Create PTEB Line-item');
        fireEvent.focus(ptebSwitch);
        fireEvent.click(ptebSwitch);

        const percentageRadio = screen.getByTestId('pteb-percentage-radio');
        fireEvent.click(percentageRadio);

        expect(mockOnUpdateBillableAccounts).toHaveBeenCalledWith(
            expect.objectContaining({
                billableAccountsData: expect.arrayContaining([
                    expect.objectContaining({ payrollTaxesBillingType: PTEBBillingType.PERCENTAGE })
                ])
            })
        );
    });

    test('updates PTEB percentage', () => {
        setupComponent(true, true);

        const ptebCheckbox = screen.getByLabelText('Create PTEB Line-item');
        fireEvent.focus(ptebCheckbox);
        fireEvent.click(ptebCheckbox);

        const percentageRadio = screen.getByTestId('pteb-percentage-radio');
        fireEvent.click(percentageRadio);

        const percentageInput = screen.getByTestId('pteb-percentage-input');
        fireEvent.change(percentageInput, { target: { value: '15' } });

        expect(mockOnUpdateBillableAccounts).toHaveBeenCalledWith(
            expect.objectContaining({
                billableAccountsData: expect.arrayContaining([
                    expect.objectContaining({ payrollTaxesPercentage: 15 })
                ])
            })
        );
    });

    test('updates PTEB line-item display name', () => {
        setupComponent(true);

        const ptebCheckbox = screen.getByLabelText('Create PTEB Line-item');
        fireEvent.focus(ptebCheckbox);
        fireEvent.click(ptebCheckbox);

        const inputElement = screen.getByTestId('pteb-line-item-title-input');
        fireEvent.change(inputElement, { target: { value: 'PTEB Title' } });

        expect(inputElement).toHaveValue('PTEB Title');
        expect(mockOnUpdateBillableAccounts).toHaveBeenCalledWith(
            expect.objectContaining({
                billableAccountsData: expect.arrayContaining([
                    expect.objectContaining({ payrollTaxesLineTitle: 'PTEB Title' })
                ])
            })
        );
    });

    test('toggles Support Services enabled switch', () => {
        setupComponent(true);

        const supportServicesCheckbox = screen.getByTestId('support-services-enabled-checkbox');
        fireEvent.focus(supportServicesCheckbox);
        fireEvent.click(supportServicesCheckbox);

        expect(mockOnUpdateBillableAccounts).toHaveBeenCalledWith(
            expect.objectContaining({
                billableAccountsData: expect.arrayContaining([
                    expect.objectContaining({ payrollSupportEnabled: true })
                ])
            })
        );
    });

    test('selects fixed fee for Support Services', () => {
        setupComponent(true);

        const supportServicesSwitch = screen.getByTestId('support-services-enabled-checkbox');
        fireEvent.focus(supportServicesSwitch);
        fireEvent.click(supportServicesSwitch);

        const fixedFeeRadio = screen.getByLabelText('Fixed Fee');
        fireEvent.click(fixedFeeRadio);

        expect(mockOnUpdateBillableAccounts).toHaveBeenCalledWith(
            expect.objectContaining({
                billableAccountsData: expect.arrayContaining([
                    expect.objectContaining({ payrollSupportBillingType: SupportServicesType.FIXED })
                ])
            })
        );
    });

    test('selects percentage for Support Services', () => {
        setupComponent(true);

        const supportServicesCheckbox = screen.getByTestId('support-services-enabled-checkbox');
        fireEvent.focus(supportServicesCheckbox);
        fireEvent.click(supportServicesCheckbox);

        const percentageRadio = screen.getByTestId('support-services-percentage-radio');
        fireEvent.click(percentageRadio);

        expect(mockOnUpdateBillableAccounts).toHaveBeenCalledWith(
            expect.objectContaining({
                billableAccountsData: expect.arrayContaining([
                    expect.objectContaining({ payrollSupportBillingType: SupportServicesType.PERCENTAGE })
                ])
            })
        );
    });

    test('updates Support Services fixed amount', () => {
        setupComponent(true);

        const supportServicesCheckbox = screen.getByTestId('support-services-enabled-checkbox');
        fireEvent.focus(supportServicesCheckbox);
        fireEvent.click(supportServicesCheckbox);

        const fixedFeeRadio = screen.getByTestId('support-services-fixed-radio');
        fireEvent.click(fixedFeeRadio);

        const fixedAmountInput = screen.getByTestId('support-services-fixed-amount-input');
        fireEvent.change(fixedAmountInput, { target: { value: '200' } });

        expect(fixedAmountInput).toHaveValue('$200.00');
        expect(mockOnUpdateBillableAccounts).toHaveBeenCalledWith(
            expect.objectContaining({
                billableAccountsData: expect.arrayContaining([
                    expect.objectContaining({ payrollSupportAmount: 200 })
                ])
            })
        );
    });

    test('updates Support Services line-item display name', () => {
        setupComponent(true);

        const supportServicesSwitch = screen.getByTestId('support-services-enabled-checkbox');
        fireEvent.focus(supportServicesSwitch);
        fireEvent.click(supportServicesSwitch);

        const inputElement = screen.getByPlaceholderText('Enter display name for Support Services');
        fireEvent.change(inputElement, { target: { value: 'Support Services Title' } });

        expect(inputElement).toHaveValue('Support Services Title');
        expect(mockOnUpdateBillableAccounts).toHaveBeenCalledWith(
            expect.objectContaining({
                billableAccountsData: expect.arrayContaining([
                    expect.objectContaining({ payrollSupportLineTitle: 'Support Services Title' })
                ])
            })
        );
    });

    test('updates expense accounts line-item title', () => {
        setupComponent(true);

        const inputElement = screen.getByTestId('expense-accounts-line-item-title-input');
        fireEvent.change(inputElement, { target: { value: 'Expense Accounts Title' } });

        expect(inputElement).toHaveValue('Expense Accounts Title');
        expect(mockOnUpdateBillableAccounts).toHaveBeenCalledWith(
            expect.objectContaining({
                billableAccountsData: expect.arrayContaining([
                    expect.objectContaining({ payrollExpenseAccountsLineTitle: 'Expense Accounts Title' })
                ])
            })
        );
    });
});