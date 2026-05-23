import { describe, it, expect } from 'vitest';
import { computeRemaining, formatCountdown, isExpired } from '../timerUtils';

/**
 * Unit tests for timerUtils.
 * Validates: Requirements 11.2, 18.3, 18.4
 */

describe('computeRemaining', () => {
  it('returns null when endTime is null', () => {
    expect(computeRemaining(null)).toBeNull();
  });

  it('returns null when endTime is undefined', () => {
    expect(computeRemaining(undefined)).toBeNull();
  });

  it('returns null for invalid date strings', () => {
    expect(computeRemaining('not-a-date')).toBeNull();
  });

  it('returns 0 when endTime is in the past', () => {
    const past = new Date(Date.now() - 60_000).toISOString();
    const now = new Date();
    expect(computeRemaining(past, now)).toBe(0);
  });

  it('returns correct seconds remaining for a future endTime', () => {
    const now = new Date('2024-01-01T12:00:00Z');
    const endTime = '2024-01-01T12:05:00Z'; // 5 minutes = 300 seconds
    expect(computeRemaining(endTime, now)).toBe(300);
  });

  it('returns 0 when endTime equals now', () => {
    const now = new Date('2024-01-01T12:00:00Z');
    const endTime = '2024-01-01T12:00:00Z';
    expect(computeRemaining(endTime, now)).toBe(0);
  });

  it('floors fractional seconds', () => {
    const now = new Date('2024-01-01T12:00:00.500Z');
    const endTime = '2024-01-01T12:00:01.200Z'; // 700ms = 0 full seconds
    expect(computeRemaining(endTime, now)).toBe(0);
  });

  it('handles large time differences', () => {
    const now = new Date('2024-01-01T00:00:00Z');
    const endTime = '2024-01-02T00:00:00Z'; // 24 hours = 86400 seconds
    expect(computeRemaining(endTime, now)).toBe(86400);
  });
});

describe('formatCountdown', () => {
  it('returns "00:00" for zero seconds', () => {
    expect(formatCountdown(0)).toBe('00:00');
  });

  it('returns "00:00" for negative seconds', () => {
    expect(formatCountdown(-10)).toBe('00:00');
  });

  it('formats seconds under a minute as MM:SS', () => {
    expect(formatCountdown(45)).toBe('00:45');
  });

  it('formats minutes and seconds as MM:SS when under 1 hour', () => {
    expect(formatCountdown(125)).toBe('02:05'); // 2 min 5 sec
  });

  it('formats exactly 1 hour as HH:MM:SS', () => {
    expect(formatCountdown(3600)).toBe('01:00:00');
  });

  it('formats hours, minutes, and seconds as HH:MM:SS', () => {
    expect(formatCountdown(3661)).toBe('01:01:01');
  });

  it('pads single-digit values with leading zeros', () => {
    expect(formatCountdown(61)).toBe('01:01');
    expect(formatCountdown(3601)).toBe('01:00:01');
  });

  it('handles large values (multi-digit hours)', () => {
    expect(formatCountdown(86400)).toBe('24:00:00'); // 24 hours
  });
});

describe('isExpired', () => {
  it('returns true when endTime is null', () => {
    expect(isExpired(null)).toBe(true);
  });

  it('returns true when endTime is undefined', () => {
    expect(isExpired(undefined)).toBe(true);
  });

  it('returns true for invalid date strings', () => {
    expect(isExpired('garbage')).toBe(true);
  });

  it('returns true when endTime is in the past', () => {
    const past = new Date(Date.now() - 60_000).toISOString();
    expect(isExpired(past)).toBe(true);
  });

  it('returns false when endTime is in the future', () => {
    const future = new Date(Date.now() + 60_000).toISOString();
    expect(isExpired(future)).toBe(false);
  });
});
