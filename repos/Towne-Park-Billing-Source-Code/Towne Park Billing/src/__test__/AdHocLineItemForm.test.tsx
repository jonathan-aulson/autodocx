import '@testing-library/jest-dom';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import AdHocLineItemForm from '../components/invoiceDetails/AdHocLineItemForm';

// Mock the useToast functionality
jest.mock('@/components/ui/use-toast', () => ({
    useToast: () => ({
        toast: jest.fn(),
    }),
}));

describe('AdHocLineItemForm Component', () => {
    const mockOnAddLineItem = jest.fn();
    const mockAlreadyHasReimbursableExpense = jest.fn();

    beforeEach(() => {
        mockOnAddLineItem.mockClear();
        mockAlreadyHasReimbursableExpense.mockClear();
    });

    it('renders form fields correctly', () => {
        render(<AdHocLineItemForm
            onAddLineItem={mockOnAddLineItem}
            invoiceNumber='123'
        />);

        expect(screen.getByText('Add Ad-Hoc Line Item')).toBeInTheDocument();
        expect(screen.getByText('Type')).toBeInTheDocument();
        expect(screen.getByText('Amount')).toBeInTheDocument();
    });

    it('displays error message when required fields are not filled', async () => {
        render(<AdHocLineItemForm
            onAddLineItem={mockOnAddLineItem}
            invoiceNumber='123'
        />);

        fireEvent.click(screen.getByRole('button', { name: /Add Line Item/i }));

        await waitFor(() => {
            expect(screen.getByText('Please select type.')).toBeInTheDocument();
            expect(screen.getByText('Amount is required.')).toBeInTheDocument();
        });
    });
});
