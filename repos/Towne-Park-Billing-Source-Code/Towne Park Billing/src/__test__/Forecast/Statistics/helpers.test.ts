import { syncOccupancyAndOccupiedRooms } from 'src/components/Forecast/Statistics/lib/helpers';
import { FormValuesByDate } from 'src/lib/models/Statistics';

describe('syncOccupancyAndOccupiedRooms', () => {
  it('should update occupied rooms when occupancy is set to 0', () => {
    const formValues: FormValuesByDate = {
      '2024-01-01': {
        'occupancy': 0,
        'occupied-rooms': 50
      }
    };
    const availableRooms = 100;
    
    const result = syncOccupancyAndOccupiedRooms(formValues, availableRooms);
    
    expect(result['2024-01-01']['occupied-rooms']).toBe(0);
    expect(result['2024-01-01']['occupancy']).toBe(0);
  });
  
  it('should update occupancy when occupied rooms is set to 0', () => {
    const formValues: FormValuesByDate = {
      '2024-01-01': {
        'occupancy': 0.5,
        'occupied-rooms': 0
      }
    };
    const availableRooms = 100;
    
    const result = syncOccupancyAndOccupiedRooms(formValues, availableRooms);
    
    expect(result['2024-01-01']['occupied-rooms']).toBe(0);
    expect(result['2024-01-01']['occupancy']).toBe(0);
  });
  
  it('should handle availableRooms = 0 edge case', () => {
    const formValues: FormValuesByDate = {
      '2024-01-01': {
        'occupancy': 0.5,
        'occupied-rooms': 50
      }
    };
    const availableRooms = 0;
    
    const result = syncOccupancyAndOccupiedRooms(formValues, availableRooms);
    
    expect(result['2024-01-01']['occupied-rooms']).toBe(0);
    expect(result['2024-01-01']['occupancy']).toBe(0);
  });

  it('should calculate occupancy from occupied rooms when occupancy is missing', () => {
    const formValues: FormValuesByDate = {
      '2024-01-01': {
        'occupied-rooms': 75
        // occupancy is missing
      }
    };
    const availableRooms = 100;
    
    const result = syncOccupancyAndOccupiedRooms(formValues, availableRooms);
    
    expect(result['2024-01-01']['occupied-rooms']).toBe(75);
    expect(result['2024-01-01']['occupancy']).toBe(0.75);
  });

  it('should calculate occupied rooms from occupancy when occupied rooms is missing', () => {
    const formValues: FormValuesByDate = {
      '2024-01-01': {
        'occupancy': 0.6
        // occupied-rooms is missing
      }
    };
    const availableRooms = 100;
    
    const result = syncOccupancyAndOccupiedRooms(formValues, availableRooms);
    
    expect(result['2024-01-01']['occupancy']).toBe(0.6);
    expect(result['2024-01-01']['occupied-rooms']).toBe(60);
  });

  it('should handle both values being zero', () => {
    const formValues: FormValuesByDate = {
      '2024-01-01': {
        'occupancy': 0,
        'occupied-rooms': 0
      }
    };
    const availableRooms = 100;
    
    const result = syncOccupancyAndOccupiedRooms(formValues, availableRooms);
    
    expect(result['2024-01-01']['occupied-rooms']).toBe(0);
    expect(result['2024-01-01']['occupancy']).toBe(0);
  });

  it('should preserve existing values when both are present and valid', () => {
    const formValues: FormValuesByDate = {
      '2024-01-01': {
        'occupancy': 0.8,
        'occupied-rooms': 80
      }
    };
    const availableRooms = 100;
    
    const result = syncOccupancyAndOccupiedRooms(formValues, availableRooms);
    
    expect(result['2024-01-01']['occupied-rooms']).toBe(80);
    expect(result['2024-01-01']['occupancy']).toBe(0.8);
  });

  it('should handle multiple periods', () => {
    const formValues: FormValuesByDate = {
      '2024-01-01': {
        'occupancy': 0,
        'occupied-rooms': 50
      },
      '2024-01-02': {
        'occupancy': 0.5,
        'occupied-rooms': 0
      }
    };
    const availableRooms = 100;
    
    const result = syncOccupancyAndOccupiedRooms(formValues, availableRooms);
    
    expect(result['2024-01-01']['occupied-rooms']).toBe(0);
    expect(result['2024-01-01']['occupancy']).toBe(0);
    expect(result['2024-01-02']['occupied-rooms']).toBe(0);
    expect(result['2024-01-02']['occupancy']).toBe(0);
  });

  it('should preserve other fields in the form values', () => {
    const formValues: FormValuesByDate = {
      '2024-01-01': {
        'occupancy': 0,
        'occupied-rooms': 50,
        'valet-daily': 100,
        'self-daily': 200
      }
    };
    const availableRooms = 100;
    
    const result = syncOccupancyAndOccupiedRooms(formValues, availableRooms);
    
    expect(result['2024-01-01']['occupied-rooms']).toBe(0);
    expect(result['2024-01-01']['occupancy']).toBe(0);
    expect(result['2024-01-01']['valet-daily']).toBe(100);
    expect(result['2024-01-01']['self-daily']).toBe(200);
  });
});
