import RevenueShare from '@/components/RevenueShare/RevenueShare';
import { InvoiceGroup, ValidationThresholdType } from '@/lib/models/Contract';
import '@testing-library/jest-dom';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';

const thresholdStructures = [
    {
        tiers: [{ sharePercentage: 0, amount: 0 }],
        revenueCodes: ["ADJ"],
        accumulationType: "Monthly",
        invoiceGroup: 1,
        validationThresholdType: ValidationThresholdType.VALIDATION_AMOUNT,
        validationThresholdAmount: 1000,
    }
];

const mockInvoiceGroups: InvoiceGroup[] = [
    { id: '1', groupNumber: 1, title: 'Invoice 1', description: 'First invoice' },
    { id: '2', groupNumber: 2, title: 'Invoice 2', description: 'Second invoice' }
];

const setThresholdStructures = jest.fn();

test('should render the component', () => {
    render(
        <RevenueShare
            revenueShareEnabled={true}
            setRevenueShareEnabled={jest.fn()}
           thresholdStructuresvalues={thresholdStructures}
            setThresholdStructures={setThresholdStructures}
            revenueCodes={[]}
            invoiceGroups={mockInvoiceGroups}
            watchInvoiceGroupingEnabled={true}
            isEditable={true}
        />
    );

    expect(screen.getByText(/Enable Revenue Share/i)).toBeInTheDocument();
});

test('should add a threshold structure', async () => {
    render(
        <RevenueShare
            revenueShareEnabled={true}
            setRevenueShareEnabled={jest.fn()}
            thresholdStructuresvalues={thresholdStructures}
            setThresholdStructures={setThresholdStructures}
            revenueCodes={['ADJ']}
            invoiceGroups={mockInvoiceGroups}
            watchInvoiceGroupingEnabled={true}
            isEditable={true}
        />
    );
    fireEvent.focus(screen.getByRole('checkbox', { name: /Create Threshold Structures/i }));
    fireEvent.click(screen.getByRole('checkbox', { name: /Create Threshold Structures/i }));
    fireEvent.focus(screen.getByText(/Add Threshold Structure/i));
    fireEvent.click(screen.getByText(/Add Threshold Structure/i));

    await waitFor(() => {
        expect(setThresholdStructures).toHaveBeenLastCalledWith(expect.arrayContaining([
            expect.objectContaining({
                accumulationType: "Monthly",
                invoiceGroup: 1,
                revenueCodes: ["ADJ"],
                tiers: expect.any(Array), 
                validationThresholdAmount: 1000,
                validationThresholdType: "ValidationAmount"
            })
        ]));
    });
});