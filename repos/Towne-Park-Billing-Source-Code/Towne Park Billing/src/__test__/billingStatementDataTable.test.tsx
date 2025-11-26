import { Statement, StatementStatus } from '@/lib/models/Statement';
import '@testing-library/jest-dom';
import { fireEvent, render, screen } from '@testing-library/react';
import { BrowserRouter as Router } from 'react-router-dom';
import StatementDataTable from '../components/BillingStatement/billingStatementDataTable';

const mockStatements: Statement[] = [
    {
        id: 'stmt001',
        customerSiteId: 'site001',
        createdMonth: 'April 2023',
        servicePeriod: 'April 1 - April 30, 2023',
        servicePeriodStart: '2023-04-01',
        totalAmount: 1000.00,
        status: StatementStatus.SENT,
        invoices: [],
        siteNumber: '001',
        siteName: 'Main Site',
        amNotes: 'No additional notes',
        forecastData: '{}'
    },
    {
        id: 'stmt002',
        customerSiteId: 'site002',
        createdMonth: 'May 2023',
        servicePeriod: 'May 1 - May 31, 2023',
        servicePeriodStart: '2023-05-01',
        totalAmount: 1500.00,
        status: StatementStatus.GENERATING,
        invoices: [],
        siteNumber: '002',
        siteName: 'Secondary Site',
        amNotes: 'Additional notes',
        forecastData: '{}'
    },
    // ... other mock statements
];

describe('StatementDataTable Component', () => {
    it('renders columns correctly', () => {
        render(
            <Router>
                <StatementDataTable statements={mockStatements} />
            </Router>
        );

        expect(screen.getByText('Billing Cycle')).toBeInTheDocument();
        expect(screen.getByText('Service Period')).toBeInTheDocument();
        expect(screen.getByText('Status')).toBeInTheDocument();
        expect(screen.getByText('Total')).toBeInTheDocument();
    });

    it('renders statement data correctly', () => {
        render(
            <Router>
                <StatementDataTable statements={mockStatements} />
            </Router>
        );

        expect(screen.getByText('April 2023')).toBeInTheDocument();
        expect(screen.getByText('April 1 - April 30, 2023')).toBeInTheDocument();
        expect(screen.getByText('Sent')).toBeInTheDocument();
        expect(screen.getByText('$1,000.00')).toBeInTheDocument();

        expect(screen.getByText('April 2023')).toBeInTheDocument();
        expect(screen.getByText('May 1 - May 31, 2023')).toBeInTheDocument();
        expect(screen.getByText('Generating')).toBeInTheDocument();
        expect(screen.getByText('$1,500.00')).toBeInTheDocument();
    });

    it('handles pagination correctly', () => {
        const mockStatements: Statement[] = Array.from({ length: 20 }, (_, i) => ({
            id: `${i + 1}`,
            customerSiteId: `site00${i + 1}`,
            createdMonth: `Month ${i + 1}`,
            servicePeriod: `01/${String(i + 1).padStart(2, '0')}/2023 - ${String(i + 1).padStart(2, '0')}/2023`,
            servicePeriodStart: `2023-${String(i + 1).padStart(2, '0')}-01`,
            status: StatementStatus.SENT,
            totalAmount: (i + 1) * 100,
            invoices: [],
            siteNumber: `00${i + 1}`,
            siteName: `Site ${i + 1}`,
            amNotes: `Note ${i + 1}`,
            forecastData: '{}'
        }));


        render(
            <Router>
                <StatementDataTable statements={mockStatements} />
            </Router>
        );

        expect(screen.getByText('Month 1')).toBeInTheDocument();
        expect(screen.getByText('Month 12')).toBeInTheDocument();
        expect(screen.queryByText('Month 13')).not.toBeInTheDocument();

        // Click to navigate to the next page
        fireEvent.click(screen.getByText('2'));
    
        // After navigation - should show second page (13-20)
        expect(screen.getByText('Month 13')).toBeInTheDocument();
        expect(screen.queryByText('Month 1')).not.toBeInTheDocument();
    });

    it('renders no data message if statements is empty', () => {
        render(
            <Router>
                <StatementDataTable statements={[]} />
            </Router>
        );

        expect(screen.getByText('No statements found')).toBeInTheDocument();
    });
});
