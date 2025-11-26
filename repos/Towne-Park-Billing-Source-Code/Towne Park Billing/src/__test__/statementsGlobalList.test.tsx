import { CustomerProvider } from '@/pages/customersDetails/CustomersDetailContext';
import '@testing-library/jest-dom';
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import fetchMock from 'jest-fetch-mock';
import StatementsGlobalList from '../pages/statements/StatementsGlobalList';


const mockStatements = [
    {
        id: "1",
        createdMonth: "August",
        servicePeriod: "August 1 - August 31, 2024",
        totalAmount: 3360.00,
        forecastData: "{\n  \"forecastedRevenue\": 15000,\n  \"postedRevenue\": 12000,\n  \"invoicedRevenue\": 13000,\n  \"totalActualRevenue\": 25000,\n  \"forecastDeviationPercentage\": 66.6,\n  \"forecastDeviationAmount\": 10000,\n  \"forecastLastUpdated\": \"10/01/2024\"\n}",
        status: "Sent",
        siteNumber: "0543",
        siteName: "Margaret R. Pardee Memorial Hospital"
    },
    {
        id: "2",
        createdMonth: "July",
        servicePeriod: "July 1 - July 31, 2024",
        totalAmount: 60032.00,
        forecastData: "{\n  \"forecastedRevenue\": 15000,\n  \"postedRevenue\": 12000,\n  \"invoicedRevenue\": 13000,\n  \"totalActualRevenue\": 25000,\n  \"forecastDeviationPercentage\": 66.6,\n  \"forecastDeviationAmount\": 10000,\n  \"forecastLastUpdated\": \"10/01/2024\"\n}",
        status: "Approved",
        siteNumber: "0543",
        siteName: "Margaret R. Pardee Memorial Hospital"
    }
];

beforeEach(() => {
    fetchMock.resetMocks();
    fetchMock.mockResponse(JSON.stringify(mockStatements));
});

test('renders the table with headers', async () => {
    render(
        <CustomerProvider>
            <StatementsGlobalList />
        </CustomerProvider>);

    await waitFor(() => screen.getByText(/Customer/i));
    expect(screen.getByText(/Customer/i)).toBeInTheDocument();
    expect(screen.getByText(/Site ID/i)).toBeInTheDocument();
    // Check for exact text match in table headers
    expect(screen.getByText((_, el) => el?.tagName.toLowerCase() === 'th' && /Service Period/i.test(el.textContent || ''))).toBeInTheDocument();
    expect(screen.getByText((_, el) => el?.tagName.toLowerCase() === 'th' && /Status/i.test(el.textContent || ''))).toBeInTheDocument();
    expect(screen.getByText((_, el) => el?.tagName.toLowerCase() === 'th' && /Total/i.test(el.textContent || ''))).toBeInTheDocument();
});

test('sorts by Site Name when header clicked', async () => {
    render(
        <CustomerProvider>
            <StatementsGlobalList />
        </CustomerProvider>
    );
    await waitFor(() => screen.getByText('Customer'));

    await act(async () => {
        fireEvent.click(screen.getByText(/Customer/i));
    });

    const firstRowCustomer = (screen.getAllByRole('row')[1] as HTMLTableRowElement).cells[1].textContent;
    expect(firstRowCustomer).toBe('August');
});

test('filters by search input', async () => {
    render(
        <CustomerProvider>
            <StatementsGlobalList />
        </CustomerProvider>);
    await waitFor(() => screen.getByPlaceholderText('Search...'));

    const searchInput = screen.getByPlaceholderText('Search...');
    
    await act(async () => {
        fireEvent.change(searchInput, { target: { value: '0543' } });
    });

    // Wait for the table to update and check for formatted date (July 2024)
    await waitFor(() => {
        expect(screen.getByText('July 2024')).toBeInTheDocument();
        expect(screen.getByText('August 2024')).toBeInTheDocument();
    });
});

test('filters by status', async () => {
    render(
        <CustomerProvider>
            <StatementsGlobalList />
        </CustomerProvider>);
    
    await waitFor(() => screen.getByText('Customer'));

    const statusFilter = screen.getByDisplayValue('');
    await act(async () => {
        fireEvent.change(statusFilter, { target: { value: 'Approved' } });
    });

    await waitFor(() => {
        expect(screen.queryByText('August 2024')).not.toBeInTheDocument();
    });
});

