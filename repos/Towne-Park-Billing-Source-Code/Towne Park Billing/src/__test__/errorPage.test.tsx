import { render, screen } from '@testing-library/react';
import { BrowserRouter as Router } from 'react-router-dom';
import '@testing-library/jest-dom';
import ErrorPage from '../pages/errorPage/ErrorPage';

describe('ErrorPage Component', () => {
    test('renders error message', () => {
        render(
            <Router>
                <ErrorPage />
            </Router>
        );
        const errorMessage = screen.getByText(/Sorry, the page you are looking for does not exist./i);
        expect(errorMessage).toBeInTheDocument();
    });

    test('renders link to homepage', () => {
        render(
            <Router>
                <ErrorPage />
            </Router>
        );
        const homepageLink = screen.getByRole('link', { name: /Go to Homepage/i });
        expect(homepageLink).toBeInTheDocument();
        expect(homepageLink).toHaveAttribute('href', '/billing/customers');
    });
});
