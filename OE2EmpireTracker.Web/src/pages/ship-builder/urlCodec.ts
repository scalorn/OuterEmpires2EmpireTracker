/**
 * URL codec for encoding/decoding ship build state into compact hash fragments.
 * Format: #build=<hullUUID>:<comp0UUID>,<comp1UUID>,...
 * Empty slots are encoded as empty string between commas.
 * Example: #build=abc12345-1234-1234-1234-123456789abc:def12345-1234-1234-1234-123456789abc,,ghi12345-1234-1234-1234-123456789abc
 */

import type { ComponentSlot } from './slotTypes';

/** Standard UUID format: 8-4-4-4-12 hex characters */
const UUID_REGEX = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

const BUILD_PREFIX = '#build=';

export interface DecodedBuild {
  hullUUID: string;
  componentUUIDs: (string | null)[];
}

export interface DecodeError {
  type: 'malformed' | 'invalid-uuid';
  message: string;
}

/**
 * Encodes build state into a compact URL hash fragment.
 * Format: #build=<hullUUID>:<comp0UUID>,<comp1UUID>,...
 * Empty slots are encoded as empty string between commas.
 */
export function encodeBuild(hullUUID: string, slots: ComponentSlot[]): string {
  const components = slots
    .map((slot) => slot.blueprintUUID ?? '')
    .join(',');
  return `${BUILD_PREFIX}${hullUUID}:${components}`;
}

/**
 * Decodes a URL hash fragment into build state.
 * Returns null if the hash is empty or doesn't contain build data.
 * Returns a DecodeError if the format is invalid.
 */
export function decodeBuild(hash: string): DecodedBuild | DecodeError | null {
  if (!hash || !hash.startsWith(BUILD_PREFIX)) {
    return null;
  }

  const payload = hash.slice(BUILD_PREFIX.length);
  if (!payload) {
    return null;
  }

  const colonIndex = payload.indexOf(':');
  if (colonIndex === -1) {
    return {
      type: 'malformed',
      message: 'Missing colon separator between hull UUID and components',
    };
  }

  const hullUUID = payload.slice(0, colonIndex);
  if (!UUID_REGEX.test(hullUUID)) {
    return {
      type: 'invalid-uuid',
      message: `Invalid hull UUID: "${hullUUID}"`,
    };
  }

  const componentsPart = payload.slice(colonIndex + 1);
  const componentUUIDs: (string | null)[] = [];

  if (componentsPart.length > 0) {
    const parts = componentsPart.split(',');
    for (const part of parts) {
      if (part === '') {
        componentUUIDs.push(null);
      } else if (UUID_REGEX.test(part)) {
        componentUUIDs.push(part);
      } else {
        return {
          type: 'invalid-uuid',
          message: `Invalid component UUID: "${part}"`,
        };
      }
    }
  }

  return { hullUUID, componentUUIDs };
}
