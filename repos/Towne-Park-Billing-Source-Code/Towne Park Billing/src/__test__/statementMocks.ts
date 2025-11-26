import { InvoiceSummary } from "@/lib/models/Statement";
import { Invoice, InvoiceLineItem } from '../lib/models/Invoice';

const lineItems1: InvoiceLineItem[] = [
    { title: 'Web hosting April 2023', code: 'WH-001', description: 'Example description 1\nNew line text\nNew line text 2 \nNew line text 3\nNew line text 4\nNew line 5', amount: 350.00 },
    { title: 'SSL Certificate', code: 'SSL-001', description: 'Example description\n', amount: 50.00 },
];

const lineItems2: InvoiceLineItem[] = [
    { title: 'Cloud services May 2023', code: 'CS-001', description: 'Example description3', amount: 250.00 },
    { title: 'Database backup and storage', code: 'DBS-001', description: 'Example description4', amount: 300.00 },
];

// Mock Invoices
const invoiceSummaries: InvoiceSummary[] = [
    {
        id: 'inv001',
        invoiceNumber: '2023-04-INV001',
        amount: 400.00
    },
    {
        id: 'inv002',
        invoiceNumber: '2023-05-INV002',
        amount: 550.00
    },
];

const invoices: Invoice[] = [
    {
        id: 'inv001',
        invoiceNumber: '2023-04-INV001',
        amount: 400.00,
        invoiceDate: '2023-04-01',
        paymentTerms: '30 days',
        title: 'Hosting and SSL Services',
        description: 'Services provided in April 2023',
        lineItems: lineItems1,
    },
    {
        id: 'inv002',
        invoiceNumber: '2023-05-INV002',
        amount: 550.00,
        invoiceDate: '2023-05-01',
        paymentTerms: '30 days',
        title: 'Cloud and Storage Services',
        description: 'Services provided in May 2023',
        lineItems: lineItems2,
    },
];

export { invoices, invoiceSummaries };

