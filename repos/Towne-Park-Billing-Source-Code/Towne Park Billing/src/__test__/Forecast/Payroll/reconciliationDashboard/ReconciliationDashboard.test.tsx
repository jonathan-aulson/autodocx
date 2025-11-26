import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import { ReconciliationDashboard } from '@/components/Forecast/Payroll/reconciliationDashboard/ReconciliationDashboard';
import { PayrollData } from '@/components/Forecast/Payroll/reconciliationDashboard/types';

// Add debug helper for diagnosis
function debugTable() {
  // Uncomment for local debugging
  // screen.debug();
}

const mockPayrollDto = {
  customerSiteId: 'test-site-id',
  forecastPayroll: [
    {
      jobGroupId: 'bell-group-id',
      jobGroupName: 'Bell',
      forecastPayrollCost: 820,
      forecastHours: 40,
      date: '2024-01-01'
    }
  ],
  budgetPayroll: [
    {
      jobGroupId: 'bell-group-id',
      jobGroupName: 'Bell',
      budgetPayrollCost: 800,
      budgetHours: 40,
      date: '2024-01-01'
    }
  ],
  actualPayroll: [
    {
      jobGroupId: 'bell-group-id',
      jobGroupName: 'Bell',
      actualPayrollCost: 850,
      actualHours: 40,
      date: '2024-01-01'
    }
  ],
  scheduledPayroll: [
    {
      jobGroupId: 'bell-group-id',
      jobGroupName: 'Bell',
      scheduledHours: 40,
      date: '2024-01-01'
    }
  ]
};

const mockData: PayrollData[] = [
  {
    date: new Date(2024, 0, 1), // Use regular Date constructor that matches getMonthBoundaries logic
    jobs: {
      'Bell': { 
        hours: 40, 
        scheduled: 40, // Add scheduled value (component uses scheduled || hours)
        budget: 800, 
        actual: 850, 
        forecast: 820 
      }
    }
  }
];

describe('ReconciliationDashboard', () => {
  const defaultProps = {
    data: mockData,
    payrollDto: mockPayrollDto,
    billingPeriod: '2024-01',
    timeHorizon: 'monthly' as const,
    showComparison: true,
    comparisonType: 'actual-vs-budget' as const
  };

  it('should render table with job data', () => {
    render(<ReconciliationDashboard {...defaultProps} />);
    debugTable();

    // Job label
    expect(screen.getByText('Bell')).toBeInTheDocument();

    // Check for currency formatted values - use getAllByText to handle multiple instances
    expect(screen.getAllByText(/\$850\.00/).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/\$800\.00/).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/\$820\.00/).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/\$40\.00/).length).toBeGreaterThan(0);
  });

  it('should show month display correctly', () => {
    render(<ReconciliationDashboard {...defaultProps} />);
    debugTable();

    expect(screen.getByText('Month:')).toBeInTheDocument();
    expect(screen.getByText('January 2024')).toBeInTheDocument();
  });

  it('should show job filter dropdown with dynamic options', () => {
    render(<ReconciliationDashboard {...defaultProps} />);
    debugTable();

    expect(screen.getByText('All Jobs')).toBeInTheDocument();
  });

  it('should render table headers correctly', () => {
    render(<ReconciliationDashboard {...defaultProps} />);
    debugTable();

    expect(screen.getByText('Job Code')).toBeInTheDocument();
    expect(screen.getByText('Scheduled')).toBeInTheDocument();
    expect(screen.getByText('Actual')).toBeInTheDocument();
    expect(screen.getByText('Forecast')).toBeInTheDocument();
    expect(screen.getByText('Budget')).toBeInTheDocument();
    expect(screen.getByText('Actual Variance to Budget')).toBeInTheDocument();
  });

  it('should show variance indicator correctly', () => {
    render(<ReconciliationDashboard {...defaultProps} />);
    debugTable();

    // Should show upward arrow for positive variance (850 - 800 = 50)
    // The arrow is rendered as a separate span, so we check for both
    expect(screen.getAllByText('↑').length).toBeGreaterThan(0);
    expect(screen.getAllByText(/\$50\.00/).length).toBeGreaterThan(0);
  });

  it('should show total row with aggregated values', () => {
    render(<ReconciliationDashboard {...defaultProps} />);
    debugTable();

    expect(screen.getByText('Total')).toBeInTheDocument();
    // Total scheduled, actual, forecast, budget, variance - use getAllByText to handle multiple instances
    expect(screen.getAllByText(/\$40\.00/).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/\$850\.00/).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/\$820\.00/).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/\$800\.00/).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/\$50\.00/).length).toBeGreaterThan(0);
  });

  it('should handle missing actual data', () => {
    const dataWithMissingActual: PayrollData[] = [
      {
        date: new Date(2024, 0, 1), // Use regular Date constructor that matches getMonthBoundaries logic
        jobs: {
          'Bell': { 
            hours: 40, 
            scheduled: 40, // Add scheduled value
            budget: 800, 
            actual: null, 
            forecast: 820 
          }
        }
      }
    ];

    render(
      <ReconciliationDashboard 
        {...defaultProps} 
        data={dataWithMissingActual}
      />
    );
    debugTable();

    // Should show "—" for missing actual data
    const cells = screen.getAllByText('—');
    expect(cells.length).toBeGreaterThan(0);
  });
});
