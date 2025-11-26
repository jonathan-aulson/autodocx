import { formatCurrency, formatCurrencyWhole } from '@/lib/utils';

describe('Currency Formatting Functions', () => {
  describe('formatCurrency', () => {
    it('should format currency with 2 decimal places', () => {
      expect(formatCurrency(1234.56)).toBe('$1,234.56');
      expect(formatCurrency(1000)).toBe('$1,000.00');
      expect(formatCurrency(0)).toBe('$0.00');
    });

    it('should handle negative numbers', () => {
      expect(formatCurrency(-1234.56)).toBe('-$1,234.56');
      expect(formatCurrency(-1000)).toBe('-$1,000.00');
    });
  });

  describe('formatCurrencyWhole', () => {
    it('should format currency to whole dollars (no decimal places)', () => {
      expect(formatCurrencyWhole(1234.56)).toBe('$1,235');
      expect(formatCurrencyWhole(1234.4)).toBe('$1,234');
      expect(formatCurrencyWhole(1234.5)).toBe('$1,235');
      expect(formatCurrencyWhole(1000)).toBe('$1,000');
      expect(formatCurrencyWhole(0)).toBe('$0');
    });

    it('should round numbers correctly', () => {
      expect(formatCurrencyWhole(1234.1)).toBe('$1,234');
      expect(formatCurrencyWhole(1234.5)).toBe('$1,235');
      expect(formatCurrencyWhole(1234.9)).toBe('$1,235');
      expect(formatCurrencyWhole(-1234.5)).toBe('-$1,234');
    });

    it('should handle edge cases', () => {
      expect(formatCurrencyWhole(0.1)).toBe('$0');
      expect(formatCurrencyWhole(0.5)).toBe('$1');
      expect(formatCurrencyWhole(0.9)).toBe('$1');
      expect(formatCurrencyWhole(-0.5)).toBe('-$0');
    });
  });
});
