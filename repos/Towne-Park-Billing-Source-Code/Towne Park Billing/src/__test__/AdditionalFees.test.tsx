import AdditionalFees from '@/components/AdditionalFees/AdditionalFees';
import { BellServiceFeeTerms, DepositedRevenueTerms, InvoiceGroup, LineTitles, MidMonthAdvancedTerms } from '@/lib/models/Contract';
import '@testing-library/jest-dom';
import { fireEvent, render, screen } from '@testing-library/react';
import { FormProvider, useForm } from 'react-hook-form';

describe('AdditionalFees Component', () => {
    const mockBellServiceFee: BellServiceFeeTerms = {
        enabled: false,
        bellServices: [{ id: '1', invoiceGroup: 1 }]
    };

    const mockMidMonthAdvance: MidMonthAdvancedTerms = {
        enabled: false,
        midMonthAdvances: [{
            id: '1',
            amount: 100,
            lineTitle: LineTitles.LESS_MID_MONTH_BILLING,
            invoiceGroup: 2
        }]
    };

    const mockDepositedRevenue: DepositedRevenueTerms = {
        enabled: false,
        depositData: [{
            id: '1',
            towneParkResponsibleForParkingTax: false,
            depositedRevenueEnabled: false,
            invoiceGroup: 1
        }]
    };

    const mockInvoiceGroups: InvoiceGroup[] = [
        { id: '1', groupNumber: 1, title: 'Invoice 1', description: 'First invoice' },
        { id: '2', groupNumber: 2, title: 'Invoice 2', description: 'Second invoice' }
    ];

    const mockOnUpdateBellServiceFee = jest.fn();
    const mockOnUpdateMidMonthAdvanced = jest.fn();
    const mockOnUpdateDepositedRevenue = jest.fn();

    beforeEach(() => {
        mockOnUpdateBellServiceFee.mockClear();
        mockOnUpdateMidMonthAdvanced.mockClear();
        mockOnUpdateDepositedRevenue.mockClear();
    });

    const TestFormProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
        const methods = useForm();
        return <FormProvider {...methods}>{children}</FormProvider>;
    };

    const setupComponent = (enabled = false, groupingEnabled = true) => {
        render(
            <TestFormProvider>
                <AdditionalFees
                    bellServiceFee={{ ...mockBellServiceFee, enabled }}
                    onUpdateBellServiceFee={mockOnUpdateBellServiceFee}
                    invoiceGroups={mockInvoiceGroups}
                    watchInvoiceGroupingEnabled={groupingEnabled}
                    midMonthAdvance={mockMidMonthAdvance}
                    onUpdateMidMonthAdvanced={mockOnUpdateMidMonthAdvanced}
                    depositedRevenue={mockDepositedRevenue}
                    onUpdateDepositedRevenue={mockOnUpdateDepositedRevenue}
                    isEditable={true}
                />
            </TestFormProvider>
        );
    };

    it('renders the component correctly', () => {
        setupComponent();
        expect(screen.getByText('Bell Service Fee')).toBeInTheDocument();
        expect(screen.getByText('Mid-Month Advance')).toBeInTheDocument();
        expect(screen.getByText('Towne Park Deposited Revenue')).toBeInTheDocument();
    });

    it('toggles the bell service fee switch', () => {
        setupComponent();

        const switchElement = screen.getByLabelText('Bell Service Fee');
        fireEvent.click(switchElement);

        expect(mockOnUpdateBellServiceFee).toHaveBeenCalledWith({
            enabled: true,
            bellServices: [{ id: '1', invoiceGroup: 1 }]
        });
    });

    it('toggles the mid-month advance switch', () => {
        setupComponent();

        const switchElement = screen.getByLabelText('Mid-Month Advance');
        fireEvent.click(switchElement);

        expect(mockOnUpdateMidMonthAdvanced).toHaveBeenCalledWith({
            enabled: true,
            midMonthAdvances: [{
                id: '1',
                amount: 100,
                lineTitle: LineTitles.LESS_MID_MONTH_BILLING,
                invoiceGroup: 2
            }]
        });
    });

    it('toggles the deposited revenue enabled switch', () => {
        setupComponent();

        const switchElement = screen.getByLabelText('Towne Park Deposited Revenue');
        fireEvent.click(switchElement);

        expect(mockOnUpdateDepositedRevenue).toHaveBeenCalledWith({
            enabled: true,
            depositData: [{
                id: '1',
                towneParkResponsibleForParkingTax: false,
                depositedRevenueEnabled: true,
                invoiceGroup: 1
            }]
        });
    });

    it('toggles the parking tax responsible switch', () => {
        setupComponent();

        const switchElement = screen.getByLabelText('Towne Park Responsible for Parking Tax');
        fireEvent.click(switchElement);

        expect(mockOnUpdateDepositedRevenue).toHaveBeenCalledWith({
            enabled: true,
            depositData: [{
                id: '1',
                towneParkResponsibleForParkingTax: true,
                depositedRevenueEnabled: false,
                invoiceGroup: 1
            }]
        });
    });

    it('shows invoice selection when either deposited revenue toggle is enabled', () => {
        setupComponent(false, true);
        
        expect(screen.queryByText(/^Invoice$/)).not.toBeInTheDocument();

        fireEvent.click(screen.getByLabelText('Towne Park Deposited Revenue'));
        expect(screen.getByText(/^Invoice$/)).toBeInTheDocument();

        fireEvent.click(screen.getByLabelText('Towne Park Deposited Revenue'));
        fireEvent.click(screen.getByLabelText('Towne Park Responsible for Parking Tax'));
        expect(screen.getByText(/^Invoice$/)).toBeInTheDocument();
    });

    it('hides invoice selection when both deposited revenue toggles are disabled', () => {
        setupComponent(false, true);
        
        fireEvent.click(screen.getByLabelText('Towne Park Deposited Revenue'));
        fireEvent.click(screen.getByLabelText('Towne Park Responsible for Parking Tax'));
        fireEvent.click(screen.getByLabelText('Towne Park Deposited Revenue'));
        fireEvent.click(screen.getByLabelText('Towne Park Responsible for Parking Tax'));
        
        expect(screen.queryByText(/^Invoice$/)).not.toBeInTheDocument();
    });

    it('does not display invoice selection when watchInvoiceGroupingEnabled is false for bell service fee', () => {
        setupComponent(true, false);
        fireEvent.click(screen.getByLabelText('Bell Service Fee'));
        expect(screen.queryByTestId('bell-service-invoice-0')).not.toBeInTheDocument();
    });

    it('does not display invoice selection when watchInvoiceGroupingEnabled is false for deposited revenue', () => {
        setupComponent(true, false);
        fireEvent.click(screen.getByLabelText('Towne Park Deposited Revenue'));
        expect(screen.queryByText('Invoice')).not.toBeInTheDocument();
    });
});
