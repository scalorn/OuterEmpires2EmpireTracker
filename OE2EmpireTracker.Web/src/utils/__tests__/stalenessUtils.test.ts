import { describe, it, expect } from 'vitest';
import { computeDaysSinceImport, getStalenessInfo } from '../stalenessUtils';

describe('computeDaysSinceImport', () => {
  const now = new Date('2025-01-15T12:00:00Z');

  it('returns null when lastImportUtc is null', () => {
    expect(computeDaysSinceImport(null, now)).toBeNull();
  });

  it('returns null when lastImportUtc is undefined', () => {
    expect(computeDaysSinceImport(undefined, now)).toBeNull();
  });

  it('returns null when lastImportUtc is empty string', () => {
    expect(computeDaysSinceImport('', now)).toBeNull();
  });

  it('returns null when lastImportUtc is invalid date', () => {
    expect(computeDaysSinceImport('not-a-date', now)).toBeNull();
  });

  it('returns 0 for same-day import', () => {
    expect(computeDaysSinceImport('2025-01-15T08:00:00Z', now)).toBe(0);
  });

  it('returns correct days for recent import', () => {
    expect(computeDaysSinceImport('2025-01-12T12:00:00Z', now)).toBe(3);
  });

  it('returns correct days for stale import (5 days)', () => {
    expect(computeDaysSinceImport('2025-01-10T12:00:00Z', now)).toBe(5);
  });

  it('returns correct days for very stale import (7 days)', () => {
    expect(computeDaysSinceImport('2025-01-08T12:00:00Z', now)).toBe(7);
  });

  it('floors partial days', () => {
    // 2.5 days ago should return 2
    expect(computeDaysSinceImport('2025-01-13T00:00:00Z', now)).toBe(2);
  });
});

describe('getStalenessInfo', () => {
  it('returns "Never imported" for null days', () => {
    const result = getStalenessInfo(null);
    expect(result.label).toBe('Never imported');
    expect(result.badgeClass).toContain('gray');
  });

  it('returns "Fresh" for 0 days', () => {
    const result = getStalenessInfo(0);
    expect(result.label).toBe('Fresh');
    expect(result.badgeClass).toContain('gray');
  });

  it('returns "Fresh" for 4 days', () => {
    const result = getStalenessInfo(4);
    expect(result.label).toBe('Fresh');
    expect(result.badgeClass).toContain('gray');
  });

  it('returns yellow "Stale" for 5 days', () => {
    const result = getStalenessInfo(5);
    expect(result.label).toBe('Stale (5 days)');
    expect(result.badgeClass).toContain('yellow');
  });

  it('returns yellow "Stale" for 6 days', () => {
    const result = getStalenessInfo(6);
    expect(result.label).toBe('Stale (6 days)');
    expect(result.badgeClass).toContain('yellow');
  });

  it('returns red "Very stale" for 7 days', () => {
    const result = getStalenessInfo(7);
    expect(result.label).toBe('Very stale (7 days)');
    expect(result.badgeClass).toContain('red');
  });

  it('returns red "Very stale" for 30 days', () => {
    const result = getStalenessInfo(30);
    expect(result.label).toBe('Very stale (30 days)');
    expect(result.badgeClass).toContain('red');
  });
});
