import App from '@/App';
import '@testing-library/jest-dom';
import { render } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';

// Mock the AuthContext
jest.mock('@/contexts/AuthContext', () => ({
    useAuth: jest.fn().mockReturnValue({
        userRoles: ['billingManager'],
        userName: 'Test User',
        isAuthenticated: true,
        isLoading: false,
        error: null,
        refreshUserData: jest.fn(),
        logout: jest.fn(),
    }),
    // Export AuthProvider as a passthrough component
    AuthProvider: ({ children }: { children: React.ReactNode }) => children,
}));

test('renders app without crashing', () => {
    render(
        <BrowserRouter>
            <App />
        </BrowserRouter>
    );
    // Your assertions here
});
