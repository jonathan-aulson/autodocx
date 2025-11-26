import { useAuth } from '@/contexts/AuthContext';
import { CustomerDetail } from "@/lib/models/GeneralInfo";
import '@testing-library/jest-dom';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import GeneralInfoTab from '../components/TabsCustomersDetails/GeneralInfoTab';

jest.mock('@/contexts/AuthContext', () => ({
    useAuth: jest.fn().mockReturnValue({
        userRoles: ['billingAdmin'],
        userName: 'Test User',
        isAuthenticated: true,
        isLoading: false,
        error: null,
        refreshUserData: jest.fn(),
        logout: jest.fn(),
    }),
}));

const sampleCustomer: CustomerDetail = {
    siteName: 'Sample Site',
    address: '1234 Main St, Springfield, IL 62701',
    glString: '123456',
    district: 'Sample District',
    siteNumber: '123456',
    invoiceRecipient: 'John Doe',
    accountManager: 'Jane Smith',
    accountManagerId: '123',
    billingContactEmail: 'john.doe@example.com',
    startDate: '2021-01-01',
    closeDate: '2022-01-01',
    totalRoomsAvailable: '100',
    totalAvailableParking: '50',
    districtManager: 'John Doe',
    assistantDistrictManager: 'Jane Smith',
    assistantAccountManager: 'John Smith',
    vendorId: '123456',
    legalEntity: 'Towne Park, LLC.',
    businessSegment: 'Hospitality',
    cogSegment: 'Base',
    plCategory: 'Operations',
    svpRegion: 'Central',
};

describe('GeneralInfoTab', () => {
    beforeEach(() => {
        jest.clearAllMocks();
        // Reset the mock for useAuth
        (useAuth as jest.Mock).mockReturnValue({
            userRoles: ['billingAdmin'],
            userName: 'Test User',
            isAuthenticated: true,
            isLoading: false,
            error: null,
            refreshUserData: jest.fn(),
            logout: jest.fn(),
        });
    });
    
    test('renders without crashing', () => {
        render(<GeneralInfoTab customer={sampleCustomer} onSave={jest.fn()} />);
    });

    test('displays customer information', async () => {
        render(<GeneralInfoTab customer={sampleCustomer} onSave={jest.fn()} />);

        await waitFor(() => {
            expect(screen.getByRole('button', { name: /Edit/i })).toBeInTheDocument();
        });
        fireEvent.click(screen.getByRole('button', { name: /Edit/i }));

        expect(screen.getByLabelText(/Site ID/i)).toHaveValue(sampleCustomer.siteNumber);
        expect(screen.getByLabelText(/Account Manager ID/i)).toHaveValue(sampleCustomer.accountManagerId);
        expect(screen.getByLabelText(/Billing Contact Email/i)).toHaveValue(sampleCustomer.billingContactEmail);
    });

    test('enables fields on edit button click', async () => {
        render(<GeneralInfoTab customer={sampleCustomer} onSave={jest.fn()} />);

        await waitFor(() => {
            expect(screen.getByRole('button', { name: /Edit/i })).toBeInTheDocument();
        });

        fireEvent.click(screen.getByRole('button', { name: /Edit/i }));

        expect(screen.getByLabelText(/Billing Contact Email/i)).not.toBeDisabled();
        expect(screen.getByLabelText(/Account Manager ID/i)).not.toBeDisabled();
        expect(screen.getByLabelText(/Billing Contact Email/i)).not.toBeDisabled();
    });

    test('calls onSave with updated data on save button click', async () => {
        const mockOnSave = jest.fn();
        render(<GeneralInfoTab customer={sampleCustomer} onSave={mockOnSave} />);

        await waitFor(() => {
            expect(screen.getByRole('button', { name: /Edit/i })).toBeInTheDocument();
        });

        fireEvent.click(screen.getByRole('button', { name: /Edit/i }));

        const updatedEmail = 'JaneDoe@test.com';
        fireEvent.change(screen.getByLabelText(/Billing Contact Email/i), { target: { value: updatedEmail } });

        fireEvent.click(screen.getByRole('button', { name: /Save/i }));

        // Using an async/await method to ensure form submission is captured
        await waitFor(() => {
            expect(mockOnSave).toHaveBeenCalledWith({
                ...sampleCustomer,
                billingContactEmail: updatedEmail,
            });
        });
    });

    test('shows validation error for invalid email', async () => {
        render(<GeneralInfoTab customer={sampleCustomer} onSave={jest.fn()} />);

        await waitFor(() => {
            expect(screen.getByRole('button', { name: /Edit/i })).toBeInTheDocument();
        });

        fireEvent.click(screen.getByRole('button', { name: /Edit/i }));

        fireEvent.change(screen.getByLabelText(/Billing Contact Email/i), { target: { value: 'invalid-email' } });

        fireEvent.click(screen.getByRole('button', { name: /Save/i }));

        expect(await screen.findByText(/Please enter valid email addresses separated by a semicolon/i)).toBeInTheDocument();
    });

    test('reverts changes on cancel button click', async () => {
        render(<GeneralInfoTab customer={sampleCustomer} onSave={jest.fn()} />);

        await waitFor(() => {
            expect(screen.getByRole('button', { name: /Edit/i })).toBeInTheDocument();
        });

        fireEvent.click(screen.getByRole('button', { name: /Edit/i }));

        const updatedEmail = 'JaneDoe@test.com';
        fireEvent.change(screen.getByLabelText(/Billing Contact Email/i), { target: { value: updatedEmail } });

        fireEvent.click(screen.getByRole('button', { name: /Cancel/i }));

        expect(screen.getByLabelText(/Billing Contact Email/i)).toHaveValue(sampleCustomer.billingContactEmail);
    });
});