import ContractDetailsTab from '@/components/TabsCustomersDetails/ContractDetailsTab';
import { AdvancedArrearsType, LineTitles, PaymentTermsType, RevenueAccumulation, SupportingReportsType } from '@/lib/models/Contract';
import '@testing-library/jest-dom';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';

class ResizeObserver {
    observe() { }
    unobserve() { }
    disconnect() { }
}

global.ResizeObserver = ResizeObserver;

const mockToast = jest.fn();
jest.mock('@/components/ui/use-toast', () => ({
    useToast: () => ({
        toast: mockToast,
    }),
}));

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

const contractDetails = {
    id: '1',
    purchaseOrder: 'PO EXAMPLE',
    paymentTerms: PaymentTermsType.PAYMENT_TERM_FIRST_OF_MONTH,
    billingType: AdvancedArrearsType.ADVANCED,
    incrementMonth: 'April',
    incrementAmount: 10.25,
    deviationPercentage: 5,
    deviationAmount: 100,
    consumerPriceIndex: false,
    notes: 'Sample notes here',
    contractType: 'Test Contract',
    deposits: true,
    fixedFee: {
        enabled: false,
        serviceRates: [
            {
                name: 'Bell',
                displayName: 'Bell',
                code: '4725',
                fee: 1000,
                invoiceGroup: '1',
            }
        ],
    },
    perLaborHour: {
        enabled: false,
        hoursBackupReport: true,
        jobRates: [
            {
                name: 'Account Manager',
                displayName: 'Account Manager',
                jobCode: 'ACCT-MGR',
                rate: 20,
                overtimeRate: 40,
                invoiceGroup: '1',
            }
        ],
    },
    perOccupiedRoom: {
        enabled: false,
        roomRate: 0,
        invoiceGroup: '1',
    },
    invoiceGrouping: {
        enabled: false,
        invoiceGroups: []
    },
    revenueShare: {
        enabled: false,
        sharePercentage: "",
        createThresholdStructures:
            false,
        revenueAccumulation:
        RevenueAccumulation.ANNUALLY_ANIVERSARY,
        thresholdStructures: []
    },
    bellServiceFee: {
        enabled: false,
        bellServices: []
    },
    midMonthAdvance: {
        enabled: true,
        midMonthAdvances: [{
            id: '1',
            amount: 100,
            lineTitle: LineTitles.LESS_MID_MONTH_BILLING,
            invoiceGroup: 2
        }]
    },
    depositedRevenue: {
        enabled: true,
        depositData: [{
            id: '1',
            towneParkResponsibleForParkingTax: true,
            depositedRevenueEnabled: false,
            invoiceGroup: 2,
        }]
    },
    billableAccounts: {
        enabled: false,
        billableAccountsData: []
    },
    managementAgreement: {
        enabled: false,
        ManagementFees: []
    },
    supportingReports: [SupportingReportsType.MIX_OF_SALES],
};

const customerDetail = {
    siteName: 'Test Customer',
    glString: '123',
    district: 'Test District',
    address: '1234 Main St, Springfield, IL 62701',
    accountManager: 'Test Manager',
    accountManagerId: '123',
    siteNumber: '12345',
    invoiceRecipient: 'John Doe',
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

const onUpdateContractDetails = jest.fn();

// Mock fetch response helper
function createFetchResponse(data: any) {
    return Promise.resolve({
        ...data,
        json: () => Promise.resolve(data)
    }) as Promise<Response>;
}

describe('ContractDetailsTab', () => {
    beforeEach(() => {
        jest.spyOn(require('@/contexts/AuthContext'), 'useAuth')
            .mockImplementation(() => ({ userRoles: ['billingAdmin'] }));
    });

    test('renders without crashing', () => {
        render(<ContractDetailsTab contractDetails={contractDetails} customerDetail={customerDetail} onUpdateContractDetails={onUpdateContractDetails} />);
        expect(screen.getByText('General Setup')).toBeInTheDocument();
    });

    test('displays form fields with correct default values', () => {
        render(<ContractDetailsTab contractDetails={contractDetails} customerDetail={customerDetail} onUpdateContractDetails={onUpdateContractDetails} />);

        expect(screen.getByDisplayValue(contractDetails.purchaseOrder)).toBeInTheDocument();
        expect(screen.getByDisplayValue(contractDetails.notes)).toBeInTheDocument();
    });

    test('allows the user to enter data in the form fields', () => {
        render(<ContractDetailsTab contractDetails={contractDetails} customerDetail={customerDetail} onUpdateContractDetails={onUpdateContractDetails} />);

        const purchaseOrderInput = screen.getByPlaceholderText('Enter PO number');
        const notesTextarea = screen.getByPlaceholderText('Enter notes');
        const incrementPercentageInput = screen.getByPlaceholderText('Enter increment percentage');

        fireEvent.change(purchaseOrderInput, { target: { value: 'New PO' } });
        fireEvent.change(notesTextarea, { target: { value: 'New notes here' } });
        fireEvent.change(incrementPercentageInput, { target: { value: '12.5' } });

        expect(purchaseOrderInput).toHaveValue('New PO');
        expect(notesTextarea).toHaveValue('New notes here');
        expect(incrementPercentageInput).toHaveValue("12.5%");
    });

    test('creates two default invoice groups when enabling multiple invoices with no pre-existing groups', async () => {
        render(<ContractDetailsTab 
            contractDetails={{ ...contractDetails, invoiceGrouping: { enabled: false, invoiceGroups: [] } }} 
            customerDetail={customerDetail} 
            onUpdateContractDetails={onUpdateContractDetails} 
        />);
    
        await waitFor(() => {
            expect(screen.getByTestId('edit-button')).toBeInTheDocument();
        });
    
        const editButton = screen.getByTestId('edit-button');
        fireEvent.click(editButton);
    
        const accordionTrigger = screen.getByText(/multiple invoices/i);
        fireEvent.click(accordionTrigger);
    
        const switchButton = screen.getByLabelText(/generate multiple invoices/i);
        fireEvent.click(switchButton);
    
        const invoiceInputs = [
            screen.getByTestId('invoice-group-title-0'),
            screen.getByTestId('invoice-group-title-1')
        ];
        expect(invoiceInputs).toHaveLength(2);
    });
});
