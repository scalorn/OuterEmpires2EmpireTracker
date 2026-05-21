/**
 * Formats an ISO date string to a human-readable format.
 */
export function formatDate(iso: string): string {
  try {
    const date = new Date(iso);
    if (isNaN(date.getTime())) return iso;
    return date.toLocaleDateString(undefined, {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  } catch {
    return iso;
  }
}

/**
 * Formats a number with locale-appropriate separators.
 */
export function formatNumber(n: number): string {
  return n.toLocaleString();
}

/**
 * Formats a duration in minutes as "Xh Ym".
 */
export function formatDuration(minutes: number): string {
  if (minutes < 0) return '0m';
  const h = Math.floor(minutes / 60);
  const m = Math.round(minutes % 60);
  if (h === 0) return `${m}m`;
  if (m === 0) return `${h}h`;
  return `${h}h ${m}m`;
}

/**
 * Formats a resource quantity with K/M suffixes for large numbers.
 */
export function formatResourceQuantity(qty: number): string {
  if (qty >= 1_000_000) {
    const val = qty / 1_000_000;
    return `${val % 1 === 0 ? val.toFixed(0) : val.toFixed(1)}M`;
  }
  if (qty >= 1_000) {
    const val = qty / 1_000;
    return `${val % 1 === 0 ? val.toFixed(0) : val.toFixed(1)}K`;
  }
  return qty.toLocaleString();
}
