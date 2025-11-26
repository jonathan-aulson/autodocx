import { SiteHeader } from '@/components/SiteHeader';
import { useAuth } from '@/contexts/AuthContext';
import '@testing-library/jest-dom';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';

// Mock the AuthContext
jest.mock('@/contexts/AuthContext', () => ({
    useAuth: jest.fn().mockReturnValue({
        userRoles: ['billingAdmin', 'accountManager'],
        userName: 'Test User',
        isAuthenticated: true,
        isLoading: false,
        error: null,
        refreshUserData: jest.fn(),
        logout: jest.fn(),
    }),
}));

// Mock the theme provider context
jest.mock('@/components/ThemeProvider', () => ({
    useTheme: jest.fn().mockReturnValue({
        theme: 'light',
        setTheme: jest.fn(),
    }),
}));

describe('SiteHeader component', () => {
    beforeEach(() => {
        jest.clearAllMocks();
        (global.fetch as jest.Mock).mockClear();
    });

    test('displays user name initials after successful authentication', () => {
        // Set up mock return values for useAuth
        (useAuth as jest.Mock).mockReturnValue({
            userRoles: ['billingAdmin', 'accountManager'],
            userName: 'Test User',
            isAuthenticated: true,
            isLoading: false,
            error: null,
            refreshUserData: jest.fn(),
            logout: jest.fn(),
        });

        render(
            <MemoryRouter>
                <SiteHeader />
            </MemoryRouter>
        );

        // Find the avatar element that contains the user initials
        const avatarFallback = screen.getByText('TU');
        expect(avatarFallback).toBeInTheDocument();
    });

    test('renders navigation links correctly', () => {
        render(
            <MemoryRouter>
                <SiteHeader />
            </MemoryRouter>
        );
        
        // Get the desktop navigation container first
        const desktopNavContainer = document.querySelector('.space-x-6.hidden.md\\:block');
        expect(desktopNavContainer).toBeInTheDocument();
        
        // Check if expected links exist in the desktop nav
        if (desktopNavContainer) {
            const desktopLinks = desktopNavContainer.querySelectorAll('a');
            expect(desktopLinks.length).toBe(4);
            
            expect(desktopLinks[0].textContent).toBe('Customers');
            expect(desktopLinks[1].textContent).toBe('Statements');
            expect(desktopLinks[2].textContent).toBe('Forecasts');
            expect(desktopLinks[3].textContent).toBe('P&L View');
        }
    });

    test('does not show Forecasts link when user is not an account manager', () => {
        // Mock the auth context to return a user without the accountManager role
        (useAuth as jest.Mock).mockReturnValue({
            userRoles: ['billingAdmin'],
            userName: 'Test User',
            isAuthenticated: true,
            isLoading: false,
            error: null,
            refreshUserData: jest.fn(),
            logout: jest.fn(),
        });

        render(
            <MemoryRouter>
                <SiteHeader />
            </MemoryRouter>
        );
        
        // Check desktop navigation container
        const desktopNavContainer = document.querySelector('.space-x-6.hidden.md\\:block');
        expect(desktopNavContainer).toBeInTheDocument();
        
        if (desktopNavContainer) {
            const desktopLinks = desktopNavContainer.querySelectorAll('a');
            // Should only have 2 links (no Forecasts)
            expect(desktopLinks.length).toBe(2);
            
            expect(desktopLinks[0].textContent).toBe('Customers');
            expect(desktopLinks[1].textContent).toBe('Statements');
            
            // Verify Forecasts is not present
            const forecastsLink = Array.from(desktopLinks).find(link => 
                link.textContent === 'Forecasts'
            );
            expect(forecastsLink).toBeUndefined();
        }
    });
});
