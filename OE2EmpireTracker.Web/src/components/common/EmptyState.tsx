interface EmptyStateProps {
  title?: string;
  message?: string;
}

export function EmptyState({
  title = 'No data',
  message = 'There is nothing to display yet.',
}: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center justify-center py-12">
      <h3 className="mb-1 text-lg font-medium text-gray-300">{title}</h3>
      <p className="text-sm text-gray-500">{message}</p>
    </div>
  );
}
