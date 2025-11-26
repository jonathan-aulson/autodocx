import RevenueReviewThreshold from '@/components/AdminPanel/RevenueReviewThreshold/RevenueReviewThreshold';
import { useAuth } from '@/contexts/AuthContext';
import '@testing-library/jest-dom';
import { render, screen, waitFor } from '@testing-library/react';

// Mock the AuthContext
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

// Mock the toast component
jest.mock('@/components/ui/use-toast', () => ({
    useToast: jest.fn(() => ({
        toast: jest.fn(),
    })),
}));

const mockData = [
    {
        customerSiteId: 'test-site-1',
        contractId: 'test-contract-1',
        siteNumber: '001',
        siteName: 'Test Site 1',
        deviationPercentage: 10,
        deviationAmount: 1000,
    },
    {
        customerSiteId: 'test-site-2',
        contractId: 'test-contract-2',
        siteNumber: '002',
        siteName: 'Test Site 2',
        deviationPercentage: 20,
        deviationAmount: 2000,
    },
    {
        customerSiteId: 'test-site-3',
        contractId: 'test-contract-3',
        siteNumber: '003',
        siteName: 'Test Site 3',
        deviationPercentage: 30,
        deviationAmount: 3000,
    },
    {
        customerSiteId: 'test-site-4',
        contractId: 'test-contract-4',
        siteNumber: '004',
        siteName: 'Test Site 4',
        deviationPercentage: 40,
        deviationAmount: 4000,
    },
    {
        customerSiteId: 'test-site-5',
        contractId: 'test-contract-5',
        siteNumber: '005',
        siteName: 'Test Site 5',
        deviationPercentage: 50,
        deviationAmount: 5000,
    },
    {
        customerSiteId: 'test-site-6',
        contractId: 'test-contract-6',
        siteNumber: '006',
        siteName: 'Test Site 6',
        deviationPercentage: 60,
        deviationAmount: 6000,
    },
];

// Setup mock fetch that resolves immediately
const mockFetchImplementation = jest.fn().mockImplementation((url) => {
    if (url === '/api/deviations') {
        return Promise.resolve({
            ok: true,
            json: () => Promise.resolve(mockData),
        });
    }
    return Promise.resolve({
        ok: true,
        json: () => Promise.resolve({}),
    });
});

describe('RevenueReviewThreshold', () => {
    beforeEach(() => {
        jest.clearAllMocks();
        global.fetch = mockFetchImplementation;

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

    afterEach(() => {
        // Clean up any global mocks
        jest.restoreAllMocks();
    });

    // Increase timeout for all tests to avoid timeouts
    jest.setTimeout(15000);

    // Test for basic rendering - simplify to just check if component renders at all
    test('renders the component', async () => {
        render(<RevenueReviewThreshold />);

        // First check that loading state appears
        expect(screen.getByText('Loading...')).toBeInTheDocument();

        // Wait for the component to finish loading
        await waitFor(() => {
            expect(screen.queryByText('Loading...')).not.toBeInTheDocument();
        }, { timeout: 10000 });

        // Check for the header text
        expect(screen.getByText('Revenue Review Threshold')).toBeInTheDocument();
    }, 15000);
    test('displays basic UI elements', async () => {
        render(<RevenueReviewThreshold />);

        // Wait for loading to complete
        await waitFor(() => {
            expect(screen.queryByText('Loading...')).not.toBeInTheDocument();
        }, { timeout: 10000 });

        // Check if the page header exists
        expect(screen.getByText('Revenue Review Threshold')).toBeInTheDocument();
        
        // Check if basic table headers exist
        expect(screen.getByText('Customer Name')).toBeInTheDocument();
        expect(screen.getByText('Site ID')).toBeInTheDocument();
        expect(screen.getByText('Deviation %')).toBeInTheDocument();
        expect(screen.getByText('Deviation $')).toBeInTheDocument();
    }, 15000);
});