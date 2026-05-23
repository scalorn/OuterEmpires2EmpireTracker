import { describe, it, expect } from 'vitest';
import { moveUp, moveDown, resequence } from '../reorderUtils';

describe('moveUp', () => {
  it('swaps item with the one above it', () => {
    const items = ['a', 'b', 'c', 'd'];
    expect(moveUp(items, 2)).toEqual(['a', 'c', 'b', 'd']);
  });

  it('is a no-op when index is 0', () => {
    const items = ['a', 'b', 'c'];
    expect(moveUp(items, 0)).toEqual(['a', 'b', 'c']);
  });

  it('is a no-op when index is negative', () => {
    const items = ['a', 'b'];
    expect(moveUp(items, -1)).toEqual(['a', 'b']);
  });

  it('is a no-op when index is out of bounds', () => {
    const items = ['a', 'b'];
    expect(moveUp(items, 5)).toEqual(['a', 'b']);
  });

  it('returns a new array (does not mutate original)', () => {
    const items = ['a', 'b', 'c'];
    const result = moveUp(items, 1);
    expect(result).not.toBe(items);
    expect(items).toEqual(['a', 'b', 'c']);
  });

  it('works with single-element array', () => {
    expect(moveUp(['x'], 0)).toEqual(['x']);
  });

  it('works with empty array', () => {
    expect(moveUp([], 0)).toEqual([]);
  });
});

describe('moveDown', () => {
  it('swaps item with the one below it', () => {
    const items = ['a', 'b', 'c', 'd'];
    expect(moveDown(items, 1)).toEqual(['a', 'c', 'b', 'd']);
  });

  it('is a no-op when index is the last element', () => {
    const items = ['a', 'b', 'c'];
    expect(moveDown(items, 2)).toEqual(['a', 'b', 'c']);
  });

  it('is a no-op when index is negative', () => {
    const items = ['a', 'b'];
    expect(moveDown(items, -1)).toEqual(['a', 'b']);
  });

  it('is a no-op when index is out of bounds', () => {
    const items = ['a', 'b'];
    expect(moveDown(items, 5)).toEqual(['a', 'b']);
  });

  it('returns a new array (does not mutate original)', () => {
    const items = ['a', 'b', 'c'];
    const result = moveDown(items, 0);
    expect(result).not.toBe(items);
    expect(items).toEqual(['a', 'b', 'c']);
  });

  it('works with single-element array', () => {
    expect(moveDown(['x'], 0)).toEqual(['x']);
  });

  it('works with empty array', () => {
    expect(moveDown([], 0)).toEqual([]);
  });
});

describe('resequence', () => {
  it('assigns sequential numbers starting from 1', () => {
    const items = [
      { name: 'a', sequence: 10 },
      { name: 'b', sequence: 20 },
      { name: 'c', sequence: 30 },
    ];
    const result = resequence(items);
    expect(result).toEqual([
      { name: 'a', sequence: 1 },
      { name: 'b', sequence: 2 },
      { name: 'c', sequence: 3 },
    ]);
  });

  it('returns a new array (does not mutate original)', () => {
    const items = [{ sequence: 5 }, { sequence: 3 }];
    const result = resequence(items);
    expect(result).not.toBe(items);
    expect(items[0].sequence).toBe(5);
    expect(items[1].sequence).toBe(3);
  });

  it('handles empty array', () => {
    expect(resequence([])).toEqual([]);
  });

  it('handles single item', () => {
    expect(resequence([{ sequence: 99 }])).toEqual([{ sequence: 1 }]);
  });

  it('preserves other properties', () => {
    const items = [
      { id: 'x', name: 'first', sequence: 7 },
      { id: 'y', name: 'second', sequence: 3 },
    ];
    const result = resequence(items);
    expect(result[0]).toEqual({ id: 'x', name: 'first', sequence: 1 });
    expect(result[1]).toEqual({ id: 'y', name: 'second', sequence: 2 });
  });
});
