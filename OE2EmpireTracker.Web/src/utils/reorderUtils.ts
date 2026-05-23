/**
 * Utility functions for reordering items in ordered lists.
 * Used by delivery routes (stops) and supply chains (steps).
 *
 * All functions return new arrays (immutable).
 */

/**
 * Swaps the item at `index` with the item at `index - 1`.
 * No-op if index is 0 or out of bounds.
 */
export function moveUp<T>(items: T[], index: number): T[] {
  if (index <= 0 || index >= items.length) {
    return [...items];
  }
  const result = [...items];
  const temp = result[index - 1];
  result[index - 1] = result[index];
  result[index] = temp;
  return result;
}

/**
 * Swaps the item at `index` with the item at `index + 1`.
 * No-op if index is the last element or out of bounds.
 */
export function moveDown<T>(items: T[], index: number): T[] {
  if (index < 0 || index >= items.length - 1) {
    return [...items];
  }
  const result = [...items];
  const temp = result[index + 1];
  result[index + 1] = result[index];
  result[index] = temp;
  return result;
}

/**
 * Assigns sequential numbers (1, 2, 3...) to the `sequence` field
 * based on array position. Returns a new array with updated sequence values.
 */
export function resequence<T extends { sequence: number }>(items: T[]): T[] {
  return items.map((item, i) => ({ ...item, sequence: i + 1 }));
}
