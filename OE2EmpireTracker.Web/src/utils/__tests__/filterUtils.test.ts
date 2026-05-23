import { describe, it, expect } from 'vitest';
import {
  matchesTextFilter,
  matchesDropdownFilter,
  applyFilters,
  type FilterConfig,
} from '../filterUtils';

interface TestItem {
  name: string;
  type: string;
  level: number;
  active: boolean;
  notes?: string;
}

const items: TestItem[] = [
  { name: 'Alpha Colony', type: 'Mining', level: 3, active: true, notes: 'primary' },
  { name: 'Beta Station', type: 'Refining', level: 5, active: false, notes: 'secondary' },
  { name: 'Gamma Outpost', type: 'Mining', level: 1, active: true },
  { name: 'Delta Hub', type: 'Manufacturing', level: 7, active: false, notes: 'main hub' },
];

describe('matchesTextFilter', () => {
  it('returns true when query is empty', () => {
    expect(matchesTextFilter(items[0], ['name'], '')).toBe(true);
  });

  it('returns true when query is whitespace only', () => {
    expect(matchesTextFilter(items[0], ['name'], '   ')).toBe(true);
  });

  it('matches case-insensitively', () => {
    expect(matchesTextFilter(items[0], ['name'], 'alpha')).toBe(true);
    expect(matchesTextFilter(items[0], ['name'], 'ALPHA')).toBe(true);
    expect(matchesTextFilter(items[0], ['name'], 'AlPhA')).toBe(true);
  });

  it('matches substring within a field', () => {
    expect(matchesTextFilter(items[0], ['name'], 'Colony')).toBe(true);
    expect(matchesTextFilter(items[1], ['name'], 'Stat')).toBe(true);
  });

  it('returns false when no field matches', () => {
    expect(matchesTextFilter(items[0], ['name'], 'Zeta')).toBe(false);
  });

  it('matches across multiple fields', () => {
    expect(matchesTextFilter(items[0], ['name', 'type'], 'Mining')).toBe(true);
    expect(matchesTextFilter(items[0], ['name', 'notes'], 'primary')).toBe(true);
  });

  it('handles undefined field values gracefully', () => {
    expect(matchesTextFilter(items[2], ['notes'], 'anything')).toBe(false);
  });

  it('converts non-string field values to string for matching', () => {
    expect(matchesTextFilter(items[0], ['level'], '3')).toBe(true);
  });
});

describe('matchesDropdownFilter', () => {
  it('returns true when selected is null', () => {
    expect(matchesDropdownFilter('Mining', null)).toBe(true);
  });

  it('returns true when selected is empty string', () => {
    expect(matchesDropdownFilter('Mining', '')).toBe(true);
  });

  it('returns true when value matches selected', () => {
    expect(matchesDropdownFilter('Mining', 'Mining')).toBe(true);
  });

  it('returns false when value does not match selected', () => {
    expect(matchesDropdownFilter('Mining', 'Refining')).toBe(false);
  });

  it('handles undefined value', () => {
    expect(matchesDropdownFilter(undefined, 'Mining')).toBe(false);
    expect(matchesDropdownFilter(undefined, null)).toBe(true);
  });
});

describe('applyFilters', () => {
  it('returns all items when filters array is empty', () => {
    expect(applyFilters(items, [])).toEqual(items);
  });

  it('filters by text filter', () => {
    const filters: FilterConfig<TestItem>[] = [
      { type: 'text', fields: ['name'], query: 'Colony' },
    ];
    const result = applyFilters(items, filters);
    expect(result).toHaveLength(1);
    expect(result[0].name).toBe('Alpha Colony');
  });

  it('filters by dropdown filter', () => {
    const filters: FilterConfig<TestItem>[] = [
      { type: 'dropdown', field: 'type', selected: 'Mining' },
    ];
    const result = applyFilters(items, filters);
    expect(result).toHaveLength(2);
    expect(result.every((i) => i.type === 'Mining')).toBe(true);
  });

  it('filters by checkbox filter when checked', () => {
    const filters: FilterConfig<TestItem>[] = [
      { type: 'checkbox', field: 'active', checked: true },
    ];
    const result = applyFilters(items, filters);
    expect(result).toHaveLength(2);
    expect(result.every((i) => i.active)).toBe(true);
  });

  it('does not filter by checkbox when unchecked', () => {
    const filters: FilterConfig<TestItem>[] = [
      { type: 'checkbox', field: 'active', checked: false },
    ];
    const result = applyFilters(items, filters);
    expect(result).toHaveLength(4);
  });

  it('applies multiple filters with AND logic', () => {
    const filters: FilterConfig<TestItem>[] = [
      { type: 'dropdown', field: 'type', selected: 'Mining' },
      { type: 'text', fields: ['name'], query: 'gamma' },
    ];
    const result = applyFilters(items, filters);
    expect(result).toHaveLength(1);
    expect(result[0].name).toBe('Gamma Outpost');
  });

  it('returns empty array when no items match all filters', () => {
    const filters: FilterConfig<TestItem>[] = [
      { type: 'dropdown', field: 'type', selected: 'Mining' },
      { type: 'text', fields: ['name'], query: 'Delta' },
    ];
    const result = applyFilters(items, filters);
    expect(result).toHaveLength(0);
  });

  it('handles inactive dropdown filter (null selected)', () => {
    const filters: FilterConfig<TestItem>[] = [
      { type: 'dropdown', field: 'type', selected: null },
    ];
    const result = applyFilters(items, filters);
    expect(result).toHaveLength(4);
  });

  it('handles empty text query as no-op filter', () => {
    const filters: FilterConfig<TestItem>[] = [
      { type: 'text', fields: ['name'], query: '' },
    ];
    const result = applyFilters(items, filters);
    expect(result).toHaveLength(4);
  });
});
