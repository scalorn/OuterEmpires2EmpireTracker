export function LoadingSpinner({ message = 'Loading...' }: { message?: string }) {
  return (
    <div className="flex flex-col items-center justify-center py-12" role="status">
      <div className="h-8 w-8 animate-spin rounded-full border-4 border-gray-600 border-t-blue-500" />
      <p className="mt-3 text-sm text-gray-400">{message}</p>
    </div>
  );
}
