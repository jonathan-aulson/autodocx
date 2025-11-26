import { useAuth } from '@/contexts/AuthContext';
import { CustomersDetails } from '@/pages/customersDetails/CustomersDetails';
import '@testing-library/jest-dom';
import { act, render, waitFor } from '@testing-library/react';

// Mock react-router-dom
jest.mock('react-router-dom', () => ({
    ...jest.requireActual('react-router-dom'),
    useParams: () => ({
        customerSiteId: 'test-site-id',
    }),
}));

// Mock the AuthContext
jest.mock('@/contexts/AuthContext', () => ({
    useAuth: jest.fn().mockReturnValue({
        userRoles: ['billingManager'],
        userName: 'Test User',
        isAuthenticated: true,
        isLoading: false,
        error: null,
        refreshUserData: jest.fn(),
        logout: jest.fn(),
    }),
}));

// Mock the CustomerProvider context
jest.mock('@/pages/customersDetails/CustomersDetailContext', () => {
    const mockCustomerData = {
        siteName: 'Test Customer',
        address: '1234 Main St, Springfield, IL 62701',
        accountManager: 'Test Manager',
        siteNumber: '12345',
        invoiceRecipient: 'John Doe',
        billingContactEmail: 'john.doe@example.com',
    };

    const mockContractData = {
        id: 'CON-001',
        createdDate: '2024-06-01',
        contractType: 'Service Contract',
        billingType: 'Hourly',
        services: [
            { id: 'SER-001', name: 'Service A', fee: 50.0 },
            { id: 'SER-002', name: 'Service B', fee: 50.0 },
        ],
        incrementMonth: 'January',
        incrementAmount: 10.0,
    };

    return {
        useCustomerDetails: jest.fn().mockReturnValue({
            customer: mockCustomerData,
            setCustomer: jest.fn(),
            fetchCustomerDetails: jest.fn().mockResolvedValue(mockCustomerData),
            contractDetails: mockContractData,
            setContractDetails: jest.fn(),
            contractId: mockContractData.id,
            setContractId: jest.fn(),
            fetchContractDetails: jest.fn().mockResolvedValue(mockContractData)
        }),
        CustomerProvider: ({ children }: { children: React.ReactNode }) => children,
    };
});

// Mock fetch responses
const mockFetch = jest.fn().mockImplementation((url) => {
    if (url.includes('/api/customers/test-site-id/statements')) {
        return Promise.resolve({
            ok: true,
            json: () => Promise.resolve([
                {
                    id: 'stmt001',
                    createdMonth: 'April 2023',
                    servicePeriod: 'April 1 - April 30, 2023',
                    totalAmount: 1000.00,
                    status: 'SENT',
                    invoices: [
                        {
                            id: 'sum-inv001',
                            totalAmount: 400.00,
                            invoices: [
                                {
                                    id: 'inv001',
                                    number: '2023-04-INV001',
                                    amount: 400.00,
                                    invoiceDate: '2023-04-01',
                                    paymentTerms: '30 days',
                                    lineItems: [
                                        { id: 'LI-001', description: 'Service A', quantity: 2, rate: 50.0 },
                                        { id: 'LI-002', description: 'Service B', quantity: 2, rate: 50.0 },
                                    ],
                                },
                            ],
                        },
                    ],
                    forecastData: JSON.stringify({
                        forecastedRevenue: 1000,
                        postedRevenue: 900,
                        forecastDeviationPercentage: 10,
                        forecastDeviationAmount: 100
                    })
                },
            ]),
        });
    }
    return Promise.resolve({
        ok: true,
        json: () => Promise.resolve({}),
    });
});

global.fetch = mockFetch;

// Mock React Suspense component for our tests
jest.mock('react', () => {
    const actualReact = jest.requireActual('react');
    return {
        ...actualReact,
        Suspense: ({ children }: { children: React.ReactNode }) => children,
    };
});

// Create a mock for the StatementContext to avoid more act warnings
jest.mock('@/components/BillingStatement/StatementContext', () => ({
    StatementContext: {
        Provider: ({ children }: { children: React.ReactNode }) => children,
    },
}));

describe('CustomersDetails component', () => {
    beforeEach(() => {
        jest.clearAllMocks();
        // Update to set the mock return value for useAuth
        (useAuth as jest.Mock).mockReturnValue({
            userRoles: ['billingManager'],
            userName: 'Test User',
            isAuthenticated: true,
            isLoading: false,
            error: null,
            refreshUserData: jest.fn(),
            logout: jest.fn(),
        });
    });

    test('renders customer details initially', async () => {
        let renderResult;
        await act(async () => {
            renderResult = render(<CustomersDetails />);
        });
        
        const { getByTestId } = renderResult!;
        expect(getByTestId('customer-details')).toBeInTheDocument();
    });

    test('renders customer details after data fetch', async () => {
        let renderResult;
        
        await act(async () => {
            renderResult = render(<CustomersDetails />);
            // Wait a bit to ensure all effects run
            await new Promise(resolve => setTimeout(resolve, 100));
        });
        
        const { getByTestId } = renderResult!;
        
        await waitFor(() => {
            expect(getByTestId('customer-details')).toBeInTheDocument();
        }, { timeout: 2000 });
    });

    test('renders contract details tab', async () => {
        let renderResult;
        
        await act(async () => {
            renderResult = render(<CustomersDetails />);
            await new Promise(resolve => setTimeout(resolve, 100));
        });
        
        const { getByTestId } = renderResult!;
        
        await waitFor(() => {
            expect(getByTestId('contract-details-tab1')).toBeInTheDocument();
        }, { timeout: 2000 });
    });

    test('renders billing statement tab', async () => {
        let renderResult;
        
        await act(async () => {
            renderResult = render(<CustomersDetails />);
            await new Promise(resolve => setTimeout(resolve, 100));
        });
        
        const { getByTestId } = renderResult!;
        
        await waitFor(() => {
            expect(getByTestId('billing-statement-tab1')).toBeInTheDocument();
        }, { timeout: 2000 });
    });
});
