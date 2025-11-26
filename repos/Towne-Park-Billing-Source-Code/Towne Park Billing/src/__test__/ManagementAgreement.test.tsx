import ManagementAgreement from '@/components/ManagementAgreement/ManagementAgreement';
import { ClaimsType, EscalatorFormatType, InsuranceType, ManagementAgreementType, Month, ProfitShareAccumulationType, PTEBBillingType, SupportPayrollType, SupportServicesType, ValidationThresholdType } from '@/lib/models/Contract';
import '@testing-library/jest-dom';
import { fireEvent, render } from '@testing-library/react';
import { waitFor } from '@testing-library/react';

// Sample props for tests
const defaultProps = {
    managementAgreement: {
        enabled: true,
        ManagementFees: [
            {
                id: '1',
                invoiceGroup: 1,
                managementAgreementType: ManagementAgreementType.FIXED_FEE,
                managementFeeEscalatorEnabled: false,
                managementFeeEscalatorMonth: Month.JANUARY,
                managementFeeEscalatorType: EscalatorFormatType.PERCENTAGE,
                managementFeeEscalatorValue: 0,
                fixedFeeAmount: 0,
                perLaborHourJobCodeData: [],
                laborHourJobCode: '',
                laborHourRate: 0,
                laborHourOvertimeRate: 0,
                revenuePercentageAmount: 0,
                insuranceEnabled: false, // initially disabled
                insuranceLineTitle: '',
                insuranceType: InsuranceType.BASED_ON_BILLABLE_ACCOUNTS,
                insuranceAdditionalPercentage: 0,
                insuranceFixedFeeAmount: 0,
                profitShareEnabled: false,
                profitShareEscalatorEnabled : false,
                profitShareEscalatorMonth: Month.JANUARY,
                profitShareEscalatorType: EscalatorFormatType.PERCENTAGE,
                profitShareAccumulationType: ProfitShareAccumulationType.MONTHLY,
                profitShareTierData: [],
                validationThresholdEnabled: false,
                validationThresholdAmount: null,
                validationThresholdType: ValidationThresholdType.VALIDATION_AMOUNT,
                claimsEnabled: false,
                claimsType: ClaimsType.ANNUALLY_CALENDAR,
                claimsCapAmount: null,
                claimsLineTitle: null,
                nonGlBillableExpensesEnabled: false,
                nonGlBillableExpenses: [],
                
                
            },
        ],
    },
    onUpdateManagementAgreement: jest.fn(),
    invoiceGroups: [
        { id: 'grp1', groupNumber: 1, title: 'Group 1', description: null },
        { id: 'grp2', groupNumber: 2, title: 'Group 2', description: null }
    ],
    watchInvoiceGroupingEnabled: true,
    billableAccounts: {
        enabled: true,
        billableAccountsData: [
            {
                payrollExpenseAccountsData: JSON.stringify([
                    { code: '7080', title: 'General Liability', isEnabled: true },
                    { code: '7082', title: 'Vehicle Insurance', isEnabled: true },
                ]),
                payrollAccountsData: JSON.stringify([
                    { code: '6010', isEnabled: true },
                    { code: '6014', isEnabled: false },
                ]),
                payrollAccountsInvoiceGroup: 1,
                payrollAccountsLineTitle: 'Payroll Accounts',
                payrollTaxesEnabled: true,
                payrollTaxesBillingType: PTEBBillingType.ACTUAL,
                payrollTaxesPercentage: 5,
                payrollTaxesLineTitle: 'Payroll Taxes',
                payrollSupportEnabled: true,
                payrollSupportBillingType: SupportServicesType.FIXED,
                payrollSupportPayrollType: SupportPayrollType.BILLABLE,
                payrollSupportAmount: 1000,
                payrollSupportLineTitle: 'Support Services',
                payrollExpenseAccountsInvoiceGroup: 1,
                payrollExpenseAccountsLineTitle: 'Total Other Expenses',
                additionalPayrollAmount: 500,
                 payrollTaxesEscalatorEnable:false,
                 payrollTaxesEscalatorMonth:Month.JANUARY,
               payrollTaxesEscalatorvalue:0,
               payrollTaxesEscalatorType:EscalatorFormatType.PERCENTAGE
            },
        ],
    },
    errors: {},
    isEditable: true,
};

describe('ManagementAgreement Component', () => {
    it('renders correctly when enabled', () => {
        const { getByTestId } = render(<ManagementAgreement {...defaultProps} />);
        expect(getByTestId('management-agreement-card')).toBeInTheDocument();
        expect(getByTestId('management-agreement-switch')).toBeChecked();
    });

    it('renders insurance section and toggles insurance line item', () => {
        const { getByLabelText, getByText, getByTestId } = render(<ManagementAgreement {...defaultProps} />);

        // Enable insurance line item
        const toggleInsuranceCheckbox = getByTestId('create-insurance-line-item');
        fireEvent.focus(toggleInsuranceCheckbox);
        fireEvent.click(toggleInsuranceCheckbox);

        expect(defaultProps.onUpdateManagementAgreement).toHaveBeenCalledWith(
            expect.objectContaining({
                ManagementFees: expect.arrayContaining([
                    expect.objectContaining({ insuranceEnabled: true })
                ])
            })
        );

        // Verify insurance section visibility
        expect(getByText('Insurance')).toBeInTheDocument();
        expect(toggleInsuranceCheckbox).toBeChecked();

        // Change insurance type
        const fixedFeeOption = getByTestId('insurance-fixed-fee');
        fireEvent.click(fixedFeeOption);

        expect(defaultProps.onUpdateManagementAgreement).toHaveBeenCalledWith(
            expect.objectContaining({
                ManagementFees: expect.arrayContaining([
                    expect.objectContaining({ insuranceType: InsuranceType.FIXED_FEE })
                ])
            })
        );
    });

    it('updates management agreement type correctly', () => {
        const { getByTestId } = render(<ManagementAgreement {...defaultProps} />);

        // Switch to "Per Labor Hour"
        const perLaborHourOption = getByTestId('per-labor-hour-0');
        fireEvent.click(perLaborHourOption);

        expect(defaultProps.onUpdateManagementAgreement).toHaveBeenCalledWith(
            expect.objectContaining({
                ManagementFees: expect.arrayContaining([
                    expect.objectContaining({
                        managementAgreementType: ManagementAgreementType.PER_LABOR_HOUR,
                        fixedFeeAmount: null
                    })
                ])
            })
        );
    });

    describe('Profit Share Section', () => {
        it('enables profit share and sets basic percentage', async () => {

            defaultProps.onUpdateManagementAgreement.mockClear();

            const { getByLabelText, getByRole } = render(<ManagementAgreement {...defaultProps} />);
            
            // Enable profit share
            const enableProfitShareCheckbox = getByLabelText('Enable Profit Share');
            fireEvent.focus(enableProfitShareCheckbox);
            fireEvent.click(enableProfitShareCheckbox);

            // Set basic share percentage
            const percentageInput = getByRole('textbox', { name: /Share Percentage/i });
            fireEvent.change(percentageInput, { target: { value: '25' } });

            await waitFor(() => {
                expect(defaultProps.onUpdateManagementAgreement).toHaveBeenCalledWith(
                    expect.objectContaining({
                        ManagementFees: expect.arrayContaining([
                            expect.objectContaining({
                                profitShareEnabled: true,
                                profitShareTierData: expect.arrayContaining([{ sharePercentage: 25, amount: 0, escalatorValue: 0 }])
                            })
                        ])
                    })
                );
            })
        });

        it('creates and manages threshold tiers', () => {
            const { getByLabelText, getByText } = render(<ManagementAgreement {...defaultProps} />);
            
            // Enable profit share
            const enableProfitShareCheckbox = getByLabelText('Enable Profit Share');
            fireEvent.focus(enableProfitShareCheckbox);
            fireEvent.click(enableProfitShareCheckbox);

            // Enable threshold structure
            const enableProfitShareThresoldCheckbox = getByLabelText('Create Threshold Structure');
            fireEvent.focus(enableProfitShareThresoldCheckbox);
            fireEvent.click(enableProfitShareThresoldCheckbox);

            // Add a tier
            fireEvent.click(getByText('Add Tier'));

            expect(defaultProps.onUpdateManagementAgreement).toHaveBeenCalledWith(
                expect.objectContaining({
                    ManagementFees: expect.arrayContaining([
                        expect.objectContaining({
                            profitShareTierData: expect.arrayContaining([
                                expect.objectContaining({ sharePercentage: 0, amount: 0, escalatorValue: 0 })
                            ])
                        })
                    ])
                })
            );
        });
    });

    describe('Validation Threshold Section', () => {
        it('enables and configures validation threshold', async () => {
            const { getByLabelText, getAllByRole, getByText } = render(<ManagementAgreement {...defaultProps} />);
            
            // Enable profit share first
            const enableProfitShareCheckbox = getByLabelText('Enable Profit Share');
            fireEvent.focus(enableProfitShareCheckbox);
            fireEvent.click(enableProfitShareCheckbox);

            // Enable validation threshold
            const enableValidationThresoldCheckbox = getByLabelText('Set Validation Threshold');
            fireEvent.focus(enableValidationThresoldCheckbox);
            fireEvent.click(enableValidationThresoldCheckbox);

            // Open the select dropdown
            const selectButtons = getAllByRole('combobox');
            fireEvent.click(selectButtons[1]); // Click the second combobox for validation threshold type

            // Force a small delay to ensure the select content is mounted
            await new Promise(resolve => setTimeout(resolve, 50));

            // Select "Validation Amount" option
            const validationAmountOption = getByText('Validation Amount');
            fireEvent.click(validationAmountOption);

            // Force a delay to allow state updates to propagate
            await new Promise(resolve => setTimeout(resolve, 50));

            const lastCall = defaultProps.onUpdateManagementAgreement.mock.calls.length - 1;
            const lastCallArgs = defaultProps.onUpdateManagementAgreement.mock.calls[lastCall][0];
            
            expect(lastCallArgs.ManagementFees[0].validationThresholdEnabled).toBe(true);
            expect(lastCallArgs.ManagementFees[0].validationThresholdType)
                .toBe(ValidationThresholdType.VALIDATION_AMOUNT);
        });
    });

    describe('Claims Section', () => {
        it('enables and configures claims', () => {
            const { getByTestId, getByLabelText } = render(<ManagementAgreement {...defaultProps} />);
            
            // Enable claims
            const enableClaimsCheckbox = getByTestId('create-claims-line-item');
            fireEvent.focus(enableClaimsCheckbox);
            fireEvent.click(enableClaimsCheckbox);

            // Set claims cap amount
            const capAmountInput = getByLabelText('Claims Cap Amount');
            fireEvent.change(capAmountInput, { target: { value: '5000' } });

            expect(defaultProps.onUpdateManagementAgreement).toHaveBeenCalledWith(
                expect.objectContaining({
                    ManagementFees: expect.arrayContaining([
                        expect.objectContaining({
                            claimsEnabled: true,
                            claimsCapAmount: 5000
                        })
                    ])
                })
            );
        });

        it('displays billable claims accounts when enabled', () => {
            const { getByTestId, getByText } = render(<ManagementAgreement {...defaultProps} />);
            
            // Enable claims
            const enableClaimsCheckbox = getByTestId('create-claims-line-item');
            fireEvent.focus(enableClaimsCheckbox);
            fireEvent.click(enableClaimsCheckbox);

            // Verify accounts are displayed
            expect(getByText('Billable Claims Accounts')).toBeInTheDocument();
        });
    });
});

