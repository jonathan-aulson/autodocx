import { StatementContext } from "@/components/BillingStatement/StatementContext";
import { InvoiceCacheProvider } from '@/components/invoiceDetails/InvoiceCacheContext';
import InvoiceDetails from '@/components/invoiceDetails/InvoiceDetails';
import { CustomerProvider } from '@/pages/customersDetails/CustomersDetailContext';
import '@testing-library/jest-dom';
import { render, screen, waitFor } from '@testing-library/react';
import fetchMock from 'jest-fetch-mock';
import { BrowserRouter as Router } from 'react-router-dom';

jest.mock('react-router-dom', () => ({
    ...jest.requireActual('react-router-dom'),
    useParams: jest.fn(),
}));

fetchMock.enableMocks();

describe('<InvoiceDetail />', () => {
    const mockReloadStatementsToggle = jest.fn();

    const renderComponent = (invoiceId: string, editMode: boolean, onCloseModal: () => void) => {
        require('react-router-dom').useParams.mockReturnValue({ invoiceId });

        return render(
            <Router>
                <CustomerProvider>
                    <InvoiceCacheProvider>
                        <StatementContext.Provider value={{ reloadStatementsToggle: mockReloadStatementsToggle }}>
                            <InvoiceDetails invoiceId={invoiceId} editMode={editMode} onCloseModal={onCloseModal} />
                        </StatementContext.Provider>
                    </InvoiceCacheProvider>
                </CustomerProvider>
            </Router>
        );
    };

    beforeEach(() => {
        fetchMock.resetMocks();
    });

    // it('displays invoice details correctly when invoice exists', async () => {
    //     const mockInvoiceId = 'inv001';
    //     const mockInvoice = {
    //         invoiceNumber: "INV-001",
    //         invoiceDate: "2023-10-23",
    //         paymentTerms: "Net 30",
    //         amount: 1000,
    //         title: "Invoice Title",
    //         description: "Invoice description here.",
    //         lineItems: [
    //             { title: "Service A", description: "Details about service A", amount: 500 }
    //         ]
    //     };

    //     fetchMock.mockResponseOnce(() => {
    //         console.log("Mock fetch request made");
    //         return Promise.resolve({
    //             status: 200,
    //             body: JSON.stringify(mockInvoice)
    //         });
    //     });

    //     renderComponent(mockInvoiceId, false, () => { });

    //     await waitFor(() => {
    //         console.log("Waiting for invoice number to appear...");
    //         const invoiceNumber = screen.getByText("INV-001");
    //         expect(invoiceNumber).toBeInTheDocument();
    //     });

    //     expect(screen.getByText("2023-10-23")).toBeInTheDocument();
    //     expect(screen.getByText("Net 30")).toBeInTheDocument();
    //     expect(screen.getByText(formatCurrency(1000))).toBeInTheDocument();
    //     expect(screen.getByText("Service A")).toBeInTheDocument();
    //     expect(screen.getByText("Details about service A")).toBeInTheDocument();
    // });

    it('shows error message for invalid invoice ID', async () => {
        const mockInvoiceId = 'invalid_id';

        fetchMock.mockResponseOnce(() => {
            console.log("Mock 404 response");
            return Promise.resolve({
                status: 404,
                body: JSON.stringify({ message: 'Not found' })
            });
        });

        renderComponent(mockInvoiceId, false, () => { });

        await waitFor(() => {
            console.log("Waiting for error message...");
            expect(screen.getByText(/an unexpected error occurred/i)).toBeInTheDocument();
        });
    });
});
