import { useAuth } from '@/contexts/AuthContext';
import { CustomersList } from '@/pages/customersList/CustomersList';
import '@testing-library/jest-dom';
import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';

// Default test data and mocks
const DEFAULT_ROLE = ['billingManager'];
const DEFAULT_CUSTOMER = {
    customerSiteId: '1',
    siteNumber: '001',
    siteName: 'Main Site',
    district: 'District 1',
    billingType: 'Advanced',
    contractType: 'Standard',
    deposits: true,
    readyForInvoiceStatus: 'Ready',
};

jest.mock('@/components/ui/use-toast', () => ({
    useToast: jest.fn(() => ({
        toast: jest.fn(),
    })),
}));

jest.mock('react-router-dom', () => ({
    ...jest.requireActual('react-router-dom'),
    useNavigate: jest.fn(),
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

const mockFetch = (url: string) => {
    switch (url) {
        case '/api/customers':
            return Promise.resolve({
                ok: true,
                json: () => Promise.resolve([DEFAULT_CUSTOMER]),
            });
        case '/api/customers/statements':
            return Promise.resolve({
                ok: true,
                json: () => Promise.resolve([]),
            });
        default:
            return Promise.resolve({
                ok: true,
                json: () => Promise.resolve([]),
            });
    }
};

global.fetch = jest.fn().mockImplementation(mockFetch);

describe('CustomersList component', () => {
    beforeEach(() => {
        jest.clearAllMocks();
        (useAuth as jest.Mock).mockReturnValue({
            userRoles: DEFAULT_ROLE,
            userName: 'Test User',
            isAuthenticated: true,
            isLoading: false,
            error: null,
            refreshUserData: jest.fn(),
            logout: jest.fn(),
        });
    });

    test('renders without crashing', async () => {
        render(<CustomersList />, { wrapper: MemoryRouter });

        await waitFor(() => {
            expect(screen.getByPlaceholderText('Search...')).toBeInTheDocument();
            
            expect(screen.getByText('Generate Statements')).toBeInTheDocument();
        });

        expect(global.fetch).toHaveBeenCalledWith('/api/customers', {
            headers: {
                "x-client-roles": JSON.stringify(DEFAULT_ROLE)
            }
        });
        expect(global.fetch).toHaveBeenCalledWith('/api/customers/statements', {
            headers: {
                "x-client-roles": JSON.stringify(DEFAULT_ROLE)
            }
        });
    });
});
