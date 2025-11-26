import * as toastModule from '@/components/ui/use-toast';
import '@testing-library/jest-dom';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { format } from 'date-fns';
import UnitAccounts from '../components/AdminPanel/UnitAccounts/UnitAccounts';

jest.mock('@/components/ui/use-toast', () => ({
  useToast: jest.fn(() => ({
    toast: jest.fn(),
  })),
}));

jest.mock('react-spinners', () => ({
  PulseLoader: () => <div data-testid="loading-spinner">Loading...</div>
}));

global.fetch = jest.fn();

describe('UnitAccounts Component', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    (fetch as jest.Mock).mockClear();
  });

  it('renders the component correctly', () => {
    render(<UnitAccounts />);

    expect(screen.getByText('Unit Accounts')).toBeInTheDocument();

    expect(screen.getByRole('button', { name: /Process Unit Accounts/i })).toBeInTheDocument();

    expect(screen.getByText(/Manually trigger a 'Unit Accounts'/)).toBeInTheDocument();
  });

  it('processes unit accounts successfully', async () => {
    const mockToast = jest.fn();
    (toastModule.useToast as jest.Mock).mockReturnValue({ toast: mockToast });

    // Mock successful fetch response
    (fetch as jest.Mock).mockResolvedValueOnce({
      ok: true,
      json: async () => ({}),
    });

    render(<UnitAccounts />);

    const currentDate = new Date();
    const formattedMonth = format(currentDate, 'MMMM yyyy');

    const processButton = screen.getByRole('button', { name: /Process Unit Accounts/i });
    fireEvent.click(processButton);

    await waitFor(() => {
      expect(fetch).toHaveBeenCalledWith(
        `/api/unit-account/${format(currentDate, 'yyyy-MM')}`,
        expect.objectContaining({
          method: 'POST',
          headers: expect.objectContaining({
            'Content-Type': 'application/json',
          })
        })
      );

      expect(mockToast).toHaveBeenCalledWith({
        title: "Success",
        description: `Unit accounts for ${formattedMonth} have been successfully processed.`,
      });
    });
  });

  it('handles API errors correctly', async () => {
    const mockToast = jest.fn();
    (toastModule.useToast as jest.Mock).mockReturnValue({ toast: mockToast });

    // Mock failed fetch response
    (fetch as jest.Mock).mockResolvedValueOnce({
      ok: false,
    });

    const consoleSpy = jest.spyOn(console, 'error').mockImplementation();

    render(<UnitAccounts />);

    const processButton = screen.getByRole('button', { name: /Process Unit Accounts/i });
    fireEvent.click(processButton);

    // Wait for the API call to resolve
    await waitFor(() => {
      // Check if error was logged
      expect(consoleSpy).toHaveBeenCalled();

      // Check if toast was called with error message
      expect(mockToast).toHaveBeenCalledWith({
        title: "Error",
        description: "Failed to process unit accounts. Please try again.",
        variant: "destructive",
      });
    });

    consoleSpy.mockRestore();
  });

  it('shows loading state during processing', async () => {
    let resolvePromise: (value: any) => void;
    const promise = new Promise((resolve) => {
      resolvePromise = resolve;
    });

    (fetch as jest.Mock).mockReturnValueOnce(promise);

    render(<UnitAccounts />);

    const processButton = screen.getByRole('button', { name: /Process Unit Accounts/i });
    fireEvent.click(processButton);

    expect(processButton).toBeDisabled();

    expect(screen.getByTestId('loading-spinner')).toBeInTheDocument();

    resolvePromise!({ ok: true, json: async () => ({}) });

    await waitFor(() => {
      expect(processButton).not.toBeDisabled();
      expect(screen.queryByTestId('loading-spinner')).not.toBeInTheDocument();
    });
  })
});
