interface RetryableErrorProps {
  message?: string;
  onRetry?: () => void;
}

export function RetryableError({
  message = 'Something went wrong. Please try again.',
  onRetry,
}: RetryableErrorProps) {
  return (
    <div className="flex flex-col items-center justify-center py-12" role="alert">
      <p className="mb-4 text-sm text-red-400">{message}</p>
      {onRetry && (
        <button
          onClick={onRetry}
          className="rounded bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700"
        >
          Retry
        </button>
      )}
    </div>
  );
}
