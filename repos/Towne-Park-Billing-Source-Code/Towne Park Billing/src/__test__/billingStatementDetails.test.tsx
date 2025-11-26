import { invoiceSummaries } from '@/__test__/statementMocks';
import { StatementStatus } from '@/lib/models/Statement';
import { formatCurrency } from '@/lib/utils';
import '@testing-library/jest-dom';
import { render } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import BillingStatementDetails from '../components/BillingStatement/billingStatementDetails';

jest.mock('react-router-dom', () => ({
    ...jest.requireActual('react-router-dom'),
    useParams: jest.fn(),
}));

describe('BillingStatementDetails Component', () => {
    it('renders billing statement details correctly', () => {
        require('react-router-dom').useParams.mockReturnValue({ statementId: 'stmt001' });

        const { getByText } = render(
            <MemoryRouter initialEntries={['/billing-statement-details/stmt001']}>
                <Routes>
                    <Route path="/billing-statement-details/:statementId" element={<BillingStatementDetails invoices={invoiceSummaries} statementStatus={StatementStatus.READY_TO_SEND} userRoles={['billingAdmin']} />} />
                </Routes>
            </MemoryRouter>
        );

        expect(getByText(formatCurrency(400))).toBeInTheDocument();
        expect(getByText(formatCurrency(550))).toBeInTheDocument();
    });

    it('displays message when no statement details are found', () => {
        require('react-router-dom').useParams.mockReturnValue({ invoices: 'invalid_id' });

        const { getByText } = render(
            <MemoryRouter initialEntries={['/billing-statement-details/invalid_id']}>
                <Routes>
                    <Route path="/billing-statement-details/:statementId" element={<BillingStatementDetails invoices={[]} statementStatus={StatementStatus.READY_TO_SEND} userRoles={['billingAdmin']}/>} />
                </Routes>
            </MemoryRouter>
        );

        expect(getByText('No invoices found.')).toBeInTheDocument();
    });
});