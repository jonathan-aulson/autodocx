import SupportingDocuments from '@/components/SupportingDocuments/SupportingDocuments';
import { ForecastData, Statement, StatementStatus } from "@/lib/models/Statement";
import '@testing-library/jest-dom';
import { fireEvent, render, screen } from '@testing-library/react';

jest.mock('@/components/Modal', () => ({
    __esModule: true,
    default: ({ children, show }: { children: React.ReactNode; show: boolean }) => {
        return show ? <div>{children}</div> : null;
    }
}));

jest.mock('@/pages/customersDetails/CustomersDetailContext', () => ({
    useCustomerDetails: () => ({
        contractDetails: {
            deviationPercentage: 5,
            deviationAmount: 500
        }
    })
}));

const mockStatement: Statement = {
    id: 'stmt1',
    customerSiteId: 'cust01',
    servicePeriod: '2024-10',
    servicePeriodStart: '2024-10-01',
    totalAmount: 15000,
    forecastData: "{\n  \"forecastedRevenue\": 15000,\n  \"postedRevenue\": 12000,\n  \"invoicedRevenue\": 13000,\n  \"totalActualRevenue\": 25000,\n  \"forecastDeviationPercentage\": 66.6,\n  \"forecastDeviationAmount\": 10000,\n  \"forecastLastUpdated\": \"10/01/2024\"\n}",
    siteNumber: '0170',
    createdMonth: '202410',
    amNotes: '',
    status: StatementStatus.APPROVED,
    invoices: [],
    siteName: 'Main Site',
};

const mockForecast: ForecastData = {
    forecastDeviationAmount: 10000,
    forecastDeviationPercentage: 66.6,
    forecastedRevenue: 15000,
    forecastLastUpdated: "10/01/2024",
    invoicedRevenue: 13000,
    postedRevenue: 12000,
    totalActualRevenue: 25000,
}

describe('ForecastComparisonReport Component', () => {
    test('renders button and header correctly', () => {
        render(<SupportingDocuments reportStatement={mockStatement} forecastData={mockForecast} />);

        expect(screen.getByText('Supporting Documents')).toBeInTheDocument();
        expect(screen.getByText('Towne Park - Forecast Comparison Report')).toBeInTheDocument();
    });

    test('opens modal with forecast details on button click', () => {
        render(<SupportingDocuments reportStatement={mockStatement} forecastData={mockForecast} />);

        const button = screen.getByRole('button');
        fireEvent.click(button);

        expect(screen.getByTestId('forecasted-revenue')).toHaveTextContent('15,000.00');
        expect(screen.getByTestId('posted-revenue')).toHaveTextContent('12,000.00');
        expect(screen.getByTestId('invoiced-revenue')).toHaveTextContent('13,000.00');
        expect(screen.getByTestId('total-actual-revenue')).toHaveTextContent('25,000.00');
    });

    test('displays the deviation percentage and amount with thresholds', () => {
        render(<SupportingDocuments reportStatement={mockStatement} forecastData={mockForecast} />);

        const button = screen.getByRole('button');
        fireEvent.click(button);

        expect(screen.getByTestId('deviation-percentage')).toHaveTextContent('66.60%');
        expect(screen.getByTestId('deviation-amount')).toHaveTextContent('10,000.00');
    });
});
