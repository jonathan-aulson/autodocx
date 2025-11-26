/**
 * Unit Tests for Debounce and Throttle Utilities (Jest)
 */

import { debounce, throttle } from '../lib/utils/debounce';

describe('Debounce Utility', () => {
  beforeEach(() => {
    jest.useFakeTimers();
  });

  afterEach(() => {
    jest.runOnlyPendingTimers();
    jest.useRealTimers();
  });

  test('should delay function execution', () => {
    const mockFn = jest.fn();
    const debouncedFn = debounce(mockFn, 300);

    debouncedFn('test');
    
    // Should not be called immediately
    expect(mockFn).not.toHaveBeenCalled();
    
    // Should be called after delay
    jest.advanceTimersByTime(300);
    expect(mockFn).toHaveBeenCalledWith('test');
  });

  test('should cancel previous calls when called again', () => {
    const mockFn = jest.fn();
    const debouncedFn = debounce(mockFn, 300);

    debouncedFn('first');
    jest.advanceTimersByTime(100);
    
    debouncedFn('second');
    jest.advanceTimersByTime(100);
    
    debouncedFn('third');
    jest.advanceTimersByTime(300);

    // Only the last call should be executed
    expect(mockFn).toHaveBeenCalledTimes(1);
    expect(mockFn).toHaveBeenCalledWith('third');
  });

  test('should handle multiple arguments', () => {
    const mockFn = jest.fn();
    const debouncedFn = debounce(mockFn, 300);

    debouncedFn('arg1', 'arg2', 'arg3');
    jest.advanceTimersByTime(300);

    expect(mockFn).toHaveBeenCalledWith('arg1', 'arg2', 'arg3');
  });
});

describe('Throttle Utility', () => {
  beforeEach(() => {
    jest.useFakeTimers();
  });

  afterEach(() => {
    jest.runOnlyPendingTimers();
    jest.useRealTimers();
  });

  test('should execute function immediately on first call', () => {
    const mockFn = jest.fn();
    const throttledFn = throttle(mockFn, 300);

    throttledFn('test');
    
    // Should be called immediately
    expect(mockFn).toHaveBeenCalledWith('test');
  });

  test('should ignore subsequent calls within throttle period', () => {
    const mockFn = jest.fn();
    const throttledFn = throttle(mockFn, 300);

    throttledFn('first');
    throttledFn('second');
    throttledFn('third');

    // Only first call should execute
    expect(mockFn).toHaveBeenCalledTimes(1);
    expect(mockFn).toHaveBeenCalledWith('first');
  });

  test('should allow execution after throttle period expires', () => {
    const mockFn = jest.fn();
    const throttledFn = throttle(mockFn, 300);

    // First call
    throttledFn('first');
    expect(mockFn).toHaveBeenCalledTimes(1);

    // Advance past throttle period
    jest.advanceTimersByTime(300);

    // Second call should now work
    throttledFn('second');
    expect(mockFn).toHaveBeenCalledTimes(2);
    expect(mockFn).toHaveBeenNthCalledWith(2, 'second');
  });
});