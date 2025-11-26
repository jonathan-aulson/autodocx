import { StatisticsCalculations } from 'src/components/Forecast/Statistics/lib/calculations';
import { FormValuesByDate, SiteStatisticData, SiteStatisticDetailData, TimeRangeType } from 'src/lib/models/Statistics';

describe('StatisticsCalculations', () => {
  // Mock data for testing
  const mockFormValues: FormValuesByDate = {
    "2025-07-01": {
      "valet-daily": 100,
      "valet-monthly": 50,
      "self-daily": 200,
      "self-monthly": 100,
      "occupied-rooms": 100,
      "occupancy": 0.5,
      "drive-in-ratio-input": 0.8,
      "capture-ratio-input": 0.3,
    },
  };

  const mockBudgetRatesByPeriod = {
    "2025-07-01": {
      valetRateDaily: 10,
      valetRateMonthly: 50,
      valetRateOvernight: 20,
      selfRateDaily: 5,
      selfRateMonthly: 25,
      selfRateOvernight: 10,
      baseRevenue: 1000,
      adjustmentPercentage: -0.04, // -4% adjustment
    },
  };

  const mockAllMonthsData: SiteStatisticData[] = [
    {
      customerSiteId: "test-site",
      periodLabel: "2025-07",
      budgetData: [
        {
          periodStart: "2025-07-01",
          periodLabel: "2025-07-01",
          valetRateDaily: 10,
          valetRateMonthly: 50,
          valetRateOvernight: 20,
          selfRateDaily: 5,
          selfRateMonthly: 25,
          selfRateOvernight: 10,
          baseRevenue: 1000,
          adjustmentPercentage: -0.04, // -4% adjustment
        } as SiteStatisticDetailData,
      ],
      forecastData: [
        {
          periodStart: "2025-07-01",
          periodLabel: "2025-07-01",
          valetRateDaily: 10,
          valetRateMonthly: 50,
          valetRateOvernight: 20,
          selfRateDaily: 5,
          selfRateMonthly: 25,
          selfRateOvernight: 10,
          baseRevenue: 1000,
          adjustmentPercentage: -0.04, // -4% adjustment
        } as SiteStatisticDetailData,
      ],
      actualData: [],
      siteStatisticId: "1",
      name: "Test Site",
      siteNumber: "123",
      totalRooms: 200,
      timeRangeType: TimeRangeType.DAILY
    },
  ];

  const mockPeriodData: SiteStatisticDetailData = {
    valetDaily: 100,
    valetRateDaily: 10,
    valetMonthly: 50,
    valetRateMonthly: 50,
    valetOvernight: 20, // Assuming this is already calculated
    valetRateOvernight: 20,
    selfDaily: 200,
    selfRateDaily: 5,
    selfMonthly: 100,
    selfRateMonthly: 25,
    selfOvernight: 40, // Assuming this is already calculated
    selfRateOvernight: 10,
    adjustmentPercentage: -0.04, // -4% adjustment
  } as SiteStatisticDetailData;


  describe('calculateExternalRevenue', () => {
    it('should calculate external revenue correctly with a negative adjustment percentage', () => {
      const periodStart = "2025-07-01";
      const selectedSite = "test-site";
      const inputType = "occupancy";
      const availableRooms = 200;

      // Mock calculateOvernightValet and calculateOvernightSelf for this test
      jest.spyOn(StatisticsCalculations, 'calculateOvernightValet').mockReturnValue(20);
      jest.spyOn(StatisticsCalculations, 'calculateOvernightSelf').mockReturnValue(40);

      // Expected gross revenue calculation:
      // valetDaily * valetRateDaily = 100 * 10 = 1000
      // valetMonthly * valetRateMonthly = 50 * 50 = 2500
      // valetOvernight * valetRateOvernight = 20 * 20 = 400
      // selfDaily * selfRateDaily = 200 * 5 = 1000
      // selfMonthly * selfRateMonthly = 100 * 25 = 2500
      // selfOvernight * selfRateOvernight = 40 * 10 = 400
      // Gross Revenue = 1000 + 2500 + 400 + 1000 + 2500 + 400 = 7800

      // Expected final revenue: 7800 * (1 + (-0.04)) = 7800 * 0.96 = 7488

      const result = StatisticsCalculations.calculateExternalRevenue(
        periodStart,
        selectedSite,
        mockFormValues,
        mockBudgetRatesByPeriod,
        inputType,
        availableRooms
      );
      expect(result).toBeCloseTo(7488);
    });

    it('should calculate external revenue correctly with a positive adjustment percentage', () => {
      const periodStart = "2025-07-01";
      const selectedSite = "test-site";
      const inputType = "occupancy";
      const availableRooms = 200;

      jest.spyOn(StatisticsCalculations, 'calculateOvernightValet').mockReturnValue(20);
      jest.spyOn(StatisticsCalculations, 'calculateOvernightSelf').mockReturnValue(40);

      const positiveAdjustmentRates = {
        ...mockBudgetRatesByPeriod,
        "2025-07-01": {
          ...mockBudgetRatesByPeriod["2025-07-01"],
          adjustmentPercentage: 0.05, // +5% adjustment
        },
      };

      // Gross Revenue (assuming same as above) = 7800
      // Expected final revenue: 7800 * (1 + 0.05) = 7800 * 1.05 = 8190

      const result = StatisticsCalculations.calculateExternalRevenue(
        periodStart,
        selectedSite,
        mockFormValues,
        positiveAdjustmentRates,
        inputType,
        availableRooms
      );
      expect(result).toBeCloseTo(8190);
    });

    it('should calculate external revenue correctly with zero adjustment percentage', () => {
      const periodStart = "2025-07-01";
      const selectedSite = "test-site";
      const inputType = "occupancy";
      const availableRooms = 200;

      jest.spyOn(StatisticsCalculations, 'calculateOvernightValet').mockReturnValue(20);
      jest.spyOn(StatisticsCalculations, 'calculateOvernightSelf').mockReturnValue(40);

      const zeroAdjustmentRates = {
        ...mockBudgetRatesByPeriod,
        "2025-07-01": {
          ...mockBudgetRatesByPeriod["2025-07-01"],
          adjustmentPercentage: 0, // 0% adjustment
        },
      };

      // Gross Revenue (assuming same as above) = 7800
      // Expected final revenue: 7800 * (1 + 0) = 7800

      const result = StatisticsCalculations.calculateExternalRevenue(
        periodStart,
        selectedSite,
        mockFormValues,
        zeroAdjustmentRates,
        inputType,
        availableRooms
      );
      expect(result).toBeCloseTo(7800);
    });
  });

  describe('calculateExternalRevenueForMonth', () => {
    it('should calculate external revenue for month correctly with a negative adjustment percentage', () => {
      const monthIndex = 0;
      const periodStart = "2025-07-01";
      const selectedSite = "test-site";
      const inputType = "occupancy";
      const availableRooms = 200;

      // Mock calculateOvernightValetForMonth and calculateOvernightSelfForMonth for this test
      jest.spyOn(StatisticsCalculations, 'calculateOvernightValetForMonth').mockReturnValue(20);
      jest.spyOn(StatisticsCalculations, 'calculateOvernightSelfForMonth').mockReturnValue(40);

      // Expected gross revenue calculation (same as above) = 7800
      // Expected final revenue: 7800 * (1 + (-0.04)) = 7800 * 0.96 = 7488

      const result = StatisticsCalculations.calculateExternalRevenueForMonth(
        monthIndex,
        periodStart,
        selectedSite,
        { [monthIndex]: mockFormValues }, // Corrected: wrap mockFormValues in a Record<number, FormValuesByDate>
        mockAllMonthsData,
        availableRooms,
        inputType,
        false, // showingBudget
        "DAILY" // timePeriod
      );
      expect(result.externalRevenue).toBeCloseTo(7488);
      expect(result.adjustmentValue).toBeCloseTo(-312); // 7800 * -0.04
      expect(result.adjustmentPercentage).toBe(-0.04);
    });
  });

  describe('Occupancy and Occupied Rooms Bidirectional Updates', () => {
    it('should calculate occupancy from occupied rooms when occupancy is set to 0', () => {
      const formValues = {
        '2024-01-01': {
          'occupancy': 0,
          'occupied-rooms': 50
        }
      };
      const availableRooms = 100;
      
      // calculateOccupancy always calculates from occupied rooms, not from stored occupancy
      const result = StatisticsCalculations.calculateOccupancy('2024-01-01', formValues, availableRooms);
      
      expect(result).toBe(0.5); // 50/100 = 0.5
    });
    
    it('should calculate occupancy from occupied rooms when occupied rooms is set to 0', () => {
      const formValues = {
        '2024-01-01': {
          'occupancy': 0.5,
          'occupied-rooms': 0
        }
      };
      const availableRooms = 100;
      
      const result = StatisticsCalculations.calculateOccupancy('2024-01-01', formValues, availableRooms);
      
      expect(result).toBe(0); // 0/100 = 0
    });
    
    it('should handle availableRooms = 0 edge case', () => {
      const formValues = {
        '2024-01-01': {
          'occupancy': 0.5,
          'occupied-rooms': 50
        }
      };
      const availableRooms = 0;
      
      const occupancyResult = StatisticsCalculations.calculateOccupancy('2024-01-01', formValues, availableRooms);
      const roomsResult = StatisticsCalculations.getOccupiedRooms('2024-01-01', formValues, availableRooms);
      
      expect(occupancyResult).toBe(0);
      expect(roomsResult).toBe(0);
    });

    it('should calculate occupancy correctly from occupied rooms', () => {
      const formValues = {
        '2024-01-01': {
          'occupied-rooms': 75
        }
      };
      const availableRooms = 100;
      
      const result = StatisticsCalculations.calculateOccupancy('2024-01-01', formValues, availableRooms);
      
      expect(result).toBe(0.75);
    });

    it('should calculate occupied rooms correctly from occupancy', () => {
      const formValues = {
        '2024-01-01': {
          'occupancy': 0.6
        }
      };
      const availableRooms = 100;
      
      const result = StatisticsCalculations.getOccupiedRooms('2024-01-01', formValues, availableRooms);
      
      expect(result).toBe(60);
    });

    it('should round occupied rooms calculation correctly', () => {
      const formValues = {
        '2024-01-01': {
          'occupancy': 0.333333
        }
      };
      const availableRooms = 100;
      
      const result = StatisticsCalculations.getOccupiedRooms('2024-01-01', formValues, availableRooms);
      
      // Removed round for fix bug 2633
      expect(result).toBe(33.3333);
    });
  });

});
