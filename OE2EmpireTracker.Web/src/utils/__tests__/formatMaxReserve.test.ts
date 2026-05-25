import fc from 'fast-check';
import { describe, it, expect } from 'vitest';
import { formatMaxReserve } from '../formatters';

// Feature: asteroid-reserves-display, Property 5: Max reserve formatting
/**
 * Property tests for formatMaxReserve.
 * Validates: Requirements 4.1, 4.4
 *
 * Property 5: Max reserve formatting
 * - Non-negative integers produce string containing only digits and thousands separators
 * - Zero displays as "0"
 * - undefined produces "-"
 * - null produces "-"
 * - No decimal point in output for integer inputs
 */

describe('Property 5: Max reserve formatting', () => {
  it('non-negative integers produce string containing only digits and thousands separators', () => {
    fc.assert(
      fc.property(
        fc.nat(),
        (value) => {
          const result = formatMaxReserve(value);
          // Result should only contain digits and locale-specific group separators (no decimal point, no letters)
          // Remove all digits to see what separators remain
          const nonDigits = result.replace(/\d/g, '');
          // Every non-digit character should be a thousands separator (comma, period, space, narrow no-break space, etc.)
          // The key property: no decimal point behavior, no negative sign, no letters
          for (const ch of nonDigits) {
            // Acceptable separator characters used by various locales
            const acceptable = [',', '.', ' ', '\u00A0', '\u202F', "'"];
            expect(acceptable).toContain(ch);
          }
          // Result must not be empty
          expect(result.length).toBeGreaterThan(0);
        },
      ),
      { numRuns: 100 },
    );
  });

  it('zero displays as "0"', () => {
    const result = formatMaxReserve(0);
    expect(result).toBe('0');
  });

  it('undefined produces "-"', () => {
    const result = formatMaxReserve(undefined);
    expect(result).toBe('-');
  });

  it('null produces "-"', () => {
    const result = formatMaxReserve(null);
    expect(result).toBe('-');
  });

  it('no decimal point in output for integer inputs', () => {
    fc.assert(
      fc.property(
        fc.nat(),
        (value) => {
          const result = formatMaxReserve(value);
          // In locales that use '.' as thousands separator, we can't simply check for '.'
          // Instead, verify the numeric value parsed back equals the input (no fractional part)
          // A simpler check: the result should not contain both a group separator AND a decimal separator
          // The strongest property: removing all non-digit chars and parsing should give back the original
          const digitsOnly = result.replace(/\D/g, '');
          expect(Number(digitsOnly)).toBe(value);
        },
      ),
      { numRuns: 100 },
    );
  });
});
