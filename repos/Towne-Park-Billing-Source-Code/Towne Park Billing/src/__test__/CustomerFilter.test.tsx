import { CustomerFilter, SelectedFilters } from '@/components/CustomerFilter/CustomerFilter';
import { CustomerSummary } from '@/lib/models/GeneralInfo';
import '@testing-library/jest-dom';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';

// Mock the CustomerContext
jest.mock('@/contexts/CustomerContext', () => ({
    useCustomer: jest.fn().mockReturnValue({
        selectedCustomer: null,
        setSelectedCustomerById: jest.fn(),
        customers: [],
        customerSummaries: [],
        isLoading: false,
        error: null,
        fetchCustomers: jest.fn(),
        fetchCustomerSummaries: jest.fn(),
        setSelectedCustomer: jest.fn()
    })
}));

// Mock ResizeObserver
class ResizeObserverMock {
  observe() {}
  unobserve() {}
  disconnect() {}
}

global.ResizeObserver = ResizeObserverMock;

// Mock scrollIntoView which is missing in jsdom
if (typeof window !== 'undefined') {
  window.HTMLElement.prototype.scrollIntoView = jest.fn();
  // For any element that might be found with getRootNode()
  document.getRootNode = jest.fn().mockImplementation(function(this: Document) {
    return this;
  });
}

// Mock data
const mockCustomers: CustomerSummary[] = [
  {
    customerSiteId: '1001',
    siteNumber: '1001',
    siteName: 'Luxury Hotel Downtown',
    legalEntity: 'Towne Park LLC',
    svpRegion: 'East',
    district: 'Southeast',
    accountManager: 'John Doe',
    districtManager: 'Jane Smith',
    plCategory: 'Luxury',
    cogSegment: 'Hotel',
    businessSegment: 'Hospitality',
    contractType: 'Management',
    billingType: 'Standard',
    deposits: false,
    readyForInvoiceStatus: 'Ready',
    period: null,
    isStatementGenerated: false
  },
  {
    customerSiteId: '1002',
    siteNumber: '1002',
    siteName: 'Airport Plaza',
    legalEntity: 'Towne Park LLC',
    svpRegion: 'West',
    district: 'Northwest',
    accountManager: 'Alice Johnson',
    districtManager: 'Bob Brown',
    plCategory: 'Airport',
    cogSegment: 'Transportation',
    businessSegment: 'Travel',
    contractType: 'Lease',
    billingType: 'Standard',
    deposits: false,
    readyForInvoiceStatus: 'Ready',
    period: null,
    isStatementGenerated: false
  },
  {
    customerSiteId: '1003',
    siteNumber: '1003',
    siteName: 'Medical Center',
    legalEntity: 'TP Services Inc',
    svpRegion: 'Central',
    district: 'Midwest',
    accountManager: 'Carol White',
    districtManager: 'Dave Green',
    plCategory: 'Healthcare',
    cogSegment: 'Medical',
    businessSegment: 'Healthcare',
    contractType: 'Management',
    billingType: 'Standard',
    deposits: false,
    readyForInvoiceStatus: 'Pending',
    period: null,
    isStatementGenerated: false
  }
];

// Add this helper function at the top of the file after imports
const getFilterBadgeByText = (container: HTMLElement, text: string): HTMLElement | null => {
  // Look for elements containing both texts (the filter label and value)
  const elements = Array.from(container.querySelectorAll('.flex.items-center.gap-1'));
  return elements.find(el => el.textContent?.includes(text)) as HTMLElement || null;
};

describe('CustomerFilter Component', () => {
  const onOpenChangeMock = jest.fn();
  const onApplyFiltersMock = jest.fn();
  
  beforeEach(() => {
    jest.clearAllMocks();
  });
  
  it('renders correctly when closed', () => {
    render(
      <CustomerFilter 
        open={false}
        onOpenChange={onOpenChangeMock}
        onApplyFilters={onApplyFiltersMock}
        customers={mockCustomers}
      />
    );
    
    // Dialog should not be visible when closed
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });
  
  it('renders correctly when open', () => {
    render(
      <CustomerFilter 
        open={true}
        onOpenChange={onOpenChangeMock}
        onApplyFilters={onApplyFiltersMock}
        customers={mockCustomers}
      />
    );
    
    // Check dialog is visible
    expect(screen.getByRole('dialog')).toBeInTheDocument();
    expect(screen.getByText('Filters')).toBeInTheDocument();
    expect(screen.getByText('Select filters to narrow down your data view')).toBeInTheDocument();
    
    // Check filter categories are present - use a more specific query
    const accordionSection = screen.getAllByText('Organizational Filters')[0];
    expect(accordionSection).toBeInTheDocument();
    expect(screen.getAllByText('Customer Filters')[0]).toBeInTheDocument();
  });
  
  it('displays filter accordion items', async () => {
    render(
      <CustomerFilter 
        open={true}
        onOpenChange={onOpenChangeMock}
        onApplyFilters={onApplyFiltersMock}
        customers={mockCustomers}
      />
    );
    
    // Check organizational filter levels
    expect(screen.getByText('Legal Entity')).toBeInTheDocument();
    expect(screen.getByText('Region')).toBeInTheDocument();
    expect(screen.getByText('District')).toBeInTheDocument();
    expect(screen.getByText('Site')).toBeInTheDocument();
    expect(screen.getByText('AM / DM')).toBeInTheDocument();
    
    // Check customer filter levels
    expect(screen.getByText('P&L Category')).toBeInTheDocument();
    expect(screen.getByText('COG')).toBeInTheDocument();
    expect(screen.getByText('Business Segment')).toBeInTheDocument();
    expect(screen.getByText('Contract Type')).toBeInTheDocument();
  });
  
  it('opens accordion item and displays filter options', async () => {
    render(
      <CustomerFilter 
        open={true}
        onOpenChange={onOpenChangeMock}
        onApplyFilters={onApplyFiltersMock}
        customers={mockCustomers}
      />
    );
    
    // Click on Legal Entity accordion
    const legalEntityTrigger = screen.getByText('Legal Entity');
    fireEvent.click(legalEntityTrigger);
    
    // Expect to see the filter options
    await waitFor(() => {
      expect(screen.getByPlaceholderText('Search Legal Entity...')).toBeInTheDocument();
    });
    
    // Should show legal entity options
    expect(screen.getByText('Towne Park LLC')).toBeInTheDocument();
    expect(screen.getByText('TP Services Inc')).toBeInTheDocument();
  });
  
  it('applies filters when clicking Apply Filters button', async () => {
    render(
      <CustomerFilter 
        open={true}
        onOpenChange={onOpenChangeMock}
        onApplyFilters={onApplyFiltersMock}
        customers={mockCustomers}
      />
    );
    
    // Open Region accordion
    const regionTrigger = screen.getByText('Region');
    fireEvent.click(regionTrigger);
    
    // Select East region
    await waitFor(() => {
      const eastOption = screen.getByText('East');
      fireEvent.click(eastOption);
    });
    
    // Apply filters
    const applyButton = screen.getByRole('button', { name: /apply filters/i });
    fireEvent.click(applyButton);
    
    // Check if onApplyFilters and onOpenChange were called with correct arguments
    expect(onApplyFiltersMock).toHaveBeenCalledWith(expect.objectContaining({
      region: ['East']
    }));
    expect(onOpenChangeMock).toHaveBeenCalledWith(false);
  });
  
  it('clears all filters when clicking Clear Filters button', async () => {
    render(
      <CustomerFilter 
        open={true}
        onOpenChange={onOpenChangeMock}
        onApplyFilters={onApplyFiltersMock}
        customers={mockCustomers}
      />
    );
    
    // Open Region accordion and select a region
    const regionTrigger = screen.getByText('Region');
    fireEvent.click(regionTrigger);
    
    await waitFor(() => {
      const eastOption = screen.getByText('East');
      fireEvent.click(eastOption);
    });
    
    // Open Contract Type accordion and select a contract type
    const contractTypeTrigger = screen.getByText('Contract Type');
    fireEvent.click(contractTypeTrigger);
    
    await waitFor(() => {
      const managementOption = screen.getByText('Management');
      fireEvent.click(managementOption);
    });
    
    // Verify both filters are selected
    expect(screen.getByText('Region:')).toBeInTheDocument();
    expect(screen.getByText('Contract Type:')).toBeInTheDocument();
    
    // Clear filters
    const clearButton = screen.getByRole('button', { name: /clear filters/i });
    fireEvent.click(clearButton);
    
    // Verify filters are cleared
    expect(screen.queryByText('Region:')).not.toBeInTheDocument();
    expect(screen.queryByText('Contract Type:')).not.toBeInTheDocument();
    expect(screen.getByText('No filters selected')).toBeInTheDocument();
  });
  
  it('shows filtered site IDs based on selection', async () => {
    render(
      <CustomerFilter 
        open={true}
        onOpenChange={onOpenChangeMock}
        onApplyFilters={onApplyFiltersMock}
        customers={mockCustomers}
      />
    );
    
    // Initially should show all sites
    expect(screen.getByText('1001')).toBeInTheDocument();
    expect(screen.getByText('1002')).toBeInTheDocument();
    expect(screen.getByText('1003')).toBeInTheDocument();
    
    // Filter by region "East"
    const regionTrigger = screen.getByText('Region');
    fireEvent.click(regionTrigger);
    
    await waitFor(() => {
      const eastOption = screen.getByText('East');
      fireEvent.click(eastOption);
    });
    
    // Now only site 1001 should be visible in site list
    await waitFor(() => {
      expect(screen.getByText('1001')).toBeInTheDocument();
      expect(screen.queryByText('1002')).not.toBeInTheDocument();
      expect(screen.queryByText('1003')).not.toBeInTheDocument();
    });
  });
  
  it('loads existing filters when provided', () => {
    const currentFilters: SelectedFilters = {
      legalEntity: ['Towne Park LLC'],
      contractType: ['Management']
    };
    
    render(
      <CustomerFilter 
        open={true}
        onOpenChange={onOpenChangeMock}
        onApplyFilters={onApplyFiltersMock}
        customers={mockCustomers}
        currentFilters={currentFilters}
      />
    );
    
    // Check if current filters are displayed
    expect(screen.getByText('Legal Entity:')).toBeInTheDocument();
    expect(screen.getByText('Towne Park LLC')).toBeInTheDocument();
    expect(screen.getByText('Contract Type:')).toBeInTheDocument();
    expect(screen.getByText('Management')).toBeInTheDocument();
  });
});
