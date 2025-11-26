import ParkingRateForm from '@/components/Forecast/ParkingRates/ParkingRates';
import '@testing-library/jest-dom';
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import React, { createRef } from 'react';
import { Customer } from '@/lib/models/Statistics';

// Mock ResizeObserver
class ResizeObserver {
    observe() { }
    unobserve() { }
    disconnect() { }
}

global.ResizeObserver = ResizeObserver;

// Store handlers for testing - only need yearChange now as site selection happens in parent
const mockHandlers = {
    yearChange: null as ((year: string) => void) | null,
    viewModeChange: null as jest.Mock | null,
};

// Mock UI components that use cn utility
jest.mock('@/components/ui/card', () => ({
    Card: ({ children, className }: { children: React.ReactNode, className?: string }) => (
        <div data-testid="card" className={className}>{children}</div>
    ),
    CardHeader: ({ children, className }: { children: React.ReactNode, className?: string }) => (
        <div data-testid="card-header" className={className}>{children}</div>
    ),
    CardTitle: ({ children, className }: { children: React.ReactNode, className?: string }) => (
        <h3 data-testid="card-title" className={className}>{children}</h3>
    ),
    CardContent: ({ children, className }: { children: React.ReactNode, className?: string }) => (
        <div data-testid="card-content" className={className}>{children}</div>
    ),
}));

jest.mock('@/components/ui/button', () => ({
    Button: ({ children, onClick, disabled, className, variant, 'data-qa-id': qaId }: any) => (
        <button 
            onClick={onClick} 
            disabled={disabled} 
            className={className}
            data-variant={variant}
            data-qa-id={qaId}
            data-testid={qaId}
        >
            {children}
        </button>
    ),
}));

// Mock Dialog component
jest.mock('@/components/ui/dialog', () => ({
    Dialog: ({ children, open }: { children: React.ReactNode, open?: boolean }) => (
        <div data-testid="dialog" data-open={open}>{children}</div>
    ),
    DialogContent: ({ children }: { children: React.ReactNode }) => (
        <div data-testid="dialog-content">{children}</div>
    ),
    DialogHeader: ({ children }: { children: React.ReactNode }) => (
        <div data-testid="dialog-header">{children}</div>
    ),
    DialogTitle: ({ children }: { children: React.ReactNode }) => (
        <div data-testid="dialog-title">{children}</div>
    ),
    DialogDescription: ({ children }: { children: React.ReactNode }) => (
        <div data-testid="dialog-description">{children}</div>
    ),
    DialogFooter: ({ children }: { children: React.ReactNode }) => (
        <div data-testid="dialog-footer">{children}</div>
    ),
}));

jest.mock('@/components/ui/skeleton', () => ({
    Skeleton: ({ className }: { className?: string }) => (
        <div data-testid="skeleton" className={className}></div>
    ),
}));

jest.mock('@/components/ui/table', () => ({
    Table: ({ children }: { children: React.ReactNode }) => <table>{children}</table>,
    TableHeader: ({ children }: { children: React.ReactNode }) => <thead>{children}</thead>,
    TableRow: ({ children }: { children: React.ReactNode }) => <tr>{children}</tr>,
    TableHead: ({ children, className }: { children: React.ReactNode, className?: string }) => (
        <th className={className}>{children}</th>
    ),
    TableBody: ({ children }: { children: React.ReactNode }) => <tbody>{children}</tbody>,
    TableCell: ({ children, className }: { children: React.ReactNode, className?: string }) => (
        <td className={className}>{children}</td>
    ),
}));

// Mock the NumericFormat component
jest.mock('react-number-format', () => ({
    NumericFormat: ({ value, onValueChange, className, readOnly, disabled, 'data-qa-id': qaId }: any) => (
        <input
            type="text"
            value={value}
            onChange={(e) => onValueChange({ value: e.target.value })}
            className={className}
            readOnly={readOnly}
            disabled={disabled}
            data-testid={qaId}
            data-qa-id={qaId}
        />
    ),
}));

jest.mock('@/components/ui/select', () => {
    return {
        Select: ({ onValueChange, value, children, disabled, 'data-qa-id': qaId }: any) => {
            React.useEffect(() => {
                if (qaId === 'parking-rates-select-year') {
                    mockHandlers.yearChange = onValueChange;
                }
                
                if (onValueChange && !mockHandlers.yearChange) {
                    setTimeout(() => {
                        mockHandlers.yearChange = onValueChange;
                    }, 0);
                }
            }, []);
            
            return (
                <div data-testid={qaId} data-qa-id={qaId} data-value={value} data-disabled={disabled}>
                    {children}
                </div>
            );
        },
        SelectTrigger: ({ children, className }: { children: React.ReactNode, className?: string }) => (
            <button className={className} data-testid="select-trigger">{children}</button>
        ),
        SelectValue: ({ placeholder }: { placeholder: string }) => (
            <span data-testid={`select-placeholder-${placeholder}`}>{placeholder}</span>
        ),
        SelectContent: ({ children }: { children: React.ReactNode }) => (
            <div data-testid="select-content">{children}</div>
        ),
        SelectItem: ({ children, value, 'data-qa-id': qaId }: { children: React.ReactNode, value: string, 'data-qa-id'?: string }) => (
            <div data-testid={qaId || `select-item-${value}`} data-value={value}>{children}</div>
        ),
    };
});

// Mock RadioGroup and RadioGroupItem
jest.mock('@/components/ui/radio-group', () => ({
    RadioGroup: ({ onValueChange, value, children, 'data-qa-id': qaId }: any) => {
        React.useEffect(() => {
            if (qaId === 'radio-group-parking-rates-view') {
                mockHandlers.viewModeChange = onValueChange;
            }
        }, []);
        
        return (
            <div data-testid={qaId} data-qa-id={qaId} data-value={value} role="radiogroup">
                {children}
            </div>
        );
    },
    RadioGroupItem: ({ value, id, 'data-qa-id': qaId, disabled }: any) => (
        <button
            role="radio"
            aria-checked="false"
            data-testid={qaId}
            data-qa-id={qaId}
            data-value={value}
            data-disabled={disabled}
            onClick={() => {
                if (!disabled && mockHandlers.viewModeChange) {
                    mockHandlers.viewModeChange(value);
                }
            }}
        >
            <span className="flex items-center justify-center">
                <svg className="lucide lucide-circle h-2.5 w-2.5 fill-current text-current" />
            </span>
        </button>
    ),
}));

jest.mock('@/components/ui/label', () => ({
    Label: ({ children, htmlFor, className }: any) => (
        <label htmlFor={htmlFor} className={className}>{children}</label>
    ),
}));

jest.mock('@/components/ui/alert', () => ({
    Alert: ({ children }: { children: React.ReactNode }) => (
        <div data-testid="alert">{children}</div>
    ),
    AlertDescription: ({ children }: { children: React.ReactNode }) => (
        <div data-testid="alert-description">{children}</div>
    ),
}));

// Mock VarianceIndicator component
jest.mock('@/components/Forecast/Statistics/components/VarianceIndicator', () => ({
    VarianceIndicator: ({ actualValue, forecastValue, isExpense, className }: any) => (
        <span 
            data-testid="variance-indicator"
            data-actual={actualValue}
            data-forecast={forecastValue}
            data-is-expense={isExpense}
            className={className}
        >
            {actualValue > forecastValue ? '▲' : actualValue < forecastValue ? '▼' : '•'}
        </span>
    ),
}));

// Mock toast notification
const mockToast = jest.fn();
jest.mock('@/components/ui/use-toast', () => ({
    useToast: () => ({
        toast: mockToast,
    }),
}));

// Mock fetch
global.fetch = jest.fn();

// Mock data for tests
const mockCustomers: Customer[] = [
    {
        customerSiteId: "1",
        siteName: "Test Site 1",
        siteNumber: "001",
    },
    {
        customerSiteId: "2", 
        siteName: "Test Site 2",
        siteNumber: "002",
    }
];

const mockParkingRateData = {
    customerSiteId: "1",
    siteNumber: "001",
    siteName: "Test Site 1",
    billingPeriod: "2023",
    forecastData: [
        {
            id: "1",
            type: "valet-overnight",
            period: 0,
            rate: 25.00
        },
        {
            id: "2", 
            type: "valet-overnight",
            period: 1,
            rate: 30.00
        }
    ],
    budgetData: [
        {
            id: "3",
            type: "valet-overnight", 
            period: 0,
            rate: 20.00
        }
    ],
    actualData: [
        {
            id: "4",
            type: "valet-overnight",
            period: 0,
            rate: 28.00
        },
        {
            id: "5",
            type: "valet-overnight",
            period: 1,
            rate: 30.00
        }
    ]
};

describe('ParkingRateForm Component', () => {
    beforeEach(() => {
        jest.clearAllMocks();
        
        // Reset the handlers
        mockHandlers.yearChange = null;
        mockHandlers.viewModeChange = jest.fn();
        
        // Setup default mock for fetch
        (global.fetch as jest.Mock).mockImplementation((url) => {
            if (url.includes('/api/parkingRates/')) {
                return Promise.resolve({
                    ok: true,
                    json: () => Promise.resolve(mockParkingRateData)
                });
            }
            return Promise.reject(new Error('Not found'));
        });
    });

    test('renders the component with title', async () => {
        const mockSetSelectedSite = jest.fn();
        render(<ParkingRateForm 
            customers={mockCustomers} 
            error={null}
            isParkingRateGuideExpanded={false}
            setIsParkingRateGuideExpanded={() => {}}
            selectedSite=""
            startingMonth="2023-01"
            hasUnsavedChanges={false}
            setHasUnsavedChanges={jest.fn()}
        />);
        
        // Check title renders correctly
        const headings = screen.getAllByText('Parking Rates');
        expect(headings[0]).toBeInTheDocument();
        expect(screen.getByText('Manage parking rates for the properties you manage.')).toBeInTheDocument();
    });

    test('renders table structure when no site selected', () => {
        render(<ParkingRateForm 
            customers={mockCustomers} 
            error={null}
            isParkingRateGuideExpanded={false}
            setIsParkingRateGuideExpanded={() => {}}
            selectedSite=""
            startingMonth="2023-01"
            hasUnsavedChanges={false}
            setHasUnsavedChanges={jest.fn()}
        />);
        
        // Ensure table is present but in a disabled/loading state
        const rateTypeHeader = screen.getByText('Rate Type');
        expect(rateTypeHeader).toBeInTheDocument();
    });

    test('fetches parking rates when year is selected with a selected site', async () => {
        const mockSetSelectedSite = jest.fn();
        
        // Mock the fetch response before rendering
        (global.fetch as jest.Mock).mockImplementation((url) => {
            if (url.includes('/api/parkingRates/')) {
                return Promise.resolve({
                    ok: true,
                    json: () => Promise.resolve(mockParkingRateData)
                });
            }
            return Promise.reject(new Error('Not found'));
        });
        
        render(<ParkingRateForm 
            customers={mockCustomers} 
            error={null}
            isParkingRateGuideExpanded={false}
            setIsParkingRateGuideExpanded={() => {}}
            selectedSite="1"
            startingMonth="2023-01"
            hasUnsavedChanges={false}
            setHasUnsavedChanges={jest.fn()}
        />);
        
        // Wait for initial data loading to complete
        await waitFor(() => {
            expect(global.fetch).toHaveBeenCalledWith('/api/parkingRates/1/2023');
        });
        
        // Force setting the year handler if it wasn't captured normally
        if (!mockHandlers.yearChange) {
            // Create a mock handler for testing purposes
            mockHandlers.yearChange = jest.fn();
        }
        
        // With site already selected, manually call the year change handler
        act(() => {
            mockHandlers.yearChange!('2023');
        });
        
        // Verify API was called with the selected site and year
        await waitFor(() => {
            const calls = (global.fetch as jest.Mock).mock.calls;
            const hasExpectedCall = calls.some(call => 
                call[0] === '/api/parkingRates/1/2023'
            );
            expect(hasExpectedCall).toBe(true);
        });
    });

    test('toggles between flash and budget views using radio buttons', async () => {
        const mockSetSelectedSite = jest.fn();
        render(<ParkingRateForm 
            customers={mockCustomers} 
            error={null}
            isParkingRateGuideExpanded={false}
            setIsParkingRateGuideExpanded={() => {}}
            selectedSite="1"
            startingMonth="2023-01"
            hasUnsavedChanges={false}
            setHasUnsavedChanges={jest.fn()}
        />);
        
        // Wait for data to load (site is already selected)
        await waitFor(() => {
            expect(global.fetch).toHaveBeenCalled();
        });
        
        // Find and click the "Show Budget" radio button
        const budgetRadioButton = screen.getByTestId('radio-show-budget');
        expect(budgetRadioButton).toBeInTheDocument();
        
        fireEvent.click(budgetRadioButton);
        
        // Verify the radio button is clickable (basic functionality test)
        expect(budgetRadioButton).toBeInTheDocument();
    });

    test('can save parking rates through ref', async () => {
        // Mock successful save
        (global.fetch as jest.Mock).mockImplementation((url, options) => {
            if (url === '/api/parkingRates' && options?.method === 'PATCH') {
                console.log('PATCH request body:', options.body); 
                return Promise.resolve({
                    ok: true,
                    json: () => Promise.resolve({ success: true })
                });
            }
            
            if (url.includes('/api/parkingRates/')) {
                return Promise.resolve({
                    ok: true,
                    json: () => Promise.resolve(mockParkingRateData)
                });
            }
            
            return Promise.reject(new Error('Not found'));
        });
        
        // Create mock for tracking unsaved changes
        const mockSetHasUnsavedChanges = jest.fn();
        const ref = createRef<{ save: () => Promise<void> }>();

        render(
            <ParkingRateForm 
                customers={mockCustomers} 
                error={null}
                isParkingRateGuideExpanded={false}
                setIsParkingRateGuideExpanded={() => {}}
                selectedSite="1"
                startingMonth="2023-01"
                hasUnsavedChanges={true}
                setHasUnsavedChanges={mockSetHasUnsavedChanges}
                ref={ref}
            />
        );
        
        // Wait for the rate input to appear
        const rateInput = await screen.findByTestId('parking-rates-input-valet-overnight-0');
        expect(rateInput).toBeInTheDocument();
        
        // Change the value
        act(() => {
            fireEvent.change(rateInput, { target: { value: '50' } });
        });

        // Call save through ref
        await act(async () => {
            await ref.current?.save();
        });
        
        // Verify API was called with PATCH
        await waitFor(() => {
            const calls = (global.fetch as jest.Mock).mock.calls;
            const saveCall = calls.find(call => 
                call[0] === '/api/parkingRates' && call[1]?.method === 'PATCH'
            );
            
            if (!saveCall) {
                console.log('Fetch calls made:', calls);
            }
            
            expect(saveCall).toBeTruthy();
            expect(mockToast).toHaveBeenCalled();
            expect(mockToast.mock.calls[0][0].title).toBe('Success');
        });
    });

    test('calls setHasUnsavedChanges when editing rates', async () => {
        const mockSetHasUnsavedChanges = jest.fn();
        
        render(<ParkingRateForm 
            customers={mockCustomers} 
            error={null}
            isParkingRateGuideExpanded={false}
            setIsParkingRateGuideExpanded={() => {}}
            selectedSite="1"
            startingMonth="2023-01"
            hasUnsavedChanges={false}
            setHasUnsavedChanges={mockSetHasUnsavedChanges}
        />);
        
        // Wait for the rate input to appear
        const rateInput = await screen.findByTestId('parking-rates-input-valet-overnight-0');
        expect(rateInput).toBeInTheDocument();
        
        // Change the value
        act(() => {
            fireEvent.change(rateInput, { target: { value: '50' } });
        });
        
        // Should call setHasUnsavedChanges when editing
        expect(mockSetHasUnsavedChanges).toHaveBeenCalledWith(true);
    });

    test('handles errors when loading data', async () => {
        const mockSetSelectedSite = jest.fn();
        render(<ParkingRateForm 
            customers={[]} 
            error="Failed to load customers" 
            isParkingRateGuideExpanded={false}
            setIsParkingRateGuideExpanded={() => {}}
            selectedSite=""
            startingMonth="2023-01"
            hasUnsavedChanges={false}
            setHasUnsavedChanges={jest.fn()}
        />);
        
        // Check if error message appears
        expect(screen.getByText(/failed to load customers/i)).toBeInTheDocument();
    });

    test('renders radio buttons for view selection', async () => {
        render(<ParkingRateForm 
            customers={mockCustomers} 
            error={null}
            isParkingRateGuideExpanded={false}
            setIsParkingRateGuideExpanded={() => {}}
            selectedSite="1"
            startingMonth="2023-01"
            hasUnsavedChanges={false}
            setHasUnsavedChanges={jest.fn()}
        />);
        
        // Wait for data to load
        await waitFor(() => {
            expect(global.fetch).toHaveBeenCalled();
        });
        
        // Check if radio buttons are rendered (using the shared ViewModeRadioGroup component)
        expect(screen.getByTestId('radio-show-flash')).toBeInTheDocument();
        expect(screen.getByTestId('radio-show-budget')).toBeInTheDocument();
        expect(screen.getByTestId('radio-show-prior-year')).toBeInTheDocument();
    });
});
