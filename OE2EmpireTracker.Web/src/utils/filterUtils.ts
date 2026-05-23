/**
 * Generic filter utility functions for client-side entity list filtering.
 * Reusable across all entity list pages (colonies, blueprints, surveys, etc.).
 *
 * Requirements: 10.3, 2.2, 3.2
 */

export interface TextFilter<T> {
  type: 'text';
  fields: (keyof T)[];
  query: string;
}

export interface DropdownFilter<T> {
  type: 'dropdown';
  field: keyof T;
  selected: string | null;
}

export interface CheckboxFilter<T> {
  type: 'checkbox';
  field: keyof T;
  checked: boolean;
}

export type FilterConfig<T> = TextFilter<T> | DropdownFilter<T> | CheckboxFilter<T>;

/**
 * Returns true if any of the specified fields on the item contain the query
 * as a case-insensitive substring.
 * Returns true if query is empty or whitespace-only (no filter active).
 */
export function matchesTextFilter<T>(item: T, fields: (keyof T)[], query: string): boolean {
  const trimmed = query.trim().toLowerCase();
  if (trimmed === '') return true;

  return fields.some((field) => {
    const value = item[field];
    if (value == null) return false;
    return String(value).toLowerCase().includes(trimmed);
  });
}

/**
 * Returns true if no dropdown selection is active (null or empty string),
 * or if the value matches the selected option (case-sensitive).
 */
export function matchesDropdownFilter(value: string | undefined, selected: string | null): boolean {
  if (selected == null || selected === '') return true;
  return value === selected;
}

/**
 * Applies all filters to the items array. Returns items that match ALL filter criteria.
 * An empty filters array returns all items unchanged.
 */
export function applyFilters<T>(items: T[], filters: FilterConfig<T>[]): T[] {
  if (filters.length === 0) return items;

  return items.filter((item) =>
    filters.every((filter) => {
      switch (filter.type) {
        case 'text':
          return matchesTextFilter(item, filter.fields, filter.query);
        case 'dropdown':
          return matchesDropdownFilter(item[filter.field] as string | undefined, filter.selected);
        case 'checkbox':
          if (!filter.checked) return true;
          return Boolean(item[filter.field]);
        default:
          return true;
      }
    }),
  );
}
