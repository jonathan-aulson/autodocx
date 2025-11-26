import SidebarContainer from '@/components/Forecast/SidebarContainer';
import { Customer, TimeRangeType } from '@/lib/models/Statistics';
import '@testing-library/jest-dom';
import { act, fireEvent, render, screen } from '@testing-library/react';

// Mock the child components
jest.mock('@/components/Forecast/CustomerSiteWidget', () => {
    return function MockCustomerSiteWidget() {
        return <div data-testid="customer-site-widget">CustomerSiteWidget</div>;
    };
});

jest.mock('@/components/Forecast/ViewOptionsWidget', () => {
    return function MockViewOptionsWidget() {
        return <div data-testid="view-options-widget">ViewOptionsWidget</div>;
    };
});

// Sample customers data for testing
const mockCustomers: Customer[] = [
    {
        customerSiteId: 'site1',
        siteNumber: '12345',
        siteName: 'Test Site 1',
    },
    {
        customerSiteId: 'site2',
        siteNumber: '67890',
        siteName: 'Test Site 2',
    },
];

// Mock window resize for testing mobile responsiveness
const resizeWindow = (width: number) => {
    window.innerWidth = width;
    act(() => {
        window.dispatchEvent(new Event('resize'));
    });
};

describe('SidebarContainer', () => {
    beforeEach(() => {
        jest.clearAllMocks();
        // Reset window size to desktop
        window.innerWidth = 1024;
    });

    test('renders without crashing', () => {
        render(
            <SidebarContainer
                customers={mockCustomers}
                isLoadingCustomers={false}
                error={null}
                selectedSite="site1"
                setSelectedSite={jest.fn()}
                totalRooms={100}
                startingMonth="2023-01"
                setStartingMonth={jest.fn()}
                timePeriod={TimeRangeType.MONTHLY}
                setTimePeriod={jest.fn()}

                activeTab="forecast"
            />
        );

        expect(screen.getByText('CustomerSiteWidget')).toBeInTheDocument();
        expect(screen.getByText('ViewOptionsWidget')).toBeInTheDocument();
    });

    test('toggles sidebar when toggle button is clicked', () => {
        const onExpandedChange = jest.fn();
        
        const { container } = render(
            <SidebarContainer
                customers={mockCustomers}
                isLoadingCustomers={false}
                error={null}
                selectedSite="site1"
                setSelectedSite={jest.fn()}
                totalRooms={100}
                startingMonth="2023-01"
                setStartingMonth={jest.fn()}
                timePeriod={TimeRangeType.MONTHLY}
                setTimePeriod={jest.fn()}

                activeTab="forecast"
                onExpandedChange={onExpandedChange}
            />
        );

        // Find the toggle button and ensure it exists before clicking
        const toggleButton = container.querySelector('[data-qa-id="toggle-sidebar-button"]');
        expect(toggleButton).not.toBeNull();
        
        if (toggleButton) {
            fireEvent.click(toggleButton);
        } else {
            throw new Error('Toggle button not found in the DOM');
        }

        expect(onExpandedChange).toHaveBeenCalledWith(false);
    });

    test('renders mobile view when screen width is small', () => {
        // Set window width to mobile size
        resizeWindow(767);
        
        const { container } = render(
            <SidebarContainer
                customers={mockCustomers}
                isLoadingCustomers={false}
                error={null}
                selectedSite="site1"
                setSelectedSite={jest.fn()}
                totalRooms={100}
                startingMonth="2023-01"
                setStartingMonth={jest.fn()}
                timePeriod={TimeRangeType.MONTHLY}
                setTimePeriod={jest.fn()}

                activeTab="forecast"
            />
        );

        // In mobile view the layout should be different
        // Should show both widgets in a different layout
        expect(screen.getByText('CustomerSiteWidget')).toBeInTheDocument();
        expect(screen.getByText('ViewOptionsWidget')).toBeInTheDocument();
        
        // Check for w-full class which is only used in mobile view
        const mainDiv = container.firstChild as HTMLElement;
        expect(mainDiv).toHaveClass('w-full');
        
        // Should not have a toggle button in mobile view
        const toggleButton = container.querySelector('[data-qa-id="toggle-sidebar-button"]');
        expect(toggleButton).not.toBeInTheDocument();
    });

    test('displays collapsed view when isExpanded is false', () => {
        const { container } = render(
            <SidebarContainer
                customers={mockCustomers}
                isLoadingCustomers={false}
                error={null}
                selectedSite="site1"
                setSelectedSite={jest.fn()}
                totalRooms={100}
                startingMonth="2023-01"
                setStartingMonth={jest.fn()}
                timePeriod={TimeRangeType.MONTHLY}
                setTimePeriod={jest.fn()}

                activeTab="forecast"
                isExpanded={false}
            />
        );

        // Should show the compact view elements
        expect(screen.getByText('Site')).toBeInTheDocument();
        expect(screen.getByText('12345')).toBeInTheDocument(); // Site number
        expect(screen.getByText('100 rooms')).toBeInTheDocument();
        expect(screen.getByText('Date')).toBeInTheDocument();
        expect(screen.getByText('View')).toBeInTheDocument();
        // Comparison field has been removed, so no longer checking for 'Comp'
        
        // Should not show the expanded view components
        expect(screen.queryByText('CustomerSiteWidget')).not.toBeInTheDocument();
        expect(screen.queryByText('ViewOptionsWidget')).not.toBeInTheDocument();
    });

    test('uses internal state for expansion when isExpanded prop is not provided', () => {
        const { container } = render(
            <SidebarContainer
                customers={mockCustomers}
                isLoadingCustomers={false}
                error={null}
                selectedSite="site1"
                setSelectedSite={jest.fn()}
                totalRooms={100}
                startingMonth="2023-01"
                setStartingMonth={jest.fn()}
                timePeriod={TimeRangeType.MONTHLY}
                setTimePeriod={jest.fn()}

                activeTab="forecast"
                // isExpanded not provided, should use internal state (default to true)
            />
        );

        // Should initially be expanded
        const sidebarDiv = container.firstChild as HTMLElement;
        expect(sidebarDiv).toHaveClass('w-80');
        
        // Should show the expanded view components
        expect(screen.getByText('CustomerSiteWidget')).toBeInTheDocument();
        expect(screen.getByText('ViewOptionsWidget')).toBeInTheDocument();
        
        // Toggle the sidebar
        const toggleButton = container.querySelector('[data-qa-id="toggle-sidebar-button"]');
        expect(toggleButton).not.toBeNull();
        
        if (toggleButton) {
            fireEvent.click(toggleButton);
        } else {
            throw new Error('Toggle button not found in the DOM');
        }
        
        // After toggling, should show collapsed view
        expect(screen.getByText('Site')).toBeInTheDocument();
        expect(screen.queryByText('CustomerSiteWidget')).not.toBeInTheDocument();
        expect(screen.queryByText('ViewOptionsWidget')).not.toBeInTheDocument();
    });

    test('correctly formats month display in collapsed view', () => {
        render(
            <SidebarContainer
                customers={mockCustomers}
                isLoadingCustomers={false}
                error={null}
                selectedSite="site1"
                setSelectedSite={jest.fn()}
                totalRooms={100}
                startingMonth="2023-01"
                setStartingMonth={jest.fn()}
                timePeriod={TimeRangeType.MONTHLY}
                setTimePeriod={jest.fn()}

                activeTab="forecast"
                isExpanded={false}
            />
        );

        // Check for abbreviated month display
        expect(screen.getByText('Jan 2023')).toBeInTheDocument();
    });
});
