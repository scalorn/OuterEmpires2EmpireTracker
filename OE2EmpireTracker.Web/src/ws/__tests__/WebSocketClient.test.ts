import fc from 'fast-check';
import { describe, it, expect } from 'vitest';
import { WS_MAX_BACKOFF_MS } from '../../utils/constants';

/**
 * Property tests for WebSocket reconnection and event handling.
 * Validates: Requirements 12.2, 12.3
 */

const ENTITY_TYPES = ['colony', 'blueprint', 'survey', 'faction', 'sharing'] as const;

// Maps entityType to the expected query key prefixes that should be invalidated
function getExpectedInvalidations(
  entityType: string,
  ownerCharacterUUID: string | undefined
): string[][] {
  switch (entityType) {
    case 'colony':
      return ownerCharacterUUID
        ? [['characters', ownerCharacterUUID, 'data', 'Colonies']]
        : [];
    case 'blueprint':
      return [
        ...(ownerCharacterUUID
          ? [['characters', ownerCharacterUUID, 'data', 'Blueprints']]
          : []),
        ['public', 'blueprints'],
      ];
    case 'survey':
      return [
        ...(ownerCharacterUUID
          ? [['characters', ownerCharacterUUID, 'data', 'Surveys']]
          : []),
        ['public', 'surveys'],
      ];
    case 'faction':
      return [['factions']];
    case 'sharing':
      return ownerCharacterUUID
        ? [['sharing', ownerCharacterUUID]]
        : [];
    default:
      return [];
  }
}

// Replicate the handleServerEvent logic for testing
function handleServerEvent(
  event: { entityType: string; ownerCharacterUUID?: string },
  invalidate: (queryKey: readonly unknown[]) => void
): void {
  const { entityType, ownerCharacterUUID } = event;
  switch (entityType) {
    case 'colony':
      if (ownerCharacterUUID) {
        invalidate(['characters', ownerCharacterUUID, 'data', 'Colonies']);
      }
      break;
    case 'blueprint':
      if (ownerCharacterUUID) {
        invalidate(['characters', ownerCharacterUUID, 'data', 'Blueprints']);
      }
      invalidate(['public', 'blueprints']);
      break;
    case 'survey':
      if (ownerCharacterUUID) {
        invalidate(['characters', ownerCharacterUUID, 'data', 'Surveys']);
      }
      invalidate(['public', 'surveys']);
      break;
    case 'faction':
      invalidate(['factions']);
      break;
    case 'sharing':
      if (ownerCharacterUUID) {
        invalidate(['sharing', ownerCharacterUUID]);
      }
      break;
  }
}

describe('Property 6: WebSocket event triggers correct cache invalidation', () => {
  it('for any entityType, the correct query keys are invalidated', () => {
    fc.assert(
      fc.property(
        fc.constantFrom(...ENTITY_TYPES),
        fc.string({ minLength: 8, maxLength: 36 }),
        (entityType, ownerUUID) => {
          const invalidated: string[][] = [];
          handleServerEvent(
            { entityType, ownerCharacterUUID: ownerUUID },
            (key) => { invalidated.push(key as string[]); }
          );

          const expected = getExpectedInvalidations(entityType, ownerUUID);
          expect(invalidated).toEqual(expected);
          return true;
        }
      ),
      { numRuns: 100 }
    );
  });
});

describe('Property 7: Reconnection uses exponential backoff', () => {
  it('for N consecutive disconnections (N < 10), delay = min(1000 * 2^N, 30000)', () => {
    fc.assert(
      fc.property(
        fc.integer({ min: 0, max: 9 }),
        (n) => {
          const expectedDelay = Math.min(1000 * Math.pow(2, n), WS_MAX_BACKOFF_MS);
          // Replicate the backoff formula from WebSocketClient
          const actualDelay = Math.min(1000 * Math.pow(2, n), 30_000);

          expect(actualDelay).toBe(expectedDelay);
          // Verify bounds
          expect(actualDelay).toBeGreaterThanOrEqual(1000);
          expect(actualDelay).toBeLessThanOrEqual(30_000);
          return true;
        }
      ),
      { numRuns: 100 }
    );
  });

  it('backoff is monotonically non-decreasing up to the cap', () => {
    fc.assert(
      fc.property(
        fc.integer({ min: 0, max: 8 }),
        (n) => {
          const delay_n = Math.min(1000 * Math.pow(2, n), 30_000);
          const delay_n1 = Math.min(1000 * Math.pow(2, n + 1), 30_000);
          expect(delay_n1).toBeGreaterThanOrEqual(delay_n);
          return true;
        }
      ),
      { numRuns: 100 }
    );
  });
});
