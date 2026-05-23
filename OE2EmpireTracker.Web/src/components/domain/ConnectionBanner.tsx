interface ConnectionBannerProps {
  status: 'connected' | 'reconnecting' | 'disconnected';
  onRetry?: () => void;
}

export function ConnectionBanner({ status, onRetry }: ConnectionBannerProps) {
  if (status === 'connected') {
    return null;
  }

  if (status === 'reconnecting') {
    return (
      <div
        role="status"
        aria-live="polite"
        className="fixed top-14 right-0 left-0 z-40 flex items-center justify-center gap-2 bg-amber-600 px-4 py-2 text-sm font-medium text-white shadow-md"
      >
        <svg
          className="h-4 w-4 animate-spin"
          xmlns="http://www.w3.org/2000/svg"
          fill="none"
          viewBox="0 0 24 24"
          aria-hidden="true"
        >
          <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
          <path
            className="opacity-75"
            fill="currentColor"
            d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
          />
        </svg>
        <span>Reconnecting...</span>
      </div>
    );
  }

  return (
    <div
      role="alert"
      aria-live="assertive"
      className="fixed top-14 right-0 left-0 z-40 flex items-center justify-center gap-3 bg-red-600 px-4 py-2 text-sm font-medium text-white shadow-md"
    >
      <span>Connection lost</span>
      {onRetry && (
        <button
          type="button"
          onClick={onRetry}
          className="rounded bg-white/20 px-3 py-1 text-xs font-semibold text-white transition hover:bg-white/30 focus:ring-2 focus:ring-white focus:outline-none"
        >
          Retry
        </button>
      )}
    </div>
  );
}
