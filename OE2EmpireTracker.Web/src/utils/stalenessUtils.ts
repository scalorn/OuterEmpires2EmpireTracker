/**
 * Import staleness classification utilities for colony administration.
 *
 * Validates: Requirement 1.10
 */

export interface StalenessInfo {
  label: string;
  badgeClass: string;
}

/**
 * Computes the number of days since the last import.
 * Returns null if lastImportUtc is null/undefined or not a valid date.
 */
export function computeDaysSinceImport(
  lastImportUtc: string | undefined | null,
  now?: Date,
): number | null {
  if (!lastImportUtc) return null;
  const importDate = new Date(lastImportUtc);
  if (isNaN(importDate.getTime())) return null;
  const currentTime = now ?? new Date();
  const diffMs = currentTime.getTime() - importDate.getTime();
  return Math.floor(diffMs / (1000 * 60 * 60 * 24));
}

/**
 * Returns staleness classification based on days since last import.
 * 0–4 days: fresh (no color / gray)
 * 5–6 days: stale (yellow)
 * >6 days: very stale (red)
 * null: never imported (gray)
 */
export function getStalenessInfo(days: number | null): StalenessInfo {
  if (days === null) {
    return {
      label: 'Never imported',
      badgeClass: 'bg-gray-600 text-gray-200',
    };
  }
  if (days <= 4) {
    return {
      label: 'Fresh',
      badgeClass: 'bg-gray-600 text-gray-200',
    };
  }
  if (days <= 6) {
    return {
      label: `Stale (${days} days)`,
      badgeClass: 'bg-yellow-600 text-yellow-100',
    };
  }
  return {
    label: `Very stale (${days} days)`,
    badgeClass: 'bg-red-600 text-red-100',
  };
}
