import SupportingReports from '@/components/SupportingReportsToggles/SupportingReportsToggles';
import { SupportingReportsType } from '@/lib/models/Contract';
import '@testing-library/jest-dom';
import { fireEvent, render, screen } from '@testing-library/react';

describe('SupportingReports', () => {
    const defaultProps = {
        reportsState: {
            [SupportingReportsType.HOURS_BACKUP_REPORT]: false,
            [SupportingReportsType.LABOR_DISTRIBUTION_REPORT]: false,
            [SupportingReportsType.MIX_OF_SALES]: false,
            [SupportingReportsType.TAX_REPORT]: false,
            [SupportingReportsType.OTHER_EXPENSES]: false,
            [SupportingReportsType.PARKING_DEPARTMENT_REPORT]: false,
            [SupportingReportsType.VALIDATION_REPORT]: false,	
        },
        onReportTypeChange: jest.fn(),
        isFixedFeeEnabled: false,
        isPerLaborHourEnabled: false,
        isPerOccupiedRoomEnabled: false,
        isRevenueShareEnabled: false,
        isManagementAgreementEnabled: false,
        isBillablePayrollEnabled: false,
        isEditable: true,
        isBillingAdmin: false,
    };

    beforeEach(() => {
        jest.clearAllMocks();
    });

    it('renders all report toggles', () => {
        render(<SupportingReports {...defaultProps} />);
        
        expect(screen.getByText('Hours Backup Report')).toBeInTheDocument();
        expect(screen.getByText('Labor Distribution Report')).toBeInTheDocument();
        expect(screen.getByText('Mix of Sales Report')).toBeInTheDocument();
        expect(screen.getByText('Tax Report')).toBeInTheDocument();
    });

    it('disables Hours Backup Report when Per Labor Hour is disabled', () => {
        const props = {
            ...defaultProps,
            reportsState: {
                ...defaultProps.reportsState,
                [SupportingReportsType.HOURS_BACKUP_REPORT]: true,
            },
            isPerLaborHourEnabled: false,
        };
        render(<SupportingReports {...props} />);
        
        expect(defaultProps.onReportTypeChange).toHaveBeenCalledWith(
            SupportingReportsType.HOURS_BACKUP_REPORT
        );
    });

    it('allows manual toggle of reports when no auto-enable conditions are met', () => {
        render(<SupportingReports {...defaultProps} isFixedFeeEnabled={true} />);
        
        const toggles = screen.getAllByRole('switch');
        fireEvent.click(toggles[4]); // Tax Report toggle
        
        expect(defaultProps.onReportTypeChange).toHaveBeenCalledWith(
            SupportingReportsType.TAX_REPORT
        );
    });

    it('allows Billing Admin to enable any report when Per Labor Hour is enabled', () => {        
        const props = {
            ...defaultProps,
            reportsState: {
                ...defaultProps.reportsState,
                [SupportingReportsType.LABOR_DISTRIBUTION_REPORT]: false,
            },
            isPerLaborHourEnabled: true,
            isBillingAdmin: true,
        };
    
        render(<SupportingReports {...props} />);

        const toggles = screen.getAllByRole('switch');
        fireEvent.click(toggles[1]);
    
        expect(props.onReportTypeChange).toHaveBeenCalledWith(SupportingReportsType.LABOR_DISTRIBUTION_REPORT);
    });
});