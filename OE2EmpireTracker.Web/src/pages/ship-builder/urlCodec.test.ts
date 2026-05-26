import { describe, it, expect } from 'vitest';
import { encodeBuild, decodeBuild } from './urlCodec';
import type { ComponentSlot } from './slotTypes';

describe('encodeBuild', () => {
  it('produces correct format: #build=<hullUUID>:<comp0>,<comp1>,...', () => {
    const hullUUID = 'abc12345-1234-1234-1234-123456789abc';
    const slots: ComponentSlot[] = [
      { slotType: 'Reactor', slotIndex: 0, blueprintUUID: 'def12345-1234-1234-1234-123456789def' },
      { slotType: 'Shield', slotIndex: 0, blueprintUUID: 'aaa12345-1234-1234-1234-123456789aaa' },
    ];

    const result = encodeBuild(hullUUID, slots);

    expect(result).toBe(
      '#build=abc12345-1234-1234-1234-123456789abc:def12345-1234-1234-1234-123456789def,aaa12345-1234-1234-1234-123456789aaa'
    );
  });

  it('encodes empty slots as empty strings between commas', () => {
    const hullUUID = 'abc12345-1234-1234-1234-123456789abc';
    const slots: ComponentSlot[] = [
      { slotType: 'Reactor', slotIndex: 0, blueprintUUID: 'def12345-1234-1234-1234-123456789def' },
      { slotType: 'Reactor', slotIndex: 1, blueprintUUID: null },
      { slotType: 'Shield', slotIndex: 0, blueprintUUID: 'aaa12345-1234-1234-1234-123456789aaa' },
    ];

    const result = encodeBuild(hullUUID, slots);

    expect(result).toBe(
      '#build=abc12345-1234-1234-1234-123456789abc:def12345-1234-1234-1234-123456789def,,aaa12345-1234-1234-1234-123456789aaa'
    );
  });

  it('encodes all empty slots as all commas', () => {
    const hullUUID = 'abc12345-1234-1234-1234-123456789abc';
    const slots: ComponentSlot[] = [
      { slotType: 'Reactor', slotIndex: 0, blueprintUUID: null },
      { slotType: 'Reactor', slotIndex: 1, blueprintUUID: null },
      { slotType: 'Shield', slotIndex: 0, blueprintUUID: null },
    ];

    const result = encodeBuild(hullUUID, slots);

    expect(result).toBe('#build=abc12345-1234-1234-1234-123456789abc:,,');
  });

  it('encodes single slot with component correctly', () => {
    const hullUUID = 'abc12345-1234-1234-1234-123456789abc';
    const slots: ComponentSlot[] = [
      { slotType: 'Reactor', slotIndex: 0, blueprintUUID: 'def12345-1234-1234-1234-123456789def' },
    ];

    const result = encodeBuild(hullUUID, slots);

    expect(result).toBe(
      '#build=abc12345-1234-1234-1234-123456789abc:def12345-1234-1234-1234-123456789def'
    );
  });
});

describe('decodeBuild', () => {
  it('round-trips with encodeBuild', () => {
    const hullUUID = 'abc12345-1234-1234-1234-123456789abc';
    const slots: ComponentSlot[] = [
      { slotType: 'Reactor', slotIndex: 0, blueprintUUID: 'def12345-1234-1234-1234-123456789def' },
      { slotType: 'Reactor', slotIndex: 1, blueprintUUID: null },
      { slotType: 'Shield', slotIndex: 0, blueprintUUID: 'aaa12345-1234-1234-1234-123456789aaa' },
    ];

    const encoded = encodeBuild(hullUUID, slots);
    const decoded = decodeBuild(encoded);

    expect(decoded).not.toBeNull();
    expect(decoded).toEqual({
      hullUUID: 'abc12345-1234-1234-1234-123456789abc',
      componentUUIDs: [
        'def12345-1234-1234-1234-123456789def',
        null,
        'aaa12345-1234-1234-1234-123456789aaa',
      ],
    });
  });

  it('returns null for empty string', () => {
    expect(decodeBuild('')).toBeNull();
  });

  it('returns null for hash without #build= prefix', () => {
    expect(decodeBuild('#other=something')).toBeNull();
    expect(decodeBuild('#abc12345-1234-1234-1234-123456789abc')).toBeNull();
  });

  it('returns DecodeError with type malformed when no colon separator', () => {
    const result = decodeBuild('#build=abc12345-1234-1234-1234-123456789abc');

    expect(result).toEqual({
      type: 'malformed',
      message: 'Missing colon separator between hull UUID and components',
    });
  });

  it('returns DecodeError with type invalid-uuid for invalid hull UUID', () => {
    const result = decodeBuild('#build=not-a-valid-uuid:def12345-1234-1234-1234-123456789def');

    expect(result).toEqual({
      type: 'invalid-uuid',
      message: 'Invalid hull UUID: "not-a-valid-uuid"',
    });
  });

  it('returns DecodeError with type invalid-uuid for invalid component UUID', () => {
    const result = decodeBuild(
      '#build=abc12345-1234-1234-1234-123456789abc:not-a-valid-uuid'
    );

    expect(result).toEqual({
      type: 'invalid-uuid',
      message: 'Invalid component UUID: "not-a-valid-uuid"',
    });
  });
});
