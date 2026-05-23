/**
 * Timer utility functions for countdown computation and formatting.
 * Used by CountdownTimer component and ColonyActivityPage.
 *
 * Validates: Requirements 11.2, 18.3, 18.4
 */

/**
 * Computes the number of seconds remaining until endTime.
 * Returns null if endTime is null/undefined.
 * Returns 0 if endTime is in the past.
 *
 * @param endTime - ISO 8601 end timestamp, or null/undefined
 * @param now - Current time (defaults to new Date())
 * @returns Seconds remaining (>= 0), or null if no endTime provided
 */
export function computeRemaining(
  endTime: string | null | undefined,
  now?: Date
): number | null {
  if (endTime == null) return null;

  const end = new Date(endTime).getTime();
  if (isNaN(end)) return null;

  const current = (now ?? new Date()).getTime();
  const diffMs = end - current;

  return diffMs <= 0 ? 0 : Math.floor(diffMs / 1000);
}

/**
 * Formats a number of seconds as a countdown string.
 * Returns "HH:MM:SS" if >= 1 hour, "MM:SS" if under 1 hour.
 * Returns "00:00" for zero or negative values.
 *
 * @param seconds - Number of seconds to format (non-negative)
 * @returns Formatted countdown string
 */
export function formatCountdown(seconds: number): string {
  if (seconds <= 0) return '00:00';

  const h = Math.floor(seconds / 3600);
  const m = Math.floor((seconds % 3600) / 60);
  const s = Math.floor(seconds % 60);

  const mm = String(m).padStart(2, '0');
  const ss = String(s).padStart(2, '0');

  if (h > 0) {
    const hh = String(h).padStart(2, '0');
    return `${hh}:${mm}:${ss}`;
  }

  return `${mm}:${ss}`;
}

/**
 * Checks whether the given endTime is in the past (or null/undefined).
 * Returns true if endTime is null, undefined, or represents a time that has passed.
 *
 * @param endTime - ISO 8601 end timestamp, or null/undefined
 * @returns true if expired or no endTime provided
 */
export function isExpired(endTime: string | null | undefined): boolean {
  if (endTime == null) return true;

  const end = new Date(endTime).getTime();
  if (isNaN(end)) return true;

  return Date.now() <= end ? false : true;
}
