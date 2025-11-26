import ViewOptionsWidget from '@/components/Forecast/ViewOptionsWidget';
import { TimeRangeType } from '@/lib/models/Statistics';
import '@testing-library/jest-dom';
import { render, screen, waitFor } from '@testing-library/react';
import React from 'react';
import { MemoryRouter } from 'react-router-dom';

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

// Store handlers for testing
const mockHandlers = {
    yearChange: null as ((value: string) => void) | null,
    monthChange: null as ((value: string) => void) | null,
    timePeriodChange: null as ((value: string) => void) | null,

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

jest.mock('@/components/ui/label', () => ({
    Label: ({ children, htmlFor, className }: any) => (
        <label htmlFor={htmlFor} className={className}>{children}</label>
    ),
}));

// Mock the Select component
jest.mock('@/components/ui/select', () => {
    return {
        Select: ({ onValueChange, value, children, disabled, 'data-qa-id': qaId }: any) => {
            // Store handlers based on qa-id
            React.useEffect(() => {
                if (qaId === 'select-starting-year') {
                    mockHandlers.yearChange = onValueChange;
                } else if (qaId === 'select-starting-month') {
                    mockHandlers.monthChange = onValueChange;
                } else if (qaId === 'select-time-period') {
                    mockHandlers.timePeriodChange = onValueChange;
                } else if (qaId === 'select-comparison') {
                    // Comparison dropdown removed
                }
            }, []);
            
            return (
                <div data-testid={qaId} data-qa-id={qaId} data-value={value} data-disabled={disabled}>
                    {children}
                </div>
            );
        },
        SelectTrigger: ({ children, className, 'data-qa-id': qaId }: { children: React.ReactNode, className?: string, 'data-qa-id'?: string }) => (
            <button className={className} data-testid={qaId} data-qa-id={qaId}>{children}</button>
        ),
        SelectValue: ({ placeholder }: { placeholder: string }) => (
            <span data-testid={`select-placeholder-${placeholder}`}>{placeholder}</span>
        ),
        SelectContent: ({ children }: { children: React.ReactNode }) => (
            <div data-testid="select-content">{children}</div>
        ),
        SelectItem: ({ children, value, 'data-qa-id': qaId }: { children: React.ReactNode, value: string, 'data-qa-id'?: string }) => (
            <div data-testid={qaId || `select-item-${value}`} data-value={value} data-qa-id={qaId}>{children}</div>
        ),
    };
});

describe('ViewOptionsWidget Component', () => {
    beforeEach(() => {
        jest.clearAllMocks();
        
        // Reset handlers
        Object.keys(mockHandlers).forEach(key => {
            mockHandlers[key as keyof typeof mockHandlers] = null;
        });
    });

    test('renders the component with title', () => {
        const mockSetStartingMonth = jest.fn();
        const mockSetTimePeriod = jest.fn();
        const mockSetComparison = jest.fn();
        
        render(
            <MemoryRouter>
                <ViewOptionsWidget 
                    startingMonth="2023-01"
                    setStartingMonth={mockSetStartingMonth}
                    timePeriod={TimeRangeType.DAILY}
                    setTimePeriod={mockSetTimePeriod}
                    activeTab="statistics"
                />
            </MemoryRouter>
        );
        
        // Check title renders correctly
        expect(screen.getByText('View Options')).toBeInTheDocument();
    });

    test('renders year selection dropdown', () => {
        const mockSetStartingMonth = jest.fn();
        const mockSetTimePeriod = jest.fn();
        const mockSetComparison = jest.fn();
        
        render(
            <MemoryRouter>
                <ViewOptionsWidget 
                    startingMonth="2023-01"
                    setStartingMonth={mockSetStartingMonth}
                    timePeriod={TimeRangeType.DAILY}
                    setTimePeriod={mockSetTimePeriod}
                    activeTab="statistics"
                />
            </MemoryRouter>
        );
        
        // Check year selection elements
        expect(screen.getByText('Starting Year')).toBeInTheDocument();
        expect(screen.getByTestId('select-starting-year')).toBeInTheDocument();
        expect(screen.getByTestId('trigger-starting-year')).toBeInTheDocument();
    });

    test('renders month selection dropdown when activeTab is statistics', () => {
        const mockSetStartingMonth = jest.fn();
        const mockSetTimePeriod = jest.fn();
        const mockSetComparison = jest.fn();
        
        render(
            <MemoryRouter>
                <ViewOptionsWidget 
                    startingMonth="2023-01"
                    setStartingMonth={mockSetStartingMonth}
                    timePeriod={TimeRangeType.DAILY}
                    setTimePeriod={mockSetTimePeriod}
                    activeTab="statistics"
                />
            </MemoryRouter>
        );
        
        // Check month selection elements
        expect(screen.getByText('Starting Month')).toBeInTheDocument();
        expect(screen.getByTestId('select-starting-month')).toBeInTheDocument();
    });

    test('does not render month selection dropdown when activeTab is not statistics or payroll', () => {
        const mockSetStartingMonth = jest.fn();
        const mockSetTimePeriod = jest.fn();
        const mockSetComparison = jest.fn();
        
        render(
            <MemoryRouter>
                <ViewOptionsWidget 
                    startingMonth="2023-01"
                    setStartingMonth={mockSetStartingMonth}
                    timePeriod={TimeRangeType.DAILY}
                    setTimePeriod={mockSetTimePeriod}
                    activeTab="parking-rates"
                />
            </MemoryRouter>
        );

        expect(screen.queryByTestId('select-starting-month')).not.toBeInTheDocument();
    });

    test('renders time period selection dropdown', () => {
        const mockSetStartingMonth = jest.fn();
        const mockSetTimePeriod = jest.fn();
        const mockSetComparison = jest.fn();
        
        render(
            <MemoryRouter>
                <ViewOptionsWidget 
                    startingMonth="2023-01"
                    setStartingMonth={mockSetStartingMonth}
                    timePeriod={TimeRangeType.DAILY}
                    setTimePeriod={mockSetTimePeriod}
                    activeTab="statistics"
                />
            </MemoryRouter>
        );
        
        // Check time period selection elements
        expect(screen.getByText('Time Period')).toBeInTheDocument();
        expect(screen.getByTestId('select-time-period')).toBeInTheDocument();
    });

    test('renders without comparison dropdown', () => {
        const mockSetStartingMonth = jest.fn();
        const mockSetTimePeriod = jest.fn();
        const mockSetComparison = jest.fn();
        
        render(
            <MemoryRouter>
                <ViewOptionsWidget 
                    startingMonth="2023-01"
                    setStartingMonth={mockSetStartingMonth}
                    timePeriod={TimeRangeType.DAILY}
                    setTimePeriod={mockSetTimePeriod}
                    activeTab="statistics"
                />
            </MemoryRouter>
        );
        
        // Verify comparison dropdown has been removed
        expect(screen.queryByTestId('select-comparison')).not.toBeInTheDocument();
        expect(screen.queryByText('Comparison')).not.toBeInTheDocument();
    });

    test('calls setStartingMonth when year is changed', async () => {
        const mockSetStartingMonth = jest.fn();
        const mockSetTimePeriod = jest.fn();
        const mockSetComparison = jest.fn();
        
        render(
            <MemoryRouter>
                <ViewOptionsWidget 
                    startingMonth="2023-01"
                    setStartingMonth={mockSetStartingMonth}
                    timePeriod={TimeRangeType.DAILY}
                    setTimePeriod={mockSetTimePeriod}
                    activeTab="statistics"
                />
            </MemoryRouter>
        );
        
        // Trigger year change
        await waitFor(() => {
            expect(mockHandlers.yearChange).not.toBeNull();
        });
        
        if (mockHandlers.yearChange) {
            mockHandlers.yearChange('2022');
            expect(mockSetStartingMonth).toHaveBeenCalledWith('2022-01');
        }
    });

    test('calls setStartingMonth when month is changed', async () => {
        const mockSetStartingMonth = jest.fn();
        const mockSetTimePeriod = jest.fn();
        const mockSetComparison = jest.fn();
        
        render(
            <MemoryRouter>
                <ViewOptionsWidget 
                    startingMonth="2023-01"
                    setStartingMonth={mockSetStartingMonth}
                    timePeriod={TimeRangeType.DAILY}
                    setTimePeriod={mockSetTimePeriod}
                    activeTab="statistics"
                />
            </MemoryRouter>
        );
        
        // Trigger month change
        await waitFor(() => {
            expect(mockHandlers.monthChange).not.toBeNull();
        });
        
        if (mockHandlers.monthChange) {
            mockHandlers.monthChange('02');
            expect(mockSetStartingMonth).toHaveBeenCalledWith('2023-02');
        }
    });

    test('calls setTimePeriod when time period is changed', async () => {
        const mockSetStartingMonth = jest.fn();
        const mockSetTimePeriod = jest.fn();
        const mockSetComparison = jest.fn();
        
        render(
            <MemoryRouter>
                <ViewOptionsWidget 
                    startingMonth="2023-01"
                    setStartingMonth={mockSetStartingMonth}
                    timePeriod={TimeRangeType.DAILY}
                    setTimePeriod={mockSetTimePeriod}
                    activeTab="statistics"
                />
            </MemoryRouter>
        );
        
        // Trigger time period change
        await waitFor(() => {
            expect(mockHandlers.timePeriodChange).not.toBeNull();
        });
        
        if (mockHandlers.timePeriodChange) {
            mockHandlers.timePeriodChange('Weekly View');
            expect(mockSetTimePeriod).toHaveBeenCalledWith('Weekly View');
        }
    });
});
